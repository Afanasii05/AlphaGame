using System.Collections;
using Alpha.Input;
using UnityEngine;

namespace Alpha.World
{
    // Inelul de pietre care inchide arena. La Seal pamantul tremura, se crapa si pietrele erup in val
    // pornind din punctul cel mai apropiat de jucator; la Open se cutremura, se faramiteaza si se scufunda.
    public class ArenaSeal : MonoBehaviour
    {
        [SerializeField] Transform[] rocks;
        [SerializeField] GameObject[] barriers;

        [Header("FX")]
        [SerializeField] ParticleSystem dustPrefab;
        [SerializeField] ParticleSystem rumbleFxPrefab;
        [SerializeField] ParticleSystem burstFxPrefab;
        [SerializeField] ParticleSystem settleFxPrefab;
        [SerializeField] GroundCrack crackPrefab;
        [SerializeField] DebrisChunk[] debrisPrefabs;
        [SerializeField] AudioClip rumbleClip;
        [SerializeField] AudioClip eruptClip;
        [SerializeField] AudioClip sinkClip;

        [Header("Ridicare")]
        [SerializeField] float rumbleTime = 0.8f;
        [SerializeField] float punchTime = 0.38f;
        [SerializeField] float settleTime = 0.65f;
        [SerializeField] float waveSpan = 1.4f;
        [SerializeField] float waveJitter = 0.05f;
        [SerializeField] float overshoot = 0.45f;
        [SerializeField] float wobbleDegrees = 7f;
        [SerializeField] int debrisPerRock = 6;
        [SerializeField] float crackSizeFactor = 2.4f;

        [Header("Coborare")]
        [SerializeField] float shudderTime = 0.7f;
        [SerializeField] float sinkDuration = 1.8f;
        [SerializeField] float openWaveSpan = 1.1f;
        [SerializeField] float jitter = 0.1f;
        [SerializeField] float crackFadeDelay = 1.2f;
        [SerializeField] float crackFadeTime = 2.5f;

        [Header("Camera")]
        [SerializeField] float eruptShake = 0.32f;
        [SerializeField] float rumbleShake = 0.07f;

        public bool IsSealed { get; private set; }
        public float SealDuration => waveSpan + rumbleTime + punchTime + settleTime;

        Vector3[] upPositions, groundPoints;
        Quaternion[] upRotations;
        float[] depths, footprints, angles;
        GroundCrack[] cracks;
        Transform player;

        void Awake()
        {
            int n = rocks.Length;
            upPositions = new Vector3[n];
            groundPoints = new Vector3[n];
            upRotations = new Quaternion[n];
            depths = new float[n];
            footprints = new float[n];
            angles = new float[n];
            cracks = new GroundCrack[n];
            for (int i = 0; i < n; i++)
            {
                // pozitia din editor e cea ridicata, la start pietrele stau ingropate
                Bounds bounds = RockBounds(rocks[i]);
                upPositions[i] = rocks[i].position;
                upRotations[i] = rocks[i].rotation;
                groundPoints[i] = new Vector3(bounds.center.x, bounds.min.y + 0.4f, bounds.center.z);
                depths[i] = bounds.size.y + 0.5f;
                footprints[i] = Mathf.Max(bounds.extents.x, bounds.extents.z);
                Vector3 offset = bounds.center - transform.position;
                angles[i] = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
                rocks[i].position = Buried(i);
                rocks[i].gameObject.SetActive(false);
            }
            SetBarriers(false);
        }

        public void Seal()
        {
            if (IsSealed) return;
            IsSealed = true;
            StopAllCoroutines();
            SetBarriers(true);
            float origin = WaveOrigin();
            for (int i = 0; i < rocks.Length; i++)
                StartCoroutine(erupt(i, WaveDelay(i, origin, waveSpan)));
            StartCoroutine(rumbleFor(SealDuration, 1f));
            PlayClip(rumbleClip, transform.position);
        }

        public void Open()
        {
            if (!IsSealed) return;
            IsSealed = false;
            StopAllCoroutines();
            SetBarriers(false);
            float origin = WaveOrigin();
            for (int i = 0; i < rocks.Length; i++)
                StartCoroutine(crumble(i, WaveDelay(i, origin, openWaveSpan)));
            StartCoroutine(rumbleFor(openWaveSpan + shudderTime + sinkDuration, 0.7f));
            PlayClip(sinkClip, transform.position);
        }

        // ------------------------------------------------------------------ ridicare

        IEnumerator erupt(int i, float delay)
        {
            yield return new WaitForSeconds(delay);
            Transform rock = rocks[i];
            Vector3 ground = groundPoints[i];
            Vector3 outward = Outward(i);

            // 1. tremur: praf, pietricele si crapatura care se intinde
            GroundCrack crack = SpawnCrack(i);
            if (crack != null) crack.GrowTo(0.6f, rumbleTime);
            SpawnFx(rumbleFxPrefab, ground, footprints[i], rumbleTime + 2f);
            yield return new WaitForSeconds(rumbleTime);

            // 2. pamantul se rupe: nor gros, bucati aruncate, piatra tasneste
            if (crack != null) crack.GrowTo(1f, 0.25f);
            SpawnFx(burstFxPrefab != null ? burstFxPrefab : dustPrefab, ground, footprints[i], 5f);
            SpawnDebris(i, ground, debrisPerRock, 6f, 11f);
            CameraRumble rumble = CameraRumble.Main;
            if (rumble != null) rumble.ImpulseAt(ground, eruptShake, 0.45f);
            PlayClip(eruptClip, ground);

            rock.gameObject.SetActive(true);
            Vector3 from = Buried(i), up = upPositions[i], peak = up + Vector3.up * overshoot;
            Vector3 tiltAxis = Vector3.Cross(Vector3.up, outward) * (i % 2 == 0 ? 1f : -1f);
            float total = punchTime + settleTime;
            bool settled = false;
            for (float t = 0f; t < total; t += Time.deltaTime)
            {
                Vector3 pos;
                if (t < punchTime)
                {
                    float k = t / punchTime;
                    pos = Vector3.LerpUnclamped(from, peak, 1f - (1f - k) * (1f - k) * (1f - k));
                }
                else
                {
                    // revenire amortizata pe pozitia finala
                    if (!settled)
                    {
                        settled = true;
                        SpawnFx(settleFxPrefab, ground, footprints[i], 3f);
                    }
                    float k = (t - punchTime) / settleTime;
                    pos = up + Vector3.up * overshoot * Mathf.Exp(-4f * k) * Mathf.Cos(k * Mathf.PI * 2.5f);
                }
                float tilt = wobbleDegrees * Mathf.Exp(-3.5f * t / total) * Mathf.Cos(t / total * Mathf.PI * 4f);
                rock.SetPositionAndRotation(pos, Quaternion.AngleAxis(tilt, tiltAxis) * upRotations[i]);
                yield return null;
            }
            rock.SetPositionAndRotation(up, upRotations[i]);
        }

        // ------------------------------------------------------------------ coborare

        IEnumerator crumble(int i, float delay)
        {
            yield return new WaitForSeconds(delay);
            Transform rock = rocks[i];
            Vector3 ground = groundPoints[i];
            Vector3 start = rock.position;
            Quaternion baseRot = rock.rotation;

            // 1. piatra se cutremura si se faramiteaza
            SpawnFx(rumbleFxPrefab, ground, footprints[i], shudderTime + 2f);
            SpawnDebris(i, ground + Vector3.up * depths[i] * 0.6f, debrisPerRock / 2, 0.5f, 2.5f);
            for (float t = 0f; t < shudderTime; t += Time.deltaTime)
            {
                float k = t / shudderTime;
                Vector2 shake = Random.insideUnitCircle * jitter * (0.4f + k);
                rock.SetPositionAndRotation(start + new Vector3(shake.x, 0f, shake.y),
                    baseRot * Quaternion.Euler(Random.Range(-1.5f, 1.5f) * k, 0f, Random.Range(-1.5f, 1.5f) * k));
                yield return null;
            }

            // 2. se scufunda in pamant cu praf
            SpawnFx(burstFxPrefab != null ? burstFxPrefab : dustPrefab, ground, footprints[i] * 0.7f, 5f);
            CameraRumble rumble = CameraRumble.Main;
            if (rumble != null) rumble.ImpulseAt(ground, eruptShake * 0.5f, 0.4f);
            Vector3 to = Buried(i);
            for (float t = 0f; t < sinkDuration; t += Time.deltaTime)
            {
                float k = t / sinkDuration;
                Vector2 shake = Random.insideUnitCircle * jitter * (1f - k * 0.5f);
                rock.SetPositionAndRotation(Vector3.LerpUnclamped(start, to, k * k) + new Vector3(shake.x, 0f, shake.y), baseRot);
                yield return null;
            }
            SpawnFx(settleFxPrefab, ground, footprints[i] * 0.8f, 3f);
            rock.SetPositionAndRotation(to, upRotations[i]);
            rock.gameObject.SetActive(false);
            if (cracks[i] != null) cracks[i].FadeOut(crackFadeDelay, crackFadeTime);
            cracks[i] = null;
        }

        // ------------------------------------------------------------------ utilitare

        IEnumerator rumbleFor(float duration, float scale)
        {
            CameraRumble rumble = CameraRumble.Main;
            if (rumble == null) yield break;
            rumble.SetRumble(rumbleShake * scale, 0.5f);
            yield return new WaitForSeconds(duration);
            rumble.SetRumble(0f, 1f);
        }

        // valul porneste din unghiul jucatorului si merge in ambele directii pe inel
        float WaveOrigin()
        {
            if (player == null)
            {
                InputHandler input = FindAnyObjectByType<InputHandler>();
                if (input != null) player = input.transform;
            }
            Vector3 from = player != null ? player.position : Camera.main != null ? Camera.main.transform.position : transform.position + Vector3.forward;
            Vector3 offset = from - transform.position;
            if (offset.sqrMagnitude < 0.25f && Camera.main != null) offset = Camera.main.transform.forward;
            return Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        }

        float WaveDelay(int i, float origin, float span)
        {
            float k = Mathf.Abs(Mathf.DeltaAngle(angles[i], origin)) / 180f;
            float wobble = Mathf.Sin(i * 12.9898f) * waveJitter;   // abatere mica, determinista
            return Mathf.Max(0f, span * k + wobble);
        }

        Vector3 Buried(int i) => upPositions[i] + Vector3.down * depths[i];

        Vector3 Outward(int i) => Quaternion.Euler(0f, angles[i], 0f) * Vector3.forward;

        GroundCrack SpawnCrack(int i)
        {
            if (crackPrefab == null) return null;
            if (cracks[i] != null) Destroy(cracks[i].gameObject);
            GroundCrack crack = Instantiate(crackPrefab, groundPoints[i], Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * crackPrefab.transform.rotation);
            crack.SetSize(footprints[i] * crackSizeFactor);
            cracks[i] = crack;
            return crack;
        }

        void SpawnDebris(int i, Vector3 point, int count, float minUp, float maxUp)
        {
            if (debrisPrefabs == null || debrisPrefabs.Length == 0) return;
            for (int n = 0; n < count; n++)
            {
                DebrisChunk prefab = debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];
                if (prefab == null) continue;
                Vector2 disc = Random.insideUnitCircle * footprints[i] * 0.8f;
                Vector3 pos = point + new Vector3(disc.x, 0.3f, disc.y);
                Vector3 side = new Vector3(disc.x, 0f, disc.y).normalized * Random.Range(2f, 5f);
                DebrisChunk chunk = Instantiate(prefab, pos, Random.rotation);
                chunk.Launch(Vector3.up * Random.Range(minUp, maxUp) + side, Random.Range(0.18f, 0.42f));
            }
        }

        void SpawnFx(ParticleSystem prefab, Vector3 point, float footprint, float life)
        {
            if (prefab == null) return;
            ParticleSystem fx = Instantiate(prefab, point, prefab.transform.rotation);
            fx.transform.localScale = prefab.transform.localScale * Mathf.Clamp(footprint / 2.4f, 0.5f, 2f);
            fx.Play(true);
            Destroy(fx.gameObject, life);
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

        static Bounds RockBounds(Transform rock)
        {
            Renderer[] renderers = rock.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(rock.position, Vector3.one * 6f);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
        }
    }
}
