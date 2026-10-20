using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Alpha.Combat
{
    [DefaultExecutionOrder(1000)]
    public class CombatFeedback : MonoBehaviour
    {
        public static CombatFeedback Instance { get; private set; }

        [SerializeField] private float hitstopScale = 0.02f;
        [SerializeField] private float flashDuration = 0.3f;
        [SerializeField] private float shakeFrequency = 25f;

        private Camera cam;
        private float baseFov;

        private float shakeStrength;
        private float shakeDuration = 1f;
        private float shakeLeft;

        private float hitstopUntil;
        private float slowUntil;
        private float slowScale = 1f;
        private bool timeChanged;

        private float fovKick;
        private float boostCurrent;
        private float appliedFov;
        private readonly Dictionary<object, float> boosts = new Dictionary<object, float>();

        private Volume flashVolume;
        private VolumeProfile flashProfile;
        private float flashPeak;
        private float flashLeft;

        private void Awake()
        {
            Instance = this;
            CreateFlashVolume();
        }

        private void Start()
        {
            cam = Camera.main;
            if (cam != null)
                baseFov = cam.fieldOfView;
        }

        private void OnDisable()
        {
            if (timeChanged) //Nu las jocul blocat in slow-motion
            {
                Time.timeScale = 1f;
                timeChanged = false;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            if (flashProfile != null)
                Destroy(flashProfile);
        }

        public void Shake(float strength, float duration)
        {
            if (duration <= 0f)
                return;
            float current = shakeLeft > 0f ? shakeStrength * shakeLeft / shakeDuration : 0f;
            if (strength < current) //Pastrez shake-ul mai puternic
                return;
            shakeStrength = strength;
            shakeDuration = duration;
            shakeLeft = duration;
        }

        public void Hitstop(float duration)
        {
            hitstopUntil = Mathf.Max(hitstopUntil, Time.unscaledTime + duration);
        }

        public void SlowMotion(float scale, float duration)
        {
            slowScale = scale;
            slowUntil = Time.unscaledTime + duration;
        }

        public void FovKick(float degrees)
        {
            fovKick = Mathf.Clamp(fovKick + degrees, -15f, 15f);
        }

        public void SetFovBoost(object owner, float degrees)
        {
            if (Mathf.Approximately(degrees, 0f))
                boosts.Remove(owner);
            else
                boosts[owner] = degrees;
        }

        public void Flash(float intensity)
        {
            flashPeak = Mathf.Clamp01(intensity);
            flashLeft = flashDuration;
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            float scale = 1f;
            if (now < slowUntil)
                scale = slowScale;
            if (now < hitstopUntil)
                scale = hitstopScale;
            if (scale == 1f && !timeChanged) //Nimic activ: las timeScale-ul altor scripturi in pace
                return;
            Time.timeScale = scale;
            timeChanged = scale != 1f;
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            if (cam == null)
            {
                cam = Camera.main;
                if (cam != null)
                    baseFov = cam.fieldOfView;
            }
            if (cam != null)
            {
                ApplyShake(dt);
                ApplyFov(dt);
            }
            UpdateFlash(dt);
        }

        private void ApplyShake(float dt)
        {
            if (shakeLeft <= 0f)
                return;
            shakeLeft -= dt;
            float k = Mathf.Clamp01(shakeLeft / shakeDuration);
            float amplitude = shakeStrength * k * k;
            float t = Time.unscaledTime * shakeFrequency;
            float x = (Mathf.PerlinNoise(t, 0.37f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0.91f, t) - 0.5f) * 2f;
            Transform camTr = cam.transform;
            camTr.position += (camTr.right * x + camTr.up * y) * amplitude; //Doar pozitie: CameraController o reface in fiecare frame
        }

        private void ApplyFov(float dt)
        {
            fovKick = Mathf.Lerp(fovKick, 0f, 1f - Mathf.Exp(-12f * dt));
            if (Mathf.Abs(fovKick) < 0.01f)
                fovKick = 0f;
            float target = 0f;
            foreach (float boost in boosts.Values)
                target += boost;
            boostCurrent = Mathf.Lerp(boostCurrent, target, 1f - Mathf.Exp(-8f * dt));
            if (target == 0f && Mathf.Abs(boostCurrent) < 0.01f)
                boostCurrent = 0f;
            float offset = fovKick + boostCurrent;
            if (offset != 0f || appliedFov != 0f) //Cand nu e nimic activ nu ating FOV-ul camerei
                cam.fieldOfView = baseFov + offset;
            appliedFov = offset;
        }

        private void UpdateFlash(float dt)
        {
            if (flashLeft <= 0f)
                return;
            flashLeft -= dt;
            flashVolume.weight = flashPeak * Mathf.Clamp01(flashLeft / flashDuration);
        }

        private void CreateFlashVolume() //Volum global creat din cod, cu weight 0 pana la primul Flash
        {
            GameObject volumeObject = new GameObject("CombatFlashVolume");
            volumeObject.transform.SetParent(transform, false);
            flashVolume = volumeObject.AddComponent<Volume>();
            flashVolume.isGlobal = true;
            flashVolume.priority = 100f;
            flashVolume.weight = 0f;
            flashProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            ChromaticAberration chroma = flashProfile.Add<ChromaticAberration>(true);
            chroma.intensity.Override(1f);
            LensDistortion lens = flashProfile.Add<LensDistortion>(true);
            lens.intensity.Override(-0.4f);
            flashVolume.sharedProfile = flashProfile;
        }
    }
}
