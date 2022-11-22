using Syroot.BinaryData;

namespace Quad64
{
    internal class LevelScripts
    {
        // return end flag
        public static bool parse(Level lvl, byte segment, uint offset)
        {
            // error: sm64 land 1st bomomb stage
            if (segment == 0x00) return false;

            ROM rom = ROM.Instance;
            byte[] data = rom.segData[segment];
            BinaryDataReader br0 = new BinaryDataReader(new MemoryStream(data));
            br0.ByteConverter = ByteConverter.BigEndian;
            br0.BaseStream.Position = offset;
            while (true)
            {
                br0.BaseStream.Position++;
                byte len = br0.ReadByte();
                br0.BaseStream.Position -= 2;
                byte[] cmd = br0.ReadBytes(len);

                // コマンド読み取り用のBinaryReader
                BinaryDataReader br = new BinaryDataReader(new MemoryStream(cmd));
                br.ByteConverter = ByteConverter.BigEndian;
                br.BaseStream.Position += 2;

                Console.WriteLine($"level script command: " + BitConverter.ToString(cmd).Replace("-", " "));

                switch (cmd[0])
                {
                    case 0x00:
                        // execute
                        {
                            br.BaseStream.Position++;
                            byte seg = br.ReadByte();
                            uint start = br.ReadUInt32();
                            uint end = br.ReadUInt32();
                            uint segAddr = br.ReadUInt32();
                            uint off = segAddr & 0x00FFFFFF;

                            if(seg == 0x14)
                            {
                                // first title screen skip
                                break;
                            }

                            rom.setSegment(seg, start, end, false);
                            if (parse(lvl, seg, off))
                                return true;
                        }
                        break;
                    case 0x02:
                        // exit
                        Console.WriteLine($"levelID: 0x{lvl.levelID:X2} does not exist in this rom hack");
                        return true;
                    case 0x03:
                        // sleep
                        return true;
                    case 0x05:
                        // jump
                        {
                            br.BaseStream.Position += 2;
                            uint segAddr = br.ReadUInt32();
                            byte seg = (byte)((segAddr & 0xFF000000) >> 24);
                            uint off = segAddr & 0x00FFFFFF;

                            if (parse(lvl, seg, off))
                                return true;
                        }
                        return false;
                    case 0x06:
                        // jump link
                        {
                            br.BaseStream.Position += 2;
                            uint segAddr = br.ReadUInt32();
                            byte seg = (byte)((segAddr & 0xFF000000) >> 24);
                            uint off = segAddr & 0x00FFFFFF;

                            if (parse(lvl, seg, off))
                                return true;
                        }
                        break;
                    case 0x07:
                        // return
                        return false;
                    case 0x0a:
                        // loop begin
                        break;
                    case 0x0c:
                        // conditional jump
                        {
                            br.ReadByte();
                            br.BaseStream.Position++;
                            uint lvlcheck = br.ReadUInt32();
                            uint segAddr = br.ReadUInt32();
                            byte seg = (byte)((segAddr & 0xFF000000) >> 24);
                            uint off = segAddr & 0x00FFFFFF;

                            if(lvlcheck == lvl.levelID)
                            {
                                if (parse(lvl, seg, off))
                                    return true;
                            }
                        }
                        break;
                    case 0x10:
                        // no operation
                        break;
                    case 0x11:
                        // call
                        break;
                    case 0x17:
                        // load raw
                        {
                            br.BaseStream.Position++;
                            byte seg = br.ReadByte();
                            uint start = br.ReadUInt32();
                            uint end = br.ReadUInt32();

                            rom.setSegment(seg, start, end, false);
                        }
                        break;
                    case 0x18: // load mio0
                    case 0x1a: // load texture
                        {
                            br.BaseStream.Position++;
                            byte seg = br.ReadByte();
                            uint start = br.ReadUInt32();
                            uint end = br.ReadUInt32();

                            rom.setSegment(seg, start, end, true);
                        }
                        break;
                    case 0x1b:
                        // init level
                        break;
                    case 0x1d:
                        // alloc level pool
                        break;
                    case 0x1e:
                        // free level pool
                        break;
                    case 0x1f:
                        // area
                        {
                            byte areaID = br.ReadByte();
                            br.BaseStream.Position++;
                            uint address = br.ReadUInt32();

                            Area area = new Area()
                            {
                                areaID = areaID,
                            };
                            area.model.geoAddress = address;

                            lvl.areas[areaID] = area;
                            lvl.curAreaID = areaID;

                            // todo GeoScripts.parse()
                        }
                        break;
                    case 0x20:
                        // end area
                        lvl.curAreaID = -1;
                        break;
                    case 0x21:
                        // load model from dl
                        {
                            int layer = br.ReadByte() >> 4;
                            byte modelID = br.ReadByte();
                            uint address = br.ReadUInt32();

                            Model3D model = new Model3D()
                            {
                                modelID = modelID,
                                dlAddress = address,
                            };

                            // todo Fast3DScripts.parse()
                        }
                        break;
                    case 0x22:
                        // load model from geo
                        {
                            br.BaseStream.Position++;
                            byte modelID = br.ReadByte();
                            uint address = br.ReadUInt32();

                            Model3D model = new Model3D()
                            {
                                modelID = modelID,
                                geoAddress = address,
                            };

                            // todo GeoScripts.parse()
                        }
                        break;
                    case 0x24:
                        // object
                        {
                            Object3D obj = new Object3D();
                            lvl.areas[lvl.curAreaID].Objects.Add(obj);

                            byte act = br.ReadByte();
                            obj.modelID = br.ReadByte();
                            obj.xPos = br.ReadInt16();
                            obj.yPos = br.ReadInt16();
                            obj.zPos = br.ReadInt16();
                            obj.xRot = br.ReadInt16();
                            obj.yRot = br.ReadInt16();
                            obj.zRot = br.ReadInt16();
                            obj.bhvParameter = br.ReadUInt32();
                            obj.bhvAddress = br.ReadUInt32();

                            // todo BehaviorScripts.parse for animation

                        }
                        break;
                    case 0x25:
                        // mario
                        break;
                    case 0x26:
                        // warp
                        break;
                    case 0x27:
                        // painting warp
                        break;
                    case 0x28:
                        // instant warp
                        break;
                    case 0x2b:
                        // mario pos
                        byte startAreaID = br.ReadByte();
                        br.BaseStream.Position++;
                        short yRot = br.ReadInt16();
                        short xPos = br.ReadInt16();
                        short yPos = br.ReadInt16();
                        short zPos = br.ReadInt16();

                        lvl.curAreaID = startAreaID;
                        return true; // end parse level script
                    case 0x2e:
                        // terrain (special object)
                        {
                            br.BaseStream.Position += 2;
                            uint segAddr = br.ReadUInt32();

                            CMD_2E(lvl, segAddr);
                        }
                        break;
                    case 0x2f:
                        // Setup Render Room
                        break;
                    case 0x30:
                        // show dialog
                        break;
                    case 0x31:
                        // terrain type
                        break;
                    case 0x36:
                        // set back ground music
                        br.BaseStream.Position += 3;
                        byte seqID = br.ReadByte();
                        lvl.areas[lvl.curAreaID].seqID = seqID;
                        break;
                    case 0x39:
                        // macro object
                        {
                            br.BaseStream.Position += 2;
                            uint segAddr = br.ReadUInt32();

                            CMD_39(lvl, segAddr);
                        }
                        break;
                    case 0x3b:
                        // jet stream
                        break;
                    case 0x3c:
                        // get or set
                        break;
                    default:
                        throw new Exception("level command is not implemented");
                        break;
                }
            }
        }

        static void CMD_2E(Level lvl, uint segAddr)
        {
            ROM rom = ROM.Instance;

            byte seg = (byte)(segAddr >> 24);
            uint off = segAddr & 0x00FFFFFF;

            byte[] data = rom.segData[seg];
            BinaryDataReader br = new BinaryDataReader(new MemoryStream(data));
            br.ByteConverter = ByteConverter.BigEndian;
            br.BaseStream.Position = off;

            // read vertices
            ushort sub_cmd = br.ReadUInt16();
            ushort num_verts = br.ReadUInt16();
            for(int i=0; i < num_verts; i++)
            {
                short x = br.ReadInt16();
                short y = br.ReadInt16();
                short z = br.ReadInt16();
            }

            // read triagnles
            while (true)
            {
                sub_cmd = br.ReadUInt16(); // surface type
                if (sub_cmd == 0x41) break;

                ushort num_tri = br.ReadUInt16();
                for(int i = 0; i < num_tri; i++)
                {
                    //ushort a = br.ReadUInt16();
                    //ushort b = br.ReadUInt16();
                    //ushort c = br.ReadUInt16();
                    br.BaseStream.Position += collisionLength(sub_cmd);
                }
            }

            // read special objects
            
            while (true)
            {
                sub_cmd = br.ReadUInt16();
                switch (sub_cmd)
                {
                    case 0x0042:
                        // end
                        return;
                    case 0x0043:
                        // special object
                        uint special_preset_table = 0xEC7E0; // check for romhacks

                        ushort num_obj = br.ReadUInt16();

                        for(int i = 0; i < num_obj; i++)
                        {
                            Object3D obj = new Object3D();
                            lvl.areas[lvl.curAreaID].SpecialObjects.Add(obj);

                            ushort presetID = br.ReadUInt16();
                            uint obj_len = getSpecialObjectLength(presetID);
                            if (obj_len == 8 || obj_len == 10 || obj_len == 12)
                            {
                                obj.xPos = br.ReadInt16();
                                obj.yPos = br.ReadInt16();
                                obj.zPos = br.ReadInt16();
                                if (obj_len == 10 || obj_len == 12)
                                {
                                    // magic number 1.40625
                                    obj.yRot = (short)(br.ReadInt16() * 1.40625);

                                    if (obj_len == 12)
                                    {
                                        br.ReadByte();
                                        br.ReadByte();
                                    }
                                }
                            }

                            // スペシャルオブジェクトテーブル経由で情報を取得
                            // search for table_off
                            rom.br.BaseStream.Position = special_preset_table;
                            byte got = 0;
                            while (got != 0xFF)
                            {
                                got = rom.br.ReadByte();
                                if(got == presetID)
                                {
                                    rom.br.ReadByte();
                                    rom.br.ReadByte();
                                    obj.modelID = rom.br.ReadByte();
                                    obj.bhvAddress = rom.br.ReadUInt32();
                                    break;
                                }
                                rom.br.BaseStream.Position += 7;
                            }

                            // todo BehaviorScripts.parse for animation

                        }
                        break;
                    case 0x0044:
                        // water box / HMC gas
                        ushort num_boxes = br.ReadUInt16();
                        br.BaseStream.Position += num_boxes * 0x0C;
                        break;
                    default:
                        throw new Exception("unknown collision command");
                        break;
                }
            }
        }

        private static uint collisionLength(int type)
        {
            switch (type)
            {
                case 0x0E: // water (flowing) has parameters
                case 0x24: // quicksand
                case 0x25: // quicksand
                case 0x27: // quicksand
                case 0x2C: // horizontal wind
                case 0x2D: // quicksand
                case 0x40:
                    return 8;
                default:
                    return 6;
            }
        }

        private static uint getSpecialObjectLength(int obj)
        {
            if (obj > 0x64 && obj < 0x79) return 10;
            else if (obj > 0x78 && obj < 0x7E) return 8;
            else if (obj > 0x7D && obj < 0x83) return 10;
            else if (obj > 0x88 && obj < 0x8E) return 10;
            else if (obj > 0x82 && obj < 0x8A) return 12;
            else if (obj == 0x40) return 10;
            else if (obj == 0x64) return 12;
            else if (obj == 0xC0) return 8;
            else if (obj == 0xE0) return 12;
            else if (obj == 0xCD) return 12;
            else if (obj == 0x00) return 10;
            return 8;
        }

        static void CMD_39(Level lvl, uint segAddr)
        {
            ROM rom = ROM.Instance;

            byte seg = (byte)(segAddr >> 24);
            uint off = segAddr & 0x00FFFFFF;

            byte[] data = rom.segData[seg];
            BinaryDataReader br = new BinaryDataReader(new MemoryStream(data));
            br.ByteConverter = ByteConverter.BigEndian;
            br.BaseStream.Position = off;

            uint macro_preset_table = 0xEC7E0; // check for romhacks

            while (true)
            {
                Object3D obj = new Object3D();

                ushort firstAndSecond = br.ReadUInt16();

                int presetID = firstAndSecond & 0x1FF;
                // end macro object list
                if (presetID == 0x00 || presetID == 0x1E)
                    break;

                lvl.areas[lvl.curAreaID].MacroObjects.Add(obj);

                // yRot is 7bit (0~127) -> (0 degree ~ 360 degree)
                obj.yRot = (short)((firstAndSecond >> 9) * 2.8125);

                obj.xPos = br.ReadInt16();
                obj.yPos = br.ReadInt16();
                obj.zPos = br.ReadInt16();
                br.ReadByte();
                br.ReadByte();

                // マクロオブジェクトテーブル経由で情報を取得
                int table_off = (presetID - 0x1F) * 8;
                rom.br.BaseStream.Position = macro_preset_table + table_off;
                obj.bhvAddress = rom.br.ReadUInt32();
                obj.modelID = (byte)rom.br.ReadUInt16();
                rom.br.ReadByte();
                rom.br.ReadByte();

                // todo BehaviorScripts.parse for animation

            }
        }
    }
}