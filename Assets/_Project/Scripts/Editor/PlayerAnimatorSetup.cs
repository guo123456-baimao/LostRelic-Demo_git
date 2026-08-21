using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LostRelic.EditorTools
{
    [InitializeOnLoad]
    public static class PlayerAnimatorSetup
    {
        private const string PlayerControllerPath =
            "Assets/Assets/Player/Animator/DogControl.controller";

        static PlayerAnimatorSetup()
        {
            EditorApplication.delayCall += EnsurePlayerAnimator;
        }

        [MenuItem("LostRelic/Setup Player Animator")]
        public static void EnsurePlayerAnimator()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath);
            if (controller == null)
            {
                return;
            }

            var idle = FindState(controller, "Idle_Battle");
            var walk = FindState(controller, "WalkForwardBattle");
            var run = FindState(controller, "RunForwardBattle");
            if (idle == null || walk == null || run == null)
            {
                Debug.LogWarning("[LostRelic] Player animator states not found, skip setup.");
                return;
            }

            EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            var speed = FindParameter(controller, "Speed");
            if (speed == null)
            {
                return;
            }

            RemoveTransitions(idle);
            RemoveTransitions(walk);
            RemoveTransitions(run);

            AddTransition(idle, walk, speed, AnimatorConditionMode.Greater, 0.1f);
            AddTransition(walk, idle, speed, AnimatorConditionMode.Less, 0.1f);
            AddTransition(walk, run, speed, AnimatorConditionMode.Greater, 5f);
            var runToWalk = AddTransition(run, walk, speed, AnimatorConditionMode.Less, 5f);
            runToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, speed.name);
            AddTransition(run, idle, speed, AnimatorConditionMode.Less, 0.1f);

            var attack01 = FindState(controller, "Attack01");
            var attack02 = FindState(controller, "Attack02");
            if (attack01 != null && attack02 != null)
            {
                EnsureParameter(controller, "Attack01", AnimatorControllerParameterType.Trigger);
                EnsureParameter(controller, "Attack02", AnimatorControllerParameterType.Trigger);

                RemoveTransitions(attack01);
                RemoveTransitions(attack02);

                AddTriggerTransition(idle, attack01, "Attack01");
                AddTriggerTransition(walk, attack01, "Attack01");
                AddTriggerTransition(run, attack01, "Attack01");
                AddTriggerTransition(idle, attack02, "Attack02");
                AddTriggerTransition(walk, attack02, "Attack02");
                AddTriggerTransition(run, attack02, "Attack02");

                AddExitTransition(attack01, idle);
                AddExitTransition(attack02, idle);
            }
            else
            {
                Debug.LogWarning("[LostRelic] Player attack states not found, skip attack setup.");
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[LostRelic] DogControl.controller movement and attack transitions ready.");
        }

        private static bool HasParameter(AnimatorController controller, string name)
        {
            foreach (var parameter in controller.parameters)
            {
                if (parameter.name == name)
                {
                    return true;
                }
            }
            return false;
        }

        private static void EnsureParameter(
            AnimatorController controller,
            string name,
            AnimatorControllerParameterType type)
        {
            if (!HasParameter(controller, name))
            {
                controller.AddParameter(name, type);
            }
        }

        private static AnimatorControllerParameter FindParameter(
            AnimatorController controller,
            string name)
        {
            foreach (var parameter in controller.parameters)
            {
                if (parameter.name == name)
                {
                    return parameter;
                }
            }
            return null;
        }

        private static AnimatorState FindState(AnimatorController controller, string name)
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

        private static void RemoveTransitions(AnimatorState state)
        {
            var transitions = state.transitions;
            for (var i = transitions.Length - 1; i >= 0; i--)
            {
                state.RemoveTransition(transitions[i]);
            }
        }

        private static AnimatorStateTransition AddTransition(
            AnimatorState from,
            AnimatorState to,
            AnimatorControllerParameter parameter,
            AnimatorConditionMode mode,
            float threshold)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.15f;
            transition.AddCondition(mode, threshold, parameter.name);
            return transition;
        }

        private static AnimatorStateTransition AddTriggerTransition(
            AnimatorState from,
            AnimatorState to,
            string parameterName)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.15f;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameterName);
            return transition;
        }

        private static AnimatorStateTransition AddExitTransition(
            AnimatorState from,
            AnimatorState to)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = 0.85f;
            transition.duration = 0.15f;
            return transition;
        }
    }
}
