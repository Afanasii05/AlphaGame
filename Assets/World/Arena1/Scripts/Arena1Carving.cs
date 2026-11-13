using System.Collections;
using Alpha.Input;
using Alpha.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Alpha.World
{
    // Cinematica de cioplire: inima intra in ciocan, camera coboara in 3/4 jos si se apropie de
    // jucator cu tot ciocanul in cadru, piatra se crapa verde, tremura, explodeaza si ramane prima etapa.
    // O lumina verde de umplere si una de contur tin jucatorul lizibil, apoi se sting la revenire.
    // Ruleaza dupa CameraController si inainte de CameraShake / CameraRumble, ca BossIntro.
    [DefaultExecutionOrder(50)]
    public class Arena1Carving : MonoBehaviour
    {
        [SerializeField] BossTitleCard titleCard;
        [SerializeField] RectTransform[] letterbox;
        [SerializeField] Image flash;
        [SerializeField] ParticleSystem dustPrefab;
        [SerializeField] int targetStage = 1;

        [Header("Ritm")]
        [SerializeField] float blendIn = 0.8f;
        [SerializeField] float absorbTime = 0.7f;
        [SerializeField] float titleDelay = 0.45f;
        [SerializeField] float blendOut = 1.1f;
        [SerializeField] float flashTime = 0.6f;

        [Header("Cadru")]
        [SerializeField] float startDistance = 4.5f;
        [SerializeField] float endDistance = 3f;
        [SerializeField] float burstDistance = 3.6f;
        [SerializeField] float cameraHeight = 0.6f;     // fata de talpi
        [SerializeField] float bodyHeight = 1.2f;       // fata de talpi
        [SerializeField] float headBias = 0.3f;
        [SerializeField] float sideAngle = -42f;        // din fata-stanga, ciocanul (mana dreapta) ramane langa corp
        [SerializeField] float fovFrom = 52f;
        [SerializeField] float fovTo = 48f;
        [SerializeField] float maxRumble = 0.14f;

        [Header("Lumina")]
        [SerializeField] Color fillColor = new Color(0.55f, 1f, 0.68f);
        [SerializeField] Color rimColor = new Color(0.78f, 1f, 0.86f);
        [SerializeField] float fillIntensity = 2.2f;
        [SerializeField] float rimIntensity = 3.2f;
        [SerializeField] float lightRange = 7f;
        [SerializeField] float crackBoost = 1.5f;

        const int BlockedLayers = (1 << 2) | (1 << 6) | (1 << 7) | (1 << 8);

        Camera cam;
        CameraController controller;
        Transform player;
        Quaternion gameRotation;
        Vector3 focus, shotDir;
        Light fillLight, rimLight;
        float time, weight, distance, fov, baseFov, glow, feetOffset;
        bool burst;

        public bool IsPlaying { get; private set; }

        void Awake()
        {
            SetLetterbox(0f);
            SetFlash(0f);
        }

        public IEnumerator Play(GolemHeart heart, InputHandler input)
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
            // pivotul jucatorului e in centrul capsulei, cadrul se masoara de la talpi
            Collider body = player != null ? player.GetComponent<Collider>() : null;
            feetOffset = body != null ? body.bounds.min.y - player.position.y : 0f;
            Vector3 head = stages != null && stages.Hammer != null ? stages.HeadPosition() : (player != null ? player.position + Vector3.up * 1.2f : Vector3.zero);
            focus = FocusPoint(head);

            // camera vine din fata-lateral, jos, privind in sus spre cap
            Vector3 forward = player != null ? Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized : Vector3.forward;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            shotDir = Quaternion.Euler(0f, sideAngle, 0f) * forward;

            bool cinematic = cam != null && controller != null;
            if (cinematic)
            {
                baseFov = cam.fieldOfView;
                gameRotation = cam.transform.rotation;
                distance = startDistance;
                fov = fovFrom;
                time = 0f;
                weight = 0f;
                burst = false;
                glow = 0f;
                CreateLights();
                IsPlaying = true;
            }

            void HandleBurst(int stage) => burst = true;
            if (stages != null) stages.onShellBurst += HandleBurst;

            // 1. inima e trasa in cap
            float wait = Mathf.Max(0f, blendIn - absorbTime);
            yield return new WaitForSeconds(wait * 0.5f);
            if (heart != null) yield return heart.Absorb(head, absorbTime);

            // 2. crapaturi si cutremur
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
                if (stages != null) stages.SetStage(targetStage);
                burst = true;
            }

            // 3. explozia cojii
            if (rumble != null)
            {
                rumble.SetRumble(0f, 0.6f);
                rumble.Impulse(0.4f, 0.5f);
            }
            CameraShake shake = cam != null ? cam.GetComponent<CameraShake>() : null;
            if (shake != null) shake.Shake(0.3f, 0.4f);
            if (player != null && dustPrefab != null)
            {
                ParticleSystem fx = Instantiate(dustPrefab, player.position, Quaternion.identity);
                fx.Play(true);
                Destroy(fx.gameObject, 4f);
            }
            StartCoroutine(flashRoutine());
            if (stages != null) stages.onShellBurst -= HandleBurst;
            yield return carve;

            // 4. titlul
            yield return new WaitForSeconds(titleDelay);
            float hold = 0f;
            if (titleCard != null)
            {
                titleCard.Show();
                hold = titleCard.Duration - blendOut * 0.6f;
            }
            yield return new WaitForSeconds(Mathf.Max(1.2f, hold));

            // 5. revenire
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

        void LateUpdate()
        {
            if (!IsPlaying) return;
            time += Time.deltaTime;
            if (time < blendIn) weight = Smooth(time / blendIn);
            Transform t = cam.transform;

            // camera de joc, asa cum a pus-o CameraController in acest cadru
            float gameDistance = controller.distance;
            Vector3 pivot = t.position + t.forward * gameDistance;
            Vector3 gamePos = pivot - gameRotation * Vector3.forward * gameDistance;

            HammerStages stages = HammerStages.Instance;
            if (stages != null && stages.Hammer != null) focus = Vector3.Lerp(focus, FocusPoint(stages.HeadPosition()), 1f - Mathf.Exp(-8f * Time.deltaTime));
            if (burst) glow = Mathf.MoveTowards(glow, 0f, Time.deltaTime * 0.8f);

            float goal = burst ? burstDistance : endDistance;
            float rate = burst ? 6f : 0.9f;
            distance = Mathf.Lerp(distance, goal, 1f - Mathf.Exp(-rate * Time.deltaTime));
            fov = Mathf.Lerp(fov, burst ? (fovFrom + fovTo) * 0.5f : fovTo, 1f - Mathf.Exp(-(burst ? 5f : 0.8f) * Time.deltaTime));

            Vector3 shotPos = ShotPosition();
            Quaternion shotRot = Quaternion.LookRotation(focus - shotPos);
            t.SetPositionAndRotation(Vector3.Lerp(gamePos, shotPos, weight), Quaternion.Slerp(gameRotation, shotRot, weight));
            cam.fieldOfView = Mathf.Lerp(baseFov, fov, weight);
            SetLetterbox(weight);
            UpdateLights();
        }

        // tinta camerei: intre pieptul jucatorului si capul ciocanului, ca sa intre amandoi in cadru
        Vector3 FocusPoint(Vector3 head)
        {
            if (player == null) return head;
            return Vector3.Lerp(Feet() + Vector3.up * bodyHeight, head, headBias);
        }

        Vector3 Feet()
        {
            return player.position + Vector3.up * feetOffset;
        }

        Vector3 ShotPosition()
        {
            // 3/4 din fata, aproape de sol, privind in sus spre jucator
            Vector3 anchor = player != null ? Feet() : focus - Vector3.up * bodyHeight;
            Vector3 wanted = anchor + shotDir * distance + Vector3.up * cameraHeight;
            Vector3 dir = wanted - focus;
            float length = dir.magnitude;
            dir = length > 0.01f ? dir / length : shotDir;
            Vector3 pos = wanted;
            // nu intram in pereti sau in stanci
            if (Physics.SphereCast(focus, 0.2f, dir, out RaycastHit hit, length, ~BlockedLayers, QueryTriggerInteraction.Ignore))
                pos = focus + dir * Mathf.Max(0.8f, hit.distance - 0.1f);
            if (Physics.Raycast(pos + Vector3.up * 3f, Vector3.down, out RaycastHit ground, 6f, ~BlockedLayers, QueryTriggerInteraction.Ignore))
                pos.y = Mathf.Max(pos.y, ground.point.y + 0.35f);
            return pos;
        }

        void CreateLights()
        {
            fillLight = MakeLight("CarvingFill", fillColor);
            rimLight = MakeLight("CarvingRim", rimColor);
            UpdateLights();
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

        // umplere dinspre camera, contur din spate-sus; intensitatea urmeaza cadrul si stralucirea pietrei
        void UpdateLights()
        {
            if (player == null) return;
            Vector3 side = Quaternion.Euler(0f, -45f, 0f) * shotDir;
            float level = weight * (1f + crackBoost * glow);
            if (fillLight != null)
            {
                fillLight.transform.position = Feet() + side * 1.8f + Vector3.up * 1.6f;
                fillLight.intensity = fillIntensity * level;
            }
            if (rimLight != null)
            {
                rimLight.transform.position = Feet() - shotDir * 1.6f + Vector3.up * 2.6f;
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

        static float Smooth(float k)
        {
            k = Mathf.Clamp01(k);
            return k * k * (3f - 2f * k);
        }
    }
}
