using System;
using HarmonyLib;
using UnityEngine;

using static OCDheim.PieceHelpers;

namespace OCDheim
{
    // Helper classes for OverlayVisualizerImpls, intended to abstract away unnecessary low level complexity.
    public abstract class OverlayVisualizer : MonoBehaviour
    {
        public static Texture2D remove { get; } = OCDheim.LoadTextureFromDisk("remove.png");
        protected static Texture2D cross { get; } = OCDheim.LoadTextureFromDisk("cross.png");
        protected static Texture2D undo { get; } = OCDheim.LoadTextureFromDisk("undo.png");
        protected static Texture2D redo { get; } = OCDheim.LoadTextureFromDisk("redo.png");
        private static Texture2D box { get; } = OCDheim.LoadTextureFromDisk("box.png");

        protected Overlay primary { get; private set; }
        protected Overlay secondary { get; private set; }
        protected Overlay tertiary { get; private set; }
        protected HoverInfo hoverInfo { get; private set; }
        
        private const int OverlayLayer = 31;
        private Camera mainCamera { get; set; }
        private Camera overlayCamera { get; set; }
        private GameObject overlayCameraGo { get; set; }

        private void Awake()
        {
            var primaryTransform = transform.Find("_GhostOnly");
            var secondaryTransform = Instantiate(primaryTransform, transform);
            var tertiaryTransform = Instantiate(secondaryTransform, transform);

            primary = new Overlay(primaryTransform);
            secondary = new Overlay(secondaryTransform);
            tertiary = new Overlay(tertiaryTransform);
            hoverInfo = new HoverInfo(transform);
            tertiary.startColor = new Color(255, 255, 255);

            primary.enabled = false;
            secondary.enabled = false;
            tertiary.enabled = false;

            InitializeOverlay();
            InitializeCameras();
        }

        private void InitializeCameras()
        {
            mainCamera = Camera.main ?? throw new InvalidOperationException();
            overlayCameraGo = new GameObject();
            overlayCameraGo.transform.SetParent(mainCamera.transform, false);
            overlayCamera = overlayCameraGo.AddComponent<Camera>();
            overlayCamera.CopyFrom(mainCamera);
            
            overlayCamera.clearFlags = CameraClearFlags.Depth;
            overlayCamera.depth = mainCamera.depth + 1;
            overlayCamera.cullingMask = 0;
            
            OverrideLayerRecursively(primary.psr.gameObject);
            OverrideLayerRecursively(secondary.psr.gameObject);
            OverrideLayerRecursively(tertiary.psr.gameObject);
        }

        private void OverrideLayerRecursively(GameObject go)
        {
            go.layer = OverlayLayer;
            foreach (Transform child in go.transform)
            {
                OverrideLayerRecursively(child.gameObject);
            }
        }

        private void Update()
        {
            if (KeyBinder.gridModFreshlyEnabled)
            {
                OnEnableGrid();
            }

            if (KeyBinder.gridModFreshlyDisabled)
            {
                OnDisableGrid();
            }
            
            if (KeyBinder.gridModeEnabled)
            {
                EnableOverlayCamera();
            }

            if (KeyBinder.gridModeDisabled)
            {
                DisableOverlayCamera();
            }

            OnRefresh();
        }
        
        private void EnableOverlayCamera()
        {
            mainCamera.cullingMask &= ~(1 << OverlayLayer);
            overlayCamera.cullingMask = 1 << OverlayLayer;
        }

        private void DisableOverlayCamera()
        {
            if (mainCamera && overlayCamera)
            {
                mainCamera.cullingMask |= (1 << OverlayLayer);
                overlayCamera.cullingMask = 0;
            }
        }

        private void OnDestroy()
        {
            DisableOverlayCamera();
            Destroy(overlayCameraGo);
        }

        protected abstract void InitializeOverlay();
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
        
        protected void ScaleVertically(Overlay ov)
        {
            var h = 10;
            var pos = new Vector2(ov.worldPosition.x, ov.worldPosition.z);
            var y = PrecisionDrill.DrillDownTillGround(pos).y;
            var Δ = ov.worldPosition.y - y;

            ov.scale = new Vector3(1.0f, Δ / h, 1.0f);
        }

        protected void VisualizeTerraformingBounds(Overlay ov)
        {
            ov.startSize = 3.0f;
            ov.psr.material.mainTexture = box;
        }

        protected void VisualizeRecoloringBounds(Overlay ov)
        {
            ov.startSize = 4.5f;
            ov.psr.material.mainTexture = box;
        }

        protected void VisualizeIconInsideTerraformingBounds(Overlay ov, Texture iconTexture)
        {
            ov.startSize = 2.5f;
            ov.psr.material.mainTexture = iconTexture;
        }

        protected void VisualizeIconInsideRecoloringBounds(Overlay ov, Texture iconTexture)
        {
            ov.startSize = 3.0f;
            ov.psr.material.mainTexture = iconTexture;
            ov.localPosition = new Vector3(0.5f, 0.0f, 0.5f);
        }
    }

    public abstract class HoverInfoEnabled : OverlayVisualizer
    {
        protected override void InitializeOverlay()
        {
            hoverInfo.color = secondary.startColor;
            hoverInfo.enabled = KeyBinder.gridModeEnabled;
        }

        protected override void OnRefresh()
        {
            if (hoverInfo.enabled)
            {
                hoverInfo.RotateToPlayer();
                hoverInfo.text = $"x: {secondary.worldPosition.x:0}, y: {secondary.worldPosition.z:0}\nh: {secondary.worldPosition.y:0.0000}";
            }
        }

        protected override void OnEnableGrid()
        {
            hoverInfo.enabled = true;
        }

        protected override void OnDisableGrid()
        {
            hoverInfo.enabled = false;
        }
    }

    public abstract class SecondaryEnabledOnGridModePrimaryDisabledOnGridMode : HoverInfoEnabled
    {
        protected override void OnRefresh() {
            base.OnRefresh();
            ScaleVertically(secondary);
            primary.enabled = KeyBinder.gridModeDisabled;
            secondary.enabled = KeyBinder.gridModeEnabled;
        }
    }

    public abstract class SecondaryEnabledOnGridModePrimaryEnabledAlways : HoverInfoEnabled
    {
        protected override void OnRefresh()
        {
            base.OnRefresh();
            primary.enabled = true;
            secondary.enabled = KeyBinder.gridModeEnabled;
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

    [HarmonyPatch]
    public static class OverlayVisualizerRedshiftBlocker
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Piece))]
        [HarmonyPatch(nameof(Piece.SetInvalidPlacementHeightlight))]
        private static bool BlockOverlayVisualizerRedshift()
        {
            return buildPiece.GetComponentInChildren<OverlayVisualizer>() == null;
        }
    }
}
