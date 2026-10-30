using Alpha.Combat;
using Alpha.Interfaces;
using UnityEngine;

public class EnemyHitPlayer : MonoBehaviour
{
    [SerializeField] LayerMask enemyLayer;
    EnemyCombatState enemyState;
    private void Awake(){enemyState = GetComponent<EnemyCombatState>();}
    private void OnTriggerEnter(Collider other) 
    {
        if (enemyState == null) return;
        if ((enemyLayer.value & (1 << other.gameObject.layer)) == 0) return;
        EnemyActions currentAction = enemyState.CurrentAction();
        if (currentAction == EnemyActions.None ) return;
        HittableCharacter target = other.GetComponentInParent<HittableCharacter>();
        enemyState.hitMeneger.HandleAttack(target, enemyState.combatDef[currentAction],GetComponentInParent<HittableCharacter>());

    }
}
