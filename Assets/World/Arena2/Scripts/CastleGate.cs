using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Alpha.World
{
    // Poarta castelului: canaturile se trantesc, grilajul cade si faclele palpaie de la suflul usilor.
    // Pozitia din editor e cea inchisa (canaturi in arc, grilaj jos); la start poarta se deschide fara animatie,
    // daca nu e marcata ca inchisa de la inceput (poterna din spate).
    public class CastleGate : MonoBehaviour
    {
        [SerializeField] Transform[] doors;
        [SerializeField] float[] openYaw;
        [SerializeField] Transform portcullis;
        [SerializeField] float portcullisLift = 9f;
        [SerializeField] GameObject[] barriers;
        [SerializeField] bool startClosed;

        [Header("FX")]
        [SerializeField] Transform[] dustPoints;
        [SerializeField] ParticleSystem slamFxPrefab;
        [SerializeField] ParticleSystem dustFxPrefab;
        [SerializeField] DebrisChunk[] debrisPrefabs;
        [SerializeField] int debrisCount = 8;
        [SerializeField] LightFlicker[] torches;
        [SerializeField] AudioClip slamClip;
        [SerializeField] AudioClip grindClip;

        [Header("Inchidere")]
        [SerializeField] float swingTime = 1.15f;
        [SerializeField] float dropDelay = 0.55f;
        [SerializeField] float dropTime = 0.42f;
        [SerializeField] float bounce = 0.18f;
        [SerializeField] float flickerTime = 1.8f;
        [SerializeField] float flickerAmount = 1f;
        [SerializeField] float flickerSpeed = 22f;

        [Header("Deschidere")]
        [SerializeField] float openTime = 3.2f;
        [SerializeField] float liftTime = 2.6f;

        [Header("Camera")]
        [SerializeField] float slamShake = 0.42f;
        [SerializeField] float rumbleShake = 0.06f;

        [SerializeField] UnityEvent onClosed;
        [SerializeField] UnityEvent onOpened;

        public bool IsClosed { get; private set; }
        public float CloseDuration => Mathf.Max(swingTime, dropDelay + dropTime) + 0.2f;

        // trezirea cavalerului se leaga aici: se anunta cand poarta s-a trantit
        public event Action Closed;
        public event Action Opened;

        Quaternion[] closedRotations;
        Vector3 portcullisDown;
        float[] torchAmounts, torchSpeeds;

        void Awake()
        {
            closedRotations = new Quaternion[doors.Length];
            for (int i = 0; i < doors.Length; i++)
                if (doors[i] != null) closedRotations[i] = doors[i].localRotation;
            if (portcullis != null) portcullisDown = portcullis.localPosition;
            torchAmounts = new float[torches.Length];
            torchSpeeds = new float[torches.Length];
            for (int i = 0; i < torches.Length; i++)
            {
                if (torches[i] == null) continue;
                torchAmounts[i] = torches[i].amount;
                torchSpeeds[i] = torches[i].speed;
            }
            IsClosed = startClosed;
            SetPose(startClosed ? 1f : 0f, startClosed ? 1f : 0f);
            SetBarriers(startClosed);
        }

        public void Close()
        {
            if (IsClosed) return;
            IsClosed = true;
            StopAllCoroutines();
            // bariera intra imediat: jucatorul nu se mai poate strecura printre canaturi
            SetBarriers(true);
            StartCoroutine(slam());
        }

        public void Open()
        {
            if (!IsClosed) return;
            IsClosed = false;
            StopAllCoroutines();
            StartCoroutine(open());
        }

        // ------------------------------------------------------------------ animatii

        IEnumerator slam()
        {
            float doorFrom = DoorProgress(), gridFrom = GridProgress();
            PlayClip(grindClip, transform.position);
            CameraRumble rumble = CameraRumble.Main;
            if (rumble != null) rumble.SetRumble(rumbleShake, 0.3f);
            bool doorsShut = doors.Length == 0, gridShut = portcullis == null;
            float total = CloseDuration;
            for (float t = 0f; t < total; t += Time.deltaTime)
            {
                // canaturile accelereaza (impinse de vant), grilajul cade liber dupa o clipa
                float kd = Mathf.Clamp01(t / swingTime);
                float door = Mathf.Lerp(doorFrom, 1f, kd * kd * kd);
                float kg = Mathf.Clamp01((t - dropDelay) / dropTime);
                float grid = Mathf.Lerp(gridFrom, 1f, kg * kg);
                if (!gridShut && kg >= 1f)
                {
                    gridShut = true;
                    Impact(0.7f);
                }
                if (!doorsShut && kd >= 1f)
                {
                    doorsShut = true;
                    Impact(1f);
                }
                SetPose(door, grid + GridBounce(t - dropDelay - dropTime));
                yield return null;
            }
            SetPose(1f, 1f);
            if (rumble != null) rumble.SetRumble(0f, 0.8f);
            onClosed?.Invoke();
            Closed?.Invoke();
            yield return new WaitForSeconds(flickerTime);
            RestoreTorches();
        }

        IEnumerator open()
        {
            float doorFrom = DoorProgress(), gridFrom = GridProgress();
            PlayClip(grindClip, transform.position);
            CameraRumble rumble = CameraRumble.Main;
            if (rumble != null) rumble.SetRumble(rumbleShake * 0.7f, 0.5f);
            SpawnDust(dustFxPrefab, 0.8f, 6f);
            float total = Mathf.Max(openTime, liftTime);
            for (float t = 0f; t < total; t += Time.deltaTime)
            {
                float kd = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / openTime));
                float kg = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / liftTime));
                // usile grele pornesc smucit: un tremur mic la inceput
                float shudder = t < 0.4f ? Mathf.Sin(t * 60f) * 0.004f * (1f - t / 0.4f) : 0f;
                SetPose(Mathf.Lerp(doorFrom, 0f, kd) + shudder, Mathf.Lerp(gridFrom, 0f, kg));
                yield return null;
            }
            SetPose(0f, 0f);
            SetBarriers(false);
            if (rumble != null) rumble.SetRumble(0f, 1f);
            onOpened?.Invoke();
            Opened?.Invoke();
        }

        // ------------------------------------------------------------------ utilitare

        // closed: 1 = inchis (pozitia din editor), 0 = deschis
        void SetPose(float door, float grid)
        {
            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] == null) continue;
                float yaw = (1f - door) * (openYaw != null && i < openYaw.Length ? openYaw[i] : 0f);
                doors[i].localRotation = closedRotations[i] * Quaternion.Euler(0f, yaw, 0f);
            }
            if (portcullis != null) portcullis.localPosition = portcullisDown + Vector3.up * (1f - grid) * portcullisLift;
        }

        float DoorProgress()
        {
            if (doors.Length == 0 || doors[0] == null || openYaw == null || openYaw.Length == 0 || Mathf.Approximately(openYaw[0], 0f)) return IsClosed ? 0f : 1f;
            float angle = Quaternion.Angle(closedRotations[0], doors[0].localRotation);
            return 1f - Mathf.Clamp01(angle / Mathf.Abs(openYaw[0]));
        }

        float GridProgress()
        {
            if (portcullis == null) return IsClosed ? 0f : 1f;
            return 1f - Mathf.Clamp01((portcullis.localPosition.y - portcullisDown.y) / Mathf.Max(portcullisLift, 0.01f));
        }

        // saritura mica a grilajului dupa ce loveste pragul (valori negative = deasupra pozitiei inchise)
        float GridBounce(float t)
        {
            if (t <= 0f) return 0f;
            return -bounce * Mathf.Exp(-6f * t) * Mathf.Abs(Mathf.Sin(t * 14f)) / Mathf.Max(portcullisLift, 0.01f);
        }

        void Impact(float strength)
        {
            CameraRumble rumble = CameraRumble.Main;
            if (rumble != null) rumble.ImpulseAt(transform.position, slamShake * strength, 0.5f, 10f, 60f);
            PlayClip(slamClip, transform.position);
            SpawnDust(slamFxPrefab, 1.2f * strength, 6f);
            SpawnDust(dustFxPrefab, 1f, 6f);
            SpawnDebris(Mathf.RoundToInt(debrisCount * strength));
            FlickerTorches();
        }

        void FlickerTorches()
        {
            for (int i = 0; i < torches.Length; i++)
            {
                if (torches[i] == null) continue;
                torches[i].amount = flickerAmount;
                torches[i].speed = flickerSpeed;
            }
            StartCoroutine(calmTorches());
        }

        IEnumerator calmTorches()
        {
            yield return new WaitForSeconds(0.2f);
            for (float t = 0f; t < flickerTime; t += Time.deltaTime)
            {
                float k = t / flickerTime;
                for (int i = 0; i < torches.Length; i++)
                {
                    if (torches[i] == null) continue;
                    torches[i].amount = Mathf.Lerp(flickerAmount, torchAmounts[i], k);
                    torches[i].speed = Mathf.Lerp(flickerSpeed, torchSpeeds[i], k);
                }
                yield return null;
            }
            RestoreTorches();
        }

        void RestoreTorches()
        {
            for (int i = 0; i < torches.Length; i++)
            {
                if (torches[i] == null) continue;
                torches[i].amount = torchAmounts[i];
                torches[i].speed = torchSpeeds[i];
            }
        }

        void SpawnDust(ParticleSystem prefab, float scale, float life)
        {
            if (prefab == null || dustPoints == null) return;
            foreach (Transform point in dustPoints)
            {
                if (point == null) continue;
                ParticleSystem fx = Instantiate(prefab, point.position, prefab.transform.rotation);
                fx.transform.localScale = prefab.transform.localScale * scale;
                fx.Play(true);
                Destroy(fx.gameObject, life);
            }
        }

        void SpawnDebris(int count)
        {
            if (debrisPrefabs == null || debrisPrefabs.Length == 0 || dustPoints == null || dustPoints.Length == 0) return;
            for (int n = 0; n < count; n++)
            {
                DebrisChunk prefab = debrisPrefabs[UnityEngine.Random.Range(0, debrisPrefabs.Length)];
                Transform point = dustPoints[n % dustPoints.Length];
                if (prefab == null || point == null) continue;
                // bucatile cad din bolta, nu tasnesc din pamant
                Vector3 pos = point.position + point.right * UnityEngine.Random.Range(-3f, 3f) + Vector3.up * UnityEngine.Random.Range(6f, 9f);
                DebrisChunk chunk = Instantiate(prefab, pos, UnityEngine.Random.rotation);
                chunk.Launch(point.forward * UnityEngine.Random.Range(0.5f, 2.5f) + Vector3.down * UnityEngine.Random.Range(0.5f, 2f), UnityEngine.Random.Range(0.12f, 0.3f));
            }
        }

        void SetBarriers(bool on)
        {
            foreach (GameObject barrier in barriers)
                if (barrier != null) barrier.SetActive(on);
        }

        static void PlayClip(AudioClip clip, Vector3 point)
        {
            if (clip != null) AudioSource.PlayClipAtPoint(clip, point);
        }
    }
}
