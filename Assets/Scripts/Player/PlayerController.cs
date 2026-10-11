using System;
using UnityEngine;
using System.Collections;
namespace Alpha.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(InputHandler))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour
    {

        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float sprintMultiplier = 2.5f;
        [SerializeField] private float crouchMultiplier = 0.1f;
        [SerializeField] private float jumpForce = 15f;
        [SerializeField] private float gravityMultiplier = 40f;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private Transform cameraTransform;

        
        private float marginError = 0.5f;
        private bool isGrounded = false;
        private bool isJumping = false;
        private bool isSprinting = false;
        private bool isCrouching = false;
        InputHandler inputHandler;
        Rigidbody rb;
        Animator anim;


        private void Awake()
        {
            anim = GetComponentInChildren<Animator>();
            inputHandler = GetComponent<InputHandler>();
            rb = GetComponent<Rigidbody>();
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
        }
        
        private void OnEnable()
        {
            inputHandler.onCrouchHold += HandleCrouch;
            inputHandler.onSprintHold += HandleSprint;
            inputHandler.onJumpTriggered += HandleJump;

        }
        private void OnDisable()
        {
            inputHandler.onCrouchHold -= HandleCrouch;
            inputHandler.onSprintHold -= HandleSprint;
            inputHandler.onJumpTriggered -= HandleJump;

        }
        void FixedUpdate()
        {

            Vector3 moveDirection = GetMoveDirection();
            if (moveDirection != Vector3.zero || !cameraController.is3D)
                RotatePlayer();
            rb.linearVelocity = new Vector3(moveDirection.x * moveSpeed, rb.linearVelocity.y, moveDirection.z * moveSpeed);
            if(rb.linearVelocity.y < 0)
                handleFall();

        }
        private void HandleSprint(bool sprintPressed){isSprinting = sprintPressed;}
        private void HandleCrouch(bool crouchPressed)
        {
            isCrouching = crouchPressed;
            anim.SetBool("Crouch", isCrouching);
        }
        
        private void handleFall()
        {
            rb.AddForce(Vector3.down * gravityMultiplier, ForceMode.Acceleration);
        }
        private void HandleJump()
        {
            if (isGrounded && !isCrouching && !isJumping)
            {
                StartCoroutine(JumpRoutine());
            }
        }
        private IEnumerator JumpRoutine()
        {
            anim.SetBool("IsGrounded", false);
            isGrounded = false;
            isJumping = true;
            yield return new WaitForSeconds(0.15f);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            yield return new WaitForSeconds(0.05f);
            isJumping = false;

        }
        
        private Vector3 GetMoveDirection()
        {
            Vector2 moveInput = inputHandler.move;//Inputul de la tastatura 
            if (moveInput.magnitude == 0)//Daca stau pe loc
            { anim.SetFloat("Speed", 0f);
                return Vector3.zero;
            }
            moveInput.Normalize();
            Vector3 cameraForward = cameraTransform.forward;//Directia camerei pentru a misca playerul relativ la rotatia camerei
            Vector3 cameraRight = cameraTransform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();
            if (isSprinting && !isCrouching)
                moveInput *= sprintMultiplier;
            else if(isCrouching)
                moveInput *= crouchMultiplier;

            Vector3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;
            anim.SetFloat("Speed", moveInput.magnitude * moveSpeed);
            Debug.Log($"Speed: {moveInput.magnitude * moveSpeed}");
            return moveDirection;

        }

        private void RotatePlayer()
        {  
            float yAngle = cameraTransform.eulerAngles.y;
            Quaternion targetRotation = Quaternion.Euler(0f, yAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.1f);
            
        }
        
        
        private void OnCollisionStay(Collision collision)
        {
            if (rb.linearVelocity.y > 0.1f || isJumping) return; //Daca abia am sarit nu updatez starea grounded
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y >= marginError)
                {
                    if (!isGrounded)
                    {
                        anim.SetBool("IsGrounded", true);
                        isGrounded = true;
                    }
                    break;
                }
            }
        }
        private void OnCollisionExit(Collision collision)
        {
            isGrounded = false;
            anim.SetBool("IsGrounded", false); 
           
        }
     
    }
}