using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Alpha.World.EditorTools
{
    // Terenul pentru Drumul 2 si castel: drum adancit intre dealuri, defileu la iesirea din Arena 1, platoul castelului
    // cu sant uscat, mlastina din spatele posternei. Marginea de sud continua exact randul de sus al terenului din Arena 1.
    public static partial class Arena2Builder
    {
        static readonly Vector3 TerrainPos = new Vector3(150f, -30f, 250f);
        static readonly Vector3 TerrainSize = new Vector3(400f, 90f, 260f);
        const int HeightRes = 1025, AlphaRes = 1024;
        const int DeadGrass = 0, Mud = 1, Gravel = 2, Rock = 3, Paving = 4, Trampled = 5, Bog = 6;

        // platoul castelului, curtea, santurile, mlastina
        static readonly Rect Plateau = Rect.MinMaxRect(384f, 357f, 456f, 474f);
        const float PlateauY = 12f, MoatY = 8.5f, MoatSouthZ = 366.25f, MoatWestX = 389.75f;
        static readonly Rect Courtyard = Rect.MinMaxRect(400.5f, 379f, 439.5f, 431f);
        static readonly Rect GatePassage = Rect.MinMaxRect(416f, 366f, 424f, 383f);
        const float PosternZ = 415.5f;

        static float[] seamRow;
        static float[,,] seamAlpha;
        static int[] seamLayerMap;
        static RoadHit[] hitCache;
        static readonly Arena3Route.Layers Road3Layers = new Arena3Route.Layers
            { grass = DeadGrass, mud = Mud, gravel = Gravel, rock = Rock, paving = Paving, trampled = Trampled, bog = Bog, peat = Bog, moss = Bog };

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

            CaptureSeam();

            hitCache = new RoadHit[HeightRes * HeightRes];
            float[,] heights = new float[HeightRes, HeightRes];
            for (int z = 0; z < HeightRes; z++)
                for (int x = 0; x < HeightRes; x++)
                {
                    float wx = TerrainPos.x + x / (float)(HeightRes - 1) * TerrainSize.x;
                    float wz = TerrainPos.z + z / (float)(HeightRes - 1) * TerrainSize.z;
                    RoadHit hit = NearestRoad(wx, wz);
                    hitCache[z * HeightRes + x] = hit;
                    heights[z, x] = Mathf.Clamp01((HeightAt(wx, wz, hit) - TerrainPos.y) / TerrainSize.y);
                }
            data.SetHeights(0, 0, heights);

            PaintTerrain(data);

            GameObject go = Terrain.CreateTerrainGameObject(data);
            go.name = TerrainRoot;
            go.transform.position = TerrainPos;
            go.layer = 3;   // Ground: NavMesh si atacurile cauta stratul 3
            Terrain t = go.GetComponent<Terrain>();
            if (arena1Terrain != null)
            {
                go.tag = arena1Terrain.gameObject.tag;
                if (arena1Terrain.materialTemplate != null) t.materialTemplate = arena1Terrain.materialTemplate;
            }
            // rezolutie diferita de terenul din Arena 1: fara lipire automata (altfel Unity avertizeaza la fiecare incarcare)
            t.allowAutoConnect = false;
            t.groupingID = 2;
            t.drawInstanced = true;
            t.heightmapPixelError = 3f;
            t.detailObjectDistance = 90f;
            t.detailObjectDensity = 1f;
            t.basemapDistance = 220f;
            EditorUtility.SetDirty(data);
            hitCache = null;
            return t;
        }

        // randul de sus al terenului din Arena 1 (inaltimi + straturi), ca sa lipim marginea fara treapta
        static void CaptureSeam()
        {
            seamRow = null;
            seamAlpha = null;
            seamLayerMap = null;
            TerrainData a1 = arena1Terrain != null ? arena1Terrain.terrainData : AssetDatabase.LoadAssetAtPath<TerrainData>(Arena1TerrainDataPath);
            if (a1 == null) return;
            int res = a1.heightmapResolution;
            float[,] row = a1.GetHeights(0, res - 1, res, 1);
            seamRow = new float[res];
            for (int i = 0; i < res; i++) seamRow[i] = row[0, i];
            int ares = a1.alphamapResolution;
            seamAlpha = a1.GetAlphamaps(0, ares - 1, ares, 1);
        }

        static float SeamH(float x)
        {
            if (seamRow == null || x < 150f || x > 350f) return 48f;
            float col = (x - 150f) / 200f * (seamRow.Length - 1);
            int i = Mathf.Clamp(Mathf.FloorToInt(col), 0, seamRow.Length - 2);
            float h01 = Mathf.Lerp(seamRow[i], seamRow[i + 1], col - i);
            return h01 * 90f - 30f;
        }

        static float HeightAt(float x, float z, RoadHit hit)
        {
            float s = hit.s, d = hit.dist;
            float halfW = RoadHalfW(s);
            float off = Mathf.Max(0f, d - halfW);
            float n = Fbm(x * 0.03f, z * 0.03f);

            // drumul si umerii lui
            float h = RoadFloor(s) + Smooth(0.5f, 8f, off) * (1.2f + 2.5f * n);

            // dealurile, mai joase langa castel
            float rectDist = RectDistance(Plateau, x, z);
            h += Smooth(35f, 110f, d) * (8f + 26f * Fbm(x * 0.008f, z * 0.008f)) * Smooth(14f, 50f, rectDist);

            // drum adancit intre maluri (de la ~55 m pana la creasta)
            float lane = Smooth(50f, 60f, s) * (1f - Smooth(117f, 127f, s));
            h += lane * 6.5f * Smooth(1f, 9f, off) * (0.75f + 0.5f * n);

            // defileul de la iesirea din Arena 1
            h += (1f - Smooth(15f, 45f, s)) * Mathf.Min(16f, off * 2.2f) * (0.85f + 0.3f * n);

            // fagase de car
            if (d < halfW) h -= 0.12f * (1f - Smooth(0.1f, 0.5f, Mathf.Abs(Mathf.Abs(hit.lat) - 1.05f)));

            // platoul castelului
            h = Mathf.Lerp(h, PlateauY, 1f - Smooth(0f, 14f, rectDist));

            // santul uscat (sud si vest)
            float moatS = (1f - Smooth(2f, 3.6f, Mathf.Abs(z - MoatSouthZ))) * Smooth(384f, 389f, x) * (1f - Smooth(449f, 454f, x));
            float moatW = (1f - Smooth(2f, 3.6f, Mathf.Abs(x - MoatWestX))) * Smooth(361f, 366f, z) * (1f - Smooth(441f, 446f, z));
            float moat = Mathf.Max(moatS, moatW);
            h = Mathf.Lerp(h, MoatY + (Fbm(x * 0.2f, z * 0.2f) - 0.5f) * 0.5f, moat);

            // mlastina din spatele castelului
            float swamp = Smooth(448f, 462f, x) * Smooth(372f, 392f, z) * (1f - Smooth(462f, 478f, z)) * (1f - Smooth(500f, 520f, x));
            h = Mathf.Lerp(h, 5f + (Fbm(x * 0.05f, z * 0.05f) - 0.5f) * 1.6f, swamp);

            // poteca de la posterna in jos, spre mlastina
            float path = (1f - Smooth(2.2f, 3.6f, Mathf.Abs(z - PosternZ))) * Smooth(439f, 442f, x) * (1f - Smooth(470f, 476f, x));
            float pathY = Mathf.Lerp(PlateauY, 6.2f, Smooth(444f, 468f, x)) + (Fbm(x * 0.4f, z * 0.4f) - 0.5f) * 0.25f;
            h = Mathf.Lerp(h, pathY, path);

            // creste pe margini, ca sa nu se vada capatul lumii
            float rim = Mathf.Max(Mathf.Max(1f - Smooth(150f, 185f, x), Smooth(515f, 550f, x)),
                Mathf.Max(Smooth(475f, 510f, z), (1f - Smooth(250f, 285f, z)) * Smooth(340f, 360f, x)));
            h += rim * 22f * (0.7f + 0.6f * Fbm(x * 0.015f + 3f, z * 0.015f));

            // cusatura cu coridorul din Arena 1 (profilul coridorului se prelungeste pe directia drumului)
            float skew = (z - 250f) * 1.376f * (1f - Smooth(320f, 360f, x));
            float seamK = Smooth(250f, 250f + Mathf.Lerp(14f, 40f, Smooth(300f, 360f, x)), z);
            h = Mathf.Lerp(SeamH(x - skew), h, seamK);

            // Drumul 3 pleaca de la posterna spre est: valea lui taie creasta (aceeasi forma si pe terenul din Arena 3)
            h = Arena3Route.Shape(x, z, h);
            return Mathf.Min(h, 58f);
        }

        static float RectDistance(Rect r, float x, float z)
        {
            float dx = Mathf.Max(r.xMin - x, 0f, x - r.xMax);
            float dz = Mathf.Max(r.yMin - z, 0f, z - r.yMax);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        static RoadHit CachedHit(float x, float z)
        {
            int hx = Mathf.Clamp(Mathf.RoundToInt((x - TerrainPos.x) / TerrainSize.x * (HeightRes - 1)), 0, HeightRes - 1);
            int hz = Mathf.Clamp(Mathf.RoundToInt((z - TerrainPos.z) / TerrainSize.z * (HeightRes - 1)), 0, HeightRes - 1);
            return hitCache != null ? hitCache[hz * HeightRes + hx] : NearestRoad(x, z);
        }

        // ------------------------------------------------------------------ straturi + iarba

        static void PaintTerrain(TerrainData data)
        {
            List<TerrainLayer> layers = new List<TerrainLayer>(Arena2ArtSetup.LoadTerrainLayers());
            // straturile din Arena 1 care se vad pe marginea de nord se adauga, ca sa continue textura peste cusatura
            TerrainLayer[] a1Layers = arena1Terrain != null ? arena1Terrain.terrainData.terrainLayers : null;
            if (seamAlpha != null && a1Layers != null)
            {
                int cols = seamAlpha.GetLength(1), count = seamAlpha.GetLength(2);
                seamLayerMap = new int[count];
                for (int l = 0; l < count; l++)
                {
                    float sum = 0f;
                    for (int c = cols / 2; c < cols; c++) sum += seamAlpha[0, c, l];
                    sum /= cols / 2f;
                    seamLayerMap[l] = -1;
                    if (sum < 0.03f || l >= a1Layers.Length || a1Layers[l] == null) continue;
                    int existing = layers.IndexOf(a1Layers[l]);
                    if (existing < 0 && layers.Count < 11) { layers.Add(a1Layers[l]); existing = layers.Count - 1; }
                    seamLayerMap[l] = existing;
                }
            }
            data.terrainLayers = layers.ToArray();
            int L = layers.Count;

            float[,,] alpha = new float[AlphaRes, AlphaRes, L];
            float[,] grassDensity = new float[AlphaRes, AlphaRes];
            float[] w = new float[L];
            for (int az = 0; az < AlphaRes; az++)
                for (int ax = 0; ax < AlphaRes; ax++)
                {
                    float x = TerrainPos.x + (ax + 0.5f) / AlphaRes * TerrainSize.x;
                    float z = TerrainPos.z + (az + 0.5f) / AlphaRes * TerrainSize.z;
                    RoadHit hit = CachedHit(x, z);
                    float steep = data.GetSteepness((ax + 0.5f) / AlphaRes, (az + 0.5f) / AlphaRes);
                    float n = Fbm(x * 0.05f, z * 0.05f), n2 = Fbm(x * 0.13f + 5f, z * 0.13f);
                    for (int l = 0; l < L; l++) w[l] = 0f;
                    w[DeadGrass] = 1f;

                    Paint(w, Mud, Smooth(0.58f, 0.72f, n) * 0.8f);
                    Paint(w, Trampled, Smooth(0.55f, 0.7f, n2) * 0.5f);

                    // drumul: pamant batatorit, fagase noroioase, pietris pe margini
                    float halfW = RoadHalfW(hit.s);
                    float off = hit.dist - halfW;
                    float road = 1f - Smooth(-0.3f, 1.4f + n2, off);
                    Paint(w, Trampled, road);
                    if (off < 0f) Paint(w, Mud, 0.85f * (1f - Smooth(0.15f, 0.55f, Mathf.Abs(Mathf.Abs(hit.lat) - 1.05f))));
                    Paint(w, Gravel, Smooth(-1f, 0f, off) * (1f - Smooth(0.5f, 2.2f, off)) * 0.65f * Smooth(0.35f, 0.6f, n2));

                    Paint(w, Rock, Smooth(28f, 42f, steep));

                    // santul
                    float moatS = (1f - Smooth(2.4f, 4.2f, Mathf.Abs(z - MoatSouthZ))) * Smooth(384f, 389f, x) * (1f - Smooth(449f, 454f, x));
                    float moatW = (1f - Smooth(2.4f, 4.2f, Mathf.Abs(x - MoatWestX))) * Smooth(361f, 366f, z) * (1f - Smooth(441f, 446f, z));
                    Paint(w, Mud, Mathf.Max(moatS, moatW) * 0.9f);

                    // curtea si gangul portii: dale
                    bool paved = Courtyard.Contains(new Vector2(x, z)) || GatePassage.Contains(new Vector2(x, z));
                    if (paved) Paint(w, Paving, 1f);

                    // mlastina + poteca
                    float swamp = Smooth(450f, 466f, x) * Smooth(376f, 396f, z) * (1f - Smooth(458f, 474f, z)) * (1f - Smooth(498f, 518f, x));
                    Paint(w, Bog, swamp * Smooth(0.3f, 0.55f, n));
                    Paint(w, Mud, swamp * 0.6f * (1f - Smooth(0.3f, 0.55f, n)));
                    float path = (1f - Smooth(1.6f, 3f, Mathf.Abs(z - PosternZ))) * Smooth(441f, 443f, x) * (1f - Smooth(468f, 474f, x));
                    Paint(w, Trampled, path * 0.8f);
                    Paint(w, Mud, path * 0.4f * Smooth(452f, 466f, x));

                    // Drumul 3: dalele scufundate, noroiul si baltile din vale
                    float bare = x > 440f ? Arena3Route.Paint(x, z, data.GetInterpolatedHeight((ax + 0.5f) / AlphaRes, (az + 0.5f) / AlphaRes) + TerrainPos.y, steep, w, Road3Layers) : 0f;

                    // langa cusatura: straturile din Arena 1, ca sa nu apara o linie
                    if (seamLayerMap != null && z < 262f && x <= 350f)
                    {
                        float k = 1f - Smooth(250f, 262f, z);
                        int cols = seamAlpha.GetLength(1);
                        int c = Mathf.Clamp(Mathf.RoundToInt((x - 150f) / 200f * cols - 0.5f), 0, cols - 1);
                        float[] a1w = new float[L];
                        float total = 0f;
                        for (int l = 0; l < seamLayerMap.Length; l++)
                        {
                            if (seamLayerMap[l] < 0) continue;
                            a1w[seamLayerMap[l]] += seamAlpha[0, c, l];
                            total += seamAlpha[0, c, l];
                        }
                        if (total > 0.01f)
                            for (int l = 0; l < L; l++) w[l] = Mathf.Lerp(w[l], a1w[l] / total, k);
                    }

                    float sum = 0f;
                    for (int l = 0; l < L; l++) sum += w[l];
                    for (int l = 0; l < L; l++) alpha[az, ax, l] = w[l] / Mathf.Max(sum, 1e-4f);

                    bool castle = RectDistance(Rect.MinMaxRect(392f, 360f, 448f, 470f), x, z) < 0.5f;
                    grassDensity[az, ax] = castle ? 0f : Mathf.Clamp01(w[DeadGrass] + w[Trampled] * 0.25f) * (1f - road) * (1f - Smooth(30f, 40f, steep)) * (1f - bare);
                }
            data.SetAlphamaps(0, 0, alpha);
            PaintGrass(data, grassDensity);
        }

        static void Paint(float[] w, int layer, float amount)
        {
            amount = Mathf.Clamp01(amount);
            for (int i = 0; i < w.Length; i++) w[i] *= 1f - amount;
            w[layer] += amount;
        }

        // iarba moarta: prototipurile din Cinder Hollow, recolorate gri-auriu
        static void PaintGrass(TerrainData data, float[,] density)
        {
            TerrainData cinder = AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/Terrain/CinderHollow_TerrainData.asset");
            if (cinder == null || cinder.detailPrototypes.Length == 0)
            {
                data.detailPrototypes = new DetailPrototype[0];
                return;
            }
            DetailPrototype[] src = cinder.detailPrototypes;
            DetailPrototype[] protos = new DetailPrototype[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                protos[i] = new DetailPrototype(src[i]);
                protos[i].healthyColor = new Color(0.62f, 0.58f, 0.42f);
                protos[i].dryColor = new Color(0.55f, 0.5f, 0.38f);
            }
            data.detailPrototypes = protos;

            System.Random grng = new System.Random(Seed + 7);
            for (int g = 0; g < protos.Length; g++)
            {
                int[,] layer = new int[AlphaRes, AlphaRes];
                for (int z = 0; z < AlphaRes; z++)
                    for (int x = 0; x < AlphaRes; x++)
                    {
                        float d = density[z, x];
                        if (d <= 0.02f) continue;
                        if (g == 0) layer[z, x] = Mathf.Clamp(Mathf.RoundToInt(d * 3f * (0.6f + 0.8f * (float)grng.NextDouble())), 0, 3);
                        else layer[z, x] = grng.NextDouble() < 0.03f * d ? 1 : 0;
                    }
                data.SetDetailLayer(0, 0, g, layer);
            }
        }
    }
}
