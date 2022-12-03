using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quad64
{
    internal class GraphNode
    {
        public GraphNode parent = null;
        public List<GraphNode> children = new List<GraphNode>();

        public bool insideSwitch = false;
        public bool alreadyDone = false;

        public uint scale;
    }
}
