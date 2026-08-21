using LostRelic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LostRelic.EditorTools
{
    public static class PlayerUIBuilder
    {
        [MenuItem("LostRelic/Build Player UI in Scene")]
        public static void BuildPlayerUI()
        {
            var root = UIManager.Root;
            root.localScale = Vector3.one;
            var font = LoadFont();
            var frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Assets/UI/UI_White_Blue.png");
            var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Assets/UI/UI_White_Transperent.png");

            if (GameObject.Find("PlayerHpBar") == null)
            {
                BuildHpBar(root, font);
            }

            if (GameObject.Find("PlayerAttrPanel") == null)
            {
                BuildAttrPanel(root, font, frameSprite, panelSprite);
            }

            if (GameObject.Find("InventoryPanel") == null)
            {
                BuildInventoryPanel(root, font, panelSprite);
            }

            if (GameObject.Find("QuestPanel") == null)
            {
                BuildQuestPanel(root, font, panelSprite);
            }

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LostRelic] Player UI built in scene.");
        }

        private static Font LoadFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Assets/Text.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            return font;
        }

        private static void BuildHpBar(RectTransform root, Font font)
        {
            var bar = UIManager.CreatePanel(
                "PlayerHpBar", root, 360f, 26f,
                new Color(0.08f, 0.10f, 0.13f, 0.92f));
            bar.anchoredPosition = new Vector2(-680f, 390f);

            var fill = UIManager.CreatePanel(
                "PlayerHpFill", bar, 360f, 20f,
                new Color(0.74f, 0.20f, 0.16f, 1f));
            fill.anchoredPosition = Vector2.zero;

            UIManager.CreateText(
                "PlayerHpText", bar, "", 16, Color.white, font,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(360f, 22f));
        }

        private static void BuildAttrPanel(
            RectTransform root,
            Font font,
            Sprite frameSprite,
            Sprite panelSprite)
        {
            var panel = UIManager.CreatePanel(
                "PlayerAttrPanel", root, 860f, 680f,
                new Color(0.08f, 0.10f, 0.13f, 0.97f));
            panel.anchoredPosition = Vector2.zero;

            UIManager.CreateImage(
                "AttrPanelBg", panel, frameSprite, Color.white,
                Vector2.zero, new Vector2(840f, 660f));

            var header = UIManager.CreatePanel(
                "AttrHeader", panel, 820f, 72f,
                new Color(0.14f, 0.17f, 0.22f, 1f));
            header.anchoredPosition = new Vector2(0f, 284f);
            var headerAccent = UIManager.CreatePanel(
                "AttrHeaderAccent", header, 6f, 44f,
                new Color(0.92f, 0.68f, 0.30f, 1f));
            headerAccent.anchoredPosition = new Vector2(-400f, 0f);

            UIManager.CreateText(
                "PlayerAttrTitle", header, "玩家属性", 30,
                new Color(0.96f, 0.84f, 0.55f, 1f), font, TextAnchor.MiddleLeft,
                new Vector2(-250f, 0f), new Vector2(260f, 44f));
            UIManager.CreateText(
                "PlayerAttrSubtitle", header, "基础能力", 18,
                new Color(0.7f, 0.74f, 0.8f, 1f), font, TextAnchor.MiddleRight,
                new Vector2(300f, 0f), new Vector2(180f, 30f));

            var cards = new[]
            {
                new { key = "Hp", label = "生命", x = -185f, y = 150f },
                new { key = "Attack", label = "攻击", x = 185f, y = 150f },
                new { key = "Defense", label = "防御", x = -185f, y = -10f },
                new { key = "Speed", label = "移速", x = 185f, y = -10f }
            };

            for (var i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                var index = i + 1;
                var cardPanel = UIManager.CreatePanel(
                    "AttrCard" + index, panel, 360f, 130f,
                    new Color(0.12f, 0.15f, 0.19f, 0.96f));
                cardPanel.anchoredPosition = new Vector2(card.x, card.y);

                UIManager.CreateImage(
                    "AttrCardBg" + index, cardPanel, panelSprite,
                    new Color(1f, 1f, 1f, 0.12f), Vector2.zero,
                    new Vector2(360f, 130f));

                var accent = UIManager.CreatePanel(
                    "AttrCardAccent" + index, cardPanel, 6f, 84f,
                    new Color(0.92f, 0.68f, 0.30f, 1f));
                accent.anchoredPosition = new Vector2(-170f, 0f);

                UIManager.CreateText(
                    "AttrCardLabel" + index, cardPanel, card.label, 20,
                    new Color(0.82f, 0.85f, 0.9f, 1f), font, TextAnchor.MiddleLeft,
                    new Vector2(-120f, 32f), new Vector2(200f, 30f));
                UIManager.CreateText(
                    "AttrCardValue" + card.key, cardPanel, "", 34,
                    new Color(0.95f, 0.82f, 0.5f, 1f), font, TextAnchor.MiddleLeft,
                    new Vector2(-120f, -24f), new Vector2(220f, 44f));

                if (card.key == "Hp")
                {
                    var hpBarBg = UIManager.CreatePanel(
                        "AttrHpBarBg", cardPanel, 220f, 16f,
                        new Color(0.05f, 0.06f, 0.08f, 1f));
                    hpBarBg.anchoredPosition = new Vector2(50f, -56f);
                    var hpBarFill = UIManager.CreatePanel(
                        "AttrHpBarFill", hpBarBg, 220f, 12f,
                        new Color(0.74f, 0.20f, 0.16f, 1f));
                    hpBarFill.anchoredPosition = Vector2.zero;
                }
            }

            var close = UIManager.CreatePanel(
                "CloseAttrPanel", panel, 130f, 48f,
                new Color(0.45f, 0.22f, 0.18f, 1f));
            close.anchoredPosition = new Vector2(350f, -300f);
            UIManager.CreateText(
                "CloseAttrText", close, "关闭", 24, Color.white, font,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(130f, 48f));

            var invTab = UIManager.CreatePanel(
                "AttrTabInventory", root, 150f, 60f,
                new Color(0.16f, 0.22f, 0.28f, 1f));
            invTab.anchoredPosition = new Vector2(-560f, 150f);
            UIManager.CreateText(
                "AttrTabInventoryText", invTab, "背包面板", 20, Color.white, font,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150f, 60f));

            var attrTab = UIManager.CreatePanel(
                "AttrTabActive", root, 150f, 60f,
                new Color(0.32f, 0.44f, 0.52f, 1f));
            attrTab.anchoredPosition = new Vector2(-560f, 60f);
            UIManager.CreateText(
                "AttrTabText", attrTab, "属性面板", 20, Color.white, font,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150f, 60f));

            var questTab = UIManager.CreatePanel(
                "AttrTabQuest", root, 150f, 60f,
                new Color(0.16f, 0.22f, 0.28f, 1f));
            questTab.anchoredPosition = new Vector2(-560f, -30f);
            UIManager.CreateText(
                "AttrTabQuestText", questTab, "任务面板", 20, Color.white, font,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150f, 60f));
        }

        private static void BuildInventoryPanel(
            RectTransform root,
            Font font,
            Sprite panelSprite)
        {
            var panel = UIManager.CreatePanel(
                "InventoryPanel", root, 860f, 680f,
                new Color(0.06f, 0.07f, 0.1f, 0.96f));
            panel.anchoredPosition = Vector2.zero;

            UIManager.CreateImage(
                "InventoryBg", panel, panelSprite,
                new Color(1f, 1f, 1f, 0.06f), Vector2.zero,
                new Vector2(840f, 660f));
            UIManager.CreateText(
                "InventoryTitle", panel, "背包", 32,
                new Color(0.95f, 0.82f, 0.5f, 1f), font,
                TextAnchor.MiddleCenter, new Vector2(0f, 310f),
                new Vector2(300f, 50f));

            var grid = UIManager.CreatePanel(
                "InventoryGrid", panel, 800f, 420f,
                new Color(0f, 0f, 0f, 0f));
            grid.anchoredPosition = new Vector2(30f, 60f);

            UIManager.CreateText(
                "DetailName", panel, "", 26, Color.white, font,
                TextAnchor.UpperLeft, new Vector2(-350f, -170f),
                new Vector2(500f, 40f));
            UIManager.CreateText(
                "DetailDesc", panel, "", 22,
                new Color(0.82f, 0.84f, 0.88f, 1f), font,
                TextAnchor.UpperLeft, new Vector2(-350f, -220f),
                new Vector2(700f, 90f));
            UIManager.CreateText(
                "DetailCount", panel, "", 22,
                new Color(0.85f, 0.9f, 0.95f, 1f), font,
                TextAnchor.UpperLeft, new Vector2(180f, -170f),
                new Vector2(300f, 40f));

            var close = UIManager.CreatePanel(
                "CloseInventory", panel, 130f, 48f,
                new Color(0.45f, 0.22f, 0.18f, 1f));
            close.anchoredPosition = new Vector2(350f, -300f);
            UIManager.CreateText(
                "CloseText", close, "关闭", 24, Color.white, font,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(130f, 48f));

            var invTab = UIManager.CreatePanel(
                "InventoryTabActive", root, 150f, 60f,
                new Color(0.32f, 0.44f, 0.52f, 1f));
            invTab.anchoredPosition = new Vector2(-560f, 150f);
            UIManager.CreateText(
                "InventoryTabText", invTab, "背包面板", 20, Color.white, font,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150f, 60f));

            var attrTab = UIManager.CreatePanel(
                "InventoryTabAttr", root, 150f, 60f,
                new Color(0.16f, 0.22f, 0.28f, 1f));
            attrTab.anchoredPosition = new Vector2(-560f, 60f);
            UIManager.CreateText(
                "InventoryTabAttrText", attrTab, "属性面板", 20, Color.white, font,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150f, 60f));

            var questTab = UIManager.CreatePanel(
                "InventoryTabQuest", root, 150f, 60f,
                new Color(0.16f, 0.22f, 0.28f, 1f));
            questTab.anchoredPosition = new Vector2(-560f, -30f);
            UIManager.CreateText(
                "InventoryTabQuestText", questTab, "任务面板", 20, Color.white, font,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150f, 60f));
        }

        private static void BuildQuestPanel(
            RectTransform root,
            Font font,
            Sprite panelSprite)
        {
            var panel = UIManager.CreatePanel(
                "QuestPanel", root, 860f, 680f,
                new Color(0.08f, 0.10f, 0.13f, 0.97f));
            panel.anchoredPosition = Vector2.zero;

            UIManager.CreateImage(
                "QuestPanelBg", panel, panelSprite,
                new Color(1f, 1f, 1f, 0.06f), Vector2.zero,
                new Vector2(840f, 660f));

            var header = UIManager.CreatePanel(
                "QuestHeader", panel, 820f, 72f,
                new Color(0.14f, 0.17f, 0.22f, 1f));
            header.anchoredPosition = new Vector2(0f, 284f);
            var accent = UIManager.CreatePanel(
                "QuestHeaderAccent", header, 6f, 36f,
                new Color(0.92f, 0.68f, 0.30f, 1f));
            accent.anchoredPosition = new Vector2(-400f, 0f);
            UIManager.CreateText(
                "QuestTitle", header, "任务", 28,
                new Color(0.96f, 0.84f, 0.55f, 1f), font,
                TextAnchor.MiddleLeft, new Vector2(-250f, 0f),
                new Vector2(200f, 40f));

            UIManager.CreateText(
                "QuestName", panel, "", 26,
                new Color(0.95f, 0.82f, 0.5f, 1f), font,
                TextAnchor.MiddleLeft, new Vector2(-180f, 200f),
                new Vector2(520f, 40f));
            UIManager.CreateText(
                "QuestDesc", panel, "", 20,
                new Color(0.88f, 0.89f, 0.92f, 1f), font,
                TextAnchor.UpperLeft, new Vector2(-180f, 120f),
                new Vector2(520f, 70f));

            var progressY = new[] { 20f, -40f, -100f, -160f };
            for (var i = 0; i < progressY.Length; i++)
            {
                UIManager.CreateText(
                    "QuestProgress" + (i + 1), panel, "", 20,
                    new Color(0.92f, 0.93f, 0.95f, 1f), font,
                    TextAnchor.MiddleLeft, new Vector2(-180f, progressY[i]),
                    new Vector2(520f, 36f));
            }

            UIManager.CreateText(
                "QuestTotal", panel, "", 22,
                new Color(0.92f, 0.78f, 0.42f, 1f), font,
                TextAnchor.MiddleLeft, new Vector2(-180f, -250f),
                new Vector2(400f, 36f));

            var close = UIManager.CreatePanel(
                "CloseQuestPanel", panel, 130f, 48f,
                new Color(0.45f, 0.22f, 0.18f, 1f));
            close.anchoredPosition = new Vector2(350f, -300f);
            UIManager.CreateText(
                "CloseQuestText", close, "关闭", 22, Color.white, font,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(130f, 48f));

            var invTab = UIManager.CreatePanel(
                "QuestTabInventory", root, 150f, 60f,
                new Color(0.16f, 0.22f, 0.28f, 1f));
            invTab.anchoredPosition = new Vector2(-560f, 150f);
            UIManager.CreateText(
                "QuestTabInventoryText", invTab, "背包面板", 20, Color.white, font,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150f, 60f));

            var attrTab = UIManager.CreatePanel(
                "QuestTabAttr", root, 150f, 60f,
                new Color(0.16f, 0.22f, 0.28f, 1f));
            attrTab.anchoredPosition = new Vector2(-560f, 60f);
            UIManager.CreateText(
                "QuestTabAttrText", attrTab, "属性面板", 20, Color.white, font,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150f, 60f));

            var questTab = UIManager.CreatePanel(
                "QuestTabActive", root, 150f, 60f,
                new Color(0.32f, 0.44f, 0.52f, 1f));
            questTab.anchoredPosition = new Vector2(-560f, -30f);
            UIManager.CreateText(
                "QuestTabActiveText", questTab, "任务面板", 20, Color.white, font,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150f, 60f));
        }
    }
}
