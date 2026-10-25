using Alpha.Combat;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class CharacterCombatState<TAction , TDef>: MonoBehaviour
    where TAction: struct, Enum
    where TDef: ActionDefinition<TAction>
{
    private static readonly EqualityComparer<TAction> Eq = EqualityComparer<TAction>.Default;
    protected static bool IsNone(TAction a) => Eq.Equals(a, default);
    protected static bool Same(TAction a, TAction b) => Eq.Equals(a, b);
    public readonly EnemyHitMeneger hitMeneger = new();
    [SerializeField] protected List<TDef> actionsDetails = new();
    [SerializeField ]protected Animator animator;
    protected TAction pending= default;
    
    [NonSerialized] public Dictionary<TAction, TDef> combatDef = new();
    protected Dictionary<TAction, float> lastTimePlayed = new();
    float pendingSince;

    protected virtual void Awake()
    {
        foreach (TDef act in actionsDetails)
        {
            act.tagHash = Animator.StringToHash(act.action.ToString());
            combatDef[act.action] = act;
        }
    }

    private bool inCoolDown(TAction action)
    {
        if (!lastTimePlayed.TryGetValue(action, out float t))
            return false;
        if (Time.time - t >= combatDef[action].coolDownTime)
            return false;
        return true;
    }
    private bool InTag(TDef ad)
    {
        AnimatorStateInfo s = animator.GetCurrentAnimatorStateInfo(ad.layer);
        AnimatorStateInfo k = animator.GetNextAnimatorStateInfo(ad.layer);
        return s.tagHash == ad.tagHash || (animator.IsInTransition(ad.layer) && k.tagHash == ad.tagHash);
    }

    private TAction ActualStateInAnimation()
    {
        foreach (TDef ad in actionsDetails)
            if (InTag(ad))
                return ad.action;
        return default;
    }

    public TAction CurrentAction()
    {
        if (!IsNone(pending)) return pending;
        return ActualStateInAnimation();
    }

    public bool CanStart(TAction action)
    {
        if (inCoolDown(action))
            return false;

        if (!CanStartExtra(combatDef[action])) return false;

        TAction current = CurrentAction();
        if (IsNone(current) || (Same(ValidNext(), action) && CanContinueCombo())) return true;
        return CanInterrupt(current, combatDef[action]);
        
    }

    public bool TryStart(TAction action)
    {
        if (!CanStart(action)) return false;
        StartAction(action);
        return true;
    }
    public void StartAction(TAction action)
    {
        pending = action;
        pendingSince = Time.time;
        lastTimePlayed[action] = pendingSince;
        if (!string.IsNullOrEmpty(combatDef[action].trigger))
            animator.SetTrigger(combatDef[action].trigger);
        hitMeneger.resetAction();
        OnActionStarted(combatDef[action]);

    }
    public bool inActiveWindow()
    {
        TAction current = ActualStateInAnimation();
        if (IsNone(current)) return false;
        TDef def = combatDef[current];
        if (def.alwaysActive) return true;
        if (animator.IsInTransition(def.layer)) return false;
        AnimatorStateInfo s = animator.GetCurrentAnimatorStateInfo(def.layer);
        if (s.tagHash != def.tagHash) return false;
        float t = s.normalizedTime;
        return t >= def.activeStart && t <= def.activeEnd;
    }
    public bool inPlaceAction() { return combatDef[CurrentAction()].mode == MoveMode.InPlace; }
    public bool inSlowDownAction() { return combatDef[CurrentAction()].mode == MoveMode.SlowDown; }

    protected virtual bool CanStartExtra(TDef def) => true;
    protected virtual bool CanContinueCombo() => true;
    protected virtual bool CanInterrupt(TAction current, TDef next) => false;
    protected virtual void OnActionStarted(TDef def) { }

    public TAction ComboFrom(TAction firstMove)
    {
        if (IsNone(firstMove)) return default;
        TAction next = ValidNext();
        if (IsNone(next) || IsNone(combatDef[firstMove].nextAction)) return firstMove;
        if (inChain(firstMove, next))
            return next;
        return firstMove;
    }


    private TAction ValidNext()
    {
        if (!IsNone(pending)) return default;
        TAction actual = ActualStateInAnimation();
        if (!combatDef.TryGetValue(actual, out TDef cur) || IsNone(cur.nextAction))
            return default;
        if (animator.IsInTransition(cur.layer)) return default;
        AnimatorStateInfo s = animator.GetCurrentAnimatorStateInfo(cur.layer);
        if (s.tagHash != cur.tagHash || s.normalizedTime < cur.comboWindow) return default;
        return cur.nextAction;
    }
    private bool inChain(TAction firstMove, TAction current)
    {
        while (!IsNone(combatDef[firstMove].nextAction))
        {
            if (Same(combatDef[firstMove].nextAction,current))
                return true;
            firstMove = combatDef[firstMove].nextAction;
        }
        return false;
    }

    protected void Update()
    {
        if (IsNone(pending)) return;
        if (Same(ActualStateInAnimation(), pending) || Time.time - pendingSince >= 0.3f)
            pending = default;

    }
}
