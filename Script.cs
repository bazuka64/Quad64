using Syroot.BinaryData;
using System.IO;

namespace Quad64
{
    internal class Script
    {
        public static bool setSegmentPosition(ref BinaryStream bs, uint segOff)
        {
            uint seg = segOff >> 24;
            uint off = segOff & 0x00FFFFFF;

            if (!ROM.instance.segments.ContainsKey((int)seg))
                return false;

            byte[] segment = ROM.instance.segments[(int)seg];

            if (segment == null || segment.Length < off)
                return false;

            bs = new BinaryStream(new MemoryStream(segment));
            bs.Position = off;
            bs.ByteConverter = ByteConverter.Big;
            return true;
        }
    }
}