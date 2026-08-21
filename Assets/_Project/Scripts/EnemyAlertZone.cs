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
        public Transform enemyRoot;

        // The Inspector is the source of truth for enemy tuning: enemy_ctrl.lua
        // reads every number below off this component, so editing a 遗迹守卫_N in
        // the Hierarchy is what changes its behaviour. The spawn_config.json
        // numbers are only bootstrap defaults, applied when this component does
        // not exist yet (a brand-new enemy). `enemyId` and `enemyRoot` are the
        // exception -- they are plumbing, not tuning, and are always rewritten
        // from the config so HP-bar events stay uniquely keyed.
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
            float attackInterval = 1.2f)
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
            }

            component.enemyId = enemyId;
            component.enemyRoot = target.transform;
            return component;
        }
    }
}
