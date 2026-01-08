using UnityEditor;
using System.Linq;
using System.IO;

// Centralized lifecycle hook control for package checker
public static class PackageCheckerHooks
{
    public static bool Enabled = true;
}

[InitializeOnLoad]
public class PackageCheckerInitializer
{
    private const string LAST_MANIFEST_TIME_KEY = "PackageChecker_LastManifestTime";
    private const string LAST_SESSION_KEY = "PackageChecker_LastSessionID";

    static PackageCheckerInitializer()
    {
        // Check on every domain reload (Unity startup, script compilation, package changes)
        if (!PackageCheckerHooks.Enabled) return;
        CheckIfManifestChanged();
    }

    private static void CheckIfManifestChanged()
    {
        if (!PackageCheckerHooks.Enabled) return;

        string manifestPath = Path.GetFullPath(NoppersPackageChecker.MANIFEST_PATH);
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
            NoppersPackageChecker.CheckPackagesDelayed();
        }
    }
}

public class PackageCheckerPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (!PackageCheckerHooks.Enabled) return;

        bool lockFileChanged = importedAssets.Where(IsLockFile).Any(IsLocksDirectory) ||
                               deletedAssets.Where(IsLockFile).Any(IsLocksDirectory) ||
                               movedAssets.Where(IsLockFile).Any(IsLocksDirectory) ||
                               movedFromAssetPaths.Where(IsLockFile).Any(IsLocksDirectory);

        if (lockFileChanged)
        {
            NoppersPackageChecker.CheckPackagesDelayed();
        }
    }

    private static bool IsLockFile(string path)
    {
        return path.EndsWith(".lock.json");
    }

    private static bool IsLocksDirectory(string path)
    {
        string locksDirAbsolutePath = NoppersPackageChecker.GetLocksDirectory();
        string fileDirAbsolutePath = Path.GetDirectoryName(Path.GetFullPath(path));

        return fileDirAbsolutePath == locksDirAbsolutePath;
    }
}
