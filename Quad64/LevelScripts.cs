using Syroot.BinaryData;

namespace Quad64
{
    internal class LevelScripts
    {
        public static void parse(Level lvl, byte segment, uint offset)
        {
            ROM rom = ROM.Instance;
            byte[] data = rom.segData[segment];
            BinaryDataReader br = new BinaryDataReader(new MemoryStream(data));
            br.ByteConverter = ByteConverter.BigEndian;
            br.BaseStream.Position = offset;
            while (true)
            {
                byte cmd = br.ReadByte();
                byte len = br.ReadByte();
                switch (cmd)
                {
                    case 0x18:
                        // load mio0
                        {
                            br.BaseStream.Position += 1;
                            byte seg = br.ReadByte();
                            uint start = br.ReadUInt32();
                            uint end = br.ReadUInt32();

                            rom.setSegment(seg, start, end, true);
                        }
                        break;
                    case 0x1d:
                        // alloc level pool
                        br.BaseStream.Position += len - 2;
                        break;
                    case 0x22:
                        // load model from geo
                        {
                            br.BaseStream.Position += 1;
                            byte modelID = br.ReadByte();
                            uint address = br.ReadUInt32();

                            Model3D model = new Model3D()
                            {
                                ModelID = modelID,
                                GeoAddress = address,
                            };

                            // todo GeoScripts.parse()
                        }
                        break;
                    default:
                        throw new Exception("level command is not implemented");
                        break;
                }
            }
        }
    }
}