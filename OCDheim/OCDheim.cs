using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System.IO;
using UnityEngine;
using static OCDheim.PlayerHelpers;

namespace OCDheim
{
    [BepInPlugin(GUID, Name, Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
    [NetworkCompatibility(CompatibilityLevel.VersionCheckOnly, VersionStrictness.Minor)]
    public class OCDheim : BaseUnityPlugin
    {
        public const string GUID = "dymek.dev.OCDheim";
        private const string Name = "OCDheim";
        private const string Version = "0.3.1";
        private const string RemoveTerrainModificationsPieceName = "Remove Terrain Modifications";
        private const string RemoveTerrainModificationsPrefabName = "remove_terrain_modifications";

        public static AssetBundle resourceBundle { get; } = LoadResourceBundle();
        private Texture2D brick1x1 { get; } = LoadTextureFromDisk("brick_1x1.png");
        private Texture2D brick2x1 { get; } = LoadTextureFromDisk("brick_2x1.png");
        private Texture2D brick2x2 { get; } = LoadTextureFromDisk("brick_2x2.png");
        private Texture2D brick1x2 { get; } = LoadTextureFromDisk("brick_1x2.png");
        private Texture2D brick4x2 { get; } = LoadTextureFromDisk("brick_4x2.png");
        private List<Piece> bricks { get; } = new List<Piece>();
        private Harmony harmony { get; } = new Harmony(GUID);

        private static AssetBundle LoadResourceBundle()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsServer:
                    return AssetUtils.LoadAssetBundleFromResources(Path.Combine("bundle_windows"));
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                case RuntimePlatform.LinuxServer:
                    return AssetUtils.LoadAssetBundleFromResources(Path.Combine("bundle_linux"));
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXServer:
                    return AssetUtils.LoadAssetBundleFromResources(Path.Combine("bundle_osx"));
                default:
                    throw new PlatformNotSupportedException(Application.platform.ToString());
            }
        }

        public static Texture2D LoadTextureFromDisk(string fileName)
        {
            var modDir = Path.GetDirectoryName(typeof(OCDheim).Assembly.Location) ?? throw new InvalidOperationException();
            var fullPath = Path.Combine(modDir,  fileName);

            return AssetUtils.LoadTexture(fullPath);
        }

        private void Awake()
        {
            global::OCDheim.Config.Bind(Config);
            global::OCDheim.Config.additionalBuildPieces.SettingChanged += (_, args) => FlipBrickBuildPieceAvailability();
            global::OCDheim.Config.removeTerrainModifications.SettingChanged += (_, args) => FlipRemoveTerrainModificationsAvailability();

            harmony.PatchAll();
            gameObject.AddComponent<KeyBinder>();
            PrefabManager.OnVanillaPrefabsAvailable += AddOCDheimToolPieces;
            PrefabManager.OnVanillaPrefabsAvailable += AddOCDheimBuildPieces;
            PrefabManager.OnVanillaPrefabsAvailable += ModVanillaValheimTools;
        }

        private void AddOCDheimToolPieces()
        {
            //AddToolPiece<UndoModificationsOverlayVisualizer>("Undo Terrain Modification", "mud_road_v2", "Hoe", OverlayVisualizer.undo);
            //AddToolPiece<RedoModificationsOverlayVisualizer>("Redo Terrain Modification", "mud_road_v2", "Hoe", OverlayVisualizer.redo);
            AddToolPiece<RemoveModificationsOverlayVisualizer>(RemoveTerrainModificationsPrefabName, RemoveTerrainModificationsPieceName, "mud_road_v2", "Hoe", OverlayVisualizer.remove);
            FlipRemoveTerrainModificationsAvailability();
        }

        private void FlipRemoveTerrainModificationsAvailability()
        {
            var tool = PieceManager.Instance.GetPiece(RemoveTerrainModificationsPrefabName);
            tool.Piece.m_enabled = global::OCDheim.Config.removeTerrainModifications.Value;

            if (player)
            {
                player.UpdateKnownRecipesList();
                player.UpdateAvailablePiecesList();
            }
        }

        private void AddToolPiece<TOverlayVisualizer>(string prefabName, string pieceName, string basePieceName, string pieceTable, Texture2D iconTexture, bool level = false, bool raise = false, bool smooth = false, bool paint = false) where TOverlayVisualizer: OverlayVisualizer
        {
            var pieceExists = PieceManager.Instance.GetPiece(prefabName);
            if (pieceExists != null) { return; }

            var pieceIcon = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), Vector2.zero);
            var piece = new CustomPiece(prefabName, basePieceName, new PieceConfig
            {
                Name = pieceName,
                Icon = pieceIcon,
                PieceTable = pieceTable
            });

            var toolPiece = piece.PiecePrefab.GetComponent<TerrainOp>();
            toolPiece.m_settings.m_level = level;
            toolPiece.m_settings.m_raise = raise;
            toolPiece.m_settings.m_smooth = smooth;
            toolPiece.m_settings.m_paintCleared = paint;
            piece.PiecePrefab.AddComponent<TOverlayVisualizer>();

            PieceManager.Instance.AddPiece(piece);
            TerrainOpRegistry.Register(toolPiece);
        }

        private void AddOCDheimBuildPieces()
        {
            AddBrickBuildPiece("1x1", new Vector3(0.5f, 1.0f, 0.5f), 3, brick1x1);
            AddBrickBuildPiece("2x1", new Vector3(1.0f, 1.0f, 0.5f), 4, brick2x1);
            AddBrickBuildPiece("1x2", new Vector3(0.5f, 2.0f, 0.5f), 5, brick1x2);
            AddBrickBuildPiece("4x2", new Vector3(2.0f, 2.0f, 0.5f), 6, brick4x2);
            AddBrickBuildPiece("2x2 (Vertical)", new Vector3(1.0f, 2.0f, 0.5f), 5, brick2x2);

            PrefabManager.OnVanillaPrefabsAvailable -= AddOCDheimBuildPieces;
        }

        private void AddBrickBuildPiece(string brickSuffix, Vector3 brickScale, int brickPrice, Texture2D iconTexture)
        {
            var brickName = $"Smooth Stone {brickSuffix}";
            var snakeSuffix = brickSuffix.Replace(" ", "_").Replace("(", "").Replace(")", "").ToLower();
            var brickExists = PieceManager.Instance.GetPiece(brickName);
            if (brickExists != null) { return; }
            
            var brick = PrefabManager.Instance.CreateClonedPrefab($"stone_floor_{snakeSuffix}", "stone_floor_2x2");
            var brickIcon = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), Vector2.zero);
            brick.transform.localScale = brickScale;

            var brickConfig = new PieceConfig();
            brickConfig.Name = brickName;
            brickConfig.PieceTable = "Hammer";
            brickConfig.Category = "HeavyBuild";
            brickConfig.Icon = brickIcon;
            brickConfig.Enabled = global::OCDheim.Config.additionalBuildPieces.Value;
            brickConfig.AddRequirement(new RequirementConfig("Stone", brickPrice));

            PieceManager.Instance.AddPiece(new CustomPiece(brick, false, brickConfig));
            bricks.Add(brick.GetComponent<Piece>());
        }

        private void FlipBrickBuildPieceAvailability()
        {
            foreach (var brick in bricks)
            {
                brick.m_enabled = global::OCDheim.Config.additionalBuildPieces.Value;
            }

            if (player)
            {
                player.UpdateKnownRecipesList();
                player.UpdateAvailablePiecesList();
            }
        }

        private void ModVanillaValheimTools()
        {
            PrefabManager.Instance.GetPrefab("mud_road_v2").AddComponent<LevelGroundOverlayVisualizer>();
            PrefabManager.Instance.GetPrefab("raise_v2").AddComponent<RaiseGroundOverlayVisualizer>();
            PrefabManager.Instance.GetPrefab("path_v2").AddComponent<PaveRoadOverlayVisualizer>();
            PrefabManager.Instance.GetPrefab("paved_road_v2").AddComponent<PaveRoadOverlayVisualizer>();
            PrefabManager.Instance.GetPrefab("cultivate_v2").AddComponent<CultivateOverlayVisualizer>();
            PrefabManager.Instance.GetPrefab("replant_v2").AddComponent<SeedGrassOverlayVisualizer>();

            PrefabManager.OnVanillaPrefabsAvailable -= ModVanillaValheimTools;
        }
    }
}
