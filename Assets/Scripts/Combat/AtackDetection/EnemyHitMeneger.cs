using Alpha.Enemies;
using System;
using UnityEngine;
namespace Alpha.Combat
{
    public class EnemyHitMeneger : MonoBehaviour
    {
        EnemyAI eminem;
        [SerializeField] PlayerCombatState combatState;
        CombatActionDefinition actual;
        public void HandleAttack(Collider enemy, CombatActionDefinition hitAction)
        {
            if (actual == hitAction)
                return;
            actual = hitAction;
            Debug.Log($"I have hit {enemy.ToString()}  with {hitAction.ToString()}");
            eminem = enemy.GetComponent<EnemyAI>();
            eminem.enemyGetHit(hitAction);
        }
        public void resetAction()
        {
            actual = null;
        }
    }
}