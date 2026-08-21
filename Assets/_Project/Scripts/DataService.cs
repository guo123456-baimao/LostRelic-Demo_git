using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LostRelic
{
    public static class DataService
    {
        private const string ConfigFolder = "Assets/_Project/Data/";
        private static readonly Dictionary<string, string> Cache =
            new Dictionary<string, string>();

        public static string LoadJson(string fileName)
        {
            var address = ConfigFolder + fileName;
            if (Cache.TryGetValue(address, out var cached))
            {
                return cached;
            }

            var text = string.Empty;

            // In the editor, prefer the source JSON so Play mode always sees
            // local config changes even if Addressables bundles are stale.
            if (Application.isEditor)
            {
                var filePath = Path.Combine(
                    Application.dataPath,
                    "_Project",
                    "Data",
                    fileName);
                if (File.Exists(filePath))
                {
                    text = File.ReadAllText(filePath);
                }
            }

            if (string.IsNullOrEmpty(text))
            {
                text = ResService.LoadText(address);
            }

            if (text != null)
            {
                Cache[address] = text;
            }

            return text;
        }

        public static void ClearCache()
        {
            Cache.Clear();
        }
    }
}
