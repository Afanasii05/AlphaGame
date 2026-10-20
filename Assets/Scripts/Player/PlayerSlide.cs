using Alpha.Combat;
using Alpha.Input;
using UnityEngine;
namespace Alpha.Player
{
    [DefaultExecutionOrder(100)]
    public class PlayerSlide : MonoBehaviour
    {
        [SerializeField] private GameObject dustTrailPrefab;
        [SerializeField] private float slideSpeed = 12f;
        [SerializeField] private float endSpeed = 3f;
        [SerializeField] private float slideDuration = 0.75f;
        [SerializeField] private float slideCooldown = 0.5f;

        private InputHandler inputHandler;
        private PlayerCombatState combatState;
        private Animator animator;
        private Rigidbody body;
        private CapsuleCollider capsule;
        private Vector2 moveInput;
        private Vector3 slideDirection;
        private ParticleSystem dustTrail;
        private bool sprintHeld;
        private bool crouchHeld;
        private bool sliding;
        private float slideStart;
        private float nextSlide;

        private void Awake()
        {
            inputHandler = GetComponentInParent<InputHandler>();
            combatState = inputHandler.GetComponent<PlayerCombatState>();
            animator = inputHandler.GetComponentInChildren<Animator>();
            body = inputHandler.GetComponent<Rigidbody>();
            capsule = inputHandler.GetComponent<CapsuleCollider>();
        }

        private void OnEnable()
        {
            inputHandler.onSprintHold += HandleSprint;
            inputHandler.onCrouchHold += HandleCrouch;
            inputHandler.onMoveUpdate += HandleMove;
        }

        private void OnDisable()
        {
            inputHandler.onSprintHold -= HandleSprint;
            inputHandler.onCrouchHold -= HandleCrouch;
            inputHandler.onMoveUpdate -= HandleMove;
            if (sliding)
                StopSlide();
        }

        private void HandleMove(Vector2 move) { moveInput = move; }

        private void HandleSprint(bool pressed)
        {
            sprintHeld = pressed;
            if (pressed)
                TryStartSlide();
        }

        private void HandleCrouch(bool pressed)
        {
            crouchHeld = pressed;
            if (pressed)
                TryStartSlide();
        }

        private void TryStartSlide()
        {
            if (sliding || !sprintHeld || !crouchHeld || Time.time < nextSlide)
                return;
            if (body.isKinematic || moveInput.sqrMagnitude < 0.01f || !animator.GetBool("IsGrounded"))
                return;
            CombatAction action = combatState.CurrentAction();
            if (action != CombatAction.None && action != CombatAction.NonCombat && action != CombatAction.NonSemiCombat)
                return;
            Vector3 velocity = body.linearVelocity;
            velocity.y = 0f;
            if (velocity.sqrMagnitude < 1f) //Fara viteza orizontala nu am in ce directie sa alunec
                return;
            slideDirection = velocity.normalized;
            sliding = true;
            slideStart = Time.time;
            if (dustTrailPrefab != null)
            {
                GameObject trail = Instantiate(dustTrailPrefab, body.transform);
                trail.transform.position = PlayerFeet.Position(capsule);
                dustTrail = trail.GetComponent<ParticleSystem>();
            }
            if (CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.SetFovBoost(this, 8f);
                CombatFeedback.Instance.Shake(0.06f, 0.12f);
            }
        }

        private void StopSlide()
        {
            sliding = false;
            nextSlide = Time.time + slideCooldown;
            if (dustTrail != null)
            {
                dustTrail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                dustTrail.transform.SetParent(null, true);
                Destroy(dustTrail.gameObject, 2f);
                dustTrail = null;
            }
            if (CombatFeedback.Instance != null)
                CombatFeedback.Instance.SetFovBoost(this, 0f);
        }

        private void FixedUpdate()
        {
            if (!sliding)
                return;
            float t = (Time.time - slideStart) / slideDuration;
            if (t >= 1f || !crouchHeld || body.isKinematic || !animator.GetBool("IsGrounded"))
            {
                StopSlide();
                return;
            }
            Vector3 velocity = slideDirection * Mathf.Lerp(slideSpeed, endSpeed, t);
            velocity.y = body.linearVelocity.y;
            body.linearVelocity = velocity;
            animator.SetFloat("Speed", 0f); //Dupa PlayerController: animatorul ramane in pozitia de crouch
        }
    }
}
