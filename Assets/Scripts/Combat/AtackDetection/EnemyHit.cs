using System;
using UnityEngine;
namespace Alpha.Combat {
    public class EnemyHit : MonoBehaviour
    {
        EnemyHitMeneger meneger;
        [SerializeField] LayerMask enemyLayer;
        PlayerCombatState combatState;
        GameObject player;
        [SerializeField] float speedRequiredForThrowToCount = 10;
        public void Init()
        {
            player =  GameObject.FindGameObjectWithTag("Player");
            meneger = player.GetComponent<EnemyHitMeneger>(); 
            combatState = player.GetComponent<PlayerCombatState>();
        }
        private void OnTriggerEnter(Collider other)
        {
            if (meneger == null) return;
            CombatAction currentAction = combatState.CurrentAction();
            if (currentAction == CombatAction.None || currentAction == CombatAction.NonSemiCombat || currentAction == CombatAction.NonSemiCombat) return;
            if ((enemyLayer.value & (1 << other.gameObject.layer)) == 0) return;
            meneger.HandleAttack(other,combatState.combatDef[currentAction]);

        }
        private void OnCollisionEnter(Collision collision)
        {
            if (meneger == null ) return;
            if ((enemyLayer.value & (1 << collision.gameObject.layer)) == 0) return;
            if (collision.relativeVelocity.magnitude < speedRequiredForThrowToCount) return;
            meneger.HandleAttack(collision.collider, combatState.combatDef[CombatAction.Throw]);


        }

    } 
}
