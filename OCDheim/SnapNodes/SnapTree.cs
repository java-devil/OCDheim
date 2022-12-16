using System;
using System.Collections.Generic;
using UnityEngine;

namespace OCDheim
{
    public class SnapTree
    {
        private const int SidesOfLine = 1;
        private const int SidesOfBox = 1;
        private const int SidesOfCube = 6;
        private const int SidesOfOctagon = 10;
        private const float SnapThreshold = 1.0f;

        private static readonly List<Vector3> horribleOptimization = new List<Vector3>(1);
        private static readonly Dictionary<Piece, SnapTree> snapWood = new Dictionary<Piece, SnapTree>(1000);

        public Piece piece { get; private set; }
        private HashSet<SnapNode> snapNodes { get; } = new HashSet<SnapNode>();

        private static bool FormValidTwoDimensionalBoxSide(Vector3 snapNodeA, Vector3 snapNodeB, Vector3 snapNodeC, Vector3 snapNodeD) => snapNodeA != snapNodeB && snapNodeA != snapNodeC && snapNodeB + snapNodeC - snapNodeA == snapNodeD;
        private static Func<Vector3, Vector3, Vector3, Vector3, bool> FormValidThreeDimensionalBoxSide(Piece piece)
        {
            var pieceMid = piece.transform.position;;
            return (snapNodeA, snapNodeB, snapNodeC, snapNodeD) => {
                var sideMid = (snapNodeA + snapNodeD) * 0.5f;
                return FormValidTwoDimensionalBoxSide(snapNodeA, snapNodeB, snapNodeC, snapNodeD) && pieceMid != sideMid;
            };
        }
        private static bool LiesOnVerticalMiddle(Vector3 snapNode, Piece piece) => snapNode.x == piece.transform.position.y;
        private static bool LiesOnHorizontalMiddle(Vector3 snapNode, Piece piece) => snapNode.x == piece.transform.position.x && snapNode.z == piece.transform.position.z;
        private static bool LiesOnSameVerticalSide(Vector3 snapNodeA, Vector3 snapNodeB) => snapNodeA.x == snapNodeB.x && snapNodeA.y != snapNodeB.y && snapNodeA.z == snapNodeB.z;
        private static bool LiesOnSameHorizontalSide(Vector3 snapNodeA, Vector3 snapNodeB) => snapNodeA.y != snapNodeB.y;

        private SnapTree(Piece piece) {
            //Debug.Log($"[INIT] SNAP TREE CONSTRUCTION of piece: '{piece.m_name}' {piece.transform.position}");
            this.piece = piece;
            var sides = FindSidesOf(piece);
            foreach (var side in sides)
            {
                side.FillUp(snapNodes);
            }

            //Debug.Log($"SIDES: {string.Join(", ", sides)}");
            //Debug.Log($"SNAP NODES: {string.Join(", ", snapNodes)}");
            //Debug.Log($"[SUCCESS] SNAP TREE CONSTRUCTION of piece: '{piece.m_name}' {piece.transform.position}");
        }

        private static SnapTree Of(Piece piece)
        {
            if (snapWood.ContainsKey(piece))
            {
                //Debug.Log($"[CACHED HIT] on piece: '{piece.m_name}' {piece.transform.position}");
                return snapWood[piece];
            }
            else
            {
                //Debug.Log($"[CACHED MISS] on piece: '{piece.m_name}' {piece.transform.position}");
                var snapTree = new SnapTree(piece);
                snapWood[piece] = snapTree;
                return snapTree;
            }
        }

        private static Func<SnapNode.Precision, bool> ordinaryPrecisionOnly = precision => precision == SnapNode.Precision.ORDINARY;
        private static Func<SnapNode.Precision, bool> ordinaryAndSuperiorPrecision = precision => precision == SnapNode.Precision.ORDINARY || precision == SnapNode.Precision.SUPERIOR;
        public static TraversalResult? FindNearestOrdinaryPrecisionSnapNodeTo(Vector3 referencePosition, List<Piece> pieces) => FindNearestSnapNodeTo(referencePosition, pieces, ordinaryPrecisionOnly);
        public static TraversalResult? FindNearestSuperiorPrecisionSnapNodeTo(Vector3 referencePosition, List<Piece> pieces) => FindNearestSnapNodeTo(referencePosition, pieces, ordinaryAndSuperiorPrecision);
        public static TraversalResult? FindNearestOrdinaryPrecisionSnapNodeCombinationOf(Piece buildPiece, List<Piece> neighbourPieces) => FindNearestSnapNodeTo(buildPiece.PrimarySnapNodesDefensiveCopy(), neighbourPieces, ordinaryPrecisionOnly);
        public static TraversalResult? FindNearestSuperiorPrecisionSnapNodeCombinationOf(Piece buildPiece, List<Piece> neighbourPieces) => FindNearestSnapNodeTo(buildPiece.PrimarySnapNodesDefensiveCopy(), neighbourPieces, ordinaryAndSuperiorPrecision);

        private static List<ISide> FindSidesOf(Piece piece)
        {
            if (piece.IsConstructionPiece())
            {
                if (piece.IsLine())
                {
                    return FindSideOfLine(piece);
                }
                else if (piece.IsBox())
                {
                    return FindSideOfBox(piece);
                }
                else if (piece.IsCube())
                {
                    return FindSidesOfCube(piece);
                }
                else if (piece.IsCylinder())
                {
                    return FindSidesOfCylinder(piece); // perfectly sufficient approximation of octagons
                }
                else
                {
                    return FindSidesOfUndefined(piece); // imperfectly sufficient approximation of triangles
                }
            }
            else if (piece.IsSnappableFurniturePiece())
            {
                return new List<ISide>() { PieceHelpers.SnappableFurniturePieces[piece.m_name].Invoke(piece) };
            }
            else
            {
                return new List<ISide>();
            }
        }

        private static List<ISide> FindSideOfLine(Piece piece)
        {
            var sides = FindSidesOfUndefined(piece);
            if (sides.Count != SidesOfLine)
            {
                Debug.LogWarning($"EXPECTED SIDES on Piece '{piece.m_name}' {piece.transform.position}: {SidesOfLine}, ACTUAL SIDES: {sides.Count}");
            }

            return sides;
        }

        private static List<ISide> FindSideOfBox(Piece piece) => FindSidesOfBoxOrCube(piece, FormValidTwoDimensionalBoxSide, SidesOfBox);

        private static List<ISide> FindSidesOfCube(Piece piece) => FindSidesOfBoxOrCube(piece, FormValidThreeDimensionalBoxSide(piece), SidesOfCube);

        private static List<ISide> FindSidesOfBoxOrCube(Piece piece, Func<Vector3, Vector3, Vector3, Vector3, bool> isValidSide, int numberOfSides)
        {
            var boxName = 'A';
            var snapNodes = piece.PrimarySnapNodes();
            var sides = new List<ISide>(numberOfSides);
            for (var i = 0; i < snapNodes.Count; i++)
            {
                for (var j = i + 1; j < snapNodes.Count; j++)
                {
                    for (var k = j + 1; k < snapNodes.Count; k++)
                    {
                        for (var l = k + 1; l < snapNodes.Count; l++)
                        {
                            if (isValidSide.Invoke(snapNodes[i], snapNodes[j], snapNodes[k], snapNodes[l]))
                            {
                                sides.Add(new Box(boxName++, snapNodes[i], snapNodes[j], snapNodes[k], snapNodes[l]));
                            }
                        }
                    }
                }
            }

            if (sides.Count != numberOfSides)
            {
                Debug.LogWarning($"EXPECTED SIDES on Piece '{piece.m_name}' {piece.transform.position}: {numberOfSides}, ACTUAL SIDES: {sides.Count}");
            }

            return sides;
        }

        private static List<ISide> FindSidesOfCylinder(Piece piece)
        {
            var lineName = 'A';
            var sides = new List<ISide>(SidesOfOctagon);
            for (var i = 0; i < piece.PrimarySnapNodes().Count; i++)
            {
                var snapNodeA = piece.PrimarySnapNodes()[i];
                if (!LiesOnHorizontalMiddle(snapNodeA, piece))
                {
                    for (var j = i + 1; j < piece.PrimarySnapNodes().Count; j++)
                    {
                        var snapNodeB = piece.PrimarySnapNodes()[j];
                        if (!LiesOnHorizontalMiddle(snapNodeB, piece))
                        {
                            var snapMid = (snapNodeA + snapNodeB) * 0.5f;
                            if (LiesOnSameVerticalSide(snapNodeA, snapNodeB) && !LiesOnHorizontalMiddle(snapMid, piece))
                            {
                                sides.Add(new Line(lineName++, snapNodeA, snapNodeB)); // Ultimately should be modeled as a sequence of boxes
                            }
                            else if (LiesOnSameHorizontalSide(snapNodeA, snapNodeB) && LiesOnHorizontalMiddle(snapMid, piece))
                            {
                                sides.Add(new Line(lineName++, snapNodeA, snapNodeB)); // Ultimately should be modeled as a pair of "circles"
                            }
                        }
                    }
                }
            }

            if (sides.Count != SidesOfOctagon)
            {
                Debug.LogWarning($"EXPECTED SIDES on Piece '{piece.m_name}' {piece.transform.position}: {SidesOfOctagon}, ACTUAL SIDES: {sides.Count}");
            }

            return sides;
        }

        private static List<ISide> FindSidesOfUndefined(Piece piece)
        {
            var lineName = 'A';
            var sides = new List<ISide>();
            for (var i = 0; i < piece.PrimarySnapNodes().Count; i++)
            {
                for (var j = i + 1; j < piece.PrimarySnapNodes().Count; j++)
                {
                    sides.Add(new Line(lineName++, piece.PrimarySnapNodes()[i], piece.PrimarySnapNodes()[j])); // Ultimate fallback. Suprisingly satisfying approxmiation of a vast majority of arbitrary pieces.
                }
            }

            return sides;
        }


        private static TraversalResult? FindNearestSnapNodeTo(Vector3 referencePosition, List<Piece> pieces, Func<SnapNode.Precision, bool> isDesired)
        {
            // This would simply be an instantiation in a less GC deprived application.
            horribleOptimization.Clear();
            horribleOptimization.Add(referencePosition);
            return FindNearestSnapNodeTo(horribleOptimization, pieces, isDesired);
        }

        private static TraversalResult? FindNearestSnapNodeTo(List<Vector3> buildPieceSnapNodes, List<Piece> neighborPieces, Func<SnapNode.Precision, bool> isDesired)
        {
            var minDistance = float.PositiveInfinity;
            TraversalResult? result = null;
            foreach (var buildPieceSnapNode in buildPieceSnapNodes)
            {
                foreach (var neighbourPiece in neighborPieces)
                {
                    var snapTree = SnapTree.Of(neighbourPiece);
                    foreach (var neighbourSnapNode in snapTree.snapNodes)
                    {
                        var distance = Vector3.Distance(buildPieceSnapNode, neighbourSnapNode);
                        if (isDesired(neighbourSnapNode.precision) && minDistance > distance && distance < SnapThreshold)
                        {
                            minDistance = distance;
                            result = new TraversalResult(buildPieceSnapNode, neighbourSnapNode, neighbourPiece);
                        }
                    }
                }
            }

            return result;
        }

        public struct TraversalResult
        {
            public Vector3 buildPieceSnapNode { get; }
            public SnapNode neighbourSnapNode { get; }
            public Piece neighbourPiece { get; }

            public TraversalResult(Vector3 buildPieceSnapNode, SnapNode neighbourSnapNode, Piece neighbourPiece)
            {
                this.buildPieceSnapNode = buildPieceSnapNode;
                this.neighbourSnapNode = neighbourSnapNode;
                this.neighbourPiece = neighbourPiece;
            }
        }
    }
}
