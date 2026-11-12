using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Alpha.World.EditorTools
{
    // Finisaje pentru Drumul 2 si Arena 2: lumina salii, ultimul zid de scuturi in fata podului, mormintele,
    // piatra juramantului, panze de paianjen, trecerea spre mlastina. Pentru lupta: pecetile juramantului,
    // cioplirea stratului al doilea, reluarea dupa moarte si coiful scutierului de langa tron.
    public static partial class Arena2Builder
    {
        const string SealMeshPath = FxFolder + "/SM_A2_OathSealRing.asset";
        const string SealMaterialPath = FxFolder + "/M_A2_OathSeal.mat";
        const string CobwebMeshPath = FxFolder + "/SM_A2_Cobweb.asset";
        const string CobwebMaterialPath = FxFolder + "/M_A2_Cobweb.mat";
        const string PuddleMeshPath = FxFolder + "/SM_A2_Puddle.asset";
        const string ShieldPath = FieldArt + "SM_A2_Shield_Heater.fbx";
        const string OathTitle = "The Warden's Oath";
        const string OathBody = "I will hold this hall\nuntil the last of us falls.\n\nThe last of us\nis me.";
        const string LayerTitle = "THE SECOND LAYER FALLS";
        const string LayerSubtitle = "The Oathbound's vow passes into the stone";
        static readonly Vector3 RespawnPoint = new Vector3(GateX, PlateauY, 384f);
        static readonly Vector3 SquireSpot = new Vector3(426.5f, PlateauY, 429.4f);

        // ------------------------------------------------------------------ lumina salii

        static void HallReadability(Transform parent)
        {
            // luna rece de sub acoperis peste toata curtea: cavalerul si lancea se citesc si departe de facle
            ColdFill(parent, "KeyLight_Moon", new Vector3(436f, PlateauY + 15f, 386f), new Vector3(420f, PlateauY + 1f, 412f), 70f, 50f, 150f);
            // contur cald din spatele tronului, ca silueta sa se desprinda de zidul de nord
            Light rim = new GameObject("DaisRim_Warm").AddComponent<Light>();
            rim.transform.SetParent(parent, false);
            rim.transform.position = new Vector3(420f, PlateauY + 3.5f, ThroneZ + 2.2f);
            rim.type = LightType.Point;
            rim.color = new Color(1f, 0.6f, 0.35f);
            rim.range = 9f;
            rim.intensity = 10f;
            rim.shadows = LightShadows.None;
        }

        // ------------------------------------------------------------------ decor

        static void BuildPolish()
        {
            Arena2KnightLook.Apply();
            GameObject road = GameObject.Find(RoadRoot), castle = GameObject.Find(CastleRoot);
            if (road == null || castle == null) return;
            Physics.SyncTransforms();
            LastStand(Group(road.transform, "LastStand"));
            Graves(Group(road.transform, "Graves"));
            ArrowsInDoors(castle.transform);
            Transform court = Group(castle.transform, "Remembrance");
            OathStone(court);
            Corners(court);
            SwampEdge(Group(castle.transform, "SwampEdge"));
        }

        // ultimul zid de scuturi in fata podului; la mijloc e spart, pe acolo a trecut cavalerul
        static void LastStand(Transform parent)
        {
            float s = roadLength - 14f;
            Vector3 c = RoadPoint(s), f = RoadForward(s), r = RoadRight(s);
            float half = RoadHalfW(s) + 1.4f, yaw = RoadYaw(s);
            Material blue = Load<Material>(A2Mat + "M_A2_ShieldPaint_Blue.mat");
            for (float lat = -half; lat <= half; lat += 0.7f)
            {
                if (Mathf.Abs(lat) < 1.2f) continue;
                Vector3 p = c + r * lat + f * R(-0.15f, 0.15f);
                bool fallen = rng.NextDouble() < 0.3;
                GameObject shield = Prop(ShieldPath, parent, p, yaw + 180f + R(-10f, 10f), fallen ? 0.02f : 0.16f, 0f, fallen ? R(70f, 85f) : R(0f, 8f));
                if (shield != null && rng.NextDouble() < 0.35) Swap(shield, "M_A2_ShieldPaint", blue);
                if (!fallen && rng.NextDouble() < 0.7) Brace(parent, p + f * R(0.7f, 1.1f), -f, R(28f, 40f));
            }
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = c - f * R(1.5f, 4.5f) + r * R(-half, half);
                Prop(Pick(Volleys), parent, p, R(0f, 360f), 0.06f);
            }
            Prop(Kit + "Bones_Pile.prefab", parent, c + f * 1.6f + r * (half - 0.6f), R(0f, 360f), 0.05f, 0.22f);
            Prop(Pick(Helmets), parent, c + f * 0.6f, R(0f, 360f), 0.03f, 0f, 40f);
            CrowFlockAt(parent, c - f * 3f + r * 2f, 4);
        }

        // lancea sprijinita in pamant, cu varful spre camp
        static void Brace(Transform parent, Vector3 p, Vector3 toward, float pitch)
        {
            GameObject pike = Spawn(Pikes[0], parent, p, 0f);
            if (pike == null) return;
            float a = pitch * Mathf.Deg2Rad;
            Vector3 dir = (toward * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)).normalized;
            pike.transform.SetPositionAndRotation(new Vector3(p.x, Ground(p.x, p.z) - 0.2f, p.z), Quaternion.LookRotation(-dir));
            StripColliders(pike);
            MarkStatic(pike);
        }

        // mormintele celor cazuti pe drum: movila, tarus, coiful in varf
        static void Graves(Transform parent)
        {
            int made = 0;
            // langa drum e santul; mormintele stau pe campul de dincolo de el
            for (float s = roadLength - 62f; s < roadLength - 24f && made < 7; s += R(2.8f, 4f))
            {
                Vector3 p = Edge(s, made % 2 == 0 ? -1 : 1, R(7.5f, 9.5f));
                if (Steepness(p.x, p.z) > 12f) continue;
                Grave(parent, p, RoadYaw(s) + 90f + R(-12f, 12f), made % 3 == 2);
                made++;
            }
            CrowFlockAt(parent, Edge(roadLength - 40f, 1, 8f), 4);
        }

        // un mormant cat un om culcat: movila lunga, pietre la cap, o cruce din cozi de lance si coiful in varf
        static void Grave(Transform parent, Vector3 p, float yaw, bool shield)
        {
            Quaternion facing = Quaternion.Euler(0f, yaw, 0f);
            Bare(p + facing * Vector3.forward * 0.4f, facing, new Vector2(1.4f, 2.5f));
            GameObject mound = Spawn(Kit + "Ground_Mound.prefab", parent, p, yaw);
            if (mound != null)
            {
                // ~1.7 x 2.9 m si 0.6 m inaltime, nu un musuroi turtit
                mound.transform.localScale = Vector3.Scale(mound.transform.localScale, new Vector3(0.55f, 3.4f, 0.95f));
                // culcata pe panta, nu ingropata dupa coltul cel mai jos
                Vector3 n = terrain.terrainData.GetInterpolatedNormal((p.x - terrain.transform.position.x) / terrain.terrainData.size.x, (p.z - terrain.transform.position.z) / terrain.terrainData.size.z);
                mound.transform.rotation = Quaternion.FromToRotation(Vector3.up, n) * mound.transform.rotation;
                if (TryBounds(mound, out Bounds m))
                    mound.transform.position += new Vector3(p.x - m.center.x, Ground(p.x, p.z) + 0.12f - m.center.y, p.z - m.center.z);
                StripColliders(mound);
                MarkStatic(mound);
            }
            Vector3 head = p + facing * Vector3.forward * 1.55f;
            float ground = Ground(head.x, head.z);
            GameObject stones = Spawn(FieldArt + "SM_A2_Cairn.fbx", parent, head, R(0f, 360f));
            if (stones != null)
            {
                stones.transform.localScale *= 0.55f;
                Put(stones, head, 0.12f);
                StripColliders(stones);
                MarkStatic(stones);
            }
            GameObject stake = Shaft(parent, head, 1.55f, Quaternion.Euler(90f + R(-5f, 5f), R(0f, 360f), 0f));
            if (stake == null || !TryBounds(stake, out Bounds b)) return;
            stake.transform.position += new Vector3(head.x - b.center.x, ground - 0.35f - b.min.y, head.z - b.center.z);
            TryBounds(stake, out b);
            GameObject bar = Shaft(parent, head, 0.62f, Quaternion.LookRotation(facing * Vector3.right) * Quaternion.Euler(0f, 0f, R(-8f, 8f)));
            if (bar != null && TryBounds(bar, out Bounds c))
                bar.transform.position += new Vector3(b.center.x, b.max.y - 0.5f, b.center.z) - c.center;
            GameObject helmet = Spawn(Helmets[0], parent, head, yaw + R(-20f, 20f));
            if (helmet != null)
            {
                helmet.transform.localScale *= 1.25f;
                helmet.transform.rotation *= Quaternion.Euler(R(10f, 18f), 0f, R(-6f, 6f));
                if (TryBounds(helmet, out Bounds h))
                    helmet.transform.position += Vector3.up * (b.max.y - 0.2f - h.min.y) + new Vector3(b.center.x - h.center.x, 0f, b.center.z - h.center.z);
                StripColliders(helmet);
                MarkStatic(helmet);
            }
            if (shield) Prop(ShieldPath, parent, head + facing * Vector3.back * 0.6f, yaw + 180f, 0.12f, 0f, 14f);
        }

        // pamant sapat sub mormant: fara iarba si cu noroi, altfel movila se pierde in iarba inalta
        static void Bare(Vector3 centre, Quaternion facing, Vector2 half)
        {
            if (terrain == null) return;
            TerrainData data = terrain.terrainData;
            Vector3 origin = terrain.transform.position;
            int res = data.alphamapResolution, layers = data.alphamapLayers;
            float cellX = data.size.x / res, cellZ = data.size.z / res, reach = Mathf.Max(half.x, half.y) + 1f;
            int x0 = Mathf.Clamp((int)((centre.x - reach - origin.x) / cellX), 0, res - 1), x1 = Mathf.Clamp((int)((centre.x + reach - origin.x) / cellX) + 1, 0, res - 1);
            int z0 = Mathf.Clamp((int)((centre.z - reach - origin.z) / cellZ), 0, res - 1), z1 = Mathf.Clamp((int)((centre.z + reach - origin.z) / cellZ) + 1, 0, res - 1);
            int w = x1 - x0 + 1, h = z1 - z0 + 1;
            float[,,] alpha = data.GetAlphamaps(x0, z0, w, h);
            int[][,] grass = new int[data.detailPrototypes.Length][,];
            for (int g = 0; g < grass.Length; g++) grass[g] = data.GetDetailLayer(x0, z0, w, h, g);
            Quaternion back = Quaternion.Inverse(facing);
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    Vector3 local = back * (origin + new Vector3((x0 + i + 0.5f) * cellX, 0f, (z0 + j + 0.5f) * cellZ) - centre);
                    float edge = Mathf.Max(Mathf.Abs(local.x) - half.x, Mathf.Abs(local.z) - half.y);
                    if (edge < 0.6f) for (int g = 0; g < grass.Length; g++) grass[g][j, i] = 0;
                    float k = (1f - Smooth(-0.3f, 0.6f, edge)) * 0.85f;
                    if (k <= 0f) continue;
                    for (int l = 0; l < layers; l++) alpha[j, i, l] *= 1f - k;
                    alpha[j, i, Mud] += k;
                }
            data.SetAlphamaps(x0, z0, alpha);
            for (int g = 0; g < grass.Length; g++) data.SetDetailLayer(x0, z0, g, grass[g]);
        }

        static GameObject Shaft(Transform parent, Vector3 at, float scale, Quaternion rotation)
        {
            GameObject pike = Spawn(Pikes[0], parent, at, 0f);
            if (pike == null) return null;
            pike.transform.localScale *= scale;
            pike.transform.rotation = rotation;
            StripColliders(pike);
            MarkStatic(pike);
            return pike;
        }

        // sageti infipte in canaturile portii si ale posternei, pe fata dinspre afara
        static void ArrowsInDoors(Transform castle)
        {
            Transform gate = FindDeep(castle, "CastleGate"), postern = FindDeep(castle, "PosternGate");
            if (gate != null) Stick(gate, Vector3.back, 5);
            if (postern != null) Stick(postern, Vector3.right, 3);
        }

        static void Stick(Transform gate, Vector3 outward, int perDoor)
        {
            Vector3 side = Vector3.Cross(Vector3.up, outward);
            foreach (string name in new[] { "Door_L", "Door_R" })
            {
                Transform hinge = gate.Find(name);
                if (hinge == null || !TryBounds(hinge.gameObject, out Bounds b)) continue;
                Collider[] colliders = hinge.GetComponentsInChildren<Collider>();
                float width = Mathf.Abs(Vector3.Dot(b.extents, side)), height = b.extents.y;
                for (int i = 0; i < perDoor; i++)
                {
                    Vector3 from = b.center + outward * 4f + side * R(-width, width) * 0.75f + Vector3.up * R(-height * 0.5f, height * 0.75f);
                    if (!HitAny(colliders, new Ray(from, -outward), out RaycastHit hit)) continue;
                    GameObject volley = Spawn(Pick(Volleys), hinge, hit.point, 0f);
                    if (volley == null) continue;
                    volley.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0f, R(0f, 360f), 0f);
                    volley.transform.localScale *= R(0.45f, 0.6f);
                    volley.transform.position = hit.point - hit.normal * 0.08f;
                    StripColliders(volley);
                    ClearStatic(volley);
                }
            }
        }

        static bool HitAny(Collider[] colliders, Ray ray, out RaycastHit best)
        {
            best = new RaycastHit();
            bool any = false;
            foreach (Collider c in colliders)
            {
                if (!c.Raycast(ray, out RaycastHit hit, 10f) || (any && hit.distance >= best.distance)) continue;
                best = hit;
                any = true;
            }
            return any;
        }

        // piatra juramantului, la intrarea in curte: ultimul jurat si-a scris aici juramantul
        static void OathStone(Transform parent)
        {
            Vector3 p = new Vector3(412.5f, 0f, 386.5f);
            GameObject tablet = Prop(FieldArt + "SM_A2_LoreTablet.fbx", parent, p, Facing(Flat(new Vector3(GateX, 0f, 388f) - p).normalized), 0.2f, 0f, 0f, true);
            if (tablet == null) return;
            tablet.name = "LoreTablet_WardensOath";
            Prop(Kit + "Candles.prefab", parent, p + new Vector3(1.4f, 0f, 0.6f), R(0f, 360f), 0.02f);
            LorePanel(tablet, OathTitle, OathBody, 3.2f);
        }

        // panze in colturile curtii si lumanari stinse la baza stalpilor
        static void Corners(Transform parent)
        {
            Mesh web = CobwebMesh();
            Material webMat = CobwebMaterial();
            Vector2[] corners = { new Vector2(WallW + 4f, WallN - 4f), new Vector2(WallE - 4f, WallN - 4f), new Vector2(WallW + 4f, GateZ + 4f), new Vector2(WallE - 4f, GateZ + 4f) };
            foreach (Vector2 c in corners)
            {
                Vector3 at = new Vector3(c.x, PlateauY + 0.6f, c.y);
                Vector3 dx = c.x < GateX ? Vector3.left : Vector3.right, dz = c.y > HallCenter.z ? Vector3.forward : Vector3.back;
                if (!Surface(at, dx, out Vector3 wx, out _, 6f) || !Surface(at, dz, out Vector3 wz, out _, 6f)) continue;
                Vector3 corner = new Vector3(wx.x, PlateauY, wz.z);
                Cobweb(parent, web, webMat, corner, -dx, -dz, R(0.9f, 1.3f));
                Cobweb(parent, web, webMat, corner + Vector3.up * R(2.4f, 3.4f), -dx, -dz, R(0.6f, 0.9f));
            }
            foreach (Vector3 pillar in new[] { new Vector3(411f, 0f, 404f), new Vector3(429f, 0f, 412f), new Vector3(411f, 0f, 420f) })
            {
                float x = pillar.x + (pillar.x < GateX ? 1.1f : -1.1f), z = pillar.z + R(-0.6f, 0.6f);
                GameObject candles = Spawn(Kit + "Candles.prefab", parent, new Vector3(x, FloorAt(x, z, PlateauY + 4f), z), R(0f, 360f));
                if (candles == null) continue;
                candles.transform.localScale *= 0.7f;
                StripColliders(candles);
                SetLights(candles, true, false);
            }
        }

        // panza intinsa intre doi pereti si podea (sau un cornis), in planul care taie coltul
        static void Cobweb(Transform parent, Mesh mesh, Material mat, Vector3 corner, Vector3 a, Vector3 b, float size)
        {
            Vector3 forward = Vector3.Dot(Vector3.Cross(Vector3.up, a), b) > 0f ? a : b;
            GameObject go = new GameObject("Cobweb");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(corner, Quaternion.LookRotation(forward, Vector3.up));
            go.transform.localScale = Vector3.one * size;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            MarkStatic(go);
        }

        // in fata posternei pamantul se uda: trestii, balti negre si ceata joasa spre mlastina
        static void SwampEdge(Transform parent)
        {
            Material reeds = TintedCopy(Atmos + "M_Atmos_GrassClump.mat", A2Mat + "M_A2_Reeds.mat", new Color(0.42f, 0.48f, 0.3f));
            for (int i = 0; i < 10; i++)
                Cards(parent, new Vector3(R(446f, 472f), 0f, PosternZ + Side() * R(3.5f, 11f)), 1.6f, 9, reeds, 1.2f, 2f);

            Mesh puddle = PuddleMesh();
            Material water = SwampWater();
            for (int i = 0, made = 0; i < 30 && made < 8; i++)
            {
                float x = R(445f, 470f), z = PosternZ + R(-9f, 9f);
                if (Steepness(x, z) > 9f) continue;
                GameObject go = new GameObject("Puddle");
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(new Vector3(x, Ground(x, z) + 0.03f, z), Quaternion.Euler(0f, R(0f, 360f), 0f));
                go.transform.localScale = new Vector3(R(0.9f, 2.2f), 1f, R(0.8f, 1.6f));
                // fara iarba prin apa, malul e noroi
                Bare(go.transform.position, go.transform.rotation, new Vector2(go.transform.localScale.x, go.transform.localScale.z) * 1.15f);
                go.AddComponent<MeshFilter>().sharedMesh = puddle;
                MeshRenderer r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = water;
                r.shadowCastingMode = ShadowCastingMode.Off;
                MarkStatic(go);
                made++;
            }

            float y = Ground(452f, PosternZ);
            ParticleSystem mist = Loop("Mist_Postern", parent, new Vector3(454f, y + 0.5f, PosternZ), Quaternion.identity, art.fog, 30, 12f, 18f, 3f, 6f, 2f);
            Box(mist, new Vector3(18f, 0.6f, 16f));
            Tint(mist, new Color(0.62f, 0.7f, 0.64f, 0.45f));
            Fade(mist, 0.4f);
            Wander(mist, 0.25f, 0.2f);
        }

        // ------------------------------------------------------------------ lupta: ecran, peceti, cioplire, reluare

        static void BuildRitual(Transform root, Transform canvas, Arena2Encounter encounter, OathboundKnight boss)
        {
            Arena2Screen screen = BuildScreen(root);
            Transform knight = boss.transform;
            Animator animator = knight.GetComponent<Animator>();
            Transform fxRoot = knight.Find("KnightFx");
            Transform lanceSocket = FindDeep(knight, "Socket_Lance_R"), tip = FindDeep(knight, "LanceTip");
            Light glow = KnightGlow(fxRoot);
            KnightFxExtras(knight.GetComponent<KnightFx>(), fxRoot, tip);

            Arena2Respawn respawn = new GameObject("Arena2_Respawn").AddComponent<Arena2Respawn>();
            respawn.transform.SetParent(root, false);
            SetRef(respawn, "encounter", encounter);
            SetRef(respawn, "screen", screen);
            // pivotul jucatorului e in mijlocul capsulei (inalta 1.86), altfel cade prin podea
            Vector3 spawn = new Vector3(RespawnPoint.x, FloorAt(RespawnPoint.x, RespawnPoint.z, PlateauY + 4f) + 1f, RespawnPoint.z);
            SetRef(respawn, "spawnPoint", Marker(root, "Arena2_RespawnPoint", spawn, 0f).transform);

            Arena2Carving carving = BuildCarving(root, canvas, knight, animator, lanceSocket, tip, screen, glow);
            BuildSeals(root, encounter, carving, screen, glow);
            BuildSquire(root, screen);
        }

        // indiciile de jos (ca in Arena 1) si ecranul negru; peste bara de viata si peste benzile cinematice
        static Arena2Screen BuildScreen(Transform root)
        {
            GameObject go = new GameObject("Arena2_HintCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(root, false);
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 25;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform fadeRect = UiRect(go.transform, "Fade", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image fade = fadeRect.gameObject.AddComponent<Image>();
            fade.color = new Color(0f, 0f, 0f, 0f);
            fade.raycastTarget = false;
            fade.enabled = false;

            RectTransform hintRect = UiRect(go.transform, "Hint", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(1400f, 80f));
            Text hint = hintRect.gameObject.AddComponent<Text>();
            hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hint.fontSize = 34;
            hint.alignment = TextAnchor.MiddleCenter;
            hint.color = new Color(0.95f, 0.9f, 0.8f);
            hint.raycastTarget = false;
            hint.horizontalOverflow = HorizontalWrapMode.Wrap;
            Outline outline = hintRect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            hintRect.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            hintRect.gameObject.SetActive(false);

            Arena2Screen screen = go.AddComponent<Arena2Screen>();
            SetRef(screen, "hint", hint);
            SetRef(screen, "fade", fade);
            return screen;
        }

        static RectTransform UiRect(Transform parent, string name, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return rect;
        }

        // lumina din pieptul cavalerului: creste cu fiecare fir primit de la peceti
        static Light KnightGlow(Transform fxRoot)
        {
            Light light = new GameObject("KnightGlow").AddComponent<Light>();
            light.transform.SetParent(fxRoot, false);
            light.transform.localPosition = new Vector3(0f, 1.5f, 0.3f);
            light.gameObject.layer = EnemyLayer;
            light.type = LightType.Point;
            light.color = new Color(1f, 0.5f, 0.25f);
            light.range = 4f;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            light.enabled = false;
            return light;
        }

        // praf la fiecare pas, dara de lumina pe varful lancii la maturare
        static void KnightFxExtras(KnightFx fx, Transform fxRoot, Transform tip)
        {
            if (fx == null || fxRoot == null) return;
            SetRef(fx, "ai", fx.GetComponent<KnightAI>());
            SetRef(fx, "animator", fx.GetComponent<Animator>());

            ParticleSystem dust = FxSystem("FX_StepDust", fxRoot, art.dust != null ? art.dust : GlowMaterial(), 1f);
            dust.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var main = dust.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.startColor = new Color(0.55f, 0.5f, 0.45f, 0.45f);
            main.gravityModifier = -0.03f;
            FxCone(dust, 75f, 0.2f);
            FxFade(dust);
            SetRef(fx, "stepDust", dust);
            if (tip == null) return;

            TrailRenderer trail = new GameObject("FX_LanceTrail").AddComponent<TrailRenderer>();
            trail.transform.SetParent(tip, false);
            trail.time = 0.18f;
            trail.minVertexDistance = 0.05f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.22f), new Keyframe(1f, 0f));
            trail.colorGradient = Fading(new Color(1f, 0.85f, 0.6f), new Color(1f, 0.4f, 0.15f), 0.7f);
            trail.sharedMaterial = SealMaterial();
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
            SetRef(fx, "trail", trail);

            ParticleSystem sparks = FxSystem("FX_TrailSparks", tip, GlowMaterial(), 1f);
            sparks.transform.localPosition = Vector3.zero;
            main = sparks.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.45f), new Color(1f, 0.55f, 0.18f));
            main.gravityModifier = 1.2f;
            var emission = sparks.emission;
            emission.rateOverDistance = 18f;
            emission.enabled = false;
            FxCone(sparks, 60f, 0.05f);
            FxFade(sparks);
            SetRef(fx, "trailSparks", sparks);
            foreach (Transform t in tip.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = EnemyLayer;
            foreach (Transform t in fxRoot.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = EnemyLayer;
        }

        static Arena2Carving BuildCarving(Transform root, Transform canvas, Transform knight, Animator animator, Transform lanceSocket, Transform tip, Arena2Screen screen, Light glow)
        {
            Arena2Carving carving = new GameObject("Arena2_Carving").AddComponent<Arena2Carving>();
            carving.transform.SetParent(root, false);
            Transform card = canvas != null ? canvas.Find("CarvingCard") : null;
            Transform flash = canvas != null ? canvas.Find("CarvingFlash") : null;
            if (canvas != null)
            {
                SetText(canvas, "CarvingCard/Title", LayerTitle, 0);
                SetText(canvas, "CarvingCard/Subtitle/Text", LayerSubtitle, 0);
                SetRefs(carving, "letterbox", new Object[] { canvas.Find("Letterbox_Top"), canvas.Find("Letterbox_Bottom") });
            }
            if (flash != null) flash.GetComponent<Image>().color = new Color(1f, 0.8f, 0.55f, 0f);

            TrailRenderer thread = Thread(carving.transform, "CarvingThread", 0.12f);
            GameObject dust = Load<GameObject>(Fx + "FX_BurstDust.prefab");
            Transform embers = FindDeep(knight, "FX_Embers");
            Material stone = Load<Material>(A2Mat + "M_A2_Engraving.mat");

            SetRef(carving, "knight", knight);
            SetRef(carving, "knightAnimator", animator);
            SetRef(carving, "lanceSocket", lanceSocket);
            SetRef(carving, "lanceTip", tip);
            SetRef(carving, "titleCard", card != null ? card.GetComponent<BossTitleCard>() : null);
            SetRef(carving, "flash", flash != null ? flash.GetComponent<Image>() : null);
            SetRef(carving, "dustPrefab", dust != null ? dust.GetComponent<ParticleSystem>() : null);
            SetRef(carving, "screen", screen);
            SetRef(carving, "thread", thread);
            SetRef(carving, "knightGlow", glow);
            SetRef(carving, "knightEmbers", embers != null ? embers.GetComponent<ParticleSystem>() : null);
            SetRef(carving, "statueTexture", stone != null ? stone.GetTexture("_BaseMap") : null);
            return carving;
        }

        // firul rosu-auriu: dara care zboara de la pecete (sau de la lance) spre tinta
        static TrailRenderer Thread(Transform parent, string name, float width)
        {
            TrailRenderer trail = new GameObject(name).AddComponent<TrailRenderer>();
            trail.transform.SetParent(parent, false);
            trail.time = 0.6f;
            trail.minVertexDistance = 0.05f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, width), new Keyframe(1f, 0f));
            trail.colorGradient = Fading(new Color(1f, 0.78f, 0.35f), new Color(1f, 0.22f, 0.1f), 1f);
            trail.sharedMaterial = SealMaterial();
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            Light light = new GameObject("Light").AddComponent<Light>();
            light.transform.SetParent(trail.transform, false);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.6f, 0.3f);
            light.range = 3.5f;
            light.intensity = 3f;
            light.shadows = LightShadows.None;
            trail.gameObject.AddComponent<ThreadRibbon>();
            trail.gameObject.SetActive(false);
            return trail;
        }

        static Gradient Fading(Color head, Color tail, float alpha)
        {
            Gradient g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(head, 0f), new GradientColorKey(tail, 1f) },
                new[] { new GradientAlphaKey(alpha, 0f), new GradientAlphaKey(alpha * 0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        // trei peceti sus pe ziduri, deasupra scutului si sub flamura fiecarei case; ultima (nord) e pazita
        static void BuildSeals(Transform root, Arena2Encounter encounter, Arena2Carving carving, Arena2Screen screen, Light glow)
        {
            Transform group = Group(root, "Arena2_Seals");
            OathSeal[] seals =
            {
                Seal(group, "OathSeal_West", new Vector3(WallW + 1.5f, PlateauY + 5f, 399.5f), Vector3.right, false),
                Seal(group, "OathSeal_East", new Vector3(WallE - 1.5f, PlateauY + 5f, 399.5f), Vector3.left, false),
                Seal(group, "OathSeal_North", new Vector3(413.5f, PlateauY + 5.5f, WallN - 1.5f), Vector3.back, true)
            };
            Arena2Seals ritual = group.gameObject.AddComponent<Arena2Seals>();
            SetRef(ritual, "encounter", encounter);
            SetRefs(ritual, "seals", seals);
            SetRef(ritual, "carving", carving);
            SetRef(ritual, "screen", screen);
            SetRef(ritual, "knightGlow", glow);
        }

        static OathSeal Seal(Transform parent, string name, Vector3 wall, Vector3 normal, bool warded)
        {
            // fata zidului: raza din curte spre zid
            if (Surface(wall + normal * 3f, -normal, out Vector3 hit, out _, 6f)) wall = hit;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(wall + normal * 0.12f, Quaternion.LookRotation(normal));
            Mesh ring = SealMesh();
            Material mat = SealMaterial();

            GameObject sigil = new GameObject("Sigil");
            sigil.transform.SetParent(go.transform, false);
            sigil.transform.localScale = Vector3.one * 1.25f;
            sigil.AddComponent<MeshFilter>().sharedMesh = ring;
            MeshRenderer sr = sigil.AddComponent<MeshRenderer>();
            sr.sharedMaterial = mat;
            sr.shadowCastingMode = ShadowCastingMode.Off;
            sr.receiveShadows = false;

            Light light = new GameObject("Glow").AddComponent<Light>();
            light.transform.SetParent(go.transform, false);
            light.transform.localPosition = new Vector3(0f, 0f, 0.8f);
            light.type = LightType.Point;
            light.range = 8f;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            light.enabled = false;

            // scutul casei, atarnat sub pecete
            GameObject shield = Spawn(ShieldPath, go.transform, wall + Vector3.down * 2.1f + normal * 0.12f, 0f);
            if (shield != null)
            {
                shield.transform.rotation = Quaternion.LookRotation(normal);
                shield.transform.localScale *= 1.4f;
                StripColliders(shield);
            }
            // flamura deasupra
            GameObject banner = HangBanner(go.transform, wall + Vector3.up * 4.4f + normal * 0.1f, Facing(Flat(normal)), 2, 1.3f);

            ParticleSystem burst = FxSystem("FX_SealBurst", go.transform, GlowMaterial(), 1f);
            var main = burst.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.4f), new Color(1f, 0.35f, 0.15f));
            main.gravityModifier = 0.8f;
            FxCone(burst, 45f, 0.6f);
            FxFade(burst);

            ParticleSystem fire = FxSystem("FX_BannerFire", go.transform, GlowMaterial(), 2f);
            fire.transform.localPosition = new Vector3(0f, 3.1f, 0.15f);
            fire.transform.localRotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
            main = fire.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.45f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.15f), new Color(1f, 0.3f, 0.08f));
            main.gravityModifier = -0.15f;
            var emission = fire.emission;
            emission.rateOverTime = 40f;
            var shape = fire.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.3f, 0.1f, 2.4f);
            FxFade(fire);

            TrailRenderer thread = Thread(go.transform, "Thread", 0.12f);

            AudioSource audio = go.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;
            audio.rolloffMode = AudioRolloffMode.Linear;
            audio.minDistance = 6f;
            audio.maxDistance = 60f;

            OathSeal seal = go.AddComponent<OathSeal>();
            SetRef(seal, "sigil", sr);
            SetRef(seal, "glow", light);
            SetRef(seal, "shield", shield != null ? shield.transform : null);
            SetRef(seal, "banner", banner != null ? banner.transform : null);
            SetRef(seal, "burst", burst);
            SetRef(seal, "fire", fire);
            SetRef(seal, "thread", thread);
            SetRef(seal, "audioSource", audio);
            SetBool(seal, "warded", warded);
            SetFloat(seal, "floorY", PlateauY);
            SetFloat(seal, "reachDown", 2.4f);
            return seal;
        }

        // coiful scutierului, uitat in spatele tronului; cine ingenuncheaza in fata lui e investit cavaler
        static void BuildSquire(Transform root, Arena2Screen screen)
        {
            Transform egg = Group(root, "Arena2_Squire");
            Vector3 p = new Vector3(SquireSpot.x, FloorAt(SquireSpot.x, SquireSpot.z, PlateauY + 4f), SquireSpot.z);
            GameObject helmet = Spawn(Helmets[1], egg, p, 200f);
            if (helmet != null)
            {
                helmet.transform.localScale *= 0.8f;
                if (TryBounds(helmet, out Bounds b)) helmet.transform.position += Vector3.up * (p.y - b.min.y - 0.02f);
                StripColliders(helmet);
            }
            GameObject candle = Spawn(Kit + "Candles.prefab", egg, p + new Vector3(-0.7f, 0f, 0.15f), R(0f, 360f));
            if (candle != null)
            {
                candle.transform.localScale *= 0.5f;
                StripColliders(candle);
                SetLights(candle, true, false);
                Boost(candle, 0.35f, 2f);
            }

            GameObject go = new GameObject("SquireAccolade");
            go.transform.SetParent(egg, false);
            go.transform.position = p + Vector3.up * 0.8f;
            go.layer = IgnoreRaycastLayer;
            SquireAccolade accolade = go.AddComponent<SquireAccolade>();
            SphereCollider trigger = go.GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 3f;

            Light wisp = new GameObject("Wisp").AddComponent<Light>();
            wisp.transform.SetParent(go.transform, false);
            wisp.type = LightType.Point;
            wisp.color = new Color(1f, 0.82f, 0.5f);
            wisp.range = 3f;
            wisp.intensity = 0f;
            wisp.shadows = LightShadows.None;
            wisp.enabled = false;

            ParticleSystem motes = Loop("Motes", go.transform, go.transform.position, Quaternion.identity, GlowMaterial(), 60, 0.8f, 1.4f, 0.03f, 0.06f, 12f);
            var main = motes.main;
            main.playOnAwake = false;
            main.prewarm = false;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            main.startColor = new Color(1f, 0.85f, 0.55f);
            var shape = motes.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;
            Wander(motes, 0.3f, 0.8f);
            FxFade(motes);

            AudioSource audio = go.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0.6f;
            audio.minDistance = 2f;
            audio.maxDistance = 20f;

            SetRef(accolade, "helmet", helmet != null ? helmet.transform : null);
            SetRef(accolade, "wisp", wisp);
            SetRef(accolade, "motes", motes);
            SetRef(accolade, "audioSource", audio);
            SetRef(accolade, "screen", screen);
        }

        // ------------------------------------------------------------------ asset-uri generate

        static Material SealMaterial()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(SealMaterialPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, SealMaterialPath);
            }
            mat.SetTexture("_BaseMap", null);
            mat.SetColor("_BaseColor", Color.white);
            Transparent(mat, BlendMode.One);
            return mat;
        }

        static Material CobwebMaterial()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(CobwebMaterialPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, CobwebMaterialPath);
            }
            mat.SetTexture("_BaseMap", null);
            mat.SetColor("_BaseColor", new Color(0.8f, 0.8f, 0.76f, 0.42f));
            mat.SetFloat("_Blend", 0f);
            Transparent(mat, BlendMode.OneMinusSrcAlpha);
            return mat;
        }

        static void Transparent(Material mat, BlendMode dst)
        {
            mat.SetFloat("_Surface", 1f);
            if (dst == BlendMode.One) mat.SetFloat("_Blend", 2f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)dst);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
        }

        static Material SwampWater()
        {
            string path = A2Mat + "M_A2_SwampWater.mat";
            // Simple Lit: fara reflexia cerului (din Lit venea albastrul la unghi razant), ramane doar luciul soarelui
            Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_BaseColor", new Color(0.022f, 0.032f, 0.018f));
            mat.SetColor("_SpecColor", new Color(0.32f, 0.33f, 0.26f));
            mat.SetFloat("_Smoothness", 0.82f);
            mat.SetFloat("_SpecularHighlights", 1f);
            mat.EnableKeyword("_SPECULAR_COLOR");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // pecetea: doua cercuri, crestaturi, si sabia juramantului in mijloc (in planul XY, fata spre +z)
        static Mesh SealMesh()
        {
            var v = new List<Vector3>();
            var t = new List<int>();
            Ring(v, t, 1f, 0.07f, 48);
            Ring(v, t, 0.74f, 0.04f, 40);
            for (int i = 0; i < 12; i++)
            {
                Vector3 d = Quaternion.Euler(0f, 0f, i * 30f) * Vector3.up;
                Line(v, t, d * 0.8f, d * 0.93f, i % 3 == 0 ? 0.05f : 0.025f);
            }
            Line(v, t, new Vector3(0f, -0.62f, 0f), new Vector3(0f, 0.55f, 0f), 0.07f);
            Line(v, t, new Vector3(-0.24f, 0.28f, 0f), new Vector3(0.24f, 0.28f, 0f), 0.06f);
            for (int i = 0; i < 3; i++)
            {
                Vector3 a = Quaternion.Euler(0f, 0f, i * 120f) * Vector3.down * 0.66f, b = Quaternion.Euler(0f, 0f, (i + 1) * 120f) * Vector3.down * 0.66f;
                Line(v, t, a, b, 0.03f);
            }
            return SaveMesh(SealMeshPath, v, t);
        }

        // panza: triunghiul care taie coltul (pereti pe +x si +z, podea pe +y), raze si spirala
        static Mesh CobwebMesh()
        {
            var v = new List<Vector3>();
            var t = new List<int>();
            Vector3 a = Vector3.right, b = Vector3.up, c = Vector3.forward, o = (a + b + c) / 3f;
            Vector3[] rim = new Vector3[12];
            for (int i = 0; i < rim.Length; i++)
            {
                float k = i / (float)rim.Length * 3f;
                int side = (int)k;
                float f = k - side;
                Vector3 from = side == 0 ? a : side == 1 ? b : c, to = side == 0 ? b : side == 1 ? c : a;
                rim[i] = Vector3.Lerp(from, to, f);
            }
            foreach (Vector3 p in rim) Line3(v, t, o, p, 0.006f);
            for (int ring = 1; ring <= 6; ring++)
            {
                float k = ring / 6.5f;
                for (int i = 0; i < rim.Length; i++)
                {
                    Vector3 p = Vector3.Lerp(o, rim[i], k), q = Vector3.Lerp(o, rim[(i + 1) % rim.Length], k * (1f + 0.04f * ((i * 7 + ring) % 3)));
                    Line3(v, t, p + Vector3.down * 0.01f * (i % 2), q, 0.004f);
                }
            }
            return SaveMesh(CobwebMeshPath, v, t);
        }

        // balta: disc neregulat, plat
        static Mesh PuddleMesh()
        {
            var v = new List<Vector3> { Vector3.zero };
            var t = new List<int>();
            const int n = 20;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                float r = 1f + 0.18f * Mathf.Sin(a * 3f + 1.3f) + 0.1f * Mathf.Sin(a * 5f + 0.4f);
                v.Add(new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }
            for (int i = 0; i < n; i++) t.AddRange(new[] { 0, 1 + (i + 1) % n, 1 + i });
            return SaveMesh(PuddleMeshPath, v, t);
        }

        static void Ring(List<Vector3> v, List<int> t, float radius, float width, int segments)
        {
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f, b = (i + 1) / (float)segments * Mathf.PI * 2f;
                Line(v, t, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius, new Vector3(Mathf.Cos(b), Mathf.Sin(b), 0f) * radius, width);
            }
        }

        static void Line(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, float width)
        {
            Vector3 side = Vector3.Cross(b - a, Vector3.forward).normalized * width * 0.5f;
            Quad(v, t, a - side, a + side, b + side, b - side);
        }

        static void Line3(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, float width)
        {
            Vector3 normal = new Vector3(1f, 1f, 1f).normalized;
            Vector3 side = Vector3.Cross(b - a, normal).normalized * width * 0.5f;
            Quad(v, t, a - side, a + side, b + side, b - side);
        }

        static void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i = v.Count;
            v.AddRange(new[] { a, b, c, d });
            t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
        }

        static Mesh SaveMesh(string path, List<Vector3> vertices, List<int> triangles)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool created = mesh == null;
            if (created) mesh = new Mesh();
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (created) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);
            return mesh;
        }
    }
}
