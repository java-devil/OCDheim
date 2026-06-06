using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using static OCDheim.PlayerHelpers;
using static OCDheim.PrecisionMode;

namespace OCDheim
{

    public class OrdinaryPrecisionGridVisualizer : GridVisualizer
    {
        protected override bool ShouldInflateBorder() => KeyBinder.gridModeEnabled;
        protected override bool ShouldDeflateBorder() => KeyBinder.gridModeDisabled;
        protected override float GridDensity() => 1.0f;
    }

    public class SuperiorPrecisionGridVisualizer : GridVisualizer
    {
        protected override bool ShouldInflateBorder() => KeyBinder.gridModeEnabled && KeyBinder.precisionMode == SUPERIOR;
        protected override bool ShouldDeflateBorder() => KeyBinder.gridModeDisabled || KeyBinder.precisionMode == ORDINARY;
        protected override float GridDensity() => 0.5f;
    }
    
    [HarmonyPatch]
    public abstract class GridVisualizer : MonoBehaviour
    {
        private const float GridBorderMaxRadius = 8.0f;
        private const float BorderExpansionTime = 0.5f;
        
        private static readonly int MinColor = Shader.PropertyToID("_MinColor");
        private static readonly int MaxColor = Shader.PropertyToID("_MaxColor");
        private static readonly int AAThickness = Shader.PropertyToID("_AAThickness");
        private static readonly int LineThickness = Shader.PropertyToID("_LineThickness");
        private static readonly int BorderThickness = Shader.PropertyToID("_BorderThickness");
        private static readonly int GridRadius = Shader.PropertyToID("_GridRadius");
        private static readonly int GridResolution = Shader.PropertyToID("_GridResolution");
        private static readonly int PlayerPosition = Shader.PropertyToID("_PlayerPosition");

        private CommandBuffer cb { get; } = new CommandBuffer { name = "PlayerMask" };
        private Material material { get; set; }
        private Projector renderer { get; set; }
        private Material playerMask { get; set; }

        private float borderExpansionTimer { get; set; }
        private float borderRadius => GridBorderMaxRadius * borderExpansionTimer / BorderExpansionTime;

        private bool ShouldShowGrid() => borderRadius > 0;
        protected abstract bool ShouldInflateBorder();
        protected abstract bool ShouldDeflateBorder();
        protected abstract float GridDensity();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player))]
        [HarmonyPatch(nameof(Player.OnSpawned))]
        private static void Initialize(Player __instance)
        {
            __instance.gameObject.AddComponent<OrdinaryPrecisionGridVisualizer>();
            __instance.gameObject.AddComponent<SuperiorPrecisionGridVisualizer>();
        }

        protected void Awake()
        {
            InitializeGridRenderer();
            InitializePlayerMask();
        }

        private void InitializeGridRenderer()
        {
            var rendererGo = new GameObject();
            rendererGo.transform.rotation = Quaternion.Euler(90, 0, 0);
            renderer = rendererGo.AddComponent<Projector>();
            renderer.orthographicSize = GridBorderMaxRadius;
            renderer.orthographic = true;
            
            material = new Material(OCDheim.resourceBundle.LoadAsset<Material>("assets/worldgrid.mat"));
            material.SetColor(MinColor, new Color(0.25f, 0.25f, 0.25f, 0.25f));
            material.SetColor(MaxColor, new Color(0.45f, 0.45f, 0.45f, 0.45f));
            material.SetFloat(AAThickness, 0.02f);
            material.SetFloat(LineThickness, 0.03f);
            material.SetFloat(BorderThickness, 0.15f);
            material.SetFloat(GridResolution, GridDensity());
            renderer.material = material;
            
            renderer.ignoreLayers = ~LayerMask.GetMask("terrain");
        }
        
        private void InitializePlayerMask()
        {
            playerMask = new Material(OCDheim.resourceBundle.LoadAsset<Material>("assets/playermask.mat"));
            Camera.main?.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, cb);
        }

        private void Update()
        {
            borderExpansionTimer = RefreshBorderRadius(borderExpansionTimer, ShouldInflateBorder, ShouldDeflateBorder);
            renderer.enabled = ShouldShowGrid();
            if (renderer.enabled)
            {
                renderer.transform.position = new Vector3(playerPos.x, playerPos.y + 8.0f, playerPos.z);
                material.SetVector(PlayerPosition, new Vector4(playerPos.x, 0, playerPos.z, 0));
                material.SetFloat(GridRadius, borderRadius);
            }
            
            RefreshBuffer();
        }

        private float RefreshBorderRadius(float currentTimer, Func<bool> shouldInflate, Func<bool> shouldDeflate)
        {
            if (shouldInflate() && currentTimer < BorderExpansionTime)
            {
                return Mathf.Min(BorderExpansionTime, currentTimer + Time.deltaTime);
            }

            if (shouldDeflate() && currentTimer > 0)
            {
                return Mathf.Max(0, currentTimer - Time.deltaTime);
            }

            return currentTimer;
        }
        
        private void RefreshBuffer()
        {
            cb.Clear();
            if (ShouldShowGrid())
            {
                DrawToBuffer(player.GetComponentsInChildren<Renderer>());
                if (player.m_placementGhost)
                {
                    DrawToBuffer(player.m_placementGhost.GetComponentsInChildren<Renderer>());
                }
            }
        }

        private void DrawToBuffer(Renderer[] renderers)
        {
            foreach (var render in renderers)
            {
                if (render is MeshRenderer || render is SkinnedMeshRenderer)
                {
                    cb.DrawRenderer(render, playerMask);
                }
            }
        }

        private void OnDestroy()
        {
            if (material != null)
            {
                Destroy(material);
            }
            
            if (playerMask != null)
            {
                Destroy(playerMask);
            }

            Camera.main?.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, cb);
            cb.Release();
        }
    }
}
