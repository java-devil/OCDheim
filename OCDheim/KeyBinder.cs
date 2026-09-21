using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

using static OCDheim.PlayerHelpers;
using static OCDheim.PrecisionMode;

namespace OCDheim
{
    public class KeyBinder : MonoBehaviour
    {
        private static ButtonConfig snapModeJoy { get; set; }
        private static ButtonConfig gridModeKey { get; set; }
        private static ButtonConfig gridModeJoy { get; set; }
        private static ButtonConfig precisionModeKey { get; set; }
        private static ButtonConfig precisionModeJoy { get; set; }
        
        public const string SuppressSnapModeKey = "AltPlace";
        private const string MouseScrollWheel = "Mouse ScrollWheel";
        private const string JoyScrollUnlock = "JoyLTrigger";
        private const string JoyScrollDown = "JoyDPadDown";
        private const string JoyScrollUp = "JoyDPadUp";

        private const float ScrollPrecision = 0.01f;
        
        private static bool _gridModeEnabled;
        private static bool _gridModeFreshlyEnabled;
        private static bool _gridModeFreshlyDisabled;

        public static bool snapModeDisabled => ZInput.GetButton(SuppressSnapModeKey) || ZInput.GetButton(snapModeJoy.Name);
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
            snapModeJoy = AddButton(new ButtonConfig { Name = nameof(snapModeJoy), GamepadConfig = Config.snapModeJoy });
            gridModeKey = AddButton(new ButtonConfig { Name = nameof(gridModeKey), Config = Config.gridModeKey });
            gridModeJoy = AddButton(new ButtonConfig { Name = nameof(gridModeJoy), GamepadConfig = Config.gridModeJoy });
            precisionModeKey = AddButton(new ButtonConfig { Name = nameof(precisionModeKey), Config = Config.precisionModeKey });
            precisionModeJoy = AddButton(new ButtonConfig { Name = nameof(precisionModeJoy), GamepadConfig = Config.precisionModeJoy });
        }

        private static ButtonConfig AddButton(ButtonConfig button)
        {
            InputManager.Instance.AddButton(OCDheim.GUID, button);
            return button;
        }

        private void Update()
        {
            var gridModeButton = ZInput.GetButtonDown(gridModeKey.Name)
                                 || (snapModeEnabled && ZInput.GetButtonDown(gridModeJoy.Name));
            var toggleGridMode = (gridModeButton && (gridModeEnabled || player.HasConstructionToolEquipped()))
                                 || (gridModeEnabled && !player.HasConstructionToolEquipped());
            var precisionModeButton = ZInput.GetButtonDown(precisionModeKey.Name)
                                      || (snapModeEnabled && ZInput.GetButtonDown(precisionModeJoy.Name));
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
