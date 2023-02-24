using MMDTools;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using Camera = Quad64.src.Camera;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Scallion.DomainModels;
using System.Numerics;
using System;
using System.Threading.Tasks.Dataflow;
using System.Runtime.InteropServices;
using System.Printing;
using NAudio.Gui;
using System.Linq;
using System.Windows;
using NAudio.Wave;

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
        int bonesVBO;
        int weightsVBO;
        int ibo;
        int vtxCount;
        int idxCount;
        int matrixBuffer;

        float[] positions;
        float[] normals;
        float[] uvs;
        int[] bones;
        float[] weights;
        int[] indices;

        Shader shader;
        public OpenTK.Mathematics.Vector3 worldPos;
        float scale = 25;

        List<SubMesh> subMeshes = new List<SubMesh>();

        public Motion motion;
        public Mp3Frame mp3;

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
            bones = new int[vtxCount * 4];
            weights = new float[vtxCount * 4];
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

                bones[i * 4 + 0] = pmx.VertexList.Span[i].BoneIndex1;
                bones[i * 4 + 1] = pmx.VertexList.Span[i].BoneIndex2;
                bones[i * 4 + 2] = pmx.VertexList.Span[i].BoneIndex3;
                bones[i * 4 + 3] = pmx.VertexList.Span[i].BoneIndex4;

                
                weights[i * 4 + 0] = pmx.VertexList.Span[i].Weight1;
                weights[i * 4 + 1] = pmx.VertexList.Span[i].Weight2;
                weights[i * 4 + 2] = pmx.VertexList.Span[i].Weight3;
                weights[i * 4 + 3] = pmx.VertexList.Span[i].Weight4;

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

            bonesVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, bonesVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vtxCount * 4 * sizeof(int), bones, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Int, false, 4 * sizeof(int), 0); 

            weightsVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, weightsVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vtxCount * 4 * sizeof(float), weights, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(4);
            GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

            ibo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, idxCount * sizeof(int), indices, BufferUsageHint.StaticDraw);

            matrixBuffer = GL.GenBuffer();
            int blockIndex = GL.GetUniformBlockIndex(shader.program, "Matrices");
            //GL.GetActiveUniformBlock(shader.program, blockIndex, ActiveUniformBlockParameter.UniformBlockDataSize, out int blockSize);
            GL.UniformBlockBinding(shader.program, blockIndex, 0);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, matrixBuffer);
        }

        public void Draw(Camera camera)
        {
            shader.Use();

            // camera
            shader.SetMatrix4("view", camera.GetViewMatrix());
            shader.SetMatrix4("projection", camera.GetProjectionMatrix());
            
            // world
            Matrix4 mat = Matrix4.Identity;
            mat = Matrix4.CreateTranslation(worldPos) * mat;
            mat = Matrix4.CreateScale(scale) * mat;
            mat = Matrix4.CreateRotationY(MathHelper.Pi) * mat;
            shader.SetMatrix4("model", mat);

            // motion
            Matrix4[] boneMatrix = new Matrix4[400];
            Dictionary<string, Matrix4> dict = new Dictionary<string, Matrix4>();
            for(int i = 0; i < motion.Bones.Count; i++)
            {
                var index = motion.Bones[i].KeyFrames.FindIndex(frame => 
                {
                    return frame.KeyFrameIndex > FrameTimer.vmdFrame;
                });
                Scallion.DomainModels.Components.BoneKeyFrame keyFrame0;
                Scallion.DomainModels.Components.BoneKeyFrame keyFrame1;
                if (index == -1)
                {
                    keyFrame0 = motion.Bones[i].KeyFrames.Last();
                    keyFrame1 = keyFrame0;
                }
                else
                {
                    keyFrame0 = motion.Bones[i].KeyFrames[index - 1];
                    keyFrame1 = motion.Bones[i].KeyFrames[index];

                }

                OpenTK.Mathematics.Vector3 position0 = new OpenTK.Mathematics.Vector3(
                    keyFrame0.Value.Position.X,
                    keyFrame0.Value.Position.Y,
                    keyFrame0.Value.Position.Z);
                OpenTK.Mathematics.Quaternion quaternion0 = new OpenTK.Mathematics.Quaternion(
                    keyFrame0.Value.Quaternion.X,
                    keyFrame0.Value.Quaternion.Y,
                    keyFrame0.Value.Quaternion.Z,
                    keyFrame0.Value.Quaternion.W);

                OpenTK.Mathematics.Vector3 position1 = new OpenTK.Mathematics.Vector3(
                    keyFrame1.Value.Position.X,
                    keyFrame1.Value.Position.Y,
                    keyFrame1.Value.Position.Z);
                OpenTK.Mathematics.Quaternion quaternion1 = new OpenTK.Mathematics.Quaternion(
                    keyFrame1.Value.Quaternion.X,
                    keyFrame1.Value.Quaternion.Y,
                    keyFrame1.Value.Quaternion.Z,
                    keyFrame1.Value.Quaternion.W);

                float a = (FrameTimer.vmdFrame - keyFrame0.KeyFrameIndex) / 
                    (float)(keyFrame1.KeyFrameIndex - keyFrame0.KeyFrameIndex);
                if (index == -1) a = 0;
                //Console.WriteLine(a);
                OpenTK.Mathematics.Vector3 position = OpenTK.Mathematics.Vector3.Lerp(position0, position1, a);
                OpenTK.Mathematics.Quaternion quaternion = OpenTK.Mathematics.Quaternion.Slerp(quaternion0, quaternion1, a);

                Matrix4 matrix = Matrix4.CreateFromQuaternion(quaternion) * Matrix4.CreateTranslation(position);
                dict.Add(motion.Bones[i].Name, matrix);
            }

            
            // boneMatrix
            for (int i = 0; i < pmx.BoneList.Length; i++)
            {
                Matrix4 matrix = Matrix4.Identity;
                Bone bone = pmx.BoneList.Span[i];


                while (true)
                {
                    if (dict.ContainsKey(bone.Name))
                    {
                        MMDTools.Vector3 pos = bone.Position;
                        var pos0 = new OpenTK.Mathematics.Vector3(pos.X, pos.Y, pos.Z) * -1;

                        // move to origin
                        matrix = matrix * Matrix4.CreateTranslation(pos0);

                        // motion
                        matrix = matrix * dict[bone.Name];

                        // back to default pos
                        matrix = matrix * Matrix4.CreateTranslation(pos0 * -1);
                    }

                    if (bone.ParentBone == -1)
                        break;
                    Bone parentBone = pmx.BoneList.Span[bone.ParentBone];

                    // parent bone
                    bone = parentBone;
                }

                boneMatrix[i] = matrix;
            }
            

            // int size = Marshal.SizeOf(typeof(Matrix4));
            GL.BindBuffer(BufferTarget.UniformBuffer, matrixBuffer);
            GL.BufferData(BufferTarget.UniformBuffer, sizeof(float) * 16 * boneMatrix.Length, boneMatrix, BufferUsageHint.DynamicDraw);

            // draw
            GL.BindVertexArray(vao);
            subMeshes.ForEach(subMesh =>
            {
                GL.BindTexture(TextureTarget.Texture2D, subMesh.texture);
                GL.DrawElements(PrimitiveType.Triangles, subMesh.VertexCount, DrawElementsType.UnsignedInt, subMesh.offset * sizeof(int));
            });
        }
    }
}