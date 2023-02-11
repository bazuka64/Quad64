namespace Quad64
{
    internal class WaterBox
    {
        public short x1, z1;
        public short x2, z2;
        public short y;

        public Mesh mesh;

        public void build()
        {
            mesh = new Mesh();
            mesh.isWaterBox = true;

            mesh.useTexture = true;
            mesh.texture = ROM.instance.waterTexture;
            mesh.layer = 1;

            mesh.useLight = false;

            mesh.positions = new short[4 * 3];

            mesh.positions[0] = x1;
            mesh.positions[1] = y;
            mesh.positions[2] = z1;

            mesh.positions[3] = x2;
            mesh.positions[4] = y;
            mesh.positions[5] = z2;

            mesh.positions[6] = x2;
            mesh.positions[7] = y;
            mesh.positions[8] = z1;

            mesh.positions[9] = x1;
            mesh.positions[10] = y;
            mesh.positions[11] = z2;


            mesh.texCoords = new float[4 * 2];

            mesh.texCoords[0] = 0;
            mesh.texCoords[1] = 10;

            mesh.texCoords[2] = 10;
            mesh.texCoords[3] = 0;

            mesh.texCoords[4] = 10;
            mesh.texCoords[5] = 10;

            mesh.texCoords[6] = 0;
            mesh.texCoords[7] = 0;


            mesh.colorNormals = new byte[4 * 4];

            for (int i = 0; i < 16; i++)
            {
                if (i % 4 == 3)
                    mesh.colorNormals[i] = 0xBF;
                else
                    mesh.colorNormals[i] = 0xFF;
            }


            mesh.indices.Add(0);
            mesh.indices.Add(1);
            mesh.indices.Add(2);
            mesh.indices.Add(0);
            mesh.indices.Add(3);
            mesh.indices.Add(1);

            mesh.build();
        }
    }
}