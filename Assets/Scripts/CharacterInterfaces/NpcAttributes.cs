using Alpha.Combat;
using System.Collections;
using UnityEngine;

namespace Alpha.Interfaces
{
    public abstract class NpcAttributes : HittableCharacter
    {
        protected EnemyCombatState npcState;
        protected EnemyController npcController;
        [SerializeField] float delayForRecover=0.4f;
        public bool isRecovering;
        Coroutine recover;

        protected override void Awake()
        {
            base.Awake();
            npcState = GetComponent<EnemyCombatState>();
            npcController = GetComponent<EnemyController>();
        }

        protected override void StartHitState()
        {
            npcState.StartAction(EnemyActions.GetHit);
            npcController.aiController.Aggro(); // daca il lovesc, ma vede indiferent de unghi/distanta
            
        }
        protected override void handleKnockbackLogic(Vector3 totalKnockback)
        {
            if (recover != null) { StopCoroutine(recover);}
            StartCoroutine(handleKnockBack(totalKnockback));
        }
      
        protected override void endHitState()
        {
            if (recover != null) { StopCoroutine(recover); }
            recover=StartCoroutine(recoverAfterKnockBack());
        }
        protected override IEnumerator handleKnockBack(Vector3 totalKnockback)
        {
            var agent = npcController.aiController.agent;
            agent.isStopped = true;
            isRecovering = true;
            isInKnockBack = true;
            lastKnockBackTime = Time.time;
            Vector3 moved = Vector3.zero;
            for (float t = 0f; t < knockBackDuration; t += Time.deltaTime)
            {
                float k = t / knockBackDuration;
                k = 1f - (1f - k) * (1f - k);
                Vector3 shouldBe = totalKnockback * k;
                agent.Move(shouldBe - moved);
                moved = shouldBe;
                yield return null;
            }
            agent.Move(totalKnockback - moved);
            isInKnockBack = false;
            endHitState();
        }

        private IEnumerator recoverAfterKnockBack()
        {
            yield return new WaitForSeconds(delayForRecover);
            npcController.aiController.agent.isStopped = false;
            isRecovering = false;
        }
    }
}
