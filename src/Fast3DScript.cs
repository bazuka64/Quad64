using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using ByteConverter = Syroot.BinaryData.ByteConverter;

namespace Quad64.src
{
    public struct Vertex
    {
        public short x, y, z, f, u, v;
        public byte r_nx, g_ny, b_nz, a;
    }

    public struct Light
    {
        public Vector3 diffuseColor;
        public Vector3 diffuseDirection;
        public Vector3 ambientColor;
    }

    internal class Fast3DScript : Script
    {
        public static List<Mesh> parse(uint startSegOff)
        {
            List<Mesh> meshes = new List<Mesh>();

            BinaryStream bs = null;
            if (!setSegmentPosition(ref bs, startSegOff))
                return meshes;

            Stack<BinaryStream> returnAddr = new Stack<BinaryStream>();

            Vertex[] vertices = new Vertex[16];
            byte preCmd = 0x00;
            Mesh curMesh = null;
            int numVert = 0;
            int vertOff = 0;
            bool useTexture = false;
            uint textureSegOff = 0;
            int width = 0, height = 0;
            int format = 0;
            int wrapS = 0, wrapT = 0;
            float scaleS = 1, scaleT = 1;
            uint geometryMode = 0x00020000;
            Light light = new Light();
            bool newMesh = true;
            bool newVert = false;

            while (true)
            {
                byte[] cmd = bs.ReadBytes(8);

                BinaryStream bsCmd = new BinaryStream(new MemoryStream(cmd));
                bsCmd.ByteConverter = ByteConverter.Big;

                if (!commandNames.ContainsKey(cmd[0]))
                    return meshes;
                //Console.WriteLine(commandNames[cmd[0]].PadRight(40) + BitConverter.ToString(cmd).Replace("-", " "));


                switch (cmd[0])
                {
                    case 0x03: // move mem
                        {
                            bsCmd.Position = 1;
                            byte flag = bsCmd.Read1Byte();
                            bsCmd.Position = 4;
                            uint segOff = bsCmd.ReadUInt32();

                            BinaryStream bsLight = null;
                            setSegmentPosition(ref bsLight, segOff);

                            if (flag == 0x86)
                            {
                                // diffuse light
                                light.diffuseColor.X = bsLight.Read1Byte() / (float)0xFF;
                                light.diffuseColor.Y = bsLight.Read1Byte() / (float)0xFF;
                                light.diffuseColor.Z = bsLight.Read1Byte() / (float)0xFF;

                                bsLight.Position += 5;

                                light.diffuseDirection.X = bsLight.ReadSByte() / (float)0x7F;
                                light.diffuseDirection.Y = bsLight.ReadSByte() / (float)0x7F;
                                light.diffuseDirection.Z = bsLight.ReadSByte() / (float)0x7F;
                            }
                            else if (flag == 0x88)
                            {
                                // ambient light
                                light.ambientColor.X = bsLight.Read1Byte() / (float)0xFF;
                                light.ambientColor.Y = bsLight.Read1Byte() / (float)0xFF;
                                light.ambientColor.Z = bsLight.Read1Byte() / (float)0xFF;
                            }

                            newMesh = true;
                        }
                        break;
                    case 0x04: // vertex
                        {
                            bsCmd.Position = 1;
                            byte temp = bsCmd.Read1Byte();
                            numVert = (temp >> 4) + 1;
                            vertOff = temp & 0x0F;
                            bsCmd.Position = 4;
                            uint segOff = bsCmd.ReadUInt32();

                            BinaryStream bsVert = null;
                            if (!setSegmentPosition(ref bsVert, segOff))
                            {
                                //throw new Exception();
                                return meshes;
                            }

                            for (int i = 0; i < numVert; i++)
                            {
                                vertices[vertOff + i].x = bsVert.ReadInt16();
                                vertices[vertOff + i].y = bsVert.ReadInt16();
                                vertices[vertOff + i].z = bsVert.ReadInt16();
                                vertices[vertOff + i].f = bsVert.ReadInt16();
                                vertices[vertOff + i].u = bsVert.ReadInt16();
                                vertices[vertOff + i].v = bsVert.ReadInt16();
                                vertices[vertOff + i].r_nx = bsVert.Read1Byte();
                                vertices[vertOff + i].g_ny = bsVert.Read1Byte();
                                vertices[vertOff + i].b_nz = bsVert.Read1Byte();
                                vertices[vertOff + i].a = bsVert.Read1Byte();
                            }

                            newVert = true;
                        }
                        break;
                    case 0x06: // dl
                        {
                            bsCmd.Position = 1;
                            byte flag = bsCmd.Read1Byte();
                            bsCmd.Position = 4;
                            uint segOff = bsCmd.ReadUInt32();

                            if (flag == 0)
                                returnAddr.Push(bs);

                            setSegmentPosition(ref bs, segOff);
                        }
                        break;
                    case 0xb6: // clear geometry mode
                        {
                            bsCmd.Position = 4;
                            uint mask = bsCmd.ReadUInt32();

                            geometryMode &= ~mask;

                            newMesh = true;
                        }
                        break;
                    case 0xb7: // set geometry mode
                        {
                            bsCmd.Position = 4;
                            uint mask = bsCmd.ReadUInt32();

                            geometryMode |= mask;

                            newMesh = true;
                        }
                        break;
                    case 0xb8: // end
                        if (returnAddr.Count > 0)
                        {
                            bs = returnAddr.Pop();
                            break;
                        }
                        else
                        {
                            // build mesh
                            foreach (var mesh in meshes)
                            {
                                mesh.build();
                            }
                            return meshes;
                        }
                    case 0xbb: // texture
                        {
                            bsCmd.Position = 3;
                            byte flag = bsCmd.Read1Byte();
                            ushort s = bsCmd.ReadUInt16(); // horizontal
                            ushort t = bsCmd.ReadUInt16(); // vertical

                            useTexture = flag == 1;

                            if ((geometryMode & 0x00040000) > 0)
                            {
                                width = s >> 6;
                                height = t >> 6;

                            }
                            else
                            {
                                scaleS = s / (float)0xFFFF;
                                scaleT = t / (float)0xFFFF;

                            }

                            newMesh = true;
                        }
                        break;
                    case 0xbf: // triangle
                        {
                            bsCmd.Position = 5;
                            int a = bsCmd.Read1Byte() / 0x0A - vertOff;
                            int b = bsCmd.Read1Byte() / 0x0A - vertOff;
                            int c = bsCmd.Read1Byte() / 0x0A - vertOff;

                            // create mesh
                            if (newMesh)
                            {
                                newMesh = false;
                                curMesh = new Mesh();
                                meshes.Add(curMesh);

                                curMesh.width = width;
                                curMesh.height = height;
                                curMesh.scaleS = scaleS;
                                curMesh.scaleT = scaleT;

                                // texture
                                curMesh.useTexture = useTexture;
                                if (useTexture)
                                {
                                    if (!Level.instance.textures.ContainsKey(textureSegOff))
                                    {
                                        byte[] pixels = TextureFormats.decodeTexture(textureSegOff, width, height, format);

                                        if (pixels != null)
                                        {
                                            Texture texture = new Texture(pixels, width, height, wrapS, wrapT);
                                            Level.instance.textures.Add(textureSegOff, texture);
                                        }
                                    }
                                    if (Level.instance.textures.ContainsKey(textureSegOff))
                                        curMesh.texture = Level.instance.textures[textureSegOff];
                                }

                                // light
                                if ((geometryMode & 0x00020000) > 0)
                                {
                                    curMesh.useLight = true;
                                    curMesh.light = light;
                                }
                                else
                                    curMesh.useLight = false;

                                // cullface
                                curMesh.cullFront = (geometryMode & 0x00001000) > 0;
                                curMesh.cullBack = (geometryMode & 0x00002000) > 0;
                            }

                            // vertex
                            if (newVert || curMesh.vertices.Count == 0)
                            {
                                curMesh.curVertNum += numVert;
                                for (int i = 0; i < numVert; i++)
                                {
                                    curMesh.vertices.Add(vertices[i + vertOff]);
                                }
                                newVert = false;
                            }

                            // index
                            if (a >= numVert || b >= numVert || c >= numVert
                                || a < 0 || b < 0 || c < 0)
                            {
                                meshes.Clear();
                                return meshes;
                            }
                            curMesh.indices.Add((ushort)(curMesh.curVertNum - numVert + a));
                            curMesh.indices.Add((ushort)(curMesh.curVertNum - numVert + b));
                            curMesh.indices.Add((ushort)(curMesh.curVertNum - numVert + c));
                        }
                        break;
                    case 0xf2: // set tile size
                        {
                            bsCmd.Position = 4;
                            uint temp = bsCmd.ReadUInt32();
                            uint w = (temp & 0x00FFF000) >> 12;
                            uint h = temp & 0x00000FFF;

                            width = (int)((w >> 2) + 1);
                            height = (int)((h >> 2) + 1);

                            newMesh = true;
                        }
                        break;
                    case 0xf5: // set tile
                        {
                            int t = (cmd[5] & 0b00001100) >> 2;
                            int s = cmd[6] & 0b00000011;

                            wrapT = getWrap(t);
                            wrapS = getWrap(s);

                            newMesh = true;
                        }
                        break;
                    case 0xfd: // set texture image
                        {
                            bsCmd.Position = 1;
                            format = bsCmd.Read1Byte() & 0b11111000;
                            bsCmd.Position = 4;
                            textureSegOff = bsCmd.ReadUInt32();

                            newMesh = true;
                        }
                        break;
                    default:
                        break;
                }

                preCmd = cmd[0];
            }
        }

        static int getWrap(int wrap)
        {
            switch (wrap)
            {

                case 1:
                    return (int)TextureWrapMode.MirroredRepeat;
                case 2:
                    return (int)TextureWrapMode.ClampToEdge;
                case 0:
                default:
                    return (int)TextureWrapMode.Repeat;
            }
        }

        static Dictionary<byte, string> commandNames = new Dictionary<byte, string>
        {

            {0x00, "G_SPNOOP"},
            {0x01, "G_MTX"},
            {0x03, "G_MOVEMEM"},
            {0x04, "G_VTX"},
            {0x06, "G_DL"},
            {0xB2, "G_RDPHALF_CONT"},
            {0xB3, "G_RDPHALF_2"},
            {0xB4, "G_RDPHALF_1"},
            {0xB6, "G_CLEARGEOMETRYMODE"},
            {0xB7, "G_SETGEOMETRYMODE"},
            {0xB8, "G_ENDDL"},
            {0xB9, "G_SetOtherMode_L"},
            {0xBA, "G_SetOtherMode_H"},
            {0xBB, "G_TEXTURE"},
            {0xBC, "G_MOVEWORD"},
            {0xBD, "G_POPMTX"},
            {0xBE, "G_CULLDL"},
            {0xBF, "G_TRI1"},
            {0xC0, "G_NOOP"},
            {0xE4, "G_TEXRECT"},
            {0xE5, "G_TEXRECTFLIP"},
            {0xE6, "G_RDPLOADSYNC"},
            {0xE7, "G_RDPPIPESYNC"},
            {0xE8, "G_RDPTILESYNC"},
            {0xE9, "G_RDPFULLSYNC"},
            {0xEA, "G_SETKEYGB"},
            {0xEB, "G_SETKEYR"},
            {0xEC, "G_SETCONVERT"},
            {0xED, "G_SETSCISSOR"},
            {0xEE, "G_SETPRIMDEPTH"},
            {0xEF, "G_RDPSetOtherMode"},
            {0xF0, "G_LOADTLUT"},
            {0xF2, "G_SETTILESIZE"},
            {0xF3, "G_LOADBLOCK"},
            {0xF4, "G_LOADTILE"},
            {0xF5, "G_SETTILE"},
            {0xF6, "G_FILLRECT"},
            {0xF7, "G_SETFILLCOLOR"},
            {0xF8, "G_SETFOGCOLOR"},
            {0xF9, "G_SETBLENDCOLOR"},
            {0xFA, "G_SETPRIMCOLOR"},
            {0xFB, "G_SETENVCOLOR"},
            {0xFC, "G_SETCOMBINE"},
            {0xFD, "G_SETTIMG"},
            {0xFE, "G_SETZIMG"},
            {0xFF, "G_SETCIMG" },

        };
    }
}