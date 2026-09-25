using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace OCDheim
{
    [HarmonyPatch]
    public static class TerrainLimiter
    {
        private const float VanillaΔHLimit = 8.0f;
        private const float VanillaΔSLimit = 1.0f;
        private const float VanillaMaxDepthThreshold = 7.95f;

        private static readonly FieldInfo ΔLField = AccessTools.Field(typeof(TerrainComp), nameof(TerrainComp.m_levelDelta));
        private static readonly FieldInfo ΔSField = AccessTools.Field(typeof(TerrainComp), nameof(TerrainComp.m_smoothDelta));
        private static readonly MethodInfo VanillaClamp = AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp), new[] { typeof(float), typeof(float), typeof(float) });

        private static float maxΔH => Config.raiseTerrainLimit.Value;
        private static float minΔH => -Config.lowerTerrainLimit.Value;
        private static float maxDepthThreshold => -minΔH - (VanillaΔHLimit - VanillaMaxDepthThreshold);
        
        // when ΔL is within ±8m: render up until 8m (as Vanilla Would do it)
        // when ΔL is beyond ±8m: render what's preserved "as is" (even beyond minΔH/maxΔH)
        public static float ΔH(float ΔL, float ΔS) => Mathf.Abs(ΔL) > VanillaΔHLimit ? ΔL + ΔS : Mathf.Clamp(ΔL + ΔS, -VanillaΔHLimit, VanillaΔHLimit);

        // OCDheim does NOT render invisible ΔS preserved by Vanilla Valheim
        public static float VisibleΔS(float ΔL, float ΔS) => ΔH(ΔL, ΔS) - ΔL;

        // OCDheim does NOT preserve invisible ΔS as to Vanilla Valheim does
        public static float LimitΔS(float ΔL, float visibleΔS, float newΔS)
        {
            var minΔS = Mathf.Max(-VanillaΔSLimit, ΔL >= -VanillaΔHLimit ? -VanillaΔHLimit - ΔL : float.MinValue, minΔH - ΔL);
            var maxΔS = Mathf.Min(VanillaΔSLimit, ΔL <= VanillaΔHLimit ? VanillaΔHLimit - ΔL : float.MaxValue, maxΔH - ΔL);
            return Limit(newΔS, visibleΔS, minΔS, maxΔS);
        }

        public static float LimitΔL(float oldΔH, float newΔL) => Limit(newΔL, oldΔH, minΔH, maxΔH);

        // when ΔH > maxΔH: do NOT decrease ΔH on an attempt to raise terrain
        // when ΔH < maxΔH: do NOT increase ΔH on an attempt to lower terrain
        private static float Limit(float newΔ, float oldΔ, float minΔ, float maxΔ) => Mathf.Clamp(newΔ, Mathf.Min(minΔ, oldΔ), Mathf.Max(maxΔ, oldΔ));

        private static float FoldΔS(float[] ΔL, float[] ΔS, int i, out float oldΔH)
        {
            oldΔH = ΔH(ΔL[i], ΔS[i]);
            return oldΔH - ΔL[i];
        }

        private static float LimitFoldedΔL(float newΔL, float oldΔH) => LimitΔL(oldΔH, newΔL);

        private static float DropInvisibleΔS(float oldΔS, float[] ΔL, int i, out float visibleΔS)
        {
            visibleΔS = VisibleΔS(ΔL[i], oldΔS);
            return visibleΔS;
        }

        private static float LimitDroppedΔS(float newΔS, float[] ΔL, int i, float visibleΔS) => LimitΔS(ΔL[i], visibleΔS, newΔS);

        // height = Mathf.Clamp(height, baseHeight - 8f, baseHeight + 8f);
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TerrainComp))]
        [HarmonyPatch(nameof(TerrainComp.ApplyToHeightmap))]
        private static void Remember(List<float> heights, out float[] __state) => __state = heights.ToArray();

        // height = Mathf.Clamp(height, baseHeight - 8f, baseHeight + 8f);
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TerrainComp))]
        [HarmonyPatch(nameof(TerrainComp.ApplyToHeightmap))]
        private static void ApplyΔH(List<float> heights, float[] __state, bool ___m_initialized, float[] ___m_levelDelta, float[] ___m_smoothDelta)
        {
            if (!___m_initialized)
            {
                return;
            }

            var ΔL = ___m_levelDelta;
            var ΔS = ___m_smoothDelta;
            for (var i = 0; i < ΔL.Length; i++)
            {
                if (Mathf.Abs(ΔL[i]) > VanillaΔHLimit)
                {
                    heights[i] = __state[i] + ΔH(ΔL[i], ΔS[i]);
                }
            }
        }

        // num4 += m_smoothDelta[i]; ... m_levelDelta[i] = Mathf.Clamp(m_levelDelta[i], -8f, 8f);
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(TerrainComp))]
        [HarmonyPatch(nameof(TerrainComp.LevelTerrain))]
        private static IEnumerable<CodeInstruction> LimitLevelGround(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original) => LimitVanillaΔL(instructions, generator, original);

        // float num9 = num7 - height + m_smoothDelta[i]; ... m_levelDelta[i] = Mathf.Clamp(m_levelDelta[i], -8f, 8f);
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(TerrainComp))]
        [HarmonyPatch(nameof(TerrainComp.RaiseTerrain))]
        private static IEnumerable<CodeInstruction> LimitRaiseGround(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original) => LimitVanillaΔL(instructions, generator, original);

        // m_smoothDelta[i] += num6; m_smoothDelta[i] = Mathf.Clamp(m_smoothDelta[i], -1f, 1f);
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(TerrainComp))]
        [HarmonyPatch(nameof(TerrainComp.SmoothTerrain))]
        private static IEnumerable<CodeInstruction> LimitSmoothGround(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var ΔSIncrement = new[] { new CodeMatch(OpCodes.Ldfld, ΔSField), new CodeMatch(instruction => instruction.IsLdloc()), new CodeMatch(OpCodes.Ldelema), new CodeMatch(OpCodes.Dup), new CodeMatch(OpCodes.Ldind_R4) };
            var ΔSClamp = new[] { new CodeMatch(OpCodes.Ldc_R4, -VanillaΔSLimit), new CodeMatch(OpCodes.Ldc_R4, VanillaΔSLimit), new CodeMatch(OpCodes.Call, VanillaClamp) };
            var matcher = new CodeMatcher(instructions);
            if (!matcher.FindVanillaLimits(original, ΔSIncrement, ΔSClamp))
            {
                return matcher.InstructionEnumeration();
            }

            // m_smoothDelta[i] += num6; → m_smoothDelta[i] = DropInvisibleΔS(m_smoothDelta[i], m_levelDelta, i, out visibleΔS) + num6;
            var visibleΔS = generator.DeclareLocal(typeof(float));
            var index = matcher.Start().MatchStartForward(ΔSIncrement).Advance(1).Instruction;
            matcher.Advance(4).Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, ΔLField),
                new CodeInstruction(index.opcode, index.operand),
                new CodeInstruction(OpCodes.Ldloca, visibleΔS),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TerrainLimiter), nameof(DropInvisibleΔS))));

            // Mathf.Clamp(m_smoothDelta[i], -1f, 1f) → LimitDroppedΔS(m_smoothDelta[i], m_levelDelta, i, visibleΔS)
            matcher.Start().MatchStartForward(ΔSClamp)
                .SetAndAdvance(OpCodes.Ldarg_0, null)
                .SetAndAdvance(OpCodes.Ldfld, ΔLField)
                .InsertAndAdvance(new CodeInstruction(index.opcode, index.operand), new CodeInstruction(OpCodes.Ldloc, visibleΔS))
                .SetOperandAndAdvance(AccessTools.Method(typeof(TerrainLimiter), nameof(LimitDroppedΔS)));

            return matcher.InstructionEnumeration();
        }

        // return Mathf.Max(baseHeight - height, 0f) >= 7.95f;
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Heightmap))]
        [HarmonyPatch(nameof(Heightmap.AtMaxWorldLevelDepth))]
        private static IEnumerable<CodeInstruction> LimitMaxDepth(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            return new CodeMatcher(instructions)
                .SubstituteVanillaLimit(original, nameof(maxDepthThreshold), new CodeMatch(OpCodes.Ldc_R4, VanillaMaxDepthThreshold))
                .InstructionEnumeration();
        }

        private static IEnumerable<CodeInstruction> LimitVanillaΔL(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var ΔSLoad = new[] { new CodeMatch(OpCodes.Ldarg_0), new CodeMatch(OpCodes.Ldfld, ΔSField), new CodeMatch(instruction => instruction.IsLdloc()), new CodeMatch(OpCodes.Ldelem_R4) };
            var ΔLClamp = new[] { new CodeMatch(OpCodes.Ldc_R4, -VanillaΔHLimit), new CodeMatch(OpCodes.Ldc_R4, VanillaΔHLimit), new CodeMatch(OpCodes.Call, VanillaClamp) };
            var matcher = new CodeMatcher(instructions);
            if (!matcher.FindVanillaLimits(original, ΔSLoad, ΔLClamp))
            {
                return matcher.InstructionEnumeration();
            }

            // m_smoothDelta[i] → FoldΔS(m_levelDelta, m_smoothDelta, i, out oldΔH)
            var oldΔH = generator.DeclareLocal(typeof(float));
            matcher.Start().MatchStartForward(ΔSLoad)
                .Advance(1)
                .SetOperandAndAdvance(ΔLField)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldfld, ΔSField))
                .Advance(1)
                .SetAndAdvance(OpCodes.Ldloca, oldΔH)
                .Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TerrainLimiter), nameof(FoldΔS))));

            // Mathf.Clamp(m_levelDelta[i], -8f, 8f) → LimitFoldedΔL(m_levelDelta[i], oldΔH)
            matcher.Start().MatchStartForward(ΔLClamp)
                .SetAndAdvance(OpCodes.Ldloc, oldΔH)
                .RemoveInstruction()
                .SetOperandAndAdvance(AccessTools.Method(typeof(TerrainLimiter), nameof(LimitFoldedΔL)));

            return matcher.InstructionEnumeration();
        }

        private static bool FindVanillaLimits(this CodeMatcher matcher, MethodBase original, params CodeMatch[][] vanillaLimits)
        {
            foreach (var vanillaLimit in vanillaLimits)
            {
                if (!matcher.Start().MatchStartForward(vanillaLimit).IsValid)
                {
                    Logger.Warn(() => $"Vanilla Valheim terrain modification limits not found in {original.DeclaringType?.Name}.{original.Name}");
                    return false;
                }
            }

            return true;
        }

        private static CodeMatcher SubstituteVanillaLimit(this CodeMatcher matcher, MethodBase original, string limit, params CodeMatch[] vanillaLimit)
        {
            matcher.Start().MatchStartForward(vanillaLimit);
            if (matcher.IsValid)
            {
                matcher.Set(OpCodes.Call, AccessTools.PropertyGetter(typeof(TerrainLimiter), limit));
            }
            else
            {
                Logger.Warn(() => $"Vanilla Valheim terrain modification limit {limit} not found in {original.DeclaringType?.Name}.{original.Name}");
            }

            return matcher;
        }
    }
}
