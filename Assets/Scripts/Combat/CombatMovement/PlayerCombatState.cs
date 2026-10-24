using System;
using System.Collections.Generic;
using UnityEngine;
namespace Alpha.Combat
{

    public class PlayerCombatState : MonoBehaviour
    {
        private static readonly CombatActionDefinition NoneDef = new() { allowsJump = true };
        Animator animator;
        WeaponSocket socket;
        CombatAction pending = CombatAction.None;
        [SerializeField] private List<CombatActionDefinition> actionsDetails = new();
        [NonSerialized]  public Dictionary<CombatAction, CombatActionDefinition> combatDef = new();
        EnemyHitMeneger hitMeneger;
        private Dictionary<CombatAction, float> lastTimePlayed = new();
        float pendingSince;

        void Awake()
        {
            hitMeneger = GetComponent<EnemyHitMeneger>();
            animator = GetComponentInChildren<Animator>();
            socket = GetComponentInChildren<WeaponSocket>();
            foreach (CombatActionDefinition act in actionsDetails)
            {
                act.tagHash = Animator.StringToHash(act.action.ToString());
                combatDef[act.action] = act;
            }
            combatDef[CombatAction.None] = NoneDef;
        }

        private bool inCoolDown(CombatAction action)
        {
            if (!lastTimePlayed.TryGetValue(action, out float t))
                return false;
            if (Time.time - t >= combatDef[action].coolDownTime)
                return false;
            return true;
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
            if (inCoolDown(action))
                return false;
            
            bool ground = animator.GetBool("IsGrounded");
            if (combatDef[action].requiresGrounded && !ground) return false;
            CombatAction current = CurrentAction();
            if (current == CombatAction.None ||(ValidNext() == action && ground)) return true;
            switch (current)
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
                lastTimePlayed[action] = pendingSince;
                if (!string.IsNullOrEmpty(combatDef[action].trigger))
                    animator.SetTrigger(combatDef[action].trigger);
                if (socket.HasWeapon)
                {
                    Weapon weapon = socket.CurrentWeapon;
                    weapon.headHitbox.transform.localScale = Vector3.Scale(weapon.HitboxOriginalSize, combatDef[action].hitboxScaleofWeapon);
                }
                hitMeneger.resetAction();
                return true;
            }
            return false;
        }
        public bool inActiveWindow()
        {
            CombatAction current = ActualStateInAnimation();
            if (current == CombatAction.None) return false;
            CombatActionDefinition def = combatDef[current];
            if (def.alwaysActive) return true;
            if (animator.IsInTransition(def.layer)) return false;
            AnimatorStateInfo s = animator.GetCurrentAnimatorStateInfo(def.layer);
            if (s.tagHash != def.tagHash) return false;
            float t = s.normalizedTime;
            return t >= def.activeStart && t <= def.activeEnd;
        }
        public bool inPlaceAction() { return combatDef[CurrentAction()].mode == MoveMode.InPlace; }
        public bool inSlowDownAction() { return combatDef[CurrentAction()].mode == MoveMode.SlowDown; }
        public bool hasOwnMovement(){return combatDef[CurrentAction()].hasItsOwnMovement;}
        public bool allowsJump(){return combatDef[CurrentAction()].allowsJump;}

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
            if (!combatDef.TryGetValue(actual, out CombatActionDefinition cur) || cur.nextAction == CombatAction.None)
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