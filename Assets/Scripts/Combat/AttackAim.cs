using System.Collections;
using UnityEngine;
using Alpha.Player;
namespace Alpha.Combat
{
    //Roteste modelul cu un unghi fix in timpul atacurilor, ca lovitura sa cada in fata (centrul camerei), nu intr-o parte.
    //Se aplica dupa PlayerDirectionalAnimation (care seteaza rotatia modelului in Update).
    [DefaultExecutionOrder(50)]
    public class AttackAim : MonoBehaviour
    {
        public enum Kind { None, Heavy, Light, AirSlash, AirSlam, Ability, Punch }

        [Header("Offset yaw (grade) pe atac - masurat de Tools/Alpha/Setup Combat Polish")]
        [SerializeField] private float heavyYaw;
        [SerializeField] private float lightYaw;
        [SerializeField] private float airSlashYaw;
        [SerializeField] private float airSlamYaw;
        [SerializeField] private float abilityYaw;
        [SerializeField] private float punchYaw;
        [Tooltip("Daca e bifat, setup-ul nu mai rescrie valorile de mai sus")]
        [SerializeField] private bool lockOffsets;

        [Header("Comportament")]
        [SerializeField] private float blendTime = 0.08f;
        [SerializeField] private bool turnBodyOnLight = true;   //Corpul se intoarce spre camera la lovitura usoara (ca la Heavy)
        [SerializeField] private float turnTime = 0.15f;
        [SerializeField] private bool debugLog;

        private static readonly int HeavyState = Animator.StringToHash("HeavyDoubleClickHit");
        private static readonly int AirSlamState = Animator.StringToHash("AirSlam");
        private static readonly int AbilityState = Animator.StringToHash("SpinningAbility");
        private static readonly int PunchState = Animator.StringToHash("punch");
        private static readonly int Punch2State = Animator.StringToHash("punch2");
        private static readonly int KickState = Animator.StringToHash("kick");
        private static readonly int HittingState = Animator.StringToHash("Hitting");
        private static readonly int AirSlashState = Animator.StringToHash("AirSlash");
        private static readonly int AirPunchState = Animator.StringToHash("AirPunch");

        private PlayerCombatState combatState;
        private Animator animator;
        private Transform model;
        private Rigidbody body;
        private CameraController cameraController;
        private Coroutine turning;
        private Kind kind;
        private Kind shownKind;
        private float weight;
        private float currentOffset;

        public Kind Current { get { return kind; } }

        private void Awake()
        {
            combatState = GetComponentInParent<PlayerCombatState>();
            animator = combatState.GetComponentInChildren<Animator>();
            model = animator.transform;
            body = combatState.GetComponent<Rigidbody>();
        }

        private void OnDisable()
        {
            turning = null;
            kind = Kind.None;
            weight = 0f;
        }

        private Kind Detect()
        {
            Kind found = LayerKind(0);
            if (found == Kind.None && animator.layerCount > 1)
                found = LayerKind(1);
            return found;
        }

        private Kind LayerKind(int layer)
        {
            AnimatorStateInfo info = animator.IsInTransition(layer)
                ? animator.GetNextAnimatorStateInfo(layer)
                : animator.GetCurrentAnimatorStateInfo(layer);
            int hash = info.shortNameHash;
            if (layer == 0)
            {
                if (hash == HeavyState) return Kind.Heavy;
                if (hash == AirSlamState) return Kind.AirSlam;
                if (hash == AbilityState) return Kind.Ability;
                if (hash == PunchState || hash == Punch2State || hash == KickState) return Kind.Punch;
                return Kind.None;
            }
            if (hash == HittingState) return Kind.Light;
            if (hash == AirSlashState) return Kind.AirSlash;
            if (hash == AirPunchState) return Kind.Punch;
            return Kind.None;
        }

        private float OffsetFor(Kind value)
        {
            switch (value)
            {
                case Kind.Heavy: return heavyYaw;
                case Kind.Light: return lightYaw;
                case Kind.AirSlash: return airSlashYaw;
                case Kind.AirSlam: return airSlamYaw;
                case Kind.Ability: return abilityYaw;
                case Kind.Punch: return punchYaw;
                default: return 0f;
            }
        }

        private static bool FacesForward(Kind value) //Aceste atacuri ignora rotatia directionala (strafe) a modelului
        {
            return value == Kind.Heavy || value == Kind.Light || value == Kind.AirSlash || value == Kind.AirSlam;
        }

        private void Update()
        {
            Kind detected = Detect();
            if (detected != kind)
            {
                if (debugLog)
                    Debug.Log("[AttackAim] " + kind + " -> " + detected + " (offset " + OffsetFor(detected) + ")");
                if (turnBodyOnLight && (detected == Kind.Light || detected == Kind.AirSlash))
                    StartTurn();
                kind = detected;
                if (kind != Kind.None)
                    shownKind = kind;
            }

            float dt = Time.unscaledDeltaTime;
            float step = blendTime > 0.001f ? dt / blendTime : 1f;
            weight = Mathf.MoveTowards(weight, kind != Kind.None ? 1f : 0f, step);
            if (weight <= 0.001f)
            {
                currentOffset = OffsetFor(shownKind);
                return;
            }
            currentOffset = Mathf.MoveTowardsAngle(currentOffset, OffsetFor(shownKind), 720f * dt);

            Quaternion baseRotation = model.localRotation;
            Quaternion yaw = Quaternion.Euler(0f, currentOffset, 0f);
            Quaternion aimed = FacesForward(shownKind) ? yaw : baseRotation * yaw;
            model.localRotation = Quaternion.Slerp(baseRotation, aimed, weight);
        }

        private void StartTurn()
        {
            if (Camera.main == null)
                return;
            if (cameraController == null)
                cameraController = Camera.main.GetComponent<CameraController>();
            if (cameraController != null && !cameraController.is3D)
                return;
            if (turning != null)
                StopCoroutine(turning);
            turning = StartCoroutine(TurnBody());
        }

        private IEnumerator TurnBody()
        {
            float duration = Mathf.Max(turnTime, 0.02f);
            Quaternion start = body.rotation;
            Quaternion target = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
            for (float t = 0f; t < duration; t += Time.fixedDeltaTime)
            {
                body.MoveRotation(Quaternion.Slerp(start, target, t / duration));
                yield return new WaitForFixedUpdate();
            }
            body.MoveRotation(target);
            turning = null;
        }
    }
}
