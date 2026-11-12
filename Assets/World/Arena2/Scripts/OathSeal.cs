using System;
using System.Collections;
using Alpha.Combat;
using UnityEngine;

namespace Alpha.World
{
    // Pecetea unui juramant, sus pe zidul salii: se aprinde dupa infrangerea cavalerului si se sparge cu ciocanul aruncat.
    // Pecetea pazita primeste ciocanul abia dupa celelalte doua: il prinde in ea si cedeaza cand e rechemat sau urmat.
    // La spargere scutul cade, stindardul arde si un fir de lumina rosu-auriu zboara spre cavaler.
    public class OathSeal : MonoBehaviour
    {
        public enum State { Dormant, Lit, Lodged, Broken }

        [SerializeField] Renderer sigil;
        [SerializeField] Light glow;
        [SerializeField] Transform shield;
        [SerializeField] Transform banner;
        [SerializeField] ParticleSystem burst;
        [SerializeField] ParticleSystem fire;
        [SerializeField] TrailRenderer thread;
        [SerializeField] AudioSource audioSource;
        [SerializeField] bool warded;

        [Header("Reglaj")]
        [SerializeField] float radius = 1.5f;
        [SerializeField] float reachDown = 2.4f;   // zona de lovire coboara pana la scutul casei: ciocanul aruncat cade sub tinta
        [SerializeField] Color litColor = new Color(2.6f, 1.15f, 0.4f);
        [SerializeField] Color wardColor = new Color(1.1f, 1.6f, 2.8f);
        [SerializeField] float glowIntensity = 7f;
        [SerializeField] float lightTime = 0.9f;
        [SerializeField] float threadTime = 1.5f;
        [SerializeField] float burnTime = 1.6f;
        [SerializeField] float floorY = 12f;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly Color Char = new Color(0.07f, 0.05f, 0.045f);

        MaterialPropertyBlock block;
        Weapon lodged;
        Transform target;
        Vector3 prevHead;
        bool hasPrev, open;
        float level, pulse, repel;
        AudioClip crack, chime;

        public event Action<OathSeal> Broken;
        public State Current { get; private set; }
        public bool Warded => warded;
        public float ThreadTime => threadTime;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            open = !warded;
            crack = Synth("SealCrack", 1.2f, CrackSample);
            chime = Synth("SealChime", 1.6f, ChimeSample);
            if (thread != null) thread.gameObject.SetActive(false);
            Apply(0f);
        }

        public void Ignite(Transform knight)
        {
            if (Current != State.Dormant) return;
            target = knight;
            Current = State.Lit;
            StartCoroutine(ignite());
            Play(chime, warded ? 0.8f : 1f, 0.5f);
        }

        // pecetea pazita se deschide cand au cazut celelalte
        public void Unward()
        {
            open = true;
            Play(chime, 0.6f, 0.7f);
        }

        IEnumerator ignite()
        {
            for (float t = 0f; t < lightTime; t += Time.deltaTime)
            {
                level = t / lightTime;
                yield return null;
            }
            level = 1f;
        }

        void LateUpdate()
        {
            if (Current == State.Dormant || Current == State.Broken) return;
            pulse += Time.deltaTime;
            repel = Mathf.MoveTowards(repel, 0f, Time.deltaTime * 2.5f);
            float wave = 0.85f + 0.15f * Mathf.Sin(pulse * 2.4f);
            if (Current == State.Lodged) wave = 0.55f + 0.45f * Mathf.PerlinNoise(pulse * 9f, 0.3f);
            Apply(level * wave + repel);

            Weapon hammer = Hammer();
            if (Current == State.Lodged)
            {
                // rechemat sau urmat prin teleport: ciocanul revine in mana si pecetea cedeaza
                if (lodged == null || lodged.IsHeld) Break();
                return;
            }
            Track(hammer);
        }

        void Track(Weapon hammer)
        {
            if (hammer == null) { hasPrev = false; return; }
            Vector3 head = HeadOf(hammer);
            // conteaza doar ciocanul in zbor: aruncat sau rechemat (in zbor nu are parinte)
            bool flying = !hammer.IsHeld || hammer.transform.parent == null;
            if (flying && hasPrev && Near(prevHead, head))
            {
                if (open) Hit(hammer);
                else if (repel <= 0.01f)
                {
                    repel = 1.2f;
                    if (burst != null) burst.Emit(12);
                    Play(crack, 1.6f, 0.4f);
                }
            }
            prevHead = head;
            hasPrev = true;
        }

        void Hit(Weapon hammer)
        {
            if (!warded || hammer.IsHeld) { Break(); return; }
            // ciocanul ramane infipt in pecete, cu capul in centrul ei
            Rigidbody rb = hammer.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
            }
            Vector3 lodge = transform.position + transform.forward * 0.35f;
            hammer.transform.position += lodge - HeadOf(hammer);
            if (rb != null) rb.position = hammer.transform.position;
            lodged = hammer;
            lodged.onHeldChange += Unlodge;
            Current = State.Lodged;
            repel = 1.5f;
            if (burst != null) burst.Emit(20);
            Play(crack, 1.2f, 0.7f);
        }

        // inainte ca arma sa fie prinsa in mana corpul redevine dinamic, ca resetarea vitezei sa fie valida
        void Unlodge(bool held)
        {
            if (!held || lodged == null) return;
            lodged.onHeldChange -= Unlodge;
            Rigidbody rb = lodged.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;
        }

        void Break()
        {
            if (Current == State.Broken) return;
            Current = State.Broken;
            if (lodged != null) lodged.onHeldChange -= Unlodge;
            lodged = null;
            Play(crack, warded ? 0.75f : 0.95f, 1f);
            if (burst != null) burst.Emit(60);
            CameraRumble rumble = CameraRumble.Main;
            if (rumble != null) rumble.ImpulseAt(transform.position, 0.25f, 0.3f, 6f, 40f);
            StartCoroutine(shatter());
            StartCoroutine(fall(shield, 0.1f, 140f));
            StartCoroutine(burn());
            if (thread != null && target != null) StartCoroutine(flow());
            Broken?.Invoke(this);
        }

        IEnumerator shatter()
        {
            float from = level + 1.5f;
            Vector3 scale = sigil != null ? sigil.transform.localScale : Vector3.one;
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                float k = t / 0.6f;
                Apply(Mathf.Lerp(from, 0f, k));
                if (sigil != null) sigil.transform.localScale = scale * (1f + 0.5f * k);
                yield return null;
            }
            Apply(0f);
            if (sigil != null) sigil.enabled = false;
        }

        // cadere simpla pana la dale, cu o rotire usoara
        IEnumerator fall(Transform piece, float delay, float spin)
        {
            if (piece == null) yield break;
            yield return new WaitForSeconds(delay);
            piece.SetParent(null, true);
            Vector3 axis = Vector3.Cross(Vector3.up, transform.forward).normalized;
            float velocity = 0f;
            Bounds b = Bounds(piece);
            float lowest = b.size.sqrMagnitude > 0f ? piece.position.y - b.min.y : 0.2f;
            while (piece.position.y - lowest > floorY)
            {
                velocity += 9.81f * Time.deltaTime;
                piece.position += (Vector3.down * velocity + transform.forward * 1.2f) * Time.deltaTime;
                piece.rotation = Quaternion.AngleAxis(spin * Time.deltaTime, axis) * piece.rotation;
                yield return null;
            }
            piece.position = new Vector3(piece.position.x, floorY + lowest, piece.position.z);
            CameraRumble rumble = CameraRumble.Main;
            if (rumble != null) rumble.ImpulseAt(piece.position, 0.12f, 0.2f, 4f, 25f);
            Play(crack, 0.5f, 0.5f);
        }

        IEnumerator burn()
        {
            if (banner == null) yield break;
            Renderer[] cloth = banner.GetComponentsInChildren<Renderer>();
            if (fire != null) fire.Play(true);
            for (float t = 0f; t < burnTime; t += Time.deltaTime)
            {
                Tint(cloth, Color.Lerp(Color.white, Char, t / burnTime));
                yield return null;
            }
            Tint(cloth, Char);
            if (fire != null) fire.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            BannerSway sway = banner.GetComponent<BannerSway>();
            if (sway != null) sway.enabled = false;
            yield return fall(banner, 0f, 25f);
        }

        // firul de lumina: de la pecete spre pieptul cavalerului, pe un arc
        IEnumerator flow()
        {
            Transform t = thread.transform;
            t.SetParent(null, true);
            t.position = transform.position;
            thread.gameObject.SetActive(true);
            thread.Clear();
            Vector3 start = transform.position;
            for (float time = 0f; time < threadTime; time += Time.deltaTime)
            {
                float k = time / threadTime;
                float e = k * k * (3f - 2f * k);
                Vector3 end = target.position;
                Vector3 control = Vector3.Lerp(start, end, 0.5f) + Vector3.up * 3.5f;
                t.position = Vector3.Lerp(Vector3.Lerp(start, control, e), Vector3.Lerp(control, end, e), e);
                yield return null;
            }
            t.position = target.position;
            Play(chime, 1.3f, 0.4f);
            yield return new WaitForSeconds(thread.time + 0.2f);
            thread.gameObject.SetActive(false);
        }

        void Apply(float amount)
        {
            if (sigil != null)
            {
                sigil.GetPropertyBlock(block);
                block.SetColor(BaseColor, (open ? litColor : wardColor) * amount);
                sigil.SetPropertyBlock(block);
                sigil.enabled = amount > 0.001f;
            }
            if (glow != null)
            {
                glow.color = open ? new Color(1f, 0.55f, 0.25f) : new Color(0.6f, 0.75f, 1f);
                glow.intensity = glowIntensity * amount;
                glow.enabled = amount > 0.001f;
            }
        }

        void Tint(Renderer[] renderers, Color color)
        {
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(BaseColor, color);
                r.SetPropertyBlock(block);
            }
        }

        // capsula verticala de la scut pana la pecete fata de pasul ciocanului din cadrul asta
        bool Near(Vector3 from, Vector3 to)
        {
            Vector3 top = transform.position, low = top + Vector3.down * reachDown;
            Vector3 d1 = to - from, d2 = top - low, r = from - low;
            float a = Vector3.Dot(d1, d1), e = Vector3.Dot(d2, d2), f = Vector3.Dot(d2, r);
            float s = 0f, t = e > 0.0001f ? Mathf.Clamp01(f / e) : 0f;
            if (a > 0.0001f && e > 0.0001f)
            {
                float b = Vector3.Dot(d1, d2), c = Vector3.Dot(d1, r), den = a * e - b * b;
                s = den > 0.0001f ? Mathf.Clamp01((b * f - c * e) / den) : 0f;
                t = (b * s + f) / e;
                if (t < 0f) { t = 0f; s = Mathf.Clamp01(-c / a); }
                else if (t > 1f) { t = 1f; s = Mathf.Clamp01((b - c) / a); }
            }
            else if (a > 0.0001f) s = Mathf.Clamp01(-Vector3.Dot(d1, r) / a);
            return (from + d1 * s - (low + d2 * t)).sqrMagnitude <= radius * radius;
        }

        static Weapon Hammer()
        {
            HammerStages stages = HammerStages.Instance;
            return stages != null ? stages.Hammer : null;
        }

        static Vector3 HeadOf(Weapon hammer)
        {
            HammerStages stages = HammerStages.Instance;
            if (stages != null && stages.Hammer == hammer) return stages.HeadPosition();
            return hammer.headHitbox != null ? hammer.headHitbox.transform.position : hammer.transform.position;
        }

        static Bounds Bounds(Transform piece)
        {
            Bounds b = new Bounds();
            bool any = false;
            foreach (Renderer r in piece.GetComponentsInChildren<Renderer>())
            {
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
            return b;
        }

        void Play(AudioClip clip, float pitch, float volume)
        {
            if (audioSource == null || clip == null) return;
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, volume);
        }

        // sunete generate: crapatura de piatra si un clopotel stins
        delegate float Sample(float t);

        static AudioClip Synth(string name, float length, Sample sample)
        {
            const int rate = 44100;
            int count = Mathf.CeilToInt(length * rate);
            float[] data = new float[count];
            for (int i = 0; i < count; i++) data[i] = Mathf.Clamp(sample(i / (float)rate), -1f, 1f);
            AudioClip clip = AudioClip.Create(name, count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float CrackSample(float t)
        {
            float body = Mathf.Sin(2f * Mathf.PI * (95f - 40f * t) * t) * 0.6f * Mathf.Exp(-6f * t);
            return body + Noise(t) * Mathf.Exp(-22f * t) * 0.7f;
        }

        static float ChimeSample(float t)
        {
            float v = Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.35f + Mathf.Sin(2f * Mathf.PI * 990f * t) * 0.2f + Mathf.Sin(2f * Mathf.PI * 1485f * t) * 0.1f;
            return v * Mathf.Exp(-2.6f * t) * Mathf.Clamp01(t * 200f);
        }

        static float Noise(float t)
        {
            float x = Mathf.Sin(t * 12.9898f * 44100f) * 43758.5453f;
            return (x - Mathf.Floor(x)) * 2f - 1f;
        }
    }
}
