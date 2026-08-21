using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LostRelic.EditorTools
{
    public static class EnemyHpBarSetup
    {
        private static readonly string[] PrefabPaths =
        {
            "Assets/Assets/Enemies/Prefabs/PBRDefault/SlimePBR.prefab",
            "Assets/Assets/Enemies/Prefabs/PolyartDefault/SlimePolyart.prefab"
        };

        [MenuItem("LostRelic/Setup Enemy HP Bar")]
        public static void SetupEnemyHpBars()
        {
            foreach (var path in PrefabPaths)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                if (root == null)
                {
                    continue;
                }

                Transform bar = root.transform.Find("EnemyHpBar");
                if (bar == null)
                {
                    bar = CreateHpBar(root.transform);
                }
                ConfigureHpBar(bar);

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[LostRelic] Enemy HP bars added to Slime prefabs.");
        }

        private static Transform CreateHpBar(Transform enemyRoot)
        {
            var barGo = new GameObject(
                "EnemyHpBar",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            barGo.transform.SetParent(enemyRoot, false);
            return barGo.transform;
        }

        private static void ConfigureHpBar(Transform bar)
        {
            var barRect = bar.GetComponent<RectTransform>();
            barRect.localPosition = new Vector3(0f, 1.2f, 0f);
            barRect.sizeDelta = new Vector2(1.1f, 0.12f);

            var canvas = bar.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            if (bar.GetComponent<GraphicRaycaster>() == null)
            {
                bar.gameObject.AddComponent<GraphicRaycaster>();
            }

            var bg = bar.Find("EnemyHpBg");
            if (bg == null)
            {
                var bgGo = new GameObject(
                    "EnemyHpBg",
                    typeof(RectTransform),
                    typeof(Image));
                bgGo.transform.SetParent(bar, false);
                bg = bgGo.transform;
            }
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.08f, 0.9f);

            var fill = bar.Find("EnemyHpFill");
            if (fill == null)
            {
                var fillGo = new GameObject(
                    "EnemyHpFill",
                    typeof(RectTransform),
                    typeof(Image));
                fillGo.transform.SetParent(bar, false);
                fill = fillGo.transform;
            }
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = new Vector2(1.1f, 0.10f);
            fill.GetComponent<Image>().color = new Color(0.74f, 0.20f, 0.16f, 1f);
        }
    }
}
