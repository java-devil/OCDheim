using System;
using System.Collections.Generic;
using UnityEngine;

using static OCDheim.PlayerHelpers;
using static OCDheim.PrecisionDrill.Floor;

namespace OCDheim
{
    public static class PrecisionDrill
    {
        public const float DropFromExosphere = 250.0f;
        private const float DrillSize = 0.01f;

        private static readonly int GroundLayerMask = LayerMask.GetMask("terrain");
        private static readonly int FloorLayerMask = LayerMask.GetMask("terrain", "piece", "static_solid");

        public static Floor DrillDownTillGround(Vector2 drillCoords)
        {
            var drillFrom = WhereToDrillFrom(drillCoords);
            return Physics.Raycast(drillFrom, Vector3.down, out var drillStrike, DropFromExosphere, GroundLayerMask)
                ? new Floor(drillStrike)
                : FallBack(drillCoords);
        }

        private static Vector3 WhereToDrillFrom(Vector2 drillCoords) => new Vector3(drillCoords.x, DropFromExosphere, drillCoords.y);

        public static Floor DrillDownTillFloor(Vector2 drillCoords, float referenceLevel, float requiredRoom)
        {
            var floors = DrillDownFloors(drillCoords, requiredRoom);
            if (floors.Count == 0)
            {
                return DrillDownTillGround(drillCoords);
            }

            var nearestFloor = floors[0];
            var minΔ = Math.Abs(referenceLevel - nearestFloor.level);

            foreach (var floor in floors)
            {
                var Δ = Math.Abs(referenceLevel - floor.level);
                if (Δ < minΔ)
                {
                    minΔ = Δ;
                    nearestFloor = floor;
                }
            }

            return nearestFloor;
        }

        private static List<Floor> DrillDownFloors(Vector2 drillCoords, float requiredRoom, float drillTill = DropFromExosphere)
        {
            var drillFrom = WhereToDrillFrom(drillCoords);
            var roofLevel = drillFrom.y;
            var floors = new List<Floor>();

            while (Physics.SphereCast(drillFrom, DrillSize, Vector3.down, out var drillStrike, drillTill, FloorLayerMask))
            {
                var floorLevel = drillStrike.point.y;
                if (!ShouldSkipDueToInsufficientSize(drillStrike.collider) && !ShouldSkipDueToInsufficientRoom(floorLevel, roofLevel, requiredRoom))
                {
                    var floor = new Floor(drillStrike);
                    floors.Add(floor);
                }
                var floorUnderside = UndersideOf(drillStrike.collider, drillCoords, floorLevel) ?? floorLevel;
                drillFrom.y -= drillStrike.distance + DrillSize / 100;
                drillTill -= drillStrike.distance + DrillSize / 100;
                roofLevel = Math.Min(roofLevel, floorUnderside);
            }

            return floors;
        }

        private static float? UndersideOf(Collider collider, Vector2 drillCoords, float topLevel)
        {
            var drillFrom = new Vector3(drillCoords.x, topLevel - DropFromExosphere, drillCoords.y);
            var ray = new Ray(drillFrom, Vector3.up);
            return collider.Raycast(ray, out var drillStrike, DropFromExosphere)
                ? drillStrike.point.y
                : (float?) null;
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

        private static bool ShouldSkipDueToInsufficientRoom(float floorLevel, float roofLevel, float requiredRoom)
        {
            var room = roofLevel - floorLevel;
            return room <= 0.0f || room < requiredRoom;
        }

        public readonly struct Floor
        {
            public float level { get; }
            public bool isGround { get; }
            public RaycastHit? drillStrike { get; }

            public Floor(RaycastHit drillStrike) : this(drillStrike.point.y, IsGround(drillStrike.collider), drillStrike) {}

            private Floor(float level, bool isGround, RaycastHit? drillStrike)
            {
                this.level = level;
                this.isGround = isGround;
                this.drillStrike = drillStrike;
            }

            public static Floor FallBack(Vector2 drillCoords)
            {
                Logger.Warn(() => $"[FAILED] Precision Drill for: {drillCoords}");
                return new Floor(player.transform.position.y, true, null);
            }

            private static bool IsGround(Collider collider) => (GroundLayerMask & (1 << collider.gameObject.layer)) != 0;
        }
    }
}
