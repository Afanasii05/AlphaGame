
using System;
using UnityEngine;
namespace Alpha.Combat
{
    public enum MoveMode { Free, SlowDown, InPlace }
    [Serializable]
    public abstract class CharacterActionDefinition
    {
        public string name;
        public string trigger;
        public int layer;
        public MoveMode mode;
        public float coolDownTime;
        public float knockBackPower = 0;
        public Vector3 hitboxScaleofWeapon = Vector3.one;
        [Range(0f, 1f)] public float comboWindow = 0.4f; //de la ce procent din animatie accepta nextAction
        [Range(0f, 1f)] public float activeStart = 0f;
        [Range(0f, 1f)] public float activeEnd = 1f;
        public bool alwaysActive;
        [System.NonSerialized] public int tagHash;
        
    }

    public enum PlayerActions
    {
        None = 0, Light = 1, Heavy = 2, Throw = 3, Ability = 4, LightPunch = 5,
        LightPunch2 = 6, LightKick3 = 7, NonCombat = 8, NonSemiCombat = 9, Charge = 10, Light2 = 11,
        Recall = 12, Teleport = 13
    }
    public enum EnemyActions
    {
        None = 0, Attack = 1, GetHit=2, Stomp=3, Roll = 4
    }
    [Serializable]
    public abstract class ActionDefinition<TAction> : CharacterActionDefinition
        where TAction : struct, Enum
    {
        public TAction action;
        public TAction nextAction;
    }
    [Serializable]
    public class PlayerActionDefinition : ActionDefinition<PlayerActions>
    {

        public bool hasItsOwnMovement;
        public bool passesNonSemiCombat;
        public bool allowsJump;
        public bool requiresGrounded;
    }
    [Serializable]
    public class EnemyActionDefinition : ActionDefinition<EnemyActions>
    {
        public bool hasItsOwnMovement;   // se deplaseaza dupa root motion-ul din animatie
    }
}