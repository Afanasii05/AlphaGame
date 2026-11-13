using System.Collections;
using UnityEngine;

namespace Alpha.World
{
    // Efectele cavalerului: scantei si clinchet pe scut, izbitura rosie in spate, sclipirea lancei inainte de atac.
    // Pe jucator: oprire scurta a timpului si tremur la impunsatura, praf la fiecare pas, dara de scantei la maturare.
    public class KnightFx : MonoBehaviour
    {
        [SerializeField] ParticleSystem sparks;
        [SerializeField] ParticleSystem backBurst;
        [SerializeField] ParticleSystem glint;
        [SerializeField] ParticleSystem embers;
        [SerializeField] Light flash;
        [SerializeField] Renderer[] shieldRenderers;
        [SerializeField] Renderer[] bodyRenderers;
        [SerializeField] AudioSource audioSource;
        [SerializeField] KnightAI ai;
        [SerializeField] Animator animator;
        [SerializeField] ParticleSystem stepDust;
        [SerializeField] TrailRenderer trail;
        [SerializeField] ParticleSystem trailSparks;
        [SerializeField] Color blockColor = new Color(2.4f, 1.8f, 0.9f);
        [SerializeField] Color backColor = new Color(2.6f, 0.55f, 0.45f);
        [SerializeField] float flashTime = 0.28f;
        [SerializeField] float blockRumble = 0.18f;
        [SerializeField] float backRumble = 0.3f;

        [Header("Lovit jucatorul")]
        [SerializeField] float hitStop = 0.07f;
        [SerializeField] float hitStopScale = 0.05f;
        [SerializeField] float heavyShake = 0.22f;
        [SerializeField] float lightShake = 0.12f;
        [SerializeField] float stepLift = 0.14f;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        MaterialPropertyBlock block;
        AudioClip clang, thud, shing;
        Coroutine shieldFlash, bodyFlash, lightFlash;
        Transform leftFoot, rightFoot;
        bool leftUp, rightUp;
        static bool stopping;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            if (flash != null) flash.intensity = 0f;
            clang = Synth("Clang", 0.9f, ClangSample);
            thud = Synth("Thud", 0.45f, ThudSample);
            shing = Synth("Shing", 0.5f, ShingSample);
            if (animator != null && animator.isHuman)
            {
                leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            }
            SetTrail(false);
        }

        void LateUpdate()
        {
            bool moving = ai != null && (ai.IsWalking || ai.IsAttacking);
            Step(leftFoot, ref leftUp, moving);
            Step(rightFoot, ref rightUp, moving);
        }

        // un pas = piciorul coboara sub prag dupa ce a fost ridicat
        void Step(Transform foot, ref bool up, bool moving)
        {
            if (foot == null) return;
            float lift = foot.position.y - transform.position.y;
            if (lift > stepLift) { up = true; return; }
            if (!up || lift > stepLift * 0.6f) return;
            up = false;
            if (!moving) return;
            if (stepDust != null)
            {
                stepDust.transform.position = new Vector3(foot.position.x, transform.position.y + 0.05f, foot.position.z);
                stepDust.Emit(Random.Range(5, 9));
            }
            Play(thud, Random.Range(1.5f, 1.8f), 0.18f);
        }

        public void SetTrail(bool on)
        {
            if (trail != null)
            {
                if (on) trail.Clear();
                trail.emitting = on;
            }
            if (trailSparks != null)
            {
                var emission = trailSparks.emission;
                emission.enabled = on;
            }
        }

        public void PlayerHit(Vector3 point, bool heavy)
        {
            Play(heavy ? clang : thud, Random.Range(0.8f, 0.9f), heavy ? 0.8f : 0.9f);
            if (heavy) Play(thud, 0.85f, 0.7f);
            Restart(ref lightFlash, flashLight(point, new Color(1f, 0.55f, 0.35f), heavy ? 6f : 4f));
            Camera cam = Camera.main;
            CameraShake shake = cam != null ? cam.GetComponent<CameraShake>() : null;
            if (shake != null) shake.Shake(heavy ? heavyShake : lightShake, heavy ? 0.3f : 0.2f);
            if (heavy && !stopping && Time.timeScale > 0.5f) StartCoroutine(freeze());
        }

        IEnumerator freeze()
        {
            stopping = true;
            float scale = Time.timeScale;
            Time.timeScale = hitStopScale;
            yield return new WaitForSecondsRealtime(hitStop);
            if (Mathf.Approximately(Time.timeScale, hitStopScale)) Time.timeScale = scale;
            stopping = false;
        }

        void OnDisable()
        {
            if (stopping) Time.timeScale = 1f;
            stopping = false;
        }

        public void Block(Vector3 point, Vector3 outward)
        {
            Burst(sparks, point, outward, 26);
            Restart(ref shieldFlash, flashRenderers(shieldRenderers, blockColor));
            Restart(ref lightFlash, flashLight(point, new Color(1f, 0.78f, 0.45f), 6f));
            Play(clang, Random.Range(0.92f, 1.08f), 0.9f);
            Impulse(point, blockRumble);
        }

        public void BackHit(Vector3 point, Vector3 outward)
        {
            Burst(backBurst, point, outward, 34);
            Restart(ref bodyFlash, flashRenderers(bodyRenderers, backColor));
            Restart(ref lightFlash, flashLight(point, new Color(1f, 0.25f, 0.18f), 7f));
            Play(thud, Random.Range(0.9f, 1.05f), 1f);
            Impulse(point, backRumble);
        }

        public void Telegraph()
        {
            if (glint != null) glint.Play(true);
            Play(shing, Random.Range(0.95f, 1.1f), 0.55f);
        }

        public void SetEnraged(bool on)
        {
            if (embers == null) return;
            if (on) embers.Play(true);
            else embers.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        void Burst(ParticleSystem ps, Vector3 point, Vector3 outward, int count)
        {
            if (ps == null) return;
            if (outward.sqrMagnitude < 0.0001f) outward = Vector3.up;
            ps.transform.SetPositionAndRotation(point, Quaternion.LookRotation(outward));
            ps.Emit(count);
        }

        void Restart(ref Coroutine slot, IEnumerator routine)
        {
            if (slot != null) StopCoroutine(slot);
            slot = StartCoroutine(routine);
        }

        IEnumerator flashRenderers(Renderer[] renderers, Color color)
        {
            if (renderers == null) yield break;
            for (float t = 0f; t < flashTime; t += Time.deltaTime)
            {
                float k = 1f - t / flashTime;
                SetTint(renderers, Color.Lerp(Color.white, color, k * k), true);
                yield return null;
            }
            SetTint(renderers, Color.white, false);
        }

        void SetTint(Renderer[] renderers, Color color, bool on)
        {
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                block.Clear();
                if (on) block.SetColor(BaseColor, color);
                r.SetPropertyBlock(block);
            }
        }

        IEnumerator flashLight(Vector3 point, Color color, float intensity)
        {
            if (flash == null) yield break;
            flash.transform.position = point;
            flash.color = color;
            flash.enabled = true;
            for (float t = 0f; t < flashTime; t += Time.deltaTime)
            {
                flash.intensity = intensity * (1f - t / flashTime);
                yield return null;
            }
            flash.intensity = 0f;
        }

        void Play(AudioClip clip, float pitch, float volume)
        {
            if (audioSource == null || clip == null) return;
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, volume);
        }

        static void Impulse(Vector3 point, float strength)
        {
            CameraRumble rumble = CameraRumble.Main;
            if (rumble != null) rumble.ImpulseAt(point, strength, 0.25f, 6f, 30f);
        }

        // sunetele se genereaza din cateva armonici, fara fisiere audio separate
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

        static readonly float[] ClangFreq = { 587f, 1243f, 2113f, 3271f, 4580f };
        static readonly float[] ClangAmp = { 0.5f, 0.38f, 0.26f, 0.16f, 0.1f };
        static readonly float[] ClangDecay = { 4.5f, 6f, 8f, 11f, 15f };

        static float ClangSample(float t)
        {
            float v = 0f;
            for (int i = 0; i < ClangFreq.Length; i++)
                v += ClangAmp[i] * Mathf.Exp(-ClangDecay[i] * t) * Mathf.Sin(2f * Mathf.PI * ClangFreq[i] * t);
            float strike = Mathf.Exp(-160f * t) * Noise(t) * 0.5f;
            return (v + strike) * Mathf.Clamp01(t * 2000f);
        }

        static float ThudSample(float t)
        {
            float body = Mathf.Sin(2f * Mathf.PI * (70f - 25f * t) * t) * 0.8f + Mathf.Sin(2f * Mathf.PI * 140f * t) * 0.3f;
            return body * Mathf.Exp(-9f * t) + Noise(t) * Mathf.Exp(-35f * t) * 0.45f;
        }

        static float ShingSample(float t)
        {
            float sweep = 2600f + 1400f * t;
            float v = Mathf.Sin(2f * Mathf.PI * sweep * t) * 0.25f + Mathf.Sin(2f * Mathf.PI * sweep * 1.51f * t) * 0.12f;
            return v * Mathf.Exp(-6f * t) * Mathf.Clamp01(t * 60f);
        }

        static float Noise(float t)
        {
            float x = Mathf.Sin(t * 12.9898f * 44100f) * 43758.5453f;
            return (x - Mathf.Floor(x)) * 2f - 1f;
        }
    }
}
