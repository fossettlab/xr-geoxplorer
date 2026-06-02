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

string target = args.Length > 0 ? args[0] : ".";
if (!Directory.Exists(target))
{
    Console.Error.WriteLine($"yield-lint: target directory '{target}' not found.");
    return 2;
}

int total = 0;
foreach (string path in Directory.EnumerateFiles(target, "*.cs", SearchOption.AllDirectories))
{
    foreach ((int line, string snippet) in FindViolations(File.ReadAllText(path)))
    {
        Console.WriteLine($"{path}:{line}: CS1626 — `yield` inside a try block with a catch clause: {snippet}");
        total++;
    }
}

Console.WriteLine(total == 0
    ? "yield-lint: no yield-in-try-with-catch violations found."
    : $"yield-lint: {total} violation(s) found — these will not compile (CS1626).");
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
