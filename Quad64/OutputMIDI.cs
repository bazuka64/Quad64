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
    internal class OutputMIDI
    {
        MidiFile midiFile = new MidiFile();

        long seqTimestamp = 0;
        long channelTimestamp = 0;
        long layerTimestamp = 0;

        XmlNode indexentry;
        JsonElement instrument_list;

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

            for(int i = 0; i < 16; i++)
            {
                midiFile.Chunks.Add(new TrackChunk());
            }

            ParseSequence(br);

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

                            // convert inst m64 to midi
                            // extra instrument bankの場合の処理 todo
                            int inst_midi = 0x7f;
                            if (inst != 0x7f)
                            {
                                string str = instrument_list[inst].ToString().Substring(4);
                                int inst_xml = int.Parse(str);
                                XmlNode item = indexentry.SelectSingleNode($"instruments/item[@index={inst_xml}]");
                                inst_midi = int.Parse(item.Attributes["program"].InnerText);
                            }

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
                    // 絶対時間の計算が必要 todo
                    timed = new TimedEvent(note, totalTimestamp);
                    tom.Objects.Add(timed);

                    // note off
                    note = new NoteOffEvent()
                    {
                        NoteNumber = (SevenBitNumber)key,
                        Velocity = (SevenBitNumber)0,
                        Channel = (FourBitNumber)channel,
                    };
                    // 絶対時間の計算が必要 todo
                    
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
    }
}