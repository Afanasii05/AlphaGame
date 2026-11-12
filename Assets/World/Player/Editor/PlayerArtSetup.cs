using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Alpha.World.EditorTools
{
    // Importul modelului rafinat al jucatorului: copiaza exportul din Art/Blender (doar daca lipseste),
    // seteaza importul texturilor si al FBX-ului ca la Lich_Character si construieste materialele URP/Lit.
    public static class PlayerArtSetup
    {
        public const string Folder = "Assets/World/Player";
        public const string ArtFolder = Folder + "/Art";
        public const string TextureFolder = ArtFolder + "/Textures";
        public const string MaterialFolder = Folder + "/Materials";
        public const string ModelPath = ArtFolder + "/SK_Lich_PlayerRefined.fbx";
        public const string OriginalModelPath = "Assets/Character/MainPlayerModel/Assets/Character/Lich_Character.fbx";
        const string ExportFolder = "Art/Blender/Export/PlayerRefined";
        const string LitShader = "Universal Render Pipeline/Lit";

        static readonly string[] Parts = { "Armour", "Cloth", "Bone", "Gear" };
        static readonly string[] Glowing = { "Bone", "Gear" };

        // citirea din joc ca la originalul echipei: corp aproape negru, os palid si cald, semne magenta, ochi verzi.
        // texturile exportate raman neatinse, totul e din tint-ul materialelor
        static readonly Dictionary<string, Color> BaseTint = new Dictionary<string, Color>
        {
            { "Armour", new Color(0.27f, 0.27f, 0.36f) },
            { "Cloth", new Color(0.5f, 0.4f, 0.62f) },
            { "Bone", new Color(1.75f, 1.62f, 1.38f) },
            { "Gear", new Color(0.85f, 0.8f, 0.9f) },
        };
        static readonly Dictionary<string, Color> EmissionTint = new Dictionary<string, Color>
        {
            { "Bone", new Color(0.25f, 1.1f, 0.45f) },
            { "Gear", new Color(2.4f, 0.35f, 2.1f) },
        };
        static readonly Dictionary<string, float> Occlusion = new Dictionary<string, float> { { "Bone", 0.55f } };

        [MenuItem("Tools/Alpha/World/Player Skin/Refresh Art From Export")]
        static void RefreshFromExport()
        {
            CopyExport(true);
            Run();
        }

        public static Mesh Run()
        {
            EnsureFolder(Folder, "Art");
            EnsureFolder(ArtFolder, "Textures");
            EnsureFolder(Folder, "Materials");
            CopyExport(false);

            Dictionary<string, Material> materials = new Dictionary<string, Material>();
            foreach (string part in Parts) materials["M_LichRefined_" + part] = BuildMaterial(part);
            Mesh mesh = ConfigureModel(materials);
            AssetDatabase.SaveAssets();
            return mesh;
        }

        // ------------------------------------------------------------------ copiere

        static void CopyExport(bool overwrite)
        {
            string project = Directory.GetParent(Application.dataPath).FullName;
            string source = Path.Combine(project, ExportFolder);
            if (!Directory.Exists(source))
            {
                Debug.LogWarning($"[PlayerArtSetup] Nu exista {ExportFolder}, folosesc fisierele deja copiate.");
                return;
            }
            bool copied = false;
            foreach (string file in Directory.GetFiles(source))
            {
                string name = Path.GetFileName(file);
                string target = name.EndsWith(".fbx") ? ModelPath : name.EndsWith(".png") ? $"{TextureFolder}/{name}" : null;
                if (target == null) continue;
                string full = Path.Combine(project, target);
                if (File.Exists(full) && !overwrite) continue;
                File.Copy(file, full, true);
                copied = true;
            }
            if (copied) AssetDatabase.Refresh();
        }

        // ------------------------------------------------------------------ materiale

        static Material BuildMaterial(string part)
        {
            string prefix = $"{TextureFolder}/T_LichRefined_{part}";
            Texture2D baseColor = ImportTexture(prefix + "_BaseColor.png", TextureImporterType.Default, true);
            Texture2D normal = ImportTexture(prefix + "_Normal.png", TextureImporterType.NormalMap, false);
            Texture2D ms = ImportTexture(prefix + "_MetallicSmoothness.png", TextureImporterType.Default, false);
            Texture2D ao = ImportTexture(prefix + "_AO.png", TextureImporterType.Default, false);
            bool glows = System.Array.IndexOf(Glowing, part) >= 0;
            Texture2D emission = glows ? ImportTexture(prefix + "_Emission.png", TextureImporterType.Default, true) : null;

            Material mat = EnsureLit("M_LichRefined_" + part);
            mat.SetFloat("_WorkflowMode", 1f);
            mat.SetFloat("_Surface", 0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetFloat("_Cull", (float)CullMode.Back);
            mat.SetTexture("_BaseMap", baseColor);
            mat.SetColor("_BaseColor", BaseTint.ContainsKey(part) ? BaseTint[part] : Color.white);
            mat.SetTexture("_MetallicGlossMap", ms);
            mat.SetFloat("_Metallic", 1f);
            mat.SetFloat("_Smoothness", 1f);
            mat.SetFloat("_SmoothnessTextureChannel", 0f);
            Keyword(mat, "_METALLICSPECGLOSSMAP", ms != null);
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", 1f);
            Keyword(mat, "_NORMALMAP", normal != null);
            mat.SetTexture("_OcclusionMap", ao);
            mat.SetFloat("_OcclusionStrength", Occlusion.ContainsKey(part) ? Occlusion[part] : 1f);
            Keyword(mat, "_OCCLUSIONMAP", ao != null);

            mat.SetTexture("_EmissionMap", emission);
            mat.SetColor("_EmissionColor", emission != null && EmissionTint.ContainsKey(part) ? EmissionTint[part] : Color.black);
            Keyword(mat, "_EMISSION", emission != null);
            mat.globalIlluminationFlags = emission != null ? MaterialGlobalIlluminationFlags.RealtimeEmissive : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material EnsureLit(string name)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Shader shader = Shader.Find(LitShader);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }
            mat.enableInstancing = true;
            return mat;
        }

        static Texture2D ImportTexture(string path, TextureImporterType type, bool srgb)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[PlayerArtSetup] Lipseste textura {path}");
                return null;
            }
            bool hq = path.EndsWith("_MetallicSmoothness.png");
            TextureImporterCompression compression = hq ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Compressed;
            bool changed = importer.textureType != type || importer.sRGBTexture != srgb || importer.maxTextureSize != 2048
                           || importer.textureCompression != compression || importer.alphaSource != (hq ? TextureImporterAlphaSource.FromInput : importer.alphaSource);
            importer.textureType = type;
            importer.sRGBTexture = srgb;
            importer.maxTextureSize = 2048;
            importer.textureCompression = compression;
            if (hq) importer.alphaSource = TextureImporterAlphaSource.FromInput;
            if (changed) importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ------------------------------------------------------------------ model

        // aceleasi unitati si axe ca Lich_Character.fbx, altfel bindpose-urile nu se potrivesc pe scheletul echipei
        static Mesh ConfigureModel(Dictionary<string, Material> materials)
        {
            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            ModelImporter original = AssetImporter.GetAtPath(OriginalModelPath) as ModelImporter;
            if (importer == null || original == null)
            {
                Debug.LogError($"[PlayerArtSetup] Lipseste {(importer == null ? ModelPath : OriginalModelPath)}");
                return null;
            }
            Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(OriginalModelPath);
            string before = EditorJsonUtility.ToJson(importer);

            importer.globalScale = original.globalScale;
            importer.useFileScale = original.useFileScale;
            importer.useFileUnits = original.useFileUnits;
            importer.bakeAxisConversion = original.bakeAxisConversion;
            importer.preserveHierarchy = original.preserveHierarchy;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.animationType = ModelImporterAnimationType.Human;
            if (avatar != null)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = avatar;
            }
            else importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            foreach (KeyValuePair<string, Material> pair in materials)
                if (pair.Value != null) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), pair.Key), pair.Value);

            if (EditorJsonUtility.ToJson(importer) != before) importer.SaveAndReimport();
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            SkinnedMeshRenderer body = model != null ? model.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
            return body != null ? body.sharedMesh : null;
        }

        static void Keyword(Material mat, string keyword, bool on)
        {
            if (on) mat.EnableKeyword(keyword);
            else mat.DisableKeyword(keyword);
        }

        static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
