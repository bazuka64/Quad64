using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.IO;

namespace Quad64.src
{
    internal class LevelScript : Script
    {
        static Area curArea;

        public static Level parse(ushort levelID)
        {
            Level level = new Level(levelID);

            ROM.instance.resetSegment();

            BinaryStream bs = null;
            setSegmentPosition(ref bs, 0x15000000);

            Stack<BinaryStream> returnAddr = new Stack<BinaryStream>();

            curArea = null;

            while (true)
            {
                bs.Position++;
                byte cmdLen = bs.Read1Byte();
                bs.Position -= 2;
                byte[] cmd = bs.ReadBytes(cmdLen);

                BinaryStream bsCmd = new BinaryStream(new MemoryStream(cmd));
                bsCmd.ByteConverter = ByteConverter.Big;

                //Console.WriteLine(BitConverter.ToString(cmd).Replace("-", " "));

                switch (cmd[0])
                {
                    case 0x00: // load and jump
                        {
                            bsCmd.Position = 3;
                            byte seg = bsCmd.Read1Byte();
                            uint start = bsCmd.ReadUInt32();
                            uint end = bsCmd.ReadUInt32();
                            uint segOff = bsCmd.ReadUInt32();

                            // skip star select screen
                            if (seg == 0x14) break;

                            ROM.instance.setSegment(seg, start, end, false);
                            setSegmentPosition(ref bs, segOff);
                        }
                        break;
                    case 0x02: // end
                        return level;
                    case 0x05: // jump
                        {
                            bsCmd.Position = 4;
                            uint segOff = bsCmd.ReadUInt32();

                            setSegmentPosition(ref bs, segOff);
                        }
                        break;
                    case 0x06: // jump link
                        {
                            bsCmd.Position = 4;
                            uint segOff = bsCmd.ReadUInt32();

                            returnAddr.Push(bs);
                            setSegmentPosition(ref bs, segOff);
                        }
                        break;
                    case 0x07: // return
                        bs = returnAddr.Pop();
                        break;
                    case 0x0c: // conditinal jump
                        {
                            bsCmd.Position = 4;
                            int checkLevelID = bsCmd.ReadInt32();
                            uint segOff = bsCmd.ReadUInt32();

                            if (checkLevelID == levelID)
                                setSegmentPosition(ref bs, segOff);
                        }
                        break;
                    case 0x17: // load raw
                    case 0x18: // load mio0
                    case 0x1a: // load texture
                        {
                            bsCmd.Position = 3;
                            byte seg = bsCmd.Read1Byte();
                            uint start = bsCmd.ReadUInt32();
                            uint end = bsCmd.ReadUInt32();

                            ROM.instance.setSegment(seg, start, end, cmd[0] != 0x17);
                        }
                        break;
                    case 0x1f: // area
                        {
                            bsCmd.Position = 2;
                            byte areaID = bsCmd.Read1Byte();
                            bsCmd.Position = 4;
                            uint segOff = bsCmd.ReadUInt32();

                            curArea = new Area(areaID);
                            level.areas[areaID] = curArea;
                            level.hasArea = true;
                            curArea.level = level;

                            curArea.model.root = GeoScript.parse(segOff, true);
                        }
                        break;
                    case 0x20: // end area
                        curArea = null;
                        break;
                    case 0x21: // load model from dl
                        {
                            bsCmd.Position = 2;
                            int layer = bsCmd.Read1Byte() >> 4;
                            byte modelID = bsCmd.Read1Byte();
                            uint segOff = bsCmd.ReadUInt32();

                            Model3D model = new Model3D();
                            level.models.Add(modelID, model);
                            model.meshes = Fast3DScript.parse(segOff);
                            foreach (var mesh in model.meshes)
                            {
                                mesh.layer = layer;
                            }
                        }
                        break;
                    case 0x22: // load model from geo
                        {
                            bsCmd.Position = 3;
                            byte modelID = bsCmd.Read1Byte();
                            uint segOff = bsCmd.ReadUInt32();

                            Model3D model = new Model3D();
                            if (level.models.ContainsKey(modelID))
                                level.models.Remove(modelID);
                            level.models.Add(modelID, model);
                            model.root = GeoScript.parse(segOff, false);
                        }
                        break;
                    case 0x24: // place object
                        {
                            Object3D obj = new Object3D();

                            bsCmd.Position = 2;
                            obj.act = bsCmd.Read1Byte();
                            obj.modelID = bsCmd.Read1Byte();
                            obj.posX = bsCmd.ReadInt16();
                            obj.posY = bsCmd.ReadInt16();
                            obj.posZ = bsCmd.ReadInt16();
                            obj.rotX = bsCmd.ReadInt16();
                            obj.rotY = bsCmd.ReadInt16();
                            obj.rotZ = bsCmd.ReadInt16();
                            obj.behParam = bsCmd.ReadUInt32();
                            obj.behAddr = bsCmd.ReadUInt32();

                            curArea.Objects.Add(obj);

                            BehaviorScript.parse(obj.behAddr, obj);
                        }
                        break;
                    case 0x2e: // special object
                        {
                            bsCmd.Position = 4;
                            uint segOff = bsCmd.ReadUInt32();

                            CMD_2E(segOff);
                        }
                        break;
                    case 0x34: // blackout screen
                        // escape from infinite loop of end cake picture
                        return level;
                    case 0x36: // set music
                        {
                            curArea.seqID = cmd[5];
                        }
                        break;
                    case 0x39: // macro object
                        {
                            bsCmd.Position = 4;
                            uint segOff = bsCmd.ReadUInt32();

                            CMD_39(segOff);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        static void CMD_39(uint segOff)
        {
            BinaryStream bs = null;
            setSegmentPosition(ref bs, segOff);

            while (true)
            {
                short temp = bs.ReadInt16();

                if (temp == 0x001E) break;

                Object3D obj = new Object3D();
                curArea.MacroObjects.Add(obj);
                obj.rotY = (short)((temp >> 9) * 2.8125f);
                int presetID = temp & 0x1FF;
                obj.posX = bs.ReadInt16();
                obj.posY = bs.ReadInt16();
                obj.posZ = bs.ReadInt16();
                bs.Position += 2;

                uint macro_preset_table = 0xEC7E0;
                BinaryStream bsRom = ROM.instance.bs;
                bsRom.Position = macro_preset_table + (presetID - 0x1F) * 8;
                obj.behAddr = bsRom.ReadUInt32();
                obj.modelID = (byte)bsRom.ReadInt16();
                bsRom.Position += 2;

                BehaviorScript.parse(obj.behAddr, obj);
            }
        }

        static void CMD_2E(uint segOff)
        {
            BinaryStream bs = null;
            setSegmentPosition(ref bs, segOff);

            // vertex
            short cmd = bs.ReadInt16();
            short num_verts = bs.ReadInt16();
            bs.Position += 6 * num_verts;

            // triangle
            while (true)
            {
                cmd = bs.ReadInt16();
                if (cmd == 0x0041) break;

                short num_tri = bs.ReadInt16();
                bs.Position += collisionLength(cmd) * num_tri;
            }

            // special object
            while (true)
            {
                cmd = bs.ReadInt16();
                switch (cmd)
                {
                    case 0x0042: // end
                        return;
                    case 0x0043: // special object
                        short num_obj = bs.ReadInt16();
                        for (int i = 0; i < num_obj; i++)
                        {
                            Object3D obj = new Object3D();
                            curArea.SpecialObjects.Add(obj);

                            short presetID = bs.ReadInt16();
                            int obj_len = getSpecialObjectLength(presetID);

                            obj.posX = bs.ReadInt16();
                            obj.posY = bs.ReadInt16();
                            obj.posZ = bs.ReadInt16();

                            if (obj_len == 10 || obj_len == 12)
                            {
                                obj.rotY = (short)(bs.ReadInt16() * 1.40625);

                                if (obj_len == 12)
                                    bs.Position += 2;
                            }

                            uint special_preset_table = 0xED350;
                            BinaryStream bsRom = ROM.instance.bs;
                            bsRom.Position = special_preset_table;

                            // search
                            byte got = 0;
                            while (got != 0xFF)
                            {
                                got = bsRom.Read1Byte();
                                if (got == presetID)
                                {
                                    bsRom.Position += 2;
                                    obj.modelID = bsRom.Read1Byte();
                                    obj.behAddr = bsRom.ReadUInt32();

                                    BehaviorScript.parse(obj.behAddr, obj);
                                    break;
                                }
                                bsRom.Position += 7;
                            }
                        }
                        break;
                    case 0x0044: // water box

                        // water texture segOff = 0x02011C58
                        // format = rgba16 32 x 32

                        short num_boxes = bs.ReadInt16();
                        for (int i = 0; i < num_boxes; i++)
                        {


                            // smaller than 0x32 => water
                            // 0x32 or 0xF0 => toxic haze
                            // greater than 0x32 => no effect
                            short id = bs.ReadInt16();

                            if (id >= 0x32) continue;

                            WaterBox box = new WaterBox();

                            box.x1 = bs.ReadInt16();
                            box.z1 = bs.ReadInt16();
                            box.x2 = bs.ReadInt16();
                            box.z2 = bs.ReadInt16();
                            box.y = bs.ReadInt16();

                            box.build();
                            curArea.boxes.Add(box);
                        }
                        break;
                }
            }
        }

        static int collisionLength(int type)
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

        static int getSpecialObjectLength(int obj)
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
    }
}