using System.Collections.Generic;

namespace Quad64
{
    public class GraphNode
    {
        public GraphNode parent;
        public List<GraphNode> children;

        public List<Mesh> meshes;

        public short offX, offY, offZ;
        public short rotX, rotY, rotZ;
        public float scale = 1;

        public bool isSwitchCase;
        public bool isAnim;
        public bool isRanderRange;
        public bool isBillboard;

        public short minDistance;
        public short maxDistance;
    }
}