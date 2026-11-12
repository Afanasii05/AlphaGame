using System.Collections.Generic;
using UnityEngine;

namespace Alpha.World.EditorTools
{
    // Finisaje: peretii inalti ai defileului (acopera textura intinsa a terenului abrupt)
    // si baza pietrei cu ghicitoarea (pietre ingropate, moloz, iarba), ca piatra sa para infipta in pamant.
    public static partial class Arena1Builder
    {
        // peretii defileului: stanci inalte cu fata la marginea drumului si baza sub podea,
        // ca peretele de teren (textura intinsa) sa ramana in spatele lor
        static void BuildGorgeWalls(Transform gorge)
        {
            if (gorge == null) return;
            System.Random saved = rng;
            rng = new System.Random(Seed + 7);
            Transform walls = Group(gorge, "UpperWalls");
            float s1 = ctrlS[5], s2 = ctrlS[9];
            List<Vector3> axis = new List<Vector3>();
            for (float s = s1 - 12f; s <= s2 + 12f; s += 1f)
            {
                RoadFrame(s, out Vector3 pos, out _, out _);
                Vector3 p = Origin + pos;
                axis.Add(new Vector3(p.x, Ground(p.x, p.z), p.z));
            }
            for (int side = -1; side <= 1; side += 2)
                for (float s = s1 + R(0f, 3f); s < s2 - 2f; s += R(7f, 9f))
                {
                    RoadFrame(s, out Vector3 pos, out Vector3 right, out float halfW);
                    Vector3 p = Origin + pos;
                    p.y = Ground(p.x, p.z);
                    Vector3 outward = right * side;
                    float wallH = WallHeight(p, outward, halfW);
                    bool spire = rng.NextDouble() < 0.3;
                    string path = spire ? WorldKit + "SM_Cliff_Spire.prefab" : WorldKit + "SM_Cliff_A.prefab";
                    float height = spire ? Mathf.Clamp(wallH * R(1.0f, 1.2f), 18f, 30f) : Mathf.Clamp(wallH * R(0.9f, 1.05f), 15f, 26f);
                    float yaw = spire ? R(0f, 360f) : FaceRoadYaw(s, side) + R(-6f, 6f);
                    WallCliff(path, walls, p, outward, halfW, yaw, height, R(0.1f, 0.6f), axis);
                }
            rng = saved;
            Physics.SyncTransforms();
            Transform road = gorge.parent;
            PullPropsFromWalls(gorge, walls, axis);
            Transform details = road != null ? road.Find("02_Gorge_Details") : null;
            if (details != null) PullPropsFromWalls(details, walls, axis);
        }

        // decorul de la marginea drumului a fost pus inainte de pereti; ce a ramas inghitit de stanca il tragem spre drum,
        // pana la fata peretelui, ca ciupercile, cristalele si lumanarile sa se vada
        static void PullPropsFromWalls(Transform group, Transform walls, List<Vector3> axis)
        {
            List<Transform> props = new List<Transform>();
            CollectProps(group, walls, props);
            foreach (Transform prop in props)
            {
                if (!TryBounds(prop.gameObject, out Bounds b)) continue;
                float radius = Mathf.Max(b.extents.x, b.extents.z);
                if (radius > 2f) continue;
                Vector3 a = NearestAxis(axis, b.center);
                Vector3 dir = Flat(b.center - a);
                float dist = dir.magnitude;
                if (dist < 0.3f || dist > 10f) continue;
                dir /= dist;
                float face = WallFace(walls, a, dir, dist + radius + 0.5f);
                if (face >= dist + radius + 0.1f) continue;
                float target = Mathf.Max(0.4f, face - radius - 0.15f);
                Vector3 old = prop.position;
                Vector3 moved = old + dir * (target - dist);
                moved.y = old.y + Ground(moved.x, moved.z) - Ground(old.x, old.z);
                prop.position = moved;
            }
            Physics.SyncTransforms();
        }

        static void CollectProps(Transform t, Transform walls, List<Transform> props)
        {
            foreach (Transform c in t)
            {
                if (c == walls || c.name.StartsWith("SM_Cliff")) continue;
                if (c.GetComponent<Light>() != null) continue;
                bool group = c.GetComponent<Renderer>() == null && c.GetComponent<LODGroup>() == null && !UnityEditor.PrefabUtility.IsOutermostPrefabInstanceRoot(c.gameObject);
                if (group) CollectProps(c, walls, props);
                else props.Add(c);
            }
        }

        static Vector3 NearestAxis(List<Vector3> axis, Vector3 p)
        {
            Vector3 best = axis[0];
            float bestD = float.MaxValue;
            foreach (Vector3 a in axis)
            {
                float d = Flat(a - p).sqrMagnitude;
                if (d < bestD) { bestD = d; best = a; }
            }
            return best;
        }

        // distanta de la ax pana la fata peretelui, pe directia data (masurata la 0.5 m si 1.5 m deasupra podelei)
        static float WallFace(Transform walls, Vector3 a, Vector3 dir, float max)
        {
            float face = max;
            for (int k = 0; k < 2; k++)
            {
                Vector3 o = a + Vector3.up * (0.5f + k);
                foreach (RaycastHit h in Physics.RaycastAll(o, dir, max, ~0, QueryTriggerInteraction.Ignore))
                    if (h.collider.transform.IsChildOf(walls)) face = Mathf.Min(face, h.distance);
            }
            return face;
        }

        // inaltimea peretelui de teren putin in spatele marginii drumului
        static float WallHeight(Vector3 p, Vector3 outward, float halfW)
        {
            Vector3 q = p + outward * (halfW + GorgeShelf + 7f);
            return Mathf.Max(0f, Ground(q.x, q.z) - p.y);
        }

        // stanca de perete: fata interioara la marginea drumului, baza sub podea;
        // mesh-ul e neregulat, asa ca verificam cu capsule pe axa drumului si o impingem in afara cat timp intra in drum.
        // in curbe stanca lata nu incape la margine, asa ca incercam si rotita cu 90 de grade (latura ingusta spre drum)
        static GameObject WallCliff(string path, Transform parent, Vector3 p, Vector3 outward, float halfW, float yaw, float height, float inset, List<Vector3> axis)
        {
            GameObject go = Spawn(path, parent, p, yaw);
            if (go == null) return null;
            ScaleToHeight(go, height);
            int pushes = FitWallCliff(go, p, outward, halfW, inset, axis);
            if (pushes > 2)
            {
                GameObject alt = Spawn(path, parent, p, yaw + 90f);
                if (alt != null)
                {
                    alt.transform.localScale = go.transform.localScale;
                    int altPushes = FitWallCliff(alt, p, outward, halfW, inset, axis);
                    GameObject worse = altPushes < pushes ? go : alt;
                    if (altPushes < pushes) { go = alt; pushes = altPushes; }
                    Object.DestroyImmediate(worse);
                    Physics.SyncTransforms();
                }
            }
            if (pushes >= MaxWallPushes)
            {
                Object.DestroyImmediate(go);
                return null;
            }
            MarkStatic(go);
            return go;
        }

        const int MaxWallPushes = 12;
        const float WallApron = 0.14f;

        // pune stanca cu fata la marginea drumului si o impinge in afara pana nu mai intra in drum; intoarce cate impingeri au trebuit
        static int FitWallCliff(GameObject go, Vector3 p, Vector3 outward, float halfW, float inset, List<Vector3> axis)
        {
            if (!TryBounds(go, out Bounds b)) return 0;
            // stancile din kit au la baza un "sort" lat; il ingropam, ca fata verticala sa ajunga la marginea drumului
            float apron = b.size.y * WallApron;
            // cel mai apropiat vertex de drum (peste sort) ajunge exact la marginea lui (bounds-ul rotit ar supraestima)
            float reachIn = FrontReach(go, outward, b.min.y + apron);
            Vector3 pivot = go.transform.position;
            Vector3 target = p + outward * (halfW + inset + reachIn);
            go.transform.position = new Vector3(target.x, pivot.y + (p.y - 1.2f - apron - b.min.y), target.z);

            Collider[] own = go.GetComponentsInChildren<Collider>(true);
            float reach = Mathf.Max(b.extents.x, b.extents.z) + 2f;
            float probe = Mathf.Max(0.8f, halfW - 0.6f);
            int pushes = 0;
            while (pushes < MaxWallPushes && Intrudes(own, go.transform.position, reach, probe, axis))
            {
                go.transform.position += outward * 0.4f;
                pushes++;
            }
            return pushes;
        }

        // cat iese mesh-ul din pivot spre drum (opus lui outward), in plan orizontal
        static float FrontReach(GameObject go, Vector3 outward, float minY)
        {
            Vector3 pivot = go.transform.position;
            float reach = 0f;
            foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                Vector3[] verts = mf.sharedMesh.vertices;
                Matrix4x4 m = mf.transform.localToWorldMatrix;
                for (int i = 0; i < verts.Length; i++)
                {
                    Vector3 w = m.MultiplyPoint3x4(verts[i]);
                    if (w.y < minY) continue;
                    w -= pivot;
                    reach = Mathf.Max(reach, -(w.x * outward.x + w.z * outward.z));
                }
            }
            if (reach <= 0f && TryBounds(go, out Bounds b)) reach = Mathf.Abs(outward.x) * b.extents.x + Mathf.Abs(outward.z) * b.extents.z;
            return reach;
        }

        static bool Intrudes(Collider[] own, Vector3 center, float reach, float radius, List<Vector3> axis)
        {
            Physics.SyncTransforms();
            foreach (Vector3 a in axis)
            {
                if (Flat(a - center).magnitude > reach) continue;
                Collider[] hits = Physics.OverlapCapsule(a + Vector3.up * (radius + 0.3f), a + Vector3.up * (radius + 3f), radius, ~0, QueryTriggerInteraction.Ignore);
                foreach (Collider h in hits)
                    if (System.Array.IndexOf(own, h) >= 0) return true;
            }
            return false;
        }

        // pietre pe jumatate ingropate in jurul bolovanului, mai ales in spate si pe laturi (in fata sunt ofrandele)
        static void BuildRiddleBase(RiddleStone riddle, Vector3 c)
        {
            if (riddle == null) return;
            System.Random saved = rng;
            rng = new System.Random(Seed + 8);
            Transform root = riddle.transform;
            Transform group = Group(root, "Base");
            Transform boulder = null;
            foreach (Transform t in root)
                if (t.name.StartsWith("Boulder")) { boulder = t; break; }
            Bounds b;
            if (boulder == null || !TryBounds(boulder.gameObject, out b)) b = new Bounds(c + Vector3.up * 1.5f, new Vector3(3f, 3f, 3f));
            float radius = Mathf.Max(b.extents.x, b.extents.z);
            float entryAngle = EntryAngle(c);

            for (int i = 0; i < 8; i++)
            {
                float angle = entryAngle + 70f + i * (220f / 7f) + R(-8f, 8f);
                Vector3 p = Around(c, angle, radius * R(0.75f, 0.95f));
                float h = R(0.5f, 0.95f);
                GameObject rock = Prop(Pick(Rocks), group, p, R(0f, 360f), h * 0.45f, h, false, false, R(5f, 20f));
                if (rock != null) rock.name = "BaseStone_" + i;
            }
            for (int i = 0; i < 3; i++)
            {
                float angle = entryAngle + 100f + i * 80f + R(-15f, 15f);
                Prop(Kit + "Rubble_Pile.prefab", group, Around(c, angle, radius * R(0.95f, 1.15f)), R(0f, 360f), 0.08f, 0f, false, false);
            }
            for (int i = 0; i < 6; i++)
            {
                float angle = entryAngle + 75f + i * 42f + R(-10f, 10f);
                GrassTufts(group, Around(c, angle, radius * R(0.9f, 1.15f)), 0.5f, 3);
            }
            rng = saved;
        }
    }
}
