using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace Alpha.World.EditorTools
{
    // Setarile de import pentru arta din Arena 2: cavalerul (Humanoid + clipuri), materialele URP ale kiturilor
    // de castel si de camp, remap-ul materialelor din FBX si straturile de teren pentru Campul Juramintelor Rupte.
    public static class Arena2ArtSetup
    {
        public const string ArtFolder = "Assets/World/Arena2/Art";
        public const string KnightFolder = ArtFolder + "/Knight";
        public const string CastleFolder = ArtFolder + "/Castle";
        public const string FieldFolder = ArtFolder + "/Field";
        public const string MaterialFolder = ArtFolder + "/Materials";
        public const string TerrainFolder = ArtFolder + "/Terrain";
        const string KnightMaterialFolder = KnightFolder + "/Materials";

        public const string KnightPath = KnightFolder + "/SK_OathboundKnight.fbx";
        public const string ThronePath = KnightFolder + "/SM_Throne.fbx";
        public const string LancePath = KnightFolder + "/SM_Knight_Lance.fbx";
        public const string ShieldPath = KnightFolder + "/SM_Knight_Shield.fbx";
        public const string ControllerPath = KnightFolder + "/AC_Knight_Throne.controller";
        public const string SitClip = "A_Knight_ThroneSit_Idle";
        public static readonly string[] Clips = { "A_Knight_ThroneRise", "A_Knight_ShieldWalk", "A_Knight_LanceSweep", SitClip, "A_Knight_Kneel_Defeated", "A_Knight_LanceThrust" };
        static readonly string[] LoopClips = { SitClip, "A_Knight_ShieldWalk" };

        const string LitShader = "Universal Render Pipeline/Lit";
        const string Tex = "Assets/World/Textures/";

        public static readonly string[] TerrainLayerNames = { "TL_A2_DeadGrass", "TL_A2_Mud", "TL_A2_Gravel", "TL_A2_Rock", "TL_A2_Paving", "TL_A2_Trampled", "TL_A2_Bog" };

        [MenuItem("Tools/Alpha/World/Setup Arena 2 Art")]
        public static void Run()
        {
            EnsureFolder(ArtFolder, "Materials");
            EnsureFolder(ArtFolder, "Terrain");
            EnsureFolder(KnightFolder, "Materials");

            Dictionary<string, Material> kit = BuildKitMaterials();
            Dictionary<string, Material> knight = BuildKnightMaterials();
            Arena2KnightLook.Apply();

            foreach (string folder in new[] { CastleFolder, FieldFolder })
                foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Dictionary<string, Material> map = kit;
                    if (path.EndsWith("SM_A2_Tapestry.fbx"))
                    {
                        map = new Dictionary<string, Material>(kit);
                        map["M_A2_BannerRed"] = kit["M_A2_Tapestry"];
                    }
                    ConfigureStatic(path, map);
                }
            foreach (string path in new[] { ThronePath, LancePath, ShieldPath }) ConfigureStatic(path, knight);

            Avatar avatar = ConfigureKnight(knight);
            ConfigureClips(avatar);
            BuildController();
            BuildTerrainLayers();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Arena2ArtSetup] Gata. Avatar cavaler valid: {(avatar != null && avatar.isValid && avatar.isHuman)}.");
        }

        // ------------------------------------------------------------------ cavalerul

        static Dictionary<string, Material> BuildKnightMaterials()
        {
            Dictionary<string, Material> map = new Dictionary<string, Material>();
            map["M_Knight_Armour"] = TexturedLit(KnightMaterialFolder, "M_Knight_Armour", KnightFolder + "/T_Knight_Armour", false, false);
            map["M_Knight_Cloth"] = TexturedLit(KnightMaterialFolder, "M_Knight_Cloth", KnightFolder + "/T_Knight_Cloth", true, true);
            map["M_Knight_Shield"] = TexturedLit(KnightMaterialFolder, "M_Knight_Shield", KnightFolder + "/T_KnightShield", false, false);
            map["M_Knight_Lance"] = TexturedLit(KnightMaterialFolder, "M_Knight_Lance", KnightFolder + "/T_KnightLance", true, true);
            map["M_Throne_Stone"] = TexturedLit(KnightMaterialFolder, "M_Throne_Stone", KnightFolder + "/T_Throne_Stone", false, false);
            map["M_Throne_Cloth"] = TexturedLit(KnightMaterialFolder, "M_Throne_Cloth", KnightFolder + "/T_Throne_Cloth", true, true);
            return map;
        }

        // materiale din setul _BaseColor / _Normal / _MetallicSmoothness (R metal, A neted) / _AO
        static Material TexturedLit(string folder, string name, string prefix, bool cutout, bool twoSided)
        {
            Texture2D baseColor = ImportTexture(prefix + "_BaseColor.png", TextureImporterType.Default, true, cutout);
            Texture2D normal = ImportTexture(prefix + "_Normal.png", TextureImporterType.NormalMap, false, false);
            Texture2D ms = ImportTexture(prefix + "_MetallicSmoothness.png", TextureImporterType.Default, false, false);
            Texture2D ao = ImportTexture(prefix + "_AO.png", TextureImporterType.Default, false, false);

            Material mat = EnsureLit(folder, name);
            mat.SetTexture("_BaseMap", baseColor);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_MetallicGlossMap", ms);
            mat.SetFloat("_Metallic", 1f);
            mat.SetFloat("_Smoothness", 1f);
            mat.SetFloat("_SmoothnessTextureChannel", 0f);
            Keyword(mat, "_METALLICSPECGLOSSMAP", ms != null);
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", 1f);
            Keyword(mat, "_NORMALMAP", normal != null);
            mat.SetTexture("_OcclusionMap", ao);
            mat.SetFloat("_OcclusionStrength", 1f);
            Keyword(mat, "_OCCLUSIONMAP", ao != null);
            Surface(mat, cutout, twoSided);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Avatar ConfigureKnight(Dictionary<string, Material> materials)
        {
            ModelImporter importer = AssetImporter.GetAtPath(KnightPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[Arena2ArtSetup] Lipseste {KnightPath}");
                return null;
            }
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            Remap(importer, materials);

            // maparea mixamorig e explicita, ca avatarul sa nu depinda de ghicitul automat
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(KnightPath);
            HumanDescription description = importer.humanDescription;
            description.human = HumanBones(model);
            description.skeleton = Skeleton(model);
            description.upperArmTwist = 0.5f;
            description.lowerArmTwist = 0.5f;
            description.upperLegTwist = 0.5f;
            description.lowerLegTwist = 0.5f;
            description.armStretch = 0.05f;
            description.legStretch = 0.05f;
            description.feetSpacing = 0f;
            description.hasTranslationDoF = false;
            importer.humanDescription = description;
            importer.SaveAndReimport();

            Avatar avatar = null;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(KnightPath))
                if (o is Avatar a) avatar = a;
            if (avatar == null || !avatar.isValid) Debug.LogError("[Arena2ArtSetup] Avatarul cavalerului nu e valid.");
            return avatar;
        }

        static readonly string[,] MixamoBones =
        {
            { "Hips", "Hips" }, { "Spine", "Spine" }, { "Chest", "Spine1" }, { "UpperChest", "Spine2" }, { "Neck", "Neck" }, { "Head", "Head" },
            { "LeftShoulder", "LeftShoulder" }, { "LeftUpperArm", "LeftArm" }, { "LeftLowerArm", "LeftForeArm" }, { "LeftHand", "LeftHand" },
            { "RightShoulder", "RightShoulder" }, { "RightUpperArm", "RightArm" }, { "RightLowerArm", "RightForeArm" }, { "RightHand", "RightHand" },
            { "LeftUpperLeg", "LeftUpLeg" }, { "LeftLowerLeg", "LeftLeg" }, { "LeftFoot", "LeftFoot" }, { "LeftToes", "LeftToeBase" },
            { "RightUpperLeg", "RightUpLeg" }, { "RightLowerLeg", "RightLeg" }, { "RightFoot", "RightFoot" }, { "RightToes", "RightToeBase" },
            { "Left Thumb Proximal", "LeftHandThumb1" }, { "Left Thumb Intermediate", "LeftHandThumb2" }, { "Left Thumb Distal", "LeftHandThumb3" },
            { "Left Index Proximal", "LeftHandIndex1" }, { "Left Index Intermediate", "LeftHandIndex2" }, { "Left Index Distal", "LeftHandIndex3" },
            { "Left Middle Proximal", "LeftHandMiddle1" }, { "Left Middle Intermediate", "LeftHandMiddle2" }, { "Left Middle Distal", "LeftHandMiddle3" },
            { "Left Ring Proximal", "LeftHandRing1" }, { "Left Ring Intermediate", "LeftHandRing2" }, { "Left Ring Distal", "LeftHandRing3" },
            { "Left Little Proximal", "LeftHandPinky1" }, { "Left Little Intermediate", "LeftHandPinky2" }, { "Left Little Distal", "LeftHandPinky3" },
            { "Right Thumb Proximal", "RightHandThumb1" }, { "Right Thumb Intermediate", "RightHandThumb2" }, { "Right Thumb Distal", "RightHandThumb3" },
            { "Right Index Proximal", "RightHandIndex1" }, { "Right Index Intermediate", "RightHandIndex2" }, { "Right Index Distal", "RightHandIndex3" },
            { "Right Middle Proximal", "RightHandMiddle1" }, { "Right Middle Intermediate", "RightHandMiddle2" }, { "Right Middle Distal", "RightHandMiddle3" },
            { "Right Ring Proximal", "RightHandRing1" }, { "Right Ring Intermediate", "RightHandRing2" }, { "Right Ring Distal", "RightHandRing3" },
            { "Right Little Proximal", "RightHandPinky1" }, { "Right Little Intermediate", "RightHandPinky2" }, { "Right Little Distal", "RightHandPinky3" }
        };

        static HumanBone[] HumanBones(GameObject model)
        {
            HashSet<string> names = new HashSet<string>();
            foreach (Transform t in model.GetComponentsInChildren<Transform>(true)) names.Add(t.name);
            List<HumanBone> bones = new List<HumanBone>();
            for (int i = 0; i < MixamoBones.GetLength(0); i++)
            {
                string bone = "mixamorig:" + MixamoBones[i, 1];
                if (!names.Contains(bone)) continue;
                HumanBone hb = new HumanBone { humanName = MixamoBones[i, 0], boneName = bone };
                hb.limit.useDefaultValues = true;
                bones.Add(hb);
            }
            return bones.ToArray();
        }

        static SkeletonBone[] Skeleton(GameObject model)
        {
            List<SkeletonBone> bones = new List<SkeletonBone>();
            foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
                bones.Add(new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale });
            return bones.ToArray();
        }

        static void ConfigureClips(Avatar avatar)
        {
            foreach (string clip in Clips)
            {
                string path = $"{KnightFolder}/{clip}.fbx";
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                {
                    Debug.LogError($"[Arena2ArtSetup] Lipseste {path}");
                    continue;
                }
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = avatar;
                // clipurile au un singur nod radacina (armatura); fara asta Unity il comaseaza si pierde rotatia de axa
                importer.preserveHierarchy = true;
                importer.importAnimation = true;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;

                ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
                bool loop = System.Array.IndexOf(LoopClips, clip) >= 0;
                for (int i = 0; i < clips.Length; i++)
                {
                    clips[i].name = clip;
                    clips[i].loopTime = loop;
                    clips[i].loopPose = loop;
                    // radacina ramane pe loc, inaltimea si orientarea sunt cele din clip (sezutul pe tron, ridicarea)
                    clips[i].lockRootRotation = true;
                    clips[i].keepOriginalOrientation = true;
                    clips[i].lockRootHeightY = true;
                    clips[i].keepOriginalPositionY = true;
                    clips[i].heightFromFeet = false;
                    // mersul se face din cod, asa ca pasii raman pe loc
                    clips[i].lockRootPositionXZ = clip != "A_Knight_ShieldWalk";
                    clips[i].keepOriginalPositionXZ = true;
                }
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
        }

        public static AnimationClip LoadClip(string name)
        {
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath($"{KnightFolder}/{name}.fbx"))
                if (o is AnimationClip c && !c.name.StartsWith("__preview")) return c;
            return null;
        }

        // controller minim: starea implicita e sezutul pe tron; celelalte clipuri sunt stari gata de folosit pentru lupta
        static void BuildController()
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            controller.AddParameter("Rise", AnimatorControllerParameterType.Trigger);
            AnimatorState sit = null, rise = null;
            int row = 0;
            foreach (string name in Clips)
            {
                AnimationClip clip = LoadClip(name);
                if (clip == null) continue;
                AnimatorState state = sm.AddState(name.Replace("A_Knight_", ""), new Vector3(300f, 60f * row++, 0f));
                state.motion = clip;
                if (name == SitClip) sit = state;
                if (name == "A_Knight_ThroneRise") rise = state;
            }
            if (sit != null) sm.defaultState = sit;
            if (sit != null && rise != null)
            {
                AnimatorStateTransition t = sit.AddTransition(rise);
                t.hasExitTime = false;
                t.duration = 0.25f;
                t.AddCondition(AnimatorConditionMode.If, 0f, "Rise");
            }
            EditorUtility.SetDirty(controller);
        }

        // ------------------------------------------------------------------ kituri statice

        static void ConfigureStatic(string path, Dictionary<string, Material> materials)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return;
            // conversia de axe intra in mesh: radacina are rotatie identitate si yaw-ul din builder e curat
            importer.bakeAxisConversion = true;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            Remap(importer, materials);
            importer.SaveAndReimport();
        }

        static void Remap(ModelImporter importer, Dictionary<string, Material> materials)
        {
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(importer.assetPath))
            {
                if (!(o is Material)) continue;
                if (materials.TryGetValue(o.name, out Material mat) && mat != null)
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), o.name), mat);
            }
            foreach (KeyValuePair<AssetImporter.SourceAssetIdentifier, Object> pair in importer.GetExternalObjectMap())
                if (pair.Key.type == typeof(Material) && materials.TryGetValue(pair.Key.name, out Material mat) && mat != null && pair.Value != mat)
                    importer.AddRemap(pair.Key, mat);
        }

        static Dictionary<string, Material> BuildKitMaterials()
        {
            Dictionary<string, Material> map = new Dictionary<string, Material>();
            // piatra castelului: blocuri cioplite cu muschi pe fetele de sus (shader-ul echipei din World/Materials)
            map["M_A2_CastleStone"] = StoneMaterial("M_A2_CastleStone", "Bricks112A", new Color(0.78f, 0.79f, 0.8f), 0.34f, 0.22f, 0.12f);
            map["M_A2_CastleStoneDark"] = StoneMaterial("M_A2_CastleStoneDark", "Rock060", new Color(0.52f, 0.53f, 0.55f), 0.5f, 0.3f, 0.1f);
            map["M_A2_CastleRoof"] = StoneMaterial("M_A2_CastleRoof", "Rock063", new Color(0.36f, 0.38f, 0.42f), 0.6f, 0.12f, 0.2f);
            map["M_A2_Engraving"] = StoneMaterial("M_A2_Engraving", "Limestone", new Color(0.62f, 0.62f, 0.6f), 1f, 0.15f, 0.15f);

            map["M_A2_Wood"] = PbrMaterial("M_A2_Wood", "Wood075", new Color(0.62f, 0.56f, 0.5f), 1f, 0f, 0.18f);
            map["M_A2_WoodCharred"] = PbrMaterial("M_A2_WoodCharred", "WoodOld", new Color(0.13f, 0.12f, 0.11f), 1f, 0f, 0.08f);
            map["M_A2_Iron"] = PbrMaterial("M_A2_Iron", "Metal046B", new Color(0.46f, 0.45f, 0.44f), 1f, 0.85f, 0.42f);

            map["M_A2_Fletching"] = Plain("M_A2_Fletching", new Color(0.55f, 0.52f, 0.47f), 0f, 0.1f, true);
            map["M_A2_Crow"] = Plain("M_A2_Crow", new Color(0.035f, 0.035f, 0.04f), 0f, 0.42f, true);
            map["M_A2_ShieldPaint"] = PbrMaterial("M_A2_ShieldPaint", "WoodOld", new Color(0.46f, 0.12f, 0.1f), 1f, 0f, 0.2f);
            map["M_A2_ShieldPaint_Blue"] = PbrMaterial("M_A2_ShieldPaint_Blue", "WoodOld", new Color(0.16f, 0.22f, 0.34f), 1f, 0f, 0.2f);
            map["M_A2_ShieldPaint_Ochre"] = PbrMaterial("M_A2_ShieldPaint_Ochre", "WoodOld", new Color(0.55f, 0.42f, 0.18f), 1f, 0f, 0.2f);

            Material glow = Plain("M_A2_WindowGlow", new Color(0.05f, 0.03f, 0.02f), 0f, 0.1f, false);
            glow.SetColor("_EmissionColor", new Color(1f, 0.55f, 0.22f) * 2.2f);
            glow.EnableKeyword("_EMISSION");
            glow.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            map["M_A2_WindowGlow"] = glow;

            map["M_A2_BannerRed"] = ClothMaterial("M_A2_BannerRed", FieldFolder + "/Textures/T_A2_Banner_BaseColor.png");
            map["M_A2_Tapestry"] = ClothMaterial("M_A2_Tapestry", FieldFolder + "/Textures/T_A2_Tapestry_BaseColor.png");
            return map;
        }

        // copie a materialului de piatra al echipei (shader cu muschi), cu textura si tenta proprii
        static Material StoneMaterial(string name, string texture, Color tint, float tiling, float moss, float smooth)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material source = AssetDatabase.LoadAssetAtPath<Material>("Assets/World/Materials/M_RuinStone.mat");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = source != null ? new Material(source) : new Material(Shader.Find(LitShader));
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (source != null)
            {
                mat.shader = source.shader;
                mat.CopyPropertiesFromMaterial(source);
            }
            Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Tex}{texture}/{texture}_Color.png");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Tex}{texture}/{texture}_NormalGL.png");
            Texture2D ao = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Tex}{texture}/{texture}_AmbientOcclusion.png");
            Vector2 scale = new Vector2(tiling, tiling);
            SetTex(mat, "_BaseMap", color, scale);
            SetTex(mat, "_BumpMap", normal, scale);
            SetTex(mat, "_OcclusionMap", ao, scale);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_MossAmount")) mat.SetFloat("_MossAmount", moss);
            if (mat.HasProperty("_MossColor")) mat.SetColor("_MossColor", new Color(0.36f, 0.4f, 0.3f));
            Keyword(mat, "_NORMALMAP", normal != null);
            Keyword(mat, "_OCCLUSIONMAP", ao != null);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material PbrMaterial(string name, string texture, Color tint, float tiling, float metallic, float smooth)
        {
            Material mat = EnsureLit(MaterialFolder, name);
            Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Tex}{texture}/{texture}_Color.png");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Tex}{texture}/{texture}_NormalGL.png");
            Texture2D ao = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Tex}{texture}/{texture}_AmbientOcclusion.png");
            Vector2 scale = new Vector2(tiling, tiling);
            SetTex(mat, "_BaseMap", color, scale);
            SetTex(mat, "_BumpMap", normal, scale);
            SetTex(mat, "_OcclusionMap", ao, scale);
            mat.SetTexture("_MetallicGlossMap", null);
            Keyword(mat, "_METALLICSPECGLOSSMAP", false);
            Keyword(mat, "_NORMALMAP", normal != null);
            Keyword(mat, "_OCCLUSIONMAP", ao != null);
            mat.SetColor("_BaseColor", tint);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smooth);
            mat.SetFloat("_BumpScale", 1f);
            Surface(mat, false, false);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material Plain(string name, Color color, float metallic, float smooth, bool twoSided)
        {
            Material mat = EnsureLit(MaterialFolder, name);
            mat.SetTexture("_BaseMap", null);
            mat.SetTexture("_BumpMap", null);
            mat.SetTexture("_MetallicGlossMap", null);
            mat.SetTexture("_OcclusionMap", null);
            Keyword(mat, "_NORMALMAP", false);
            Keyword(mat, "_METALLICSPECGLOSSMAP", false);
            Keyword(mat, "_OCCLUSIONMAP", false);
            Keyword(mat, "_EMISSION", false);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smooth);
            Surface(mat, false, twoSided);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // panza rupta: decupare dupa alpha, ambele fete (steagurile se vad din ambele parti)
        static Material ClothMaterial(string name, string texturePath)
        {
            Texture2D baseColor = ImportTexture(texturePath, TextureImporterType.Default, true, true);
            Material mat = EnsureLit(MaterialFolder, name);
            mat.SetTexture("_BaseMap", baseColor);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_BumpMap", null);
            mat.SetTexture("_MetallicGlossMap", null);
            Keyword(mat, "_NORMALMAP", false);
            Keyword(mat, "_METALLICSPECGLOSSMAP", false);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.12f);
            Surface(mat, true, true);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ teren

        static void BuildTerrainLayers()
        {
            const string Env = "Assets/Environment/Terrain/";
            Layer("TL_A2_DeadGrass", Env + "TL_Grass007.terrainlayer", new Vector4(0.86f, 0.78f, 0.56f, 1f), 3f, 0.04f);
            Layer("TL_A2_Mud", "Assets/World/Terrain/TL_Ground068.terrainlayer", new Vector4(0.58f, 0.53f, 0.48f, 1f), 3.5f, 0.32f);
            Layer("TL_A2_Gravel", Env + "TL_Gravel043.terrainlayer", new Vector4(0.7f, 0.71f, 0.73f, 1f), 2.5f, 0.06f);
            Layer("TL_A2_Rock", Env + "TL_Rock064.terrainlayer", new Vector4(0.66f, 0.68f, 0.72f, 1f), 12f, 0.08f);
            Layer("TL_A2_Trampled", Env + "TL_Ground085.terrainlayer", new Vector4(0.72f, 0.67f, 0.56f, 1f), 4f, 0.05f);
            Layer("TL_A2_Bog", "Assets/World/Terrain/TL_Moss002.terrainlayer", new Vector4(0.5f, 0.55f, 0.46f, 1f), 3.5f, 0.38f);

            // dalele curtii: textura de pavaj a echipei ca strat de teren
            string path = $"{TerrainFolder}/TL_A2_Paving.terrainlayer";
            TerrainLayer paving = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (paving == null)
            {
                paving = new TerrainLayer();
                AssetDatabase.CreateAsset(paving, path);
            }
            paving.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + "Bricks112A/Bricks112A_Color.png");
            paving.normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + "Bricks112A/Bricks112A_NormalGL.png");
            paving.tileSize = new Vector2(3.2f, 3.2f);
            paving.diffuseRemapMax = new Vector4(0.7f, 0.7f, 0.7f, 1f);
            paving.smoothness = 0.1f;
            paving.metallic = 0f;
            EditorUtility.SetDirty(paving);
        }

        static void Layer(string name, string sourcePath, Vector4 tint, float tile, float smooth)
        {
            TerrainLayer source = AssetDatabase.LoadAssetAtPath<TerrainLayer>(sourcePath);
            if (source == null)
            {
                Debug.LogError($"[Arena2ArtSetup] Lipseste {sourcePath}");
                return;
            }
            string path = $"{TerrainFolder}/{name}.terrainlayer";
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, path);
            }
            layer.diffuseTexture = source.diffuseTexture;
            layer.normalMapTexture = source.normalMapTexture;
            layer.maskMapTexture = source.maskMapTexture;
            layer.normalScale = source.normalScale;
            layer.tileSize = new Vector2(tile, tile);
            layer.diffuseRemapMin = Vector4.zero;
            layer.diffuseRemapMax = tint;
            layer.smoothness = smooth;
            layer.metallic = 0f;
            EditorUtility.SetDirty(layer);
        }

        public static TerrainLayer[] LoadTerrainLayers()
        {
            TerrainLayer[] layers = new TerrainLayer[TerrainLayerNames.Length];
            for (int i = 0; i < layers.Length; i++)
                layers[i] = AssetDatabase.LoadAssetAtPath<TerrainLayer>($"{TerrainFolder}/{TerrainLayerNames[i]}.terrainlayer");
            return layers;
        }

        // ------------------------------------------------------------------ utilitare

        static Material EnsureLit(string folder, string name)
        {
            string path = $"{folder}/{name}.mat";
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
            mat.enableInstancing = true;
            return mat;
        }

        static void Surface(Material mat, bool cutout, bool twoSided)
        {
            mat.SetFloat("_Surface", 0f);
            mat.SetFloat("_ZWrite", 1f);
            mat.SetFloat("_Cull", (float)(twoSided ? CullMode.Off : CullMode.Back));
            mat.doubleSidedGI = twoSided;
            if (cutout)
            {
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetFloat("_Cutoff", 0.5f);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.SetOverrideTag("RenderType", "TransparentCutout");
                mat.renderQueue = (int)RenderQueue.AlphaTest;
            }
            else
            {
                mat.SetFloat("_AlphaClip", 0f);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.SetOverrideTag("RenderType", "Opaque");
                mat.renderQueue = (int)RenderQueue.Geometry;
            }
        }

        static void SetTex(Material mat, string property, Texture2D texture, Vector2 scale)
        {
            if (!mat.HasProperty(property)) return;
            mat.SetTexture(property, texture);
            mat.SetTextureScale(property, scale);
        }

        static void Keyword(Material mat, string keyword, bool on)
        {
            if (on) mat.EnableKeyword(keyword);
            else mat.DisableKeyword(keyword);
        }

        static Texture2D ImportTexture(string path, TextureImporterType type, bool srgb, bool cutout)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[Arena2ArtSetup] Lipseste textura {path}");
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

        static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
