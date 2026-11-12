using Alpha.Combat;
using UnityEngine;

namespace Alpha.World
{
    // Prezenta lichului: lumina verde slaba la ochi si una magenta la sigiliul de pe spate,
    // cu puls lent si o aprindere scurta la atac / aruncare. Luminile urmaresc oasele, fara umbre.
    // In cutscene-ul cu sabia ochii corpului original clipesc rosu (material schimbat de animatie);
    // citim acelasi slot ca sa clipeasca si lumina ochilor, deci efectul se vede si pe modelul rafinat.
    public class PlayerPresence : MonoBehaviour
    {
        [SerializeField] PlayerCombatState combat;
        [SerializeField] Transform body;
        [SerializeField] Transform head;
        [SerializeField] Transform back;

        [Header("Ochi")]
        [SerializeField] Color eyeColor = new Color(0.45f, 1f, 0.55f);
        [SerializeField] float eyeIntensity = 0.6f;
        [SerializeField] float eyeRange = 1.2f;
        [SerializeField] Vector3 eyeOffset = new Vector3(0f, 0.1f, 0.14f);

        [Header("Sigiliu")]
        [SerializeField] Color sigilColor = new Color(1f, 0.25f, 0.85f);
        [SerializeField] float sigilIntensity = 0.16f;
        [SerializeField] float sigilRange = 0.8f;
        [SerializeField] Vector3 sigilOffset = new Vector3(0f, 0f, -0.2f);

        [Header("Puls si aprindere")]
        [SerializeField] float pulsePeriod = 3.2f;
        [SerializeField, Range(0f, 1f)] float pulseDepth = 0.3f;
        [SerializeField] float flareBoost = 2.2f;
        [SerializeField] float flareTime = 0.45f;

        [Header("Clipirea ochilor din cutscene")]
        [SerializeField] Renderer eyeSource;
        [SerializeField] int eyeSlot = 12;
        [SerializeField] Color eyeFlashColor = new Color(1f, 0.15f, 0.1f);
        [SerializeField] float eyeFlashBoost = 3f;

        Light eyeLight, sigilLight;
        PlayerActions lastAction;
        float flare;
        Material eyeRest;
        PlayerSkin skin;

        void Awake()
        {
            skin = GetComponent<PlayerSkin>();
            eyeLight = MakeLight("PresenceEyes", eyeColor, eyeRange);
            sigilLight = MakeLight("PresenceSigil", sigilColor, sigilRange);
            if (eyeSource != null && eyeSlot < eyeSource.sharedMaterials.Length) eyeRest = eyeSource.sharedMaterials[eyeSlot];
        }

        void OnEnable() => SetLights(true);
        void OnDisable() => SetLights(false);

        void LateUpdate()
        {
            if (head == null || back == null) return;
            Quaternion facing = body != null ? body.rotation : transform.rotation;
            eyeLight.transform.position = head.position + facing * eyeOffset;
            sigilLight.transform.position = back.position + facing * sigilOffset;

            if (combat != null)
            {
                PlayerActions action = combat.currentAction;
                if (action != lastAction && IsStrike(action)) flare = 1f;
                lastAction = action;
            }
            flare = Mathf.MoveTowards(flare, 0f, Time.deltaTime / Mathf.Max(0.01f, flareTime));

            float pulse = 1f - pulseDepth * 0.5f * (1f + Mathf.Sin(Time.time * 2f * Mathf.PI / Mathf.Max(0.1f, pulsePeriod)));
            float boost = 1f + flareBoost * flare * flare;
            bool flash = EyeFlash();
            eyeLight.color = flash ? eyeFlashColor : eyeColor;
            eyeLight.intensity = eyeIntensity * pulse * (flash ? eyeFlashBoost : boost);
            sigilLight.intensity = sigilIntensity * pulse * boost;
            bool shown = BodyVisible();
            eyeLight.enabled = shown;
            sigilLight.enabled = shown;
        }

        static bool IsStrike(PlayerActions action)
        {
            switch (action)
            {
                case PlayerActions.Light:
                case PlayerActions.Light2:
                case PlayerActions.Heavy:
                case PlayerActions.HeavyFall:
                case PlayerActions.Throw:
                case PlayerActions.Charge:
                case PlayerActions.Ability:
                case PlayerActions.LightPunch:
                case PlayerActions.LightPunch2:
                case PlayerActions.LightKick3:
                    return true;
                default:
                    return false;
            }
        }

        bool EyeFlash()
        {
            if (eyeSource == null || eyeRest == null) return false;
            Material[] mats = eyeSource.sharedMaterials;
            return eyeSlot < mats.Length && mats[eyeSlot] != eyeRest;
        }

        // la teleport corpul e ascuns, luminile se sting odata cu el
        bool BodyVisible()
        {
            Renderer active = skin != null ? skin.ActiveBody : eyeSource;
            return active == null || active.enabled;
        }

        Light MakeLight(string lightName, Color color, float range)
        {
            Light light = new GameObject(lightName).AddComponent<Light>();
            light.transform.SetParent(transform, false);
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
            return light;
        }

        void SetLights(bool on)
        {
            if (eyeLight != null) eyeLight.enabled = on;
            if (sigilLight != null) sigilLight.enabled = on;
        }
    }
}
