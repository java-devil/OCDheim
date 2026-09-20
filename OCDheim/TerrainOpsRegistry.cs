using System.Collections.Generic;
using HarmonyLib;

namespace OCDheim
{
    [HarmonyPatch]
    public static class TerrainOpsRegistry
    {
        private static readonly List<TerrainOp> ModdedTerrainOps = new List<TerrainOp>();

        public static void Register(TerrainOp terrainOp)
        {
            ModdedTerrainOps.Add(terrainOp);
            if (ObjectDB.instance)
            {
                RegisterAll(ObjectDB.instance);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ObjectDB))]
        [HarmonyPatch(nameof(ObjectDB.UpdateRegisters))]
        private static void RegisterAll(ObjectDB __instance)
        {
            foreach (var terrainOp in ModdedTerrainOps)
            {
                if (!__instance.m_terrainOps.Contains(terrainOp))
                {
                    __instance.m_terrainOps.Add(terrainOp);
                }
                __instance.m_terrainOpsByHash[__instance.GetPrefabHash(terrainOp.gameObject)] = terrainOp;
                Logger.Debug(() => $"[REGISTERED] Terrain Op '{terrainOp.name}' with ODB");
            }
        }
    }
}
