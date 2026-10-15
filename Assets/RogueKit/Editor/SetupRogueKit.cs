// Unity menu:  Tools > RogueKit > 1. Setup Materials & Prefabs
//              Tools > RogueKit > 2. Build Showcase In Open Scene
//              Tools > RogueKit > 3. Apply Dark Cinematic Atmosphere
// Works from ANY folder: it finds the RogueKit models / textures by name.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SetupRogueKit
{
    static string Root, Models, Mats, Prefabs;

    // ---------------------------------------------------------------- materials
    class MatDef
    {
        public string name, tex; public Color tint = Color.white; public float bump = 1f;
        public Color emission = Color.black; public float emissionIntensity = 0f; public float smoothness = 0.3f; public float metallic = 0f;
        public MatDef(string n, string t) { name = n; tex = t; }
    }

    static List<MatDef> MatDefs()
    {
        var L = new List<MatDef>();
        L.Add(new MatDef("M_Rock064", "Rock064"));
        L.Add(new MatDef("M_Rock061", "Rock061"));
        L.Add(new MatDef("M_Gravel043", "Gravel043"));
        L.Add(new MatDef("M_Ground068", "Ground068"));
        L.Add(new MatDef("M_Moss002", "Moss002"));
        L.Add(new MatDef("M_Clay006", "Clay006") { tint = new Color(0.78f, 0.45f, 0.30f) });
        L.Add(new MatDef("M_Wood075", "Wood075"));
        L.Add(new MatDef("M_Metal046B", "Metal046B"));
        L.Add(new MatDef("M_Metal062C", "Metal062C"));
        L.Add(new MatDef("M_Metal041B", "Metal041B"));
        L.Add(new MatDef("M_Fabric061_Banner", "Fabric061") { tint = new Color(0.36f, 0.13f, 0.46f) });
        L.Add(new MatDef("M_Fabric025_Sack", "Fabric025") { tint = new Color(0.62f, 0.50f, 0.36f) });
        L.Add(new MatDef("M_Leather030", "Leather030"));
        L.Add(new MatDef("M_Bone", null) { tint = new Color(0.78f, 0.72f, 0.60f), smoothness = 0.35f });
        L.Add(new MatDef("M_Wax", null) { tint = new Color(0.86f, 0.80f, 0.66f), smoothness = 0.5f });
        L.Add(new MatDef("M_Ember", null) { tint = new Color(0.25f, 0.05f, 0.01f), smoothness = 0.1f, emission = new Color(1f, 0.32f, 0.04f), emissionIntensity = 4f });
        L.Add(new MatDef("M_Flame", null) { tint = new Color(1f, 0.5f, 0.12f), smoothness = 0f, emission = new Color(1f, 0.45f, 0.08f), emissionIntensity = 6f });
        L.Add(new MatDef("M_Crystal_Glow", null) { tint = new Color(0.22f, 0.04f, 0.38f), smoothness = 0.92f, emission = new Color(0.6f, 0.15f, 1f), emissionIntensity = 3f });
        L.Add(new MatDef("M_Mushroom_Glow", null) { tint = new Color(0.25f, 0.8f, 0.6f), smoothness = 0.5f, emission = new Color(0.2f, 1f, 0.6f), emissionIntensity = 2.5f });
        L.Add(new MatDef("M_Rune_Glow", null) { tint = new Color(0.1f, 0.6f, 0.2f), smoothness = 0.5f, emission = new Color(0.2f, 1f, 0.35f), emissionIntensity = 3.5f });
        return L;
    }

    // ---------------------------------------------------------------- lights (positions in Blender coords x,y,z -> converted)
    class LightDef { public Vector3 b; public Color c; public float range, intensity, flicker; public bool shadows;
        public LightDef(float x, float y, float z, Color col, float r, float i, float f, bool s) { b = new Vector3(x, y, z); c = col; range = r; intensity = i; flicker = f; shadows = s; } }

    static readonly Color Fire = new Color(1f, 0.55f, 0.2f), Purple = new Color(0.7f, 0.3f, 1f), Teal = new Color(0.3f, 1f, 0.75f), Green = new Color(0.35f, 1f, 0.45f);

    static Dictionary<string, LightDef[]> Lights()
    {
        var d = new Dictionary<string, LightDef[]>();
        d["SM_Brazier"]        = new[] { new LightDef(0, 0, 1.45f, Fire, 8f, 3f, 0.4f, false) };
        d["SM_Torch_Wall"]     = new[] { new LightDef(0, -0.3f, 2.15f, Fire, 6f, 2.2f, 0.45f, false) };
        d["SM_Torch_Standing"] = new[] { new LightDef(0, 0, 2.2f, Fire, 7f, 2.5f, 0.45f, false) };
        d["SM_Candles"]        = new[] { new LightDef(0, 0, 0.45f, Fire, 3f, 1.0f, 0.3f, false) };
        d["SM_Altar_Shrine"]   = new[] { new LightDef(0, 0, 1.9f, Purple, 6f, 2.5f, 0.05f, false), new LightDef(0, -1.0f, 0.9f, Green, 3f, 0.8f, 0.05f, false) };
        d["SM_Crystals_Glow"]  = new[] { new LightDef(0, 0, 0.9f, Purple, 5f, 2f, 0.05f, false) };
        d["SM_Mushrooms_Glow"] = new[] { new LightDef(0, 0, 0.5f, Teal, 3.5f, 1.2f, 0.05f, false) };
        return d;
    }

    // movable props get convex colliders (so you can add a Rigidbody / make them breakable); decor gets none
    static readonly HashSet<string> Movable = new HashSet<string> { "SM_Crate", "SM_Crate_Small", "SM_Barrel", "SM_Pot_Round", "SM_Pot_Amphora", "SM_Sacks", "SM_Chest" };
    static readonly HashSet<string> NoCollider = new HashSet<string> { "SM_Banner", "SM_Chain_Hanging", "SM_Candles", "SM_Mushrooms_Glow", "SM_Ground_Mound" };

    enum Kind { Color, Normal, MS }

    [MenuItem("Tools/RogueKit/1. Setup Materials and Prefabs")]
    public static void Run()
    {
        string anyFbx = null;
        foreach (var guid in AssetDatabase.FindAssets("SM_Crate t:Model"))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.EndsWith("/SM_Crate.fbx")) { anyFbx = p; break; }
        }
        if (anyFbx == null) { Debug.LogError("[RogueKit] SM_Crate.fbx not found. Copy the RogueKit folder into Assets first."); return; }
        Models = anyFbx.Substring(0, anyFbx.LastIndexOf('/'));
        Root = Models.Substring(0, Models.LastIndexOf('/'));
        Mats = Root + "/Materials"; Prefabs = Root + "/Prefabs";
        if (!AssetDatabase.IsValidFolder(Mats)) AssetDatabase.CreateFolder(Root, "Materials");
        if (!AssetDatabase.IsValidFolder(Prefabs)) AssetDatabase.CreateFolder(Root, "Prefabs");

        var defs = MatDefs();
        // 1) textures
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var d in defs)
            {
                if (d.tex == null) continue;
                SetupTexture("T_" + d.tex + "_Color.png", Kind.Color);
                SetupTexture("T_" + d.tex + "_Normal.png", Kind.Normal);
                SetupTexture("T_" + d.tex + "_MS.png", Kind.MS);
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.Refresh();

        // 2) materials
        var map = new Dictionary<string, Material>();
        foreach (var d in defs) map[d.name] = Make(d);

        // 3) models: import settings + material remap
        var lights = Lights();
        int made = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Models }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!name.StartsWith("SM_")) continue;
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            imp.globalScale = 1f; imp.useFileScale = true; imp.bakeAxisConversion = false;
            imp.importNormals = ModelImporterNormals.Import;
            imp.importTangents = ModelImporterTangents.CalculateMikk;
            imp.importBlendShapes = false; imp.importCameras = false; imp.importLights = false;
            imp.importAnimation = false; imp.animationType = ModelImporterAnimationType.None;
            imp.generateSecondaryUV = true;   // lightmap UVs for baked GI
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
            foreach (var kv in map) imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
            imp.SaveAndReimport();

            // 4) prefab
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var inst = new GameObject(name.Substring(3));                       // upright root: rotate/scale it freely
            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            model.transform.SetParent(inst.transform, false);                     // keeps the FBX axis conversion on the child
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                var arr = r.sharedMaterials;
                for (int i = 0; i < arr.Length; i++)
                {
                    if (arr[i] == null) continue;
                    var key = arr[i].name.Replace(" (Instance)", "");
                    Material m; if (map.TryGetValue(key, out m)) arr[i] = m;
                }
                r.sharedMaterials = arr;
                if (!NoCollider.Contains(name))
                {
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf != null && r.GetComponent<Collider>() == null)
                    {
                        var mc = r.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                        mc.convex = Movable.Contains(name);
                    }
                }
            }
            if (!Movable.Contains(name))
                foreach (var tr in inst.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(tr.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);

            LightDef[] ld;
            if (lights.TryGetValue(name, out ld))
            {
                int k = 0;
                foreach (var l in ld)
                {
                    var go = new GameObject("Light_" + (k++));
                    go.transform.SetParent(inst.transform, false);
                    go.transform.localPosition = new Vector3(-l.b.x, l.b.z, -l.b.y);   // Blender -> Unity
                    var lt = go.AddComponent<Light>();
                    lt.type = LightType.Point; lt.color = l.c; lt.range = l.range; lt.intensity = l.intensity;
                    lt.shadows = l.shadows ? LightShadows.Soft : LightShadows.None;
                    lt.renderMode = LightRenderMode.Auto;
                    if (l.flicker > 0.1f) { var f = go.AddComponent<LightFlicker>(); f.amount = l.flicker; }
                }
            }
            PrefabUtility.SaveAsPrefabAsset(inst, Prefabs + "/" + name.Substring(3) + ".prefab");
            Object.DestroyImmediate(inst);
            made++;
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        AddFireFX();
        Debug.Log("[RogueKit] Done: " + map.Count + " materials, " + made + " prefabs in " + Prefabs);
    }

    // ---------------------------------------------------------------- showcase + atmosphere
    [MenuItem("Tools/RogueKit/2. Build Showcase In Open Scene")]
    public static void Showcase()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab");
        var list = new List<GameObject>();
        foreach (var g in guids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (p.Contains("/RogueKit/Prefabs/")) list.Add(AssetDatabase.LoadAssetAtPath<GameObject>(p));
        }
        if (list.Count == 0) { Debug.LogError("[RogueKit] Run '1. Setup Materials and Prefabs' first."); return; }
        var root = new GameObject("RogueKit_Showcase");
        Undo.RegisterCreatedObjectUndo(root, "RogueKit Showcase");
        int cols = 6; float sp = 6f;
        for (int i = 0; i < list.Count; i++)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(list[i], root.transform);
            go.transform.position = new Vector3((i % cols) * sp, 0f, -(i / cols) * sp);
        }
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground"; ground.transform.SetParent(root.transform, false);
        ground.transform.position = new Vector3((cols - 1) * sp / 2f, 0f, -((list.Count - 1) / cols) * sp / 2f);
        ground.transform.localScale = new Vector3(5f, 1f, 5f);
        foreach (var g in AssetDatabase.FindAssets("M_Gravel043 t:Material"))
        {
            var src = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
            var inst = new Material(src) { name = "M_Gravel043_Ground" };
            inst.mainTextureScale = new Vector2(25, 25);
            if (inst.HasProperty("_BumpMap")) inst.SetTextureScale("_BumpMap", new Vector2(25, 25));
            ground.GetComponent<Renderer>().sharedMaterial = inst; break;
        }
        Selection.activeGameObject = root;
    }

    [MenuItem("Tools/RogueKit/3. Apply Dark Cinematic Atmosphere")]
    public static void Atmosphere()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.07f, 0.075f, 0.1f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.06f, 0.065f, 0.085f);
        RenderSettings.fogDensity = 0.035f;
        foreach (var l in Resources.FindObjectsOfTypeAll<Light>())
            if (l.gameObject.scene.IsValid() && l.type == LightType.Directional) { l.intensity = 0.35f; l.color = new Color(0.55f, 0.62f, 0.85f); }
        Debug.Log("[RogueKit] Dark atmosphere applied (fog + low ambient + moonlight). Undo with Lighting window if needed.");
    }

    // ---------------------------------------------------------------- helpers
    static string FindTex(string file)
    {
        string nameNoExt = System.IO.Path.GetFileNameWithoutExtension(file);
        string fallback = null;
        foreach (var guid in AssetDatabase.FindAssets(nameNoExt + " t:Texture2D"))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (!p.EndsWith("/" + file)) continue;
            if (Root != null && p.StartsWith(Root + "/")) return p;   // prefer the kit's own copy
            if (fallback == null) fallback = p;
        }
        return fallback;
    }

    static Texture2D T(string file)
    {
        var p = FindTex(file);
        if (p == null) { Debug.LogError("[RogueKit] Texture not found in project: " + file); return null; }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }

    static void SetupTexture(string file, Kind kind)
    {
        var p = FindTex(file);
        var ti = p == null ? null : AssetImporter.GetAtPath(p) as TextureImporter;
        if (ti == null) { Debug.LogError("[RogueKit] Missing texture " + file); return; }
        ti.textureType = kind == Kind.Normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        ti.sRGBTexture = kind == Kind.Color;
        ti.alphaSource = kind == Kind.MS ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
        ti.alphaIsTransparency = false;
        ti.wrapMode = TextureWrapMode.Repeat;
        ti.mipmapEnabled = true; ti.anisoLevel = 8; ti.maxTextureSize = 2048;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.SaveAndReimport();
    }

    static Material Make(MatDef d)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        bool urp = shader != null;
        if (!urp) shader = Shader.Find("Standard");
        string path = Mats + "/" + d.name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        m.shader = shader;
        string baseMap = urp ? "_BaseMap" : "_MainTex", baseCol = urp ? "_BaseColor" : "_Color";

        Texture2D color = d.tex != null ? T("T_" + d.tex + "_Color.png") : null;
        Texture2D normal = d.tex != null ? T("T_" + d.tex + "_Normal.png") : null;
        Texture2D ms = d.tex != null ? T("T_" + d.tex + "_MS.png") : null;
        m.SetTexture(baseMap, color);
        m.SetColor(baseCol, d.tint);
        if (normal != null) { m.SetTexture("_BumpMap", normal); m.SetFloat("_BumpScale", d.bump); m.EnableKeyword("_NORMALMAP"); }
        if (ms != null)
        {
            m.SetTexture("_MetallicGlossMap", ms);
            m.EnableKeyword(urp ? "_METALLICSPECGLOSSMAP" : "_METALLICGLOSSMAP");
            m.SetFloat("_Metallic", 1f);
            m.SetFloat(urp ? "_Smoothness" : "_GlossMapScale", 1f);
            m.SetFloat("_SmoothnessTextureChannel", 0f);
        }
        else
        {
            m.SetFloat("_Metallic", d.metallic);
            m.SetFloat(urp ? "_Smoothness" : "_Glossiness", d.smoothness);
        }
        if (d.emissionIntensity > 0f)
        {
            m.SetColor("_EmissionColor", d.emission * d.emissionIntensity);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }
        else
        {
            m.DisableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    // ---------------------------------------------------------------- fire particles on braziers / torches
    [MenuItem("Tools/RogueKit/4. (Re)Add Fire Particles To Prefabs")]
    public static void AddFireFX()
    {
        if (Prefabs == null)
        {
            foreach (var guid in AssetDatabase.FindAssets("Brazier t:Prefab"))
            { var p = AssetDatabase.GUIDToAssetPath(guid); if (p.EndsWith("/RogueKit/Prefabs/Brazier.prefab")) { Prefabs = p.Substring(0, p.LastIndexOf('/')); Root = Prefabs.Substring(0, Prefabs.LastIndexOf('/')); Mats = Root + "/Materials"; } }
            if (Prefabs == null) { Debug.LogError("[RogueKit] Run setup first."); return; }
        }
        if (!AssetDatabase.IsValidFolder(Mats + "/FX")) AssetDatabase.CreateFolder(Mats, "FX");
        var fire = ParticleMat("M_FX_Fire"); var ember = ParticleMat("M_FX_Ember");
        var spec = new Dictionary<string, object[]> {
            { "Brazier", new object[] { new Vector3(0, 0.86f, 0), 1.0f } },
            { "Torch_Standing", new object[] { new Vector3(0, 1.87f, 0), 0.75f } },
            { "Torch_Wall", new object[] { new Vector3(0, 1.98f, 0.27f), 0.5f } } };
        foreach (var kv in spec)
        {
            string path = Prefabs + "/" + kv.Key + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;
            var c = PrefabUtility.LoadPrefabContents(path);
            BuildFire(c, (Vector3)kv.Value[0], (float)kv.Value[1], fire, ember);
            PrefabUtility.SaveAsPrefabAsset(c, path); PrefabUtility.UnloadPrefabContents(c);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[RogueKit] Fire particles added to Brazier / Torch_Standing / Torch_Wall.");
    }

    static Material ParticleMat(string name)
    {
        string path = Mats + "/FX/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Legacy Shaders/Particles/Additive");
        m = new Material(sh);
        m.SetTexture("_BaseMap", AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd"));
        m.mainTexture = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
        if (m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000;
        }
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void BuildFire(GameObject rootGo, Vector3 lp, float s, Material fire, Material ember)
    {
        var old = rootGo.transform.Find("FX_Fire"); if (old) Object.DestroyImmediate(old.gameObject);
        var fx = new GameObject("FX_Fire"); fx.transform.SetParent(rootGo.transform, false);
        fx.transform.localPosition = lp; fx.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        var ps = fx.AddComponent<ParticleSystem>(); var pr = fx.GetComponent<ParticleSystemRenderer>();
        var main = ps.main; main.maxParticles = 80; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.8f); main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f * s, 1.0f * s);
        main.startSize = new ParticleSystem.MinMaxCurve(0.28f * s, 0.5f * s); main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
        main.gravityModifier = -0.12f; main.startColor = new Color(1f, 0.7f, 0.35f, 1f);
        var em = ps.emission; em.rateOverTime = 34f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 6f; sh.radius = 0.16f * s;
        var col = ps.colorOverLifetime; col.enabled = true; var gr = new Gradient();
        gr.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.85f, 0.55f), 0f), new GradientColorKey(new Color(1f, 0.5f, 0.12f), 0.3f), new GradientColorKey(new Color(0.8f, 0.18f, 0.03f), 0.7f), new GradientColorKey(new Color(0.2f, 0.03f, 0.01f), 1f) },
                   new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.12f), new GradientAlphaKey(0.45f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = gr;
        var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.6f), new Keyframe(0.25f, 1f), new Keyframe(1, 0.25f)));
        var nz = ps.noise; nz.enabled = true; nz.strength = 0.35f * s; nz.frequency = 1.6f; nz.scrollSpeed = 1.2f;
        var rol = ps.rotationOverLifetime; rol.enabled = true; rol.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
        pr.sharedMaterial = fire; pr.renderMode = ParticleSystemRenderMode.Billboard; pr.maxParticleSize = 1f;
        var eg = new GameObject("Embers"); eg.transform.SetParent(fx.transform, false);
        var ep = eg.AddComponent<ParticleSystem>(); var er = eg.GetComponent<ParticleSystemRenderer>();
        var m2 = ep.main; m2.maxParticles = 40; m2.simulationSpace = ParticleSystemSimulationSpace.World;
        m2.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.6f); m2.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
        m2.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.05f); m2.gravityModifier = -0.04f; m2.startColor = new Color(1f, 0.55f, 0.15f, 1f);
        var e2 = ep.emission; e2.rateOverTime = 5f * s;
        var s2 = ep.shape; s2.shapeType = ParticleSystemShapeType.Cone; s2.angle = 18f; s2.radius = 0.15f * s;
        var c2 = ep.colorOverLifetime; c2.enabled = true; var g2 = new Gradient();
        g2.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.8f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.35f, 0.05f), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        c2.color = g2;
        var n2 = ep.noise; n2.enabled = true; n2.strength = 0.6f; n2.frequency = 0.8f;
        er.sharedMaterial = ember; er.renderMode = ParticleSystemRenderMode.Billboard;
    }
}
