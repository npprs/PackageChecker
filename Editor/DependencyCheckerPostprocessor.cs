using UnityEditor;
using System.Linq;

[InitializeOnLoad]
public class DependencyCheckerInitializer
{
    static DependencyCheckerInitializer()
    {
        if (!SessionState.GetBool("DependencyCheckerRan", false))
        {
            SessionState.SetBool("DependencyCheckerRan", true);
            EditorApplication.delayCall += () => NoppersDependencyChecker.CheckVersions();
        }
    }
}

public class DependencyCheckerPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        bool lockFileChanged = importedAssets.Any(IsLockFile) ||
                               deletedAssets.Any(IsLockFile) ||
                               movedAssets.Any(IsLockFile) ||
                               movedFromAssetPaths.Any(IsLockFile);

        if (lockFileChanged)
        {
            NoppersDependencyChecker.CheckVersions();
        }
    }

    private static bool IsLockFile(string path)
    {
        if (!path.EndsWith(".lock.json")) return false;

        string locksDir = NoppersDependencyChecker.GetLocksDirectory();
        int assetsIndex = locksDir.IndexOf("Assets");
        if (assetsIndex == -1) return false;

        string locksDirAssetPath = locksDir.Substring(assetsIndex).Replace("\\", "/");
        string normalizedPath = path.Replace("\\", "/");

        // Get the directory of the file
        int lastSlash = normalizedPath.LastIndexOf('/');
        if (lastSlash == -1) return false;

        string fileDir = normalizedPath.Substring(0, lastSlash);
        return fileDir == locksDirAssetPath;
    }
}
