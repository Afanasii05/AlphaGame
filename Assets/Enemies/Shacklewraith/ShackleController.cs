using Alpha.Combat;
public class ShackleController : EnemyController
{
    public override void HandleTargetInLateralAttack()
    {
        enemyCombatState.TryStart(EnemyActions.Spin);
    }
    public override void HandleCutDistanceToPlayer()
    {
        enemyCombatState.TryStart(EnemyActions.Special);
    }
}
