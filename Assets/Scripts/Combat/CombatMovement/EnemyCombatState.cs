using Alpha.Combat;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
[DefaultExecutionOrder(-50)] // calculez actiunea curenta inaintea celorlalte scripturi
public class EnemyCombatState : CharacterCombatState<EnemyActions, EnemyActionDefinition>
{
    public bool IsBusy => !IsNone(currentAction); //orice stare din enemy action definition intrerupe enemy ai 
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
        if (hitboxDef != null) setHitboxes(hitboxDef, false); // logica de anulare a coliderelor fostei stari daca aceasta a fost intrerupta
        hitboxDef = def;
        hitboxRoutine = StartCoroutine(collidersDisabler(def));
    }
    protected override bool CanInterrupt(EnemyActions current, EnemyActionDefinition next)
            => next.action == EnemyActions.GetHit;

    IEnumerator collidersDisabler(EnemyActionDefinition def)
    {
        while (!inActiveWindow())
        {
            if (!Same(currentAction, def.action)) yield break;
            yield return null;
        }
        setHitboxes(def, true);
        while (Same(currentAction, def.action) && inActiveWindow()) yield return null;
        setHitboxes(def, false);
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
