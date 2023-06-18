using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace OCDheim
{
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    public static class PreciseBuildPieceSnapper
    {
        private const float NeighbourhoodSize = 3.5f;
        private const float CollisionDetectionThreshold = 10.0f;

        private static readonly List<Piece> neighbourPieces = new List<Piece>();
        private static readonly List<Piece> snappableNeighbourPieces = new List<Piece>();

        private static bool ShouldOverrideVanillaValheim(ItemDrop.ItemData tool) => Keybindings.SnapModeEnabled && tool.IsHammer();
        private static bool ShouldSkipCollisionDetection(Piece buildPiece) => buildPiece.m_groundPiece || buildPiece.m_clipGround || buildPiece.m_clipEverything;
        private static bool ShouldSnapInternally(Piece buildPiece) => buildPiece.IsConstructionPiece() && Keybindings.PrecisionModeEnabled;
        private static bool ShouldSnapExternally(Piece buildPiece) => buildPiece.IsFurniturePiece();

        public static void Postfix(bool flashGuardStone, Player __instance, GameObject ___m_placementGhost, int ___m_placeRayMask)
        {
            if (___m_placementGhost != null && ___m_placementGhost.activeSelf)
            {
                var tool = __instance.GetRightItem();
                var piece = ___m_placementGhost.GetComponent<Piece>();
                var layerMask = ___m_placeRayMask - LayerMask.GetMask("piece_nonsolid");

                Keybindings.Refresh();
                if (Keybindings.GridModeEnabled)
                {
                    PreciseGridModeSnapper.SnapToGroundGrid(tool, piece, layerMask);
                }
                else if (ShouldOverrideVanillaValheim(tool))
                {
                    SnapToNeighbourPiece(piece, layerMask);
                }
            }
        }

        public static void GetAllPiecesInRadius(Vector3 p, float radius, List<Piece> pieces)
        {
            if (Piece.s_ghostLayer == 0)
                Piece.s_ghostLayer = LayerMask.NameToLayer("ghost");
            foreach (Piece allPiece in Piece.s_allPieces)
            {
                if (allPiece.gameObject.layer != Piece.s_ghostLayer && (double)Vector3.Distance(p, allPiece.transform.position) < (double)radius)
                    pieces.Add(allPiece);
            }
        }

        private static void SnapToNeighbourPiece(Piece buildPiece, int layerMask)
        {
            var playerPoV = DeterminePlayerPoV(layerMask);
            var neighbourPieces = FindNeighbourPieces(playerPoV);
            if (ShouldSnapInternally(buildPiece))
            {
                SnapInternally(buildPiece, neighbourPieces);
            }
            else if (ShouldSnapExternally(buildPiece))
            {
                var neighbourPieceFound = Keybindings.PrecisionModeEnabled
                    ? SnapTree.FindNearestSuperiorPrecisionSnapNodeTo(playerPoV, neighbourPieces)
                    : SnapTree.FindNearestOrdinaryPrecisionSnapNodeTo(playerPoV, neighbourPieces);

                if (neighbourPieceFound.HasValue)
                {
                    (var neighbourPieceExit, var perpendicularToplayerPoV) = DetermineNeighbourPieceExit(neighbourPieceFound.Value.neighbourSnapNode, neighbourPieceFound.Value.neighbourPiece, layerMask);
                    if (!ShouldSkipCollisionDetection(buildPiece))
                    {
                        SnapExternally(buildPiece, neighbourPieceExit, perpendicularToplayerPoV);
                    }
                    else
                    {
                        buildPiece.transform.position = neighbourPieceExit;
                    }
                }
            }
        }

        public static void SnapInternally(Piece buildPiece, List<Piece> neighbourPieces)
        {
            var snapNodeCoupleFound = SnapTree.FindNearestOrdinaryPrecisionSnapNodeCombinationOf(buildPiece, neighbourPieces);
            if (snapNodeCoupleFound.HasValue)
            {
                buildPiece.transform.position += snapNodeCoupleFound.Value.neighbourSnapNode - snapNodeCoupleFound.Value.buildPieceSnapNode;
            }
        }

        public static void SnapExternally(Piece buildPiece, Vector3 neighbourPieceExit, Vector3 perpendicularToPlayerPoV)
        {
            buildPiece.transform.position = neighbourPieceExit + perpendicularToPlayerPoV * CollisionDetectionThreshold;
            var buildPieceExit = DeterminePieceExit(buildPiece, neighbourPieceExit);

            SnapPiecesByExits(buildPiece, buildPieceExit, neighbourPieceExit, perpendicularToPlayerPoV);
        }

        private static (Vector3, Vector3) DetermineNeighbourPieceExit(SnapNode neighbourSnapNode, Piece neighbourPiece, int layerMask)
        {
            var pokedNeighbourSnapNode = PokeToMiddle(neighbourSnapNode, neighbourPiece);
            var perpendicularToplayerPoV = DeterminePerpendicularToPlayerPoVOn(pokedNeighbourSnapNode, layerMask);
            var microscopicObserver = neighbourSnapNode + perpendicularToplayerPoV;
            var pokedNeighbourPieceExit = DeterminePieceExit(neighbourPiece, microscopicObserver);

            return (pokedNeighbourPieceExit, perpendicularToplayerPoV);
        }

        private static Vector3 DeterminePlayerPoV(int layerMask)
        {
            var playerPosition = GameCamera.instance.transform.position;
            var playerPerspective = GameCamera.instance.transform.forward;
            Physics.Raycast(playerPosition, playerPerspective, out var rayHit, CollisionDetectionThreshold, layerMask);
            return rayHit.point;
        }

        private static Vector3 DeterminePerpendicularToPlayerPoVOn(Vector3 snapNode, int layerMask)
        {
            var playerPosition = GameCamera.instance.transform.position;
            var playerPerspectiveOnSnapNode = snapNode - playerPosition;
            Physics.Raycast(playerPosition, playerPerspectiveOnSnapNode, out var rayHit, CollisionDetectionThreshold, layerMask);
            return rayHit.normal;
        }

        private static List<Piece> FindNeighbourPieces(Vector3 playerPoV)
        {
            neighbourPieces.Clear();
            snappableNeighbourPieces.Clear();
            GetAllPiecesInRadius(playerPoV, NeighbourhoodSize, neighbourPieces);
            foreach (var neighbourPiece in neighbourPieces)
            {
                if (neighbourPiece.IsSnappablePiece())
                {
                    snappableNeighbourPieces.Add(neighbourPiece);
                }
            }
            return snappableNeighbourPieces;
        }

        private static Vector3 DeterminePieceExit(Piece piece, Vector3 observer)
        {
            var collisionDistance = float.PositiveInfinity;
            var exitCollision = piece.transform.position;
            var colliders = piece.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                if (collider.enabled && !collider.isTrigger)
                {
                    var meshCollider = collider as MeshCollider;
                    if (meshCollider == null || meshCollider.convex)
                    {
                        var collision = collider.ClosestPoint(observer);
                        var distance = Vector3.Distance(observer, collision);
                        if (distance < collisionDistance)
                        {
                            exitCollision = collision;
                            collisionDistance = distance;
                        }
                    }
                }
            }

            return exitCollision;
        }

        // Remove seemingly arbitrary deflections when hitting piece corners.
        private static Vector3 PokeToMiddle(Vector3 snapNode, Piece piece)
        {
            var direction = piece.transform.position - snapNode;
            var diminishedDirection = direction.normalized * 0.05f;
            return snapNode + diminishedDirection;
        }

        private static void SnapPiecesByExits(Piece buildPiece, Vector3 buildPieceExit, Vector3 neighbourPieceEntry, Vector3 perpendicularToNeighbourPiece)
        {
            var diff = buildPiece.transform.position - buildPieceExit;
            diff = new Vector3(Mathf.Abs(diff.x), Mathf.Abs(diff.y), Mathf.Abs(diff.z)); // Supposed to prevent sign flipping from minus to plus during "double minus" vector multiplication.
            diff = Vector3.Scale(diff, perpendicularToNeighbourPiece); // We move the piece only on dimensions required to prevent collisions. Otherwise the piece would lose its center as it does in Vanilla Valheim.
            buildPiece.transform.position = neighbourPieceEntry + diff;
        }
    }
}
