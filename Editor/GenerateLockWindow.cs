#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public class GenerateLockWindow : EditorWindow
{
    private const int SIZE_8 = 8;
    private const int SIZE_16 = 16;
    private const int SIZE_128 = 128;
    private const int SIZE_256 = 256;
    private const int SIZE_512 = 512;

    private string _authorName = "";
    private string _assetName = "";
    private Vector2 _scrollPosition;
    private Dictionary<string, NoppersPackageChecker.PackageInfo>? _currentPackages;
    private Dictionary<string, bool> _packageSelection = new();

    private const string VPM_RESOLVER = "com.vrchat.core.vpm-resolver";
    private const string VRCHAT_BASE = "com.vrchat.base";
    private const string VRCHAT_AVATARS = "com.vrchat.avatars";

    public static void ShowWindow()
    {
        var window = GetWindow<GenerateLockWindow>("Generate Lock File");
        window.minSize = new Vector2(SIZE_512 + SIZE_128, SIZE_512);
        window.Show();
    }

    private void OnEnable()
    {
        LoadCurrentPackages();
    }

    private void LoadCurrentPackages()
    {
        try
        {
            string? manifestJson = File.ReadAllText(NoppersPackageChecker.MANIFEST_PATH);
            var manifestData = JsonConvert.DeserializeObject<NoppersPackageChecker.ManifestData>(manifestJson);
            _currentPackages = manifestData?.locked;

            _packageSelection.Clear();
            if (_currentPackages != null)
            {
                foreach (var package in _currentPackages)
                {
                    _packageSelection[package.Key] = false;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load current packages: {ex.Message}");
            _currentPackages = null;
        }
    }

    private void GenerateLockFile()
    {
        var lockData = NoppersPackageChecker.ComposeLockFile(_currentPackages, _packageSelection);
        string locksDir = NoppersPackageChecker.GetLocksDirectory();
        string fileName = $"{_authorName}_{_assetName}.lock.json";
        var success = NoppersPackageChecker.CreateLockFile(lockData, fileName, locksDir);

        if (success)
        {
            // Display success message
            EditorUtility.DisplayDialog("Success", $"Lock file created:\n{fileName}\n\n{lockData.locked.Count} packages included", "OK");
            Close();
        }
        else
        {
            Debug.LogError("Failed to create lock file.");
        }
    }

    private void OnGUI()
    {
        var headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 15;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.normal.textColor = new Color(0.4f, 0.7f, 1f);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.Space(SIZE_8);

        EditorGUILayout.LabelField("GENERATE LOCK FILE", headerStyle);
        EditorGUILayout.Space(SIZE_16);

        var infoStyle = new GUIStyle(EditorStyles.label);
        infoStyle.fontSize = 12;
        infoStyle.wordWrap = true;
        infoStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

        EditorGUILayout.LabelField(
            "Create a lock file snapshot of your current VPM packages for this asset. Ensure you export the package checker along with the lock files.",
            infoStyle
        );

        EditorGUILayout.Space(SIZE_8);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(SIZE_16);

        // Input fields
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.Space(SIZE_8);

        var labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.fontSize = 12;
        labelStyle.fontStyle = FontStyle.Bold;

        EditorGUILayout.LabelField("Author Name:", labelStyle);
        _authorName = EditorGUILayout.TextField(_authorName);
        EditorGUILayout.Space(SIZE_8);

        EditorGUILayout.LabelField("Asset Name:", labelStyle);
        _assetName = EditorGUILayout.TextField(_assetName);
        EditorGUILayout.Space(SIZE_8);

        if (!string.IsNullOrWhiteSpace(_authorName) && !string.IsNullOrWhiteSpace(_assetName))
        {
            var filenameStyle = new GUIStyle(EditorStyles.label);
            filenameStyle.fontSize = 11;
            filenameStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            EditorGUILayout.LabelField($"Filename: {_authorName}_{_assetName}.lock.json", filenameStyle);
        }

        EditorGUILayout.Space(SIZE_8);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(SIZE_16);

        // Package list
        var separatorRect = EditorGUILayout.GetControlRect(false, 2);
        EditorGUI.DrawRect(separatorRect, new Color(0.3f, 0.3f, 0.3f));
        EditorGUILayout.Space(SIZE_8);

        var packageHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
        packageHeaderStyle.fontSize = 13;
        packageHeaderStyle.normal.textColor = new Color(0.4f, 0.7f, 1f);
        EditorGUILayout.LabelField($"Current Packages ({_currentPackages?.Count ?? 0})", packageHeaderStyle);
        EditorGUILayout.Space(SIZE_8);

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        if (_currentPackages != null && _currentPackages.Count > 0)
        {
            foreach (var package in _currentPackages)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                bool isExcluded = package.Key == VPM_RESOLVER ||
                                  package.Key == VRCHAT_BASE ||
                                  package.Key == VRCHAT_AVATARS;
                bool isSelected = _packageSelection.TryGetValue(package.Key, out bool selected) && selected;

                // Checkbox (disabled for excluded packages)
                GUI.enabled = !isExcluded;
                bool newSelection = EditorGUILayout.Toggle(isSelected, GUILayout.Width(SIZE_16));
                if (newSelection != isSelected && !isExcluded)
                {
                    _packageSelection[package.Key] = newSelection;
                }
                GUI.enabled = true;

                var packageStyle = new GUIStyle(EditorStyles.label);
                packageStyle.fontSize = 12;
                if (isExcluded)
                {
                    packageStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
                }
                EditorGUILayout.LabelField($"{package.Key}", packageStyle, GUILayout.Width(SIZE_256 + SIZE_128 - SIZE_16));

                var versionStyle = new GUIStyle(EditorStyles.boldLabel);
                versionStyle.fontSize = 11;
                versionStyle.normal.textColor = isExcluded ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.3f, 1f, 0.3f);
                EditorGUILayout.LabelField($"v{package.Value.version}", versionStyle);

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }
        }
        else
        {
            var emptyStyle = new GUIStyle(EditorStyles.label);
            emptyStyle.fontSize = 12;
            emptyStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            EditorGUILayout.LabelField("No packages found in vpm-manifest.json", emptyStyle);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(SIZE_16);

        // Buttons
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        int selectedCount = _packageSelection.Values.Count(v => v);
        bool canGenerate = !string.IsNullOrWhiteSpace(_authorName) &&
                          !string.IsNullOrWhiteSpace(_assetName) &&
                          selectedCount > 0;

        GUI.enabled = canGenerate;
        string buttonText = selectedCount > 0 ? $"Generate Lock File ({selectedCount})" : "Generate Lock File";
        if (GUILayout.Button(buttonText, GUILayout.Height(SIZE_16 + SIZE_8)))
        {
            GenerateLockFile();
        }
        GUI.enabled = true;
        GUI.backgroundColor = Color.white;

        GUILayout.Space(SIZE_8);

        GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
        if (GUILayout.Button("Cancel", GUILayout.Height(SIZE_16 + SIZE_8)))
        {
            Close();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(SIZE_8);
    }
}
