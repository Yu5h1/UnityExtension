// Test-player probe: loads the existing remote character through the package loader and reports to the browser.
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Yu5h1Lib.AddressableAssets;

public class WebGLRemoteProbe : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void RemoteProbeReport(string message);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void StartProbe()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer && Application.absoluteURL.Contains("remote-test=1"))
            new GameObject(nameof(WebGLRemoteProbe)).AddComponent<WebGLRemoteProbe>();
    }

    private async void Start()
    {
        try
        {
            var page = new Uri(Application.absoluteURL);
            var origin = page.Query.Contains("same-origin=1") ? page.GetLeftPart(UriPartial.Authority) : "http://127.0.0.1:8002";
            Addressables.InternalIdTransformFunc = location => location.InternalId.StartsWith("http://127.0.0.1:8002/")
                ? origin + new Uri(location.InternalId).PathAndQuery : location.InternalId;
            Report("loading " + origin);
            var loader = gameObject.AddComponent<AddressablesLoader>();
            var prefab = await loader.LoadAsync<GameObject>("characters/test-character-remote");
            if (prefab == null)
                throw new Exception("remote character was null");
            var instance = Instantiate(prefab);
            Report("PASS " + origin + " " + instance.name);
            Destroy(instance);
            loader.Release(prefab);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            Report("FAIL " + error);
        }
    }

    private static void Report(string message)
    {
        Debug.Log("[WebGLRemoteProbe] " + message);
        RemoteProbeReport(message);
    }
}
