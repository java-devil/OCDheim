using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System.IO;
using UnityEngine;

namespace OCDheim
{
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInPlugin("dymek.dev.OCDheim", "OCDheim", "0.1.3")]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class OCDheim : BaseUnityPlugin
    {        
        private Texture2D brick1x1 { get { return LoadTextureFromDisk("brick_1x1.png"); } }
        private Texture2D brick2x1 { get { return LoadTextureFromDisk("brick_2x1.png"); } }
        private Texture2D brick1x2 { get { return LoadTextureFromDisk("brick_1x2.png"); } }
        private Texture2D brick4x2 { get { return LoadTextureFromDisk("brick_4x2.png"); } }
        private Harmony harmony { get; } = new Harmony("dymek.OCDheim");
        private static OCDheim ocdheim { get; set; }

        public static Texture2D LoadTextureFromDisk(string fileName) => AssetUtils.LoadTexture(Path.Combine(Path.GetDirectoryName(ocdheim.Info.Location), fileName));

        public void Awake()
        {
            ocdheim = this;
            harmony.PatchAll();
            PrefabManager.OnVanillaPrefabsAvailable += AddOCDheimToolPieces;
            PrefabManager.OnVanillaPrefabsAvailable += AddOCDheimBuildPieces;
            PrefabManager.OnVanillaPrefabsAvailable += ModVanillaValheimTools;
        }

        private void AddOCDheimToolPieces()
        {
            //AddToolPiece<UndoModificationsOverlayVisualizer>("Undo Terrain Modification", "mud_road_v2", "Hoe", OverlayVisualizer.undo);
            //AddToolPiece<RedoModificationsOverlayVisualizer>("Redo Terrain Modification", "mud_road_v2", "Hoe", OverlayVisualizer.redo);
            AddToolPiece<RemoveModificationsOverlayVisualizer>("Remove Terrain Modifications", "mud_road_v2", "Hoe", OverlayVisualizer.remove);
        }

        private void AddToolPiece<TOverlayVisualizer>(string pieceName, string basePieceName, string pieceTable, Texture2D iconTexture, bool level = false, bool raise = false, bool smooth = false, bool paint = false) where TOverlayVisualizer: OverlayVisualizer
        {
            var pieceExists = PieceManager.Instance.GetPiece(pieceName) != null;
            if (pieceExists)
                return;
            
            var pieceIcon = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), Vector2.zero);
            var piece = new CustomPiece(pieceName, basePieceName, new PieceConfig
            {
                Name = pieceName,
                Icon = pieceIcon,
                PieceTable = pieceTable
            });

            var settings = piece.PiecePrefab.GetComponent<TerrainOp>().m_settings;
            settings.m_level = level;
            settings.m_raise = raise;
            settings.m_smooth = smooth;
            settings.m_paintCleared = paint;
            piece.PiecePrefab.AddComponent<TOverlayVisualizer>();

            

            PieceManager.Instance.AddPiece(piece);
        }

        private void AddOCDheimBuildPieces()
        {
            AddBrickBuildPiece("1x1", new Vector3(0.5f, 1.0f, 0.5f), 3, brick1x1);
            AddBrickBuildPiece("2x1", new Vector3(1.0f, 1.0f, 0.5f), 4, brick2x1);
            AddBrickBuildPiece("4x2", new Vector3(2.0f, 2.0f, 0.5f), 6, brick4x2);
            AddBrickBuildPiece("1x2", new Vector3(0.5f, 2.0f, 0.5f), 5, brick1x2);

            PrefabManager.OnVanillaPrefabsAvailable -= AddOCDheimBuildPieces;
        }

        private void AddBrickBuildPiece(string brickSuffix, Vector3 brickScale, int brickPrice, Texture2D iconTexture)
        {
            var pieceExists = PieceManager.Instance.GetPiece($"stone_floor_{brickSuffix}") != null;
            if (pieceExists)
                return;

            var brick = PrefabManager.Instance.CreateClonedPrefab($"stone_floor_{brickSuffix}", "stone_floor_2x2");
            var brickIcon = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), Vector2.zero);
            brick.transform.localScale = brickScale;

            var brickConfig = new PieceConfig();
            brickConfig.Name = $"Smooth stone {brickSuffix}";
            brickConfig.PieceTable = "Hammer";
            brickConfig.Category = "Building";
            brickConfig.Icon = brickIcon;
            brickConfig.AddRequirement(new RequirementConfig("Stone", brickPrice, 0, true));

            PieceManager.Instance.AddPiece(new CustomPiece(brick, false, brickConfig));
        }

        private void ModVanillaValheimTools()
        {
            PrefabManager.Instance.GetPrefab("mud_road_v2").AddComponent<LevelGroundOverlayVisualizer>();
            PrefabManager.Instance.GetPrefab("raise_v2").AddComponent<RaiseGroundOverlayVisualizer>();
            PrefabManager.Instance.GetPrefab("path_v2").AddComponent<PaveRoadOverlayVisualizer>();
            PrefabManager.Instance.GetPrefab("paved_road_v2").AddComponent<PaveRoadOverlayVisualizer>();
            PrefabManager.Instance.GetPrefab("cultivate_v2").AddComponent<CultivateOverlayVisualizer>();
            PrefabManager.Instance.GetPrefab("replant_v2").AddComponent<SeedGrassOverlayVisualizer>();
        }
    }
}
