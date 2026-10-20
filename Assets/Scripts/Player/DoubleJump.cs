using Alpha.Combat;
using Alpha.Input;
using UnityEngine;
namespace Alpha.Player
{
    [DefaultExecutionOrder(100)]
    public class DoubleJump : MonoBehaviour
    {
        [SerializeField] private GameObject jumpRingPrefab;
        [SerializeField] private float jumpSpeed = 12f;
        [SerializeField] private float gravity = 30f;
        [SerializeField] private float minAirTime = 0.15f;

        private InputHandler inputHandler;
        private PlayerCombatState combatState;
        private Animator animator;
        private Rigidbody body;
        private CapsuleCollider capsule;
        private bool used;
        private bool boosting;
        private float boostStart;
        private float airStart;

        private void Awake()
        {
            inputHandler = GetComponentInParent<InputHandler>();
            combatState = inputHandler.GetComponent<PlayerCombatState>();
            animator = inputHandler.GetComponentInChildren<Animator>();
            body = inputHandler.GetComponent<Rigidbody>();
            capsule = inputHandler.GetComponent<CapsuleCollider>();
        }

        private void OnEnable() { inputHandler.onJumpTriggered += HandleJump; }

        private void OnDisable()
        {
            inputHandler.onJumpTriggered -= HandleJump;
            boosting = false;
        }

        private void HandleJump()
        {
            if (used || body.isKinematic || animator.GetBool("IsGrounded") || Time.time - airStart < minAirTime)
                return;
            CombatAction action = combatState.CurrentAction();
            if (action != CombatAction.None && action != CombatAction.NonCombat)
                return;
            used = true;
            boosting = true;
            boostStart = Time.time;
            animator.CrossFadeInFixedTime("Jump ", 0.05f, 0, 0.1f); //Numele starii are spatiu la final
            if (jumpRingPrefab != null)
                Destroy(Instantiate(jumpRingPrefab, PlayerFeet.Position(capsule), Quaternion.identity), 1.5f);
            if (CombatFeedback.Instance != null)
                CombatFeedback.Instance.FovKick(5f);
        }

        private void Update()
        {
            if (animator.GetBool("IsGrounded"))
            {
                used = false;
                boosting = false;
                airStart = Time.time; //Timpul in aer se numara de la ultima atingere a solului
            }
        }

        private void FixedUpdate()
        {
            if (!boosting)
                return;
            float speed = jumpSpeed - gravity * (Time.time - boostStart);
            if (speed <= 0f || body.isKinematic)
            {
                boosting = false;
                return;
            }
            Vector3 velocity = body.linearVelocity;
            velocity.y = speed; //PlayerController limiteaza viteza in sus in aer, o rescriu in fiecare pas
            body.linearVelocity = velocity;
        }
    }
}
