using System.Reflection;
using Alpha.Interfaces;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Alpha.World
{
    // Vigneta rosie scurta pe marginea ecranului cand lichul ia damage si un puls lent cand viata e mica.
    // Volum global propriu, creat la rulare, cu prioritate peste volumele regiunilor; greutatea lui face efectul.
    // Viata e privata in HittableCharacter: o citim prin reflexie, ca Arena2Respawn. Dupa respawn viata e plina, pulsul dispare singur.
    public class DamageVignette : MonoBehaviour
    {
        [SerializeField] HittableCharacter target;
        [SerializeField] Color color = new Color(0.55f, 0.02f, 0.03f);
        [SerializeField] float intensity = 0.45f;
        [SerializeField, Range(0.01f, 1f)] float smoothness = 0.85f;
        [SerializeField] float priority = 100f;

        [Header("Lovitura")]
        [SerializeField] float flashTime = 0.5f;
        [SerializeField, Range(0f, 1f)] float flashWeight = 0.85f;

        [Header("Viata mica")]
        [SerializeField, Range(0f, 1f)] float lowHealth = 0.3f;
        [SerializeField] float pulsePeriod = 1.6f;
        [SerializeField, Range(0f, 1f)] float pulseWeight = 0.55f;

        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly FieldInfo HealthField = typeof(HittableCharacter).GetField("currentHealth", Private);
        static readonly FieldInfo MaxHealthField = typeof(HittableCharacter).GetField("Max_Health", Private);

        Volume volume;
        VolumeProfile profile;
        float lastHealth = -1f;
        float flash;

        void Awake()
        {
            if (target == null) target = GetComponent<HittableCharacter>();
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "DamageVignette";
            Vignette vignette = profile.Add<Vignette>(true);
            vignette.color.Override(color);
            vignette.intensity.Override(intensity);
            vignette.smoothness.Override(smoothness);
            vignette.rounded.Override(false);

            GameObject go = new GameObject("DamageVignetteVolume");
            go.transform.SetParent(transform, false);
            volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = priority;
            volume.sharedProfile = profile;
            volume.weight = 0f;
        }

        void OnDisable()
        {
            if (volume != null) volume.weight = 0f;
            flash = 0f;
            lastHealth = -1f;
        }

        void OnDestroy()
        {
            if (profile != null) Destroy(profile);
        }

        void Update()
        {
            if (volume == null || target == null || HealthField == null || MaxHealthField == null) return;
            float health = (float)HealthField.GetValue(target);
            float max = Mathf.Max(1f, (float)MaxHealthField.GetValue(target));
            if (lastHealth >= 0f && health < lastHealth - 0.01f) flash = 1f;
            lastHealth = health;
            flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime / Mathf.Max(0.01f, flashTime));

            float share = health / max;
            float low = 0f;
            if (health > 0f && share < lowHealth)
            {
                float depth = 1f - share / lowHealth;
                float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * 2f * Mathf.PI / Mathf.Max(0.1f, pulsePeriod));
                low = pulseWeight * (0.45f + 0.55f * depth) * (0.4f + 0.6f * wave);
            }
            volume.weight = Mathf.Max(flashWeight * flash * flash, low);
        }
    }
}
