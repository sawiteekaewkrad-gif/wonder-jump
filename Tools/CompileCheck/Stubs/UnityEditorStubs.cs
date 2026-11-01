// Compile-only stand-ins for the UnityEditor API. NOT UNITY - see UnityEngineStubs.cs header.
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Obj = UnityEngine.Object;

namespace UnityEditor
{
    public static class AssetDatabase
    {
        public static T LoadAssetAtPath<T>(string p) where T : Obj { return null; }
        public static Obj[] LoadAllAssetsAtPath(string p) { return new Obj[0]; }
        public static void CreateAsset(Obj o, string p) { }
        public static void SaveAssets() { }
        public static void SaveAssetIfDirty(Obj o) { }
        public static void Refresh() { }
        public static void ImportAsset(string p) { }
        public static bool IsValidFolder(string p) { return p == "Assets"; }
        public static string CreateFolder(string parent, string name) { return ""; }
        public static bool DeleteAsset(string p) { return true; }
        public static string[] FindAssets(string filter, string[] folders) { return new string[0]; }
        public static string GUIDToAssetPath(string g) { return ""; }
        public static string AssetPathToGUID(string p) { return ""; }
        public static string GetAssetPath(Obj o) { return ""; }
    }

    public enum PrefabAssetType { NotAPrefab, Regular, Model, Variant, MissingAsset }

    public static class PrefabUtility
    {
        public static GameObject SaveAsPrefabAsset(GameObject go, string path) { return go; }
        public static GameObject SaveAsPrefabAsset(GameObject go, string path, out bool success) { success = true; return go; }
        public static Obj InstantiatePrefab(Obj o) { return o; }
        public static GameObject LoadPrefabContents(string path) { return new GameObject(); }
        public static void UnloadPrefabContents(GameObject go) { }
        public static T GetCorrespondingObjectFromSource<T>(T o) where T : Obj { return null; }
        public static PrefabAssetType GetPrefabAssetType(Obj o) { return PrefabAssetType.Regular; }
    }

    public static class EditorUtility
    {
        public static bool DisplayDialog(string title, string message, string ok) { return true; }
        public static bool DisplayDialog(string title, string message, string ok, string cancel) { return true; }
        public static void SetDirty(Obj o) { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class MenuItem : Attribute
    {
        public MenuItem(string path) { }
        public MenuItem(string path, bool validate, int priority) { }
    }

    public class SerializedProperty
    {
        public bool isArray { get { return true; } }
        public int arraySize { get { return 32; } }
        public string stringValue { get; set; }
        public SerializedProperty GetArrayElementAtIndex(int i) { return new SerializedProperty(); }
    }

    public class SerializedObject
    {
        public SerializedObject(Obj o) { }
        public SerializedProperty FindProperty(string p) { return new SerializedProperty(); }
        public bool ApplyModifiedPropertiesWithoutUndo() { return true; }
    }

    public enum UIOrientation { Portrait = 0, PortraitUpsideDown = 1, LandscapeRight = 2, LandscapeLeft = 3, AutoRotation = 4 }
    public enum ScriptingImplementation { Mono2x = 0, IL2CPP = 1 }
    [Flags] public enum AndroidArchitecture { None = 0, ARMv7 = 1, ARM64 = 2, X86_64 = 8, All = -1 }
    public enum AndroidSdkVersions { AndroidApiLevelAuto = 0, AndroidApiLevel23 = 23, AndroidApiLevel34 = 34 }
    public enum BuildTarget { StandaloneWindows64 = 19, Android = 13 }

    public static class PlayerSettings
    {
        public static string productName { get; set; }
        public static UIOrientation defaultInterfaceOrientation { get; set; }
        public static bool allowedAutorotateToPortrait { get; set; }
        public static bool allowedAutorotateToPortraitUpsideDown { get; set; }
        public static bool allowedAutorotateToLandscapeLeft { get; set; }
        public static bool allowedAutorotateToLandscapeRight { get; set; }
        public static ColorSpace colorSpace { get; set; }
        public static void SetScriptingBackend(Build.NamedBuildTarget t, ScriptingImplementation s) { }
        public static void SetUseDefaultGraphicsAPIs(BuildTarget t, bool b) { }
        public static void SetGraphicsAPIs(BuildTarget t, GraphicsDeviceType[] apis) { }
        public static string GetApplicationIdentifier(Build.NamedBuildTarget t) { return "com.DefaultCompany.Stub"; }

        public static class Android
        {
            public static AndroidArchitecture targetArchitectures { get; set; }
            public static AndroidSdkVersions minSdkVersion { get; set; }
            public static AndroidSdkVersions targetSdkVersion { get; set; }
        }
    }

    public static class EditorUserBuildSettings { public static BuildTarget activeBuildTarget { get { return BuildTarget.StandaloneWindows64; } } }

    public class EditorBuildSettingsScene
    {
        public string path;
        public bool enabled;
        public EditorBuildSettingsScene(string path, bool enabled) { this.path = path; this.enabled = enabled; }
    }
    public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes { get; set; } }
    public sealed class SceneAsset : Obj { }
}

namespace UnityEditor.Build
{
    public struct NamedBuildTarget { public static readonly NamedBuildTarget Android = new NamedBuildTarget(); }
}

namespace UnityEditor.SceneManagement
{
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }
    public static class EditorSceneManager
    {
        public static bool SaveCurrentModifiedScenesIfUserWantsTo() { return true; }
        public static Scene NewScene(NewSceneSetup s, NewSceneMode m) { return new Scene(); }
        public static bool SaveScene(Scene s, string path) { return true; }
    }
}
