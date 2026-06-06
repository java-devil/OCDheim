using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

using static OCDheim.PieceHelpers;
using static OCDheim.PieceType;
using static OCDheim.PlayerHelpers;
using static OCDheim.PreciseTerrainModifier;
using static OCDheim.PrecisionMode;

namespace OCDheim
{
    [HarmonyPatch]
    public static class RemoveExpensiveUnnecessaryCalls
    {
        private static bool ShouldSuppressVanillaValheim() =>
            (KeyBinder.gridModeEnabled && PrecisePieceSnapper.GridModeRequirementsSatisfied()) ||
            (KeyBinder.snapModeEnabled && PrecisePieceSnapper.SnapModeRequirementsSatisfied());
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Player))]
        [HarmonyPatch(nameof(Player.UpdatePlacementGhost))]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var foundCodeToRemove = false;
            var foundCodeToReplace = false;
            foreach (var instruction in instructions)
            {
                foundCodeToRemove = foundCodeToRemove ? foundCodeToRemove : instruction.opcode == OpCodes.Ldstr && (string)instruction.operand == "AltPlace";
                if (foundCodeToRemove && !foundCodeToReplace)
                {
                    foundCodeToReplace = foundCodeToReplace ? foundCodeToReplace : instruction.opcode == OpCodes.Call;
                    if (!foundCodeToReplace)
                    {
                        instruction.opcode = OpCodes.Nop;
                        instruction.operand = null;

                        yield return instruction;
                    }
                    else
                    {
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = SymbolExtensions.GetMethodInfo(() => ShouldSuppressVanillaValheim());

                        yield return instruction;
                    }
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }

    [HarmonyPatch]
    public static class PrecisePieceSnapper
    {
        private const float NeighbourhoodSize = 2.5f;

        private static readonly int PiecesOnly = UnityEngine.LayerMask.GetMask("piece");
        private static readonly int LayerMask = player.m_placeRayMask - UnityEngine.LayerMask.GetMask("piece_nonsolid");
        private static readonly Collider[] NeighbourColliders = new Collider[byte.MaxValue];
        private static readonly List<Piece> NeighbourPieces = new List<Piece>();

        public static bool GridModeRequirementsSatisfied() => player.HasBuildPieceEquipped() || player.HasOverlayVisible();
        public static bool SnapModeRequirementsSatisfied() => player.HasBuildPieceEquipped() && (buildPiece.Type() != CONSTRUCTION || KeyBinder.precisionMode == SUPERIOR);
        private static bool ShouldUsePlayerPositionAsGroundLevelReference() => player.HasLevelGroundTerraformToolEquipped() && KeyBinder.snapModeEnabled;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player))]
        [HarmonyPatch(nameof(Player.UpdatePlacementGhost))]
        private static void SnapBuildPiece()
        {
            if (KeyBinder.gridModeEnabled && GridModeRequirementsSatisfied())
            {
                SnapToWorldGrid(buildPiece);
            }
            else if (KeyBinder.snapModeEnabled && SnapModeRequirementsSatisfied())
            {
                SnapToNeighbourPiece(buildPiece);
            }
        }

        private static void SnapToWorldGrid(Piece buildPiece)
        {
            var playerPoV = DeterminePlayerPoV();
            var precision = (int)KeyBinder.precisionMode;
            var (xOnGrid, zOnGrid) = SnapToWorldGrid(playerPoV, precision);

            if (ShouldUsePlayerPositionAsGroundLevelReference())
            {
                buildPiece.transform.position = new Vector3(xOnGrid, playerPos.y, zOnGrid);
            }
            else
            {
                var drillCoords = new Vector2(xOnGrid, zOnGrid);
                var posOnGrid = player.HasOverlayVisible() || buildPiece.IsGroundBound()
                    ? PrecisionDrill.DrillDownTillGround(drillCoords)
                    : PrecisionDrill.DrillDownTillFloor(drillCoords, buildPiece.transform.position.y);
                SnapExternallyHelper(buildPiece, posOnGrid, Vector3.up);
            }

            if (player.HasOverlayVisible())
            {
                FixVanillaValheimBugWithSpinningTerrainModificationVFX();
            }
        }

        private static (float xOnGrid, float zOnGrid) SnapToWorldGrid(Vector3 playerPoV, int precision)
        {
            float xOnGrid;
            float zOnGrid;
            if (player.HasColorBrushEquipped())
            {
                var chunkMidX = Mathf.Floor(playerPoV.x / HTilesPerChunk) * HTilesPerChunk;
                var chunkMidZ = Mathf.Floor(playerPoV.z / HTilesPerChunk) * HTilesPerChunk;
                var chunkMinX = chunkMidX - HalfPTilesPerChunk;
                var chunkMinZ = chunkMidZ - HalfPTilesPerChunk;
                var relPosX = playerPoV.x - chunkMinX;
                var relPosZ = playerPoV.z - chunkMinZ;

                xOnGrid = chunkMinX + Mathf.Floor(relPosX / PTileSize) * PTileSize + (PTileSize * 0.5f);
                zOnGrid = chunkMinZ + Mathf.Floor(relPosZ / PTileSize) * PTileSize + (PTileSize * 0.5f);
            }
            else
            {
                xOnGrid = Mathf.Round(playerPoV.x * precision) / precision;
                zOnGrid = Mathf.Round(playerPoV.z * precision) / precision;
            }
            
            return (xOnGrid, zOnGrid);
        }

        private static void SnapToNeighbourPiece(Piece buildPiece)
        {
            var playerPoV = DeterminePlayerPoV();
            var neighbourPieces = FindNeighbourPieces(playerPoV);
            switch (buildPiece.Type())
            {
                case CONSTRUCTION:
                    SnapInternally(buildPiece, neighbourPieces);
                    break;
                case FURNITURE:
                case TABLE:
                    SnapExternally(buildPiece, neighbourPieces, playerPoV);
                    break;
            }
        }

        private static void SnapInternally(Piece buildPiece, List<Piece> neighbourPieces)
        {
            var snapNodeCoupleOrNull = KeyBinder.precisionMode == ORDINARY
                ? SnapTree.FindNearestOrdinaryPrecisionSnapNodeCombinationOf(buildPiece, neighbourPieces) // TODO: This is dead code ATM
                : SnapTree.FindNearestSuperiorPrecisionSnapNodeCombinationOf(buildPiece, neighbourPieces);

            if (snapNodeCoupleOrNull is SnapTree.TraversalResult snapNodeCouple)
            {
                buildPiece.transform.position += snapNodeCouple.neighbourSnapNode - snapNodeCouple.buildPieceSnapNode;
            }
        }

        private static void SnapExternally(Piece buildPiece, List<Piece> neighbourPieces, Vector3 playerPoV)
        {
            var neighbourPieceOrNull = KeyBinder.precisionMode == ORDINARY
                ? SnapTree.FindNearestOrdinaryPrecisionSnapNodeTo(playerPoV, neighbourPieces)
                : SnapTree.FindNearestSuperiorPrecisionSnapNodeTo(playerPoV, neighbourPieces);

            if (neighbourPieceOrNull is SnapTree.TraversalResult neighbourPiece)
            {
                var (neighbourPieceExit, perpendicularToPlayerPoV) = DetermineNeighbourPieceExit(neighbourPiece.neighbourSnapNode, neighbourPiece.neighbourPiece);
                if (!buildPiece.IsGroundBound())
                {
                    SnapExternallyHelper(buildPiece, neighbourPieceExit, perpendicularToPlayerPoV);
                }
                else
                {
                    buildPiece.transform.position = neighbourPieceExit;
                }
            }
        }

        private static void SnapExternallyHelper(Piece buildPiece, Vector3 neighbourPieceExit, Vector3 perpendicularToPlayerPoV)
        {
            buildPiece.transform.position = neighbourPieceExit + perpendicularToPlayerPoV * 10;
            var buildPieceExit = DeterminePieceExit(buildPiece, neighbourPieceExit);
        
            SnapPiecesByExits(buildPiece, buildPieceExit, neighbourPieceExit, perpendicularToPlayerPoV);
        }

        private static (Vector3, Vector3) DetermineNeighbourPieceExit(SnapNode neighbourSnapNode, Piece neighbourPiece)
        {
            var pokedNeighbourSnapNode = PokeToMiddle(neighbourSnapNode, neighbourPiece);
            var perpendicularToPlayerPoV = DeterminePerpendicularToPlayerPoVOn(pokedNeighbourSnapNode);
            var microscopicObserver = neighbourSnapNode + perpendicularToPlayerPoV;
            var pokedNeighbourPieceExit = DeterminePieceExit(neighbourPiece, microscopicObserver);

            return (pokedNeighbourPieceExit, perpendicularToPlayerPoV);
        }

        private static Vector3 DeterminePlayerPoV()
        {
            var playerPosition = GameCamera.instance.transform.position;
            var playerPerspective = GameCamera.instance.transform.forward;
            Physics.Raycast(playerPosition, playerPerspective, out var rayHit, PrecisionDrill.DropFromExosphere, LayerMask);
            return rayHit.point;
        }

        private static Vector3 DeterminePerpendicularToPlayerPoVOn(Vector3 snapNode)
        {
            var playerPosition = GameCamera.instance.transform.position;
            var playerPerspectiveOnSnapNode = snapNode - playerPosition;
            Physics.Raycast(playerPosition, playerPerspectiveOnSnapNode, out var rayHit, PrecisionDrill.DropFromExosphere, LayerMask);
            return rayHit.normal;
        }

        private static List<Piece> FindNeighbourPieces(Vector3 playerPoV)
        {
            NeighbourPieces.Clear();
            var numberOfNeighbours = Physics.OverlapSphereNonAlloc(playerPoV, NeighbourhoodSize, NeighbourColliders, PiecesOnly);
            for (var i = 0; i < numberOfNeighbours; i++)
            {
                var neighbourCollider = NeighbourColliders[i];
                var neighbourPiece = neighbourCollider.transform.root?.GetComponentInChildren<Piece>();
                if (neighbourPiece != null && neighbourPiece.Type().IsSnappable())
                {
                    NeighbourPieces.Add(neighbourPiece);
                }
            }

            return NeighbourPieces;
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
            var diff = neighbourPieceEntry - buildPieceExit;
            // We move the piece only on dimensions required to prevent collisions. Otherwise, the piece would lose its center as it does in Vanilla Valheim.
            var normalizedDiff = Vector3.Project(diff, perpendicularToNeighbourPiece);
            buildPiece.transform.position += normalizedDiff;
        }

        // Manifested only in OCDheim.
        private static void FixVanillaValheimBugWithSpinningTerrainModificationVFX()
        {
            buildPiece.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }
}
