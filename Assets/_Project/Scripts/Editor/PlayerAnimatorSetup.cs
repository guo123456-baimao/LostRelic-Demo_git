using System.Collections.Generic;
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
        private const float TransitionDuration = 0.15f;
        private const float AttackExitTime = 0.85f;
        private const float WalkThreshold = 0.1f;
        private const float RunThreshold = 5f;

        private class Cond
        {
            public AnimatorConditionMode Mode;
            public string Parameter;
            public float Threshold;
        }

        private class TransitionSpec
        {
            public AnimatorState From;
            public AnimatorState To;
            public bool HasExitTime;
            public float ExitTime;
            public Cond[] Conditions;
        }

        static PlayerAnimatorSetup()
        {
            EditorApplication.delayCall += EnsurePlayerAnimator;
        }

        public static void EnsurePlayerAnimator()
        {
            Run(false);
        }

        [MenuItem("LostRelic/Setup Player Animator")]
        public static void SetupFromMenu()
        {
            Run(true);
        }

        // Runs on every editor load via InitializeOnLoad, so it must not write
        // unless something is actually wrong. It used to unconditionally
        // RemoveTransitions() and re-add, and deleting plus re-adding mints fresh
        // fileIDs for every AnimatorStateTransition -- so the .controller came out
        // byte-different on every single load, showed up dirty in git forever, and
        // its diff read like edited conditions when nothing had changed. Now the
        // desired shape is built as data first and compared; the rebuild only
        // happens when the comparison fails.
        private static void Run(bool verbose)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath);
            if (controller == null)
            {
                if (verbose)
                {
                    Debug.LogWarning("[LostRelic] Player animator not found: " + PlayerControllerPath);
                }
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

            var attack01 = FindState(controller, "Attack01");
            var attack02 = FindState(controller, "Attack02");
            var withAttacks = attack01 != null && attack02 != null;
            if (!withAttacks)
            {
                Debug.LogWarning("[LostRelic] Player attack states not found, skip attack setup.");
            }

            var parametersAdded = EnsureParameter(
                controller, "Speed", AnimatorControllerParameterType.Float);
            if (withAttacks)
            {
                if (EnsureParameter(controller, "Attack01", AnimatorControllerParameterType.Trigger))
                {
                    parametersAdded = true;
                }
                if (EnsureParameter(controller, "Attack02", AnimatorControllerParameterType.Trigger))
                {
                    parametersAdded = true;
                }
            }

            var speed = FindParameter(controller, "Speed");
            if (speed == null)
            {
                return;
            }

            // Every state this tool clears transitions on, i.e. every state whose
            // transition list it owns outright. Defend/Die/Dizzy/GetHit and the
            // rest are authored in the asset and never touched.
            var owned = new List<AnimatorState> { idle, walk, run };
            if (withAttacks)
            {
                owned.Add(attack01);
                owned.Add(attack02);
            }

            var specs = BuildSpecs(idle, walk, run, attack01, attack02, withAttacks, speed.name);
            var transitionsOk = Matches(owned, specs);

            if (transitionsOk && !parametersAdded)
            {
                if (verbose)
                {
                    Debug.Log("[LostRelic] DogControl.controller already correct, nothing written.");
                }
                return;
            }

            if (!transitionsOk)
            {
                for (var i = 0; i < owned.Count; i++)
                {
                    RemoveTransitions(owned[i]);
                }
                for (var i = 0; i < specs.Count; i++)
                {
                    Apply(specs[i]);
                }
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log(transitionsOk
                ? "[LostRelic] DogControl.controller parameters added."
                : "[LostRelic] DogControl.controller movement and attack transitions rebuilt.");
        }

        // The order matters and is part of what gets compared: Unity evaluates a
        // state's transitions in list order, and the existing asset was produced by
        // this same sequence. Build it once, then either compare or apply it.
        private static List<TransitionSpec> BuildSpecs(
            AnimatorState idle,
            AnimatorState walk,
            AnimatorState run,
            AnimatorState attack01,
            AnimatorState attack02,
            bool withAttacks,
            string speedName)
        {
            var specs = new List<TransitionSpec>();

            specs.Add(Move(idle, walk, One(
                AnimatorConditionMode.Greater, speedName, WalkThreshold)));
            specs.Add(Move(walk, idle, One(
                AnimatorConditionMode.Less, speedName, WalkThreshold)));
            specs.Add(Move(walk, run, One(
                AnimatorConditionMode.Greater, speedName, RunThreshold)));
            specs.Add(Move(run, walk, new[]
            {
                Condition(AnimatorConditionMode.Less, speedName, RunThreshold),
                Condition(AnimatorConditionMode.Greater, speedName, WalkThreshold)
            }));
            specs.Add(Move(run, idle, One(
                AnimatorConditionMode.Less, speedName, WalkThreshold)));

            if (!withAttacks)
            {
                return specs;
            }

            specs.Add(Move(idle, attack01, One(AnimatorConditionMode.If, "Attack01", 0f)));
            specs.Add(Move(walk, attack01, One(AnimatorConditionMode.If, "Attack01", 0f)));
            specs.Add(Move(run, attack01, One(AnimatorConditionMode.If, "Attack01", 0f)));
            specs.Add(Move(idle, attack02, One(AnimatorConditionMode.If, "Attack02", 0f)));
            specs.Add(Move(walk, attack02, One(AnimatorConditionMode.If, "Attack02", 0f)));
            specs.Add(Move(run, attack02, One(AnimatorConditionMode.If, "Attack02", 0f)));

            specs.Add(Exit(attack01, idle));
            specs.Add(Exit(attack02, idle));
            return specs;
        }

        private static Cond Condition(
            AnimatorConditionMode mode,
            string parameter,
            float threshold)
        {
            return new Cond { Mode = mode, Parameter = parameter, Threshold = threshold };
        }

        private static Cond[] One(
            AnimatorConditionMode mode,
            string parameter,
            float threshold)
        {
            return new[] { Condition(mode, parameter, threshold) };
        }

        private static TransitionSpec Move(
            AnimatorState from,
            AnimatorState to,
            Cond[] conditions)
        {
            return new TransitionSpec
            {
                From = from,
                To = to,
                HasExitTime = false,
                Conditions = conditions
            };
        }

        private static TransitionSpec Exit(AnimatorState from, AnimatorState to)
        {
            return new TransitionSpec
            {
                From = from,
                To = to,
                HasExitTime = true,
                ExitTime = AttackExitTime,
                Conditions = new Cond[0]
            };
        }

        private static bool Matches(List<AnimatorState> owned, List<TransitionSpec> specs)
        {
            for (var i = 0; i < owned.Count; i++)
            {
                if (!StateMatches(owned[i], specs))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool StateMatches(AnimatorState state, List<TransitionSpec> specs)
        {
            var expected = new List<TransitionSpec>();
            for (var i = 0; i < specs.Count; i++)
            {
                if (specs[i].From == state)
                {
                    expected.Add(specs[i]);
                }
            }

            var existing = state.transitions;
            if (existing.Length != expected.Count)
            {
                return false;
            }

            for (var i = 0; i < existing.Length; i++)
            {
                if (!TransitionMatches(existing[i], expected[i]))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool TransitionMatches(AnimatorStateTransition actual, TransitionSpec want)
        {
            if (actual.destinationState != want.To ||
                actual.destinationStateMachine != null ||
                actual.isExit)
            {
                return false;
            }
            if (actual.hasExitTime != want.HasExitTime)
            {
                return false;
            }
            // exitTime only means anything when hasExitTime is set; on the movement
            // transitions it keeps whatever default Unity picked, so comparing it
            // there would fail for no reason.
            if (want.HasExitTime && !Mathf.Approximately(actual.exitTime, want.ExitTime))
            {
                return false;
            }
            if (!Mathf.Approximately(actual.duration, TransitionDuration))
            {
                return false;
            }

            var conditions = actual.conditions;
            if (conditions.Length != want.Conditions.Length)
            {
                return false;
            }
            for (var i = 0; i < conditions.Length; i++)
            {
                if (conditions[i].mode != want.Conditions[i].Mode ||
                    conditions[i].parameter != want.Conditions[i].Parameter ||
                    !Mathf.Approximately(conditions[i].threshold, want.Conditions[i].Threshold))
                {
                    return false;
                }
            }
            return true;
        }

        private static void Apply(TransitionSpec spec)
        {
            var transition = spec.From.AddTransition(spec.To);
            transition.hasExitTime = spec.HasExitTime;
            if (spec.HasExitTime)
            {
                transition.exitTime = spec.ExitTime;
            }
            transition.duration = TransitionDuration;
            for (var i = 0; i < spec.Conditions.Length; i++)
            {
                var condition = spec.Conditions[i];
                transition.AddCondition(condition.Mode, condition.Threshold, condition.Parameter);
            }
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

        // Returns true only when it actually added the parameter, so the caller can
        // tell whether the asset needs saving.
        private static bool EnsureParameter(
            AnimatorController controller,
            string name,
            AnimatorControllerParameterType type)
        {
            if (HasParameter(controller, name))
            {
                return false;
            }
            controller.AddParameter(name, type);
            return true;
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
    }
}
