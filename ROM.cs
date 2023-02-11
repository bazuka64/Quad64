using OpenTK.Graphics.OpenGL4;
using Quad64.lib;
using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.IO;

namespace Quad64
{
    internal class ROM
    {
        public static ROM instance;

        byte[] bytes;
        public BinaryStream bs;
        public Dictionary<int, byte[]> segments = new Dictionary<int, byte[]>();

        public Texture waterTexture;

        public string romName;
        public List<Sequence> sequences = new List<Sequence>();
        int seqCount;

        public ROM(string romPath)
        {
            // load rom
            instance = this;
            bytes = File.ReadAllBytes(romPath);
            bs = new BinaryStream(new MemoryStream(bytes));
            bs.ByteConverter = ByteConverter.Big;

            // get rom name
            bs.Position = 0x20;
            romName = bs.ReadString(0x14).TrimEnd();
            // if sm74 then modify
            if (romName == "SUPER MARIO 64" && bytes.Length >= 0x02FFFFFF)
            {
                romName = "SUPER MARIO 74";
            }

            // set segment
            uint seg15start = 0x2ABCA0;
            uint seg15end = 0x2AC6B0;
            uint seg2start = 0x108A40;
            uint seg2end = 0x114750;
            if (romName == "THROUGH THE AGES")
            {
                seg15start = 0x2E3D30;
                seg15end = 0x2E47B0;
                seg2start = 0x0EB390;
                seg2end = 0x0F6570;
            }
            setSegment(0x15, seg15start, seg15end, false);
            setSegment(0x02, seg2start, seg2end, true);

            // get water texture
            byte[] pixels = TextureFormats.decodeTexture(0x02014ab8, 32, 32, 0x10);
            waterTexture = new Texture(pixels, 32, 32, (int)TextureWrapMode.Repeat, (int)TextureWrapMode.Repeat);

            // sequence
            loadSequence();
        }

        public void setSegment(int seg, uint start, uint end, bool mio0)
        {
            if (start > end)
                return;

            uint size = end - start;
            byte[] data = new byte[size];
            Array.Copy(bytes, start, data, 0, size);

            if (mio0)
                data = MIO0.mio0_decode(data);

            if (segments.ContainsKey(seg))
                segments.Remove(seg);
            segments.Add(seg, data);
        }

        public void resetSegment()
        {
            foreach(var segment in segments)
            {
                if (segment.Key != 0x15 && segment.Key != 0x02)
                    segments.Remove(segment.Key);
            }
        }

        void loadSequence()
        {
            // delete midi and m64 files
            if(Directory.Exists("../../../midi/"))
                Directory.Delete("../../../midi/", true);
            Directory.CreateDirectory("../../../midi/");

            // set table
            uint seqTable = 0x03E00000;
            uint instsTable = 0x007F0000;
            uint nameTable = 0x007F1000;
            if (Sequence.addresses.ContainsKey(romName))
            {
                var address = Sequence.addresses[romName];
                seqTable = address.seqTable;
                instsTable = address.instsTable;
                nameTable = address.nameTable;
            }

            // get sequence count
            bs.Position = seqTable + 2;
            seqCount = bs.ReadUInt16();
            for (int i = 0; i < seqCount; i++)
            {
                sequences.Add(new Sequence(i));
            }

            // get sequence data
            bs.Position = seqTable + 4;
            foreach (var seq in sequences)
            {
                uint startoff = bs.ReadUInt32();
                uint len = bs.ReadUInt32();
                long offBefore = bs.Position;
                bs.Position = seqTable + startoff;
                seq.data = bs.ReadBytes((int)len);
                bs.Position = offBefore;
            }

            // get instrument sets
            bs.Position = instsTable;
            foreach (var seq in sequences)
            {
                ushort startoff = bs.ReadUInt16();
                long offBefore = bs.Position;
                bs.Position = instsTable + startoff;
                int instCount = bs.ReadByte();
                for (int i = 0; i < instCount; i++)
                {
                    seq.insts.Add(bs.Read1Byte());
                }
                bs.Position = offBefore;
            }

            // get sequence name
            if (nameTable == 0x00000000)
            {
                // use original name table
                foreach (var seq in sequences)
                {
                    if (seq.id > Sequence.tNames.Length - 1)
                    {
                        seq.name = "Unknown Sequence Name";
                    }
                    else
                    {
                        seq.name = Sequence.tNames[seq.id];
                        seq.defaultSeq = true;
                    }
                }
            }
            else
            {
                // read hacked name
                bs.Position = nameTable;
                foreach (var seq in sequences)
                {
                    seq.name = bs.ReadString();
                    if (seq.id > Sequence.tNames.Length - 1)
                        continue;
                    if (seq.name.Contains(Sequence.tNames[seq.id], StringComparison.OrdinalIgnoreCase))
                    {
                        seq.defaultSeq = true;
                    }
                }
            }
        }

        public static Dictionary<string, ushort> levelIDs = new Dictionary<string, ushort>
        {
            { "[C01] Bob-omb Battlefield", 0x09 },
            { "[C02] Whomp's Fortress", 0x18 },
            { "[C03] Jolly Roger Bay", 0x0C },
            { "[C04] Cool Cool Mountain", 0x05 },
            { "[C05] Big Boo's Haunt", 0x04 },
            { "[C06] Hazy Maze Cave", 0x07 },
            { "[C07] Lethal Lava Land", 0x16 },
            { "[C08] Shifting Sand Land", 0x08 },
            { "[C09] Dire Dire Docks", 0x17 },
            { "[C10] Snowman's Land", 0x0A },
            { "[C11] Wet Dry World", 0x0B },
            { "[C12] Tall Tall Mountain", 0x24 },
            { "[C13] Tiny Huge Island", 0x0D },
            { "[C14] Tick Tock Clock", 0x0E },
            { "[C15] Rainbow Ride", 0x0F },
            { "[OW1] Castle Grounds", 0x10 },
            { "[OW2] Inside Castle", 0x06 },
            { "[OW3] Castle Courtyard", 0x1A },
            { "[BC1] Bowser Course 1", 0x11 },
            { "[BC2] Bowser Course 2", 0x13 },
            { "[BC3] Bowser Course 3", 0x15 },
            { "[MCL] Metal Cap", 0x1C },
            { "[WCL] Wing Cap", 0x1D },
            { "[VCL] Vanish Cap", 0x12 },
            { "[BB1] Bowser Battle 1", 0x1E },
            { "[BB2] Bowser Battle 2", 0x21 },
            { "[BB3] Bowser Battle 3", 0x22 },
            { "[SC1] Secret Aquarium", 0x14 },
            { "[SC2] Rainbow Clouds", 0x1F },
            { "[SC3] End Cake Picture", 0x19 },
            { "[SlC] Peach's Secret Slide", 0x1B }
        };
    }
}