using System;
using System.Collections.Generic;
using UnityEngine;

using static OCDheim.PlayerHelpers;

namespace OCDheim
{
    public static class PrecisionDrill
    {
        public const float DropFromExosphere = 250.0f;
        private const int MaxDrillsInMemory = 4096;
        private const float DrillSize = 0.01f;

        private static readonly int GroundLayerMask = LayerMask.GetMask("terrain");
        private static readonly int FloorLayerMask = LayerMask.GetMask("terrain", "piece", "static_solid");

        private static Func<Vector2, Vector2> roundDrillCoords => drillCoords => new Vector2(Mathf.Round(drillCoords.x * 10) / 10, Mathf.Round(drillCoords.y * 10) / 10);
        public static IRefreshableMemoryRepo<Vector2, Vector3> groundLevels => new MemoryRepo<Vector2, Vector3>(DrillDownToGround, MaxDrillsInMemory).EvictionPolicy(TimeSpan.FromMilliseconds(125));
        public static IRefreshableMemoryRepo<Vector2, List<float>> floorLevels => new MemoryRepo<Vector2, List<float>>(drillCoords => DrillDownFloors(drillCoords), roundDrillCoords, MaxDrillsInMemory).EvictionPolicy(TimeSpan.FromMilliseconds(500));

        public static Vector3 DrillDownTillGround(Vector2 drillCoords) => groundLevels.LookUp(drillCoords);

        private static Vector3 DrillDownToGround(Vector2 drillCoords)
        {
            var drillFrom = WhereToDrillFrom(drillCoords);
            if (Physics.Raycast(drillFrom, Vector3.down, out var drillStrike, DropFromExosphere, GroundLayerMask))
            {
                return drillStrike.point;
            }

            return FallBack(drillCoords);
        }

        private static Vector3 WhereToDrillFrom(Vector2 drillCoords) => new Vector3(drillCoords.x, DropFromExosphere, drillCoords.y);

        private static Vector3 FallBack(Vector2 drillCoords)
        {
            Logger.Warn(() => $"[FAILED] Precision Drill for: {drillCoords}");
            return new Vector3(drillCoords.x, player.transform.position.y, drillCoords.y);
        }

        public static Vector3 DrillDownTillFloor(Vector2 drillCoords, float referenceLevel)
        {
            var floors = floorLevels.LookUp(drillCoords);
            var nearestFloor = floors[0];
            var minΔ = Math.Abs(referenceLevel - nearestFloor);

            foreach (var floor in floors)
            {
                var Δ = Math.Abs(referenceLevel - floor);
                if (Δ < minΔ)
                {
                    minΔ = Δ;
                    nearestFloor = floor;
                }
            }

            return new Vector3(drillCoords.x, nearestFloor, drillCoords.y);
        }

        private static List<float> DrillDownFloors(Vector2 drillCoords, float drillTill = DropFromExosphere)
        {
            var drillFrom = WhereToDrillFrom(drillCoords);
            var floors = new List<float>();
            var roof = drillFrom.y;

            while (Physics.SphereCast(drillFrom, DrillSize, Vector3.down, out var drillStrike, drillTill, FloorLayerMask))
            {
                var floor = drillStrike.point.y;
                if (!ShouldSkipDueToInsufficientSize(drillStrike.collider) && !ShouldSkipDueToInsufficientRoom(floor, roof))
                {
                    floors.Add(floor);
                }
                drillFrom.y -= drillStrike.distance + DrillSize / 100;
                drillTill -= drillStrike.distance + DrillSize / 100;
                roof = floor;
            }
            
            return floors;
        }
        
        private static bool ShouldSkipDueToInsufficientSize(Collider collider)
        {
            var piece = collider.transform.root.GetComponentInChildren<Piece>();
            if (piece != null)
            {
                return false;
            }
            
            var bounds = collider.bounds;
            return bounds.max.x - bounds.min.x < 0.5f || bounds.max.z - bounds.min.z < 0.5f;
        }

        private static bool ShouldSkipDueToInsufficientRoom(float floorLevel, float roofLevel)
        {
            return roofLevel - floorLevel < 0.5f; // TODO: Should consider roof - floor vs buildPiece.y
        }
    }
}
