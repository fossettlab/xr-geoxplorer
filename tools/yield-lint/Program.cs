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

const string firebaseCacheBad = @"
class FirebaseExchanger {
    System.Collections.IEnumerator FetchAnchorsFromServer() {
        anchorObjects.Clear();
        yield return null;
    }
}";
const string firebaseCacheGood = @"
class FirebaseExchanger {
    System.Collections.IEnumerator FetchAnchorsFromServer() {
        if (fetchSucceeded) { anchorObjects.Clear(); }
        yield return null;
    }
}";
if (FindFirebaseCacheWipeBeforeFetch("Assets/Scripts/FirebaseExchanger.cs", firebaseCacheBad).Count != 1 ||
    FindFirebaseCacheWipeBeforeFetch("Assets/Scripts/FirebaseExchanger.cs", firebaseCacheGood).Count != 0)
{
    Console.Error.WriteLine("yield-lint firebase-cache self-test FAILED — the cache-wipe detector is broken.");
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

    foreach ((int line, string snippet) in FindFirebaseCacheWipeBeforeFetch(path, source))
    {
        Console.WriteLine($"{path}:{line}: firebase-cache — {snippet}");
        total++;
    }
}

Console.WriteLine(total == 0
    ? "yield-lint: no yield-in-try-with-catch, forbidden scene-load, Camera.current, or Firebase cache-wipe violations found."
    : $"yield-lint: {total} violation(s) found.");
return total == 0 ? 0 : 1;

static string NormalizeScriptPath(string path) => path.Replace('\\', '/');

static bool IsUnderAssetsScripts(string path) =>
    NormalizeScriptPath(path).Contains("Assets/Scripts/", StringComparison.Ordinal);

static bool IsFirebaseExchanger(string path) =>
    NormalizeScriptPath(path).EndsWith("Assets/Scripts/FirebaseExchanger.cs", StringComparison.Ordinal);

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
    if (!IsUnderAssetsScripts(path))
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
    if (!IsUnderAssetsScripts(path))
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

static List<(int line, string snippet)> FindFirebaseCacheWipeBeforeFetch(string path, string code)
{
    var findings = new List<(int, string)>();
    if (!IsFirebaseExchanger(path))
    {
        return findings;
    }

    SyntaxNode root = CSharpSyntaxTree.ParseText(code).GetRoot();
    foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
    {
        if (method.Identifier.Text != "FetchAnchorsFromServer")
        {
            continue;
        }

        foreach (InvocationExpressionSyntax invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression.ToString() != "anchorObjects.Clear")
            {
                continue;
            }

            bool insideFetchSucceededGuard = false;
            foreach (SyntaxNode anc in invocation.Ancestors())
            {
                if (anc == method)
                {
                    break;
                }

                if (anc is IfStatementSyntax ifStmt &&
                    ifStmt.Condition.ToString().Contains("fetchSucceeded", StringComparison.Ordinal))
                {
                    insideFetchSucceededGuard = true;
                    break;
                }
            }

            if (!insideFetchSucceededGuard)
            {
                int line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                findings.Add((line,
                    "anchorObjects.Clear() before fetch succeeds wipes the cache on network failure; stage in a temp list and commit only on success"));
            }
        }
    }

    return findings;
}
