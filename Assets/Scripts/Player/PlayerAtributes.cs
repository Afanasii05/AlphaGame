using Alpha.Combat;
using Alpha.Interfaces;

public class PlayerAtributes : HittableCharacter
{
    PlayerCombatState playerState;

    protected override void Awake()
    {
        base.Awake();
        allys.Add(this);
        playerState = GetComponent<PlayerCombatState>();
    }
    protected override void StartHitState(CharacterActionDefinition attack)
    {
        playerState.StartAction(PlayerActions.GetHit);
    }
}
