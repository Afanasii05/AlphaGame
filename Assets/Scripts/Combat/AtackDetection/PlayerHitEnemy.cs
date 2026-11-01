using Alpha.Interfaces;
using UnityEngine;
namespace Alpha.Combat {
    public class PlayerHitEnemy : MonoBehaviour
    {
        
        [SerializeField] LayerMask enemyLayer;
        [SerializeField] LayerMask playerLayer;
        [SerializeField] GameObject hitParticlesPrefab;
        [SerializeField] float speedRequiredForThrowToCount = 10;
        [SerializeField] Collider hitbox;
        CameraShake cameraShake;
        Weapon weapon; 
        PlayerCombatState playerState => weapon.playerState;

        private void Awake(){
            weapon = GetComponent<Weapon>();
            cameraShake = Camera.main.GetComponent<CameraShake>();
        }

        private void OnTriggerEnter(Collider other) //Orice colider cu Trigger pe care se afla scriptul.
        {
            if (playerState == null || (playerLayer.value & (1 << other.gameObject.layer)) != 0) return;
            PlayerActions currentAction = playerState.CurrentAction();
            if (currentAction == PlayerActions.None || currentAction == PlayerActions.NonSemiCombat) return;
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
        private void OnCollisionEnter(Collision collision) //Orice colider nonKinematic pe care se afla scriptul.
        {
            if (playerState == null) return;
            if ((enemyLayer.value & (1 << collision.gameObject.layer)) == 0) return;
            if (collision.relativeVelocity.magnitude < speedRequiredForThrowToCount) return;
            ContactPoint contact = collision.GetContact(0);
            Vector3 punct = contact.point;
            Vector3 normala = contact.normal;
            Instantiate(hitParticlesPrefab, punct, Quaternion.LookRotation(normala));
            HittableCharacter target = collision.collider.GetComponentInParent<HittableCharacter>();
            playerState.hitMeneger.HandleAttack(target, playerState.combatDef[PlayerActions.Throw]);


        }
        static bool SupportsClosestPoint(Collider c) {
           return c is BoxCollider || c is SphereCollider || c is CapsuleCollider || (c is MeshCollider mesh && mesh.convex); }

    } 
}
