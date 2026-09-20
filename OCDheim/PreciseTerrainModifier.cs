using HarmonyLib;
using UnityEngine;

using static OCDheim.GroundLevelSpinner;
using static TerrainModifier;

namespace OCDheim
{
    [HarmonyPatch]
    public static class PreciseTerrainModifier
    {
        private const int AoESize = 1;
        public const int HTilesPerChunk = 64;
        private const int PTilesPerChunk = 65;
        public const float HalfPTilesPerChunk = HTilesPerChunk * 0.5f;
        public const float PTileSize = HTilesPerChunk / (float)PTilesPerChunk;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TerrainComp))]
        [HarmonyPatch(nameof(TerrainComp.InternalDoOperation))]
        private static bool Prefix(Vector3 pos, TerrainOp.Settings modifier, Heightmap ___m_hmap, ref float[] ___m_levelDelta, ref float[] ___m_smoothDelta, ref Color[] ___m_paintMask, ref bool[] ___m_modifiedHeight, ref bool[] ___m_modifiedPaint)
        {
            if (!modifier.m_level && !modifier.m_raise && !modifier.m_smooth && !modifier.m_paintCleared)
            {
                RemoveTerrainModifications(pos, ___m_hmap, ref ___m_levelDelta, ref ___m_smoothDelta, ref ___m_modifiedHeight);
                RecolorTerrain(pos, PaintType.Reset, ___m_hmap, ref ___m_paintMask, ref ___m_modifiedPaint, removeColor: true);
            }
            return true;
        }

        public static void SmoothenTerrain(Vector3 worldPos, Heightmap hMap, TerrainComp compiler, ref float[] smoothΔ, ref bool[] modifiedHeight)
        {
            Logger.Debug(() => "[INIT] Smooth Terrain Modification");

            hMap.WorldToVertex(worldPos, out var xPos, out var yPos);
            var referenceH = worldPos.y - compiler.transform.position.y;
            Logger.Debug(() => $"worldPos: {worldPos}, xPos: {xPos}, yPos: {yPos}, referenceH: {referenceH}");

            FindExtremums(xPos, out var xMin, out var xMax);
            FindExtremums(yPos, out var yMin, out var yMax);
            for (var x = xMin; x <= xMax; x++)
            {
                for (var y = yMin; y <= yMax; y++)
                {
                    var tileIndex = y * PTilesPerChunk + x;
                    var tileH = hMap.GetHeight(x, y);
                    var Δh = referenceH - tileH;
                    var oldΔh = smoothΔ[tileIndex];
                    var newΔh = oldΔh + Δh;
                    var roundedNewΔh = RoundToTwoDecimals(tileH, oldΔh, newΔh);
                    var limΔh = Mathf.Clamp(roundedNewΔh, -1.0f, 1.0f);
                    smoothΔ[tileIndex] = limΔh;
                    modifiedHeight[tileIndex] = true;
                    Logger.Debug(() => $"tilePos: ({x}, {y}), tileH: {tileH}, Δh: {Δh}, oldΔh: {oldΔh}, newΔh: {newΔh}, roundedNewΔh: {roundedNewΔh}, limΔh: {limΔh}");
                }
            }

            Logger.Debug(() => "[SUCCESS] Smooth Terrain Modification");
        }

        public static void RaiseTerrain(Vector3 worldPos, Heightmap hMap, TerrainComp compiler, float power, ref float[] levelΔ, ref float[] smoothΔ, ref bool[] modifiedHeight)
        {
            Logger.Debug(() => "[INIT] Raise Terrain Modification");

            hMap.WorldToVertex(worldPos, out var xPos, out var yPos);
            var referenceH = worldPos.y - compiler.transform.position.y + power;
            Logger.Debug(() => $"worldPos: {worldPos}, xPos: {xPos}, yPos: {yPos}, power: {power}, referenceH: {referenceH}");

            FindExtremums(xPos, out var xMin, out var xMax);
            FindExtremums(yPos, out var yMin, out var yMax);
            for (var x = xMin; x <= xMax; x++)
            {
                for (var y = yMin; y <= yMax; y++)
                {
                    var tileIndex = y * PTilesPerChunk + x;
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
                        Logger.Debug(() => $"tilePos: ({x}, {y}), tileH: {tileH}, Δh: {Δh}, oldLevelΔ: {oldLevelΔ}, oldSmoothΔ: {oldSmoothΔ}, newLevelΔ: {newLevelΔ}, newSmoothΔ: {newSmoothΔ}, roundedNewLevelΔ: {roundedNewLevelΔ}, limitedNewLevelΔ: {limitedNewLevelΔ}");
                    }
                    else
                    {
                        Logger.Debug(() => "Declined to process tile: Δh < 0!");
                        Logger.Debug(() => $"tilePos: ({x}, {y}), tileH: {tileH}, Δh: {Δh}");
                    }
                }
            }

            Logger.Debug(() => "[SUCCESS] Raise Terrain Modification");
        }

        public static void RecolorTerrain(Vector3 worldPos, PaintType paintType, Heightmap hMap, ref Color[] paintMask, ref bool[] modifiedPaint, bool removeColor = false)
        {
            Logger.Info(() => "[INIT] Color Terrain Modification");

            var tileColor = ResolveColor(paintType);
            PositionRelativeTo(hMap.transform.position, worldPos, out var xPos, out var yPos);
            Logger.Info(() => $"worldPos: {worldPos}, chunkPos: {hMap.transform.position}, relPos: ({xPos}, {yPos})");

            FindExtremums(xPos, out var xMin, out var xMax);
            FindExtremums(yPos, out var yMin, out var yMax);
            for (var x = xMin; x <= xMax; x++)
            {
                for (var y = yMin; y <= yMax; y++)
                {
                    tileColor.a = ResolveAlpha(hMap, x, y);
                    ApplyColor(x, y, tileColor, ref paintMask, ref modifiedPaint, removeColor);
                }
            }

            Logger.Info(() => "[SUCCESS] Color Terrain Modification");
        }

        private static void RemoveTerrainModifications(Vector3 worldPos, Heightmap hMap, ref float[] levelΔ, ref float[] smoothΔ, ref bool[] modifiedHeight)
        {
            Logger.Debug(() => "[INIT] Remove Terrain Modifications");
            
            hMap.WorldToVertex(worldPos, out var xPos, out var yPos);
            Logger.Debug(() => $"worldPos: {worldPos}, vertexPos: ({xPos}, {yPos})");

            FindExtremums(xPos, out var xMin, out var xMax);
            FindExtremums(yPos, out var yMin, out var yMax);
            for (var x = xMin; x <= xMax; x++)
            {
                for (var y = yMin; y <= yMax; y++)
                {
                    var tileIndex = y * PTilesPerChunk + x;
                    levelΔ[tileIndex] = 0;
                    smoothΔ[tileIndex] = 0;
                    modifiedHeight[tileIndex] = false;
                    Logger.Debug(() => $"tilePos: ({x}, {y}), tileIndex: {tileIndex}");
                }
            }
            Logger.Debug(() => "[SUCCESS] Remove Terrain Modifications");
        }

        private static void FindExtremums(int val, out int minVal, out int maxVal)
        {
            minVal = Mathf.Max(0, val - AoESize);
            maxVal = Mathf.Min(val + AoESize, HTilesPerChunk);
        }

        private static float RoundToTwoDecimals(float oldH, float oldΔh, float newΔh)
        {
            var newH = oldH - oldΔh + newΔh;
            var roundedNewH = Mathf.Round(newH * 100) / 100;
            var roundedNewΔh = roundedNewH - oldH + oldΔh;
            Logger.Debug(() => $"oldH: {oldH}, oldΔH: {oldΔh}, newΔH: {newΔh}, newH: {newH}, roundedNewH: {roundedNewH}, roundedNewΔh: {roundedNewΔh}");

            return roundedNewΔh;
        }

        private static void PositionRelativeTo(Vector3 chunkMid, Vector3 worldPos, out int x, out int y)
        {
            var chunkMin = chunkMid - new Vector3(HalfPTilesPerChunk, 0.0f, HalfPTilesPerChunk);
            var relPos = worldPos - chunkMin;
            x = Mathf.FloorToInt(relPos.x / PTileSize);
            y = Mathf.FloorToInt(relPos.z / PTileSize);
        }

        private static float ResolveAlpha(Heightmap hMap, int x, int y) => hMap.GetPaintMask(x, y).a;

        private static Color ResolveColor(PaintType paintType)
        {
            switch (paintType)
            {
                case PaintType.Dirt:
                    return Heightmap.m_paintMaskDirt;
                case PaintType.Paved:
                    return Heightmap.m_paintMaskPaved;
                case PaintType.Cultivate:
                    return Heightmap.m_paintMaskCultivated;
                case PaintType.ClearVegetation:
                    return Heightmap.m_paintMaskClearVegetation;
                case PaintType.DeepSnow:
                    return Heightmap.m_paintMaskDeepSnow;
                default:
                    return Heightmap.m_paintMaskNothing;
            }
        }

        private static void ApplyColor(int x, int y, Color tileColor, ref Color[] paintMask, ref bool[] modifiedPaint, bool removeColor = false)
        {
            var tileIndex = y * PTilesPerChunk + x;
            paintMask[tileIndex] = tileColor;
            modifiedPaint[tileIndex] = !removeColor;
            Logger.Info(() => $"tilePos: ({x}, {y}), tileIndex: {tileIndex}, tileColor: {tileColor}");
        }
    }

    [HarmonyPatch]
    public static class PreciseSmoothTerrainModification
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TerrainComp))]
        [HarmonyPatch(nameof(TerrainComp.SmoothTerrain))]
        private static bool Prefix(Vector3 worldPos, TerrainComp __instance, Heightmap ___m_hmap, ref float[] ___m_smoothDelta, ref bool[] ___m_modifiedHeight)
        {
            if (GridModeOverride.enabled)
            {
                PreciseTerrainModifier.SmoothenTerrain(worldPos, ___m_hmap, __instance, ref ___m_smoothDelta, ref ___m_modifiedHeight);
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch]
    public static class PreciseRaiseTerrainModification
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TerrainComp))]
        [HarmonyPatch(nameof(TerrainComp.RaiseTerrain))]
        private static bool Prefix(Vector3 worldPos, TerrainComp __instance, Heightmap ___m_hmap, ref float[] ___m_levelDelta, ref float[] ___m_smoothDelta, ref bool[] ___m_modifiedHeight)
        {
            if (GridModeOverride.enabled)
            {
                PreciseTerrainModifier.RaiseTerrain(worldPos, ___m_hmap, __instance, GridModeOverride.raiseGroundΔ, ref ___m_levelDelta, ref ___m_smoothDelta, ref ___m_modifiedHeight);
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch]
    public static class PreciseColorTerrainModification
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TerrainComp))]
        [HarmonyPatch(nameof(TerrainComp.PaintCleared))]
        private static bool Prefix(Vector3 worldPos, TerrainOp.Settings settings, Heightmap ___m_hmap, ref Color[] ___m_paintMask, ref bool[] ___m_modifiedPaint)
        {
            if (GridModeOverride.enabled)
            {
                PreciseTerrainModifier.RecolorTerrain(worldPos, settings.m_paintType, ___m_hmap, ref ___m_paintMask, ref ___m_modifiedPaint);
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch]
    public static class GridModeOverride
    {
        private const string RPC = "RPC_ApplyOperation";
        private const int TailSize = sizeof(int) + 2 * sizeof(float);
        private static readonly int Marker = "OCDheim.GridMode".GetStableHashCode();

        public static bool enabled { get; private set; }
        public static float raiseGroundΔ { get; private set; }
        public static float lowerGroundΔ { get; private set; }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TerrainComp))]
        [HarmonyPatch(nameof(TerrainComp.ApplyOperation))]
        private static bool Send(TerrainOp modifier, ZNetView ___m_nview)
        {
            if (!ShouldOverride(modifier))
            {
                return true;
            }

            var envelope = new ZPackage();
            AppendVanillaModifiers(envelope, modifier);
            AppendOCDheimModifiers(envelope);

            ___m_nview.InvokeRPC(RPC, envelope);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TerrainComp))]
        [HarmonyPatch(nameof(TerrainComp.RPC_ApplyOperation))]
        private static void Receive([HarmonyArgument("pkg")] ZPackage envelope)
        {
            var tail = envelope.Size() - TailSize;
            enabled = false;
            if (tail >= 0)
            {
                envelope.SetPos(tail);
                if (envelope.ReadInt() == Marker)
                {
                    enabled = true;
                    raiseGroundΔ = envelope.ReadSingle();
                    lowerGroundΔ = envelope.ReadSingle();
                }
                envelope.SetPos(0);
            }
        }

        private static bool ShouldOverride(TerrainOp modifier) => KeyBinder.gridModeEnabled && IsOCDheim(modifier) && !IsSnow(modifier);

        private static bool IsOCDheim(TerrainOp modifier) => modifier.GetComponent<OverlayVisualizer>();

        private static bool IsSnow(TerrainOp modifier)
        {
            var settings = modifier.m_settings;
            var position = modifier.transform.position;
            return settings.m_paintCleared && settings.m_paintType == PaintType.Cultivate && WorldGenerator.IsDeepnorth(position.x, position.z);
        }

        private static void AppendVanillaModifiers(ZPackage envelope, TerrainOp modifier)
        {
            envelope.Write(modifier.transform.position);
            envelope.Write(modifier.m_settings.m_rotation);
            if (modifier.m_settings.m_rotation)
            {
                envelope.Write(modifier.transform.forward);
            }
            modifier.m_settings.Serialize(envelope, modifier.gameObject);
        }

        private static void AppendOCDheimModifiers(ZPackage envelope)
        {
            envelope.Write(Marker);
            envelope.Write(RaiseGroundSpinner.value);
            envelope.Write(LowerGroundSpinner.value);
        }
    }
}
