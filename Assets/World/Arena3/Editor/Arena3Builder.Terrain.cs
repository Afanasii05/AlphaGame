using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Alpha.World.EditorTools
{
    // Terenul de la est de Arena 2: continuarea vaii Drumului 3, bazinul cu apa neagra si insula arenei.
    // Relieful de baza e aceeasi formula ca pe marginea de est a terenului din Arena 2, iar valea vine din Arena3Route,
    // deci pe cusatura (x = 550) inaltimile si straturile sunt identice.
    public static partial class Arena3Builder
    {
        const string TerrainDataPath = Folder + "/Terrain/Arena3_TerrainData.asset";
        static readonly Vector3 TerrainPos = new Vector3(550f, -30f, 250f);
        static readonly Vector3 TerrainSize = new Vector3(260f, 90f, 260f);
        const int HeightRes = 1025, AlphaRes = 1024;
        // aceeasi marime de celula pentru iarba ca in Arena 2 (1024 pe 400 m), ca densitatea sa nu sara pe cusatura
        const int DetailRes = 672;
        // straturile Arenei 2 (aceeasi ordine) + turba si muschiul ud
        const int DeadGrass = 0, Mud = 1, Gravel = 2, Rock = 3, Paving = 4, Trampled = 5, Bog = 6, Peat = 7, WetMoss = 8;
        static readonly Arena3Route.Layers RouteLayers = new Arena3Route.Layers
            { grass = DeadGrass, mud = Mud, gravel = Gravel, rock = Rock, paving = Paving, trampled = Trampled, bog = Bog, peat = Peat, moss = WetMoss };

        static TerrainLayer peatLayer, mossLayer;

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
            data.SetDetailResolution(DetailRes, 32);
            data.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);

            float[,] heights = new float[HeightRes, HeightRes];
            for (int z = 0; z < HeightRes; z++)
                for (int x = 0; x < HeightRes; x++)
                {
                    float wx = TerrainPos.x + x / (float)(HeightRes - 1) * TerrainSize.x;
                    float wz = TerrainPos.z + z / (float)(HeightRes - 1) * TerrainSize.z;
                    heights[z, x] = Mathf.Clamp01((HeightAt(wx, wz) - TerrainPos.y) / TerrainSize.y);
                }
            data.SetHeights(0, 0, heights);

            PaintTerrain(data);

            GameObject go = Terrain.CreateTerrainGameObject(data);
            go.name = TerrainRoot;
            go.transform.position = TerrainPos;
            go.layer = 3;   // Ground
            Terrain t = go.GetComponent<Terrain>();
            if (arena2Terrain != null)
            {
                go.tag = arena2Terrain.gameObject.tag;
                if (arena2Terrain.materialTemplate != null) t.materialTemplate = arena2Terrain.materialTemplate;
            }
            t.allowAutoConnect = false;
            t.groupingID = 3;
            t.drawInstanced = true;
            t.heightmapPixelError = 2f;
            t.detailObjectDistance = 90f;
            t.detailObjectDensity = 1f;
            t.basemapDistance = 220f;
            EditorUtility.SetDirty(data);
            return t;
        }

        // marginea de est a terenului din Arena 2 (dealuri + creasta), fara drumul de acolo
        public static float BaseHeight(float x, float z)
        {
            float n = Fbm(x * 0.03f, z * 0.03f);
            float h = 11.98f + 1.2f + 2.5f * n;
            h += 8f + 26f * Fbm(x * 0.008f, z * 0.008f);
            h += 22f * (0.7f + 0.6f * Fbm(x * 0.015f + 3f, z * 0.015f));
            // langa marginea de sud terenul din Arena 2 coboara spre cusatura cu Arena 1
            h = Mathf.Lerp(48f, h, Smooth(250f, 290f, z));
            return h;
        }

        public static float HeightAt(float x, float z) => Mathf.Min(Arena3Route.Shape(x, z, BaseHeight(x, z)), 58f);

        // ------------------------------------------------------------------ straturi + iarba

        static void PrepareTerrainLayers()
        {
            peatLayer = Layer("TL_A3_Peat", "Assets/World/Terrain/TL_Ground068.terrainlayer", new Vector4(0.26f, 0.23f, 0.2f, 1f), 3.5f, 0.45f);
            mossLayer = Layer("TL_A3_WetMoss", "Assets/World/Terrain/TL_Moss002.terrainlayer", new Vector4(0.32f, 0.38f, 0.28f, 1f), 3f, 0.3f);
        }

        static TerrainLayer Layer(string name, string sourcePath, Vector4 tint, float tile, float smooth)
        {
            TerrainLayer source = Load<TerrainLayer>(sourcePath);
            if (source == null) return null;
            string path = $"{TerrainArtFolder}/{name}.terrainlayer";
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, path);
            }
            layer.diffuseTexture = source.diffuseTexture;
            layer.normalMapTexture = source.normalMapTexture;
            layer.maskMapTexture = source.maskMapTexture;
            layer.normalScale = source.normalScale;
            layer.tileSize = new Vector2(tile, tile);
            layer.diffuseRemapMin = Vector4.zero;
            layer.diffuseRemapMax = tint;
            layer.smoothness = smooth;
            layer.metallic = 0f;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        static void PaintTerrain(TerrainData data)
        {
            List<TerrainLayer> layers = new List<TerrainLayer>(Arena2ArtSetup.LoadTerrainLayers()) { peatLayer, mossLayer };
            data.terrainLayers = layers.ToArray();
            int L = layers.Count;

            float[,,] alpha = new float[AlphaRes, AlphaRes, L];
            float[,] grass = new float[AlphaRes, AlphaRes];
            float[] w = new float[L];
            for (int az = 0; az < AlphaRes; az++)
                for (int ax = 0; ax < AlphaRes; ax++)
                {
                    float u = (ax + 0.5f) / AlphaRes, v = (az + 0.5f) / AlphaRes;
                    float x = TerrainPos.x + u * TerrainSize.x, z = TerrainPos.z + v * TerrainSize.z;
                    float steep = data.GetSteepness(u, v);
                    float h = data.GetInterpolatedHeight(u, v) + TerrainPos.y;
                    float n = Fbm(x * 0.05f, z * 0.05f), n2 = Fbm(x * 0.13f + 5f, z * 0.13f);
                    for (int l = 0; l < L; l++) w[l] = 0f;

                    // aceeasi baza ca pe marginea de est a Arenei 2
                    w[DeadGrass] = 1f;
                    Arena3Route.Paint(w, Mud, Smooth(0.58f, 0.72f, n) * 0.8f);
                    Arena3Route.Paint(w, Trampled, Smooth(0.55f, 0.7f, n2) * 0.5f);
                    Arena3Route.Paint(w, Rock, Smooth(28f, 42f, steep));
                    // spre est dealurile se acopera de muschi ud
                    float swampy = Smooth(552f, 600f, x);
                    Arena3Route.Paint(w, WetMoss, swampy * 0.55f * Smooth(0.4f, 0.65f, Fbm(x * 0.04f + 7f, z * 0.04f)) * (1f - Smooth(26f, 40f, steep)));
                    Arena3Route.Paint(w, Bog, swampy * 0.35f * Smooth(0.5f, 0.7f, n2) * (1f - Smooth(26f, 40f, steep)));

                    float bare = Arena3Route.Paint(x, z, h, steep, w, RouteLayers);

                    // pe cusatura terenul din Arena 2 foloseste Bog in loc de turba si muschi: trecem treptat la straturile noi
                    float k = Smooth(550f, 575f, x);
                    if (k < 1f)
                    {
                        w[Bog] += (w[Peat] + w[WetMoss]) * (1f - k);
                        w[Peat] *= k;
                        w[WetMoss] *= k;
                    }

                    float sum = 0f;
                    for (int l = 0; l < L; l++) sum += w[l];
                    for (int l = 0; l < L; l++) alpha[az, ax, l] = w[l] / Mathf.Max(sum, 1e-4f);
                    grass[az, ax] = Mathf.Clamp01(w[DeadGrass] + w[Trampled] * 0.25f + w[WetMoss] * 0.35f * k) * (1f - Smooth(30f, 40f, steep)) * (1f - bare);
                }
            data.SetAlphamaps(0, 0, alpha);
            PaintGrass(data, grass);
        }

        // iarba: aceleasi prototipuri ca in Arena 2; spre est iarba de mlastina, mai verde si mai inalta
        static void PaintGrass(TerrainData data, float[,] density)
        {
            TerrainData cinder = AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/Terrain/CinderHollow_TerrainData.asset");
            if (cinder == null || cinder.detailPrototypes.Length == 0)
            {
                data.detailPrototypes = new DetailPrototype[0];
                return;
            }
            DetailPrototype field = new DetailPrototype(cinder.detailPrototypes[0])
            {
                healthyColor = new Color(0.62f, 0.58f, 0.42f),
                dryColor = new Color(0.55f, 0.5f, 0.38f)
            };
            DetailPrototype swamp = new DetailPrototype(cinder.detailPrototypes[0])
            {
                healthyColor = new Color(0.44f, 0.47f, 0.3f),
                dryColor = new Color(0.36f, 0.34f, 0.24f)
            };
            swamp.minHeight *= 1.15f;
            swamp.maxHeight *= 1.35f;
            data.detailPrototypes = new[] { field, swamp };

            System.Random grng = new System.Random(Seed + 7);
            int[,] a = new int[DetailRes, DetailRes], b = new int[DetailRes, DetailRes];
            for (int z = 0; z < DetailRes; z++)
                for (int x = 0; x < DetailRes; x++)
                {
                    int ax = Mathf.Min(AlphaRes - 1, (int)((x + 0.5f) / DetailRes * AlphaRes));
                    int az = Mathf.Min(AlphaRes - 1, (int)((z + 0.5f) / DetailRes * AlphaRes));
                    float d = density[az, ax];
                    float roll = (float)grng.NextDouble(), pick = (float)grng.NextDouble();
                    if (d <= 0.02f) continue;
                    int count = Mathf.Clamp(Mathf.RoundToInt(d * 3f * (0.6f + 0.8f * roll)), 0, 3);
                    float wx = TerrainPos.x + (x + 0.5f) / DetailRes * TerrainSize.x;
                    if (pick < Smooth(555f, 610f, wx)) b[z, x] = count;
                    else a[z, x] = count;
                }
            data.SetDetailLayer(0, 0, 0, a);
            data.SetDetailLayer(0, 0, 1, b);
        }
    }
}
