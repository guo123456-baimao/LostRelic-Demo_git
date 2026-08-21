using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LostRelic.EditorTools
{
    [InitializeOnLoad]
    public static class EnemyAnimatorSetup
    {
        private const string EnemyControllerPath =
            "Assets/Assets/Enemies/Animators/Slime.controller";

        static EnemyAnimatorSetup()
        {
            EditorApplication.delayCall += EnsureEnemyAnimator;
        }

        [MenuItem("LostRelic/Setup Enemy Animator")]
        public static void EnsureEnemyAnimator()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(EnemyControllerPath);
            if (controller == null)
            {
                return;
            }

            foreach (var layer in controller.layers)
            {
                foreach (var child in layer.stateMachine.states)
                {
                    var state = child.state;
                    RemoveTransitions(state);

                    var clip = state.motion as AnimationClip;
                    if (clip != null)
                    {
                        var settings = AnimationUtility.GetAnimationClipSettings(clip);
                        var shouldLoop = state.name != "Die" &&
                            state.name != "Attack01" &&
                            state.name != "Attack02" &&
                            state.name != "GetHit";
                        if (settings.loopTime != shouldLoop)
                        {
                            settings.loopTime = shouldLoop;
                            AnimationUtility.SetAnimationClipSettings(clip, settings);
                            EditorUtility.SetDirty(clip);
                        }
                    }
                }
            }

            var idle = FindState(controller, "IdleNormal");
            if (idle != null)
            {
                controller.layers[0].stateMachine.defaultState = idle;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[LostRelic] Enemy animator FSM states ready.");
        }

        private static void RemoveTransitions(AnimatorState state)
        {
            var transitions = state.transitions;
            for (var i = transitions.Length - 1; i >= 0; i--)
            {
                state.RemoveTransition(transitions[i]);
            }
        }

        private static AnimatorState FindState(
            AnimatorController controller,
            string name)
        {
            foreach (var layer in controller.layers)
            {
                foreach (var child in layer.stateMachine.states)
                {
                    if (child.state.name == name)
                    {
                        return child.state;
                    }
                }
            }
            return null;
        }
    }
}
