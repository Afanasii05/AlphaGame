using Alpha.Combat;
using Alpha.Enemies;
using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyCombatState))]
[RequireComponent(typeof(EnemyAI))]
[RequireComponent(typeof(Animator))]
public abstract class EnemyController : MonoBehaviour
{
    protected Animator animator;
    protected EnemyCombatState enemyCombatState;
    protected bool isInKnockBack = false;
    protected EnemyAI aiController;
    Transform target => aiController.target;
    [SerializeField] float knockBackDuration;
    [SerializeField] Transform rig;
    [SerializeField] ParticleSystem stompVFX;
    [SerializeField] float knockBackCooldown = 1.5f;
    [SerializeField] Transform stompPoint;
    float lastTimeKnockBack;
    Vector3 rigRestPosition;
    Quaternion rigRestRotation;

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
        aiController.onTargetInLateralAttack += HandleTargetInLateralAttack;
        aiController.onCutDistanceToPlayer += HandleCutDistanceToPlayer;
    }
    private void OnDisable() 
    {
        aiController.onTargetInAttackRange -= HandleTargetInAttackRange;
        aiController.onTargetInLateralAttack-= HandleTargetInLateralAttack;
        aiController.onCutDistanceToPlayer -= HandleCutDistanceToPlayer;
    }

    private void HandleTargetInAttackRange()
    {enemyCombatState.TryStart(EnemyActions.Attack);}
    public virtual void HandleTargetInLateralAttack()
    {
        if (enemyCombatState.TryStart(EnemyActions.Stomp))
            StartCoroutine(PlayStompVFX(EnemyActions.Stomp));
    }
    public abstract void HandleCutDistanceToPlayer();
    

    public void enemyGetHit(CharacterActionDefinition attack)
    {
        if (isInKnockBack || Time.time - lastTimeKnockBack < knockBackCooldown) return;
        enemyCombatState.TryStart(EnemyActions.GetHit);
        Vector3 knowkBackDirection = -(target.position - transform.position);
        knowkBackDirection = knowkBackDirection.normalized;
        Vector3 totalKnokback = knowkBackDirection * attack.knockBackPower;
        aiController.Aggro();  
        StartCoroutine(knockBackEnemy(totalKnokback));

    }
    IEnumerator knockBackEnemy(Vector3 totalKnockback)
    {
        lastTimeKnockBack = Time.time;
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
    protected IEnumerator PlayStompVFX(EnemyActions action)
    {
        if (stompVFX == null) yield break;
        while (!enemyCombatState.inActiveWindow())
        {
            if (enemyCombatState.CurrentAction() != action) yield break;
            yield return null;
        }
        ParticleSystem fx = Instantiate(stompVFX, stompPoint.position, transform.rotation);
        fx.Play(true);
        Destroy(fx.gameObject, 3f);
    }
}
