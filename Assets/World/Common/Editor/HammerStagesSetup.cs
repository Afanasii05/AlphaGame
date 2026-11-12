using UnityEditor;
using UnityEngine;

namespace Alpha.World.EditorTools
{
    // Pregateste arta pentru etapele ciocanului: setari de import identice cu Sword.fbx,
    // texturi (normal / liniare / sRGB) si materialele URP Lit legate pe sloturile din FBX.
    public static class HammerStagesSetup
    {
        public const string Folder = "Assets/World/Common/Art/HammerStages";
        public const string MaterialFolder = Folder + "/Materials";
        const string SwordFbx = "Assets/Weapons/Sword_Unity/Assets/Sword/Sword.fbx";
        const string SwordMaterials = "Assets/Weapons/Sword_Unity/Assets/Sword/Materials/";
        const string LitShader = "Universal Render Pipeline/Lit";

        public static readonly string[] Models =
        {
            "SM_Hammer_Stage1", "SM_Hammer_Stage2", "SM_Sword_Plain",
            "SM_HammerShell_0to1_Chunks", "SM_HammerShell_1to2_Chunks", "SM_HammerShell_2to3_Chunks"
        };

        // nume material, intensitate emisie
        static readonly (string name, float emission)[] Materials =
        {
            ("HammerStage1", 4f), ("HammerStage2", 2f),
            ("HammerShell_0to1", 4f), ("HammerShell_1to2", 2f), ("HammerShell_2to3", 1.5f)
        };

        // sloturi care trebuie legate de materialele existente ale sabiei
        static readonly string[] SharedSlots =
        {
            "M_Gravel043_Stone", "M_Metal046B_Guard", "M_Metal062C_GripWrap", "M_Metal062C_Blade", "M_Rune_Glow"
        };

        [MenuItem("Tools/Alpha/World/Setup Hammer Stages")]
        public static void Setup()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder(Folder, "Materials");
            foreach (var (name, _) in Materials) SetupTextures(name);
            foreach (var (name, emission) in Materials) BuildMaterial(name, emission);
            AssetDatabase.SaveAssets();
            foreach (string model in Models) SetupModel(model);
            AssetDatabase.SaveAssets();
            Debug.Log("[HammerStagesSetup] Etapele ciocanului sunt pregatite.");
        }

        // valorile etapelor in spatiul lui Visual (Y in sus, inainte de scara lui Visual)
        public static void Configure(Alpha.World.HammerStages stages, ParticleSystem burst)
        {
            var so = new SerializedObject(stages);
            SerializedProperty list = so.FindProperty("stages");
            list.arraySize = 4;
            SetStage(list.GetArrayElementAtIndex(0), null, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero);
            SetStage(list.GetArrayElementAtIndex(1), Model("SM_Hammer_Stage1"),
                new Vector3(0.009f, 1.26f, 0.022f), new Vector3(0.96f, 0.52f, 0.34f),
                new Vector3(0.47f, 1.2f, 0.022f), new Vector3(0.009f, 1.5f, 0.022f));
            SetStage(list.GetArrayElementAtIndex(2), Model("SM_Hammer_Stage2"),
                new Vector3(0.007f, 0.85f, -0.006f), new Vector3(0.3f, 0.8f, 0.35f),
                new Vector3(0.1f, 1f, 0f), new Vector3(0.009f, 1.36f, 0f));
            SetStage(list.GetArrayElementAtIndex(3), null,
                new Vector3(0.007f, 0.85f, -0.006f), new Vector3(0.3f, 0.8f, 0.35f),
                new Vector3(0.1f, 0.9f, 0f), new Vector3(0.007f, 1.2f, 0f));

            SerializedProperty shells = so.FindProperty("shells");
            shells.arraySize = 3;
            shells.GetArrayElementAtIndex(0).objectReferenceValue = Model("SM_HammerShell_0to1_Chunks");
            shells.GetArrayElementAtIndex(1).objectReferenceValue = Model("SM_HammerShell_1to2_Chunks");
            shells.GetArrayElementAtIndex(2).objectReferenceValue = Model("SM_HammerShell_2to3_Chunks");
            so.FindProperty("burstPrefab").objectReferenceValue = burst;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetStage(SerializedProperty stage, GameObject model, Vector3 center, Vector3 size, Vector3 side, Vector3 top)
        {
            stage.FindPropertyRelative("model").objectReferenceValue = model;
            stage.FindPropertyRelative("headCenter").vector3Value = center;
            stage.FindPropertyRelative("headSize").vector3Value = size;
            stage.FindPropertyRelative("impactSide").vector3Value = side;
            stage.FindPropertyRelative("impactTop").vector3Value = top;
        }

        public static GameObject Model(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/" + name + ".fbx");

        static void SetupTextures(string name)
        {
            Texture(name, "BaseColor", TextureImporterType.Default, true);
            Texture(name, "Normal", TextureImporterType.NormalMap, false);
            Texture(name, "MetallicSmoothness", TextureImporterType.Default, false);
            Texture(name, "AO", TextureImporterType.Default, false);
            Texture(name, "Emission", TextureImporterType.Default, true);
        }

        static void Texture(string name, string map, TextureImporterType type, bool srgb)
        {
            string path = Folder + "/T_" + name + "_" + map + ".png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[HammerStagesSetup] Lipseste textura " + path);
                return;
            }
            bool dirty = importer.textureType != type || importer.sRGBTexture != srgb || importer.mipmapEnabled != true;
            if (!dirty) return;
            importer.textureType = type;
            importer.sRGBTexture = srgb;
            importer.mipmapEnabled = true;
            importer.alphaIsTransparency = false;
            importer.anisoLevel = 4;
            importer.SaveAndReimport();
        }

        static Texture2D Tex(string name, string map) => AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/T_" + name + "_" + map + ".png");

        static void BuildMaterial(string name, float emission)
        {
            string path = MaterialFolder + "/M_" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find(LitShader));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = Shader.Find(LitShader);
            mat.name = "M_" + name;
            mat.SetTexture("_BaseMap", Tex(name, "BaseColor"));
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_BumpMap", Tex(name, "Normal"));
            mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_MetallicGlossMap", Tex(name, "MetallicSmoothness"));
            mat.SetFloat("_Metallic", 1f);
            mat.SetFloat("_Smoothness", 1f);
            mat.SetFloat("_SmoothnessTextureChannel", 0f);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetTexture("_OcclusionMap", Tex(name, "AO"));
            mat.SetFloat("_OcclusionStrength", 1f);
            mat.EnableKeyword("_OCCLUSIONMAP");
            mat.SetTexture("_EmissionMap", Tex(name, "Emission"));
            mat.SetColor("_EmissionColor", Color.white * emission);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
        }

        // copiem setarile de import care conteaza din Sword.fbx ca etapele sa stea in acelasi spatiu
        static void SetupModel(string name)
        {
            string path = Folder + "/" + name + ".fbx";
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            ModelImporter sword = AssetImporter.GetAtPath(SwordFbx) as ModelImporter;
            if (importer == null || sword == null) return;

            importer.globalScale = sword.globalScale;
            importer.useFileScale = sword.useFileScale;
            importer.bakeAxisConversion = sword.bakeAxisConversion;
            importer.importNormals = sword.importNormals;
            importer.importTangents = sword.importTangents;
            importer.meshCompression = sword.meshCompression;
            importer.isReadable = true;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;

            foreach (var (mat, _) in Materials)
                Remap(importer, "M_" + mat, AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/M_" + mat + ".mat"));
            foreach (string slot in SharedSlots)
                Remap(importer, slot, AssetDatabase.LoadAssetAtPath<Material>(SwordMaterials + slot + ".mat"));
            importer.SaveAndReimport();
        }

        static void Remap(ModelImporter importer, string slot, Material mat)
        {
            if (mat == null) return;
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), slot), mat);
        }
    }
}
