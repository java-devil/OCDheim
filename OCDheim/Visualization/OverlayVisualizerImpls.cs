using UnityEngine;

namespace OCDheim
{
    // Minimal classes describing specific VFXs of specific "ToolOps" in a DSL-like form.
    public class LevelGroundOverlayVisualizer : SecondaryEnabledOnGridModePrimaryDisabledOnGridMode
    {
        protected override void Initialize()
        {
            base.Initialize();
            SpeedUp(secondary);
            VisualizeTerraformingBounds(secondary);
        }
    }

    public class RaiseGroundOverlayVisualizer : SecondaryEnabledOnGridModePrimaryEnabledAlways
    {
        protected override void Initialize()
        {
            base.Initialize();
            Freeze(secondary);
            Freeze(tetriary);
            VisualizeTerraformingBounds(secondary);
            VisualizeTerraformingBounds(tetriary);
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            if (Keybindings.GridModeEnabled)
            {
                GroundLevelSpinner.Refresh();
                secondary.locPosition = new Vector3(0f, GroundLevelSpinner.value, 0f);

                if (GroundLevelSpinner.value > 0f)
                {
                    hoverInfo.text = $"h: +{secondary.locPosition.y:0.00}";
                }
                else
                {
                    hoverInfo.text = $"x: {secondary.position.x:0}, y: {secondary.position.z:0}, h: {secondary.position.y:0.00000}";
                }
            }
            tetriary.enabled = Keybindings.GridModeEnabled;
        }
    }
    public class PaveRoadOverlayVisualizer : SecondaryEnabledOnGridModePrimaryDisabledOnGridMode
    {
        protected override void Initialize()
        {
            base.Initialize();
            SpeedUp(secondary);
            VisualizeRecoloringBounds(secondary);
        }
    }

    public class CultivateOverlayVisualizer : PaveRoadOverlayVisualizer
    {
        protected override void OnRefresh()
        {
            base.OnRefresh();
            hoverInfo.color = secondary.color;
        }
    }

    public class SeedGrassOverlayVisualizer : SecondaryEnabledOnGridModePrimaryEnabledAlways
    {
        protected override void Initialize()
        {
            base.Initialize();
            Freeze(secondary);
            VisualizeRecoloringBounds(secondary);
            primary.locPosition = new Vector3(0, 2.0f, 0);
        }

        protected override void OnRefresh()
        {
            if (Keybindings.GridModeEnabled)
            {
                primary.startSize = 4.0f;
                primary.locPosition = new Vector3(0.5f, 2.5f, 0.5f);
            }
            else
            {
                primary.startSize = 5.5f;
                primary.locPosition = new Vector3(0, 2.5f, 0);
            }
            base.OnRefresh();
        }
        protected override void OnEnableGrid()
        {
            base.OnEnableGrid();
            primary.enabled = false;
            primary.startSize = 4.0f;
            primary.locPosition = new Vector3(0.5f, 2.5f, 0.5f);
            primary.enabled = true;
        }
        protected override void OnDisableGrid()
        {
            primary.enabled = false;
            primary.locPosition = new Vector3(0, 2.5f, 0);
            primary.startSize = 5.5f;
            primary.enabled = true;
            base.OnDisableGrid();
        }
    }

    public class RemoveModificationsOverlayVisualizer : SecondaryAndPrimaryEnabledAlways
    {
        protected override void Initialize()
        {
            Freeze(primary);
            SpeedUp(secondary);
            VisualizeTerraformingBounds(primary);
            VisualizeIconInsideTerraformingBounds(secondary, cross);
        }
    }

    public abstract class UndoRedoModificationsOverlayVisualizer : SecondaryAndPrimaryEnabledAlways
    {
        protected override void Initialize()
        {
            Freeze(primary);
            Freeze(secondary);
            VisualizeRecoloringBounds(primary);
            VisualizeIconInsideRecoloringBounds(secondary, icon());
        }

        protected abstract Texture2D icon();
    }

    public class UndoModificationsOverlayVisualizer : UndoRedoModificationsOverlayVisualizer
    {
        protected override Texture2D icon()
        {
            return undo;
        }
    }

    public class RedoModificationsOverlayVisualizer : UndoRedoModificationsOverlayVisualizer
    {
        protected override Texture2D icon()
        {
            return redo;
        }
    }
}
