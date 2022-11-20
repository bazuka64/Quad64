using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Syroot.BinaryData;
using System.Xml;
using System.Text.Json;

// last impact: end command not exist problem
// sr8 デザイアドライブ and so on: too
// ASA p4heartbeat: loop command not implemented

namespace Quad64
{
    class MasterTrackChunk
    {
        public TrackChunk trackChunk;
        public int channel;
        public XmlNode item;
        public JsonElement inst;
        public int rawInst;
    }

    internal class OutputMIDI
    {
        MidiFile midiFile = new MidiFile();

        long seqTimestamp = 0;
        long channelTimestamp = 0;
        long layerTimestamp = 0;

        XmlNode indexentry;
        JsonElement instrument_list;
        JsonElement instruments;

        List<MasterTrackChunk> masterTrackChunks = new List<MasterTrackChunk>();

        public MidiFile ConvertToMIDI(Sequence seq)
        {
            BinaryDataReader br = new BinaryDataReader(new MemoryStream(seq.data));
            br.ByteConverter = ByteConverter.BigEndian;

            // 楽器変換用のxml, jsonの読み込み
            int instSetID = seq.insts[0];
            XmlDocument xml = new XmlDocument();
            xml.Load("../../../../instrument/sm64_info.xml");
            indexentry = xml.SelectSingleNode($"RomDesc/audiobankidx/indexentry[@index={instSetID}]");

            string jsonFile = $"{instSetID:X2}.json";
            string jsonText = File.ReadAllText($"../../../../instrument/sound_banks/{jsonFile}");
            JsonDocument json = JsonDocument.Parse(jsonText);
            instrument_list = json.RootElement.GetProperty("instrument_list");
            instruments = json.RootElement.GetProperty("instruments");

            for(int i = 0; i < 17; i++)
            {
                TrackChunk trackChunk = new TrackChunk();
                midiFile.Chunks.Add(trackChunk);

                MasterTrackChunk masterTrackChunk = new MasterTrackChunk();
                masterTrackChunks.Add(masterTrackChunk);
                masterTrackChunk.trackChunk = trackChunk;
                masterTrackChunk.channel = i;
            }

            ParseSequence(br);

            // ドラムの音符の調整
            foreach(var masterTrackChunk in masterTrackChunks)
            {
                if(masterTrackChunk.rawInst == 0x7f)
                {
                    // general percussion
                    GeneralPercussionProcess(masterTrackChunk);
                }
                else if(masterTrackChunk.item != null)
                {
                    if (masterTrackChunk.item.Attributes["map"].InnerText == "drum")
                    {
                        // other drums
                        DrumProcess(masterTrackChunk);
                    }
                }
            }

            // 4分音符あたりのtick数
            midiFile.TimeDivision = new TicksPerQuarterNoteTimeDivision(48);

            return midiFile;
        }

        void ParseSequence(BinaryDataReader br)
        {
            seqTimestamp = 0;

            MidiEvent midiEvent;
            TimedEvent timedEvent;

            while (true)
            {
                byte cmd = br.ReadByte();

                if(cmd >= 0xc0)
                {
                    // 細かい命令
                    switch (cmd)
                    {
                        case 0xff:
                            // end
                            return;
                        case 0xfd:
                            // timestamp
                            seqTimestamp += ReadParameter(br);
                            break;
                        case 0xfb:
                            // branch always
                            uint jumpTo = br.ReadUInt16();
                            if(jumpTo > br.BaseStream.Position)
                            {
                                // ループ処理でなければジャンプする
                                br.BaseStream.Position = jumpTo;
                            }
                            break;
                        case 0xf9:
                            // branch if
                            br.ReadUInt16();
                            break;
                        case 0xf5:
                            // branch if
                            br.ReadUInt16();
                            break;
                        case 0xdd:
                            // tempo
                            // bpm: 1分あたりの4分音符数
                            int bpm = br.ReadByte();
                            long result = (long)((1 / (double)bpm) * 60 * 1000 * 1000);
                            midiEvent = new SetTempoEvent()
                            {
                                MicrosecondsPerQuarterNote = result,
                            };
                            timedEvent = new TimedEvent(midiEvent, seqTimestamp);

                            // set tempo in trackChunk[0]
                            TrackChunk trackChunk = (TrackChunk)midiFile.Chunks[0];
                            TimedObjectsManager<TimedEvent> tom = trackChunk.ManageTimedEvents();
                            tom.Objects.Add(timedEvent);
                            tom.SaveChanges();
                            break;
                        case 0xdb:
                            // master volume
                            br.ReadByte();
                            break;
                        case 0xd7:
                            // enable channel
                            br.ReadUInt16();
                            break;
                        case 0xd6:
                            // disable channel
                            br.ReadUInt16();
                            break;
                        case 0xd5:
                            // set mute scale
                            br.ReadSByte();
                            break;
                        case 0xd3:
                            // set mute bhv
                            br.ReadByte();
                            break;
                        default:
                            throw new Exception("not implemented command");
                            break;
                    }
                }
                else
                {
                    // cmd <= 0xbf
                    // 大まかな命令
                    int loBits = cmd & 0x0f;
                    switch(cmd & 0xf0)
                    {
                        case 0x90:
                            // set channel
                            ushort channelOffset = br.ReadUInt16();
                            long returnOffset = br.BaseStream.Position;
                            br.BaseStream.Position = channelOffset;

                            
                            if(loBits == 9)
                            {
                                // 10チャネルなら別の空いてるチャンネルに移行
                                // 埋まってる場合がある
                                //MasterTrackChunk masterChunk = masterTrackChunks.First(master => master.trackChunk.Events.Count == 0);
                                //loBits = masterChunk.channel;

                                // チャンネル16に割当
                                loBits = 15;
                            }

                            ParseChannel(br, loBits);
                            br.BaseStream.Position = returnOffset;
                            break;
                        case 0x80:
                            // get variation
                            break;
                        default:
                            throw new Exception("not implemented command");
                            break;
                    }
                }
            }
        }

        // できればチャンネルイベントの処理を済ませてからレイヤーイベントの処理に入りたい
        void ParseChannel(BinaryDataReader br, int channel)
        {
            TrackChunk trackChunk = (TrackChunk)midiFile.Chunks[channel];
            TimedObjectsManager<TimedEvent> tom = trackChunk.ManageTimedEvents();

            channelTimestamp = 0;

            ChannelEvent channelEvent;
            TimedEvent timedEvent;

            Stack<long> returnOffsets = new Stack<long>();

            while (true)
            {
                byte cmd = br.ReadByte();

                if (cmd >= 0xc0)
                {
                    // 細かい命令
                    switch (cmd)
                    {
                        case 0xff:
                            // end
                            if (returnOffsets.Count > 0)
                            {
                                br.BaseStream.Position = returnOffsets.Pop();
                                break;
                            }
                            tom.SaveChanges();
                            return;
                        case 0xfd:
                            // timestamp
                            channelTimestamp += ReadParameter(br);
                            break;
                        case 0xfc:
                            // call
                            ushort jumpTo = br.ReadUInt16();
                            long retrunOffset = br.BaseStream.Position;
                            returnOffsets.Push(retrunOffset);
                            br.BaseStream.Position = jumpTo;
                            break;
                        case 0xdf:
                            // channel volume
                            int volume = br.ReadByte();
                            // sm64 0-255
                            // midi 0-127
                            volume &= 0x7f; // 最大ボリューム頭打ち
                            channelEvent = new ControlChangeEvent()
                            {
                                ControlNumber = (SevenBitNumber)(byte)ControlName.ChannelVolume,
                                ControlValue = (SevenBitNumber)volume,
                                Channel = (FourBitNumber)channel,
                            };
                            timedEvent = new TimedEvent(channelEvent, seqTimestamp + channelTimestamp);
                            tom.Objects.Add(timedEvent);
                            break;
                        case 0xdd:
                            // channel pan
                            int pan = br.ReadByte();
                            channelEvent = new ControlChangeEvent()
                            {
                                ControlNumber = (SevenBitNumber)(byte)ControlName.Pan,
                                ControlValue = (SevenBitNumber)pan,
                                Channel = (FourBitNumber)channel,
                            };
                            timedEvent = new TimedEvent(channelEvent, seqTimestamp + channelTimestamp);
                            tom.Objects.Add(timedEvent);
                            break;
                        case 0xdc:
                            // set pan chan weight
                            br.ReadByte();
                            break;
                        case 0xd8:
                            // set vibrato extent
                            br.ReadByte();
                            break;
                        case 0xd4:
                            // set reverb
                            br.ReadByte();
                            break;
                        case 0xd3:
                            // pitch bend
                            int pitchBend = br.ReadSByte();
                            pitchBend += 0x80; // 0 ~ 255
                            pitchBend <<= 6; // 0x0 ~ 0x4000

                            channelEvent = new PitchBendEvent()
                            {
                                PitchValue = (ushort)pitchBend,
                                Channel = (FourBitNumber)channel,
                            };
                            timedEvent = new TimedEvent(channelEvent, seqTimestamp + channelTimestamp);
                            tom.Objects.Add(timedEvent);
                            break;
                        case 0xc4:
                            // large notes on
                            break;
                        case 0xc1:
                            // set instrument
                            int inst = br.ReadByte();
                            Console.WriteLine(inst);

                            // convert inst m64 to midi
                            // extra instrument bankの場合の処理 todo
                            int inst_midi = 0x7f;
                            if (inst != 0x7f)
                            {
                                // クソみたいな処理
                                // instrument_listのinstまでのnullの数をカウント
                                int null_count = 0 ;
                                for(int i = 0; i <= inst; i++)
                                {
                                    if (instrument_list[i].ToString() == "")
                                        null_count++;
                                }
                                int xml_inst = inst - null_count;

                                //string str = instrument_list[inst].ToString().Substring(4);
                                //int inst_xml = int.Parse(str);

                                XmlNode item = indexentry.SelectSingleNode($"instruments/item[@index={xml_inst}]");
                                if(item.Attributes["map"].InnerText == "program")
                                {
                                    inst_midi = int.Parse(item.Attributes["program"].InnerText);
                                }

                                // for other drums
                                masterTrackChunks[channel].item = item;
                                masterTrackChunks[channel].inst = instruments.GetProperty(instrument_list[inst].ToString());
                            }
                            // for general percussion
                            masterTrackChunks[channel].rawInst = inst;

                            channelEvent = new ProgramChangeEvent()
                            {
                                ProgramNumber = (SevenBitNumber)inst_midi,
                                Channel = (FourBitNumber)channel,
                            };
                            timedEvent = new TimedEvent(channelEvent, seqTimestamp + channelTimestamp);
                            tom.Objects.Add(timedEvent);
                            break;
                        default:
                            throw new Exception("not implemented command");
                            break;
                    }
                }
                else
                {
                    // cmd <= 0xbf
                    // 大まかな命令
                    int loBits = cmd & 0x0f;
                    switch (cmd & 0xf0)
                    {
                        case 0x90:
                            // set layer
                            ushort layerOffset = br.ReadUInt16();
                            long returnOffset = br.BaseStream.Position;
                            br.BaseStream.Position = layerOffset;
                            ParseLayer(br, channel, loBits, tom);
                            br.BaseStream.Position = returnOffset;
                            break;
                        case 0x60:
                            // set note priority
                            break;
                        default:
                            throw new Exception("not implemented command");
                            break;
                    }
                }
            }
        }

        void ParseLayer(BinaryDataReader br, int channel ,int layer, TimedObjectsManager<TimedEvent> tom)
        {
            layerTimestamp = 0;

            int key = 0;
            int timestamp = 0;
            int velocity = 0;
            int gatetime = 0;

            int transpose = 0;

            Stack<long> returnOffsets = new Stack<long>();

            while (true)
            {
                byte cmd = br.ReadByte();


                if (cmd >= 0xd0 && cmd <= 0xef)
                {
                    // cmd == 0xd_ or 0xe_
                    switch(cmd & 0xf0)
                    {
                        case 0xd0:
                            // setshortnotevelocityfromtable
                            break;
                        default:
                            throw new Exception("not implemented command");
                            break;
                    }
                }
                else if (cmd >= 0xc0)
                {
                    // 細かい命令
                    // cmd == 0xc_ or 0xf_
                    switch (cmd)
                    {
                        case 0xff:
                            // end
                            if(returnOffsets.Count > 0)
                            {
                                br.BaseStream.Position = returnOffsets.Pop();
                                break;
                            }
                            tom.SaveChanges();
                            return;
                        case 0xfd:
                            // timestamp ?
                            layerTimestamp += ReadParameter(br);
                            break;
                        case 0xfc:
                            // call
                            ushort jumpTo = br.ReadUInt16();
                            long retrunOffset = br.BaseStream.Position;
                            returnOffsets.Push(retrunOffset);
                            br.BaseStream.Position = jumpTo;
                            break;
                        case 0xf8:
                            // loop start todo
                            br.ReadByte();
                            break;
                        case 0xf7:
                            // loop end todo
                            break;
                        case 0xc2:
                            // transpose
                            transpose = br.ReadByte();
                            break;
                        case 0xc1:
                            // setshortnotevelocity
                            br.ReadByte();
                            break;
                        case 0xc0:
                            // layer timestamp
                            layerTimestamp += ReadParameter(br);
                            break;
                        default:
                            throw new Exception("not implemented command");
                            break;
                    }
                }
                else
                {
                    // cmd <= 0xbf
                    // 大まかな命令
                    /*
                     * 音符
                     * T Timestamp 自身の音符の始点から、次の音符の始点までの時間
                     * V Velocity 強さ
                     * G Gatetime 自身の音符の終点から、次の音符の始点までの、Timestampに対する時間の割合(0-255)
                     */
                    

                    switch (cmd & 0xc0)
                    {
                        case 0x00:
                            key = cmd;
                            timestamp = ReadParameter(br);
                            velocity = br.ReadByte();
                            gatetime = br.ReadByte();
                            break;
                        case 0x40:
                            key = cmd - 0x40;
                            timestamp = ReadParameter(br);
                            velocity = br.ReadByte();
                            gatetime = 0; // 長音
                            break;
                        case 0x80:
                            key = cmd - 0x80;
                            // timestamp = previous one
                            velocity = br.ReadByte();
                            gatetime = br.ReadByte();
                            break;
                        default:
                            throw new Exception("not implemented command");
                            break;
                    }

                    key = key + 21 + transpose;
                    velocity &= 0x7f; // 最大ボリューム頭打ち

                    NoteEvent note;
                    TimedEvent timed;

                    long totalTimestamp = seqTimestamp + channelTimestamp + layerTimestamp;

                    // note on
                    note = new NoteOnEvent()
                    {
                        NoteNumber = (SevenBitNumber)key,
                        Velocity = (SevenBitNumber)velocity,
                        Channel = (FourBitNumber)channel,
                    };
                    timed = new TimedEvent(note, totalTimestamp);
                    tom.Objects.Add(timed);

                    // note off
                    note = new NoteOffEvent()
                    {
                        NoteNumber = (SevenBitNumber)key,
                        Velocity = (SevenBitNumber)0,
                        Channel = (FourBitNumber)channel,
                    };
                    
                    timed = new TimedEvent(note, totalTimestamp + timestamp - (timestamp * gatetime) / 255);
                    tom.Objects.Add(timed);

                    
                    layerTimestamp += timestamp;
                }
            }
        }

        ushort ReadParameter(BinaryDataReader br)
        {
            ushort num = br.ReadByte();
            if ((num & 0x80) == 0x80)
            {
                num = (ushort)((num << 8) & 0x7f00);
                num |= br.ReadByte();
            }
            return num;
        }

        void GeneralPercussionProcess(MasterTrackChunk masterTrackChunk)
        {
            TrackChunk trackChunk = masterTrackChunk.trackChunk;
            TimedObjectsManager<Note> noteManager = trackChunk.Events.ManageNotes();
            foreach (Note note in noteManager.Objects)
            {
                if (note.NoteNumber <= 36)
                {
                    // kick drum
                    note.NoteNumber = (SevenBitNumber)36;
                }
                else if (note.NoteNumber == 37)
                {
                    // drum stick
                    note.NoteNumber = (SevenBitNumber)37;
                }
                else if (note.NoteNumber <= 40)
                {
                    // snare drum
                    if (note.NoteNumber == 38)
                        note.NoteNumber = (SevenBitNumber)38;
                    else if (note.NoteNumber == 39)
                        note.NoteNumber = (SevenBitNumber)39;
                    else if (note.NoteNumber == 40)
                        note.NoteNumber = (SevenBitNumber)40;
                }
                else if (note.NoteNumber <= 53)
                {
                    // tom drum
                    note.NoteNumber = (SevenBitNumber)47; // low mid tom
                }
                else if (note.NoteNumber <= 61)
                {
                    // tambourine
                    note.NoteNumber = (SevenBitNumber)54;
                }
                else if (note.NoteNumber <= 69)
                {
                    // low bongo
                    note.NoteNumber = (SevenBitNumber)61;
                }
                else if (note.NoteNumber <= 71)
                {
                    // high bongo
                    note.NoteNumber = (SevenBitNumber)60;
                }
                else if (note.NoteNumber <= 84)
                {
                    // conga stick
                    note.NoteNumber = (SevenBitNumber)64; // low conga
                }
                else if (note.NoteNumber == 85)
                {
                    // claves
                    note.NoteNumber = (SevenBitNumber)75;
                }
            }
            noteManager.SaveChanges();

            // チャンネル10に変更
            var timed = trackChunk.ManageTimedEvents();
            foreach (var obj in timed.Objects)
            {
                if (obj.Event is ChannelEvent)
                {
                    ((ChannelEvent)obj.Event).Channel = (FourBitNumber)9;
                }
            }
            timed.SaveChanges();
        }

        public class Inst
        {
            public int release_rate { get; set; }
            public int normal_range_lo { get; set; }
            public int normal_range_hi { get; set; }
            public string envelope { get; set; }
            public string sound_lo { get; set; }
            public string sound { get; set; }
            public string sound_hi { get; set; }
        }

        void DrumProcess(MasterTrackChunk masterTrackChunk)
        {
            TrackChunk trackChunk = masterTrackChunk.trackChunk;

            int drumsplit1 = int.Parse(masterTrackChunk.item.Attributes["drumsplit1"].InnerText);
            int drumsplit2 = int.Parse(masterTrackChunk.item.Attributes["drumsplit2"].InnerText);
            int drumsplit3 = int.Parse(masterTrackChunk.item.Attributes["drumsplit3"].InnerText);

            Inst inst = JsonSerializer.Deserialize<Inst>(masterTrackChunk.inst);

            // ドラムのノートナンバーをセット
            TimedObjectsManager<Note> noteManager = trackChunk.Events.ManageNotes();
            foreach (Note note in noteManager.Objects)
            {
                if (inst.normal_range_lo != 0 && inst.normal_range_lo > note.NoteNumber - 21)
                {
                    note.NoteNumber = (SevenBitNumber)drumsplit1;
                }
                else if (inst.normal_range_hi != 0 && inst.normal_range_hi < note.NoteNumber - 21)
                {
                    note.NoteNumber = (SevenBitNumber)drumsplit3;
                }
                else
                {
                    note.NoteNumber = (SevenBitNumber)drumsplit2;
                }
            }
            noteManager.SaveChanges();

            // チャンネル10に変更
            var timed = trackChunk.ManageTimedEvents();
            foreach (var obj in timed.Objects)
            {
                if (obj.Event is ChannelEvent)
                {
                    ((ChannelEvent)obj.Event).Channel = (FourBitNumber)9;
                }
            }
            timed.SaveChanges();

            // ドラムパートに変更
            //TimedObjectsManager<TimedEvent> tom = masterTrackChunk.trackChunk.ManageTimedEvents();
            //MidiEvent sysex = new NormalSysExEvent()
            //{
            //    Data = new byte[] {  0x41, 0x10, 0x42, 0x12, 0x40, 0x00, 0x7F, 0x00, 0x41, 0xF7 },
            //};
            //TimedEvent timed = new TimedEvent(sysex, 0);
            //tom.Objects.Add(timed);
            //int channel = masterTrackChunk.channel;
            //sysex = new NormalSysExEvent()
            //{
            //    Data = new byte[] {  0x41, 0x10, 0x42, 0x12, 0x40, (byte)(0x10 + channel), 0x15, 0x02, (byte)(0x19 - channel), 0xF7 },
            //};
            //timed = new TimedEvent(sysex, 0);
            //tom.Objects.Add(timed);
            //tom.SaveChanges();
        }
    }
}