// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class AnchorExchanger
{
//#if !UNITY_EDITOR
		private static readonly HttpClient Http = new HttpClient();

		private string baseAddress = "";
		private CancellationTokenSource watchCancellation;

		private List<string> anchorkeys = new List<string>();

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

		public void StopWatching()
		{
			watchCancellation?.Cancel();
			watchCancellation?.Dispose();
			watchCancellation = null;
		}

		public void WatchKeys(string exchangerUrl)
		{
			StopWatching();
			baseAddress = exchangerUrl;
			watchCancellation = new CancellationTokenSource();
			CancellationToken token = watchCancellation.Token;

			_ = Task.Run(async () =>
			{
				string previousKey = string.Empty;
				while (!token.IsCancellationRequested)
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

					try
					{
						await Task.Delay(500, token);
					}
					catch (OperationCanceledException)
					{
						break;
					}
				}
			}, token);
		}

		public async Task<string> RetrieveAnchorKey(long anchorNumber)
		{
			try
			{
				return await Http.GetStringAsync(baseAddress + "/" + anchorNumber.ToString());
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
				return await Http.GetStringAsync(baseAddress + "/last");
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
				var response = await Http.PostAsync(baseAddress, new StringContent(anchorKey));
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
