using Alpha.Combat;
using Alpha.Interfaces;
using UnityEngine;

public class EnemyHitPlayer : MonoBehaviour
{
    [SerializeField] LayerMask enemyLayer;
    [SerializeField] Collider hitbox;
    [SerializeField] GameObject hitParticlesPrefab;
    EnemyCombatState enemyState;
    private void Awake(){enemyState = GetComponent<EnemyCombatState>();}
    private void OnTriggerEnter(Collider other) 
    {
        if (enemyState == null) return;
        if ((enemyLayer.value & (1 << other.gameObject.layer)) == 0) return;
        EnemyActions currentAction = enemyState.currentAction;
        if (currentAction == EnemyActions.None ) return;
        if (SupportsClosestPoint(other)) 
        {
            Vector3 punct = other.ClosestPoint(hitbox.bounds.center);
            Vector3 directie = (punct - other.bounds.center).normalized;
            Instantiate(hitParticlesPrefab, punct, Quaternion.LookRotation(directie));
        }
        HittableCharacter target = other.GetComponentInParent<HittableCharacter>();
        HittableCharacter self = GetComponentInParent<HittableCharacter>();
        if (target == null || target == self) return;
        enemyState.hitMeneger.HandleAttack(target, enemyState.combatDef[currentAction], self);

    }

    static bool SupportsClosestPoint(Collider c)
    {
        return c is BoxCollider || c is SphereCollider || c is CapsuleCollider || (c is MeshCollider mesh && mesh.convex);
    }
}

