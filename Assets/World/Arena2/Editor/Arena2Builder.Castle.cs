using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Alpha.World.EditorTools
{
    // Castelul: zidurile curtii, poarta cu canaturi si grilaj, sala tronului cu acoperisul prabusit,
    // cavalerul asezat pe tron, poterna din spate spre mlastina si NavMesh-ul arenei.
    public static partial class Arena2Builder
    {
        static readonly Vector3 HallCenter = new Vector3(420f, PlateauY, 406f);
        // punctele de pe dale unde cad razele; golurile din acoperis se calculeaza din directia soarelui
        static readonly Vector3[] RoofGaps = { new Vector3(419.5f, PlateauY, 422.5f), new Vector3(426f, PlateauY, 409f) };

        const float PieceY = PlateauY - 0.9f;   // soclul pieselor de castel iese 0.9 m peste pivot: asa ajunge la nivelul dalelor
        const float GateX = 420f, GateZ = 378f;
        const float WallW = 399f, WallE = 441f, WallN = 432f;
        const float RoofY = 30.86f, ArchY = 22.31f;
        const float ThroneZ = 425.6f;
        const int IgnoreRaycastLayer = 2;

        static readonly Color ColdKey = new Color(0.66f, 0.76f, 1f);

        static void BuildCastle()
        {
            Transform root = new GameObject(CastleRoot).transform;
            Curtain(Group(root, "Walls"));
            Physics.SyncTransforms();

            Gatehouse(root);
            Hall(root);
            Postern(root);
            Swamp(Group(root, "Swamp"));
            Containment(Group(root, "Containment"));
            Physics.SyncTransforms();
            ThroneAndKnight();

            Marker(root, "Arena2_CourtyardCenter", new Vector3(420f, PlateauY, Courtyard.center.y), 0f);
            Marker(root, "Arena2_RearExit", new Vector3(WallE + 2.6f, PlateauY, PosternZ), 90f);
            Marker(root, "Road3_Start", new Vector3(463f, Ground(463f, PosternZ), PosternZ), 90f);
        }

        // ------------------------------------------------------------------ zidurile curtii

        static void Curtain(Transform parent)
        {
            // fata -z a fiecarei piese priveste spre exterior; segmentele de 12 m se suprapun 1.5 m
            foreach (float x in new[] { 403.5f, 436.5f })
                Piece(CastleArt + "SM_Castle_Wall.fbx", parent, new Vector3(x, PieceY, GateZ), 0f);

            float[] sideZ = { 384f, 394.5f, 405f, 415.5f, 426f };
            string[] west = { "SM_Castle_Wall", "SM_Castle_WallWindow", "SM_Castle_WallRuin", "SM_Castle_Wall", "SM_Castle_WallWindow" };
            string[] east = { "SM_Castle_WallWindow", "SM_Castle_Wall", "SM_Castle_WallRuin", null, "SM_Castle_Wall" };
            for (int i = 0; i < sideZ.Length; i++)
            {
                Piece(CastleArt + west[i] + ".fbx", parent, new Vector3(WallW, PieceY, sideZ[i]), 90f);
                if (east[i] != null) Piece(CastleArt + east[i] + ".fbx", parent, new Vector3(WallE, PieceY, sideZ[i]), 270f);
            }

            float[] northX = { 404.25f, 414.75f, 425.25f, 435.75f };
            string[] north = { "SM_Castle_WallWindow", "SM_Castle_Wall", "SM_Castle_Wall", "SM_Castle_WallWindow" };
            for (int i = 0; i < northX.Length; i++)
                Piece(CastleArt + north[i] + ".fbx", parent, new Vector3(northX[i], PieceY, WallN), 180f);

            Piece(CastleArt + "SM_Castle_Tower.fbx", parent, new Vector3(WallW, PieceY, GateZ), 0f);
            Piece(CastleArt + "SM_Castle_Tower.fbx", parent, new Vector3(WallE, PieceY, GateZ), 90f);
            Piece(CastleArt + "SM_Castle_TowerRoofed.fbx", parent, new Vector3(WallW, PieceY, WallN), 0f);
            Piece(CastleArt + "SM_Castle_TowerRuin.fbx", parent, new Vector3(WallE, PieceY, WallN), 180f);
            Piece(CastleArt + "SM_Castle_Keep.fbx", parent, new Vector3(420f, PieceY, 455f), 0f);

            // daramaturi la piciorul zidurilor surpate, pe ambele fete
            foreach (Vector3 ruin in new[] { new Vector3(WallW, 0f, 405f), new Vector3(WallE, 0f, 405f) })
                foreach (int side in new[] { -1, 1 })
                {
                    for (int i = 0; i < 3; i++)
                        Prop(Kit + "Rubble_Pile.prefab", parent, ruin + new Vector3(side * R(3.2f, 4.5f), 0f, R(-4.5f, 4.5f)), R(0f, 360f), 0.1f, R(0.7f, 1.3f), 0f, true);
                    for (int i = 0; i < 4; i++)
                        Prop(Pick(Rocks), parent, ruin + new Vector3(side * R(2.8f, 5.5f), 0f, R(-5f, 5f)), R(0f, 360f), 0.05f, R(0.3f, 0.7f));
                }
        }

        // ------------------------------------------------------------------ poarta: canaturi, grilaj, pod mobil, facle, stindarde

        static CastleGate Gatehouse(Transform root)
        {
            Transform gateRoot = Group(root, "Gatehouse");
            Piece(CastleArt + "SM_Castle_Gatehouse.fbx", gateRoot, new Vector3(GateX, PieceY, GateZ), 0f);
            Physics.SyncTransforms();

            GameObject gateGo = new GameObject("CastleGate");
            gateGo.transform.SetParent(gateRoot, false);
            gateGo.transform.position = new Vector3(GateX, PlateauY, GateZ);
            Transform gt = gateGo.transform;

            // canaturile: pivot pe balama, spre curte; deschise stau lipite de zid, inchise umplu arcada
            Transform[] doors = new Transform[2];
            string[] doorArt = { "SM_A2_GateDoor_L.fbx", "SM_A2_GateDoor_R.fbx" };
            for (int i = 0; i < 2; i++)
            {
                Transform hinge = new GameObject(i == 0 ? "Door_L" : "Door_R").transform;
                hinge.SetParent(gt, false);
                hinge.localPosition = new Vector3(i == 0 ? -4.2f : 4.2f, -0.9f, 4.3f);
                GameObject door = Child(FieldArt + doorArt[i], hinge, Vector3.zero);
                if (door != null) EnsureColliders(door);
                doors[i] = hinge;
            }

            // grilajul: jos in pozitia din editor, se ridica la start
            GameObject grid = Spawn(WorldKit + "SM_Portcullis.prefab", gt, Vector3.zero, 0f);
            if (grid != null)
            {
                grid.name = "Portcullis";
                grid.transform.localPosition = new Vector3(0f, -0.9f, -3.8f);
                grid.transform.localScale = Vector3.Scale(grid.transform.localScale, new Vector3(1.75f, 1.6f, 1f));
                StripColliders(grid);
            }

            GameObject barrier = Barrier(gt, "Barrier", new Vector3(0f, 6f, 3.8f), new Vector3(9f, 12f, 1f));

            Transform[] dust = new Transform[3];
            Vector3[] dustAt = { new Vector3(-3f, 0.2f, 5.5f), new Vector3(3f, 0.2f, 5.5f), new Vector3(0f, 0.2f, 3f) };
            for (int i = 0; i < dust.Length; i++)
            {
                dust[i] = new GameObject("DustPoint_" + i).transform;
                dust[i].SetParent(gt, false);
                dust[i].localPosition = dustAt[i];
            }

            // facle pe turnurile de flanc (afara) si pe fata dinspre curte
            List<LightFlicker> flickers = new List<LightFlicker>();
            foreach (int side in new[] { -1, 1 })
            {
                WallTorch(gateRoot, new Vector3(GateX + side * 5.6f, PlateauY + 3.6f, GateZ - 16f), Vector3.forward, flickers);
                Boost(WallTorch(gateRoot, new Vector3(GateX + side * 5.6f, PlateauY + 3.6f, GateZ + 14f), Vector3.back, flickers), 2f, 8f);
            }

            CastleGate gate = gateGo.AddComponent<CastleGate>();
            SetRefs(gate, "doors", doors);
            SetFloats(gate, "openYaw", new[] { -85f, 85f });
            if (grid != null) SetRef(gate, "portcullis", grid.transform);
            SetFloat(gate, "portcullisLift", 9f);
            SetRefs(gate, "barriers", new Object[] { barrier });
            SetBool(gate, "startClosed", false);
            SetRefs(gate, "dustPoints", dust);
            AssignGateFx(gate);
            SetRefs(gate, "torches", flickers.ToArray());

            gateGo.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            ClearStatic(gateGo);

            // pragul: toata latimea curtii, ca jucatorul sa nu-l poata ocoli
            GameObject trigGo = new GameObject("Arena2_GateTrigger");
            trigGo.transform.SetParent(gateRoot, false);
            trigGo.transform.position = new Vector3(GateX, PlateauY + 2f, 390f);
            CastleGateTrigger trigger = trigGo.AddComponent<CastleGateTrigger>();
            SetRef(trigger, "gate", gate);
            SetVector3(trigger, "size", new Vector3(Courtyard.width - 1f, 6f, 4f));

            // pragurile podelei din gang au ~0.3 m: rampe invizibile ca jucatorul sa nu se agate
            Ramp(gateRoot, "Ramp_GateOuter", new Vector3(GateX, PlateauY, GateZ - 5.7f), new Vector3(GateX, PlateauY + 0.31f, GateZ - 4.4f), 8f);
            Ramp(gateRoot, "Ramp_GateInner", new Vector3(GateX, PlateauY, GateZ + 5.9f), new Vector3(GateX, PlateauY + 0.31f, GateZ + 4.6f), 8f);

            Drawbridge(gateRoot);
            GateBanners(gateRoot);

            // facle pe stalp la capatul drumului, inainte de pod
            foreach (int side in new[] { -1, 1 })
                Prop(Kit + "Torch_Standing.prefab", gateRoot, new Vector3(GateX + side * 4.6f, 0f, 359.6f), R(0f, 360f), 0.05f, 0f, 0f, true);
            Prop(Kit + "Bones_Pile.prefab", gateRoot, new Vector3(GateX - 5.6f, 0f, 358.4f), R(0f, 360f), 0.05f, 0.25f);
            Prop(FieldArt + "SM_A2_Shield_Heater.fbx", gateRoot, new Vector3(GateX + 5.2f, 0f, 357.8f), R(0f, 360f), 0.02f, 0f, 70f);
            return gate;
        }

        static void AssignGateFx(CastleGate gate)
        {
            SetRef(gate, "slamFxPrefab", FxSystem("FX_BurstDust"));
            SetRef(gate, "dustFxPrefab", FxSystem("FX_RumbleDust"));
            List<Object> debris = new List<Object>();
            foreach (string name in new[] { "FX_Debris_A", "FX_Debris_B", "FX_Debris_C" })
            {
                GameObject prefab = Load<GameObject>(Fx + name + ".prefab");
                DebrisChunk chunk = prefab != null ? prefab.GetComponent<DebrisChunk>() : null;
                if (chunk != null) debris.Add(chunk);
            }
            SetRefs(gate, "debrisPrefabs", debris.ToArray());
        }

        static ParticleSystem FxSystem(string name)
        {
            GameObject prefab = Load<GameObject>(Fx + name + ".prefab");
            return prefab != null ? prefab.GetComponent<ParticleSystem>() : null;
        }

        // bariera invizibila + carving pe NavMesh; activa doar cat poarta e inchisa
        static GameObject Barrier(Transform parent, string name, Vector3 localPos, Vector3 size)
        {
            GameObject barrier = new GameObject(name);
            barrier.transform.SetParent(parent, false);
            barrier.transform.localPosition = localPos;
            BoxCollider box = barrier.AddComponent<BoxCollider>();
            box.size = size;
            NavMeshObstacle obstacle = barrier.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = size;
            obstacle.carving = true;
            barrier.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            barrier.SetActive(false);
            return barrier;
        }

        // rampa invizibila: fata de sus trece prin low si high
        static GameObject Ramp(Transform parent, string name, Vector3 low, Vector3 high, float width)
        {
            const float thickness = 0.4f;
            Vector3 along = high - low;
            GameObject ramp = new GameObject(name);
            ramp.transform.SetParent(parent, false);
            Quaternion rot = Quaternion.LookRotation(along.normalized, Vector3.up);
            ramp.transform.SetPositionAndRotation((low + high) * 0.5f - rot * Vector3.up * (thickness * 0.5f), rot);
            BoxCollider box = ramp.AddComponent<BoxCollider>();
            box.size = new Vector3(width, thickness, along.magnitude);
            MarkStatic(ramp);
            return ramp;
        }

        // facla de perete asezata pe suprafata gasita cu o raza (facla iese pe +z)
        static GameObject WallTorch(Transform parent, Vector3 from, Vector3 dir, List<LightFlicker> flickers)
        {
            if (!Surface(from, dir, out Vector3 p, out Vector3 n)) return null;
            n = Flat(n).normalized;
            GameObject torch = Spawn(Kit + "Torch_Wall.prefab", parent, p - Vector3.up * 1.8f + n * 0.05f, Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg);
            if (torch == null) return null;
            StripColliders(torch);
            SetLights(torch, true, false);
            if (flickers != null) flickers.AddRange(torch.GetComponentsInChildren<LightFlicker>(true));
            return torch;
        }

        static void Drawbridge(Transform parent)
        {
            // podul coborat, rupt: o scandura lasata intr-o parte, lanturile smulse atarna din zid
            GameObject bridge = Spawn(FieldArt + "SM_A2_Drawbridge.fbx", parent, new Vector3(GateX, PlateauY - 0.31f, GateZ - 4.6f), 180f);
            if (bridge != null)
            {
                bridge.transform.rotation = Quaternion.AngleAxis(2f, Vector3.forward) * Quaternion.AngleAxis(-0.6f, Vector3.right) * bridge.transform.rotation;
                EnsureColliders(bridge);
                MarkStatic(bridge);
                // capetele podului sunt putin sub teren
                Ramp(parent, "Ramp_BridgeOuter", new Vector3(GateX + 1f, PlateauY - 0.31f, GateZ - 14.2f), new Vector3(GateX + 1f, PlateauY, GateZ - 15.9f), 4.5f);
                Ramp(parent, "Ramp_BridgeInner", new Vector3(GateX, PlateauY - 0.25f, GateZ - 9.2f), new Vector3(GateX, PlateauY + 0.01f, GateZ - 7.6f), 6f);
            }
            foreach (int side in new[] { -1, 1 })
            {
                if (!Surface(new Vector3(GateX + side * 3.4f, PlateauY + 9f, 360f), Vector3.forward, out Vector3 p, out Vector3 n)) continue;
                GameObject chain = Spawn(FieldArt + "SM_A2_Chain.fbx", parent, p + Flat(n).normalized * 0.15f, R(0f, 360f));
                if (chain == null) continue;
                chain.transform.localScale *= side < 0 ? 1.5f : 0.9f;
                StripColliders(chain);
                MarkStatic(chain);
            }
        }

        static void GateBanners(Transform parent)
        {
            // pe turnurile de flanc, spre drum, si pe fata dinspre curte
            foreach (int side in new[] { -1, 1 })
            {
                if (Surface(new Vector3(GateX + side * 7.4f, PlateauY + 13f, 355f), Vector3.forward, out Vector3 p, out Vector3 n))
                    HangBanner(parent, p + Flat(n).normalized * 0.3f, Facing(Flat(n)), 3, 1.7f);
                if (Surface(new Vector3(GateX + side * 5.8f, PlateauY + 9.5f, 395f), Vector3.back, out p, out n))
                    HangBanner(parent, p + Flat(n).normalized * 0.25f, Facing(Flat(n)), 3, 1.3f);
            }
        }

        // panza atarnata de un zid sau de o arcada: segmente inlantuite cu pivot sus, fata pe -z
        static GameObject HangBanner(Transform parent, Vector3 top, float yaw, int segments, float scale)
        {
            GameObject root = new GameObject("Banner_Hanging");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(top, Quaternion.Euler(0f, yaw, 0f));
            root.transform.localScale = Vector3.one * scale;
            Transform[] segs = new Transform[segments];
            Transform holder = root.transform;
            for (int i = 0; i < segments; i++)
            {
                Transform seg = new GameObject("Seg" + i).transform;
                seg.SetParent(holder, false);
                seg.localPosition = i == 0 ? Vector3.zero : new Vector3(0f, -1f, 0f);
                Child(BannerCloth[i], seg, Vector3.zero);
                segs[i] = seg;
                holder = seg;
            }
            StripColliders(root);
            BannerSway sway = root.AddComponent<BannerSway>();
            SetRefs(sway, "segments", segs);
            SetFloat(sway, "angle", R(2.5f, 4f));
            SetFloat(sway, "twist", 1.5f);
            SetFloat(sway, "speed", R(0.35f, 0.55f));
            ClearStatic(root);
            return root;
        }

        // ------------------------------------------------------------------ sala tronului

        static void Hall(Transform root)
        {
            Transform hall = Group(root, "ThroneHall");
            Transform structure = Group(hall, "Structure");

            // nava: doua siruri de stalpi; stalpul de est din fata e rupt, arcada lui a cazut
            foreach (float z in new[] { 404f, 412f, 420f })
                foreach (float x in new[] { 411f, 429f })
                {
                    bool broken = x > 420f && z < 405f;
                    Piece(CastleArt + (broken ? "SM_Hall_PillarBroken.fbx" : "SM_Hall_Pillar.fbx"), structure, new Vector3(x, PlateauY, z), 0f);
                }
            Piece(CastleArt + "SM_Hall_ArchBroken.fbx", structure, new Vector3(420f, ArchY, 404f), 0f, false);
            Piece(CastleArt + "SM_Hall_Arch.fbx", structure, new Vector3(420f, ArchY, 412f), 0f, false);
            Piece(CastleArt + "SM_Hall_Arch.fbx", structure, new Vector3(420f, ArchY, 420f), 0f, false);
            Roof(structure);
            Fallen(Group(hall, "Collapse"));
            Physics.SyncTransforms();

            Dais(hall);
            HallLights(Group(hall, "Lights"));
            Tapestries(Group(hall, "Tapestries"));
            HallDressing(Group(hall, "Dressing"));
        }

        // dalele acoperisului stau pe arcade; lipsesc acolo unde trebuie sa treaca razele spre RoofGaps
        static void Roof(Transform parent)
        {
            Vector3 axis = RayAxis();
            List<Vector2> holes = new List<Vector2>();
            foreach (Vector3 gap in RoofGaps)
            {
                Vector3 h = gap + axis * ((RoofY - PlateauY) / axis.y);
                holes.Add(new Vector2(h.x, h.z));
            }

            Transform roof = Group(parent, "Roof");
            float[] cols = { 414f, 426f };
            float[] rows = { 399.6f, 408.48f, 417.36f };
            foreach (float z0 in rows)
                foreach (float x in cols)
                {
                    Rect slab = Rect.MinMaxRect(x - 5.5f, z0 + 0.5f, x + 5.5f, z0 + 8.4f);
                    bool open = false;
                    foreach (Vector2 h in holes) open |= slab.Contains(h);
                    if (!open)
                    {
                        Piece(FieldArt + "SM_A2_RoofBroken.fbx", roof, new Vector3(x, RoofY, z0), 0f, false);
                        continue;
                    }
                    // ciot de dala atarnat de arcada, cu capatul rupt in jos
                    GameObject stub = Piece(FieldArt + "SM_A2_RoofBroken.fbx", roof, new Vector3(x + (x < 420f ? -3f : 3f), RoofY - 0.2f, z0 + 0.1f), 0f, false);
                    if (stub != null)
                    {
                        stub.transform.localScale = Vector3.Scale(stub.transform.localScale, new Vector3(0.5f, 1f, 0.42f));
                        stub.transform.rotation = Quaternion.AngleAxis(52f, Vector3.right) * stub.transform.rotation;
                    }
                    // o grinda rupta ramasa in gol
                    GameObject beam = Piece(CastleArt + "SM_Hall_Beam.fbx", roof, new Vector3(x + (x < 420f ? 2f : -2f), RoofY - 1.2f, z0 + 4.4f), 90f + R(-8f, 8f), false);
                    if (beam != null) beam.transform.rotation = Quaternion.AngleAxis(R(14f, 22f), Vector3.right) * beam.transform.rotation;
                }
        }

        // ce a cazut din acoperis: o dala sprijinita pe dale, un tambur de coloana, grinzi si moloz
        static void Fallen(Transform parent)
        {
            GameObject slab = Spawn(FieldArt + "SM_A2_RoofBroken.fbx", parent, new Vector3(433f, PlateauY, 399f), 205f);
            if (slab != null)
            {
                slab.transform.localScale *= 0.62f;
                slab.transform.rotation = Quaternion.AngleAxis(-16f, Vector3.forward) * slab.transform.rotation;
                Put(slab, new Vector3(433f, 0f, 399f), 0.45f);
                EnsureColliders(slab);
                MarkStatic(slab);
            }
            GameObject drum = Spawn(CastleArt + "SM_Hall_PillarBroken.fbx", parent, new Vector3(434f, PlateauY, 406f), 0f);
            if (drum != null)
            {
                drum.transform.rotation = Quaternion.Euler(R(-4f, 4f), 72f, 88f) * drum.transform.rotation;
                drum.transform.localScale *= 0.7f;
                Put(drum, new Vector3(434.5f, 0f, 407f), 0.25f);
                EnsureColliders(drum);
                MarkStatic(drum);
            }
            GameObject beam = Spawn(CastleArt + "SM_Hall_Beam.fbx", parent, new Vector3(405.5f, PlateauY, 397f), 35f);
            if (beam != null)
            {
                beam.transform.rotation = Quaternion.AngleAxis(9f, Vector3.forward) * beam.transform.rotation;
                Put(beam, new Vector3(405.5f, 0f, 397f), 0.2f);
                EnsureColliders(beam);
                MarkStatic(beam);
            }
            Vector3[] heaps = { new Vector3(430.5f, 0f, 396f), new Vector3(436f, 0f, 402f), new Vector3(431f, 0f, 404.5f), new Vector3(404f, 0f, 395f), new Vector3(425.5f, 0f, 411.5f) };
            foreach (Vector3 h in heaps)
                Prop(Kit + "Rubble_Pile.prefab", parent, h, R(0f, 360f), 0.1f, R(0.6f, 1.1f), 0f, true);
            for (int i = 0; i < 18; i++)
            {
                Vector3 p = heaps[i % heaps.Length] + new Vector3(R(-3f, 3f), 0f, R(-3f, 3f));
                Prop(Pick(Rocks), parent, p, R(0f, 360f), 0.05f, R(0.2f, 0.55f));
            }
        }

        static void Dais(Transform hall)
        {
            Transform dais = Group(hall, "Dais");
            Piece(CastleArt + "SM_Hall_Dais.fbx", dais, new Vector3(420f, PlateauY, 424f), 0f);
            Physics.SyncTransforms();
            // lumanari pe trepte si ramasitele celor care au jurat aici
            foreach (int side in new[] { -1, 1 })
            {
                for (int i = 0; i < 2; i++)
                {
                    float x = 420f + side * R(2.2f, 5.2f), z = R(420.2f, 421.2f);
                    GameObject candles = Spawn(Kit + "Candles.prefab", dais, new Vector3(x, FloorAt(x, z, PlateauY + 4f), z), R(0f, 360f));
                    if (candles != null) { StripColliders(candles); SetLights(candles, true, false); }
                }
            }
            Prop(Kit + "Bones_Pile.prefab", dais, new Vector3(414.6f, 0f, 418.2f), R(0f, 360f), 0.05f, 0.25f);
            Prop(FieldArt + "SM_A2_Helmet_Great.fbx", dais, new Vector3(425.6f, 0f, 418.4f), R(0f, 360f), 0.03f, 0f, 25f);
            Prop(FieldArt + "SM_A2_Shield_Heater.fbx", dais, new Vector3(424.4f, 0f, 417.8f), R(0f, 360f), 0.02f, 0f, 78f);
        }

        static void HallLights(Transform parent)
        {
            // jaratic in brazierele de pe soclurile podiumului, mai puternic: lumina calda a tronului
            foreach (int side in new[] { -1, 1 })
            {
                float x = 420f + side * 4.2f, z = 421.4f;
                GameObject brazier = Spawn(Kit + "Brazier.prefab", parent, new Vector3(x, FloorAt(x, z, PlateauY + 5f), z), R(0f, 360f));
                if (brazier == null) continue;
                EnsureColliders(brazier);
                SetLights(brazier, true, false);
                foreach (Light light in brazier.GetComponentsInChildren<Light>(true))
                {
                    light.intensity *= 2f;
                    light.range = 13f;
                }
            }
            Vector3[] nave = { new Vector3(415.5f, 0f, 391.5f), new Vector3(424.5f, 0f, 391.5f), new Vector3(414.2f, 0f, 415.5f), new Vector3(425.8f, 0f, 415.5f) };
            // prefab-ul e gandit pentru spatii mici; curtea e larga, deci focul bate mai departe
            foreach (Vector3 p in nave)
                Boost(Prop(Kit + "Brazier.prefab", parent, p, R(0f, 360f), 0.05f, 0f, 0f, true), 3f, 13f);

            // facle pe zidurile curtii
            foreach (float z in new[] { 390f, 409.6f, 421.6f })
            {
                WallTorch(parent, new Vector3(408f, PlateauY + 3.4f, z), Vector3.left, null);
                WallTorch(parent, new Vector3(432f, PlateauY + 3.4f, z), Vector3.right, null);
            }
            foreach (float x in new[] { 407f, 433f })
                WallTorch(parent, new Vector3(x, PlateauY + 3.4f, 424f), Vector3.forward, null);

            // lumina rece din golul de deasupra tronului (cheia scenei) si o umplere slaba prin al doilea gol
            Vector3 axis = RayAxis();
            for (int i = 0; i < RoofGaps.Length; i++)
            {
                GameObject go = new GameObject(i == 0 ? "KeyLight_Cold" : "FillLight_Cold");
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(RoofGaps[i] + axis * 26f, Quaternion.LookRotation(-axis));
                Light light = go.AddComponent<Light>();
                light.type = LightType.Spot;
                light.color = ColdKey;
                light.range = 42f;
                light.spotAngle = i == 0 ? 34f : 40f;
                light.innerSpotAngle = i == 0 ? 14f : 18f;
                light.intensity = i == 0 ? 130f : 70f;
                light.shadows = i == 0 ? LightShadows.Soft : LightShadows.None;
                if (i == 0) shadowLights.Add(light);
            }

            // lumina de luna peste zidul de vest pe fata interioara a portii, ca poarta inchisa sa se citeasca
            ColdFill(parent, "GateFill_Cold", new Vector3(GateX - 6f, PlateauY + 15f, GateZ + 22f), new Vector3(GateX, PlateauY + 4f, GateZ + 4f), 70f, 40f, 220f);
            // canaturile stau in umbra arcadei: o pata rece de sus ca scandurile si benzile de fier sa se vada
            ColdFill(parent, "DoorFill_Cold", new Vector3(GateX, PlateauY + 6.5f, GateZ + 15f), new Vector3(GateX, PlateauY + 3f, GateZ + 3.5f), 44f, 22f, 190f);
            // raza de luna prin acoperisul spart, direct pe cavalerul de pe tron
            ColdFill(parent, "ThroneFill_Cold", new Vector3(420f, PlateauY + 16f, ThroneZ - 7.6f), new Vector3(420f, PlateauY + 1.8f, ThroneZ), 26f, 26f, 260f);
            ColdFill(parent, "CourtFill_Cold",new Vector3(GateX + 4f, PlateauY + 18f, GateZ + 6f), new Vector3(GateX - 2f, PlateauY, GateZ + 24f), 80f, 45f, 150f);
            HallReadability(parent);
        }

        static void Boost(GameObject go, float intensity, float range)
        {
            if (go == null) return;
            foreach (Light light in go.GetComponentsInChildren<Light>(true))
            {
                light.intensity *= intensity;
                light.range = Mathf.Max(light.range, range);
            }
        }

        static void ColdFill(Transform parent, string name, Vector3 from, Vector3 at, float angle, float range, float intensity)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(from, Quaternion.LookRotation(at - from));
            Light light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = ColdKey;
            light.range = range;
            light.spotAngle = angle;
            light.innerSpotAngle = angle * 0.45f;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        static void Tapestries(Transform parent)
        {
            // pe zidurile pline: vest, est si o tapiserie mare in spatele tronului
            Hang(parent, new Vector3(408f, PlateauY + 9f, 384f), Vector3.left, 1f);
            Hang(parent, new Vector3(408f, PlateauY + 9f, 415.5f), Vector3.left, 1f);
            Hang(parent, new Vector3(432f, PlateauY + 9f, 394.5f), Vector3.right, 1f);
            Hang(parent, new Vector3(432f, PlateauY + 9f, 426f), Vector3.right, 1f);
            Hang(parent, new Vector3(420f, PlateauY + 10.5f, 427.5f), Vector3.forward, 1.35f);

            // stindarde rosii atarnate de arcadele navei
            foreach (float z in new[] { 412f, 420f })
                foreach (float x in new[] { 415f, 425f })
                    if (Surface(new Vector3(x, PlateauY + 6f, z), Vector3.up, out Vector3 p, out Vector3 n, 20f))
                        HangBanner(parent, p - Vector3.up * 0.1f, 0f, 3, 1.7f);
        }

        static void Hang(Transform parent, Vector3 from, Vector3 dir, float scale)
        {
            if (!Surface(from, dir, out Vector3 p, out Vector3 n)) return;
            n = Flat(n).normalized;
            GameObject tapestry = Spawn(FieldArt + "SM_A2_Tapestry.fbx", parent, p + n * 0.12f, Facing(n));
            if (tapestry == null) return;
            tapestry.transform.localScale *= scale;
            StripColliders(tapestry);
            MarkStatic(tapestry);
        }

        static void HallDressing(Transform parent)
        {
            // juramintele rupte si in sala: scuturi, coifuri si oase pe langa ziduri, lanturi in arcade
            for (int i = 0; i < 16; i++)
            {
                bool west = i % 2 == 0;
                Vector3 p = new Vector3(west ? R(402.5f, 407f) : R(433f, 437.5f), 0f, R(382f, 428f));
                if (Mathf.Abs(p.z - PosternZ) < 3.5f && !west) continue;
                switch (i % 4)
                {
                    case 0: Prop(FieldArt + "SM_A2_Shield_Heater.fbx", parent, p, R(0f, 360f), 0.02f, 0f, R(60f, 85f)); break;
                    case 1: Prop(Pick(Helmets), parent, p, R(0f, 360f), 0.03f, 0f, R(0f, 40f)); break;
                    case 2: Prop(Kit + "Bones_Pile.prefab", parent, p, R(0f, 360f), 0.05f, R(0.2f, 0.3f)); break;
                    default: Prop(Pick(Pikes), parent, p, R(0f, 360f), 0.02f, 0f, R(82f, 88f)); break;
                }
            }
            foreach (Vector3 p in new[] { new Vector3(413.5f, 0f, 400.5f), new Vector3(427f, 0f, 395f), new Vector3(417.5f, 0f, 413.5f) })
                Prop(FieldArt + "SM_A2_Shield_Heater.fbx", parent, p, R(0f, 360f), 0.02f, 0f, R(70f, 85f));

            foreach (float z in new[] { 412f, 420f })
                foreach (float x in new[] { 411.8f, 428.2f })
                {
                    if (!Surface(new Vector3(x + (x < 420f ? 1.6f : -1.6f), PlateauY + 8f, z), Vector3.up, out Vector3 p, out Vector3 n, 20f)) continue;
                    GameObject chain = Spawn(FieldArt + "SM_A2_Chain.fbx", parent, p, R(0f, 360f));
                    if (chain == null) continue;
                    chain.transform.localScale *= R(1.2f, 1.8f);
                    StripColliders(chain);
                    MarkStatic(chain);
                }
        }

        // ------------------------------------------------------------------ tronul si cavalerul

        static void ThroneAndKnight()
        {
            float y = FloorAt(420f, ThroneZ + 0.6f, PlateauY + 6f);
            Vector3 seat = new Vector3(420f, y, ThroneZ);

            GameObject throne = Spawn(Arena2ArtSetup.ThronePath, null, seat, 0f);
            if (throne != null)
            {
                throne.name = ThroneRoot;
                EnsureColliders(throne);
                MarkStatic(throne);
            }

            GameObject knight = Spawn(Arena2ArtSetup.KnightPath, null, seat, 180f);
            if (knight == null) return;
            knight.name = KnightRoot;
            Animator animator = knight.GetComponent<Animator>();
            if (animator == null) animator = knight.AddComponent<Animator>();
            animator.runtimeAnimatorController = Load<RuntimeAnimatorController>(Arena2ArtSetup.ControllerPath);
            animator.applyRootMotion = false;
            Attach(knight.transform, "Socket_Lance_R", Arena2ArtSetup.LancePath);
            Attach(knight.transform, "Socket_Shield_L", Arena2ArtSetup.ShieldPath);
            StripColliders(knight);
            ClearStatic(knight);
        }

        static void Attach(Transform rig, string socketName, string path)
        {
            Transform socket = FindDeep(rig, socketName);
            if (socket == null) { Debug.LogError($"[Arena2Builder] Socket-ul {socketName} lipseste de pe cavaler."); return; }
            GameObject prefab = Load<GameObject>(path);
            if (prefab == null) return;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, socket);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
        }

        static Transform FindDeep(Transform parent, string name)
        {
            foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        // ------------------------------------------------------------------ poterna din spate si mlastina

        static void Postern(Transform root)
        {
            Transform post = Group(root, "Postern");
            Piece(CastleArt + "SM_Castle_WallPostern.fbx", post, new Vector3(WallE, PieceY, PosternZ), 270f);

            GameObject gateGo = new GameObject("PosternGate");
            gateGo.transform.SetParent(post, false);
            gateGo.transform.SetPositionAndRotation(new Vector3(WallE, PlateauY, PosternZ), Quaternion.Euler(0f, 270f, 0f));
            Transform gt = gateGo.transform;

            Transform[] doors = new Transform[2];
            string[] doorArt = { "SM_A2_PosternDoor_L.fbx", "SM_A2_PosternDoor_R.fbx" };
            for (int i = 0; i < 2; i++)
            {
                Transform hinge = new GameObject(i == 0 ? "Door_L" : "Door_R").transform;
                hinge.SetParent(gt, false);
                hinge.localPosition = new Vector3(i == 0 ? -2.3f : 2.3f, -0.9f, -0.9f);
                GameObject door = Child(FieldArt + doorArt[i], hinge, Vector3.zero);
                if (door != null) EnsureColliders(door);
                doors[i] = hinge;
            }
            GameObject barrier = Barrier(gt, "Barrier", new Vector3(0f, 3.5f, -0.9f), new Vector3(5f, 7f, 1f));
            Transform dust = new GameObject("DustPoint").transform;
            dust.SetParent(gt, false);
            dust.localPosition = new Vector3(0f, 0.2f, -2.5f);
            dust.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // ramane inchisa: Drumul 3 o deschide cu Open()
            CastleGate gate = gateGo.AddComponent<CastleGate>();
            SetRefs(gate, "doors", doors);
            SetFloats(gate, "openYaw", new[] { 85f, -85f });
            SetFloat(gate, "portcullisLift", 0f);
            SetRefs(gate, "barriers", new Object[] { barrier });
            SetBool(gate, "startClosed", true);
            SetRefs(gate, "dustPoints", new[] { dust });
            AssignGateFx(gate);
            SetInt(gate, "debrisCount", 3);
            SetRefs(gate, "torches", new Object[0]);
            gateGo.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            ClearStatic(gateGo);

            // oase in fata usii, pe partea de mlastina
            Prop(Kit + "Bones_Pile.prefab", post, new Vector3(WallE + 4f, 0f, PosternZ - 2.6f), R(0f, 360f), 0.05f, 0.22f);

            // facle pe fata dinspre mlastina si luna rece pe zid, ca iesirea sa se citeasca din vale
            Physics.SyncTransforms();
            foreach (int side in new[] { -1, 1 })
                Boost(WallTorch(post, new Vector3(WallE + 6f, PlateauY + 3.4f, PosternZ + side * 3.4f), Vector3.left, null), 1.6f, 9f);
            ColdFill(post, "PosternFill_Cold", new Vector3(WallE + 22f, PlateauY + 12f, PosternZ - 8f), new Vector3(WallE, PlateauY + 2.5f, PosternZ), 38f, 40f, 200f);
        }

        static void Swamp(Transform parent)
        {
            // poteca coboara ~20 m de la poterna spre mlastina; pe margini copaci morti, cioturi, bolovani
            for (int i = 0; i < 9; i++)
            {
                float x = R(446f, 474f);
                float z = PosternZ + Side() * R(4f, 12f);
                GameObject tree = Prop(Kit + "Tree_Dead.prefab", parent, new Vector3(x, 0f, z), R(0f, 360f), 0.3f, R(5f, 9f), R(0f, 12f), true);
                if (tree != null && i % 3 == 0) Swap(tree, "M_Wood075", charred);
            }
            for (int i = 0; i < 8; i++)
                Prop(Kit + "Stump_Roots.prefab", parent, new Vector3(R(445f, 476f), 0f, PosternZ + Side() * R(3.2f, 13f)), R(0f, 360f), 0.15f, R(0.6f, 1.1f));
            for (int i = 0; i < 4; i++)
                Prop(Kit + "Log_Fallen.prefab", parent, new Vector3(R(450f, 476f), 0f, PosternZ + Side() * R(3.5f, 10f)), R(0f, 360f), 0.2f);
            for (int i = 0; i < 7; i++)
                Prop(Pick(Boulders), parent, new Vector3(R(444f, 470f), 0f, PosternZ + Side() * R(3.4f, 9f)), R(0f, 360f), 0.4f, R(0.8f, 2f), 0f, true);
            for (int i = 0; i < 14; i++)
                Prop(Pick(Rocks), parent, new Vector3(R(444f, 476f), 0f, PosternZ + Side() * R(2.8f, 11f)), R(0f, 360f), 0.05f, R(0.2f, 0.6f));
            CrowFlockAt(parent, new Vector3(458f, 0f, PosternZ + 6f), 5);
        }

        // ziduri invizibile pe linia zidurilor: jucatorul ramane in curte si dupa ce poarta s-a inchis
        static void Containment(Transform parent)
        {
            Wall(parent, "Contain_W", new Vector3(WallW + 0.5f, 0f, 405f), new Vector3(2f, 30f, 56f));
            Wall(parent, "Contain_E_South", new Vector3(WallE - 0.5f, 0f, 395f), new Vector3(2f, 30f, 36f));
            Wall(parent, "Contain_E_North", new Vector3(WallE - 0.5f, 0f, 425.5f), new Vector3(2f, 30f, 15f));
            Wall(parent, "Contain_N", new Vector3(420f, 0f, WallN - 0.5f), new Vector3(46f, 30f, 2f));
            Wall(parent, "Contain_S_West", new Vector3(402.5f, 0f, GateZ + 0.5f), new Vector3(13f, 30f, 2f));
            Wall(parent, "Contain_S_East", new Vector3(437.5f, 0f, GateZ + 0.5f), new Vector3(13f, 30f, 2f));
        }

        static void Wall(Transform parent, string name, Vector3 p, Vector3 size)
        {
            GameObject go = new GameObject(name);
            go.layer = IgnoreRaycastLayer;
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(p.x, PlateauY - 1f + size.y * 0.5f, p.z);
            go.AddComponent<BoxCollider>().size = size;
            go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
        }

        // piesa de arhitectura asezata exact (fara cautarea solului), cu collider din mesh
        static GameObject Piece(string path, Transform parent, Vector3 p, float yaw, bool collider = true)
        {
            GameObject go = Spawn(path, parent, p, yaw);
            if (go == null) return null;
            if (collider) EnsureColliders(go);
            else StripColliders(go);
            MarkStatic(go);
            return go;
        }

        // ------------------------------------------------------------------ NavMesh

        static int BakeNavMesh()
        {
            GameObject castle = GameObject.Find(CastleRoot);
            if (castle == null) return 0;
            GameObject nav = new GameObject("Arena2_NavMesh");
            nav.transform.SetParent(castle.transform, false);
            nav.transform.position = HallCenter;

            Physics.SyncTransforms();
            string agentName = NavMesh.GetSettingsNameFromID(0);
            NavMeshSurface surface = nav.AddComponent<NavMeshSurface>();
            surface.agentTypeID = 0;
            surface.collectObjects = CollectObjects.Volume;
            surface.size = new Vector3(46f, 16f, 64f);   // curtea + pasajul portii
            surface.center = new Vector3(0f, 4f, -4f);
            surface.layerMask = ~((1 << 6) | (1 << 7) | (1 << 8));
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            if (surface.navMeshData == null) { Debug.LogError($"[Arena2Builder] Bake NavMesh esuat pentru {agentName}."); return 0; }

            string path = $"{Folder}/NavMesh/Arena2_NavMesh_{agentName}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surface.navMeshData, path);
            return 1;
        }
    }
}
