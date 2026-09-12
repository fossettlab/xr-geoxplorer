using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework.Api;
using NUnit.Framework.Internal;

internal static class Program
{
    private static int Main(string[] args)
    {
        var runner = new NUnitTestAssemblyRunner(new DefaultTestAssemblyBuilder());
        runner.Load(Assembly.GetExecutingAssembly(), new Dictionary<string, object>
        { ["NumberOfTestWorkers"] = 0 });
        var result = runner.Run(TestListener.NULL, TestFilter.Empty);
        File.WriteAllText(args[0], result.ToXml(true).OuterXml);
        Console.WriteLine($"Spatial tests: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped, {result.InconclusiveCount} inconclusive.");
        return result.PassCount > 0 && result.FailCount == 0 && result.SkipCount == 0 && result.InconclusiveCount == 0 ? 0 : 1;
    }
}
