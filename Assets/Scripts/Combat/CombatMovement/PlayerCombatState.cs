using System.Collections.Generic;
using UnityEngine;
namespace Alpha.Combat
{

    [DefaultExecutionOrder(-50)] // calculez actiunea curenta inaintea celorlalte scripturi
    public class PlayerCombatState : CharacterCombatState <PlayerActions, PlayerActionDefinition>
    {
        WeaponSocket socket;
        HashSet<PlayerActions> intreruptableActions = new() {PlayerActions.Block, PlayerActions.NonSemiCombat};
        protected override void Awake()
        {
            base.Awake();
            socket = GetComponentInChildren<WeaponSocket>();
        }
        public bool Grounded => animator.GetBool("IsGrounded");
        protected override bool CanContinueCombo() => Grounded;
        protected override bool CanInterrupt(PlayerActions current, PlayerActionDefinition next)
            => intreruptableActions.Contains(current) && next.passesNonSemiCombat;
        protected override bool OtherVerifications(PlayerActionDefinition def)
        {
            if(def.requiresGrounded)
                return Grounded;
            return true;
        }
        
        //Momentan doar scalez hitboxurile pe arma
        protected override void OnActionStarted(PlayerActionDefinition def)
        {
            intreruptContinuousStates();
            if (socket.HasWeapon)
            {
                Weapon weapon = socket.CurrentWeapon;
                weapon.headHitbox.transform.localScale =
                    Vector3.Scale(weapon.HitboxOriginalSize, def.hitboxScaleofWeapon);
            }
            
        }

        public bool hasOwnMovement() => combatDef[currentAction].hasItsOwnMovement;
        public bool allowsJump() => combatDef[currentAction].allowsJump;
    }
}