using Alpha.Combat;
using Alpha.Interfaces;
using UnityEngine;

namespace Alpha.World
{
    // Tepii golemului lovesc jucatorul in numele celui mai apropiat inamic (golemul care i-a ridicat)
    public class GolemSpikeDamage : MonoBehaviour
    {
        [SerializeField] LayerMask targetLayers = 384;
        [SerializeField] GameObject hitParticlesPrefab;
        [SerializeField] EnemyActions attackAction = EnemyActions.Special;

        void OnTriggerEnter(Collider other)
        {
            if ((targetLayers.value & (1 << other.gameObject.layer)) == 0) return;
            HittableCharacter target = other.GetComponentInParent<HittableCharacter>();
            if (target == null) return;

            HittableCharacter golem = HittableCharacter.Closest(HittableCharacter.enemies, transform.position);
            if (golem == null || golem == target || !golem.TryGetComponent(out EnemyCombatState state)) return;
            if (!state.combatDef.TryGetValue(attackAction, out EnemyActionDefinition def)) return;

            if (hitParticlesPrefab != null)
            {
                bool closest = other is BoxCollider || other is SphereCollider || other is CapsuleCollider || (other is MeshCollider mesh && mesh.convex);
                Vector3 point = closest ? other.ClosestPoint(transform.position) : other.bounds.center;
                Instantiate(hitParticlesPrefab, point, Quaternion.LookRotation(point - transform.position + Vector3.up * 0.01f));
            }
            state.hitMeneger.HandleAttack(target, def, golem);
        }
    }
}
