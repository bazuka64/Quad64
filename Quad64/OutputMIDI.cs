using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Syroot.BinaryData;

namespace Quad64
{
    internal class OutputMIDI
    {
        MidiFile midiFile = new MidiFile();

        public OutputMIDI()
        {
        }

        public MidiFile ConvertToMIDI(Sequence seq)
        {
            BinaryDataReader br = new BinaryDataReader(new MemoryStream(seq.data));
            br.ByteConverter = ByteConverter.BigEndian;

            for(int i = 0; i < 16; i++)
            {
                midiFile.Chunks.Add(new TrackChunk());
            }

            ParseSequence(br);

            midiFile.TimeDivision = new TicksPerQuarterNoteTimeDivision(48);

            return midiFile;
        }

        void ParseSequence(BinaryDataReader br)
        {
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
                            ReadParameter(br);
                            break;
                        case 0xdd:
                            // tempo
                            br.ReadByte();
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
                        case 0xd3:
                            // set mute bhv
                            br.ReadByte();
                            break;
                        default:
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
                        default:
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

            long channelTimestamp = 0;

            ChannelEvent channelEvent;
            TimedEvent timedEvent;

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
                            tom.SaveChanges();
                            return;
                        case 0xfd:
                            // timestamp
                            channelTimestamp += ReadParameter(br);
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
                            timedEvent = new TimedEvent(channelEvent, channelTimestamp);
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
                            timedEvent = new TimedEvent(channelEvent, channelTimestamp);
                            tom.Objects.Add(timedEvent);
                            break;
                        case 0xdc:
                            // set pan chan weight
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
                            timedEvent = new TimedEvent(channelEvent, channelTimestamp);
                            tom.Objects.Add(timedEvent);
                            break;
                        case 0xc4:
                            // large notes on
                            break;
                        case 0xc1:
                            // set instrument
                            int inst = br.ReadByte();

                            // convert inst m64 to midi

                            channelEvent = new ProgramChangeEvent()
                            {
                                ProgramNumber = (SevenBitNumber)inst,
                                Channel = (FourBitNumber)channel,
                            };
                            timedEvent = new TimedEvent(channelEvent, channelTimestamp);
                            tom.Objects.Add(timedEvent);
                            break;
                        default:
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
                            break;
                    }
                }
            }
        }

        void ParseLayer(BinaryDataReader br, int channel ,int layer, TimedObjectsManager<TimedEvent> tom)
        {
            long layerTimestamp = 0;

            int key = 0;
            int timestamp = 0;
            int velocity = 0;
            int gatetime = 0;

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
                            tom.SaveChanges();
                            return;
                        case 0xc0:
                            // layer timestamp
                            layerTimestamp += ReadParameter(br);
                            break;
                        default:
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
                            break;
                    }

                    key += 21;

                    

                    NoteEvent note;
                    TimedEvent timed;

                    // note on
                    note = new NoteOnEvent()
                    {
                        NoteNumber = (SevenBitNumber)key,
                        Velocity = (SevenBitNumber)velocity,
                        Channel = (FourBitNumber)channel,
                    };
                    // 絶対時間の計算が必要 todo
                    timed = new TimedEvent(note, layerTimestamp);
                    tom.Objects.Add(timed);

                    // note off
                    note = new NoteOffEvent()
                    {
                        NoteNumber = (SevenBitNumber)key,
                        Velocity = (SevenBitNumber)0,
                        Channel = (FourBitNumber)channel,
                    };
                    // 絶対時間の計算が必要 todo
                    timed = new TimedEvent(note, layerTimestamp + timestamp - (timestamp * gatetime) / 255);
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