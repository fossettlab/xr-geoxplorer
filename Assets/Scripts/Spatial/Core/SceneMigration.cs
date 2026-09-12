using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace GeoX.Spatial
{
    /// <summary>Explicit import of experimental schema 1; never invoked by the v2 reader.</summary>
    public static class SceneMigration
    {
        public static SceneMigrationResult FromSchema1(string json, string newRevision)
        {
            JObject original = PortableJson.Read(json);
            SceneState old = SceneJson.Read(json);
            PortableJson.Require(!string.IsNullOrWhiteSpace(newRevision), "missing_migration_revision");
            var context = new MigrationContext(old, original, newRevision);
            return context.Run();
        }

        private sealed class MigrationContext
        {
            private readonly SceneState old;
            private readonly JObject original;
            private readonly string revision;
            private readonly JArray frames = new JArray(), assets = new JArray(), operations = new JArray();
            private readonly JArray registrations = new JArray(), layers = new JArray(), viewLayers = new JArray();
            private readonly JArray mappings = new JArray(), notes = new JArray();
            private readonly JObject access = new JObject();
            private readonly Dictionary<string, ReferenceFrame> sourceFrames = new Dictionary<string, ReferenceFrame>();
            private readonly Dictionary<string, JObject> sourceAssets = new Dictionary<string, JObject>();
            private readonly HashSet<string> reservedIds;

            internal MigrationContext(SceneState scene, JObject source, string newRevision)
            {
                old = scene; original = source; revision = newRevision;
                // Reserve all incoming asset names before allocating any generated name.
                reservedIds = new HashSet<string>(old.Layers.SelectMany(l => l.Assets).Select(a => a.Id), StringComparer.Ordinal);
            }

            internal SceneMigrationResult Run()
            {
                AddFrame(old.Frame, "/Frame");
                for (int i = 0; i < old.Layers.Count; i++) AddLayer(old.Layers[i], i);
                var document = new JObject
                {
                    ["schemaVersion"] = 2, ["id"] = old.Id, ["revision"] = revision,
                    ["sceneFrameId"] = old.Frame.Id, ["frames"] = frames, ["assets"] = assets,
                    ["operations"] = operations, ["registrations"] = registrations, ["layers"] = layers,
                    ["metadata"] = old.Metadata.DeepClone(), ["extensions"] = new JObject()
                };
                var view = new JObject
                {
                    ["schemaVersion"] = 1, ["sceneId"] = old.Id, ["revision"] = revision,
                    ["layers"] = viewLayers, ["bookmarks"] = new JArray()
                };
                var session = new JObject
                {
                    ["schemaVersion"] = 1, ["sceneId"] = old.Id, ["kind"] = "schema1-unity-display",
                    ["display"] = original["Display"].DeepClone(), ["resetDisplay"] = original["ResetDisplay"].DeepClone()
                };
                var privateAccess = new JObject { ["schemaVersion"] = 1, ["sceneId"] = old.Id, ["assets"] = access };
                var report = new JObject
                {
                    ["schemaVersion"] = 1, ["sourceSchemaVersion"] = 1, ["targetSchemaVersion"] = 2,
                    ["sceneId"] = old.Id, ["newRevision"] = revision, ["mappings"] = mappings, ["notes"] = notes,
                    ["interpretation"] = "Frames remain opaque. Legacy provenance/validation is uninterpreted. Hash algorithms and physical calibration are not inferred."
                };
                return new SceneMigrationResult(SceneDocument.Read(document.ToString()), view, session, privateAccess, report);
            }

            private void AddFrame(ReferenceFrame frame, string path)
            {
                if (sourceFrames.TryGetValue(frame.Id, out ReferenceFrame previous))
                    PortableJson.Require(frame.SameDefinition(previous), "conflicting_schema1_frame: " + frame.Id);
                else
                {
                    sourceFrames.Add(frame.Id, frame);
                    frames.Add(new JObject
                    {
                        ["id"] = frame.Id, ["body"] = NullableLabel(frame.Body, path + "/Body"),
                        ["kind"] = frame.Kind.ToString().ToLowerInvariant(),
                        // Preserve the whole old definition and convenience fields, including
                        // unusual unknown-frame combinations, without interpreting their text.
                        ["definition"] = new JObject
                        {
                            ["encoding"] = "opaque", ["value"] = new JObject
                            {
                                ["schema1"] = new JObject
                                {
                                    ["definition"] = frame.Definition.DeepClone(), ["body"] = frame.Body,
                                    ["axisConvention"] = frame.AxisConvention, ["metresPerUnit"] = frame.MetresPerUnit
                                }
                            }
                        },
                        ["cartesian"] = string.IsNullOrWhiteSpace(frame.AxisConvention) ? JValue.CreateNull() : (JToken)new JObject
                        { ["axisConvention"] = frame.AxisConvention, ["metresPerUnit"] = frame.MetresPerUnit }
                    });
                }
                Mapping(path, "frame", frame.Id, frame.Id);
            }

            private void AddLayer(LayerState layer, int index)
            {
                string path = "/Layers/" + index;
                AddFrame(layer.SourceFrame, path + "/SourceFrame");
                var assetIds = new JArray();
                for (int j = 0; j < layer.Assets.Count; j++)
                    assetIds.Add(AddAsset(layer.Assets[j], (JObject)original["Layers"][index]["Assets"][j], path + "/Assets/" + j));
                string solutionId = null;
                if (layer.Registration != null)
                {
                    Registration registration = layer.Registration;
                    string operationId = GeneratedId("operation", path + "/Registration/SourceToScene");
                    solutionId = GeneratedId("registration", path + "/Registration");
                    Vector3d translation = registration.SourceToScene.Translation;
                    operations.Add(new JObject
                    {
                        ["id"] = operationId, ["kind"] = "affine", ["version"] = 1,
                        ["sourceFrameId"] = registration.SourceFrameId, ["targetFrameId"] = registration.TargetFrameId,
                        ["payload"] = new JObject
                        {
                            ["linear"] = new JArray(registration.SourceToScene.Linear),
                            ["translation"] = new JArray(translation.X, translation.Y, translation.Z)
                        },
                        ["evidence"] = Declaration(path + "/Registration/SourceToScene")
                    });
                    registrations.Add(new JObject
                    {
                        ["id"] = solutionId, ["operationId"] = operationId, ["supersedes"] = null,
                        ["evidence"] = LegacyEvidence(new JObject
                        {
                            ["Provenance"] = registration.Provenance.DeepClone(),
                            ["ValidationEvidence"] = registration.ValidationEvidence?.DeepClone()
                        })
                    });
                    Mapping(path + "/Registration/SourceToScene", "operation", null, operationId);
                    Mapping(path + "/Registration", "registration", null, solutionId);
                }
                layers.Add(new JObject
                {
                    ["id"] = layer.Id, ["name"] = layer.Name, ["sourceFrameId"] = layer.SourceFrame.Id,
                    ["activeRegistrationId"] = solutionId, ["sourceAssetIds"] = assetIds,
                    // Schema 1 did not identify representation frames/basis conversions.
                    ["representations"] = new JArray(), ["metadata"] = layer.Metadata.DeepClone(),
                    ["evidence"] = LegacyEvidence(new JObject { ["Provenance"] = layer.Provenance.DeepClone() })
                });
                viewLayers.Add(new JObject
                {
                    ["layerId"] = layer.Id, ["visible"] = layer.Visible,
                    ["style"] = new JObject(), ["selectedRepresentationId"] = null
                });
                Mapping(path, "layer", layer.Id, layer.Id);
            }

            private string AddAsset(SourceAsset asset, JObject source, string path)
            {
                string id = asset.Id;
                if (sourceAssets.TryGetValue(id, out JObject previous))
                {
                    if (JToken.DeepEquals(previous, source)) { Mapping(path, "asset", asset.Id, id); return id; }
                    id = GeneratedId("asset", path);
                }
                sourceAssets.Add(id, (JObject)source.DeepClone());
                JObject metadata = (JObject)asset.Metadata.DeepClone();
                string key = "geox:schema1";
                while (metadata.Property(key) != null) key += ":legacy";
                // Legacy strings remain available, even when empty or not interpretable.
                metadata[key] = new JObject { ["ContentHash"] = asset.ContentHash, ["Format"] = asset.Format };
                assets.Add(new JObject
                {
                    ["id"] = id, ["contentHash"] = null, ["format"] = NullableLabel(asset.Format, path + "/Format"),
                    ["locators"] = new JArray(), ["metadata"] = metadata, ["evidence"] = EmptyEvidence()
                });
                access[id] = new JObject { ["resolverKeys"] = new JArray(asset.AccessReferenceId) };
                Mapping(path, "asset", asset.Id, id);
                notes.Add(new JObject { ["sourcePath"] = path, ["legacyMetadataKey"] = key, ["accessMapAssetId"] = id });
                return id;
            }

            private string NullableLabel(string value, string path)
            {
                if (value == null || !string.IsNullOrWhiteSpace(value)) return value;
                notes.Add(new JObject { ["sourcePath"] = path, ["interpretation"] = "Blank label retained in legacy details; typed value is unknown." });
                return null;
            }

            private string GeneratedId(string kind, string path)
            {
                // Hash identifies the migration path, not dataset bytes or scientific equivalence.
                string input = new JArray(old.Id, path).ToString(Newtonsoft.Json.Formatting.None);
                string digest;
                using (SHA256 sha = SHA256.Create())
                    digest = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", "").ToLowerInvariant();
                string id = "schema1:" + kind + ":" + digest;
                while (!reservedIds.Add(id)) id += ":collision";
                return id;
            }

            private void Mapping(string path, string kind, string from, string to) => mappings.Add(new JObject
            { ["sourcePath"] = path, ["kind"] = kind, ["oldId"] = from, ["newId"] = to });
        }

        private static JObject EmptyEvidence() => new JObject
        { ["origins"] = new JArray(), ["lineage"] = new JArray(), ["statements"] = new JArray() };

        private static JObject LegacyEvidence(JObject details)
        {
            JObject evidence = EmptyEvidence();
            ((JArray)evidence["statements"]).Add(new JObject
            { ["category"] = "legacy", ["status"] = "unknown", ["details"] = details });
            return evidence;
        }

        private static JObject Declaration(string path)
        {
            JObject evidence = EmptyEvidence();
            ((JArray)evidence["statements"]).Add(new JObject
            {
                ["category"] = "declaration", ["status"] = "supplied",
                ["details"] = new JObject { ["sourcePath"] = path, ["method"] = "Exact schema-1 affine copy; no fitting or validation upgrade." }
            });
            return evidence;
        }
    }
}
