using Alpha.Combat;
using UnityEngine;
namespace Alpha.Interfaces
{
    public abstract class HittableCharacter : MonoBehaviour
    {
        public abstract void HandleGotHit(CharacterActionDefinition attack, HittableCharacter attacker);
       
    }
}
