using System;
using UnityEngine;
using UnityEngine.AI;
namespace Alpha.Enemies
{
    public enum EnemyState { Idle, Chase, Return, Dead }

    //Doar detectie si pathfinding. Ce face inamicul cand ajunge la tinta decide EnemyController.
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] public Transform target;         
        public EnemyState State = EnemyState.Idle;
        public NavMeshAgent agent;
        private Vector3 homePosition;
        public event Action onTargetInRange;   //tinta e in raza si in fata

        //Valori pentru detectie
        [SerializeField] float maxDistanceToDetectFromHome = 15f;
        [SerializeField] float maxAngleToBeSeen = 120f;
        [SerializeField] float detectDistance = 10f;
        [SerializeField] float loseDistance = 15f;
        [SerializeField] float distanceFromHome= 30f;
        [SerializeField] float attackRange = 2.65f;   //aici se opreste si anunta controllerul
        [SerializeField] float attackAngle = 70;

        //Miscare
        [SerializeField] float runSpeed = 3f;
        [SerializeField] float walkSpeed = 0.53f;
        [SerializeField] float turnSpeed = 1f;
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private float sqrDistToPlayer;
        EnemyCombatState enemyCombatState;
        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            homePosition = transform.position;
            enemyCombatState = GetComponent<EnemyCombatState>();
        }
        void Update()
        {
            if (State == EnemyState.Dead) return;
            sqrDistToPlayer = (target.position - transform.position).sqrMagnitude;
            if (enemyCombatState.IsBusy)
                agent.isStopped = true;   //cat ruleaza o actiune nu ma misc
            else
            {
                switch (State)
                {
                    case EnemyState.Idle:
                        determineChaseFromNonAggro();
                        break;
                    case EnemyState.Chase:
                        UpdateChase();
                        break;
                    case EnemyState.Return:
                        if (determineIddle()) SetState(EnemyState.Idle);
                        else determineChaseFromNonAggro();
                        break;
                }
            }
            animator.SetFloat(SpeedHash, agent.velocity.magnitude, 0.1f, Time.deltaTime);
        }
        public void Aggro()
        {
            if (State != EnemyState.Dead && State != EnemyState.Chase) SetState(EnemyState.Chase);
        }
        private bool determineIddle() => Vector3.Distance(homePosition, transform.position) <= agent.stoppingDistance + 0.1f;
        private void UpdateChase()
        {
            if (!shouldContinueToChase()) { SetState(EnemyState.Return); return; }
            if (sqrDistToPlayer > attackRange * attackRange)
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);
                return;
            }

            agent.isStopped = true;
            if (FaceTarget() <= attackAngle) onTargetInRange?.Invoke();
        }
        private float FaceTarget()
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
            return Vector3.Angle(transform.forward, dir);
        }
        private bool canChase()
        {
            if (sqrDistToPlayer > detectDistance * detectDistance)
                return false;
            if ((target.position - homePosition).sqrMagnitude > maxDistanceToDetectFromHome * maxDistanceToDetectFromHome)
                return false;
            Vector3 toPlayer = target.position - transform.position;
            Vector3 forwardOfEnemy = transform.forward; 
            toPlayer.y = 0;
            forwardOfEnemy.y = 0;
            forwardOfEnemy = forwardOfEnemy.normalized;
            toPlayer = toPlayer.normalized;
            return (Vector3.Dot(toPlayer, forwardOfEnemy) >= Mathf.Cos(maxAngleToBeSeen * Mathf.Deg2Rad));
            
        }
        private bool shouldContinueToChase()
        {
            Vector3 toHome = homePosition - transform.position;
            return ((sqrDistToPlayer <= loseDistance * loseDistance) && (toHome.sqrMagnitude<=distanceFromHome*distanceFromHome));

        }
        private void determineChaseFromNonAggro()
        {
            if (canChase())
                SetState(EnemyState.Chase);

        }
        private void SetState(EnemyState updatingState)
        {
            State = updatingState;
            switch (updatingState)
            {
                case EnemyState.Chase:
                    agent.isStopped = false;
                    agent.SetDestination(target.position);
                    agent.speed = runSpeed;
                    break;
                case EnemyState.Return:
                    agent.isStopped = false;   
                    agent.SetDestination(homePosition);
                    agent.speed = walkSpeed;
                    break;
                case EnemyState.Idle:
                    agent.ResetPath();
                    agent.speed = 0f;
                    break;
            }
        }
    }
}
