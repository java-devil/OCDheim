using System;
using System.Collections.Generic;
using UnityEngine;

namespace OCDheim
{
    public static class PieceHelpers
    {
        public static readonly Dictionary<string, Func<Piece, ISide>> SnappableFurniturePieces = new Dictionary<string, Func<Piece, ISide>>()
        {
            ["$piece_table_oak"]         = piece => new Box(piece, new Vector2(3.0f, 0.8f)),
            ["$piece_blackmarble_table"] = piece => new Box(piece, new Vector2(1.15f, 0.5f)),
            ["$piece_table"]             = piece => new Box(piece, new Vector2(1.1f, 0.475f)),
            ["$piece_table_round"]       = piece => new Circle(piece, new Vector2(1.15f, 0.0f))
        };
        private static readonly List<Transform> primarySPs = new List<Transform>();
        private static readonly List<Vector3> primarySNs = new List<Vector3>();
        private static Piece thisPiece = null;

        public static bool IsConstructionPiece(this Piece piece) => IsType(piece, snapNodes => snapNodes.Count != 0, "CONSTRUCTION");
        public static bool IsFurniturePiece(this Piece piece) => IsType(piece, snapNodes => snapNodes.Count == 0, "FURNITURE");
        public static bool IsSnappableFurniturePiece(this Piece piece) => IsType(piece, snapNodes => piece.IsFurniturePiece() && SnappableFurniturePieces.ContainsKey(piece.m_name), "SNAPPABLE FURNITURE");
        public static bool IsSnappablePiece(this Piece piece) => IsConstructionPiece(piece) || IsSnappableFurniturePiece(piece);
        public static bool IsLine(this Piece piece) => IsType(piece, snapNodes => snapNodes.Count == 2, "LINE");
        public static bool IsBox(this Piece piece) => IsType(piece, snapNodes => snapNodes.Count == 4 && EverySnapNodeLiesOnExtremums(piece), "BOX");
        public static bool IsCube(this Piece piece) => IsType(piece, snapNodes => snapNodes.Count == 8 && EverySnapNodeLiesOnExtremums(piece), "CUBE");
        public static bool IsCylinder(this Piece piece) => IsType(piece, snapNodes => snapNodes.Count == 18, "CYLINDER"); // Mathematically indefensible. However sufficient with Vanilla Valheim pieces for now ;)

        // SURPRISE! Screw with the GC too much via unbelievable GLOBAL STATE sorcery and it will lovingly screw you back beyond belief via DATA RACE necromancy!
        public static List<Vector3> PrimarySnapNodesDefensiveCopy(this Piece piece)
        {
            return new List<Vector3>(primarySNs);
        }
        
        public static List<Vector3> PrimarySnapNodes(this Piece piece)
        {
            piece.FlushWhenRequired();
            return primarySNs;
        }

        public static Vector3 TopMiddlee(this Piece piece)
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

        private static void FlushWhenRequired(this Piece piece)
        {
            if (thisPiece != piece)
            {
                //Debug.Log($"[FLUSHED] Primary Snap Nodes of previous piece in lieu of Piece: '{piece.m_name}' {piece.transform.position}");
                thisPiece = piece;
                primarySPs.Clear();
                primarySNs.Clear();

                piece.PopulatePrimarySnapNodes();
                //Debug.Log($"PRIMARY SNAP NODES: {(primarySNs.Count > 0 ? string.Join(", ", primarySNs) : "NONE")} of Piece: '{piece.m_name}' {piece.transform.position}");
            }
        }

        private static bool IsType(this Piece piece, Func<List<Vector3>, bool> isShape, string shapeName)
        {
            piece.FlushWhenRequired();
            if (isShape.Invoke(primarySNs))
            {
                //Debug.Log($"Piece '{piece.m_name}' { piece.transform.position} IS a {shapeName} piece");
                return true;
            }
            else
            {
                //Debug.Log($"Piece '{piece.m_name}' { piece.transform.position} IS NOT a {shapeName} piece");
                return false;
            }
        }

        private static void PopulatePrimarySnapNodes(this Piece piece)
        {
            piece.GetSnapPoints(primarySPs);
            foreach (var sp in primarySPs)
            {
                primarySNs.Add(sp.transform.position);
            }
        }

        private static bool EverySnapNodeLiesOnExtremums(this Piece piece)
        {
            var minimums = piece.SolveMinimumsOf();
            var maximums = piece.SolveMaximumsOf();
            foreach (var snapNode in primarySNs)
            {
                if (!LiesOnExtremums(snapNode, minimums, maximums))
                {
                    return false;
                }
            }
            return true;
        }

        private static Vector3 SolveMinimumsOf(this Piece piece)
        {
            var xMin = float.PositiveInfinity;
            var yMin = float.PositiveInfinity;
            var zMin = float.PositiveInfinity;
            foreach (var sn in primarySNs)
            {
                xMin = xMin > sn.x ? sn.x : xMin;
                yMin = yMin > sn.y ? sn.y : yMin;
                zMin = zMin > sn.z ? sn.z : zMin;
            }

            return new Vector3(xMin, yMin, zMin);
        }

        private static Vector3 SolveMaximumsOf(this Piece piece)
        {
            var xMax = float.NegativeInfinity;
            var yMax = float.NegativeInfinity;
            var zMax = float.NegativeInfinity;
            foreach (var sn in primarySNs)
            {
                xMax = xMax < sn.x ? sn.x : xMax;
                yMax = yMax < sn.y ? sn.y : yMax;
                zMax = zMax < sn.z ? sn.z : zMax;
            }

            return new Vector3(xMax, yMax, zMax);
        }

        private static bool LiesOnExtremums(Vector3 snapNode, Vector3 minimums, Vector3 maximums)
        {
            if (snapNode.x != minimums.x && snapNode.x != maximums.x)
            {
                return false;
            }
            if (snapNode.y != minimums.y && snapNode.y != maximums.y)
            {
                return false;
            }
            if (snapNode.z != minimums.z && snapNode.z != maximums.z)
            {
                return false;
            }
            return true;
        }
    }
}
