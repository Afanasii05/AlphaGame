using System;
using UnityEngine;
using UnityEngine.AI;
using Alpha.Interfaces;

namespace Alpha.Enemies
{
    public enum EnemyState { Idle, Chase, Return, Dead }

    //Doar detectie si pathfinding. Ce face inamicul cand ajunge la tinta decide EnemyController.
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyCombatState))]
    public class EnemyAI : MonoBehaviour
    {
        [SerializeField]bool Ally;
        [SerializeField] private Animator animator;
        [SerializeField] public Transform target;         
        public EnemyState State = EnemyState.Idle;
        public NavMeshAgent agent;
        private Vector3 homePosition;
        

        //Valori pentru detectie
        [SerializeField] float maxDistanceToDetectFromHome = 15f;
        [SerializeField] float maxAngleToBeSeen = 120f;
        [SerializeField] float detectDistance = 10f;
        [SerializeField] float loseDistance = 15f;
        [SerializeField] float distanceFromHome= 30f;
        [SerializeField] float attackRange = 2.65f;   //aici se opreste si anunta controllerul
        [SerializeField] float attackAngle = 70;
        [SerializeField] float timeToAskForTarget = 0.2f;

        //Miscare
        [SerializeField] float runSpeed = 3f;
        [SerializeField] float walkSpeed = 0.53f;
        [SerializeField] float turnSpeed = 1f;
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private float sqrDistToPlayer;
        private float getCloserTimer;
        private float lastTimeAskedForTarget;
        private float lastTimePatrulate;
        EnemyCombatState enemyCombatState;
        NpcAttributes npcAtrib;

        //Trimise catre controller:
        public event Action onTargetInAttackRange;   //tinta e in raza si in fata
        public event Action onTargetInLateralAttack; //Daca nu e in fata,dau stomp
        public event Action onCutDistanceToPlayer;
        //Pentru roll sau scurtre de distanta
        [SerializeField] float minRollDistance = 3f;
        [SerializeField] float maxRollDistance = 10f;
        const float rollCheckDistance = 2.5f; //cat verific in fata pe NavMesh inainte de roll
        [SerializeField] float randomDistanceFromHome = 5f;
        [SerializeField] float timeBetweenPatrulate = 10f;
        
        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            homePosition = transform.position;
            enemyCombatState = GetComponent<EnemyCombatState>();
            npcAtrib = GetComponent<NpcAttributes>();
        }
        void Update()
        {
            if (State == EnemyState.Dead) return;
            updateTarget();
            if (target == null)   //nu exista nicio tinta activa
            {
                if (agent.isOnNavMesh) agent.isStopped = true;
                animator.SetFloat(SpeedHash, 0f, 0.1f, Time.deltaTime);
                return;
            }
            sqrDistToPlayer = (target.position - transform.position).sqrMagnitude;

            if (enemyCombatState.IsBusy || npcAtrib.isRecovering)
                agent.isStopped = true;   //cat ruleaza o actiune nu ma misc sau cat e in starea de recover
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
                        if (canChase()) SetState(EnemyState.Chase);
                        else if (determineIddle()) SetState(EnemyState.Idle);
                        else if (!agent.hasPath && !agent.pathPending) agent.SetDestination(homePosition);
                        break;
                }
            }
            animator.SetFloat(SpeedHash, agent.velocity.magnitude, 0.1f, Time.deltaTime);
        }
        public void Aggro()
        {
            if (target == null) updateTargetNow();
            if (target == null) return;
            if (State != EnemyState.Dead && State != EnemyState.Chase) SetState(EnemyState.Chase);
        }
        private void patrulate()
        {
            if(Time.time - lastTimePatrulate - UnityEngine.Random.value * timeBetweenPatrulate < timeBetweenPatrulate) return;
            Vector3 randomPoint = homePosition + UnityEngine.Random.insideUnitSphere * randomDistanceFromHome;
            agent.isStopped = false;
            agent.speed = walkSpeed;
            agent.SetDestination(randomPoint);
            lastTimePatrulate = Time.time;
        }
        private bool determineIddle() => Vector3.Distance(homePosition, transform.position) <= agent.stoppingDistance + 0.1f;
        private void UpdateChase()
        {
            if (!shouldContinueToChase()) { SetState(EnemyState.Return); return; }
            if (sqrDistToPlayer > attackRange * attackRange)
            {
                if (Time.time - getCloserTimer >= 0.2)
                {
                    agent.isStopped = false;
                    agent.SetDestination(target.position);
                    agent.speed = runSpeed;
                    getCloserTimer = Time.time;
                }
                if (canCutDistanceTwardsPlayer())
                    onCutDistanceToPlayer?.Invoke();
                return;
            }

            agent.isStopped = true;
            if (FaceTarget() <= attackAngle) onTargetInAttackRange?.Invoke();
            else onTargetInLateralAttack?.Invoke();
        }
        private float FaceTarget()
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
            return Vector3.Angle(transform.forward, dir);
        }
        private bool canCutDistanceTwardsPlayer()
        {
            float distFromTarget = Vector3.Distance(transform.position, target.position);
            if (distFromTarget <= minRollDistance || distFromTarget >= maxRollDistance || FaceTarget() >= attackAngle) return false;
            return !NavMesh.Raycast(transform.position, transform.position + transform.forward * rollCheckDistance, out _, NavMesh.AllAreas);
               
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
            {
                SetState(EnemyState.Chase);
                return;
            }
            patrulate();

        }

        private void updateTarget()
        {
            if(Time.time - lastTimeAskedForTarget < timeToAskForTarget) return;
            updateTargetNow();
        }
        private void updateTargetNow()
        {
            lastTimeAskedForTarget = Time.time;
            if (Ally)
                target = HittableCharacter.Closest(HittableCharacter.enemies, transform.position)?.transform;
            else
                target = HittableCharacter.Closest(HittableCharacter.allys, transform.position)?.transform;
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
