using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace GeoX.Spatial
{
    /// <summary>Versioned document serialization; no Unity objects or runtime resource handles.</summary>
    public static class SceneJson
    {
        private static JsonSerializer Serializer() => JsonSerializer.Create(new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            MissingMemberHandling = MissingMemberHandling.Error,
            DateParseHandling = DateParseHandling.None,
            // Replace initialized immutable maps rather than populating their read-only properties.
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            Converters = { new StrictScalarConverter(), new StringEnumConverter { AllowIntegerValues = false } }
        });

        public static string Write(SceneState scene)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));
            scene.Validate();
            return JObject.FromObject(scene, Serializer()).ToString(Formatting.None);
        }

        public static SceneState Read(string json)
        {
            using (var text = new StringReader(json))
            using (var reader = new JsonTextReader(text) { DateParseHandling = DateParseHandling.None })
            {
                var document = JObject.Load(reader, new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                });
                if (reader.Read()) throw new JsonSerializationException("trailing_scene_content");
                JToken version = document[nameof(SceneState.SchemaVersion)];
                if (version == null || version.Type != JTokenType.Integer ||
                    version.Value<long>() != SceneState.CurrentSchemaVersion)
                    throw new JsonSerializationException("unsupported_scene_schema");
                SceneState scene = document.ToObject<SceneState>(Serializer());
                scene.Validate();
                return scene;
            }
        }

        internal static DisplayState CopyDisplay(DisplayState display) =>
            JObject.FromObject(display, Serializer()).ToObject<DisplayState>(Serializer());

        private sealed class StrictScalarConverter : JsonConverter
        {
            public override bool CanWrite => false;
            public override bool CanConvert(Type type) => type == typeof(double) ||
                type == typeof(double?) || type == typeof(int) || type == typeof(bool) || type == typeof(string);

            public override object ReadJson(JsonReader reader, Type type, object existing, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null && (type == typeof(double?) || type == typeof(string))) return null;
                if (type == typeof(string) && reader.TokenType == JsonToken.String) return (string)reader.Value;
                if (type == typeof(bool) && reader.TokenType == JsonToken.Boolean) return (bool)reader.Value;
                if (type == typeof(int) && reader.TokenType == JsonToken.Integer) return Convert.ToInt32(reader.Value);
                if ((type == typeof(double) || type == typeof(double?)) &&
                    (reader.TokenType == JsonToken.Integer || reader.TokenType == JsonToken.Float))
                    return Convert.ToDouble(reader.Value);
                throw new JsonSerializationException("unexpected_scalar_type: " + reader.Path);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
                throw new NotSupportedException();
        }
    }
}
