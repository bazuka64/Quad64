using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quad64
{
    internal class GeoScripts
    {
        public static void parse(GraphNode graphNode, byte segment, uint offset)
        {
            ROM rom = ROM.Instance;
            byte[] data = rom.segData[segment];
            BinaryDataReader br0 = new BinaryDataReader(new MemoryStream(data));
            br0.ByteConverter = ByteConverter.BigEndian;
            br0.BaseStream.Position = offset;

            GraphNode curNode = graphNode;
            bool insideSwitch = false;
            Stack<long> returnAddress = new Stack<long>();

            while (true)
            {
                byte cmd0 = br0.ReadByte();
                byte len = getCmdLength(cmd0);
                br0.BaseStream.Position--;
                byte[] cmd = br0.ReadBytes(len);

                // コマンド読み取り用のBinaryReader
                BinaryDataReader br = new BinaryDataReader(new MemoryStream(cmd));
                br.ByteConverter = ByteConverter.BigEndian;
                br.BaseStream.Position++;

                switch (cmd[0])
                {
                    case 0x01:
                        // end
                        return;
                    case 0x02:
                        // branch
                        {
                            // 0=jump, 1=jump and return
                            byte returnFlag = br.ReadByte();
                            br.BaseStream.Position += 2;
                            uint address = br.ReadUInt32();

                            byte seg = (byte)((address & 0xFF000000) >> 24);
                            uint off = address & 0x00FFFFFF;

                            if (seg != segment)
                                throw new Exception();

                            // switch case ２回目以降はここで弾く
                            if (curNode.insideSwitch && curNode.alreadyDone)
                                break;
                            if(curNode.insideSwitch && !curNode.alreadyDone)
                                curNode.alreadyDone = false;

                            if(returnFlag == 0)
                            {
                                br0.BaseStream.Position = off;
                            }
                            else if(returnFlag == 1)
                            {
                                returnAddress.Push(br0.BaseStream.Position);
                                br0.BaseStream.Position = off;
                            }
                        }
                        break;
                    case 0x03:
                        // return
                        {
                            br0.BaseStream.Position = returnAddress.Pop();
                        }
                        break;
                    case 0x04:
                        // open node
                        {
                            GraphNode newNode = new GraphNode();
                            newNode.parent = curNode;
                            curNode.children.Add(newNode);
                            curNode = newNode;

                            if (insideSwitch)
                            {
                                curNode.insideSwitch = true;
                                insideSwitch = false;
                            }
                        }
                        break;
                    case 0x05:
                        // close node
                        {
                            curNode = curNode.parent;
                        }
                        break;
                    case 0x08:
                        // screen area
                        break;
                    case 0x09:
                        // ortho
                        break;
                    case 0x0a:
                        // camera frustum
                        break;
                    case 0x0b:
                        // start (come from branch)
                        break;
                    case 0x0c:
                        // z buffer
                        break;
                    case 0x0e:
                        // switch case
                        // select first case
                        // まだbranchにしか対応してない
                        {
                            br.BaseStream.Position += 2;
                            byte switchCount = br.ReadByte();

                            insideSwitch = true;
                        }
                        // switch caseに対応する命令がbranch以外のとき例外
                        {
                            long temp = br0.Position;
                            br0.Position += 4; // skip open node
                            byte cmdSwitch = br0.ReadByte();
                            if(cmdSwitch != 0x02 && cmdSwitch != 0x15 && cmdSwitch != 0x13)
                            {
                                // branch, dl, animでなければ
                                throw new Exception();
                            }
                            br0.Position -= 5;
                        }
                        break;
                    case 0x0f:
                        // camera
                        break;
                    case 0x13:
                        // animated part
                        {
                            byte layer = br.ReadByte();
                            short x = br.ReadInt16();
                            short y = br.ReadInt16();
                            short z = br.ReadInt16();
                            uint address = br.ReadUInt32();
                            // if address == 0x00000000 then an invisible rotation joint is created

                            // switch case ２回目以降はここで弾く
                            if (curNode.insideSwitch && curNode.alreadyDone)
                                break;
                            if (curNode.insideSwitch && !curNode.alreadyDone)
                                curNode.alreadyDone = false;

                            //byte seg = (byte)((address & 0xFF000000) >> 24);
                            //uint off = address & 0x00FFFFFF;
                            // Fast3DScripts.parse(seg, off);
                        }
                        break;
                    case 0x15:
                        // display list
                        {
                            byte layer = br.ReadByte();
                            br.BaseStream.Position += 2;
                            uint address = br.ReadUInt32();

                            // switch case ２回目以降はここで弾く
                            if (curNode.insideSwitch && curNode.alreadyDone)
                                break;
                            if (curNode.insideSwitch && !curNode.alreadyDone)
                                curNode.alreadyDone = false;

                            //byte seg = (byte)((address & 0xFF000000) >> 24);
                            //uint off = address & 0x00FFFFFF;
                            // Fast3DScripts.parse(seg, off);
                        }
                        break;
                    case 0x16:
                        // shadow
                        break;
                    case 0x17:
                        // render obj
                        break;
                    case 0x18:
                        // geo asm
                        break;
                    case 0x19:
                        // background
                        break;
                    case 0x1d:
                        // scale
                        {
                            // Scale percentage (0x10000 = 100%) 
                            br.Position += 3;
                            uint scale = br.ReadUInt32();

                            curNode.scale = scale;
                        }
                        break;
                    default:
                        throw new Exception("level command is not implemented");
                        break;
                }
            }
        }

        private static byte getCmdLength(byte cmd)
        {
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
                case 0x1F:
                    return 0x10;
                case 0x0F:
                case 0x10:
                    return 0x14;
                default:
                    return 0x04;
            }
        }
    }
}
