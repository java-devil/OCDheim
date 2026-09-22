using HarmonyLib;
using Jotunn.Configs;
using UnityEngine;
using static OCDheim.PlayerHelpers;

namespace OCDheim
{
    [HarmonyPatch]
    public class KeyHinter : MonoBehaviour
    {
        private const string CycleKeyPath = "Keyboard/Snap";
        private const string CycleJoyPath = "Gamepad/Text - Snap";
        private const string PlaceKeyPath = "Keyboard/Place";
        private const string BlockKeyPath = "Keyboard/Block";
        private const string RotateKeyPath = "Keyboard/rotate";
        private const string PlaceJoyPath = "Gamepad/Text - Place";
        private const string BlockJoyPath = "Gamepad/Text - Block";
        private const string RotateJoyPath = "Gamepad/Text - Rotate";
        private const string BuildMenuJoyPath = "Gamepad/Text - BuildMenu";

        private KeyHints hints { get; set; }
        private KeyHint gridModeHint { get; set; }
        private KeyHint precisionModeHint { get; set; }
        private KeyHint fineTuneEffectHint { get; set; }
        private KeyHint pickaxeGridModeHint { get; set; }

        private Transform cycleKeyHint { get; set; }
        private Transform cycleJoyHint { get; set; }
        private Transform rotateKeyHint { get; set; }
        private Transform rotateJoyHint { get; set; }
        private bool showHelperHints { get; set; } = true;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(KeyHints))]
        [HarmonyPatch(nameof(KeyHints.Awake))]
        private static void Initialize(KeyHints __instance)
        {
            __instance.gameObject.AddComponent<KeyHinter>();
        }

        private void Awake()
        {
            hints = GetComponent<KeyHints>();
            var buildHints = hints.m_buildHints.transform;
            var combatHints = hints.m_combatHints.transform;

            cycleKeyHint = buildHints.Find(CycleKeyPath);
            cycleJoyHint = buildHints.Find(CycleJoyPath);
            rotateKeyHint = buildHints.Find(RotateKeyPath);
            rotateJoyHint = buildHints.Find(RotateJoyPath);
            var placeKeyHint = buildHints.Find(PlaceKeyPath);
            var placeJoyHint = buildHints.Find(PlaceJoyPath);
            var blockKeyHint = combatHints.Find(BlockKeyPath);
            var blockJoyHint = combatHints.Find(BlockJoyPath);
            var buildMenuJoyHint = buildHints.Find(BuildMenuJoyPath);

            gridModeHint = new KeyHint("Grid Mode", placeKeyHint, placeJoyHint,
                () => Key(KeyBinder.gridModeKey),
                () => Joy(KeyBinder.gridModeJoy)
                ).Before(rotateKeyHint, buildMenuJoyHint);
            precisionModeHint = new KeyHint("Precision Mode", placeKeyHint, placeJoyHint,
                () => Key(KeyBinder.precisionModeKey),
                () => Joy(KeyBinder.precisionModeJoy)
                ).Before(rotateKeyHint, buildMenuJoyHint);
            fineTuneEffectHint = new KeyHint("Fine-Tune Effect", rotateKeyHint, rotateJoyHint,
                null, // keyResolver is null when the key is baked into the keyTemplate (a.k.a Scroll Wheel)
                () => Combo(KeyBinder.JoyScrollUnlock, KeyBinder.JoyScrollUp, KeyBinder.JoyScrollDown)
                ).After(rotateKeyHint, rotateJoyHint);
            pickaxeGridModeHint = new KeyHint("Grid Mode", blockKeyHint, blockJoyHint,
                () => Key(KeyBinder.gridModeKey),
                () => Joy(KeyBinder.gridModeJoy)
                );
        }

        private void Update()
        {
            if (hints.m_combatHints.activeSelf)
            {
                pickaxeGridModeHint.Refresh(player.HasPickaxeEquipped());
            }

            if (hints.m_buildHints.activeSelf)
            {
                gridModeHint.Refresh(player.HasGridModeToolEquipped());
                precisionModeHint.Refresh(player.HasBuildPieceEquipped());
                fineTuneEffectHint.Refresh(player.HasRaiseGroundTerraformToolEquipped() && KeyBinder.gridModeEnabled);
            }

            if (showHelperHints == player.HasOverlayVisible())
            {
                showHelperHints = !showHelperHints;
                cycleKeyHint.gameObject.SetActive(showHelperHints);
                cycleJoyHint.gameObject.SetActive(showHelperHints);
                rotateKeyHint.gameObject.SetActive(showHelperHints);
                rotateJoyHint.gameObject.SetActive(showHelperHints);
            }
        }
        
        private static string Key(ButtonConfig button) => Localization.instance.GetBoundKeyString(button.Name, true);
        private static string Joy(ButtonConfig button) => ZInput.instance.GetBoundKeyString($"Joy!{button.Name}", true);
        private static string Joy(string buttonName) => ZInput.instance.GetBoundKeyString(buttonName, true);
        private static string Combo(string modifier, string b1, string b2) => $"{Joy(modifier)} + {Joy(b1)} / {Joy(b2)}";
    }
}
