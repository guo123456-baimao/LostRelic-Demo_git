using System;
using UnityEditor;
using UnityEngine;

namespace LostRelic.EditorTools
{
    [InitializeOnLoad]
    public static class PluginCompatFix
    {
        static PluginCompatFix()
        {
            EditorApplication.delayCall += FixXluaEditorCompatibility;
        }

        [MenuItem("LostRelic/Fix xLua Plugin Compatibility")]
        public static void FixXluaEditorCompatibility()
        {
            var changed = false;
            var guids = AssetDatabase.FindAssets("xlua.dll");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as PluginImporter;
                if (importer == null)
                {
                    continue;
                }

                var normalized = path.Replace('\\', '/');
                var compatibleWithEditor = normalized.EndsWith(
                    "/x86_64/xlua.dll",
                    StringComparison.OrdinalIgnoreCase);
                importer.SetCompatibleWithEditor(compatibleWithEditor);
                changed = true;
            }

            if (changed)
            {
                AssetDatabase.SaveAssets();
                Debug.Log("[LostRelic] xLua plugin editor compatibility fixed.");
            }
        }
    }
}
