using System;
using System.Collections.Generic;
using UnityEngine;
using Alpha.Player;
namespace Alpha.Combat
{
    //Miscarea corpului in atacuri. Fara viteza constanta: ori miscarea reala din animatie (RootMotionRelay),
    //ori un impuls scurt cu clopot (accelereaza si franeaza repede). Plus shake-ul la momentul impactului.
    [DefaultExecutionOrder(100)]
    public class AttackMotion : MonoBehaviour
    {
        [Serializable]
        public class MotionEntry
        {
            public CombatAction action;
            public int layer;
            [Header("Miscare din animatie")]
            public bool useRootMotion;
            public float rootScale = 1f;
            public float maxRootDistance = 2.5f;
            [Header("Impuls scurt (clipuri fara miscare)")]
            public float burstDistance;
            public float burstDelay = 0.07f;
            public float burstDuration = 0.1f;
            [Header("Impact")]
            [Range(0f, 1f)] public float impactTime;
            public float impactShake;

            public MotionEntry() { }

            public MotionEntry(CombatAction action, int layer, bool useRootMotion, float burstDistance, float impactTime, float impactShake)
            {
                this.action = action;
                this.layer = layer;
                this.useRootMotion = useRootMotion;
                this.burstDistance = burstDistance;
                this.impactTime = impactTime;
                this.impactShake = impactShake;
            }
        }

        [SerializeField] private List<MotionEntry> motions = new List<MotionEntry>
        {
            new MotionEntry(CombatAction.Heavy, 0, true, 0f, 0.45f, 0f),
            new MotionEntry(CombatAction.LightPunch, 0, false, 0.45f, 0.3f, 0.06f),
            new MotionEntry(CombatAction.LightPunch2, 0, false, 0.45f, 0.3f, 0.06f),
            new MotionEntry(CombatAction.LightKick3, 0, false, 0.6f, 0.3f, 0.06f),
            new MotionEntry(CombatAction.Light, 1, false, 0f, 0.35f, 0.08f)
        };
        [SerializeField] private float maxSpeed = 15f;   //Plafon pentru viteza adaugata de atac (m/s)

        private PlayerCombatState combatState;
        private Animator animator;
        private Rigidbody body;
        private RootMotionRelay relay;
        private PlayerDash dash;
        private int lastStateHash;
        private float lastTime;
        private float attackStart;
        private float rootDistance;

        private void Awake()
        {
            combatState = GetComponentInParent<PlayerCombatState>();
            animator = combatState.GetComponentInChildren<Animator>();
            body = combatState.GetComponent<Rigidbody>();
            relay = animator.GetComponent<RootMotionRelay>();
            dash = combatState.GetComponentInChildren<PlayerDash>();
        }

        private MotionEntry ActiveEntry(out AnimatorStateInfo info)
        {
            info = default;
            CombatAction action = combatState.CurrentAction();
            int tag = Animator.StringToHash(action.ToString());
            foreach (MotionEntry entry in motions)
            {
                if (entry.action != action)
                    continue;
                info = animator.GetCurrentAnimatorStateInfo(entry.layer);
                if (info.tagHash != tag && animator.IsInTransition(entry.layer)) //Intrarea in atac conteaza de la inceputul tranzitiei
                    info = animator.GetNextAnimatorStateInfo(entry.layer);
                if (info.tagHash != tag || info.IsName("AirSlam")) //Plonjonul are alt feedback
                    return null;
                return entry;
            }
            return null;
        }

        private void FixedUpdate()
        {
            Vector3 rootDelta = relay != null ? relay.Consume() : Vector3.zero; //Consumat mereu, altfel s-ar aduna miscarea din mers
            if (body.isKinematic || !animator.GetBool("IsGrounded") || (dash != null && dash.IsDashing))
                return;
            MotionEntry entry = ActiveEntry(out AnimatorStateInfo info);
            if (entry == null)
                return;

            Vector3 add = Vector3.zero;
            float dt = Time.fixedDeltaTime;
            if (entry.useRootMotion && rootDelta.sqrMagnitude > 0f)
            {
                rootDelta *= entry.rootScale;
                float length = rootDelta.magnitude;
                float allowed = Mathf.Min(length, Mathf.Max(0f, entry.maxRootDistance - rootDistance));
                if (allowed > 0f && length > 0.0001f)
                {
                    add += rootDelta / length * (allowed / dt);
                    rootDistance += allowed;
                }
            }
            if (entry.burstDistance > 0f && entry.burstDuration > 0.01f)
            {
                float u = (Time.time - attackStart - entry.burstDelay) / entry.burstDuration;
                if (u >= 0f && u <= 1f) //6u(1-u) are integrala 1: distanta totala = burstDistance
                    add += body.transform.forward * (entry.burstDistance / entry.burstDuration * 6f * u * (1f - u));
            }
            add.y = 0f;
            if (add.sqrMagnitude < 0.0001f) //Nu ating viteza cand nu e nimic de aplicat
                return;
            if (add.magnitude > maxSpeed)
                add = add.normalized * maxSpeed;
            Vector3 velocity = body.linearVelocity; //Adaug peste viteza lui PlayerController (0 pe loc in atacurile InPlace)
            velocity.x += add.x;
            velocity.z += add.z;
            body.linearVelocity = velocity;
        }

        private void Update()
        {
            MotionEntry entry = ActiveEntry(out AnimatorStateInfo info);
            if (entry == null)
            {
                lastStateHash = 0;
                return;
            }
            float time = info.normalizedTime;
            bool newState = info.fullPathHash != lastStateHash || time < lastTime; //Alta animatie sau aceeasi reluata
            lastStateHash = info.fullPathHash;
            if (newState)
            {
                attackStart = Time.time;
                rootDistance = 0f;
            }
            else if (lastTime < entry.impactTime && time >= entry.impactTime && entry.impactShake > 0f && CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.Shake(entry.impactShake, 0.15f);
                CombatFeedback.Instance.FovKick(entry.impactShake * 20f);
            }
            lastTime = time;
        }
    }
}
