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

    [SerializeField] ParticleSystem stompVFX;

    [SerializeField] Transform rig;
    Vector3 rigRestPosition;
    Quaternion rigRestRotation;
    [SerializeField, Range(0f, 1f)] float stompImpactTime = 0.8f;



    private void Awake()
    {
        animator = GetComponent<Animator>();
        enemyCombatState = GetComponent<EnemyCombatState>();
        aiController = GetComponent<EnemyAI>();
        rigRestPosition = rig.localPosition;
        rigRestRotation = rig.localRotation;
    }
    private void OnEnable()  
    { 
        aiController.onTargetInAttackRange += HandleTargetInAttackRange;
        aiController.onTargetShouldBeStomped += HandleTargetInStompRange;
        aiController.onRollToPlayer += HandleRollToPlayer;
    }
    private void OnDisable() 
    {
        aiController.onTargetInAttackRange -= HandleTargetInAttackRange;
        aiController.onTargetShouldBeStomped -= HandleTargetInStompRange;
        aiController.onRollToPlayer -= HandleRollToPlayer;
    }

    private void HandleTargetInAttackRange()
    {
        enemyCombatState.TryStart(EnemyActions.Attack);
    }
    private void HandleTargetInStompRange()
    {
        if (enemyCombatState.TryStart(EnemyActions.Stomp))
            StartCoroutine(PlayStompVFX(EnemyActions.Stomp));
    }
    private void HandleRollToPlayer()
    {
        if (enemyCombatState.TryStart(EnemyActions.Roll))
            StartCoroutine(PlayStompVFX(EnemyActions.Roll));
    }

    IEnumerator PlayStompVFX(EnemyActions action)
    {
        if (stompVFX == null) yield break;
        while (!enemyCombatState.inActiveWindow())
        {
            if (enemyCombatState.CurrentAction() != action) yield break; 
            yield return null;
        }

        stompVFX.Play(true);
    }

    

    public void enemyGetHit(CharacterActionDefinition attack)
    {
        if (isInKnockBack) return;
        enemyCombatState.TryStart(EnemyActions.GetHit);
        Vector3 knowkBackDirection = -(target.position - transform.position);
        knowkBackDirection = knowkBackDirection.normalized;
        Vector3 totalKnokback = knowkBackDirection * attack.knockBackPower;
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

    private void OnAnimatorMove()
    {
        EnemyActions action = enemyCombatState.CurrentAction();
        if (action == EnemyActions.None || !enemyCombatState.combatDef[action].hasItsOwnMovement) return;
        Vector3 local = rigRestRotation * (Quaternion.Inverse(transform.rotation) * animator.deltaPosition);
        aiController.agent.Move(transform.rotation * local);
    }

    private void LateUpdate() => rig.localPosition = rigRestPosition;
}
