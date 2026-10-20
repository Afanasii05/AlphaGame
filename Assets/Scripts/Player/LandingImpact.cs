using System.Collections;
using Alpha.Combat;
using Alpha.Input;
using UnityEngine;
namespace Alpha.Player
{
    public class LandingImpact : MonoBehaviour
    {
        [SerializeField] private GameObject dustRingPrefab;
        [SerializeField] private GameObject footDustPrefab;
        [SerializeField] private float minAirTime = 0.3f;
        [SerializeField] private float heavyLandSpeed = 12f;
        [SerializeField] private float squatLandSpeed = 20f;
        [SerializeField] private float squatTime = 0.22f;

        private InputHandler inputHandler;
        private Animator animator;
        private Rigidbody body;
        private CapsuleCollider capsule;
        private bool wasGrounded = true;
        private bool crouchHeld;
        private float airStart;
        private float minVelocityY;

        private void Awake()
        {
            inputHandler = GetComponentInParent<InputHandler>();
            animator = inputHandler.GetComponentInChildren<Animator>();
            body = inputHandler.GetComponent<Rigidbody>();
            capsule = inputHandler.GetComponent<CapsuleCollider>();
        }

        private void OnEnable() { inputHandler.onCrouchHold += HandleCrouch; }
        private void OnDisable() { inputHandler.onCrouchHold -= HandleCrouch; }

        private void HandleCrouch(bool pressed) { crouchHeld = pressed; }

        private void FixedUpdate()
        {
            if (!animator.GetBool("IsGrounded"))
                minVelocityY = Mathf.Min(minVelocityY, body.linearVelocity.y);
        }

        private void Update()
        {
            bool grounded = animator.GetBool("IsGrounded");
            if (wasGrounded && !grounded)
            {
                airStart = Time.time;
                minVelocityY = 0f;
            }
            else if (!wasGrounded && grounded)
                Land(Time.time - airStart);
            wasGrounded = grounded;
        }

        private void Land(float airTime)
        {
            if (airTime <= minAirTime || animator.GetBool("AirSlam")) //Plonjonul are propriul efect de aterizare
                return;
            Vector3 feet = PlayerFeet.Position(capsule);
            float fallSpeed = -minVelocityY;
            if (fallSpeed > heavyLandSpeed)
            {
                if (dustRingPrefab != null)
                    Destroy(Instantiate(dustRingPrefab, feet, Quaternion.identity), 2f);
                if (CombatFeedback.Instance != null)
                {
                    CombatFeedback.Instance.Shake(Mathf.Clamp(fallSpeed * 0.012f, 0.05f, 0.35f), 0.25f);
                    CombatFeedback.Instance.FovKick(-4f);
                }
                if (fallSpeed > squatLandSpeed)
                    StartCoroutine(LandingSquat());
            }
            else if (footDustPrefab != null)
                Destroy(Instantiate(footDustPrefab, feet, Quaternion.identity), 1.5f);
        }

        private IEnumerator LandingSquat()
        {
            animator.SetBool("Crouch", true);
            yield return new WaitForSeconds(squatTime);
            animator.SetBool("Crouch", crouchHeld); //Revin la starea reala a inputului
        }
    }
}
