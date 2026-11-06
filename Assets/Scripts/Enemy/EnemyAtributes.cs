using Alpha.Combat;
using Alpha.Interfaces;

public class EnemyAtributes : NpcAttributes
{
    protected override void Awake()
    {
        base.Awake();
        enemies.Add(this);
    }

    protected override void EndCharacter()
    {
        enemies.Remove(this);
        npcState.StartAction(EnemyActions.Death);
        Destroy(gameObject, 3.5f);
    }
}
