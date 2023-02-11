using OpenTK.Mathematics;
using System.Collections.Generic;

namespace Quad64.src
{
    internal class Level
    {
        public static Level instance;
        public ushort levelID;
        public byte curAreaID = 1;
        public bool hasArea;
        public Area[] areas = new Area[8];
        public Area curArea { get => areas[curAreaID]; }

        public Dictionary<byte, Model3D> models = new Dictionary<byte, Model3D>();
        public Dictionary<uint, Texture> textures = new Dictionary<uint, Texture>();

        public bool[] layers = new bool[7];

        public Level(ushort levelID)
        {
            instance = this;
            this.levelID = levelID;
        }

        public void drawModels()
        {
            int posX = 0;
            foreach (var model in models.Values)
            {
                Matrix4 transform;

                transform = Matrix4.CreateTranslation(posX, 0, 0);

                model.draw(transform, null);

                posX += 100;
            }
        }
    }
}