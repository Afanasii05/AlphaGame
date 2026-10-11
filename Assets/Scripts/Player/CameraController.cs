using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Alpha.Player
{
    public class CameraController : MonoBehaviour
    {

        // Update is called once per frame
        [SerializeField] private float mouseSensitivity = 0.1f;
        [SerializeField] private float distance = 5f;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private InputHandler inputHandler;
        public bool is3D { get; private set; } = true;
        private float xAngle = 0f;
        private float yAngle = 0f;
        private float yShoulder = 1.5f;
        private float maxDownAngleDown= 80f;
        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        void Update()
        {
            if (Keyboard.current.pKey.wasReleasedThisFrame)
                toggleCameraPerspective();
            if (Mouse.current != null)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                xAngle += mouseDelta.x * mouseSensitivity;
                yAngle -= mouseDelta.y * mouseSensitivity;
                if (is3D)
                    maxDownAngleDown = 89f;
                else
                    maxDownAngleDown = 70f;
                yAngle = Mathf.Clamp(yAngle, -89.9f, maxDownAngleDown); 

            }
        }
        private void LateUpdate()
        {
            if(playerTransform == null) return;
            
            Quaternion rotation = Quaternion.Euler(yAngle, xAngle, 0f); // toata rotatia camerei pe ambele axe in quaternion
            Vector3 target = playerTransform.position + new Vector3(0,yShoulder, 0);
            Vector3 backDirection = rotation * Vector3.back; // extrag forward si il inversez apoi din pozitia camerei extrag invers pentru o rotatie in jurul playerului a camerei
            transform.position = target + backDirection * distance;
            transform.rotation = rotation; //rotatia in sine a camerei
        }
        private void toggleCameraPerspective()
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