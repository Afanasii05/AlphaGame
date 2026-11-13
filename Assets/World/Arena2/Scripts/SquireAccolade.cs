using System.Collections;
using Alpha.Input;
using UnityEngine;

namespace Alpha.World
{
    // Coiful scutierului, uitat langa tronul stapanului. Cine ingenuncheaza in fata lui cateva clipe
    // primeste investitura: o luminita iese din coif si il atinge pe umarul stang, pe cel drept, apoi pe crestet.
    [RequireComponent(typeof(SphereCollider))]
    public class SquireAccolade : MonoBehaviour
    {
        [SerializeField] Transform helmet;
        [SerializeField] Light wisp;
        [SerializeField] ParticleSystem motes;
        [SerializeField] AudioSource audioSource;
        [SerializeField] Arena2Screen screen;

        [Header("Investitura")]
        [SerializeField] float holdTime = 2f;
        [SerializeField] float kneelDistance = 2.2f;
        [SerializeField] float maxFacingAngle = 60f;
        [SerializeField] float wispIntensity = 3f;
        [SerializeField] float tapTime = 0.9f;

        [Header("Indicii")]
        [SerializeField] string nearHint = "A squire's helmet, waiting by its master's throne...";
        [SerializeField] string stirHint = "A faint warmth stirs...";
        [SerializeField] string doneHint = "Arise, Sir Hammer-bearer.";

        InputHandler input;
        Transform player;
        AudioClip chime;
        float holdTimer, releaseRadius, pulse;
        bool kneeling, playing, hinted;

        public bool Knighted { get; private set; }

        void Awake()
        {
            SphereCollider trigger = GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            releaseRadius = trigger.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z) + 1f;
            chime = Synth();
            if (wisp != null)
            {
                wisp.intensity = 0f;
                wisp.enabled = false;
            }
        }

        void OnDisable() => Release();

        void OnTriggerEnter(Collider other)
        {
            if (input != null || playing || Knighted) return;
            InputHandler handler = other.GetComponentInParent<InputHandler>();
            if (handler == null) return;
            input = handler;
            player = handler.transform;
            input.onCrouchHold += HandleCrouch;
        }

        void Update()
        {
            if (playing || player == null) return;
            Vector3 toHelmet = Anchor() - player.position;
            toHelmet.y = 0f;
            float distance = toHelmet.magnitude;
            if (distance > releaseRadius) { Release(); return; }

            Vector3 facing = Vector3.ProjectOnPlane(player.forward, Vector3.up);
            bool bowing = kneeling && distance <= kneelDistance && Vector3.Angle(facing, toHelmet) <= maxFacingAngle;
            holdTimer = bowing ? holdTimer + Time.deltaTime : Mathf.MoveTowards(holdTimer, 0f, Time.deltaTime * 2f);
            float stir = Mathf.Clamp01(holdTimer / holdTime);

            // coiful palpaie cald cat timp jucatorul sta in genunchi
            pulse += Time.deltaTime;
            SetWisp(Anchor() + Vector3.up * 0.25f, stir * wispIntensity * 0.4f * (0.8f + 0.2f * Mathf.Sin(pulse * 6f)));

            if (bowing) Hint(stirHint);
            else if (!hinted)
            {
                hinted = true;
                Hint(nearHint);
            }
            if (holdTimer >= holdTime) StartCoroutine(accolade());
        }

        void HandleCrouch(bool held)
        {
            kneeling = held;
            if (!held) holdTimer = Mathf.Min(holdTimer, holdTime * 0.5f);
        }

        IEnumerator accolade()
        {
            playing = true;
            Knighted = true;
            hinted = true;
            Transform target = player;
            Animator animator = target != null ? target.GetComponentInChildren<Animator>() : null;
            Release();
            if (motes != null) motes.Play(true);

            Vector3 start = Anchor() + Vector3.up * 0.25f;
            Vector3 pos = start;
            Transform[] taps =
            {
                Bone(animator, HumanBodyBones.LeftShoulder),
                Bone(animator, HumanBodyBones.RightShoulder),
                Bone(animator, HumanBodyBones.Head)
            };
            Vector3[] fallback = { new Vector3(-0.2f, 1.4f, 0f), new Vector3(0.2f, 1.4f, 0f), new Vector3(0f, 1.7f, 0f) };

            // se ridica deasupra coifului
            yield return Fly(pos, start + Vector3.up * 0.8f, 0.8f, wispIntensity * 0.4f, wispIntensity);
            pos = start + Vector3.up * 0.8f;
            for (int i = 0; i < taps.Length; i++)
            {
                Vector3 at = taps[i] != null ? taps[i].position + Vector3.up * 0.12f : (target != null ? target.TransformPoint(fallback[i]) : pos);
                yield return Fly(pos, at + Vector3.up * 0.35f, tapTime * 0.6f, wispIntensity, wispIntensity);
                yield return Fly(at + Vector3.up * 0.35f, at, tapTime * 0.25f, wispIntensity, wispIntensity * 2f);
                Play(1f + i * 0.25f, 0.6f);
                if (motes != null) motes.Emit(14);
                pos = at;
                yield return new WaitForSeconds(0.25f);
            }
            Hint(doneHint);
            yield return Fly(pos, pos + Vector3.up * 2.5f, 1.6f, wispIntensity * 2f, 0f);
            if (motes != null) motes.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            SetWisp(pos, 0f);
            playing = false;
        }

        IEnumerator Fly(Vector3 from, Vector3 to, float duration, float lightFrom, float lightTo)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Smooth(t / duration);
                Vector3 p = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.15f;
                SetWisp(p, Mathf.Lerp(lightFrom, lightTo, k));
                yield return null;
            }
            SetWisp(to, lightTo);
        }

        void SetWisp(Vector3 at, float intensity)
        {
            if (wisp == null) return;
            wisp.transform.position = at;
            if (motes != null) motes.transform.position = at;
            wisp.intensity = intensity;
            wisp.enabled = intensity > 0.01f;
        }

        void Release()
        {
            if (input != null) input.onCrouchHold -= HandleCrouch;
            input = null;
            player = null;
            kneeling = false;
            holdTimer = 0f;
            if (!playing) SetWisp(Anchor() + Vector3.up * 0.25f, 0f);
        }

        Vector3 Anchor() => helmet != null ? helmet.position : transform.position;

        void Hint(string message)
        {
            if (screen != null) screen.Show(message, message == stirHint ? 0.3f : 4f);
        }

        void Play(float pitch, float volume)
        {
            if (audioSource == null || chime == null) return;
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(chime, volume);
        }

        static Transform Bone(Animator animator, HumanBodyBones bone)
        {
            return animator != null && animator.isHuman ? animator.GetBoneTransform(bone) : null;
        }

        // un clopotel scurt, generat
        static AudioClip Synth()
        {
            const int rate = 44100;
            int count = rate;
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float v = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.4f + Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.2f + Mathf.Sin(2f * Mathf.PI * 1760f * t) * 0.08f;
                data[i] = v * Mathf.Exp(-4f * t) * Mathf.Clamp01(t * 300f);
            }
            AudioClip clip = AudioClip.Create("SquireChime", count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Smooth(float k)
        {
            k = Mathf.Clamp01(k);
            return k * k * (3f - 2f * k);
        }
    }
}
