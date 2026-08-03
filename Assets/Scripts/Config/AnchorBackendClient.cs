using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// HTTP client for Azure Function anchor persistence (#40 Phase B).
/// Replaces Firebase Realtime Database PUT/GET when wired into gameplay.
/// Uses the same <see cref="RemoteConfig.SasEndpointBaseUrl"/> and
/// <see cref="RemoteConfig.SasApiKey"/> as the SAS client for v1.
/// </summary>
public static class AnchorBackendClient
{
    [Serializable]
    public class AnchorBackendRecord
    {
        public string id;
        public string name;
        public string identifier;
        public string date_created;
        public string date_expired;
    }

    /// <summary>Firebase-compatible list entry (camelCase date fields).</summary>
    [Serializable]
    public class AnchorListEntry
    {
        public string name;
        public string identifier;
        public string dateCreated;
        public string dateExpired;
    }

    [Serializable]
    private class CreateRequest
    {
        public string name;
        public string identifier;
        public string dateExpired;
    }

    public static string BuildListUrl(string endpointBaseUrl)
    {
        return AppendApiPath(endpointBaseUrl, "anchors");
    }

    public static string BuildCreateUrl(string endpointBaseUrl)
    {
        return AppendApiPath(endpointBaseUrl, "anchors");
    }

    public static string BuildGetUrl(string endpointBaseUrl, string anchorId)
    {
        if (string.IsNullOrWhiteSpace(anchorId))
        {
            return string.Empty;
        }

        return AppendApiPath(endpointBaseUrl, "anchors/" + anchorId.Trim());
    }

    public static bool TryParseAnchorResponse(string json, out AnchorBackendRecord record)
    {
        record = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        AnchorBackendRecord parsed;
        try
        {
            parsed = JsonUtility.FromJson<AnchorBackendRecord>(json);
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (parsed == null ||
            string.IsNullOrWhiteSpace(parsed.id) ||
            string.IsNullOrWhiteSpace(parsed.identifier))
        {
            return false;
        }

        record = parsed;
        return true;
    }

    public static bool TryParseAnchorListResponse(string json, out List<AnchorListEntry> entries)
    {
        entries = null;
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "null")
        {
            entries = new List<AnchorListEntry>();
            return true;
        }

        try
        {
            entries = JsonConvert.DeserializeObject<List<AnchorListEntry>>(json);
        }
        catch (JsonException)
        {
            entries = null;
            return false;
        }

        return entries != null;
    }

    public static IEnumerator ListAnchors(
        Action<List<AnchorListEntry>> onSuccess,
        Action<string> onError)
    {
        RemoteConfig config = RemoteConfig.Current;
        if (config == null)
        {
            onError?.Invoke("RemoteConfig is not loaded.");
            yield break;
        }

        string url = BuildListUrl(config.SasEndpointBaseUrl);
        if (string.IsNullOrEmpty(url))
        {
            onError?.Invoke("Anchor endpoint is not configured.");
            yield break;
        }

        if (string.IsNullOrEmpty(config.SasApiKey))
        {
            onError?.Invoke("API key is not configured.");
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("X-API-Key", config.SasApiKey);
            yield return request.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                onError?.Invoke("List anchors failed (" + request.responseCode + "): " + request.error);
                yield break;
            }

            List<AnchorListEntry> parsed;
            if (!TryParseAnchorListResponse(request.downloadHandler.text, out parsed))
            {
                onError?.Invoke("List anchors response was invalid JSON.");
                yield break;
            }

            onSuccess?.Invoke(parsed);
        }
    }

    public static IEnumerator CreateAnchor(
        string name,
        string identifier,
        DateTime dateExpired,
        Action<AnchorBackendRecord> onSuccess,
        Action<string> onError)
    {
        RemoteConfig config = RemoteConfig.Current;
        if (config == null)
        {
            onError?.Invoke("RemoteConfig is not loaded.");
            yield break;
        }

        string url = BuildCreateUrl(config.SasEndpointBaseUrl);
        if (string.IsNullOrEmpty(url))
        {
            onError?.Invoke("Anchor endpoint is not configured.");
            yield break;
        }

        if (string.IsNullOrEmpty(config.SasApiKey))
        {
            onError?.Invoke("API key is not configured.");
            yield break;
        }

        string body = JsonUtility.ToJson(new CreateRequest
        {
            name = name,
            identifier = identifier,
            dateExpired = dateExpired.ToUniversalTime().ToString("o")
        });
        byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

        using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-API-Key", config.SasApiKey);

            yield return request.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                onError?.Invoke("Create anchor failed (" + request.responseCode + "): " + request.error);
                yield break;
            }

            AnchorBackendRecord parsed;
            if (!TryParseAnchorResponse(request.downloadHandler.text, out parsed))
            {
                onError?.Invoke("Create anchor response was invalid JSON.");
                yield break;
            }

            onSuccess?.Invoke(parsed);
        }
    }

    public static IEnumerator GetAnchor(
        string anchorId,
        Action<AnchorBackendRecord> onSuccess,
        Action<string> onError)
    {
        RemoteConfig config = RemoteConfig.Current;
        if (config == null)
        {
            onError?.Invoke("RemoteConfig is not loaded.");
            yield break;
        }

        string url = BuildGetUrl(config.SasEndpointBaseUrl, anchorId);
        if (string.IsNullOrEmpty(url))
        {
            onError?.Invoke("Anchor endpoint or id is not configured.");
            yield break;
        }

        if (string.IsNullOrEmpty(config.SasApiKey))
        {
            onError?.Invoke("API key is not configured.");
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("X-API-Key", config.SasApiKey);
            yield return request.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                onError?.Invoke("Get anchor failed (" + request.responseCode + "): " + request.error);
                yield break;
            }

            AnchorBackendRecord parsed;
            if (!TryParseAnchorResponse(request.downloadHandler.text, out parsed))
            {
                onError?.Invoke("Get anchor response was invalid JSON.");
                yield break;
            }

            onSuccess?.Invoke(parsed);
        }
    }

    private static string AppendApiPath(string endpointBaseUrl, string route)
    {
        if (string.IsNullOrWhiteSpace(endpointBaseUrl))
        {
            return string.Empty;
        }

        string trimmed = endpointBaseUrl.Trim().TrimEnd('/');
        if (trimmed.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed + "/" + route;
        }

        return trimmed + "/api/" + route;
    }
}
