using Alpha.Combat;
using Alpha.Interfaces;
public class AllyAtributes : HittableCharacter
{
    EnemyController allyController;

    protected override void Awake()
    {
        base.Awake();
        allys.Add(this);
        allyController = GetComponent<EnemyController>();
    }
    protected override void StartHitState(CharacterActionDefinition attack)
    {
        allyController.enemyGetHit(attack);
    }
}
