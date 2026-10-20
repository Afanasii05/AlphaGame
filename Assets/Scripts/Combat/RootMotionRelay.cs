using UnityEngine;
namespace Alpha.Combat
{
    //Sta pe obiectul cu Animator. Preia miscarea din animatie (OnAnimatorMove) fara sa mute modelul;
    //AttackMotion o aplica pe Rigidbody doar in timpul atacurilor.
    [RequireComponent(typeof(Animator))]
    public class RootMotionRelay : MonoBehaviour
    {
        [Tooltip("deltaPosition exista doar cand animatorul are root motion pornit; cu OnAnimatorMove prezent, Unity nu mai muta transformul singur")]
        [SerializeField] private bool enableRootMotionFlag = true;

        private Animator animator;
        private Vector3 accumulated;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (enableRootMotionFlag)
                animator.applyRootMotion = true;
        }

        private void OnAnimatorMove()
        {
            if (animator == null)
                return;
            Vector3 delta = animator.deltaPosition;
            delta.y = 0f;
            accumulated += delta;
            if (accumulated.sqrMagnitude > 100f) //Nimeni nu consuma: nu las valoarea sa creasca
                accumulated = Vector3.zero;
        }

        public Vector3 Consume()
        {
            Vector3 value = accumulated;
            accumulated = Vector3.zero;
            return value;
        }
    }
}
