using Alpha.Combat;
using Alpha.Interfaces;

public class EnemyAtributes : HittableCharacter
{
    EnemyCombatState enemyState;
    EnemyController enemyController;
    protected override void Awake()
    {
        base.Awake();
        enemies.Add(this);
        enemyState = GetComponent<EnemyCombatState>();
        enemyController = GetComponent<EnemyController>();
    }
    protected override void EndCharacter()
    {
        enemies.Remove(this);
        enemyState.StartAction(EnemyActions.Death);
    }
    protected override void StartHitState()
    {
        enemyState.StartAction(EnemyActions.GetHit);
        var agent = enemyController.aiController.agent;
        agent.isStopped = true;
        agent.updatePosition = false;            
    }
    protected override void endHitState()
    {
        var agent = enemyController.aiController.agent;
        agent.nextPosition = transform.position; 
        agent.updatePosition = true;
        agent.isStopped = false;
    }
}
