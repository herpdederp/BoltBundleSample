using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class SceneLoader : Bolt.GlobalEventListener
{
    //Set of all possible bundle types on the storage server
    [SerializeField] private string[] bundleNames;
    
    //Static links to storage servers
    private string _baseUriString = "https://vrh-test.s3.amazonaws.com/vrh-assetbundles";
    private string _baseFileString = string.Empty;

    string _myPlatform = string.Empty;

    private void Awake()
    {
        
#if UNITY_EDITOR
    _myPlatform = "windows";
#endif

#if UNITY_ANDROID
    _myPlatform = "android";
#endif

#if UNITY_STANDALONE_WIN
    _myPlatform = "windows";
#endif
    }

    public void LoadWorld()
    {
        DownloadWorld();
    }

    private void DownloadWorld()
    {
        print("Downloading...");
        StartCoroutine(DownloadAndCacheBundles());
    }

    private IEnumerator DownloadAndCacheBundles()
    {
        foreach (string blobName in bundleNames)
        {
            string uri = _baseUriString + "/" + _myPlatform + "/" + staticData.myAdditiveWorld + "/" + blobName;

            // Wait for the Caching system to be ready
            while (!Caching.ready)
            {
                yield return null;
            }

            // get current bundle hash from server, random value added to avoid caching
            UnityWebRequest www = UnityWebRequest.Get(uri + ".manifest?r=" + (Random.value * 9999999));
            Debug.Log("Loading manifest:" + uri + ".manifest");

            // wait for load to finish
            yield return www.Send();
            
            if (www.isNetworkError == true)
            {
                Debug.LogError("www error: " + www.error);
                www.Dispose();
                www = null;
                yield break;
            }

            // create empty hash string
            Hash128 hashString = (default(Hash128));// new Hash128(0, 0, 0, 0);

            // check if received data contains 'ManifestFileVersion'
            if (www.downloadHandler.text.Contains("ManifestFileVersion"))
            {
                var hashRow = www.downloadHandler.text.ToString().Split("\n".ToCharArray())[5];
                hashString = Hash128.Parse(hashRow.Split(':')[1].Trim());

                if (hashString.isValid == true)
                {
                    // we can check if there is cached version or not
                    if (Caching.IsVersionCached(uri, hashString) == true)
                    {
                        Debug.Log("Bundle with this hash is already cached!");
                    }
                    else
                    {
                        Debug.Log("No cached version founded for this hash..");
                    }
                }
                else
                {
                    // invalid loaded hash, just try loading latest bundle
                    Debug.LogError("Invalid hash:" + hashString);
                    yield break;
                }

            }
            else
            {
                Debug.LogError("Cannot find AssetBundle");
            }

            // now download the actual bundle, with hashString parameter it uses cached version if available
            www = UnityWebRequestAssetBundle.GetAssetBundle(uri + "?r=" + (Random.value * 9999999), hashString, 0);

            print("Starting retrieval...");

            // wait for load to finish
            yield return www.Send();

            if (www.error != null)
            {
                print(www.error);
                www.Dispose();
                www = null;
            }

            if (www != null)
            {
                // get bundle from downloadhandler
                AssetBundle bundle = ((DownloadHandlerAssetBundle)www.downloadHandler).assetBundle;
                print("Found bundle");

                if (bundle.isStreamedSceneAssetBundle)
                {
                    string[] scenePaths = bundle.GetAllScenePaths();
                    string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePaths[0]);
                    print("Loading Scene");
                    SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
                }
                else if (bundle.name.Contains("config"))
                {
                    print(bundle.GetAllAssetNames());
                    foreach (string configAsset in bundle.GetAllAssetNames())
                    {
                        if (configAsset.Contains("lighting"))
                        {
                            LightModificationFile lightConfig = bundle.LoadAsset(configAsset) as LightModificationFile;
                            RenderSettings.ambientSkyColor = lightConfig.skyColor;
                            RenderSettings.ambientEquatorColor = lightConfig.equatorColor;
                            RenderSettings.ambientGroundColor = lightConfig.groundColor;
                            RenderSettings.skybox = lightConfig.skyboxMat;
                        }
                    }
                } else
                {
                    bundle.LoadAllAssets();
                }

                www.Dispose();
                www = null;

                // try to cleanup memory
                bundle = null;
            }
        }
    }
}