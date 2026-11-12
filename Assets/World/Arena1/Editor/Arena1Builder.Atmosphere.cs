using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Alpha.World.EditorTools
{
    // Atmosfera locala pentru drum si arena: volume de post-process cu greutate dupa distanta, ceata si cer pe regiuni,
    // particule (ceata joasa, praf, licurici, frunze, ciori) si raze de lumina false. Volumul global si soarele raman neatinse.
    public static partial class Arena1Builder
    {
        const string AtmosphereRoot = "Arena1_Atmosphere";
        static readonly Vector3 DefaultSunForward = new Vector3(0.61f, -0.29f, 0.73f);
        static readonly List<Light> shadowLights = new List<Light>();

        static void BuildAtmosphere(Vector3 c)
        {
            float sStart = 10f, s1 = ctrlS[5], s2 = ctrlS[9], sEnd = ctrlS[ctrlS.Length - 1];
            Transform root = new GameObject(AtmosphereRoot).transform;

            // regiunile: drumul (pajiste + defileu + poarta) si arena; arena are prioritate mai mare
            Region(root, "Region_Road", Origin + new Vector3(6f, 5f, 102f), new Vector3(80f, 70f, 155f), 25f, 10, art.roadProfile,
                new Color(0.42f, 0.44f, 0.48f), 0.012f, 1.05f, 0.9f, new Color(0.5f, 0.48f, 0.56f), 1.6f);
            Region(root, "Region_Arena", c + Vector3.up * 4f, new Vector3(56f, 40f, 56f), 15f, 11, art.arenaProfile,
                new Color(0.38f, 0.4f, 0.44f), 0.015f, 1f, 0.8f, new Color(0.44f, 0.44f, 0.52f), 1.9f);

            // ceata joasa de-a lungul drumului, cate un emitator la ~22 m
            Transform fog = Group(root, "GroundFog");
            for (float s = sStart; s < sEnd - 6f; s += 22f)
            {
                RoadFrame(s + 11f, out Vector3 pos, out _, out float halfW);
                Vector3 p = Origin + pos;
                ParticleSystem ps = Loop("Fog", fog, new Vector3(p.x, Ground(p.x, p.z) + 0.6f, p.z), Quaternion.Euler(0f, RoadYaw(s + 11f), 0f), art.fog, 24, 14f, 20f, 6f, 10f, 1.5f);
                Box(ps, new Vector3(halfW * 2f + 10f, 1f, 22f));
                Tint(ps, new Color(1f, 1f, 1f, 0.14f));
                Fade(ps, 0.5f);
                Wander(ps, 0.15f, 0.1f);
            }
            ParticleSystem ring = Loop("Fog_Arena", fog, c + Vector3.up * 0.5f, Quaternion.Euler(-90f, 0f, 0f), art.fog, 50, 16f, 22f, 6f, 10f, 2.6f);
            var ringShape = ring.shape;
            ringShape.shapeType = ParticleSystemShapeType.Circle;
            ringShape.radius = 17f;
            ringShape.radiusThickness = 0.45f;
            Tint(ring, new Color(1f, 1f, 1f, 0.14f));
            Fade(ring, 0.5f);
            Wander(ring, 0.2f, 0.1f);

            // praf care pluteste in lumina: arena si defileul
            Transform motes = Group(root, "Motes");
            ParticleSystem arenaMotes = Loop("Motes_Arena", motes, c + Vector3.up * 2.6f, Quaternion.identity, art.glow, 120, 8f, 12f, 0.025f, 0.06f, 12f);
            Box(arenaMotes, new Vector3(34f, 5f, 34f));
            Tint(arenaMotes, new Color(1f, 0.86f, 0.62f, 0.55f));
            Fade(arenaMotes, 0.3f);
            Wander(arenaMotes, 0.25f, 0.3f);
            float sGorge = Mathf.Lerp(s1, s2, 0.5f);
            Vector3 gorge = RoadPoint(sGorge);
            ParticleSystem gorgeMotes = Loop("Motes_Gorge", motes, new Vector3(gorge.x, Ground(gorge.x, gorge.z) + 2.2f, gorge.z), Quaternion.Euler(0f, RoadYaw(sGorge), 0f), art.glow, 60, 8f, 12f, 0.025f, 0.05f, 6f);
            Box(gorgeMotes, new Vector3(8f, 4f, Mathf.Max(20f, s2 - s1)));
            Tint(gorgeMotes, new Color(0.75f, 0.85f, 1f, 0.5f));
            Fade(gorgeMotes, 0.3f);
            Wander(gorgeMotes, 0.25f, 0.3f);

            // licurici pe pajistea pelerinilor (la altar si la morminte)
            Transform fireflies = Group(root, "Fireflies");
            foreach (float k in new[] { 0.35f, 0.82f })
            {
                Vector3 p = Edge(Mathf.Lerp(sStart, s1, k), -1, 5f);
                ParticleSystem ps = Loop("Fireflies", fireflies, p + Vector3.up * 1.1f, Quaternion.identity, art.firefly, 25, 4f, 7f, 0.06f, 0.1f, 5f);
                Box(ps, new Vector3(14f, 2f, 14f));
                Blink(ps);
                Wander(ps, 0.6f, 0.4f);
            }

            // frunze uscate care cad din copacii morti
            Transform leaves = Group(root, "Leaves");
            Vector3[] leafSpots = { Edge(Mathf.Lerp(sStart, s1, 0.82f) + 5f, -1, 9f), Edge(Mathf.Lerp(s2, sEnd, 0.5f), 1, 3f), Around(c, EntryAngle(c) + 150f, 20f) };
            foreach (Vector3 p in leafSpots)
            {
                ParticleSystem ps = Loop("Leaves", leaves, p + Vector3.up * 6f, Quaternion.identity, art.leaf, 20, 6f, 10f, 0.12f, 0.2f, 2f);
                Box(ps, new Vector3(10f, 1f, 10f));
                var main = ps.main;
                main.gravityModifier = 0.05f;
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                var rotation = ps.rotationOverLifetime;
                rotation.enabled = true;
                rotation.z = new ParticleSystem.MinMaxCurve(-2f, 2f);
                Wander(ps, 0.8f, 0.35f);
                Fade(ps, 0.1f);
                Sheet(ps, 2, 2, 0f);
            }

            // ciori care se rotesc deasupra arenei si deasupra cimitirului
            Transform crows = Group(root, "Crows");
            Crows(crows, c + Vector3.up * 24f, 16f, 6, 0.4f, 0.8f, 1.1f);
            Vector3 grave = Edge(Mathf.Lerp(sStart, s1, 0.82f) + 5f, -1, 9f);
            Crows(crows, grave + Vector3.up * 16f, 8f, 3, 0.2f, 0.6f, 0.85f);

            // raze de lumina false, inclinate dinspre soare
            Transform shafts = Group(root, "LightShafts");
            for (int i = 0; i < 3; i++) Shaft(shafts, Around(c, EntryAngle(c) + 60f + i * 95f + R(-15f, 15f), R(8f, 13f)), R(3.5f, 4.5f));
            for (int i = 0; i < 2; i++)
            {
                Vector3 p = RoadPoint(Mathf.Lerp(s1, s2, 0.3f + i * 0.4f));
                Shaft(shafts, new Vector3(p.x, Ground(p.x, p.z), p.z), R(2.6f, 3.2f));
            }
        }

        // umbrele raman doar pe lumini alese; restul luminilor din zona noastra nu mai fac umbre
        static void LimitShadows(params string[] roots)
        {
            foreach (string name in roots)
            {
                GameObject root = GameObject.Find(name);
                if (root == null) continue;
                foreach (Light light in root.GetComponentsInChildren<Light>(true))
                    if (light.type != LightType.Directional && !shadowLights.Contains(light)) light.shadows = LightShadows.None;
            }
        }

        static void Region(Transform parent, string name, Vector3 center, Vector3 size, float blend, int priority, VolumeProfile profile,
            Color fogColor, float fogDensity, float ambient, float skyExposure, Color skyTint, float skyThickness)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            Volume volume = go.AddComponent<Volume>();
            volume.isGlobal = true;   // greutatea o da RegionAtmosphere dupa distanta, fara collidere
            volume.priority = priority;
            volume.weight = 0f;
            volume.sharedProfile = profile;

            RegionAtmosphere region = go.AddComponent<RegionAtmosphere>();
            SetRef(region, "volume", volume);
            SetVector3(region, "size", size);
            SetFloat(region, "blendDistance", blend);
            SetInt(region, "priority", priority);
            SetColor(region, "fogColor", fogColor);
            SetFloat(region, "fogDensity", fogDensity);
            SetFloat(region, "ambientIntensity", ambient);
            SetFloat(region, "skyExposure", skyExposure);
            SetColor(region, "skyTint", skyTint);
            SetFloat(region, "skyThickness", skyThickness);
        }

        static ParticleSystem Loop(string name, Transform parent, Vector3 position, Quaternion rotation, Material mat, int max,
            float lifeMin, float lifeMax, float sizeMin, float sizeMax, float rate)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 10f;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = max;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startSpeed = 0f;
            var emission = ps.emission;
            emission.rateOverTime = rate;
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.maxParticleSize = 4f;
            return ps;
        }

        static void Box(ParticleSystem ps, Vector3 size)
        {
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = size;
        }

        static void Tint(ParticleSystem ps, Color color)
        {
            var main = ps.main;
            main.startColor = color;
        }

        static void Fade(ParticleSystem ps, float hold)
        {
            var color = ps.colorOverLifetime;
            color.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.5f - hold * 0.5f), new GradientAlphaKey(1f, 0.5f + hold * 0.5f), new GradientAlphaKey(0f, 1f) });
            color.color = g;
        }

        static void Blink(ParticleSystem ps)
        {
            var color = ps.colorOverLifetime;
            color.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.1f, 0.35f), new GradientAlphaKey(1f, 0.55f),
                        new GradientAlphaKey(0.05f, 0.75f), new GradientAlphaKey(0.9f, 0.88f), new GradientAlphaKey(0f, 1f) });
            color.color = g;
        }

        static void Wander(ParticleSystem ps, float strength, float frequency)
        {
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = strength;
            noise.frequency = frequency;
            noise.scrollSpeed = 0.1f;
            noise.quality = ParticleSystemNoiseQuality.Low;
        }

        static void Sheet(ParticleSystem ps, int x, int y, float cycles)
        {
            var sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Grid;
            sheet.numTilesX = x;
            sheet.numTilesY = y;
            sheet.animation = ParticleSystemAnimationType.WholeSheet;
            sheet.startFrame = new ParticleSystem.MinMaxCurve(0f, x * y - 0.01f);
            if (cycles > 0f)
            {
                sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
                sheet.cycleCount = Mathf.RoundToInt(cycles);
            }
            else
            {
                sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
            }
        }

        // ciorile se nasc pe un cerc si orbiteaza in jurul axei verticale (sistem local, rotit cu z in sus)
        static void Crows(Transform parent, Vector3 center, float radius, int max, float rate, float sizeMin, float sizeMax)
        {
            ParticleSystem ps = Loop("Crows", parent, center, Quaternion.Euler(-90f, 0f, 0f), art.crow, max, 30f, 30f, sizeMin, sizeMax, rate);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.radiusThickness = 0.25f;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            // toate axele in acelasi mod (doua constante), altfel sistemul de particule refuza curbele
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0.32f, 0.45f);
            Wander(ps, 0.6f, 0.15f);
            Fade(ps, 0.9f);
            Sheet(ps, 4, 1, 60f);
        }

        static void Shaft(Transform parent, Vector3 basePoint, float width)
        {
            Vector3 sun = DefaultSunForward;
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
                if (light.type == LightType.Directional && light.name == "Sun") sun = light.transform.forward;
            Vector3 toSun = Flat(-sun).normalized;
            Vector3 axis = (Vector3.up * Mathf.Cos(35f * Mathf.Deg2Rad) + toSun * Mathf.Sin(35f * Mathf.Deg2Rad)).normalized;

            GameObject go = new GameObject("Shaft");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(new Vector3(basePoint.x, Ground(basePoint.x, basePoint.z) - 0.5f, basePoint.z),
                Quaternion.FromToRotation(Vector3.up, axis) * Quaternion.Euler(0f, R(0f, 360f), 0f));
            go.transform.localScale = new Vector3(width, R(10f, 12f), width);
            go.AddComponent<MeshFilter>().sharedMesh = art.crossCard;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = art.shaft;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        static void SetVector3(Object target, string name, Vector3 value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = Find(so, name);
            if (p == null) return;
            p.vector3Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetColor(Object target, string name, Color value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = Find(so, name);
            if (p == null) return;
            p.colorValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
