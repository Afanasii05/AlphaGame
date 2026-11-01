using Alpha.Combat;
using UnityEngine;
using System.Collections;
public class StumphornController : EnemyController
{
    [SerializeField] ParticleSystem stompVFX;
    [SerializeField, Range(0f, 1f)] float stompImpactTime = 0.8f;
    public override void HandleTargetInLateralAttack()
    {
            if (enemyCombatState.TryStart(EnemyActions.Stomp))
                StartCoroutine(PlayStompVFX(EnemyActions.Stomp));
        
    }
    public override void HandleCutDistanceToPlayer()
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
}
