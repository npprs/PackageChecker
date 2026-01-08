using System.IO;
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NOPPERS.PackageChecker.Editor.Tests")]

public class NoppersPackageChecker
{
    private static bool DEBUG = false;
    public const string MANIFEST_PATH = "Packages/vpm-manifest.json";
    internal static bool IsDebugEnabled() => DEBUG;
    internal static void SetDebugMode(bool enabled) => DEBUG = enabled;

    public class ManifestData
    {
        public Dictionary<string, PackageInfo> locked;
    }

    public class PackageInfo
    {
        public string version;
    }

    public class PackageIssue
    {
        public string PackageName { get; set; }
        public string ExpectedVersion { get; set; }
        public string ActualVersion { get; set; }  // null = missing package

        public PackageIssue(string packageName, string expectedVersion, string actualVersion = null)
        {
            PackageName = packageName;
            ExpectedVersion = expectedVersion;
            ActualVersion = actualVersion;
        }
    }

    [MenuItem("Tools/NOPPERS/PackageChecker/Check Packages")]
    public static void CheckPackagesMenuItem()
    {
        CheckPackages();
    }

    public static void CheckPackagesDelayed()
    {
        EditorApplication.delayCall += CheckPackages;
    }

    public static void CheckPackages()
    {
        // Fetch and process vpm-manifest.json
        string manifest = GetManifest(MANIFEST_PATH);

        if (manifest == null)
        {
            Log("PackageChecker -> Manifest file could not be read.");
            return;
        }

        var manifestJSON = JsonConvert.DeserializeObject<ManifestData>(manifest);

        if (!ValidateManifestStructure(manifestJSON))
        {
            Log("PackageChecker -> Manifest JSON structure is invalid.");
            return;
        }

        // Fetch and process all /locks/*.json files
        string locksDir = GetLocksDirectory();

        if (!Directory.Exists(locksDir))
        {
            Log($"PackageChecker -> Locks directory not found: {locksDir}");
            return;
        }

        var lockFiles = GetLockFiles(locksDir, GetManifest, ValidateManifestStructure);
        if (lockFiles.Count == 0)
        {
            return;
        }

        var mergedRequirements = MergeLockFiles(lockFiles, IsVersionGreater);

        var issues = CompareManifestsWithMerged(mergedRequirements, manifest);

        if (issues != null && issues.Count > 0)
        {
            PackageIssuesWindow.ShowWindow(issues);
        }
    }


    [MenuItem("Tools/NOPPERS/PackageChecker/Generate Lock File")]
    public static void OpenGenerateLockWindow()
    {
        GenerateLockWindow.ShowWindow();
    }

    internal static string GetManifest(string path)
    {
        if (!File.Exists(path))
        {
            Log("PackageChecker -> JSON file not found at path: " + path);
            return null;
        }

        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            Log($"PackageChecker -> Error reading file: {path} - {ex.Message}");
            return null;
        }
    }

    public static string GetLocksDirectory([System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "")
    {
        string editorDir = Path.GetDirectoryName(sourceFilePath);
        return Path.GetFullPath(Path.Combine(editorDir, "..", "Locks"));
    }

    internal static bool ValidateManifestStructure(ManifestData data)
    {
        if (data == null)
        {
            Log("PackageChecker -> data is null");
            return false;
        }

        if (data.locked == null)
        {
            Log("PackageChecker -> locked field is null");
            return false;
        }

        if (data.locked.Count == 0)
        {
            Log("PackageChecker -> locked field is empty");
            return false;
        }

        foreach (var dep in data.locked)
        {
            if (string.IsNullOrWhiteSpace(dep.Key))
            {
                Log("PackageChecker -> package key is null or whitespace");
                return false;
            }

            if (dep.Value == null || string.IsNullOrWhiteSpace(dep.Value.version))
            {
                Log($"PackageChecker -> package '{dep.Key}' has null or whitespace version");
                return false;
            }
        }

        return true;
    }

    internal static Dictionary<string, string> MergeLockFiles(
        List<ManifestData> lockFiles,
        Func<string, string, bool> isVersionGreaterFn
    )
    {
        var merged = new Dictionary<string, string>();

        foreach (var lockFile in lockFiles)
        {
            foreach (var package in lockFile.locked)
            {
                string packageName = package.Key;
                string requiredVersion = package.Value.version;

                if (!merged.ContainsKey(packageName))
                {
                    merged[packageName] = requiredVersion;
                }
                else
                {
                    string existingVersion = merged[packageName];
                    if (isVersionGreaterFn(requiredVersion, existingVersion))
                    {
                        merged[packageName] = requiredVersion;
                    }
                }
            }
        }

        return merged;
    }

    internal static List<ManifestData> GetLockFiles(
        string locksDir,
        Func<string, string> getManifestFn,
        Func<ManifestData, bool> validateManifestFn
    )
    {
        var lockFiles = new List<ManifestData>();

        var jsonFiles = Directory.GetFiles(locksDir, "*.json", SearchOption.TopDirectoryOnly);
        if (jsonFiles.Length == 0)
        {
            return lockFiles;
        }

        foreach (var jsonFile in jsonFiles)
        {
            string lockJson = getManifestFn(jsonFile);
            if (lockJson == null)
            {
                Log($"PackageChecker -> Failed to read: {jsonFile}");
                continue;
            }

            var lockData = JsonConvert.DeserializeObject<ManifestData>(lockJson);
            if (!validateManifestFn(lockData))
            {
                Log($"PackageChecker -> Invalid structure in: {jsonFile}");
                continue;
            }

            lockFiles.Add(lockData);
        }

        return lockFiles;
    }

    internal static List<PackageIssue> CompareManifestsWithMerged(
        Dictionary<string, string> requirements,
        string manifestJson
    )
    {
        var manifest = JsonConvert.DeserializeObject<ManifestData>(manifestJson);

        if (manifest?.locked == null)
        {
            Log("PackageChecker -> Missing locked field in manifest");
            return null;
        }

        var issues = new List<PackageIssue>();

        foreach (var item in requirements)
        {
            string packageName = item.Key;
            string requiredVersion = item.Value;

            if (!manifest.locked.ContainsKey(packageName))
            {
                Log($"Missing: {packageName} (v{requiredVersion})");
                issues.Add(new PackageIssue(
                    packageName,
                    requiredVersion
                ));
                continue;
            }

            string installedVersion = manifest.locked[packageName].version!;
            if (installedVersion != requiredVersion)
            {
                Log($"Version mismatch {packageName}: required={requiredVersion}, installed={installedVersion}");
                issues.Add(new PackageIssue(
                    packageName,
                    requiredVersion,
                    installedVersion
                ));
            }
        }

        return issues;
    }

    public static ManifestData ComposeLockFile(
        Dictionary<string, PackageInfo> currentPackages,
        Dictionary<string, bool> packageSelection
    )
    {
        var selectedPackages = new Dictionary<string, PackageInfo>();

        if (currentPackages != null)
        {
            foreach (var package in currentPackages)
            {
                if (packageSelection.TryGetValue(package.Key, out bool isSelected) && isSelected)
                {
                    selectedPackages[package.Key] = package.Value;
                }
            }
        }

        return new ManifestData
        {
            locked = selectedPackages
        };
    }

    internal static bool CreateLockFile(
        ManifestData lockData,
        string fileName,
        string locksDirectory
    )
    {
        try
        {
            string fullPath = Path.Combine(locksDirectory, fileName);

            // Create directory if it doesn't exist
            if (!Directory.Exists(locksDirectory))
            {
                Directory.CreateDirectory(locksDirectory);
            }

            // Write lock file
            string json = JsonConvert.SerializeObject(lockData, Formatting.Indented);
            File.WriteAllText(fullPath, json);

            // Refresh to create .meta file
            AssetDatabase.Refresh();

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create lock file '{fileName}': {ex.Message}");
            return false;
        }
    }

    internal static bool UpdateManifest(
        string manifestPath,
        List<PackageIssue> issues,
        Func<string, string> getManifestFn,
        Func<string, string, bool> writeManifestFn
    )
    {
        try
        {
            string manifestJson = getManifestFn(manifestPath);
            if (manifestJson == null)
            {
                Debug.LogError("Failed to read manifest");
                return false;
            }

            // Use JObject for in-place editing to preserve all fields
            var jManifest = Newtonsoft.Json.Linq.JObject.Parse(manifestJson);
            var locked = jManifest["locked"] as Newtonsoft.Json.Linq.JObject;

            if (locked == null)
            {
                Debug.LogError("Manifest has invalid structure (missing 'locked' field)");
                return false;
            }

            // Update versions based on issues
            foreach (var issue in issues)
            {
                var package = locked[issue.PackageName] as Newtonsoft.Json.Linq.JObject;

                if (package != null)
                {
                    // Update existing package version
                    package["version"] = issue.ExpectedVersion;
                }
                else
                {
                    // Add missing package to locked
                    locked[issue.PackageName] = new Newtonsoft.Json.Linq.JObject
                    {
                        ["version"] = issue.ExpectedVersion,
                        ["dependencies"] = new Newtonsoft.Json.Linq.JObject()
                    };
                }
            }

            // Write updated manifest back to file with indentation
            string updatedJson = jManifest.ToString(Newtonsoft.Json.Formatting.Indented);
            return writeManifestFn(manifestPath, updatedJson);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to update manifest: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    private static void Log(string message)
    {
        if (DEBUG)
        {
            Debug.Log(message);
        }
    }

    public static bool IsVersionGreater(string currentVersion, string newVersion)
    {
        try
        {
            var currentVer = SemanticVersioning.Version.Parse(currentVersion);
            var newVer = SemanticVersioning.Version.Parse(newVersion);
            return currentVer > newVer;
        }
        catch
        {
            return false;
        }
    }
}