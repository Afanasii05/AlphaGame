using Alpha.Combat;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
public class EnemyCombatState : CharacterCombatState<EnemyActions, EnemyActionDefinition>
{
    public bool IsBusy => !IsNone(CurrentAction());
    Dictionary<Collider, Vector3> colidersOriginalSize = new();
    EnemyActionDefinition hitboxDef;
    Coroutine hitboxRoutine;
    void Start()
    {
        foreach (EnemyActionDefinition def in combatDef.Values)
            foreach (Collider col in def.hitboxes)
            {
                col.enabled = false;
                colidersOriginalSize[col] = col.transform.localScale;
            }
        
    }
    protected override void OnActionStarted(EnemyActionDefinition def)
    {
        if (hitboxRoutine != null) StopCoroutine(hitboxRoutine);
        if (hitboxDef != null) setHitboxes(hitboxDef, false);
        hitboxDef = def;
        hitboxRoutine = StartCoroutine(collidersDisabler(def));
    }
    protected override bool CanInterrupt(EnemyActions current, EnemyActionDefinition next)
            => next.action == EnemyActions.GetHit;

    IEnumerator collidersDisabler(EnemyActionDefinition def)
    {
        while (!inWindow(def))
        {
            if (!Same(CurrentAction(), def.action)) yield break;
            yield return null;
        }
        setHitboxes(def, true);
        while (inWindow(def)) yield return null;
        setHitboxes(def, false);
    }
    bool inWindow(EnemyActionDefinition def)
    {
        float t = NormalizedTime(def.action);
        return t > 0f && t >= def.activeStart && t <= def.activeEnd;
    }
    void setHitboxes(EnemyActionDefinition def, bool state)
    {
        foreach (Collider col in def.hitboxes)
        {
            col.enabled = state;
            if (state) col.transform.localScale = Vector3.Scale(colidersOriginalSize[col], def.hitboxScaleofWeapon);
        }
    }
}
