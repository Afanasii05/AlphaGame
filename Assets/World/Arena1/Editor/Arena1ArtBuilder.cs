using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Alpha.World.EditorTools
{
    // Arta pentru atmosfera drumului si a arenei: texturi procedurale, mesh-ul de card in cruce, materiale
    // si profilele de post-process. Texturile se scriu doar daca lipsesc; materialele si profilele se refac la fiecare rulare.
    public static class Arena1ArtBuilder
    {
        public const string ArtFolder = "Assets/World/Arena1/Art/Atmos";
        public const string MaterialFolder = "Assets/World/Arena1/Art/Materials";
        const string ProfileFolder = "Assets/World/Arena1/Art";
        const string LitShader = "Universal Render Pipeline/Lit";
        const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";
        const string DecalShader = "Shader Graphs/Decal";
        const string DustTexPath = "Assets/World/Arena1/Art/FX/T_DustPuff.png";

        public static readonly Color RuneGlow = new Color(0.4f, 1f, 0.55f);

        public class Result
        {
            public Mesh crossCard;
            public Material runes, ritualDecal, fog, glow, firefly, leaf, crow, shaft, flowers, grass;
            public VolumeProfile roadProfile, arenaProfile;
        }

        public static Result Build()
        {
            EnsureFolder("Assets/World/Arena1", "Art");
            EnsureFolder("Assets/World/Arena1/Art", "Atmos");
            EnsureFolder("Assets/World/Arena1/Art", "Materials");

            Texture2D runeTex = EnsureTexture("T_RuneAtlas", 512, RunePixels, true, 256);
            Texture2D circleTex = EnsureTexture("T_RitualCircle", 1024, RitualCirclePixels, true);
            Texture2D glowTex = EnsureTexture("T_Glow", 64, GlowPixels, true);
            Texture2D leafTex = EnsureTexture("T_Leaf", 128, LeafPixels, true);
            Texture2D crowTex = EnsureTexture("T_Crow", 256, CrowPixels, true, 64);
            Texture2D shaftTex = EnsureTexture("T_Shaft", 64, ShaftPixels, true, 256);
            Texture2D flowerTex = EnsureTexture("T_Wildflowers", 256, FlowerPixels, true);
            Texture2D grassTex = EnsureTexture("T_GrassClump", 256, GrassPixels, true);
            Texture2D dustTex = AssetDatabase.LoadAssetAtPath<Texture2D>(DustTexPath);

            Result result = new Result();
            result.crossCard = EnsureCrossCard();
            result.runes = RuneMaterial(runeTex);
            result.ritualDecal = DecalMaterial(circleTex);
            result.fog = ParticleMaterial("M_Atmos_Fog", dustTex, false, 0f, 2.5f, 1.5f, 6f);
            result.glow = ParticleMaterial("M_Atmos_Glow", glowTex, true, 0f, 0.3f, 0.3f, 1.2f);
            result.firefly = ParticleMaterial("M_Atmos_Firefly", glowTex, true, 0f, 0.3f, 0.3f, 1.2f);
            result.firefly.SetColor("_BaseColor", new Color(2.2f, 2.8f, 1.1f, 1f));
            result.leaf = ParticleMaterial("M_Atmos_Leaf", leafTex, false, 0f, 0.2f, 0.4f, 1.2f);
            result.crow = ParticleMaterial("M_Atmos_Crow", crowTex, false, 0f, 0f, 0f, 0f);
            result.shaft = ParticleMaterial("M_Atmos_Shaft", shaftTex, true, 0f, 1.5f, 3f, 10f);
            result.shaft.SetFloat("_Cull", (float)CullMode.Off);
            result.shaft.SetColor("_BaseColor", new Color(1f, 0.84f, 0.62f, 0.07f));
            result.fog.SetColor("_BaseColor", new Color(0.52f, 0.56f, 0.6f, 1f));
            result.flowers = CardMaterial("M_Atmos_Wildflowers", flowerTex, Color.white);
            result.grass = CardMaterial("M_Atmos_GrassClump", grassTex, new Color(0.86f, 0.86f, 0.8f));
            result.roadProfile = RoadProfile();
            result.arenaProfile = ArenaProfile();
            AssetDatabase.SaveAssets();
            return result;
        }

        // copie "ruginita" a unui material de arma; originalul ramane neatins
        public static Material Rusted(Material source)
        {
            if (source == null) return null;
            string path = MaterialFolder + "/M_Rusted_" + source.name.Replace("M_", "") + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(source);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = source.shader;
                mat.CopyPropertiesFromMaterial(source);
            }
            Color tint = new Color(0.62f, 0.46f, 0.36f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mat.GetColor("_BaseColor") * tint);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", mat.GetFloat("_Smoothness") * 0.35f);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
            mat.DisableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // scut lasat in ploaie: lemn tocit si fier mat. Fara harta metalica, altfel in vale iese aproape negru
        public static Material Weathered(Material source)
        {
            if (source == null) return null;
            string path = MaterialFolder + "/M_Weathered_" + source.name.Replace("M_", "") + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(source);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = source.shader;
                mat.CopyPropertiesFromMaterial(source);
            }
            bool wood = source.name.Contains("Wood") || source.name.Contains("Back");
            if (mat.HasProperty("_MetallicGlossMap")) mat.SetTexture("_MetallicGlossMap", null);
            mat.DisableKeyword("_METALLICSPECGLOSSMAP");
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", wood ? new Color(0.86f, 0.8f, 0.72f) : new Color(0.78f, 0.66f, 0.56f));
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", wood ? 0f : 0.45f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", wood ? 0.18f : 0.32f);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
            mat.DisableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ materiale

        static Material RuneMaterial(Texture2D tex)
        {
            Material mat = EnsureMaterial("M_RiddleRunes", LitShader);
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", new Color(0.05f, 0.06f, 0.05f));
            mat.SetFloat("_Smoothness", 0.15f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", 0.45f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetOverrideTag("RenderType", "TransparentCutout");
            mat.renderQueue = (int)RenderQueue.AlphaTest;
            // RiddleStone inmulteste emisia cu 0.15 in repaus si cu glyphBoost cand asculta
            mat.SetTexture("_EmissionMap", tex);
            mat.SetColor("_EmissionColor", RuneGlow * 2.2f);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return mat;
        }

        static Material DecalMaterial(Texture2D tex)
        {
            Material mat = EnsureMaterial("M_RitualCircle_Decal", DecalShader);
            if (mat.HasProperty("Base_Map")) mat.SetTexture("Base_Map", tex);
            if (mat.HasProperty("Normal_Blend")) mat.SetFloat("Normal_Blend", 0f);
            if (mat.HasProperty("_DrawOrder")) mat.SetFloat("_DrawOrder", 0f);
            mat.enableInstancing = true;
            return mat;
        }

        static Material ParticleMaterial(string name, Texture2D tex, bool additive, float softNear, float softFar, float camNear, float camFar)
        {
            Material mat = EnsureMaterial(name, ParticleShader);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", additive ? 2f : 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (additive) mat.EnableKeyword("_BLENDMODE_ADD");
            else mat.DisableKeyword("_BLENDMODE_ADD");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);

            bool soft = softFar > 0f;
            mat.SetFloat("_SoftParticlesEnabled", soft ? 1f : 0f);
            mat.SetFloat("_SoftParticlesNearFadeDistance", softNear);
            mat.SetFloat("_SoftParticlesFarFadeDistance", softFar);
            mat.SetVector("_SoftParticleFadeParams", new Vector4(softNear, 1f / Mathf.Max(0.01f, softFar - softNear), 0f, 0f));
            if (soft) mat.EnableKeyword("_SOFTPARTICLES_ON");
            else mat.DisableKeyword("_SOFTPARTICLES_ON");

            bool fade = camFar > 0f;
            mat.SetFloat("_CameraFadingEnabled", fade ? 1f : 0f);
            mat.SetFloat("_CameraNearFadeDistance", camNear);
            mat.SetFloat("_CameraFarFadeDistance", camFar);
            mat.SetVector("_CameraFadeParams", new Vector4(camNear, 1f / Mathf.Max(0.01f, camFar - camNear), 0f, 0f));
            if (fade) mat.EnableKeyword("_FADING_ON");
            else mat.DisableKeyword("_FADING_ON");
            return mat;
        }

        static Material CardMaterial(string name, Texture2D tex, Color tint)
        {
            Material mat = EnsureMaterial(name, LitShader);
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", tint);
            mat.SetFloat("_Smoothness", 0.05f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", 0.5f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetFloat("_Cull", (float)CullMode.Off);
            mat.SetOverrideTag("RenderType", "TransparentCutout");
            mat.renderQueue = (int)RenderQueue.AlphaTest;
            mat.enableInstancing = true;
            return mat;
        }

        static Material EnsureMaterial(string name, string shaderName)
        {
            string path = ArtFolder + "/" + name + ".mat";
            Shader shader = Shader.Find(shaderName);
            if (shader == null) Debug.LogError($"[Arena1ArtBuilder] Lipseste shader-ul {shaderName}");
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

        // ------------------------------------------------------------------ mesh

        // trei carduri la 60 de grade, pivot jos; normalele in sus ca iarba sa fie luminata ca solul
        static Mesh EnsureCrossCard()
        {
            string path = ArtFolder + "/Mesh_CrossCard.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, path);
            }
            mesh.Clear();
            mesh.name = "Mesh_CrossCard";
            Vector3[] verts = new Vector3[12];
            Vector3[] normals = new Vector3[12];
            Vector2[] uvs = new Vector2[12];
            Color[] colors = new Color[12];
            int[] tris = new int[18];
            for (int i = 0; i < 3; i++)
            {
                Vector3 axis = Quaternion.Euler(0f, i * 60f, 0f) * Vector3.right * 0.5f;
                int v = i * 4;
                verts[v] = -axis;
                verts[v + 1] = axis;
                verts[v + 2] = -axis + Vector3.up;
                verts[v + 3] = axis + Vector3.up;
                uvs[v] = new Vector2(0f, 0f);
                uvs[v + 1] = new Vector2(1f, 0f);
                uvs[v + 2] = new Vector2(0f, 1f);
                uvs[v + 3] = new Vector2(1f, 1f);
                for (int k = 0; k < 4; k++)
                {
                    normals[v + k] = Vector3.up;
                    colors[v + k] = Color.white;
                }
                int t = i * 6;
                tris[t] = v; tris[t + 1] = v + 2; tris[t + 2] = v + 3;
                tris[t + 3] = v; tris[t + 4] = v + 3; tris[t + 5] = v + 1;
            }
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        // ------------------------------------------------------------------ post-process

        static VolumeProfile RoadProfile()
        {
            VolumeProfile profile = EnsureProfile("Arena1_RoadVolume");
            ColorAdjustments color = Add<ColorAdjustments>(profile);
            color.postExposure.Override(0.2f);
            color.contrast.Override(14f);
            color.saturation.Override(-22f);
            color.colorFilter.Override(new Color(0.92f, 0.96f, 1f));
            SplitToning split = Add<SplitToning>(profile);
            split.shadows.Override(new Color(0.34f, 0.52f, 0.56f));
            split.highlights.Override(new Color(0.76f, 0.6f, 0.44f));
            split.balance.Override(-15f);
            LiftGammaGain lgg = Add<LiftGammaGain>(profile);
            lgg.lift.Override(new Vector4(0.98f, 1f, 1.03f, -0.02f));
            lgg.gamma.Override(new Vector4(1f, 1f, 1f, 0.02f));
            lgg.gain.Override(new Vector4(1.03f, 1f, 0.96f, 0f));
            Vignette vignette = Add<Vignette>(profile);
            vignette.intensity.Override(0.32f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(Color.black);
            FilmGrain grain = Add<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Thin2);
            grain.intensity.Override(0.18f);
            grain.response.Override(0.8f);
            Bloom bloom = Add<Bloom>(profile);
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(0.9f);
            bloom.scatter.Override(0.72f);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static VolumeProfile ArenaProfile()
        {
            VolumeProfile profile = EnsureProfile("Arena1_ArenaVolume");
            ColorAdjustments color = Add<ColorAdjustments>(profile);
            color.postExposure.Override(0.1f);
            color.contrast.Override(18f);
            color.saturation.Override(-28f);
            color.colorFilter.Override(new Color(0.88f, 0.93f, 1f));
            SplitToning split = Add<SplitToning>(profile);
            split.shadows.Override(new Color(0.3f, 0.5f, 0.56f));
            split.highlights.Override(new Color(0.8f, 0.58f, 0.4f));
            split.balance.Override(-22f);
            LiftGammaGain lgg = Add<LiftGammaGain>(profile);
            lgg.lift.Override(new Vector4(0.97f, 1f, 1.04f, -0.03f));
            lgg.gamma.Override(new Vector4(1f, 1f, 1f, 0f));
            lgg.gain.Override(new Vector4(1.04f, 1f, 0.95f, 0f));
            Vignette vignette = Add<Vignette>(profile);
            vignette.intensity.Override(0.38f);
            vignette.smoothness.Override(0.5f);
            vignette.color.Override(Color.black);
            FilmGrain grain = Add<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Thin2);
            grain.intensity.Override(0.22f);
            grain.response.Override(0.8f);
            Bloom bloom = Add<Bloom>(profile);
            bloom.threshold.Override(0.8f);
            bloom.intensity.Override(1.1f);
            bloom.scatter.Override(0.75f);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        // profilul se pastreaza (GUID stabil), componentele se refac de la zero
        static VolumeProfile EnsureProfile(string name)
        {
            string path = ProfileFolder + "/" + name + ".asset";
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

        // ------------------------------------------------------------------ texturi

        delegate Color[] PixelSource(int width, int height);

        static Texture2D EnsureTexture(string name, int width, PixelSource source, bool mips, int height = 0)
        {
            if (height <= 0) height = width;
            string path = ArtFolder + "/" + name + ".png";
            if (!File.Exists(path))
            {
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.SetPixels(source(width, height));
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = mips;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.mipMapsPreserveCoverage = true;
                importer.alphaTestReferenceValue = 0.5f;
                importer.maxTextureSize = Mathf.Max(width, height);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // 8 rune in stil futhark, celule 4x2, coordonate 0..1 in celula
        static readonly float[][] Glyphs =
        {
            new[] { .38f, .1f, .38f, .9f,   .38f, .58f, .7f, .8f,   .38f, .36f, .7f, .6f },
            new[] { .32f, .1f, .32f, .9f,   .32f, .9f, .7f, .66f,   .7f, .66f, .7f, .1f },
            new[] { .36f, .1f, .36f, .9f,   .36f, .72f, .66f, .5f,   .66f, .5f, .36f, .28f },
            new[] { .36f, .1f, .36f, .9f,   .36f, .9f, .7f, .7f,   .36f, .66f, .7f, .46f },
            new[] { .34f, .1f, .34f, .9f,   .34f, .9f, .66f, .72f,   .66f, .72f, .34f, .52f,   .34f, .52f, .68f, .1f },
            new[] { .5f, .1f, .5f, .9f,   .5f, .52f, .24f, .86f,   .5f, .52f, .76f, .86f },
            new[] { .5f, .9f, .26f, .62f,   .5f, .9f, .74f, .62f,   .26f, .62f, .74f, .1f,   .74f, .62f, .26f, .1f },
            new[] { .5f, .1f, .5f, .9f,   .5f, .9f, .25f, .64f,   .5f, .9f, .75f, .64f }
        };

        static Color[] RunePixels(int w, int h)
        {
            Canvas canvas = new Canvas(w, h);
            int cell = w / 4;
            for (int i = 0; i < Glyphs.Length; i++)
            {
                Vector2 origin = new Vector2((i % 4) * cell, (i / 4) * cell);
                float[] g = Glyphs[i];
                for (int k = 0; k + 3 < g.Length; k += 4)
                {
                    Vector2 a = origin + new Vector2(g[k], g[k + 1]) * cell;
                    Vector2 b = origin + new Vector2(g[k + 2], g[k + 3]) * cell;
                    canvas.Line(a, b, cell * 0.055f, cell * 0.05f, Color.white, 1.6f);
                }
            }
            return canvas.pixels;
        }

        // cerc ritual pictat cu ocru inchis: inele, heptagrama, banda de rune, erodat de timp
        static Color[] RitualCirclePixels(int w, int h)
        {
            Color[] pixels = new Color[w * h];
            Vector2[] star = new Vector2[7];
            for (int i = 0; i < 7; i++)
            {
                float a = (90f + i * 360f / 7f) * Mathf.Deg2Rad;
                star[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.78f;
            }
            Color paint = new Color(0.26f, 0.07f, 0.045f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2((x + 0.5f) / w * 2f - 1f, (y + 0.5f) / h * 2f - 1f);
                    float r = p.magnitude;
                    float ink = 0f;
                    ink = Mathf.Max(ink, Band(r, 0.95f, 0.011f));
                    ink = Mathf.Max(ink, Band(r, 0.86f, 0.007f));
                    ink = Mathf.Max(ink, Band(r, 0.36f, 0.009f));
                    ink = Mathf.Max(ink, Band(r, 0.33f, 0.004f));
                    for (int i = 0; i < 7; i++)
                        ink = Mathf.Max(ink, 1f - Step(0.004f, 0.009f, SegmentDistance(p, star[i], star[(i + 3) % 7])));
                    if (r > 0.87f && r < 0.94f)
                    {
                        // banda de rune: 42 de semne scurte
                        float angle = Mathf.Atan2(p.y, p.x) / (Mathf.PI * 2f) + 0.5f;
                        float cells = angle * 42f;
                        int id = Mathf.FloorToInt(cells);
                        float u = cells - id, v = (r - 0.87f) / 0.07f;
                        float stroke = Mathf.Abs(u - 0.5f) < 0.07f && v > 0.15f && v < 0.85f ? 1f : 0f;
                        float slant = (id * 7919 % 3) switch { 0 => Mathf.Abs(u - 0.5f - (v - 0.5f) * 0.6f), 1 => Mathf.Abs(u - 0.5f + (v - 0.5f) * 0.6f), _ => 1f };
                        if (slant < 0.06f && v > 0.45f && v < 0.85f) stroke = 1f;
                        ink = Mathf.Max(ink, stroke);
                    }
                    for (int i = 0; i < 7; i++)
                        ink = Mathf.Max(ink, Band((p - star[i]).magnitude, 0.045f, 0.006f));

                    float erosion = Fbm(p.x * 6f + 13f, p.y * 6f + 7f);
                    float wear = Step(0.32f, 0.55f, erosion) * (0.55f + 0.45f * Fbm(p.x * 30f, p.y * 30f));
                    float a2 = Mathf.Clamp01(ink * wear) * 0.82f * (1f - Step(0.97f, 1f, r));
                    pixels[y * w + x] = new Color(paint.r, paint.g, paint.b, a2);
                }
            return pixels;
        }

        static Color[] GlowPixels(int w, int h)
        {
            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float r = new Vector2((x + 0.5f) / w * 2f - 1f, (y + 0.5f) / h * 2f - 1f).magnitude;
                    float a = Mathf.Clamp01(1f - r);
                    pixels[y * w + x] = new Color(1f, 1f, 1f, a * a * a);
                }
            return pixels;
        }

        // 2x2 frunze uscate in tonuri diferite
        static Color[] LeafPixels(int w, int h)
        {
            Canvas canvas = new Canvas(w, h);
            Color[] tones = { new Color(0.55f, 0.36f, 0.16f), new Color(0.42f, 0.28f, 0.14f), new Color(0.6f, 0.46f, 0.2f), new Color(0.46f, 0.18f, 0.1f) };
            int cell = w / 2;
            for (int i = 0; i < 4; i++)
            {
                Vector2 c = new Vector2((i % 2 + 0.5f) * cell, (i / 2 + 0.5f) * cell);
                Vector2 dir = Rot(35f + i * 25f);
                Vector2 tip = c + dir * cell * 0.38f, stem = c - dir * cell * 0.38f;
                canvas.Leaf(stem, tip, cell * 0.17f, tones[i]);
                canvas.Line(stem - dir * cell * 0.08f, tip, 1.2f, 0.4f, tones[i] * 0.6f, 1f);
            }
            return canvas.pixels;
        }

        // 4 cadre de bataie din aripi, siluete negre
        static Color[] CrowPixels(int w, int h)
        {
            Canvas canvas = new Canvas(w, h);
            Color ink = new Color(0.03f, 0.03f, 0.035f);
            float[] elbow = { 9f, 3f, -3f, 3f };
            float[] tip = { 15f, 5f, -9f, -1f };
            for (int f = 0; f < 4; f++)
            {
                Vector2 c = new Vector2(f * 64 + 32f, 30f);
                canvas.Line(c + new Vector2(0f, 1f), c + new Vector2(0f, -10f), 3.2f, 1.6f, ink, 1f);
                canvas.Disc(c + new Vector2(0f, 6f), 3f, ink);
                canvas.Disc(c, 4f, ink);
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector2 shoulder = c + new Vector2(s * 3f, 1f);
                    Vector2 e = c + new Vector2(s * 13f, elbow[f]);
                    Vector2 t = c + new Vector2(s * 27f, tip[f]);
                    canvas.Line(shoulder, e, 4.2f, 3f, ink, 1f);
                    canvas.Line(e, t, 3f, 0.8f, ink, 1f);
                }
            }
            return canvas.pixels;
        }

        static Color[] ShaftPixels(int w, int h)
        {
            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                    float edge = Mathf.Pow(Mathf.Sin(u * Mathf.PI), 2f);
                    float streak = 0.6f + 0.4f * Mathf.PerlinNoise(u * 9f, 3.1f);
                    float vertical = Step(0f, 0.55f, v) * (1f - Step(0.82f, 1f, v));
                    pixels[y * w + x] = new Color(1f, 1f, 1f, edge * streak * vertical);
                }
            return pixels;
        }

        // tulpini subtiri cu flori mici, palide
        static Color[] FlowerPixels(int w, int h)
        {
            Canvas canvas = new Canvas(w, h);
            System.Random rand = new System.Random(71);
            Color[] petals = { new Color(0.88f, 0.86f, 0.78f), new Color(0.86f, 0.74f, 0.36f), new Color(0.56f, 0.44f, 0.66f), new Color(0.62f, 0.24f, 0.2f) };
            for (int i = 0; i < 16; i++)
            {
                float x0 = w * (0.1f + 0.8f * (float)rand.NextDouble());
                float height = h * (0.35f + 0.55f * (float)rand.NextDouble());
                float lean = w * 0.12f * ((float)rand.NextDouble() - 0.5f);
                Vector2 a = new Vector2(x0, 0f), m = new Vector2(x0 + lean * 0.4f, height * 0.5f), b = new Vector2(x0 + lean, height);
                Color stem = Color.Lerp(new Color(0.24f, 0.32f, 0.16f), new Color(0.38f, 0.42f, 0.22f), (float)rand.NextDouble());
                canvas.Line(a, m, 2.2f, 1.8f, stem, 1f);
                canvas.Line(m, b, 1.8f, 1.3f, stem, 1f);
                if (rand.NextDouble() < 0.5)
                    canvas.Leaf(m, m + new Vector2(lean > 0 ? -18f : 18f, 14f), 4f, stem * 0.9f);
                Color petal = petals[rand.Next(petals.Length)];
                float size = 5f + 4f * (float)rand.NextDouble();
                for (int k = 0; k < 5; k++)
                    canvas.Disc(b + Rot(k * 72f + i * 13f) * size * 0.75f, size * 0.55f, petal);
                canvas.Disc(b, size * 0.35f, petal * 0.45f + new Color(0.25f, 0.2f, 0.05f) * 0.5f);
            }
            return canvas.pixels;
        }

        // smoc de iarba uscata, mai inchis la baza
        static Color[] GrassPixels(int w, int h)
        {
            Canvas canvas = new Canvas(w, h);
            System.Random rand = new System.Random(29);
            for (int i = 0; i < 46; i++)
            {
                float x0 = w * (0.2f + 0.6f * (float)rand.NextDouble());
                float height = h * (0.45f + 0.5f * (float)rand.NextDouble());
                float lean = w * 0.35f * ((float)rand.NextDouble() - 0.5f);
                Color tone = Color.Lerp(new Color(0.36f, 0.4f, 0.2f), new Color(0.62f, 0.58f, 0.34f), (float)rand.NextDouble());
                Vector2 a = new Vector2(x0, 0f), m = new Vector2(x0 + lean * 0.3f, height * 0.55f), b = new Vector2(x0 + lean, height);
                canvas.Line(a, m, 3.4f, 2.4f, tone * 0.7f, 1f);
                canvas.Line(m, b, 2.4f, 0.3f, tone, 1f);
            }
            return canvas.pixels;
        }

        // ------------------------------------------------------------------ utilitare

        // desen simplu in pixeli: linii cu grosime variabila, discuri si frunze, cu acoperire fina
        class Canvas
        {
            public readonly Color[] pixels;
            readonly int w, h;

            public Canvas(int width, int height)
            {
                w = width;
                h = height;
                pixels = new Color[w * h];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0f, 0f, 0f, 0f);
            }

            public void Line(Vector2 a, Vector2 b, float w0, float w1, Color color, float feather)
            {
                float pad = Mathf.Max(w0, w1) + feather + 1f;
                int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - pad)), x1 = Mathf.Min(w - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + pad));
                int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - pad)), y1 = Mathf.Min(h - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + pad));
                Vector2 ab = b - a;
                float len2 = Mathf.Max(ab.sqrMagnitude, 1e-4f);
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                        float d = (p - (a + ab * t)).magnitude;
                        float width = Mathf.Lerp(w0, w1, t);
                        Blend(x, y, color, 1f - Mathf.Clamp01((d - width) / feather + 0.5f));
                    }
            }

            public void Disc(Vector2 c, float radius, Color color)
            {
                int x0 = Mathf.Max(0, Mathf.FloorToInt(c.x - radius - 2f)), x1 = Mathf.Min(w - 1, Mathf.CeilToInt(c.x + radius + 2f));
                int y0 = Mathf.Max(0, Mathf.FloorToInt(c.y - radius - 2f)), y1 = Mathf.Min(h - 1, Mathf.CeilToInt(c.y + radius + 2f));
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float d = (new Vector2(x + 0.5f, y + 0.5f) - c).magnitude;
                        Blend(x, y, color, 1f - Mathf.Clamp01(d - radius + 0.5f));
                    }
            }

            // frunza = elipsa ascutita intre baza si varf
            public void Leaf(Vector2 a, Vector2 b, float halfWidth, Color color)
            {
                Vector2 ab = b - a;
                float len = ab.magnitude;
                if (len < 0.01f) return;
                Vector2 dir = ab / len, side = new Vector2(-dir.y, dir.x);
                float pad = halfWidth + 2f;
                int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - pad)), x1 = Mathf.Min(w - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + pad));
                int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - pad)), y1 = Mathf.Min(h - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + pad));
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - a;
                        float t = Vector2.Dot(p, dir) / len;
                        if (t < 0f || t > 1f) continue;
                        float half = halfWidth * Mathf.Sin(t * Mathf.PI) * (1f - 0.25f * t);
                        float d = Mathf.Abs(Vector2.Dot(p, side));
                        float shade = 0.85f + 0.15f * (1f - d / Mathf.Max(half, 0.01f));
                        Blend(x, y, color * shade, 1f - Mathf.Clamp01(d - half + 0.5f));
                    }
            }

            void Blend(int x, int y, Color color, float coverage)
            {
                if (coverage <= 0f) return;
                int i = y * w + x;
                Color dst = pixels[i];
                float a = coverage + dst.a * (1f - coverage);
                Color rgb = a > 0f ? (color * coverage + dst * dst.a * (1f - coverage)) / a : color;
                pixels[i] = new Color(rgb.r, rgb.g, rgb.b, a);
            }
        }

        static float Band(float r, float radius, float halfWidth) => 1f - Step(halfWidth, halfWidth * 1.8f, Mathf.Abs(r - radius));

        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
            return (p - (a + ab * t)).magnitude;
        }

        static float Fbm(float x, float y)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int i = 0; i < 4; i++)
            {
                sum += Mathf.PerlinNoise(x, y) * amp;
                norm += amp;
                x *= 2.03f;
                y *= 2.03f;
                amp *= 0.5f;
            }
            return sum / norm;
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

        static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
