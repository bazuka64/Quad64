using Syroot.BinaryData;

namespace Quad64
{
    internal class ROM
    {
        public static ROM Instance;

        byte[] bytes;
        MemoryStream ms;
        public BinaryDataReader br;
        public string romName;

        public Dictionary<byte, byte[]> segData = new Dictionary<byte, byte[]>();
        public byte[] segData0E;

        public List<Sequence> sequences = new List<Sequence>();
        int seqCount;

        public ROM(string romPath)
        {
            bytes = File.ReadAllBytes(romPath);
            ms = new MemoryStream(bytes);
            br = new BinaryDataReader(ms);
            br.ByteConverter = ByteConverter.BigEndian;

            // romNameの取得
            br.BaseStream.Position = 0x20;
            romName = br.ReadString(0x14);
            romName = romName.TrimEnd();
            // if sm74 then modify
            if(romName == "SUPER MARIO 64" && bytes.Length >= 0x02FFFFFF)
            {
                romName = "SUPER MARIO 74";
            }

            // set segment for level script
            uint seg15start = 0x2ABCA0;
            uint seg15end = 0x2AC6B0;
            uint seg2start = 0x108A40;
            uint seg2end = 0x114750;
            // through the ages 15seg start:2E3D30 end:2E47B0
            //                  02seg start:0EB390 end:0F6570 maybe
            if (romName == "THROUGH THE AGES")
            {
                seg15start = 0x2E3D30;
                seg15end   = 0x2E47B0;
                seg2start  = 0x0EB390;
                seg2end    = 0x0F6570;
            }
            setSegment(0x15, seg15start, seg15end, false);
            setSegment(0x02, seg2start, seg2end, true);

            // フォルダ作成
            string path = "../../../../midi/" + romName;
            if(Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            Directory.CreateDirectory(path);

            // romごとの曲に関するデータの位置をセット
            if (!Sequence.addresses.ContainsKey(romName))
            {
                return;
            }
            var address = Sequence.addresses[romName];
            uint seqTable   = address.seqTable;
            uint instsTable = address.instsTable;
            uint nameTable  = address.nameTable;

            // 曲数の取得
            br.BaseStream.Position = seqTable + 2;
            seqCount = br.ReadUInt16();
            for(int i = 0; i < seqCount; i++)
            {
                sequences.Add(new Sequence(i));
            }

            // 曲データの取得
            br.BaseStream.Position = seqTable + 4;
            foreach (var seq in sequences)
            {
                uint startoff = br.ReadUInt32();
                uint len = br.ReadUInt32();
                long offBefore = br.BaseStream.Position;
                br.BaseStream.Position = seqTable + startoff;
                seq.data = br.ReadBytes((int)len);
                br.BaseStream.Position = offBefore;
            }

            // 使用楽器セットの取得
            br.BaseStream.Position = instsTable;
            foreach (var seq in sequences)
            {
                ushort startoff = br.ReadUInt16();
                long offBefore = br.BaseStream.Position;
                br.BaseStream.Position = instsTable + startoff;
                int instCount = br.ReadByte();
                for (int j = 0; j < instCount; j++)
                {
                    seq.insts.Add(br.ReadByte());
                }
                br.BaseStream.Position = offBefore;
            }

            // 曲名の取得
            if(nameTable == 0x00000000)
            {
                // use original name table
                foreach (var seq in sequences)
                {
                    if(seq.id > Sequence.tNames.Length - 1)
                    {
                        seq.name = "Unknown Sequence Name";
                        continue;
                    }
                    seq.name = Sequence.tNames[seq.id];
                    seq.defaultSeq = true;
                }
                return;
            }
            br.BaseStream.Position = nameTable;
            foreach(var seq in sequences)
            {
                seq.name = br.ReadString();
                if (seq.id > Sequence.tNames.Length - 1)
                    continue;
                if (seq.name.Contains(Sequence.tNames[seq.id], StringComparison.OrdinalIgnoreCase))
                {
                    seq.defaultSeq = true;
                }
            }
        }

        public void setSegment(byte seg, uint start, uint end, bool mio0)
        {
            // error
            if (start > end) return;

            uint size = end - start;
            byte[] data = new byte[size];
            Array.Copy(bytes, start, data, 0, size);

            if (mio0)
                data = MIO0.mio0_decode(data);

            if (segData.ContainsKey(seg))
                segData.Remove(seg);
            segData.Add(seg, data);
        }

        public void setSegment0E(uint start, uint end)
        {
            uint size = end - start;
            byte[] data = new byte[size];
            Array.Copy(bytes, start, data, 0, size);

            segData0E = data;
        }

        // これ以外にもレベルIDが存在すると思われる
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
