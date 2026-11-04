using Alpha.Combat;
using UnityEngine;
public class ShackleController : EnemyController
{
    public override void HandleTargetInLateralAttack()
    {

        enemyCombatState.TryStart(EnemyActions.Spin);

    }
    public override void HandleCutDistanceToPlayer()
    {

    }
}
