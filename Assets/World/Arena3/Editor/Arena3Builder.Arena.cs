using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Alpha.World.EditorTools
{
    // Insula Arenei 3: rotunda, joasa, aproape plata, intr-un bazin cu apa neagra; se ajunge pe un dig ingust dinspre vest.
    // Doar terenul, reperele si peretii invizibili; bossul si copacii vin mai tarziu pe reperele de aici.
    public static partial class Arena3Builder
    {
        const float IslandRing = 28f;   // pana aici apa mica din jurul insulei, apoi perete invizibil
        static readonly float[] ResurfaceAngles = { 35f, 105f, 150f, 210f, 285f };
        static readonly float[] TreeAngles = { 30f, 90f, 150f, 210f, 270f, 330f };

        static Vector3 IslandPoint(float angle, float radius)
        {
            // unghiul 0 = est (spre spatele arenei), creste spre nord
            float a = angle * Mathf.Deg2Rad;
            float x = Arena3Route.IslandCentre.x + Mathf.Cos(a) * radius, z = Arena3Route.IslandCentre.y + Mathf.Sin(a) * radius;
            return new Vector3(x, Ground(x, z), z);
        }

        static void BuildArena()
        {
            Transform root = new GameObject(ArenaRoot).transform;
            Vector2 c = Arena3Route.IslandCentre;

            Marker(root, "Arena3_Centre", IslandPoint(0f, 0f), 270f);
            Vector3 entrance = Arena3Route.Ctrl[Arena3Route.Ctrl.Length - 1];
            Marker(root, "Arena3_Entrance", new Vector3(entrance.x, Ground(entrance.x, entrance.z), entrance.z), 90f);
            Marker(root, "Arena3_BossSpot", IslandPoint(0f, 9f), 270f);
            for (int i = 0; i < ResurfaceAngles.Length; i++)
            {
                // reapare din apa mica din jurul insulei, cu fata spre centru
                Vector3 p = IslandPoint(ResurfaceAngles[i], 24f);
                p.y = Arena3Route.WaterY;
                Marker(root, $"Arena3_ResurfaceSpot_{i + 1}", p, 270f - ResurfaceAngles[i]);
            }
            for (int i = 0; i < TreeAngles.Length; i++)
                Marker(root, $"Arena3_TreeSpot_{i + 1}", IslandPoint(TreeAngles[i], 19f), 270f - TreeAngles[i]);
            Marker(root, "Road4_Start", new Vector3(722f, Ground(722f, c.y), c.y), 90f);

            ClosedExit(Group(root, "Road4_Closed"));
            Rim(Group(root, "Rim"));
            IslandContainment(Group(root, "Containment"));
        }

        // drumul spre Arena 4 e inchis: un copac prabusit si bolovani peste capatul digului
        static void ClosedExit(Transform parent)
        {
            Prop(Kit + "Tree_Dead.prefab", parent, new Vector3(717f, 0f, 424.2f), 12f, 0.3f, 6f, 82f, true);
            Prop(Kit + "Boulder_A.prefab", parent, new Vector3(719.5f, 0f, 426.8f), R(0f, 360f), 0.4f, 2.2f, 6f, true);
            Prop(Kit + "Boulder_B.prefab", parent, new Vector3(720.5f, 0f, 422.6f), R(0f, 360f), 0.35f, 2f, 4f, true);
            Prop(Kit + "Log_Fallen.prefab", parent, new Vector3(716f, 0f, 426.6f), 75f, 0.15f, 0f, 6f, true);
        }

        // marginea insulei: cateva pietre, cioturi si trestii, departe de locurile copacilor si de intrare
        static void Rim(Transform parent)
        {
            string[] pieces = { Kit + "Boulder_C.prefab", Kit + "Stump_Roots.prefab", Kit + "Rubble_Pile.prefab", Kit + "Boulder_A.prefab" };
            for (int i = 0; i < 9; i++)
            {
                float angle = 15f + i * 37f + R(-6f, 6f);
                if (Mathf.Abs(Mathf.DeltaAngle(angle, 180f)) < 22f) continue;   // intrarea
                bool nearTree = false;
                foreach (float t in TreeAngles) nearTree |= Mathf.Abs(Mathf.DeltaAngle(angle, t)) < 9f;
                if (nearTree) continue;
                Prop(Pick(pieces), parent, IslandPoint(angle, R(21.5f, 23.5f)), R(0f, 360f), R(0.25f, 0.5f), 0f, R(0f, 12f), true);
            }
            if (reeds == null) return;
            for (int i = 0; i < 22; i++)
            {
                float angle = R(0f, 360f);
                if (Mathf.Abs(Mathf.DeltaAngle(angle, 180f)) < 14f) continue;
                Reeds(parent, IslandPoint(angle, R(21f, 25f)), 1.2f, 4, 0.9f, 1.8f);
            }
        }

        // inel de pereti la 28 m, cu o deschidere cat digul spre vest; inchide si capatul spre Arena 4
        static void IslandContainment(Transform parent)
        {
            Vector2 c = Arena3Route.IslandCentre;
            float gap = (Arena3Route.HalfW(Arena3Route.Length) + 1.4f) / IslandRing;
            const int pieces = 26;
            float span = Mathf.PI * 2f - gap * 2f, step = span / pieces;
            for (int i = 0; i < pieces; i++)
            {
                // unghi masurat de la vest (directia digului)
                float a = gap + (i + 0.5f) * step;
                Vector2 d = new Vector2(-Mathf.Cos(a), Mathf.Sin(a));
                Vector2 p = c + d * IslandRing;
                float yaw = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
                float length = 2f * IslandRing * Mathf.Sin(step * 0.5f) + 0.6f;
                Contain(parent, $"Contain_Ring_{i:00}", new Vector3(p.x, Arena3Route.WaterY, p.y), yaw, new Vector3(length, 40f, 1f));
            }
            // inelul taie NavMesh-ul, ca inamicii sa nu iasa din arena prin colturile volumului
            foreach (NavMeshModifier m in parent.GetComponentsInChildren<NavMeshModifier>()) m.ignoreFromBuild = false;
        }

        // ------------------------------------------------------------------ NavMesh

        static int BakeNavMesh()
        {
            GameObject arena = GameObject.Find(ArenaRoot);
            if (arena == null) return 0;
            GameObject nav = new GameObject("Arena3_NavMesh");
            nav.transform.SetParent(arena.transform, false);
            nav.transform.position = new Vector3(Arena3Route.IslandCentre.x, Arena3Route.IslandY, Arena3Route.IslandCentre.y);

            Physics.SyncTransforms();
            string agentName = NavMesh.GetSettingsNameFromID(0);
            NavMeshSurface surface = nav.AddComponent<NavMeshSurface>();
            surface.agentTypeID = 0;
            surface.collectObjects = CollectObjects.Volume;
            surface.size = new Vector3(IslandRing * 2f + 2f, 10f, IslandRing * 2f + 2f);   // insula + inelul de apa mica
            surface.center = Vector3.zero;
            surface.layerMask = ~((1 << 6) | (1 << 7) | (1 << 8));
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            if (surface.navMeshData == null) { Debug.LogError($"[Arena3Builder] Bake NavMesh esuat pentru {agentName}."); return 0; }

            string path = $"{Folder}/NavMesh/Arena3_NavMesh_{agentName}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surface.navMeshData, path);
            return 1;
        }
    }
}
