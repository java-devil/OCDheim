using BepInEx.Configuration;
using UnityEngine;
using static Jotunn.Managers.InputManager;

namespace OCDheim
{
    public static class Config
    {
        private const string Keybinds = "Keybinds";
        private const string Functionalities = "Functionalities";
        private const string Logging = "Logging";

        private const string DecidedByServer = "The Server decides for Everyone if The Server is running OCDheim.";
        private const string UnbindToDisable = "Unbind (set to None) to DISABLE {0}. To re-ENABLE {0} a restart of Valheim is required.";

        private const string AdditionalSnapPointsDesc = "Snap Build Pieces to Additional Snap Points derived by OCDheim.";
        private static readonly string AdditionalBuildPiecesDesc = $"Show the Smooth Stone Build Pieces in the Build Menu. {DecidedByServer}";
        private const string RemoveTerrainModificationsDesc = "Show Remove Terrain Modifications in The Hoe.";
        private static readonly string PileUpperDesc = $"Stacks, Piles and Barrels now stack, pile, and... erm... barrel(:P) vertically on top of each other. {DecidedByServer}"; 

        private const string LoggingLevelDesc = "No need to modify this *unless* you *know* you need to modify this ;)";

        private const string SnapModeJoyDesc = "HOLD to temporarily DISABLE Snap Mode.";
        private static readonly string GridModeDesc = $"PRESS to toggle Grid Mode. {string.Format(UnbindToDisable, "Grid Mode")}";
        private static readonly string PrecisionModeDesc = $"PRESS to toggle Precision Mode. {string.Format(UnbindToDisable, "Precision Mode")}";

        public static ConfigEntry<bool> pileUpper { get; private set; }
        public static ConfigEntry<bool> additionalSnapPoints { get; private set; }
        public static ConfigEntry<bool> additionalBuildPieces { get; private set; }
        public static ConfigEntry<bool> removeTerrainModifications { get; private set; }

        public static ConfigEntry<KeyCode> gridModeKey { get; private set; }
        public static ConfigEntry<KeyCode> precisionModeKey { get; private set; }
        public static ConfigEntry<GamepadButton> snapModeJoy { get; private set; }
        public static ConfigEntry<GamepadButton> gridModeJoy { get; private set; }
        public static ConfigEntry<GamepadButton> precisionModeJoy { get; private set; }

        public static ConfigEntry<LoggingLevel> loggingLevel { get; private set; }

        public static void Bind(ConfigFile config)
        {
            additionalSnapPoints = config.Bind(Functionalities, "Additional Snap Points", true, Describe(AdditionalSnapPointsDesc, 4, false));
            additionalBuildPieces = config.Bind(Functionalities, "Additional Build Pieces", false, Describe(AdditionalBuildPiecesDesc, 3, true));
            removeTerrainModifications = config.Bind(Functionalities, "Remove Terrain Modifications", true, Describe(RemoveTerrainModificationsDesc, 2, false));
            pileUpper = config.Bind(Functionalities, "Vertical Stacking", true, Describe(PileUpperDesc, 1, true));

            snapModeJoy = config.Bind(Keybinds, "Snap Mode Gamepad Button", GamepadButton.RightShoulder, Describe(SnapModeJoyDesc, 5, false));
            gridModeKey = config.Bind(Keybinds, "Grid Mode Key", KeyCode.LeftAlt, Describe(GridModeDesc, 4, false));
            gridModeJoy = config.Bind(Keybinds, "Grid Mode Gamepad Button", GamepadButton.RightStickButton, Describe(GridModeDesc, 3, false));
            precisionModeKey = config.Bind(Keybinds, "Precision Mode Key", KeyCode.Z, Describe(PrecisionModeDesc, 2, false));
            precisionModeJoy = config.Bind(Keybinds, "Precision Mode Gamepad Button", GamepadButton.ButtonWest, Describe(PrecisionModeDesc, 1, false));

            loggingLevel = config.Bind(Logging, "Logging Level", LoggingLevel.WARNING, Describe(LoggingLevelDesc, 1, false));
        }

        private static ConfigDescription Describe(string description, int order, bool adminOnly)
        {
            var attributes = new ConfigurationManagerAttributes { Order = order, IsAdminOnly = adminOnly };
            return new ConfigDescription(description, null, attributes);
        }
    }
}
