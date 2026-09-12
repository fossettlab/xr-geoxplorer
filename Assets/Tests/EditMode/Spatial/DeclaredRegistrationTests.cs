using System;
using System.Linq;
using GeoX.Spatial;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace GeoX.Spatial.Tests
{
    public class DeclaredRegistrationTests
    {
        private const string AppliedOperation = "user-supplied-operation";
        private const string AppliedRegistration = "user-supplied-registration";
        private const string AppliedRevision = "user-supplied-revision";

        private static JObject Evidence(string basis = "authored declaration") => new JObject
        {
            ["origins"] = new JArray(),
            ["lineage"] = new JArray(new JObject { ["source"] = "test fixture" }),
            ["statements"] = new JArray(new JObject
            {
                ["category"] = "declaration",
                ["status"] = "supplied",
                ["details"] = new JObject { ["basis"] = basis }
            })
        };

        private static AffineMap Mapping() => new AffineMap(
            new double[] { 1, 0.5, 0, 0, -2, 0, 0, 0, 3 },
            new Vector3d(4, 5, 6));

        private static SceneDocument UnregisteredScene()
        {
            JObject data = SpatialFixtures.Affine();
            data["layers"][0]["activeRegistrationId"] = null;
            data["metadata"]["preserved"] = new JObject { ["unknown"] = null };
            JObject other = (JObject)data["layers"][0].DeepClone();
            other["id"] = "unrelated-layer";
            other["name"] = "unrelated";
            ((JArray)data["layers"]).Add(other);
            return SpatialFixtures.ReadScene(data);
        }

        private static DeclaredRegistrationProposal Begin(SceneDocument scene,
            AffineMap map = null, JObject evidence = null) => DeclaredRegistration.Begin(
                scene, scene.Revision, "model-layer", scene.SceneFrameId,
                map ?? Mapping(), evidence ?? Evidence());

        private static SceneDocument Apply(SceneDocument scene,
            DeclaredRegistrationProposal proposal) => DeclaredRegistration.Apply(
                scene, proposal, AppliedOperation, AppliedRegistration, AppliedRevision);

        [Test]
        public void PreviewIsDetachedAndChangesOnlyTheSelectedLayerMapping()
        {
            SceneDocument original = UnregisteredScene();
            string originalJson = original.ToJson();
            DeclaredRegistrationProposal proposal = Begin(original);

            Assert.That(original.ToJson(), Is.EqualTo(originalJson));
            Assert.That((string)original.ToJObject()["layers"][0]["activeRegistrationId"], Is.Null);
            SpatialFixtures.Equal(proposal.Evaluate(new Vector3d(2, 3, 4)), 7.5, -1, 18);
            SpatialFixtures.Equal(proposal.PreviewScene.SourceToScene(
                "model-layer", new Vector3d(2, 3, 4)), 7.5, -1, 18);
            Assert.That((string)proposal.PreviewScene.ToJObject()["layers"][1]
                ["activeRegistrationId"], Is.Null);
        }

        [Test]
        public void ProposalDefensivelyCopiesTheMapAndEvidence()
        {
            SceneDocument original = UnregisteredScene();
            JObject evidence = Evidence("original basis");
            AffineMap map = Mapping();
            DeclaredRegistrationProposal proposal = Begin(original, map, evidence);
            evidence["statements"][0]["details"]["basis"] = "mutated caller value";
            JObject exposed = proposal.Evidence;
            exposed["statements"][0]["details"]["basis"] = "mutated returned copy";

            JObject previewOperation = (JObject)proposal.PreviewScene.ToJObject()
                ["operations"].Last;
            Assert.That((string)previewOperation["evidence"]["statements"][0]
                ["details"]["basis"], Is.EqualTo("original basis"));
            SpatialFixtures.Equal(proposal.Mapping.Apply(new Vector3d(2, 3, 4)),
                7.5, -1, 18);
        }

        [Test]
        public void ApplyAppendsHistoryAndPreservesUnknownsMetadataAndOtherLayers()
        {
            SceneDocument original = UnregisteredScene();
            JObject before = original.ToJObject();
            DeclaredRegistrationProposal proposal = Begin(original);
            SceneDocument applied = Apply(original, proposal);
            JObject after = applied.ToJObject();

            Assert.That(applied.Revision, Is.EqualTo(AppliedRevision));
            Assert.That(after["operations"].Count(), Is.EqualTo(before["operations"].Count() + 1));
            Assert.That(after["registrations"].Count(), Is.EqualTo(before["registrations"].Count() + 1));
            Assert.That((string)after["layers"][0]["activeRegistrationId"],
                Is.EqualTo(AppliedRegistration));
            Assert.That(after["registrations"].Last["supersedes"].Type,
                Is.EqualTo(JTokenType.Null));
            Assert.That(JToken.DeepEquals(after["frames"], before["frames"]), Is.True);
            Assert.That(JToken.DeepEquals(after["assets"], before["assets"]), Is.True);
            Assert.That(JToken.DeepEquals(after["metadata"], before["metadata"]), Is.True);
            Assert.That(JToken.DeepEquals(after["layers"][1], before["layers"][1]), Is.True);
            Assert.That(after["frames"][1]["body"].Type, Is.EqualTo(JTokenType.Null));
            Assert.That(after["frames"][1]["cartesian"].Type, Is.EqualTo(JTokenType.Null));
            Assert.That(JToken.DeepEquals(after["operations"].Last["evidence"], Evidence()), Is.True);
        }

        [Test]
        public void ApplyLinksThePreviousSolutionWithoutChangingOldRecords()
        {
            SceneDocument original = SpatialFixtures.ReadScene(SpatialFixtures.Affine());
            JObject before = original.ToJObject();
            SceneDocument applied = Apply(original, Begin(original));
            JObject after = applied.ToJObject();

            Assert.That((string)after["registrations"].Last["supersedes"],
                Is.EqualTo("declared-solution"));
            Assert.That(JToken.DeepEquals(after["operations"][0], before["operations"][0]), Is.True);
            Assert.That(JToken.DeepEquals(after["registrations"][0], before["registrations"][0]), Is.True);
        }

        [Test]
        public void CancelDiscardsPreviewAndKeepsCanonicalSceneExactly()
        {
            SceneDocument original = UnregisteredScene();
            DeclaredRegistrationProposal proposal = Begin(original);
            SceneDocument canonical = DeclaredRegistration.Cancel(original, proposal);
            Assert.That(canonical, Is.SameAs(original));
            Assert.That(canonical.ToJson(), Is.EqualTo(original.ToJson()));
        }

        [Test]
        public void ApplyReloadAndUndoRetainRecordsWhileRestoringNull()
        {
            SceneDocument original = UnregisteredScene();
            SceneDocument applied = Apply(original, Begin(original));
            SceneDocument reloaded = SceneDocument.Read(applied.ToJson());
            SpatialFixtures.Equal(reloaded.SourceToScene(
                "model-layer", new Vector3d(2, 3, 4)), 7.5, -1, 18);

            SceneDocument undone = DeclaredRegistration.Undo(reloaded,
                AppliedRevision, "model-layer", "undo-revision");
            JObject data = undone.ToJObject();
            Assert.That((string)data["layers"][0]["activeRegistrationId"], Is.Null);
            Assert.That(data["operations"].Count(), Is.EqualTo(2));
            Assert.That(data["registrations"].Count(), Is.EqualTo(2));
            Assert.Throws<InvalidOperationException>(() =>
                undone.SourceToScene("model-layer", default));
            Assert.That(SceneDocument.Read(undone.ToJson()).ToJson(), Is.EqualTo(undone.ToJson()));
        }

        [Test]
        public void UndoRestoresPreviousActiveSolutionWithoutErasingNewHistory()
        {
            SceneDocument original = SpatialFixtures.ReadScene(SpatialFixtures.Affine());
            SceneDocument applied = Apply(original, Begin(original));
            SceneDocument undone = DeclaredRegistration.Undo(applied,
                AppliedRevision, "model-layer", "undo-revision");
            JObject data = undone.ToJObject();

            Assert.That((string)data["layers"][0]["activeRegistrationId"],
                Is.EqualTo("declared-solution"));
            Assert.That(data["operations"].Count(), Is.EqualTo(2));
            Assert.That(data["registrations"].Count(), Is.EqualTo(2));
            SpatialFixtures.Equal(undone.SourceToScene(
                "model-layer", new Vector3d(2, 3, 4)), 14, 26, 46);
        }

        [Test]
        public void StaleProposalCannotOverwriteAnotherSceneRevision()
        {
            SceneDocument original = UnregisteredScene();
            DeclaredRegistrationProposal proposal = Begin(original);
            JObject changed = original.ToJObject();
            changed["revision"] = "concurrent-revision";
            changed["metadata"]["concurrent"] = true;
            SceneDocument concurrent = SpatialFixtures.ReadScene(changed);
            concurrent.ValidateRevisionOf(original);

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
                DeclaredRegistration.Apply(concurrent, proposal,
                    AppliedOperation, AppliedRegistration, "later-revision"));
            Assert.That(error.Message, Is.EqualTo("stale_registration_proposal"));
        }

        [Test]
        public void BeginAndUndoRequireTheExactExpectedRevisionAndSceneTarget()
        {
            SceneDocument scene = UnregisteredScene();
            Assert.Throws<InvalidOperationException>(() => DeclaredRegistration.Begin(
                scene, "stale-revision", "model-layer", scene.SceneFrameId,
                Mapping(), Evidence()));
            Assert.Throws<InvalidOperationException>(() => DeclaredRegistration.Begin(
                scene, scene.Revision, "model-layer", "original-model",
                Mapping(), Evidence()));

            SceneDocument applied = Apply(scene, Begin(scene));
            Assert.Throws<InvalidOperationException>(() => DeclaredRegistration.Undo(
                applied, "stale-revision", "model-layer", "undo-revision"));
        }

        [TestCase("operation")]
        [TestCase("registration")]
        [TestCase("revision")]
        public void ApplyRejectsReusedIdentifiers(string reused)
        {
            SceneDocument scene = SpatialFixtures.ReadScene(SpatialFixtures.Affine());
            DeclaredRegistrationProposal proposal = Begin(scene);
            string operation = reused == "operation" ? "declared-placement" : AppliedOperation;
            string registration = reused == "registration" ? "declared-solution" : AppliedRegistration;
            string revision = reused == "revision" ? scene.Revision : AppliedRevision;
            Assert.Throws<InvalidOperationException>(() => DeclaredRegistration.Apply(
                scene, proposal, operation, registration, revision), reused);
        }

        [Test]
        public void KnownBodyConflictIsRejectedWithoutRelabelingTheSource()
        {
            JObject data = SpatialFixtures.Affine();
            data["frames"][0]["body"] = "Earth";
            data["frames"][1]["body"] = "Moon";
            data["layers"][0]["activeRegistrationId"] = null;
            SceneDocument scene = SpatialFixtures.ReadScene(data);

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => Begin(scene));
            Assert.That(error.Message, Is.EqualTo("known_body_conflict"));
            Assert.That((string)scene.ToJObject()["frames"][1]["body"], Is.EqualTo("Moon"));
        }

        [TestCase("geographic")]
        [TestCase("wkt2")]
        [TestCase("required-extension")]
        public void UnsupportedCrsEvaluationIsRejected(string kind)
        {
            JObject data = SpatialFixtures.Affine();
            data["layers"][0]["activeRegistrationId"] = null;
            if (kind == "geographic") data["frames"][1]["kind"] = "geographic";
            if (kind == "wkt2")
            {
                data["frames"][1]["definition"]["encoding"] = "wkt2";
                data["frames"][1]["definition"]["value"] = "LOCAL_CS[\"authored\"]";
            }
            if (kind == "required-extension") data["extensions"] = new JObject
            {
                ["test:required"] = new JObject
                {
                    ["version"] = 1, ["required"] = true, ["payload"] = new JObject()
                }
            };
            SceneDocument scene = SpatialFixtures.ReadScene(data);
            Assert.Throws<InvalidOperationException>(() => Begin(scene), kind);
        }

        [Test]
        public void SingularMapAndMissingDeclarationEvidenceAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new AffineMap(new double[9], default));
            SceneDocument scene = UnregisteredScene();
            JObject empty = new JObject
            {
                ["origins"] = new JArray(),
                ["lineage"] = new JArray(),
                ["statements"] = new JArray()
            };
            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
                Begin(scene, evidence: empty));
            Assert.That(error.Message, Is.EqualTo("supplied_declaration_evidence_required"));
        }
    }
}
