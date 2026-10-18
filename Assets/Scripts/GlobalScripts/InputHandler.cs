using System;
using UnityEngine;
namespace Alpha.Input
{
    public class InputHandler : MonoBehaviour
    {
        //KEY EVENTS
        public event Action<bool> onCrouchHold;             //LEFT CTRL  CROUCH
        public event Action<bool> onSprintHold;             //LEFT SHIFT SPRINT
        public event Action onJumpTriggered;                //SPACE      JUMP
        public event Action onTogglePerspective;            //P          CAMERA PERSPECTIVE
        public event Action onToggleWeapon;                 //E          TOGGLE WEAPON
        public event Action onClick;                        //Click         
        public event Action onToggleSpecialMove;            //Z          SPECIAL MOVE
        public event Action onCharge;                       //Q          SPECIAL MOVE CHARGE
        public event Action<Vector2> onMouseMoveUpdate;     //MOUSE      LOOK
        public event Action<Vector2> onMoveUpdate;          //WASD       MOVE
        private ActionsHandler inputActions;


        private void Awake()
        {
            inputActions = new ActionsHandler();
            inputActions.Enable();
            //PLAYER
            // JUMP
            inputActions.Player.Jump.performed += ctx => onJumpTriggered?.Invoke();

            // CROUCH
            inputActions.Player.Crouch.started += ctx => onCrouchHold?.Invoke(true);
            inputActions.Player.Crouch.canceled += ctx => onCrouchHold?.Invoke(false);

            // SPRINT
            inputActions.Player.Sprint.started += ctx => onSprintHold?.Invoke(true);
            inputActions.Player.Sprint.canceled += ctx => onSprintHold?.Invoke(false);

            // MOVE (WASD)
            inputActions.Player.MovePlayer.performed += ctx => onMoveUpdate?.Invoke(ctx.ReadValue<Vector2>());
            inputActions.Player.MovePlayer.canceled += ctx => onMoveUpdate?.Invoke(Vector2.zero);

            // LOOK (MOUSE)
            inputActions.Player.Look.performed += ctx => onMouseMoveUpdate?.Invoke(ctx.ReadValue<Vector2>());
            inputActions.Player.Look.canceled += ctx => onMouseMoveUpdate?.Invoke(Vector2.zero);

            // PERSPECTIVE
            inputActions.Player.TogglePerspective.performed += ctx => onTogglePerspective?.Invoke();
            //WEAPON
            //TOGGLE WEAPON
            inputActions.Weapon.Toggle.performed += ctx => onToggleWeapon?.Invoke();
            //CLICK(ATTACK)
            inputActions.Weapon.Click.performed += ctx => onClick?.Invoke();
            //SPECIAL MOVE
            inputActions.Weapon.Special.performed += ctx => onToggleSpecialMove?.Invoke();
            //SPECIAL MOVE CHARGE
            inputActions.Weapon.Charge.performed += ctx => onCharge?.Invoke();
        }
        

        private void OnDisable()
        {
            inputActions.Disable();
        }
        public void enableInputs()
        {
            inputActions.Enable();
        }
        public void disableInputs()
        {
            inputActions.Disable();
        }
    }
}