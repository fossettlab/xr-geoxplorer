using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Flags `yield return` / `yield break` inside a try block that has a catch clause.
// That is C# compile error CS1626 ("Cannot yield a value in the body of a try block
// with a catch clause") — it will not compile. This is a pure syntax-tree check, so it
// needs no Unity assemblies, no project restore, and no license: just parse and walk.

// Self-test: we can't run this locally (no dotnet on the author's machine), so prove the
// detector works on every CI run. If it regresses, fail loudly instead of passing silently.
const string bad = @"class C { System.Collections.IEnumerator M() { try { yield return null; } catch { } } }";
const string good = @"class C { System.Collections.IEnumerator M() { yield return null; try { } catch { } } }";
if (FindViolations(bad).Count != 1 || FindViolations(good).Count != 0)
{
    Console.Error.WriteLine("yield-lint self-test FAILED — the detector is broken, not your code.");
    return 2;
}

const string cameraBad = "class C { void M() { var x = Camera.current; } }";
const string cameraGood = "class C { void M() { var x = Camera.main; } }";
if (FindCameraCurrentUsage("Assets/Scripts/Example.cs", cameraBad).Count != 1 ||
    FindCameraCurrentUsage("Assets/Scripts/Example.cs", cameraGood).Count != 0)
{
    Console.Error.WriteLine("yield-lint camera self-test FAILED — the Camera.current detector is broken.");
    return 2;
}

const string raycastBad = "class C { void M() { raycastManager.Raycast(p, h, t); } }";
const string raycastGood = "class C { void M() { if (raycastManager == null) return; raycastManager.Raycast(p, h, t); } }";
if (FindUnguardedRaycastManagerUsage("Assets/Scripts/RoomManager.cs", raycastBad).Count != 1 ||
    FindUnguardedRaycastManagerUsage("Assets/Scripts/RoomManager.cs", raycastGood).Count != 0)
{
    Console.Error.WriteLine("yield-lint raycast self-test FAILED — the AR raycast detector is broken.");
    return 2;
}

const string abInteractionBadGuard = @"class AssetBundleInteraction { void Update() { #if UNITY_EDITOR || UNITY_IOS
 } }";
const string abInteractionGoodGuard = @"class AssetBundleInteraction { void Update() { #if UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID
 } }";
const string abInteractionBadPointer = @"class AssetBundleInteraction { void OnPointerClicked(MixedRealityPointerEventData eventData) { } }";
const string abInteractionGoodPointer = @"class AssetBundleInteraction { void OnPointerClicked(MixedRealityPointerEventData eventData) { if (eventData?.Pointer?.Result?.Details?.Object == null) return; } }";
if (FindAssetBundleInteractionQuestGuard("Assets/Scripts/AssetBundleInteraction.cs", abInteractionBadGuard).Count != 1 ||
    FindAssetBundleInteractionQuestGuard("Assets/Scripts/AssetBundleInteraction.cs", abInteractionGoodGuard).Count != 0 ||
    FindAssetBundleInteractionQuestGuard("Assets/Scripts/AssetBundleInteraction.cs", abInteractionBadPointer).Count != 1 ||
    FindAssetBundleInteractionQuestGuard("Assets/Scripts/AssetBundleInteraction.cs", abInteractionGoodPointer).Count != 0)
{
    Console.Error.WriteLine("yield-lint AssetBundleInteraction self-test FAILED — the Quest interaction detector is broken.");
    return 2;
}

string target = args.Length > 0 ? args[0] : ".";
if (!Directory.Exists(target))
{
    Console.Error.WriteLine($"yield-lint: target directory '{target}' not found.");
    return 2;
}

int total = 0;
foreach (string path in Directory.EnumerateFiles(target, "*.cs", SearchOption.AllDirectories))
{
    string source = File.ReadAllText(path);
    foreach ((int line, string snippet) in FindViolations(source))
    {
        Console.WriteLine($"{path}:{line}: CS1626 — `yield` inside a try block with a catch clause: {snippet}");
        total++;
    }

    foreach ((int line, string snippet) in FindForbiddenSceneLoads(path, source))
    {
        Console.WriteLine($"{path}:{line}: scene-load — {snippet}");
        total++;
    }

    foreach ((int line, string snippet) in FindCameraCurrentUsage(path, source))
    {
        Console.WriteLine($"{path}:{line}: camera — {snippet}");
        total++;
    }

    foreach ((int line, string snippet) in FindFirebasePrematureFetchFlagReset(path, source))
    {
        Console.WriteLine($"{path}:{line}: firebase-anchor — {snippet}");
        total++;
    }

    foreach ((int line, string snippet) in FindUnguardedRaycastManagerUsage(path, source))
    {
        Console.WriteLine($"{path}:{line}: ar-placement — {snippet}");
        total++;
    }

    foreach ((int line, string snippet) in FindAssetBundleInteractionQuestGuard(path, source))
    {
        Console.WriteLine($"{path}:{line}: quest-interaction — {snippet}");
        total++;
    }
}

Console.WriteLine(total == 0
    ? "yield-lint: no yield-in-try-with-catch, forbidden scene-load, Camera.current, Firebase fetch-flag, unguarded AR raycast, or Quest asset-bundle interaction violations found."
    : $"yield-lint: {total} violation(s) found.");
return total == 0 ? 0 : 1;

static List<(int line, string snippet)> FindViolations(string code)
{
    var findings = new List<(int, string)>();
    SyntaxNode root = CSharpSyntaxTree.ParseText(code).GetRoot();
    foreach (YieldStatementSyntax y in root.DescendantNodes().OfType<YieldStatementSyntax>())
    {
        foreach (SyntaxNode anc in y.Ancestors())
        {
            // A yield belongs to its nearest enclosing iterator scope — stop at the function boundary
            // so a yield in this method isn't blamed on a try in an outer one (or vice versa).
            if (anc is MethodDeclarationSyntax or LocalFunctionStatementSyntax
                    or AccessorDeclarationSyntax or AnonymousFunctionExpressionSyntax)
                break;
            if (anc is TryStatementSyntax t && t.Catches.Count > 0 && t.Block.Span.Contains(y.Span))
            {
                int line = y.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                findings.Add((line, y.ToString().Trim()));
                break;
            }
        }
    }
    return findings;
}

static List<(int line, string snippet)> FindForbiddenSceneLoads(string path, string code)
{
    var findings = new List<(int, string)>();
    if (!IsAssetsScriptsPath(path))
    {
        return findings;
    }

    string[] lines = code.Split('\n');
    for (int i = 0; i < lines.Length; i++)
    {
        string line = lines[i];
        if (line.Contains("LoadLevel(\"SampleScene\")", StringComparison.Ordinal) ||
            line.Contains("LoadLevel(\"SampleScene\");", StringComparison.Ordinal))
        {
            findings.Add((i + 1, line.Trim()));
        }
    }

    return findings;
}

static List<(int line, string snippet)> FindCameraCurrentUsage(string path, string code)
{
    var findings = new List<(int, string)>();
    if (!IsAssetsScriptsPath(path))
    {
        return findings;
    }

    string[] lines = code.Split('\n');
    for (int i = 0; i < lines.Length; i++)
    {
        string line = lines[i];
        if (line.Contains("Camera.current", StringComparison.Ordinal))
        {
            findings.Add((i + 1, "Camera.current is null outside render callbacks; use Camera.main instead — " + line.Trim()));
        }
    }

    return findings;
}

static List<(int line, string snippet)> FindFirebasePrematureFetchFlagReset(string path, string code)
{
    var findings = new List<(int, string)>();
    if (!("/" + path.Replace('\\', '/').TrimStart('/')).EndsWith("/FirebaseExchanger.cs", StringComparison.OrdinalIgnoreCase))
    {
        return findings;
    }

    string[] lines = code.Split('\n');
    for (int i = 0; i < lines.Length; i++)
    {
        string line = lines[i];
        if (line.Contains("anchorsFetchSucceeded = false", StringComparison.Ordinal))
        {
            findings.Add((i + 1,
                "Do not reset anchorsFetchSucceeded before a fetch completes; use lastFetchSucceeded for per-fetch upload gating — " + line.Trim()));
        }
    }

    return findings;
}

static List<(int line, string snippet)> FindUnguardedRaycastManagerUsage(string path, string code)
{
    var findings = new List<(int, string)>();
    if (!("/" + path.Replace('\\', '/').TrimStart('/')).EndsWith("/RoomManager.cs", StringComparison.OrdinalIgnoreCase))
    {
        return findings;
    }

    string[] lines = code.Split('\n');
    for (int i = 0; i < lines.Length; i++)
    {
        string line = lines[i];
        if (!line.Contains("raycastManager.Raycast", StringComparison.Ordinal))
        {
            continue;
        }

        if (line.Contains("raycastManager == null", StringComparison.Ordinal) ||
            line.Contains("!arPlacementAvailable", StringComparison.Ordinal))
        {
            continue;
        }

        bool guarded = false;
        for (int j = Math.Max(0, i - 20); j < i; j++)
        {
            if (lines[j].Contains("raycastManager == null", StringComparison.Ordinal) ||
                lines[j].Contains("!arPlacementAvailable", StringComparison.Ordinal))
            {
                guarded = true;
                break;
            }
        }

        if (!guarded)
        {
            findings.Add((i + 1,
                "Guard raycastManager before Raycast — it is null on Quest OpenXR and other non-AR scenes — " + line.Trim()));
        }
    }

    return findings;
}

static List<(int line, string snippet)> FindAssetBundleInteractionQuestGuard(string path, string code)
{
    var findings = new List<(int, string)>();
    if (!("/" + path.Replace('\\', '/').TrimStart('/')).EndsWith("/AssetBundleInteraction.cs", StringComparison.OrdinalIgnoreCase))
    {
        return findings;
    }

    if (code.Contains("#if UNITY_EDITOR || UNITY_IOS\n", StringComparison.Ordinal) ||
        code.Contains("#if UNITY_EDITOR || UNITY_IOS\r\n", StringComparison.Ordinal))
    {
        int line = IndexOfLine(code, "#if UNITY_EDITOR || UNITY_IOS") + 1;
        if (line > 0)
        {
            findings.Add((line,
                "Update() touch path must include UNITY_ANDROID — Quest builds compile it out and lose all asset-bundle interactions — #if UNITY_EDITOR || UNITY_IOS"));
        }
    }

    if (code.Contains("void OnPointerClicked(MixedRealityPointerEventData eventData)", StringComparison.Ordinal) &&
        !code.Contains("eventData?.Pointer?.Result?.Details", StringComparison.Ordinal))
    {
        int line = IndexOfLine(code, "void OnPointerClicked(MixedRealityPointerEventData eventData)") + 1;
        if (line > 0)
        {
            findings.Add((line,
                "OnPointerClicked must handle MRTK pointer hits — HoloLens/WSA path was removed in OpenXR migration and Quest has no replacement without this handler"));
        }
    }

    return findings;
}

static int IndexOfLine(string code, string needle)
{
    int index = code.IndexOf(needle, StringComparison.Ordinal);
    if (index < 0)
    {
        return -1;
    }

    return code[..index].Count(c => c == '\n');
}

static bool IsAssetsScriptsPath(string path)
{
    // Normalize to a leading-slash form so the check matches both a bare
    // "Assets/Scripts/..." and an absolute ".../Assets/Scripts/...", while
    // still rejecting sibling names such as "MyAssets/Scripts/".
    string normalizedPath = "/" + path.Replace('\\', '/').TrimStart('/');
    return normalizedPath.Contains("/Assets/Scripts/", StringComparison.Ordinal);
}
