using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace LostRelic.EditorTools
{
    public static class AddressableSetup
    {
        private const string SettingsFolder = "Assets/AddressableAssetsData";
        private const string SettingsName = "AddressableAssetSettings";

        private static readonly string[] UserAssetPaths =
        {
            "Assets/Project-Assets/Player/Prefab/Palyer_prefab.prefab",
            "Assets/Project-Assets/Old Guide/Old Guide/Characters/fbx/Mage.fbx",
            "Assets/Project-Assets/Old Guide/Animator/MageIdle.controller",
            "Assets/Project-Assets/Quest Item/Quest Item/Prefabs/Quest Item_1.prefab",
            "Assets/Project-Assets/Quest Item/Quest Item/Prefabs/Quest Item_2.prefab",
            "Assets/Project-Assets/Quest Item/Quest Item/Prefabs/Quest Item_3.prefab",
            "Assets/Project-Assets/Quest Item/Quest Item/Prefabs/Quest Item_4.prefab",
            "Assets/Project-Assets/Enemies/Prefabs/PBRDefault/SlimePBR.prefab",
            "Assets/Project-Assets/Enemies/Prefabs/PBRDefault/TurtleShellPBR.prefab",
            "Assets/Project-Assets/Icons/#2 - Transparent Icons & Drop Shadow.png",
            "Assets/Project-Assets/Text.ttf",
            "Assets/Project-Assets/Audio/BGM/Background Music.wav",
            "Assets/Project-Assets/Audio/BGM/Enemy Spawned.wav",
            "Assets/Project-Assets/Audio/Sound Effects/sfx_spell 1.mp3",
            "Assets/Project-Assets/Audio/Sound Effects/sfx_attack2.mp3",
            "Assets/Project-Assets/Audio/Sound Effects/sfx_attack3.mp3",
            "Assets/Project-Assets/Audio/Sound Effects/Dagger_stab_Quick__#4-1766764076280.mp3",
            "Assets/Project-Assets/Audio/BGM/Sounds/click-b.ogg"
        };

        [InitializeOnLoadMethod]
        private static void AutoEnsure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var settingsPath = SettingsFolder + "/" + SettingsName + ".asset";
            if (!File.Exists(settingsPath))
            {
                EnsureReady();
            }
        }

        [MenuItem("LostRelic/Setup Addressables")]
        public static void EnsureReady()
        {
            if (!AssetDatabase.IsValidFolder(SettingsFolder))
            {
                AssetDatabase.CreateFolder("Assets", "AddressableAssetsData");
            }

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                settings = AddressableAssetSettings.Create(SettingsFolder, SettingsName, true, true);
                AddressableAssetSettingsDefaultObject.Settings = settings;
            }

            var luaGroup = EnsureGroup(settings, "ProjectLua");
            var dataGroup = EnsureGroup(settings, "ProjectData");
            var userGroup = EnsureGroup(settings, "UserAssets");

            MarkFolder(settings, luaGroup, "Assets/_Project/Lua", "*.lua.txt", "lua");
            MarkFolder(settings, dataGroup, "Assets/_Project/Data", "*.json", "data");

            PrepareSpriteFolder("Assets/Project-Assets/UI");
            PrepareSpriteFolder("Assets/Project-Assets/Icons");

            MarkFolder(settings, userGroup, "Assets/Project-Assets/UI", "*.png", "ui");
            MarkFolder(settings, userGroup, "Assets/Project-Assets/Icons", "*.png", "ui");

            foreach (var path in UserAssetPaths)
            {
                MarkPath(settings, userGroup, path, "user");
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LostRelic] Addressables groups ready: ProjectLua, ProjectData, UserAssets");
        }

        private static AddressableAssetGroup EnsureGroup(AddressableAssetSettings settings, string groupName)
        {
            var group = settings.groups.Find(g => g.Name == groupName);
            if (group == null)
            {
                group = settings.CreateGroup(groupName, false, false, true, null, typeof(BundledAssetGroupSchema));
            }

            return group;
        }

        private static void MarkFolder(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string folder,
            string pattern,
            string label)
        {
            if (!Directory.Exists(folder))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(folder, pattern, SearchOption.AllDirectories))
            {
                MarkPath(settings, group, file.Replace('\\', '/'), label);
            }
        }

        private static void MarkPath(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string path,
            string label)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Debug.LogWarning("[LostRelic] Missing addressable asset: " + path);
                return;
            }

            var guid = AssetDatabase.AssetPathToGUID(path);
            var entry = settings.CreateOrMoveEntry(guid, group, false, true);
            if (entry == null)
            {
                return;
            }

            entry.address = path;
            entry.SetLabel(label, true);
        }

        private static void PrepareSpriteFolder(string folder)
        {
            if (!Directory.Exists(folder))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories))
            {
                var path = file.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.textureType == TextureImporterType.Sprite)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
        }
    }
}
