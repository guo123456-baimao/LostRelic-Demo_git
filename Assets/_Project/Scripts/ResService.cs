using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LostRelic
{
    public static class ResService
    {
        public static T LoadAsset<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("[ResService] Empty address.");
                return null;
            }

            var handle = Addressables.LoadAssetAsync<T>(address);
            var result = handle.WaitForCompletion();
            if (result == null)
            {
                Debug.LogError($"[ResService] Failed to load <{typeof(T).Name}> at {address}");
            }

            return result;
        }

        public static UnityEngine.Object LoadAsset(Type type, string address)
        {
            if (type == null || string.IsNullOrEmpty(address))
            {
                Debug.LogError("[ResService] Invalid load request.");
                return null;
            }

            var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(address);
            var result = handle.WaitForCompletion();
            if (result == null)
            {
                Debug.LogError($"[ResService] Failed to load <{type.Name}> at {address}");
            }

            return result;
        }

        public static string LoadText(string address)
        {
            var asset = LoadAsset<TextAsset>(address);
            return asset != null ? asset.text : null;
        }

        public static Font LoadFont(string address)
        {
            return LoadAsset<Font>(address);
        }

        public static Sprite LoadSprite(string address)
        {
            return LoadAsset<Sprite>(address);
        }

        public static AudioClip LoadAudioClip(string address)
        {
            return LoadAsset<AudioClip>(address);
        }

        public static GameObject LoadPrefab(string address)
        {
            return LoadAsset<GameObject>(address);
        }

        public static RuntimeAnimatorController LoadAnimatorController(string address)
        {
            return LoadAsset<RuntimeAnimatorController>(address);
        }
    }
}
