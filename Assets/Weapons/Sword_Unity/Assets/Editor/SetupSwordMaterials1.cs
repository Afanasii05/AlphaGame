// Unity menu: Tools > Sword > Setup Materials   (top menu bar, next to Window / Help)
// Works from ANY folder: it finds Sword.fbx and the textures by name.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SetupSwordMaterials
{
    static string Dir, Mats, Fbx, PrefabPath;

    static readonly string[] SwordSlots = { "M_Metal062C_GripWrap", "M_Metal062C_Blade", "M_Metal046B_Guard", "M_Rune_Glow" };
    static readonly string[] StoneSlots = { "M_Gravel043_Stone", "M_Rune_Glow" };

    enum Kind { Color, Normal, MetallicSmoothness, Emission }

    [MenuItem("Tools/Sword/Setup Materials")]
    public static void Run()
    {
        // locate Sword.fbx anywhere in the project
        Fbx = null;
        foreach (var guid in AssetDatabase.FindAssets("Sword t:Model"))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.EndsWith("/Sword.fbx")) { Fbx = p; break; }
        }
        if (Fbx == null) { Debug.LogError("[Sword] Sword.fbx not found anywhere in the project."); return; }
        Dir = Fbx.Substring(0, Fbx.LastIndexOf('/'));
        Mats = Dir + "/Materials";
        PrefabPath = Dir + "/Sword_Prefab.prefab";
        Debug.Log("[Sword] Using FBX at " + Fbx);

        if (!AssetDatabase.IsValidFolder(Mats)) AssetDatabase.CreateFolder(Dir, "Materials");

        // 1) textures
        foreach (var n in new[] { "T_Gravel043_Color.png", "T_Metal046B_Color.png", "T_Metal062C_Color.png" }) SetupTexture(n, Kind.Color);
        foreach (var n in new[] { "T_Gravel043_Normal.png", "T_Metal046B_Normal.png", "T_Metal062C_Normal.png" }) SetupTexture(n, Kind.Normal);
        foreach (var n in new[] { "T_Gravel043_MS.png", "T_Metal046B_MS.png", "T_Metal062C_MS.png", "T_Metal062C_Grip_MS.png" }) SetupTexture(n, Kind.MetallicSmoothness);
        SetupTexture("T_RuneGlow.png", Kind.Emission);
        AssetDatabase.Refresh();

        // 2) materials
        var map = new Dictionary<string, Material>();
        map["M_Gravel043_Stone"]    = Make("M_Gravel043_Stone",    T("T_Gravel043_Color.png"), Color.white, T("T_Gravel043_Normal.png"), 1.0f, T("T_Gravel043_MS.png"), 0f);
        map["M_Metal046B_Guard"]    = Make("M_Metal046B_Guard",    T("T_Metal046B_Color.png"), Color.white, T("T_Metal046B_Normal.png"), 1.0f, T("T_Metal046B_MS.png"), 0f);
        map["M_Metal062C_Blade"]    = Make("M_Metal062C_Blade",    T("T_Metal062C_Color.png"), Color.white, T("T_Metal062C_Normal.png"), 0.9f, T("T_Metal062C_MS.png"), 0f);
        map["M_Metal062C_GripWrap"] = Make("M_Metal062C_GripWrap", null, new Color(0.42f, 0.015f, 0.03f).gamma, T("T_Metal062C_Normal.png"), 1.4f, T("T_Metal062C_Grip_MS.png"), 0f);
        var rune = Make("M_Rune_Glow", null, new Color(0.01f, 0.02f, 0.03f).gamma, null, 1f, null, 0.65f);
        SetupEmission(rune, T("T_RuneGlow.png"), 3.5f);
        map["M_Rune_Glow"] = rune;

        // 3) FBX import settings
        var imp = (ModelImporter)AssetImporter.GetAtPath(Fbx);
        imp.globalScale = 1f;
        imp.useFileScale = true;
        imp.importNormals = ModelImporterNormals.Import;
        imp.importTangents = ModelImporterTangents.CalculateMikk;
        imp.importBlendShapes = false;
        imp.importCameras = false;
        imp.importLights = false;
        imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
        foreach (var kv in map)
            imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
        imp.SaveAndReimport();

        // 4) prefab with materials assigned directly on the renderers
        var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
        foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
        {
            var slots = r.gameObject.name.StartsWith("Stone") ? StoneSlots : SwordSlots;
            var old = r.sharedMaterials;
            var arr = new Material[old.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                Material m = null;
                if (old[i] != null) map.TryGetValue(old[i].name, out m);
                if (m == null && i < slots.Length) map.TryGetValue(slots[i], out m);
                arr[i] = m;
            }
            r.sharedMaterials = arr;
            Debug.Log("[Sword] " + r.gameObject.name + " slots=" + arr.Length + " -> " + string.Join(", ", System.Array.ConvertAll(arr, x => x ? x.name : "NULL")));
        }
        PrefabUtility.SaveAsPrefabAsset(inst, PrefabPath);
        Object.DestroyImmediate(inst);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Sword] Done. Use " + PrefabPath + " in your scene (not Sword.fbx).");
    }

    // find a texture by exact file name anywhere in the project
    static string FindTex(string file)
    {
        string nameNoExt = System.IO.Path.GetFileNameWithoutExtension(file);
        foreach (var guid in AssetDatabase.FindAssets(nameNoExt + " t:Texture2D"))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.EndsWith("/" + file)) return p;
        }
        return null;
    }

    static Texture2D T(string file)
    {
        var p = FindTex(file);
        if (p == null) { Debug.LogError("[Sword] Texture not found in project: " + file); return null; }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }

    static void SetupTexture(string file, Kind kind)
    {
        var p = FindTex(file);
        var ti = p == null ? null : AssetImporter.GetAtPath(p) as TextureImporter;
        if (ti == null) { Debug.LogError("[Sword] Missing texture " + file); return; }
        ti.textureType = kind == Kind.Normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        ti.sRGBTexture = (kind == Kind.Color || kind == Kind.Emission);   // MS maps must be linear
        ti.alphaSource = kind == Kind.MetallicSmoothness ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
        ti.alphaIsTransparency = false;
        ti.wrapMode = kind == Kind.Emission ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        ti.mipmapEnabled = true;
        ti.anisoLevel = 8;
        ti.maxTextureSize = 2048;
        ti.SaveAndReimport();
    }

    static Material Make(string name, Texture2D color, Color baseColor, Texture2D normal, float bump, Texture2D ms, float smoothness)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        bool urp = shader != null;
        if (!urp) shader = Shader.Find("Standard");

        string path = Mats + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        m.shader = shader;

        string baseMap = urp ? "_BaseMap" : "_MainTex";
        string baseCol = urp ? "_BaseColor" : "_Color";
        string smoothP = urp ? "_Smoothness" : "_GlossMapScale";

        m.SetTexture(baseMap, color);
        m.SetColor(baseCol, baseColor);

        if (normal != null) { m.SetTexture("_BumpMap", normal); m.SetFloat("_BumpScale", bump); m.EnableKeyword("_NORMALMAP"); }

        if (ms != null)
        {
            m.SetTexture("_MetallicGlossMap", ms);
            m.EnableKeyword(urp ? "_METALLICSPECGLOSSMAP" : "_METALLICGLOSSMAP");
            m.SetFloat("_Metallic", 1f);
            m.SetFloat(smoothP, 1f);
            m.SetFloat("_SmoothnessTextureChannel", 0f);
        }
        else
        {
            m.SetFloat("_Metallic", 0f);
            m.SetFloat(urp ? "_Smoothness" : "_Glossiness", smoothness);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static void SetupEmission(Material m, Texture2D map, float intensity)
    {
        m.SetTexture("_EmissionMap", map);
        m.SetColor("_EmissionColor", Color.white * intensity);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
        EditorUtility.SetDirty(m);
    }
}
