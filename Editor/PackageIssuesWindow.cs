#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class PackageIssuesWindow : EditorWindow
{
    private const int SIZE_8 = 8;
    private const int SIZE_16 = 16;
    private const int SIZE_64 = 64;
    private const int SIZE_128 = 128;
    private const int SIZE_256 = 256;
    private const int SIZE_512 = 512;

    private static List<NoppersPackageChecker.PackageIssue> _issues = new();
    private static PackageIssuesWindow? _instance;
    private Vector2 _scrollPosition;
    private bool _showAdvancedOptions = false;

    public static void ShowWindow(List<NoppersPackageChecker.PackageIssue> issues)
    {
        // Close all existing instances
        var existingWindows = Resources.FindObjectsOfTypeAll<PackageIssuesWindow>();
        foreach (var window in existingWindows)
        {
            window.Close();
        }

        _issues = issues;
        _instance = GetWindow<PackageIssuesWindow>("Package Check");
        _instance.minSize = new Vector2(SIZE_512 + SIZE_128, SIZE_512);
        _instance.Show();
    }

    private void OnEnable()
    {
        if (_instance == this && (_issues == null || _issues.Count == 0))
        {
            EditorApplication.delayCall += () =>
            {
                if (this != null) Close();
                NoppersPackageChecker.CheckVersions();
            };
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void AcceptCurrentVersions()
    {
        try
        {
            string locksDir = NoppersPackageChecker.GetLocksDirectory();
            string disabledDir = Path.Combine(Path.GetDirectoryName(locksDir)!, "Locks_Disabled").Replace("\\", "/");

            // Find all lock files
            var lockFiles = Directory.GetFiles(locksDir, "*.lock.json");

            if (lockFiles.Length == 0)
            {
                return;
            }

            // Create disabled directory if it doesn't exist
            if (!Directory.Exists(disabledDir))
            {
                Directory.CreateDirectory(disabledDir);
            }

            int movedCount = 0;
            foreach (var lockFile in lockFiles)
            {
                string fileName = Path.GetFileName(lockFile);
                string destPath = Path.Combine(disabledDir, fileName);

                int assetsIndex = lockFile.IndexOf("Assets");
                if (assetsIndex == -1) continue;

                string assetPath = lockFile.Substring(assetsIndex).Replace("\\", "/");
                string destAssetPath = destPath.Substring(destPath.IndexOf("Assets")).Replace("\\", "/");

                // Delete existing file at destination if present
                if (File.Exists(destPath))
                {
                    AssetDatabase.DeleteAsset(destAssetPath);
                }

                // Move the file using AssetDatabase to properly handle .meta files
                string moveResult = AssetDatabase.MoveAsset(assetPath, destAssetPath);
                if (string.IsNullOrEmpty(moveResult))
                {
                    movedCount++;
                }
                else
                {
                    Debug.LogWarning($"Failed to move {assetPath}: {moveResult}");
                }
            }

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success",
                $"Accepted current versions.\n\n{movedCount} lock file(s) moved to Locks_Disabled/\n\nYou can move them back manually if needed.",
                "OK");

            Close();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to accept current versions: {ex.Message}");
        }
    }

    public static void OpenPackageResolverAndResolve()
    {
        TriggerResolveAll();
    }

    private static void TriggerResolveAll()
    {
        try
        {
            // Update manifest with required versions
            string manifestPath = NoppersPackageChecker.MANIFEST_PATH;

            if (!NoppersPackageChecker.UpdateManifest(
                manifestPath,
                _issues,
                NoppersPackageChecker.GetManifest,
                (path, content) => {
                    try
                    {
                        File.WriteAllText(path, content);
                        AssetDatabase.Refresh();
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            ))
            {
                Debug.LogError("Failed to update manifest with required versions.");
                return;
            }

            // Trigger VPM Resolver
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
                Debug.LogWarning("Could not find Resolver class. Manifest updated but you'll need to click 'Resolve All' manually.");
                return;
            }

            // Get the ResolveManifest static method
            var resolveMethod = resolverType.GetMethod("ResolveManifest",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            if (resolveMethod != null)
            {
                // Call the static method (no instance needed)
                resolveMethod.Invoke(null, null);
            }
            else
            {
                Debug.LogWarning("Could not find ResolveManifest method on Resolver class. Manifest updated but you'll need to click 'Resolve All' manually.");
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

        EditorGUILayout.LabelField("FIX PACKAGE ISSUES", headerStyle);
        EditorGUILayout.Space(SIZE_16);

        var infoStyle = new GUIStyle(EditorStyles.label);
        infoStyle.fontSize = 13;
        infoStyle.wordWrap = true;
        infoStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

        EditorGUILayout.LabelField(
            "Incorrect packages can cause unexpected errors with your avatar and attachments. Please installed the correct package versions.\n",
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
                EditorGUILayout.LabelField($"{issue.PackageName}", packageStyle, GUILayout.Width(SIZE_256));

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
                EditorGUILayout.LabelField($"{issue.PackageName}", packageStyle, GUILayout.Width(SIZE_256));

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
                "Fix all attempts to repair package package issues. If you would like to use your own packages you can disable the lock files.",
                advancedWarningStyle
            );

            EditorGUILayout.Space(SIZE_16);

            // Buttons side by side
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(1f, 0.8f, 0.2f);
            if (GUILayout.Button("Fix All (Experimental)", GUILayout.Height(SIZE_16 + SIZE_8), GUILayout.Width(SIZE_128 + SIZE_64)))
            {
                TriggerResolveAll();
                Close();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(SIZE_8);

            if (GUILayout.Button("Disable Locks", GUILayout.Height(SIZE_16 + SIZE_8), GUILayout.Width(SIZE_128 + SIZE_64)))
            {
                AcceptCurrentVersions();
            }

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
            "Dismissing this window won't resolve package issues.",
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
