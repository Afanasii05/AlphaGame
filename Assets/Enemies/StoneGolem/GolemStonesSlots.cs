using Alpha.Combat;
using System;
using System.Collections.Generic;
using UnityEngine;

public class GolemStonesSlots : MonoBehaviour
{
    [NonSerialized] public Transform[] stones;
    EnemyCombatState golemState;
    void Awake()
    {
        golemState = GetComponent<EnemyCombatState>();
        var list = new List<Transform>();
        foreach (MeshRenderer r in GetComponentsInChildren<MeshRenderer>(true))
            if (r.name.StartsWith("Wall"))
                list.Add(r.transform);

        list.Sort((a, b) => a.position.y.CompareTo(b.position.y));   
        stones = list.ToArray();
        gameObject.SetActive(false);
    }
    public void Activate()
    {
        this.gameObject.SetActive(true);
    }
    public void Deactivate()
    {
        golemState.StartAction(EnemyActions.Death);
        Destroy(this.gameObject,3.5f);
    }


}
