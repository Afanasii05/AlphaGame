using Alpha.Combat;
using Alpha.Interfaces;
using UnityEngine;
using System.Collections;
public class PlayerAtributes :  HittableCharacter
{
    [SerializeField] private int health = 100;
    [SerializeField] float knockBackDuration = 0.5f;
    PlayerCombatState playerState;
    Rigidbody rb;
    bool isInKnockBack;
    void Awake()
    {
        playerState = GetComponent<PlayerCombatState>();
        rb = GetComponent<Rigidbody>();
    }

    public override void HandleGotHit(CharacterActionDefinition attack, HittableCharacter attacker)
    {
        if (isInKnockBack) return;
        playerState.StartAction(PlayerActions.GetHit);
        Vector3 knowkBackDirection = -(attacker.transform.position - transform.position);
        knowkBackDirection = knowkBackDirection.normalized;
        Vector3 totalKnokback = knowkBackDirection * attack.knockBackPower;
        StartCoroutine(knockBackEnemy(totalKnokback));
    }
  
    IEnumerator knockBackEnemy(Vector3 totalKnockback)
    {
        isInKnockBack = true;
        Vector3 start = rb.position;
        for (float step = 0; step < 1f; step += Time.fixedDeltaTime / knockBackDuration)
        {
            Vector3 target = Vector3.Lerp(start, start + totalKnockback, step);
            target.y = rb.position.y;
            rb.MovePosition(target);
            yield return new WaitForFixedUpdate();
        }
        isInKnockBack = false;

    }
  

}
