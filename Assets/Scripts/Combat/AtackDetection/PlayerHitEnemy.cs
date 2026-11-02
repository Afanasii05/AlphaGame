using Alpha.Interfaces;
using UnityEngine;
namespace Alpha.Combat {
    public class PlayerHitEnemy : MonoBehaviour
    {
        
        [SerializeField] LayerMask enemyLayer;
        [SerializeField] LayerMask playerLayer;
        [SerializeField] GameObject hitParticlesPrefab;
        
        [SerializeField] Collider hitbox;
        CameraShake cameraShake;
        PlayerCombatState playerState;

        private void Awake(){
            GameObject player = GameObject.FindWithTag("Player");
            playerState = player.GetComponent<PlayerCombatState>();
            cameraShake = Camera.main.GetComponent<CameraShake>();
        }

        private void OnTriggerEnter(Collider other) //Orice colider cu Trigger pe care se afla scriptul.
        {
            if (playerState == null || (playerLayer.value & (1 << other.gameObject.layer)) != 0) return;
            PlayerActions currentAction = playerState.CurrentAction();
            if (currentAction == PlayerActions.None || currentAction == PlayerActions.NonSemiCombat || currentAction == PlayerActions.Block) return;
            if (SupportsClosestPoint(other)) // Daca coliderul suporta closest point, atunci instantiem particulele de hit.
            {
                Vector3 punct = other.ClosestPoint(hitbox.bounds.center);
                Vector3 directie = (punct - other.bounds.center).normalized;
                Instantiate(hitParticlesPrefab, punct, Quaternion.LookRotation(directie));
            }
            if ((enemyLayer.value & (1 << other.gameObject.layer)) == 0) return;
            cameraShake.Shake(0.12f, 0.15f);
            HittableCharacter target = other.GetComponentInParent<HittableCharacter>();
            playerState.hitMeneger.HandleAttack(target, playerState.combatDef[currentAction]);

        }
        
        static bool SupportsClosestPoint(Collider c) {
           return c is BoxCollider || c is SphereCollider || c is CapsuleCollider || (c is MeshCollider mesh && mesh.convex); }

    } 
}
