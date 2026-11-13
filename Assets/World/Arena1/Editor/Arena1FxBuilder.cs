using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Alpha.World.EditorTools
{
    // Efectele pentru inelul de pietre si UI-ul boss-ului: texturi generate procedural, materiale si prefab-uri FX.
    // Texturile se scriu doar daca lipsesc; materialele si prefab-urile se refac la fiecare rulare (GUID-urile raman).
    public static class Arena1FxBuilder
    {
        public const string ArtFolder = "Assets/World/Arena1/Art/FX";
        public const string PrefabFolder = "Assets/World/Arena1/Prefabs/FX";
        const string RockFolder = "Assets/Environment/Rocks/Prefabs/";
        const string RockMatPath = "Assets/Environment/Rocks/M_Rock.mat";
        const string DustTemplatePath = "Assets/Enemies/Stumphorn/VFX/M_StompDust.mat";
        const string DecalShader = "Shader Graphs/Decal";
        const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";
        const string UnlitShader = "Universal Render Pipeline/Unlit";
        const int NoCharacters = ~((1 << 2) | (1 << 6) | (1 << 7) | (1 << 8));

        static readonly Color DustDark = new Color(0.42f, 0.37f, 0.3f, 0.85f);
        static readonly Color DustLight = new Color(0.66f, 0.6f, 0.5f, 0.75f);

        public class Result
        {
            public GroundCrack crack;
            public ParticleSystem rumble, burst, ring;
            public DebrisChunk[] debris;
            public Sprite ornament, bar;
        }

        public static Result Build()
        {
            EnsureFolder("Assets/World/Arena1", "Art");
            EnsureFolder("Assets/World/Arena1/Art", "FX");
            EnsureFolder("Assets/World/Arena1/Prefabs", "FX");

            Texture2D crackTex = EnsureTexture("T_GroundCrack", 1024, CrackPixels, TextureImporterType.Default, true);
            Texture2D crackNormal = EnsureTexture("T_GroundCrack_N", 1024, CrackNormalPixels, TextureImporterType.NormalMap, true);
            Texture2D dustTex = EnsureTexture("T_DustPuff", 256, DustPixels, TextureImporterType.Default, true);
            EnsureTexture("T_UI_Ornament", 1024, OrnamentPixels, TextureImporterType.Sprite, false, 64);
            EnsureTexture("T_UI_Bar", 512, BarPixels, TextureImporterType.Sprite, false, 32);

            Material rockMat = AssetDatabase.LoadAssetAtPath<Material>(RockMatPath);
            Mesh[] rockMeshes = { RockMesh("Rock_A"), RockMesh("Rock_B"), RockMesh("Rock_C") };
            Material dustMat = DustMaterial(dustTex);

            Result result = new Result();
            result.crack = BuildCrack(crackTex, crackNormal);
            result.rumble = BuildRumble(dustMat, rockMeshes[2], rockMat);
            result.burst = BuildBurst(dustMat, rockMeshes[2], rockMat);
            result.ring = BuildRing(dustMat);
            result.debris = new DebrisChunk[3];
            string[] letters = { "A", "B", "C" };
            for (int i = 0; i < 3; i++) result.debris[i] = BuildDebris("FX_Debris_" + letters[i], rockMeshes[i], rockMat);
            result.ornament = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/T_UI_Ornament.png");
            result.bar = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/T_UI_Bar.png");
            return result;
        }

        // ------------------------------------------------------------------ crapatura

        static GroundCrack BuildCrack(Texture2D tex, Texture2D normal)
        {
            GameObject go = new GameObject("FX_GroundCrack");
            GroundCrack crack = go.AddComponent<GroundCrack>();
            if (HasDecalFeature())
            {
                Material mat = EnsureMaterial("M_GroundCrack_Decal", DecalShader);
                if (mat.HasProperty("Base_Map")) mat.SetTexture("Base_Map", tex);
                if (mat.HasProperty("Normal_Map")) mat.SetTexture("Normal_Map", normal);
                if (mat.HasProperty("Normal_Blend")) mat.SetFloat("Normal_Blend", 1f);
                if (mat.HasProperty("_DrawOrder")) mat.SetFloat("_DrawOrder", 1f);
                mat.enableInstancing = true;

                // proiecteaza in jos; fata verticala a pietrelor nu primeste decal
                go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                DecalProjector decal = go.AddComponent<DecalProjector>();
                decal.material = mat;
                decal.size = new Vector3(4f, 4f, 3f);
                decal.pivot = Vector3.zero;
                decal.drawDistance = 150f;
                decal.fadeScale = 0.9f;
                decal.startAngleFade = 35f;
                decal.endAngleFade = 65f;
                SetRef(crack, "decal", decal);
            }
            else
            {
                Material mat = EnsureMaterial("M_GroundCrack_Flat", UnlitShader);
                SetupTransparent(mat);
                mat.SetTexture("_BaseMap", tex);
                mat.SetColor("_BaseColor", Color.white);
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.name = "Crack";
                quad.transform.SetParent(go.transform, false);
                quad.transform.localPosition = Vector3.up * 0.05f;
                quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Renderer r = quad.GetComponent<Renderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                go.transform.localScale = new Vector3(4f, 1f, 4f);
                SetRef(crack, "flatRenderer", r);
            }
            return SavePrefab(go, "FX_GroundCrack").GetComponent<GroundCrack>();
        }

        static bool HasDecalFeature()
        {
            UniversalRenderPipelineAsset urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null) return false;
            SerializedProperty list = new SerializedObject(urp).FindProperty("m_RendererDataList");
            if (list == null || list.arraySize == 0) return false;
            for (int i = 0; i < list.arraySize; i++)
            {
                ScriptableRendererData data = list.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererData;
                if (data == null) continue;
                bool found = false;
                foreach (ScriptableRendererFeature feature in data.rendererFeatures)
                    if (feature is DecalRendererFeature && feature.isActive) found = true;
                if (!found) return false;
            }
            return true;
        }

        // ------------------------------------------------------------------ particule

        // tremurul de dinainte: praf jos care se ridica incet + pietricele care sar
        static ParticleSystem BuildRumble(Material dust, Mesh pebble, Material rock)
        {
            ParticleSystem ps = NewSystem("FX_RumbleDust", null, 1f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.7f, 1.5f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(DustDark, DustLight);
            main.gravityModifier = -0.03f;
            main.maxParticles = 80;
            Emit(ps, 34f);
            Cone(ps, 75f, 1.4f);
            SizeOverLife(ps, 0.5f, 1.6f);
            FadeOverLife(ps, 0.55f);
            Drag(ps, 1.5f);
            DustRenderer(ps, dust);

            ParticleSystem pebbles = NewSystem("Pebbles", ps.transform, 0.8f);
            main = pebbles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 3.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);
            main.gravityModifier = 1f;
            main.maxParticles = 40;
            Emit(pebbles, 26f);
            Cone(pebbles, 25f, 1.2f);
            Pebbles(pebbles, pebble, rock);
            return SavePrefab(ps.gameObject, "FX_RumbleDust").GetComponent<ParticleSystem>();
        }

        // ruperea pamantului: nor gros, fusta de praf pe sol si pietre aruncate
        static ParticleSystem BuildBurst(Material dust, Mesh pebble, Material rock)
        {
            ParticleSystem ps = NewSystem("FX_BurstDust", null, 0.5f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.4f, 3f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(DustDark, DustLight);
            main.gravityModifier = -0.02f;
            main.maxParticles = 90;
            Burst(ps, 46);
            Cone(ps, 35f, 1.4f);
            SizeOverLife(ps, 0.45f, 1.9f);
            FadeOverLife(ps, 0.85f);
            Drag(ps, 2.2f);
            DustRenderer(ps, dust);

            ParticleSystem skirt = NewSystem("Skirt", ps.transform, 0.5f);
            main = skirt.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.9f, 1.8f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(DustDark, DustLight);
            main.maxParticles = 40;
            Burst(skirt, 30);
            Circle(skirt, 1.6f);
            SizeOverLife(skirt, 0.6f, 1.7f);
            FadeOverLife(skirt, 0.7f);
            Drag(skirt, 3f);
            DustRenderer(skirt, dust);

            ParticleSystem stones = NewSystem("Pebbles", ps.transform, 0.5f);
            main = stones.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.22f);
            main.gravityModifier = 1.2f;
            main.maxParticles = 40;
            Burst(stones, 28);
            Cone(stones, 38f, 1.2f);
            Pebbles(stones, pebble, rock);
            return SavePrefab(ps.gameObject, "FX_BurstDust").GetComponent<ParticleSystem>();
        }

        // inelul mic de praf cand piatra se aseaza
        static ParticleSystem BuildRing(Material dust)
        {
            ParticleSystem ps = NewSystem("FX_DustRing", null, 0.3f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(DustDark, DustLight);
            main.maxParticles = 40;
            Burst(ps, 32);
            Circle(ps, 2f);
            SizeOverLife(ps, 0.6f, 1.5f);
            FadeOverLife(ps, 0.6f);
            Drag(ps, 3f);
            DustRenderer(ps, dust);
            return SavePrefab(ps.gameObject, "FX_DustRing").GetComponent<ParticleSystem>();
        }

        static ParticleSystem NewSystem(string name, Transform parent, float duration)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            else go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);   // conul priveste in sus
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.stopAction = ParticleSystemStopAction.None;
            return ps;
        }

        static void Emit(ParticleSystem ps, float rate)
        {
            var emission = ps.emission;
            emission.rateOverTime = rate;
            emission.SetBursts(new ParticleSystem.Burst[0]);
        }

        static void Burst(ParticleSystem ps, int count)
        {
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
        }

        static void Cone(ParticleSystem ps, float angle, float radius)
        {
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = angle;
            shape.radius = radius;
            shape.radiusThickness = 1f;
        }

        static void Circle(ParticleSystem ps, float radius)
        {
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.radiusThickness = 0.15f;
            shape.arc = 360f;
        }

        static void SizeOverLife(ParticleSystem ps, float from, float to)
        {
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(to, new AnimationCurve(new Keyframe(0f, from / to), new Keyframe(0.35f, 0.8f), new Keyframe(1f, 1f)));
        }

        static void FadeOverLife(ParticleSystem ps, float peak)
        {
            var color = ps.colorOverLifetime;
            color.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(peak, 0.1f), new GradientAlphaKey(peak * 0.6f, 0.55f), new GradientAlphaKey(0f, 1f) });
            color.color = g;
        }

        static void Drag(ParticleSystem ps, float drag)
        {
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.drag = drag;
            limit.multiplyDragByParticleSize = false;
            limit.multiplyDragByParticleVelocity = false;
        }

        static void DustRenderer(ParticleSystem ps, Material dust)
        {
            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
            ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = dust;
            r.maxParticleSize = 2f;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        static void Pebbles(ParticleSystem ps, Mesh mesh, Material rock)
        {
            var main = ps.main;
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.72f, 0.68f), Color.white);
            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.separateAxes = true;
            rotation.x = new ParticleSystem.MinMaxCurve(-6f, 6f);
            rotation.y = new ParticleSystem.MinMaxCurve(-6f, 6f);
            rotation.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            var collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.collidesWith = NoCharacters;
            collision.bounce = 0.3f;
            collision.dampen = 0.45f;
            collision.lifetimeLoss = 0f;
            collision.quality = ParticleSystemCollisionQuality.Medium;
            var color = ps.colorOverLifetime;
            color.enabled = false;
            ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = mesh;
            r.sharedMaterial = rock;
            r.alignment = ParticleSystemRenderSpace.World;
            r.shadowCastingMode = ShadowCastingMode.On;
        }

        // ------------------------------------------------------------------ bucati de piatra

        static DebrisChunk BuildDebris(string name, Mesh mesh, Material rock)
        {
            GameObject go = new GameObject(name);
            go.layer = 2;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = rock;
            BoxCollider box = go.AddComponent<BoxCollider>();
            if (mesh != null)
            {
                box.center = mesh.bounds.center;
                box.size = mesh.bounds.size * 0.9f;
            }
            box.excludeLayers = (1 << 6) | (1 << 7) | (1 << 8);
            Rigidbody body = go.AddComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.excludeLayers = (1 << 6) | (1 << 7) | (1 << 8);
            go.AddComponent<DebrisChunk>();
            return SavePrefab(go, name).GetComponent<DebrisChunk>();
        }

        static Mesh RockMesh(string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RockFolder + name + ".prefab");
            MeshFilter mf = prefab != null ? prefab.GetComponentInChildren<MeshFilter>(true) : null;
            if (mf == null) Debug.LogError($"[Arena1FxBuilder] Lipseste mesh-ul pentru {name}");
            return mf != null ? mf.sharedMesh : null;
        }

        // ------------------------------------------------------------------ materiale

        static Material DustMaterial(Texture2D tex)
        {
            Material mat = EnsureMaterial("M_FX_Dust", ParticleShader);
            Material template = AssetDatabase.LoadAssetAtPath<Material>(DustTemplatePath);
            if (template != null && template.shader == mat.shader) mat.CopyPropertiesFromMaterial(template);
            else SetupTransparent(mat);
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            SoftFade(mat, 0f, 1.2f, 0.6f, 2.2f);
            return mat;
        }

        // praful se estompeaza unde intra in sol si langa camera, fara margini taiate drept
        static void SoftFade(Material mat, float softNear, float softFar, float camNear, float camFar)
        {
            mat.SetFloat("_SoftParticlesEnabled", 1f);
            mat.SetFloat("_SoftParticlesNearFadeDistance", softNear);
            mat.SetFloat("_SoftParticlesFarFadeDistance", softFar);
            mat.SetVector("_SoftParticleFadeParams", new Vector4(softNear, 1f / Mathf.Max(0.01f, softFar - softNear), 0f, 0f));
            mat.EnableKeyword("_SOFTPARTICLES_ON");
            mat.SetFloat("_CameraFadingEnabled", 1f);
            mat.SetFloat("_CameraNearFadeDistance", camNear);
            mat.SetFloat("_CameraFarFadeDistance", camFar);
            mat.SetVector("_CameraFadeParams", new Vector4(camNear, 1f / Mathf.Max(0.01f, camFar - camNear), 0f, 0f));
            mat.EnableKeyword("_FADING_ON");
        }

        static void SetupTransparent(Material mat)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        static Material EnsureMaterial(string name, string shaderName)
        {
            string path = ArtFolder + "/" + name + ".mat";
            Shader shader = Shader.Find(shaderName);
            if (shader == null) Debug.LogError($"[Arena1FxBuilder] Lipseste shader-ul {shaderName}");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (shader != null && mat.shader != shader)
            {
                mat.shader = shader;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ texturi

        delegate Color32[] PixelSource(int width, int height);

        static Texture2D EnsureTexture(string name, int width, PixelSource source, TextureImporterType type, bool mips, int height = 0)
        {
            if (height <= 0) height = width;
            string path = ArtFolder + "/" + name + ".png";
            if (!File.Exists(path))
            {
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.SetPixels32(source(width, height));
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = type;
                importer.mipmapEnabled = mips;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = type != TextureImporterType.NormalMap;
                importer.maxTextureSize = Mathf.Max(width, height);
                if (type == TextureImporterType.Sprite)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 100f;
                }
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // crapaturi ramificate care pornesc din centru, cu un halou intunecat de pamant rupt
        static float[] crackDepth;

        static float[] CrackDepth(int size)
        {
            if (crackDepth != null && crackDepth.Length == size * size) return crackDepth;
            float[] depth = new float[size * size];
            System.Random rng = new System.Random(7);
            float c = size * 0.5f;
            int mains = 9;
            for (int i = 0; i < mains; i++)
            {
                float angle = i * 360f / mains + (float)(rng.NextDouble() * 24 - 12);
                Vector2 start = new Vector2(c, c) + Rot(angle) * (float)(rng.NextDouble() * size * 0.03);
                Branch(depth, size, rng, start, angle, size * (0.34f + (float)rng.NextDouble() * 0.12f), size * 0.012f + (float)rng.NextDouble() * size * 0.006f, 0);
            }
            // fisuri circulare scurte intre ramuri
            for (int ring = 0; ring < 2; ring++)
            {
                float radius = size * (ring == 0 ? 0.11f : 0.21f);
                for (int arc = 0; arc < 5; arc++)
                {
                    float a0 = (float)(rng.NextDouble() * 360), span = 25f + (float)rng.NextDouble() * 35f;
                    // pas de ~1.5 pixeli pe arc, ca linia sa fie continua
                    float stepDeg = 1.5f / radius * Mathf.Rad2Deg;
                    for (float a = 0f; a < span; a += stepDeg)
                    {
                        float r = radius * (1f + Mathf.Sin(a * 0.12f + arc) * 0.04f);
                        Vector2 p = new Vector2(c, c) + Rot(a0 + a) * r;
                        Stamp(depth, size, p, size * 0.0035f * (1f - Mathf.Abs(a / span - 0.5f)) + 1f, 0.8f);
                    }
                }
            }
            crackDepth = depth;
            return depth;
        }

        static void Branch(float[] depth, int size, System.Random rng, Vector2 pos, float angle, float length, float width, int level)
        {
            const float step = 3f;
            for (float s = 0f; s < length; s += step)
            {
                float k = s / length;
                angle += (float)(rng.NextDouble() * 22 - 11);
                pos += Rot(angle) * step;
                Stamp(depth, size, pos, width * Mathf.Pow(1f - k, 0.7f) + 0.8f, 1f);
                if (level < 2 && k > 0.08f && rng.NextDouble() < 0.025)
                {
                    float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                    Branch(depth, size, rng, pos, angle + side * (28f + (float)rng.NextDouble() * 30f), (length - s) * 0.55f, width * (1f - k) * 0.6f, level + 1);
                }
            }
        }

        static void Stamp(float[] depth, int size, Vector2 p, float radius, float strength)
        {
            int x0 = Mathf.Max(0, (int)(p.x - radius - 1)), x1 = Mathf.Min(size - 1, (int)(p.x + radius + 1));
            int y0 = Mathf.Max(0, (int)(p.y - radius - 1)), y1 = Mathf.Min(size - 1, (int)(p.y + radius + 1));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), p);
                    float v = (1f - Step(radius * 0.35f, radius, d)) * strength;
                    int i = y * size + x;
                    if (v > depth[i]) depth[i] = v;
                }
        }

        static Color32[] CrackPixels(int w, int h)
        {
            float[] depth = CrackDepth(w);
            Color32[] px = new Color32[w * h];
            Color core = new Color(0.03f, 0.025f, 0.02f), halo = new Color(0.2f, 0.16f, 0.12f);
            float c = w * 0.5f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float r = new Vector2(x - c, y - c).magnitude / c;
                    float noise = Mathf.PerlinNoise(x * 0.012f, y * 0.012f) * 0.6f + Mathf.PerlinNoise(x * 0.05f + 9f, y * 0.05f) * 0.4f;
                    float haloA = (1f - Step(0.05f, 0.75f, r)) * Mathf.Clamp01(noise * 1.4f - 0.2f) * 0.55f;
                    float d = depth[y * w + x];
                    float edge = 1f - Step(0.85f, 1f, r);
                    Color col = Color.Lerp(halo, core, Mathf.Clamp01(d * 1.3f));
                    col.a = Mathf.Clamp01(Mathf.Max(d, haloA)) * edge;
                    px[y * w + x] = col;
                }
            return px;
        }

        static Color32[] CrackNormalPixels(int w, int h)
        {
            float[] depth = CrackDepth(w);
            Color32[] px = new Color32[w * h];
            const float strength = 3f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float l = depth[y * w + Mathf.Max(x - 1, 0)], r = depth[y * w + Mathf.Min(x + 1, w - 1)];
                    float d = depth[Mathf.Max(y - 1, 0) * w + x], u = depth[Mathf.Min(y + 1, h - 1) * w + x];
                    // crapatura e o adancitura: inaltimea = -depth
                    Vector3 n = new Vector3((r - l) * strength, (u - d) * strength, 1f).normalized;
                    px[y * w + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            return px;
        }

        // nor de praf pufos, alb (culoarea vine din particule)
        static Color32[] DustPixels(int w, int h)
        {
            System.Random rng = new System.Random(3);
            Vector3[] blobs = new Vector3[7];
            for (int i = 0; i < blobs.Length; i++)
            {
                Vector2 p = Rot((float)rng.NextDouble() * 360f) * (float)rng.NextDouble() * 0.2f;
                blobs[i] = new Vector3(0.5f + p.x, 0.5f + p.y, 0.14f + (float)rng.NextDouble() * 0.12f);
            }
            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                    float density = 0f;
                    foreach (Vector3 b in blobs)
                    {
                        float d2 = ((u - b.x) * (u - b.x) + (v - b.y) * (v - b.y)) / (b.z * b.z);
                        density += Mathf.Exp(-d2 * 1.6f);
                    }
                    float noise = Mathf.PerlinNoise(u * 6f, v * 6f) * 0.65f + Mathf.PerlinNoise(u * 15f + 3f, v * 15f) * 0.35f;
                    float r = new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f;
                    float a = Mathf.Clamp01(density * 0.55f) * (0.55f + noise * 0.6f) * (1f - Step(0.7f, 1f, r));
                    float shade = 0.82f + 0.18f * Mathf.Clamp01(v * 0.7f + (1f - u) * 0.3f + (noise - 0.5f) * 0.4f);
                    px[y * w + x] = new Color(shade, shade, shade, Mathf.Clamp01(a));
                }
            return px;
        }

        // linie ornamentala subtire, cu romb in centru si puncte pe laterale
        static Color32[] OrnamentPixels(int w, int h)
        {
            Color32[] px = new Color32[w * h];
            float cx = w * 0.5f, cy = h * 0.5f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - cx), dy = Mathf.Abs(y + 0.5f - cy);
                    float u = dx / cx;
                    float thick = 2.2f * Mathf.Pow(Mathf.Max(1f - u, 0f), 0.6f) + 0.3f;
                    float line = Mathf.Clamp01(thick - dy + 0.5f) * Mathf.Pow(Mathf.Max(1f - u, 0f), 0.35f);
                    if (dx < 30f) line *= Mathf.Clamp01((dx - 22f) / 3f);   // golul din jurul rombului
                    float diamond = Mathf.Clamp01(13f - (dx + dy) + 0.5f);
                    float outline = Mathf.Clamp01(1.4f - Mathf.Abs(dx + dy - 20f));
                    float dot = 0f;
                    foreach (float ox in new[] { 52f, 78f })
                        dot = Mathf.Max(dot, Mathf.Clamp01(3.2f - new Vector2(dx - ox, dy).magnitude + 0.5f));
                    float a = Mathf.Max(Mathf.Max(line, diamond), Mathf.Max(outline, dot));
                    px[y * w + x] = new Color(1f, 1f, 1f, a);
                }
            return px;
        }

        static Color32[] BarPixels(int w, int h)
        {
            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float v = (y + 0.5f) / h;
                    float shade = 0.72f + 0.28f * Step(0f, 1f, v);
                    if (v > 0.62f && v < 0.74f) shade = Mathf.Min(1f, shade + 0.12f);   // luciu subtire
                    px[y * w + x] = new Color(shade, shade, shade, 1f);
                }
            return px;
        }

        static float Step(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        static Vector2 Rot(float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
        }

        // ------------------------------------------------------------------ utilitare

        static GameObject SavePrefab(GameObject go, string name)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/" + name + ".prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }

        static void SetRef(Object target, string name, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(name);
            if (p == null) { Debug.LogError($"[Arena1FxBuilder] Campul {name} nu exista pe {target.GetType().Name}"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
