using UnityEngine;

namespace OCDheim
{
    public static class PlayerHelpers
    {
        private const string HoeToolName = "$item_hoe";
        private const string HammerToolName = "$item_hammer";
        private const string PickaxeToolName = "$item_pickaxe";
        private const string CultivatorToolName = "$item_cultivator";

        public static Player player => Player.m_localPlayer;
        public static Vector3 playerPos => player.transform.position;

        private static bool HasHoeEquipped(this Player p) => p?.GetRightItem() != null && p.GetRightItem().m_shared.m_name == HoeToolName;
        private static bool HasHammerEquipped(this Player p) => p?.GetRightItem() != null && p.GetRightItem().m_shared.m_name == HammerToolName;
        private static bool HasCultivatorEquipped(this Player p) => p?.GetRightItem() != null && p.GetRightItem().m_shared.m_name == CultivatorToolName;
        private static bool HasPickaxeEquipped(this Player p) => p?.GetRightItem() != null && p.GetRightItem().m_shared.m_name.StartsWith(PickaxeToolName);
        public static bool HasConstructionToolEquipped(this Player p) => p.HasHoeEquipped() || p.HasHammerEquipped() || p.HasCultivatorEquipped() || p.HasPickaxeEquipped();
        public static bool HasColorBrushEquipped(this Player p) => p.HasPaveRoadTerraformToolEquipped() || p.HasCultivateTerraformToolEquipped() || p.HasSeedGrassTerraformToolEquipped();

        public static bool HasOverlayVisible(this Player p) => p.m_placementGhost?.GetComponent<OverlayVisualizer>() != null;
        public static bool HasBuildPieceEquipped(this Player p) => p.m_placementGhost && !p.m_placementGhost.GetComponent<OverlayVisualizer>();
        private static bool HasPaveRoadTerraformToolEquipped(this Player p) => p.m_placementGhost?.GetComponent<PaveRoadOverlayVisualizer>() != null;
        private static bool HasCultivateTerraformToolEquipped(this Player p) => p.m_placementGhost?.GetComponent<CultivateOverlayVisualizer>() != null;
        private static bool HasSeedGrassTerraformToolEquipped(this Player p) => p.m_placementGhost?.GetComponent<SeedGrassOverlayVisualizer>() != null;
        public static bool HasLevelGroundTerraformToolEquipped(this Player p) => p.m_placementGhost?.GetComponent<LevelGroundOverlayVisualizer>() != null;
        public static bool HasRaiseGroundTerraformToolEquipped(this Player p) => p.m_placementGhost?.GetComponent<ModifyGroundLevelOverlayVisualizer>() != null;
    }
}
