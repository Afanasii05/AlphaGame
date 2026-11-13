using System;
using System.Collections;
using Alpha.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Alpha.World
{
    // Inima Golemului: dupa moartea boss-ului se ridica din daramaturi si fuge prin arena.
    // E prea sus si prea iute pentru lovituri de aproape, asa ca trebuie prinsa cu ciocanul aruncat.
    // Se lipeste de cap, se intoarce cu el in mana jucatorului si atunci e "livrata".
    [RequireComponent(typeof(SphereCollider))]
    public class GolemHeart : MonoBehaviour
    {
        enum State { Hidden, Rising, Fleeing, Caught, Delivered }

        [SerializeField] Transform core;
        [SerializeField] Renderer coreRenderer;
        [SerializeField] Light glow;
        [SerializeField] ParticleSystem motes;
        [SerializeField] Transform[] pebbles;
        [SerializeField] ParticleSystem dustPrefab;
        [SerializeField] ParticleSystem sparkPrefab;
        [SerializeField] Text screenHint;

        [Header("Arena")]
        [SerializeField] float roamRadius = 11.5f;
        [SerializeField] float hoverHeight = 2f;
        [SerializeField] float hoverRange = 0.45f;
        [SerializeField] LayerMask groundMask = 1 << 3;

        [Header("Fuga")]
        [SerializeField] float driftSpeed = 2.2f;
        [SerializeField] float fleeSpeed = 5f;
        [SerializeField] float dartSpeed = 14f;
        [SerializeField] float awareDistance = 9f;
        [SerializeField] float dartDistance = 4.5f;
        [SerializeField] float dartCooldown = 0.9f;
        [SerializeField] float knockDistance = 6f;

        [Header("Prindere")]
        [SerializeField] float riseTime = 2.4f;
        [SerializeField] float catchRadius = 1f;
        [SerializeField] float flightSpeed = 4f;
        [SerializeField] float homingAngle = 16f;
        [SerializeField] float homingRange = 30f;
        [SerializeField] float homingTurn = 140f;
        [SerializeField] float carriedScale = 0.55f;
        [SerializeField] float hintRange = 34f;

        [Header("Puls")]
        [SerializeField] float beatInterval = 1.1f;
        [SerializeField] float emissionBase = 3f;
        [SerializeField] float emissionBeat = 5f;

        [Header("Texte")]
        [SerializeField] string chaseHint = "The heart flees the hand that chases it...\nlet the hammer fly.   [E] Throw";
        [SerializeField] string knockHint = "It slips away from the blow... let the hammer fly.   [E] Throw";
        [SerializeField] string recallHint = "The heart clings to the stone.   [E] Call the hammer back";
        [SerializeField] string waitingHint = "The heart waits for the hammer...";

        public event Action onDelivered;
        public bool IsDelivered => state == State.Delivered;
        public bool IsActive => state == State.Rising || state == State.Fleeing || state == State.Caught;

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        State state = State.Hidden;
        Vector3 center, target, velocity, lastHead;
        Transform player;
        TriggerAttack attack;
        WeaponSocket socket;
        Weapon carrier;
        Material coreMat;
        Color emissionColor;
        SphereCollider trigger;
        Vector3 baseScale;
        float lightBase, retargetTimer, dartTimer, beatTime, hintTimer;
        bool knocked, hasLastHead;

        void Awake()
        {
            trigger = GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            baseScale = transform.localScale;
            if (coreRenderer != null)
            {
                coreMat = coreRenderer.material;
                emissionColor = coreMat.GetColor(EmissionId);
                float max = Mathf.Max(emissionColor.r, Mathf.Max(emissionColor.g, emissionColor.b));
                if (max > 0.0001f) emissionColor /= max;
            }
            if (glow != null) lightBase = glow.intensity;
            SetVisible(false);
        }

        void OnDisable() => ShowHint(null);

        // pos = locul unde a cazut boss-ul, arenaCenter = centrul arenei
        public void Rise(Vector3 pos, Vector3 arenaCenter)
        {
            if (state != State.Hidden) return;
            center = arenaCenter;
            FindPlayer();
            StartCoroutine(rise(pos));
        }

        // inima intra in capul ciocanului (folosit de cinematica de cioplire)
        public IEnumerator Absorb(Vector3 head, float duration)
        {
            transform.SetParent(null, true);
            Vector3 from = transform.position;
            Vector3 scale = transform.localScale;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                float e = k * k;
                transform.position = Vector3.Lerp(from, head, e);
                transform.localScale = scale * (1f - e * 0.95f);
                SetGlow(1f + k * 3f);
                yield return null;
            }
            SetVisible(false);
            if (motes != null) motes.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        IEnumerator rise(Vector3 pos)
        {
            state = State.Rising;
            float ground = GroundHeight(pos);
            Vector3 from = new Vector3(pos.x, ground - 0.4f, pos.z);
            Vector3 to = new Vector3(pos.x, ground + hoverHeight, pos.z);
            transform.position = from;
            transform.localScale = baseScale * 0.2f;
            SetVisible(true);
            Spawn(dustPrefab, new Vector3(pos.x, ground, pos.z), 1f);
            CameraRumble rumble = CameraRumble.Main;
            if (rumble != null)
            {
                rumble.SetRumble(0.05f, 0.4f);
                rumble.ImpulseAt(pos, 0.2f, 0.5f, 8f, 40f);
            }

            for (float t = 0f; t < riseTime; t += Time.deltaTime)
            {
                float k = t / riseTime;
                float e = 1f - Mathf.Pow(1f - k, 3f);
                Vector2 shake = UnityEngine.Random.insideUnitCircle * 0.04f * (1f - k);
                transform.position = Vector3.Lerp(from, to, e) + new Vector3(shake.x, 0f, shake.y);
                transform.localScale = baseScale * Mathf.Lerp(0.2f, 1f, e);
                SetGlow(Mathf.Lerp(0.2f, 1.6f, e));
                Pulse();
                yield return null;
            }
            transform.position = to;
            transform.localScale = baseScale;
            if (rumble != null) rumble.SetRumble(0f, 0.8f);
            Spawn(sparkPrefab, to, 0.6f);
            target = to;
            velocity = Vector3.zero;
            state = State.Fleeing;
        }

        void Update()
        {
            if (state == State.Hidden || state == State.Delivered) return;
            Pulse();
            SpinPebbles();
            if (state == State.Rising) return;
            if (player == null) FindPlayer();
            if (state == State.Fleeing) Flee();
            else if (state == State.Caught) Carry();
            UpdateHint();
        }

        void FixedUpdate()
        {
            if (state == State.Fleeing) Home();
        }

        // ------------------------------------------------------------------ fuga

        void Flee()
        {
            dartTimer -= Time.deltaTime;
            retargetTimer -= Time.deltaTime;
            Weapon hammer = CurrentHammer();
            bool armed = hammer != null && player != null && Flat(player.position - center).magnitude < hintRange;

            float speed = driftSpeed;
            if (!armed)
            {
                // fara ciocan sau cu jucatorul departe, inima asteapta deasupra centrului
                target = center;
                speed = driftSpeed * 0.6f;
            }
            else
            {
                Vector3 away = Flat(transform.position - player.position);
                float distance = away.magnitude;
                if (distance < dartDistance && dartTimer <= 0f)
                {
                    target = DartTarget(away);
                    dartTimer = dartCooldown;
                    retargetTimer = 1.2f;
                    Spawn(sparkPrefab, transform.position, 0.35f);
                }
                else if (retargetTimer <= 0f || Flat(target - transform.position).magnitude < 0.8f)
                {
                    target = WanderTarget();
                    retargetTimer = UnityEngine.Random.Range(1.6f, 3f);
                }
                if (dartTimer > dartCooldown - 0.45f) speed = dartSpeed;
                else if (distance < awareDistance) speed = fleeSpeed;
                if (InFlight(hammer)) speed *= 0.35f;   // cand zboara ciocanul inima ezita
            }

            Vector3 pos = transform.position;
            Vector3 flatTarget = new Vector3(target.x, pos.y, target.z);
            Vector3 next = Vector3.SmoothDamp(pos, flatTarget, ref velocity, 0.35f, speed);
            Vector3 offset = Flat(next - center);
            if (offset.magnitude > roamRadius) next = center + offset.normalized * roamRadius + Vector3.up * next.y;

            float bob = Mathf.Sin(Time.time * 1.7f) * hoverRange * 0.6f + Mathf.Sin(Time.time * 0.63f) * hoverRange * 0.4f;
            float height = GroundHeight(next) + hoverHeight + bob;
            next.y = Mathf.Lerp(pos.y, height, 1f - Mathf.Exp(-6f * Time.deltaTime));
            transform.position = next;

            if (hammer != null) TryCatch(hammer);
        }

        Vector3 DartTarget(Vector3 away)
        {
            Vector3 dir = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward;
            float best = -1f;
            Vector3 pick = transform.position;
            for (int i = 0; i < 5; i++)
            {
                Vector3 d = Quaternion.Euler(0f, UnityEngine.Random.Range(-55f, 55f), 0f) * dir;
                Vector3 p = Clamp(transform.position + d * UnityEngine.Random.Range(6f, 9f));
                float score = Flat(p - player.position).magnitude;
                if (score > best) { best = score; pick = p; }
            }
            return pick;
        }

        Vector3 WanderTarget()
        {
            float best = -1f;
            Vector3 pick = center;
            for (int i = 0; i < 6; i++)
            {
                Vector2 r = UnityEngine.Random.insideUnitCircle * roamRadius * 0.85f;
                Vector3 p = center + new Vector3(r.x, 0f, r.y);
                float score = player != null ? Flat(p - player.position).magnitude : 0f;
                score -= Flat(p - transform.position).magnitude * 0.25f;
                if (score > best) { best = score; pick = p; }
            }
            return pick;
        }

        Vector3 Clamp(Vector3 p)
        {
            Vector3 offset = Flat(p - center);
            if (offset.magnitude > roamRadius * 0.9f) offset = offset.normalized * roamRadius * 0.9f;
            return center + offset;
        }

        // ------------------------------------------------------------------ prindere

        // ciocanul aruncat e tras usor spre inima daca e aruncat aproximativ in directia ei
        void Home()
        {
            if (attack == null || attack.Thrown == null) return;
            Rigidbody rb = attack.Thrown.GetComponent<Rigidbody>();
            if (rb == null || rb.isKinematic) return;
            Vector3 v = rb.linearVelocity;
            if (v.magnitude < 8f) return;
            Vector3 to = transform.position - rb.worldCenterOfMass;
            if (to.magnitude > homingRange || Vector3.Angle(v, to) > homingAngle) return;
            rb.linearVelocity = Vector3.RotateTowards(v, to.normalized * v.magnitude, homingTurn * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f);
        }

        void TryCatch(Weapon hammer)
        {
            Vector3 head = HeadOf(hammer);
            if (!hasLastHead)
            {
                lastHead = head;
                hasLastHead = true;
                return;
            }
            bool flying = InFlight(hammer) && (head - lastHead).magnitude / Mathf.Max(Time.deltaTime, 0.0001f) > flightSpeed;
            if (flying && DistanceToSegment(transform.position, lastHead, head) < catchRadius) Catch(hammer);
            lastHead = head;
        }

        void OnTriggerEnter(Collider other)
        {
            if (state != State.Fleeing) return;
            Weapon weapon = other.GetComponentInParent<Weapon>();
            if (weapon == null) return;
            if (InFlight(weapon)) Catch(weapon);
            else if (InSocket(weapon)) Knock(other.bounds.center);
        }

        void Catch(Weapon weapon)
        {
            state = State.Caught;
            carrier = weapon;
            trigger.enabled = false;
            Transform anchor = weapon.ImpactPoint2 != null ? weapon.ImpactPoint2 : weapon.transform;
            transform.SetParent(anchor, true);
            StartCoroutine(stick());

            Spawn(sparkPrefab, transform.position, 1f);
            CameraShake shake = Camera.main != null ? Camera.main.GetComponent<CameraShake>() : null;
            if (shake != null) shake.Shake(0.18f, 0.25f);
            Rigidbody rb = weapon.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic) rb.linearVelocity *= 0.35f;
        }

        IEnumerator stick()
        {
            Vector3 from = transform.localPosition;
            Vector3 scaleFrom = transform.localScale;
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            Vector3 scaleTo = new Vector3(baseScale.x / parentScale.x, baseScale.y / parentScale.y, baseScale.z / parentScale.z) * carriedScale;
            for (float t = 0f; t < 0.18f; t += Time.deltaTime)
            {
                float k = t / 0.18f;
                if (state != State.Caught) yield break;
                transform.localPosition = Vector3.Lerp(from, Vector3.zero, k);
                transform.localScale = Vector3.Lerp(scaleFrom, scaleTo, k);
                yield return null;
            }
            if (state != State.Caught) yield break;
            transform.localPosition = Vector3.zero;
            transform.localScale = scaleTo;
        }

        void Knock(Vector3 from)
        {
            Vector3 away = Flat(transform.position - from);
            if (away.sqrMagnitude < 0.01f) away = Flat(transform.position - center);
            target = Clamp(transform.position + away.normalized * knockDistance);
            velocity = away.normalized * dartSpeed;
            dartTimer = dartCooldown;
            retargetTimer = 1.5f;
            knocked = true;
            hintTimer = 0f;
            Spawn(sparkPrefab, transform.position, 0.7f);
        }

        void Carry()
        {
            if (carrier == null || transform.parent == null)
            {
                Drop();
                return;
            }
            if (!InSocket(carrier)) return;
            state = State.Delivered;
            ShowHint(null);
            onDelivered?.Invoke();
        }

        // ciocanul a disparut (schimbat / distrus): inima se elibereaza si fuge din nou
        void Drop()
        {
            transform.SetParent(null, true);
            transform.localScale = baseScale;
            transform.rotation = Quaternion.identity;
            carrier = null;
            trigger.enabled = true;
            hasLastHead = false;
            velocity = Vector3.zero;
            target = Clamp(transform.position);
            state = State.Fleeing;
        }

        // ------------------------------------------------------------------ jucator si ciocan

        void FindPlayer()
        {
            GameObject go = GameObject.FindWithTag("Player");
            if (go == null) return;
            player = go.transform;
            attack = go.GetComponent<TriggerAttack>();
            socket = go.GetComponentInChildren<WeaponSocket>();
        }

        Weapon CurrentHammer()
        {
            if (attack != null && attack.Thrown != null) return attack.Thrown;
            if (socket != null && socket.HasWeapon) return socket.CurrentWeapon;
            HammerStages stages = HammerStages.Instance;
            return stages != null && stages.Hammer != null && stages.Hammer.gameObject.activeInHierarchy ? stages.Hammer : null;
        }

        static bool InSocket(Weapon weapon)
        {
            Transform parent = weapon.transform.parent;
            return weapon.IsHeld && parent != null && parent.GetComponent<WeaponSocket>() != null && weapon.transform.localPosition.sqrMagnitude < 0.0004f;
        }

        static bool InFlight(Weapon weapon)
        {
            Transform parent = weapon.transform.parent;
            return parent == null || parent.GetComponent<WeaponSocket>() == null;
        }

        static Vector3 HeadOf(Weapon weapon)
        {
            HammerStages stages = HammerStages.Instance;
            if (stages != null && stages.Hammer == weapon) return stages.HeadPosition();
            if (weapon.headHitbox != null) return weapon.headHitbox.bounds.center;
            return weapon.transform.position;
        }

        // ------------------------------------------------------------------ indicii

        void UpdateHint()
        {
            if (screenHint == null) return;
            // livrata in acelasi cadru (Carry): indiciul ramane ascuns, cinematica preia ecranul
            if (state != State.Fleeing && state != State.Caught) return;
            bool near = player != null && Flat(player.position - center).magnitude < hintRange;
            if (!near)
            {
                ShowHint(null);
                return;
            }
            hintTimer += Time.deltaTime;
            if (state == State.Caught)
            {
                ShowHint(InFlight(carrier) ? recallHint : null);
                return;
            }
            Weapon hammer = CurrentHammer();
            if (hammer == null) ShowHint(waitingHint);
            else if (attack != null && attack.Thrown != null) ShowHint(null);
            else ShowHint(knocked && hintTimer < 4f ? knockHint : chaseHint);
        }

        void ShowHint(string text)
        {
            if (screenHint == null) return;
            bool on = !string.IsNullOrEmpty(text);
            if (on && screenHint.text != text) screenHint.text = text;
            if (screenHint.gameObject.activeSelf != on) screenHint.gameObject.SetActive(on);
        }

        // ------------------------------------------------------------------ vizual

        // bataie dubla, ca o inima
        void Pulse()
        {
            beatTime += Time.deltaTime;
            float phase = Mathf.Repeat(beatTime, beatInterval) / beatInterval;
            float beat = Mathf.Exp(-Mathf.Pow((phase - 0.08f) * 18f, 2f)) + 0.6f * Mathf.Exp(-Mathf.Pow((phase - 0.3f) * 18f, 2f));
            SetGlow(1f + beat);
            if (core != null)
            {
                core.localScale = Vector3.one * (1f + beat * 0.08f);
                core.Rotate(0f, 40f * Time.deltaTime, 0f, Space.Self);
            }
        }

        void SpinPebbles()
        {
            if (pebbles == null) return;
            for (int i = 0; i < pebbles.Length; i++)
            {
                if (pebbles[i] == null) continue;
                pebbles[i].RotateAround(transform.position, Vector3.up, (30f + i * 9f) * Time.deltaTime);
                pebbles[i].Rotate(17f * Time.deltaTime, 23f * Time.deltaTime, 0f, Space.Self);
            }
        }

        void SetGlow(float k)
        {
            if (coreMat != null) coreMat.SetColor(EmissionId, emissionColor * (emissionBase + emissionBeat * (k - 1f)));
            if (glow != null) glow.intensity = lightBase * k;
        }

        void SetVisible(bool on)
        {
            if (core != null) core.gameObject.SetActive(on);
            if (glow != null) glow.enabled = on;
            if (pebbles != null) foreach (Transform p in pebbles) if (p != null) p.gameObject.SetActive(on);
            if (motes != null)
            {
                if (on) motes.Play(true);
                else motes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (trigger != null) trigger.enabled = on;
        }

        void Spawn(ParticleSystem prefab, Vector3 point, float scale)
        {
            if (prefab == null) return;
            ParticleSystem fx = Instantiate(prefab, point, Quaternion.identity);
            fx.transform.localScale = Vector3.one * scale;
            fx.Play(true);
            Destroy(fx.gameObject, 4f);
        }

        float GroundHeight(Vector3 p)
        {
            if (Physics.Raycast(new Vector3(p.x, center.y + 30f, p.z), Vector3.down, out RaycastHit hit, 60f, groundMask, QueryTriggerInteraction.Ignore))
                return hit.point.y;
            return center.y;
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float len = ab.sqrMagnitude;
            float k = len < 0.000001f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, ab) / len);
            return Vector3.Distance(p, a + ab * k);
        }
    }
}
