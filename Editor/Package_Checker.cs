#nullable enable
using System.IO;
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using VRC.SDKBase.Editor.BuildPipeline;

public class NoppersDependencyChecker
{
    // This determines when your callback runs (lower = earlier)
    private static bool DEBUG = false;
    public int callbackOrder => -100;

    public const string MANIFEST_PATH = "Packages/vpm-manifest.json";

    public const string TEMPLATE_PATH = "Assets/NOPPERS/DependencyChecker/Templates/template.json";

    private static void Log(string message)
    {
        if (DEBUG)
        {
            Debug.Log(message);
        }
    }

    [System.Serializable]
    public class ManifestData
    {
        public Dictionary<string, PackageInfo>? dependencies;
    }

    [System.Serializable]
    public class PackageInfo
    {
        public string? version;
    }

    public class DependencyIssue
    {
        public string PackageName { get; set; }
        public string ExpectedVersion { get; set; }
        public string? ActualVersion { get; set; }  // null = missing package

        public DependencyIssue(string packageName, string expectedVersion, string? actualVersion = null)
        {
            PackageName = packageName;
            ExpectedVersion = expectedVersion;
            ActualVersion = actualVersion;
        }
    }

    [MenuItem("Tools/NOPPERS/DependencyChecker/Check Versions")]
    public static void CheckVersions()
    {
        Log("DependencyChecker -> Clicked");
        string? manifest = GetManifest(MANIFEST_PATH);
        Log("DependencyChecker -> GetManifest -> manifest ->\n" + manifest);
        string? template = GetManifest(TEMPLATE_PATH);
        Log("DependencyChecker -> GetManifest -> template ->\n" + template);

        if (manifest == null || template == null)
        {
            Log("DependencyChecker -> One or both manifest files could not be read.");
            return;
        }

        var templateJSON = JsonConvert.DeserializeObject<ManifestData>(template);
        var manifestJSON = JsonConvert.DeserializeObject<ManifestData>(manifest);

        if (!ValidateManifestStructure(templateJSON))
        {
            Log("DependencyChecker -> Template JSON structure is invalid.");
            return;
        }

        if (!ValidateManifestStructure(manifestJSON))
        {
            Log("DependencyChecker -> Manifest JSON structure is invalid.");
            return;
        }

        var issues = CompareManifests(template, manifest);

        if (issues != null && issues.Count > 0)
        {
            // DisplayIssuesPopup(issues);
            DependencyIssuesWindow.ShowWindow(issues);
        }
        return;
    }

    [MenuItem("Tools/NOPPERS/DependencyChecker/Debug")]
    public static void ToggleDebug()
    {
        DEBUG = !DEBUG;
        Debug.Log($"Dependency Checker Debug Logging: {(DEBUG ? "Enabled" : "Disabled")}");
    }

    [MenuItem("Tools/NOPPERS/DependencyChecker/Debug", true)]
    public static bool ToggleDebugValidate()
    {
        Menu.SetChecked("Tools/NOPPERS/DependencyChecker/Toggle Debug Logging", DEBUG);
        return true;
    }

    [MenuItem("Tools/NOPPERS/DependencyChecker/Debug: VPM Resolve All")]
    public static void ShowManifestData()
    {
        // Debug-only action
        // Debug.Log("Manifest data...");
        DependencyIssuesWindow.OpenPackageResolverAndResolve();
    }

    [MenuItem("Tools/NOPPERS/DependencyChecker/Debug: VPM Resolve All", true)]
    public static bool ShowManifestDataValidate()
    {
        return DEBUG; // Only show if debug enabled
    }

    [MenuItem("Tools/NOPPERS/DependencyChecker/Debug: List All Open Windows")]
    public static void ListOpenWindows()
    {
        var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
        Debug.Log($"=== Found {windows.Length} open windows ===");
        foreach (var window in windows)
        {
            Debug.Log($"Title: '{window.titleContent.text}' | Type: {window.GetType().FullName}");
        }
    }

    [MenuItem("Tools/NOPPERS/DependencyChecker/Debug: List All Open Windows", true)]
    public static bool ListOpenWindowsValidate()
    {
        return DEBUG;
    }

    [InitializeOnLoadMethod]
    private static void CheckOnLoad()
    {
        Log("DependencyChecker -> CheckOnLoad");
    }

    public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
    {
        Log("DependencyChecker -> OnBuildRequested");
        return true;
    }

    private static string? GetManifest(string path)
    {
        if (!File.Exists(path))
        {
            Log("DependencyChecker -> JSON file not found at path: " + path);
            return null;
        }

        string manifestContent = File.ReadAllText(path);
        return manifestContent;
    }

    private static bool ValidateManifestStructure(ManifestData? data)
    {
        if (data == null)
        {
            Log("DependencyChecker -> data is null");
            return false;
        }

        if (data.dependencies == null)
        {
            Log("DependencyChecker -> dependencies field is null");
            return false;
        }

        if (data.dependencies.Count == 0)
        {
            Log("DependencyChecker -> dependencies field is empty");
            return false;
        }

        foreach (var dep in data.dependencies)
        {
            if (string.IsNullOrWhiteSpace(dep.Key))
            {
                Log("DependencyChecker -> dependency key is null or whitespace");
                return false;
            }

            if (dep.Value == null || string.IsNullOrWhiteSpace(dep.Value.version))
            {
                Log($"DependencyChecker -> dependency '{dep.Key}' has null or whitespace version");
                return false;
            }
        }

        return true;
    }

    private static List<DependencyIssue>? CompareManifests(string templateJson, string manifestJson)
    {
        var template = JsonConvert.DeserializeObject<ManifestData>(templateJson);
        var manifest = JsonConvert.DeserializeObject<ManifestData>(manifestJson);

        if (template?.dependencies == null || manifest?.dependencies == null)
        {
            Log("DependencyChecker -> Missing dependencies field");
            return null;
        }

        var issues = new List<DependencyIssue>();

        foreach (var item in template.dependencies)
        {
            Log($"DependencyChecker -> Checking: {item.Key} : {item.Value.version}");

            if (!manifest.dependencies.ContainsKey(item.Key))
            {
                Log($"Missing: {item.Key} (v{item.Value.version})");
                issues.Add(new DependencyIssue(
                    item.Key,
                    item.Value!.version!
                ));
                continue;
            }

            if (manifest.dependencies[item.Key].version != item.Value.version)
            {
                Log($"Version mismatch {item.Key}: template={item.Value.version},manifest={manifest.dependencies[item.Key].version}");
                issues.Add(new DependencyIssue(
                    item.Key,
                    item.Value!.version!,
                    manifest.dependencies[item.Key].version!
                ));
            }
        }

        return issues;
    }
}

public class DependencyIssuesWindow : EditorWindow
{
    // Simplified size constants (base 2)
    private const int SIZE_8 = 8;
    private const int SIZE_16 = 16;
    private const int SIZE_64 = 64;
    private const int SIZE_128 = 128;
    private const int SIZE_256 = 256;
    private const int SIZE_512 = 512;

    private static List<NoppersDependencyChecker.DependencyIssue> _issues = new();
    private Vector2 _scrollPosition;
    private bool _showAdvancedOptions = false;

    public static void ShowWindow(List<NoppersDependencyChecker.DependencyIssue> issues)
    {
        _issues = issues;
        var window = GetWindow<DependencyIssuesWindow>("Dependency Check");
        window.minSize = new Vector2(SIZE_512 + SIZE_128, SIZE_512);
        window.Show();
    }

    private void AcceptCurrentVersions()
    {
        try
        {
            // Read current template
            string? templateJson = File.ReadAllText(NoppersDependencyChecker.TEMPLATE_PATH);
            var templateData = JsonConvert.DeserializeObject<NoppersDependencyChecker.ManifestData>(templateJson);

            if (templateData?.dependencies == null)
            {
                Debug.LogError("Failed to read template data");
                return;
            }

            int updatedCount = 0;
            int removedCount = 0;

            // Update versions for mismatches and remove missing packages
            foreach (var issue in _issues)
            {
                if (issue.ActualVersion == null)
                {
                    // Missing package - remove from template
                    if (templateData.dependencies.Remove(issue.PackageName))
                    {
                        removedCount++;
                        Debug.Log($"Removed requirement: {issue.PackageName}");
                    }
                }
                else
                {
                    // Version mismatch - update to current version
                    if (templateData.dependencies.ContainsKey(issue.PackageName))
                    {
                        templateData.dependencies[issue.PackageName].version = issue.ActualVersion;
                        updatedCount++;
                        Debug.Log($"Updated {issue.PackageName}: {issue.ExpectedVersion} → {issue.ActualVersion}");
                    }
                }
            }

            // Save updated template
            string updatedJson = JsonConvert.SerializeObject(templateData, Formatting.Indented);
            File.WriteAllText(NoppersDependencyChecker.TEMPLATE_PATH, updatedJson);

            Debug.Log($"Template updated successfully: {updatedCount} version(s) updated, {removedCount} package(s) removed");

            // Close window
            Close();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to update template: {ex.Message}");
        }
    }

    public static void OpenPackageResolverAndResolve()
    {
        // Just call Resolver.ResolveManifest directly - it's a static method, no window needed
        TriggerResolveAll();
    }

    private static void TriggerResolveAll()
    {
        try
        {
            // The "Resolve All" button calls Resolver.ResolveManifest (static method)
            // Source: https://github.com/vrchat-community/examples-image-loading/blob/ac58660cc4b87e82297a495f01a80d34b1b4ce10/Packages/com.vrchat.core.vpm-resolver/Editor/Resolver/ResolverWindow.cs#L110

            // Find the Resolver class
            var resolverType = Type.GetType("VRC.PackageManagement.Resolver.Resolver, VRC.SDK3A.Editor");

            if (resolverType == null)
            {
                // Try without assembly specification
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    resolverType = assembly.GetType("VRC.PackageManagement.Resolver.Resolver");
                    if (resolverType != null) break;
                }
            }

            if (resolverType == null)
            {
                Debug.LogWarning("Could not find Resolver class. Please click 'Resolve All' manually.");
                return;
            }

            // Get the ResolveManifest static method
            var resolveMethod = resolverType.GetMethod("ResolveManifest",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            if (resolveMethod != null)
            {
                // Call the static method (no instance needed)
                resolveMethod.Invoke(null, null);
                Debug.Log("✓ Successfully triggered Resolver.ResolveManifest");
            }
            else
            {
                Debug.LogWarning("Could not find ResolveManifest method on Resolver class. Please click 'Resolve All' manually.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to trigger Resolve All: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void OnGUI()
    {
        // Helpful instructions header
        var headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 15;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.normal.textColor = new Color(0.4f, 0.7f, 1f);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.Space(SIZE_8);

        EditorGUILayout.LabelField("HOW TO FIX THESE ISSUES", headerStyle);
        EditorGUILayout.Space(SIZE_16);

        var infoStyle = new GUIStyle(EditorStyles.label);
        infoStyle.fontSize = 13;
        infoStyle.wordWrap = true;
        infoStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

        EditorGUILayout.LabelField(
            "Dependency mismatches can cause compilation errors, runtime failures, and compatibility issues.\n",
            infoStyle
        );

        EditorGUILayout.Space(SIZE_8);

        var stepsTitleStyle = new GUIStyle(EditorStyles.boldLabel);
        stepsTitleStyle.fontSize = 13;
        stepsTitleStyle.normal.textColor = new Color(1f, 0.9f, 0.5f);
        EditorGUILayout.LabelField("Fix Steps:", stepsTitleStyle);

        EditorGUILayout.Space(4);

        var stepsStyle = new GUIStyle(EditorStyles.label);
        stepsStyle.fontSize = 12;
        stepsStyle.wordWrap = true;
        stepsStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

        EditorGUILayout.LabelField(
            "  1.  Open VRChat Creator Companion (VCC)\n" +
            "  2.  Select this project\n" +
            "  3.  Click \"Manage Project\"\n" +
            "  4.  Install or update the packages below",
            stepsStyle
        );

        EditorGUILayout.Space(SIZE_8);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(SIZE_16);

        // Visual separator
        var separatorRect = EditorGUILayout.GetControlRect(false, 2);
        EditorGUI.DrawRect(separatorRect, new Color(0.3f, 0.3f, 0.3f));
        EditorGUILayout.Space(SIZE_8);

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        var missing = _issues.Where(i => i.ActualVersion == null).ToList();
        var mismatches = _issues.Where(i => i.ActualVersion != null).ToList();

        if (missing.Count > 0)
        {
            var sectionStyle = new GUIStyle(EditorStyles.boldLabel);
            sectionStyle.fontSize = 13;
            sectionStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);
            EditorGUILayout.LabelField($"Missing Packages ({missing.Count})", sectionStyle);
            EditorGUILayout.Space(SIZE_8);

            foreach (var issue in missing)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                var packageStyle = new GUIStyle(EditorStyles.label);
                packageStyle.fontSize = 12;
                EditorGUILayout.LabelField($"📦 {issue.PackageName}", packageStyle, GUILayout.Width(SIZE_256));

                // Spacer to align with "Current: v..." in mismatches
                EditorGUILayout.LabelField("", GUILayout.Width(SIZE_128));

                // Spacer for arrow
                EditorGUILayout.LabelField("", GUILayout.Width(SIZE_16 + SIZE_8));

                var needStyle = new GUIStyle(EditorStyles.boldLabel);
                needStyle.fontSize = 11;
                needStyle.normal.textColor = new Color(0.3f, 1f, 0.3f);
                EditorGUILayout.LabelField($"v{issue.ExpectedVersion}", needStyle);

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.Space(SIZE_16);
        }

        if (mismatches.Count > 0)
        {
            var sectionStyle = new GUIStyle(EditorStyles.boldLabel);
            sectionStyle.fontSize = 13;
            sectionStyle.normal.textColor = new Color(1f, 0.7f, 0.3f);
            EditorGUILayout.LabelField($"Version Mismatches ({mismatches.Count})", sectionStyle);
            EditorGUILayout.Space(SIZE_8);

            foreach (var issue in mismatches)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                var packageStyle = new GUIStyle(EditorStyles.label);
                packageStyle.fontSize = 12;
                EditorGUILayout.LabelField($"📦 {issue.PackageName}", packageStyle, GUILayout.Width(SIZE_256));

                var currentStyle = new GUIStyle(EditorStyles.label);
                currentStyle.fontSize = 11;
                currentStyle.normal.textColor = new Color(1f, 0.5f, 0.5f);
                EditorGUILayout.LabelField($"v{issue.ActualVersion}", currentStyle, GUILayout.Width(SIZE_128));

                var arrowStyle = new GUIStyle(EditorStyles.boldLabel);
                arrowStyle.fontSize = 14;
                arrowStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                EditorGUILayout.LabelField("➜", arrowStyle, GUILayout.Width(SIZE_16 + SIZE_8));

                var needStyle = new GUIStyle(EditorStyles.boldLabel);
                needStyle.fontSize = 11;
                needStyle.normal.textColor = new Color(0.3f, 1f, 0.3f);
                EditorGUILayout.LabelField($"v{issue.ExpectedVersion}", needStyle);

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(SIZE_16);

        // Visual separator
        var separatorRect2 = EditorGUILayout.GetControlRect(false, 2);
        EditorGUI.DrawRect(separatorRect2, new Color(0.3f, 0.3f, 0.3f));
        EditorGUILayout.Space(SIZE_8);

        // Advanced Options Section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        var advancedHeaderStyle = new GUIStyle(EditorStyles.foldout);
        advancedHeaderStyle.fontSize = 13;
        advancedHeaderStyle.fontStyle = FontStyle.Bold;

        _showAdvancedOptions = EditorGUILayout.Foldout(_showAdvancedOptions, "Advanced Options", true, advancedHeaderStyle);

        if (_showAdvancedOptions)
        {
            EditorGUILayout.Space(SIZE_8);

            // Warning message
            var advancedWarningStyle = new GUIStyle(EditorStyles.label);
            advancedWarningStyle.fontSize = 12;
            advancedWarningStyle.wordWrap = true;

            EditorGUILayout.LabelField(
                "These actions will modify project files directly. Only use if you understand the implications.",
                advancedWarningStyle
            );

            EditorGUILayout.Space(SIZE_16);

            // Accept Current button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(1f, 0.6f, 0.2f);
            if (GUILayout.Button("🔧 Accept Current Versions", GUILayout.Height(SIZE_16 + SIZE_8)))
            {
                AcceptCurrentVersions();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(SIZE_8);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(SIZE_16);

        // Minimal reminder
        var reminderStyle = new GUIStyle(EditorStyles.label);
        reminderStyle.fontSize = 11;
        reminderStyle.alignment = TextAnchor.MiddleCenter;
        reminderStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);

        EditorGUILayout.LabelField(
            "Dismissing this window won't resolve the issues",
            reminderStyle
        );

        EditorGUILayout.Space(SIZE_8);

        // Center the button horizontally
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
        if (GUILayout.Button("Dismiss", GUILayout.Height(SIZE_16 + SIZE_8)))
        {
            Close();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(SIZE_8);
    }
}