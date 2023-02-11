using Melanchall.DryWetMidi.MusicTheory;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Quad64.src
{
    internal class Area
    {
        byte areaID;
        public Level level;
        public byte seqID;

        public Model3D model = new Model3D();

        public List<Object3D> Objects = new List<Object3D>();
        public List<Object3D> MacroObjects = new List<Object3D>();
        public List<Object3D> SpecialObjects = new List<Object3D>();
        public List<Object3D> AllObjects
        {
            get
            {
                List<Object3D> list = new List<Object3D>();
                list.AddRange(Objects);
                list.AddRange(MacroObjects);
                list.AddRange(SpecialObjects);
                return list;
            }
        }
        public Object3D selectedObject;

        public List<WaterBox> boxes = new List<WaterBox>();

        public int meshCount;

        public Area(byte areaID)
        {
            this.areaID = areaID;
            model.isArea = true;
        }

        public void draw()
        {
            Level.instance.areas[Level.instance.curAreaID].meshCount = 0;

            model.draw(Matrix4.Identity, null);

            if ((bool)MainWindow.instance.objectFlag.IsChecked)
            {
                Objects.ForEach(obj => drawObject(obj));
                MacroObjects.ForEach(obj => drawObject(obj));
                SpecialObjects.ForEach(obj => drawObject(obj));
            }

            if ((bool)MainWindow.instance.waterBoxFlag.IsChecked)
                boxes.ForEach(box => box.mesh.draw(Matrix4.Identity));

            Console.WriteLine("Mesh Count: " + meshCount);
        }

        void drawObject(Object3D obj)
        {

            Matrix4 transform;

            Matrix4 translate = Matrix4.CreateTranslation(obj.posX, obj.posY, obj.posZ);
            Matrix4 rotateX = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(obj.rotX));
            Matrix4 rotateY = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(obj.rotY));
            Matrix4 rotateZ = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(obj.rotZ));

            // 左から頂点にかかる
            transform = rotateX * rotateY * rotateZ * translate;

            if (level.models.ContainsKey(obj.modelID))
            {
                level.models[obj.modelID].draw(transform, obj);
            }

            // draw bounding box
            if (obj == selectedObject)
            {
                //BoundingBox.draw(Vector3.One, new Quaternion(obj.rotX, obj.rotY, obj.rotZ, 1.0f), new Vector3(obj.posX, obj.posY, obj.posZ),
                //        Color.Blue,
                //        Vector3.Zero, new Vector3(1000, 1000, 1000));

            }
        }
    }
}