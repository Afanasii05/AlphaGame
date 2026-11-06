using Alpha.Combat;
using Alpha.Interfaces;

public class AllyAtributes : HittableCharacter
{
    EnemyCombatState allyState;
    EnemyController allyController;

    protected override void Awake()
    {
        base.Awake();
        allys.Add(this);
        allyState = GetComponent<EnemyCombatState>();
        allyController = GetComponent<EnemyController>();
    }
    protected override void StartHitState()
    {
        allyState.StartAction(EnemyActions.GetHit);
        var agent = allyController.aiController.agent;
        agent.isStopped = true;
        agent.updatePosition = false;              
    }
    protected override void EndCharacter()
    {
        allys.Remove(this);
        allyState.StartAction(EnemyActions.Death);
    }
    protected override void endHitState()
    {
        var agent = allyController.aiController.agent;
        agent.nextPosition = transform.position;  
        agent.updatePosition = true;
        agent.isStopped = false;
    }
}
