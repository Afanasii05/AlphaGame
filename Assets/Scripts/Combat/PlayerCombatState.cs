using System.Collections.Generic;
using UnityEngine;
namespace Alpha.Combat
{

    public class PlayerCombatState : MonoBehaviour
    {
        Animator animator;
        CombatAction pending = CombatAction.None;
        [SerializeField] private List<CombatActionDefinition> actionsDetails = new();
        private Dictionary<CombatAction, MoveMode> actionsMoveMode = new();
        private Dictionary<CombatAction, bool> actionAllowsJump = new();
        private Dictionary<CombatAction, bool> actionHasOwnMovement = new();
        private Dictionary<CombatAction, CombatActionDefinition> combatDef = new();
        float pendingSince;

        void Awake()
        {
            animator = GetComponentInChildren<Animator>();

            foreach (CombatActionDefinition act in actionsDetails)
            {
                act.tagHash = Animator.StringToHash(act.action.ToString());
                actionsMoveMode[act.action] = act.mode;
                actionAllowsJump[act.action] = act.allowsJump;
                actionHasOwnMovement[act.action] = act.hasItsOwnMovement;
                combatDef[act.action] = act;
            }
            actionsMoveMode[CombatAction.None] = MoveMode.Free;
            actionAllowsJump[CombatAction.None] = true;
            actionHasOwnMovement[CombatAction.None] = false;
            combatDef[CombatAction.None] = null;
        }

        private bool InTag(CombatActionDefinition ad)
        {
            AnimatorStateInfo s = animator.GetCurrentAnimatorStateInfo(ad.layer);
            AnimatorStateInfo k = animator.GetNextAnimatorStateInfo(ad.layer);
            return s.tagHash == ad.tagHash || (animator.IsInTransition(ad.layer) && k.tagHash == ad.tagHash);
        }

        private CombatAction ActualStateInAnimation()
        {
            foreach (CombatActionDefinition ad in actionsDetails)
                if (InTag(ad))
                    return ad.action;
            return CombatAction.None;
        }

        public CombatAction CurrentAction()
        {
            if (pending != CombatAction.None) return pending;
            return ActualStateInAnimation();
        }

        public bool CanStart(CombatAction action)
        {
            CombatAction Current = CurrentAction();
            if (Current == CombatAction.None ||(ValidNext() == action && animator.GetBool("IsGrounded"))) return true;
            switch (Current)
            {   
                case CombatAction.NonSemiCombat: return combatDef[action].passesNonSemiCombat;
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
        public bool inPlaceAction() { return actionsMoveMode[CurrentAction()] == MoveMode.InPlace; }
        public bool inSlowDownAction() { return actionsMoveMode[CurrentAction()] == MoveMode.SlowDown; }
        public bool hasOwnMovement(){return actionHasOwnMovement[CurrentAction()];}
        public bool allowsJump(){return actionAllowsJump[CurrentAction()];}

        public CombatAction ComboFrom(CombatAction firstMove)
        {
            if (firstMove == CombatAction.None) return CombatAction.None;
            CombatAction next = ValidNext();
            if (next == CombatAction.None || combatDef[firstMove].nextAction == CombatAction.None) return firstMove;
            if (inChain(firstMove, next))
                return next;
            return firstMove;
        }

        
        private CombatAction ValidNext()
        {
            if (pending != CombatAction.None) return CombatAction.None;
            CombatAction actual = ActualStateInAnimation();
            if (!combatDef.TryGetValue(actual, out CombatActionDefinition cur) || cur == null || cur.nextAction == CombatAction.None)
                return CombatAction.None;
            if (animator.IsInTransition(cur.layer)) return CombatAction.None;
            AnimatorStateInfo s = animator.GetCurrentAnimatorStateInfo(cur.layer);
            if (s.tagHash != cur.tagHash || s.normalizedTime < cur.comboWindow) return CombatAction.None;
            return cur.nextAction;
        }
        private bool inChain(CombatAction firstMove, CombatAction current)
        {
            while (combatDef[firstMove].nextAction != CombatAction.None)
            {
                if (combatDef[firstMove].nextAction == current)
                    return true;
                firstMove = combatDef[firstMove].nextAction;
            }
            return false;
        }

        void Update()
        {
            if (pending == CombatAction.None) return;
            if (ActualStateInAnimation() == pending || Time.time - pendingSince >= 0.3f)
                pending = CombatAction.None;

        }
    }
}