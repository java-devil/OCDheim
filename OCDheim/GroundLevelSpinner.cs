using UnityEngine;

namespace OCDheim
{
    public static class GroundLevelSpinner
    {
        public const float MaxSpinner = 1.0f;
        public const float MinSpinner = 0.0f;

        public static float value { get; private set; } = 1.0f;

        public static void Refresh()
        {
            var scrollΔ = Keybindings.ScrollΔ();
            if (scrollΔ > 0) {
                up(scrollΔ);
            }
            if (scrollΔ < 0)
            {
                down(scrollΔ);
            }
        }

        private static void up(float scrollΔ)
        {
            if (value + scrollΔ > MaxSpinner)
            {
                value = MaxSpinner;
            }
            else
            {
                value = Mathf.Round((value + scrollΔ) * 100) / 100;
            }
        }

        private static void down(float scrollΔ)
        {
            if (value + scrollΔ < MinSpinner)
            {
                value = MinSpinner;
            }
            else
            {
                value = Mathf.Round((value + scrollΔ) * 100) / 100;
            }
        }
    }
}
