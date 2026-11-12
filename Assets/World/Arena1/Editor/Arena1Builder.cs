using System.Collections.Generic;
using Alpha.Combat;
using Alpha.Enemies;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Alpha.World.EditorTools
{
    // Construieste zona de start + drumul + Arena 1 in scena World. Se poate rula de mai multe ori,
    // reface doar radacinile Arena1_* (shrine-ul copiat din SampleScene se pastreaza daca exista deja).
    public static partial class Arena1Builder
    {
        const string WorldScenePath = "Assets/Scenes/World.unity";
        const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        const string Folder = "Assets/World/Arena1";
        const string TerrainDataPath = Folder + "/Terrain/Arena1_TerrainData.asset";
        const string BossPath = Folder + "/Prefabs/StoneGolem_Boss.prefab";
        const string SpikePath = Folder + "/Prefabs/RockSpike_Boss.prefab";
        const string ShrineRoot = "Arena1_StartShrine", TerrainRoot = "Arena1_Terrain", RoadRoot = "Arena1_Road", ArenaRoot = "Arena1_Arena";
        const int Seed = 1337;

        const string GolemPath = "Assets/Enemies/StoneGolem/StoneGolem.prefab";
        const string RockSpikePath = "Assets/Prefabs/RockSpike/RockSpike.prefab";
        const string DustPath = "Assets/VFX/StompVFX.prefab";
        const string CinderDataPath = "Assets/World/Terrain/CinderHollow_TerrainData.asset";
        const string GrassTexPath = "Assets/World/Terrain/T_GrassBlades.png";
        const string Kit = "Assets/RogueKit/Prefabs/", WorldKit = "Assets/World/Prefabs/", Env = "Assets/Environment/";

        static readonly string[] LayerPaths =
        {
            Env + "Terrain/TL_Grass007.terrainlayer", Env + "Terrain/TL_Ground037.terrainlayer", Env + "Terrain/TL_Gravel043.terrainlayer",
            Env + "Terrain/TL_Rock064.terrainlayer", "Assets/World/Terrain/TL_Moss002.terrainlayer", "Assets/World/Terrain/TL_Ground068.terrainlayer",
            Env + "Terrain/TL_Ground085.terrainlayer"
        };
        const int Grass = 0, Dirt = 1, Gravel = 2, Rock = 3, Moss = 4, Patches = 5, Worn = 6;

        static readonly string[] ShrineObjects = { "Sword", "Main Camera", "Player", "EventSystem", "Cutscene_SwordPulling", "Podium", "Sanctum_Dressing", "Vegetation" };
        static readonly string[] Rocks = { Env + "Rocks/Prefabs/Rock_A.prefab", Env + "Rocks/Prefabs/Rock_B.prefab", Env + "Rocks/Prefabs/Rock_C.prefab" };
        static readonly string[] Bushes = { Env + "Bushes/Prefabs/Bush_Large.prefab", Env + "Bushes/Prefabs/Bush_Medium.prefab", Env + "Bushes/Prefabs/Bush_Small.prefab" };
        static readonly string[] Boulders = { Kit + "Boulder_A.prefab", Kit + "Boulder_B.prefab", Kit + "Boulder_C.prefab" };
        static readonly string[] Cliffs = { WorldKit + "SM_Cliff_A.prefab", WorldKit + "SM_Cliff_B.prefab", WorldKit + "SM_Cliff_Low.prefab", WorldKit + "SM_Cliff_Ledge.prefab", WorldKit + "SM_Cliff_Spire.prefab" };
        static readonly string[] RuinWalls = { WorldKit + "SM_RuinWall_Straight.prefab", WorldKit + "SM_RuinWall_Broken.prefab", WorldKit + "SM_RuinWall_Low.prefab" };

        // geometrie (coordonate locale fata de Origin; local = pozitia din SampleScene pentru shrine)
        static readonly Vector3 Origin = new Vector3(250f, 0f, 0f);
        static readonly Vector3 TerrainOffset = new Vector3(-100f, -30f, -70f);
        static readonly Vector3 TerrainSize = new Vector3(200f, 90f, 320f);
        const int HeightRes = 513, AlphaRes = 512;
        static readonly Rect ShrinePad = Rect.MinMaxRect(-30f, -27f, 14f, 12f);
        const float PadRadius = 5f, PadBankH = 8f, PadBankK = 6f;
        static readonly Vector2 MeadowCenter = new Vector2(12f, 196f);
        const float MeadowRadius = 26f, MeadowHeight = 6f, MeadowBankH = 12f, MeadowBankK = 6f, RingRadius = 18f;

        // drumul: 0-4 Pilgrim Way, 5-8 defileul, 9-11 poarta veche
        static readonly Vector3[] RoadCtrl =
        {
            new Vector3(3f, 0.5f, 6f), new Vector3(8f, 1f, 22f), new Vector3(18f, 2.5f, 40f), new Vector3(22f, 4f, 58f), new Vector3(16f, 5f, 76f),
            new Vector3(6f, 4.5f, 92f), new Vector3(-6f, 3.5f, 108f), new Vector3(-10f, 3f, 124f), new Vector3(-4f, 3f, 140f),
            new Vector3(6f, 4f, 154f), new Vector3(10f, 5.5f, 168f), new Vector3(12f, 6f, 180f)
        };
        static readonly float[] StretchHalfWidth = { 4f, 2.6f, 3.5f };
        static readonly float[] StretchBankH = { 9f, 24f, 13f };
        static readonly float[] StretchBankK = { 7f, 3.5f, 5f };
        const float GorgeShelf = 3.5f;
        const int RoadSteps = 12;

        struct RoadHit { public float dist, y, halfW, bankH, bankK; public int stretch; }

        static System.Random rng;
        static readonly HashSet<string> missingPaths = new HashSet<string>();
        static readonly List<Vector3> roadPts = new List<Vector3>();
        static readonly List<Vector3> roadParams = new List<Vector3>();
        static readonly List<float> roadS = new List<float>();
        static readonly List<int> roadStretch = new List<int>();
        static float[] ctrlS;
        static TerrainData hubData;
        static Vector3 hubPos;
        static Material hubMaterial;
        static string hubTag = "Untagged";
        static Terrain terrain;
        static int placed;

        [MenuItem("Tools/Alpha/World/Build Arena 1")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            rng = new System.Random(Seed);
            missingPaths.Clear();
            placed = 0;

            Scene world = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);
            SceneManager.SetActiveScene(world);
            EnsureFolder(Folder, "Terrain");
            EnsureFolder(Folder, "Prefabs");
            EnsureFolder(Folder, "NavMesh");

            // a) shrine-ul + datele terenului hub din SampleScene (SampleScene nu se salveaza)
            Scene sample = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Additive);
            CaptureHub(sample);
            bool shrineCopied = CopyShrine(world, sample);
            EditorSceneManager.CloseScene(sample, true);
            SceneManager.SetActiveScene(world);
            GameObject playerRig = FindRoot(world, "PlayerRig");
            if (playerRig != null) playerRig.SetActive(false);   // jucatorul vine acum din shrine
            DisableStartingSword(world);

            foreach (string root in new[] { TerrainRoot, RoadRoot, ArenaRoot, AtmosphereRoot })
            {
                GameObject old = FindRoot(world, root);
                if (old != null) Object.DestroyImmediate(old);
            }

            BuildRoadSpline();
            terrain = BuildTerrain();
            PrepareDressing();
            int roadProps = BuildRoad();
            GameObject spike = BuildSpikeVariant();
            GameObject boss = BuildBossVariant(spike);
            int navSurfaces = BuildArena(boss);

            EditorSceneManager.MarkSceneDirty(world);
            EditorSceneManager.SaveScene(world);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Arena1Builder] Gata. Shrine copiat acum: {shrineCopied}, decor drum: {roadProps} obiecte, NavMesh: {navSurfaces} suprafete, " +
                      $"boss: {(boss != null ? BossPath : "LIPSA")}, asset-uri lipsa: {missingPaths.Count}. Arena e la {Origin + new Vector3(MeadowCenter.x, MeadowHeight, MeadowCenter.y)}.");
        }

        // ------------------------------------------------------------------ shrine din SampleScene

        static void CaptureHub(Scene sample)
        {
            GameObject env = FindRoot(sample, "Environment");
            Transform hubT = env != null ? env.transform.Find("HubTerrain") : null;
            Terrain hub = hubT != null ? hubT.GetComponent<Terrain>() : null;
            hubMaterial = null;
            hubTag = "Untagged";
            if (hub == null)
            {
                Debug.LogError("[Arena1Builder] Nu am gasit Environment/HubTerrain in SampleScene, zona de start va fi plata.");
                hubData = null;
                return;
            }
            hubData = hub.terrainData;
            hubPos = hub.GetPosition();
            hubMaterial = hub.materialTemplate;
            hubTag = hub.gameObject.tag;
        }

        static bool CopyShrine(Scene world, Scene sample)
        {
            if (FindRoot(world, ShrineRoot) != null) return false;
            GameObject shrine = new GameObject(ShrineRoot);
            SceneManager.MoveGameObjectToScene(shrine, world);
            shrine.transform.position = Origin;

            GameObject env = FindRoot(sample, "Environment");
            Transform vegetation = env != null ? env.transform.Find("Vegetation") : null;
            if (vegetation != null) vegetation.SetParent(null, true);

            foreach (string name in ShrineObjects)
            {
                GameObject go = name == "Vegetation" ? (vegetation != null ? vegetation.gameObject : null) : FindRoot(sample, name);
                if (go == null) { Debug.LogError($"[Arena1Builder] Lipseste din SampleScene: {name}"); continue; }
                SceneManager.MoveGameObjectToScene(go, world);
                go.transform.SetParent(shrine.transform, false);   // local = pozitia veche din SampleScene
            }
            return true;
        }

        // in World jucatorul porneste fara sabie; o primeste din cutscene-ul de pe Podium (PlayCutscene)
        static void DisableStartingSword(Scene world)
        {
            GameObject shrine = FindRoot(world, ShrineRoot);
            DebugingScript[] starters = shrine != null ? shrine.GetComponentsInChildren<DebugingScript>(true) : new DebugingScript[0];
            if (starters.Length == 0) Debug.LogWarning("[Arena1Builder] Nu am gasit DebugingScript in shrine, verifica manual sabia de start.");
            foreach (DebugingScript starter in starters) SetBool(starter, "enquipAtStart", false);
        }

        static float HubHeight(float x, float z)
        {
            if (hubData == null) return 0.5f;
            Vector3 size = hubData.size;
            return hubData.GetInterpolatedHeight(Mathf.Clamp01((x - hubPos.x) / size.x), Mathf.Clamp01((z - hubPos.z) / size.z)) + hubPos.y;
        }

        // ------------------------------------------------------------------ drumul

        static void BuildRoadSpline()
        {
            Vector3[] ctrl = (Vector3[])RoadCtrl.Clone();
            ctrl[0].y = HubHeight(ctrl[0].x, ctrl[0].z);
            roadPts.Clear(); roadParams.Clear(); roadS.Clear(); roadStretch.Clear();
            ctrlS = new float[ctrl.Length];

            for (int i = 0; i < ctrl.Length - 1; i++)
            {
                Vector3 p0 = ctrl[Mathf.Max(i - 1, 0)], p1 = ctrl[i], p2 = ctrl[i + 1], p3 = ctrl[Mathf.Min(i + 2, ctrl.Length - 1)];
                for (int step = 0; step < RoadSteps; step++)
                    AddRoadPoint(CatmullRom(p0, p1, p2, p3, step / (float)RoadSteps), i, step / (float)RoadSteps);
            }
            AddRoadPoint(ctrl[ctrl.Length - 1], ctrl.Length - 2, 1f);
            for (int i = 0; i < ctrl.Length; i++) ctrlS[i] = roadS[Mathf.Min(i * RoadSteps, roadS.Count - 1)];
        }

        static void AddRoadPoint(Vector3 p, int segment, float t)
        {
            int a = StretchOf(segment), b = StretchOf(segment + 1);
            roadParams.Add(new Vector3(Mathf.Lerp(StretchHalfWidth[a], StretchHalfWidth[b], t),
                                       Mathf.Lerp(StretchBankH[a], StretchBankH[b], t), Mathf.Lerp(StretchBankK[a], StretchBankK[b], t)));
            roadStretch.Add(t < 0.5f ? a : b);
            roadS.Add(roadPts.Count == 0 ? 0f : roadS[roadS.Count - 1] + Flat(p - roadPts[roadPts.Count - 1]).magnitude);
            roadPts.Add(p);
        }

        static int StretchOf(int ctrlIndex) => ctrlIndex <= 4 ? 0 : ctrlIndex <= 8 ? 1 : 2;

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) =>
            0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (3f * p1 - p0 - 3f * p2 + p3) * t * t * t);

        static RoadHit NearestRoad(float px, float pz)
        {
            RoadHit best = new RoadHit { dist = float.MaxValue };
            for (int j = 0; j < roadPts.Count - 1; j++)
            {
                Vector3 a = roadPts[j], b = roadPts[j + 1];
                float abx = b.x - a.x, abz = b.z - a.z;
                float t = Mathf.Clamp01(((px - a.x) * abx + (pz - a.z) * abz) / (abx * abx + abz * abz + 1e-6f));
                float dx = px - (a.x + abx * t), dz = pz - (a.z + abz * t);
                float d = dx * dx + dz * dz;
                if (d >= best.dist) continue;
                Vector3 par = Vector3.Lerp(roadParams[j], roadParams[j + 1], t);
                best = new RoadHit { dist = d, y = Mathf.Lerp(a.y, b.y, t), halfW = par.x, bankH = par.y, bankK = par.z, stretch = roadStretch[j] };
            }
            best.dist = Mathf.Sqrt(best.dist);
            return best;
        }

        static void RoadFrame(float s, out Vector3 pos, out Vector3 right, out float halfW)
        {
            int j = 0;
            while (j < roadS.Count - 2 && roadS[j + 1] < s) j++;
            float t = Mathf.Clamp01((s - roadS[j]) / Mathf.Max(roadS[j + 1] - roadS[j], 1e-4f));
            pos = Vector3.Lerp(roadPts[j], roadPts[j + 1], t);
            Vector3 tangent = Flat(roadPts[j + 1] - roadPts[j]).normalized;
            right = new Vector3(tangent.z, 0f, -tangent.x);
            halfW = Mathf.Lerp(roadParams[j].x, roadParams[j + 1].x, t);
        }

        static float RoadYaw(float s)
        {
            RoadFrame(s, out _, out Vector3 right, out _);
            return Mathf.Atan2(-right.z, right.x) * Mathf.Rad2Deg;   // yaw-ul tangentei
        }

        static float FaceRoadYaw(float s, int side)
        {
            RoadFrame(s, out _, out Vector3 right, out _);
            return Mathf.Atan2(-right.x * side, -right.z * side) * Mathf.Rad2Deg;
        }

        // ------------------------------------------------------------------ teren

        static Terrain BuildTerrain()
        {
            TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            if (data == null)
            {
                data = new TerrainData();
                AssetDatabase.CreateAsset(data, TerrainDataPath);
            }
            data.heightmapResolution = HeightRes;
            data.size = TerrainSize;
            data.alphamapResolution = AlphaRes;
            data.baseMapResolution = 1024;
            data.SetDetailResolution(AlphaRes, 32);
            data.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);

            float[,] heights = new float[HeightRes, HeightRes];
            for (int z = 0; z < HeightRes; z++)
                for (int x = 0; x < HeightRes; x++)
                {
                    float lx = TerrainOffset.x + x / (float)(HeightRes - 1) * TerrainSize.x;
                    float lz = TerrainOffset.z + z / (float)(HeightRes - 1) * TerrainSize.z;
                    heights[z, x] = Mathf.Clamp01((HeightAt(lx, lz) - TerrainOffset.y) / TerrainSize.y);
                }
            data.SetHeights(0, 0, heights);

            PaintTerrain(data);

            GameObject go = Terrain.CreateTerrainGameObject(data);
            go.name = TerrainRoot;
            go.transform.position = Origin + TerrainOffset;
            go.layer = 3;   // Ground: MirageAttack si NavMesh cauta stratul 3
            go.tag = hubTag;
            Terrain t = go.GetComponent<Terrain>();
            if (hubMaterial != null) t.materialTemplate = hubMaterial;
            t.drawInstanced = true;
            t.heightmapPixelError = 4f;
            t.detailObjectDistance = 90f;
            t.detailObjectDensity = 1f;
            EditorUtility.SetDirty(data);
            return t;
        }

        static float HeightAt(float lx, float lz)
        {
            RoadHit road = NearestRoad(lx, lz);
            float padSigned = PadDistance(lx, lz);
            float meadowCenter = Vector2.Distance(new Vector2(lx, lz), MeadowCenter);
            float dRoad = Mathf.Max(0f, road.dist - road.halfW), dPad = Mathf.Max(0f, padSigned), dMeadow = Mathf.Max(0f, meadowCenter - MeadowRadius);

            // podeaua = cea a zonei walkable celei mai apropiate, malurile cresc cu distanta
            float dMin = Mathf.Min(dRoad, Mathf.Min(dPad, dMeadow));
            float wRoad = Mathf.Exp(-(dRoad - dMin) / 4f), wPad = Mathf.Exp(-(dPad - dMin) / 4f), wMeadow = Mathf.Exp(-(dMeadow - dMin) / 4f);
            float wSum = wRoad + wPad + wMeadow;
            float padFloor = HubHeight(Mathf.Clamp(lx, ShrinePad.xMin, ShrinePad.xMax), Mathf.Clamp(lz, ShrinePad.yMin, ShrinePad.yMax));
            float floor = (wRoad * road.y + wPad * padFloor + wMeadow * MeadowHeight) / wSum;
            float bankH = (wRoad * road.bankH + wPad * PadBankH + wMeadow * MeadowBankH) / wSum;
            float bankK = (wRoad * road.bankK + wPad * PadBankK + wMeadow * MeadowBankK) / wSum;

            float k = 3f;
            float d = Mathf.Max(0f, dMin - k * Mathf.Log(Mathf.Exp(-(dRoad - dMin) / k) + Mathf.Exp(-(dPad - dMin) / k) + Mathf.Exp(-(dMeadow - dMin) / k)));
            // in defileu malul incepe sa urce mai departe de drum: pe treapta asta stau stancile peretelui, in fata terenului abrupt
            float shelf = GorgeShelf * Mathf.InverseLerp(StretchBankH[2], StretchBankH[1], road.bankH) * wRoad / wSum;
            d = Mathf.Max(0f, d - shelf);
            float h = floor + bankH * (1f - Mathf.Exp(-d / bankK)) * (0.7f + 0.6f * Fbm(lx * 0.03f, lz * 0.03f));
            h += (Fbm(lx * 0.15f + 50f, lz * 0.15f + 50f) - 0.5f) * 1.6f * Mathf.Clamp01(d / 3f);
            h += (Fbm(lx * 0.2f + 90f, lz * 0.2f + 90f) - 0.5f) * 0.3f;

            h = Mathf.Lerp(MeadowHeight, h, Smooth(RingRadius + 2f, RingRadius + 6f, meadowCenter));   // interiorul arenei e plat
            h = Mathf.Lerp(h, HubHeight(lx, lz), Smooth(0f, -4f, padSigned));                         // in shrine copiem exact terenul hub

            float edge = Mathf.Min(Mathf.Min(lx - TerrainOffset.x, TerrainOffset.x + TerrainSize.x - lx), Mathf.Min(lz - TerrainOffset.z, TerrainOffset.z + TerrainSize.z - lz));
            if (edge < 25f) h += (1f - edge / 25f) * (1f - edge / 25f) * 30f;
            return Road2Carve(lx, lz, h);   // defileul spre Drumul 2 taie prin malul de nord
        }

        static void PaintTerrain(TerrainData data)
        {
            List<TerrainLayer> layers = new List<TerrainLayer>();
            int[] channel = new int[LayerPaths.Length];
            for (int i = 0; i < LayerPaths.Length; i++)
            {
                TerrainLayer layer = Load<TerrainLayer>(LayerPaths[i]);
                channel[i] = layer != null ? layers.Count : -1;
                if (layer != null) layers.Add(layer);
            }

            float[,,] hubAlpha = null;
            int[] hubChannel = null;
            int hubRes = 0;
            if (hubData != null && hubData.alphamapLayers > 0)
            {
                hubRes = hubData.alphamapResolution;
                hubAlpha = hubData.GetAlphamaps(0, 0, hubRes, hubRes);
                TerrainLayer[] hubLayers = hubData.terrainLayers;
                hubChannel = new int[hubLayers.Length];
                for (int i = 0; i < hubLayers.Length; i++)
                {
                    int idx = layers.IndexOf(hubLayers[i]);
                    if (idx < 0 && hubLayers[i] != null) { idx = layers.Count; layers.Add(hubLayers[i]); }
                    hubChannel[i] = Mathf.Max(idx, 0);
                }
            }
            if (layers.Count == 0) { Debug.LogError("[Arena1Builder] Niciun TerrainLayer gasit, terenul ramane nevopsit."); return; }
            for (int i = 0; i < channel.Length; i++) if (channel[i] < 0) channel[i] = 0;
            data.terrainLayers = layers.ToArray();

            DetailPrototype[] grass = GrassPrototypes();
            data.detailPrototypes = grass;
            int[][,] details = new int[grass.Length][,];
            for (int i = 0; i < grass.Length; i++) details[i] = new int[AlphaRes, AlphaRes];

            float[,,] alpha = new float[AlphaRes, AlphaRes, layers.Count];
            float[] w = new float[LayerPaths.Length];
            for (int z = 0; z < AlphaRes; z++)
                for (int x = 0; x < AlphaRes; x++)
                {
                    float nx = x / (float)(AlphaRes - 1), nz = z / (float)(AlphaRes - 1);
                    float lx = TerrainOffset.x + nx * TerrainSize.x, lz = TerrainOffset.z + nz * TerrainSize.z;
                    float steep = data.GetSteepness(nx, nz);
                    RoadHit road = NearestRoad(lx, lz);
                    float meadowCenter = Vector2.Distance(new Vector2(lx, lz), MeadowCenter);
                    float jitter = (Fbm(lx * 0.4f + 400f, lz * 0.4f) - 0.5f) * 1.4f;
                    float rd = road.dist + jitter;

                    System.Array.Clear(w, 0, w.Length);
                    w[Grass] = 1f;
                    Paint(w, Patches, 0.75f * Smooth(0.52f, 0.68f, Fbm(lx * 0.045f + 200f, lz * 0.045f + 200f)));
                    if (road.stretch == 1) Paint(w, Dirt, 0.8f * (1f - Smooth(road.halfW + 1f, road.halfW + 9f, rd)));   // grohotis in defileu
                    Paint(w, Moss, Smooth(12f, 26f, steep));
                    Paint(w, Dirt, 1f - Smooth(road.halfW, road.halfW + 2.2f, rd));
                    Paint(w, Gravel, 1f - Smooth(road.halfW - 1.4f, road.halfW + 0.2f, rd));
                    Paint(w, Dirt, 0.85f * (1f - Smooth(0.6f, 2.4f, Mathf.Abs(meadowCenter - RingRadius) + jitter * 0.5f)));   // cercul batatorit
                    Paint(w, Worn, 1f - Smooth(3f, 5.5f, meadowCenter + jitter));
                    Paint(w, Rock, Smooth(30f, 42f, steep));
                    float road2 = Road2PaintWeight(lx, lz, jitter);
                    Paint(w, Dirt, road2);
                    Paint(w, Gravel, 0.75f * road2 * road2);

                    float padCore = Smooth(0f, -4f, PadDistance(lx, lz));
                    for (int c = 0; c < layers.Count; c++) alpha[z, x, c] = 0f;
                    for (int i = 0; i < w.Length; i++) alpha[z, x, channel[i]] += w[i] * (1f - padCore);
                    if (hubAlpha != null && padCore > 0f)
                    {
                        int hx = Mathf.Clamp(Mathf.RoundToInt((lx - hubPos.x) / hubData.size.x * (hubRes - 1)), 0, hubRes - 1);
                        int hz = Mathf.Clamp(Mathf.RoundToInt((lz - hubPos.z) / hubData.size.z * (hubRes - 1)), 0, hubRes - 1);
                        for (int i = 0; i < hubChannel.Length; i++) alpha[z, x, hubChannel[i]] += hubAlpha[hz, hx, i] * padCore;
                    }
                    else if (padCore > 0f) alpha[z, x, channel[Grass]] += padCore;
                    float grassW = alpha[z, x, channel[Grass]];

                    // iarba doar pe pajiste si pante line, nu langa piatra cu ghicitoarea
                    if (grass.Length == 0 || grassW < 0.55f || steep > 28f || meadowCenter < 4.5f || road2 > 0.05f) continue;
                    float density = (grassW - 0.55f) / 0.45f;
                    details[0][z, x] = Mathf.Clamp(Mathf.RoundToInt(density * 3f * (0.6f + 0.8f * (float)rng.NextDouble())), 0, 3);
                    for (int g = 1; g < grass.Length; g++)
                        if (rng.NextDouble() < 0.03 * density) details[g][z, x] = 1;
                }
            data.SetAlphamaps(0, 0, alpha);
            for (int g = 0; g < grass.Length; g++) data.SetDetailLayer(0, 0, g, details[g]);
        }

        static DetailPrototype[] GrassPrototypes()
        {
            TerrainData cinder = Load<TerrainData>(CinderDataPath);
            if (cinder != null && cinder.detailPrototypes.Length > 0) return cinder.detailPrototypes;
            if (hubData != null && hubData.detailPrototypes.Length > 0) return hubData.detailPrototypes;
            Texture2D blades = Load<Texture2D>(GrassTexPath);
            if (blades == null) return new DetailPrototype[0];
            return new[]
            {
                new DetailPrototype
                {
                    prototypeTexture = blades, renderMode = DetailRenderMode.GrassBillboard, usePrototypeMesh = false,
                    minWidth = 0.6f, maxWidth = 1.1f, minHeight = 0.4f, maxHeight = 0.8f,
                    healthyColor = new Color(0.55f, 0.7f, 0.35f), dryColor = new Color(0.65f, 0.6f, 0.35f)
                }
            };
        }

        static float PadDistance(float lx, float lz)
        {
            Vector2 c = ShrinePad.center, half = ShrinePad.size * 0.5f - Vector2.one * PadRadius;
            float qx = Mathf.Abs(lx - c.x) - half.x, qz = Mathf.Abs(lz - c.y) - half.y;
            return new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qz, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qz), 0f) - PadRadius;
        }

        static void Paint(float[] w, int layer, float amount)
        {
            amount = Mathf.Clamp01(amount);
            for (int i = 0; i < w.Length; i++) w[i] *= 1f - amount;
            w[layer] += amount;
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

        static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        // ------------------------------------------------------------------ decor drum

        static int BuildRoad()
        {
            int before = placed;
            Transform road = new GameObject(RoadRoot).transform;
            road.position = Origin;
            float sStart = 10f, s1 = ctrlS[5], s2 = ctrlS[9], sEnd = ctrlS[ctrlS.Length - 1];

            // 1. Pilgrim Way: drum larg intre dealuri, torte, altar, marfa abandonata, cimitir
            Transform pilgrim = Group(road, "01_PilgrimWay");
            int torch = 0;
            for (float s = sStart + 4f; s < s1 - 6f; s += 17f, torch++)
                Dress(Kit + "Torch_Standing.prefab", pilgrim, s, torch % 2 == 0 ? 1 : -1, 0.6f, 0.05f, RoadYaw(s));
            for (int i = 0; i < 4; i++)
            {
                float s = Mathf.Lerp(sStart + 8f, s1 - 10f, i / 3f) + R(-2f, 2f);
                Dress(Kit + "Fence_Broken.prefab", pilgrim, s, 1, R(1f, 1.8f), 0.1f, RoadYaw(s) + R(-8f, 8f));
            }
            float sShrine = Mathf.Lerp(sStart, s1, 0.35f);
            Dress(Kit + "Altar_Shrine.prefab", pilgrim, sShrine, -1, 2.5f, 0.05f, FaceRoadYaw(sShrine, -1));
            Dress(Kit + "Candles.prefab", pilgrim, sShrine + 1.8f, -1, 1.6f, 0f, R(0f, 360f));
            Dress(Kit + "Pot_Round.prefab", pilgrim, sShrine - 1.8f, -1, 1.8f, 0f, R(0f, 360f));
            Dress(Kit + "Banner.prefab", pilgrim, sShrine, -1, 5f, 0.1f, FaceRoadYaw(sShrine, -1));
            float sCargo = Mathf.Lerp(sStart, s1, 0.62f);
            foreach (string cargo in new[] { "Barrel", "Crate", "Crate_Small", "Sacks", "Pot_Amphora", "Barrel" })
                Dress(Kit + cargo + ".prefab", pilgrim, sCargo + R(-2.5f, 2.5f), 1, R(1.5f, 3.5f), 0.02f, R(0f, 360f));
            float sGrave = Mathf.Lerp(sStart, s1, 0.82f);
            for (int i = 0; i < 6; i++)
                Dress(Kit + (i % 3 == 0 ? "Gravestone_Cross" : "Gravestone") + ".prefab", pilgrim, sGrave + (i % 3) * 2.4f - 2.4f, -1,
                      4.5f + (i / 3) * 2.6f + R(-0.3f, 0.3f), 0.15f, FaceRoadYaw(sGrave, -1) + R(-12f, 12f));
            Dress(Kit + "Tree_Dead.prefab", pilgrim, sGrave + 5f, -1, 9f, 0.3f, R(0f, 360f));
            for (int i = 0; i < 16; i++) Dress(Pick(Bushes), pilgrim, R(sStart, s1), Side(), R(1.5f, 11f), 0.15f, R(0f, 360f));
            for (int i = 0; i < 8; i++) Dress(Pick(Rocks), pilgrim, R(sStart, s1), Side(), R(3f, 12f), 0.3f, R(0f, 360f), R(0.8f, 2.2f));
            for (int i = 0; i < 5; i++) Dress(Kit + "Tree_Dead.prefab", pilgrim, R(sStart, s1), Side(), R(4f, 13f), 0.3f, R(0f, 360f));
            for (int i = 0; i < 3; i++) Dress(Kit + "Stump_Roots.prefab", pilgrim, R(sStart, s1), Side(), R(2f, 9f), 0.2f, R(0f, 360f));
            for (int i = 0; i < 2; i++)
            {
                float s = R(sStart, s1);
                Dress(Kit + "Log_Fallen.prefab", pilgrim, s, Side(), R(2f, 6f), 0.15f, RoadYaw(s) + R(-25f, 25f));
            }

            // 2. Defileul: pereti abrupti, stanci in pereti, ciuperci si cristale care lumineaza rece
            Transform gorge = Group(road, "02_Gorge");
            for (float s = s1 + 2f; s < s2 - 2f; s += 7.5f)
                for (int side = -1; side <= 1; side += 2)
                {
                    float sc = s + R(-1.5f, 1.5f);
                    Dress(Pick(Cliffs), gorge, sc, side, R(-0.3f, 1f), 1f, FaceRoadYaw(sc, side) + R(-25f, 25f), R(5f, 9f));
                }
            for (int i = 0; i < 6; i++) Dress(Pick(Boulders), gorge, R(s1, s2), Side(), R(0.2f, 1.2f), 0.3f, R(0f, 360f), R(1.2f, 2.4f));
            for (int i = 0; i < 5; i++) Dress(Kit + "Rubble_Pile.prefab", gorge, R(s1, s2), Side(), R(0f, 1.5f), 0.1f, R(0f, 360f));
            for (int i = 0; i < 3; i++) Dress(Kit + "Bones_Pile.prefab", gorge, R(s1, s2), Side(), R(0f, 1f), 0.05f, R(0f, 360f));
            float sLog = Mathf.Lerp(s1, s2, 0.55f);
            Dress(Kit + "Log_Fallen.prefab", gorge, sLog, Side(), 0.3f, 0.1f, RoadYaw(sLog) + R(-10f, 10f));
            for (int i = 0; i < 5; i++) Dress(Kit + "Mushrooms_Glow.prefab", gorge, R(s1, s2), Side(), R(0.2f, 1.5f), 0.02f, R(0f, 360f));
            for (int i = 0; i < 4; i++) Dress(Kit + "Crystals_Glow.prefab", gorge, R(s1, s2), Side(), R(0.5f, 2f), 0.1f, R(0f, 360f));
            foreach (float k in new[] { 0.2f, 0.5f, 0.8f })
                AddLight(gorge, Mathf.Lerp(s1, s2, k), 5f, new Color(0.45f, 0.62f, 1f), 14f, 2.5f);

            // 3. Poarta veche: ruine de zid, coloane, braziere si statui cazute inainte de pajiste
            Transform gate = Group(road, "03_OldGate");
            float sGate = Mathf.Lerp(s2, sEnd, 0.4f);
            for (int side = -1; side <= 1; side += 2)
            {
                Dress(Kit + "Pillar.prefab", gate, sGate, side, 0.5f, 0.1f, RoadYaw(sGate));
                Dress(Kit + "Wall_Ruin_A.prefab", gate, sGate, side, 1.2f, 0.2f, RoadYaw(sGate) + 90f);
                Dress(Kit + "Banner.prefab", gate, sGate - 1.2f, side, 0.3f, 0.1f, RoadYaw(sGate));
                Dress(Kit + "Brazier.prefab", gate, sGate - 6f, side, 0.6f, 0.05f, R(0f, 360f));
                Dress(Kit + "Torch_Standing.prefab", gate, sGate + 8f, side, 0.8f, 0.05f, RoadYaw(sGate + 8f));
                AddLight(gate, sGate, 3.2f, new Color(1f, 0.62f, 0.32f), 9f, 3f, side);
            }
            for (int i = 0; i < 6; i++)
            {
                float s = R(s2 + 3f, sEnd - 4f);
                Dress(Pick(RuinWalls), gate, s, Side(), R(1.5f, 5f), 0.2f, RoadYaw(s) + R(-15f, 15f));
            }
            Dress(WorldKit + "SM_StatueHead_Fallen.prefab", gate, Mathf.Lerp(s2, sEnd, 0.2f), 1, 2.5f, 0.6f, R(0f, 360f));
            float sStatue = Mathf.Lerp(s2, sEnd, 0.7f);
            Dress(WorldKit + "SM_WeepingStatue_Broken.prefab", gate, sStatue, -1, 2.5f, 0.2f, FaceRoadYaw(sStatue, -1));
            for (int i = 0; i < 3; i++) Dress(Kit + "Pillar_Broken.prefab", gate, R(s2, sEnd - 3f), Side(), R(1f, 4f), 0.15f, R(0f, 360f));

            // scenele mici de pe drum au seed-ul lor, ca decorul de mai sus sa ramana neschimbat
            System.Random saved = rng;
            rng = new System.Random(Seed + 1);
            BuildRoadVignettes(road);
            rng = saved;
            BuildGorgeWalls(gorge);
            return placed - before;
        }

        // pune un prefab langa drum: marginea obiectului incepe la halfWidth + extra de axul drumului
        static GameObject Dress(string path, Transform parent, float s, int side, float extra, float sink, float yaw, float height = 0f)
        {
            RoadFrame(s, out Vector3 pos, out Vector3 right, out float halfW);
            GameObject go = Spawn(path, parent, Origin + pos, yaw);
            if (go == null) return null;
            if (height > 0f) ScaleToHeight(go, height);
            float radius = 0f;
            if (TryBounds(go, out Bounds b)) radius = Mathf.Abs(right.x) * b.extents.x + Mathf.Abs(right.z) * b.extents.z;
            Put(go, Origin + pos + right * side * (halfW + extra + radius), sink);
            if (System.Array.IndexOf(Rocks, path) >= 0) EnsureColliders(go);
            return go;
        }

        static void AddLight(Transform parent, float s, float height, Color color, float range, float intensity, int side = 0)
        {
            RoadFrame(s, out Vector3 pos, out Vector3 right, out float halfW);
            Vector3 world = Origin + pos + right * side * (halfW + 0.6f);
            Light light = new GameObject("FillLight").AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.position = new Vector3(world.x, Ground(world.x, world.z) + height, world.z);
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        // ------------------------------------------------------------------ prefab-uri boss

        static GameObject BuildSpikeVariant()
        {
            GameObject source = Load<GameObject>(RockSpikePath);
            if (source == null) return null;
            Scene preview = EditorSceneManager.NewPreviewScene();
            GameObject spike = (GameObject)PrefabUtility.InstantiatePrefab(source, preview);

            // tepii originali lovesc inamicii in numele jucatorului; pentru boss lovesc jucatorul
            GameObject particles = null;
            PlayerHitEnemy playerHit = spike.GetComponent<PlayerHitEnemy>();
            if (playerHit != null)
            {
                particles = new SerializedObject(playerHit).FindProperty("hitParticlesPrefab").objectReferenceValue as GameObject;
                Object.DestroyImmediate(playerHit);
            }
            GolemSpikeDamage damage = spike.GetComponent<GolemSpikeDamage>();
            if (damage == null) damage = spike.AddComponent<GolemSpikeDamage>();
            SetRef(damage, "hitParticlesPrefab", particles);
            MirageAttack mirage = spike.GetComponent<MirageAttack>();
            HeavyHitSwordsTransition transition = spike.GetComponent<HeavyHitSwordsTransition>();
            if (mirage != null && transition != null) SetRef(mirage, "mirageSwordPrefab", transition);

            spike.name = "RockSpike_Boss";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(spike, SpikePath);
            EditorSceneManager.ClosePreviewScene(preview);
            return saved;
        }

        static GameObject BuildBossVariant(GameObject spike)
        {
            GameObject source = Load<GameObject>(GolemPath);
            if (source == null) return null;
            Scene preview = EditorSceneManager.NewPreviewScene();
            GameObject boss = (GameObject)PrefabUtility.InstantiatePrefab(source, preview);

            // golemul e aliat al jucatorului; varianta boss trece pe stratul Enemy si in lista enemies
            foreach (Transform t in boss.GetComponentsInChildren<Transform>(true))
                if (t.gameObject.layer == 8) t.gameObject.layer = 6;
            AllyAtributes ally = boss.GetComponent<AllyAtributes>();
            EnemyAtributes enemy = boss.GetComponent<EnemyAtributes>();
            if (enemy == null) enemy = boss.AddComponent<EnemyAtributes>();
            if (ally != null)
            {
                CopySerialized(ally, enemy);
                Object.DestroyImmediate(ally);
            }
            SetFloat(enemy, "Max_Health", 600f);
            SetFloat(enemy, "knockBackProtection", 6f);

            EnemyAI ai = boss.GetComponent<EnemyAI>();
            if (ai != null)
            {
                SetBool(ai, "Ally", false);
                SetFloat(ai, "detectDistance", 45f);
                SetFloat(ai, "loseDistance", 60f);
                SetFloat(ai, "maxDistanceToDetectFromHome", 60f);
                SetFloat(ai, "maxAngleToBeSeen", 180f);
                SetFloat(ai, "distanceFromHome", 80f);
                SetFloat(ai, "randomDistanceFromHome", 2f);
            }
            EnemyHitPlayer hit = boss.GetComponent<EnemyHitPlayer>();
            if (hit != null) SetInt(hit, "enemyLayer", (1 << 7) | (1 << 8));
            GolemController controller = boss.GetComponent<GolemController>();
            if (controller != null && spike != null) SetRef(controller, "mirageAttack", spike.GetComponent<MirageAttack>());

            boss.name = "StoneGolem_Boss";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(boss, BossPath);
            EditorSceneManager.ClosePreviewScene(preview);
            return saved;
        }

        // ------------------------------------------------------------------ arena

        static int BuildArena(GameObject boss)
        {
            Transform arena = new GameObject(ArenaRoot).transform;
            arena.position = Origin;
            Vector3 c = Origin + new Vector3(MeadowCenter.x, 0f, MeadowCenter.y);
            c.y = Ground(c.x, c.z);
            ParticleSystem dust = Load<ParticleSystem>(DustPath);
            Arena1FxBuilder.Result fx = Arena1FxBuilder.Build();

            RiddleStone riddle = BuildRiddleStone(arena, c);

            Transform spawn = new GameObject("BossSpawn").transform;
            spawn.SetParent(arena, false);
            spawn.SetPositionAndRotation(new Vector3(c.x, Ground(c.x, c.z + 9f), c.z + 9f), Quaternion.Euler(0f, 180f, 0f));

            ArenaSeal seal = BuildSeal(arena, c, dust, fx);
            BossIntro intro = Arena1BossUiBuilder.Build(arena, fx, out BossHealthBar healthBar);

            // decor in afara inelului, lasand liber intrarea drumului
            Transform scatter = Group(arena, "Scatter");
            Vector3 entry = Origin + RoadCtrl[RoadCtrl.Length - 2] - c;
            float entryAngle = Mathf.Atan2(entry.x, entry.z) * Mathf.Rad2Deg;
            string[] props = { Rocks[0], Rocks[1], Rocks[2], Boulders[0], Boulders[2], Bushes[0], Bushes[1], Kit + "Tree_Dead.prefab" };
            for (int i = 0, n = 0; n < 14 && i < 200; i++)
            {
                float angle = R(0f, 360f);
                if (Mathf.Abs(Mathf.DeltaAngle(angle, entryAngle)) < 22f) continue;
                n++;
                Vector3 p = c + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * R(21f, 25f);
                string path = Pick(props);
                GameObject go = Spawn(path, scatter, p, R(0f, 360f));
                if (go == null) continue;
                if (System.Array.IndexOf(Rocks, path) >= 0 || System.Array.IndexOf(Boulders, path) >= 0) ScaleToHeight(go, R(1.2f, 3.5f));
                Put(go, p, 0.3f);
                if (System.Array.IndexOf(Rocks, path) >= 0) EnsureColliders(go);
            }

            Arena1Encounter encounter = new GameObject("Arena1_Encounter").AddComponent<Arena1Encounter>();
            encounter.transform.SetParent(arena, false);
            encounter.transform.position = c;
            SetRef(encounter, "riddle", riddle);
            SetRef(encounter, "seal", seal);
            SetRef(encounter, "bossPrefab", boss);
            SetRef(encounter, "bossSpawn", spawn);
            SetRef(encounter, "dustPrefab", dust);
            SetRef(encounter, "intro", intro);
            SetRef(encounter, "healthBar", healthBar);
            SetString(encounter, "bossName", Arena1BossUiBuilder.BossBarName);
            if (boss == null) Debug.LogError("[Arena1Builder] Varianta boss nu a fost creata, Arena1_Encounter nu are bossPrefab.");

            // detalii, steaguri si atmosfera, fiecare cu seed-ul lui
            System.Random saved = rng;
            rng = new System.Random(Seed + 2);
            BuildArenaDetails(arena, c, riddle);
            BuildBanners(arena, c, riddle, Arena1BannerSetup.Build());
            BuildRiddleBase(riddle, c);
            ClearStatic(riddle.gameObject);   // tot ce sta pe piatra se scufunda cu ea
            SetString(riddle, "idleHint", BannerHint);
            rng = new System.Random(Seed + 3);
            BuildAtmosphere(c);

            // poarta de nord, inima golemului si puiul Adormitului
            Text hint = arena.Find("Arena1_HintCanvas/Hint").GetComponent<Text>();
            rng = new System.Random(Seed + 4);
            BuildRoad2(arena, c, dust, fx, hint, encounter);
            rng = new System.Random(Seed + 5);
            BuildHeart(arena, c, dust, fx, hint, encounter);
            rng = new System.Random(Seed + 6);
            BuildSleeper(arena, c, hint);
            rng = saved;
            LimitShadows(RoadRoot, ArenaRoot, AtmosphereRoot);

            return BakeNavMesh(arena, c, boss);
        }

        static RiddleStone BuildRiddleStone(Transform arena, Vector3 c)
        {
            GameObject root = new GameObject("RiddleStone");
            root.transform.SetParent(arena, false);
            root.transform.position = c;
            NavMeshModifier modifier = root.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;   // dupa ce piatra se scufunda, golemul trebuie sa poata trece pe acolo

            GameObject boulder = Spawn(Kit + "Boulder_B.prefab", root.transform, c, R(0f, 360f));
            if (boulder != null)
            {
                ScaleToHeight(boulder, 3.4f);
                Put(boulder, c, 0.75f);   // infipt in pamant, nu asezat pe el
                if (boulder.GetComponentInChildren<Collider>(true) == null) EnsureColliders(boulder);
            }

            // runele sapate pe fata dinspre drum, lipite de suprafata bolovanului
            Vector3 entry = Origin + RoadCtrl[RoadCtrl.Length - 2] - c;
            Renderer[] glyphs = boulder != null ? BuildRunes(root.transform, boulder, c, Mathf.Atan2(entry.x, entry.z) * Mathf.Rad2Deg) : new Renderer[0];

            Light glow = new GameObject("GlowLight").AddComponent<Light>();
            glow.transform.SetParent(root.transform, false);
            glow.transform.position = c + Vector3.up * 2.2f;
            glow.type = LightType.Point;
            glow.color = new Color(0.4f, 1f, 0.55f);
            glow.range = 7f;
            glow.intensity = 0.6f;
            glow.shadows = LightShadows.None;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text hint = BuildHintCanvas(arena, font);

            ClearStatic(root);   // piatra se scufunda la runtime

            RiddleStone riddle = root.AddComponent<RiddleStone>();
            SphereCollider trigger = root.GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 7f;
            trigger.center = new Vector3(0f, 1.5f, 0f);
            SetRef(riddle, "screenHint", hint);
            SetRef(riddle, "glowLight", glow);
            SetRefs(riddle, "glyphs", glyphs);
            return riddle;
        }

        static Text BuildHintCanvas(Transform arena, Font font)
        {
            GameObject canvasGo = new GameObject("Arena1_HintCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(arena, false);
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject hintGo = new GameObject("Hint", typeof(RectTransform));
            hintGo.transform.SetParent(canvasGo.transform, false);
            RectTransform rect = (RectTransform)hintGo.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 140f);
            rect.sizeDelta = new Vector2(1200f, 80f);
            Text text = hintGo.AddComponent<Text>();
            text.font = font;
            text.fontSize = 34;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.9f, 1f, 0.92f);
            text.raycastTarget = false;
            Outline outline = hintGo.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            hintGo.SetActive(false);
            return text;
        }

        static void ClearStatic(GameObject go)
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        }

        static ArenaSeal BuildSeal(Transform arena, Vector3 c, ParticleSystem dust, Arena1FxBuilder.Result fx)
        {
            GameObject root = new GameObject("ArenaSeal");
            root.transform.SetParent(arena, false);
            root.transform.position = c;
            NavMeshModifier modifier = root.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;

            // pozitia din editor = pozitia ridicata; ArenaSeal le ingroapa la Awake
            Transform rockGroup = Group(root.transform, "Rocks");
            List<Transform> rocks = new List<Transform>();
            const int rockCount = 22;
            for (int i = 0; i < rockCount; i++)
            {
                bool spire = i % 4 == 3;
                string path = spire ? WorldKit + "SM_Cliff_Spire.prefab" : Rocks[i % Rocks.Length];
                Vector3 p = c + Quaternion.Euler(0f, i * 360f / rockCount + R(-3f, 3f), 0f) * Vector3.forward * (RingRadius + R(-0.6f, 0.6f));
                GameObject rock = Spawn(path, rockGroup, p, R(0f, 360f));
                if (rock == null) continue;
                if (TryBounds(rock, out Bounds b))
                {
                    // inaltime si amprenta separate: pietrele turtite din kit nu mai cresc la 13-21 m latime;
                    // marginea interioara ramane la cel putin 14 m de centru
                    float height = R(5.5f, 7.5f), footprint = R(4.6f, 6f);
                    float sy = height / Mathf.Max(b.size.y, 0.01f);
                    float sxz = footprint / Mathf.Max(Mathf.Max(b.size.x, b.size.z), 0.01f);
                    if (spire) rock.transform.localScale *= Mathf.Min(sy, sxz);
                    else rock.transform.localScale = Vector3.Scale(rock.transform.localScale, new Vector3(sxz, sy, sxz));
                }
                Put(rock, p, 0.4f);
                foreach (Transform t in rock.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 3;
                EnsureColliders(rock);
                ClearStatic(rock);   // pietrele se misca la runtime, static batching le-ar bloca
                rocks.Add(rock.transform);
            }

            // ziduri invizibile de rezerva + carving pe NavMesh, ca golemul sa nu iasa printre pietre
            Transform barrierGroup = Group(root.transform, "Barriers");
            const int barrierCount = 24;
            float width = 2f * Mathf.PI * (RingRadius + 0.2f) / barrierCount + 0.6f;
            List<GameObject> barriers = new List<GameObject>();
            for (int i = 0; i < barrierCount; i++)
            {
                Quaternion rot = Quaternion.Euler(0f, i * 360f / barrierCount, 0f);
                Vector3 p = c + rot * Vector3.forward * (RingRadius + 0.2f);
                GameObject barrier = new GameObject("Barrier_" + i);
                barrier.transform.SetParent(barrierGroup, false);
                barrier.transform.SetPositionAndRotation(new Vector3(p.x, Ground(p.x, p.z) - 1f, p.z), rot);
                BoxCollider box = barrier.AddComponent<BoxCollider>();
                box.size = new Vector3(width, 10f, 1.6f);
                box.center = new Vector3(0f, 5f, 0f);
                NavMeshObstacle obstacle = barrier.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.size = box.size;
                obstacle.center = box.center;
                obstacle.carving = true;
                barrier.SetActive(false);
                barriers.Add(barrier);
            }

            ArenaSeal seal = root.AddComponent<ArenaSeal>();
            SetRefs(seal, "rocks", rocks.ToArray());
            SetRefs(seal, "barriers", barriers.ToArray());
            SetRef(seal, "dustPrefab", dust);
            SetRef(seal, "rumbleFxPrefab", fx.rumble);
            SetRef(seal, "burstFxPrefab", fx.burst);
            SetRef(seal, "settleFxPrefab", fx.ring);
            SetRef(seal, "crackPrefab", fx.crack);
            SetRefs(seal, "debrisPrefabs", fx.debris);
            SetFloat(seal, "crackSizeFactor", 1.8f);
            return seal;
        }

        static int BakeNavMesh(Transform arena, Vector3 c, GameObject boss)
        {
            GameObject nav = new GameObject("Arena1_NavMesh");
            nav.transform.SetParent(arena, false);
            nav.transform.position = c;

            List<int> agents = new List<int>();
            NavMeshAgent bossAgent = boss != null ? boss.GetComponent<NavMeshAgent>() : null;
            if (bossAgent != null) agents.Add(bossAgent.agentTypeID);
            if (!agents.Contains(0)) agents.Add(0);

            // colliderele mutate in acest frame (teren, pietre) trebuie sincronizate, altfel bake-ul le vede in pozitia veche
            Physics.SyncTransforms();

            int built = 0;
            foreach (int agentId in agents)
            {
                string agentName = NavMesh.GetSettingsNameFromID(agentId);
                if (string.IsNullOrEmpty(agentName)) { Debug.LogError($"[Arena1Builder] Agent type necunoscut: {agentId}"); continue; }
                NavMeshSurface surface = nav.AddComponent<NavMeshSurface>();
                surface.agentTypeID = agentId;
                surface.collectObjects = CollectObjects.Volume;
                surface.size = new Vector3(64f, 34f, 86f);   // arena + coridorul spre Drumul 2
                surface.center = new Vector3(3f, 5f, 11f);
                surface.layerMask = ~((1 << 6) | (1 << 7) | (1 << 8));
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                surface.BuildNavMesh();
                if (surface.navMeshData == null) { Debug.LogError($"[Arena1Builder] Bake NavMesh esuat pentru {agentName}."); continue; }

                string path = $"{Folder}/NavMesh/Arena1_NavMesh_{agentName}.asset";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(surface.navMeshData, path);
                built++;
            }
            return built;
        }

        // ------------------------------------------------------------------ utilitare

        static T Load<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null && missingPaths.Add(path)) Debug.LogError($"[Arena1Builder] Lipseste asset-ul: {path}");
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

        static float Ground(float x, float z) =>
            terrain != null ? terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y : Origin.y;

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

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
        static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static int Side() => rng.Next(2) == 0 ? -1 : 1;
        static string Pick(string[] options) => options[rng.Next(options.Length)];

        static void CopySerialized(Object from, Object to)
        {
            SerializedObject src = new SerializedObject(from), dst = new SerializedObject(to);
            SerializedProperty it = src.GetIterator();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                enter = false;
                if (it.propertyPath != "m_Script") dst.CopyFromSerializedProperty(it);
            }
            dst.ApplyModifiedPropertiesWithoutUndo();
        }

        static SerializedProperty Find(SerializedObject so, string name)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null) Debug.LogError($"[Arena1Builder] Campul {name} nu exista pe {so.targetObject.GetType().Name}");
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
    }
}
