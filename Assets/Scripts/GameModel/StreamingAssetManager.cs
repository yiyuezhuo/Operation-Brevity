using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace YYZ.Unity
{
    public class CoroutineRunner : MonoBehaviour
    {
        static CoroutineRunner _instance;

        public static CoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[CoroutineRunner]");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<CoroutineRunner>();
                }
                return _instance;
            }
        }
    }

    public class StreamingAssetManager
    {
        static StreamingAssetManager instance = new();
        public static StreamingAssetManager Instance => instance;

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
            }
            else
            {
                Debug.LogError($"failed to fetch and setup: {path}");
            }

            busyUnityWebRequests.Remove(request);
        }

        public async Task<string> FetchTextAsync(string path)
        {
            string ret = null;

            var request = UnityWebRequest.Get(path);

            busyUnityWebRequests.Add(request);

            await request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Success: {path}");
                ret = request.downloadHandler.text;
            }
            else
            {
                Debug.LogError($"failed to fetch and setup: {path}");
            }

            busyUnityWebRequests.Remove(request);

            return ret;
        }

        public class ImageCache
        {
            public string path;
            public bool completed; // succ or failed
            public Texture2D texture;

            Sprite _sprite;
            public Sprite sprite
            {
                get
                {
                    if (texture == null)
                    {
                        return null;
                    }

                    if (_sprite == null)
                    {
                        _sprite = Sprite.Create(
                            texture,
                            new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f),
                            100.0f
                        );
                    }
                    return _sprite;
                }
            }
        }

        Dictionary<string, ImageCache> pathToImageCache = new();

        public IEnumerator FetchImageCache(ImageCache cache)
        {
            using (var webRequest = UnityWebRequestTexture.GetTexture(cache.path))
            {
                busyUnityWebRequests.Add(webRequest);

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    var texture = DownloadHandlerTexture.GetContent(webRequest);
                    cache.texture = texture;
                }
                else
                {
                    Debug.LogError($"failed to fetch and setup: {cache.path}");
                }

                cache.completed = true;
                busyUnityWebRequests.Remove(webRequest);
            }
        }

        public async void FetchImageCacheAsync(ImageCache cache)
        {
            using (var webRequest = UnityWebRequestTexture.GetTexture(cache.path))
            {
                busyUnityWebRequests.Add(webRequest);

                await webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    var texture = DownloadHandlerTexture.GetContent(webRequest);
                    cache.texture = texture;
                }
                else
                {
                    Debug.LogError($"failed to fetch and setup: {cache.path}");
                }

                cache.completed = true;
                busyUnityWebRequests.Remove(webRequest);
            }
        }

        public ImageCache GetImageCache(string path)
        {
            if(pathToImageCache.TryGetValue(path, out var cache))
                return cache;
            
            cache = pathToImageCache[path] = new(){path=path};
            // IOManager.Instance.StartCoroutine(FetchImageCache(cache)); // TODO: decouple with IOManager
            CoroutineRunner.Instance.StartCoroutine(FetchImageCache(cache));

            return cache;
        }

        public ImageCache GetImageCacheAsync(string path)
        {
            if(pathToImageCache.TryGetValue(path, out var cache))
                return cache;
            
            cache = pathToImageCache[path] = new(){path=path};
            // IOManager.Instance.StartCoroutine(FetchImageCache(cache)); // TODO: decouple with IOManager
            // CoroutineRunner.Instance.StartCoroutine(FetchImageCache(cache));
            FetchImageCacheAsync(cache);

            return cache;
        }

        public Texture2D GetTexture2D(string path) => GetImageCache(path).texture;
        public Sprite GetSprite(string path) => GetImageCache(path).sprite;
        public Texture2D GetTexture2DAsync(string path) => GetImageCacheAsync(path).texture;
        public Sprite GetSpriteAsync(string path) => GetImageCacheAsync(path).sprite;
    }

    public class StreamingAssetManagerEnumHelper<TEnum> where TEnum : Enum
    {
        static StreamingAssetManagerEnumHelper<TEnum> instance = new();
        public static StreamingAssetManagerEnumHelper<TEnum> Instance => instance;
        public string rootPath = Application.streamingAssetsPath + "/Pictures/" + typeof(TEnum).Name + "/";
        public string ext = ".png";
        Dictionary<TEnum, StreamingAssetManager.ImageCache> enumToImageCache = new();
        public string GetPath(TEnum value) => rootPath + value + ext;

        public StreamingAssetManager.ImageCache GetImageCache(TEnum value)
        {
            if(!enumToImageCache.TryGetValue(value, out var cache))
            {
                cache = StreamingAssetManager.Instance.GetImageCache(GetPath(value));
            }
            return cache;
        }

        public StreamingAssetManager.ImageCache GetImageCacheAsync(TEnum value)
        {
            if(!enumToImageCache.TryGetValue(value, out var cache))
            {
                cache = StreamingAssetManager.Instance.GetImageCacheAsync(GetPath(value));
            }
            return cache;
        }

        public Texture2D GetTexture2D(TEnum value) => GetImageCache(value).texture;
        public Sprite GetSprite(TEnum value) => GetImageCache(value).sprite;
        public Texture2D GetTexture2DAsync(TEnum value) => GetImageCacheAsync(value).texture;
        public Sprite GetSpriteAsync(TEnum value) => GetImageCacheAsync(value).sprite;
    }
}