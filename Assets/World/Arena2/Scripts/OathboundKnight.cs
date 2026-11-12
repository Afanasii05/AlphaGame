using System;
using System.Reflection;
using Alpha.Combat;
using Alpha.Interfaces;
using UnityEngine;

namespace Alpha.World
{
    // Cavalerul din sala tronului. Scutul acopera fata: loviturile din fata trec doar putin,
    // cele din spate (inclusiv ciocanul aruncat sau rechemat prin spatele lui) intra complet.
    [RequireComponent(typeof(Rigidbody))]
    public class OathboundKnight : HittableCharacter
    {
        [SerializeField] KnightAI ai;
        [SerializeField] KnightFx fx;
        [SerializeField] Collider body;
        [SerializeField] Collider shield;
        [SerializeField] float guardAngle = 70f;
        [SerializeField] float guardDamage = 0.1f;
        [SerializeField] float phaseTwoAt = 0.5f;
        [SerializeField] float weaponReach = 6f;

        public event Action<OathboundKnight> Defeated;
        public event Action PhaseTwo;

        public bool IsAwake { get; private set; }
        public bool IsDefeated { get; private set; }
        public bool InPhaseTwo { get; private set; }
        public bool LastHitFromBehind { get; private set; }
        public float Health => health != null ? health.Fraction : 1f;

        readonly EnemyActionDefinition blocked = new EnemyActionDefinition();
        BossHealthReader health;
        Weapon[] weapons = new Weapon[0];
        Vector3[] lastHeads = new Vector3[0];
        float weaponsAt;
        Vector3 nextSource;
        int nextSourceFrame = -1;

        protected override void Awake()
        {
            base.Awake();
            health = new BossHealthReader(this);
        }

        void OnDisable() => enemies.Remove(this);

        // intra in lista de inamici abia cand se ridica: pana atunci nu il tintesti si nu are bara
        public void Awaken()
        {
            if (IsAwake || IsDefeated) return;
            IsAwake = true;
            if (!enemies.Contains(this)) enemies.Add(this);
        }

        // noua incercare dupa moartea jucatorului: viata plina, inapoi in somn
        public void ResetForRetry()
        {
            if (IsDefeated) return;
            IsAwake = false;
            InPhaseTwo = false;
            LastHitFromBehind = false;
            enemies.Remove(this);
            FieldInfo current = typeof(HittableCharacter).GetField("currentHealth", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo max = typeof(HittableCharacter).GetField("Max_Health", BindingFlags.Instance | BindingFlags.NonPublic);
            if (current != null && max != null) current.SetValue(this, max.GetValue(this));
            UpdateHealthLight();
        }

        public override void HandleGotHit(CharacterActionDefinition attack, HittableCharacter attacker)
        {
            if (!IsAwake || IsDefeated || attack == null || attacker == null) return;

            Vector3 source = HitSource(attacker);
            LastHitFromBehind = IsBehind(source);
            if (LastHitFromBehind)
            {
                if (fx != null) fx.BackHit(Closest(body, source), Outward(source));
                base.HandleGotHit(attack, attacker);
            }
            else
            {
                blocked.damage = attack.damage * guardDamage;
                blocked.knockBackPower = 0f;
                if (fx != null) fx.Block(Closest(shield != null ? shield : body, source), Outward(source));
                base.HandleGotHit(blocked, attacker);
            }

            if (!InPhaseTwo && !IsDefeated && Health <= phaseTwoAt)
            {
                InPhaseTwo = true;
                PhaseTwo?.Invoke();
            }
        }

        public bool IsBehind(Vector3 point)
        {
            Vector3 to = point - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) return false;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            return Vector3.Angle(forward, to) > guardAngle;
        }

        // pozitia armelor din cadrul trecut, ca sa stim din ce parte vine una aflata in zbor
        void Update()
        {
            if (!IsAwake || IsDefeated) return;
            if (Time.time >= weaponsAt)
            {
                weaponsAt = Time.time + 1f;
                Weapon[] found = FindObjectsByType<Weapon>();
                if (found.Length != weapons.Length || Array.IndexOf(weapons, null) >= 0)
                {
                    weapons = found;
                    lastHeads = new Vector3[found.Length];
                    for (int i = 0; i < found.Length; i++) lastHeads[i] = Head(found[i]);
                }
            }
            for (int i = 0; i < weapons.Length; i++)
                if (weapons[i] != null) lastHeads[i] = Head(weapons[i]);
        }

        // pentru lovituri calculate in afara fizicii (ciocanul rechemat): de unde a venit
        public void NextHitFrom(Vector3 point)
        {
            nextSource = point;
            nextSourceFrame = Time.frameCount;
        }

        // de unde vine lovitura: in mana conteaza unde sta jucatorul; aruncata sau rechemata,
        // conteaza din ce directie a venit ciocanul
        Vector3 HitSource(HittableCharacter attacker)
        {
            if (nextSourceFrame == Time.frameCount) return nextSource;
            Vector3 center = Center();
            Vector3 source = attacker.transform.position;
            float bestSqr = weaponReach * weaponReach;
            for (int i = 0; i < weapons.Length; i++)
            {
                Weapon weapon = weapons[i];
                if (weapon == null || !Flying(weapon)) continue;
                Vector3 head = Head(weapon);
                float d = (head - center).sqrMagnitude;
                if (d >= bestSqr) continue;
                bestSqr = d;
                Vector3 travel = head - lastHeads[i];
                source = travel.sqrMagnitude > 0.0004f ? head - travel.normalized * 2f : head;
            }
            return source;
        }

        static bool Flying(Weapon weapon) => !weapon.IsHeld || weapon.GetComponentInParent<WeaponSocket>() == null;

        static Vector3 Head(Weapon weapon) => weapon.headHitbox != null ? weapon.headHitbox.bounds.center : weapon.transform.position;

        Vector3 Center() => body != null ? body.bounds.center : transform.position + Vector3.up * 1.3f;

        Vector3 Outward(Vector3 source)
        {
            Vector3 dir = source - Center();
            dir.y = 0f;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
        }

        Vector3 Closest(Collider c, Vector3 point)
        {
            if (c == null || !c.enabled) return Center();
            return c.ClosestPoint(point);
        }

        protected override void StartHitState()
        {
            if (ai != null) ai.OnHit(LastHitFromBehind);
        }

        // nu e impins de lovituri: sta in scut
        protected override void handleKnockbackLogic(Vector3 totalKnockback) { }

        protected override void EndCharacter()
        {
            if (IsDefeated) return;
            IsDefeated = true;
            enemies.Remove(this);
            if (ai != null) ai.Defeat();
            Defeated?.Invoke(this);
        }
    }
}
