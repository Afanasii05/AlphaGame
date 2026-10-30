using Alpha.Combat;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
public class EnemyCombatState : CharacterCombatState<EnemyActions, EnemyActionDefinition>
{
    public bool IsBusy => !IsNone(CurrentAction());
    Dictionary<Collider, Vector3> colidersOriginalSize = new();
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
        foreach(Collider col in def.hitboxes)
        {
            col.enabled = true;
            col.transform.localScale = Vector3.Scale(colidersOriginalSize[col], def.hitboxScaleofWeapon);
        }
        StartCoroutine(collidersDisabler(def));
    }
    IEnumerator collidersDisabler(EnemyActionDefinition def)
    {

        while (!inActiveWindow()) yield return null;
        while (inActiveWindow()) yield return null;
        foreach (Collider col in def.hitboxes)
        {
            col.enabled = false;
        }
    }
}
