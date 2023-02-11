using Syroot.BinaryData;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Quad64.src
{
    internal class TextureFormats
    {
        public static byte[] decodeTexture(uint segOff, int width, int height, int format)
        {
            BinaryStream bs = null;
            if (!Script.setSegmentPosition(ref bs, segOff))
                return null;

            int color_format = format >> 5;
            int bit_size = (format & 0b00011000) >> 3;

            byte[] pixels = new byte[width * height * 4];

            if (color_format == 0 && bit_size == 2)
            {
                // RGBA 16-bit
                for (int i = 0; i < width * height; i++)
                {
                    ushort pixel;
                    try
                    {
                        pixel = bs.ReadUInt16();
                    }
                    catch
                    {
                        return null;
                    }

                    pixels[i * 4 + 0] = (byte)((pixel >> 11 & 0x1F) * 8);
                    pixels[i * 4 + 1] = (byte)((pixel >> 6 & 0x1F) * 8);
                    pixels[i * 4 + 2] = (byte)((pixel >> 1 & 0x1F) * 8);
                    pixels[i * 4 + 3] = (byte)((pixel & 0x01) > 0 ? 0xFF : 0x00);
                }

                return pixels;
            }
            else if (color_format == 3 && bit_size == 2)
            {
                // IA 16-bit

                for (int i = 0; i < width * height; i++)
                {
                    ushort pixel;
                    try
                    {
                        pixel = bs.ReadUInt16();
                    }
                    catch
                    {
                        return null;
                    }

                    pixels[i * 4 + 0] = (byte)(pixel >> 8);
                    pixels[i * 4 + 1] = (byte)(pixel >> 8);
                    pixels[i * 4 + 2] = (byte)(pixel >> 8);
                    pixels[i * 4 + 3] = (byte)(pixel & 0xFF);
                }

                return pixels;
            }
            else
                return null;
        }
    }
}