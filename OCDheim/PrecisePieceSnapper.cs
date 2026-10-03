using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
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
            (KeyBinder.snapModeEnabled && PrecisePieceSnapper.SnapModeRequirementsSatisfied()) ||
            KeyBinder.snapModeDisabled;
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Player))]
        [HarmonyPatch(nameof(Player.UpdatePlacementGhost))]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var foundCodeToRemove = false;
            var foundCodeToReplace = false;
            foreach (var instruction in instructions)
            {
                foundCodeToRemove = foundCodeToRemove ? foundCodeToRemove : instruction.opcode == OpCodes.Ldstr && (string)instruction.operand == KeyBinder.SuppressSnapModeKey;
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
    public static class PrecisePieceSnapperBeforePlacementValidator
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Player))]
        [HarmonyPatch(nameof(Player.UpdatePlacementGhost))]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var matcher = new CodeMatcher(instructions).MatchStartForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Player), nameof(Player.m_placementGhost))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(GameObject), nameof(GameObject.transform))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.position))),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Location), nameof(Location.IsInsideNoBuildLocation))));

            if (matcher.IsValid)
            {
                var snapBuildPiece = new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => SnapBuildPiece()));
                matcher.Instruction.MoveLabelsTo(snapBuildPiece);
                matcher.Insert(snapBuildPiece);
            }
            else
            {
                Logger.Warn(() => $"FAILED to find Vanilla Valheim code to transpile in: {original.DeclaringType?.Name}.{original.Name}");
            }

            return matcher.InstructionEnumeration();
        }

        private static void SnapBuildPiece()
        {
            var surfaceOrNull = PrecisePieceSnapper.SnapBuildPiece();
            if (surfaceOrNull is RaycastHit surface && player.HasBuildPieceEquipped())
            {
                player.m_placementStatus = PlacementValidator.Validate(buildPiece, surface);
            }
        }
    }

    public static class PrecisePieceSnapper
    {
        private const float NeighbourhoodSize = 2.5f;
        private const float ClippingTolerance = 0.25f;

        private static readonly int PiecesOnly = UnityEngine.LayerMask.GetMask("piece");
        private static readonly int LayerMask = player.m_placeRayMask - UnityEngine.LayerMask.GetMask("piece_nonsolid");
        private static readonly Collider[] NeighbourColliders = new Collider[byte.MaxValue];
        private static readonly List<Piece> NeighbourPieces = new List<Piece>();

        public static bool GridModeRequirementsSatisfied() => player.HasBuildPieceEquipped() || player.HasOverlayVisible();
        public static bool SnapModeRequirementsSatisfied() => Config.additionalSnapPoints.Value && player.HasBuildPieceEquipped() && (buildPiece.Type() != CONSTRUCTION || KeyBinder.precisionMode == SUPERIOR);
        private static bool ShouldUsePlayerPositionAsGroundLevelReference() => player.HasLevelGroundTerraformToolEquipped() && KeyBinder.snapModeEnabled;

        // if a Build Piece was snapped to a Surface → the Surface
        // if a Build Piece was NOT snapped to a Surface → null
        public static RaycastHit? SnapBuildPiece()
        {
            if (KeyBinder.gridModeEnabled && GridModeRequirementsSatisfied())
            {
                return SnapToWorldGrid(buildPiece);
            }
            if (KeyBinder.snapModeEnabled && SnapModeRequirementsSatisfied())
            {
                return SnapToNeighbourPiece(buildPiece);
            }

            return null;
        }

        private static RaycastHit? SnapToWorldGrid(Piece buildPiece)
        {
            var playerPoV = DeterminePlayerPoV();
            var precision = (int)KeyBinder.precisionMode;
            var (xOnGrid, zOnGrid) = SnapToWorldGrid(playerPoV, precision);

            if (player.HasOverlayVisible())
            {
                FixVanillaValheimBugWithSpinningTerrainModificationVFX();
            }

            if (ShouldUsePlayerPositionAsGroundLevelReference())
            {
                buildPiece.transform.position = new Vector3(xOnGrid, playerPos.y, zOnGrid);
            }
            else
            {
                var drillCoords = new Vector2(xOnGrid, zOnGrid);
                var floor = player.HasOverlayVisible() || buildPiece.IsGroundBound()
                    ? PrecisionDrill.DrillDownTillGround(drillCoords)
                    : PrecisionDrill.DrillDownTillFloor(drillCoords, DetermineReferenceLevel(buildPiece), DetermineRequiredRoom(buildPiece));

                var posOnGrid = new Vector3(xOnGrid, floor.level, zOnGrid);
                if (!buildPiece.ClipsIntoBuildPieces() && !(buildPiece.ClipsIntoGround() && floor.isGround))
                {
                    SnapExternally(buildPiece, posOnGrid, Vector3.up);
                }
                else
                {
                    buildPiece.transform.position = posOnGrid;
                }

                return floor.drillStrike;
            }

            return null;
        }

        private static float DetermineReferenceLevel(Piece buildPiece) => buildPiece.ClipsIntoBuildPieces()
            ? buildPiece.transform.position.y
            : buildPiece.BottomLevel();

        private static float DetermineRequiredRoom(Piece buildPiece) => buildPiece.ClipsIntoBuildPieces() ? 0.0f
            : Mathf.Max(0.0f, buildPiece.TopLevel() - buildPiece.BottomLevel() - ClippingTolerance);

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

        private static RaycastHit? SnapToNeighbourPiece(Piece buildPiece)
        {
            var playerPoV = DeterminePlayerPoV();
            var neighbourPieces = FindNeighbourPieces(playerPoV);
            switch (buildPiece.Type())
            {
                case CONSTRUCTION:
                    return SnapInternally(buildPiece, neighbourPieces);
                case FURNITURE:
                case TABLE:
                    return SnapExternally(buildPiece, neighbourPieces, playerPoV);
                default:
                    return null;
            }
        }

        private static RaycastHit? SnapInternally(Piece buildPiece, List<Piece> neighbourPieces)
        {
            var snapNodeCoupleOrNull = KeyBinder.precisionMode == ORDINARY
                ? SnapTree.FindNearestOrdinaryPrecisionSnapNodeCombinationOf(buildPiece, neighbourPieces) // TODO: This is dead code ATM
                : SnapTree.FindNearestSuperiorPrecisionSnapNodeCombinationOf(buildPiece, neighbourPieces);

            if (snapNodeCoupleOrNull is SnapTree.TraversalResult snapNodeCouple)
            {
                buildPiece.transform.position += snapNodeCouple.neighbourSnapNode - snapNodeCouple.buildPieceSnapNode;
                return DeterminePlayerPoVOn(snapNodeCouple.neighbourSnapNode, snapNodeCouple.neighbourPiece);
            }

            return null;
        }

        private static RaycastHit? SnapExternally(Piece buildPiece, List<Piece> neighbourPieces, Vector3 playerPoV)
        {
            var neighbourPieceOrNull = KeyBinder.precisionMode == ORDINARY
                ? SnapTree.FindNearestOrdinaryPrecisionSnapNodeTo(playerPoV, neighbourPieces)
                : SnapTree.FindNearestSuperiorPrecisionSnapNodeTo(playerPoV, neighbourPieces);

            if (neighbourPieceOrNull is SnapTree.TraversalResult neighbourPiece && DeterminePlayerPoVOn(neighbourPiece.neighbourSnapNode, neighbourPiece.neighbourPiece) is RaycastHit playerPoVOnNeighbourPiece)
            {
                var microscopicObserver = neighbourPiece.neighbourSnapNode + playerPoVOnNeighbourPiece.normal;
                var neighbourPieceExit = neighbourPiece.neighbourPiece.ExitTo(microscopicObserver);
                if (!buildPiece.ClipsIntoBuildPieces())
                {
                    SnapExternally(buildPiece, neighbourPieceExit, playerPoVOnNeighbourPiece.normal);
                }
                else
                {
                    buildPiece.transform.position = neighbourPieceExit;
                }

                return playerPoVOnNeighbourPiece;
            }

            return null;
        }

        private static void SnapExternally(Piece buildPiece, Vector3 neighbourPieceExit, Vector3 perpendicularToPlayerPoV)
        {
            buildPiece.transform.position = neighbourPieceExit + perpendicularToPlayerPoV * 10;
            var buildPieceExit = buildPiece.ExitTo(neighbourPieceExit);

            SnapPiecesByExits(buildPiece, buildPieceExit, neighbourPieceExit, perpendicularToPlayerPoV);
        }

        private static Vector3 DeterminePlayerPoV()
        {
            var playerPosition = GameCamera.instance.transform.position;
            var playerPerspective = GameCamera.instance.transform.forward;
            var found = Physics.Raycast(playerPosition, playerPerspective, out var rayHit, PrecisionDrill.DropFromExosphere, LayerMask);

            return found ? rayHit.point : buildPiece.transform.position;
        }

        private static RaycastHit? DeterminePlayerPoVOn(Vector3 neighbourSnapNode, Piece neighbourPiece)
        {
            var playerPosition = GameCamera.instance.transform.position;
            var pokedNeighbourSnapNode = PokeToMiddle(neighbourSnapNode, neighbourPiece);
            var playerPerspectiveOnSnapNode = pokedNeighbourSnapNode - playerPosition;
            var found = Physics.Raycast(playerPosition, playerPerspectiveOnSnapNode, out var rayHit, PrecisionDrill.DropFromExosphere, LayerMask);

            return found ? rayHit : (RaycastHit?) null;
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
