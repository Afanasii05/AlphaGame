using Alpha.Input;
using UnityEngine;
namespace Alpha.Player
{
    //Mareste FOV-ul camerei cat timp playerul alearga pe sol si il readuce lin la valoarea initiala.
    public class SprintFeel : MonoBehaviour
    {
        [SerializeField] private float fovBoost = 10f;
        [SerializeField] private float blendSpeed = 8f;

        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

        private InputHandler inputHandler;
        private Animator animator;
        private Camera cam;
        private float baseFov;
        private float currentBoost;
        private Vector2 moveInput;
        private bool sprintHeld;
        private bool crouchHeld;

        private void Awake()
        {
            inputHandler = GetComponentInParent<InputHandler>();
            animator = inputHandler.GetComponentInChildren<Animator>();
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
            if (cam != null)
                cam.fieldOfView = baseFov;
            currentBoost = 0f;
        }

        private void HandleSprint(bool pressed) { sprintHeld = pressed; }
        private void HandleCrouch(bool pressed) { crouchHeld = pressed; }
        private void HandleMove(Vector2 move) { moveInput = move; }

        private void LateUpdate()
        {
            if (cam == null)
            {
                cam = Camera.main;
                if (cam == null)
                    return;
                baseFov = cam.fieldOfView;
            }
            bool active = sprintHeld && !crouchHeld && moveInput.sqrMagnitude > 0.01f && animator.GetBool(IsGroundedHash);
            float target = active ? fovBoost : 0f;
            currentBoost = Mathf.Lerp(currentBoost, target, 1f - Mathf.Exp(-blendSpeed * Time.deltaTime));
            cam.fieldOfView = baseFov + currentBoost;
        }
    }
}
