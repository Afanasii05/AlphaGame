using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace Alpha.World.EditorTools
{
    // Poarta de nord si inceputul Drumului 2: un defileu scurt taiat in malul pajistii, inchis de o lespede cu rune
    // care coboara in pamant abia dupa cioplirea inimii. Coridorul e definit in coordonate locale (fata de Origin).
    public static partial class Arena1Builder
    {
        const string GateFolder = Folder + "/Art/Gate";
        const string GateFramePath = WorldKit + "SM_HeavyDoor_Frame.prefab";
        const string GateSlabPath = WorldKit + "SM_HeavyDoor_Slab.prefab";
        const string Road2StartName = "Road2_Start";

        // coridorul: iese din pajiste spre nord (opus intrarii), apoi coteste spre nord-est
        const float Road2Begin = 20f, Road2GateDistance = 30.5f, Road2Bend = 39f, Road2BendTurn = 50f, Road2Length = 14f;
        const float Road2HalfW = 2.7f, Road2CliffH = 16f, Road2CliffK = 2.2f, Road2Rise = 1.6f;
        const float Road2Shelf = 3f;
        // prelungirea drumului dincolo de capatul coridorului, prin malul de la marginea terenului, spre Drumul 2 (Arena 2)
        const float Road2Junction = 24f;
        static readonly Color GateRuneColor = new Color(0.35f, 1f, 0.5f);

        static List<Vector2> road2Pts;
        static List<float> road2S;

        // ------------------------------------------------------------------ geometrie

        static Vector2 Road2Dir()
        {
            Vector3 entry = RoadCtrl[RoadCtrl.Length - 2];
            Vector2 toEntry = new Vector2(entry.x - MeadowCenter.x, entry.z - MeadowCenter.y).normalized;
            return -toEntry;
        }

        static Vector2 Rotate(Vector2 v, float degrees)
        {
            // rotatie in sensul acelor de ceas privita de sus, ca yaw-ul din Unity
            float r = -degrees * Mathf.Deg2Rad;
            return new Vector2(v.x * Mathf.Cos(r) - v.y * Mathf.Sin(r), v.x * Mathf.Sin(r) + v.y * Mathf.Cos(r));
        }

        static void EnsureRoad2Path()
        {
            if (road2Pts != null) return;
            road2Pts = new List<Vector2>();
            road2S = new List<float>();
            Vector2 dir = Road2Dir(), dir2 = Rotate(dir, Road2BendTurn);
            Vector2 a = MeadowCenter + dir * Road2Begin;
            Vector2 b0 = MeadowCenter + dir * (Road2Bend - 7f);
            Vector2 corner = MeadowCenter + dir * Road2Bend;
            Vector2 b2 = corner + dir2 * 7f;
            Vector2 end = corner + dir2 * Road2Length;

            AddRoad2Point(a);
            for (float d = 2f; d < Road2Bend - 7f - Road2Begin; d += 2f) AddRoad2Point(a + dir * d);
            for (int i = 0; i <= 10; i++)
            {
                float t = i / 10f;
                AddRoad2Point((1f - t) * (1f - t) * b0 + 2f * (1f - t) * t * corner + t * t * b2);
            }
            for (float d = 9f; d < Road2Length; d += 2f) AddRoad2Point(corner + dir2 * d);
            AddRoad2Point(end);
        }

        static void AddRoad2Point(Vector2 p)
        {
            if (road2Pts.Count > 0 && (p - road2Pts[road2Pts.Count - 1]).sqrMagnitude < 0.01f) return;
            road2S.Add(road2Pts.Count == 0 ? 0f : road2S[road2S.Count - 1] + (p - road2Pts[road2Pts.Count - 1]).magnitude);
            road2Pts.Add(p);
        }

        static float Road2TotalLength
        {
            get { EnsureRoad2Path(); return road2S[road2S.Count - 1]; }
        }

        // distanta pana la axa coridorului si pozitia pe el (s, in metri de la inceput)
        static float Road2Distance(float lx, float lz, out float s)
        {
            EnsureRoad2Path();
            float best = float.MaxValue;
            s = 0f;
            for (int j = 0; j < road2Pts.Count - 1; j++)
            {
                Vector2 a = road2Pts[j], ab = road2Pts[j + 1] - a;
                float t = Mathf.Clamp01(((lx - a.x) * ab.x + (lz - a.y) * ab.y) / (ab.sqrMagnitude + 1e-6f));
                float dx = lx - (a.x + ab.x * t), dz = lz - (a.y + ab.y * t);
                float d = dx * dx + dz * dz;
                if (d >= best) continue;
                best = d;
                s = road2S[j] + ab.magnitude * t;
            }
            // segmentul de legatura: continua directia finala, podeaua ramane la nivelul capatului
            Vector2 end = road2Pts[road2Pts.Count - 1], dir = (end - road2Pts[road2Pts.Count - 2]).normalized;
            float tj = Mathf.Clamp(((lx - end.x) * dir.x + (lz - end.y) * dir.y), 0f, Road2Junction);
            float jx = lx - (end.x + dir.x * tj), jz = lz - (end.y + dir.y * tj);
            float dj = jx * jx + jz * jz;
            if (tj > 0f && dj < best)
            {
                best = dj;
                s = road2S[road2S.Count - 1] + tj;
            }
            return Mathf.Sqrt(best);
        }

        // pozitia pe segmentul de legatura (s masurat de la capatul coridorului)
        static Vector3 Road2JunctionWorld(float along)
        {
            EnsureRoad2Path();
            Vector2 end = road2Pts[road2Pts.Count - 1], dir = (end - road2Pts[road2Pts.Count - 2]).normalized;
            Vector2 q = end + dir * along;
            Vector3 p = Origin + new Vector3(q.x, 0f, q.y);
            p.y = Ground(p.x, p.z);
            return p;
        }

        static float Road2Floor(float s) => MeadowHeight + Road2Rise * Smooth(Road2GateDistance - Road2Begin, Road2TotalLength, s);

        static void Road2Frame(float s, out Vector3 local, out Vector2 forward)
        {
            EnsureRoad2Path();
            int j = 0;
            while (j < road2S.Count - 2 && road2S[j + 1] < s) j++;
            float t = Mathf.Clamp01((s - road2S[j]) / Mathf.Max(road2S[j + 1] - road2S[j], 1e-4f));
            Vector2 p = Vector2.Lerp(road2Pts[j], road2Pts[j + 1], t);
            forward = (road2Pts[j + 1] - road2Pts[j]).normalized;
            local = new Vector3(p.x, Road2Floor(s), p.y);
        }

        // inaltimea maxima permisa de coridor: podea + pereti abrupti; HeightAt ia minimul
        static float Road2Carve(float lx, float lz, float h)
        {
            if (lz < MeadowCenter.y + Road2Begin - 12f) return h;
            float d = Road2Distance(lx, lz, out float s);
            if (d > 40f) return h;
            // dincolo de poarta peretele de teren se retrage putin: pe treapta stau stancile coridorului
            float sGate = Road2GateDistance - Road2Begin;
            float shelf = Road2Shelf * Smooth(sGate + 3f, sGate + 6f, s);
            float wall = Mathf.Max(0f, d - Road2HalfW - shelf);
            float cliff = Road2CliffH * (1f - Mathf.Exp(-wall / Road2CliffK)) * (0.75f + 0.5f * Fbm(lx * 0.06f + 300f, lz * 0.06f + 300f));
            float rough = (Fbm(lx * 0.25f + 610f, lz * 0.25f + 610f) - 0.5f) * 1.2f * Mathf.Clamp01(wall / 2f);
            return Mathf.Min(h, Road2Floor(s) + cliff + rough);
        }

        // cat de mult e pixelul pe podeaua coridorului (dincolo de pajiste); 0 in afara
        static float Road2PaintWeight(float lx, float lz, float jitter)
        {
            if (lz < MeadowCenter.y + Road2Begin - 6f) return 0f;
            float d = Road2Distance(lx, lz, out float s) + jitter;
            float beyondMeadow = Smooth(MeadowRadius - 4f - Road2Begin, MeadowRadius - Road2Begin, s);
            return (1f - Smooth(Road2HalfW - 0.4f, Road2HalfW + 1.6f, d)) * beyondMeadow;
        }

        static Vector3 Road2World(float s)
        {
            Road2Frame(s, out Vector3 local, out _);
            Vector3 p = Origin + local;
            p.y = Ground(p.x, p.z);
            return p;
        }

        static float Road2Yaw(float s)
        {
            Road2Frame(s, out _, out Vector2 f);
            return Mathf.Atan2(f.x, f.y) * Mathf.Rad2Deg;
        }

        // ------------------------------------------------------------------ constructie

        static void BuildRoad2(Transform arena, Vector3 c, ParticleSystem dust, Arena1FxBuilder.Result fx, Text hint, Arena1Encounter encounter)
        {
            ClearRoad2Corridor(arena, FindSceneRoot(AtmosphereRoot));
            Transform root = Group(arena, "Road2_NorthGate");
            float sGate = Road2GateDistance - Road2Begin;
            Road2Gate gate = BuildGate(root, sGate, dust, fx, hint);
            BuildRoad2Walls(root, sGate);
            BuildRoad2Marker(root);
            if (gate != null) SetRef(encounter, "gate", gate);
        }

        static Transform FindSceneRoot(string name)
        {
            foreach (GameObject go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                if (go.name == name) return go.transform;
            return null;
        }

        // scoate decorul care ar sta in drumul spre poarta (scatter, copaci, atmosfera)
        static void ClearRoad2Corridor(params Transform[] roots)
        {
            EnsureRoad2Path();
            HashSet<GameObject> doomed = new HashSet<GameObject>();
            float sFrom = MeadowRadius - 5f - Road2Begin;
            foreach (Transform root in roots)
            {
                if (root == null) continue;
                foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (r is ParticleSystemRenderer) continue;
                    if (r.GetComponentInParent<ArenaSeal>(true) != null || r.GetComponentInParent<RiddleStone>(true) != null) continue;
                    Bounds b = r.bounds;
                    if (Mathf.Max(b.size.x, b.size.z) > 30f) continue;
                    if (!TouchesRoad2(b, Road2HalfW + 1.2f, sFrom)) continue;
                    GameObject top = PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject);
                    doomed.Add(top != null ? top : r.gameObject);
                }
            }
            foreach (GameObject go in doomed)
                if (go != null) Object.DestroyImmediate(go);
        }

        static bool TouchesRoad2(Bounds b, float clearance, float sFrom)
        {
            for (int j = 0; j < road2Pts.Count; j++)
            {
                if (road2S[j] < sFrom) continue;
                Vector2 p = road2Pts[j] + new Vector2(Origin.x, Origin.z);
                float qx = Mathf.Clamp(p.x, b.min.x, b.max.x), qz = Mathf.Clamp(p.y, b.min.z, b.max.z);
                if (new Vector2(p.x - qx, p.y - qz).magnitude < clearance) return true;
            }
            return false;
        }

        static Road2Gate BuildGate(Transform root, float sGate, ParticleSystem dust, Arena1FxBuilder.Result fx, Text hint)
        {
            Vector3 g = Road2World(sGate);
            float yaw = Road2Yaw(sGate);
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 right = rot * Vector3.right, forward = rot * Vector3.forward;

            GameObject gateGo = new GameObject("NorthGate");
            gateGo.transform.SetParent(root, false);
            gateGo.transform.SetPositionAndRotation(g, rot);
            Material green = GateGlyphMaterial();

            GameObject frame = Spawn(GateFramePath, gateGo.transform, g, yaw);
            if (frame != null)
            {
                frame.transform.position = g + Vector3.down * 0.25f;
                Swap(frame, "M_GlyphGlow", green);
            }

            // lespedea are pivotul pe o margine: o mutam ca sa stea centrata in deschidere
            GameObject slab = Spawn(GateSlabPath, gateGo.transform, g, yaw);
            if (slab != null)
            {
                Vector3 s = slab.transform.localScale;
                slab.transform.localScale = new Vector3(s.x, s.y * 1.06f, s.z);
                slab.transform.position = g + right * 1.48f + Vector3.down * 0.05f;
                Swap(slab, "M_GlyphGlow", green);
                if (slab.GetComponentInChildren<Collider>(true) == null) EnsureColliders(slab);
                ClearStatic(slab);
                slab.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            }

            // zid invizibil de rezerva + carving pe NavMesh pana se deschide poarta
            GameObject blocker = new GameObject("Blocker");
            blocker.transform.SetParent(gateGo.transform, false);
            blocker.transform.SetPositionAndRotation(g, rot);
            BoxCollider box = blocker.AddComponent<BoxCollider>();
            box.size = new Vector3(3.4f, 5.2f, 0.9f);
            box.center = new Vector3(0f, 2.6f, 0f);
            NavMeshObstacle obstacle = blocker.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = box.size;
            obstacle.center = box.center;
            obstacle.carving = true;
            blocker.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

            Light rune = new GameObject("RuneLight").AddComponent<Light>();
            rune.transform.SetParent(gateGo.transform, false);
            rune.transform.position = g + Vector3.up * 4.2f - forward * 1.8f;
            rune.type = LightType.Point;
            rune.color = GateRuneColor;
            rune.range = 9f;
            rune.intensity = 2.6f;
            rune.shadows = LightShadows.None;

            Transform dustPoint = new GameObject("DustPoint").transform;
            dustPoint.SetParent(gateGo.transform, false);
            dustPoint.position = g + Vector3.up * 0.2f;

            // lanturi atarnate sub buiandrug, pe fata dinspre arena, si lumanari la picioarele portii
            Physics.SyncTransforms();
            foreach (float offset in new[] { -1.05f, 0.85f })
                HangChain(gateGo.transform, g - forward * 0.62f + right * offset, yaw);
            Candles(gateGo.transform, g - forward * 2.4f - right * 2.3f, true);
            Candles(gateGo.transform, g - forward * 2.2f + right * 2.4f, false);
            Prop(Kit + "Bones_Pile.prefab", gateGo.transform, g - forward * 2.6f + right * 1.6f, R(0f, 360f), 0.05f, 0.22f, false, false);
            for (int i = 0; i < 2; i++)
                Prop(Kit + "Rubble_Pile.prefab", gateGo.transform, g - forward * R(1.6f, 2.4f) + right * (i == 0 ? -1f : 1f) * R(2.6f, 3.2f), R(0f, 360f), 0.08f, 0f, false, false);

            List<Renderer> glyphs = new List<Renderer>();
            foreach (Renderer r in gateGo.GetComponentsInChildren<Renderer>(true))
                if (System.Array.IndexOf(r.sharedMaterials, green) >= 0) glyphs.Add(r);

            Road2Gate gate = gateGo.AddComponent<Road2Gate>();
            if (slab != null) SetRef(gate, "slab", slab.transform);
            SetRefs(gate, "glyphRenderers", glyphs.ToArray());
            SetRef(gate, "glyphMaterial", green);
            SetRef(gate, "runeLight", rune);
            SetRefs(gate, "blockers", new Object[] { box });
            SetRef(gate, "obstacle", obstacle);
            SetRef(gate, "dustPoint", dustPoint);
            SetRef(gate, "dustPrefab", dust);
            SetRef(gate, "burstPrefab", fx.burst);
            SetRefs(gate, "debrisPrefabs", fx.debris);
            SetRef(gate, "screenHint", hint);
            ClearStatic(gateGo);
            return gate;
        }

        static void Swap(GameObject go, string materialName, Material to)
        {
            if (to == null) return;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name == materialName) { mats[i] = to; changed = true; }
                if (changed) r.sharedMaterials = mats;
            }
        }

        // runele portii sunt verzi ca inima golemului; copie a materialului din kit
        static Material GateGlyphMaterial()
        {
            EnsureFolder(Folder + "/Art", "Gate");
            string path = GateFolder + "/M_Gate_GlyphGreen.mat";
            Material source = AssetDatabase.LoadAssetAtPath<Material>("Assets/World/Materials/M_GlyphGlow.mat");
            if (source == null)
            {
                foreach (string guid in AssetDatabase.FindAssets("M_GlyphGlow t:Material"))
                {
                    source = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                    if (source != null) break;
                }
            }
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = source != null ? new Material(source) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (source != null)
            {
                mat.shader = source.shader;
                mat.CopyPropertiesFromMaterial(source);
            }
            mat.name = "M_Gate_GlyphGreen";
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.16f, 0.34f, 0.2f));
            mat.SetColor("_EmissionColor", GateRuneColor * 1.7f);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ziduri ruinate si stanci care leaga rama de maluri, plus pereti de stanca pe coridor
        static void BuildRoad2Walls(Transform root, float sGate)
        {
            Transform walls = Group(root, "Walls");
            Vector3 g = Road2World(sGate);
            float yaw = Road2Yaw(sGate);
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 right = rot * Vector3.right, forward = rot * Vector3.forward;

            for (int side = -1; side <= 1; side += 2)
            {
                GameObject wall = Prop(WorldKit + "SM_RuinWall_Straight.prefab", walls, g + right * side * 5.3f + forward * 0.2f, yaw + R(-4f, 4f), 0.6f, 0f, true);
                if (wall != null) MarkStatic(wall);
                GameObject wall2 = Prop(WorldKit + "SM_RuinWall_Broken.prefab", walls, g + right * side * 8.6f - forward * 0.6f, yaw + side * R(12f, 22f), 0.5f, 0f, true);
                if (wall2 != null) MarkStatic(wall2);
                Prop(Kit + "Pillar_Broken.prefab", walls, g + right * side * 3.9f - forward * 1.9f, R(0f, 360f), 0.15f, R(2.2f, 3f), true);
                Prop(Pick(Boulders), walls, g + right * side * R(4.4f, 5.4f) - forward * R(2.4f, 3.2f), R(0f, 360f), 0.3f, R(1.4f, 2.2f), true);
                Prop(Pick(Rocks), walls, g + right * side * R(6.5f, 7.5f) - forward * R(1f, 2f), R(0f, 360f), 0.3f, R(1.8f, 2.6f), true);
                GrassTufts(walls, g + right * side * 4.2f - forward * 2.6f, 0.8f, 4);
            }
            Prop(WorldKit + "SM_StatueHead_Fallen.prefab", walls, g - forward * 3.2f + right * -4.6f, yaw + 140f, 0.5f, 1.6f, true);

            // pereti de stanca pe ambele parti ale coridorului, dincolo de poarta
            float total = Road2TotalLength;
            List<Vector3> axis = new List<Vector3>();
            for (float s = sGate - 4f; s <= total; s += 1f) axis.Add(Road2World(s));
            for (int side = -1; side <= 1; side += 2)
                for (float s = sGate + 6.5f + R(0f, 1.5f); s < total + 1f; s += R(6f, 7.5f))
                    Road2Cliff(walls, s, side, axis);
            // capatul coridorului: o stanca inalta, drumul cotind dupa ea
            Road2Frame(total, out _, out Vector2 f);
            Vector3 endDir = new Vector3(f.x, 0f, f.y);
            // drumul continua spre Arena 2: stanca e creata si scoasa imediat, ca restul decorului sa ramana identic
            GameObject endCliff = WallCliff(WorldKit + "SM_Cliff_A.prefab", walls, Road2World(total), endDir, 1.5f, Mathf.Atan2(-f.x, -f.y) * Mathf.Rad2Deg + R(-8f, 8f), R(14f, 17f), 0.2f, axis);
            if (endCliff != null) Object.DestroyImmediate(endCliff);

            // pietris si iarba pe marginile drumului
            for (float s = sGate + 2.5f; s < total; s += 3f)
            {
                Road2Frame(s, out Vector3 local, out Vector2 fw);
                Vector3 side = new Vector3(fw.y, 0f, -fw.x) * (Rng01() < 0.5f ? -1f : 1f);
                Vector3 p = Origin + local + side * (Road2HalfW - R(0.1f, 0.6f));
                if (Rng01() < 0.5f)
                    for (int k = 0; k < 3; k++)
                        Prop(Pick(Rocks), walls, p + new Vector3(R(-0.5f, 0.5f), 0f, R(-0.5f, 0.5f)), R(0f, 360f), 0.05f, R(0.15f, 0.4f), false, false);
                else GrassTufts(walls, p, 0.7f, 3);
            }
            foreach (float k in new[] { 0.45f, 0.95f })
            {
                Vector3 p = Road2World(Mathf.Lerp(sGate, total, k));
                Light fill = new GameObject("FillLight").AddComponent<Light>();
                fill.transform.SetParent(walls, false);
                fill.transform.position = p + Vector3.up * 4.5f;
                fill.type = LightType.Point;
                fill.color = new Color(0.55f, 0.7f, 1f);
                fill.range = 12f;
                fill.intensity = 1.6f;
                fill.shadows = LightShadows.None;
            }
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = Road2World(R(sGate + 3f, total - 1f));
                GameObject shroom = Prop(Kit + "Mushrooms_Glow.prefab", walls, p + new Vector3(R(-1f, 1f), 0f, R(-1f, 1f)) * Road2HalfW * 0.8f, R(0f, 360f), 0.02f, 0f, false, false);
                SetLights(shroom, i == 0, false);
            }
            BuildRoad2Junction(walls, axis, f);
        }

        // pereti de stanca pe legatura spre Arena 2, cu generator propriu ca sa nu schimbe restul coridorului
        static void BuildRoad2Junction(Transform walls, List<Vector3> axis, Vector2 f)
        {
            System.Random saved = rng;
            rng = new System.Random(Seed + 20);
            List<Vector3> extended = new List<Vector3>(axis);
            for (float a = 1f; a <= Road2Junction; a += 1f) extended.Add(Road2JunctionWorld(a));
            Vector3 outwardBase = new Vector3(f.y, 0f, -f.x);
            for (int side = -1; side <= 1; side += 2)
                for (float a = R(1f, 3f); a < Road2Junction - 8f; a += R(6f, 7.5f))
                {
                    Vector3 outward = outwardBase * side;
                    Vector3 p = Road2JunctionWorld(a);
                    bool spire = rng.NextDouble() < 0.25;
                    string path = spire ? WorldKit + "SM_Cliff_Spire.prefab" : WorldKit + "SM_Cliff_A.prefab";
                    float yaw = spire ? R(0f, 360f) : Mathf.Atan2(-outward.x, -outward.z) * Mathf.Rad2Deg + R(-6f, 6f);
                    WallCliff(path, walls, p, outward, Road2HalfW, yaw, spire ? R(19f, 25f) : R(16f, 21f), R(0.1f, 0.5f), extended);
                }
            rng = saved;
        }

        static float Rng01() => (float)rng.NextDouble();

        static GameObject Road2Cliff(Transform parent, float s, int side, List<Vector3> axis)
        {
            Road2Frame(s, out _, out Vector2 f);
            Vector3 p = Road2World(s);
            Vector3 outward = new Vector3(f.y, 0f, -f.x) * side;
            bool spire = rng.NextDouble() < 0.3;
            string path = spire ? WorldKit + "SM_Cliff_Spire.prefab" : WorldKit + "SM_Cliff_A.prefab";
            float yaw = spire ? R(0f, 360f) : Mathf.Atan2(-outward.x, -outward.z) * Mathf.Rad2Deg + R(-6f, 6f);
            float height = spire ? R(19f, 25f) : R(16f, 21f);
            return WallCliff(path, parent, p, outward, Road2HalfW, yaw, height, R(0.1f, 0.5f), axis);
        }

        static void BuildRoad2Marker(Transform root)
        {
            float total = Road2TotalLength;
            Vector3 p = Road2World(total - 1.5f);
            GameObject marker = new GameObject(Road2StartName);
            marker.transform.SetParent(root, false);
            marker.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, Road2Yaw(total - 1.5f), 0f));
            Debug.Log($"[Arena1Builder] Road2_Start la {p} (yaw {Road2Yaw(total - 1.5f):F1}).");
        }
    }
}
