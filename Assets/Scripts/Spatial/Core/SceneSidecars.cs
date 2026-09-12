using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static GeoX.Spatial.PortableJson;

namespace GeoX.Spatial
{
    /// <summary>Optional state, read independently and never merged into scientific records.</summary>
    public static class SceneSidecars
    {
        public static JObject ReadView(string json, SceneDocument scene)
        {
            JObject view = Read(json);
            Shape(view, "schemaVersion", "sceneId", "revision", "layers", "bookmarks");
            Header(view, scene); Text(view["revision"]);
            var layers = SceneDocumentValidation.Table(scene.ToJObject()["layers"]);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (JToken entry in Array(view["layers"]))
            {
                Shape(entry, "layerId", "visible", "style", "selectedRepresentationId");
                string id = SceneDocumentValidation.Reference(entry["layerId"], layers);
                Require(seen.Add(id), "duplicate_view_layer");
                Boolean(entry["visible"]); Object(entry["style"]);
                SceneDocumentValidation.Reference(entry["selectedRepresentationId"],
                    SceneDocumentValidation.Table(layers[id]["representations"]), true);
            }
            foreach (JToken bookmark in Array(view["bookmarks"]))
            {
                Shape(bookmark, "id", "name", "frameId", "position");
                Text(bookmark["id"]); Text(bookmark["name"], blankAllowed: true);
                SceneDocumentValidation.Reference(bookmark["frameId"],
                    SceneDocumentValidation.Table(scene.ToJObject()["frames"]));
                Require(Array(bookmark["position"]).Count == 3, "invalid_bookmark_position");
                foreach (JToken coordinate in (JArray)bookmark["position"]) Number(coordinate);
            }
            Require(Array(view["bookmarks"]).Select(b => (string)b["id"]).Distinct().Count() ==
                Array(view["bookmarks"]).Count, "duplicate_bookmark");
            return view;
        }

        public static JObject ReadPrivateAccess(string json, SceneDocument scene)
        {
            JObject access = Read(json);
            Shape(access, "schemaVersion", "sceneId", "assets"); Header(access, scene);
            var assets = SceneDocumentValidation.Table(scene.ToJObject()["assets"]);
            foreach (JProperty entry in Object(access["assets"]).Properties())
            {
                Require(assets.ContainsKey(entry.Name), "dangling_private_asset");
                Shape(entry.Value, "resolverKeys");
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (JToken key in Array(entry.Value["resolverKeys"]))
                    Require(seen.Add(Text(key)), "duplicate_resolver_key");
            }
            return access;
        }

        public static JObject ReadLegacyUnitySession(string json, SceneDocument scene)
        {
            JObject session = Read(json);
            Shape(session, "schemaVersion", "sceneId", "kind", "display", "resetDisplay");
            Header(session, scene); Choice(session["kind"], "schema1-unity-display");
            ReadDisplay(session["display"]); ReadDisplay(session["resetDisplay"]);
            return session;
        }

        /// <summary>Schema-1 display compatibility only. The legacy unitless fallback is
        /// retained here and never supplies physical calibration to the science document.</summary>
        public static Vector3d LegacySourceToDisplay(SceneDocument scene, JObject session,
            string layerId, Vector3d sourcePoint, bool reset = false)
        {
            JObject copy = ReadLegacyUnitySession(session.ToString(Formatting.None), scene);
            DisplayState display = ReadDisplay(copy[reset ? "resetDisplay" : "display"]);
            JObject frame = SceneDocumentValidation.Table(scene.ToJObject()["frames"])[scene.SceneFrameId];
            JToken units = (frame["cartesian"] as JObject)?["metresPerUnit"];
            double factor = units == null || units.Type == JTokenType.Null ? 1 : Number(units);
            Vector3d point = scene.SourceToScene(layerId, sourcePoint);
            return display.Pose.Apply(display.AxisBridge.ApplyLinear((point - display.Origin) * factor) * display.Scale);
        }

        private static void Header(JObject sidecar, SceneDocument scene)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));
            Require(Version(sidecar["schemaVersion"], 1), "unsupported_sidecar_schema");
            Require(Text(sidecar["sceneId"]) == scene.Id, "sidecar_scene_mismatch");
        }

        private static DisplayState ReadDisplay(JToken token)
        {
            Shape(token, "Origin", "AxisBridge", "Pose", "Scale");
            var display = new DisplayState
            {
                Origin = ReadVector(token["Origin"]), AxisBridge = ReadMap(token["AxisBridge"]),
                Pose = ReadMap(token["Pose"]), Scale = Number(token["Scale"])
            };
            display.Validate();
            return display;
        }

        private static AffineMap ReadMap(JToken token)
        {
            Shape(token, "Linear", "Translation");
            Require(Array(token["Linear"]).Count == 9, "invalid_session_map");
            return new AffineMap(((JArray)token["Linear"]).Select(Number).ToArray(), ReadVector(token["Translation"]));
        }

        private static Vector3d ReadVector(JToken token)
        {
            Shape(token, "X", "Y", "Z");
            return new Vector3d(Number(token["X"]), Number(token["Y"]), Number(token["Z"]));
        }
    }
}
