using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LostRelic.EditorTools
{
    [InitializeOnLoad]
    public static class NpcAnimatorSetup
    {
        private const string NpcControllerPath =
            "Assets/Assets/Old Guide/Animator/MageIdle.controller";
        private const string NpcAnimationFbxPath =
            "Assets/Assets/Old Guide/Old Guide/Animations/fbx/Rig_Medium/Rig_Medium_General.fbx";

        static NpcAnimatorSetup()
        {
            EditorApplication.delayCall += EnsureNpcAnimator;
        }

        [MenuItem("LostRelic/Setup NPC Animator")]
        public static void EnsureNpcAnimator()
        {
            EnsureFolder("Assets/Assets", "Old Guide");
            EnsureFolder("Assets/Assets/Old Guide", "Animator");

            var clips = AssetDatabase.LoadAllAssetsAtPath(NpcAnimationFbxPath)
                .OfType<AnimationClip>()
                .ToArray();
            var idleClip = clips.FirstOrDefault(c => c.name == "Idle_A");
            if (idleClip == null)
            {
                idleClip = clips.FirstOrDefault(c => c.name.Contains("Idle"));
            }

            if (idleClip == null)
            {
                Debug.LogWarning("[LostRelic] NPC Idle clip not found in " + NpcAnimationFbxPath);
                return;
            }

            var clipSettings = AnimationUtility.GetAnimationClipSettings(idleClip);
            if (!clipSettings.loopTime)
            {
                clipSettings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(idleClip, clipSettings);
                EditorUtility.SetDirty(idleClip);
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(NpcControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(NpcControllerPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            var idleState = stateMachine.states
                .FirstOrDefault(s => s.state.name == "Idle")
                .state;
            if (idleState == null)
            {
                idleState = stateMachine.AddState("Idle");
            }

            idleState.motion = idleClip;
            stateMachine.defaultState = idleState;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AddressableSetup.EnsureReady();

            Debug.Log("[LostRelic] Mage Idle animator ready: " + NpcControllerPath);
        }

        private static void EnsureFolder(string parent, string name)
        {
            var path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
