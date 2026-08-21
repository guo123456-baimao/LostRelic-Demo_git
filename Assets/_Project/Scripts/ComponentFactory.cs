using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace LostRelic
{
    public static class ComponentFactory
    {
        private static readonly List<Interactable> InteractableCache =
            new List<Interactable>();

        public static CharacterController AddCharacterController(
            GameObject target,
            float height,
            float radius,
            Vector3 center)
        {
            var controller = target.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = target.AddComponent<CharacterController>();
            }

            controller.height = height;
            controller.radius = radius;
            controller.center = center;
            controller.stepOffset = 0.6f;
            controller.slopeLimit = 60f;
            controller.skinWidth = 0.05f;
            controller.minMoveDistance = 0f;
            return controller;
        }

        public static NavMeshAgent AddNavMeshAgent(
            GameObject target,
            float radius,
            float height,
            float speed,
            float angularSpeed)
        {
            var agent = target.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = target.AddComponent<NavMeshAgent>();
            }

            agent.radius = radius;
            agent.height = height;
            agent.speed = speed;
            agent.angularSpeed = angularSpeed;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.5f;
            agent.autoBraking = true;
            agent.updateRotation = true;
            agent.updatePosition = true;
            return agent;
        }

        // A patrol waypoint has to be somewhere the agent can actually finish a
        // path to. A blind random offset around the spawn happily lands on the
        // far side of a wall; NavMeshAgent answers that with PathPartial, walks
        // into the wall and leaves pathPending stuck true, which used to freeze
        // enemy_ctrl's patrol state for the rest of the session. Rejecting bad
        // candidates here is far cheaper than detecting the stall afterwards.
        public static Vector3 SampleReachablePoint(
            Vector3 from,
            Vector3 center,
            float radius,
            int attempts)
        {
            var path = new NavMeshPath();
            for (var i = 0; i < attempts; i++)
            {
                var angle = Random.value * Mathf.PI * 2f;
                var distance = radius * (0.4f + 0.6f * Random.value);
                var candidate = center + new Vector3(
                    Mathf.Cos(angle) * distance,
                    0f,
                    Mathf.Sin(angle) * distance);

                NavMeshHit hit;
                if (!NavMesh.SamplePosition(candidate, out hit, 1f, NavMesh.AllAreas))
                {
                    continue;
                }

                if (NavMesh.CalculatePath(from, hit.position, NavMesh.AllAreas, path) &&
                    path.status == NavMeshPathStatus.PathComplete)
                {
                    return hit.position;
                }
            }

            // Boxed in: stay put. The caller reads that as an instant arrival
            // and goes back to idle before trying again a few seconds later.
            return from;
        }

        // enemy_ctrl needs the true length of a swing so it can show exactly one
        // swing per damage tick. TurtleShell's Attack01 clip is marked looping,
        // so a bare Play() re-swings every 0.83s while damage ticks on the much
        // slower attackInterval -- animation and health bar visibly disagreed.
        public static float GetClipLength(Animator animator, string clipName)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return 0f;
            }

            var clips = animator.runtimeAnimatorController.animationClips;
            foreach (var clip in clips)
            {
                if (clip != null && clip.name == clipName)
                {
                    return clip.length;
                }
            }

            return 0f;
        }

        public static CapsuleCollider AddCapsuleCollider(
            GameObject target,
            float height,
            float radius,
            Vector3 center)
        {
            var collider = target.GetComponent<CapsuleCollider>();
            if (collider == null)
            {
                collider = target.AddComponent<CapsuleCollider>();
            }

            collider.height = height;
            collider.radius = radius;
            collider.center = center;
            return collider;
        }

        public static Button AddButton(GameObject target)
        {
            var button = target.GetComponent<Button>();
            if (button == null)
            {
                button = target.AddComponent<Button>();
            }
            return button;
        }

        public static Animator GetAnimator(GameObject target)
        {
            return target != null ? target.GetComponent<Animator>() : null;
        }

        public static RectTransform GetRect(Transform target)
        {
            return target != null ? target as RectTransform : null;
        }

        public static GameObject FindIncludingInactive(string name)
        {
            GameObject fallback = null;
            var all = Object.FindObjectsOfType<GameObject>(true);
            foreach (var go in all)
            {
                if (go.name != name)
                {
                    continue;
                }

                if (go.activeInHierarchy)
                {
                    return go;
                }

                if (fallback == null)
                {
                    fallback = go;
                }
            }

            return fallback;
        }

        public static GameObject FindByPartialName(string part)
        {
            var all = Object.FindObjectsOfType<GameObject>(true);
            foreach (var go in all)
            {
                if (go.name.Contains(part))
                {
                    return go;
                }
            }

            return null;
        }

        public static GameObject[] FindAllByExactName(string name)
        {
            var all = Object.FindObjectsOfType<GameObject>(true);
            var matches = new List<GameObject>();
            foreach (var go in all)
            {
                if (go.name == name)
                {
                    matches.Add(go);
                }
            }
            return matches.ToArray();
        }

        public static GameObject[] FindAllByPartialName(string part)
        {
            var all = Object.FindObjectsOfType<GameObject>(true);
            var matches = new List<GameObject>();
            foreach (var go in all)
            {
                if (go.name.Contains(part))
                {
                    matches.Add(go);
                }
            }
            return matches.ToArray();
        }

        public static Text GetText(Transform target)
        {
            return target != null ? target.GetComponent<Text>() : null;
        }

        public static Animator SetAnimatorController(
            GameObject target,
            RuntimeAnimatorController controller)
        {
            if (target == null || controller == null)
            {
                return null;
            }

            var animator = target.GetComponent<Animator>();
            if (animator == null)
            {
                animator = target.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            return animator;
        }

        public static Animator GetAnimatorInChildren(GameObject target)
        {
            return target != null ? target.GetComponentInChildren<Animator>(true) : null;
        }

        public static void SetRenderersActive(GameObject target, bool active)
        {
            if (target == null)
            {
                return;
            }

            var renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                renderer.enabled = active;
            }
        }

        public static void RefreshInteractables()
        {
            InteractableCache.Clear();
            var found = Object.FindObjectsOfType<Interactable>();
            if (found != null)
            {
                InteractableCache.AddRange(found);
            }
        }

        public static int GetInteractableCount()
        {
            return InteractableCache.Count;
        }

        public static Interactable GetInteractable(int index)
        {
            return InteractableCache[index];
        }
    }
}
