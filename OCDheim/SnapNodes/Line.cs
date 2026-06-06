using System.Collections.Generic;
using UnityEngine;

namespace OCDheim
{
    public readonly struct Line : ISide
    {
        private char name { get; }
        private Vector3 minSnapNode { get; }
        private Vector3 maxSnapNode { get; }

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
