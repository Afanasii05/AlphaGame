using Alpha.Combat;
using Alpha.Interfaces;
using UnityEngine;

public class ThrowDetection : MonoBehaviour
{
    Weapon weapon;
    [SerializeField] LayerMask enemyLayer;
    [SerializeField] GameObject hitParticlesPrefab;
    [SerializeField] float speedRequiredForThrowToCount = 10;
    PlayerCombatState playerState => weapon.playerState;
    private void Awake()
    {
        weapon = GetComponent<Weapon>();
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
}
