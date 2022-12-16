namespace OCDheim
{
    public static class ToolHelper
    {
        private const string HoeToolName = "$item_hoe";
        private const string HammerToolName = "$item_hammer";
        private const string CultivatorToolName = "$item_cultivator";

        public static bool IsHoe(this ItemDrop.ItemData tool) => tool.m_shared.m_name == HoeToolName;
        public static bool IsHammer(this ItemDrop.ItemData tool) => tool.m_shared.m_name == HammerToolName;
        public static bool IsCultivator(this ItemDrop.ItemData tool) => tool.m_shared.m_name == CultivatorToolName;
        public static bool IsTerrainModificationTool(this ItemDrop.ItemData tool) => tool.IsHoe() || tool.IsCultivator();
    }
}
