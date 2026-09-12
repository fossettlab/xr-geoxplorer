using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeoX.Spatial
{
    /// <summary>Separate migration outputs. Private access keys are never in Scene.</summary>
    public sealed class SceneMigrationResult
    {
        public SceneDocument Scene { get; }
        private readonly JObject view, session, access, report;
        public JObject View => (JObject)view.DeepClone();
        public JObject Session => (JObject)session.DeepClone();
        public JObject PrivateAccess => (JObject)access.DeepClone();
        public JObject Report => (JObject)report.DeepClone();

        internal SceneMigrationResult(SceneDocument scene, JObject viewState, JObject sessionState,
            JObject privateAccess, JObject migrationReport)
        {
            Scene = scene;
            view = SceneSidecars.ReadView(viewState.ToString(), scene);
            session = SceneSidecars.ReadLegacyUnitySession(sessionState.ToString(), scene);
            access = SceneSidecars.ReadPrivateAccess(privateAccess.ToString(), scene);
            report = (JObject)migrationReport.DeepClone();
        }

        /// <summary>Publishes a new local directory atomically; refuses an existing target.
        /// The access file is private and must be excluded when sharing the scene.</summary>
        public void WriteNewDirectory(string directory)
        {
            string target = Path.GetFullPath(directory);
            if (Directory.Exists(target) || File.Exists(target)) throw new IOException("migration_target_exists");
            string parent = Path.GetDirectoryName(target);
            if (parent == null || !Directory.Exists(parent)) throw new DirectoryNotFoundException(parent);
            string staging = Path.Combine(parent, ".geox-migration-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(staging);
            try
            {
                WriteNew(staging, "scene.json", Scene.ToJson());
                WriteNew(staging, "view.json", view.ToString(Formatting.None));
                WriteNew(staging, "session.legacy-unity.json", session.ToString(Formatting.None));
                WriteNew(staging, "access.private.json", access.ToString(Formatting.None));
                WriteNew(staging, "migration-report.json", report.ToString(Formatting.None));
                Directory.Move(staging, target);
            }
            finally
            {
                if (Directory.Exists(staging)) Directory.Delete(staging, true);
            }
        }

        private static void WriteNew(string directory, string name, string json)
        {
            using (var stream = new FileStream(Path.Combine(directory, name), FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) writer.Write(json);
        }
    }
}
