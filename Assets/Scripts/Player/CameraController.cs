using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Alpha.Player
{
    public class CameraController : MonoBehaviour
    {

        
        [SerializeField] private float mouseSensitivity = 0.1f;
        [SerializeField] private float distance = 5f;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private float lookAroundAngle = 20f;

        public bool is3D { get; private set; } = true;
        private float xAngle = 0f;
        private float yAngle = 0f;
        private float yShoulder = 1.5f;
        private float maxDownAngleDown= 80f;

        void Start()
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
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
            Vector3 target = playerTransform.position + Vector3.up * yShoulder;
            transform.position = -transform.forward * distance + target;
        }
        
        private void HandleMouseRotation(Vector2 mouseVector) {
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