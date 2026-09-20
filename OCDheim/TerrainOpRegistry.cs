using System.Collections.Generic;
using HarmonyLib;

namespace OCDheim
{
    // Valheim 1.0 resolves a Terrain Op received over the network by prefab hash through ObjectDB.m_terrainOpsByHash.
    // Jötunn (2.30.0) registers custom pieces with ZNetScene only, so OCDheim's own Terrain Op prefabs are registered
    // here instead, every time ObjectDB rebuilds its registers (i.e. on ObjectDB.Awake and ObjectDB.CopyOtherDB).
    [HarmonyPatch]
    public static class TerrainOpRegistry
    {
        private static readonly List<TerrainOp> CustomTerrainOps = new List<TerrainOp>();

        public static void Register(TerrainOp terrainOp)
        {
            CustomTerrainOps.Add(terrainOp);
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
            foreach (var terrainOp in CustomTerrainOps)
            {
                if (!__instance.m_terrainOps.Contains(terrainOp))
                {
                    __instance.m_terrainOps.Add(terrainOp);
                }
                __instance.m_terrainOpsByHash[__instance.GetPrefabHash(terrainOp.gameObject)] = terrainOp;
                Logger.Debug(() => $"[REGISTERED] Terrain Op '{terrainOp.name}' with ObjectDB");
            }
        }
    }
}
