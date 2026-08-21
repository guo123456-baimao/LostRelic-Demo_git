using UnityEngine;

namespace LostRelic
{
    public static class InputService
    {
        public static float GetAxisRaw(string axis)
        {
            return Input.GetAxisRaw(axis);
        }

        public static float GetAxis(string axis)
        {
            return Input.GetAxis(axis);
        }

        public static bool GetKeyDown(KeyCode key)
        {
            return Input.GetKeyDown(key);
        }

        public static bool GetKey(KeyCode key)
        {
            return Input.GetKey(key);
        }

        public static bool GetMouseButtonDown(int button)
        {
            return Input.GetMouseButtonDown(button);
        }

        public static bool GetButtonDown(string name)
        {
            return Input.GetButtonDown(name);
        }

        public static Vector2 GetMousePosition()
        {
            return Input.mousePosition;
        }
    }
}
