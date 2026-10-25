using Alpha.Combat;

public class EnemyCombatState : CharacterCombatState<EnemyActions, EnemyActionDefinition>
{
    public bool IsBusy => !IsNone(CurrentAction());
}
