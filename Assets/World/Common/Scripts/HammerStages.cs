using System;
using System.Collections;
using System.Collections.Generic;
using Alpha.Combat;
using Alpha.Input;
using UnityEngine;

namespace Alpha.World
{
    // Etapele ciocanului-sabie: 0 = piatra intreaga, 1 = primul strat cioplit, 2 = al doilea strat, 3 = sabia goala.
    // Gaseste instanta SwordHammer a jucatorului oricand apare si ii aplica etapa curenta (etapa tine toata sesiunea).
    // Carve() ruleaza cioplirea: crapaturile se aprind, coaja se rupe in bucati care zboara si apare etapa noua.
    // Cum se castiga fiecare etapa tine de scriptul fiecarei arene.
    public class HammerStages : MonoBehaviour
    {
        [Serializable]
        public class Stage
        {
            public GameObject model;        // coaja etapei, copil al lui Visual (null = pietrele originale sau doar sabia)
            public Vector3 headCenter;      // capul in spatiul lui Visual, inainte de scara lui
            public Vector3 headSize;
            public Vector3 impactSide;      // punctele de impact in spatiul lui Visual
            public Vector3 impactTop;
        }

        public const int MaxStage = 3;
        public static int CurrentStage { get; private set; }
        public static HammerStages Instance { get; private set; }

        [SerializeField] int startStage;
        [SerializeField] Stage[] stages = new Stage[MaxStage + 1];
        [SerializeField] GameObject[] shells = new GameObject[MaxStage];
        [SerializeField] Color[] crackColors = { new Color(0.45f, 1f, 0.35f), new Color(1f, 0.55f, 0.15f), new Color(0.85f, 0.95f, 1f) };
        [SerializeField] Texture crackMask;
        [SerializeField] float crackMaskTiling = 1.6f;
        [SerializeField] float crackGlow = 6f;
        [SerializeField] ParticleSystem burstPrefab;
        [SerializeField] float burstScale = 0.35f;
        [SerializeField] float crackTime = 2.4f;
        [SerializeField] float chunkSpeed = 4.5f;
        [SerializeField] float chunkLift = 2.5f;
        [SerializeField] Vector2 chunkLife = new Vector2(2.6f, 3.6f);
        [SerializeField] float flashIntensity = 40f;
        [SerializeField] float flashRange = 7f;
        [SerializeField] float searchInterval = 0.4f;

        public event Action<int> onCarveStarted;   // etapa tinta
        public event Action<int> onShellBurst;     // momentul in care coaja se rupe
        public event Action<int> onCarved;         // etapa noua e pe ciocan

        public Weapon Hammer => rig != null ? rig.weapon : null;
        public bool IsCarving { get; private set; }
        public float CrackTime => crackTime;

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");
        static bool sessionStarted;

        class Rig
        {
            public Weapon weapon;
            public Transform visual, impact1, impact2, hitbox;
            public Renderer[] stones;
            public BoxCollider visualBox, headBox;
            public Vector3 boxCenter, boxSize, headCenter, headSize, impact1Pos, impact2Pos;
            public GameObject[] shellObjects = new GameObject[MaxStage + 1];
            public int applied = -1;
        }

        Rig rig;
        float searchTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            CurrentStage = 0;
            Instance = null;
            sessionStarted = false;
        }

        void Awake()
        {
            Instance = this;
            if (!sessionStarted)
            {
                sessionStarted = true;
                CurrentStage = Mathf.Clamp(startStage, 0, MaxStage);
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (rig != null && rig.weapon == null) rig = null;
            if (rig == null)
            {
                searchTimer -= Time.deltaTime;
                if (searchTimer > 0f) return;
                searchTimer = searchInterval;
                Weapon found = FindHammer();
                if (found == null) return;
                rig = BuildRig(found);
            }
            if (!IsCarving && rig.applied != CurrentStage) ApplyVisuals(CurrentStage);
        }

        // seteaza etapa direct, fara cioplire (debug / incarcare)
        public void SetStage(int stage)
        {
            CurrentStage = Mathf.Clamp(stage, 0, MaxStage);
            if (rig != null && !IsCarving) ApplyVisuals(CurrentStage);
        }

        public Coroutine Carve(int toStage)
        {
            toStage = Mathf.Clamp(toStage, 0, MaxStage);
            if (IsCarving || toStage <= CurrentStage) return null;
            return StartCoroutine(carve(toStage));
        }

        // centrul capului in lume, pentru camera si efecte
        public Vector3 HeadPosition()
        {
            if (rig == null) return transform.position;
            Stage stage = StageAt(rig.applied < 0 ? CurrentStage : rig.applied);
            return rig.visual.TransformPoint(stage != null && stage.headSize != Vector3.zero ? stage.headCenter : rig.boxCenter);
        }

        IEnumerator carve(int toStage)
        {
            IsCarving = true;
            onCarveStarted?.Invoke(toStage);
            while (CurrentStage < toStage)
            {
                int from = CurrentStage;
                if (rig == null)
                {
                    CurrentStage = from + 1;
                    continue;
                }
                ApplyVisuals(from);
                yield return crack(from);
                if (rig == null)
                {
                    CurrentStage = from + 1;
                    continue;
                }
                Burst(from);
                CurrentStage = from + 1;
                ApplyVisuals(CurrentStage);
                onShellBurst?.Invoke(CurrentStage);
            }
            IsCarving = false;
            onCarved?.Invoke(CurrentStage);
        }

        // crapaturile se aprind pe materialele etapei care pleaca
        IEnumerator crack(int from)
        {
            Renderer[] renderers = OutgoingRenderers(from);
            Color color = crackColors[Mathf.Clamp(from, 0, crackColors.Length - 1)];
            var mats = new List<Material>();
            var bases = new List<Color>();
            var weights = new List<float>();
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                Material[] instances = r.materials;
                for (int i = 0; i < instances.Length; i++)
                {
                    Material m = instances[i];
                    if (!m.HasProperty(EmissionId)) continue;
                    bool hasMap = m.HasProperty(EmissionMapId) && m.GetTexture(EmissionMapId) != null;
                    bool lit = m.IsKeywordEnabled("_EMISSION");
                    // piatra fara harta de emisie primeste masca de crapaturi, altfel doar o tenta slaba
                    if (!hasMap && crackMask != null && m.HasProperty(EmissionMapId))
                    {
                        m.SetTexture(EmissionMapId, crackMask);
                        m.SetTextureScale(EmissionMapId, Vector2.one * crackMaskTiling);
                        hasMap = true;
                        lit = false;
                    }
                    if (!lit) m.SetColor(EmissionId, Color.black);
                    m.EnableKeyword("_EMISSION");
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    mats.Add(m);
                    bases.Add(m.GetColor(EmissionId));
                    weights.Add(hasMap ? 1f : 0.05f);
                }
            }

            Light glow = MakeLight(color, 0f, flashRange * 0.6f);
            for (float t = 0f; t < crackTime; t += Time.deltaTime)
            {
                float k = t / crackTime;
                float pulse = 1f + Mathf.Sin(t * Mathf.Lerp(4f, 22f, k)) * 0.25f * k;
                float glowK = k * k * pulse;
                for (int i = 0; i < mats.Count; i++)
                    if (mats[i] != null) mats[i].SetColor(EmissionId, bases[i] + color * (crackGlow * glowK * weights[i]));
                if (glow != null && rig != null)
                {
                    glow.transform.position = HeadPosition();
                    glow.intensity = flashIntensity * 0.25f * glowK;
                }
                if (rig != null && rig.visual != null)
                {
                    // ciocanul tremura tot mai tare inainte sa crape
                    rig.visual.localPosition = visualRest + UnityEngine.Random.insideUnitSphere * 0.012f * k;
                }
                yield return null;
            }
            if (rig != null && rig.visual != null) rig.visual.localPosition = visualRest;
            if (glow != null) Destroy(glow.gameObject);
        }

        Vector3 visualRest;
        bool visualRestSet;

        void Burst(int from)
        {
            Vector3 head = HeadPosition();
            Color color = crackColors[Mathf.Clamp(from, 0, crackColors.Length - 1)];
            GameObject shell = from < shells.Length ? shells[from] : null;
            Collider[] ignore = IgnoredColliders();
            Vector3 forward = rig.weapon.transform.root != rig.weapon.transform ? rig.weapon.transform.root.forward : Camera.main != null ? -Camera.main.transform.forward : Vector3.forward;

            if (shell != null)
            {
                GameObject set = Instantiate(shell, rig.visual, false);
                set.transform.localPosition = shell.transform.localPosition;
                set.transform.localRotation = shell.transform.localRotation;
                set.transform.localScale = shell.transform.localScale;
                foreach (MeshRenderer piece in set.GetComponentsInChildren<MeshRenderer>())
                {
                    Transform t = piece.transform;
                    t.SetParent(null, true);
                    piece.gameObject.layer = 2;
                    BoxCollider box = piece.gameObject.AddComponent<BoxCollider>();
                    foreach (Collider c in ignore) if (c != null) Physics.IgnoreCollision(box, c);
                    Rigidbody body = piece.gameObject.AddComponent<Rigidbody>();
                    body.mass = 0.6f;
                    body.interpolation = RigidbodyInterpolation.Interpolate;
                    body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    Vector3 away = t.TransformPoint(piece.GetComponent<MeshFilter>().sharedMesh.bounds.center) - head;
                    if (away.sqrMagnitude < 0.0001f) away = UnityEngine.Random.onUnitSphere;
                    away = (away.normalized + forward * 0.35f + UnityEngine.Random.insideUnitSphere * 0.3f).normalized;
                    body.linearVelocity = away * chunkSpeed * UnityEngine.Random.Range(0.7f, 1.3f) + Vector3.up * chunkLift;
                    body.angularVelocity = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(6f, 16f);
                    StartCoroutine(expire(t, UnityEngine.Random.Range(chunkLife.x, chunkLife.y)));
                }
                Destroy(set);
            }

            if (burstPrefab != null)
            {
                ParticleSystem fx = Instantiate(burstPrefab, head, Quaternion.identity);
                fx.transform.localScale = Vector3.one * burstScale;
                fx.Play(true);
                Destroy(fx.gameObject, 4f);
            }
            StartCoroutine(flash(head, color));
        }

        IEnumerator expire(Transform piece, float life)
        {
            yield return new WaitForSeconds(life);
            if (piece == null) yield break;
            Rigidbody body = piece.GetComponent<Rigidbody>();
            if (body != null) body.isKinematic = true;
            Collider col = piece.GetComponent<Collider>();
            if (col != null) col.enabled = false;
            // se scufunda in pamant si se micsoreaza
            Vector3 scale = piece.localScale;
            Vector3 pos = piece.position;
            for (float t = 0f; t < 0.8f && piece != null; t += Time.deltaTime)
            {
                float k = t / 0.8f;
                piece.localScale = scale * (1f - k * k);
                piece.position = pos + Vector3.down * 0.15f * k;
                yield return null;
            }
            if (piece != null) Destroy(piece.gameObject);
        }

        IEnumerator flash(Vector3 point, Color color)
        {
            Light light = MakeLight(color, flashIntensity, flashRange);
            if (light == null) yield break;
            light.transform.position = point;
            for (float t = 0f; t < 0.7f; t += Time.deltaTime)
            {
                float k = t / 0.7f;
                light.intensity = flashIntensity * (1f - k) * (1f - k);
                yield return null;
            }
            Destroy(light.gameObject);
        }

        Light MakeLight(Color color, float intensity, float range)
        {
            Light light = new GameObject("HammerStages_Light").AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            light.transform.position = HeadPosition();
            return light;
        }

        Collider[] IgnoredColliders()
        {
            var list = new List<Collider>();
            if (rig != null) list.AddRange(rig.weapon.GetComponentsInChildren<Collider>(true));
            InputHandler player = rig != null ? rig.weapon.GetComponentInParent<InputHandler>() : null;
            if (player == null) player = FindAnyObjectByType<InputHandler>();
            if (player != null) list.AddRange(player.GetComponentsInChildren<Collider>(true));
            return list.ToArray();
        }

        Renderer[] OutgoingRenderers(int from)
        {
            if (rig == null) return new Renderer[0];
            if (from == 0) return rig.stones;
            GameObject shell = rig.shellObjects[from];
            return shell != null ? shell.GetComponentsInChildren<Renderer>() : new Renderer[0];
        }

        Stage StageAt(int index) => stages != null && index >= 0 && index < stages.Length ? stages[index] : null;

        static Weapon FindHammer()
        {
            // instanta jucatorului: arma care are pietrele ciocanului sub Visual
            Weapon best = null;
            foreach (Weapon w in FindObjectsByType<Weapon>(FindObjectsInactive.Exclude))
            {
                if (w.transform.Find("Visual/Stone_01") == null) continue;
                if (best == null || w.GetComponentInParent<WeaponSocket>() != null) best = w;
            }
            return best;
        }

        Rig BuildRig(Weapon weapon)
        {
            Rig r = new Rig { weapon = weapon, visual = weapon.transform.Find("Visual") };
            var stones = new List<Renderer>();
            for (int i = 1; i <= 3; i++)
            {
                Transform s = r.visual.Find("Stone_0" + i);
                if (s != null && s.TryGetComponent(out Renderer sr)) stones.Add(sr);
            }
            r.stones = stones.ToArray();
            r.visualBox = r.visual.GetComponent<BoxCollider>();
            if (r.visualBox != null)
            {
                r.boxCenter = r.visualBox.center;
                r.boxSize = r.visualBox.size;
            }
            r.headBox = weapon.headHitbox as BoxCollider;
            if (r.headBox != null)
            {
                r.hitbox = r.headBox.transform;
                r.headCenter = r.headBox.center;
                r.headSize = r.headBox.size;
            }
            r.impact1 = weapon.ImpactPoint1;
            r.impact2 = weapon.ImpactPoint2;
            if (r.impact1 != null) r.impact1Pos = r.impact1.localPosition;
            if (r.impact2 != null) r.impact2Pos = r.impact2.localPosition;
            visualRest = r.visual.localPosition;
            visualRestSet = true;
            return r;
        }

        void ApplyVisuals(int stage)
        {
            if (rig == null || rig.visual == null) return;
            rig.applied = stage;
            foreach (Renderer s in rig.stones) if (s != null) s.enabled = stage == 0;

            for (int i = 1; i <= MaxStage; i++)
            {
                Stage def = StageAt(i);
                bool on = i == stage && def != null && def.model != null;
                if (on && rig.shellObjects[i] == null)
                {
                    GameObject shell = Instantiate(def.model, rig.visual, false);
                    shell.name = "HammerStage_" + i;
                    shell.transform.localPosition = def.model.transform.localPosition;
                    shell.transform.localRotation = def.model.transform.localRotation;
                    shell.transform.localScale = def.model.transform.localScale;
                    rig.shellObjects[i] = shell;
                }
                if (rig.shellObjects[i] != null) rig.shellObjects[i].SetActive(on);
            }
            ApplyColliders(stage);
        }

        // hitbox-ul si colliderul de aruncare urmaresc capul etapei; punctele de impact la fel
        void ApplyColliders(int stage)
        {
            Stage def = StageAt(stage);
            bool original = stage == 0 || def == null || def.headSize == Vector3.zero;
            if (rig.visualBox != null)
            {
                rig.visualBox.center = original ? rig.boxCenter : def.headCenter;
                rig.visualBox.size = original ? rig.boxSize : def.headSize;
            }
            if (rig.headBox != null)
            {
                if (original)
                {
                    rig.headBox.center = rig.headCenter;
                    rig.headBox.size = rig.headSize;
                }
                else
                {
                    Vector3 scale = rig.weapon.HitboxOriginalSize;
                    if (scale.x == 0f || scale.y == 0f || scale.z == 0f) scale = Vector3.one;
                    Vector3 center = Quaternion.Inverse(rig.hitbox.localRotation) * (VisualToRoot(def.headCenter) - rig.hitbox.localPosition);
                    Vector3 size = Vector3.Scale(def.headSize, Abs(rig.visual.localScale));
                    rig.headBox.center = Divide(center, scale);
                    rig.headBox.size = Divide(size, scale);
                }
            }
            if (rig.impact1 != null) rig.impact1.localPosition = original || def.impactSide == Vector3.zero ? rig.impact1Pos : VisualToRoot(def.impactSide);
            if (rig.impact2 != null) rig.impact2.localPosition = original || def.impactTop == Vector3.zero ? rig.impact2Pos : VisualToRoot(def.impactTop);
        }

        Vector3 VisualToRoot(Vector3 p) => (visualRestSet ? visualRest : rig.visual.localPosition) + rig.visual.localRotation * Vector3.Scale(rig.visual.localScale, p);

        static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        static Vector3 Divide(Vector3 a, Vector3 b) => new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
    }
}
