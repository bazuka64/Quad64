using libsm64sharp;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SixLabors.ImageSharp.ColorSpaces;
using System;
using System.Windows.Media.Media3D;

namespace Quad64.src
{
    internal class Sm64Manager
    {
        public ISm64Context sm64Context;
        public ISm64Mario sm64Mario;

        float[] positions;
        float[] texCoords;
        float[] colors;
        float[] normals;

        int vao;
        int positionBuf;
        int texCoordBuf;
        int colorBuf;
        int normalBuf;
        int texture;

        Shader shader;

        public void Init(Level level)
        {
            shader = new Shader("shaders/mario_shader.vert", "shaders/mario_shader.frag");

            var sm64StaticCollisionMeshBuilder = sm64Context.CreateStaticCollisionMesh();
            CollisionMap collisionMap = level.curArea.collision;

            if (collisionMap.triangles.Count == 0)
                return;

            foreach (var collisionTriangleList in collisionMap.triangles)
            {
                var surfaceType = (Sm64SurfaceType)collisionTriangleList.id;
                var vertices = collisionMap.vertices;
                var indices = collisionTriangleList.indicesList;
                for (var i = 0; i < indices.Count; i += 3)
                {
                    (int, int, int) vertex1 = ConvertVector_(vertices[(int)indices[i]]);
                    (int, int, int) vertex2 = ConvertVector_(vertices[(int)indices[i + 1]]);
                    (int, int, int) vertex3 = ConvertVector_(vertices[(int)indices[i + 2]]);
                    sm64StaticCollisionMeshBuilder.AddTriangle(
                    surfaceType,
                        (Sm64TerrainType)level.curArea.terraiType,
                        vertex1, vertex2, vertex3);
                }
            }
            sm64StaticCollisionMeshBuilder.Build();

            sm64Mario = sm64Context.CreateMario(level.marioPos.X, level.marioPos.Y, level.marioPos.Z);
            

            sm64Mario.Tick();

            positions = new float[sm64Mario.Mesh.TriangleData.TriangleCount * 3 * 3];
            texCoords = new float[sm64Mario.Mesh.TriangleData.TriangleCount * 3 * 2];
            colors = new float[sm64Mario.Mesh.TriangleData.TriangleCount * 3 * 3];
            normals = new float[sm64Mario.Mesh.TriangleData.TriangleCount * 3 * 3];

            ToArray();

            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            positionBuf = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionBuf);
            GL.BufferData(BufferTarget.ArrayBuffer, positions.Length * sizeof(float), positions, BufferUsageHint.DynamicDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(float) * 3, 0);

            texCoordBuf = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, texCoordBuf);
            GL.BufferData(BufferTarget.ArrayBuffer, texCoords.Length * sizeof(float), texCoords, BufferUsageHint.DynamicDraw);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, sizeof(float) * 2, 0);

            colorBuf = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, colorBuf);
            GL.BufferData(BufferTarget.ArrayBuffer, colors.Length * sizeof(float), colors, BufferUsageHint.DynamicDraw);
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, sizeof(float) * 3, 0);

            normalBuf = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, normalBuf);
            GL.BufferData(BufferTarget.ArrayBuffer, normals.Length * sizeof(float), normals, BufferUsageHint.DynamicDraw);
            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, sizeof(float) * 3, 0);

            // texture
            var image = sm64Mario.Mesh.Texture;
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var rgba = new byte[4 * imageWidth * imageHeight];
            var frame = image.Frames[0];
            for (var y = 0; y < imageHeight; y++)
            {
                for (var x = 0; x < imageWidth; x++)
                {
                    var pixel = frame[x, y];

                    var outI = 4 * (y * imageWidth + x);
                    rgba[outI] = pixel.R;
                    rgba[outI + 1] = pixel.G;
                    rgba[outI + 2] = pixel.B;
                    rgba[outI + 3] = pixel.A;
                }
            }

            texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, imageWidth, imageHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, rgba);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                  (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                            (int)TextureMagFilter.Linear);

            GL.TexParameter(TextureTarget.Texture2D,
                      TextureParameterName.TextureWrapS,
                      (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D,
                            TextureParameterName.TextureWrapT,
                            (int)TextureWrapMode.ClampToEdge);

        }

        public void Draw(Camera camera)
        {
            shader.Use();
            shader.SetMatrix4("model", Matrix4.Identity);
            shader.SetMatrix4("view", camera.GetViewMatrix());
            shader.SetMatrix4("projection", camera.GetProjectionMatrix());

            ToArray();

            GL.BindVertexArray(vao);
            GL.BindTexture(TextureTarget.Texture2D, texture);

            GL.BindBuffer(BufferTarget.ArrayBuffer, positionBuf);
            GL.BufferData(BufferTarget.ArrayBuffer, positions.Length * sizeof(float), positions, BufferUsageHint.DynamicDraw);

            GL.BindBuffer(BufferTarget.ArrayBuffer, texCoordBuf);
            GL.BufferData(BufferTarget.ArrayBuffer, texCoords.Length * sizeof(float), texCoords, BufferUsageHint.DynamicDraw);

            GL.BindBuffer(BufferTarget.ArrayBuffer, colorBuf);
            GL.BufferData(BufferTarget.ArrayBuffer, colors.Length * sizeof(float), colors, BufferUsageHint.DynamicDraw);

            GL.BindBuffer(BufferTarget.ArrayBuffer, normalBuf);
            GL.BufferData(BufferTarget.ArrayBuffer, normals.Length * sizeof(float), normals, BufferUsageHint.DynamicDraw);

            GL.DrawArrays(PrimitiveType.Triangles, 0, sm64Mario.Mesh.TriangleData.TriangleCount * 3);
        }

        private void ToArray()
        {
            for (var i = 0; i < sm64Mario.Mesh.TriangleData.TriangleCount; ++i)
            {
                for (var v = 0; v < 3; ++v)
                {
                    var offset = 3 * i + v;

                    var vertexPosition = sm64Mario.Mesh.TriangleData.Positions[offset];
                    positions[offset * 3 + 0] = vertexPosition.X;
                    positions[offset * 3 + 1] = vertexPosition.Y;
                    positions[offset * 3 + 2] = vertexPosition.Z;

                    var vertexUv = sm64Mario.Mesh.TriangleData.Uvs[offset];
                    texCoords[offset * 2 + 0] = vertexUv.X;
                    texCoords[offset * 2 + 1] = vertexUv.Y;

                    var vertexColor = sm64Mario.Mesh.TriangleData.Colors[offset];
                    colors[offset * 3 + 0] = vertexColor.X;
                    colors[offset * 3 + 1] = vertexColor.Y;
                    colors[offset * 3 + 2] = vertexColor.Z;

                    var vertexNormal = sm64Mario.Mesh.TriangleData.Normals[offset];
                    normals[offset * 3 + 0] = vertexNormal.X;
                    normals[offset * 3 + 1] = vertexNormal.Y;
                    normals[offset * 3 + 2] = vertexNormal.Z;
                }
            }
        }

        private static (int, int, int) ConvertVector_(Vector3 vector)
      => ((int)vector.X, (int)vector.Y, (int)vector.Z);
    }


}