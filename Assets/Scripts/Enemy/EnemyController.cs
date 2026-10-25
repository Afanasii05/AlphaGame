using Alpha.Combat;
using Alpha.Enemies;
using System;
using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    Animator animator;
    EnemyCombatState enemyCombatState;
    bool isInKnockBack = false;
    EnemyAI aiController;
    Transform target => aiController.target;
    [SerializeField] float knockBackDuration;
    private void Awake()
    {
        animator = GetComponent<Animator>();
        enemyCombatState = GetComponent<EnemyCombatState>();
        aiController = GetComponent<EnemyAI>();
    }
    private void OnEnable()  { aiController.onTargetInRange += HandleTargetInRange; }
    private void OnDisable() { aiController.onTargetInRange -= HandleTargetInRange; }

    private void HandleTargetInRange()
    {
        enemyCombatState.TryStart(EnemyActions.Attack);
    }

    public void enemyGetHit(CharacterActionDefinition attack)
    {
        if (isInKnockBack) return;
        Vector3 knowkBackDirection = -(target.position - transform.position);
        knowkBackDirection = knowkBackDirection.normalized;
        Vector3 totalKnokback = knowkBackDirection * attack.knockBackPower;
        enemyCombatState.StartAction(EnemyActions.GetHit);
        aiController.Aggro();  
        StartCoroutine(knockBackEnemy(totalKnokback));
    }
    IEnumerator knockBackEnemy(Vector3 totalKnockback)
    {
        isInKnockBack = true;
        Vector3 start = transform.position;
        for (float step = 0; step < 1f; step += Time.deltaTime / knockBackDuration)
        {
            Vector3 next = Vector3.Lerp(start, start + totalKnockback, step);
            aiController.agent.Move(next - transform.position);
            yield return null;
        }
        isInKnockBack = false;

    }
}
