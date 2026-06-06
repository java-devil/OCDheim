using HarmonyLib;

namespace OCDheim
{
    [HarmonyPatch]
    public static class PileUpper
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(WearNTear))]
        [HarmonyPatch(nameof(WearNTear.Start))]
        private static void MakePilesAsDurableAsWood(WearNTear __instance)
        {
            if (__instance.name.Contains("pile") || __instance.name.Contains("stack"))
            {
                __instance.m_supports = true;
            }
        }
    }
}
