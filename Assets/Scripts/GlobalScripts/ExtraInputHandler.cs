using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Alpha.Input
{
    public class ExtraInputHandler : MonoBehaviour
    {
        public event Action onHeavyAttack;                  //RIGHT CLICK HEAVY ATTACK
        public event Action onDash;                         //C           DASH
        private ActionsHandler inputActions;
        private InputAction heavyAction;
        private InputAction dashAction;
        private Rigidbody playerBody;

        private void Awake()
        {
            playerBody = GetComponentInParent<InputHandler>().GetComponent<Rigidbody>();
            inputActions = new ActionsHandler();
            heavyAction = inputActions.asset.FindAction("Weapon/Heavy", true);
            dashAction = inputActions.asset.FindAction("Player/Dash", true);
        }

        private void OnEnable()
        {
            heavyAction.performed += HandleHeavy;
            dashAction.performed += HandleDash;
            heavyAction.Enable();
            dashAction.Enable();
        }

        private void OnDisable()
        {
            heavyAction.performed -= HandleHeavy;
            dashAction.performed -= HandleDash;
            heavyAction.Disable();
            dashAction.Disable();
        }

        private void OnDestroy()
        {
            inputActions.Dispose();
        }

        private void HandleHeavy(InputAction.CallbackContext ctx)
        {
            if (playerBody.isKinematic) //Cutscene: fara input
                return;
            onHeavyAttack?.Invoke();
        }

        private void HandleDash(InputAction.CallbackContext ctx)
        {
            if (playerBody.isKinematic)
                return;
            onDash?.Invoke();
        }
    }
}
