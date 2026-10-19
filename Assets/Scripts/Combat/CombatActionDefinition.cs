using UnityEngine;
namespace Alpha.Combat
{
    public enum CombatAction { None, Light, Heavy, Throw, Ability, LightPunch,LightPunch2,LightKick3, NonCombat, NonSemiCombat, Charge}
    public enum MoveMode {Free, SlowDown, InPlace}
    [System.Serializable]
    public class CombatActionDefinition
    {
        public int layer;
        public CombatAction action;
        public CombatAction nextAction;
        public MoveMode mode;
        [Range(0f, 1f)] public float comboWindow = 0.4f; //de la ce procent din animatie accepta nextAction
        [System.NonSerialized] public int tagHash;        //tagul din Animator = numele actiunii, calculat in Awake
    }
}