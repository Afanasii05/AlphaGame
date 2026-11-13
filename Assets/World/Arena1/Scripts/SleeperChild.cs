using System;
using System.Collections;
using Alpha.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Alpha.World
{
    // Puiul Adormitului: un golem mic de pietricele, ghemuit printre pietre. Cine ingenuncheaza in fata lui
    // cateva clipe il trezeste: ochii se aprind, se ridica, se inclina si se ghemuieste la loc, cu o lumina slaba.
    [RequireComponent(typeof(SphereCollider))]
    public class SleeperChild : MonoBehaviour
    {
        [SerializeField] Transform spine;
        [SerializeField] Transform head;
        [SerializeField] Renderer[] eyes;
        [SerializeField] Light glow;
        [SerializeField] ParticleSystem motes;
        [SerializeField] Text screenHint;

        [Header("Trezire")]
        [SerializeField] float holdTime = 2f;
        [SerializeField] float wakeDistance = 2.8f;
        [SerializeField] float maxFacingAngle = 55f;

        [Header("Poze")]
        [SerializeField] Vector3 curledSpine = new Vector3(78f, 0f, 6f);
        [SerializeField] Vector3 curledHead = new Vector3(38f, 0f, 0f);
        [SerializeField] Vector3 bowSpine = new Vector3(34f, 0f, 0f);
        [SerializeField] Vector3 bowHead = new Vector3(28f, 0f, 0f);
        [SerializeField] float sleepGlow = 0.25f;
        [SerializeField] float awakeGlow = 6f;

        [Header("Indicii")]
        [SerializeField] float nearHintTime = 4f;
        [SerializeField] string nearHint = "Something small is sleeping among the stones...";
        [SerializeField] string stirHint = "It stirs...";
        [SerializeField] string wokeHint = "The Sleeper's child bows to you.";

        public event Action onWoken;
        public bool Woken { get; private set; }

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        InputHandler input;
        Transform player;
        Material eyeMat;
        Color eyeColor;
        float lightBase, holdTimer, releaseRadius, breath, nearTime;
        bool kneeling, playing, hinted;
        Quaternion baseRot;

        void Awake()
        {
            SphereCollider trigger = GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            releaseRadius = trigger.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z) + 1f;
            baseRot = transform.rotation;

            // ochii folosesc un singur material comun, instantiat o data
            if (eyes != null && eyes.Length > 0 && eyes[0] != null)
            {
                eyeMat = new Material(eyes[0].sharedMaterial);
                eyeColor = eyeMat.GetColor(EmissionId);
                float max = Mathf.Max(eyeColor.r, Mathf.Max(eyeColor.g, eyeColor.b));
                if (max > 0.0001f) eyeColor /= max;
                foreach (Renderer eye in eyes)
                    if (eye != null) eye.sharedMaterial = eyeMat;
            }
            if (glow != null) lightBase = glow.intensity;
            Pose(curledSpine, curledHead);
            SetGlow(sleepGlow);
        }

        void OnDisable() => Release();

        void OnTriggerEnter(Collider other)
        {
            if (input != null || playing) return;
            InputHandler handler = other.GetComponentInParent<InputHandler>();
            if (handler == null) return;
            input = handler;
            player = handler.transform;
            input.onCrouchHold += HandleCrouch;
        }

        void Update()
        {
            if (!playing)
            {
                // respiratie lenta in somn
                breath += Time.deltaTime;
                float k = 0.5f + 0.5f * Mathf.Sin(breath * 1.3f);
                if (spine != null) spine.localRotation = Quaternion.Euler(curledSpine + new Vector3(-2.5f * k, 0f, 0f));
                SetGlow(sleepGlow * (0.7f + 0.6f * k) * (Woken ? 1.6f : 1f));
            }
            if (playing || player == null) return;

            Vector3 toChild = transform.position - player.position;
            toChild.y = 0f;
            float distance = toChild.magnitude;
            if (distance > releaseRadius) { Release(); return; }

            Vector3 facing = Vector3.ProjectOnPlane(player.forward, Vector3.up);
            bool bowing = !Woken && kneeling && distance <= wakeDistance && Vector3.Angle(facing, toChild) <= maxFacingAngle;
            holdTimer = bowing ? holdTimer + Time.deltaTime : Mathf.MoveTowards(holdTimer, 0f, Time.deltaTime * 2f);
            float stir = Mathf.Clamp01(holdTimer / holdTime);
            SetGlow(Mathf.Lerp(sleepGlow, awakeGlow * 0.4f, stir));
            if (head != null) head.localRotation = Quaternion.Euler(curledHead + new Vector3(-10f * stir, 0f, 0f));

            // indiciul de apropiere apare o singura data, cateva secunde
            if (!Woken)
            {
                if (bowing) ShowHint(stirHint);
                else if (!hinted && nearTime < nearHintTime) ShowHint(nearHint);
                else ShowHint(null);
                if (!bowing && !hinted) nearTime += Time.deltaTime;
                if (nearTime >= nearHintTime) hinted = true;
            }
            if (holdTimer >= holdTime) StartCoroutine(wake());
        }

        void HandleCrouch(bool held)
        {
            kneeling = held;
            if (!held) holdTimer = Mathf.Min(holdTimer, holdTime * 0.5f);
        }

        IEnumerator wake()
        {
            playing = true;
            Woken = true;
            hinted = true;
            holdTimer = 0f;
            ShowHint(null);
            Vector3 look = player != null ? player.position : transform.position + transform.forward;
            Release();
            if (motes != null) motes.Play(true);

            // ochii se aprind si se ridica, intorcandu-se spre jucator
            Quaternion from = transform.rotation;
            Vector3 to = look - transform.position;
            to.y = 0f;
            Quaternion face = to.sqrMagnitude > 0.01f ? Quaternion.LookRotation(to) : from;
            yield return Blend(1.1f, curledSpine, curledHead, Vector3.zero, new Vector3(-12f, 0f, 0f), from, face, sleepGlow, awakeGlow);
            yield return new WaitForSeconds(0.6f);
            ShowHint(wokeHint);

            // plecaciunea
            yield return Blend(0.7f, Vector3.zero, new Vector3(-12f, 0f, 0f), bowSpine, bowHead, face, face, awakeGlow, awakeGlow * 0.8f);
            yield return new WaitForSeconds(1.1f);
            yield return Blend(0.6f, bowSpine, bowHead, Vector3.zero, Vector3.zero, face, face, awakeGlow * 0.8f, awakeGlow * 0.6f);
            yield return new WaitForSeconds(0.8f);
            ShowHint(null);

            // se ghemuieste la loc, ramane cu o lumina calda
            yield return Blend(1.6f, Vector3.zero, Vector3.zero, curledSpine, curledHead, face, Quaternion.Slerp(face, baseRot, 0.5f), awakeGlow * 0.6f, sleepGlow * 1.6f);
            if (motes != null) motes.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            playing = false;
            onWoken?.Invoke();
        }

        IEnumerator Blend(float duration, Vector3 spineFrom, Vector3 headFrom, Vector3 spineTo, Vector3 headTo, Quaternion rootFrom, Quaternion rootTo, float glowFrom, float glowTo)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Smooth(t / duration);
                Pose(Vector3.Lerp(spineFrom, spineTo, k), Vector3.Lerp(headFrom, headTo, k));
                transform.rotation = Quaternion.Slerp(rootFrom, rootTo, k);
                SetGlow(Mathf.Lerp(glowFrom, glowTo, k));
                yield return null;
            }
            Pose(spineTo, headTo);
            transform.rotation = rootTo;
            SetGlow(glowTo);
        }

        void Release()
        {
            if (input != null) input.onCrouchHold -= HandleCrouch;
            input = null;
            player = null;
            kneeling = false;
            holdTimer = 0f;
            if (!playing) ShowHint(null);
        }

        void Pose(Vector3 spineEuler, Vector3 headEuler)
        {
            if (spine != null) spine.localRotation = Quaternion.Euler(spineEuler);
            if (head != null) head.localRotation = Quaternion.Euler(headEuler);
        }

        void SetGlow(float k)
        {
            if (eyeMat != null) eyeMat.SetColor(EmissionId, eyeColor * k);
            if (glow != null) glow.intensity = lightBase * k / Mathf.Max(0.01f, awakeGlow);
        }

        void ShowHint(string text)
        {
            if (screenHint == null) return;
            bool on = !string.IsNullOrEmpty(text);
            if (on) screenHint.text = text;
            else if (screenHint.text != nearHint && screenHint.text != stirHint && screenHint.text != wokeHint) return;   // nu stingem indiciile altora
            if (screenHint.gameObject.activeSelf != on) screenHint.gameObject.SetActive(on);
        }

        static float Smooth(float k)
        {
            k = Mathf.Clamp01(k);
            return k * k * (3f - 2f * k);
        }
    }
}
