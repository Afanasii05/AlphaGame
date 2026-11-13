using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace Alpha.World
{
    // Poarta de nord spre Drumul 2. Lespedea cu rune tine drumul inchis pana cand inima golemului
    // e cioplita in ciocan; la Open runele palpaie si se sting, poarta se cutremura si lespedea coboara in pamant.
    public class Road2Gate : MonoBehaviour
    {
        [SerializeField] Transform slab;
        [SerializeField] Renderer[] glyphRenderers;
        [SerializeField] Material glyphMaterial;
        [SerializeField] Light runeLight;
        [SerializeField] Collider[] blockers;
        [SerializeField] NavMeshObstacle obstacle;
        [SerializeField] Transform dustPoint;

        [Header("FX")]
        [SerializeField] ParticleSystem dustPrefab;
        [SerializeField] ParticleSystem burstPrefab;
        [SerializeField] DebrisChunk[] debrisPrefabs;
        [SerializeField] AudioClip openClip;

        [Header("Ritm")]
        [SerializeField] float flickerTime = 1.3f;
        [SerializeField] float shudderTime = 0.9f;
        [SerializeField] float sinkTime = 2.8f;
        [SerializeField] float sinkDepth = 5f;
        [SerializeField] float breathSpeed = 0.9f;

        [Header("Indiciu")]
        [SerializeField] Text screenHint;
        [SerializeField] float hintDistance = 5f;
        [SerializeField] string closedHint = "The seal answers only to the Sleeper's heart...";

        public bool IsOpen { get; private set; }

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        Material liveGlyph;
        Color glyphColor;
        float lightBase, glow = 1f;
        bool opening, hintShown;
        Transform player;

        void Awake()
        {
            // o copie a materialului runelor, ca stingerea sa nu atinga asset-ul
            if (glyphMaterial != null)
            {
                liveGlyph = new Material(glyphMaterial);
                glyphColor = glyphMaterial.GetColor(EmissionId);
                foreach (Renderer r in glyphRenderers)
                {
                    if (r == null) continue;
                    Material[] mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                        if (mats[i] == glyphMaterial) mats[i] = liveGlyph;
                    r.sharedMaterials = mats;
                }
            }
            if (runeLight != null) lightBase = runeLight.intensity;
        }

        void OnDisable() => ShowHint(false);

        void Update()
        {
            if (opening) return;
            // runele respira incet cat timp poarta e inchisa
            float breath = 0.75f + 0.25f * Mathf.Sin(Time.time * breathSpeed * Mathf.PI * 2f * 0.5f);
            SetGlow(breath);
            UpdateHint();
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            opening = true;
            ShowHint(false);
            StartCoroutine(open());
        }

        IEnumerator open()
        {
            Vector3 basePoint = dustPoint != null ? dustPoint.position : transform.position;
            CameraRumble rumble = CameraRumble.Main;

            // 1. runele se aprind o data, apoi palpaie si mor
            for (float t = 0f; t < flickerTime; t += Time.deltaTime)
            {
                float k = t / flickerTime;
                float flare = k < 0.15f ? Mathf.Lerp(1f, 2.6f, k / 0.15f) : Mathf.Lerp(2.6f, 0f, (k - 0.15f) / 0.85f);
                float flicker = Random.value < 0.25f + k * 0.4f ? Random.Range(0.05f, 0.5f) : 1f;
                SetGlow(flare * flicker);
                yield return null;
            }
            SetGlow(0f);
            if (runeLight != null) runeLight.enabled = false;

            // 2. poarta se cutremura
            if (rumble != null) rumble.ImpulseAt(basePoint, 0.22f, 0.5f, 6f, 40f);
            if (openClip != null) AudioSource.PlayClipAtPoint(openClip, basePoint);
            Spawn(dustPrefab, basePoint, 1.2f, 4f);
            SpawnDebris(basePoint + Vector3.up * 4.6f, 5, 0.5f, 2f);
            Vector3 start = slab != null ? slab.position : Vector3.zero;
            Quaternion baseRot = slab != null ? slab.rotation : Quaternion.identity;
            for (float t = 0f; t < shudderTime && slab != null; t += Time.deltaTime)
            {
                float k = t / shudderTime;
                Vector2 shake = Random.insideUnitCircle * 0.03f * (0.4f + k);
                slab.SetPositionAndRotation(start + new Vector3(shake.x, 0f, shake.y), baseRot * Quaternion.Euler(0f, 0f, Random.Range(-0.6f, 0.6f) * k));
                if (rumble != null) rumble.SetRumble(0.05f * k, 0.2f);
                yield return null;
            }

            // 3. lespedea coboara in pamant intr-un nor de praf
            Spawn(burstPrefab != null ? burstPrefab : dustPrefab, basePoint, 1.4f, 5f);
            if (rumble != null) rumble.ImpulseAt(basePoint, 0.3f, 0.5f, 6f, 40f);
            float nextDust = 0.5f;
            for (float t = 0f; t < sinkTime && slab != null; t += Time.deltaTime)
            {
                float k = t / sinkTime;
                float e = k * k * (3f - 2f * k);
                Vector2 shake = Random.insideUnitCircle * 0.025f * (1f - k);
                slab.SetPositionAndRotation(start + Vector3.down * sinkDepth * e + new Vector3(shake.x, 0f, shake.y), baseRot);
                if (rumble != null) rumble.SetRumble(0.06f * (1f - k), 0.3f);
                if (t > nextDust)
                {
                    nextDust += 0.7f;
                    Spawn(dustPrefab, basePoint, 0.8f, 3f);
                    SpawnDebris(basePoint + Vector3.up * 0.4f, 2, 1.5f, 3f);
                }
                yield return null;
            }
            if (slab != null)
            {
                slab.SetPositionAndRotation(start + Vector3.down * sinkDepth, baseRot);
                slab.gameObject.SetActive(false);
            }
            if (rumble != null) rumble.SetRumble(0f, 0.8f);
            Spawn(dustPrefab, basePoint, 1f, 4f);

            // 4. drumul e liber
            foreach (Collider c in blockers)
                if (c != null) c.enabled = false;
            if (obstacle != null) obstacle.enabled = false;
        }

        void UpdateHint()
        {
            if (screenHint == null) return;
            if (player == null)
            {
                GameObject go = GameObject.FindWithTag("Player");
                if (go == null) return;
                player = go.transform;
            }
            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            ShowHint(offset.magnitude < hintDistance);
        }

        void ShowHint(bool on)
        {
            if (screenHint == null || hintShown == on) return;
            hintShown = on;
            if (on) screenHint.text = closedHint;
            screenHint.gameObject.SetActive(on);
        }

        void SetGlow(float k)
        {
            glow = Mathf.Max(0f, k);
            if (liveGlyph != null) liveGlyph.SetColor(EmissionId, glyphColor * glow);
            if (runeLight != null) runeLight.intensity = lightBase * glow;
        }

        void Spawn(ParticleSystem prefab, Vector3 point, float scale, float life)
        {
            if (prefab == null) return;
            ParticleSystem fx = Instantiate(prefab, point, prefab.transform.rotation);
            fx.transform.localScale = prefab.transform.localScale * scale;
            fx.Play(true);
            Destroy(fx.gameObject, life);
        }

        void SpawnDebris(Vector3 point, int count, float minUp, float maxUp)
        {
            if (debrisPrefabs == null || debrisPrefabs.Length == 0) return;
            Vector3 across = transform.right;
            for (int n = 0; n < count; n++)
            {
                DebrisChunk prefab = debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];
                if (prefab == null) continue;
                Vector3 pos = point + across * Random.Range(-1.8f, 1.8f) + transform.forward * Random.Range(-0.6f, 0.6f);
                Vector3 side = (transform.forward * (Random.value < 0.5f ? -1f : 1f) + across * Random.Range(-0.5f, 0.5f)) * Random.Range(0.8f, 2.2f);
                DebrisChunk chunk = Instantiate(prefab, pos, Random.rotation);
                chunk.Launch(Vector3.up * Random.Range(minUp, maxUp) + side, Random.Range(0.12f, 0.3f));
            }
        }
    }
}
