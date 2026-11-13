using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Alpha.World.EditorTools
{
    // Puiul Adormitului: un golem mic din pietricele, ascuns printre bolovani la marginea pajistii.
    // Pivotii (Spine, Head) sunt la baza, ca SleeperChild sa-l poata ghemui, ridica si inclina.
    public static partial class Arena1Builder
    {
        const string SleeperFolder = Folder + "/Art/Sleeper";

        static void BuildSleeper(Transform arena, Vector3 c, Text hint)
        {
            EnsureFolder(Folder + "/Art", "Sleeper");
            Vector3 p = FindSleeperSpot(c);
            Vector3 toCenter = Flat(c - p);
            Quaternion facing = Quaternion.LookRotation(toCenter.normalized);

            GameObject root = new GameObject("SleeperChild");
            root.transform.SetParent(arena, false);
            root.transform.SetPositionAndRotation(new Vector3(p.x, Ground(p.x, p.z) - 0.04f, p.z), facing);
            root.layer = 2;
            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 3.2f;
            trigger.center = new Vector3(0f, 0.6f, 0f);

            Transform spine = Group(root.transform, "Spine");
            spine.localPosition = new Vector3(0f, 0.05f, 0f);
            Pebble(Rocks[1], spine, new Vector3(0f, 0.3f, 0f), 0.62f, "Body");
            Pebble(Rocks[2], spine, new Vector3(-0.29f, 0.36f, 0.06f), 0.2f, "Arm_L");
            Pebble(Rocks[0], spine, new Vector3(0.29f, 0.34f, 0.06f), 0.19f, "Arm_R");
            Pebble(Rocks[2], spine, new Vector3(-0.3f, 0.18f, 0.12f), 0.12f, "Hand_L");
            Pebble(Rocks[2], spine, new Vector3(0.31f, 0.17f, 0.12f), 0.12f, "Hand_R");

            Transform head = Group(spine, "Head");
            head.localPosition = new Vector3(0f, 0.6f, 0f);
            Pebble(Rocks[0], head, new Vector3(0f, 0.15f, 0f), 0.36f, "HeadStone");
            Pebble(Rocks[2], head, new Vector3(0.05f, 0.3f, -0.04f), 0.08f, "Crest");

            Material eyeMat = SleeperEyeMaterial();
            Renderer[] eyes = new Renderer[2];
            for (int i = 0; i < 2; i++)
            {
                GameObject eye = Primitive(PrimitiveType.Sphere, head, i == 0 ? "Eye_L" : "Eye_R", new Vector3(i == 0 ? -0.075f : 0.075f, 0.17f, 0.16f), Vector3.one * 0.06f, Quaternion.identity);
                MeshRenderer r = eye.GetComponent<MeshRenderer>();
                r.sharedMaterial = eyeMat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                eyes[i] = r;
            }
            Pebble(Rocks[1], root.transform, new Vector3(-0.17f, 0.07f, 0.06f), 0.15f, "Leg_L");
            Pebble(Rocks[1], root.transform, new Vector3(0.18f, 0.07f, 0.05f), 0.15f, "Leg_R");

            Light glow = new GameObject("Glow").AddComponent<Light>();
            glow.transform.SetParent(root.transform, false);
            glow.transform.localPosition = new Vector3(0f, 0.7f, 0.5f);
            glow.type = LightType.Point;
            glow.color = new Color(0.45f, 1f, 0.55f);
            glow.range = 3f;
            glow.intensity = 2.4f;
            glow.shadows = LightShadows.None;

            ParticleSystem motes = Loop("Motes", root.transform, root.transform.TransformPoint(new Vector3(0f, 0.6f, 0f)), Quaternion.identity, art.glow, 30, 1.5f, 2.5f, 0.02f, 0.05f, 10f);
            var main = motes.main;
            main.playOnAwake = false;
            main.prewarm = false;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            main.gravityModifier = -0.04f;
            Tint(motes, new Color(0.55f, 1f, 0.6f, 0.9f));
            Fade(motes, 0.3f);
            var shape = motes.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            // corpul solid, ca jucatorul sa nu treaca prin el
            GameObject solid = new GameObject("Solid");
            solid.transform.SetParent(root.transform, false);
            CapsuleCollider capsule = solid.AddComponent<CapsuleCollider>();
            capsule.radius = 0.35f;
            capsule.height = 0.8f;
            capsule.center = new Vector3(0f, 0.4f, 0f);

            SleeperChild child = root.AddComponent<SleeperChild>();
            SetRef(child, "spine", spine);
            SetRef(child, "head", head);
            SetRefs(child, "eyes", eyes);
            SetRef(child, "glow", glow);
            SetRef(child, "motes", motes);
            SetRef(child, "screenHint", hint);
            ClearStatic(root);

            ClearGrassAround(root.transform.position, 1.3f, 2.4f);
            BuildSleeperNest(arena, root.transform, c);
            Debug.Log($"[Arena1Builder] SleeperChild la {root.transform.position}.");
        }

        // un loc liber pe pajiste, departe de drum, de intrare si de poarta de nord
        static Vector3 FindSleeperSpot(Vector3 c)
        {
            float entryAngle = EntryAngle(c);
            Vector3 fallback = Around(c, entryAngle + 112f, 23.5f);
            for (int i = 0; i < 80; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                float angle = entryAngle + side * R(70f, 125f);
                Vector3 p = Around(c, angle, R(22.8f, 24.2f));
                if (!Free(p, 1.6f)) continue;
                if (Road2Distance(p.x - Origin.x, p.z - Origin.z, out _) < 9f) continue;
                float slope = Mathf.Max(Mathf.Abs(Ground(p.x + 1f, p.z) - Ground(p.x - 1f, p.z)), Mathf.Abs(Ground(p.x, p.z + 1f) - Ground(p.x, p.z - 1f)));
                if (slope > 0.35f) continue;
                return p;
            }
            return fallback;
        }

        // iarba terenului (~0.8 m) l-ar acoperi complet: o scoatem in jurul lui si o rarim pana la marginea cuibului
        static void ClearGrassAround(Vector3 p, float inner, float outer)
        {
            if (terrain == null) return;
            TerrainData data = terrain.terrainData;
            int res = data.detailResolution;
            Vector3 local = p - terrain.transform.position;
            float cellX = data.size.x / res, cellZ = data.size.z / res;
            int x0 = Mathf.Clamp(Mathf.FloorToInt((local.x - outer) / cellX), 0, res - 1), x1 = Mathf.Clamp(Mathf.CeilToInt((local.x + outer) / cellX), 0, res - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((local.z - outer) / cellZ), 0, res - 1), z1 = Mathf.Clamp(Mathf.CeilToInt((local.z + outer) / cellZ), 0, res - 1);
            int w = x1 - x0 + 1, h = z1 - z0 + 1;
            for (int layer = 0; layer < data.detailPrototypes.Length; layer++)
            {
                int[,] cells = data.GetDetailLayer(x0, z0, w, h, layer);
                for (int z = 0; z < h; z++)
                    for (int x = 0; x < w; x++)
                    {
                        float d = Vector2.Distance(new Vector2((x0 + x + 0.5f) * cellX, (z0 + z + 0.5f) * cellZ), new Vector2(local.x, local.z));
                        if (d < inner) cells[z, x] = 0;
                        else if (d < outer) cells[z, x] = Mathf.Min(cells[z, x], 1);
                    }
                data.SetDetailLayer(x0, z0, layer, cells);
            }
        }

        static GameObject Pebble(string path, Transform parent, Vector3 local, float height, string name)
        {
            GameObject go = Spawn(path, parent, parent.TransformPoint(local), 0f);
            if (go == null) return null;
            go.name = name;
            go.transform.rotation = parent.rotation * Random3();
            ScaleToSize(go, height);
            if (TryBounds(go, out Bounds b)) go.transform.position += parent.TransformPoint(local) - b.center;
            StripColliders(go);
            return go;
        }

        // scara uniforma pana cand cea mai mare dimensiune a bounds-ului ajunge la size (pietrele din kit sunt turtite)
        static void ScaleToSize(GameObject go, float size)
        {
            if (!TryBounds(go, out Bounds b)) return;
            float max = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (max > 0.001f) go.transform.localScale *= size / max;
        }

        static Material SleeperEyeMaterial()
        {
            string path = SleeperFolder + "/M_SleeperEyes.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", new Color(0.05f, 0.1f, 0.05f));
            mat.SetFloat("_Smoothness", 0.9f);
            mat.SetColor("_EmissionColor", new Color(0.4f, 1f, 0.5f) * 4f);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // cuibul: bolovani in spate si pe laturi, ciuperci care lumineaza slab, iarba si flori
        static void BuildSleeperNest(Transform arena, Transform sleeper, Vector3 c)
        {
            Transform nest = Group(arena, "SleeperNest");
            Vector3 p = sleeper.position;
            Vector3 fwd = sleeper.forward, right = sleeper.right;
            float[] angles = { 180f, 130f, -135f, 85f, -95f, 160f };
            for (int i = 0; i < angles.Length; i++)
            {
                Vector3 dir = Quaternion.AngleAxis(angles[i] + R(-10f, 10f), Vector3.up) * fwd;
                float h = i < 3 ? R(1.1f, 1.7f) : R(0.5f, 0.9f);
                GameObject rock = Prop(i % 2 == 0 ? Pick(Boulders) : Pick(Rocks), nest, p + dir * R(1.3f, 1.9f), R(0f, 360f), 0.2f, h, true);
                if (rock != null) MarkStatic(rock);
            }
            for (int i = 0; i < 2; i++)
            {
                Vector3 q = p + right * (i == 0 ? -1f : 1f) * R(0.8f, 1.1f) - fwd * R(0.2f, 0.6f);
                GameObject shroom = Prop(Kit + "Mushrooms_Glow.prefab", nest, q, R(0f, 360f), 0.02f, 0f, false, false);
                SetLights(shroom, false, false);
            }
            GrassTufts(nest, p - fwd * 0.8f, 1.4f, 8);
            // in fata doar pe laturi, ca puiul sa ramana vizibil dinspre pajiste
            GrassTufts(nest, p + fwd * 1.3f - right * 1.1f, 0.4f, 2);
            GrassTufts(nest, p + fwd * 1.3f + right * 1.1f, 0.4f, 2);
            Flowers(nest, p + right * 1.2f, 0.8f, 4);
            Flowers(nest, p - right * 1.3f, 0.7f, 3);
        }
    }
}
