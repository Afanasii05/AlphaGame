using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Alpha.World
{
    // Atmosfera de-a lungul unui drum: ceata, ambientul, soarele si cerul se amesteca continuu dupa distanta
    // parcursa pe traseu (fara trigger-e, fara salturi). Ruleaza dupa RegionAtmosphere si se aseaza peste ce a
    // scris aceea in frame-ul curent; primul punct al traseului are aceleasi valori ca regiunea de la intrare,
    // asa ca trecerea dinspre castel nu se vede. Valorile initiale se pun inapoi cand componenta se opreste.
    [DefaultExecutionOrder(100)]
    public class RouteAtmosphere : MonoBehaviour
    {
        static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        static readonly int TintId = Shader.PropertyToID("_SkyTint");
        static readonly int ThicknessId = Shader.PropertyToID("_AtmosphereThickness");

        [Serializable]
        public struct Stop
        {
            public float distance;
            public Color fogColor;
            public float fogDensity;
            public float ambientIntensity;
            public Color ambientTint;
            public Color sunColor;
            public float sunIntensity;
            public Color skyTint;
            public float skyExposure;
            public float skyThickness;
            public float volumeWeight;
        }

        // o valoare globala pe care o scriem peste ce au scris altii in acelasi frame
        struct Layered<T>
        {
            public T under, wrote;
            public bool valid;
        }

        [SerializeField] Vector3[] path;
        [SerializeField] Stop[] stops;
        [SerializeField] Light sun;
        [SerializeField] Volume volume;
        [Tooltip("Metri inainte de inceputul traseului pe care efectul creste de la 0 la 1")]
        [SerializeField] float fadeIn = 15f;
        [Tooltip("Pana la distanta asta de traseu efectul e complet, apoi scade pe lateralFade")]
        [SerializeField] float lateralFull = 70f;
        [SerializeField] float lateralFade = 30f;

        float[] lengths;
        SphericalHarmonicsL2 baseProbe;
        Layered<Color> fog, sunColor, skyTint;
        Layered<float> density, ambient, sunIntensity, skyExposure, skyThickness;

        public float Weight { get; private set; }
        public float Distance { get; private set; }

        void OnEnable()
        {
            BuildLengths();
            baseProbe = RenderSettings.ambientProbe;
            fog = sunColor = skyTint = default;
            density = ambient = sunIntensity = skyExposure = skyThickness = default;
            if (volume != null) volume.weight = 0f;
        }

        void OnDisable()
        {
            RenderSettings.ambientProbe = baseProbe;
            RenderSettings.fogColor = Restore(RenderSettings.fogColor, fog);
            RenderSettings.fogDensity = Restore(RenderSettings.fogDensity, density);
            RenderSettings.ambientIntensity = Restore(RenderSettings.ambientIntensity, ambient);
            if (sun != null)
            {
                sun.color = Restore(sun.color, sunColor);
                sun.intensity = Restore(sun.intensity, sunIntensity);
            }
            Material sky = Sky();
            if (sky != null)
            {
                sky.SetColor(TintId, Restore(sky.GetColor(TintId), skyTint));
                sky.SetFloat(ExposureId, Restore(sky.GetFloat(ExposureId), skyExposure));
                sky.SetFloat(ThicknessId, Restore(sky.GetFloat(ThicknessId), skyThickness));
            }
            if (volume != null) volume.weight = 0f;
        }

        void LateUpdate()
        {
            if (path == null || path.Length < 2 || stops == null || stops.Length == 0) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            Locate(cam.transform.position, out float s, out float lateral, out float behind);
            float w = (1f - Smooth(0f, fadeIn, behind)) * (1f - Smooth(lateralFull, lateralFull + lateralFade, lateral));
            Distance = s;
            Weight = w;
            Stop t = Sample(s);

            // ceata si ambientul: peste valorile scrise de regiuni in acest frame
            RenderSettings.fogColor = Over(RenderSettings.fogColor, ref fog, t.fogColor, w);
            RenderSettings.fogDensity = Over(RenderSettings.fogDensity, ref density, t.fogDensity, w);
            RenderSettings.ambientIntensity = Over(RenderSettings.ambientIntensity, ref ambient, t.ambientIntensity, w);

            // proba de lumina ambientala de la pornire, colorata
            Color tint = Color.Lerp(Color.white, t.ambientTint, w);
            SphericalHarmonicsL2 probe = baseProbe;
            for (int c = 0; c < 9; c++)
            {
                probe[0, c] *= tint.r;
                probe[1, c] *= tint.g;
                probe[2, c] *= tint.b;
            }
            RenderSettings.ambientProbe = probe;

            if (sun != null)
            {
                sun.color = Over(sun.color, ref sunColor, t.sunColor, w);
                sun.intensity = Over(sun.intensity, ref sunIntensity, t.sunIntensity, w);
            }

            // cerul: doar copia facuta de RegionAtmosphere, niciodata materialul din proiect
            Material sky = Sky();
            if (sky != null)
            {
                sky.SetColor(TintId, Over(sky.GetColor(TintId), ref skyTint, t.skyTint, w));
                sky.SetFloat(ExposureId, Over(sky.GetFloat(ExposureId), ref skyExposure, t.skyExposure, w));
                sky.SetFloat(ThicknessId, Over(sky.GetFloat(ThicknessId), ref skyThickness, t.skyThickness, w));
            }

            if (volume != null) volume.weight = t.volumeWeight * w;
        }

        static Material Sky()
        {
            Material sky = RenderSettings.skybox;
            return sky != null && sky.name.EndsWith(" (Region)") && sky.HasProperty(TintId) && sky.HasProperty(ThicknessId) ? sky : null;
        }

        // daca altcineva a rescris valoarea dupa noi, aceea devine noua baza; altfel pastram baza din frame-ul trecut
        static float Over(float now, ref Layered<float> v, float target, float w)
        {
            if (!v.valid || !Mathf.Approximately(now, v.wrote)) v.under = now;
            v.wrote = Mathf.Lerp(v.under, target, w);
            v.valid = true;
            return v.wrote;
        }

        static Color Over(Color now, ref Layered<Color> v, Color target, float w)
        {
            if (!v.valid || now != v.wrote) v.under = now;
            v.wrote = Color.Lerp(v.under, target, w);
            v.valid = true;
            return v.wrote;
        }

        static float Restore(float now, Layered<float> v) => v.valid && Mathf.Approximately(now, v.wrote) ? v.under : now;
        static Color Restore(Color now, Layered<Color> v) => v.valid && now == v.wrote ? v.under : now;

        void BuildLengths()
        {
            if (path == null) return;
            lengths = new float[path.Length];
            for (int i = 1; i < path.Length; i++) lengths[i] = lengths[i - 1] + Vector2.Distance(new Vector2(path[i - 1].x, path[i - 1].z), new Vector2(path[i].x, path[i].z));
        }

        // distanta pe traseu, distanta laterala si cat de mult e punctul in spatele inceputului
        void Locate(Vector3 eye, out float s, out float lateral, out float behind)
        {
            if (lengths == null || lengths.Length != path.Length) BuildLengths();
            Vector2 p = new Vector2(eye.x, eye.z);
            float best = float.MaxValue;
            s = 0f;
            behind = 0f;
            for (int i = 0; i < path.Length - 1; i++)
            {
                Vector2 a = new Vector2(path[i].x, path[i].z), ab = new Vector2(path[i + 1].x, path[i + 1].z) - a;
                float len2 = ab.sqrMagnitude;
                float raw = len2 > 1e-6f ? Vector2.Dot(p - a, ab) / len2 : 0f;
                float u = Mathf.Clamp01(raw);
                float d2 = (p - (a + ab * u)).sqrMagnitude;
                if (d2 >= best) continue;
                best = d2;
                s = Mathf.Lerp(lengths[i], lengths[i + 1], u);
                behind = i == 0 && raw < 0f ? -raw * Mathf.Sqrt(len2) : 0f;
            }
            lateral = Mathf.Sqrt(best);
            // in spatele startului distanta e masurata de-a lungul drumului, nu lateral
            if (behind > 0f) lateral = Mathf.Sqrt(Mathf.Max(0f, best - behind * behind));
        }

        Stop Sample(float s)
        {
            if (s <= stops[0].distance) return stops[0];
            for (int i = 1; i < stops.Length; i++)
            {
                if (s > stops[i].distance) continue;
                Stop a = stops[i - 1], b = stops[i];
                float k = Smooth(a.distance, b.distance, s);
                return new Stop
                {
                    distance = s,
                    fogColor = Color.Lerp(a.fogColor, b.fogColor, k),
                    fogDensity = Mathf.Lerp(a.fogDensity, b.fogDensity, k),
                    ambientIntensity = Mathf.Lerp(a.ambientIntensity, b.ambientIntensity, k),
                    ambientTint = Color.Lerp(a.ambientTint, b.ambientTint, k),
                    sunColor = Color.Lerp(a.sunColor, b.sunColor, k),
                    sunIntensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, k),
                    skyTint = Color.Lerp(a.skyTint, b.skyTint, k),
                    skyExposure = Mathf.Lerp(a.skyExposure, b.skyExposure, k),
                    skyThickness = Mathf.Lerp(a.skyThickness, b.skyThickness, k),
                    volumeWeight = Mathf.Lerp(a.volumeWeight, b.volumeWeight, k)
                };
            }
            return stops[stops.Length - 1];
        }

        static float Smooth(float a, float b, float x)
        {
            if (b <= a) return x >= b ? 1f : 0f;
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        void OnDrawGizmosSelected()
        {
            if (path == null) return;
            Gizmos.color = new Color(0.5f, 0.9f, 0.6f, 0.8f);
            for (int i = 0; i < path.Length - 1; i++) Gizmos.DrawLine(path[i], path[i + 1]);
        }
    }
}
