using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Alpha.Combat;
using Alpha.Interfaces;
using UnityEngine;
using UnityEngine.AI;

namespace Alpha.World
{
    // Lupta cavalerului: inainteaza incet cu scutul spre jucator, impunge cu lancea de la distanta
    // si matura cand jucatorul e prea aproape sau ii intra in flanc. Dupa fiecare atac se intoarce greu,
    // asa ca spatele lui ramane deschis cateva clipe.
    public class KnightAI : MonoBehaviour
    {
        [Serializable]
        public class Strike
        {
            public string state = "Thrust";
            public int frames = 50;
            public int hitFrom = 15;
            public int hitTo = 21;
            public float damage = 40f;
            public float healthShare;       // cat din viata jucatorului ia lovitura (0 = damage fix)
            public float knockBack = 4f;
            public float windupSpeed = 0.75f;
            public float aimOffset;
            public float lunge;             // m/s spre jucator in pregatire, daca a ramas prea departe
            public bool heavy;
        }

        enum Mode { Throne, Rising, Idle, Fight, Attack, Stagger, Defeated }

        [SerializeField] OathboundKnight knight;
        [SerializeField] KnightFx fx;
        [SerializeField] Animator animator;
        [SerializeField] NavMeshAgent agent;
        [SerializeField] Transform lanceGrip;
        [SerializeField] Transform lanceTip;
        [SerializeField] GameObject hitParticlesPrefab;
        [SerializeField] LayerMask targetLayers = 384;
        [SerializeField] Rect arena = Rect.MinMaxRect(402f, 380.5f, 438f, 429.5f);

        [Header("Miscare")]
        [SerializeField] float walkSpeed = 1.05f;
        [SerializeField] float turnSpeed = 70f;
        [SerializeField] float phaseTwoTurnSpeed = 95f;
        [SerializeField] float recoveryTurnSpeed = 20f;
        [SerializeField] float angryTurnBoost = 1.6f;
        [SerializeField] float angryTime = 1.5f;
        [SerializeField] float keepDistance = 2.4f;

        [Header("Atacuri")]
        [SerializeField] Strike thrust = new Strike { state = "Thrust", frames = 50, hitFrom = 15, hitTo = 21, damage = 45f, healthShare = 0.09f, knockBack = 4f, windupSpeed = 0.75f, heavy = true };
        [SerializeField] Strike sweep = new Strike { state = "Sweep", frames = 60, hitFrom = 22, hitTo = 34, damage = 35f, healthShare = 0.06f, knockBack = 4f, windupSpeed = 0.8f };
        [SerializeField] Strike followUp = new Strike { state = "Thrust2", frames = 50, hitFrom = 15, hitTo = 21, damage = 35f, healthShare = 0.06f, knockBack = 3.5f, windupSpeed = 1f, lunge = 4.5f, heavy = true };
        [SerializeField] float sweepRange = 2.6f;
        [SerializeField] float thrustRange = 3.6f;
        [SerializeField] float thrustAngle = 25f;
        [SerializeField] float flankAngle = 55f;
        [SerializeField] float flankRange = 3.6f;
        [SerializeField] float telegraph = 0.45f;
        [SerializeField] float phaseTwoTelegraph = 0.25f;
        [SerializeField] float recovery = 1.5f;
        [SerializeField] float phaseTwoRecovery = 0.9f;
        [SerializeField] float cooldown = 1.2f;
        [SerializeField] float phaseTwoCooldown = 0.5f;
        [SerializeField] float phaseTwoWindup = 1.15f;
        [SerializeField] float lanceRadius = 0.3f;
        [SerializeField] float comboKnockBack = 1.5f;   // prima impunsatura din dubla impinge mai putin
        [SerializeField] float lungeStop = 2.7f;

        [Header("Lovit")]
        [SerializeField] float staggerTime = 0.6f;
        [SerializeField] float staggerCooldown = 2.5f;
        [SerializeField] float backLurch = 12f;
        [SerializeField] float blockLurch = -6f;

        const float Fps = 30f;
        static readonly FieldInfo MaxHealthField = typeof(HittableCharacter).GetField("Max_Health", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo ArmorField = typeof(HittableCharacter).GetField("armor", BindingFlags.Instance | BindingFlags.NonPublic);
        const int GroundMask = ~((1 << 2) | (1 << 6) | (1 << 7) | (1 << 8));

        static readonly int GuardState = Animator.StringToHash("Guard");
        static readonly int WalkState = Animator.StringToHash("Walk");
        static readonly int RiseState = Animator.StringToHash("Rise");
        static readonly int KneelState = Animator.StringToHash("Kneel");
        static readonly int SitState = Animator.StringToHash("Sit");
        static readonly int AttackSpeed = Animator.StringToHash("AttackSpeed");
        static readonly int WalkSpeedParam = Animator.StringToHash("WalkSpeed");

        readonly Collider[] overlap = new Collider[16];
        readonly HashSet<HittableCharacter> struck = new HashSet<HittableCharacter>();
        readonly EnemyActionDefinition hitDef = new EnemyActionDefinition();

        Mode mode = Mode.Throne;
        Transform target, spine;
        Strike active;
        Coroutine attackRoutine;
        Vector3 prevTip;
        bool tipValid, walking;
        float speed, nextAttack, recoverUntil, angryUntil, staggerReady, repathAt, targetSearchAt;
        float lurchAmount, lurchTime = 99f;
        Vector3 homePosition;
        Quaternion homeRotation;

        public bool IsFighting => mode == Mode.Fight || mode == Mode.Attack || mode == Mode.Stagger;
        public bool IsAttacking => mode == Mode.Attack;
        public bool InHitWindow => active != null;
        public string CurrentStrike { get; private set; }
        public bool IsWalking => walking;
        public bool IsDefeated => mode == Mode.Defeated;

        bool PhaseTwo => knight != null && knight.InPhaseTwo;

        void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (animator != null && animator.isHuman) spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            homePosition = transform.position;
            homeRotation = transform.rotation;
            if (agent != null)
            {
                agent.updatePosition = false;
                agent.updateRotation = false;
                agent.enabled = false;
            }
        }

        void OnEnable() { if (knight != null) knight.PhaseTwo += HandlePhaseTwo; }
        void OnDisable() { if (knight != null) knight.PhaseTwo -= HandlePhaseTwo; }

        // ------------------------------------------------------------------ trezirea

        public Coroutine Rise() => StartCoroutine(rise());

        IEnumerator rise()
        {
            if (mode != Mode.Throne) yield break;
            mode = Mode.Rising;
            animator.CrossFadeInFixedTime(RiseState, 0.25f);
            yield return null;
            for (float t = 0f; t < 8f; t += Time.deltaTime)
            {
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                if (info.shortNameHash == RiseState && !animator.IsInTransition(0) && info.normalizedTime >= 1f) break;
                yield return null;
            }

            // la capatul ridicarii soldul e cu 0.4 m in fata radacinii: mutam radacina sub el, fara salt
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Vector3 before = hips != null ? hips.position : transform.position;
            animator.Play(GuardState, 0, 0f);
            animator.Update(0f);
            if (hips != null)
            {
                Vector3 delta = before - hips.position;
                delta.y = 0f;
                transform.position += delta;
            }
            mode = Mode.Idle;
        }

        public void Engage()
        {
            if (mode == Mode.Defeated) return;
            if (agent != null)
            {
                agent.enabled = true;
                // Warp muta si transform-ul la inaltimea NavMesh-ului; picioarele raman unde sunt
                Vector3 feet = transform.position;
                if (NavMesh.SamplePosition(feet, out NavMeshHit hit, 3f, NavMesh.AllAreas)) agent.Warp(hit.position);
                transform.position = feet;
                agent.speed = walkSpeed;
            }
            mode = Mode.Fight;
            nextAttack = Time.time + 1f;
        }

        public void Defeat()
        {
            if (mode == Mode.Defeated) return;
            mode = Mode.Defeated;
            StopAllCoroutines();
            attackRoutine = null;
            active = null;
            CurrentStrike = null;
            if (agent != null) agent.enabled = false;
            if (fx != null)
            {
                fx.SetEnraged(false);
                fx.SetTrail(false);
            }
            animator.SetFloat(AttackSpeed, 1f);
            animator.CrossFadeInFixedTime(KneelState, 0.35f);
        }

        // jucatorul a murit: cavalerul se intoarce pe tron, gata pentru o noua incercare
        public void ResetToThrone()
        {
            StopAllCoroutines();
            attackRoutine = null;
            active = null;
            CurrentStrike = null;
            target = null;
            speed = 0f;
            walking = false;
            lurchTime = 99f;
            nextAttack = recoverUntil = angryUntil = staggerReady = 0f;
            if (agent != null) agent.enabled = false;
            if (fx != null)
            {
                fx.SetEnraged(false);
                fx.SetTrail(false);
            }
            transform.SetPositionAndRotation(homePosition, homeRotation);
            animator.SetFloat(AttackSpeed, 1f);
            animator.Play(SitState, 0, 0f);
            mode = Mode.Throne;
        }

        // ------------------------------------------------------------------ lupta

        void Update()
        {
            if (mode != Mode.Fight && mode != Mode.Attack && mode != Mode.Stagger) return;
            FindTarget();
            if (mode != Mode.Fight || target == null)
            {
                SetWalking(false, 0f);
                return;
            }

            Vector3 toTarget = Flat(target.position - transform.position);
            float dist = toTarget.magnitude;
            Face(toTarget, CurrentTurnSpeed());

            if (Time.time >= nextAttack && Time.time >= recoverUntil)
            {
                float angle = Vector3.Angle(Flat(transform.forward), toTarget);
                if (dist <= sweepRange || (angle >= flankAngle && dist <= flankRange))
                {
                    attackRoutine = StartCoroutine(attack(sweep));
                    return;
                }
                if (dist <= thrustRange && angle <= thrustAngle)
                {
                    attackRoutine = StartCoroutine(attack(thrust));
                    return;
                }
            }
            Advance(toTarget, dist);
        }

        void Advance(Vector3 toTarget, float dist)
        {
            float want = 0f;
            Vector3 dir = dist > 0.001f ? toTarget / dist : transform.forward;
            if (dist > keepDistance && Time.time >= recoverUntil)
            {
                if (agent != null && agent.isOnNavMesh)
                {
                    if (Time.time >= repathAt)
                    {
                        repathAt = Time.time + 0.25f;
                        agent.SetDestination(target.position);
                    }
                    Vector3 desired = Flat(agent.desiredVelocity);
                    if (desired.sqrMagnitude > 0.01f) dir = desired.normalized;
                }
                // merge doar cat timp priveste in directia de mers, altfel se intoarce intai
                want = walkSpeed * Mathf.Max(0f, Vector3.Dot(Flat(transform.forward), dir));
            }
            speed = Mathf.MoveTowards(speed, want, 3f * Time.deltaTime);
            Move(Vector3.Lerp(Flat(transform.forward), dir, 0.5f).normalized * speed * Time.deltaTime);
            SetWalking(speed > 0.2f, speed / walkSpeed);
        }

        void Move(Vector3 step)
        {
            Vector3 pos = transform.position + step;
            pos.x = Mathf.Clamp(pos.x, arena.xMin, arena.xMax);
            pos.z = Mathf.Clamp(pos.z, arena.yMin, arena.yMax);
            if (agent != null && agent.isOnNavMesh)
            {
                // NavMesh-ul tine cavalerul in curte, departe de ziduri si de tron
                agent.nextPosition = pos;
                Vector3 onMesh = agent.nextPosition;
                pos.x = onMesh.x;
                pos.z = onMesh.z;
            }
            if (Physics.Raycast(pos + Vector3.up * 0.45f, Vector3.down, out RaycastHit hit, 3f, GroundMask, QueryTriggerInteraction.Ignore) && hit.normal.y > 0.6f)
                pos.y = Mathf.MoveTowards(transform.position.y, hit.point.y, 3f * Time.deltaTime);
            else
                pos.y = transform.position.y;
            transform.position = pos;
        }

        void Face(Vector3 dir, float degreesPerSecond)
        {
            dir = Flat(dir);
            if (dir.sqrMagnitude < 0.0001f) return;
            Quaternion want = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, degreesPerSecond * Time.deltaTime);
        }

        float CurrentTurnSpeed()
        {
            if (Time.time < recoverUntil) return recoveryTurnSpeed;
            float t = PhaseTwo ? phaseTwoTurnSpeed : turnSpeed;
            return Time.time < angryUntil ? t * angryTurnBoost : t;
        }

        void SetWalking(bool on, float pace)
        {
            animator.SetFloat(WalkSpeedParam, Mathf.Max(0.3f, pace));
            if (on == walking) return;
            walking = on;
            animator.CrossFadeInFixedTime(on ? WalkState : GuardState, 0.25f);
        }

        IEnumerator attack(Strike first)
        {
            mode = Mode.Attack;
            speed = 0f;
            SetWalking(false, 0f);

            // semnalul: lancea sclipeste si cavalerul se aseaza in garda inainte sa loveasca
            if (fx != null) fx.Telegraph();
            float hold = PhaseTwo ? phaseTwoTelegraph : telegraph;
            for (float t = 0f; t < hold; t += Time.deltaTime)
            {
                Aim(first, CurrentTurnSpeed());
                yield return null;
            }

            // in faza a doua impunsatura vine dubla
            for (Strike s = first; s != null && mode == Mode.Attack; s = PhaseTwo && s == thrust ? followUp : null)
            {
                if (s != first && fx != null) fx.Telegraph();
                if (fx != null) fx.SetTrail(s == sweep);
                CurrentStrike = s.state;
                struck.Clear();
                tipValid = false;
                float boost = PhaseTwo ? phaseTwoWindup : 1f;
                animator.SetFloat(AttackSpeed, s.windupSpeed * boost);
                animator.CrossFadeInFixedTime(s.state, 0.12f, 0, 0f);
                for (float frame = 0f; frame < s.frames;)
                {
                    float rate = frame < s.hitFrom ? s.windupSpeed * boost : 1f;
                    animator.SetFloat(AttackSpeed, rate);
                    if (frame < s.hitFrom) Aim(s, CurrentTurnSpeed() * 0.6f);
                    if (s.lunge > 0f && frame <= s.hitTo) Lunge(s.lunge);
                    active = frame >= s.hitFrom && frame <= s.hitTo ? s : null;
                    yield return null;
                    frame += Time.deltaTime * Fps * rate;
                }
                active = null;
            }

            if (fx != null) fx.SetTrail(false);
            CurrentStrike = null;
            animator.SetFloat(AttackSpeed, 1f);
            animator.CrossFadeInFixedTime(GuardState, 0.3f);
            recoverUntil = Time.time + (PhaseTwo ? phaseTwoRecovery : recovery);
            nextAttack = recoverUntil + (PhaseTwo ? phaseTwoCooldown : cooldown);
            attackRoutine = null;
            mode = Mode.Fight;
        }

        // a doua impunsatura: dupa ce prima l-a impins pe jucator, cavalerul face un pas lung dupa el
        void Lunge(float metersPerSecond)
        {
            if (target == null) return;
            Vector3 to = Flat(target.position - transform.position);
            if (to.magnitude <= lungeStop) return;
            Move(Flat(transform.forward) * metersPerSecond * Time.deltaTime);
        }

        void Aim(Strike s, float degreesPerSecond)
        {
            if (target == null) return;
            Vector3 to = Quaternion.Euler(0f, s.aimOffset, 0f) * Flat(target.position - transform.position);
            Face(to, degreesPerSecond);
        }

        void FindTarget()
        {
            if (target != null && Time.time < targetSearchAt) return;
            targetSearchAt = Time.time + 1f;
            HittableCharacter closest = HittableCharacter.Closest(HittableCharacter.allys, transform.position);
            target = closest != null ? closest.transform : null;
        }

        // ------------------------------------------------------------------ lovituri primite

        public void OnHit(bool fromBehind)
        {
            if (mode == Mode.Defeated) return;
            lurchAmount = fromBehind ? backLurch : blockLurch;
            lurchTime = 0f;
            if (!fromBehind) return;

            angryUntil = Time.time + angryTime;
            // in fereastra de lovire nu se opreste: atacul pornit se duce pana la capat
            if (active != null || Time.time < staggerReady || !IsFighting) return;
            staggerReady = Time.time + staggerCooldown;
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            attackRoutine = StartCoroutine(stagger());
        }

        IEnumerator stagger()
        {
            mode = Mode.Stagger;
            if (fx != null) fx.SetTrail(false);
            active = null;
            CurrentStrike = null;
            speed = 0f;
            walking = false;
            animator.SetFloat(AttackSpeed, 1f);
            animator.CrossFadeInFixedTime(GuardState, 0.12f);
            yield return new WaitForSeconds(staggerTime);
            recoverUntil = Time.time + 0.2f;
            nextAttack = Mathf.Max(nextAttack, Time.time + 0.6f);
            attackRoutine = null;
            mode = Mode.Fight;
        }

        void HandlePhaseTwo()
        {
            if (fx != null) fx.SetEnraged(true);
            angryUntil = Time.time + angryTime;
        }

        // ------------------------------------------------------------------ dupa animatie

        void LateUpdate()
        {
            ApplyLurch();
            if (active == null || lanceGrip == null || lanceTip == null || mode == Mode.Defeated)
            {
                tipValid = false;
                return;
            }
            Vector3 grip = lanceGrip.position, tip = lanceTip.position;
            Sweep(grip, tip, tip);
            if (tipValid) Sweep(prevTip, tip, tip);
            prevTip = tip;
            tipValid = true;
        }

        void Sweep(Vector3 a, Vector3 b, Vector3 tip)
        {
            int count = Physics.OverlapCapsuleNonAlloc(a, b, lanceRadius, overlap, targetLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider other = overlap[i];
                HittableCharacter victim = other.GetComponentInParent<HittableCharacter>();
                if (victim == null || victim == knight || !struck.Add(victim)) continue;
                bool closest = other is BoxCollider || other is SphereCollider || other is CapsuleCollider || (other is MeshCollider mesh && mesh.convex);
                Vector3 point = closest ? other.ClosestPoint(tip) : other.bounds.center;
                if (hitParticlesPrefab != null)
                    Instantiate(hitParticlesPrefab, point, Quaternion.LookRotation(point - a + Vector3.up * 0.01f));
                hitDef.damage = DamageFor(active, victim);
                hitDef.knockBackPower = active == thrust && PhaseTwo ? comboKnockBack : active.knockBack;
                victim.HandleGotHit(hitDef, knight);
                if (fx != null) fx.PlayerHit(point, active.heavy);
            }
        }

        // viata si armura jucatorului sunt private in HittableCharacter: le citim prin reflexie, ca BossHealthReader
        static float DamageFor(Strike s, HittableCharacter victim)
        {
            if (s.healthShare <= 0f || MaxHealthField == null || ArmorField == null) return s.damage;
            float max = (float)MaxHealthField.GetValue(victim);
            float armor = (float)ArmorField.GetValue(victim);
            return max > 0f ? s.healthShare * max * Mathf.Max(armor, 0.01f) : s.damage;
        }

        // tresarirea trunchiului la lovitura, peste animatie
        void ApplyLurch()
        {
            if (spine == null || lurchTime > 1f) return;
            lurchTime += Time.deltaTime;
            float k = lurchTime < 0.08f ? lurchTime / 0.08f : Mathf.Exp(-(lurchTime - 0.08f) * 7f);
            spine.rotation = Quaternion.AngleAxis(lurchAmount * k, transform.right) * spine.rotation;
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
