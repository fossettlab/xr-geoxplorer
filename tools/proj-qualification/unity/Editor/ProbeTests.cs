using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace GeoX.ProjQualification
{
    public sealed class ProbeTests
    {
        [Test]
        public void NativeReferencesAndFailureRecoveryThroughManagedAbi()
        {
            string resources = Path.Combine(Application.streamingAssetsPath, "ProjQualification");
            string fixtures = Path.Combine(resources, "fixtures.json");
            Directory.CreateDirectory("build");
            Assert.That(ProbeBridge.geox_proj_probe_run(resources, fixtures,
                "build/unity-editor-report.json"), Is.Zero);
            Assert.That(ProbeBridge.geox_proj_probe_run(resources + "/absent", fixtures,
                "build/unity-editor-missing-database.json"), Is.EqualTo(1));
            Assert.That(File.ReadAllText("build/unity-editor-missing-database.json"),
                Does.Contain("database_unavailable"));
            Assert.That(ProbeBridge.geox_proj_probe_run(resources, fixtures,
                "build/unity-editor-recovery.json"), Is.Zero);
        }
    }
}
