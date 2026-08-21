using UnityEngine;

namespace LostRelic
{
    [DisallowMultipleComponent]
    public class Interactable : MonoBehaviour
    {
        public string id;
        public string type = "item";
        public string displayName;
        public string prompt = "E 交互";
        public float radius = 2.5f;
        public bool disabled;

        public static Interactable Attach(
            GameObject target,
            string id,
            string type,
            string displayName,
            string prompt,
            float radius)
        {
            var component = target.GetComponent<Interactable>();
            if (component == null)
            {
                component = target.AddComponent<Interactable>();
            }

            component.id = id;
            component.type = type;
            component.displayName = displayName;
            component.prompt = prompt;
            component.radius = radius;
            component.disabled = false;
            return component;
        }
    }
}
