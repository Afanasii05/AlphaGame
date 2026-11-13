using System.Collections;
using System.Collections.Generic;
using Alpha.Input;
using Alpha.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Alpha.World
{
    // Cioplirea a doua: jucatorul sta in fata cavalerului ingenuncheat, camera din lateral ii prinde pe amandoi.
    // Cavalerul ridica fruntea, isi coboara lancea spre ciocan si ii preda juramantul printr-un fir de lumina;
    // ciocanul se crapa, a doua coaja cade, iar cavalerul ramane o statuie de piatra, cu capul plecat.
    // Ruleaza dupa CameraController si inainte de CameraShake / CameraRumble, ca Arena1Carving.
    [DefaultExecutionOrder(50)]
    public class Arena2Carving : MonoBehaviour
    {
        [SerializeField] Transform knight;
        [SerializeField] Animator knightAnimator;
        [SerializeField] Transform lanceSocket;
        [SerializeField] Transform lanceTip;
        [SerializeField] BossTitleCard titleCard;
        [SerializeField] RectTransform[] letterbox;
        [SerializeField] Image flash;
        [SerializeField] ParticleSystem dustPrefab;
        [SerializeField] Arena2Screen screen;
        [SerializeField] TrailRenderer thread;
        [SerializeField] Light knightGlow;
        [SerializeField] ParticleSystem knightEmbers;
        [SerializeField] Texture statueTexture;
        [SerializeField] int targetStage = 2;

        [Header("Ritm")]
        [SerializeField] float fadeOut = 0.7f;
        [SerializeField] float fadeIn = 0.8f;
        [SerializeField] float liftTime = 1.4f;
        [SerializeField] float lowerTime = 1.4f;
        [SerializeField] float threadTime = 1.2f;
        [SerializeField] float statueTime = 2.6f;
        [SerializeField] float titleDelay = 0.45f;
        [SerializeField] float blendOut = 1.1f;
        [SerializeField] float flashTime = 0.6f;

        [Header("Cadru")]
        [SerializeField] float standDistance = 3.4f;
        [SerializeField] float startDistance = 4.8f;
        [SerializeField] float endDistance = 4.2f;
        [SerializeField] float shotHeight = 0.35f;
        [SerializeField] float playerPitch = 12f;
        [SerializeField] float fov = 45f;
        [SerializeField] float maxRumble = 0.14f;

        [Header("Poza")]
        [SerializeField] float headLift = 26f;
        [SerializeField] float headSlump = 22f;
        [SerializeField] float lanceReach = 0.85f;

        [Header("Lumina")]
        [SerializeField] Color fillColor = new Color(1f, 0.72f, 0.45f);
        [SerializeField] Color rimColor = new Color(1f, 0.45f, 0.3f);
        [SerializeField] float fillIntensity = 2.6f;
        [SerializeField] float rimIntensity = 4f;
        [SerializeField] float lightRange = 8f;
        [SerializeField] Color stoneColor = new Color(0.56f, 0.54f, 0.5f);

        const int BlockedLayers = (1 << 2) | (1 << 6) | (1 << 7) | (1 << 8);
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        static readonly int MetallicId = Shader.PropertyToID("_Metallic");

        Camera cam;
        CameraController controller;
        Transform player, head, neck, chest;
        Quaternion gameRotation, socketBase;
        Vector3 shotSide;
        Light fillLight, rimLight;
        float weight, distance, baseFov, lift, lance, glow;
        bool posing, burst;

        public bool IsPlaying { get; private set; }

        void Awake()
        {
            SetLetterbox(0f);
            SetFlash(0f);
            if (thread != null) thread.gameObject.SetActive(false);
            if (knightAnimator != null && knightAnimator.isHuman)
            {
                head = knightAnimator.GetBoneTransform(HumanBodyBones.Head);
                neck = knightAnimator.GetBoneTransform(HumanBodyBones.Neck);
                chest = knightAnimator.GetBoneTransform(HumanBodyBones.Chest);
            }
            if (lanceSocket != null) socketBase = lanceSocket.localRotation;
        }

        public IEnumerator Play(InputHandler input)
        {
            cam = Camera.main;
            controller = cam != null ? cam.GetComponent<CameraController>() : null;
            HammerStages stages = HammerStages.Instance;
            if (input != null) input.disableInputs();
            player = input != null ? input.transform : null;
            if (player == null)
            {
                GameObject go = GameObject.FindWithTag("Player");
                if (go != null) player = go.transform;
            }

            // 1. ecran negru, jucatorul e asezat in fata cavalerului
            if (screen != null) yield return screen.FadeTo(1f, fadeOut);
            PlacePlayer();
            bool cinematic = cam != null && controller != null && player != null && knight != null;
            if (cinematic)
            {
                baseFov = cam.fieldOfView;
                Vector3 toKnight = Flat(knight.position - player.position);
                controller.xAngle = Quaternion.LookRotation(toKnight).eulerAngles.y;
                controller.yAngle = playerPitch;
                gameRotation = Quaternion.Euler(playerPitch, controller.xAngle, 0f);
                shotSide = PickSide(toKnight.normalized);
                distance = startDistance;
                weight = 1f;
                burst = false;
                glow = 0f;
                CreateLights();
                IsPlaying = true;
            }
            posing = true;
            yield return null;
            if (screen != null) yield return screen.FadeTo(0f, fadeIn);

            // 2. cavalerul ridica fruntea, apoi lancea coboara spre ciocan
            yield return Animate(liftTime, k => lift = k);
            yield return new WaitForSeconds(0.3f);
            yield return Animate(lowerTime, k => lance = k);

            // 3. firul juramantului: din piept, pe lance, in ciocan
            yield return flow();

            // 4. a doua coaja
            void HandleBurst(int stage) => burst = true;
            if (stages != null) stages.onShellBurst += HandleBurst;
            Coroutine carve = stages != null ? stages.Carve(targetStage) : null;
            float crackTime = stages != null ? stages.CrackTime : 2f;
            CameraRumble rumble = CameraRumble.Main;
            if (carve != null)
            {
                for (float t = 0f; !burst && t < crackTime + 2f; t += Time.deltaTime)
                {
                    float k = Mathf.Clamp01(t / crackTime);
                    if (rumble != null) rumble.SetRumble(maxRumble * k * k, 0.1f);
                    glow = k * k;
                    yield return null;
                }
            }
            else
            {
                if (stages != null && HammerStages.CurrentStage < targetStage) stages.SetStage(targetStage);
                burst = true;
            }
            if (rumble != null)
            {
                rumble.SetRumble(0f, 0.6f);
                rumble.Impulse(0.4f, 0.5f);
            }
            CameraShake shake = cam != null ? cam.GetComponent<CameraShake>() : null;
            if (shake != null) shake.Shake(0.3f, 0.4f);
            Dust(stages != null && stages.Hammer != null ? stages.HeadPosition() : player != null ? player.position : transform.position);
            StartCoroutine(flashRoutine());
            if (stages != null) stages.onShellBurst -= HandleBurst;
            yield return carve;

            // 5. titlul, iar cavalerul se preface in piatra
            yield return new WaitForSeconds(titleDelay);
            float hold = 0f;
            if (titleCard != null)
            {
                titleCard.Show();
                hold = titleCard.Duration - blendOut * 0.6f;
            }
            yield return petrify();
            yield return new WaitForSeconds(Mathf.Max(0.8f, hold - statueTime));

            // 6. revenire
            if (cinematic)
            {
                for (float t = 0f; t < blendOut; t += Time.deltaTime)
                {
                    weight = 1f - Smooth(t / blendOut);
                    yield return null;
                }
                Finish();
            }
            if (input != null) input.enableInputs();
        }

        IEnumerator flow()
        {
            if (thread == null || chest == null || lanceTip == null) yield break;
            Transform t = thread.transform;
            t.position = chest.position;
            thread.gameObject.SetActive(true);
            thread.Clear();
            float from = knightGlow != null ? knightGlow.intensity : 0f;
            for (float time = 0f; time < threadTime; time += Time.deltaTime)
            {
                float k = Smooth(time / threadTime);
                Vector3 end = HammerHead();
                Vector3 a = Vector3.Lerp(chest.position, lanceTip.position, k);
                Vector3 b = Vector3.Lerp(lanceTip.position, end, k);
                t.position = Vector3.Lerp(a, b, k);
                if (knightGlow != null) knightGlow.intensity = from * (1f - k);
                yield return null;
            }
            t.position = HammerHead();
            if (knightGlow != null) knightGlow.enabled = false;
            yield return new WaitForSeconds(0.25f);
            StartCoroutine(hideThread());
        }

        IEnumerator hideThread()
        {
            yield return new WaitForSeconds(thread.time + 0.2f);
            thread.gameObject.SetActive(false);
        }

        // armura se stinge in piatra: culoarea, luciul si metalul coboara, apoi sub un nor de praf ramane piatra
        IEnumerator petrify()
        {
            if (knight == null) yield break;
            if (knightEmbers != null) knightEmbers.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            var mats = new List<Material>();
            var colors = new List<Color>();
            var smooth = new List<float>();
            var metal = new List<float>();
            foreach (Renderer r in knight.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer) continue;
                r.SetPropertyBlock(null);
                foreach (Material m in r.materials)
                {
                    if (!m.HasProperty(BaseColorId)) continue;
                    mats.Add(m);
                    colors.Add(m.GetColor(BaseColorId));
                    smooth.Add(m.HasProperty(SmoothnessId) ? m.GetFloat(SmoothnessId) : 0.5f);
                    metal.Add(m.HasProperty(MetallicId) ? m.GetFloat(MetallicId) : 0f);
                }
            }
            bool swapped = false;
            for (float t = 0f; t < statueTime; t += Time.deltaTime)
            {
                float k = Smooth(t / statueTime);
                lift = Mathf.Lerp(1f, -headSlump / Mathf.Max(1f, headLift), k);
                lance = 1f - k;
                for (int i = 0; i < mats.Count; i++)
                {
                    mats[i].SetColor(BaseColorId, Color.Lerp(colors[i], stoneColor, k));
                    if (mats[i].HasProperty(SmoothnessId)) mats[i].SetFloat(SmoothnessId, Mathf.Lerp(smooth[i], 0.08f, k));
                    if (mats[i].HasProperty(MetallicId)) mats[i].SetFloat(MetallicId, Mathf.Lerp(metal[i], 0f, k));
                }
                if (!swapped && k > 0.55f)
                {
                    // sub praf: dispare harta de metal si apare textura de piatra
                    swapped = true;
                    Dust(chest != null ? chest.position : knight.position);
                    foreach (Material m in mats)
                    {
                        m.DisableKeyword("_METALLICSPECGLOSSMAP");
                        if (statueTexture != null && m.HasProperty(BaseMapId)) m.SetTexture(BaseMapId, statueTexture);
                    }
                }
                yield return null;
            }
            // ultima poza ramane inghetata
            yield return null;
            posing = false;
            if (knightAnimator != null) knightAnimator.enabled = false;
        }

        void LateUpdate()
        {
            if (posing) Pose();
            if (!IsPlaying) return;
            Transform t = cam.transform;

            float gameDistance = controller.distance;
            Vector3 pivot = t.position + t.forward * gameDistance;
            Vector3 gamePos = pivot - gameRotation * Vector3.forward * gameDistance;

            if (burst) glow = Mathf.MoveTowards(glow, 0f, Time.deltaTime * 0.8f);
            distance = Mathf.Lerp(distance, endDistance, 1f - Mathf.Exp(-0.5f * Time.deltaTime));

            Vector3 focus = Focus();
            Vector3 shotPos = ShotPosition(focus);
            Quaternion shotRot = Quaternion.LookRotation(focus - shotPos);
            t.SetPositionAndRotation(Vector3.Lerp(gamePos, shotPos, weight), Quaternion.Slerp(gameRotation, shotRot, weight));
            cam.fieldOfView = Mathf.Lerp(baseFov, fov, weight);
            SetLetterbox(weight);
            UpdateLights(focus);
        }

        // capul si lancea se adauga peste animatia de ingenunchere
        void Pose()
        {
            if (knight == null) return;
            Vector3 right = knight.right;
            float angle = -headLift * lift;
            if (chest != null) chest.rotation = Quaternion.AngleAxis(angle * 0.25f, right) * chest.rotation;
            if (neck != null) neck.rotation = Quaternion.AngleAxis(angle * 0.35f, right) * neck.rotation;
            if (head != null) head.rotation = Quaternion.AngleAxis(angle * 0.5f, right) * head.rotation;
            if (lanceSocket == null) return;
            lanceSocket.localRotation = socketBase;
            if (lance <= 0f || lanceTip == null) return;
            Vector3 from = lanceTip.position - lanceSocket.position;
            Vector3 to = HammerHead() - lanceSocket.position;
            if (from.sqrMagnitude < 0.0001f || to.sqrMagnitude < 0.0001f) return;
            Quaternion bend = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(from, to), lance * lanceReach);
            lanceSocket.rotation = bend * lanceSocket.rotation;
        }

        // jucatorul la cativa metri in fata cavalerului, cu fata la el; cautam un loc liber si cu podea
        void PlacePlayer()
        {
            if (player == null || knight == null) return;
            Collider body = player.GetComponent<Collider>();
            float feet = body != null ? body.bounds.min.y - player.position.y : 0f;
            float radius = body is CapsuleCollider capsule ? capsule.radius : 0.35f;
            Vector3 forward = Flat(knight.forward).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.back;
            float[] angles = { 0f, 30f, -30f, 60f, -60f, 90f, -90f, 180f };
            foreach (float a in angles)
            {
                Vector3 spot = knight.position + Quaternion.Euler(0f, a, 0f) * forward * standDistance;
                if (!Physics.Raycast(spot + Vector3.up * 3f, Vector3.down, out RaycastHit ground, 6f, ~BlockedLayers, QueryTriggerInteraction.Ignore)) continue;
                Vector3 bottom = ground.point + Vector3.up * (radius + 0.05f);
                if (Physics.CheckCapsule(bottom, bottom + Vector3.up * 1.2f, radius, ~BlockedLayers, QueryTriggerInteraction.Ignore)) continue;
                Vector3 pos = ground.point - Vector3.up * feet + Vector3.up * 0.02f;
                Quaternion rot = Quaternion.LookRotation(Flat(knight.position - pos));
                Rigidbody rb = player.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    if (!rb.isKinematic)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    rb.position = pos;
                    rb.rotation = rot;
                }
                player.SetPositionAndRotation(pos, rot);
                Physics.SyncTransforms();
                return;
            }
        }

        Vector3 PlayerChest()
        {
            Collider body = player.GetComponent<Collider>();
            float feet = body != null ? body.bounds.min.y : player.position.y;
            return new Vector3(player.position.x, feet + 1.25f, player.position.z);
        }

        // cadrul: intre pieptul cavalerului si al jucatorului, putin spre ciocan
        Vector3 Focus()
        {
            Vector3 knightChest = chest != null ? chest.position : knight.position + Vector3.up * 1.2f;
            return Vector3.Lerp(knightChest, PlayerChest(), 0.5f) + Vector3.down * 0.1f;
        }

        Vector3 ShotPosition(Vector3 focus)
        {
            Vector3 wanted = focus + shotSide * distance + Vector3.up * shotHeight;
            Vector3 dir = wanted - focus;
            float length = dir.magnitude;
            dir = length > 0.01f ? dir / length : shotSide;
            Vector3 pos = wanted;
            if (Physics.SphereCast(focus, 0.2f, dir, out RaycastHit hit, length, ~BlockedLayers, QueryTriggerInteraction.Ignore))
                pos = focus + dir * Mathf.Max(1.2f, hit.distance - 0.1f);
            if (Physics.Raycast(pos + Vector3.up * 3f, Vector3.down, out RaycastHit ground, 6f, ~BlockedLayers, QueryTriggerInteraction.Ignore))
                pos.y = Mathf.Max(pos.y, ground.point.y + 0.4f);
            return pos;
        }

        // partea din care camera are cel mai mult loc liber
        Vector3 PickSide(Vector3 toKnight)
        {
            Vector3 right = Vector3.Cross(Vector3.up, toKnight).normalized;
            Vector3 focus = Focus();
            float Room(Vector3 d) => Physics.SphereCast(focus, 0.2f, d, out RaycastHit hit, startDistance + 1f, ~BlockedLayers, QueryTriggerInteraction.Ignore) ? hit.distance : startDistance + 1f;
            return Room(right) >= Room(-right) ? right : -right;
        }

        Vector3 HammerHead()
        {
            HammerStages stages = HammerStages.Instance;
            if (stages != null && stages.Hammer != null) return stages.HeadPosition();
            return player != null ? PlayerChest() : transform.position;
        }

        void Dust(Vector3 at)
        {
            if (dustPrefab == null) return;
            ParticleSystem fx = Instantiate(dustPrefab, at, Quaternion.identity);
            fx.Play(true);
            Destroy(fx.gameObject, 4f);
        }

        IEnumerator Animate(float duration, System.Action<float> apply)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                apply(Smooth(t / duration));
                yield return null;
            }
            apply(1f);
        }

        void CreateLights()
        {
            fillLight = MakeLight("CarvingFill", fillColor);
            rimLight = MakeLight("CarvingRim", rimColor);
        }

        Light MakeLight(string lightName, Color color)
        {
            Light light = new GameObject(lightName).AddComponent<Light>();
            light.transform.SetParent(transform, false);
            light.type = LightType.Point;
            light.color = color;
            light.range = lightRange;
            light.shadows = LightShadows.None;
            light.intensity = 0f;
            return light;
        }

        // umplere dinspre camera, contur din spatele cavalerului
        void UpdateLights(Vector3 focus)
        {
            float level = weight * (1f + 1.2f * glow);
            if (fillLight != null)
            {
                fillLight.transform.position = focus + shotSide * 2f + Vector3.up * 1.2f;
                fillLight.intensity = fillIntensity * level;
            }
            if (rimLight != null && knight != null)
            {
                Vector3 back = Flat(knight.position - player.position).normalized;
                rimLight.transform.position = knight.position + back * 1.8f + Vector3.up * 2.8f;
                rimLight.intensity = rimIntensity * level;
            }
        }

        void DestroyLights()
        {
            if (fillLight != null) Destroy(fillLight.gameObject);
            if (rimLight != null) Destroy(rimLight.gameObject);
            fillLight = null;
            rimLight = null;
        }

        void OnDisable()
        {
            DestroyLights();
        }

        void Finish()
        {
            IsPlaying = false;
            DestroyLights();
            Vector3 euler = gameRotation.eulerAngles;
            controller.xAngle = euler.y;
            controller.yAngle = Mathf.DeltaAngle(0f, euler.x);
            cam.fieldOfView = baseFov;
            SetLetterbox(0f);
        }

        IEnumerator flashRoutine()
        {
            for (float t = 0f; t < flashTime; t += Time.deltaTime)
            {
                float k = t / flashTime;
                SetFlash(k < 0.12f ? k / 0.12f : 1f - Smooth((k - 0.12f) / 0.88f));
                yield return null;
            }
            SetFlash(0f);
        }

        void SetFlash(float amount)
        {
            if (flash == null) return;
            Color c = flash.color;
            c.a = amount * 0.85f;
            flash.color = c;
            flash.enabled = amount > 0.001f;
        }

        void SetLetterbox(float amount)
        {
            if (letterbox == null) return;
            foreach (RectTransform bar in letterbox)
                if (bar != null) bar.localScale = new Vector3(1f, amount, 1f);
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        static float Smooth(float k)
        {
            k = Mathf.Clamp01(k);
            return k * k * (3f - 2f * k);
        }
    }
}
