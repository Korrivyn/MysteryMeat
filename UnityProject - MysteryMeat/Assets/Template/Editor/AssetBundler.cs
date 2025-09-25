using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class AssetBundler
{
    /// <summary>
    /// The types of assets to search for in checks.
    /// </summary>
    private static readonly string ASSET_SEARCH_QUERY = "t:prefab,t:textAsset,t:audioclip";

    /// <summary>
    /// Temporary location for building AssetBundles.
    /// </summary>
    private static readonly string TEMP_BUILD_FOLDER = "Temp/AssetBundles";

    /// <summary>
    /// Name of the output bundle file. This needs to match the bundle that you tag your assets with.
    /// </summary>
    private static readonly string BUNDLE_FILENAME = "mod.assets";

    /// <summary>
    /// The output folder to place the completed bundle in.
    /// </summary>
    private static readonly string OUTPUT_FOLDER = "content";

    /// <summary>
    /// The folders to not search for assets in.
    /// </summary>
    private static readonly string[] EXCLUDED_FOLDERS = new string[] { "Assets/Editor", "Packages" };

    /// <summary>
    /// The build target of the asset bundle. Should either be StandaloneWindows or StandaloneOSX, depending on your platform.
    /// </summary>
    private BuildTarget Target = BuildTarget.StandaloneWindows;

    /// <summary>
    /// Number of warnings encountered.
    /// </summary>
    private int NumWarnings;

    /// <summary>
    /// Tracks the temporary asset bundle tag generated during build.
    /// </summary>
    private string GeneratedAssetBundleTag;

    /// <summary>
    /// Provides reflection based access to the runtime debug log system while working inside the Unity editor.
    /// </summary>
    private static class EditorDebugLogBridge
    {
        private static Type _debugType;
        private static MethodInfo _infoMethod;
        private static MethodInfo _warningMethod;
        private static MethodInfo _errorMethod;
        private static MethodInfo _verboseMethod;

        private static Type ResolveDebugType()
        {
            if (_debugType != null)
            {
                return _debugType;
            }

            // Attempt to locate the runtime logging helper so editor tooling can reuse its configuration.
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type candidate = assembly.GetType("KitchenMysteryMeat.Systems.Logging.DebugLogSystem");
                if (candidate != null)
                {
                    _debugType = candidate;
                    break;
                }
            }

            return _debugType;
        }

        private static MethodInfo ResolveMethod(ref MethodInfo cache, string methodName)
        {
            if (cache != null)
            {
                return cache;
            }

            Type debugType = ResolveDebugType();
            if (debugType != null)
            {
                cache = debugType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            }

            return cache;
        }

        private static void Invoke(MethodInfo method, object[] parameters, Action<string> fallback, string message)
        {
            if (method != null)
            {
                try
                {
                    method.Invoke(null, parameters);
                    return;
                }
                catch (Exception)
                {
                    // Reflection may fail when the runtime assembly is not loaded; fall back to Unity logging.
                }
            }

            fallback?.Invoke(message);
        }

        public static void LogInfo(string message)
        {
            MethodInfo method = ResolveMethod(ref _infoMethod, "LogInfo");
            Invoke(method, new object[] { message }, m => Debug.Log(m), message);
        }

        public static void LogWarning(string message)
        {
            MethodInfo method = ResolveMethod(ref _warningMethod, "LogWarning");
            Invoke(method, new object[] { message }, m => Debug.LogWarning(m), message);
        }

        public static void LogError(string message)
        {
            MethodInfo method = ResolveMethod(ref _errorMethod, "LogError");
            Invoke(method, new object[] { message }, m => Debug.LogError(m), message);
        }

        public static void LogVerbose(string message)
        {
            MethodInfo method = ResolveMethod(ref _verboseMethod, "LogVerbose");
            Invoke(method, new object[] { message }, m => Debug.Log(m), message);
        }
    }

    /// <summary>
    /// Builds the mod asset bundle while routing progress through the shared debug logging system.
    /// </summary>
    [MenuItem("PlateUp!/Build Asset Bundle _F6")]
    public static void BuildAssetBundle()
    {
        EditorDebugLogBridge.LogInfo(string.Format("Creating \"{0}\" AssetBundle...", BUNDLE_FILENAME));

        AssetBundler bundler = new AssetBundler();

        // Apply the macOS build target when needed so the generated bundle is compatible with the editor.
        if (Application.platform == RuntimePlatform.OSXEditor)
        {
            bundler.Target = BuildTarget.StandaloneOSX;
        }

        // Randomly generate the resulting name of the asset bundle.
        bundler.GenerateRandomAssetBundleTag();

        bool success = false;
        try
        {
            // Check for assets and emit warnings when tagging appears incorrect.
            bundler.WarnIfAssetsAreNotTagged();
            bundler.WarnIfZeroAssetsAreTagged();
            bundler.WarnIfMeshAssetsAreTagged();
            // bundler.WarnIfMaterialsAreTaggedOrIncluded();

            // Delete the contents of OUTPUT_FOLDER.
            bundler.CleanBuildFolder();

            // Temporarily move the tagged assets to the temporary tag.
            bundler.MoveAssetsToTemporaryAssetBundle();

            // Lastly, create the asset bundle itself and copy it to the output folder.
            bundler.CreateAssetBundle();

            success = true;
        }
        catch (Exception e)
        {
            EditorDebugLogBridge.LogError(string.Format("Failed to build AssetBundle: {0}\n{1}", e.Message, e.StackTrace));
        }

        // Return assets to the original asset bundle tag.
        bundler.RestoreAssetBundleTags();
        AssetDatabase.RemoveUnusedAssetBundleNames();

        if (success)
        {
            EditorDebugLogBridge.LogInfo(string.Format("[{0}] Build complete with {1} warnings! Output: {2} (temporary ID: {3})",
                DateTime.Now.ToLocalTime(), bundler.NumWarnings, OUTPUT_FOLDER + "/" + BUNDLE_FILENAME, bundler.GeneratedAssetBundleTag));
        }
    }

    /// <summary>
    /// Generate the random asset bundle tag to use when building the asset bundle.
    /// </summary>
    private void GenerateRandomAssetBundleTag()
    {
        System.Random rand = new System.Random();
        GeneratedAssetBundleTag = $"mod-{rand.Next(0, int.MaxValue)}.assets";
        EditorDebugLogBridge.LogVerbose($"Generated temporary asset bundle tag {GeneratedAssetBundleTag}.");
    }

    /// <summary>
    /// Move assets tagged with BUNDLE_FILENAME to the temporary asset bundle.
    /// </summary>
    private void MoveAssetsToTemporaryAssetBundle()
    {
        SubstituteAssetBundleTags(BUNDLE_FILENAME, GeneratedAssetBundleTag);
    }

    /// <summary>
    /// Move assets tagged with the temporary asset bundle back to BUNDLE_FILENAME.
    /// </summary>
    private void RestoreAssetBundleTags()
    {
        SubstituteAssetBundleTags(GeneratedAssetBundleTag, BUNDLE_FILENAME);
    }

    /// <summary>
    /// Find all assets tagged with a certain asset bundle tag and replace them with another tag.
    /// </summary>
    /// <param name="from">The asset bundle tag to search for.</param>
    /// <param name="to">The new asset bundle tag.</param>
    private void SubstituteAssetBundleTags(string from, string to)
    {
        string[] assetGUIDs = AssetDatabase.FindAssets($"b:{from}");
        foreach (var assetGUID in assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(assetGUID);
            AssetImporter importer = AssetImporter.GetAtPath(path);
            importer.assetBundleName = to;
        }

        EditorDebugLogBridge.LogVerbose(string.Format("Retagged {0} assets from {1} to {2}.", assetGUIDs.Length, from, to));
    }

    /// <summary>
    /// Delete and recreate the OUTPUT_FOLDER to ensure a clean build.
    /// </summary>
    protected void CleanBuildFolder()
    {
        EditorDebugLogBridge.LogInfo(string.Format("Cleaning {0}...", OUTPUT_FOLDER));

        // Guard: remove the existing output directory to avoid stale bundles.
        if (Directory.Exists(OUTPUT_FOLDER))
        {
            Directory.Delete(OUTPUT_FOLDER, true);
        }

        Directory.CreateDirectory(OUTPUT_FOLDER);
    }

    /// <summary>
    /// Build the AssetBundle itself and copy it to the OUTPUT_FOLDER.
    /// </summary>
    protected void CreateAssetBundle()
    {
        EditorDebugLogBridge.LogInfo("Building AssetBundle...");

        // Guard: ensure the temporary folder exists before writing bundles to disk.
        if (!Directory.Exists(TEMP_BUILD_FOLDER))
        {
            Directory.CreateDirectory(TEMP_BUILD_FOLDER);
        }

#pragma warning disable 618
        // Build the asset bundle with the CollectDependencies flag. This is necessary or else ScriptableObjects will
        // not be accessible within the asset bundle. Unity has deprecated this flag claiming it is now always active,
        // but due to a bug we must still include it (and ignore the warning).
        BuildPipeline.BuildAssetBundles(
            TEMP_BUILD_FOLDER,
            BuildAssetBundleOptions.UncompressedAssetBundle | BuildAssetBundleOptions.CollectDependencies,
            Target);
#pragma warning restore 618

        // We are only interested in the BUNDLE_FILENAME bundle (and not any extra AssetBundle or the manifest files
        // that Unity makes), so just copy that to the final output folder.
        string srcPath = Path.Combine(TEMP_BUILD_FOLDER, GeneratedAssetBundleTag);
        string destPath = Path.Combine(OUTPUT_FOLDER, BUNDLE_FILENAME);
        File.Copy(srcPath, destPath, true);
    }

    /// <summary>
    /// Checks if the given path is a search path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>true if the given path is a search path, otherwise false.</returns>
    protected static bool IsIncludedAssetPath(string path)
    {
        foreach (string excludedPath in EXCLUDED_FOLDERS)
        {
            if (path.StartsWith(excludedPath))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Log a warning for all potential assets that are not currently tagged to be in this AssetBundle.
    /// </summary>
    protected void WarnIfAssetsAreNotTagged()
    {
        string[] assetGUIDs = AssetDatabase.FindAssets(ASSET_SEARCH_QUERY);
        foreach (var assetGUID in assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(assetGUID);
            if (!IsIncludedAssetPath(path))
            {
                continue;
            }

            AssetImporter importer = AssetImporter.GetAtPath(path);
            if (!importer.assetBundleName.Equals(BUNDLE_FILENAME))
            {
                // Future enhancement: emit warnings when assets are skipped from the bundle build.
            }
        }
    }

    /// <summary>
    /// Verify that there is at least one asset to be included in the asset bundle.
    /// </summary>
    protected void WarnIfZeroAssetsAreTagged()
    {
        string[] assetsInBundle = AssetDatabase.FindAssets($"{ASSET_SEARCH_QUERY},b:{BUNDLE_FILENAME}");
        if (assetsInBundle.Length == 0)
        {
            // Future enhancement: throw when no assets are tagged to avoid distributing empty bundles.
        }
    }

    /// <summary>
    /// Warn if there are any mesh assets tagged. If so, the user probably meant to tag a prefab instead.
    /// </summary>
    protected void WarnIfMeshAssetsAreTagged()
    {
        string[] assetGUIDs = AssetDatabase.FindAssets($"t:mesh,b:{BUNDLE_FILENAME}");
        foreach (var assetGUID in assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(assetGUID);
            if (!IsIncludedAssetPath(path))
            {
                continue;
            }

            EditorDebugLogBridge.LogWarning(string.Format("Mesh asset \"{0}\" is tagged for inclusion in the {1} AssetBundle! This is likely a mistake. You should include a prefab instead.", path, BUNDLE_FILENAME));
            ++NumWarnings;
        }
    }

    /// <summary>
    /// Warn if there are any material assets tagged. If so, the user probably meant to tag a prefab instead.
    /// </summary>
    protected void WarnIfMaterialsAreTaggedOrIncluded()
    {
        // Check for directly tagged materials.
        string[] assetGUIDs = AssetDatabase.FindAssets($"t:material,b:{BUNDLE_FILENAME}");
        foreach (var assetGUID in assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(assetGUID);
            if (!IsIncludedAssetPath(path))
            {
                continue;
            }

            EditorDebugLogBridge.LogWarning(string.Format("Material asset \"{0}\" is tagged for inclusion in the {1} AssetBundle! This is likely a mistake. You should use generate materials using the vanilla shaders instead.", path, BUNDLE_FILENAME));
            ++NumWarnings;
        }

        // Check for materials assigned to prefabs.
        assetGUIDs = AssetDatabase.FindAssets($"t:prefab,b:{BUNDLE_FILENAME}");
        foreach (var assetGUID in assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(assetGUID);
            if (!IsIncludedAssetPath(path))
            {
                continue;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            MeshRenderer[] renderers = prefab.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer.sharedMaterials.Any(m => m != null))
                {
                    EditorDebugLogBridge.LogWarning(string.Format("Material found attached to bundle prefab in \"{0}\" at \"<root>/{1}\"! This is likely a mistake. To avoid log spam and texturing issues, you should remove these materials or set them to \"None\".", path, GetGameObjectPath(renderer.transform).Split(new char[] { '/' }, 3)[2]));
                    ++NumWarnings;
                }
            }
        }
    }

    /// <summary>
    /// Computes the hierarchical path to a Unity transform for diagnostic output.
    /// </summary>
    /// <param name="current">The transform to trace.</param>
    /// <returns>The slash delimited path for the transform.</returns>
    public static string GetGameObjectPath(Transform current)
    {
        if (current.parent == null)
        {
            return "/" + current.name;
        }

        return GetGameObjectPath(current.parent) + "/" + current.name;
    }

    /// <summary>
    /// Removes all materials from prefabs tagged for the bundle after explicit user confirmation.
    /// </summary>
    [MenuItem("PlateUp!/Preparation/[Deprecated] Strip Materials From Prefabs")]
    public static void RemoveAllPrefabMaterials()
    {
        if (!EditorUtility.DisplayDialog("Confirm", "Stripping materials from prefabs is an irreversible process. Perform at your own risk.", "Proceed", "Cancel"))
        {
            return;
        }

        string[] assetGUIDs = AssetDatabase.FindAssets($"t:prefab,b:{BUNDLE_FILENAME}");
        foreach (var assetGUID in assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(assetGUID);
            if (!IsIncludedAssetPath(path))
            {
                continue;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            MeshRenderer[] renderers = prefab.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer.sharedMaterials.Length > 0)
                {
                    renderer.sharedMaterials = new Material[renderer.sharedMaterials.Length];
                    EditorDebugLogBridge.LogInfo(string.Format("Stripped materials from \"{0}\" at \"<root>/{1}\".", path, GetGameObjectPath(renderer.transform).Split(new char[] { '/' }, 3)[2]));
                }
            }
        }

        EditorDebugLogBridge.LogInfo(string.Format("[{0}] Done stripping materials.", DateTime.Now.ToLocalTime()));
    }

    /// <summary>
    /// Replaces all bundle prefab materials with Unity's default material after explicit user confirmation.
    /// </summary>
    [MenuItem("PlateUp!/Preparation/[Deprecated] Set Prefab Materials to Default")]
    public static void SetAllPrefabMaterialsToDefault()
    {
        if (!EditorUtility.DisplayDialog("Confirm", "Changing the materials of prefabs is an irreversible process. Perform at your own risk.", "Proceed", "Cancel"))
        {
            return;
        }

        string[] assetGUIDs = AssetDatabase.FindAssets($"t:prefab,b:{BUNDLE_FILENAME}");
        foreach (var assetGUID in assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(assetGUID);
            if (!IsIncludedAssetPath(path))
            {
                continue;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            MeshRenderer[] renderers = prefab.GetComponentsInChildren<MeshRenderer>();
            Material defaultMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer.sharedMaterials.Length > 0)
                {
                    Material[] newMaterials = new Material[renderer.sharedMaterials.Length];

                    for (int i = 0; i < newMaterials.Length; i++)
                    {
                        newMaterials[i] = defaultMaterial;
                    }

                    renderer.sharedMaterials = newMaterials;

                    EditorDebugLogBridge.LogInfo(string.Format("Set materials from \"{0}\" at \"<root>/{1}\".", path, GetGameObjectPath(renderer.transform).Split(new char[] { '/' }, 3)[2]));
                }
            }
        }

        EditorDebugLogBridge.LogInfo(string.Format("[{0}] Done setting materials.", DateTime.Now.ToLocalTime()));
    }
}
