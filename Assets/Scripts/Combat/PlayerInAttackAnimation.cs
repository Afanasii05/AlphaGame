using UnityEngine;
namespace Alpha.Combat
{
    public class PlayerInAttackAnimation : MonoBehaviour
    {
        Animator anim;
        void Awake()
        {
            anim = GetComponentInChildren<Animator>();
        }

        public bool isAttacking()
        {
            AnimatorStateInfo s = anim.GetCurrentAnimatorStateInfo(1);
            AnimatorStateInfo k = anim.GetNextAnimatorStateInfo(1);
            return s.IsTag("Attack") || (anim.IsInTransition(1) && k.IsTag("Attack"))|| anim.GetBool("Click");
        }
        public bool isThrowing()
        {
            AnimatorStateInfo s = anim.GetCurrentAnimatorStateInfo(1);
            AnimatorStateInfo k = anim.GetNextAnimatorStateInfo(1);
            return s.IsTag("Throw") || (anim.IsInTransition(1) && k.IsTag("Throw"));
        }
        void Update()
        {

        }
    }
}