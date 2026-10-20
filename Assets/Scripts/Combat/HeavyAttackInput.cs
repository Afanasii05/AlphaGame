using System.Collections;
using Alpha.Input;
using UnityEngine;
namespace Alpha.Combat
{
    public class HeavyAttackInput : MonoBehaviour
    {
        private ExtraInputHandler extraInput;
        private WeaponSocket socket;
        private PlayerCombatState combatState;
        private Animator animator;
        private Rigidbody body;

        private void Awake()
        {
            InputHandler inputHandler = GetComponentInParent<InputHandler>();
            extraInput = GetComponent<ExtraInputHandler>();
            socket = inputHandler.GetComponentInChildren<WeaponSocket>();
            combatState = inputHandler.GetComponent<PlayerCombatState>();
            animator = inputHandler.GetComponentInChildren<Animator>();
            body = inputHandler.GetComponent<Rigidbody>();
        }

        private void OnEnable() { extraInput.onHeavyAttack += HandleHeavy; }
        private void OnDisable() { extraInput.onHeavyAttack -= HandleHeavy; }

        private void HandleHeavy()
        {
            if (!socket.HasWeapon || !animator.GetBool("IsGrounded"))
                return;
            if (!combatState.TryStart(CombatAction.Heavy))
                return;
            animator.ResetTrigger("Click");
            animator.SetBool("DoubleClick", true);
            HeavyImpactDetection impact = socket.CurrentWeapon.GetComponent<HeavyImpactDetection>();
            if (impact != null)
                impact.Begin();
            StartCoroutine(TurnToCamera());
        }

        private IEnumerator TurnToCamera()
        {
            Quaternion start = body.rotation;
            Quaternion target = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
            for (float t = 0f; t < 0.15f; t += Time.fixedDeltaTime)
            {
                body.MoveRotation(Quaternion.Slerp(start, target, t / 0.15f));
                yield return new WaitForFixedUpdate();
            }
            body.MoveRotation(target);
        }
    }
}
