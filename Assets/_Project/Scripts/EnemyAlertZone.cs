using UnityEngine;

namespace LostRelic
{
    [DisallowMultipleComponent]
    public class EnemyAlertZone : MonoBehaviour
    {
        public string enemyId;
        public float radius = 6f;
        public float patrolRadius = 4f;
        public float chaseRadius = 17.5f;
        public float patrolSpeed = 1.8f;
        public float chaseSpeed = 4f;
        public float idleMin = 1.5f;
        public float idleMax = 4f;
        public float attackDistance = 1.2f;
        public float maxHp = 50f;
        public float hp = 50f;
        public float attack = 5f;
        public float defense = 2f;
        public float attackRange = 1.5f;
        public float attackInterval = 1.2f;
        // How far a hit shoves this enemy back, in world units. 0 = immune, and
        // that has to keep working through the config merge, which is why the
        // Lua side tests for nil rather than falsiness. The window the shove is
        // spread over is not tunable -- it lives in enemy_ctrl.lua so the
        // designer only has one dial per enemy.
        public float knockback = 0.6f;
        public Transform enemyRoot;

        // Tuning precedence, per field: spawn_config.json wins wherever it
        // specifies a key, and the Inspector wins wherever it does not. That
        // merge lives in enemy_ctrl.lua (apply_config_overrides), NOT here -- by
        // the time Attach is called the Lua side has already substituted
        // defaults for absent keys, so these parameters genuinely cannot tell
        // "the designer omitted this" from "the designer wrote the default".
        // They are therefore only used to seed a component that does not exist
        // yet (a brand-new enemy dragged into the scene). `enemyId` and
        // `enemyRoot` are the exception -- they are plumbing, not tuning, and
        // are always rewritten from the config so HP-bar events stay uniquely
        // keyed.
        public static EnemyAlertZone Attach(
            GameObject target,
            string enemyId,
            float radius,
            float patrolRadius = 4f,
            float chaseRadius = 17.5f,
            float patrolSpeed = 1.8f,
            float chaseSpeed = 4f,
            float idleMin = 1.5f,
            float idleMax = 4f,
            float attackDistance = 1.2f,
            float maxHp = 50f,
            float hp = 50f,
            float attack = 5f,
            float defense = 2f,
            float attackRange = 1.5f,
            float attackInterval = 1.2f,
            float knockback = 0.6f)
        {
            var component = target.GetComponent<EnemyAlertZone>();
            if (component == null)
            {
                // No authored zone yet, so seed it from the config once. Note
                // both shipped enemy prefabs already carry one (SlimePBR an
                // EnemyAlertZone, TurtleShellPBR a RelicGuard subclass), which
                // means this branch only runs for newly authored enemies.
                component = target.AddComponent<EnemyAlertZone>();
                component.radius = radius;
                component.patrolRadius = patrolRadius;
                component.chaseRadius = chaseRadius;
                component.patrolSpeed = patrolSpeed;
                component.chaseSpeed = chaseSpeed;
                component.idleMin = idleMin;
                component.idleMax = idleMax;
                component.attackDistance = attackDistance;
                component.maxHp = maxHp;
                component.hp = hp;
                component.attack = attack;
                component.defense = defense;
                component.attackRange = attackRange;
                component.attackInterval = attackInterval;
                component.knockback = knockback;
            }

            component.enemyId = enemyId;
            component.enemyRoot = target.transform;
            return component;
        }
    }
}
