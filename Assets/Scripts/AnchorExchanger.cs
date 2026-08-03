// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;

public class AnchorExchanger
{
//#if !UNITY_EDITOR
		private static readonly HttpClient SharedHttpClient = new HttpClient();

		private string baseAddress = "";

		private List<string> anchorkeys = new List<string>();
		private CancellationTokenSource watchCancellation;

		public List<string> AnchorKeys
		{
			get
			{
				lock (anchorkeys)
				{
					return new List<string>(anchorkeys);
				}
			}
		}

		public void WatchKeys(string exchangerUrl)
		{
			baseAddress = exchangerUrl;
			watchCancellation?.Cancel();
			watchCancellation?.Dispose();
			watchCancellation = new CancellationTokenSource();
			_ = WatchKeysAsync(watchCancellation.Token);
		}

		public void StopWatchingKeys()
		{
			watchCancellation?.Cancel();
			watchCancellation?.Dispose();
			watchCancellation = null;
		}

		private async Task WatchKeysAsync(CancellationToken cancellationToken)
		{
			string previousKey = string.Empty;
			while (!cancellationToken.IsCancellationRequested)
			{
				string currentKey = await RetrieveLastAnchorKey();
				if (!string.IsNullOrWhiteSpace(currentKey) && currentKey != previousKey)
				{
					Debug.Log("Found key " + currentKey);
					lock (anchorkeys)
					{
						anchorkeys.Add(currentKey);
					}
					previousKey = currentKey;
				}
				await Task.Delay(500, cancellationToken);
			}
		}

		public async Task<string> RetrieveAnchorKey(long anchorNumber)
		{
			try
			{
				return await SharedHttpClient.GetStringAsync(baseAddress + "/" + anchorNumber.ToString());
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				Debug.LogError($"Failed to retrieve anchor key for anchor number: {anchorNumber}.");
				return null;
			}
		}

		public async Task<string> RetrieveLastAnchorKey()
		{
			try
			{
				return await SharedHttpClient.GetStringAsync(baseAddress + "/last");
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				Debug.LogError("Failed to retrieve last anchor key.");
				return null;
			}
		}

		internal async Task<long> StoreAnchorKey(string anchorKey)
		{
			if (string.IsNullOrWhiteSpace(anchorKey))
			{
				return -1;
			}

			try
			{
				var response = await SharedHttpClient.PostAsync(baseAddress, new StringContent(anchorKey));
				if (response.IsSuccessStatusCode)
				{
					string responseBody = await response.Content.ReadAsStringAsync();
					long ret;
					if (long.TryParse(responseBody, out ret))
					{
						Debug.Log("Key " + ret.ToString());
						return ret;
					}
					else
					{
						Debug.LogError($"Failed to store the anchor key. Failed to parse the response body to a long: {responseBody}.");
					}
				}
				else
				{
					Debug.LogError($"Failed to store the anchor key: {response.StatusCode} {response.ReasonPhrase}.");
				}

				Debug.LogError($"Failed to store the anchor key: {anchorKey}.");
				return -1;
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				Debug.LogError($"Failed to store the anchor key: {anchorKey}.");
				return -1;
			}
		}
//#endif
}