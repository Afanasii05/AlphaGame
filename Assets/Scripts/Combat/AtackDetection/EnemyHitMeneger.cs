using Alpha.Interfaces;
using System.Collections.Generic;
namespace Alpha.Combat
{
    public class EnemyHitMeneger 
    {
        readonly HashSet<HittableCharacter> hitThisAction = new();

        public void HandleAttack(HittableCharacter characterThatGotHit, CharacterActionDefinition hitAction, HittableCharacter characterThatHit=null)
        {
            if (!hitThisAction.Add(characterThatGotHit)) return;
            characterThatGotHit.HandleGotHit(hitAction, characterThatHit);
        }
        public void resetAction() { hitThisAction.Clear(); }
    }
}