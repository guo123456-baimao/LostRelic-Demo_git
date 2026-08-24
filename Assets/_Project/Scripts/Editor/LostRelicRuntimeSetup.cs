using System;
using System.IO;
using LostRelic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LostRelic.EditorTools
{
    public static class LostRelicRuntimeSetup
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string SpawnConfigPath = "Assets/_Project/Data/spawn_config.json";
        private const string FontPath = "Assets/Project-Assets/Text.ttf";
        private const string PanelSpritePath = "Assets/Project-Assets/UI/UI_White_Transperent.png";
        private const string DialogSpritePath = "Assets/Project-Assets/UI/UI_White_Blue.png";
        private const string DialogSpriteName = "artdecoUI_PIPO_wb_1";

        [Serializable]
        private class SpawnPoint
        {
            public string id;
            public string name;
            public string displayName;
            public string prompt;
            public float[] position;
            public float rotationY;
        }

        [Serializable]
        private class SpawnConfig
        {
            public string rootName;
            public SpawnPoint npc;
            public SpawnPoint[] owls;
        }

        [MenuItem("LostRelic/Setup Runtime UI & Components")]
        public static void Setup()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "SampleScene")
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var canvasGo = GameObject.Find("LostRelicCanvas");
            if (canvasGo == null)
            {
                Debug.LogError("[LostRelic] LostRelicCanvas not found. Setup aborted.");
                return;
            }

            var root = canvasGo.transform;
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelSpritePath);
            var dialogSprite = LoadSubSprite(DialogSpritePath, DialogSpriteName);

            EnsurePromptDialogEnd(root, font, dialogSprite);
            EnsureAttrExtras(root, font);
            EnsureButtons();
            EnsureActors();
            EnsureEnemyPrefabs();

            var cameraGo = GameObject.Find("Main Camera");
            if (cameraGo != null && cameraGo.GetComponent<AudioListener>() == null)
            {
                cameraGo.AddComponent<AudioListener>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LostRelic] Runtime UI/components setup complete. Existing layout untouched.");
        }

        private static void EnsurePromptDialogEnd(
            Transform root,
            Font font,
            Sprite dialogSprite)
        {
            EnsureText(
                "InteractPrompt", root, "", 28,
                new Color(1f, 0.9f, 0.55f, 1f), font,
                TextAnchor.LowerCenter,
                new Vector2(0f, 64f), new Vector2(1000f, 64f));

            var dialog = EnsurePanel(
                "DialogPanel", root, 1500f, 250f,
                new Color(0.09f, 0.11f, 0.14f, 0.96f),
                new Vector2(0f, -380f));

            EnsureImage(
                "DialogBg", dialog, dialogSprite, Color.white,
                Vector2.zero, new Vector2(1480f, 232f));

            var accent = EnsurePanel(
                "DialogAccent", dialog, 1420f, 5f,
                new Color(0.92f, 0.68f, 0.30f, 1f),
                new Vector2(0f, 116f));

            EnsureText(
                "DialogSpeaker", dialog, "", 24,
                new Color(0.92f, 0.78f, 0.42f, 1f), font,
                TextAnchor.UpperLeft,
                new Vector2(-460f, 92f), new Vector2(520f, 34f));
            EnsureText(
                "DialogBody", dialog, "", 27,
                new Color(0.92f, 0.93f, 0.95f, 1f), font,
                TextAnchor.UpperLeft,
                new Vector2(-40f, -30f), new Vector2(1340f, 140f));

            var end = EnsurePanel(
                "EndPanel", root, 1200f, 520f,
                new Color(0.03f, 0.04f, 0.07f, 0.96f),
                Vector2.zero);
            EnsureText(
                "EndText", end, "", 34,
                new Color(1f, 0.88f, 0.55f, 1f), font,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 40f), new Vector2(1000f, 280f));

            var restart = EnsurePanel(
                "RestartButton", end, 240f, 64f,
                new Color(0.3f, 0.55f, 0.42f, 1f),
                new Vector2(0f, -190f));
            EnsureButton(restart.gameObject);
            EnsureText(
                "RestartText", restart, "\u91CD\u65B0\u5F00\u59CB", 28,
                Color.white, font, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(240f, 64f));
        }

        private static void EnsureAttrExtras(Transform root, Font font)
        {
            var attr = GameObject.Find("PlayerAttrPanel");
            if (attr != null)
            {
                var card1 = attr.transform.Find("AttrCard1");
                if (card1 != null)
                {
                    var hpBg = EnsurePanel(
                        "AttrHpBarBg", card1, 220f, 16f,
                        new Color(0.05f, 0.06f, 0.08f, 1f),
                        new Vector2(50f, -56f));
                    var hpFill = EnsurePanel(
                        "AttrHpBarFill", hpBg, 220f, 12f,
                        new Color(0.74f, 0.20f, 0.16f, 1f),
                        Vector2.zero);
                }
            }

            var questTab = EnsurePanel(
                "AttrTabQuest", root, 150f, 60f,
                new Color(0.16f, 0.22f, 0.28f, 1f),
                new Vector2(-560f, -30f));
            EnsureText(
                "AttrTabQuestText", questTab, "\u4EFB\u52A1\u9762\u677F", 20,
                Color.white, font, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(150f, 60f));
        }

        private static void EnsureButtons()
        {
            var names = new[]
            {
                "CloseAttrPanel", "CloseInventory", "CloseQuestPanel",
                "AttrTabInventory", "AttrTabActive", "AttrTabQuest",
                "InventoryTabActive", "InventoryTabAttr", "InventoryTabQuest",
                "QuestTabInventory", "QuestTabAttr", "QuestTabActive"
            };

            foreach (var name in names)
            {
                var go = GameObject.Find(name);
                if (go != null)
                {
                    EnsureButton(go);
                }
            }
        }

        private static void EnsureActors()
        {
            if (!File.Exists(SpawnConfigPath))
            {
                return;
            }

            var config = JsonUtility.FromJson<SpawnConfig>(
                File.ReadAllText(SpawnConfigPath));
            if (config == null)
            {
                return;
            }

            var npc = GameObject.Find("NPC_OldGuide");
            if (npc != null && config.npc != null)
            {
                if (npc.GetComponent<Animator>() == null)
                {
                    npc.AddComponent<Animator>();
                }
                ComponentFactory.AddCapsuleCollider(
                    npc, 1.8f, 0.4f, new Vector3(0f, 0.9f, 0f));
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
                    var owl = GameObject.Find("Owl_" + (i + 1));
                    if (owl == null || config.owls[i] == null)
                    {
                        continue;
                    }
                    Interactable.Attach(
                        owl,
                        config.owls[i].id,
                        "item",
                        config.owls[i].name,
                        "E \u62FE\u53D6",
                        2.6f);
                }
            }
        }

        private static void EnsureEnemyPrefabs()
        {
            EnsureEnemyPrefab(
                "Assets/Project-Assets/Enemies/Prefabs/PBRDefault/SlimePBR.prefab",
                "enemy_slime_1");
            EnsureEnemyPrefab(
                "Assets/Project-Assets/Enemies/Prefabs/PBRDefault/TurtleShellPBR.prefab",
                "enemy_turtle_1");
        }

        private static void EnsureEnemyPrefab(string path, string enemyId)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning("[LostRelic] Missing enemy prefab: " + path);
                return;
            }

            var root = prefab.transform;
            var controller = root.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = root.gameObject.AddComponent<CharacterController>();
            }
            controller.height = 1.2f;
            controller.radius = 0.6f;
            controller.center = new Vector3(0f, 0.6f, 0f);
            controller.slopeLimit = 60f;
            controller.stepOffset = 0.6f;
            controller.skinWidth = 0.05f;
            controller.minMoveDistance = 0f;

            var agent = root.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = root.gameObject.AddComponent<NavMeshAgent>();
            }
            agent.radius = 0.6f;
            agent.height = 1.2f;
            agent.speed = 1.8f;
            agent.angularSpeed = 360f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.5f;
            agent.autoBraking = true;
            agent.updateRotation = true;
            agent.updatePosition = true;

            // enemy_ctrl.lua reads every number off this component at spawn, and
            // the Inspector is what supplies any field spawn_config.json leaves
            // out, so this tool must not stamp defaults over what is authored on
            // the prefab. It only guarantees the component exists and that its
            // plumbing -- id and root -- is wired.
            var zone = root.GetComponent<EnemyAlertZone>();
            if (zone == null)
            {
                zone = root.gameObject.AddComponent<RelicGuard>();
            }
            zone.enemyId = enemyId;
            zone.enemyRoot = root;

            PrefabUtility.SavePrefabAsset(prefab);
        }

        private static RectTransform EnsurePanel(
            string name,
            Transform parent,
            float width,
            float height,
            Color color,
            Vector2 anchoredPosition)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                var rect = existing as RectTransform;
                if (rect != null && rect.GetComponent<Image>() == null)
                {
                    var image = rect.gameObject.AddComponent<Image>();
                    image.color = color;
                }
                return rect;
            }

            var created = UIManager.CreatePanel(name, parent, width, height, color);
            created.anchoredPosition = anchoredPosition;
            return created;
        }

        private static Text EnsureText(
            string name,
            Transform parent,
            string content,
            int fontSize,
            Color color,
            Font font,
            TextAnchor alignment,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing.GetComponent<Text>();
            }

            return UIManager.CreateText(
                name,
                parent,
                content,
                fontSize,
                color,
                font,
                alignment,
                anchoredPosition,
                sizeDelta);
        }

        private static Image EnsureImage(
            string name,
            Transform parent,
            Sprite sprite,
            Color color,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                var image = existing.GetComponent<Image>();
                if (image == null)
                {
                    image = existing.gameObject.AddComponent<Image>();
                }
                image.sprite = sprite;
                image.color = color;
                return image;
            }

            return UIManager.CreateImage(
                name,
                parent,
                sprite,
                color,
                anchoredPosition,
                sizeDelta);
        }

        private static void EnsureButton(GameObject go)
        {
            if (go != null && go.GetComponent<Button>() == null)
            {
                go.AddComponent<Button>();
            }
        }

        private static Sprite LoadSubSprite(string path, string name)
        {
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var sprite = obj as Sprite;
                if (sprite != null && sprite.name == name)
                {
                    return sprite;
                }
            }
            return null;
        }
    }
}
