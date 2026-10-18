using System;
using UnityEngine;
using System.Collections;
using Alpha.Input;
using Alpha.Combat;
namespace Alpha.Player
{
    [SelectionBase] // click pe caracter in Scene selecteaza Player-ul (cel cu coliderul)
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
        [SerializeField] private float AttackSlowness = 0.3f;

        
        private float marginError = 0.5f;
        private bool isGrounded = false;
        private Vector3 wallNormal = Vector3.zero;
        private float maxVelocityY = 0f;
        private bool isJumping = false;
        private bool isSprinting = false;
        private bool isCrouching = false;
        private Vector2 moveInput = Vector2.zero;
        InputHandler inputHandler;
        Rigidbody rb;
        CapsuleCollider capsule;
        PlayerCombatState combState;
        [SerializeField] private float groundSnapDistance = 0.35f; 
        Animator anim;


        private void Awake()
        {
            anim = GetComponentInChildren<Animator>();
            inputHandler = GetComponent<InputHandler>();
            capsule = GetComponent<CapsuleCollider>();
            rb = GetComponent<Rigidbody>();
            combState = GetComponent<PlayerCombatState>();
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
        }
        
        private void OnEnable()
        {
            inputHandler.onCrouchHold += HandleCrouch;
            inputHandler.onSprintHold += HandleSprint;
            inputHandler.onJumpTriggered += HandleJump;
            inputHandler.onMoveUpdate += HandleMovementUpdate;

        }
        private void OnDisable()
        {
            inputHandler.onCrouchHold -= HandleCrouch;
            inputHandler.onSprintHold -= HandleSprint;
            inputHandler.onJumpTriggered -= HandleJump;
            inputHandler.onMoveUpdate -= HandleMovementUpdate;

        }
        void FixedUpdate()
        {
            if (rb.isKinematic)
                return;
            Vector3 moveDirectionAndMagnitude = GetMoveDirectionAndMagnitude();
            if (moveDirectionAndMagnitude != Vector3.zero || !cameraController.is3D)
                RotatePlayer();
            Vector3 velocity = new Vector3(moveDirectionAndMagnitude.x * moveSpeed, rb.linearVelocity.y, moveDirectionAndMagnitude.z * moveSpeed);
            if (Vector3.Dot(velocity, wallNormal) < 0) //Nu imping in perete, altfel fizica ma urca pe el
                velocity -= wallNormal * Vector3.Dot(velocity, wallNormal);
            bool hasGround = CheckGround(out RaycastHit groundHit, out float groundGap);
            if (!isJumping && hasGround != isGrounded) //Grounded = am sol sub picioare acum, nu doar o coliziune veche
            {
                isGrounded = hasGround;
                anim.SetBool("IsGrounded", isGrounded);
            }
            if (isGrounded && !isJumping)
            {
                velocity = Vector3.ProjectOnPlane(velocity, groundHit.normal); //La coborare urmaresc panta
                if (groundGap > 0.02f) //Am coborat de pe o treapta mica: lipesc playerul de sol, nu-l las sa pluteasca
                    velocity.y = Mathf.Min(velocity.y, -groundGap / Time.fixedDeltaTime * 0.5f);
            }
            if (isGrounded && !isJumping && velocity.y > 0) //Nu decolez de pe varful pantelor
                velocity.y = 0;

            if (isGrounded)
            {
                maxVelocityY = 10000;
            }
            else //In aer viteza in sus doar scade, nu ma mai ridica obiectele atinse
            {
                velocity.y = Mathf.Min(velocity.y, maxVelocityY);
                maxVelocityY = Mathf.Max(velocity.y, 0f);
            }
            rb.linearVelocity = velocity;
            wallNormal = Vector3.zero;
            if(rb.linearVelocity.y < 0)
                handleFall();

        }
       
        private void HandleMovementUpdate(Vector2 moveVector){ moveInput = moveVector;}
        private void HandleSprint(bool sprintPressed){isSprinting = sprintPressed;}
        private void HandleCrouch(bool crouchPressed)
        {
            isCrouching = crouchPressed;
            anim.SetBool("Crouch", isCrouching);
        }
        
        private bool CheckGround(out RaycastHit hit, out float gap) //Sol sub capsula, la cel mult groundSnapDistance de baza ei
        {
            gap = 0f;
            float castRadius = capsule.radius * 0.9f;
            Vector3 center = transform.TransformPoint(capsule.center);
            float restDistance = capsule.height * 0.5f - castRadius; //cat se deplaseaza sfera pana atinge solul cand stau pe el
            if (Physics.SphereCast(center, castRadius, Vector3.down, out hit, restDistance + groundSnapDistance, ~0, QueryTriggerInteraction.Ignore)
                && hit.normal.y >= marginError)
            {
                gap = Mathf.Max(0f, hit.distance - restDistance);
                return true;
            }
            return false;
        }
        private void handleFall(){rb.AddForce(Vector3.down * gravityMultiplier, ForceMode.Acceleration);}
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
            maxVelocityY = jumpForce / rb.mass;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, maxVelocityY, rb.linearVelocity.z); //Acelasi efect ca impulsul, dar fara viteza adunata de pe panta
            yield return new WaitForSeconds(0.2f);
            isJumping = false;

        }
        
        private Vector3 GetMoveDirectionAndMagnitude()
        {
            if (moveInput.magnitude == 0 || combState.inPlaceAction())//Daca stau pe loc sau rulez o aniamtie care trebuie ea sa stea pe loc
            { 
                anim.SetFloat("Speed", 0f);
                return Vector3.zero;
            } 

            Vector3 cameraForward = cameraTransform.forward;//Directia camerei pentru a misca playerul relativ la rotatia camerei
            Vector3 cameraRight = cameraTransform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();
            float speedMultiplier = 1f;
            if (isSprinting && !isCrouching)
                speedMultiplier *= sprintMultiplier;
            else if(isCrouching)
                speedMultiplier *= crouchMultiplier;
            if (combState.inSlowDownAction())
                speedMultiplier *= (1 - AttackSlowness);
            Vector3 moveDirection = cameraForward * moveInput.y + cameraRight*moveInput.x;
            anim.SetFloat("Speed", moveInput.magnitude * moveSpeed * speedMultiplier);
            return moveDirection * speedMultiplier;

        }

        private void RotatePlayer()
        {
            float yAngle = cameraTransform.eulerAngles.y;
            Quaternion targetRotation = Quaternion.Euler(0f, yAngle, 0f);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 0.1f));

        }
             
        
        private void OnCollisionStay(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                if (contact.normal.y < marginError) //Perete sau panta prea abrupta
                    wallNormal = new Vector3(contact.normal.x, 0, contact.normal.z).normalized;
                //Starea grounded se decide in CheckGround (FixedUpdate), nu aici
            }
        }
        private void OnCollisionExit(Collision collision)
        {
            if (isJumping)
            {
                isGrounded = false;
                anim.SetBool("IsGrounded", false);
            }
           
        }
     
    }
}