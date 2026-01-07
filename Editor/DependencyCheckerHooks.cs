using UnityEditor;
using System.Linq;
using System.IO;

// Centralized lifecycle hook control for dependency checker
public static class DependencyCheckerHooks
{
    public static bool Enabled = false;
}

[InitializeOnLoad]
public class DependencyCheckerInitializer
{
    private const string LAST_MANIFEST_TIME_KEY = "DependencyChecker_LastManifestTime";
    private const string LAST_SESSION_KEY = "DependencyChecker_LastSessionID";

    static DependencyCheckerInitializer()
    {
        // Check on every domain reload (Unity startup, script compilation, package changes)
        if (!DependencyCheckerHooks.Enabled) return;
        CheckIfManifestChanged();
    }

    private static void CheckIfManifestChanged()
    {
        if (!DependencyCheckerHooks.Enabled) return;

        string manifestPath = Path.GetFullPath(NoppersDependencyChecker.MANIFEST_PATH);
        if (!File.Exists(manifestPath))
        {
            return;
        }

        // Detect if this is a new Unity session (not just domain reload)
        string currentSessionID = SessionState.GetString(LAST_SESSION_KEY, "");
        bool isNewSession = string.IsNullOrEmpty(currentSessionID);

        if (isNewSession)
        {
            // Generate unique session ID for this Unity session
            SessionState.SetString(LAST_SESSION_KEY, System.Guid.NewGuid().ToString());
        }

        // Get last modified time
        string lastModifiedTime = ((System.DateTimeOffset)File.GetLastWriteTimeUtc(manifestPath)).ToUnixTimeSeconds().ToString();
        // Get last stored time
        string lastStoredTime = SessionState.GetString(LAST_MANIFEST_TIME_KEY, "0");

        // Update stored time
        SessionState.SetString(LAST_MANIFEST_TIME_KEY, lastModifiedTime);

        // Run if new Unity session OR manifest changed
        if (isNewSession || lastModifiedTime != lastStoredTime)
        {
            NoppersDependencyChecker.CheckVersionsDelayed();
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
        if (!DependencyCheckerHooks.Enabled) return;

        bool lockFileChanged = importedAssets.Where(IsLockFile).Any(IsLocksDirectory) ||
                               deletedAssets.Where(IsLockFile).Any(IsLocksDirectory) ||
                               movedAssets.Where(IsLockFile).Any(IsLocksDirectory) ||
                               movedFromAssetPaths.Where(IsLockFile).Any(IsLocksDirectory);

        if (lockFileChanged)
        {
            NoppersDependencyChecker.CheckVersionsDelayed();
        }
    }

    private static bool IsLockFile(string path)
    {
        return path.EndsWith(".lock.json");
    }

    private static bool IsLocksDirectory(string path)
    {
        string locksDirAbsolutePath = NoppersDependencyChecker.GetLocksDirectory();
        string fileDirAbsolutePath = Path.GetDirectoryName(Path.GetFullPath(path));

        return fileDirAbsolutePath == locksDirAbsolutePath;
    }
}
