using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Alpha.Player
{
    public class InputHandler : MonoBehaviour
    {
        //KEY EVENTS
        public event Action<bool> onCrouchHold; //LEFT CTRL  CROUCH
        public event Action<bool> onSprintHold; //LEFT SHIFT SPRINT
        public event Action onJumpTriggered;    //SPACE      JUMP
        
        public Vector2 move { get; private set; }
        public Vector2 mouseMoveDelta { get; private set; } = Vector2.zero;
        void Start()
        {

        }
        //INPUT SYSTEM EVENTS
        public void onCrouch(InputValue value){onCrouchHold?.Invoke(value.isPressed); } //CROUCH 
        public void OnSprint(InputValue value){onSprintHold?.Invoke(value.isPressed); } //SPRINT
        public void OnJump(InputValue value) {if(value.isPressed) onJumpTriggered?.Invoke(); }              //JUMP

        void Update()
        {
            //AFK 
            if (Keyboard.current == null)
            {
                move = Vector2.zero;
                return;
            }

            //LEFT SHIFT = CROUCH
            if (Keyboard.current.leftShiftKey.wasPressedThisFrame)
                onCrouchHold?.Invoke(true);
            else if(Keyboard.current.leftShiftKey.wasReleasedThisFrame)
                onCrouchHold?.Invoke(false);
            //SPACE = JUMP
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
                onJumpTriggered?.Invoke();

            //WASD = MOVE
            float xVelocity =0, zVelocity=0;
            if(Keyboard.current.wKey.isPressed)
                zVelocity += 1;
            if (Keyboard.current.sKey.isPressed)
                zVelocity -= 1;
            if(Keyboard.current.aKey.isPressed)
                xVelocity -= 1;
            if(Keyboard.current.dKey.isPressed)
                xVelocity += 1;
            //LEFT CTRL = SPRINT

            move = new Vector2(xVelocity, zVelocity).normalized;
            mouseMoveDelta = Vector2.zero;
            if (Mouse.current != null)
                mouseMoveDelta = Mouse.current.delta.ReadValue();
        }
    }
}