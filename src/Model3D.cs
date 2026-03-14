using OpenTK.Mathematics;
using Quad64_new;
using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace Quad64.src
{
    public class Model3D
    {
        public GraphNode root;
        public List<Mesh> meshes;

        int animatedPartCount;
        float curScale;
        public bool isArea;

        public void draw(Matrix4 transform, Object3D obj)
        {
            if (root != null)
            {
                animatedPartCount = 0;
                curScale = 1;
                drawNode(root, transform, obj);
            }
            else if (meshes != null)
            {
                foreach (var mesh in meshes)
                {
                    mesh.draw(transform);
                }
            }
        }

        void drawNode(GraphNode node, Matrix4 transform, Object3D obj)
        {
            // scale
            if (node.scale != 1)
            {
                if (isArea)
                    transform = Matrix4.CreateScale(node.scale) * transform;
                curScale *= node.scale;
            }

            // billboard
            if (node.isBillboard)
            {
                //transform = Matrix4.CreateScale(1 / curScale) * transform;

                MainWindow.shader.SetFloat("scale", curScale);
            }

            // offset
            if (node.offX != 0 || node.offY != 0 || node.offZ != 0)
            {
                transform = Matrix4.CreateTranslation(node.offX, node.offY, node.offZ) * transform;
            }

            // rotation
            if (node.rotX != 0 || node.rotY != 0 || node.rotZ != 0)
            {
                Matrix4 rotateX = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(node.rotX));
                Matrix4 rotateY = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(node.rotY));
                Matrix4 rotateZ = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(node.rotZ));

                transform = rotateX * rotateY * rotateZ * transform;
            }

            // animated part
            if (node.isAnim && obj != null && obj.animsAddr != 0)
            {
                processAnim(obj, ref transform);
            }

            // display list
            if (node.meshes != null)
            {
                foreach (var mesh in node.meshes)
                {
                    mesh.draw(transform);
                }
            }

            // children
            if (node.children != null)
            {
                if (node.isSwitchCase && !isArea)
                {
                    if (obj != null)
                    {
                        // case coin
                        if (obj.modelID >= 0x74 && obj.modelID <= 0x77 || obj.modelID == 0xd7 || obj.modelID == 0xd8)
                        {
                            int selectedChild = FrameTimer.coinFrame % node.children.Count;
                            drawNode(node.children[selectedChild], transform, obj);
                        }
                        // case bowser
                        else if (obj.modelID == 0x64 && new List<int> { 0x1E, 0x21, 0x22 }.Contains(Level.instance.levelID))
                        {
                            // select second choice
                            drawNode(node.children[1], transform, obj);
                        }
                        // case Yoshi
                        //else if (obj.modelID == 0x55 && Level.instance.levelID == 0x10)
                        //{
                        //    // select second choice
                        //    drawNode(node.children[1], transform, obj);
                        //}
                        else
                        {
                            // change node per second
                            int selectedChild = FrameTimer.switchFrame % node.children.Count;
                            drawNode(node.children[selectedChild], transform, obj);
                        }
                    }
                    else
                    {
                        // change node per second
                        int selectedChild = FrameTimer.switchFrame % node.children.Count;
                        drawNode(node.children[selectedChild], transform, obj);
                    }
                }
                //else if (node.isRanderRange && !isArea)
                //{
                //    Vector3 objPos = new Vector3(obj.posX, obj.posY, obj.posZ);
                //    float distance = Vector3.Distance(MainWindow.camera.position, objPos);
                //    if (node.minDistance < distance && distance < node.maxDistance)
                //    {
                //        foreach (var child in node.children)
                //        {
                //            drawNode(child, transform, obj);
                //        }
                //    }
                //}
                else
                {
                    foreach (var child in node.children)
                    {
                        drawNode(child, transform, obj);
                    }
                }
            }
        }

        void processAnim(Object3D obj, ref Matrix4 transform)
        {

            BinaryStream bs = null;
            if (!Script.setSegmentPosition(ref bs, obj.animsAddr))
                return;

            int animCount = 0;
            uint animAddr = 0;
            while (true)
            {
                animAddr = bs.ReadUInt32();
                if (animAddr == 0 || animAddr >> 24 != obj.animsAddr >> 24) break;
                animCount++;
            }

            bs.Position = (obj.animsAddr & 0x00FFFFFF) + FrameTimer.changeAnimFrame % animCount * 4;

            animAddr = bs.ReadUInt32();

            Script.setSegmentPosition(ref bs, animAddr);

            bs.Position += 0x08;
            short maxFrame = bs.ReadInt16();
            short num = bs.ReadInt16();
            uint valueAddr = bs.ReadUInt32();
            uint indexAddr = bs.ReadUInt32();
            if (animatedPartCount > num - 1)
                return;

            Script.setSegmentPosition(ref bs, indexAddr);

            // root translation
            int transXFinalIndex = 0;
            int transYFinalIndex = 0;
            int transZFinalIndex = 0;
            ushort transXMax = 0;
            ushort transXIndex = 0;
            ushort transYMax = 0;
            ushort transYIndex = 0;
            ushort transZMax = 0;
            ushort transZIndex = 0;
            if (animatedPartCount == 0)
            {
                transXMax = bs.ReadUInt16();
                transXIndex = bs.ReadUInt16();
                transYMax = bs.ReadUInt16();
                transYIndex = bs.ReadUInt16();
                transZMax = bs.ReadUInt16();
                transZIndex = bs.ReadUInt16();

                transXFinalIndex = transXIndex + FrameTimer.animFrame % transXMax;
                transYFinalIndex = transYIndex + FrameTimer.animFrame % transYMax;
                transZFinalIndex = transZIndex + FrameTimer.animFrame % transZMax;
            }
            else
                bs.Position += 12;

            bs.Position += 12 * animatedPartCount;

            ushort rotXMax = bs.ReadUInt16();
            ushort rotXIndex = bs.ReadUInt16();
            ushort rotYMax = bs.ReadUInt16();
            ushort rotYIndex = bs.ReadUInt16();
            ushort rotZMax = bs.ReadUInt16();
            ushort rotZIndex = bs.ReadUInt16();

            int rotXFinalIndex = rotXIndex + FrameTimer.animFrame % rotXMax;
            int rotYFinalIndex = rotYIndex + FrameTimer.animFrame % rotYMax;
            int rotZFinalIndex = rotZIndex + FrameTimer.animFrame % rotZMax;

            Script.setSegmentPosition(ref bs, valueAddr);
            uint off = valueAddr & 0x00FFFFFF;

            Matrix4 transMat = Matrix4.Identity;
            if (animatedPartCount == 0)
            {
                bs.Position = off + transXFinalIndex * 2;
                short transX = bs.ReadInt16();
                bs.Position = off + transYFinalIndex * 2;
                short transY = bs.ReadInt16();
                bs.Position = off + transZFinalIndex * 2;
                short transZ = bs.ReadInt16();
                transMat = Matrix4.CreateTranslation(transX, transY, transZ);
            }

            bs.Position = off + rotXFinalIndex * 2;
            short rotX = bs.ReadInt16();
            bs.Position = off + rotYFinalIndex * 2;
            short rotY = bs.ReadInt16();
            bs.Position = off + rotZFinalIndex * 2;
            short rotZ = bs.ReadInt16();

            Matrix4 rotXMat = Matrix4.CreateRotationX(rotX / 32767f * (float)Math.PI);
            Matrix4 rotYMat = Matrix4.CreateRotationY(rotY / 32767f * (float)Math.PI);
            Matrix4 rotZMat = Matrix4.CreateRotationZ(rotZ / 32767f * (float)Math.PI);

            transform = rotXMat * rotYMat * rotZMat * transMat * transform;

            animatedPartCount++;
        }
    }
}