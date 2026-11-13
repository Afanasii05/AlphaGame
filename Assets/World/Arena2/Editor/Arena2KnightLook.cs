using UnityEditor;
using UnityEngine;

namespace Alpha.World.EditorTools
{
    // Lancea de razboi si scutul uzat ale cavalerului: texturi noi derivate din cele exportate, pe clase de pixeli.
    // Lancea: dungile rosu/crem devin lemn inchis la culoare, inelele aurii fier, varful otel innegrit, flamura rosu stins si roasa.
    // Scutul: albul devine un alb murdar, patat. Originalele raman neatinse ca rezerva; rezultatul e determinist.
    public static class Arena2KnightLook
    {
        const string Folder = Arena2ArtSetup.KnightFolder;
        const string LanceSource = Folder + "/T_KnightLance_BaseColor.png";
        const string ShieldSource = Folder + "/T_KnightShield_BaseColor.png";
        public const string LanceWar = Folder + "/T_KnightLance_War_BaseColor.png";
        public const string ShieldWorn = Folder + "/T_KnightShield_Worn_BaseColor.png";
        const string LanceMaterial = Folder + "/Materials/M_Knight_Lance.mat";
        const string ShieldMaterial = Folder + "/Materials/M_Knight_Shield.mat";

        // zona flamurii si fata scutului in UV (0..1, de jos in sus)
        static readonly Rect PennonArea = new Rect(0f, 0f, 0.45f, 0.455f);
        static readonly Rect ShieldTip = new Rect(0.48f, 0.69f, 0.2f, 0.31f);
        static readonly Rect ShieldFace = new Rect(0.68f, 0.62f, 0.32f, 0.38f);

        static readonly Color Wood = new Color(0.2f, 0.125f, 0.075f);
        static readonly Color Iron = new Color(0.17f, 0.17f, 0.18f);
        static readonly Color Pennon = new Color(0.22f, 0.03f, 0.035f);
        static readonly Color OffWhite = new Color(0.68f, 0.645f, 0.57f);
        const float Blacken = 0.24f;
        const float PennonHoles = 0.79f;

        enum Kind { Keep, Wood, Stripe, OldWood, Iron, Steel, Pennon }

        [MenuItem("Tools/Alpha/World/Arena 2 Knight Look")]
        static void Rebuild()
        {
            Apply(true);
            AssetDatabase.SaveAssets();
        }

        // texturile se fac doar daca lipsesc (sau la cerere), materialele se leaga mereu
        public static void Apply(bool force = false)
        {
            if (force || !System.IO.File.Exists(LanceWar)) Bake(LanceSource, LanceWar, Lance);
            if (force || !System.IO.File.Exists(ShieldWorn)) Bake(ShieldSource, ShieldWorn, Shield);
            Assign(LanceMaterial, LanceWar);
            Assign(ShieldMaterial, ShieldWorn);
        }

        static void Assign(string materialPath, string texturePath)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (mat == null || tex == null)
            {
                Debug.LogError($"[Arena2KnightLook] Lipseste {(mat == null ? materialPath : texturePath)}.");
                return;
            }
            if (mat.GetTexture("_BaseMap") == tex) return;
            mat.SetTexture("_BaseMap", tex);
            EditorUtility.SetDirty(mat);
        }

        delegate void Recolor(Color[] px, int width, int height);

        static void Bake(string source, string target, Recolor recolor)
        {
            if (!System.IO.File.Exists(source))
            {
                Debug.LogError($"[Arena2KnightLook] Lipseste {source}.");
                return;
            }
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(System.IO.File.ReadAllBytes(source));
            Color[] px = tex.GetPixels();
            recolor(px, tex.width, tex.height);
            tex.SetPixels(px);
            tex.Apply();
            System.IO.File.WriteAllBytes(target, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter from = AssetImporter.GetAtPath(source) as TextureImporter;
            TextureImporter to = AssetImporter.GetAtPath(target) as TextureImporter;
            if (from != null && to != null)
            {
                TextureImporterSettings settings = new TextureImporterSettings();
                from.ReadTextureSettings(settings);
                to.SetTextureSettings(settings);
                to.SetPlatformTextureSettings(from.GetDefaultPlatformTextureSettings());
                to.SaveAndReimport();
            }
        }

        // ------------------------------------------------------------------ lancea

        static void Lance(Color[] px, int width, int height)
        {
            Kind[] kinds = new Kind[px.Length];
            float[] sum = new float[7];
            int[] count = new int[7];
            for (int i = 0; i < px.Length; i++)
            {
                kinds[i] = LanceKind(px[i], Uv(i, width, height));
                sum[(int)kinds[i]] += Luma(px[i]);
                count[(int)kinds[i]]++;
            }
            for (int i = 0; i < px.Length; i++)
            {
                Kind kind = kinds[i];
                if (kind == Kind.Keep) continue;
                Vector2 uv = Uv(i, width, height);
                Color c = px[i];
                float detail = Detail(c, sum[(int)kind], count[(int)kind]);
                Color o;
                switch (kind)
                {
                    case Kind.Wood:
                    case Kind.Stripe:
                        o = Wood * Mathf.Lerp(1f, detail, 0.35f) * Grain(uv);
                        break;
                    case Kind.OldWood:
                        o = Color.Lerp(c, Wood, 0.6f) * 0.8f;
                        break;
                    case Kind.Iron:
                        o = Iron * Mathf.Lerp(1f, detail, 0.8f) * Grime(uv, 0.8f);
                        break;
                    case Kind.Steel:
                        float g = Luma(c) * Blacken;
                        o = new Color(g * 0.97f, g * 0.97f, g) * Grime(uv, 0.85f);
                        break;
                    default:
                        o = Pennon * Mathf.Lerp(1f, detail, 0.7f) * Grime(uv, 0.7f);
                        break;
                }
                bool hole = kind == Kind.Pennon && Mathf.PerlinNoise(uv.x * 38f + 3.1f, uv.y * 38f + 7.7f) > PennonHoles;
                o.a = hole ? 0f : c.a;
                px[i] = o;
            }
        }

        static Kind LanceKind(Color c, Vector2 uv)
        {
            if (c.a < 0.5f) return Kind.Keep;
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);
            bool red = (h < 0.035f || h > 0.95f) && s > 0.35f;
            if (red) return PennonArea.Contains(uv) ? Kind.Pennon : Kind.Stripe;
            if (h >= 0.07f && h <= 0.18f && s >= 0.2f && s <= 0.7f && v > 0.62f) return Kind.Iron;
            if (h >= 0.035f && h <= 0.14f && s >= 0.25f && v < 0.62f) return Kind.OldWood;
            if (s < 0.3f && c.r - c.b > 0.035f && v > 0.3f) return Kind.Wood;
            if (s < 0.2f) return Kind.Steel;
            return Kind.Keep;
        }

        // ------------------------------------------------------------------ scutul

        static void Shield(Color[] px, int width, int height)
        {
            bool[] white = new bool[px.Length];
            float sum = 0f;
            int count = 0;
            for (int i = 0; i < px.Length; i++)
            {
                Color c = px[i];
                Vector2 uv = Uv(i, width, height);
                if (c.a < 0.5f || !(ShieldFace.Contains(uv) || ShieldTip.Contains(uv))) continue;
                float h, s, v;
                Color.RGBToHSV(c, out h, out s, out v);
                if (s >= 0.25f || v <= 0.4f || c.b > c.r + 0.01f) continue;
                white[i] = true;
                sum += Luma(c);
                count++;
            }
            for (int i = 0; i < px.Length; i++)
            {
                if (!white[i]) continue;
                Vector2 uv = Uv(i, width, height);
                Color c = px[i];
                Color o = OffWhite * Mathf.Lerp(1f, Detail(c, sum, count), 0.9f) * Grime(uv, 0.72f);
                // pete de funingine si urme de maini spre margini
                float soot = Mathf.PerlinNoise(uv.x * 7f + 11f, uv.y * 7f + 5f);
                o = Color.Lerp(o, o * new Color(0.7f, 0.66f, 0.6f), Mathf.Clamp01((soot - 0.55f) * 3f));
                o.a = c.a;
                px[i] = o;
            }
        }

        // ------------------------------------------------------------------ utilitare

        static Vector2 Uv(int i, int width, int height) => new Vector2((i % width + 0.5f) / width, (i / width + 0.5f) / height);

        static float Luma(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

        // variatia locala a pixelului fata de media clasei lui (pastreaza zgarieturile si umbrele din textura)
        static float Detail(Color c, float sum, int count)
        {
            float mean = count > 0 ? sum / count : 0.5f;
            return Mathf.Clamp(Luma(c) / Mathf.Max(0.02f, mean), 0.55f, 1.45f);
        }

        // fibra lemnului in ambele directii, insulele UV ale cozii sunt si orizontale si verticale
        static float Grain(Vector2 uv)
        {
            float a = Mathf.PerlinNoise(uv.x * 140f, uv.y * 9f);
            float b = Mathf.PerlinNoise(uv.x * 9f + 40f, uv.y * 140f);
            return 0.82f + 0.36f * (a * b + 0.25f * Mathf.PerlinNoise(uv.x * 30f, uv.y * 30f));
        }

        static float Grime(Vector2 uv, float min)
        {
            float n = 0.6f * Mathf.PerlinNoise(uv.x * 11f + 2f, uv.y * 11f + 9f) + 0.4f * Mathf.PerlinNoise(uv.x * 47f, uv.y * 47f + 3f);
            return Mathf.Lerp(min, 1f, n);
        }
    }
}
