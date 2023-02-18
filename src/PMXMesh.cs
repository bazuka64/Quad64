using MMDTools;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using Camera = Quad64.src.Camera;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Quad64.src
{
    public class SubMesh
    {
        public int VertexCount;
        public int offset;
        public string texturePath;
        public int texture;
    }

    internal class PMXMesh
    {
        PMXObject pmx;

        int vao;
        int posVBO;
        int norVBO;
        int uvVBO;
        int ibo;
        int vtxCount;
        int idxCount;

        float[] positions;
        float[] normals;
        float[] uvs;
        int[] indices;

        Shader shader;
        public OpenTK.Mathematics.Vector3 worldPos;
        float scale = 20;

        List<SubMesh> subMeshes = new List<SubMesh>();

        public PMXMesh(PMXObject pmx)
        {
            this.pmx = pmx;

            shader = new Shader("shaders/pmx_shader.vert", "shaders/pmx_shader.frag");

            int offset = 0;
            for (int i = 0; i < pmx.MaterialList.Length; i++)
            {
                SubMesh subMesh = new SubMesh();
                subMeshes.Add(subMesh);
                subMesh.VertexCount = pmx.MaterialList.Span[i].VertexCount;
                subMesh.offset = offset;
                offset += subMesh.VertexCount;

                int Texture = pmx.MaterialList.Span[i].Texture;
                subMesh.texturePath = pmx.TextureList.Span[Texture];

                Image<Rgba32> image = Image.Load<Rgba32>("Model/" + subMesh.texturePath);
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

                subMesh.texture = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, subMesh.texture);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, imageWidth, imageHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, rgba);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                      (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                                (int)TextureMagFilter.Linear);
            }

            vtxCount = pmx.VertexList.Length;
            idxCount = pmx.SurfaceList.Length * 3;

            positions = new float[vtxCount * 3];
            normals = new float[vtxCount * 3];
            uvs = new float[vtxCount * 2];
            indices = new int[idxCount];
            for (int i = 0; i < vtxCount; i++)
            {
                positions[i * 3 + 0] = pmx.VertexList.Span[i].Position.X;
                positions[i * 3 + 1] = pmx.VertexList.Span[i].Position.Y;
                positions[i * 3 + 2] = pmx.VertexList.Span[i].Position.Z;

                normals[i * 3 + 0] = pmx.VertexList.Span[i].Normal.X;
                normals[i * 3 + 1] = pmx.VertexList.Span[i].Normal.Y;
                normals[i * 3 + 2] = pmx.VertexList.Span[i].Normal.Z;

                uvs[i * 2 + 0] = pmx.VertexList.Span[i].UV.X;
                uvs[i * 2 + 1] = pmx.VertexList.Span[i].UV.Y;
            }
            for (int i = 0; i < pmx.SurfaceList.Length; i++)
            {
                indices[i * 3 + 0] = pmx.SurfaceList.Span[i].V1;
                indices[i * 3 + 1] = pmx.SurfaceList.Span[i].V2;
                indices[i * 3 + 2] = pmx.SurfaceList.Span[i].V3;
            }

            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            posVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, posVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vtxCount * 3 * sizeof(float), positions, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

            norVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, norVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vtxCount * 3 * sizeof(float), normals, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

            uvVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, uvVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vtxCount * 2 * sizeof(float), uvs, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);

            ibo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, idxCount * sizeof(int), indices, BufferUsageHint.StaticDraw);
        }

        public void Draw(Camera camera)
        {
            shader.Use();
            shader.SetMatrix4("view", camera.GetViewMatrix());
            shader.SetMatrix4("projection", camera.GetProjectionMatrix());

            Matrix4 mat = Matrix4.Identity;
            mat = Matrix4.CreateTranslation(worldPos) * mat;
            mat = Matrix4.CreateScale(scale) * mat;
            mat = Matrix4.CreateRotationY(MathHelper.Pi) * mat;
            shader.SetMatrix4("model", mat);

            GL.BindVertexArray(vao);
            subMeshes.ForEach(subMesh =>
            {
                GL.BindTexture(TextureTarget.Texture2D, subMesh.texture);
                GL.DrawElements(PrimitiveType.Triangles, subMesh.VertexCount, DrawElementsType.UnsignedInt, subMesh.offset * sizeof(int));
            });
        }
    }
}