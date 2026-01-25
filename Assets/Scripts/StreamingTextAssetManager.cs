using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

using System.Collections.Generic;

public class StreamingTextAssetManager
{
    static StreamingTextAssetManager instance = new();
    public static StreamingTextAssetManager Instance => instance;

    public List<UnityWebRequest> busyUnityWebRequests = new();

    public IEnumerator FetchText(string path, Action<string> callback)
    {
        var request = UnityWebRequest.Get(path);

        busyUnityWebRequests.Add(request);

        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"Success: {path}");
            callback(request.downloadHandler.text);

            busyUnityWebRequests.Remove(request);
        }
        else
        {
            Debug.LogError($"failed to fetch and setup: {path}");
        }
    }
}
