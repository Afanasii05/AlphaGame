using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Alpha.World.EditorTools
{
    // Steagurile cu ghicitoarea: setarile de import ale texturilor si cele 6 materiale URP/Lit.
    // FBX-urile si PNG-urile sursa raman neatinse; materialele se pun direct pe renderere din builder.
    public static class Arena1BannerSetup
    {
        public const string Folder = "Assets/World/Arena1/Art/Banners";
        const string MaterialFolder = Folder + "/Materials";
        const string LitShader = "Universal Render Pipeline/Lit";

        public static readonly string[] Names = { "01", "02", "03", "04", "Title" };

        public static string MeshPath(string name) => $"{Folder}/SM_RiddleBanner_{name}.fbx";

        // index 0..4 = panza pentru Names, ultimul = lemnul comun
        public static Material[] Build()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder(Folder, "Materials");

            Material[] result = new Material[Names.Length + 1];
            for (int i = 0; i < Names.Length; i++)
            {
                string normal = Names[i] == "Title" ? "Title" : "Line";
                result[i] = BuildMaterial("M_RiddleBanner_" + Names[i], Names[i], normal, true);
            }
            result[Names.Length] = BuildMaterial("M_RiddleBanner_Wood", "Wood", "Wood", false);
            AssetDatabase.SaveAssets();
            return result;
        }

        static Material BuildMaterial(string name, string texName, string normalName, bool cloth)
        {
            Texture2D baseColor = ImportTexture($"{Folder}/T_RiddleBanner_{texName}_BaseColor.png", TextureImporterType.Default, true, cloth);
            Texture2D metallic = ImportTexture($"{Folder}/T_RiddleBanner_{texName}_MetallicSmoothness.png", TextureImporterType.Default, false, false);
            Texture2D normal = ImportTexture($"{Folder}/T_RiddleBanner_{normalName}_Normal.png", TextureImporterType.NormalMap, false, false);

            string path = $"{MaterialFolder}/{name}.mat";
            Shader shader = Shader.Find(LitShader);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }

            mat.SetTexture("_BaseMap", baseColor);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_MetallicGlossMap", metallic);
            mat.SetFloat("_Metallic", 1f);
            mat.SetFloat("_Smoothness", 1f);
            mat.SetFloat("_SmoothnessTextureChannel", 0f);
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", 1f);
            mat.SetFloat("_Surface", 0f);
            mat.SetFloat("_ZWrite", 1f);
            if (metallic != null) mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            if (normal != null) mat.EnableKeyword("_NORMALMAP");

            if (cloth)
            {
                // panza: decupare dupa alpha, doar fata (spatele are acelasi mesh, nu dublam fetele)
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetFloat("_Cutoff", 0.5f);
                mat.SetFloat("_Cull", (float)CullMode.Back);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.SetOverrideTag("RenderType", "TransparentCutout");
                mat.renderQueue = (int)RenderQueue.AlphaTest;
            }
            else
            {
                mat.SetFloat("_AlphaClip", 0f);
                mat.SetFloat("_Cull", (float)CullMode.Back);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.SetOverrideTag("RenderType", "Opaque");
                mat.renderQueue = (int)RenderQueue.Geometry;
            }
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Texture2D ImportTexture(string path, TextureImporterType type, bool srgb, bool cutout)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[Arena1BannerSetup] Lipseste textura {path}");
                return null;
            }
            bool changed = importer.textureType != type || importer.sRGBTexture != srgb;
            importer.textureType = type;
            importer.sRGBTexture = srgb;
            if (cutout)
            {
                changed |= !importer.alphaIsTransparency || !importer.mipMapsPreserveCoverage || !Mathf.Approximately(importer.alphaTestReferenceValue, 0.5f);
                importer.alphaIsTransparency = true;
                importer.mipMapsPreserveCoverage = true;
                importer.alphaTestReferenceValue = 0.5f;
            }
            if (changed) importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
