using UnityEngine;
using Alpha.Input;
namespace Alpha.Player
{
    [RequireComponent(typeof(InputHandler))]
    public class PlayerDirectionalAnimation : MonoBehaviour
    {
        [SerializeField] private Transform model;

        [SerializeField] private float sideTurnAngle = 90f;
        [SerializeField] private float backTurnAngle = 40f;
        [SerializeField] private float turnSmoothTime = 0.12f;
        [SerializeField] private float directionBlendTime = 0.1f;
        private InputHandler inputHandler;
        private CameraController cameraController;
        private Animator anim;
        private Vector2 moveInput;
        private bool isBackward;
        private float yRotation;
        private float yawVelocity;
        private float targetYRotation=0f;
        float target = 1f;
        private void Awake()
        {
            inputHandler = GetComponent<InputHandler>();
            anim = model != null ? model.GetComponent<Animator>() : GetComponentInChildren<Animator>();
            if (model == null && anim != null) model = anim.transform;
            if (Camera.main != null) cameraController = Camera.main.GetComponent<CameraController>();
        }

        private void OnEnable() { inputHandler.onMoveUpdate += HandleMove; }
        private void OnDisable() { inputHandler.onMoveUpdate -= HandleMove; }
        private void HandleMove(Vector2 v) { moveInput = v; }

        private void Update()
        {
            bool isMoving = moveInput.sqrMagnitude > 0.01f;
            bool is3D = cameraController == null || cameraController.is3D;
            targetYRotation = 0f;                       
            if (isMoving)
            {
                Vector2 dir = moveInput.normalized;
                isBackward = dir.y < 0f;
                if (!is3D)
                    targetYRotation = 0f;
                else if (isBackward)
                {
                    targetYRotation = Mathf.Clamp(Mathf.Atan2(-dir.x, -dir.y) * Mathf.Rad2Deg, -backTurnAngle, backTurnAngle);
                    target = -1;
                }
                else
                {
                    targetYRotation = Mathf.Clamp(Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg, -sideTurnAngle, sideTurnAngle);
                    target = 1;
                }
            }
            yRotation = Mathf.SmoothDampAngle(yRotation, targetYRotation, ref yawVelocity, turnSmoothTime);
            model.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            if (isMoving) anim.SetFloat("MoveForward", target, directionBlendTime, Time.deltaTime);
            
        }
    }
}
