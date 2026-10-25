using Alpha.Combat;
using Alpha.Interfaces;
using UnityEngine;

public class PlayerAtributes :  HittableCharacter
{
    [SerializeField] private int health = 100;
    private float damageMultiplier = 1f;
    Animator animator;
    PlayerCombatState playerState;
    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        playerState = GetComponent<PlayerCombatState>();
    }

    public override void HandleAttack(CharacterActionDefinition attack)
    {

    }
}
