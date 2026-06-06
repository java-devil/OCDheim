using System;
using System.Collections.Generic;
using UnityEngine;
using static OCDheim.PieceShape;
using static OCDheim.PieceType;

namespace OCDheim
{
    public readonly struct SnapTree
    {
        private const int ZeroSidesAllowed = 0;
        private const int OneSideAllowed = 1;
        private const int SidesOfCube = 6;
        private const int SidesOfOctagon = 10;
        private const float SnapThreshold = 1.0f;

        private static readonly List<Box> Boxes = new List<Box>(SidesOfCube);
        private static readonly List<Line> Lines = new List<Line>(SidesOfOctagon);
        private static readonly List<ISide> Sides = new List<ISide>(OneSideAllowed);
        private static readonly List<Vector3> HorribleOptimization = new List<Vector3>(OneSideAllowed);
        private static readonly HashSet<SnapNode> ZeroSnapNodes = new HashSet<SnapNode>(ZeroSidesAllowed);
        private static readonly IMemoryRepo<Piece, SnapTree> SnapTrees = new MemoryRepo<Piece, SnapTree>(piece => new SnapTree(piece), 1024);

        private HashSet<SnapNode> snapNodes { get; }

        private static bool FormValidTwoDimensionalBoxSide(Vector3 snapNodeA, Vector3 snapNodeB, Vector3 snapNodeC, Vector3 snapNodeD) => snapNodeA != snapNodeB && snapNodeA != snapNodeC && snapNodeB + snapNodeC - snapNodeA == snapNodeD;
        private static Func<Vector3, Vector3, Vector3, Vector3, bool> FormValidThreeDimensionalBoxSide(Piece piece)
        {
            var pieceMid = piece.transform.position;
            return (snapNodeA, snapNodeB, snapNodeC, snapNodeD) => {
                var sideMid = (snapNodeA + snapNodeD) * 0.5f;
                return FormValidTwoDimensionalBoxSide(snapNodeA, snapNodeB, snapNodeC, snapNodeD) && pieceMid != sideMid;
            };
        }
        private static bool LiesOnVerticalMiddle(Vector3 snapNode, Piece piece) => Mathf.Approximately(snapNode.x, piece.transform.position.y);
        private static bool LiesOnHorizontalMiddle(Vector3 snapNode, Piece piece) => Mathf.Approximately(snapNode.x, piece.transform.position.x) && Mathf.Approximately(snapNode.z, piece.transform.position.z);
        private static bool LiesOnSameVerticalSide(Vector3 snapNodeA, Vector3 snapNodeB) => Mathf.Approximately(snapNodeA.x, snapNodeB.x) && !Mathf.Approximately(snapNodeA.y, snapNodeB.y) && Mathf.Approximately(snapNodeA.z, snapNodeB.z);
        private static bool LiesOnSameHorizontalSide(Vector3 snapNodeA, Vector3 snapNodeB) => !Mathf.Approximately(snapNodeA.y, snapNodeB.y);

        private SnapTree(Piece piece) {
            Logger.Debug(() => $"[INIT] SNAP TREE CONSTRUCTION of piece: '{piece.m_name}' {piece.transform.position}");
            snapNodes = DeriveSnapNodesFrom(piece);

            var tree = this;
            Logger.Debug(() => $"SNAP NODES: {string.Join(", ", tree.snapNodes)}");
            Logger.Debug(() => $"[SUCCESS] SNAP TREE CONSTRUCTION of piece: '{piece.m_name}' {piece.transform.position}");
        }

        private static readonly Func<SnapNode.Precision, bool> OrdinaryPrecisionOnly = precision => precision == SnapNode.Precision.ORDINARY;
        private static readonly Func<SnapNode.Precision, bool> OrdinaryAndSuperiorPrecision = precision => precision == SnapNode.Precision.ORDINARY || precision == SnapNode.Precision.SUPERIOR;

        public static TraversalResult? FindNearestOrdinaryPrecisionSnapNodeTo(Vector3 referencePosition, List<Piece> pieces) => FindNearestSnapNodeTo(referencePosition, pieces, OrdinaryPrecisionOnly);
        public static TraversalResult? FindNearestSuperiorPrecisionSnapNodeTo(Vector3 referencePosition, List<Piece> pieces) => FindNearestSnapNodeTo(referencePosition, pieces, OrdinaryAndSuperiorPrecision);
        public static TraversalResult? FindNearestOrdinaryPrecisionSnapNodeCombinationOf(Piece buildPiece, List<Piece> neighbourPieces) => FindNearestSnapNodeTo(buildPiece.PrimarySnapNodes(), neighbourPieces, OrdinaryPrecisionOnly);
        public static TraversalResult? FindNearestSuperiorPrecisionSnapNodeCombinationOf(Piece buildPiece, List<Piece> neighbourPieces) => FindNearestSnapNodeTo(buildPiece.PrimarySnapNodes(), neighbourPieces, OrdinaryAndSuperiorPrecision);

        private static HashSet<SnapNode> DeriveSnapNodesFrom(Piece piece)
        {
            switch (piece.Type())
            {
                case CONSTRUCTION:
                    switch (piece.Shape())
                    {
                        case LINE:
                            return DeriveSnapNodesFrom(piece, DeriveLineFrom);
                        case BOX:
                            return DeriveSnapNodesFrom(piece, DeriveBoxFrom);
                        case CUBE:
                            return DeriveSnapNodesFrom(piece, DeriveSidesFromCube);
                        case CYLINDER:
                            return DeriveSnapNodesFrom(piece, DeriveSidesFromCylinder); // perfectly sufficient approximation of octagons
                        case UNDEFINED:
                            return DeriveSnapNodesFrom(piece, DeriveSidesFromUndefined); // imperfectly sufficient approximation of triangle
                    }
                    break;
                case FURNITURE:
                    return ZeroSnapNodes;
                case TABLE:
                    return DeriveSnapNodesFrom(piece, ImposeTopSideOn);
            }

            throw new InvalidOperationException("This is supposedly mathematically impossible :D");
        }

        private static HashSet<SnapNode> DeriveSnapNodesFrom<T>(Piece piece, Func<Piece, List<T>> deriveSidesFrom) where T : ISide
        {
            var sides = deriveSidesFrom(piece);
            Logger.Debug(() => $"SIDES: {string.Join(", ", sides)}");

            var snapNodes = new HashSet<SnapNode>(byte.MaxValue);
            foreach (var side in sides)
            {
                side.FillUp(snapNodes);
            }

            return snapNodes;
        }

        private static List<Line> DeriveLineFrom(Piece piece)
        {
            var sides = DeriveSidesFromUndefined(piece);
            if (sides.Count != OneSideAllowed)
            {
                Logger.Warn(() => $"EXPECTED SIDES on Piece '{piece.m_name}' {piece.transform.position}: {OneSideAllowed}, ACTUAL SIDES: {sides.Count}");
            }

            return sides;
        }

        private static List<Box> DeriveBoxFrom(Piece piece) => FindSidesOfBoxOrCube(piece, FormValidTwoDimensionalBoxSide, OneSideAllowed);

        private static List<Box> DeriveSidesFromCube(Piece piece) => FindSidesOfBoxOrCube(piece, FormValidThreeDimensionalBoxSide(piece), SidesOfCube);

        private static List<Box> FindSidesOfBoxOrCube(Piece piece, Func<Vector3, Vector3, Vector3, Vector3, bool> isValidSide, int numberOfSides)
        {
            Boxes.Clear();
            var boxName = 'A';
            var snapNodes = piece.PrimarySnapNodes();
            for (var i = 0; i < snapNodes.Count; i++)
            {
                for (var j = i + 1; j < snapNodes.Count; j++)
                {
                    for (var k = j + 1; k < snapNodes.Count; k++)
                    {
                        for (var l = k + 1; l < snapNodes.Count; l++)
                        {
                            if (isValidSide(snapNodes[i], snapNodes[j], snapNodes[k], snapNodes[l]))
                            {
                                Boxes.Add(new Box(boxName++, snapNodes[i], snapNodes[j], snapNodes[k], snapNodes[l]));
                            }
                        }
                    }
                }
            }

            if (Boxes.Count != numberOfSides)
            {
                Logger.Warn(() => $"EXPECTED SIDES on Piece '{piece.m_name}' {piece.transform.position}: {numberOfSides}, ACTUAL SIDES: {Boxes.Count}");
            }

            return Boxes;
        }

        private static List<Line> DeriveSidesFromCylinder(Piece piece)
        {
            Lines.Clear();
            var lineName = 'A';
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
                                Lines.Add(new Line(lineName++, snapNodeA, snapNodeB)); // Ultimately should be modeled as a sequence of boxes
                            }
                            else if (LiesOnSameHorizontalSide(snapNodeA, snapNodeB) && LiesOnHorizontalMiddle(snapMid, piece))
                            {
                                Lines.Add(new Line(lineName++, snapNodeA, snapNodeB)); // Ultimately should be modeled as a pair of "circles"
                            }
                        }
                    }
                }
            }

            if (Lines.Count != SidesOfOctagon)
            {
                Logger.Warn(() => $"EXPECTED SIDES on Piece '{piece.m_name}' {piece.transform.position}: {SidesOfOctagon}, ACTUAL SIDES: {Lines.Count}");
            }

            return Lines;
        }

        private static List<Line> DeriveSidesFromUndefined(Piece piece)
        {
            Lines.Clear();
            var lineName = 'A';
            for (var i = 0; i < piece.PrimarySnapNodes().Count; i++)
            {
                for (var j = i + 1; j < piece.PrimarySnapNodes().Count; j++)
                {
                    Lines.Add(new Line(lineName++, piece.PrimarySnapNodes()[i], piece.PrimarySnapNodes()[j])); // Ultimate fallback. Surprisingly satisfying approximation of a vast majority of arbitrary pieces.
                }
            }

            return Lines;
        }

        private static List<ISide> ImposeTopSideOn(Piece piece)
        {
            Sides.Clear();
            var side = piece.TopSide();
            Sides.Add(side);

            return Sides;
        }

        private static TraversalResult? FindNearestSnapNodeTo(Vector3 referencePosition, List<Piece> pieces, Func<SnapNode.Precision, bool> isDesired)
        {
            HorribleOptimization.Clear();
            HorribleOptimization.Add(referencePosition);
            return FindNearestSnapNodeTo(HorribleOptimization, pieces, isDesired);
        }

        private static TraversalResult? FindNearestSnapNodeTo(List<Vector3> buildPieceSnapNodes, List<Piece> neighborPieces, Func<SnapNode.Precision, bool> isDesired)
        {
            var minDistance = float.PositiveInfinity;
            TraversalResult? result = null;
            foreach (var buildPieceSnapNode in buildPieceSnapNodes)
            {
                foreach (var neighbourPiece in neighborPieces)
                {
                    var snapTree = SnapTrees.LookUp(neighbourPiece);
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
