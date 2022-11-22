namespace Quad64
{
    internal class Area
    {
        public int areaID;
        public int seqID;
        public Model3D model = new Model3D();

        public List<Object3D> Objects = new List<Object3D>();
        public List<Object3D> MacroObjects = new List<Object3D>();
        public List<Object3D> SpecialObjects = new List<Object3D>();

        public Area()
        {
        }
    }
}