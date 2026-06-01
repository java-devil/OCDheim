using UnityEditor;
using System.IO;

public class BuildAssetBundleScript
{
    [MenuItem("Assets/Build Cross-Platform Bundles")]
    static void BuildAllAssetBundles()
    {
        var dir = "Assets/AssetBundles";
        BuildTarget[] platforms = {
            BuildTarget.StandaloneWindows64,
            BuildTarget.StandaloneLinux64,
            BuildTarget.StandaloneOSX,
        };

        foreach (var platform in platforms) {
            var subdir = Path.Combine(dir, platform.ToString());
            if (!Directory.Exists(subdir))
            {
                Directory.CreateDirectory(subdir);
            }

            BuildPipeline.BuildAssetBundles(subdir, BuildAssetBundleOptions.None, platform);
        }
        
    }
}
