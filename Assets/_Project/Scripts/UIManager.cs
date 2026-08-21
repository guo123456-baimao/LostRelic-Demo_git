using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LostRelic
{
    public static class UIManager
    {
        private static Canvas _canvas;

        public static Canvas Canvas
        {
            get
            {
                if (_canvas != null &&
                    (_canvas.name != "LostRelicCanvas" ||
                     _canvas.renderMode != RenderMode.ScreenSpaceOverlay))
                {
                    _canvas = null;
                }

                if (_canvas == null)
                {
                    Canvas existing = null;
                    var canvases = Object.FindObjectsOfType<Canvas>();
                    for (var i = 0; i < canvases.Length; i++)
                    {
                        var candidate = canvases[i];
                        if (candidate.name == "LostRelicCanvas" &&
                            candidate.renderMode == RenderMode.ScreenSpaceOverlay)
                        {
                            existing = candidate;
                            break;
                        }
                    }

                    if (existing == null)
                    {
                        for (var i = 0; i < canvases.Length; i++)
                        {
                            var candidate = canvases[i];
                            if (candidate.renderMode == RenderMode.ScreenSpaceOverlay)
                            {
                                existing = candidate;
                                break;
                            }
                        }
                    }

                    if (existing != null)
                    {
                        _canvas = existing;
                        _canvas.name = "LostRelicCanvas";
                        Debug.Log("[UI] Reusing scene canvas " + existing.name);
                    }
                    else
                    {
                        var go = new GameObject("LostRelicCanvas",
                            typeof(Canvas),
                            typeof(CanvasScaler),
                            typeof(GraphicRaycaster));
                        _canvas = go.GetComponent<Canvas>();
                        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        _canvas.sortingOrder = 100;

                        var scaler = go.GetComponent<CanvasScaler>();
                        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                        scaler.referenceResolution = new Vector2(1920f, 1080f);
                        scaler.matchWidthOrHeight = 0.5f;
                        Debug.Log("[UI] Created runtime canvas");
                    }
                }

                if (EventSystem.current == null &&
                    Object.FindObjectOfType<EventSystem>() == null)
                {
                    new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                }

                _canvas.transform.localScale = Vector3.one;
                return _canvas;
            }
        }

        public static RectTransform Root
        {
            get { return (RectTransform)Canvas.transform; }
        }

        public static RectTransform CreatePanel(string name, Transform parent, float width, float height, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = color;
            return rect;
        }

        public static Text CreateText(
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
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;

            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.font = font;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Image CreateImage(
            string name,
            Transform parent,
            Sprite sprite,
            Color color,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }
    }
}
