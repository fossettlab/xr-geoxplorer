using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeoX.Spatial
{
    // JSON.NET supplies the parser and lossless integer storage. The lexical check
    // excludes its JavaScript conveniences (comments, single quotes, NaN, etc.).
    internal static class PortableJson
    {
        private static readonly Regex NumberToken = new Regex(
            @"\A-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][+-]?[0-9]+)?\z",
            RegexOptions.CultureInvariant);

        internal static JObject Read(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            CheckLexemes(json);
            using (var text = new StringReader(json))
            using (var reader = new StrictReader(text) { DateParseHandling = DateParseHandling.None })
            {
                var value = JObject.Load(reader, new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                });
                Require(!reader.Read(), "trailing_content");
                CheckValues(value);
                return value;
            }
        }

        private sealed class StrictReader : JsonTextReader
        {
            internal StrictReader(TextReader reader) : base(reader) { }
            public override bool Read()
            {
                bool read = base.Read();
                Require(TokenType != JsonToken.PropertyName || QuoteChar == '"', "unquoted_property");
                return read;
            }
        }

        private static void CheckLexemes(string json)
        {
            char previous = '\0';
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n') continue;
                if (c == '"')
                {
                    bool closed = false;
                    while (++i < json.Length)
                    {
                        c = json[i];
                        Require(c >= ' ', "control_character_in_string");
                        if (c == '"') { closed = true; break; }
                        if (c != '\\') continue;
                        Require(++i < json.Length, "unfinished_escape");
                        c = json[i];
                        Require("\"\\/bfnrtu".IndexOf(c) >= 0, "invalid_escape");
                        if (c != 'u') continue;
                        for (int j = 0; j < 4; j++)
                            Require(++i < json.Length && Uri.IsHexDigit(json[i]), "invalid_unicode_escape");
                    }
                    Require(closed, "unfinished_string");
                    previous = 'v';
                }
                else if ("{}[],:".IndexOf(c) >= 0)
                {
                    Require((c != '}' && c != ']') || previous != ',', "trailing_comma");
                    previous = c;
                }
                else
                {
                    int start = i;
                    while (i + 1 < json.Length && " \t\r\n{}[],:\"".IndexOf(json[i + 1]) < 0) i++;
                    string token = json.Substring(start, i - start + 1);
                    Require(token == "true" || token == "false" || token == "null" ||
                        NumberToken.IsMatch(token), "invalid_json_token");
                    previous = 'v';
                }
            }
        }

        internal static void CheckValues(JToken value)
        {
            if (value is JContainer container)
            {
                foreach (JToken child in container.Children()) CheckValues(child);
                return;
            }
            Require(value.Type == JTokenType.Integer || value.Type == JTokenType.Float ||
                value.Type == JTokenType.String || value.Type == JTokenType.Boolean ||
                value.Type == JTokenType.Null, "non_json_value");
            if (value.Type == JTokenType.Float) Number(value);
        }

        internal static JObject Shape(JToken value, params string[] fields)
        {
            JObject obj = Object(value);
            Require(obj.Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal)
                .SequenceEqual(fields.OrderBy(n => n, StringComparer.Ordinal)), "unexpected_fields: " + obj.Path);
            return obj;
        }

        internal static JObject Object(JToken value)
        {
            Require(value is JObject, "expected_object: " + value?.Path);
            return (JObject)value;
        }

        internal static JArray Array(JToken value)
        {
            Require(value is JArray, "expected_array: " + value?.Path);
            return (JArray)value;
        }

        internal static string Text(JToken value, bool blankAllowed = false, bool nullable = false)
        {
            if (nullable && value?.Type == JTokenType.Null) return null;
            Require(value?.Type == JTokenType.String, "expected_string: " + value?.Path);
            string text = (string)value;
            Require(blankAllowed || !string.IsNullOrWhiteSpace(text), "blank_identifier: " + value.Path);
            return text;
        }

        internal static bool Boolean(JToken value)
        {
            Require(value?.Type == JTokenType.Boolean, "expected_boolean: " + value?.Path);
            return (bool)value;
        }

        internal static double Number(JToken value)
        {
            Require(value?.Type == JTokenType.Integer || value?.Type == JTokenType.Float,
                "expected_number: " + value?.Path);
            // Parse the JSON spelling so BigInteger does not pass through IConvertible.
            Require(double.TryParse(value.ToString(Formatting.None), NumberStyles.Float,
                CultureInfo.InvariantCulture, out double number) &&
                !double.IsNaN(number) && !double.IsInfinity(number), "non_finite_number: " + value.Path);
            return number;
        }

        internal static void PositiveInteger(JToken value)
        {
            Require(value?.Type == JTokenType.Integer, "expected_integer: " + value?.Path);
            string digits = value.ToString(Formatting.None);
            Require(digits != "0" && !digits.StartsWith("-", StringComparison.Ordinal), "expected_positive_integer");
        }

        internal static bool Version(JToken value, int expected) => value?.Type == JTokenType.Integer &&
            value.ToString(Formatting.None) == expected.ToString(CultureInfo.InvariantCulture);

        internal static void Choice(JToken value, params string[] choices) =>
            Require(choices.Contains(Text(value)), "unexpected_enum: " + value.Path);

        internal static void Require(bool condition, string error)
        {
            if (!condition) throw new JsonSerializationException(error);
        }
    }
}
