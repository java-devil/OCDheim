using System.Collections.Generic;
using HarmonyLib;

namespace OCDheim
{
    [HarmonyPatch]
    public static class PileUpper
    {
        private static readonly List<string> ApplicableSuffixes = new List<string> { "pile", "stack", "barrel" };
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(WearNTear))]
        [HarmonyPatch(nameof(WearNTear.Start))]
        private static void MakePilesAsDurableAsWood(WearNTear __instance)
        {
            var pieceName = __instance.name;
            foreach (var suffix in ApplicableSuffixes)
            {
                if (pieceName.Contains(suffix))
                {
                    __instance.m_supports = true;
                }
            }
        }
    }
}
