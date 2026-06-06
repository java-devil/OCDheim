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
            ["$piece_table_oak"]         = piece => new Box(piece, new Vector2(3.0f, 0.8f)),
            ["$piece_blackmarble_table"] = piece => new Box(piece, new Vector2(1.15f, 0.5f)),
            ["$piece_table"]             = piece => new Box(piece, new Vector2(1.1f, 0.475f)),
            ["$piece_table_round"]       = piece => new Circle(piece, new Vector2(1.15f, 0.0f))
        };

        private static readonly List<Transform> PrimarySPs = new List<Transform>();
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

        private static bool EverySnapNodeLiesOnExtremums(List<Vector3> snapNodes)
        {
            var minimums = SolveMinimumsOf(snapNodes);
            var maximums = SolveMaximumsOf(snapNodes);
            foreach (var snapNode in snapNodes)
            {
                if (!LiesOnExtremums(snapNode, minimums, maximums))
                {
                    return false;
                }
            }
            return true;
        }

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

        private static bool LiesOnExtremums(Vector3 snapNode, Vector3 minimums, Vector3 maximums)
        {
            if (!Mathf.Approximately(snapNode.x, minimums.x) && !Mathf.Approximately(snapNode.x, maximums.x))
            {
                return false;
            }
            if (!Mathf.Approximately(snapNode.y, minimums.y) && !Mathf.Approximately(snapNode.y, maximums.y))
            {
                return false;
            }
            if (!Mathf.Approximately(snapNode.z, minimums.z) && !Mathf.Approximately(snapNode.z, maximums.z))
            {
                return false;
            }

            return true;
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
            var type = TypeOf(piece.PrimarySnapNodes(), piece.m_name);
            Logger.Debug(() => $"Piece '{piece.m_name}' { piece.transform.position} IS a {type} Piece");
            
            return type;
        }

        private static PieceType TypeOf(List<Vector3> primarySNs, string pieceName)
        {
            if (Tables.ContainsKey(pieceName)) { return TABLE; }
            return primarySNs.Count != 0 ? CONSTRUCTION : FURNITURE;
        }
        
        private static PieceShape ShapeOf(Piece piece)
        {
            piece.FlushPrimarySnapNodes();
            var shape = ShapeOf(piece.PrimarySnapNodes());
            Logger.Debug(() => $"Piece '{piece.m_name}' { piece.transform.position} IS a {shape} Piece");
            
            return shape;
        }

        private static PieceShape ShapeOf(List<Vector3> primarySNs)
        {
            if (primarySNs.Count == 2) { return LINE; }
            if (primarySNs.Count == 4 && EverySnapNodeLiesOnExtremums(primarySNs)) { return BOX; }
            if (primarySNs.Count == 8 && EverySnapNodeLiesOnExtremums(primarySNs)) { return CUBE; }
            if (primarySNs.Count == 18) { return CYLINDER; } // Mathematically indefensible. However, sufficient with Vanilla Valheim pieces for now ;)

            return UNDEFINED;
        }
    }
}
