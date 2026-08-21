using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostRelic.EditorTools
{
    public static class NavMeshBakeSetup
    {
        [MenuItem("LostRelic/Bake NavMesh")]
        public static void BakeActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.name))
            {
                Debug.LogWarning("[LostRelic] No active scene to bake.");
                return;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                SetNavigationStaticRecursive(root);
            }

            NavMeshBuilder.BuildNavMesh();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[LostRelic] NavMesh baked for " + scene.name);
        }

        private static void SetNavigationStaticRecursive(GameObject go)
        {
            if (IsDynamicRoot(go))
            {
                return;
            }

            var flags = GameObjectUtility.GetStaticEditorFlags(go);
            GameObjectUtility.SetStaticEditorFlags(
                go,
                flags | StaticEditorFlags.NavigationStatic);

            foreach (Transform child in go.transform)
            {
                SetNavigationStaticRecursive(child.gameObject);
            }
        }

        private static bool IsDynamicRoot(GameObject go)
        {
            var name = go.name;
            return name.StartsWith("__LostRelic") ||
                name == "Player" ||
                name.StartsWith("NPC_") ||
                name.StartsWith("Owl_") ||
                name.StartsWith("\u9057\u8FF9\u5B88\u536B_") ||
                name == "Main Camera" ||
                name == "Directional Light";
        }
    }
}
