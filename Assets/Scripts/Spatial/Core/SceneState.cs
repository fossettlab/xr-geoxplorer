using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeoX.Spatial
{
    public enum CoordinateKind { Unknown, Cartesian, Geographic }

    /// <summary>Editable document records; validate at load/save and operation boundaries.</summary>
    public sealed class ReferenceFrame
    {
        [JsonProperty(Required = Required.Always)] public string Id { get; set; }
        [JsonProperty(Required = Required.AllowNull)] public string Body { get; set; }
        [JsonProperty(Required = Required.Always)] public CoordinateKind Kind { get; set; }
        [JsonProperty(Required = Required.AllowNull)] public string AxisConvention { get; set; }
        [JsonProperty(Required = Required.AllowNull)] public double? MetresPerUnit { get; set; }
        // Datum, epoch, original axis units and conventions remain inspectable.
        [JsonProperty(Required = Required.Always)] public JObject Definition { get; set; } = new JObject();

        public void Validate()
        {
            SceneState.Require(!string.IsNullOrWhiteSpace(Id), "missing_frame_identity");
            SceneState.Require(Enum.IsDefined(typeof(CoordinateKind), Kind), "unknown_coordinate_kind");
            SceneState.Require(Definition != null, "missing_frame_definition");
            if (Kind == CoordinateKind.Cartesian)
                SceneState.Require(!string.IsNullOrWhiteSpace(AxisConvention), "missing_axis_convention");
            if (MetresPerUnit.HasValue) SceneState.Positive(MetresPerUnit.Value, "invalid_unit_factor");
        }

        internal bool SameDefinition(ReferenceFrame other) =>
            Id == other.Id && Body == other.Body && Kind == other.Kind &&
            AxisConvention == other.AxisConvention && MetresPerUnit == other.MetresPerUnit &&
            JToken.DeepEquals(Definition, other.Definition);
    }

    public sealed class SourceAsset
    {
        [JsonProperty(Required = Required.Always)] public string Id { get; set; }
        // An application resolver key, never a credential-bearing download URL.
        [JsonProperty(Required = Required.Always)] public string AccessReferenceId { get; set; }
        [JsonProperty(Required = Required.AllowNull)] public string ContentHash { get; set; }
        [JsonProperty(Required = Required.AllowNull)] public string Format { get; set; }
        [JsonProperty(Required = Required.Always)] public JObject Metadata { get; set; } = new JObject();
    }

    public sealed class Registration
    {
        [JsonProperty(Required = Required.Always)] public string SourceFrameId { get; set; }
        [JsonProperty(Required = Required.Always)] public string TargetFrameId { get; set; }
        [JsonProperty(Required = Required.Always)] public AffineMap SourceToScene { get; set; }
        [JsonProperty(Required = Required.Always)] public JObject Provenance { get; set; } = new JObject();
        [JsonProperty(Required = Required.AllowNull)] public JObject ValidationEvidence { get; set; }
    }

    public sealed class LayerState
    {
        [JsonProperty(Required = Required.Always)] public string Id { get; set; }
        [JsonProperty(Required = Required.Always)] public string Name { get; set; }
        [JsonProperty(Required = Required.Always)] public ReferenceFrame SourceFrame { get; set; }
        // Null explicitly means unregistered. There is no automatic identity map.
        [JsonProperty(Required = Required.AllowNull)] public Registration Registration { get; set; }
        [JsonProperty(Required = Required.Always)] public bool Visible { get; set; } = true;
        [JsonProperty(Required = Required.Always)] public List<SourceAsset> Assets { get; set; } = new List<SourceAsset>();
        [JsonProperty(Required = Required.Always)] public JObject Metadata { get; set; } = new JObject();
        [JsonProperty(Required = Required.Always)] public JObject Provenance { get; set; } = new JObject();
    }

    public sealed class DisplayState
    {
        [JsonProperty(Required = Required.Always)] public Vector3d Origin { get; set; }
        [JsonProperty(Required = Required.Always)] public AffineMap AxisBridge { get; set; } = AffineMap.Identity;
        [JsonProperty(Required = Required.Always)] public AffineMap Pose { get; set; } = AffineMap.Identity;
        [JsonProperty(Required = Required.Always)] public double Scale { get; set; } = 1;

        public void Validate()
        {
            SceneState.Positive(Scale, "invalid_display_scale");
            SceneState.Require(Pose != null && Pose.IsOrthogonal(false), "display_pose_must_be_rigid");
            SceneState.Require(AxisBridge != null && AxisBridge.IsOrthogonal(true) &&
                AxisBridge.Translation.X == 0 && AxisBridge.Translation.Y == 0 &&
                AxisBridge.Translation.Z == 0, "axis_bridge_must_only_change_axes");
        }
    }

    public sealed class SceneState
    {
        public const int CurrentSchemaVersion = 1;
        [JsonProperty(Required = Required.Always)] public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        [JsonProperty(Required = Required.Always)] public string Id { get; set; }
        [JsonProperty(Required = Required.Always)] public ReferenceFrame Frame { get; set; }
        [JsonProperty(Required = Required.Always)] public List<LayerState> Layers { get; set; } = new List<LayerState>();
        [JsonProperty(Required = Required.Always)] public DisplayState Display { get; set; } = new DisplayState();
        [JsonProperty(Required = Required.Always)] public DisplayState ResetDisplay { get; set; } = new DisplayState();
        [JsonProperty(Required = Required.Always)] public JObject Metadata { get; set; } = new JObject();

        [JsonIgnore]
        public bool HasPhysicalScaleBasis => Frame != null && Frame.Kind == CoordinateKind.Cartesian &&
            Frame.MetresPerUnit.HasValue;

        public void Validate()
        {
            Require(SchemaVersion == CurrentSchemaVersion, "unsupported_scene_schema");
            Require(!string.IsNullOrWhiteSpace(Id), "missing_scene_identity");
            Require(Frame != null && Display != null && ResetDisplay != null &&
                Layers != null && Metadata != null, "incomplete_scene");
            Frame.Validate(); Display.Validate(); ResetDisplay.Validate();
            Require(Frame.Kind != CoordinateKind.Geographic, "unsupported_common_frame");
            var ids = new HashSet<string>();
            foreach (LayerState layer in Layers)
            {
                Require(layer != null && !string.IsNullOrWhiteSpace(layer.Id) && ids.Add(layer.Id), "invalid_layer_identity");
                Require(layer.SourceFrame != null && layer.Assets != null && layer.Metadata != null &&
                    layer.Provenance != null && layer.Name != null, "incomplete_layer");
                layer.SourceFrame.Validate();
                var assetIds = new HashSet<string>();
                foreach (SourceAsset asset in layer.Assets)
                    Require(asset != null && !string.IsNullOrWhiteSpace(asset.Id) && assetIds.Add(asset.Id) &&
                        !string.IsNullOrWhiteSpace(asset.AccessReferenceId) && asset.Metadata != null &&
                        !asset.AccessReferenceId.Contains("://") && !asset.AccessReferenceId.Contains("?") &&
                        !asset.AccessReferenceId.Contains("#"), "invalid_asset_reference");
                if (layer.Registration != null) ValidateRegistration(layer);
            }
        }

        private void ValidateRegistration(LayerState layer)
        {
            Registration r = layer.Registration;
            ReferenceFrame source = layer.SourceFrame;
            Require(r.SourceFrameId == source.Id && r.TargetFrameId == Frame.Id &&
                r.SourceToScene != null && r.Provenance != null, "incomplete_registration");
            Require(source.Kind == CoordinateKind.Cartesian && Frame.Kind == CoordinateKind.Cartesian,
                "unsupported_registration_kind");
            Require(source.Body == Frame.Body, "registration_body_conflict");
            if (source.Id == Frame.Id)
            {
                Require(source.SameDefinition(Frame), "conflicting_frame_definition");
                Require(r.SourceToScene.Linear.SequenceEqual(AffineMap.Identity.Linear) &&
                    r.SourceToScene.Translation.X == 0 && r.SourceToScene.Translation.Y == 0 &&
                    r.SourceToScene.Translation.Z == 0, "same_frame_requires_identity_registration");
            }
            Require(source.MetresPerUnit.HasValue == Frame.MetresPerUnit.HasValue &&
                (source.MetresPerUnit.HasValue || source.SameDefinition(Frame)), "missing_unit_mapping");
        }

        /// <summary>Returns a separate document; scientific records are not edited by display changes.</summary>
        public SceneState WithDisplay(DisplayState display)
        {
            Require(display != null, "missing_display");
            display.Validate();
            SceneState copy = SceneJson.Read(SceneJson.Write(this));
            copy.Display = SceneJson.CopyDisplay(display);
            return copy;
        }

        public SceneState Reset() => WithDisplay(ResetDisplay);

        public Vector3d SourceToDisplay(string layerId, Vector3d sourcePoint)
        {
            Validate();
            LayerState layer = Layers.Single(l => l.Id == layerId);
            Require(layer.Registration != null, "layer_not_registered");
            Vector3d scenePoint = layer.Registration.SourceToScene.Apply(sourcePoint);
            return Display.Pose.Apply(Display.AxisBridge.ApplyLinear(
                (scenePoint - Display.Origin) * (Frame.MetresPerUnit ?? 1)) * Display.Scale);
        }

        public Vector3d DisplayToSource(string layerId, Vector3d displayPoint)
        {
            Validate();
            LayerState layer = Layers.Single(l => l.Id == layerId);
            Require(layer.Registration != null, "layer_not_registered");
            Vector3d local = Display.Pose.Inverse().Apply(displayPoint) * (1 / Display.Scale);
            Vector3d scene = Display.AxisBridge.Inverse().ApplyLinear(local) *
                (1 / (Frame.MetresPerUnit ?? 1)) + Display.Origin;
            return layer.Registration.SourceToScene.Inverse().Apply(scene);
        }

        internal static void Require(bool condition, string error)
        {
            if (!condition) throw new ArgumentException(error);
        }

        internal static void Positive(double value, string error)
        {
            Vector3d.CheckFinite(value);
            Require(value > 0, error);
            Require(!double.IsInfinity(1 / value), error);
        }
    }
}
