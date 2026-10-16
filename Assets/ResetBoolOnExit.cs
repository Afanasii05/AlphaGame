using UnityEngine;

public class ResetBoolOnState : StateMachineBehaviour
{
    [SerializeField] string boolName = "DoubleClick";
    [SerializeField] bool resetOnEnter = true;
    [SerializeField] bool resetOnExit = true;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo info, int layer)
    {
        if (resetOnEnter) animator.SetBool(boolName, false);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo info, int layer)
    {
        if (resetOnExit) animator.SetBool(boolName, false);
    }
}