using HarmonyLib;
using UnityEngine;

namespace OCDheim
{
    [HarmonyPatch(typeof(ZInput), "Load")]
    public class Keybindings
    {
        public const string GridMode = "GridMode";
        public const string NoSnapMode = "AltPlace";
        public const string JoyGridMode = "JoyGridMode";
        public const string JoyNoSnapMode = "JoyAltPlace";
        public const string PrecisionMode = "PrecisionMode";
        public const string JoyPrecisionMode = "JoyPrecisionMode";
        public const string MouseScrollWheel = "Mouse ScrollWheel";
        public const string JoyScrollUnlock = "JoyLTrigger";
        public const string JoyScrollDOwn = "JoyDPadDown";
        public const string JoyScrollUp = "JoyDPadUp";

        public const float OrdinaryScrollPrecision = 0.05f;
        public const float SuperiorScrollPrecisionMulltiplier = 0.2f;

        private static bool gridModeFreshlyEnabled = false;
        private static bool gridModeFreshlyDisabled = false;

        public static bool GridModeEnabled { get; private set; } = false;
        public static bool GridModeDisabled { get { return !GridModeEnabled; } }
        public static bool GridModFreshlyEnabled { get { var temp = gridModeFreshlyEnabled; gridModeFreshlyEnabled = false; return temp; } }
        public static bool GridModFreshlyDisabled { get { var temp = gridModeFreshlyDisabled; gridModeFreshlyDisabled = false; return temp; } }
        public static bool PrecisionModeEnabled { get; private set; } = false;
        public static bool PrecisionModeDisabled { get { return !PrecisionModeEnabled; } }
        public static bool SnapModeEnabled { get { return !ZInput.GetButton(NoSnapMode) && !ZInput.GetButton(JoyNoSnapMode); } }
        public static bool SnapModeDisabled { get { return !SnapModeEnabled; } }

        public static KeyCode PrecisionModeKeycode;
        public static KeyCode PrecisionModeJoycode;
        public static KeyCode GridModeKeycode;
        public static KeyCode GridModeJoycode;
        public static ZInput ZInstance;

        public static void Postfix(ZInput __instance)
        {
            ZInstance = __instance;
            __instance.AddButton(PrecisionMode, OCDheim.configPrecisionModeKeybind.Value.MainKey);
            __instance.AddButton(JoyPrecisionMode, OCDheim.configPrecisionModeJoybind.Value.MainKey);
            __instance.AddButton(GridMode, OCDheim.configGridModeKeybind.Value.MainKey);
            __instance.AddButton(JoyGridMode, OCDheim.configGridModeJoybind.Value.MainKey);
        }

        public static void Refresh()
        {
            if (
                ZInstance.GetButtonDef(PrecisionMode).m_key != OCDheim.configPrecisionModeKeybind.Value.MainKey
                || ZInstance.GetButtonDef(JoyPrecisionMode).m_key != OCDheim.configPrecisionModeJoybind.Value.MainKey
                || ZInstance.GetButtonDef(GridMode).m_key != OCDheim.configGridModeKeybind.Value.MainKey
                || ZInstance.GetButtonDef(JoyGridMode).m_key != OCDheim.configGridModeJoybind.Value.MainKey
                )
            {
                Debug.Log("Keybind change detected, updating to use new value");
                ZInstance.Setbutton(PrecisionMode, OCDheim.configPrecisionModeKeybind.Value.MainKey);
                ZInstance.Setbutton(JoyPrecisionMode, OCDheim.configPrecisionModeJoybind.Value.MainKey);
                ZInstance.Setbutton(GridMode, OCDheim.configGridModeKeybind.Value.MainKey);
                ZInstance.Setbutton(JoyGridMode, OCDheim.configGridModeJoybind.Value.MainKey);
            }
            
            if (ZInput.GetButtonDown(GridMode) || ZInput.GetButtonDown(JoyGridMode))
            {
                GridModeEnabled = !GridModeEnabled;
                gridModeFreshlyEnabled = GridModeEnabled;
                gridModeFreshlyDisabled = GridModeDisabled;
                Debug.Log("Grid Mode Toggle: " + GridModeEnabled);
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft,"Grid Mode: " + GridModeEnabled);
            }
            if (ZInput.GetButtonDown(PrecisionMode) || ZInput.GetButtonDown(JoyPrecisionMode))
            {
                PrecisionModeEnabled = !PrecisionModeEnabled;
                Debug.Log("Precision Mode Toggle: " + PrecisionModeEnabled);
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Precision Mode: " + PrecisionModeEnabled);
            }
        }

        public static float ScrollΔ()
        {
            var scrollΔ = Input.GetAxis(MouseScrollWheel);
            if (scrollΔ != 0)
            {
                scrollΔ = scrollΔ > 0 ? OrdinaryScrollPrecision : - OrdinaryScrollPrecision;
            }
            else if (ZInput.GetButton(JoyScrollUnlock))
            {
                Debug.Log("DUPA");
                scrollΔ = ZInput.GetButtonDown(JoyScrollUp) ? OrdinaryScrollPrecision : scrollΔ;
                scrollΔ = ZInput.GetButtonDown(JoyScrollDOwn) ? - OrdinaryScrollPrecision : scrollΔ;
            }

            return PrecisionModeDisabled ? scrollΔ : scrollΔ * SuperiorScrollPrecisionMulltiplier;
        }
    }
}
