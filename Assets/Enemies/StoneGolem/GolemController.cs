using Alpha.Combat;
using System.Collections;
using UnityEngine;

public class GolemController : EnemyController
{
    [SerializeField]MirageAttack mirageAttack;
    [SerializeField] Transform impactPoint;

    public override void HandleCutDistanceToPlayer()
    {
        if (enemyCombatState.TryStart(EnemyActions.Special))
            StartCoroutine(waitForMirage());
    }
    IEnumerator waitForMirage()
    {
        while (!enemyCombatState.inActiveWindow()) yield return null;
        if (enemyCombatState.currentAction != EnemyActions.Special) yield break;
        mirageAttack.mirageFromGround(impactPoint);
    }
}
