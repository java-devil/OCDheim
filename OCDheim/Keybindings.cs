using HarmonyLib;
using UnityEngine;

namespace OCDheim
{
    [HarmonyPatch(typeof(ZInput), "Load")]
    public static class Keybindings
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

        public static void Postfix(ZInput __instance)
        {
            __instance.AddButton(PrecisionMode, KeyCode.Z);
            __instance.AddButton(GridMode, KeyCode.LeftAlt);
            __instance.AddButton(JoyGridMode, KeyCode.JoystickButton5);
            __instance.AddButton(JoyPrecisionMode, KeyCode.JoystickButton2);
        }

        public static void Refresh()
        {
            if (ZInput.GetButtonDown(GridMode) || ZInput.GetButtonDown(JoyGridMode))
            {
                GridModeEnabled = !GridModeEnabled;
                gridModeFreshlyEnabled = GridModeEnabled;
                gridModeFreshlyDisabled = GridModeDisabled;
            }
            if (ZInput.GetButtonDown(PrecisionMode) || ZInput.GetButtonDown(JoyPrecisionMode))
            {
                PrecisionModeEnabled = !PrecisionModeEnabled;
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
