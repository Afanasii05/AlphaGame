using UnityEngine;
using Alpha.Input;
namespace Alpha.Combat
{
    //Tine parametrul ActionLock pe true cat dureaza abilitatea (Z); tranzitiile spre mers/alergare il verifica,
    //ca sa nu intrerupa animatia cand jucatorul se misca.
    public class ActionLock : MonoBehaviour
    {
        [SerializeField] private float grace = 0.3f;   //Dupa apasarea Z, atatea secunde asteptam intrarea in animatie
        [SerializeField] private bool debugLog;

        private static readonly int AbilityTag = Animator.StringToHash("Ability");
        private static readonly int LockId = Animator.StringToHash("ActionLock");

        private InputHandler inputHandler;
        private WeaponSocket socket;
        private PlayerCombatState combatState;
        private Animator animator;
        private bool hasParameter;
        private bool locked;
        private float requestTime = -10f;

        private void Awake()
        {
            inputHandler = GetComponentInParent<InputHandler>();
            socket = inputHandler.GetComponentInChildren<WeaponSocket>();
            combatState = inputHandler.GetComponent<PlayerCombatState>();
            animator = inputHandler.GetComponentInChildren<Animator>();
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                if (parameter.nameHash == LockId)
                    hasParameter = true;
            if (!hasParameter)
                Debug.LogWarning("[ActionLock] Parametrul ActionLock lipseste din Animator. Ruleaza Tools/Alpha/Setup Combat Polish.");
        }

        private void OnEnable() { inputHandler.onToggleSpecialMove += HandleSpecial; }

        private void OnDisable()
        {
            inputHandler.onToggleSpecialMove -= HandleSpecial;
            SetLock(false);
        }

        private void HandleSpecial()
        {
            if (socket.HasWeapon)
                requestTime = Time.unscaledTime;
        }

        private bool InAbility()
        {
            if (animator.GetCurrentAnimatorStateInfo(0).tagHash == AbilityTag)
                return true;
            return animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).tagHash == AbilityTag;
        }

        private void Update()
        {
            bool want = InAbility();
            if (!want && Time.unscaledTime - requestTime < grace)
            {
                CombatAction action = combatState.CurrentAction(); //Heavy = cererea a fost acceptata (pending), Ability = animatia ruleaza
                want = action == CombatAction.Heavy || action == CombatAction.Ability;
            }
            SetLock(want);
        }

        private void SetLock(bool value)
        {
            if (value == locked)
                return;
            locked = value;
            if (hasParameter && animator != null)
                animator.SetBool(LockId, value);
            if (debugLog)
                Debug.Log("[ActionLock] " + value);
        }
    }
}
