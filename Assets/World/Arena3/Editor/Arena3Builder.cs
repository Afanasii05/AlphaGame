using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alpha.World.EditorTools
{
    // Construieste Drumul 3 (de la posterna castelului, in jos, prin mlastina) si insula Arenei 3 in scena World.
    // Se ruleaza dupa Build Arena 2 (terenul de acolo contine deja inceputul vaii) si reface doar radacinile Arena3_*.
    public static partial class Arena3Builder
    {
        const string WorldScenePath = "Assets/Scenes/World.unity";
        const string Folder = "Assets/World/Arena3";
        const string TerrainRoot = "Arena3_Terrain", RoadRoot = "Arena3_Road", ArenaRoot = "Arena3_Arena", AtmosphereRoot = "Arena3_Atmosphere";
        const int Seed = 3031;
        const int IgnoreRaycastLayer = 2, WaterLayer = 4;

        const string Kit = "Assets/RogueKit/Prefabs/";
        const string CastleArt = Arena2ArtSetup.CastleFolder + "/", FieldArt = Arena2ArtSetup.FieldFolder + "/";
        const string Atmos = "Assets/World/Arena1/Art/Atmos/";
        const string MatFolder = Folder + "/Art/Materials", VolumeFolder = Folder + "/Art/Volumes", TerrainArtFolder = Folder + "/Art/Terrain";

        static System.Random rng;
        static readonly HashSet<string> missingPaths = new HashSet<string>();
        static Terrain terrain, arena2Terrain;
        static int placed;

        [MenuItem("Tools/Alpha/World/Build Arena 3")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            rng = new System.Random(Seed);
            missingPaths.Clear();
            placed = 0;

            Scene world = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);
            SceneManager.SetActiveScene(world);
            EnsureFolder(Folder, "Terrain");
            EnsureFolder(Folder, "NavMesh");
            EnsureFolder(Folder, "Art");
            EnsureFolder(Folder + "/Art", "Materials");
            EnsureFolder(Folder + "/Art", "Volumes");
            EnsureFolder(Folder + "/Art", "Terrain");

            foreach (string root in new[] { TerrainRoot, RoadRoot, ArenaRoot, AtmosphereRoot })
            {
                GameObject old = FindRoot(world, root);
                if (old != null) Object.DestroyImmediate(old);
            }
            GameObject a2 = FindRoot(world, "Arena2_Terrain");
            arena2Terrain = a2 != null ? a2.GetComponent<Terrain>() : null;
            if (arena2Terrain == null) Debug.LogWarning("[Arena3Builder] Arena2_Terrain lipseste: ruleaza intai Build Arena 2, altfel inceputul drumului nu are vale.");

            Arena3Route.Prepare();
            PrepareArt();
            terrain = BuildTerrain();
            Physics.SyncTransforms();
            BuildRoad();
            BuildArena();
            BuildAtmosphere();
            int nav = BakeNavMesh();

            EditorSceneManager.MarkSceneDirty(world);
            EditorSceneManager.SaveScene(world);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Arena3Builder] Gata. Drum {Arena3Route.Length:F0} m, decor {placed} obiecte, NavMesh: {nav} suprafete, asset-uri lipsa: {missingPaths.Count}.");
            LogLayout();
        }

        static void LogLayout()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder("[Arena3Builder] Repere:\n");
            GameObject arena = GameObject.Find(ArenaRoot);
            if (arena != null)
                foreach (Transform t in arena.GetComponentsInChildren<Transform>())
                    if (t.name.StartsWith("Arena3_") && t.gameObject != arena || t.name.StartsWith("Road4_"))
                        sb.AppendLine($"  {t.name}: pos {t.position.ToString("F2")}, yaw {t.eulerAngles.y:F1}");
            sb.Append($"  Drum: {Arena3Route.Length:F1} m, dig de la s = {Arena3Route.CausewayS:F1}");
            Debug.Log(sb.ToString());
        }

        // ------------------------------------------------------------------ utilitare

        static T Load<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null && missingPaths.Add(path)) Debug.LogError($"[Arena3Builder] Lipseste asset-ul: {path}");
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

        // piesa asezata pe sol: aplecata, scalata la o inaltime si scufundata cu sink metri sub cel mai jos punct de teren
        static GameObject Prop(string path, Transform parent, Vector3 p, float yaw, float sink, float height = 0f, float lean = 0f, bool collider = false)
        {
            GameObject go = Spawn(path, parent, p, yaw);
            if (go == null) return null;
            if (lean > 0f) go.transform.rotation = Quaternion.AngleAxis(lean, Quaternion.Euler(0f, R(0f, 360f), 0f) * Vector3.right) * go.transform.rotation;
            if (height > 0f) ScaleToHeight(go, height);
            Put(go, p, sink);
            if (collider) EnsureColliders(go);
            else StripColliders(go);
            MarkStatic(go);
            return go;
        }

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

        static void MarkStatic(GameObject go)
        {
            foreach (MeshRenderer r in go.GetComponentsInChildren<MeshRenderer>(true))
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }

        static void ClearStatic(GameObject go)
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        }

        // solul: terenul din Arena 2 la vest de cusatura, al nostru la est
        static float Ground(float x, float z)
        {
            Terrain t = x < TerrainPos.x && arena2Terrain != null ? arena2Terrain : terrain;
            if (t == null) t = arena2Terrain;
            return t != null ? t.SampleHeight(new Vector3(x, 0f, z)) + t.transform.position.y : 0f;
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

        static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static int Side() => rng.Next(2) == 0 ? -1 : 1;
        static string Pick(string[] options) => options[rng.Next(options.Length)];
        static float Smooth(float a, float b, float x) => Arena3Route.Smooth(a, b, x);
        static float Fbm(float x, float z) => Arena3Route.Fbm(x, z);

        static SerializedProperty Find(SerializedObject so, string name)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null) Debug.LogError($"[Arena3Builder] Campul {name} nu exista pe {so.targetObject.GetType().Name}");
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

        static void SetFloat(Object target, string name, float value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = Find(so, name);
            if (p == null) return;
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
