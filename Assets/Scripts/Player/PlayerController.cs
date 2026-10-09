using UnityEngine;

namespace Alpha.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(InputHandler))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour
    {
       
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float gravityMultiplier = 40f;
        private float marginError = 0.1f;
        private bool isGrounded = false;
        InputHandler inputHandler;
        Rigidbody rb;
        Animator anim;

        private void Awake()
        {
            anim = GetComponentInChildren<Animator>();
            inputHandler = GetComponent<InputHandler>();
            rb = GetComponent<Rigidbody>();
        }
        void Start()
        {

        }

        void FixedUpdate()
        {
            Vector2 moveInput = inputHandler.move;
            anim.SetFloat("Speed", moveInput.magnitude * moveSpeed);
            anim.SetBool("IsGrounded", isGrounded);
            rb.linearVelocity = new Vector3(moveInput.x * moveSpeed, rb.linearVelocity.y, moveInput.y * moveSpeed);
            if(inputHandler.jump && isGrounded)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                isGrounded = false;
            }
            if(inputHandler.crouch && !isGrounded)
            {
                rb.AddForce(Vector3.down * gravityMultiplier, ForceMode.Impulse);
            }
        }
        private void OnCollisionStay(Collision collision)
        {
            foreach(ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y >= marginError)
                {
                    if(!isGrounded)
                        anim.CrossFade("Idle", 0.05f);
                    isGrounded = true;
                }
            }
        }
        private void OnCollisionExit(Collision collision)
        {
            isGrounded = false;
            anim.CrossFade("Jump", 0.05f);
        }
    }
}