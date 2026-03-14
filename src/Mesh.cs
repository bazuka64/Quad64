using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Quad64_new;
using System;
using System.Collections.Generic;

namespace Quad64.src
{
    public class Mesh
    {
        public List<Vertex> vertices = new List<Vertex>();
        public int curVertNum;
        public int width = 0, height = 0;
        public float scaleS = 1, scaleT = 1;

        public short[] positions;
        public float[] texCoords;
        public byte[] colorNormals;
        public List<ushort> indices = new List<ushort>();

        public bool useTexture;
        public Texture texture;

        public bool useLight;
        public Light light;

        public bool isBillboard;
        public bool isWaterBox;

        public int layer;
        public bool cullFront;
        public bool cullBack;

        int vao;
        int vertexCount;

        int positionBuf;
        int texCoordBuf;
        int colorNormalBuf;
        int ibo;

        public void build()
        {
            vertexCount = indices.Count;

            // list to array
            if (!isWaterBox)
            {
                positions = new short[vertices.Count * 3];
                texCoords = new float[vertices.Count * 2];
                colorNormals = new byte[vertices.Count * 4];
                for (int i = 0; i < vertices.Count; i++)
                {
                    positions[i * 3 + 0] = vertices[i].x;
                    positions[i * 3 + 1] = vertices[i].y;
                    positions[i * 3 + 2] = vertices[i].z;
                    texCoords[i * 2 + 0] = vertices[i].u / (width * 32f) * scaleS;
                    texCoords[i * 2 + 1] = vertices[i].v / (height * 32f) * scaleT;
                    colorNormals[i * 4 + 0] = vertices[i].r_nx;
                    colorNormals[i * 4 + 1] = vertices[i].g_ny;
                    colorNormals[i * 4 + 2] = vertices[i].b_nz;
                    colorNormals[i * 4 + 3] = vertices[i].a;
                }
            }

            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            positionBuf = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionBuf);
            GL.BufferData(BufferTarget.ArrayBuffer, positions.Length * sizeof(short), positions, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Short, false, sizeof(short) * 3, 0);

            texCoordBuf = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, texCoordBuf);
            GL.BufferData(BufferTarget.ArrayBuffer, texCoords.Length * sizeof(float), texCoords, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, sizeof(float) * 2, 0);

            colorNormalBuf = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, colorNormalBuf);
            GL.BufferData(BufferTarget.ArrayBuffer, colorNormals.Length * sizeof(byte), colorNormals, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(2);
            if (!useLight)
                GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, sizeof(sbyte) * 4, 0);
            else
                GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Byte, true, sizeof(byte) * 4, 0);

            ibo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(ushort), indices.ToArray(), BufferUsageHint.StaticDraw);
        }

        public void draw(Matrix4 transform)
        {
            if (Level.instance.layers[layer])
            {
                GL.Disable(EnableCap.CullFace);
                if (cullFront || cullBack)
                    GL.Enable(EnableCap.CullFace);
                if (cullFront)
                    GL.CullFace(CullFaceMode.Front);

                GL.BindVertexArray(vao);

                MainWindow.shader.SetMatrix4("model", transform);

                MainWindow.shader.SetInt("useTexture", useTexture ? 1 : 0);
                MainWindow.shader.SetInt("useLight", useLight ? 1 : 0);


                MainWindow.shader.SetInt("isBillboard", isBillboard ? 1 : 0);
                MainWindow.shader.SetInt("isWaterBox", isWaterBox ? 1 : 0);

                if (useLight)
                {
                    MainWindow.shader.SetVector3("diffuseColor", light.diffuseColor);
                    MainWindow.shader.SetVector3("diffuseDirection", light.diffuseDirection);
                    MainWindow.shader.SetVector3("ambientColor", light.ambientColor);
                }



                if (useTexture && texture != null)
                {
                    GL.BindTexture(TextureTarget.Texture2D, texture.id);
                }

                GL.DrawElements(BeginMode.Triangles, vertexCount, DrawElementsType.UnsignedShort, 0);

                GL.BindTexture(TextureTarget.Texture2D, 0);

                Level.instance.areas[Level.instance.curAreaID].meshCount++;
            }

        }
    }
}