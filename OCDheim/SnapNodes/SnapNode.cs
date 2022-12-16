using System.Collections.Generic;
using UnityEngine;

namespace OCDheim
{
    public struct SnapNode
    {
        public const float OrdinaryPrecisionSplitThreshold = 1.5f;
        public const float SuperiorPrecisionMultiplier = 2.0f;

        public Vector3 position { get; private set; }
        public Type type { get; private set; }
        public Precision precision { get; private set; }

        private static bool ShouldSplitInOrdinaryPrecisionMode(Vector3 prevSnapNode, Vector3 nextSnapNode) => Vector3.Distance(prevSnapNode, nextSnapNode) > OrdinaryPrecisionSplitThreshold;
        private static bool ShouldSplitInSuperiorPrecisionMode(Vector3 prevSnapNode, Vector3 nextSnapNode) => Vector3.Distance(prevSnapNode, nextSnapNode) > OrdinaryPrecisionSplitThreshold / SuperiorPrecisionMultiplier;

        public SnapNode(Vector3 position, Type type, Precision precision)
        {
            this.position = position;
            this.type = type;
            this.precision = precision;
        }

        public enum Type
        {
            PRIMARY,
            IMPOSED,
            DERIVED
        }

        public enum Precision
        {
            ORDINARY,
            SUPERIOR
        }

        public static void RecursiveSplit(Vector3 snapNodeA, Vector3 snapNodeB, HashSet<SnapNode> snapNodes)
        {
            snapNodes.Add(new SnapNode(snapNodeA, Type.PRIMARY, Precision.ORDINARY));
            snapNodes.Add(new SnapNode(snapNodeB, Type.PRIMARY, Precision.ORDINARY));
            RecursiveSplitInOrdinaryPrecisionMode(snapNodeA, snapNodeB, snapNodes);
        }

        private static void RecursiveSplitInOrdinaryPrecisionMode(Vector3 snapNodeA, Vector3 snapNodeB, HashSet<SnapNode> snapNodes, int splitLevel = 0)
        {
            if (ShouldSplitInOrdinaryPrecisionMode(snapNodeA, snapNodeB))
            {
                var midSnapNode = (snapNodeA + snapNodeB) * 0.5f;
                snapNodes.Add(new SnapNode(midSnapNode, Type.DERIVED, Precision.ORDINARY));
                //Debug.Log($"[SPLIT LEVEL {splitLevel++}] Ordinary Precision Child of: {snapNodeA} & {snapNodeB}, is: {midSnapNode}");

                RecursiveSplitInOrdinaryPrecisionMode(snapNodeA, midSnapNode, snapNodes, splitLevel);
                RecursiveSplitInOrdinaryPrecisionMode(midSnapNode, snapNodeB, snapNodes, splitLevel);
            }
            else
            {
                RecursiveSplitInSuperiorPrecisionMode(snapNodeA, snapNodeB, snapNodes, splitLevel);
            }
        }

        private static void RecursiveSplitInSuperiorPrecisionMode(Vector3 snapNodeA, Vector3 snapNodeB, HashSet<SnapNode> snapNodes, int splitLevel)
        {
            if (ShouldSplitInSuperiorPrecisionMode(snapNodeA, snapNodeB))
            {
                var midSnapNode = (snapNodeA + snapNodeB) * 0.5f;
                snapNodes.Add(new SnapNode(midSnapNode, Type.DERIVED, Precision.SUPERIOR));
                //Debug.Log($"[SPLIT LEVEL {splitLevel++}] Superior Precision Child of: {snapNodeA} & {snapNodeB}, is: {midSnapNode}");

                RecursiveSplitInSuperiorPrecisionMode(snapNodeA, midSnapNode, snapNodes, splitLevel);
                RecursiveSplitInSuperiorPrecisionMode(midSnapNode, snapNodeB, snapNodes, splitLevel);
            }
        }

        public static implicit operator Vector3(SnapNode snapNode) => snapNode.position;
        public override string ToString() => $"{type} Snap Node of {precision} precision: {position}";
        public override bool Equals(object other) => other is SnapNode snapNode && snapNode.position == position;
        public override int GetHashCode() => position.GetHashCode();
    }
}
