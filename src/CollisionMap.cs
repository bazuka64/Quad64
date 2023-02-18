using OpenTK.Mathematics;
using System.Collections.Generic;

namespace Quad64.src
{
    public class CollisionTriangleList
    {
        public int id = 0;
        public List<uint> indicesList;

        public CollisionTriangleList(int ID)
        {
            id = ID;
            indicesList = new List<uint>();
        }

        public void AddTriangle(uint a, uint b, uint c)
        {
            indicesList.Add(a);
            indicesList.Add(b);
            indicesList.Add(c);
        }
    }

    public class CollisionMap
    {
        public List<Vector3> vertices = new List<Vector3>();

        public List<CollisionTriangleList> triangles =
        new List<CollisionTriangleList>();

        public void AddVertex(Vector3 newVert)
        {
            vertices.Add(newVert);
        }

        public void AddTriangle(uint a, uint b, uint c)
        {
            if (triangles.Count > 0)
                triangles[triangles.Count - 1].AddTriangle(a, b, c);
        }

        public void NewTriangleList(int ID)
        {
            triangles.Add(new CollisionTriangleList(ID));
        }
    }
}