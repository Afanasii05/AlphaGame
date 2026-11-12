using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Alpha.World.EditorTools
{
    // Drumul 3 in trei etape care se topesc una in alta:
    // a) daramaturi din castel care se scufunda in pamant ud, b) camp de lupta inecat, c) mlastina adanca cu apa neagra.
    public static partial class Arena3Builder
    {
        static readonly string[] BannerCloth = { FieldArt + "SM_A2_BannerCloth_0.fbx", FieldArt + "SM_A2_BannerCloth_1.fbx", FieldArt + "SM_A2_BannerCloth_2.fbx" };
        static readonly List<Vector4> taken = new List<Vector4>();   // x, z, raza

        static void BuildRoad()
        {
            taken.Clear();
            Transform root = new GameObject(RoadRoot).transform;
            SinkingCastle(Group(root, "A_SinkingCastle"));
            DrownedField(Group(root, "B_DrownedField"));
            Mire(Group(root, "C_Mire"));
            Water(Group(root, "Water"));
            RoadContainment(Group(root, "Containment"));
        }

        // ------------------------------------------------------------------ a) castelul se scufunda

        static void SinkingCastle(Transform parent)
        {
            // dale de pe drum, tot mai inclinate si mai afundate
            Transform slabs = Group(parent, "Slabs");
            for (float s = 4f; s < 62f; s += R(1.8f, 3.6f))
            {
                if (rng.NextDouble() > 0.85f * (1f - Smooth(22f, 64f, s))) continue;
                float hw = Arena3Route.HalfW(s);
                Vector3 p = At(s, R(-hw + 0.9f, hw - 0.9f));
                GameObject slab = Prop(Kit + "Floor_Slab_2x2.prefab", slabs, p, Arena3Route.Yaw(s) + R(-14f, 14f), 0f, 0f, R(1f, 3f) + 5f * Smooth(25f, 62f, s));
                if (slab == null) continue;
                Swap(slab, "M_Gravel043", stoneDark);
                // muchia de sus abia iese din noroi
                Settle(slab, p, R(0.01f, 0.05f) - 0.1f * Smooth(20f, 62f, s));
            }

            // turnul surpat si zidurile cazute, departe de drum
            Prop(CastleArt + "SM_Castle_TowerRuin.fbx", parent, At(34f, 21f), R(0f, 360f), 4.2f, 9f, 9f, true);
            float[] walls = { 9f, 22f, 47f, 60f };
            for (int i = 0; i < walls.Length; i++)
            {
                float s = walls[i];
                Vector3 p = At(s, (i % 2 == 0 ? -1f : 1f) * (Arena3Route.HalfW(s) + R(7.5f, 11f)));
                if (!Claim(p, 4f)) continue;
                GameObject wall = Prop(CastleArt + "SM_Castle_WallRuin.fbx", parent, p, Arena3Route.Yaw(s) + R(-35f, 35f), R(1.6f, 2.6f) + 1.4f * Smooth(10f, 60f, s), R(5.5f, 6.8f), R(6f, 14f), true);
                Swap(wall, "M_A2_CastleStone", stoneDark);
            }

            // arcade, coloane si grinzi aplecate, unele culcate in iarba uda
            string[] pieces = { CastleArt + "SM_Hall_ArchBroken.fbx", CastleArt + "SM_Hall_PillarBroken.fbx", CastleArt + "SM_Hall_PillarBroken.fbx", CastleArt + "SM_Hall_Beam.fbx", Kit + "Pillar_Broken.prefab", Kit + "Wall_Ruin_A.prefab" };
            for (float s = 6f; s < 70f; s += R(4.5f, 7f))
            {
                float hw = Arena3Route.HalfW(s);
                Vector3 p = At(s, Side() * (hw + R(2.6f, 9f)));
                string path = Pick(pieces);
                bool big = path.Contains("Arch") || path.Contains("SM_Hall_Pillar");
                if (!Claim(p, big ? 3f : 2f)) continue;
                float sink = (big ? R(0.8f, 1.6f) : R(0.2f, 0.5f)) * (0.6f + Smooth(5f, 65f, s));
                // coloana sparta are cioburile lipite de baza: doar putin aplecata, altfel plutesc
                float lean = path.Contains("Beam") ? R(4f, 14f) : path.Contains("PillarBroken") || path.Contains("Arch") ? R(2f, 7f)
                    : rng.NextDouble() < 0.35 ? R(70f, 84f) : R(5f, 18f);
                Prop(path, parent, p, R(0f, 360f), sink, 0f, lean, big || path.Contains("Wall"));
            }
            // o coloana intreaga cazuta peste fundul vaii
            Prop(CastleArt + "SM_Hall_Pillar.fbx", parent, At(52f, -10.5f), Arena3Route.Yaw(52f) + 68f, 0.55f, 0f, 86f, true);

            // moloz
            for (float s = 3f; s < 75f; s += R(3f, 5f))
            {
                Vector3 p = At(s, Side() * (Arena3Route.HalfW(s) + R(0.6f, 6f)));
                if (!Claim(p, 1.2f)) continue;
                Prop(Kit + "Rubble_Pile.prefab", parent, p, R(0f, 360f), R(0.08f, 0.22f) * (1f + Smooth(20f, 70f, s)));
            }
        }

        // ------------------------------------------------------------------ b) camp de lupta inecat

        static void DrownedField(Transform parent)
        {
            float[] banners = { 66f, 80f, 95f, 109f, 123f, 137f };
            foreach (float s in banners)
            {
                Vector3 p = At(s, Side() * (Arena3Route.HalfW(s) + R(3.5f, 10f)));
                if (!Claim(p, 1.5f)) continue;
                Banner(parent, p, R(0f, 360f), R(14f, 32f), rng.Next(1, 3), R(0.7f, 1.1f));
            }

            // sulite infipte in noroi, unele chiar in balti
            List<Arena3Route.Pool> pools = new List<Arena3Route.Pool>(Arena3Route.Pools);
            for (int i = 0; i < 16; i++)
            {
                Vector3 p;
                if (i < 6 && pools.Count > 0)
                {
                    Arena3Route.Pool pool = pools[rng.Next(pools.Count)];
                    Vector2 q = pool.centre + Random2() * pool.radius * 0.6f;
                    p = new Vector3(q.x, 0f, q.y);
                }
                else
                {
                    float s = R(58f, 142f);
                    p = At(s, Side() * (Arena3Route.HalfW(s) + R(1.2f, 12f)));
                }
                string path = rng.NextDouble() < 0.5 ? FieldArt + "SM_A2_Pike_Broken.fbx" : FieldArt + "SM_A2_Pike_Shaft.fbx";
                Prop(path, parent, p, R(0f, 360f), R(0.35f, 0.8f), 0f, R(12f, 50f));
            }

            // scuturi, coifuri, sageti, oase pe jumatate in apa
            Scatter(parent, FieldArt + "SM_A2_Shield_Heater.fbx", 8, 58f, 142f, 0.8f, 11f, 0.05f, 0.14f, 55f, 85f, 0.6f);
            Scatter(parent, FieldArt + "SM_A2_Helmet_Great.fbx", 3, 58f, 142f, 0.8f, 10f, 0.08f, 0.16f, 10f, 60f, 0.4f);
            Scatter(parent, FieldArt + "SM_A2_Helmet_Kettle.fbx", 3, 58f, 142f, 0.8f, 10f, 0.06f, 0.14f, 10f, 60f, 0.4f);
            Scatter(parent, FieldArt + "SM_A2_ArrowVolley_A.fbx", 2, 60f, 140f, 1f, 9f, 0.05f, 0.15f, 0f, 10f, 0.8f);
            Scatter(parent, FieldArt + "SM_A2_ArrowVolley_B.fbx", 2, 60f, 140f, 1f, 9f, 0.05f, 0.15f, 0f, 10f, 0.8f);
            Scatter(parent, FieldArt + "SM_A2_ArrowVolley_C.fbx", 2, 60f, 140f, 1f, 9f, 0.05f, 0.15f, 0f, 10f, 0.8f);
            Scatter(parent, Kit + "Bones_Pile.prefab", 4, 60f, 140f, 1f, 9f, 0.08f, 0.15f, 0f, 6f, 1f);
            Scatter(parent, FieldArt + "SM_A2_Barricade_Stakes.fbx", 3, 62f, 135f, 4f, 11f, 0.35f, 0.6f, 4f, 16f, 2.6f, true);

            // trebuseta rasturnata, pe jumatate in mlastina
            Vector3 t = At(110f, -14f);
            Claim(t, 6f);
            Prop(FieldArt + "SM_A2_Trebuchet_Wreck.fbx", parent, t, Arena3Route.Yaw(110f) + 32f, 1.25f, 0f, 9f, true);
        }

        // ------------------------------------------------------------------ c) mlastina

        static void Mire(Transform parent)
        {
            float end = Arena3Route.CausewayS;
            // copaci morti in apa, cioturi pe mal, busteni pe jumatate scufundati, bolovani cu muschi
            for (int i = 0; i < 9; i++)
            {
                float s = R(138f, end + 4f);
                Vector3 p = At(s, Side() * (Arena3Route.HalfW(s) + R(5f, 17f)));
                if (!Claim(p, 2.2f)) continue;
                GameObject tree = Prop(Kit + "Tree_Dead.prefab", parent, p, R(0f, 360f), R(0.5f, 1.1f), R(4.5f, 7f), R(2f, 14f), true);
                if (i % 2 == 0) Swap(tree, "M_Wood075", charred);
            }
            for (int i = 0; i < 7; i++)
            {
                float s = R(132f, end);
                Vector3 p = At(s, Side() * (Arena3Route.HalfW(s) + R(0.9f, 2.6f)));
                if (!Claim(p, 1.2f)) continue;
                Prop(Kit + "Stump_Roots.prefab", parent, p, R(0f, 360f), R(0.15f, 0.4f), 0f, R(0f, 12f), true);
            }
            for (int i = 0; i < 5; i++)
            {
                float s = R(140f, end + 6f);
                Vector3 p = At(s, Side() * (Arena3Route.HalfW(s) + R(5f, 14f)));
                if (!Claim(p, 1.8f)) continue;
                Prop(Kit + "Log_Fallen.prefab", parent, p, R(0f, 360f), R(0.35f, 0.6f), R(0.8f, 1.1f) * 0.85f, R(0f, 8f));
            }
            string[] boulders = { Kit + "Boulder_A.prefab", Kit + "Boulder_B.prefab", Kit + "Boulder_C.prefab" };
            for (int i = 0; i < 6; i++)
            {
                float s = R(130f, end + 8f);
                Vector3 p = At(s, Side() * (Arena3Route.HalfW(s) + R(6f, 20f)));
                if (!Claim(p, 1.8f)) continue;
                Prop(Pick(boulders), parent, p, R(0f, 360f), R(0.6f, 1.1f), R(1.4f, 2.4f), R(0f, 10f));
            }

            // trestii pe malul digului si pe marginea baltilor
            if (reeds != null)
            {
                Transform reedRoot = Group(parent, "Reeds");
                for (float s = 70f; s < end + 6f; s += R(4f, 7f))
                {
                    float hw = Arena3Route.HalfW(s);
                    Reeds(reedRoot, At(s, Side() * (hw + R(1.2f, 4.5f))), 1.4f, 7, 1.1f, 2.1f);
                }
                foreach (Arena3Route.Pool pool in Arena3Route.Pools)
                {
                    Vector2 q = pool.centre + Random2().normalized * pool.radius * R(0.85f, 1.05f);
                    Reeds(reedRoot, new Vector3(q.x, 0f, q.y), 1f, 5, 0.9f, 1.6f);
                }
            }
        }

        // ------------------------------------------------------------------ apa

        static void Water(Transform parent)
        {
            // baltile din etapa b: discuri de apa la nivelul celui mai jos punct de pe mal
            Mesh puddle = Load<Mesh>(Arena2ArtSetup.ArtFolder + "/FX/SM_A2_Puddle.asset");
            foreach (Arena3Route.Pool pool in Arena3Route.Pools)
            {
                float level = float.MaxValue;
                for (int i = 0; i < 16; i++)
                {
                    float a = i / 16f * Mathf.PI * 2f;
                    level = Mathf.Min(level, Ground(pool.centre.x + Mathf.Cos(a) * pool.radius * 0.9f, pool.centre.y + Mathf.Sin(a) * pool.radius * 0.9f));
                }
                GameObject go = new GameObject("Pool");
                go.layer = WaterLayer;
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(new Vector3(pool.centre.x, level - 0.04f, pool.centre.y), Quaternion.Euler(0f, R(0f, 360f), 0f));
                go.transform.localScale = new Vector3(pool.radius * 0.92f, 1f, pool.radius * 0.92f);
                go.AddComponent<MeshFilter>().sharedMesh = puddle;
                MeshRenderer r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = poolWater;
                r.shadowCastingMode = ShadowCastingMode.Off;
                MarkStatic(go);
            }

            // apa neagra: un singur plan peste bazin si valea din etapa c (sub teren unde malul e mai sus)
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "BlackWater";
            Object.DestroyImmediate(plane.GetComponent<Collider>());
            plane.layer = WaterLayer;
            plane.transform.SetParent(parent, false);
            plane.transform.SetPositionAndRotation(new Vector3(685f, Arena3Route.WaterY, 422.5f), Quaternion.identity);
            plane.transform.localScale = new Vector3(25f, 1f, 17.5f);
            MeshRenderer pr = plane.GetComponent<MeshRenderer>();
            pr.sharedMaterial = blackWater;
            pr.shadowCastingMode = ShadowCastingMode.Off;
            MarkStatic(plane);
        }

        // ------------------------------------------------------------------ pereti invizibili

        // de-a lungul vaii la 18 m de drum, apoi tot mai aproape, pana la marginea digului; inelul insulei e in Arena
        static void RoadContainment(Transform parent)
        {
            int i = 0;
            for (float s = 18f; s < Arena3Route.Length; s += 8f)
            {
                Vector3 c = Arena3Route.Point(s);
                if (Vector2.Distance(new Vector2(c.x, c.z), Arena3Route.IslandCentre) < IslandRing - 1f) break;
                float lat = Mathf.Lerp(18f, Arena3Route.HalfW(s) + 1.2f, Arena3Route.StageC(s));
                foreach (int side in new[] { -1, 1 })
                {
                    Vector3 p = c + Arena3Route.Right(s) * side * lat;
                    Contain(parent, $"Contain_{i++:00}", new Vector3(p.x, c.y, p.z), Arena3Route.Yaw(s), new Vector3(1f, 40f, 8.6f));
                }
            }
        }

        static void Contain(Transform parent, string name, Vector3 p, float yaw, Vector3 size)
        {
            GameObject go = new GameObject(name);
            go.layer = IgnoreRaycastLayer;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(new Vector3(p.x, p.y - 10f + size.y * 0.5f, p.z), Quaternion.Euler(0f, yaw, 0f));
            go.AddComponent<BoxCollider>().size = size;
            go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
        }

        // ------------------------------------------------------------------ utilitare de decor

        static Vector3 At(float s, float lateral)
        {
            Vector3 p = Arena3Route.Point(s) + Arena3Route.Right(s) * lateral;
            p.y = Ground(p.x, p.z);
            return p;
        }

        static Vector2 Random2()
        {
            float a = R(0f, Mathf.PI * 2f), r = Mathf.Sqrt(R(0f, 1f));
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }

        // un loc liber (fara alt obiect mare prea aproape)
        static bool Claim(Vector3 p, float radius)
        {
            foreach (Vector4 t in taken)
                if ((new Vector2(t.x - p.x, t.y - p.z)).sqrMagnitude < (t.z + radius) * (t.z + radius)) return false;
            taken.Add(new Vector4(p.x, p.z, radius, 0f));
            return true;
        }

        static void Scatter(Transform parent, string path, int count, float sMin, float sMax, float latMin, float latMax,
            float sinkMin, float sinkMax, float leanMin, float leanMax, float radius, bool collider = false)
        {
            for (int i = 0, tries = 0; i < count && tries < count * 6; tries++)
            {
                float s = R(sMin, sMax);
                Vector3 p = At(s, Side() * (Arena3Route.HalfW(s) + R(latMin, latMax)));
                if (!Claim(p, radius)) continue;
                Prop(path, parent, p, R(0f, 360f), R(sinkMin, sinkMax), 0f, R(leanMin, leanMax), collider);
                i++;
            }
        }

        // muta piesa ca partea ei de sus sa fie la top metri peste sol
        static void Settle(GameObject go, Vector3 p, float top)
        {
            if (!TryBounds(go, out Bounds b)) return;
            go.transform.position += Vector3.up * (Ground(p.x, p.z) + top - b.max.y);
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

        // stalp + panza in segmente inlantuite, ca in Arena 2
        static GameObject Banner(Transform parent, Vector3 p, float yaw, float lean, int segments, float sink)
        {
            GameObject root = new GameObject("Banner");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(new Vector3(p.x, Ground(p.x, p.z) - sink, p.z),
                Quaternion.AngleAxis(lean, Quaternion.Euler(0f, R(0f, 360f), 0f) * Vector3.right) * Quaternion.Euler(0f, yaw, 0f));
            Child(FieldArt + "SM_A2_BannerPole.fbx", root.transform, Vector3.zero);
            Transform[] segs = new Transform[segments];
            Transform holder = root.transform;
            for (int i = 0; i < segments; i++)
            {
                Transform seg = new GameObject("Seg" + i).transform;
                seg.SetParent(holder, false);
                seg.localPosition = i == 0 ? new Vector3(0f, 6f, 0.02f) : new Vector3(0f, -1f, 0f);
                if (i == 0) seg.rotation = Quaternion.Euler(0f, yaw, 0f);
                Child(BannerCloth[i], seg, Vector3.zero);
                segs[i] = seg;
                holder = seg;
            }
            StripColliders(root);
            BannerSway sway = root.AddComponent<BannerSway>();
            SetRefs(sway, "segments", segs);
            SetFloat(sway, "angle", R(4f, 6f));
            SetFloat(sway, "speed", R(0.4f, 0.6f));
            ClearStatic(root);
            return root;
        }

        static void Reeds(Transform parent, Vector3 p, float radius, int count, float hMin, float hMax)
        {
            if (crossCard == null || reeds == null) return;
            for (int i = 0; i < count; i++)
            {
                Vector3 q = p + new Vector3(R(-radius, radius), 0f, R(-radius, radius));
                Arena3Route.Hit hit = Arena3Route.Nearest(q.x, q.z);
                if (hit.dist < Arena3Route.HalfW(hit.s) + 0.4f) continue;
                float g = Ground(q.x, q.z);
                if (g < Arena3Route.WaterY - 0.7f) continue;
                float h = R(hMin, hMax);
                GameObject card = new GameObject("Reeds");
                card.transform.SetParent(parent, false);
                card.transform.SetPositionAndRotation(new Vector3(q.x, g - 0.05f, q.z), Quaternion.Euler(R(-6f, 6f), R(0f, 360f), R(-6f, 6f)));
                card.transform.localScale = new Vector3(h * R(0.8f, 1.1f), h, h * R(0.8f, 1.1f));
                card.AddComponent<MeshFilter>().sharedMesh = crossCard;
                MeshRenderer r = card.AddComponent<MeshRenderer>();
                r.sharedMaterial = reeds;
                r.shadowCastingMode = ShadowCastingMode.Off;
                MarkStatic(card);
            }
        }
    }
}
