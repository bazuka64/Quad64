using Syroot.BinaryData;
using System.Collections.Generic;
using System.IO;

namespace Quad64
{
    internal class BehaviorScript : Script
    {
        public static void parse(uint startSegOff, Object3D obj)
        {
            if (startSegOff == 0)
                return;

            BinaryStream bs = null;
            if (!setSegmentPosition(ref bs, startSegOff))
                return;

            Stack<BinaryStream> returnAddr = new Stack<BinaryStream>();

            while (true)
            {
                byte cmd0 = bs.Read1Byte();
                byte cmdLen = getCmdLength(cmd0);
                bs.Position--;
                byte[] cmd = bs.ReadBytes(cmdLen);

                BinaryStream bsCmd = new BinaryStream(new MemoryStream(cmd));
                bsCmd.ByteConverter = ByteConverter.Big;

                switch (cmd[0])
                {
                    case 0x02: // call
                        {
                            bsCmd.Position = 4;
                            uint segOff = bsCmd.ReadUInt32();

                            returnAddr.Push(bs);
                            setSegmentPosition(ref bs, segOff);
                        }
                        break;
                    case 0x03: // return
                        bs = returnAddr.Pop();
                        break;
                    case 0x04: // goto
                        {
                            bsCmd.Position = 4;
                            uint segOff = bsCmd.ReadUInt32();

                            setSegmentPosition(ref bs, segOff);
                        }
                        break;
                    case 0x08: // begin loop
                        return;
                    case 0x0a: // end
                        return;
                    case 0x1d: // deactivate
                        return;
                    case 0x1c: // spawn child object
                        {

                        }
                        break;
                    case 0x1e: // drop to floor
                        break;
                    case 0x27: // animation
                        {
                            bsCmd.Position = 4;
                            uint segOff = bsCmd.ReadUInt32();

                            obj.animsAddr = segOff;
                        }
                        break;
                    case 0x29: // spawn child object with param
                        {

                        }
                        break;
                    case 0x2c: // spawn new object
                        {

                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private static byte getCmdLength(byte cmd)
        {
            switch (cmd)
            {
                case 0x02:
                case 0x04:
                case 0x0C:
                case 0x13:
                case 0x16:
                case 0x17:
                case 0x23:
                case 0x27:
                case 0x2A:
                case 0x2E:
                case 0x2F:
                case 0x36:
                case 0x37:
                    return 0x08;
                case 0x1C:
                case 0x29:
                case 0x2B:
                case 0x2C:
                    return 0x0C;
                case 0x30:
                    return 0x14;
                default:
                    return 0x04;
            }
        }
    }
}