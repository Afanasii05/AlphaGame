using UnityEngine;

namespace Alpha.World
{
    // Tremur de camera cumulativ: un vuiet de fond + impulsuri scurte care se aduna.
    // Ruleaza dupa CameraController/CameraShake si adauga doar un offset peste pozitia din frame.
    [DefaultExecutionOrder(110)]
    public class CameraRumble : MonoBehaviour
    {
        [SerializeField] float frequency = 22f;
        [SerializeField] float maxStrength = 0.6f;
        [SerializeField] float tiltDegrees = 1.2f;

        const int MaxImpulses = 16;
        readonly float[] impulseStrength = new float[MaxImpulses];
        readonly float[] impulseTime = new float[MaxImpulses];
        readonly float[] impulseDuration = new float[MaxImpulses];
        float rumble, rumbleTarget, rumbleFade = 1f, seed;
        Vector3 lastOffset, lastPosition;
        Quaternion lastTilt = Quaternion.identity, lastRotation;

        public static CameraRumble Main
        {
            get
            {
                Camera cam = Camera.main;
                if (cam == null) return null;
                CameraRumble rumble = cam.GetComponent<CameraRumble>();
                return rumble != null ? rumble : cam.gameObject.AddComponent<CameraRumble>();
            }
        }

        void Awake() => seed = Random.value * 100f;

        // vuiet continuu; se apropie lin de tinta in fadeTime secunde
        public void SetRumble(float strength, float fadeTime = 0.3f)
        {
            rumbleTarget = Mathf.Max(0f, strength);
            rumbleFade = Mathf.Max(fadeTime, 0.01f);
        }

        public void Impulse(float strength, float duration)
        {
            int slot = 0;
            for (int i = 1; i < MaxImpulses; i++)
                if (Remaining(i) < Remaining(slot)) slot = i;
            impulseStrength[slot] = strength;
            impulseDuration[slot] = Mathf.Max(duration, 0.01f);
            impulseTime[slot] = impulseDuration[slot];
        }

        // impuls atenuat cu distanta fata de camera
        public void ImpulseAt(Vector3 source, float strength, float duration, float near = 8f, float far = 45f)
        {
            float distance = Vector3.Distance(source, transform.position);
            float k = 1f - Mathf.Clamp01((distance - near) / Mathf.Max(far - near, 0.01f));
            if (k > 0f) Impulse(strength * k * k, duration);
        }

        float Remaining(int i) => impulseTime[i] > 0f ? impulseStrength[i] * impulseTime[i] / impulseDuration[i] : 0f;

        void LateUpdate()
        {
            // daca nimeni nu a rescris camera in acest frame, scoatem offsetul vechi ca sa nu se adune
            if (transform.position == lastPosition) transform.position -= lastOffset;
            if (transform.rotation == lastRotation) transform.rotation *= Quaternion.Inverse(lastTilt);
            lastOffset = Vector3.zero;
            lastTilt = Quaternion.identity;

            rumble = Mathf.MoveTowards(rumble, rumbleTarget, Time.deltaTime / rumbleFade * Mathf.Max(rumbleTarget, rumble, 0.05f));
            float strength = rumble;
            for (int i = 0; i < MaxImpulses; i++)
            {
                if (impulseTime[i] <= 0f) continue;
                float k = impulseTime[i] / impulseDuration[i];
                strength += impulseStrength[i] * k * k;
                impulseTime[i] -= Time.deltaTime;
            }
            if (strength > 0.0001f)
            {
                strength = Mathf.Min(strength, maxStrength);
                float t = Time.time * frequency;
                lastOffset = transform.TransformDirection(new Vector3(Noise(t, 0f), Noise(0f, t), 0f) * strength);
                lastTilt = Quaternion.Euler(0f, 0f, Noise(t * 0.7f, 5f) * tiltDegrees * strength / maxStrength);
                transform.position += lastOffset;
                transform.rotation *= lastTilt;
            }
            lastPosition = transform.position;
            lastRotation = transform.rotation;
        }

        float Noise(float x, float y) => (Mathf.PerlinNoise(x + seed, y + seed * 0.5f) - 0.5f) * 2f;
    }
}
