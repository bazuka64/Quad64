using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Quad64.src
{
    internal class GeoScript : Script
    {
        static GraphNode curNode;
        static Stack<GraphNode> parentNodes;

        public static void createNode()
        {
            curNode = new GraphNode();
            GraphNode curParent;
            curParent = parentNodes.Peek();
            curParent.children.Add(curNode);
            curNode.parent = curParent;
        }

        public static GraphNode parse(uint startSegOff, bool isArea)
        {
            GraphNode root = new GraphNode();
            curNode = root;
            parentNodes = new Stack<GraphNode>();
            parentNodes.Push(root);
            root.children = new List<GraphNode>();

            BinaryStream bs = null;
            if (!setSegmentPosition(ref bs, startSegOff))
                return root;

            Stack<BinaryStream> returnAddr = new Stack<BinaryStream>();
            Stack<BinaryStream> endAddr = new Stack<BinaryStream>();

            while (true)
            {
                byte cmd0 = bs.Read1Byte();
                byte cmdLen = getCmdLength(cmd0);
                bs.Position--;
                byte[] cmd = bs.ReadBytes(cmdLen);

                BinaryStream bsCmd = new BinaryStream(new MemoryStream(cmd));
                bsCmd.ByteConverter = ByteConverter.Big;

                //Console.WriteLine(new String(' ', parentNodes.Count) + commandNames[cmd0].PadRight(40) + BitConverter.ToString(cmd).Replace("-", " "));

                switch (cmd[0])
                {
                    case 0x00: // branch and store
                        {
                            bsCmd.Position = 4;
                            uint segOff = bsCmd.ReadUInt32();

                            endAddr.Push(bs);
                            setSegmentPosition(ref bs, segOff);
                        }
                        break;
                    case 0x01: // end
                        if (endAddr.Count > 0)
                        {
                            bs = endAddr.Pop();
                            break;
                        }
                        else
                            return root;
                    case 0x02: // branch
                        {
                            bsCmd.Position = 1;
                            byte flag = bsCmd.Read1Byte();
                            bsCmd.Position = 4;
                            uint segOff = bsCmd.ReadUInt32();

                            if (flag == 1)
                                returnAddr.Push(bs);

                            setSegmentPosition(ref bs, segOff);
                        }
                        break;
                    case 0x03: // return
                        bs = returnAddr.Pop();
                        break;
                    case 0x04: // open node
                        parentNodes.Push(curNode);
                        curNode.children = new List<GraphNode>();
                        break;
                    case 0x05: // close node
                        curNode = parentNodes.Pop();
                        break;
                    case 0x0a: // set camera frustum
                        createNode();
                        if (cmd[1] == 0)
                            throw new Exception();
                        break;
                    case 0x0b: // start geo layout
                        createNode();

                        break;
                    case 0x0d: // rander range
                        createNode();
                        {
                            bsCmd.Position = 4;
                            curNode.minDistance = bsCmd.ReadInt16();
                            curNode.maxDistance = bsCmd.ReadInt16();

                            curNode.isRanderRange = true;
                        }
                        break;
                    case 0x0e: // switch case
                        createNode();
                        curNode.isSwitchCase = true;
                        break;
                    case 0x10: // translate rotate
                        createNode();
                        {
                            // ex. unagi
                            bsCmd.Position = 1;
                            int param = bsCmd.ReadByte();
                            bsCmd.Position = 4;
                            curNode.offX = bsCmd.ReadInt16();
                            curNode.offY = bsCmd.ReadInt16();
                            curNode.offZ = bsCmd.ReadInt16();
                            curNode.rotX = bsCmd.ReadInt16();
                            curNode.rotY = bsCmd.ReadInt16();
                            curNode.rotZ = bsCmd.ReadInt16();

                            // follow dl
                            int enableDL = param & 0b10000000;
                            int fieldLayout = param & 0b01110000;
                            int layer = param & 0b00001111;
                            if (fieldLayout == 0)
                            {
                                if (enableDL != 0)
                                {
                                    uint segOff = bs.ReadUInt32();
                                    curNode.meshes = Fast3DScript.parse(segOff);
                                    foreach (var mesh in curNode.meshes)
                                    {
                                        mesh.layer = layer;
                                    }
                                }
                            }
                            else
                                throw new Exception();
                        }
                        break;
                    case 0x11: // translate
                        createNode();
                        {
                            bsCmd.Position = 1;
                            int temp = bsCmd.ReadByte();
                            int enableDL = temp & 0b10000000;
                            int layer = temp & 0x0F;
                            curNode.offX = bsCmd.ReadInt16();
                            curNode.offY = bsCmd.ReadInt16();
                            curNode.offZ = bsCmd.ReadInt16();

                            // follow dl
                            if (enableDL != 0)
                                throw new Exception();
                        }
                        break;
                    case 0x12: // rotate
                        createNode();
                        {
                            bsCmd.Position = 1;
                            int temp = bsCmd.ReadByte();
                            int enableDL = temp & 0b10000000;
                            int layer = temp & 0x0F;
                            curNode.rotX = bsCmd.ReadInt16();
                            curNode.rotY = bsCmd.ReadInt16();
                            curNode.rotZ = bsCmd.ReadInt16();

                            // follow dl
                            if (enableDL != 0)
                                throw new Exception();
                        }
                        break;
                    case 0x13: // animated part
                        createNode();
                        {
                            bsCmd.Position = 1;
                            int layer = bsCmd.ReadByte();
                            curNode.offX = bsCmd.ReadInt16();
                            curNode.offY = bsCmd.ReadInt16();
                            curNode.offZ = bsCmd.ReadInt16();
                            uint segOff = bsCmd.ReadUInt32();

                            curNode.isAnim = true;

                            if (segOff == 0x00000000) break;

                            curNode.meshes = Fast3DScript.parse(segOff);
                            foreach (var mesh in curNode.meshes)
                            {
                                mesh.layer = layer;
                            }
                        }
                        break;
                    case 0x14: // billboard
                        createNode();
                        curNode.isBillboard = true;
                        if ((cmd[1] & 0b10000000) > 0)
                            throw new Exception();
                        break;
                    case 0x15: // display list
                        createNode();
                        {
                            bsCmd.Position = 1;
                            byte layer = bsCmd.Read1Byte();
                            bsCmd.Position = 4;
                            uint segOff = bsCmd.ReadUInt32();

                            if (segOff == 0x00000000) break;

                            curNode.meshes = Fast3DScript.parse(segOff);
                            foreach (var mesh in curNode.meshes)
                            {
                                mesh.layer = layer;

                                mesh.isBillboard = curNode.parent.isBillboard;
                                if (mesh.isBillboard)
                                    mesh.cullBack = mesh.cullFront = false;

                                if (isArea)
                                    mesh.cullBack = true;
                            }
                        }
                        break;
                    case 0x1b:
                        throw new Exception();
                    case 0x1d: // scale
                        if ((cmd[1] & 0b10000000) > 0)
                            throw new Exception();
                        createNode();
                        {
                            bsCmd.Position = 4;
                            uint scale = bsCmd.ReadUInt32();

                            curNode.scale = scale / (float)0x10000;
                        }
                        break;
                    default:
                        createNode();
                        break;
                }
            }
        }

        private static byte getCmdLength(byte cmd)
        {
            if (cmd > 0x20)
                throw new Exception();

            switch (cmd)
            {
                case 0x00:
                case 0x02:
                case 0x0D:
                case 0x0E:
                case 0x11:
                case 0x12:
                case 0x14:
                case 0x15:
                case 0x16:
                case 0x18:
                case 0x19:
                case 0x1A:
                case 0x1D:
                case 0x1E:
                    return 0x08;
                case 0x08:
                case 0x0A:
                case 0x13:
                case 0x1C:
                    return 0x0C;
                case 0x10:
                case 0x1F:
                    return 0x10;
                case 0x0F:
                    return 0x14;
                default:
                    return 0x04;
            }
        }

        static Dictionary<byte, string> commandNames = new Dictionary<byte, string>
        {

            {0x00, "Branch and Store"},
            {0x01, "Terminate Geometry Layout"},
            {0x02, "Branch Geometry Layout"},
            {0x03, "Return From Branch"},
            {0x04, "Open Node"},
            {0x05, "Close Node"},
            {0x06, "Store Current Node Pointer To Table"},
            {0x07, "Set/OR/AND Node Flags"},
            {0x08, "Set Screen Render Area"},
            {0x09, "Create Ortho Matrix"},
            {0x0A, "Set Camera Frustum"},
            {0x0B, "Start Geo Layout"},
            {0x0C, "Enable/Disable Z-Buffer"},
            {0x0D, "Set Render Range"},
            {0x0E, "Switch Case"},
            {0x0F, "Create Camera Graph Node"},
            {0x10, "Translate and Rotate"},
            {0x11, "Translate Node"},
            {0x12, "Rotate Node"},
            {0x13, "Load Display List With Offset"},
            {0x14, "Billboard Model and Translate"},
            {0x15, "Load Display List"},
            {0x16, "Start Geo Layout with Shadow"},
            {0x17, "Create Object List"},
            {0x18, "Load Polygons ASM"},
            {0x19, "Set Background"},
            {0x1A, "No Operation"},
            {0x1C, "Create Held Object"},
            {0x1D, "Scale Model"},
            {0x1E, "No Operation"},
            {0x1F, "No Operation"},
            {0x20, "Start Geo Layout with Render Area"},
        };
    }
}