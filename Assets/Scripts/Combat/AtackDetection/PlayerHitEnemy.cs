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
        HittableCharacter playerHittable;

        private void Awake(){
            GameObject player = GameObject.FindWithTag("Player");
            playerState = player.GetComponent<PlayerCombatState>();
            playerHittable = player.GetComponent<HittableCharacter>();
            cameraShake = Camera.main.GetComponent<CameraShake>();
        }

        private void OnTriggerEnter(Collider other) //Orice colider cu Trigger pe care se afla scriptul.
        {
            if (playerState == null || (playerLayer.value & (1 << other.gameObject.layer)) != 0) return;
            PlayerActions currentAction = playerState.currentAction;
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
            if (target == null) return;
            playerState.hitMeneger.HandleAttack(target, playerState.combatDef[currentAction], playerHittable);

        }
        
        static bool SupportsClosestPoint(Collider c) {
           return c is BoxCollider || c is SphereCollider || c is CapsuleCollider || (c is MeshCollider mesh && mesh.convex); }

    } 
}
