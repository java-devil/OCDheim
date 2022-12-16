using System.Collections.Generic;
using UnityEngine;

namespace OCDheim
{
    public class Circle : ISide
    {
        private const int CircleDivisions = 8;

        public char name { get; private set; }
        public Vector3 midSnapNode { get; private set; }
        public Vector2 radial { get; private set; }

        public Circle(Piece piece, Vector2 radial, char name = 'A')
        {
            this.name = name;
            this.radial = radial;
            midSnapNode = piece.TopMiddlee();
        }

        public void FillUp(HashSet<SnapNode> snapNodes)
        {
            for (var i = 0; i < CircleDivisions; i++)
            {
                var shift = Quaternion.Euler(new Vector3(0.0f, 360.0f / CircleDivisions * i, 0.0f)) * radial;
                var shiftedSnapNode = midSnapNode + shift;

                snapNodes.Add(new SnapNode(midSnapNode, SnapNode.Type.IMPOSED, SnapNode.Precision.ORDINARY));
                snapNodes.Add(new SnapNode(shiftedSnapNode, SnapNode.Type.IMPOSED, SnapNode.Precision.ORDINARY));
                SnapNode.RecursiveSplit(midSnapNode, shiftedSnapNode, snapNodes);
            }
        }

        public override string ToString() => $"Circle {name}: {midSnapNode} + {radial} * quaternion rotation";
    }
}
