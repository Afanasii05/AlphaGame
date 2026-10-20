using Alpha.Combat;
using Alpha.Input;
using UnityEngine;
namespace Alpha.Player
{
    public class SprintFeel : MonoBehaviour
    {
        [SerializeField] private GameObject speedLinesPrefab;
        [SerializeField] private GameObject footDustPrefab;
        [SerializeField] private float fovBoost = 10f;
        [SerializeField] private float dustInterval = 0.28f;

        private InputHandler inputHandler;
        private Animator animator;
        private CapsuleCollider capsule;
        private ParticleSystem speedLines;
        private Vector2 moveInput;
        private bool sprintHeld;
        private bool crouchHeld;
        private bool wasActive;
        private float nextDust;

        private void Awake()
        {
            inputHandler = GetComponentInParent<InputHandler>();
            animator = inputHandler.GetComponentInChildren<Animator>();
            capsule = inputHandler.GetComponent<CapsuleCollider>();
        }

        private void Start()
        {
            if (speedLinesPrefab == null || Camera.main == null)
                return;
            GameObject lines = Instantiate(speedLinesPrefab, Camera.main.transform);
            lines.transform.localPosition = new Vector3(0f, 0f, 4f);
            speedLines = lines.GetComponent<ParticleSystem>();
            speedLines.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
            if (CombatFeedback.Instance != null)
                CombatFeedback.Instance.SetFovBoost(this, 0f);
            if (speedLines != null)
                speedLines.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            wasActive = false;
        }

        private void HandleSprint(bool pressed) { sprintHeld = pressed; }
        private void HandleCrouch(bool pressed) { crouchHeld = pressed; }
        private void HandleMove(Vector2 move) { moveInput = move; }

        private void Update()
        {
            bool grounded = animator.GetBool("IsGrounded");
            bool active = sprintHeld && !crouchHeld && grounded && moveInput.sqrMagnitude > 0.01f;
            if (CombatFeedback.Instance != null)
                CombatFeedback.Instance.SetFovBoost(this, active ? fovBoost : 0f);
            if (active != wasActive && speedLines != null)
            {
                if (active)
                    speedLines.Play(true);
                else
                    speedLines.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            wasActive = active;
            if (active && footDustPrefab != null && Time.time >= nextDust)
            {
                nextDust = Time.time + dustInterval;
                Destroy(Instantiate(footDustPrefab, PlayerFeet.Position(capsule), Quaternion.identity), 1.5f);
            }
        }
    }
}
