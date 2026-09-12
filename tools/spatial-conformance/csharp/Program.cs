using System;
using GeoX.Spatial;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Build and run from the repository root:
//   python3 scripts/build_spatial_conformance.py
//   printf '%s\n' '{"document": { ... }, "operationId": "...", "point": [0, 0, 0]}' | \
//     "$(python3 -c 'import json; print(json.load(open("build/spatial-conformance/csharp/command.json"))["argv"][0])')" \
//     "$(python3 -c 'import json; print(json.load(open("build/spatial-conformance/csharp/command.json"))["argv"][1])')"
// The process reads one JSON request from stdin and writes one JSON response to stdout.
// It links only the provider-free GeoX.Spatial core and JSON.NET; no assets or
// coordinate providers are fetched or invoked.
internal static class Program
{
    private static int Main()
    {
        try
        {
            string raw = Console.In.ReadToEnd();
            JObject request = PortableJson.Read(raw);
            ValidateRequest(request);

            JObject documentJson = (JObject)request["document"];
            SceneDocument document = SceneDocument.Read(documentJson.ToString(Formatting.None));
            JObject response = new JObject
            {
                ["status"] = "valid",
                ["document"] = document.ToJObject()
            };

            if (request.Property("operationId") != null)
            {
                string operationId = PortableJson.Text(request["operationId"]);
                bool available;
                string reason;
                try
                {
                    available = document.TryGetAffineOperation(operationId, out _, out reason);
                }
                catch (ArgumentException exception)
                {
                    available = false;
                    reason = CapabilityReason(exception);
                }

                response["available"] = available;
                response["reason"] = reason == null ? JValue.CreateNull() : new JValue(reason);
                if (available && request.Property("point") != null)
                {
                    Vector3d point = ReadPoint(request["point"]);
                    bool inverse = request.Property("inverse") != null &&
                        PortableJson.Boolean(request["inverse"]);
                    response["point"] = WritePoint(document.EvaluateOperation(operationId, point, inverse));
                }
            }
            else if (request.Property("layerId") != null)
            {
                bool available;
                string reason;
                try
                {
                    string layerId = PortableJson.Text(request["layerId"]);
                    string representationId = request.Property("representationId") == null
                        ? null : PortableJson.Text(request["representationId"]);
                    Vector3d point = ReadPoint(request["point"]);
                    bool inverse = request.Property("inverse") != null &&
                        PortableJson.Boolean(request["inverse"]);
                    Vector3d transformed = representationId == null
                        ? document.SourceToScene(layerId, point, inverse)
                        : document.RepresentationToSource(layerId, representationId, point, inverse);
                    available = true;
                    reason = null;
                    response["point"] = WritePoint(transformed);
                }
                catch (ArgumentException exception)
                {
                    available = false;
                    reason = CapabilityReason(exception);
                }
                catch (InvalidOperationException exception)
                {
                    available = false;
                    reason = exception.Message;
                }

                response["available"] = available;
                response["reason"] = reason == null ? JValue.CreateNull() : new JValue(reason);
            }

            Write(response);
            return 0;
        }
        catch (Exception exception)
        {
            Write(new JObject
            {
                ["status"] = "invalid",
                ["error"] = ErrorText(exception)
            });
            return 0;
        }
    }

    private static void ValidateRequest(JObject request)
    {
        foreach (JProperty property in request.Properties())
        {
            if (property.Name != "document" && property.Name != "operationId" &&
                property.Name != "point" && property.Name != "inverse" &&
                property.Name != "layerId" && property.Name != "representationId")
                throw new JsonSerializationException("unexpected_input_field: " + property.Name);
        }

        JToken document = request["document"];
        if (!(document is JObject))
            throw new JsonSerializationException("expected_object: document");

        if (request.Property("operationId") != null)
            PortableJson.Text(request["operationId"]);
        if (request.Property("layerId") != null)
            PortableJson.Text(request["layerId"]);
        if (request.Property("representationId") != null)
            PortableJson.Text(request["representationId"]);
        if (request.Property("operationId") != null && request.Property("layerId") != null)
            throw new JsonSerializationException("multiple_coordinate_operations");
        if (request.Property("representationId") != null && request.Property("layerId") == null)
            throw new JsonSerializationException("representation_requires_layer_id");
        if (request.Property("inverse") != null)
            PortableJson.Boolean(request["inverse"]);
        bool coordinateRequest = request.Property("operationId") != null ||
            request.Property("layerId") != null;
        if (request.Property("point") != null && !coordinateRequest)
            throw new JsonSerializationException("point_requires_operation_id");
        if (request.Property("inverse") != null && !coordinateRequest)
            throw new JsonSerializationException("inverse_requires_operation_id");
        if (request.Property("layerId") != null && request.Property("point") == null)
            throw new JsonSerializationException("layer_mapping_requires_point");
        if (request.Property("point") != null)
            ReadPoint(request["point"]);
    }

    private static Vector3d ReadPoint(JToken token)
    {
        JArray point = PortableJson.Array(token);
        PortableJson.Require(point.Count == 3, "expected_three_coordinate_point");
        return new Vector3d(
            PortableJson.Number(point[0]),
            PortableJson.Number(point[1]),
            PortableJson.Number(point[2]));
    }

    private static JArray WritePoint(Vector3d point) => new JArray(point.X, point.Y, point.Z);

    private static string CapabilityReason(ArgumentException exception)
    {
        const string unknownRecord = "unknown_record_identity";
        return exception.Message.StartsWith(unknownRecord, StringComparison.Ordinal)
            ? unknownRecord : exception.Message;
    }

    private static string ErrorText(Exception exception)
    {
        if (exception is JsonException || exception is ArgumentException ||
            exception is InvalidOperationException)
            return exception.Message;
        return exception.GetType().Name + ": " + exception.Message;
    }

    private static void Write(JObject value)
    {
        Console.Out.WriteLine(value.ToString(Formatting.None));
    }
}
