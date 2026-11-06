using Alpha.Combat;
using Alpha.Interfaces;

public class AllyAtributes : NpcAttributes
{
    protected override void Awake()
    {
        base.Awake();
        allys.Add(this);
    }

    protected override void EndCharacter()
    {
        allys.Remove(this);
        npcState.StartAction(EnemyActions.Death);
        Destroy(gameObject, 3.5f);
    }
}
