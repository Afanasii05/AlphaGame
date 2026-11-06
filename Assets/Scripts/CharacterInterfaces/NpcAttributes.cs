using Alpha.Combat;

namespace Alpha.Interfaces
{
    public abstract class NpcAttributes : HittableCharacter
    {
        protected EnemyCombatState npcState;
        protected EnemyController npcController;

        protected override void Awake()
        {
            base.Awake();
            npcState = GetComponent<EnemyCombatState>();
            npcController = GetComponent<EnemyController>();
        }

        protected override void StartHitState()
        {
            npcState.StartAction(EnemyActions.GetHit);
            var agent = npcController.aiController.agent;
            agent.isStopped = true;
            agent.updatePosition = false;
        }

        protected override void endHitState()
        {
            var agent = npcController.aiController.agent;
            agent.nextPosition = transform.position;
            agent.updatePosition = true;
            agent.isStopped = false;
        }
    }
}
