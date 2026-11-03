using Alpha.Combat;
using Alpha.Interfaces;

public class EnemyAtributes : HittableCharacter
{
    EnemyController enemyController;

    protected override void Awake()
    {
        base.Awake();
        enemies.Add(this);
        enemyController = GetComponent<EnemyController>();
    }
    protected override void StartHitState(CharacterActionDefinition attack)
    {
        enemyController.enemyGetHit(attack);
    }
}
