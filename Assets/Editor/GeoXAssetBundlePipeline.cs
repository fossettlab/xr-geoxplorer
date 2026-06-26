using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class GeoXAssetBundlePipeline
{
    private const string SourceRootArgument = "-geoXSourceRoot=";
    private const string OutputRootArgument = "-geoXOutputRoot=";
    private const string MetadataManifestArgument = "-geoXMetadataManifest=";
    private const string AllowPartialSourceArgument = "-geoXAllowPartialSource=";
    private const string BundlePathArgument = "-geoXBundlePath=";
    private const string DefaultSourceRoot = "Assets/GeoXSource/importable-source";
    private const string DefaultOutputRoot = "AssetBundles";
    private const string DefaultMetadataManifestPath = "docs/assetbundle-metadata-manifest.json";
    private const string BundlePrefix = "geoxplorer-";
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

    private static readonly HashSet<string> ManifestBuildPlatforms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "android",
        "ios",
        "wsa"
    };

    private static readonly string[] TicketBuildPlatforms =
    {
        "android",
        "ios"
    };

    private static readonly HashSet<string> OptionalRawSourceCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "bio"
    };

    private static readonly Dictionary<string, string[]> SourceCategoryFolderAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        { "handsample", new[] { "handsample", "handsamples" } },
        { "outcrop", new[] { "outcrop", "outcrops" } }
    };

    private static readonly HashSet<string> GenericSourceParentKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "assets",
        "assetbundles",
        "crystalmodels",
        "crystalviewer",
        "geoxassetbundles",
        "lroassetbundles",
        "materials",
        "model",
        "models",
        "textures"
    };

    [MenuItem("GeoXplorer/AssetBundles/Assign Per-Model Bundle Names")]
    public static void AssignPerModelBundleNames()
    {
        string platform = GetBuildTargetFolderName(EditorUserBuildSettings.activeBuildTarget);
        int assignedCount = AssignPerModelBundleNames(GetSourceRoot(), GetMetadataManifestPath(), platform);
        Debug.Log($"GeoX AssetBundle pipeline assigned {assignedCount} per-model bundle names.");
    }

    [MenuItem("GeoXplorer/AssetBundles/Validate/Source Layout")]
    public static void ValidateSourceLayout()
    {
        int modelCount = ValidateSourceLayout(GetSourceRoot());
        Debug.Log($"GeoX AssetBundle pipeline found {modelCount} source model assets across all required categories.");
    }

    [MenuItem("GeoXplorer/AssetBundles/Validate/Source Coverage Against Manifest")]
    public static void ValidateSourceCoverageAgainstManifest()
    {
        string platform = GetBuildTargetFolderName(EditorUserBuildSettings.activeBuildTarget);
        bool allowPartialSource = GetAllowPartialSource();
        int matchedCount = 0;
        if (ManifestBuildPlatforms.Contains(platform))
        {
            List<ManifestBundleEntry> manifestEntries = LoadManifestBundleEntries(GetMetadataManifestPath(), platform);
            matchedCount = ValidateSourceCoverageAgainstManifest(GetSourceRoot(), manifestEntries, allowPartialSource);
            Debug.Log($"GeoX AssetBundle pipeline matched {matchedCount} source entries for '{platform}' in {GetSourceCoverageModeLabel(allowPartialSource)} mode.");
            return;
        }

        foreach (string manifestPlatform in TicketBuildPlatforms)
        {
            List<ManifestBundleEntry> manifestEntries = LoadManifestBundleEntries(GetMetadataManifestPath(), manifestPlatform);
            matchedCount += ValidateSourceCoverageAgainstManifest(GetSourceRoot(), manifestEntries, allowPartialSource);
        }

        Debug.Log($"GeoX AssetBundle pipeline matched {matchedCount} source entries across ticket build platforms in {GetSourceCoverageModeLabel(allowPartialSource)} mode.");
    }

    [MenuItem("GeoXplorer/AssetBundles/Validate/Available Source Against Manifest")]
    public static void ValidateAvailableSourceAgainstManifest()
    {
        SourceBackedCoverage coverage = ResolveSourceBackedManifestCoverage(
            GetSourceRoot(),
            GetMetadataManifestPath(),
            GetValidationPlatformsForActiveTarget());
        LogSourceBackedCoverageSummary(coverage);
    }

    [MenuItem("GeoXplorer/AssetBundles/Validate/Staging Output Against Manifest")]
    public static void ValidateStagingOutputAgainstManifest()
    {
        ValidateStagingOutputAgainstManifest(GetOutputRoot(), GetMetadataManifestPath(), false);
    }

    [MenuItem("GeoXplorer/AssetBundles/Validate/Initial Bake Against Manifest")]
    public static void ValidateInitialBakeOutputAgainstManifest()
    {
        ValidateStagingOutputAgainstManifest(GetOutputRoot(), GetMetadataManifestPath(), true);
    }

    [MenuItem("GeoXplorer/AssetBundles/Validate/Available Source Output Against Manifest")]
    public static void ValidateAvailableSourceOutputAgainstManifest()
    {
        ValidateAvailableSourceOutputAgainstManifest(GetOutputRoot(), GetSourceRoot(), GetMetadataManifestPath());
    }

    [MenuItem("GeoXplorer/AssetBundles/Validate/Load Bundle From File")]
    public static void ValidateBundleLoadFromFile()
    {
        string bundlePath = GetArgumentValue(BundlePathArgument);
        if (string.IsNullOrEmpty(bundlePath))
        {
            throw new ArgumentException($"Pass {BundlePathArgument}<path> when running GeoXAssetBundlePipeline.ValidateBundleLoadFromFile.");
        }

        ValidateBundleLoadFromFile(bundlePath);
    }

    [MenuItem("GeoXplorer/AssetBundles/Write Azure Upload Plan")]
    public static void WriteAzureUploadPlan()
    {
        string planPath = WriteAzureUploadPlan(GetOutputRoot(), GetMetadataManifestPath());
        Debug.Log($"GeoX AssetBundle pipeline wrote Azure upload plan to '{planPath}'.");
    }

    [MenuItem("GeoXplorer/AssetBundles/Build/Build Active Target")]
    public static void BuildActiveTarget()
    {
        string platform = GetBuildTargetFolderName(EditorUserBuildSettings.activeBuildTarget);
        AssignBundleNamesForTargetPlatform(platform);
        BuildForTarget(EditorUserBuildSettings.activeBuildTarget, platform);
    }

    [MenuItem("GeoXplorer/AssetBundles/Build/Build Android")]
    public static void BuildAndroid()
    {
        AssignBundleNamesForTargetPlatform("android");
        BuildForTarget(BuildTarget.Android, "android");
    }

    [MenuItem("GeoXplorer/AssetBundles/Build/Build iOS")]
    public static void BuildIos()
    {
        AssignBundleNamesForTargetPlatform("ios");
        BuildForTarget(BuildTarget.iOS, "ios");
    }

    [MenuItem("GeoXplorer/AssetBundles/Build/Build Historical WSA")]
    public static void BuildWsa()
    {
        AssignBundleNamesForTargetPlatform("wsa");
        BuildForTarget(BuildTarget.WSAPlayer, "wsa");
    }

    [MenuItem("GeoXplorer/AssetBundles/Build/Build Standalone")]
    public static void BuildStandalone()
    {
        AssignPerModelBundleNames();
        BuildForTarget(GetStandaloneBuildTarget(), "standalone");
    }

    [MenuItem("GeoXplorer/AssetBundles/Build/Build Available Android")]
    public static void BuildAvailableAndroid()
    {
        AssignBundleNamesForTargetPlatform("android", true);
        BuildForTarget(BuildTarget.Android, "android");
    }

    [MenuItem("GeoXplorer/AssetBundles/Build/Build Available iOS")]
    public static void BuildAvailableIos()
    {
        AssignBundleNamesForTargetPlatform("ios", true);
        BuildForTarget(BuildTarget.iOS, "ios");
    }

    [MenuItem("GeoXplorer/AssetBundles/Build/Build All Ticket #6 Targets")]
    public static void BuildAllTicketTargets()
    {
        AssignBundleNamesForTargetPlatform("android");
        BuildForTarget(BuildTarget.Android, "android");

        AssignBundleNamesForTargetPlatform("ios");
        BuildForTarget(BuildTarget.iOS, "ios");
    }

    [MenuItem("GeoXplorer/AssetBundles/Build/Build Available Ticket #6 Targets")]
    public static void BuildAvailableTicketTargets()
    {
        AssignBundleNamesForTargetPlatform("android", true);
        BuildForTarget(BuildTarget.Android, "android");

        AssignBundleNamesForTargetPlatform("ios", true);
        BuildForTarget(BuildTarget.iOS, "ios");
    }

    [MenuItem("GeoXplorer/AssetBundles/Assemble Featured Bundles")]
    public static void AssembleFeaturedBundles()
    {
        int copiedCount = 0;
        string outputRoot = GetOutputRoot();
        JObject manifest = LoadMetadataManifest(GetMetadataManifestPath());
        JObject containers = (JObject)manifest["containers"];

        foreach (JProperty container in containers.Properties().Where(container => TicketBuildPlatforms.Contains(container.Name, StringComparer.OrdinalIgnoreCase)))
        {
            string platformFolder = Path.Combine(outputRoot, container.Name);
            if (!Directory.Exists(platformFolder))
            {
                Debug.LogWarning($"GeoX AssetBundle pipeline skipped featured assembly for '{container.Name}' because '{platformFolder}' does not exist.");
                continue;
            }

            copiedCount += AssembleFeaturedBundlesForPlatform(platformFolder, container.Value);
        }

        Debug.Log($"GeoX AssetBundle pipeline assembled {copiedCount} featured bundle aliases.");
    }

    public static int AssignPerModelBundleNames(string sourceRoot)
    {
        string platform = GetBuildTargetFolderName(EditorUserBuildSettings.activeBuildTarget);
        return AssignPerModelBundleNames(sourceRoot, GetMetadataManifestPath(), platform);
    }

    public static int AssignPerModelBundleNames(string sourceRoot, string metadataManifestPath, string platform)
    {
        return AssignPerModelBundleNames(sourceRoot, metadataManifestPath, platform, true);
    }

    public static int AssignPerModelBundleNames(string sourceRoot, string metadataManifestPath, string platform, bool failOnMissing)
    {
        if (ManifestBuildPlatforms.Contains(platform) && File.Exists(metadataManifestPath))
        {
            return AssignManifestBundleNames(sourceRoot, LoadManifestBundleEntries(metadataManifestPath, platform), failOnMissing);
        }

        return AssignLegacyPerModelBundleNames(sourceRoot);
    }

    private static int AssignLegacyPerModelBundleNames(string sourceRoot)
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
                string detail = IsOptionalRawSourceCategory(category)
                    ? "skipped known raw-source-gap category"
                    : "did not find source category";
                Debug.LogWarning($"GeoX AssetBundle pipeline {detail} '{category}' under '{sourceRoot}'.");
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

                importer.assetBundleName = GetBundleName(category, modelPath);
                importer.assetBundleVariant = string.Empty;
                assignedCount++;
            }
        }

        AssetDatabase.RemoveUnusedAssetBundleNames();
        AssetDatabase.SaveAssets();
        return assignedCount;
    }

    public static int ValidateSourceLayout(string sourceRoot)
    {
        if (string.IsNullOrEmpty(sourceRoot))
        {
            throw new ArgumentException("Source root is empty.", nameof(sourceRoot));
        }

        List<string> missingCategories = new List<string>();
        List<string> emptyCategories = new List<string>();
        List<string> optionalMissingCategories = new List<string>();
        List<string> optionalEmptyCategories = new List<string>();
        Dictionary<string, string> bundleNameOwners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        List<string> duplicateBundleNames = new List<string>();
        int modelCount = 0;

        foreach (string category in SourceCategories)
        {
            string categoryPath = FindCategoryPath(sourceRoot, category);
            if (categoryPath == null)
            {
                if (IsOptionalRawSourceCategory(category))
                {
                    optionalMissingCategories.Add(category);
                }
                else
                {
                    missingCategories.Add(category);
                }
                continue;
            }

            List<string> modelPaths = EnumerateModelAssetPaths(categoryPath).ToList();
            if (modelPaths.Count == 0)
            {
                if (IsOptionalRawSourceCategory(category))
                {
                    optionalEmptyCategories.Add(category);
                }
                else
                {
                    emptyCategories.Add(category);
                }
                continue;
            }

            modelCount += modelPaths.Count;
            foreach (string modelPath in modelPaths)
            {
                string bundleName = ResolveExistingBundleName(category, modelPath) ?? GetBundleName(category, modelPath);
                if (bundleNameOwners.TryGetValue(bundleName, out string existingPath))
                {
                    int existingPreference = GetModelTypePreference(Path.GetExtension(existingPath).ToLowerInvariant());
                    int currentPreference = GetModelTypePreference(Path.GetExtension(modelPath).ToLowerInvariant());
                    int existingPathPreference = GetSourcePathPreference(existingPath, bundleName);
                    int currentPathPreference = GetSourcePathPreference(modelPath, bundleName);
                    if (currentPreference < existingPreference ||
                        currentPreference == existingPreference && currentPathPreference < existingPathPreference)
                    {
                        bundleNameOwners[bundleName] = modelPath;
                    }
                    else if (existingPreference == currentPreference && existingPathPreference == currentPathPreference)
                    {
                        duplicateBundleNames.Add($"{bundleName}: {existingPath}, {modelPath}");
                    }
                }
                else
                {
                    bundleNameOwners.Add(bundleName, modelPath);
                }
            }
        }

        if (missingCategories.Count > 0 || emptyCategories.Count > 0 || duplicateBundleNames.Count > 0)
        {
            string message =
                "GeoX AssetBundle source validation failed." +
                FormatProblemList("Missing categories", missingCategories) +
                FormatProblemList("Empty categories", emptyCategories) +
                FormatProblemList("Duplicate bundle names", duplicateBundleNames);
            throw new InvalidOperationException(message);
        }

        if (optionalMissingCategories.Count > 0 || optionalEmptyCategories.Count > 0)
        {
            Debug.LogWarning(
                "GeoX AssetBundle source validation skipped known raw-source gaps." +
                FormatProblemList("Optional missing categories", optionalMissingCategories) +
                FormatProblemList("Optional empty categories", optionalEmptyCategories));
        }

        return modelCount;
    }

    private static int ValidateSourceCoverageAgainstManifest(string sourceRoot, List<ManifestBundleEntry> manifestEntries)
    {
        return ValidateSourceCoverageAgainstManifest(sourceRoot, manifestEntries, false);
    }

    private static int ValidateSourceCoverageAgainstManifest(string sourceRoot, List<ManifestBundleEntry> manifestEntries, bool allowMissingEntries)
    {
        ManifestSourceResolution resolution = ResolveManifestSourceEntries(sourceRoot, manifestEntries);
        ThrowIfBlockingSourceCoverageProblems(resolution, allowMissingEntries);

        if (allowMissingEntries && resolution.MissingEntries.Count > 0)
        {
            Debug.LogWarning(
                $"GeoX AssetBundle source coverage skipped {resolution.MissingEntries.Count} deployed bundles without staged source in partial-source mode." +
                FormatProblemList("Source-missing deployed bundles", resolution.MissingEntries.Take(30).Select(entry => entry.BlobName)));
        }

        if (resolution.OptionalMissingEntries.Count > 0)
        {
            Debug.LogWarning(
                $"GeoX AssetBundle source coverage skipped {resolution.OptionalMissingEntries.Count} known raw-source-gap bundles." +
                FormatProblemList("Optional missing bundles", resolution.OptionalMissingEntries.Take(20).Select(entry => entry.BlobName)));
        }

        return resolution.Matches.Count;
    }

    public static void ValidateStagingOutputAgainstManifest(string outputRoot, string metadataManifestPath)
    {
        ValidateStagingOutputAgainstManifest(outputRoot, metadataManifestPath, false);
    }

    public static void ValidateStagingOutputAgainstManifest(string outputRoot, string metadataManifestPath, bool allowKnownRawSourceGaps)
    {
        ValidateStagingOutputAgainstManifest(
            outputRoot,
            metadataManifestPath,
            allowKnownRawSourceGaps,
            GetValidationPlatformsForActiveTarget());
    }

    private static void ValidateStagingOutputAgainstManifest(
        string outputRoot,
        string metadataManifestPath,
        bool allowKnownRawSourceGaps,
        IEnumerable<string> platforms)
    {
        JObject manifest = LoadMetadataManifest(metadataManifestPath);
        JObject containers = (JObject)manifest["containers"];
        List<string> problems = new List<string>();
        List<string> allowedMissing = new List<string>();
        int expectedCount = 0;
        int allowedMissingCount = 0;
        List<string> checkedPlatforms = new List<string>();

        foreach (string platform in platforms.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            JToken platformManifest = containers[platform];
            if (platformManifest == null)
            {
                throw new InvalidDataException($"GeoX AssetBundle metadata manifest '{metadataManifestPath}' has no '{platform}' container.");
            }

            checkedPlatforms.Add(platform);
            string platformOutputRoot = Path.Combine(outputRoot, platform);
            HashSet<string> expectedNames = new HashSet<string>(
                platformManifest
                    .Select(blob => blob.Value<string>("name"))
                    .Where(name => !string.IsNullOrEmpty(name)),
                StringComparer.OrdinalIgnoreCase);
            HashSet<string> actualNames = GetStagingBundleNames(platformOutputRoot);
            expectedCount += expectedNames.Count;

            foreach (string missingName in expectedNames.Except(actualNames))
            {
                if (allowKnownRawSourceGaps && IsOptionalRawSourceBlob(missingName))
                {
                    allowedMissingCount++;
                    if (allowedMissing.Count < 20)
                    {
                        allowedMissing.Add($"{platform}: missing {missingName}");
                    }
                }
                else
                {
                    if (problems.Count < 20)
                    {
                        problems.Add($"{platform}: missing {missingName}");
                    }
                }
            }

            foreach (string unexpectedName in actualNames.Except(expectedNames))
            {
                if (problems.Count < 20)
                {
                    problems.Add($"{platform}: unexpected {unexpectedName}");
                }
            }
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                "GeoX AssetBundle staging output does not match the metadata manifest." +
                FormatProblemList("Problems", problems));
        }

        if (allowedMissing.Count > 0)
        {
            Debug.LogWarning(
                $"GeoX AssetBundle staging output is missing {allowedMissingCount} known raw-source-gap bundles." +
                FormatProblemList("Allowed missing bundles", allowedMissing));
        }

        int requiredCount = expectedCount - allowedMissingCount;
        Debug.Log($"GeoX AssetBundle staging output matches {requiredCount} required manifest bundle names for {string.Join(", ", checkedPlatforms)}.");
    }

    public static void ValidateAvailableSourceOutputAgainstManifest(string outputRoot, string sourceRoot, string metadataManifestPath)
    {
        IEnumerable<string> platforms = GetValidationPlatformsForActiveTarget();
        SourceBackedCoverage coverage = ResolveSourceBackedManifestCoverage(sourceRoot, metadataManifestPath, platforms);
        JObject manifest = LoadMetadataManifest(metadataManifestPath);
        JObject containers = (JObject)manifest["containers"];
        List<string> problems = new List<string>();
        int expectedCount = 0;

        foreach (string platform in platforms)
        {
            string platformOutputRoot = Path.Combine(outputRoot, platform);
            HashSet<string> expectedNames = new HashSet<string>(
                coverage.MatchedEntriesByPlatform[platform].Select(entry => entry.BlobName),
                StringComparer.OrdinalIgnoreCase);

            JToken platformManifest = containers[platform];
            foreach (string featuredBlobName in GetManifestBundleNames(platformManifest).Where(IsFeaturedBundleName))
            {
                string backingBundleName = GetFeaturedBackingBundleName(featuredBlobName);
                if (!string.IsNullOrEmpty(backingBundleName) && expectedNames.Contains(backingBundleName))
                {
                    expectedNames.Add(featuredBlobName);
                }
            }

            HashSet<string> actualNames = GetStagingBundleNames(platformOutputRoot);
            expectedCount += expectedNames.Count;

            foreach (string missingName in expectedNames.Except(actualNames))
            {
                if (problems.Count < 30)
                {
                    problems.Add($"{platform}: missing source-backed {missingName}");
                }
            }

            foreach (string unexpectedName in actualNames.Except(expectedNames))
            {
                if (problems.Count < 30)
                {
                    problems.Add($"{platform}: unexpected {unexpectedName}");
                }
            }
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                "GeoX AssetBundle available-source output does not match source-backed manifest entries." +
                FormatProblemList("Problems", problems));
        }

        LogSourceBackedCoverageSummary(coverage);
        Debug.Log($"GeoX AssetBundle available-source output matches {expectedCount} source-backed manifest bundle names.");
    }

    public static void ValidateBundleLoadFromFile(string bundlePath)
    {
        string normalizedPath = NormalizeSeparators(bundlePath);
        if (!File.Exists(normalizedPath))
        {
            throw new FileNotFoundException("GeoX AssetBundle load smoke file was not found.", normalizedPath);
        }

        AssetBundle bundle = AssetBundle.LoadFromFile(normalizedPath);
        if (bundle == null)
        {
            throw new InvalidOperationException($"GeoX AssetBundle load smoke could not load '{normalizedPath}'.");
        }

        try
        {
            string[] assetNames = bundle.GetAllAssetNames();
            string[] scenePaths = bundle.GetAllScenePaths();
            if (assetNames.Length == 0 && scenePaths.Length == 0)
            {
                throw new InvalidOperationException($"GeoX AssetBundle load smoke loaded '{normalizedPath}', but it contained no assets or scenes.");
            }

            Debug.Log($"GeoX AssetBundle load smoke loaded '{normalizedPath}' with {assetNames.Length} assets and {scenePaths.Length} scenes.");
        }
        finally
        {
            bundle.Unload(true);
        }
    }

    public static string WriteAzureUploadPlan(string outputRoot, string metadataManifestPath)
    {
        JObject manifest = LoadMetadataManifest(metadataManifestPath);
        JObject containers = (JObject)manifest["containers"];
        JObject planContainers = new JObject();

        foreach (JProperty container in containers.Properties())
        {
            string platform = container.Name;
            string platformOutputRoot = Path.Combine(outputRoot, platform);
            JArray blobs = new JArray();

            foreach (JToken blob in container.Value)
            {
                string blobName = blob.Value<string>("name");
                string localPath = ResolveWithinRoot(platformOutputRoot, blobName);
                if (localPath == null)
                {
                    Debug.LogWarning($"GeoX AssetBundle pipeline skipped upload-plan entry with unsafe blob name '{blobName}'.");
                    continue;
                }
                if (!File.Exists(localPath))
                {
                    continue;
                }

                blobs.Add(new JObject
                {
                    ["name"] = blobName,
                    ["platform"] = platform,
                    ["targetContainer"] = platform,
                    ["targetBlobName"] = blobName,
                    ["stagingBlobName"] = $"{platform}/{blobName}",
                    ["sourcePath"] = localPath,
                    ["contentType"] = blob.Value<string>("contentType") ?? "application/octet-stream",
                    ["metadata"] = blob["metadata"]?.DeepClone() ?? new JObject()
                });
            }

            planContainers[platform] = blobs;
        }

        JObject plan = new JObject
        {
            ["schemaVersion"] = 1,
            ["sourceManifest"] = metadataManifestPath,
            ["generatedAtUtc"] = DateTime.UtcNow.ToString("o"),
            ["containers"] = planContainers
        };

        Directory.CreateDirectory(outputRoot);
        string planPath = Path.Combine(outputRoot, "azure-upload-plan.json");
        File.WriteAllText(planPath, plan.ToString());
        return planPath;
    }

    // Combines root with a manifest-supplied relative name, rejecting names that
    // escape root via "../" segments or an absolute/rooted path. Returns the
    // resolved full path when it stays within root, otherwise null so callers
    // skip the entry instead of reading or writing outside the output tree.
    private static string ResolveWithinRoot(string root, string relativeName)
    {
        if (string.IsNullOrEmpty(relativeName))
        {
            return null;
        }

        string rootFull = Path.GetFullPath(root);
        string combined = Path.GetFullPath(Path.Combine(rootFull, relativeName));
        string rootWithSeparator = rootFull.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? rootFull
            : rootFull + Path.DirectorySeparatorChar;

        return combined.StartsWith(rootWithSeparator, StringComparison.Ordinal) ? combined : null;
    }

    private static void AssignBundleNamesForTargetPlatform(string platform)
    {
        AssignBundleNamesForTargetPlatform(platform, GetAllowPartialSource());
    }

    private static void AssignBundleNamesForTargetPlatform(string platform, bool allowMissingSource)
    {
        int assignedCount = AssignPerModelBundleNames(GetSourceRoot(), GetMetadataManifestPath(), platform, !allowMissingSource);
        string assignmentMode = ManifestBuildPlatforms.Contains(platform) ? "manifest" : "legacy";
        string coverageMode = allowMissingSource ? "available source" : "strict";
        Debug.Log($"GeoX AssetBundle pipeline assigned {assignedCount} {assignmentMode} bundle names for '{platform}' in {coverageMode} mode.");
    }

    private static int AssignManifestBundleNames(string sourceRoot, List<ManifestBundleEntry> manifestEntries, bool failOnMissing)
    {
        ManifestSourceResolution resolution = ResolveManifestSourceEntries(sourceRoot, manifestEntries);
        ThrowIfBlockingSourceCoverageProblems(resolution, !failOnMissing);

        if (!failOnMissing && resolution.MissingEntries.Count > 0)
        {
            Debug.LogWarning(
                $"GeoX AssetBundle pipeline skipped {resolution.MissingEntries.Count} deployed bundles without source in available-source mode." +
                FormatProblemList("Source-missing deployed bundles", resolution.MissingEntries.Take(20).Select(entry => entry.BlobName)));
        }

        if (resolution.OptionalMissingEntries.Count > 0)
        {
            Debug.LogWarning(
                $"GeoX AssetBundle pipeline skipped {resolution.OptionalMissingEntries.Count} known raw-source-gap bundles." +
                FormatProblemList("Optional missing bundles", resolution.OptionalMissingEntries.Take(20).Select(entry => entry.BlobName)));
        }

        ClearSourceAssetBundleNames(sourceRoot);

        foreach (KeyValuePair<ManifestBundleEntry, SourceAssetCandidate> match in resolution.Matches)
        {
            AssetImporter importer = AssetImporter.GetAtPath(match.Value.AssetPath);
            if (importer == null)
            {
                throw new InvalidOperationException($"GeoX AssetBundle pipeline could not load importer for matched source asset '{match.Value.AssetPath}'.");
            }

            importer.assetBundleName = match.Key.BlobName;
            importer.assetBundleVariant = string.Empty;
        }

        AssetDatabase.RemoveUnusedAssetBundleNames();
        AssetDatabase.SaveAssets();
        return resolution.Matches.Count;
    }

    private static void BuildForTarget(BuildTarget buildTarget, string outputFolderName)
    {
        EnsureActiveBuildTarget(buildTarget);

        string outputPath = Path.Combine(GetOutputRoot(), outputFolderName);
        Directory.CreateDirectory(outputPath);

        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
            outputPath,
            BuildAssetBundleOptions.StrictMode,
            buildTarget);

        if (manifest == null)
        {
            throw new InvalidOperationException($"GeoX AssetBundle pipeline failed to build {buildTarget} bundles; Unity did not return an AssetBundle manifest.");
        }

        string[] builtBundles = manifest.GetAllAssetBundles();
        if (builtBundles.Length == 0)
        {
            throw new InvalidOperationException($"GeoX AssetBundle pipeline failed to build {buildTarget} bundles; no bundles were written to '{outputPath}'.");
        }

        Debug.Log($"GeoX AssetBundle pipeline built {builtBundles.Length} {buildTarget} bundles to '{outputPath}'.");
    }

    private static void EnsureActiveBuildTarget(BuildTarget buildTarget)
    {
        if (EditorUserBuildSettings.activeBuildTarget == buildTarget)
        {
            return;
        }

        BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
        if (buildTargetGroup == BuildTargetGroup.Unknown)
        {
            throw new InvalidOperationException($"GeoX AssetBundle pipeline could not resolve a build target group for {buildTarget}.");
        }

        Debug.Log($"GeoX AssetBundle pipeline switching active build target from {EditorUserBuildSettings.activeBuildTarget} to {buildTarget}.");
        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget))
        {
            throw new InvalidOperationException($"GeoX AssetBundle pipeline failed to switch the active build target to {buildTarget}.");
        }
    }

    private static int AssembleFeaturedBundlesForPlatform(string platformFolder, JToken platformManifest)
    {
        int copiedCount = 0;

        foreach (string featuredBlobName in GetManifestBundleNames(platformManifest).Where(IsFeaturedBundleName))
        {
            string sourceBundle = ResolveFeaturedSourceBundle(platformFolder, featuredBlobName);
            if (sourceBundle == null)
            {
                Debug.LogWarning($"GeoX AssetBundle pipeline could not resolve featured source bundle for '{featuredBlobName}' in '{platformFolder}'.");
                continue;
            }

            string destinationBundle = ResolveWithinRoot(platformFolder, featuredBlobName);
            if (destinationBundle == null)
            {
                Debug.LogWarning($"GeoX AssetBundle pipeline skipped featured bundle with unsafe name '{featuredBlobName}' in '{platformFolder}'.");
                continue;
            }
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
        if (!Directory.Exists(normalizedRoot))
        {
            return null;
        }

        string[] categoryFolderNames = GetCategoryFolderNames(category).ToArray();
        string[] candidates = categoryFolderNames
            .SelectMany(name => new[]
            {
                Path.Combine(normalizedRoot, name + "~"),
                Path.Combine(normalizedRoot, name),
                Path.Combine(normalizedRoot, ToPascalCase(name) + "~"),
                Path.Combine(normalizedRoot, ToPascalCase(name))
            })
            .ToArray();

        string exactCandidate = candidates.FirstOrDefault(Directory.Exists);
        if (exactCandidate != null)
        {
            return exactCandidate;
        }

        return Directory.GetDirectories(normalizedRoot)
            .FirstOrDefault(path =>
            {
                string folderName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                return categoryFolderNames.Any(name =>
                    string.Equals(folderName, name + "~", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(folderName, name, StringComparison.OrdinalIgnoreCase));
            });
    }

    private static IEnumerable<string> GetCategoryFolderNames(string category)
    {
        yield return category;

        if (SourceCategoryFolderAliases.TryGetValue(category, out string[] aliases))
        {
            foreach (string alias in aliases)
            {
                if (!string.Equals(alias, category, StringComparison.OrdinalIgnoreCase))
                {
                    yield return alias;
                }
            }
        }
    }

    private static IEnumerable<string> GetManifestBundleNames(JToken platformManifest)
    {
        return platformManifest
            .Select(blob => blob.Value<string>("name"))
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => name.Replace("\\", "/"));
    }

    private static bool IsFeaturedBundleName(string blobName)
    {
        return blobName.StartsWith(FeaturedPrefix + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetFeaturedBackingBundleName(string featuredBlobName)
    {
        string[] parts = featuredBlobName.Replace("\\", "/").Split('/');
        if (parts.Length != 3)
        {
            return null;
        }

        return $"{BundlePrefix}{parts[1]}/{parts[2]}";
    }

    private static string ResolveFeaturedSourceBundle(string platformFolder, string featuredBlobName)
    {
        string[] parts = featuredBlobName.Replace("\\", "/").Split('/');
        if (parts.Length != 3)
        {
            return null;
        }

        string category = parts[1];
        string bundleName = parts[2];
        string directPath = Path.Combine(platformFolder, BundlePrefix + category, bundleName);
        if (File.Exists(directPath))
        {
            return directPath;
        }

        string searchPattern = Path.GetFileName(bundleName);
        string[] matches = Directory.GetFiles(platformFolder, searchPattern, SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}{FeaturedPrefix}{Path.DirectorySeparatorChar}"))
            .Where(path => NormalizeSeparators(path).Contains($"/{BundlePrefix}{category}/"))
            .ToArray();

        if (matches.Length == 1)
        {
            return matches[0];
        }

        if (matches.Length > 1)
        {
            Debug.LogWarning($"GeoX AssetBundle pipeline found multiple source bundles for featured manifest entry '{featuredBlobName}'.");
        }

        return null;
    }

    private static List<ManifestBundleEntry> LoadManifestBundleEntries(string metadataManifestPath, string platform)
    {
        JObject manifest = LoadMetadataManifest(metadataManifestPath);
        JObject containers = (JObject)manifest["containers"];
        JToken platformManifest = containers[platform];
        if (platformManifest == null)
        {
            throw new InvalidDataException($"GeoX AssetBundle metadata manifest '{metadataManifestPath}' has no '{platform}' container.");
        }

        List<ManifestBundleEntry> entries = new List<ManifestBundleEntry>();
        foreach (JToken blob in platformManifest)
        {
            string blobName = NormalizeSeparators(blob.Value<string>("name") ?? string.Empty);
            if (string.IsNullOrEmpty(blobName) || IsFeaturedBundleName(blobName))
            {
                continue;
            }

            string category = GetCategoryFromBlobName(blobName);
            if (string.IsNullOrEmpty(category))
            {
                continue;
            }

            JObject metadata = blob["metadata"] as JObject;
            string bundleFileName = Path.GetFileName(blobName);
            entries.Add(new ManifestBundleEntry
            {
                BlobName = blobName,
                Category = category,
                BundleKey = NormalizeKey(RemoveBundleSuffix(bundleFileName)),
                PrefabKey = NormalizeKey(metadata?.Value<string>("prefabName"))
            });
        }

        return entries.OrderBy(entry => entry.BlobName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static IEnumerable<string> GetValidationPlatformsForActiveTarget()
    {
        string platform = GetBuildTargetFolderName(EditorUserBuildSettings.activeBuildTarget);
        if (ManifestBuildPlatforms.Contains(platform))
        {
            return new[] { platform };
        }

        return TicketBuildPlatforms;
    }

    private static SourceBackedCoverage ResolveSourceBackedManifestCoverage(string sourceRoot, string metadataManifestPath, IEnumerable<string> platforms)
    {
        SourceBackedCoverage coverage = new SourceBackedCoverage();
        foreach (string platform in platforms.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            List<ManifestBundleEntry> manifestEntries = LoadManifestBundleEntries(metadataManifestPath, platform);
            ManifestSourceResolution resolution = ResolveManifestSourceEntries(sourceRoot, manifestEntries);
            ThrowIfBlockingSourceCoverageProblems(resolution, true);

            coverage.MatchedEntriesByPlatform[platform] = resolution.Matches.Keys
                .OrderBy(entry => entry.BlobName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            coverage.MissingRequiredCountsByPlatform[platform] = resolution.MissingEntries.Count;
            coverage.OptionalMissingCountsByPlatform[platform] = resolution.OptionalMissingEntries.Count;
        }

        return coverage;
    }

    private static void LogSourceBackedCoverageSummary(SourceBackedCoverage coverage)
    {
        int matchedCount = coverage.MatchedEntriesByPlatform.Values.Sum(entries => entries.Count);
        int missingRequiredCount = coverage.MissingRequiredCountsByPlatform.Values.Sum();
        int optionalMissingCount = coverage.OptionalMissingCountsByPlatform.Values.Sum();
        string perPlatform = string.Join(
            ", ",
            coverage.MatchedEntriesByPlatform.Select(pair =>
                $"{pair.Key}: {pair.Value.Count} matched, {coverage.MissingRequiredCountsByPlatform[pair.Key]} source-missing, {coverage.OptionalMissingCountsByPlatform[pair.Key]} optional-missing"));

        Debug.Log(
            "GeoX AssetBundle pipeline validated available source against the manifest. " +
            $"{matchedCount} source-backed bundles matched; {missingRequiredCount} deployed bundles have no staged source; " +
            $"{optionalMissingCount} optional raw-source-gap bundles were skipped. {perPlatform}");
    }

    private static ManifestSourceResolution ResolveManifestSourceEntries(string sourceRoot, List<ManifestBundleEntry> manifestEntries)
    {
        if (string.IsNullOrEmpty(sourceRoot))
        {
            throw new ArgumentException("Source root is empty.", nameof(sourceRoot));
        }

        if (!Directory.Exists(NormalizeSeparators(sourceRoot)))
        {
            throw new DirectoryNotFoundException($"GeoX AssetBundle source root '{sourceRoot}' was not found.");
        }

        ManifestSourceResolution resolution = new ManifestSourceResolution();
        Dictionary<string, List<SourceAssetCandidate>> candidatesByCategory = LoadSourceAssetCandidates(sourceRoot)
            .GroupBy(candidate => candidate.Category, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        Dictionary<string, ManifestBundleEntry> assignedAssetPaths = new Dictionary<string, ManifestBundleEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (ManifestBundleEntry entry in manifestEntries)
        {
            candidatesByCategory.TryGetValue(entry.Category, out List<SourceAssetCandidate> candidates);
            SourceAssetCandidate candidate = ResolveBestSourceCandidate(entry, candidates ?? new List<SourceAssetCandidate>(), resolution);
            if (candidate == null)
            {
                if (IsOptionalRawSourceCategory(entry.Category))
                {
                    resolution.OptionalMissingEntries.Add(entry);
                }
                else
                {
                    resolution.MissingEntries.Add(entry);
                }

                continue;
            }

            if (assignedAssetPaths.TryGetValue(candidate.AssetPath, out ManifestBundleEntry existingEntry))
            {
                resolution.DuplicateAssignments.Add($"{candidate.AssetPath}: {existingEntry.BlobName}, {entry.BlobName}");
                continue;
            }

            resolution.Matches.Add(entry, candidate);
            assignedAssetPaths.Add(candidate.AssetPath, entry);
        }

        return resolution;
    }

    private static SourceAssetCandidate ResolveBestSourceCandidate(ManifestBundleEntry entry, List<SourceAssetCandidate> candidates, ManifestSourceResolution resolution)
    {
        List<CandidateMatchScore> scores = candidates
            .Select(candidate => new CandidateMatchScore
            {
                Candidate = candidate,
                Score = GetMatchScore(entry, candidate),
                PathPreference = GetSourceCandidatePathPreference(entry, candidate)
            })
            .Where(score => score.Score >= 0)
            .OrderBy(score => score.Score)
            .ThenBy(score => score.Candidate.TypePreference)
            .ThenBy(score => score.PathPreference)
            .ThenBy(score => score.Candidate.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (scores.Count == 0)
        {
            return null;
        }

        CandidateMatchScore best = scores[0];
        List<CandidateMatchScore> exactTies = scores
            .Where(score =>
                score.Score == best.Score &&
                score.Candidate.TypePreference == best.Candidate.TypePreference &&
                score.PathPreference == best.PathPreference)
            .ToList();
        if (exactTies.Count > 1)
        {
            resolution.AmbiguousMatches.Add($"{entry.BlobName}: " + string.Join(", ", exactTies.Take(5).Select(score => score.Candidate.AssetPath)));
            return null;
        }

        return best.Candidate;
    }

    private static int GetMatchScore(ManifestBundleEntry entry, SourceAssetCandidate candidate)
    {
        if (!string.IsNullOrEmpty(entry.PrefabKey) && candidate.FileKey == entry.PrefabKey)
        {
            return 0;
        }

        if (!string.IsNullOrEmpty(entry.PrefabKey) && IsSafeSourcePrefixMatch(candidate.FileKey, entry.PrefabKey))
        {
            return 5 + candidate.FileKey.Length - entry.PrefabKey.Length;
        }

        if (!string.IsNullOrEmpty(candidate.ExistingBundleKey) && candidate.ExistingBundleKey == entry.BundleKey)
        {
            return 30;
        }

        if (candidate.FileKey == entry.BundleKey)
        {
            return 40;
        }

        if (!string.IsNullOrEmpty(entry.PrefabKey) && IsSafeFuzzyMatch(candidate.FileKey, entry.PrefabKey))
        {
            return 100 + GetLevenshteinDistance(candidate.FileKey, entry.PrefabKey);
        }

        if (!string.IsNullOrEmpty(candidate.ExistingBundleKey))
        {
            if (IsSafeFuzzyMatch(candidate.ExistingBundleKey, entry.BundleKey))
            {
                return 130 + GetLevenshteinDistance(candidate.ExistingBundleKey, entry.BundleKey);
            }
        }

        return -1;
    }

    private static int GetSourceCandidatePathPreference(ManifestBundleEntry entry, SourceAssetCandidate candidate)
    {
        string parentKey = GetParentFolderKey(candidate.AssetPath);
        if (!string.IsNullOrEmpty(parentKey))
        {
            if (!string.IsNullOrEmpty(entry.PrefabKey) && IsMatchingParentKey(parentKey, entry.PrefabKey))
            {
                return 0;
            }

            if (IsMatchingParentKey(parentKey, entry.BundleKey) ||
                IsMatchingParentKey(parentKey, candidate.FileKey) ||
                IsMatchingParentKey(parentKey, candidate.ExistingBundleKey))
            {
                return 0;
            }

            if (IsSpecificSourceParentKey(parentKey))
            {
                return 2;
            }
        }

        if (!string.IsNullOrEmpty(entry.PrefabKey) && candidate.FileKey == entry.PrefabKey)
        {
            return 5;
        }

        if (!string.IsNullOrEmpty(candidate.ExistingBundleKey) && candidate.ExistingBundleKey == entry.BundleKey)
        {
            return 6;
        }

        if (candidate.FileKey == entry.BundleKey)
        {
            return 7;
        }

        return 10;
    }

    private static List<SourceAssetCandidate> LoadSourceAssetCandidates(string sourceRoot)
    {
        List<SourceAssetCandidate> candidates = new List<SourceAssetCandidate>();
        foreach (string category in SourceCategories)
        {
            string categoryPath = FindCategoryPath(sourceRoot, category);
            if (categoryPath == null)
            {
                continue;
            }

            foreach (string modelPath in EnumerateModelAssetPaths(categoryPath))
            {
                string extension = Path.GetExtension(modelPath).ToLowerInvariant();
                string existingBundleFileName = ResolveExistingBundleFileName(modelPath);
                candidates.Add(new SourceAssetCandidate
                {
                    AssetPath = modelPath,
                    Category = category,
                    FileKey = NormalizeKey(Path.GetFileNameWithoutExtension(modelPath)),
                    ExistingBundleKey = NormalizeKey(RemoveBundleSuffix(existingBundleFileName)),
                    TypePreference = GetModelTypePreference(extension)
                });
            }
        }

        return candidates;
    }

    private static void ThrowIfBlockingSourceCoverageProblems(ManifestSourceResolution resolution)
    {
        ThrowIfBlockingSourceCoverageProblems(resolution, false);
    }

    private static void ThrowIfBlockingSourceCoverageProblems(ManifestSourceResolution resolution, bool allowMissingEntries)
    {
        bool hasBlockingMissingEntries = resolution.MissingEntries.Count > 0 && !allowMissingEntries;
        if (!hasBlockingMissingEntries && resolution.DuplicateAssignments.Count == 0 && resolution.AmbiguousMatches.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "GeoX AssetBundle source coverage does not match the metadata manifest." +
            FormatProblemList("Missing required manifest bundles", hasBlockingMissingEntries ? resolution.MissingEntries.Take(30).Select(entry => entry.BlobName) : Enumerable.Empty<string>()) +
            FormatProblemList("Duplicate source assignments", resolution.DuplicateAssignments.Take(20)) +
            FormatProblemList("Ambiguous source matches", resolution.AmbiguousMatches.Take(20)));
    }

    private static void ClearSourceAssetBundleNames(string sourceRoot)
    {
        foreach (string assetPath in EnumerateSourceAssetPaths(sourceRoot))
        {
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null || string.IsNullOrEmpty(importer.assetBundleName))
            {
                continue;
            }

            importer.assetBundleVariant = string.Empty;
            importer.assetBundleName = string.Empty;
        }
    }

    private static IEnumerable<string> EnumerateSourceAssetPaths(string sourceRoot)
    {
        foreach (string category in SourceCategories)
        {
            string categoryPath = FindCategoryPath(sourceRoot, category);
            if (categoryPath == null)
            {
                continue;
            }

            foreach (string path in Directory.GetFiles(categoryPath, "*", SearchOption.AllDirectories))
            {
                if (!path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    yield return ToAssetPath(path);
                }
            }
        }
    }

    private static string ResolveExistingBundleName(string category, string assetPath)
    {
        string bundleFileName = ResolveExistingBundleFileName(assetPath);
        if (string.IsNullOrEmpty(bundleFileName))
        {
            return null;
        }

        if (bundleFileName.Contains("/"))
        {
            return bundleFileName;
        }

        return $"{BundlePrefix}{category}/{bundleFileName}";
    }

    private static string ResolveExistingBundleFileName(string assetPath)
    {
        string metaPath = assetPath + ".meta";
        if (!File.Exists(metaPath))
        {
            return null;
        }

        foreach (string line in File.ReadLines(metaPath))
        {
            string trimmed = line.Trim();
            if (!trimmed.StartsWith("assetBundleName:", StringComparison.Ordinal))
            {
                continue;
            }

            string bundleName = NormalizeSeparators(trimmed.Substring("assetBundleName:".Length).Trim());
            if (string.IsNullOrEmpty(bundleName))
            {
                return null;
            }

            return Path.GetFileName(bundleName);
        }

        return null;
    }

    private static int GetSourcePathPreference(string assetPath, string bundleName)
    {
        string parentKey = GetParentFolderKey(assetPath);
        string fileKey = NormalizeKey(Path.GetFileNameWithoutExtension(assetPath));
        string bundleKey = NormalizeKey(RemoveBundleSuffix(Path.GetFileName(bundleName ?? string.Empty)));

        if (!string.IsNullOrEmpty(parentKey) && (IsMatchingParentKey(parentKey, fileKey) || IsMatchingParentKey(parentKey, bundleKey)))
        {
            return 0;
        }

        if (IsSpecificSourceParentKey(parentKey))
        {
            return 2;
        }

        if (!string.IsNullOrEmpty(bundleKey) && fileKey == bundleKey)
        {
            return 5;
        }

        return 10;
    }

    private static string GetParentFolderKey(string assetPath)
    {
        string parent = Path.GetDirectoryName(NormalizeSeparators(assetPath));
        if (string.IsNullOrEmpty(parent))
        {
            return string.Empty;
        }

        return NormalizeKey(Path.GetFileName(parent));
    }

    private static bool IsMatchingParentKey(string parentKey, string targetKey)
    {
        if (string.IsNullOrEmpty(parentKey) || string.IsNullOrEmpty(targetKey))
        {
            return false;
        }

        return parentKey == targetKey || parentKey.Length >= 4 && targetKey.StartsWith(parentKey, StringComparison.Ordinal);
    }

    private static bool IsSpecificSourceParentKey(string parentKey)
    {
        return !string.IsNullOrEmpty(parentKey) &&
            !GenericSourceParentKeys.Contains(parentKey) &&
            !SourceCategories.Contains(parentKey);
    }

    private static string GetSourceRoot()
    {
        string explicitSourceRoot = GetArgumentValue(SourceRootArgument) ?? Environment.GetEnvironmentVariable("GEOX_BUNDLE_SOURCE_ROOT");
        if (!string.IsNullOrEmpty(explicitSourceRoot))
        {
            return explicitSourceRoot;
        }

        return Directory.Exists(DefaultSourceRoot) ? DefaultSourceRoot : "Assets";
    }

    private static string GetOutputRoot()
    {
        return GetArgumentValue(OutputRootArgument) ?? Environment.GetEnvironmentVariable("GEOX_BUNDLE_OUTPUT_ROOT") ?? DefaultOutputRoot;
    }

    private static string GetMetadataManifestPath()
    {
        return GetArgumentValue(MetadataManifestArgument)
            ?? Environment.GetEnvironmentVariable("GEOX_METADATA_MANIFEST")
            ?? DefaultMetadataManifestPath;
    }

    private static bool GetAllowPartialSource()
    {
        string value = GetArgumentValue(AllowPartialSourceArgument) ?? Environment.GetEnvironmentVariable("GEOX_ALLOW_PARTIAL_SOURCE");
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetSourceCoverageModeLabel(bool allowPartialSource)
    {
        return allowPartialSource ? "partial-source" : "strict";
    }

    private static JObject LoadMetadataManifest(string metadataManifestPath)
    {
        if (string.IsNullOrEmpty(metadataManifestPath) || !File.Exists(metadataManifestPath))
        {
            throw new FileNotFoundException("GeoX AssetBundle metadata manifest was not found.", metadataManifestPath);
        }

        JObject manifest = JObject.Parse(File.ReadAllText(metadataManifestPath));
        if (manifest["containers"] == null)
        {
            throw new InvalidDataException($"GeoX AssetBundle metadata manifest '{metadataManifestPath}' has no containers object.");
        }

        return manifest;
    }

    private static HashSet<string> GetStagingBundleNames(string platformOutputRoot)
    {
        if (!Directory.Exists(platformOutputRoot))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return new HashSet<string>(
            Directory.GetFiles(platformOutputRoot, "*", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase))
                .Select(path => NormalizeSeparators(path.Substring(platformOutputRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)))
                .Where(path => path.StartsWith("geoxplorer-", StringComparison.OrdinalIgnoreCase)),
            StringComparer.OrdinalIgnoreCase);
    }

    private static string GetBundleName(string category, string modelPath)
    {
        string existingBundleName = ResolveExistingBundleName(category, modelPath);
        if (!string.IsNullOrEmpty(existingBundleName))
        {
            return existingBundleName;
        }

        string modelName = Path.GetFileNameWithoutExtension(modelPath).ToLowerInvariant();
        return $"{BundlePrefix}{category}/{modelName}-bundle";
    }

    private static bool IsOptionalRawSourceCategory(string category)
    {
        return OptionalRawSourceCategories.Contains(category);
    }

    private static bool IsOptionalRawSourceBlob(string blobName)
    {
        string normalized = blobName.Replace("\\", "/");
        foreach (string category in OptionalRawSourceCategories)
        {
            if (normalized.StartsWith($"{BundlePrefix}{category}/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string FormatProblemList(string title, IEnumerable<string> problems)
    {
        List<string> problemList = problems.ToList();
        if (problemList.Count == 0)
        {
            return string.Empty;
        }

        return $"{Environment.NewLine}{title}:{Environment.NewLine}- " + string.Join($"{Environment.NewLine}- ", problemList);
    }

    private static string GetCategoryFromBlobName(string blobName)
    {
        string normalized = NormalizeSeparators(blobName);
        int slashIndex = normalized.IndexOf("/", StringComparison.Ordinal);
        if (slashIndex < 0)
        {
            return null;
        }

        string containerPrefix = normalized.Substring(0, slashIndex);
        if (!containerPrefix.StartsWith(BundlePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return containerPrefix.Substring(BundlePrefix.Length);
    }

    private static string RemoveBundleSuffix(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        string normalized = value;
        if (normalized.EndsWith("-bundle", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(0, normalized.Length - "-bundle".Length);
        }

        return normalized;
    }

    private static string NormalizeKey(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        char[] characters = value
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray();
        return new string(characters);
    }

    private static int GetModelTypePreference(string extension)
    {
        switch (extension)
        {
            case ".prefab":
                return 0;
            case ".fbx":
                return 10;
            case ".obj":
                return 20;
            case ".dae":
                return 30;
            case ".blend":
                return 40;
            default:
                return 50;
        }
    }

    private static int GetLevenshteinDistance(string left, string right)
    {
        if (string.IsNullOrEmpty(left))
        {
            return string.IsNullOrEmpty(right) ? 0 : right.Length;
        }

        if (string.IsNullOrEmpty(right))
        {
            return left.Length;
        }

        int[,] distances = new int[left.Length + 1, right.Length + 1];
        for (int i = 0; i <= left.Length; i++)
        {
            distances[i, 0] = i;
        }

        for (int j = 0; j <= right.Length; j++)
        {
            distances[0, j] = j;
        }

        for (int i = 1; i <= left.Length; i++)
        {
            for (int j = 1; j <= right.Length; j++)
            {
                int substitutionCost = left[i - 1] == right[j - 1] ? 0 : 1;
                int deletion = distances[i - 1, j] + 1;
                int insertion = distances[i, j - 1] + 1;
                int substitution = distances[i - 1, j - 1] + substitutionCost;
                distances[i, j] = Math.Min(Math.Min(deletion, insertion), substitution);
            }
        }

        return distances[left.Length, right.Length];
    }

    private static bool IsSafeFuzzyMatch(string left, string right)
    {
        if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
        {
            return false;
        }

        if (!string.Equals(GetDigits(left), GetDigits(right), StringComparison.Ordinal))
        {
            return false;
        }

        return GetLevenshteinDistance(left, right) <= 2;
    }

    private static bool IsSafeSourcePrefixMatch(string candidateKey, string manifestKey)
    {
        if (string.IsNullOrEmpty(candidateKey) || string.IsNullOrEmpty(manifestKey) || manifestKey.Length < 4)
        {
            return false;
        }

        if (!candidateKey.StartsWith(manifestKey, StringComparison.Ordinal))
        {
            return false;
        }

        string manifestDigits = GetDigits(manifestKey);
        return string.IsNullOrEmpty(manifestDigits) || string.Equals(GetDigits(candidateKey), manifestDigits, StringComparison.Ordinal);
    }

    private static string GetDigits(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return new string(value.Where(char.IsDigit).ToArray());
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

    private sealed class ManifestBundleEntry
    {
        public string BlobName;
        public string Category;
        public string BundleKey;
        public string PrefabKey;
    }

    private sealed class SourceAssetCandidate
    {
        public string AssetPath;
        public string Category;
        public string FileKey;
        public string ExistingBundleKey;
        public int TypePreference;
    }

    private sealed class CandidateMatchScore
    {
        public SourceAssetCandidate Candidate;
        public int Score;
        public int PathPreference;
    }

    private sealed class ManifestSourceResolution
    {
        public readonly Dictionary<ManifestBundleEntry, SourceAssetCandidate> Matches = new Dictionary<ManifestBundleEntry, SourceAssetCandidate>();
        public readonly List<ManifestBundleEntry> MissingEntries = new List<ManifestBundleEntry>();
        public readonly List<ManifestBundleEntry> OptionalMissingEntries = new List<ManifestBundleEntry>();
        public readonly List<string> DuplicateAssignments = new List<string>();
        public readonly List<string> AmbiguousMatches = new List<string>();
    }

    private sealed class SourceBackedCoverage
    {
        public readonly Dictionary<string, List<ManifestBundleEntry>> MatchedEntriesByPlatform = new Dictionary<string, List<ManifestBundleEntry>>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, int> MissingRequiredCountsByPlatform = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, int> OptionalMissingCountsByPlatform = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }
}
