using System;
using System.IO;
using LostRelic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostRelic.EditorTools
{
#pragma warning disable 0649
    [InitializeOnLoad]
    public static class DemoSceneBuilder
    {
        private const string SpawnConfigPath = "Assets/_Project/Data/spawn_config.json";

        [Serializable]
        private class SpawnPoint
        {
            public string prefab;
            public float[] position;
            public float rotationY;
            public string id;
            public string name;
            public string displayName;
            public string prompt;
            public float alertRadius;
            public float patrolRadius;
            public float chaseRadius;
            public float patrolSpeed;
            public float chaseSpeed;
            public float idleMin;
            public float idleMax;
            public float attackDistance;
            public float maxHp;
            public float hp;
            public float attack;
            public float defense;
            public float attackRange;
            public float attackInterval;
        }

        [Serializable]
        private class SpawnConfig
        {
            public string rootName;
            public SpawnPoint player;
            public SpawnPoint npc;
            public SpawnPoint[] owls;
            public SpawnPoint[] enemies;
        }

        static DemoSceneBuilder()
        {
            EditorApplication.delayCall += AutoBuildIfNeeded;
        }

        private static void AutoBuildIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (SceneManager.GetActiveScene().name != "SampleScene" ||
                GameObject.Find("__LostRelicDemo") != null)
            {
                return;
            }

            Build();
        }

        [MenuItem("LostRelic/Build Demo Scene")]
        public static void Build()
        {
            AddressableSetup.EnsureReady();

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            var config = JsonUtility.FromJson<SpawnConfig>(File.ReadAllText(SpawnConfigPath));
            if (config == null || config.player == null)
            {
                Debug.LogError("[LostRelic] Failed to parse spawn config.");
                return;
            }

            var oldRoot = GameObject.Find(config.rootName);
            if (oldRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(oldRoot);
            }

            var root = new GameObject(config.rootName);

            var player = SpawnActor(config.player, "Player", root.transform);
            if (player != null)
            {
                ComponentFactory.AddCharacterController(player, 1.7f, 0.35f, new Vector3(0f, 0.85f, 0f));
            }

            var npc = SpawnActor(config.npc, "NPC_OldGuide", root.transform);
            if (npc != null)
            {
                ComponentFactory.AddCapsuleCollider(npc, 1.8f, 0.4f, new Vector3(0f, 0.9f, 0f));
                Interactable.Attach(
                    npc,
                    config.npc.id,
                    "npc",
                    config.npc.displayName,
                    config.npc.prompt,
                    3f);
            }

            if (config.owls != null)
            {
                for (var i = 0; i < config.owls.Length; i++)
                {
                    var owl = SpawnActor(config.owls[i], "Owl_" + (i + 1), root.transform);
                    if (owl != null)
                    {
                        Interactable.Attach(
                            owl,
                            config.owls[i].id,
                            "item",
                            config.owls[i].name,
                            "E 拾取",
                            2.6f);
                    }
                }
            }

            if (config.enemies != null)
            {
                for (var i = 0; i < config.enemies.Length; i++)
                {
                    var enemy = SpawnActor(config.enemies[i], "遗迹守卫_" + (i + 1), root.transform);
                    if (enemy != null)
                    {
                        EnemyAlertZone.Attach(
                            enemy,
                            config.enemies[i].id,
                            config.enemies[i].alertRadius,
                            config.enemies[i].patrolRadius,
                            config.enemies[i].chaseRadius,
                            config.enemies[i].patrolSpeed,
                            config.enemies[i].chaseSpeed,
                            config.enemies[i].idleMin,
                            config.enemies[i].idleMax,
                            config.enemies[i].attackDistance,
                            config.enemies[i].maxHp,
                            config.enemies[i].hp,
                            config.enemies[i].attack,
                            config.enemies[i].defense,
                            config.enemies[i].attackRange,
                            config.enemies[i].attackInterval);
                    }
                }
            }

            var bootGo = GameObject.Find("__LostRelicBootstrap");
            if (bootGo == null)
            {
                bootGo = new GameObject("__LostRelicBootstrap", typeof(GameBootstrap));
            }

            var boot = bootGo.GetComponent<GameBootstrap>();
            if (boot == null)
            {
                boot = bootGo.AddComponent<GameBootstrap>();
            }

            boot.luaEntry = "main";
            boot.logEvents = true;

            var cameraGo = GameObject.Find("Main Camera");
            if (cameraGo != null && cameraGo.GetComponent<AudioListener>() == null)
            {
                cameraGo.AddComponent<AudioListener>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            NavMeshBakeSetup.BakeActiveScene();
            PlayerUIBuilder.BuildPlayerUI();
            EnemyHpBarSetup.SetupEnemyHpBars();

            Debug.Log("[LostRelic] Demo scene built with embedded player/NPC/owls/enemies.");
        }

        private static GameObject SpawnActor(SpawnPoint data, string objectName, Transform parent)
        {
            if (data == null || string.IsNullOrEmpty(data.prefab))
            {
                return null;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(data.prefab);
            if (prefab == null)
            {
                Debug.LogWarning("[LostRelic] Missing scene prefab: " + data.prefab);
                return null;
            }

            var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (go == null)
            {
                go = UnityEngine.Object.Instantiate(prefab);
            }

            go.name = objectName;
            go.transform.SetParent(parent, true);

            var pos = data.position;
            if (pos != null && pos.Length >= 3)
            {
                go.transform.position = new Vector3(pos[0], pos[1], pos[2]);
            }
            go.transform.rotation = Quaternion.Euler(0f, data.rotationY, 0f);
            return go;
        }
    }
}
#pragma warning restore 0649
