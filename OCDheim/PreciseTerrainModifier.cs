using HarmonyLib;
using UnityEngine;

namespace OCDheim
{
    [HarmonyPatch(typeof(TerrainComp), "InternalDoOperation")]
    public static class PreciseTerrainModifier
    {
        public const int SizeInTiles = 1;

        private static bool Prefix(Vector3 pos, TerrainOp.Settings modifier, Heightmap ___m_hmap, int ___m_width, ref float[] ___m_levelDelta, ref float[] ___m_smoothDelta, ref Color[] ___m_paintMask, ref bool[] ___m_modifiedHeight, ref bool[] ___m_modifiedPaint)
        {
            if (!modifier.m_level && !modifier.m_raise && !modifier.m_smooth && !modifier.m_paintCleared)
            {
                RemoveTerrainModifications(pos, ___m_hmap, ___m_width, ref ___m_levelDelta, ref ___m_smoothDelta, ref ___m_modifiedHeight);
                RecolorTerrain(pos, TerrainModifier.PaintType.Reset, ___m_hmap, ___m_width, ref ___m_paintMask, ref ___m_modifiedPaint);
            }
            return true;
        }

        public static void SmoothenTerrain(Vector3 worldPos, Heightmap hMap, TerrainComp compiler, int worldWidth, ref float[] smoothΔ, ref bool[] modifiedHeight)
        {
            Debug.Log("[INIT] Smooth Terrain Modification");

            var worldSize = worldWidth + 1;
            hMap.WorldToVertex(worldPos, out var xPos, out var yPos);
            var referenceH = worldPos.y - compiler.transform.position.y;
            Debug.Log($"worldPos: {worldPos}, xPos: {xPos}, yPos: {yPos}, referenceH: {referenceH}");

            FindExtremums(xPos, worldSize, out var xMin, out var xMax);
            FindExtremums(yPos, worldSize, out var yMin, out var yMax);
            for (var x = xMin; x <= xMax; x++)
            {
                for (var y = yMin; y <= yMax; y++)
                {
                    var tileIndex = y * worldSize + x;
                    var tileH = hMap.GetHeight(x, y);
                    var Δh = referenceH - tileH;
                    var oldΔh = smoothΔ[tileIndex];
                    var newΔh = oldΔh + Δh;
                    var roundedNewΔh = RoundToTwoDecimals(tileH, oldΔh, newΔh);
                    var limΔh = Mathf.Clamp(roundedNewΔh, -1.0f, 1.0f);
                    smoothΔ[tileIndex] = limΔh;
                    modifiedHeight[tileIndex] = true;
                    Debug.Log($"tilePos: ({x}, {y}), tileH: {tileH}, Δh: {Δh}, oldΔh: {oldΔh}, newΔh: {newΔh}, roundedNewΔh: {roundedNewΔh}, limΔh: {limΔh}");
                }
            }
            Debug.Log("[SUCCESS] Smooth Terrain Modification");
        }

        public static void RaiseTerrain(Vector3 worldPos, Heightmap hMap, TerrainComp compiler, int worldWidth, float power, ref float[] levelΔ, ref float[] smoothΔ, ref bool[] modifiedHeight)
        {
            Debug.Log("[INIT] Raise Terrain Modification");

            var worldSize = worldWidth + 1;
            hMap.WorldToVertex(worldPos, out var xPos, out var yPos);
            var referenceH = worldPos.y - compiler.transform.position.y + power;
            Debug.Log($"worldPos: {worldPos}, xPos: {xPos}, yPos: {yPos}, power: {power}, referenceH: {referenceH}");

            FindExtremums(xPos, worldSize, out var xMin, out var xMax);
            FindExtremums(yPos, worldSize, out var yMin, out var yMax);
            for (var x = xMin; x <= xMax; x++)
            {
                for (var y = yMin; y <= yMax; y++)
                {
                    var tileIndex = y * worldSize + x;
                    var tileH = hMap.GetHeight(x, y);
                    var Δh = referenceH - tileH;
                    if (Δh >= 0)
                    {
                        var oldLevelΔ = levelΔ[tileIndex];
                        var oldSmoothΔ = smoothΔ[tileIndex];
                        var newLevelΔ = oldLevelΔ + oldSmoothΔ + Δh;
                        var newSmoothΔ = 0f;
                        var roundedNewLevelΔ = RoundToTwoDecimals(tileH, oldLevelΔ + oldSmoothΔ, newLevelΔ + newSmoothΔ);
                        var limitedNewLevelΔ = Mathf.Clamp(roundedNewLevelΔ, -16.0f, 16.0f);
                        levelΔ[tileIndex] = limitedNewLevelΔ;
                        smoothΔ[tileIndex] = newSmoothΔ;
                        modifiedHeight[tileIndex] = true;
                        Debug.Log($"tilePos: ({x}, {y}), tileH: {tileH}, Δh: {Δh}, oldLevelΔ: {oldLevelΔ}, oldSmoothΔ: {oldSmoothΔ}, newLevelΔ: {newLevelΔ}, newSmoothΔ: {newSmoothΔ}, roundedNewLevelΔ: {roundedNewLevelΔ}, limitedNewLevelΔ: {limitedNewLevelΔ}");
                    }
                    else
                    {
                        Debug.Log("Declined to process tile: Δh < 0!");
                        Debug.Log($"tilePos: ({x}, {y}), tileH: {tileH}, Δh: {Δh}");
                    }
                }
            }

            Debug.Log("[SUCCESS] Raise Terrain Modification");
        }

        public static void RecolorTerrain(Vector3 worldPos, TerrainModifier.PaintType paintType, Heightmap hMap, int worldWidth, ref Color[] paintMask, ref bool[] modifiedPaint)
        {
            Debug.Log("[INIT] Color Terrain Modification");
            worldPos -= new Vector3(0.5f, 0, 0.5f);
            hMap.WorldToVertex(worldPos, out var xPos, out var yPos);
            Debug.Log($"worldPos: {worldPos}, vertexPos: ({xPos}, {yPos})");

            var tileColor = ResolveColor(paintType);
            var removeColor = tileColor == Color.black;
            FindExtremums(xPos, worldWidth, out var xMin, out var xMax);
            FindExtremums(yPos, worldWidth, out var yMin, out var yMax);
            for (var x = xMin; x <= xMax; x++)
            {
                for (var y = yMin; y <= yMax; y++)
                {
                    var tileIndex = y * worldWidth + x;
                    paintMask[tileIndex] = tileColor;
                    modifiedPaint[tileIndex] = !removeColor;
                    Debug.Log($"tilePos: ({x}, {y}), tileIndex: {tileIndex}, tileColor: {tileColor}");
                }
            }
            Debug.Log("[SUCCESS] Color Terrain Modification");
        }

        public static void RemoveTerrainModifications(Vector3 worldPos, Heightmap hMap, int worldWidth, ref float[] levelΔ, ref float[] smoothΔ, ref bool[] modifiedHeight)
        {
            Debug.Log("[INIT] Remove Terrain Modifications");

            var worldSize = worldWidth + 1;
            hMap.WorldToVertex(worldPos, out var xPos, out var yPos);
            Debug.Log($"worldPos: {worldPos}, vertexPos: ({xPos}, {yPos})");

            FindExtremums(xPos, worldSize, out var xMin, out var xMax);
            FindExtremums(yPos, worldSize, out var yMin, out var yMax);
            for (var x = xMin; x <= xMax; x++)
            {
                for (var y = yMin; y <= yMax; y++)
                {
                    var tileIndex = y * worldSize + x;
                    levelΔ[tileIndex] = 0;
                    smoothΔ[tileIndex] = 0;
                    modifiedHeight[tileIndex] = false;
                    Debug.Log($"tilePos: ({x}, {y}), tileIndex: {tileIndex}");
                }
            }
            Debug.Log("[SUCCESS] Remove Terrain Modifications");
        }

        public static Color ResolveColor(TerrainModifier.PaintType paintType)
        {
            if (paintType == TerrainModifier.PaintType.Dirt) { return Color.red; }
            if (paintType == TerrainModifier.PaintType.Paved) { return Color.blue; }
            if (paintType == TerrainModifier.PaintType.Cultivate) { return Color.green; }
            return Color.black;
        }

        public static void FindExtremums(int x, int worldSize, out int xMin, out int xMax)
        {
            xMin = Mathf.Max(0, x - SizeInTiles);
            xMax = Mathf.Min(x + SizeInTiles, worldSize - 1);
        }

        public static float RoundToTwoDecimals(float oldH, float oldΔh, float newΔh)
        {
            var newH = oldH - oldΔh + newΔh;
            var roundedNewH = Mathf.Round(newH * 100) / 100;
            var roundedNewΔh = roundedNewH - oldH + oldΔh;
            Debug.Log($"oldH: {oldH}, oldΔH: {oldΔh}, newΔH: {newΔh}, newH: {newH}, roundedNewH: {roundedNewH}, roundedNewΔh: {roundedNewΔh}");
            return roundedNewΔh;
        }
    }

    [HarmonyPatch(typeof(TerrainComp), "SmoothTerrain")]
    public static class PreciseSmoothTerrainModification
    {
        private static bool Prefix(Vector3 worldPos, float radius, bool square, float power, TerrainComp __instance, Heightmap ___m_hmap, int ___m_width, ref float[] ___m_smoothDelta, ref bool[] ___m_modifiedHeight)
        {
            if (ClientSideGridModeOverride.IsGridModeEnabled(radius))
            {
                PreciseTerrainModifier.SmoothenTerrain(worldPos, ___m_hmap, __instance, ___m_width, ref ___m_smoothDelta, ref ___m_modifiedHeight);
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(TerrainComp), "RaiseTerrain")]
    public static class PreciseRaiseTerrainModification
    {
        private static bool Prefix(Vector3 worldPos, float radius, float delta, bool square, float power, TerrainComp __instance, Heightmap ___m_hmap, int ___m_width, ref float[] ___m_levelDelta, ref float[] ___m_smoothDelta, ref bool[] ___m_modifiedHeight)
        {
            if (ClientSideGridModeOverride.IsGridModeEnabled(radius))
            {
                PreciseTerrainModifier.RaiseTerrain(worldPos, ___m_hmap, __instance, ___m_width, delta, ref ___m_levelDelta, ref ___m_smoothDelta, ref ___m_modifiedHeight);
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(TerrainComp), "PaintCleared")]
    public static class PreciseColorTerrainModification
    {
        private static bool Prefix(Vector3 worldPos, float radius, TerrainModifier.PaintType paintType, bool heightCheck, bool apply, Heightmap ___m_hmap, int ___m_width, ref Color[] ___m_paintMask, ref bool[] ___m_modifiedPaint)
        {
            if (ClientSideGridModeOverride.IsGridModeEnabled(radius))
            {
                PreciseTerrainModifier.RecolorTerrain(worldPos, paintType, ___m_hmap, ___m_width, ref ___m_paintMask, ref ___m_modifiedPaint);
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    // DIRTY HACK: bend the flow to our will with a hijacked "unused" variable. Thus is the life of the modder ;)
    [HarmonyPatch(typeof(TerrainComp), "ApplyOperation")]
    public static class ClientSideGridModeOverride
    {
        private static bool Prefix(TerrainOp modifier)
        {
            if (Keybindings.GridModeEnabled)
            {
                if (modifier.m_settings.m_smooth)
                {
                    modifier.m_settings.m_smoothRadius = float.NegativeInfinity;
                }
                if (modifier.m_settings.m_raise && modifier.m_settings.m_raiseDelta >= 0)
                {
                    modifier.m_settings.m_raiseRadius = float.NegativeInfinity;
                    modifier.m_settings.m_raiseDelta = GroundLevelSpinner.value;
                }
                if (modifier.m_settings.m_paintCleared)
                {
                    modifier.m_settings.m_paintRadius = float.NegativeInfinity;
                }
            }
            return true;
        }

        // DIRTY HACK: This is surely how I will be remembered ;)
        public static bool IsGridModeEnabled(float radius)
        {
            return radius == float.NegativeInfinity;
        }
    }
}
