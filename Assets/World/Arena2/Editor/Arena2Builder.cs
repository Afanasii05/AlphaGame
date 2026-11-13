using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Alpha.World.EditorTools
{
    // Construieste Drumul 2 (Campul Juramintelor Rupte) + castelul cu sala tronului (Arena 2) in scena World.
    // Se poate rula de mai multe ori: reface doar radacinile Arena2_*. Arena 1 trebuie construita inainte (iesirea coridorului).
    public static partial class Arena2Builder
    {
        const string WorldScenePath = "Assets/Scenes/World.unity";
        const string Folder = "Assets/World/Arena2";
        const string TerrainDataPath = Folder + "/Terrain/Arena2_TerrainData.asset";
        const string Arena1TerrainDataPath = "Assets/World/Arena1/Terrain/Arena1_TerrainData.asset";
        const string TerrainRoot = "Arena2_Terrain", RoadRoot = "Arena2_Road", CastleRoot = "Arena2_Castle", AtmosphereRoot = "Arena2_Atmosphere";
        const string KnightRoot = "Arena2_Knight", ThroneRoot = "Arena2_Throne";
        const int Seed = 2024;

        const string Kit = "Assets/RogueKit/Prefabs/", WorldKit = "Assets/World/Prefabs/", Env = "Assets/Environment/";
        const string CastleArt = Arena2ArtSetup.CastleFolder + "/", FieldArt = Arena2ArtSetup.FieldFolder + "/", A2Mat = Arena2ArtSetup.MaterialFolder + "/";
        const string Atmos = "Assets/World/Arena1/Art/Atmos/", Fx = "Assets/World/Arena1/Prefabs/FX/";

        static readonly string[] Rocks = { Env + "Rocks/Prefabs/Rock_A.prefab", Env + "Rocks/Prefabs/Rock_B.prefab", Env + "Rocks/Prefabs/Rock_C.prefab" };
        static readonly string[] Boulders = { Kit + "Boulder_A.prefab", Kit + "Boulder_B.prefab", Kit + "Boulder_C.prefab" };

        // drumul: de la iesirea coridorului din Arena 1 pana la capatul podului mobil
        static readonly Vector2[] RoadCtrl =
        {
            new Vector2(274.9f, 242.2f), new Vector2(285.6f, 250f), new Vector2(297.7f, 258.8f), new Vector2(309f, 272f), new Vector2(321f, 287f),
            new Vector2(336f, 300f), new Vector2(354f, 310f), new Vector2(372f, 318f), new Vector2(389f, 327f), new Vector2(402f, 338f),
            new Vector2(412f, 348f), new Vector2(418f, 356f), new Vector2(420f, 362f)
        };
        const int RoadSteps = 20;

        struct RoadHit { public float dist, s, lat; }

        static System.Random rng;
        static readonly HashSet<string> missingPaths = new HashSet<string>();
        static readonly List<Vector2> roadPts = new List<Vector2>();
        static readonly List<float> roadS = new List<float>();
        static float roadLength;
        static Terrain terrain, arena1Terrain;
        static int placed;

        [MenuItem("Tools/Alpha/World/Build Arena 2")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            // arta (materiale, controller, straturi de teren) se pregateste o singura data
            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Arena2ArtSetup.ControllerPath) == null || Arena2ArtSetup.LoadTerrainLayers()[0] == null)
                Arena2ArtSetup.Run();
            rng = new System.Random(Seed);
            missingPaths.Clear();
            shadowLights.Clear();
            placed = 0;

            Scene world = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);
            SceneManager.SetActiveScene(world);
            EnsureFolder(Folder, "Terrain");
            EnsureFolder(Folder, "NavMesh");
            EnsureFolder(Folder + "/Art", "Volumes");

            foreach (string root in new[] { TerrainRoot, RoadRoot, CastleRoot, AtmosphereRoot, KnightRoot, ThroneRoot, BossRoot })
            {
                GameObject old = FindRoot(world, root);
                if (old != null) Object.DestroyImmediate(old);
            }
            GameObject a1 = FindRoot(world, "Arena1_Terrain");
            arena1Terrain = a1 != null ? a1.GetComponent<Terrain>() : null;
            if (arena1Terrain == null) Debug.LogWarning("[Arena2Builder] Arena1_Terrain lipseste: ruleaza intai Build Arena 1, altfel cusatura cu coridorul nu se potriveste.");

            PrepareArt();
            BuildRoadSpline();
            terrain = BuildTerrain();
            Physics.SyncTransforms();
            BuildField();
            BuildCastle();
            BuildAtmosphere();
            BuildPolish();
            LimitShadows(RoadRoot, CastleRoot, AtmosphereRoot);
            int nav = BakeNavMesh();
            BuildBoss();

            EditorSceneManager.MarkSceneDirty(world);
            EditorSceneManager.SaveScene(world);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Arena2Builder] Gata. Drum {roadLength:F0} m, decor {placed} obiecte, NavMesh: {nav} suprafete, asset-uri lipsa: {missingPaths.Count}.");
            LogLayout();
        }

        // ------------------------------------------------------------------ drum

        static void BuildRoadSpline()
        {
            roadPts.Clear();
            roadS.Clear();
            float s = 0f;
            for (int i = 0; i < RoadCtrl.Length - 1; i++)
            {
                Vector2 p0 = RoadCtrl[Mathf.Max(i - 1, 0)], p1 = RoadCtrl[i], p2 = RoadCtrl[i + 1], p3 = RoadCtrl[Mathf.Min(i + 2, RoadCtrl.Length - 1)];
                for (int k = 0; k < RoadSteps; k++)
                {
                    Vector2 p = CatmullRom(p0, p1, p2, p3, k / (float)RoadSteps);
                    if (roadPts.Count > 0) s += (p - roadPts[roadPts.Count - 1]).magnitude;
                    roadPts.Add(p);
                    roadS.Add(s);
                }
            }
            Vector2 last = RoadCtrl[RoadCtrl.Length - 1];
            s += (last - roadPts[roadPts.Count - 1]).magnitude;
            roadPts.Add(last);
            roadS.Add(s);
            roadLength = s;
        }

        static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t) =>
            0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (3f * p1 - p0 - 3f * p2 + p3) * t * t * t);

        static RoadHit NearestRoad(float px, float pz)
        {
            RoadHit best = new RoadHit { dist = float.MaxValue };
            Vector2 p = new Vector2(px, pz);
            for (int i = 0; i < roadPts.Count - 1; i++)
            {
                Vector2 a = roadPts[i], ab = roadPts[i + 1] - a;
                float len2 = ab.sqrMagnitude;
                float t = len2 > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2) : 0f;
                Vector2 q = a + ab * t;
                float d2 = (p - q).sqrMagnitude;
                if (d2 >= best.dist) continue;
                best.dist = d2;
                best.s = Mathf.Lerp(roadS[i], roadS[i + 1], t);
                // partea: pozitiv la dreapta sensului de mers
                best.lat = ab.x * (p.y - a.y) - ab.y * (p.x - a.x) < 0f ? 1f : -1f;
            }
            best.dist = Mathf.Sqrt(best.dist);
            best.lat *= best.dist;
            return best;
        }

        static float RoadFloor(float s) => 7.58f + 5.4f * Smooth(20f, 120f, s) - 1f * Smooth(125f, 165f, s);
        static float RoadHalfW(float s) => Mathf.Lerp(2.7f, 3.5f, Smooth(0f, 20f, s));

        static Vector2 RoadFlat(float s)
        {
            s = Mathf.Clamp(s, 0f, roadLength);
            int i = roadS.BinarySearch(s);
            if (i >= 0) return roadPts[i];
            i = ~i;
            if (i <= 0) return roadPts[0];
            if (i >= roadPts.Count) return roadPts[roadPts.Count - 1];
            float t = Mathf.InverseLerp(roadS[i - 1], roadS[i], s);
            return Vector2.Lerp(roadPts[i - 1], roadPts[i], t);
        }

        static Vector3 RoadForward(float s)
        {
            Vector2 d = RoadFlat(Mathf.Min(s + 1f, roadLength)) - RoadFlat(Mathf.Max(s - 1f, 0f));
            return new Vector3(d.x, 0f, d.y).normalized;
        }

        static Vector3 RoadRight(float s)
        {
            Vector3 f = RoadForward(s);
            return new Vector3(f.z, 0f, -f.x);
        }

        static float RoadYaw(float s)
        {
            Vector3 f = RoadForward(s);
            return Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
        }

        static Vector3 RoadPoint(float s)
        {
            Vector2 p = RoadFlat(s);
            return new Vector3(p.x, Ground(p.x, p.y), p.y);
        }

        // punct pe marginea drumului: side -1 stanga, +1 dreapta, extra = metri dincolo de margine
        static Vector3 Edge(float s, int side, float extra)
        {
            Vector2 p = RoadFlat(s);
            Vector3 r = RoadRight(s);
            Vector3 q = new Vector3(p.x, 0f, p.y) + r * side * (RoadHalfW(s) + extra);
            return new Vector3(q.x, Ground(q.x, q.z), q.z);
        }

        // ------------------------------------------------------------------ raport

        static void LogLayout()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder("[Arena2Builder] Repere:\n");
            foreach (string name in new[] { "Road2_Path_Start", "Arena2_GateTrigger", "Arena2_CourtyardCenter", ThroneRoot, KnightRoot, "Arena2_RearExit", "Road3_Start" })
            {
                GameObject go = GameObject.Find(name);
                if (go == null) { sb.AppendLine($"  {name}: LIPSA"); continue; }
                sb.AppendLine($"  {name}: pos {go.transform.position.ToString("F2")}, yaw {go.transform.eulerAngles.y:F1}");
            }
            sb.Append($"  Drum: {roadLength:F1} m, de la {RoadPoint(0f).ToString("F1")} la {RoadPoint(roadLength).ToString("F1")}");
            Debug.Log(sb.ToString());
        }

        // ------------------------------------------------------------------ utilitare

        static T Load<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null && missingPaths.Add(path)) Debug.LogError($"[Arena2Builder] Lipseste asset-ul: {path}");
            return asset;
        }

        static GameObject Spawn(string path, Transform parent, Vector3 position, float yaw)
        {
            GameObject prefab = Load<GameObject>(path);
            if (prefab == null) return null;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation);
            placed++;
            return go;
        }

        // copil cu mesh-ul unui FBX/prefab, cu rotatia de import pastrata (pentru pivoti construiti de mana)
        static GameObject Child(string path, Transform parent, Vector3 localPos)
        {
            GameObject prefab = Load<GameObject>(path);
            if (prefab == null) return null;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = localPos;
            go.transform.localRotation = prefab.transform.localRotation;
            go.transform.localScale = prefab.transform.localScale;
            placed++;
            return go;
        }

        static GameObject Prop(string path, Transform parent, Vector3 p, float yaw, float sink, float height = 0f, float lean = 0f, bool collider = false)
        {
            GameObject go = Spawn(path, parent, p, yaw);
            if (go == null) return null;
            if (lean > 0f) go.transform.rotation = Quaternion.AngleAxis(lean, Quaternion.Euler(0f, R(0f, 360f), 0f) * Vector3.right) * go.transform.rotation;
            if (height > 0f) ScaleToHeight(go, height);
            Put(go, p, sink);
            if (collider) EnsureColliders(go);
            else StripColliders(go);
            SetLights(go, true, false);
            MarkStatic(go);
            return go;
        }

        // centreaza obiectul pe target si il aseaza pe cel mai jos punct de teren de sub el
        static void Put(GameObject go, Vector3 target, float sink)
        {
            if (!TryBounds(go, out Bounds b))
            {
                go.transform.position = new Vector3(target.x, Ground(target.x, target.z) - sink, target.z);
                return;
            }
            Vector3 shift = new Vector3(target.x - b.center.x, 0f, target.z - b.center.z);
            b.center += shift;
            float ground = Mathf.Min(Ground(b.center.x, b.center.z),
                Mathf.Min(Mathf.Min(Ground(b.min.x, b.min.z), Ground(b.min.x, b.max.z)), Mathf.Min(Ground(b.max.x, b.min.z), Ground(b.max.x, b.max.z))));
            go.transform.position += shift + Vector3.up * (ground - sink - b.min.y);
        }

        static void ScaleToHeight(GameObject go, float height)
        {
            if (TryBounds(go, out Bounds b) && b.size.y > 0.01f) go.transform.localScale *= height / b.size.y;
        }

        static bool TryBounds(GameObject go, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (MeshRenderer r in go.GetComponentsInChildren<MeshRenderer>())
            {
                if (r.GetComponent<TextMesh>() != null) continue;
                if (!found) { bounds = r.bounds; found = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return found;
        }

        static void EnsureColliders(GameObject go)
        {
            foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh != null && mf.GetComponent<Collider>() == null)
                    mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        }

        static void StripColliders(GameObject go)
        {
            foreach (Collider col in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
        }

        static void SetLights(GameObject go, bool on, bool shadows)
        {
            if (go == null) return;
            foreach (Light light in go.GetComponentsInChildren<Light>(true))
            {
                light.enabled = on;
                if (!shadows) light.shadows = LightShadows.None;
            }
        }

        static void MarkStatic(GameObject go)
        {
            foreach (MeshRenderer r in go.GetComponentsInChildren<MeshRenderer>(true))
                if (r.GetComponentInParent<LightFlicker>() == null)
                    GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }

        static void ClearStatic(GameObject go)
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        }

        static void Swap(GameObject go, string materialName, Material to)
        {
            if (go == null || to == null) return;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name == materialName) { mats[i] = to; changed = true; }
                if (changed) r.sharedMaterials = mats;
            }
        }

        // inaltimea solului: terenul nostru, iar la sud de cusatura terenul din Arena 1
        static float Ground(float x, float z)
        {
            Terrain t = terrain;
            if (t == null || z < t.transform.position.z || x < t.transform.position.x) t = arena1Terrain != null ? arena1Terrain : terrain;
            return t != null ? t.SampleHeight(new Vector3(x, 0f, z)) + t.transform.position.y : 0f;
        }

        // punctul de pe suprafata unei piese (raza din exterior), cu normala
        static bool Surface(Vector3 from, Vector3 dir, out Vector3 point, out Vector3 normal, float max = 30f)
        {
            point = from;
            normal = -dir;
            if (!Physics.Raycast(from, dir, out RaycastHit hit, max, ~0, QueryTriggerInteraction.Ignore)) return false;
            point = hit.point;
            normal = hit.normal;
            return true;
        }

        static float FloorAt(float x, float z, float fromY)
        {
            return Physics.Raycast(new Vector3(x, fromY, z), Vector3.down, out RaycastHit hit, 60f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : Ground(x, z);
        }

        static Transform Group(Transform parent, string name)
        {
            Transform t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
                if (go.name == name) return go;
            return null;
        }

        static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }

        static GameObject Marker(Transform parent, string name, Vector3 p, float yaw)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, yaw, 0f));
            return go;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
        static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static int Side() => rng.Next(2) == 0 ? -1 : 1;
        static string Pick(string[] options) => options[rng.Next(options.Length)];

        static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        static float Fbm(float x, float z)
        {
            float sum = 0f, amp = 1f, norm = 0f, f = 1f;
            for (int o = 0; o < 4; o++)
            {
                sum += Mathf.PerlinNoise(x * f + 13.7f + o * 31.1f, z * f + 71.3f + o * 17.9f) * amp;
                norm += amp; amp *= 0.5f; f *= 2f;
            }
            return sum / norm;
        }

        static SerializedProperty Find(SerializedObject so, string name)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null) Debug.LogError($"[Arena2Builder] Campul {name} nu exista pe {so.targetObject.GetType().Name}");
            return p;
        }

        static void SetRef(Object target, string name, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = Find(so, name);
            if (p == null) return;
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetRefs(Object target, string name, Object[] values)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = Find(so, name);
            if (p == null) return;
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloats(Object target, string name, float[] values)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = Find(so, name);
            if (p == null) return;
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).floatValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Object target, string name, float value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = Find(so, name);
            if (p == null) return;
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetBool(Object target, string name, bool value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = Find(so, name);
            if (p == null) return;
            p.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetString(Object target, string name, string value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = Find(so, name);
            if (p == null) return;
            p.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetInt(Object target, string name, int value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = Find(so, name);
            if (p == null) return;
            p.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetVector3(Object target, string name, Vector3 value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = Find(so, name);
            if (p == null) return;
            p.vector3Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetColor(Object target, string name, Color value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = Find(so, name);
            if (p == null) return;
            p.colorValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
