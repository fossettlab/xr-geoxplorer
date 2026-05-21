using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class GeoXAssetBundlePipeline
{
    private const string SourceRootArgument = "-geoXSourceRoot=";
    private const string OutputRootArgument = "-geoXOutputRoot=";
    private const string FeaturedModelsArgument = "-geoXFeaturedModels=";
    private const string DefaultOutputRoot = "AssetBundles";
    private const string FeaturedPrefix = "geoxplorer-featured";

    private static readonly string[] SourceCategories =
    {
        "archeology",
        "architecture",
        "arthistory",
        "bio",
        "crystallattice",
        "dem",
        "drama",
        "handsample",
        "outcrop"
    };

    private static readonly string[] ModelExtensions =
    {
        ".prefab",
        ".fbx",
        ".obj",
        ".dae",
        ".blend"
    };

    [MenuItem("GeoXplorer/AssetBundles/Assign Per-Model Bundle Names")]
    public static void AssignPerModelBundleNames()
    {
        int assignedCount = AssignPerModelBundleNames(GetSourceRoot());
        Debug.Log($"GeoX AssetBundle pipeline assigned {assignedCount} per-model bundle names.");
    }

    [MenuItem("GeoXplorer/AssetBundles/Build/Build Active Target")]
    public static void BuildActiveTarget()
    {
        AssignPerModelBundleNames();
        BuildForTarget(EditorUserBuildSettings.activeBuildTarget, GetBuildTargetFolderName(EditorUserBuildSettings.activeBuildTarget));
    }

    [MenuItem("GeoXplorer/AssetBundles/Build/Build Android")]
    public static void BuildAndroid()
    {
        AssignPerModelBundleNames();
        BuildForTarget(BuildTarget.Android, "android");
    }

    [MenuItem("GeoXplorer/AssetBundles/Build/Build iOS")]
    public static void BuildIos()
    {
        AssignPerModelBundleNames();
        BuildForTarget(BuildTarget.iOS, "ios");
    }

    [MenuItem("GeoXplorer/AssetBundles/Build/Build WSA")]
    public static void BuildWsa()
    {
        AssignPerModelBundleNames();
        BuildForTarget(BuildTarget.WSAPlayer, "wsa");
    }

    [MenuItem("GeoXplorer/AssetBundles/Build/Build Standalone")]
    public static void BuildStandalone()
    {
        AssignPerModelBundleNames();
        BuildForTarget(GetStandaloneBuildTarget(), "standalone");
    }

    [MenuItem("GeoXplorer/AssetBundles/Build/Build All Ticket #6 Targets")]
    public static void BuildAllTicketTargets()
    {
        AssignPerModelBundleNames();
        BuildForTarget(GetStandaloneBuildTarget(), "standalone");
        BuildForTarget(BuildTarget.Android, "android");
        BuildForTarget(BuildTarget.iOS, "ios");
        BuildForTarget(BuildTarget.WSAPlayer, "wsa");
    }

    [MenuItem("GeoXplorer/AssetBundles/Assemble Featured Bundles")]
    public static void AssembleFeaturedBundles()
    {
        string featuredModelsPath = GetFeaturedModelsPath();
        if (string.IsNullOrEmpty(featuredModelsPath) || !File.Exists(featuredModelsPath))
        {
            throw new FileNotFoundException(
                "FeaturedModels.txt was not found. Pass -geoXFeaturedModels=/path/to/FeaturedModels.txt or place it at the output root.",
                featuredModelsPath);
        }

        int copiedCount = 0;
        foreach (string platformFolder in Directory.GetDirectories(GetOutputRoot()))
        {
            copiedCount += AssembleFeaturedBundlesForPlatform(platformFolder, featuredModelsPath);
        }

        Debug.Log($"GeoX AssetBundle pipeline assembled {copiedCount} featured bundle aliases.");
    }

    public static int AssignPerModelBundleNames(string sourceRoot)
    {
        if (string.IsNullOrEmpty(sourceRoot))
        {
            throw new ArgumentException("Source root is empty.", nameof(sourceRoot));
        }

        int assignedCount = 0;
        foreach (string category in SourceCategories)
        {
            string categoryPath = FindCategoryPath(sourceRoot, category);
            if (categoryPath == null)
            {
                Debug.LogWarning($"GeoX AssetBundle pipeline did not find source category '{category}' under '{sourceRoot}'.");
                continue;
            }

            foreach (string modelPath in EnumerateModelAssetPaths(categoryPath))
            {
                AssetImporter importer = AssetImporter.GetAtPath(modelPath);
                if (importer == null)
                {
                    Debug.LogWarning($"GeoX AssetBundle pipeline skipped '{modelPath}' because Unity has no importer for it.");
                    continue;
                }

                string modelName = Path.GetFileNameWithoutExtension(modelPath).ToLowerInvariant();
                importer.assetBundleName = $"geoxplorer-{category}/{modelName}-bundle";
                importer.assetBundleVariant = string.Empty;
                assignedCount++;
            }
        }

        AssetDatabase.RemoveUnusedAssetBundleNames();
        AssetDatabase.SaveAssets();
        return assignedCount;
    }

    private static void BuildForTarget(BuildTarget buildTarget, string outputFolderName)
    {
        string outputPath = Path.Combine(GetOutputRoot(), outputFolderName);
        Directory.CreateDirectory(outputPath);

        BuildPipeline.BuildAssetBundles(
            outputPath,
            BuildAssetBundleOptions.StrictMode,
            buildTarget);

        Debug.Log($"GeoX AssetBundle pipeline built {buildTarget} bundles to '{outputPath}'.");
    }

    private static int AssembleFeaturedBundlesForPlatform(string platformFolder, string featuredModelsPath)
    {
        int copiedCount = 0;
        string destinationRoot = Path.Combine(platformFolder, FeaturedPrefix);
        Directory.CreateDirectory(destinationRoot);

        foreach (string modelReference in ReadFeaturedModelReferences(featuredModelsPath))
        {
            string sourceBundle = ResolveFeaturedSourceBundle(platformFolder, modelReference);
            if (sourceBundle == null)
            {
                Debug.LogWarning($"GeoX AssetBundle pipeline could not resolve featured model '{modelReference}' in '{platformFolder}'.");
                continue;
            }

            string destinationBundle = GetFeaturedDestinationPath(destinationRoot, sourceBundle);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationBundle));
            File.Copy(sourceBundle, destinationBundle, true);
            copiedCount++;

            string sourceManifest = sourceBundle + ".manifest";
            if (File.Exists(sourceManifest))
            {
                File.Copy(sourceManifest, destinationBundle + ".manifest", true);
            }
        }

        return copiedCount;
    }

    private static IEnumerable<string> EnumerateModelAssetPaths(string categoryPath)
    {
        return Directory.GetFiles(categoryPath, "*.*", SearchOption.AllDirectories)
            .Where(IsModelAssetPath)
            .Select(ToAssetPath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsModelAssetPath(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        return ModelExtensions.Contains(extension) && !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindCategoryPath(string sourceRoot, string category)
    {
        string normalizedRoot = NormalizeSeparators(sourceRoot);
        string[] candidates =
        {
            Path.Combine(normalizedRoot, category + "~"),
            Path.Combine(normalizedRoot, category),
            Path.Combine(normalizedRoot, ToPascalCase(category) + "~"),
            Path.Combine(normalizedRoot, ToPascalCase(category))
        };

        return candidates.FirstOrDefault(Directory.Exists);
    }

    private static IEnumerable<string> ReadFeaturedModelReferences(string featuredModelsPath)
    {
        return File.ReadAllLines(featuredModelsPath)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .Where(line => !line.StartsWith("#", StringComparison.Ordinal))
            .Select(line => line.Replace("\\", "/"));
    }

    private static string ResolveFeaturedSourceBundle(string platformFolder, string modelReference)
    {
        string normalized = modelReference.Trim().Replace("\\", "/");
        string directPath = Path.Combine(platformFolder, normalized);
        if (File.Exists(directPath))
        {
            return directPath;
        }

        if (!normalized.EndsWith("-bundle", StringComparison.OrdinalIgnoreCase))
        {
            normalized += "-bundle";
        }

        if (normalized.StartsWith("geoxplorer-", StringComparison.OrdinalIgnoreCase))
        {
            directPath = Path.Combine(platformFolder, normalized);
            return File.Exists(directPath) ? directPath : null;
        }

        if (normalized.Contains("/"))
        {
            string[] parts = normalized.Split('/');
            if (parts.Length == 2)
            {
                directPath = Path.Combine(platformFolder, $"geoxplorer-{parts[0]}", parts[1]);
                return File.Exists(directPath) ? directPath : null;
            }
        }

        string[] matches = Directory.GetFiles(platformFolder, normalized, SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}{FeaturedPrefix}{Path.DirectorySeparatorChar}"))
            .ToArray();

        if (matches.Length == 1)
        {
            return matches[0];
        }

        if (matches.Length > 1)
        {
            Debug.LogWarning($"GeoX AssetBundle pipeline found multiple source bundles for featured model '{modelReference}'. Use category/modelName to disambiguate.");
        }

        return null;
    }

    private static string GetFeaturedDestinationPath(string destinationRoot, string sourceBundle)
    {
        string category = new DirectoryInfo(Path.GetDirectoryName(sourceBundle)).Name;
        string bundleName = Path.GetFileName(sourceBundle);
        string normalizedCategory = category.StartsWith("geoxplorer-", StringComparison.OrdinalIgnoreCase)
            ? category.Substring("geoxplorer-".Length)
            : category;

        return Path.Combine(destinationRoot, normalizedCategory, bundleName);
    }

    private static string GetSourceRoot()
    {
        return GetArgumentValue(SourceRootArgument) ?? Environment.GetEnvironmentVariable("GEOX_BUNDLE_SOURCE_ROOT") ?? "Assets";
    }

    private static string GetOutputRoot()
    {
        return GetArgumentValue(OutputRootArgument) ?? Environment.GetEnvironmentVariable("GEOX_BUNDLE_OUTPUT_ROOT") ?? DefaultOutputRoot;
    }

    private static string GetFeaturedModelsPath()
    {
        string explicitPath = GetArgumentValue(FeaturedModelsArgument) ?? Environment.GetEnvironmentVariable("GEOX_FEATURED_MODELS");
        if (!string.IsNullOrEmpty(explicitPath))
        {
            return explicitPath;
        }

        return Path.Combine(GetOutputRoot(), "FeaturedModels.txt");
    }

    private static string GetArgumentValue(string prefix)
    {
        foreach (string argument in Environment.GetCommandLineArgs())
        {
            if (argument.StartsWith(prefix, StringComparison.Ordinal))
            {
                return argument.Substring(prefix.Length).Trim('"');
            }
        }

        return null;
    }

    private static BuildTarget GetStandaloneBuildTarget()
    {
#if UNITY_EDITOR_OSX
        return BuildTarget.StandaloneOSX;
#elif UNITY_EDITOR_WIN
        return BuildTarget.StandaloneWindows64;
#else
        return BuildTarget.StandaloneLinux64;
#endif
    }

    private static string GetBuildTargetFolderName(BuildTarget buildTarget)
    {
        switch (buildTarget)
        {
            case BuildTarget.Android:
                return "android";
            case BuildTarget.iOS:
                return "ios";
            case BuildTarget.WSAPlayer:
                return "wsa";
            default:
                return "standalone";
        }
    }

    private static string ToAssetPath(string path)
    {
        string normalized = NormalizeSeparators(path);
        int assetsIndex = normalized.IndexOf("Assets/", StringComparison.Ordinal);
        return assetsIndex >= 0 ? normalized.Substring(assetsIndex) : normalized;
    }

    private static string NormalizeSeparators(string path)
    {
        return path.Replace("\\", "/");
    }

    private static string ToPascalCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return char.ToUpperInvariant(value[0]) + value.Substring(1);
    }
}
