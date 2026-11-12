using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Alpha.World.EditorTools
{
    // Atmosfera mlastinii: ceata verde-albastruie tot mai deasa, apa neagra, turba, soare jos si chihlimbariu, licariri
    // de chihlimbar deasupra apei. Totul se amesteca dupa distanta pe drum (RouteAtmosphere), fara trigger-e.
    public static partial class Arena3Builder
    {
        const string A2Mat = Arena2ArtSetup.MaterialFolder + "/";
        const string SwampVolumeName = "Arena3_SwampVolume";
        static readonly Color Amber = new Color(1f, 0.62f, 0.24f);

        static Mesh crossCard;
        static Material fogMat, glowMat, reeds, charred, stoneDark, poolWater, blackWater;
        static VolumeProfile swampProfile;

        static void PrepareArt()
        {
            crossCard = Load<Mesh>(Atmos + "Mesh_CrossCard.asset");
            fogMat = Load<Material>(Atmos + "M_Atmos_Fog.mat");
            glowMat = Load<Material>(Atmos + "M_Atmos_Glow.mat");
            reeds = Load<Material>(A2Mat + "M_A2_Reeds.mat");
            charred = Load<Material>(A2Mat + "M_A2_WoodCharred.mat");
            stoneDark = Load<Material>(A2Mat + "M_A2_CastleStoneDark.mat");
            // apa ieftina, fara reflexia cerului: doar luciul soarelui pe negru
            poolWater = WaterMaterial("M_A3_PoolWater", new Color(0.02f, 0.03f, 0.018f), new Color(0.3f, 0.31f, 0.25f), 0.84f);
            blackWater = WaterMaterial("M_A3_BlackWater", new Color(0.012f, 0.016f, 0.012f), new Color(0.28f, 0.3f, 0.26f), 0.86f);
            swampProfile = SwampProfile();
            PrepareTerrainLayers();
        }

        static Material WaterMaterial(string name, Color baseColor, Color spec, float smooth)
        {
            string path = $"{MatFolder}/{name}.mat";
            Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_BaseColor", baseColor);
            mat.SetColor("_SpecColor", spec);
            mat.SetFloat("_Smoothness", smooth);
            mat.SetFloat("_SpecularHighlights", 1f);
            mat.SetFloat("_ReceiveShadows", 1f);
            mat.EnableKeyword("_SPECULAR_COLOR");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ post-process

        static VolumeProfile SwampProfile()
        {
            // aceleasi componente ca la camp, ca trecerea sa nu sara: mai putina culoare, umbre verzui, lumini chihlimbarii
            VolumeProfile profile = EnsureProfile(SwampVolumeName);
            ColorAdjustments color = Add<ColorAdjustments>(profile);
            color.postExposure.Override(0.3f);
            color.contrast.Override(12f);
            color.saturation.Override(-22f);
            color.colorFilter.Override(new Color(0.93f, 1f, 0.95f));
            SplitToning split = Add<SplitToning>(profile);
            split.shadows.Override(new Color(0.34f, 0.47f, 0.43f));
            split.highlights.Override(new Color(0.74f, 0.6f, 0.42f));
            split.balance.Override(-14f);
            LiftGammaGain lgg = Add<LiftGammaGain>(profile);
            lgg.lift.Override(new Vector4(0.97f, 1f, 0.99f, 0.01f));
            lgg.gamma.Override(new Vector4(0.99f, 1f, 0.99f, 0.02f));
            lgg.gain.Override(new Vector4(1.03f, 1f, 0.95f, 0f));
            Vignette vignette = Add<Vignette>(profile);
            vignette.intensity.Override(0.3f);
            vignette.smoothness.Override(0.48f);
            vignette.color.Override(Color.black);
            FilmGrain grain = Add<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Thin2);
            grain.intensity.Override(0.17f);
            grain.response.Override(0.8f);
            Bloom bloom = Add<Bloom>(profile);
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(1f);
            bloom.scatter.Override(0.72f);
            EditorUtility.SetDirty(profile);
            return profile;
        }

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
            Route(root);
            Fog(Group(root, "Fog"));
            Wisps(Group(root, "Wisps"));
        }

        // amestecul continuu: primul punct are exact valorile regiunii de mlastina din Arena 2 si lumina de baza a scenei
        static void Route(Transform root)
        {
            GameObject go = new GameObject("Swamp_Route");
            go.transform.SetParent(root, false);
            Volume volume = go.AddComponent<Volume>();
            volume.isGlobal = true;   // greutatea o da RouteAtmosphere
            volume.priority = 13;
            volume.weight = 0f;
            volume.sharedProfile = swampProfile;

            Light sun = null;
            GameObject sunGo = GameObject.Find("Sun");
            if (sunGo != null) sun = sunGo.GetComponent<Light>();
            if (sun == null) Debug.LogWarning("[Arena3Builder] Lumina Sun lipseste: soarele nu se mai coloreaza pe drum.");
            Color sunColor = sun != null ? sun.color : new Color(1f, 0.72f, 0.48f);
            float sunIntensity = sun != null ? sun.intensity : 2.1f;

            float island = Arena3Route.Length + 20f;
            RouteAtmosphere.Stop[] stops =
            {
                Stop(0f, new Color(0.4f, 0.45f, 0.43f), 0.012f, Color.white, sunColor, sunIntensity, new Color(0.46f, 0.5f, 0.5f), 0.7f, 2.4f, 0f),
                Stop(35f, new Color(0.37f, 0.43f, 0.4f), 0.013f, new Color(0.93f, 0.98f, 0.95f), new Color(1f, 0.7f, 0.46f), 1.75f, new Color(0.44f, 0.5f, 0.48f), 0.6f, 1.8f, 1f),
                Stop(100f, new Color(0.31f, 0.39f, 0.34f), 0.015f, new Color(0.82f, 0.92f, 0.86f), new Color(0.98f, 0.64f, 0.38f), 1.3f, new Color(0.42f, 0.5f, 0.45f), 0.5f, 1.3f, 1f),
                Stop(170f, new Color(0.26f, 0.35f, 0.31f), 0.018f, new Color(0.72f, 0.85f, 0.78f), new Color(0.95f, 0.57f, 0.28f), 0.95f, new Color(0.4f, 0.5f, 0.44f), 0.42f, 1f, 1f),
                Stop(island, new Color(0.24f, 0.33f, 0.29f), 0.017f, new Color(0.7f, 0.84f, 0.77f), new Color(0.95f, 0.55f, 0.26f), 0.9f, new Color(0.4f, 0.5f, 0.44f), 0.4f, 1f, 1f)
            };

            Vector3[] road = Arena3Route.Polyline(4f);
            Vector3[] path = new Vector3[road.Length + 1];
            road.CopyTo(path, 0);
            path[road.Length] = new Vector3(Arena3Route.IslandCentre.x, Arena3Route.IslandY, Arena3Route.IslandCentre.y);

            RouteAtmosphere atmosphere = go.AddComponent<RouteAtmosphere>();
            SerializedObject so = new SerializedObject(atmosphere);
            SerializedProperty pathProp = Find(so, "path");
            pathProp.arraySize = path.Length;
            for (int i = 0; i < path.Length; i++) pathProp.GetArrayElementAtIndex(i).vector3Value = path[i];
            SerializedProperty stopProp = Find(so, "stops");
            stopProp.arraySize = stops.Length;
            for (int i = 0; i < stops.Length; i++) stopProp.GetArrayElementAtIndex(i).boxedValue = stops[i];
            Find(so, "sun").objectReferenceValue = sun;
            Find(so, "volume").objectReferenceValue = volume;
            Find(so, "fadeIn").floatValue = 15f;
            Find(so, "lateralFull").floatValue = 70f;
            Find(so, "lateralFade").floatValue = 30f;
            so.ApplyModifiedPropertiesWithoutUndo();

            // banda de ceata peste cerul de la orizont
            GameObject band = new GameObject("HorizonVeil_Band");
            band.transform.SetParent(go.transform, false);
            band.AddComponent<MeshFilter>().sharedMesh = VeilMesh();
            MeshRenderer r = band.AddComponent<MeshRenderer>();
            r.sharedMaterial = VeilMaterial();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            r.enabled = false;
            HorizonVeil veil = go.AddComponent<HorizonVeil>();
            SetRef(veil, "route", atmosphere);
            SetRef(veil, "band", r);
        }

        // cilindru deschis de raza 1, de la putin sub orizont pana la ~63 de grade; v = inaltimea
        static Mesh VeilMesh()
        {
            string path = $"{MatFolder}/Mesh_A3_HorizonVeil.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh { name = "Mesh_A3_HorizonVeil" };
                AssetDatabase.CreateAsset(mesh, path);
            }
            const int segments = 64;
            const float bottom = -0.25f, top = 2f;
            Vector3[] v = new Vector3[(segments + 1) * 2];
            Vector2[] uv = new Vector2[v.Length];
            int[] tris = new int[segments * 6];
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                float x = Mathf.Cos(a), z = Mathf.Sin(a);
                v[i * 2] = new Vector3(x, bottom, z);
                v[i * 2 + 1] = new Vector3(x, top, z);
                uv[i * 2] = new Vector2(i / (float)segments, 0f);
                uv[i * 2 + 1] = new Vector2(i / (float)segments, 1f);
                if (i == segments) continue;
                int t = i * 6, b = i * 2;
                tris[t] = b; tris[t + 1] = b + 1; tris[t + 2] = b + 2;
                tris[t + 3] = b + 2; tris[t + 4] = b + 1; tris[t + 5] = b + 3;
            }
            mesh.Clear();
            mesh.vertices = v;
            mesh.uv = uv;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.bounds = new Bounds(new Vector3(0f, 0.875f, 0f), new Vector3(2f, 2.25f, 2f));
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        static Material VeilMaterial()
        {
            // gradientul: opac pana la ~6 grade peste orizont, apoi mai slab spre zenit; marginea de sus se stinge de tot
            string texPath = $"{MatFolder}/T_A3_HorizonVeil.asset";
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex == null)
            {
                tex = new Texture2D(4, 64, TextureFormat.RGBA32, false) { name = "T_A3_HorizonVeil" };
                AssetDatabase.CreateAsset(tex, texPath);
            }
            tex.wrapModeU = TextureWrapMode.Repeat;
            tex.wrapModeV = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < 64; y++)
            {
                float h = -0.25f + y / 63f * 2.25f;
                float a = (1f - 0.5f * Smooth(0.1f, 1.4f, h)) * (1f - Smooth(1.5f, 2f, h));
                for (int x = 0; x < 4; x++) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            EditorUtility.SetDirty(tex);

            string path = $"{MatFolder}/M_A3_HorizonVeil.mat";
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", new Color(0.3f, 0.38f, 0.34f, 0f));
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_Cull", 0f);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent - 50;   // inaintea cetii din particule
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static RouteAtmosphere.Stop Stop(float distance, Color fog, float density, Color ambientTint, Color sunColor, float sunIntensity,
            Color skyTint, float skyExposure, float skyThickness, float volumeWeight) => new RouteAtmosphere.Stop
        {
            distance = distance,
            fogColor = fog,
            fogDensity = density,
            ambientIntensity = 1f,
            ambientTint = ambientTint,
            sunColor = sunColor,
            sunIntensity = sunIntensity,
            skyTint = skyTint,
            skyExposure = skyExposure,
            skyThickness = skyThickness,
            volumeWeight = volumeWeight
        };

        static void Fog(Transform parent)
        {
            if (fogMat == null) return;
            // ceata joasa pe drum, tot mai deasa spre mlastina
            for (float s = 30f; s < Arena3Route.CausewayS; s += 20f)
            {
                float c = Arena3Route.StageC(s);
                Vector3 p = Arena3Route.Point(s + 10f);
                p.y = Mathf.Max(p.y - 0.5f, Arena3Route.WaterY) + 0.6f;
                ParticleSystem ps = Loop("Fog", parent, p, Quaternion.Euler(0f, Arena3Route.Yaw(s + 10f), 0f), fogMat, 30, 14f, 20f, 6f, 11f, 1.8f + c);
                Box(ps, new Vector3(Arena3Route.HalfW(s) * 2f + 20f, 1f, 22f));
                Tint(ps, new Color(0.8f, 0.9f, 0.86f, 0.13f + 0.08f * c));
                Fade(ps, 0.5f);
                Wander(ps, 0.14f, 0.1f);
            }
            // bancuri pe apa neagra din bazin
            for (int i = 0; i < 9; i++)
            {
                float a = i / 9f * Mathf.PI * 2f + 0.3f;
                float k = 0.62f + 0.2f * (i % 3) / 2f;
                Vector3 p = new Vector3(Arena3Route.BasinCentre.x + Mathf.Cos(a) * Arena3Route.BasinRadii.x * k, Arena3Route.WaterY + 0.7f,
                    Arena3Route.BasinCentre.y + Mathf.Sin(a) * Arena3Route.BasinRadii.y * k);
                ParticleSystem ps = Loop("FogBank", parent, p, Quaternion.Euler(0f, R(0f, 360f), 0f), fogMat, 26, 16f, 24f, 8f, 14f, 1.4f);
                Box(ps, new Vector3(30f, 1.4f, 22f));
                Tint(ps, new Color(0.76f, 0.88f, 0.83f, 0.2f));
                Fade(ps, 0.5f);
                Wander(ps, 0.12f, 0.08f);
            }
            // inel de abur pe apa mica din jurul insulei
            ParticleSystem ring = Loop("Mist_Island", parent, new Vector3(Arena3Route.IslandCentre.x, Arena3Route.WaterY + 0.45f, Arena3Route.IslandCentre.y),
                Quaternion.Euler(90f, 0f, 0f), fogMat, 50, 12f, 18f, 3f, 6f, 3f);
            var shape = ring.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = Arena3Route.IslandR + 4f;
            shape.radiusThickness = 0.22f;
            Tint(ring, new Color(0.7f, 0.82f, 0.76f, 0.32f));
            Fade(ring, 0.4f);
            Wander(ring, 0.2f, 0.15f);
        }

        // licariri de chihlimbar deasupra apei (mlastina si marginea insulei), cu doua lumini mici fara umbre
        static void Wisps(Transform parent)
        {
            if (glowMat == null) return;
            float[] at = { 150f, 175f, 196f };
            for (int i = 0; i < at.Length; i++)
            {
                Vector3 p = At(at[i], (i % 2 == 0 ? -1f : 1f) * (Arena3Route.HalfW(at[i]) + 8f));
                p.y = Arena3Route.WaterY + 1.2f;
                ParticleSystem ps = Loop("Wisps", parent, p, Quaternion.identity, glowMat, 12, 6f, 10f, 0.06f, 0.13f, 1.4f);
                Box(ps, new Vector3(12f, 1.6f, 12f));
                Tint(ps, new Color(Amber.r, Amber.g, Amber.b, 0.75f));
                Fade(ps, 0.4f);
                Wander(ps, 0.5f, 0.35f);
                if (i == 1) continue;
                Light light = new GameObject("WispLight").AddComponent<Light>();
                light.transform.SetParent(ps.transform, false);
                light.type = LightType.Point;
                light.color = Amber;
                light.range = 7f;
                light.intensity = 1.3f;
                light.shadows = LightShadows.None;
            }
            ParticleSystem island = Loop("Wisps_Island", parent, new Vector3(Arena3Route.IslandCentre.x, Arena3Route.WaterY + 1.3f, Arena3Route.IslandCentre.y),
                Quaternion.Euler(90f, 0f, 0f), glowMat, 24, 6f, 10f, 0.06f, 0.12f, 2.6f);
            var shape = island.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = Arena3Route.IslandR + 5f;
            shape.radiusThickness = 0.15f;
            Tint(island, new Color(Amber.r, Amber.g, Amber.b, 0.7f));
            Fade(island, 0.4f);
            Wander(island, 0.5f, 0.3f);
        }

        // ------------------------------------------------------------------ particule (ca in Arena 2)

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
    }
}
