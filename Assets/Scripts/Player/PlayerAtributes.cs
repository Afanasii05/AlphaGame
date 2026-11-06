using Alpha.Combat;
using Alpha.Interfaces;
using UnityEngine;

public class PlayerAtributes : HittableCharacter
{
    PlayerCombatState playerState;

    protected override void Awake()
    {
        base.Awake();
        allys.Add(this);
        playerState = GetComponent<PlayerCombatState>();
    }
    protected override void EndCharacter()
    { 
        playerState.StartAction(PlayerActions.Death);
    }
    protected override void StartHitState()
    {
        playerState.StartAction(PlayerActions.GetHit);
    }
    
}
