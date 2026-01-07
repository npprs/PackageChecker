#nullable enable
using UnityEditor;
using UnityEngine;

public static class PackageCheckerDebug
{
    [MenuItem("Tools/NOPPERS/PackageChecker/Debug/Enable")]
    public static void ToggleDebug()
    {
        NoppersPackageChecker.SetDebugMode(!NoppersPackageChecker.IsDebugEnabled());
        Debug.Log($"Package Checker Debug Logging: {(NoppersPackageChecker.IsDebugEnabled() ? "Enabled" : "Disabled")}");
    }

    [MenuItem("Tools/NOPPERS/PackageChecker/Debug/Enable", true)]
    public static bool ToggleDebugValidate()
    {
        Menu.SetChecked("Tools/NOPPERS/PackageChecker/Debug", NoppersPackageChecker.IsDebugEnabled());
        return true;
    }

    [MenuItem("Tools/NOPPERS/PackageChecker/Debug/VPM Resolve All")]
    public static void TriggerVPMResolve()
    {
        PackageIssuesWindow.OpenPackageResolverAndResolve();
    }

    [MenuItem("Tools/NOPPERS/PackageChecker/Debug/VPM Resolve All", true)]
    public static bool TriggerVPMResolveValidate()
    {
        return NoppersPackageChecker.IsDebugEnabled();
    }

    [MenuItem("Tools/NOPPERS/PackageChecker/Debug/List All Open Windows")]
    public static void ListOpenWindows()
    {
        var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
        Debug.Log($"=== Found {windows.Length} open windows ===");
        foreach (var window in windows)
        {
            Debug.Log($"Title: '{window.titleContent.text}' | Type: {window.GetType().FullName}");
        }
    }

    [MenuItem("Tools/NOPPERS/PackageChecker/Debug/List All Open Windows", true)]
    public static bool ListOpenWindowsValidate()
    {
        return NoppersPackageChecker.IsDebugEnabled();
    }
}
