using Unity.AppUI.UI;
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

        public bool inPlaceAnimation()
        {
            AnimatorStateInfo s = anim.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo k = anim.GetNextAnimatorStateInfo(0);
            return s.IsTag("HeavyAttack") || (anim.IsInTransition(0) && k.IsTag("HeavyAttack"));

        }
        void Update()
        {

        }
    }
}