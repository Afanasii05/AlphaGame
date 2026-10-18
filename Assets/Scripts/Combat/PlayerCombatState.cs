using System.Collections.Generic;
using UnityEngine;
namespace Alpha.Combat
{
    public enum CombatAction { None, Light, Heavy, Throw, Ability, LightPunch, NonCombat, NonSemiCombat, Charge }
    public class PlayerCombatState : MonoBehaviour
    {
        Animator animator;
        CombatAction pending = CombatAction.None;
        private HashSet<CombatAction> inPlaceActions = new HashSet<CombatAction>(){CombatAction.Heavy, CombatAction.Ability, CombatAction.LightPunch, CombatAction.Charge};
        private HashSet<CombatAction> slowDownMomementActions = new HashSet<CombatAction>() { CombatAction.Throw, CombatAction.Light };
        float pendingSince;

        void Awake()
        {
            animator = GetComponentInChildren<Animator>();
        }

        private bool InTag(int layer, string tag)
        {
            AnimatorStateInfo s = animator.GetCurrentAnimatorStateInfo(layer);
            AnimatorStateInfo k = animator.GetNextAnimatorStateInfo(layer);
            return s.IsTag(tag) || (animator.IsInTransition(layer) && k.IsTag(tag));
        }


        private CombatAction ActualStateInAnimation()
        {
       
            if (InTag(0, "HeavyAttack"))   return CombatAction.Heavy;
            if (InTag(0, "Ability"))       return CombatAction.Ability;
            if (InTag(0, "LightPunch"))    return CombatAction.LightPunch;
            if (InTag(1, "LightAttack"))   return CombatAction.Light;
            if (InTag(1, "Throw"))         return CombatAction.Throw;
            if (InTag(0, "NonCombat"))     return CombatAction.NonCombat;
            if (InTag(0, "NonSemiCombat")) return CombatAction.NonSemiCombat;
            if (InTag(0, "Charge"))        return CombatAction.Charge;
            return CombatAction.None;
        }
        public CombatAction CurrentAction()
        {
            if (pending != CombatAction.None) return pending;
            return ActualStateInAnimation();
        }
        public bool CanStart(CombatAction action)
        {
            if ((action == CombatAction.Light || action == CombatAction.Heavy) && !animator.GetBool("IsGrounded"))
                return false;
            CombatAction Current = CurrentAction();
            switch (Current)
            {
                case CombatAction.None: return true;
                case CombatAction.Light: return action == CombatAction.Heavy;
                case CombatAction.NonSemiCombat: return (action == CombatAction.Throw || action == CombatAction.Light);
                default: return false;
            }
        }
        public bool TryStart(CombatAction action)
        {
            if (CanStart(action))
            {
                pending = action;
                pendingSince = Time.time;
                return true;
            }
            return false;
        }
        public bool inPlaceAction(){return inPlaceActions.Contains(CurrentAction());}
        public bool inSlowDownAction() { return slowDownMomementActions.Contains(CurrentAction()); }
        void Update()
        {
            if (pending == CombatAction.None) return;
            if (ActualStateInAnimation() == pending || Time.time - pendingSince >= 0.3f)
                pending = CombatAction.None;

        }
    }
}