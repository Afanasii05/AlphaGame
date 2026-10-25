using Alpha.Interfaces;
using UnityEngine;
namespace Alpha.Combat {
    public class EnemyHit : MonoBehaviour
    {
        
        [SerializeField] LayerMask enemyLayer;
        [SerializeField] float speedRequiredForThrowToCount = 10;
        Weapon weapon; 
        PlayerCombatState playerState => weapon.playerState;

        private void Awake(){weapon = GetComponent<Weapon>();}

        private void OnTriggerEnter(Collider other) //Orice colider cu Trigger pe care se afla scriptul.
        {
            if (playerState == null) return;
            if ((enemyLayer.value & (1 << other.gameObject.layer)) == 0) return;
            PlayerActions currentAction = playerState.CurrentAction();
            if (currentAction == PlayerActions.None || currentAction == PlayerActions.NonCombat || currentAction == PlayerActions.NonSemiCombat) return;
            HittableCharacter target = other.GetComponentInParent<HittableCharacter>();
            playerState.hitMeneger.HandleAttack(target, playerState.combatDef[currentAction]);

        }
        private void OnCollisionEnter(Collision collision) //Orice colider nonKinematic pe care se afla scriptul.
        {
            if (playerState == null) return;
            if ((enemyLayer.value & (1 << collision.gameObject.layer)) == 0) return;
            if (collision.relativeVelocity.magnitude < speedRequiredForThrowToCount) return;
            HittableCharacter target = collision.collider.GetComponentInParent<HittableCharacter>();
            playerState.hitMeneger.HandleAttack(target, playerState.combatDef[PlayerActions.Throw]);


        }

    } 
}
