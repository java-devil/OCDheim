using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

using static OCDheim.PlayerHelpers;
using static OCDheim.PrecisionMode;

namespace OCDheim
{
    [HarmonyPatch]
    public class KeyBinder : MonoBehaviour
    {
        private static readonly ButtonConfig SnapModeKey = new ButtonConfig { Name = "SnapModeKey", Key = KeyCode.LeftShift };
        private static readonly ButtonConfig SnapModeJoy = new ButtonConfig { Name = "SnapModeJoy", GamepadButton = InputManager.GamepadButton.RightShoulder };
        private static readonly ButtonConfig GridModeKey = new ButtonConfig { Name = "GridModeKey", Key = KeyCode.LeftAlt };
        private static readonly ButtonConfig GridModeJoy = new ButtonConfig { Name = "GridModeJoy", GamepadButton = InputManager.GamepadButton.RightStickButton };
        private static readonly ButtonConfig PrecisionModeKey = new ButtonConfig { Name = "PrecisionModeKey", Key = KeyCode.Z };
        private static readonly ButtonConfig PrecisionModeJoy = new ButtonConfig { Name = "PrecisionModeJoy", GamepadButton = InputManager.GamepadButton.ButtonWest };
        
        private const string MouseScrollWheel = "Mouse ScrollWheel";
        private const string JoyScrollUnlock = "JoyLTrigger";
        private const string JoyScrollDown = "JoyDPadDown";
        private const string JoyScrollUp = "JoyDPadUp";

        private const float ScrollPrecision = 0.01f;
        
        private static bool _gridModeEnabled;
        private static bool _gridModeFreshlyEnabled;
        private static bool _gridModeFreshlyDisabled;

        private static bool snapModeDisabled => ZInput.GetButton(SnapModeKey.Name) || ZInput.GetButton(SnapModeJoy.Name);
        public static bool snapModeEnabled => !snapModeDisabled;

        public static bool gridModeEnabled
        {
            get => _gridModeEnabled;
            private set {
                _gridModeEnabled = value;
                _gridModeFreshlyEnabled = value;
                _gridModeFreshlyDisabled = !value;
            }
        }
        public static bool gridModeDisabled => !gridModeEnabled;
        public static bool gridModFreshlyEnabled { get { var temp = _gridModeFreshlyEnabled; _gridModeFreshlyEnabled = false; return temp; } }
        public static bool gridModFreshlyDisabled { get { var temp = _gridModeFreshlyDisabled; _gridModeFreshlyDisabled = false; return temp; } }

        public static PrecisionMode precisionMode { get; private set; } = ORDINARY;

        private void Awake()
        {
            InputManager.Instance.AddButton(OCDheim.GUID, SnapModeJoy);
            InputManager.Instance.AddButton(OCDheim.GUID, SnapModeKey);
            InputManager.Instance.AddButton(OCDheim.GUID, GridModeKey);
            InputManager.Instance.AddButton(OCDheim.GUID, GridModeJoy);
            InputManager.Instance.AddButton(OCDheim.GUID, PrecisionModeKey);
            InputManager.Instance.AddButton(OCDheim.GUID, PrecisionModeJoy);
        }

        private void Update()
        {
            var gridModeButton = ZInput.GetButtonDown(GridModeKey.Name)
                                 || (snapModeEnabled && ZInput.GetButtonDown(GridModeJoy.Name));
            var toggleGridMode = (gridModeButton && (gridModeEnabled || player.HasConstructionToolEquipped()))
                                 || (gridModeEnabled && !player.HasConstructionToolEquipped());
            var precisionModeButton = ZInput.GetButtonDown(PrecisionModeKey.Name)
                                      || (snapModeEnabled && ZInput.GetButtonDown(PrecisionModeJoy.Name));
            var togglePrecisionMode = (precisionModeButton && (precisionMode == SUPERIOR || player.HasBuildPieceEquipped()))
                                      || (precisionMode == SUPERIOR && !player.HasBuildPieceEquipped());

            if (toggleGridMode)
            {
                gridModeEnabled = !gridModeEnabled;
                Logger.Info(() => $"[{(gridModeEnabled ? "ENABLED" : "DISABLED")}] GRID MODE");
            }
            if (togglePrecisionMode)
            {
                precisionMode = precisionMode == ORDINARY ? SUPERIOR : ORDINARY;
                Logger.Info(() => $"[{(precisionMode == SUPERIOR ? "ENABLED" : "DISABLED")}] PRECISION MODE");
            }
        }

        public static float ScrollΔ()
        {
            var scrollΔ = Input.GetAxis(MouseScrollWheel);
            if (scrollΔ != 0)
            {
                return scrollΔ > 0 ? ScrollPrecision : - ScrollPrecision;
            }
            
            if (ZInput.GetButton(JoyScrollUnlock) && ZInput.GetButtonDown(JoyScrollDown))
            {
                return - ScrollPrecision;
            }

            if (ZInput.GetButton(JoyScrollUnlock) && ZInput.GetButtonDown(JoyScrollUp))
            {
                return ScrollPrecision;
            }

            return scrollΔ;
        }
    }
}
