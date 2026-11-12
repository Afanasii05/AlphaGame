using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Alpha.World.EditorTools
{
    // Atmosfera pentru camp, castel si mlastina: volume locale reci (albastru-otel), ceata joasa, fum pe dalele salii,
    // praf in razele de lumina, ciori peste donjon. Materialele de particule sunt cele din Arena 1, incarcate dupa cale.
    public static partial class Arena2Builder
    {
        const string VolumeFolder = Folder + "/Art/Volumes";
        static readonly Vector3 DefaultSunForward = new Vector3(0.61f, -0.29f, 0.73f);
        static readonly List<Light> shadowLights = new List<Light>();

        class AtmosArt
        {
            public Mesh crossCard;
            public Material fog, glow, crow, shaft, leaf, grass, dust;
            public VolumeProfile fieldProfile, hallProfile;
        }

        static AtmosArt art;

        static void PrepareArt()
        {
            art = new AtmosArt
            {
                crossCard = Load<Mesh>(Atmos + "Mesh_CrossCard.asset"),
                fog = Load<Material>(Atmos + "M_Atmos_Fog.mat"),
                glow = Load<Material>(Atmos + "M_Atmos_Glow.mat"),
                crow = Load<Material>(Atmos + "M_Atmos_Crow.mat"),
                shaft = Load<Material>(Atmos + "M_Atmos_Shaft.mat"),
                leaf = Load<Material>(Atmos + "M_Atmos_Leaf.mat"),
                grass = TintedCopy(Atmos + "M_Atmos_GrassClump.mat", A2Mat + "M_A2_GrassDead.mat", new Color(0.78f, 0.72f, 0.52f)),
                dust = Load<Material>("Assets/World/Arena1/Art/FX/M_FX_Dust.mat"),
                fieldProfile = FieldProfile(),
                hallProfile = HallProfile()
            };
        }

        // copie a unui material din Arena 1 cu alta culoare (originalul ramane neatins)
        static Material TintedCopy(string source, string path, Color tint)
        {
            Material src = Load<Material>(source);
            if (src == null) return null;
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(src);
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.CopyPropertiesFromMaterial(src);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ post-process

        static VolumeProfile FieldProfile()
        {
            // amurg rece dar lizibil: umbrele raman albastrui, fara sa inghita drumul si personajul
            VolumeProfile profile = EnsureProfile("Arena2_FieldVolume");
            ColorAdjustments color = Add<ColorAdjustments>(profile);
            color.postExposure.Override(0.4f);
            color.contrast.Override(10f);
            color.saturation.Override(-18f);
            color.colorFilter.Override(new Color(0.97f, 0.97f, 1f));
            SplitToning split = Add<SplitToning>(profile);
            split.shadows.Override(new Color(0.42f, 0.47f, 0.53f));
            split.highlights.Override(new Color(0.7f, 0.63f, 0.53f));
            split.balance.Override(-10f);
            LiftGammaGain lgg = Add<LiftGammaGain>(profile);
            lgg.lift.Override(new Vector4(0.98f, 0.99f, 1.02f, 0.015f));
            lgg.gamma.Override(new Vector4(1f, 1f, 1.01f, 0.03f));
            lgg.gain.Override(new Vector4(1.02f, 1f, 0.97f, 0f));
            Vignette vignette = Add<Vignette>(profile);
            vignette.intensity.Override(0.26f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(Color.black);
            FilmGrain grain = Add<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Thin2);
            grain.intensity.Override(0.15f);
            grain.response.Override(0.8f);
            Bloom bloom = Add<Bloom>(profile);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.85f);
            bloom.scatter.Override(0.7f);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static VolumeProfile HallProfile()
        {
            // sala: focul cald contra lunii reci, cu umbre deschise ca sa se vada lancea si pasii
            VolumeProfile profile = EnsureProfile("Arena2_HallVolume");
            ColorAdjustments color = Add<ColorAdjustments>(profile);
            color.postExposure.Override(0.6f);
            color.contrast.Override(12f);
            color.saturation.Override(-14f);
            color.colorFilter.Override(new Color(1f, 0.98f, 0.97f));
            SplitToning split = Add<SplitToning>(profile);
            split.shadows.Override(new Color(0.42f, 0.47f, 0.54f));
            split.highlights.Override(new Color(0.78f, 0.62f, 0.46f));
            split.balance.Override(-8f);
            LiftGammaGain lgg = Add<LiftGammaGain>(profile);
            lgg.lift.Override(new Vector4(0.98f, 0.99f, 1.03f, 0.02f));
            lgg.gamma.Override(new Vector4(1f, 1f, 1f, 0.04f));
            lgg.gain.Override(new Vector4(1.04f, 1f, 0.95f, 0f));
            Vignette vignette = Add<Vignette>(profile);
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.5f);
            vignette.color.Override(Color.black);
            FilmGrain grain = Add<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Thin2);
            grain.intensity.Override(0.16f);
            grain.response.Override(0.8f);
            Bloom bloom = Add<Bloom>(profile);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(1f);
            bloom.scatter.Override(0.72f);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        // profilul se pastreaza (GUID stabil), componentele se refac de la zero
        static VolumeProfile EnsureProfile(string name)
        {
            string path = VolumeFolder + "/" + name + ".asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            foreach (VolumeComponent component in profile.components)
                if (component != null) Object.DestroyImmediate(component, true);
            profile.components.Clear();
            return profile;
        }

        static T Add<T>(VolumeProfile profile) where T : VolumeComponent
        {
            T component = profile.Add<T>(false);
            component.name = typeof(T).Name;
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        // ------------------------------------------------------------------ atmosfera

        static void BuildAtmosphere()
        {
            Transform root = new GameObject(AtmosphereRoot).transform;

            // regiunile: trecatoarea dinspre Arena 1, campul, castelul si mlastina (umeda, verzuie)
            Region(root, "Region_Pass", new Vector3(272f, 15f, 246f), new Vector3(30f, 40f, 20f), 12f, 9, art.fieldProfile,
                new Color(0.42f, 0.44f, 0.47f), 0.0065f, 1.05f, 0.85f, new Color(0.5f, 0.52f, 0.58f), 2f);
            Region(root, "Region_Field", new Vector3(352f, 18f, 318f), new Vector3(190f, 80f, 120f), 25f, 10, art.fieldProfile,
                new Color(0.44f, 0.46f, 0.5f), 0.005f, 1.1f, 0.85f, new Color(0.5f, 0.53f, 0.6f), 2f);
            Region(root, "Region_Castle", HallCenter + Vector3.up * 8f, new Vector3(56f, 44f, 76f), 14f, 12, art.hallProfile,
                new Color(0.4f, 0.42f, 0.47f), 0.0075f, 1.2f, 0.75f, new Color(0.48f, 0.5f, 0.58f), 2.3f);
            Region(root, "Region_Swamp", new Vector3(476f, 10f, 418f), new Vector3(48f, 34f, 64f), 14f, 11, art.fieldProfile,
                new Color(0.4f, 0.45f, 0.43f), 0.012f, 1f, 0.7f, new Color(0.46f, 0.5f, 0.5f), 2.4f);

            // ceata joasa de-a lungul drumului
            Transform fog = Group(root, "GroundFog");
            for (float s = 6f; s < roadLength - 8f; s += 22f)
            {
                Vector3 p = RoadPoint(s + 11f);
                ParticleSystem ps = Loop("Fog", fog, p + Vector3.up * 0.6f, Quaternion.Euler(0f, RoadYaw(s + 11f), 0f), art.fog, 26, 14f, 20f, 6f, 11f, 1.6f);
                Box(ps, new Vector3(RoadHalfW(s) * 2f + 14f, 1f, 22f));
                Tint(ps, new Color(0.92f, 0.94f, 0.97f, 0.12f));
                Fade(ps, 0.5f);
                Wander(ps, 0.15f, 0.1f);
            }
            // bancuri de ceata pe camp, departe de drum
            for (int i = 0; i < 9; i++)
            {
                float s = Mathf.Lerp(25f, roadLength - 30f, i / 8f);
                Vector3 p = Edge(s, i % 2 == 0 ? -1 : 1, R(12f, 30f));
                ParticleSystem ps = Loop("FogBank", fog, p + Vector3.up * 0.8f, Quaternion.Euler(0f, R(0f, 360f), 0f), art.fog, 20, 16f, 24f, 8f, 14f, 1.1f);
                Box(ps, new Vector3(26f, 1.5f, 18f));
                Tint(ps, new Color(0.9f, 0.92f, 0.96f, 0.11f));
                Fade(ps, 0.5f);
                Wander(ps, 0.12f, 0.08f);
            }
            // santul si mlastina
            ParticleSystem moatS = Loop("Fog_Moat", fog, new Vector3(419f, MoatY + 0.8f, MoatSouthZ), Quaternion.identity, art.fog, 30, 14f, 20f, 5f, 9f, 2f);
            Box(moatS, new Vector3(64f, 1f, 5f));
            Tint(moatS, new Color(0.88f, 0.91f, 0.95f, 0.17f));
            Fade(moatS, 0.5f);
            Wander(moatS, 0.12f, 0.1f);
            ParticleSystem moatW = Loop("Fog_MoatWest", fog, new Vector3(MoatWestX, MoatY + 0.8f, 404f), Quaternion.identity, art.fog, 24, 14f, 20f, 5f, 9f, 1.5f);
            Box(moatW, new Vector3(5f, 1f, 76f));
            Tint(moatW, new Color(0.88f, 0.91f, 0.95f, 0.17f));
            Fade(moatW, 0.5f);
            Wander(moatW, 0.12f, 0.1f);
            ParticleSystem swamp = Loop("Fog_Swamp", fog, new Vector3(478f, 6f, 418f), Quaternion.identity, art.fog, 60, 16f, 24f, 6f, 12f, 3.5f);
            Box(swamp, new Vector3(44f, 1.5f, 70f));
            Tint(swamp, new Color(0.82f, 0.9f, 0.88f, 0.22f));
            Fade(swamp, 0.5f);
            Wander(swamp, 0.15f, 0.1f);

            // fum pe dalele salii si praf in aer
            Transform hall = Group(root, "Hall");
            ParticleSystem smoke = Loop("FloorSmoke", hall, HallCenter + Vector3.up * 0.35f, Quaternion.identity, art.fog, 60, 12f, 18f, 4f, 8f, 4f);
            Box(smoke, new Vector3(34f, 0.4f, 46f));
            Tint(smoke, new Color(0.88f, 0.88f, 0.9f, 0.12f));
            Fade(smoke, 0.5f);
            Wander(smoke, 0.18f, 0.12f);
            ParticleSystem motes = Loop("Motes", hall, HallCenter + Vector3.up * 4f, Quaternion.identity, art.glow, 160, 8f, 12f, 0.025f, 0.06f, 16f);
            Box(motes, new Vector3(30f, 8f, 44f));
            Tint(motes, new Color(1f, 0.88f, 0.66f, 0.5f));
            Fade(motes, 0.3f);
            Wander(motes, 0.25f, 0.3f);
            // praf mai dens in coloanele de lumina de sub acoperisul prabusit
            foreach (Vector3 gap in RoofGaps)
            {
                ParticleSystem ray = Loop("Motes_Ray", hall, new Vector3(gap.x, PlateauY + 6f, gap.z), Quaternion.identity, art.glow, 80, 6f, 10f, 0.02f, 0.05f, 9f);
                Box(ray, new Vector3(6f, 11f, 5f));
                Tint(ray, new Color(0.85f, 0.92f, 1f, 0.6f));
                Fade(ray, 0.3f);
                Wander(ray, 0.2f, 0.25f);
            }

            // cenusa fina pe camp
            Transform ash = Group(root, "Ash");
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = RoadPoint(Mathf.Lerp(30f, roadLength - 20f, i / 3f));
                ParticleSystem ps = Loop("Ash", ash, p + Vector3.up * 5f, Quaternion.identity, art.leaf, 24, 8f, 12f, 0.05f, 0.1f, 3f);
                Box(ps, new Vector3(24f, 4f, 24f));
                Tint(ps, new Color(0.45f, 0.43f, 0.42f, 0.8f));
                var main = ps.main;
                main.gravityModifier = 0.015f;
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                Wander(ps, 0.6f, 0.3f);
                Fade(ps, 0.2f);
                Sheet(ps, 2, 2, 0f);
            }

            // ciori care se rotesc peste donjon si peste camp
            Transform crows = Group(root, "Crows");
            Crows(crows, new Vector3(420f, 66f, 452f), 18f, 8, 0.45f, 0.9f, 1.2f);
            Crows(crows, new Vector3(352f, 34f, 296f), 14f, 5, 0.3f, 0.7f, 1f);
            Crows(crows, HallCenter + Vector3.up * 30f, 10f, 4, 0.25f, 0.8f, 1f);

            // raze de lumina prin golurile din acoperis
            Transform shafts = Group(root, "LightShafts");
            foreach (Vector3 gap in RoofGaps)
            {
                Shaft(shafts, new Vector3(gap.x + R(-1f, 1f), PlateauY, gap.z + R(-1f, 1f)), R(4f, 5f), 19f);
                Shaft(shafts, new Vector3(gap.x + R(-2f, 2f), PlateauY, gap.z + R(-1.5f, 1.5f)), R(2.5f, 3.2f), 18f);
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
            volume.isGlobal = true;   // greutatea o da RegionAtmosphere dupa distanta
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

        static Vector3 SunForward()
        {
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
                if (light.type == LightType.Directional && light.name == "Sun") return light.transform.forward;
            return DefaultSunForward;
        }

        // directia razelor din sala: aproape verticale, aplecate putin dinspre soare
        static Vector3 RayAxis()
        {
            Vector3 toSun = Flat(-SunForward()).normalized;
            return (Vector3.up * Mathf.Cos(22f * Mathf.Deg2Rad) + toSun * Mathf.Sin(22f * Mathf.Deg2Rad)).normalized;
        }

        static void Shaft(Transform parent, Vector3 basePoint, float width, float length)
        {
            if (art.crossCard == null || art.shaft == null) return;
            GameObject go = new GameObject("Shaft");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(basePoint - Vector3.up * 0.5f, Quaternion.FromToRotation(Vector3.up, RayAxis()) * Quaternion.Euler(0f, R(0f, 360f), 0f));
            go.transform.localScale = new Vector3(width, length, width);
            go.AddComponent<MeshFilter>().sharedMesh = art.crossCard;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = art.shaft;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        // carduri in cruce (smocuri de iarba uscata), fara umbre
        static void Cards(Transform parent, Vector3 p, float radius, int count, Material mat, float hMin, float hMax)
        {
            if (art.crossCard == null || mat == null) return;
            for (int i = 0; i < count; i++)
            {
                Vector3 q = p + new Vector3(R(-radius, radius), 0f, R(-radius, radius));
                if (NearestRoad(q.x, q.z).dist < RoadHalfW(0f) + 0.3f) continue;
                float h = R(hMin, hMax);
                GameObject card = new GameObject("Grass");
                card.transform.SetParent(parent, false);
                card.transform.SetPositionAndRotation(new Vector3(q.x, Ground(q.x, q.z) - 0.03f, q.z), Quaternion.Euler(R(-6f, 6f), R(0f, 360f), R(-6f, 6f)));
                card.transform.localScale = new Vector3(h * R(0.8f, 1.1f), h, h * R(0.8f, 1.1f));
                card.AddComponent<MeshFilter>().sharedMesh = art.crossCard;
                MeshRenderer r = card.AddComponent<MeshRenderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                MarkStatic(card);
            }
        }
    }
}
