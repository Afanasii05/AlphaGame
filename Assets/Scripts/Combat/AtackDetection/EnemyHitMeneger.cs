using Alpha.Interfaces;
using System.Collections.Generic;
using UnityEngine;
namespace Alpha.Combat
{
    public class EnemyHitMeneger 
    {
        readonly HashSet<HittableCharacter> hitThisAction = new();

        public void HandleAttack(HittableCharacter characterThatGotHit, CharacterActionDefinition hitAction)
        {
            if (!hitThisAction.Add(characterThatGotHit)) return;
            characterThatGotHit.HandleAttack(hitAction);

            Debug.Log($"I have hit {characterThatGotHit.ToString()}  with {hitAction.ToString()}");
        }
        public void resetAction() { hitThisAction.Clear(); }
    }
}