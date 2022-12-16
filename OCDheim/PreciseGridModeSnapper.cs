using UnityEngine;

namespace OCDheim
{
    public static class PreciseGridModeSnapper
    {
        private const string LevelGroundToolName = "mud_road_v2";
        private const float MaxDrill = 5.0f;

        private static bool ShouldUsePlayerPositionAsGroundLevelReference(Piece piece) => piece.name == LevelGroundToolName && Keybindings.SnapModeEnabled;
        private static bool ShouldUseDoublePrecision(ItemDrop.ItemData tool) => Keybindings.PrecisionModeEnabled && !tool.IsHoe();

        public static void SnapToGroundGrid(ItemDrop.ItemData tool, Piece piece, int layerMask)
        {
            var precision = ShouldUseDoublePrecision(tool) ? 2 : 1;
            var xOnGrid = Mathf.Round(piece.transform.position.x * precision) / precision;
            var zOnGrid = Mathf.Round(piece.transform.position.z * precision) / precision;
            if (ShouldUsePlayerPositionAsGroundLevelReference(piece))
            {
                piece.transform.position = new Vector3(xOnGrid, piece.transform.position.y, zOnGrid);
            }
            else
            {
                var preciseGroundPosition = DeterminePreciseGroundPosition(piece, xOnGrid, zOnGrid, layerMask);
                if (preciseGroundPosition.HasValue)
                {
                    PreciseBuildPieceSnapper.SnapExternally(piece, preciseGroundPosition.Value, Vector3.up);
                }
                else
                {
                    piece.gameObject.SetActive(false);
                }
            }

            if (tool.IsTerrainModificationTool())
            {
                FixVanillaValheimBugWithSpinningTerrainModificationVFX(piece);
            }
        }

        private static Vector3? DeterminePreciseGroundPosition(Piece piece, float xOnGrid, float zOnGrid, int layerMask)
        {
            var groundPositionEstimation = new Vector3(xOnGrid, piece.transform.position.y, zOnGrid);
            var elevatedGroundPoistionEstimation = groundPositionEstimation + new Vector3(0, MaxDrill / 2, 0);
            return RecursiveDrillDown(groundPositionEstimation, elevatedGroundPoistionEstimation, MaxDrill, layerMask);
        }

        private static Vector3? RecursiveDrillDown(Vector3 referencePosition, Vector3 drillFrom, float drillTill, int layerMask)
        {
            if (Physics.Raycast(drillFrom, Vector3.down, out var rayHit, drillTill, layerMask))
            {
                var floorPosition = rayHit.point;
                var remainingDrillTill = drillTill - Vector3.Distance(drillFrom, floorPosition);
                if (drillTill - remainingDrillTill > 0.001f) // Otherwise nowhere else to drill, we're done.
                {
                    var lowerFloorPosition = RecursiveDrillDown(referencePosition, floorPosition, remainingDrillTill, layerMask);
                    if (lowerFloorPosition.HasValue)
                    {
                        if (Vector3.Distance(referencePosition, floorPosition) > Vector3.Distance(referencePosition, lowerFloorPosition.Value))
                        {
                            return lowerFloorPosition;
                        }
                        else { return floorPosition; }
                    }
                    else { return floorPosition; }
                }
                else { return null; }
            }
            else { return null; }
        }

        // Manifested only in OCDheim.
        private static void FixVanillaValheimBugWithSpinningTerrainModificationVFX(Piece piece)
        {
            piece.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }
}
