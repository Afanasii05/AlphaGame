using Alpha.Combat;
using Alpha.Enemies;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyCombatState))]
[RequireComponent(typeof(EnemyAI))]
[RequireComponent(typeof(Animator))]
public abstract class EnemyController : MonoBehaviour
{
    protected Animator animator;
    protected EnemyCombatState enemyCombatState;
    public EnemyAI aiController;
    [SerializeField] Transform rig;
    [SerializeField] ParticleSystem stompVFX;
    [SerializeField] Transform stompPoint;
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
    private void LateUpdate() => rig.localPosition = rigRestPosition;

    private void OnAnimatorMove()
    {
        EnemyActions action = enemyCombatState.CurrentAction();
        if (action == EnemyActions.None || !enemyCombatState.combatDef[action].hasItsOwnMovement) return;
        Vector3 local = rigRestRotation * (Quaternion.Inverse(transform.rotation) * animator.deltaPosition);
        aiController.agent.Move(transform.rotation * local);
    }

    //Actiunile apelate din ENEMY AI -> actiuni still combat;
    private void HandleTargetInAttackRange()
    {enemyCombatState.TryStart(EnemyActions.Attack);}
    public abstract void HandleCutDistanceToPlayer();
    public virtual void HandleTargetInLateralAttack()
    {
        if (enemyCombatState.TryStart(EnemyActions.Stomp))
            StartCoroutine(PlayStompVFX(EnemyActions.Stomp));
    }

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
