using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostRelic
{
    public static class EventCenter
    {
        private static readonly Dictionary<string, List<Delegate>> Handlers =
            new Dictionary<string, List<Delegate>>();

        public static bool LogEnabled { get; set; }

        public static void Subscribe(string eventName, Action<object[]> handler)
        {
            if (string.IsNullOrEmpty(eventName) || handler == null)
            {
                return;
            }

            if (!Handlers.TryGetValue(eventName, out var list))
            {
                list = new List<Delegate>();
                Handlers[eventName] = list;
            }

            list.Add(handler);
        }

        public static void Unsubscribe(string eventName, Action<object[]> handler)
        {
            if (!Handlers.TryGetValue(eventName, out var list))
            {
                return;
            }

            list.Remove(handler);
        }

        public static void Publish(string eventName, params object[] args)
        {
            if (LogEnabled)
            {
                Debug.Log($"[EventCenter] {eventName}");
            }

            if (!Handlers.TryGetValue(eventName, out var list))
            {
                return;
            }

            var snapshot = list.ToArray();
            foreach (var handler in snapshot)
            {
                ((Action<object[]>)handler).Invoke(args);
            }
        }

        public static void Clear()
        {
            Handlers.Clear();
        }
    }
}
