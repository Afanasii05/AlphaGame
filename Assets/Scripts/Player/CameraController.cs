using UnityEngine;
using Alpha.Input;

namespace Alpha.Player
{
    public class CameraController : MonoBehaviour
    {


        [SerializeField] private float mouseSensitivity = 0.1f;
        [SerializeField] public float distance = 5f;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private InputHandler inputHandler;

        public bool is3D { get; private set;} = true;
        public float xAngle;
        public float yAngle;
        private float yShoulder = 1.5f;
        private float maxDownAngleDown= 80f;
        private Vector2 lookDelta;          // miscarea mouse-ului din frame-ul curent
        private int lookFrame = -1;

        void Start()
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
            Vector3 target = playerTransform.position + Vector3.up * yShoulder;
            Quaternion playerRor = playerTransform.rotation;
            transform.rotation = playerRor;
            transform.position = -transform.forward * distance + target;
            xAngle = playerTransform.eulerAngles.y;
        }
        private void OnEnable()
        {
            inputHandler.onMouseMoveUpdate += HandleMouseRotation;
            inputHandler.onTogglePerspective += HandlePerspective;
        }
        private void OnDisable()
        {
            inputHandler.onMouseMoveUpdate -= HandleMouseRotation;
            inputHandler.onTogglePerspective -= HandlePerspective;
        }

        private void LateUpdate()
        {
            if (lookFrame == Time.frameCount)   // rotatia din mouse se aplica o singura data pe frame( altfel am bug de flicker)
                ApplyMouseRotation(lookDelta);
            Vector3 target = playerTransform.position + Vector3.up * yShoulder;
            transform.position = -transform.forward * distance + target;
        }
        
        private void HandleMouseRotation(Vector2 mouseVector) {
            lookDelta = mouseVector;            // ultimul apel din frame contine totalul miscarii mouse-ului
            lookFrame = Time.frameCount;
        }
        private void ApplyMouseRotation(Vector2 mouseVector) {
            xAngle += mouseVector.x * mouseSensitivity;
            yAngle -= mouseVector.y * mouseSensitivity;
            if (is3D)
                maxDownAngleDown = 89f;
            else
                maxDownAngleDown = 70f;
            yAngle = Mathf.Clamp(yAngle, -89.9f, maxDownAngleDown);
            Quaternion rotation = Quaternion.Euler(yAngle, xAngle, 0f); // toata rotatia camerei pe ambele axe in quaternion
            transform.rotation = rotation; //rotatia in sine a camerei
        }
        private void HandlePerspective()
        {
            is3D = !is3D;
            if (is3D)
            {
                distance = 5f;
                yShoulder = 1.5f;
            }
            else
            {
                distance = -0.5f;
                yShoulder = 1f;
            }
        }
        

    }

}