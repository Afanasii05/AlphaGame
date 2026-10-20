using Unity.Collections;
using UnityEngine;
namespace Alpha.Combat
{
    public enum CombatAction
    {
        None = 0, Light = 1, Heavy = 2, Throw = 3, Ability = 4, LightPunch = 5,
        LightPunch2 = 6, LightKick3 = 7, NonCombat = 8, NonSemiCombat = 9, Charge = 10, Light2 = 11
    }
    public enum MoveMode {Free, SlowDown, InPlace}
    [System.Serializable]
    public class CombatActionDefinition
    {
        public string name;
        public int layer;
        public CombatAction action;
        public CombatAction nextAction;
        public MoveMode mode;
        public bool hasItsOwnMovement;
        [Range(0f, 1f)] public float comboWindow = 0.4f; //de la ce procent din animatie accepta nextAction
        [System.NonSerialized] public int tagHash;        //tagul din Animator = numele actiunii, calculat in Awake
    }
}