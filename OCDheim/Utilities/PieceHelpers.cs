using System;
using System.Collections.Generic;
using UnityEngine;

using static OCDheim.PieceShape;
using static OCDheim.PieceType;
using static OCDheim.PlayerHelpers;

namespace OCDheim
{
    public static class PieceHelpers
    {
        public static Piece buildPiece => player.m_placementGhost?.GetComponent<Piece>();

        private static readonly IMemoryRepo<Piece, Bounds> PieceSizes = new MemoryRepo<Piece, string, Bounds>(BoundsOf, piece => piece.m_name, byte.MaxValue);
        private static readonly IMemoryRepo<Piece, PieceType> PieceTypes = new MemoryRepo<Piece, string, PieceType>(TypeOf, piece => piece.m_name, byte.MaxValue);
        private static readonly IMemoryRepo<Piece, PieceShape> PieceShapes = new MemoryRepo<Piece, string, PieceShape>(ShapeOf, piece => piece.m_name, byte.MaxValue);
        private static readonly Dictionary<string, Func<Piece, ISide>> Tables = new Dictionary<string, Func<Piece, ISide>>
        {
            ["$piece_stool"]             = piece => new Box(piece, new Vector2(0.0f, 0.0f)),
            ["$piece_table_oak"]         = piece => new Box(piece, new Vector2(3.0f, 0.8f)),
            ["$piece_blackmarble_table"] = piece => new Box(piece, new Vector2(1.15f, 0.5f)),
            ["$piece_table"]             = piece => new Box(piece, new Vector2(1.1f, 0.475f)),
            ["$piece_chestbarrel"]       = piece => new Circle(piece, new Vector2(0.0f, 0.0f)),
            ["$piece_table_round"]       = piece => new Circle(piece, new Vector2(1.15f, 0.0f))
        };

        private static readonly List<Transform> PrimarySPs = new List<Transform>();
        private static readonly List<Vector3> FixedAxisSNs = new List<Vector3>();
        private static readonly List<Vector3> PrimarySNs = new List<Vector3>();

        public static Bounds Bounds(this Piece piece) => PieceSizes.LookUp(piece);
        public static PieceType Type(this Piece piece) => PieceTypes.LookUp(piece);
        public static PieceShape Shape(this Piece piece) => PieceShapes.LookUp(piece);
        public static ISide TopSide(this Piece piece) => Tables[piece.m_name].Invoke(piece);
        public static bool IsGroundBound(this Piece piece) => piece.m_groundPiece || piece.m_clipGround || piece.m_clipEverything;

        public static List<Vector3> PrimarySnapNodes(this Piece piece)
        {
            piece.FlushPrimarySnapNodes();
            piece.PopulatePrimarySnapNodes();

            return PrimarySNs;
        }

        private static List<Vector3> FixedAxisSnapNodes(this Piece piece)
        {
            PrimarySPs.Clear();
            FixedAxisSNs.Clear();
            piece.GetSnapPoints(PrimarySPs);
            foreach (var sn in PrimarySPs)
            {
                FixedAxisSNs.Add(piece.transform.InverseTransformDirection(sn.position));
            }
            
            return FixedAxisSNs;
        }

        private static void FlushPrimarySnapNodes(this Piece _)
        {
            PrimarySPs.Clear();
            PrimarySNs.Clear();
            Logger.Debug(() => $"[FLUSHED] Primary Snap Nodes of previous piece");
        }

        private static void PopulatePrimarySnapNodes(this Piece piece)
        {
            piece.GetSnapPoints(PrimarySPs);
            foreach (var sp in PrimarySPs)
            {
                PrimarySNs.Add(sp.transform.position);
            }
            
            Logger.Debug(() => $"PRIMARY SNAP NODES: {(PrimarySNs.Count > 0 ? string.Join(", ", PrimarySNs) : "NONE")} of Piece: '{piece.m_name}' {piece.transform.position}");
        }
        
        public static Vector3 TopMiddle(this Piece piece)
        {
            var colliders = piece.GetComponentsInChildren<Collider>();
            var bounds = piece.GetComponentInChildren<Collider>().bounds;
            foreach (var collider in colliders)
            {
                bounds.Encapsulate(collider.bounds);
            }

            var y = bounds.max.y;
            return new Vector3(piece.transform.position.x, y, piece.transform.position.z);
        }

        private static bool EverySnapNodeLiesOnExtremums(List<Vector3> snapNodes, Func<int, int, int, int, int, int, bool> condition)
        {
            var minimums = SolveMinimumsOf(FixedAxisSNs);
            var maximums = SolveMaximumsOf(FixedAxisSNs);

            var (xMin, yMin, zMin) = OccurrencesOf(minimums, snapNodes);
            var (xMax, yMax, zMax) = OccurrencesOf(maximums, snapNodes);
            
            return condition(xMin, xMax, yMin, yMax, zMin, zMax);
        }

        private static bool EverySnapNodeLiesOn2DExtremums(List<Vector3> snapNodes) => EverySnapNodeLiesOnExtremums(snapNodes, (xMinimums, xMaximums, yMinimums, yMaximums, zMinimums, zMaximums) =>
            (xMinimums == 2 && xMaximums == 2 && yMinimums == 2 && yMaximums == 2  && zMinimums == 4 && zMaximums == 4)
            || (xMinimums == 2 && xMaximums == 2 && yMinimums == 4 && yMaximums == 4  && zMinimums == 2 && zMaximums == 2)
            || (xMinimums == 4 && xMaximums == 4 && yMinimums == 2 && yMaximums == 2  && zMinimums == 2 && zMaximums == 2)
        );
        
        private static bool EverySnapNodeLiesOn3DExtremums(List<Vector3> snapNodes) => EverySnapNodeLiesOnExtremums(snapNodes, (xMinimums, xMaximums, yMinimums, yMaximums, zMinimums, zMaximums) =>
            xMinimums == 4 && xMaximums == 4 && yMinimums == 4 && yMaximums == 4 && zMinimums == 4 && zMaximums == 4);

        private static Vector3 SolveMinimumsOf(List<Vector3> snapNodes)
        {
            var xMin = float.PositiveInfinity;
            var yMin = float.PositiveInfinity;
            var zMin = float.PositiveInfinity;
            foreach (var sn in snapNodes)
            {
                xMin = xMin > sn.x ? sn.x : xMin;
                yMin = yMin > sn.y ? sn.y : yMin;
                zMin = zMin > sn.z ? sn.z : zMin;
            }

            return new Vector3(xMin, yMin, zMin);
        }

        private static Vector3 SolveMaximumsOf(List<Vector3> snapNodes)
        {
            var xMax = float.NegativeInfinity;
            var yMax = float.NegativeInfinity;
            var zMax = float.NegativeInfinity;
            foreach (var sn in snapNodes)
            {
                xMax = xMax < sn.x ? sn.x : xMax;
                yMax = yMax < sn.y ? sn.y : yMax;
                zMax = zMax < sn.z ? sn.z : zMax;
            }

            return new Vector3(xMax, yMax, zMax);
        }

        private static (int, int, int) OccurrencesOf(Vector3 extremums, List<Vector3> snapNodes)
        {
            var xs = 0;
            var ys = 0;
            var zs = 0;
            foreach (var sn in snapNodes)
            {
                if (Mathf.Approximately(extremums.x, sn.x)) { xs++; }
                if (Mathf.Approximately(extremums.y, sn.y)) { ys++; }
                if (Mathf.Approximately(extremums.z, sn.z)) { zs++; }
            }

            return (xs, ys, zs);
        }
        
        private static Bounds BoundsOf(Piece piece)
        {
            var rotation = piece.transform.rotation;
            piece.transform.rotation = Quaternion.Euler(0, 0, 0);
            var bounds = piece.GetComponentInChildren<MeshRenderer>().bounds;
            piece.transform.rotation = rotation;
            
            return bounds;
        }
        
        private static PieceType TypeOf(Piece piece)
        {
            var type = TypeOf(piece, piece.FixedAxisSnapNodes());
            Logger.Debug(() => $"Piece '{piece.m_name}' { piece.transform.position} IS a {type} Piece");
            
            return type;
        }

        private static PieceType TypeOf(Piece piece, List<Vector3> snapNodes)
        {
            if (Tables.ContainsKey(piece.m_name)) { return TABLE; }
            return snapNodes.Count != 0 ? CONSTRUCTION : FURNITURE;
        }
        
        private static PieceShape ShapeOf(Piece piece)
        {
            var shape = ShapeOf(piece.FixedAxisSnapNodes());
            Logger.Debug(() => $"Piece '{piece.m_name}' { piece.transform.position} IS a {shape} Piece");
            
            return shape;
        }

        private static PieceShape ShapeOf(List<Vector3> snapNodes)
        {
            if (snapNodes.Count == 2) { return LINE; }
            if (snapNodes.Count == 4 && EverySnapNodeLiesOn2DExtremums(snapNodes)) { return BOX; }
            if (snapNodes.Count == 8 && EverySnapNodeLiesOn3DExtremums(snapNodes)) { return CUBE; }
            if (snapNodes.Count == 18) { return CYLINDER; } // Mathematically indefensible. However, sufficient with Vanilla Valheim pieces for now ;)

            return UNDEFINED;
        }
    }
}
