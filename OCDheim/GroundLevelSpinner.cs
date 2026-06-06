using HarmonyLib;
using UnityEngine;

using static OCDheim.PlayerHelpers;

namespace OCDheim
{
    public class GroundLevelSpinner
    {
        public static readonly GroundLevelSpinner RaiseGroundSpinner = new GroundLevelSpinner( 1.0f,  0.0f, 1.0f);
        public static readonly GroundLevelSpinner LowerGroundSpinner = new GroundLevelSpinner(-0.5f, -1.0f, 0.0f);
        
        public float value { get; private set; }
        private float maxValue { get; }
        private float minValue { get; }

        private GroundLevelSpinner(float value , float minValue, float maxValue)
        {
            this.value = value;
            this.minValue = minValue;
            this.maxValue = maxValue;
        }

        public void Refresh()
        {
            var scrollΔ = KeyBinder.ScrollΔ();
            if (scrollΔ > 0) {
                Up(scrollΔ);
            }
            if (scrollΔ < 0)
            {
                Down(scrollΔ);
            }
        }

        private void Up(float scrollΔ)
        {
            if (value + scrollΔ > maxValue)
            {
                value = maxValue;
            }
            else
            {
                value = Mathf.Round((value + scrollΔ) * 100) / 100;
            }
        }

        private void Down(float scrollΔ)
        {
            if (value + scrollΔ < minValue)
            {
                value = minValue;
            }
            else
            {
                value = Mathf.Round((value + scrollΔ) * 100) / 100;
            }
        }
    }

    [HarmonyPatch]
    public static class BlockCameraZoom
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ZInput))]
        [HarmonyPatch(nameof(ZInput.GetMouseScrollWheel))]
        public static bool Prefix(ref float __result)
        {
            if (player.HasRaiseGroundTerraformToolEquipped())
            {
                __result = 0f;
                return false;
            }

            return true;
        }
    }
}
