using UnityEngine;

using static OCDheim.PlayerHelpers;
using static Player.PlacementStatus;

namespace OCDheim
{
    public static class PlacementValidator
    {
        private static readonly int PiecesOnly = LayerMask.GetMask("piece");
        private static readonly int LiquidOnly = LayerMask.GetMask("Water");

        public static Player.PlacementStatus Validate(Piece buildPiece, RaycastHit surface)
        {
            var status = Valid;
            var point = surface.point;
            var normal = surface.normal;
            var piece = surface.collider.GetComponentInParent<Piece>();
            var heightmap = surface.collider.GetComponent<Heightmap>();
            var wearNTear = piece ? piece.GetComponent<WearNTear>() : null;

            var stationExtension = buildPiece.GetComponent<StationExtension>();
            if (stationExtension)
            {
                var craftingStation = stationExtension.FindClosestStationInRange(point);
                if (craftingStation)
                {
                    stationExtension.StartConnectionEffect(craftingStation);
                }
                else
                {
                    stationExtension.StopConnectionEffect();
                    status = ExtensionMissingStation;
                }
                if (stationExtension.OtherExtensionInRange(buildPiece.m_spaceRequirement))
                {
                    status = MoreSpace;
                }
            }
            if (buildPiece.m_blockRadius > 0.0f && IsBlocked(buildPiece, point))
            {
                status = MoreSpace;
            }
            if (wearNTear && !wearNTear.m_supports)
            {
                status = Invalid;
            }
            if (buildPiece.m_waterPiece && !IsUnderWater(point))
            {
                status = Invalid;
            }
            if (buildPiece.m_noInWater && IsUnderWater(point))
            {
                status = Invalid;
            }
            if (buildPiece.m_groundOnly && !heightmap)
            {
                status = Invalid;
            }
            if (buildPiece.m_cultivatedGroundOnly && (!heightmap || !heightmap.IsCultivated(point)))
            {
                status = NeedCultivated;
            }
            if (buildPiece.m_notOnWood && wearNTear && (wearNTear.m_materialType == WearNTear.MaterialType.Wood || wearNTear.m_materialType == WearNTear.MaterialType.HardWood))
            {
                status = Invalid;
            }
            if (buildPiece.m_notOnTiltingSurface && normal.y < 0.8f)
            {
                status = Invalid;
            }
            if (buildPiece.m_inCeilingOnly && normal.y > -0.5f)
            {
                status = Invalid;
            }
            if (buildPiece.m_notOnFloor && normal.y > 0.1f)
            {
                status = Invalid;
            }
            if (!buildPiece.m_allowedInDungeons && player.InInterior() && !EnvMan.instance.CheckInteriorBuildingOverride() && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.DungeonBuild))
            {
                status = NotInDungeon;
            }
            if (player.m_currentBiome == Heightmap.Biome.DeepNorth && heightmap && heightmap.GetCultivationMask(point) > player.m_deepSnowBuildHeight)
            {
                status = DeepSnow;
            }

            return status;
        }

        private static bool IsUnderWater(Vector3 point)
        {
            var probeFrom = point + Vector3.up * PrecisionDrill.DropFromExosphere;
            return Physics.Raycast(probeFrom, Vector3.down, PrecisionDrill.DropFromExosphere, LiquidOnly);
        }

        private static bool IsBlocked(Piece buildPiece, Vector3 point)
        {
            foreach (var collider in Physics.OverlapSphere(point, buildPiece.m_blockRadius, PiecesOnly))
            {
                var neighbourPiece = collider.GetComponentInParent<Piece>();
                if (neighbourPiece && buildPiece.m_blockingPieces.Exists(blockingPiece => blockingPiece.m_name == neighbourPiece.m_name))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
