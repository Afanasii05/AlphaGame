using Alpha.Combat;
public class StumphornController : EnemyController
{
    
    
    public override void HandleCutDistanceToPlayer()
    {
        if (enemyCombatState.TryStart(EnemyActions.Roll))
            StartCoroutine(PlayStompVFX(EnemyActions.Roll));
    }
    
}
