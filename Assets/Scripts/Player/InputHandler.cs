using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Alpha.Player
{
    public class InputHandler : MonoBehaviour
    {
        //KEY EVENTS
        public event Action<bool> onCrouchHold;             //LEFT CTRL  CROUCH
        public event Action<bool> onSprintHold;             //LEFT SHIFT SPRINT
        public event Action onJumpTriggered;                //SPACE      JUMP
        public event Action onTogglePerspective;            //P          CAMERA PERSPECTIVE
        public event Action<Vector2> onMouseMoveUpdate;     //MOUSE      LOOK
        public event Action<Vector2> onMoveUpdate;            //WASD       MOVE

        //INPUT SYSTEM EVENTS
        public void OnCrouch(InputValue value){onCrouchHold?.Invoke(value.isPressed); }                 //CROUCH 
        public void OnSprint(InputValue value){onSprintHold?.Invoke(value.isPressed); }                 //SPRINT
        public void OnJump(InputValue value)  {if(value.isPressed) onJumpTriggered?.Invoke(); }         //JUMP
        public void OnMovePlayer(InputValue value) {onMoveUpdate?.Invoke(value.Get<Vector2>()); }         //MOVE  
        public void OnLook(InputValue value) { onMouseMoveUpdate?.Invoke(value.Get<Vector2>()); }       //MOUSE VECTOR
        public void OnTogglePerspective(InputValue value) {if (value.isPressed) onTogglePerspective?.Invoke();}//CAMERA PERSPECTIVE
        
    }
}