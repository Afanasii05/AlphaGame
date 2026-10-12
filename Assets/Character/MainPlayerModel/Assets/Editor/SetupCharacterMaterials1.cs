// Unity menu: Tools > Character > Setup Lich Materials   (top menu bar, next to Window / Help)
// Finds Lich_Character.fbx anywhere in the project, creates the 13 materials (URP/Lit, falls back to Standard),
// sets the model up as a Humanoid and builds Lich_Character_Prefab.prefab with materials assigned.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SetupCharacterMaterials
{
    struct Def { public string name; public Color color; public float metallic, smoothness; public Color emission; public float emissionStrength; }

    // Blender values are LINEAR; they are converted with .gamma below. smoothness = 1 - roughness.
    static Def D(string n, float r, float g, float b, float metal, float rough, float er = 0, float eg = 0, float eb = 0, float es = 0)
    {
        return new Def { name = n, color = new Color(r, g, b), metallic = metal, smoothness = 1f - rough, emission = new Color(er, eg, eb), emissionStrength = es };
    }

    // sheets of cloth / thin shells that are seen from both sides -> render both faces
    static readonly HashSet<string> TwoSided = new HashSet<string> { "Cloth_Purple", "Cloth_DarkPurple" };

    static readonly Def[] Defs =
    {
        D("Glow_Green",       0.15f, 1.00f, 0.30f, 0.00f, 0.40f, 0.15f, 1.00f, 0.30f, 4f),
        D("Pants_Black",      0.03f, 0.03f, 0.04f, 0.00f, 0.90f),
        D("Void",             0.01f, 0.01f, 0.012f, 0.00f, 1.00f),
        D("Skin",             0.55f, 0.36f, 0.26f, 0.00f, 0.55f),
        D("Glove_Dark",       0.05f, 0.05f, 0.06f, 0.00f, 0.60f),
        D("Cloth_Purple",     0.17f, 0.09f, 0.24f, 0.00f, 0.92f),
        D("Bone",             0.72f, 0.68f, 0.57f, 0.00f, 0.60f),
        D("Armor_Dark",       0.07f, 0.09f, 0.17f, 0.55f, 0.42f),
        D("Trim_Silver",      0.50f, 0.50f, 0.46f, 0.90f, 0.32f),
        D("Bandage",          0.40f, 0.38f, 0.32f, 0.00f, 0.95f),
        D("Leather",          0.16f, 0.09f, 0.06f, 0.00f, 0.55f),
        D("Glow_Magenta",     1.00f, 0.25f, 0.90f, 0.00f, 0.40f, 1.00f, 0.25f, 0.90f, 4f),
        D("Cloth_DarkPurple", 0.09f, 0.06f, 0.15f, 0.00f, 0.92f),
    };

    // material slot order exactly as in Blender (used as a fallback if names do not match)
    static readonly string[] SlotOrder = { "Glow_Green", "Pants_Black", "Void", "Skin", "Glove_Dark", "Cloth_Purple", "Bone", "Armor_Dark", "Trim_Silver", "Bandage", "Leather", "Glow_Magenta", "Cloth_DarkPurple" };

    [MenuItem("Tools/Character/Setup Lich Materials")]
    public static void Run()
    {
        string fbx = null;
        foreach (var guid in AssetDatabase.FindAssets("Lich_Character t:Model"))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.EndsWith("/Lich_Character.fbx")) { fbx = p; break; }
        }
        if (fbx == null) { Debug.LogError("[Lich] Lich_Character.fbx not found in the project."); return; }
        string dir = fbx.Substring(0, fbx.LastIndexOf('/'));
        string mats = dir + "/Materials";
        string prefabPath = dir + "/Lich_Character_Prefab.prefab";
        Debug.Log("[Lich] Using " + fbx);
        if (!AssetDatabase.IsValidFolder(mats)) AssetDatabase.CreateFolder(dir, "Materials");

        // 1) materials
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        bool urp = shader != null;
        if (!urp) shader = Shader.Find("Standard");
        var map = new Dictionary<string, Material>();
        foreach (var d in Defs)
        {
            string path = mats + "/" + d.name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetColor(urp ? "_BaseColor" : "_Color", d.color.gamma);
            m.SetFloat("_Metallic", d.metallic);
            m.SetFloat(urp ? "_Smoothness" : "_Glossiness", d.smoothness);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", TwoSided.Contains(d.name) ? 0f : 2f);   // 0 = Both, 2 = Back
            if (d.emissionStrength > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", d.emission * d.emissionStrength);   // HDR, linear
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }
            EditorUtility.SetDirty(m);
            map[d.name] = m;
        }

        // 2) model import settings: Humanoid + correct normals/tangents
        var imp = (ModelImporter)AssetImporter.GetAtPath(fbx);
        imp.globalScale = 1f;
        imp.useFileScale = true;
        imp.importNormals = ModelImporterNormals.Import;
        imp.importTangents = ModelImporterTangents.CalculateMikk;
        imp.importBlendShapes = false;
        imp.importCameras = false;
        imp.importLights = false;
        imp.animationType = ModelImporterAnimationType.Human;
        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
        foreach (var kv in map)
            imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
        imp.SaveAndReimport();

        // 3) prefab with materials set directly on the renderer
        var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
        foreach (var r in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var old = r.sharedMaterials;
            var arr = new Material[old.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                Material m = null;
                if (old[i] != null) map.TryGetValue(old[i].name, out m);
                if (m == null && i < SlotOrder.Length) map.TryGetValue(SlotOrder[i], out m);
                arr[i] = m;
            }
            r.sharedMaterials = arr;
            Debug.Log("[Lich] " + r.name + ": " + arr.Length + " material slots assigned.");
        }
        PrefabUtility.SaveAsPrefabAsset(inst, prefabPath);
        Object.DestroyImmediate(inst);

        // 4) report whether the Humanoid avatar built correctly
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbx))
        {
            var av = o as Avatar;
            if (av != null) Debug.Log("[Lich] Avatar '" + av.name + "' valid=" + av.isValid + " human=" + av.isHuman);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Lich] Done. Use " + prefabPath + " in your scene.");
    }
}
