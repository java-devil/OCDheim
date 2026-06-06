using System;
using System.Collections.Generic;
using UnityEngine;

namespace OCDheim
{
    public readonly struct Box : ISide
    {
        private const double Epsilon = 1e-8;
        
        private char name { get; }
        private Vector3 minMinSnapNode { get; }
        private Vector3 minMaxSnapNode { get; }
        private Vector3 maxMinSnapNode { get; }
        private Vector3 maxMaxSnapNode { get; }
        private SnapNode.Type primarySnapNodeType { get; }

        public Box(char name, Vector3 minMinSnapNode, Vector3 minMaxSnapNode, Vector3 maxMinSnapNode, Vector3 maxMaxSnapNode)
        {
            this.name = name;
            this.minMinSnapNode = minMinSnapNode;
            this.minMaxSnapNode = minMaxSnapNode;
            this.maxMinSnapNode = maxMinSnapNode;
            this.maxMaxSnapNode = maxMaxSnapNode;
            primarySnapNodeType = SnapNode.Type.PRIMARY;
        }

        public Box(Piece piece, Vector2 shift, char name = 'A')
        {
            this.name = name;
            var topMidSnapNode = piece.TopMiddle();
            minMinSnapNode = topMidSnapNode + piece.transform.rotation * new Vector3(-shift.x, 0.0f, -shift.y);
            minMaxSnapNode = topMidSnapNode + piece.transform.rotation * new Vector3(-shift.x, 0.0f, +shift.y);
            maxMinSnapNode = topMidSnapNode + piece.transform.rotation * new Vector3(+shift.x, 0.0f, -shift.y);
            maxMaxSnapNode = topMidSnapNode + piece.transform.rotation * new Vector3(+shift.x, 0.0f, +shift.y);
            primarySnapNodeType = SnapNode.Type.IMPOSED;
        }

        public void FillUp(HashSet<SnapNode> snapNodes)
        {
            var dimensionX = minMaxSnapNode - minMinSnapNode;
            var dimensionY = maxMinSnapNode - minMinSnapNode;
            var (xΔ, splitsOfDimensionX) = RecursiveSplit(dimensionX);
            var (yΔ, splitsOfDimensionY) = RecursiveSplit(dimensionY);
            var snapNodesInDimensionA = Math.Pow(2, splitsOfDimensionX) + 1;
            var snapNodesInDimensionB = Math.Pow(2, splitsOfDimensionY) + 1;
            for (var i = 0; i < snapNodesInDimensionA; i++)
            {
                for (var j = 0; j < snapNodesInDimensionB; j++)
                {
                    var position = minMinSnapNode + xΔ * i + yΔ * j;
                    if ((i == 0 || Math.Abs(i - (snapNodesInDimensionA - 1)) < Epsilon) && (j == 0 || Math.Abs(j - (snapNodesInDimensionB - 1)) < Epsilon))
                    {
                        snapNodes.Add(new SnapNode(position, primarySnapNodeType, SnapNode.Precision.ORDINARY));
                    }
                    else if (i % SnapNode.SuperiorPrecisionMultiplier == 0 && j % SnapNode.SuperiorPrecisionMultiplier == 0)
                    {
                        snapNodes.Add(new SnapNode(position, SnapNode.Type.DERIVED, SnapNode.Precision.ORDINARY));
                    }
                    else
                    {
                        snapNodes.Add(new SnapNode(position, SnapNode.Type.DERIVED, SnapNode.Precision.SUPERIOR));
                    }
                }
            }
        }

        private (Vector3, int) RecursiveSplit(Vector3 dimensionΔ, int splitLevel = 0)
        {
            if (dimensionΔ.magnitude > SnapNode.OrdinaryPrecisionSplitThreshold / SnapNode.SuperiorPrecisionMultiplier)
            {
                return RecursiveSplit(dimensionΔ * 0.5f, ++splitLevel);
            }

            return (dimensionΔ, splitLevel);
        }

        public override string ToString() => $"Box {name}: ({minMinSnapNode}, {minMaxSnapNode}, {maxMinSnapNode}, {maxMaxSnapNode})";
    }
}
