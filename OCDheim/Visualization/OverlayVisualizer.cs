using HarmonyLib;
using Jotunn.Utils;
using UnityEngine;

namespace OCDheim
{
    // Helper classes for OverlayVisualizerImpls, intented to abstract away unecessary low level complexity.
    public abstract class OverlayVisualizer : MonoBehaviour
    {
        public static Texture2D remove { get { return OCDheim.LoadTextureFromDisk("remove.png"); } }
        public static Texture2D cross { get { return OCDheim.LoadTextureFromDisk("cross.png"); } }
        public static Texture2D undo { get { return OCDheim.LoadTextureFromDisk("undo.png"); } }
        public static Texture2D redo { get { return OCDheim.LoadTextureFromDisk("redo.png"); } }
        public static Texture2D box { get { return OCDheim.LoadTextureFromDisk("box.png"); } }

        protected Overlay primary;
        protected Overlay secondary;
        protected Overlay tetriary;
        protected HoverInfo hoverInfo;

        void Update()
        {
            if (primary == null)
            {
                var primaryTransform = transform.Find("_GhostOnly");
                var secondaryTransform = Instantiate(primaryTransform, transform);
                var tetriaryTransform = Instantiate(secondaryTransform, transform);
                primary = new Overlay(primaryTransform);
                secondary = new Overlay(secondaryTransform);
                tetriary = new Overlay(tetriaryTransform);
                hoverInfo = new HoverInfo(secondaryTransform);
                tetriary.startColor = new Color(255, 255, 255);

                primary.enabled = false;
                secondary.enabled = false;
                tetriary.enabled = false;
                Initialize();
            }

            if (Keybindings.GridModFreshlyEnabled)
            {
                OnEnableGrid();
            }

            if (Keybindings.GridModFreshlyDisabled)
            {
                OnDisableGrid();
            }

            OnRefresh();
        }

        protected abstract void Initialize();
        protected abstract void OnRefresh();
        protected abstract void OnEnableGrid();
        protected abstract void OnDisableGrid();

        protected void SpeedUp(Overlay ov)
        {
            var animationCurve = new AnimationCurve();
            animationCurve.AddKey(0.0f, 0.0f);
            animationCurve.AddKey(0.5f, 1.0f);
            var minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, animationCurve);

            ov.startLifetime = 2.0f;
            ov.sizeOverLifetime = minMaxCurve;
        }

        protected void Freeze(Overlay ov)
        {
            ov.startSpeed = 0;
            ov.sizeOverLifetimeEnabled = false;
        }

        protected void VisualizeTerraformingBounds(Overlay ov)
        {
            ov.startSize = 3.0f;
            ov.psr.material.mainTexture = box;
            ov.locPosition = new Vector3(0, 0.05f, 0);
        }

        protected void VisualizeRecoloringBounds(Overlay ov)
        {
            ov.startSize = 4.0f;
            ov.psr.material.mainTexture = box;
            ov.locPosition = new Vector3(0.5f, 0.05f, 0.5f);
        }

        protected void VisualizeIconInsideTerraformingBounds(Overlay ov, Texture iconTexture)
        {
            ov.startSize = 2.5f;
            ov.psr.material.mainTexture = iconTexture;
            ov.locPosition = new Vector3(0, 0.05f, 0);
        }

        protected void VisualizeIconInsideRecoloringBounds(Overlay ov, Texture iconTexture)
        {
            ov.startSize = 3.0f;
            ov.psr.material.mainTexture = iconTexture;
            ov.position = transform.position + new Vector3(0.5f, 0.05f, 0.5f);
        }
    }

    public abstract class HoverInfoEnabled : OverlayVisualizer
    {
        protected override void Initialize()
        {
            hoverInfo.color = secondary.startColor;
        }

        protected override void OnRefresh()
        {
            if (hoverInfo.enabled)
            {
                hoverInfo.RotateToPlayer();
                hoverInfo.text = $"x: {secondary.position.x:0}, y: {secondary.position.z:0}, h: {secondary.position.y - 0.05f:0.00000}";
            }
        }

        protected override void OnEnableGrid() { }
        protected override void OnDisableGrid() { }
    }

    public abstract class SecondaryEnabledOnGridModePrimaryDisabledOnGridMode : HoverInfoEnabled
    {
        protected override void OnRefresh() {
            base.OnRefresh();
            primary.enabled = Keybindings.GridModeDisabled;
            secondary.enabled = Keybindings.GridModeEnabled;
        }
    }

    public abstract class SecondaryEnabledOnGridModePrimaryEnabledAlways : HoverInfoEnabled
    {
        protected override void OnRefresh()
        {
            base.OnRefresh();
            primary.enabled = true;
            secondary.enabled = Keybindings.GridModeEnabled;
        }
    }

    public abstract class SecondaryAndPrimaryEnabledAlways : OverlayVisualizer
    {
        protected override void OnRefresh()
        {
            primary.enabled = true;
            secondary.enabled = true;
        }
        protected override void OnEnableGrid() { }
        protected override void OnDisableGrid() { }
    }

    [HarmonyPatch(typeof(Piece), "SetInvalidPlacementHeightlight")]
    public static class OverlayVisualizationRedshiftHeighlightBlocker
    {
        private static bool Prefix(bool enabled, Piece __instance)
        {
            return __instance.GetComponentInChildren<OverlayVisualizer>() == null;
        }
    }
}
