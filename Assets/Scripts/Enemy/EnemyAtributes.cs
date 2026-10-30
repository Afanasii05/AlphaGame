using Alpha.Combat;
using Alpha.Enemies;
using Alpha.Interfaces;
using UnityEngine;

public class EnemyAtributes : HittableCharacter
{
    [SerializeField] private int health = 100;
    [SerializeField] private float armor = 1;
    [SerializeField] private float damageMultiplier=1;
    EnemyController enemyController;
    
    private void Awake()
    {
        enemyController = GetComponent<EnemyController>();
    }
    public override void HandleGotHit(CharacterActionDefinition attack, HittableCharacter attacker)
    {
        enemyController.enemyGetHit(attack);
    }
}
