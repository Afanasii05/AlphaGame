using Alpha.Combat;
using NUnit;
using System.Collections;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.AI;
namespace Alpha.Enemies
{
    public enum EnemyState { Idle, Chase, Attack, Hit, Return, Dead }

    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform target;         
        public EnemyState State { get; private set; } = EnemyState.Idle;
        private NavMeshAgent agent;
        private Vector3 homePosition;

        //Valori pentru detectie
        [SerializeField] float maxDistanceToDetectFromHome = 15f;
        [SerializeField] float maxAngleToBeSeen = 120f;
        [SerializeField] float detectDistance = 10f;
        [SerializeField] float loseDistance = 15f;
        [SerializeField] float distanceFromHome= 30f;
        [SerializeField] float attackRange = 2.65f;
        [SerializeField] float attackAngle = 70;

        //Miscare
        [SerializeField] float runSpeed = 3f;
        [SerializeField] float walkSpeed = 0.53f;
        [SerializeField] float turnSpeed = 1f;
        //Lovit
        [SerializeField] float knockBackPower = 5f;
        [SerializeField] float knockBackDuration = 1f;
        private   int SpeedHash = Animator.StringToHash("Speed");
        private   int AttackHash = Animator.StringToHash("Attack");
        private   int HitHash = Animator.StringToHash("Hit");
        private   int DeadHash = Animator.StringToHash("Dead");
        private   int AttackTag = Animator.StringToHash("Attack");
        private float sqrDistToPlayer;
        private Vector3 totalKnokback;
        private bool isInKnockBack=false;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            homePosition = transform.position;
        }
        void Update()
        {
            if (State == EnemyState.Dead) return;
            sqrDistToPlayer = (target.position - transform.position).sqrMagnitude;
            switch (State)
            {
                case EnemyState.Idle: 
                    updateChase();
                    break;
                case EnemyState.Chase:
                    UpdateChase();
                    break;
                case EnemyState.Return:
                    if (Vector3.Distance(homePosition, transform.position) <= agent.stoppingDistance + 0.1f) SetState(EnemyState.Idle);
                    else updateChase();
                    break;
                case EnemyState.Attack:
                    if(!inAnimation(AttackHash))
                    {
                        if (shouldContinueToChase()) SetState(EnemyState.Chase);
                        else SetState(EnemyState.Return);
                    }
                    break;
                case EnemyState.Hit:
                    if (!inAnimation(HitHash))
                    {
                        SetState(EnemyState.Chase);
                    }
                    break;
                default:return;

            }
            animator.SetFloat(SpeedHash, agent.velocity.magnitude, 0.1f, Time.deltaTime);
        }
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
            if (FaceTarget() <= attackAngle) SetState(EnemyState.Attack);
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
        private void updateChase()
        {
            if (canChase())
                SetState(EnemyState.Chase);

        }
        private bool inAnimation(int hash)
        {
            AnimatorStateInfo s = animator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo k = animator.GetNextAnimatorStateInfo(0);
            return s.tagHash == hash || (animator.IsInTransition(0) && k.tagHash == hash);
        }
        
        private void SetState(EnemyState updatingState)
        {
            State = updatingState;
            switch (updatingState)
            {
                case EnemyState.Chase:
                    agent.SetDestination(target.position);
                    agent.speed = runSpeed;
                    break;
                case EnemyState.Return:
                    agent.SetDestination(homePosition);
                    agent.speed = walkSpeed;
                    break;
                case EnemyState.Idle:
                    agent.ResetPath();
                    agent.speed = 0f;
                    break;
                case EnemyState.Attack:
                    animator.SetTrigger(AttackHash);
                    break;
                default:
                    return;

            }
            
        }

        public void enemyGetHit(CombatActionDefinition attack) 
        {
            Vector3 knowkBackDirection = -(target.position - transform.position);
            knowkBackDirection = knowkBackDirection.normalized;
            totalKnokback = knowkBackDirection * attack.knockBackPower;
            State = EnemyState.Hit;
            animator.SetTrigger(HitHash);
            if(!isInKnockBack)
                StartCoroutine(knockBackEnemy());
        }
        IEnumerator knockBackEnemy()
        {
            isInKnockBack = true;
            Vector3 start = transform.position;
            for(float step =0; step <1f;step +=Time.deltaTime/knockBackDuration)
            {
                Vector3 next = Vector3.Lerp(start, start+totalKnokback, step);
                agent.Move(next - transform.position);
                yield return null;
            }
            isInKnockBack = false;
            
        }
       

    }
}
