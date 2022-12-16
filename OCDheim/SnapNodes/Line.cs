using System.Collections.Generic;
using UnityEngine;

namespace OCDheim
{
    public struct Line : ISide
    {
        public char name { get; private set; }
        public Vector3 minSnapNode { get; private set; }
        public Vector3 maxSnapNode { get; private set; }
        public void FillUp(HashSet<SnapNode> snapNodes) => SnapNode.RecursiveSplit(minSnapNode, maxSnapNode, snapNodes);

        public Line(char name, Vector3 minSnapNode, Vector3 maxSnapNode)
        {
            this.name = name;
            this.minSnapNode = minSnapNode;
            this.maxSnapNode = maxSnapNode;
        }

        public override string ToString() => $"Line {name}: ({minSnapNode}, {maxSnapNode})";
    }
}
