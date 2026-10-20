using UnityEngine;
namespace Alpha.Combat
{
    //Sta pe obiectul cu Animator. Cat timp atacul tine arma cu doua maini, mana stanga e dusa pe Grip_L al armei (IK).
    //Layerele animatorului trebuie sa aiba IK Pass bifat (il pune Setup Combat Polish).
    [RequireComponent(typeof(Animator))]
    public class LeftHandGrip : MonoBehaviour
    {
        [Header("Blend")]
        [SerializeField] private float blendIn = 0.1f;
        [SerializeField] private float blendOut = 0.15f;

        [Header("Pozitie")]
        [SerializeField, Range(0f, 1f)] private float positionWeight = 1f;
        [Tooltip("Decalaj in spatiul lui Grip_L (metri)")]
        [SerializeField] private Vector3 positionOffset;
        [Tooltip("Metri de-a lungul manerului (axa Y a lui Grip_L): + spre capul armei, - spre varf")]
        [SerializeField] private float gripSlide;
        [Tooltip("Animatorul citeste pozitia armei cu un cadru intarziere; 1 = compensez cu miscarea ultimului cadru, 0 = oprit")]
        [SerializeField, Range(0f, 2f)] private float lead = 1f;

        [Header("Rotatie")]
        [Tooltip("0 = mana pastreaza rotatia din animatie, 1 = se aliniaza cu Grip_L")]
        [SerializeField, Range(0f, 1f)] private float rotationWeight;
        [SerializeField] private Vector3 rotationOffsetEuler;

        [Header("Cot")]
        [SerializeField, Range(0f, 1f)] private float elbowWeight;
        [Tooltip("Pozitia indicatorului de cot, in spatiul corpului")]
        [SerializeField] private Vector3 elbowHint = new Vector3(-0.6f, 1.1f, -0.2f);

        [SerializeField] private bool debugLog;

        private const string GripName = "Grip_L";
        private static readonly int HeavyTag = Animator.StringToHash("Heavy");
        private static readonly int AbilityTag = Animator.StringToHash("Ability");
        private static readonly int LightTag = Animator.StringToHash("Light");

        private Animator animator;
        private WeaponSocket[] sockets;
        private Weapon weapon;
        private Transform grip;
        private float weight;
        private Vector3 previousGripPosition;
        private Vector3 gripDelta;
        private bool hasPrevious;
        private int lastFrame = -1;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            sockets = GetComponentsInChildren<WeaponSocket>(true);
        }

        private void OnDisable()
        {
            weight = 0f;
            hasPrevious = false;
        }

        private Weapon HeldWeapon()
        {
            for (int i = 0; i < sockets.Length; i++)
                if (sockets[i] != null && sockets[i].HasWeapon)
                    return sockets[i].CurrentWeapon;
            return null;
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name)
                    return child;
            return null;
        }

        private bool TwoHanded()
        {
            return StateHasTag(0, HeavyTag) || StateHasTag(0, AbilityTag)
                || (animator.layerCount > 1 && StateHasTag(1, LightTag));
        }

        private bool StateHasTag(int layer, int tag) //Intr-o tranzitie conteaza starea spre care se merge
        {
            AnimatorStateInfo info = animator.IsInTransition(layer)
                ? animator.GetNextAnimatorStateInfo(layer)
                : animator.GetCurrentAnimatorStateInfo(layer);
            return info.tagHash == tag;
        }

        private void Update()
        {
            Weapon held = HeldWeapon();
            if (held != weapon)
            {
                weapon = held;
                grip = weapon != null ? FindChild(weapon.transform, GripName) : null;
                hasPrevious = false;
                if (debugLog)
                    Debug.Log("[LeftHandGrip] arma: " + (weapon != null ? weapon.name : "-") + ", grip: " + (grip != null ? grip.name : "-"));
            }
            bool want = grip != null && TwoHanded();
            float time = want ? blendIn : blendOut;
            float step = time > 0.001f ? Time.unscaledDeltaTime / time : 1f;
            weight = Mathf.MoveTowards(weight, want ? 1f : 0f, step);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (weight <= 0.001f || grip == null)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
                animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 0f);
                return;
            }
            if (Time.frameCount != lastFrame) //Callback-ul vine pe fiecare layer cu IK Pass: calculez o singura data pe cadru
            {
                lastFrame = Time.frameCount;
                Vector3 current = grip.position;
                gripDelta = hasPrevious ? Vector3.ClampMagnitude(current - previousGripPosition, 0.3f) : Vector3.zero;
                previousGripPosition = current;
                hasPrevious = true;
            }

            Vector3 target = grip.position + gripDelta * lead
                + grip.TransformVector(positionOffset)
                + grip.TransformDirection(Vector3.up) * gripSlide;
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight * positionWeight);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, target);

            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, weight * rotationWeight);
            if (rotationWeight > 0f)
                animator.SetIKRotation(AvatarIKGoal.LeftHand, grip.rotation * Quaternion.Euler(rotationOffsetEuler));

            animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, weight * elbowWeight);
            if (elbowWeight > 0f)
                animator.SetIKHintPosition(AvatarIKHint.LeftElbow, transform.TransformPoint(elbowHint));
        }
    }
}
