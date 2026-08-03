using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Requests short-lived SAS URLs for allow-listed bundles in the private
/// <c>restricted</c> container (#24). Endpoint + API key come from
/// <see cref="RemoteConfig"/> (#25) — never hardcoded in source.
/// </summary>
public static class RestrictedBundleSasClient
{
    [Serializable]
    private class SasRequest
    {
        public string bundle;
    }

    [Serializable]
    private class SasResponse
    {
        public string url;
        public int ttlMinutes;
    }

    /// <summary>
    /// Builds the POST URL for the Azure Function SAS endpoint.
    /// Accepts a base like <c>https://geoxplorer-sas.azurewebsites.net</c> or
    /// <c>https://geoxplorer-sas.azurewebsites.net/api</c>.
    /// </summary>
    public static string BuildRequestUrl(string endpointBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(endpointBaseUrl))
        {
            return string.Empty;
        }

        string trimmed = endpointBaseUrl.Trim().TrimEnd('/');
        if (trimmed.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed + "/sas/restricted";
        }

        return trimmed + "/api/sas/restricted";
    }

    /// <summary>
    /// Parses the Function JSON body <c>{"url":"...","ttlMinutes":15}</c>.
    /// </summary>
    public static bool TryParseResponse(string json, out string url, out int ttlMinutes)
    {
        url = null;
        ttlMinutes = 0;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        SasResponse parsed;
        try
        {
            parsed = JsonUtility.FromJson<SasResponse>(json);
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (parsed == null || string.IsNullOrWhiteSpace(parsed.url))
        {
            return false;
        }

        url = parsed.url;
        ttlMinutes = parsed.ttlMinutes;
        return true;
    }

    /// <summary>
    /// POSTs to the SAS Function and invokes <paramref name="onComplete"/> with the
    /// signed download URL or an error message. Requires
    /// <see cref="RemoteConfig.SasEndpointBaseUrl"/> and
    /// <see cref="RemoteConfig.SasApiKey"/> to be set on the active config.
    /// </summary>
    public static IEnumerator RequestSasUrl(
        string bundleName,
        Action<string, int> onSuccess,
        Action<string> onError)
    {
        RemoteConfig config = RemoteConfig.Current;
        if (config == null)
        {
            onError?.Invoke("RemoteConfig is not loaded.");
            yield break;
        }

        string requestUrl = BuildRequestUrl(config.SasEndpointBaseUrl);
        if (string.IsNullOrEmpty(requestUrl))
        {
            onError?.Invoke("SAS endpoint is not configured (sasEndpointBaseUrl).");
            yield break;
        }

        if (string.IsNullOrEmpty(config.SasApiKey))
        {
            onError?.Invoke("SAS API key is not configured (sasApiKey).");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(bundleName))
        {
            onError?.Invoke("Bundle name is required.");
            yield break;
        }

        string body = JsonUtility.ToJson(new SasRequest { bundle = bundleName });
        byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

        using (UnityWebRequest request = new UnityWebRequest(requestUrl, UnityWebRequest.kHttpVerbPOST))
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
                onError?.Invoke(
                    "SAS request failed (" + request.responseCode + "): " + request.error);
                yield break;
            }

            string responseJson = request.downloadHandler.text;
            string url;
            int ttl;
            if (!TryParseResponse(responseJson, out url, out ttl))
            {
                onError?.Invoke("SAS response was not valid JSON with a url field.");
                yield break;
            }

            onSuccess?.Invoke(url, ttl);
        }
    }
}
