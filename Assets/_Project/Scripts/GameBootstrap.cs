using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace LostRelic
{
    public class GameBootstrap : MonoBehaviour
    {
        public string luaEntry = "main";
        public bool logEvents = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (!Application.isPlaying ||
                SceneManager.GetActiveScene().name != "SampleScene" ||
                UnityEngine.Object.FindObjectOfType<GameBootstrap>() != null)
            {
                return;
            }

            var go = new GameObject("__LostRelicBootstrap");
            go.AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            EventCenter.LogEnabled = logEvents;
            AudioService.Instance.name = "LostRelicAudio";
            RemoveDuplicateEventSystems();
        }

        private static void RemoveDuplicateEventSystems()
        {
            var systems = UnityEngine.Object.FindObjectsOfType<EventSystem>();
            if (systems.Length <= 1)
            {
                return;
            }

            var keep = EventSystem.current;
            if (keep == null)
            {
                keep = systems[0];
            }

            foreach (var system in systems)
            {
                if (system != keep)
                {
                    UnityEngine.Object.Destroy(system.gameObject);
                }
            }

            Debug.Log("[UI] Removed duplicate EventSystem, kept " + keep.name);
        }

        private void Start()
        {
            LogSceneUiState();
            XLuaManager.Initialize(luaEntry);
        }

        private static void LogSceneUiState()
        {
            var names = new[]
            {
                "PlayerHpBar", "PlayerAttrPanel", "InventoryPanel", "QuestPanel",
                "AttrTabInventory", "AttrTabActive", "AttrTabQuest",
                "InventoryTabActive", "InventoryTabAttr", "InventoryTabQuest",
                "QuestTabInventory", "QuestTabAttr", "QuestTabActive"
            };
            var all = UnityEngine.Object.FindObjectsOfType<GameObject>(true);
            foreach (var name in names)
            {
                var count = 0;
                foreach (var go in all)
                {
                    if (go.name == name)
                    {
                        count++;
                    }
                }
                Debug.Log(
                    "[UI] C# scan " + name +
                    " count=" + count +
                    " find=" + (GameObject.Find(name) != null));
            }

            foreach (var go in all)
            {
                if (go.name.Contains("Panel") ||
                    go.name.Contains("Tab") ||
                    go.name == "PlayerHpBar")
                {
                    Debug.Log(
                        "[UI] obj " + go.name +
                        " activeSelf=" + go.activeSelf +
                        " activeInHierarchy=" + go.activeInHierarchy +
                        " scene=" + go.scene.name +
                        " parent=" + (go.transform.parent != null
                            ? go.transform.parent.name
                            : "null"));
                }
            }
        }

        private void Update()
        {
            XLuaManager.Tick();
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                XLuaManager.Dispose();
                DataService.ClearCache();
                EventCenter.Clear();
            }
        }
    }
}
