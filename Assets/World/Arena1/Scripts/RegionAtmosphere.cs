using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Alpha.World
{
    // Atmosfera locala a unei regiuni: greutatea volumului de post-process, ceata, ambientul si cerul
    // se amesteca dupa distanta camerei fata de o cutie. Fara collidere, ca sa nu atinga trigger-ele de lovire.
    // Valorile globale se salveaza cand porneste prima regiune si se pun inapoi cand se opreste ultima.
    public class RegionAtmosphere : MonoBehaviour
    {
        static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        static readonly int TintId = Shader.PropertyToID("_SkyTint");
        static readonly int ThicknessId = Shader.PropertyToID("_AtmosphereThickness");

        [SerializeField] Volume volume;
        [SerializeField] Vector3 size = new Vector3(60f, 40f, 60f);
        [SerializeField] float blendDistance = 15f;
        [SerializeField] int priority;

        [Header("Ceata si ambient")]
        [SerializeField] bool overrideFog = true;
        [SerializeField] Color fogColor = new Color(0.4f, 0.42f, 0.46f);
        [SerializeField] float fogDensity = 0.014f;
        [SerializeField] float ambientIntensity = 0.85f;

        [Header("Cer")]
        [SerializeField] bool overrideSky = true;
        [SerializeField] float skyExposure = 0.75f;
        [SerializeField] Color skyTint = new Color(0.46f, 0.46f, 0.52f);
        [SerializeField] float skyThickness = 1.8f;

        static readonly List<RegionAtmosphere> regions = new List<RegionAtmosphere>();
        static int lastFrame = -1;
        static Color baseFogColor, baseTint;
        static float baseFogDensity, baseAmbient, baseExposure, baseThickness;
        static Material baseSky, skyInstance;

        public float Weight { get; private set; }

        void OnEnable()
        {
            if (regions.Count == 0) Capture();
            regions.Add(this);
            regions.Sort((a, b) => a.priority.CompareTo(b.priority));
            if (volume != null) volume.weight = 0f;
        }

        void OnDisable()
        {
            regions.Remove(this);
            if (volume != null) volume.weight = 0f;
            if (regions.Count == 0) Restore();
        }

        void LateUpdate()
        {
            // prima regiune din frame le calculeaza pe toate, ca amestecul sa respecte prioritatea
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 eye = cam.transform.position;

            Color fog = baseFogColor;
            float density = baseFogDensity, ambient = baseAmbient, exposure = baseExposure, thickness = baseThickness;
            Color tint = baseTint;
            foreach (RegionAtmosphere region in regions)
            {
                float w = region.WeightAt(eye);
                region.Weight = w;
                if (region.volume != null) region.volume.weight = w;
                if (w <= 0f) continue;
                if (region.overrideFog)
                {
                    fog = Color.Lerp(fog, region.fogColor, w);
                    density = Mathf.Lerp(density, region.fogDensity, w);
                    ambient = Mathf.Lerp(ambient, region.ambientIntensity, w);
                }
                if (region.overrideSky)
                {
                    exposure = Mathf.Lerp(exposure, region.skyExposure, w);
                    tint = Color.Lerp(tint, region.skyTint, w);
                    thickness = Mathf.Lerp(thickness, region.skyThickness, w);
                }
            }

            RenderSettings.fogColor = fog;
            RenderSettings.fogDensity = density;
            RenderSettings.ambientIntensity = ambient;
            if (skyInstance != null)
            {
                skyInstance.SetFloat(ExposureId, exposure);
                skyInstance.SetColor(TintId, tint);
                skyInstance.SetFloat(ThicknessId, thickness);
            }
        }

        // 1 in interiorul cutiei, scade lin pe blendDistance in afara ei
        float WeightAt(Vector3 point)
        {
            Vector3 local = transform.InverseTransformPoint(point);
            Vector3 half = size * 0.5f;
            Vector3 outside = new Vector3(Mathf.Max(0f, Mathf.Abs(local.x) - half.x), Mathf.Max(0f, Mathf.Abs(local.y) - half.y), Mathf.Max(0f, Mathf.Abs(local.z) - half.z));
            float d = outside.magnitude;
            if (blendDistance <= 0f) return d > 0f ? 0f : 1f;
            float k = 1f - Mathf.Clamp01(d / blendDistance);
            return k * k * (3f - 2f * k);
        }

        static void Capture()
        {
            baseFogColor = RenderSettings.fogColor;
            baseFogDensity = RenderSettings.fogDensity;
            baseAmbient = RenderSettings.ambientIntensity;
            baseSky = RenderSettings.skybox;
            baseExposure = 1f;
            baseThickness = 1f;
            baseTint = Color.grey;
            skyInstance = null;

            // cerul se modifica pe o copie, materialul din proiect ramane neatins
            if (baseSky != null && baseSky.HasProperty(ExposureId) && baseSky.HasProperty(TintId) && baseSky.HasProperty(ThicknessId))
            {
                baseExposure = baseSky.GetFloat(ExposureId);
                baseTint = baseSky.GetColor(TintId);
                baseThickness = baseSky.GetFloat(ThicknessId);
                skyInstance = new Material(baseSky) { name = baseSky.name + " (Region)" };
                RenderSettings.skybox = skyInstance;
            }
        }

        static void Restore()
        {
            RenderSettings.fogColor = baseFogColor;
            RenderSettings.fogDensity = baseFogDensity;
            RenderSettings.ambientIntensity = baseAmbient;
            if (skyInstance != null)
            {
                RenderSettings.skybox = baseSky;
                Destroy(skyInstance);
                skyInstance = null;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireCube(Vector3.zero, size);
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.25f);
            Gizmos.DrawWireCube(Vector3.zero, size + Vector3.one * (blendDistance * 2f));
        }
    }
}
