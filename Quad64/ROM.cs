using Syroot.BinaryData;

namespace Quad64
{
    internal class ROM
    {
        public static ROM Instance;

        byte[] bytes;
        MemoryStream ms;
        BinaryDataReader br;
        public string romName;

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
            //romName = br.ReadString(StringDataFormat.ZeroTerminated);
            romName = br.ReadString(0x14);
            romName = romName.TrimEnd();
            // if sm74 then modify
            if(romName == "SUPER MARIO 64" && bytes.Length >= 0x02FFFFFF)
            {
                romName = "SUPER MARIO 74";
            }

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
                }
                return;
            }
            br.BaseStream.Position = nameTable;
            foreach(var seq in sequences)
            {
                seq.name = br.ReadString();
            }
        }
    }
}
