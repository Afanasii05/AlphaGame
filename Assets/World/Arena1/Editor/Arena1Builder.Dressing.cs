using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Alpha.World.EditorTools
{
    // Decorul de detaliu: scene mici de-a lungul drumului, detaliile arenei, runele sapate in piatra si steagurile cu ghicitoarea.
    // Nimic nu se pune pe drum; colliderele raman doar pe obiectele mari.
    public static partial class Arena1Builder
    {
        const string WoodMatPath = "Assets/RogueKit/Materials/M_Wood075.mat";
        const string SpearPath = "Assets/Prefabs/Spear/Spear.fbx";
        const string ShieldPath = "Assets/Prefabs/Shield/Shield.prefab";
        const string SwordPath = "Assets/Weapons/Sword_Unity/Assets/Sword/Sword_Prefab.prefab";
        const string RuneMeshPath = Folder + "/Art/Atmos/Mesh_RiddleRunes.asset";
        const string BannerHint = "Old words hang upon the banners...";
        const float FightRadius = 14f;
        static readonly string[] BannerNumerals = { "I", "II", "III", "IV" };

        static Arena1ArtBuilder.Result art;
        static Material woodMat;
        static Mesh swordMesh, spearMesh;
        static Material[] swordMats, spearMats, shieldMats;
        static readonly List<Bounds> occupied = new List<Bounds>();

        static void PrepareDressing()
        {
            art = Arena1ArtBuilder.Build();
            shadowLights.Clear();
            woodMat = Load<Material>(WoodMatPath);

            GameObject spear = Load<GameObject>(SpearPath);
            MeshFilter spearFilter = spear != null ? spear.GetComponentInChildren<MeshFilter>() : null;
            spearMesh = spearFilter != null ? spearFilter.sharedMesh : null;
            spearMats = spear != null ? Rust(spear.GetComponentInChildren<MeshRenderer>().sharedMaterials) : null;

            GameObject sword = Load<GameObject>(SwordPath);
            swordMesh = null;
            if (sword != null)
                foreach (MeshFilter mf in sword.GetComponentsInChildren<MeshFilter>(true))
                    if (mf.name == "Sword")
                    {
                        swordMesh = mf.sharedMesh;
                        swordMats = Rust(mf.GetComponent<MeshRenderer>().sharedMaterials);
                    }
            // runa stralucitoare a sabiei devine metal ruginit ca garda
            if (swordMats != null)
                for (int i = 0; i < swordMats.Length; i++)
                    if (swordMats[i] != null && swordMats[i].name.Contains("Rune_Glow")) swordMats[i] = swordMats[0];

            GameObject shield = Load<GameObject>(ShieldPath);
            shieldMats = null;
            if (shield != null)
            {
                Material[] source = shield.GetComponentInChildren<MeshRenderer>().sharedMaterials;
                shieldMats = new Material[source.Length];
                for (int i = 0; i < source.Length; i++) shieldMats[i] = Arena1ArtBuilder.Weathered(source[i]);
            }
        }

        static Material[] Rust(Material[] source)
        {
            Material[] result = new Material[source.Length];
            for (int i = 0; i < source.Length; i++)
                result[i] = source[i] != null && source[i].name.Contains("Rune_Glow") ? source[i] : Arena1ArtBuilder.Rusted(source[i]);
            return result;
        }

        // ------------------------------------------------------------------ drum

        static void BuildRoadVignettes(Transform road)
        {
            CollectOccupied(road);
            float sStart = 10f, s1 = ctrlS[5], s2 = ctrlS[9], sEnd = ctrlS[ctrlS.Length - 1];
            float sApproach = sEnd - 9f;

            // 1. Pilgrim Way: caruta parasita, gard vechi, borne, torte stinse, coloana prabusita, flori la morminte
            Transform pilgrim = Group(road, "01_PilgrimWay_Details");
            float sCart = Mathf.Lerp(sStart, s1, 0.53f);
            BuildCart(pilgrim, sCart, 1);
            FenceLine(pilgrim, Mathf.Lerp(sStart, s1, 0.08f), Mathf.Lerp(sStart, s1, 0.3f), -1, 0.7f);
            FenceLine(pilgrim, Mathf.Lerp(sStart, s1, 0.68f), Mathf.Lerp(sStart, s1, 0.8f), 1, 0.9f);
            for (int i = 0; i < 3; i++)
            {
                float s = Mathf.Lerp(sStart + 12f, s1 - 6f, (i + 0.5f) / 3f);
                BurntTorch(pilgrim, s, i % 2 == 0 ? -1 : 1, i == 1);
            }
            float sFallen = Mathf.Lerp(sStart, s1, 0.74f);
            FallenPillar(pilgrim, sFallen, -1, 3.2f);
            float sBroken = sFallen + 4f;
            Prop(Kit + "Pillar_Broken.prefab", pilgrim, FlatEdge(ref sBroken, -1, 4.5f, 1.2f), R(0f, 360f), 0.15f, 0f, true);
            float sCorner = Mathf.Lerp(sStart, s1, 0.92f);
            Vector3 corner = FlatEdge(ref sCorner, 1, 5f, 2.5f);
            Prop(Kit + "Wall_Ruin_Corner.prefab", pilgrim, corner, FaceRoadYaw(sCorner, 1) + 30f, 0.2f, 0f, true);
            float sGrave = Mathf.Lerp(sStart, s1, 0.82f);
            Flowers(pilgrim, Edge(sGrave, -1, 4.8f), 2.2f, 7);
            Flowers(pilgrim, Edge(Mathf.Lerp(sStart, s1, 0.35f), -1, 2f), 1.4f, 4);
            for (int i = 0; i < 4; i++)
            {
                float s = R(sStart + 5f, s1 - 5f);
                GameObject shroom = Prop(Kit + "Mushrooms_Glow.prefab", pilgrim, Edge(s, Side(), R(2f, 6f)), R(0f, 360f), 0.02f, 0f, false);
                SetLights(shroom, i == 0, false);
            }

            // 2. Defileul: scheletul unui calator cu sabia si scutul, o torta cazuta, pietris
            Transform gorge = Group(road, "02_Gorge_Details");
            float sFallenPilgrim = Mathf.Lerp(s1, s2, 0.38f);
            // peretele defileului urca aproape vertical chiar de la margine: fiecare obiect sta pe fundul defileului,
            // la piciorul peretelui, insirate de-a lungul drumului
            Vector3 bones = FloorEdge(sFallenPilgrim, 1, 0.7f);
            Prop(Kit + "Bones_Pile.prefab", gorge, bones, R(0f, 360f), 0.05f, 0f, false);
            StuckWeapon(gorge, FloorEdge(sFallenPilgrim + 1.1f, 1, 0.35f), R(0f, 360f), R(8f, 18f), false);
            LyingShield(gorge, FloorEdge(sFallenPilgrim - 1.1f, 1, 0.65f), R(0f, 360f));
            // lumanari lasate de cei care au trecut: singurul punct cald din defileu
            Candles(gorge, FloorEdge(sFallenPilgrim - 2.2f, 1, 0.3f), true);
            BurntTorch(gorge, Mathf.Lerp(s1, s2, 0.7f), -1, true);
            BurntTorch(gorge, Mathf.Lerp(s1, s2, 0.15f), 1, false);

            // 3. Poarta veche -> arena: borne, gard, coloana cazuta, campul celor care au incercat inainte
            Transform gate = Group(road, "03_OldGate_Details");
            float sBattle = Mathf.Lerp(s2, sEnd, 0.55f);
            FallenPillar(gate, Mathf.Lerp(s2, sEnd, 0.18f), -1, 2.2f);
            FenceLine(gate, s2 + 1f, s2 + 7f, 1, 1.4f);
            for (int i = 0; i < 3; i++)
            {
                float s = R(sBattle - 4f, sApproach);
                int side = i % 2 == 0 ? 1 : -1;
                Prop(Kit + "Bones_Pile.prefab", gate, Edge(s, side, R(0.4f, 2f)), R(0f, 360f), 0.05f, 0f, false);
            }
            for (int i = 0; i < 5; i++)
            {
                float s = Mathf.Lerp(sBattle - 6f, sApproach, i / 4f) + R(-1f, 1f);
                StuckWeapon(gate, Edge(s, i % 2 == 0 ? -1 : 1, R(0.5f, 2.5f)), R(0f, 360f), R(6f, 28f), i % 3 != 0);
            }
            for (int i = 0; i < 2; i++)
            {
                float s = R(sBattle - 4f, sApproach);
                LyingShield(gate, Edge(s, i == 0 ? 1 : -1, R(0.6f, 2f)), R(0f, 360f));
            }
            BurntTorch(gate, sBattle + 2f, -1, true);

            // borne de-a lungul drumului, una la ~40 m, pe partea opusa fata de torte
            int k = 0;
            for (float s = sStart + 18f; s < sApproach - 10f; s += 40f, k++)
                Milestone(k < 2 ? pilgrim : k < 4 ? gorge : gate, s, k % 2 == 0 ? -1 : 1);

            // margini: pietris, moloz, iarba si flori
            EdgeDressing(pilgrim, sStart, s1, 0.55f, 0.6f);
            EdgeDressing(gorge, s1, s2, 0.35f, 0.2f);
            EdgeDressing(gate, s2, sApproach, 0.5f, 0.45f);
        }

        // pietricele, smocuri de iarba si cateva flori, mereu in afara benzii drumului
        static void EdgeDressing(Transform parent, float from, float to, float grassChance, float flowerChance)
        {
            Transform edges = Group(parent, "Edges");
            for (float s = from + 1f; s < to - 1f; s += 3.2f)
                for (int side = -1; side <= 1; side += 2)
                {
                    if (rng.NextDouble() < 0.55)
                    {
                        int count = rng.Next(1, 4);
                        for (int i = 0; i < count; i++)
                            Prop(Pick(Rocks), edges, Edge(s + R(-1f, 1f), side, R(-0.1f, 0.7f)), R(0f, 360f), 0.04f, R(0.1f, 0.32f), false, false);
                    }
                    if (rng.NextDouble() < grassChance) GrassTufts(edges, Edge(s + R(-1.2f, 1.2f), side, R(0.3f, 1.6f)), 0.7f, rng.Next(2, 5));
                    if (rng.NextDouble() < flowerChance * 0.35f) Flowers(edges, Edge(s + R(-1.2f, 1.2f), side, R(0.6f, 2.4f)), 0.8f, rng.Next(2, 4));
                }
            for (float s = from + 6f; s < to - 4f; s += R(18f, 28f))
                Prop(Kit + "Rubble_Pile.prefab", edges, Edge(s, Side(), R(0.2f, 0.9f)), R(0f, 360f), 0.08f, 0f, false);
        }

        static void Milestone(Transform parent, float s, int side)
        {
            Vector3 p = Edge(s, side, 0.6f);
            if (!Free(p, 0.5f)) p = Edge(s, side, 1.6f);
            // pe peretii abrupti borna coboara la nivelul drumului
            if (Steep(p, s)) p = FloorEdge(s, side, 0.3f);
            bool pillar = rng.Next(2) == 0;
            GameObject stone = Prop(pillar ? Kit + "Pillar.prefab" : Kit + "Gravestone.prefab", parent, p, FaceRoadYaw(s, side), 0.12f, pillar ? 1.15f : 0.95f, false, true, R(5f, 11f));
            if (stone != null) stone.name = "Milestone";
        }

        static void FallenPillar(Transform parent, float s, int side, float extra)
        {
            Vector3 p = FlatEdge(ref s, side, extra, 2.5f);
            GameObject pillar = Spawn(Kit + "Pillar.prefab", parent, p, 0f);
            if (pillar == null) return;
            // culcata aproape paralel cu drumul, putin rotita
            pillar.transform.rotation = Quaternion.Euler(0f, RoadYaw(s) + R(-20f, 20f), 0f) * Quaternion.Euler(0f, 0f, 86f + R(-3f, 3f));
            Put(pillar, p, 0.25f);
            if (!OffRoad(pillar, 0.2f)) Put(pillar, p + RoadRight(s) * side * 1.2f, 0.25f);
            Track(pillar);
            Prop(Kit + "Rubble_Pile.prefab", parent, p + RoadForward(s) * R(1.8f, 2.4f), R(0f, 360f), 0.08f, 0f, false);
        }

        static void BurntTorch(Transform parent, float s, int side, bool fallen)
        {
            Vector3 p = Edge(s, side, fallen ? 1.6f : 0.7f);
            // langa un perete abrupt nu are loc sa zaca: sta rezemata la piciorul lui
            if (Steep(p, s))
            {
                p = FloorEdge(s, side, 0.3f);
                fallen = false;
            }
            GameObject torch = Spawn(Kit + "Torch_Standing.prefab", parent, p, R(0f, 360f));
            if (torch == null) return;
            torch.name = "Torch_BurntOut";
            float lean = fallen ? R(78f, 86f) : R(8f, 22f);
            torch.transform.rotation = Quaternion.AngleAxis(lean, Quaternion.Euler(0f, R(0f, 360f), 0f) * Vector3.right) * torch.transform.rotation;
            Put(torch, p, fallen ? 0.05f : 0.2f);
            SetLights(torch, false, false);
            foreach (ParticleSystem ps in torch.GetComponentsInChildren<ParticleSystem>(true))
            {
                var emission = ps.emission;
                emission.enabled = false;
                ps.GetComponent<ParticleSystemRenderer>().enabled = false;
            }
            foreach (MeshRenderer r in torch.GetComponentsInChildren<MeshRenderer>(true))
            {
                // flacara si jarul dispar, ramane lemnul ars
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && (mats[i].name.Contains("Flame") || mats[i].name.Contains("Ember"))) mats[i] = woodMat;
                r.sharedMaterials = mats;
            }
            if (fallen) StripColliders(torch);
            Track(torch);
        }

        // caruta din bucati de lemn: o roata rupta, platforma lasata pe o parte, oistea in pamant
        static void BuildCart(Transform parent, float s, int side)
        {
            Vector3 p = FlatEdge(ref s, side, 1.9f, 2f, 12f, 4f);
            GameObject cart = new GameObject("Cart_Abandoned");
            cart.transform.SetParent(parent, false);
            // partea lasata (-right) e mereu spre exterior, marfa nu se varsa pe panta spre drum
            cart.transform.SetPositionAndRotation(new Vector3(p.x, Ground(p.x, p.z), p.z), Quaternion.Euler(0f, RoadYaw(s) + side * 18f + (side > 0 ? 180f : 0f), 0f));

            Transform bed = new GameObject("Bed").transform;
            bed.SetParent(cart.transform, false);
            bed.localPosition = new Vector3(0f, 0.5f, 0f);
            bed.localRotation = Quaternion.Euler(-4f, 0f, 15f);
            for (int i = 0; i < 6; i++)
            {
                if (i == 4) continue;   // scandura lipsa
                float length = i == 1 ? 1.7f : 2.4f;
                Box(bed, "Plank", new Vector3(-0.6f + i * 0.24f, 0f, (2.4f - length) * 0.5f), new Vector3(0.22f, 0.05f, length), Quaternion.Euler(0f, R(-1.5f, 1.5f), 0f));
            }
            for (int x = -1; x <= 1; x += 2)
            {
                Box(bed, "Rail", new Vector3(x * 0.68f, 0.18f, 0f), new Vector3(0.06f, 0.28f, 2.4f), Quaternion.identity);
                Box(bed, "Beam", new Vector3(x * 0.4f, -0.08f, 0f), new Vector3(0.1f, 0.1f, 2.5f), Quaternion.identity);
            }
            Box(bed, "Board_Back", new Vector3(0f, 0.18f, -1.18f), new Vector3(1.36f, 0.28f, 0.05f), Quaternion.identity);
            Box(bed, "Board_Front", new Vector3(0.25f, 0.08f, 1.18f), new Vector3(0.8f, 0.2f, 0.05f), Quaternion.Euler(0f, 0f, -12f));

            // roata intreaga pe dreapta, axul, oistea care atinge pamantul
            Cylinder(bed, "Axle", new Vector3(0f, -0.14f, 0.1f), new Vector3(0.08f, 0.8f, 0.08f), Quaternion.Euler(0f, 0f, 90f));
            Cylinder(bed, "Wheel", new Vector3(0.82f, -0.12f, 0.1f), new Vector3(0.95f, 0.04f, 0.95f), Quaternion.Euler(0f, 0f, 90f));
            Cylinder(bed, "Hub", new Vector3(0.86f, -0.12f, 0.1f), new Vector3(0.18f, 0.08f, 0.18f), Quaternion.Euler(0f, 0f, 90f));
            for (int x = -1; x <= 1; x += 2)
                Box(bed, "Shaft", new Vector3(x * 0.32f, -0.2f, 2.05f), new Vector3(0.07f, 0.07f, 1.9f), Quaternion.Euler(14f, -x * 4f, 0f));

            // roata rupta culcata langa caruta, cu doua spite rupte alaturi
            Transform loose = new GameObject("Wheel_Broken").transform;
            loose.SetParent(cart.transform, false);
            loose.localPosition = new Vector3(-1.45f, 0.04f, -0.5f);
            loose.localRotation = Quaternion.Euler(4f, R(0f, 360f), 2f);
            Cylinder(loose, "Rim", Vector3.zero, new Vector3(0.95f, 0.035f, 0.95f), Quaternion.identity);
            Box(loose, "Spoke", new Vector3(0.75f, 0.01f, 0.3f), new Vector3(0.05f, 0.04f, 0.5f), Quaternion.Euler(0f, 40f, 0f));
            Box(loose, "Spoke", new Vector3(0.55f, 0.01f, -0.55f), new Vector3(0.05f, 0.04f, 0.45f), Quaternion.Euler(0f, -25f, 0f));

            BoxCollider col = cart.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.55f, 0.6f);
            col.size = new Vector3(1.6f, 1.1f, 3.6f);
            MarkStatic(cart);
            Track(cart);

            // marfa varsata pe partea lasata
            Vector3 low = cart.transform.position - cart.transform.right * 1.2f;
            Prop(Kit + "Sacks.prefab", parent, low + cart.transform.forward * 0.6f, R(0f, 360f), 0.02f, 0f, false);
            GameObject barrel = Spawn(Kit + "Barrel.prefab", parent, low - cart.transform.forward * 1.3f, R(0f, 360f));
            if (barrel != null)
            {
                barrel.transform.rotation = Quaternion.Euler(0f, R(0f, 360f), 0f) * Quaternion.Euler(90f, 0f, 0f) * barrel.transform.rotation;
                Put(barrel, low - cart.transform.forward * 1.3f - cart.transform.right * 0.4f, 0.06f);
                StripColliders(barrel);
                MarkStatic(barrel);
            }
            Prop(Kit + "Pot_Amphora.prefab", parent, low + cart.transform.forward * 1.6f - cart.transform.right * 0.5f, R(0f, 360f), 0.02f, 0f, false, true, 70f);
        }

        // gard de lemn putrezit: stalpi strambi, unii lipsa, sipci cazute
        static void FenceLine(Transform parent, float from, float to, int side, float extra)
        {
            Transform fence = Group(parent, "Fence_Old");
            Vector3 prev = Vector3.zero;
            bool hasPrev = false;
            for (float s = from; s <= to; s += 2.3f)
            {
                Vector3 p = Edge(s, side, extra + R(-0.12f, 0.12f));
                if (rng.NextDouble() < 0.15 || !Free(p, 0.15f)) { hasPrev = false; continue; }
                float h = R(1f, 1.3f);
                Quaternion tilt = Quaternion.AngleAxis(R(2f, 11f), Quaternion.Euler(0f, R(0f, 360f), 0f) * Vector3.right);
                GameObject post = Cylinder(fence, "Post", Vector3.zero, new Vector3(0.11f, h * 0.5f, 0.11f), tilt);
                post.transform.position = p + tilt * Vector3.up * (h * 0.5f - 0.15f);
                if (hasPrev && rng.NextDouble() > 0.25)
                {
                    float[] heights = { 0.82f, 0.42f };
                    foreach (float y in heights)
                    {
                        Vector3 a = prev + Vector3.up * y, b = p + Vector3.up * y;
                        if (rng.NextDouble() < 0.25) b = p + Vector3.up * 0.05f;   // sipca desprinsa la un capat
                        Vector3 mid = (a + b) * 0.5f;
                        Quaternion rot = Quaternion.LookRotation(b - a) * Quaternion.Euler(0f, 0f, R(-8f, 8f));
                        GameObject rail = Box(fence, "Rail", Vector3.zero, new Vector3(0.05f, 0.09f, (b - a).magnitude + 0.15f), rot);
                        rail.transform.position = mid;
                    }
                }
                prev = p;
                hasPrev = true;
            }
            MarkStatic(fence.gameObject);
            Track(fence.gameObject);
        }

        // ------------------------------------------------------------------ arena

        static void BuildArenaDetails(Transform arena, Vector3 c, RiddleStone riddle)
        {
            CollectOccupied(arena);
            Transform details = Group(arena, "Details");
            float entryAngle = EntryAngle(c);

            // cercul ritual pictat sub piatra
            GameObject circle = new GameObject("RitualCircle_Decal");
            circle.transform.SetParent(details, false);
            circle.transform.SetPositionAndRotation(c + Vector3.up * 0.6f, Quaternion.Euler(90f, R(0f, 360f), 0f));
            DecalProjector decal = circle.AddComponent<DecalProjector>();
            decal.material = art.ritualDecal;
            decal.size = new Vector3(11.5f, 11.5f, 2.4f);
            decal.pivot = Vector3.zero;
            decal.drawDistance = 120f;
            decal.fadeFactor = 0.9f;

            // menhire vechi in interiorul inelului, intre pietrele sigiliului
            Transform stones = Group(details, "StandingStones");
            for (int i = 0; i < 7; i++)
            {
                float angle = entryAngle + 40f + i * (280f / 6f) + R(-4f, 4f);
                Vector3 p = Around(c, angle, 15.3f);
                GameObject stone = Prop(WorldKit + "SM_Cliff_Spire.prefab", stones, p, R(0f, 360f), 0.35f, R(3f, 4.4f), true, true, R(2f, 7f));
                if (stone != null) stone.name = "StandingStone_" + i;
                GrassTufts(stones, Around(c, angle + R(-3f, 3f), 16f), 0.9f, 4);
            }

            // doua braziere care incadreaza intrarea; doar unul arunca umbre (o lumina punctuala = 6 harti de umbra)
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject brazier = Prop(Kit + "Brazier.prefab", details, Around(c, entryAngle + side * 26f, 15.4f), R(0f, 360f), 0.05f, 0f, true);
                if (brazier == null) continue;
                foreach (Light light in brazier.GetComponentsInChildren<Light>(true))
                {
                    if (side < 0)
                    {
                        light.shadows = LightShadows.Soft;
                        shadowLights.Add(light);
                    }
                    light.shadowStrength = 0.75f;
                    light.range = Mathf.Max(light.range, 11f);
                    light.intensity *= 1.4f;
                }
            }

            // la nord nu mai e arcada: acolo e poarta spre Drumul 2 (Arena1Builder.Road2); locul ramane liber
            occupied.Add(new Bounds(Around(c, entryAngle + 180f, 23f), new Vector3(7.5f, 6f, 11f)));

            // spanzuratori de lemn langa intrare, in afara inelului
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 p = Around(c, entryAngle + side * R(34f, 44f), R(22f, 24f));
                if (Free(p, 1.2f)) BuildGibbet(details, p, Mathf.Atan2(c.x - p.x, c.z - p.z) * Mathf.Rad2Deg + side * 70f);
            }
            for (int i = 0, n = 0; n < 2 && i < 40; i++)
            {
                Vector3 p = Around(c, entryAngle + R(70f, 290f), R(22.5f, 26f));
                if (!Free(p, 2f)) continue;
                Prop(Kit + "Tree_Dead.prefab", details, p, R(0f, 360f), 0.3f, R(5f, 7f), true);
                n++;
            }

            // banda de margine (14..17 m): moloz, oase, arme rupte, flori si ciuperci la baza pietrelor
            Transform rim = Group(details, "Rim");
            for (int i = 0; i < 26; i++)
            {
                float angle = entryAngle + R(20f, 340f);
                Vector3 p = Around(c, angle, R(FightRadius + 0.6f, 17f));
                switch (i % 7)
                {
                    case 0: Prop(Kit + "Rubble_Pile.prefab", rim, p, R(0f, 360f), 0.08f, 0f, false); break;
                    case 1: Prop(Kit + "Bones_Pile.prefab", rim, p, R(0f, 360f), 0.05f, 0f, false); break;
                    case 2: StuckWeapon(rim, p, R(0f, 360f), R(5f, 25f), rng.Next(2) == 0); break;
                    case 3: LyingShield(rim, p, R(0f, 360f)); break;
                    case 4: Flowers(rim, p, 0.9f, rng.Next(3, 6)); break;
                    case 5: GrassTufts(rim, p, 1f, rng.Next(3, 6)); break;
                    default:
                        for (int k = 0; k < 3; k++)
                            Prop(Pick(Rocks), rim, p + new Vector3(R(-0.6f, 0.6f), 0f, R(-0.6f, 0.6f)), R(0f, 360f), 0.04f, R(0.12f, 0.35f), false, false);
                        break;
                }
            }
            for (int i = 0; i < 5; i++)
            {
                GameObject shroom = Prop(Kit + "Mushrooms_Glow.prefab", rim, Around(c, entryAngle + R(30f, 330f), R(16f, 17f)), R(0f, 360f), 0.02f, 0f, false);
                SetLights(shroom, i < 2, false);
            }

            BuildOfferings(riddle.transform, c, entryAngle);
        }

        // lumanari si ofrande la piatra; sunt copii ai pietrei, se scufunda odata cu ea
        static void BuildOfferings(Transform root, Vector3 c, float entryAngle)
        {
            Transform offerings = Group(root, "Offerings");
            float radius = 2.2f;
            Collider rock = null;
            foreach (Collider col in root.GetComponentsInChildren<Collider>())
                if (!col.isTrigger) rock = col;
            if (rock != null) radius = Mathf.Max(rock.bounds.extents.x, rock.bounds.extents.z) + 0.45f;

            for (int i = 0; i < 4; i++)
                Candles(offerings, Around(c, entryAngle + (i - 1.5f) * 22f + R(-4f, 4f), radius + R(0f, 0.3f)), i == 0 || i == 3);
            Prop(Kit + "Pot_Round.prefab", offerings, Around(c, entryAngle - 50f, radius + 0.2f), R(0f, 360f), 0.03f, 0.45f, false);
            Prop(Kit + "Pot_Amphora.prefab", offerings, Around(c, entryAngle + 58f, radius + 0.3f), R(0f, 360f), 0.03f, 0.7f, false, true, 75f);
            Prop(Kit + "Bones_Pile.prefab", offerings, Around(c, entryAngle + 38f, radius + 0.6f), R(0f, 360f), 0.05f, 0.18f, false);
            LyingSword(offerings, Around(c, entryAngle - 8f, radius + 0.55f), entryAngle + 90f + R(-10f, 10f));
            Flowers(offerings, Around(c, entryAngle + 18f, radius + 0.4f), 0.5f, 3);
            Flowers(offerings, Around(c, entryAngle - 30f, radius + 0.35f), 0.5f, 3);
        }

        // ------------------------------------------------------------------ steaguri

        static void BuildBanners(Transform arena, Vector3 c, RiddleStone riddle, Material[] mats)
        {
            Transform group = Group(arena, "RiddleBanners");
            float sEnd = ctrlS[ctrlS.Length - 1];
            float[] distances = { 31f, 28f, 25f, 22f };   // I e primul pe care il vede jucatorul
            for (int i = 0; i < 4; i++)
            {
                float s = sEnd;
                while (s > 0f && FlatDistance(RoadPoint(s), c) < distances[i]) s -= 0.25f;
                int side = i % 2 == 0 ? -1 : 1;
                RoadFrame(s, out _, out Vector3 right, out _);
                Vector3 forward = new Vector3(-right.z, 0f, right.x);
                float extra = 0.7f;
                Vector3 p = Edge(s, side, extra);
                for (int k = 0; k < 6 && !Free(p, 0.7f); k++) p = Edge(s, side, extra += 0.6f);

                // fata scrisa spre cel care vine pe drum si putin spre axul drumului
                Vector3 face = (-forward * 0.75f - right * side * 0.65f).normalized;
                GameObject banner = Banner(BannerNumerals[i], Arena1BannerSetup.Names[i], group, p, Mathf.Atan2(face.x, face.z) * Mathf.Rad2Deg, mats[i], mats[mats.Length - 1], false);
                if (banner == null) continue;
                MarkStatic(banner);
                Candles(group, p + face * 0.45f + forward * 0.25f, true);
                Flowers(group, p - face * 0.3f, 0.6f, 3);
            }

            // steagul cu titlul langa piatra; se scufunda impreuna cu ea
            Vector3 entry = Around(c, EntryAngle(c), 1f) - c;
            Vector3 lateral = new Vector3(entry.z, 0f, -entry.x);
            Vector3 titlePos = c + entry * 1.6f + lateral * 3.3f;
            Vector3 toReader = (Around(c, EntryAngle(c), 12f) - titlePos);
            float yaw = Mathf.Atan2(toReader.x, toReader.z) * Mathf.Rad2Deg;
            GameObject title = Banner("Title", "Title", riddle.transform, titlePos, yaw, mats[Arena1BannerSetup.Names.Length - 1], mats[mats.Length - 1], true);
            if (title == null) return;
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            Prop(Kit + "Brazier.prefab", riddle.transform, titlePos + rot * new Vector3(1.75f, 0f, 0.5f), R(0f, 360f), 0.05f, 0f, true);
            Candles(riddle.transform, titlePos + rot * new Vector3(-0.5f, 0f, 0.45f), false);
            Candles(riddle.transform, titlePos + rot * new Vector3(0.55f, 0f, 0.5f), false);

            Light spot = new GameObject("TitleLight").AddComponent<Light>();
            spot.transform.SetParent(riddle.transform, false);
            spot.transform.position = titlePos + rot * new Vector3(0.4f, 3.4f, 2.6f);
            spot.transform.rotation = Quaternion.LookRotation(titlePos + Vector3.up * 2f - spot.transform.position);
            spot.type = LightType.Spot;
            spot.spotAngle = 55f;
            spot.innerSpotAngle = 25f;
            spot.range = 9f;
            spot.intensity = 7f;
            spot.color = new Color(1f, 0.64f, 0.34f);
            spot.shadows = LightShadows.None;
            LightFlicker flicker = spot.gameObject.AddComponent<LightFlicker>();
            flicker.amount = 0.3f;
            flicker.wobble = 0.015f;
        }

        // FBX-ul intr-un parinte aliniat cu lumea, ca colliderele stalpilor sa fie simple
        static GameObject Banner(string label, string meshName, Transform parent, Vector3 p, float yaw, Material cloth, Material wood, bool twoPosts)
        {
            GameObject root = new GameObject("Banner_" + label);
            root.transform.SetParent(parent, false);
            float ground = Ground(p.x, p.z);
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            if (twoPosts)
            {
                Vector3 a = p + rot * new Vector3(-0.83f, 0f, 0f), b = p + rot * new Vector3(0.83f, 0f, 0f);
                ground = Mathf.Min(Ground(a.x, a.z), Ground(b.x, b.z));
            }
            root.transform.SetPositionAndRotation(new Vector3(p.x, ground - 0.1f, p.z), rot);

            GameObject banner = Spawn(Arena1BannerSetup.MeshPath(meshName), root.transform, root.transform.position, yaw);
            if (banner == null) return null;
            foreach (MeshRenderer r in banner.GetComponentsInChildren<MeshRenderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                if (mats.Length > 0) mats[0] = cloth;
                if (mats.Length > 1) mats[1] = wood;
                r.sharedMaterials = mats;
                r.shadowCastingMode = ShadowCastingMode.On;
            }

            float[] posts = twoPosts ? new[] { -0.83f, 0.83f } : new[] { 0f };
            foreach (float x in posts)
            {
                CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
                capsule.direction = 1;
                capsule.radius = 0.07f;
                capsule.height = 3.6f;
                capsule.center = new Vector3(x, 1.6f, 0f);
            }
            NavMeshModifier modifier = root.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;   // stalpii subtiri nu taie gauri in NavMesh
            Track(root);
            return root;
        }

        // ------------------------------------------------------------------ runele de pe piatra

        // runele sunt mesh-uri lipite de suprafata bolovanului (raycast pe un grid), cu UV in atlasul de rune
        static Renderer[] BuildRunes(Transform root, GameObject boulder, Vector3 c, float entryAngle)
        {
            List<Collider> disabled = new List<Collider>();
            foreach (Collider col in Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude))
                if (col.enabled && col.bounds.Intersects(new Bounds(c, Vector3.one * 8f)) && !col.transform.IsChildOf(boulder.transform))
                {
                    col.enabled = false;
                    disabled.Add(col);
                }
            List<MeshCollider> temp = new List<MeshCollider>();
            foreach (MeshFilter mf in boulder.GetComponentsInChildren<MeshFilter>())
            {
                MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                temp.Add(mc);
            }
            Physics.SyncTransforms();

            AssetDatabase.DeleteAsset(RuneMeshPath);
            Renderer[] runes = new Renderer[8];
            float ground = Ground(c.x, c.z);
            for (int i = 0; i < runes.Length; i++)
            {
                int row = i / 4, col = i % 4;
                float angle = entryAngle + (col - 1.5f) * 30f + row * 7.5f;
                float height = row == 0 ? 1.25f : 2.05f;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Vector3 origin = c + dir * 6f + Vector3.up * (ground - c.y + height);
                Mesh mesh = null;
                Vector3 center = Vector3.zero;
                if (Physics.Raycast(origin, -dir, out RaycastHit hit, 8f, ~0, QueryTriggerInteraction.Ignore) && hit.transform.IsChildOf(boulder.transform))
                {
                    center = hit.point;
                    mesh = RuneMesh(i, hit.point, hit.normal, boulder.transform);
                }
                if (mesh == null)
                {
                    Debug.LogWarning($"[Arena1Builder] Runa {i} nu a gasit suprafata pietrei.");
                    continue;
                }
                if (i == 0) AssetDatabase.CreateAsset(mesh, RuneMeshPath);
                else AssetDatabase.AddObjectToAsset(mesh, RuneMeshPath);

                GameObject rune = new GameObject("Rune_" + i);
                rune.transform.SetParent(root, false);
                rune.transform.SetPositionAndRotation(center, Quaternion.identity);
                rune.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer r = rune.AddComponent<MeshRenderer>();
                r.sharedMaterial = art.runes;
                r.shadowCastingMode = ShadowCastingMode.Off;
                runes[i] = r;
            }

            foreach (MeshCollider mc in temp) Object.DestroyImmediate(mc);
            foreach (Collider col in disabled) col.enabled = true;
            Physics.SyncTransforms();
            AssetDatabase.SaveAssets();
            return System.Array.FindAll(runes, r => r != null);
        }

        static Mesh RuneMesh(int index, Vector3 center, Vector3 normal, Transform boulder)
        {
            const int grid = 6;
            const float width = 0.5f, height = 0.58f, lift = 0.012f;
            Vector3 n = normal.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, n);
            if (right.sqrMagnitude < 0.01f) right = Vector3.right;
            right.Normalize();
            Vector3 up = Vector3.Cross(n, right);

            Vector3[] verts = new Vector3[(grid + 1) * (grid + 1)];
            Vector3[] normals = new Vector3[verts.Length];
            Vector2[] uvs = new Vector2[verts.Length];
            int col = index % 4, row = index / 4;
            for (int y = 0; y <= grid; y++)
                for (int x = 0; x <= grid; x++)
                {
                    float u = x / (float)grid, v = y / (float)grid;
                    Vector3 onPlane = center + right * ((u - 0.5f) * width) + up * ((v - 0.5f) * height);
                    Vector3 point = onPlane, pointNormal = n;
                    if (Physics.Raycast(onPlane + n * 0.6f, -n, out RaycastHit hit, 1.2f, ~0, QueryTriggerInteraction.Ignore) && hit.transform.IsChildOf(boulder))
                    {
                        point = hit.point;
                        pointNormal = hit.normal;
                    }
                    int k = y * (grid + 1) + x;
                    verts[k] = point + pointNormal * lift - center;
                    normals[k] = pointNormal;
                    uvs[k] = new Vector2((col + u) / 4f, (row + v) / 2f);
                }
            int[] tris = new int[grid * grid * 6];
            int t = 0;
            for (int y = 0; y < grid; y++)
                for (int x = 0; x < grid; x++)
                {
                    int a = y * (grid + 1) + x, b = a + 1, d = a + grid + 1, e = d + 1;
                    tris[t++] = a; tris[t++] = d; tris[t++] = e;
                    tris[t++] = a; tris[t++] = e; tris[t++] = b;
                }
            Mesh mesh = new Mesh { name = "Mesh_Rune_" + index };
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            // triunghiurile trebuie sa priveasca in afara pietrei
            Vector3 faceNormal = Vector3.Cross(verts[grid + 1] - verts[0], verts[1] - verts[0]);
            if (Vector3.Dot(faceNormal, n) < 0f)
            {
                for (int i = 0; i < tris.Length; i += 3)
                {
                    int tmp = tris[i + 1];
                    tris[i + 1] = tris[i + 2];
                    tris[i + 2] = tmp;
                }
                mesh.triangles = tris;
            }
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        // ------------------------------------------------------------------ obiecte mici

        static GameObject Prop(string path, Transform parent, Vector3 p, float yaw, float sink, float height, bool collider, bool track = true, float lean = 0f)
        {
            GameObject go = Spawn(path, parent, p, yaw);
            if (go == null) return null;
            if (lean > 0f) go.transform.rotation = Quaternion.AngleAxis(lean, Quaternion.Euler(0f, R(0f, 360f), 0f) * Vector3.right) * go.transform.rotation;
            if (height > 0f) ScaleToHeight(go, height);
            Put(go, p, sink);
            if (!collider) StripColliders(go);
            else if (System.Array.IndexOf(Rocks, path) >= 0) EnsureColliders(go);
            SetLights(go, true, false);
            if (track) Track(go);
            return go;
        }

        static void Candles(Transform parent, Vector3 p, bool lit)
        {
            GameObject candles = Prop(Kit + "Candles.prefab", parent, p, R(0f, 360f), 0f, 0f, false, false);
            SetLights(candles, lit, false);
        }

        static void HangChain(Transform parent, Vector3 p, float yaw)
        {
            // raza in sus gaseste partea de jos a arcadei; lantul atarna de acolo
            if (!Physics.Raycast(new Vector3(p.x, Ground(p.x, p.z) + 0.5f, p.z), Vector3.up, out RaycastHit hit, 12f, ~0, QueryTriggerInteraction.Ignore)) return;
            GameObject chain = Spawn(Kit + "Chain_Hanging.prefab", parent, p, yaw + R(0f, 360f));
            if (chain == null || !TryBounds(chain, out Bounds b)) return;
            chain.transform.position += Vector3.up * (hit.point.y + 0.05f - b.max.y);
            StripColliders(chain);
        }

        static void BuildGibbet(Transform parent, Vector3 p, float yaw)
        {
            GameObject gibbet = new GameObject("Gibbet");
            gibbet.transform.SetParent(parent, false);
            gibbet.transform.SetPositionAndRotation(new Vector3(p.x, Ground(p.x, p.z), p.z), Quaternion.Euler(R(-2f, 2f), yaw, R(-3f, 3f)));
            Box(gibbet.transform, "Post", new Vector3(0f, 1.6f, 0f), new Vector3(0.2f, 3.6f, 0.2f), Quaternion.identity);
            Box(gibbet.transform, "Beam", new Vector3(0f, 3.28f, 0.65f), new Vector3(0.15f, 0.15f, 1.6f), Quaternion.identity);
            Box(gibbet.transform, "Brace", new Vector3(0f, 2.86f, 0.32f), new Vector3(0.1f, 0.1f, 0.85f), Quaternion.Euler(45f, 0f, 0f));
            BoxCollider col = gibbet.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.6f, 0f);
            col.size = new Vector3(0.3f, 3.6f, 0.3f);
            MarkStatic(gibbet);
            Track(gibbet);

            GameObject chain = Spawn(Kit + "Chain_Hanging.prefab", parent, gibbet.transform.TransformPoint(new Vector3(0f, 0f, 1.3f)), yaw);
            if (chain != null && TryBounds(chain, out Bounds b))
            {
                chain.transform.position += Vector3.up * (gibbet.transform.TransformPoint(new Vector3(0f, 3.2f, 1.3f)).y - b.max.y);
                StripColliders(chain);
            }
        }

        static void StuckWeapon(Transform parent, Vector3 p, float yaw, float lean, bool spear)
        {
            Mesh mesh = spear ? spearMesh : swordMesh;
            if (mesh == null) return;
            GameObject go = new GameObject(spear ? "Spear_Rusted" : "Sword_Rusted");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = spear ? spearMats : swordMats;
            // varful e sus in mesh; intors, intra in pamant
            go.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(180f + lean, 0f, R(-5f, 5f)));
            float bury = spear ? 0.4f : 0.28f;
            go.transform.position += Vector3.up * (Ground(p.x, p.z) - bury - r.bounds.min.y);
            MarkStatic(go);
            Track(go);
        }

        static void LyingSword(Transform parent, Vector3 p, float yaw)
        {
            if (swordMesh == null) return;
            GameObject go = new GameObject("Sword_Offering");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = swordMesh;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = swordMats;
            go.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(90f, 0f, 0f));
            go.transform.position += Vector3.up * (Ground(p.x, p.z) - 0.02f - r.bounds.min.y);
        }

        static void LyingShield(Transform parent, Vector3 p, float yaw)
        {
            GameObject shield = Spawn(ShieldPath, parent, p, yaw);
            if (shield == null) return;
            float flip = rng.Next(3) == 0 ? -1f : 1f;
            shield.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(flip * R(78f, 88f), 0f, R(-6f, 6f)) * shield.transform.rotation;
            Put(shield, p, 0.02f);
            StripColliders(shield);
            if (shieldMats != null)
                foreach (MeshRenderer r in shield.GetComponentsInChildren<MeshRenderer>(true)) r.sharedMaterials = shieldMats;
            Track(shield);
        }

        static void GrassTufts(Transform parent, Vector3 p, float radius, int count) => Cards(parent, p, radius, count, art.grass, 0.35f, 0.75f, "Grass");

        static void Flowers(Transform parent, Vector3 p, float radius, int count) => Cards(parent, p, radius, count, art.flowers, 0.3f, 0.55f, "Flowers");

        // carduri in cruce (iarba / flori), fara umbre; sar peste drum
        static void Cards(Transform parent, Vector3 p, float radius, int count, Material mat, float hMin, float hMax, string name)
        {
            if (art == null || art.crossCard == null) return;
            for (int i = 0; i < count; i++)
            {
                Vector3 q = p + new Vector3(R(-radius, radius), 0f, R(-radius, radius));
                if (!OffRoad(q, 0.15f)) continue;
                float h = R(hMin, hMax);
                GameObject card = new GameObject(name);
                card.transform.SetParent(parent, false);
                card.transform.SetPositionAndRotation(new Vector3(q.x, Ground(q.x, q.z) - 0.03f, q.z), Quaternion.Euler(R(-6f, 6f), R(0f, 360f), R(-6f, 6f)));
                card.transform.localScale = new Vector3(h * R(0.8f, 1.1f), h, h * R(0.8f, 1.1f));
                card.AddComponent<MeshFilter>().sharedMesh = art.crossCard;
                MeshRenderer r = card.AddComponent<MeshRenderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                MarkStatic(card);
            }
        }

        static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 size, Quaternion localRot) =>
            Primitive(PrimitiveType.Cube, parent, name, localPos, size, localRot);

        static GameObject Cylinder(Transform parent, string name, Vector3 localPos, Vector3 size, Quaternion localRot) =>
            Primitive(PrimitiveType.Cylinder, parent, name, localPos, size, localRot);

        static GameObject Primitive(PrimitiveType type, Transform parent, string name, Vector3 localPos, Vector3 size, Quaternion localRot)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = size;
            MeshRenderer r = go.GetComponent<MeshRenderer>();
            if (woodMat != null) r.sharedMaterial = woodMat;
            return go;
        }

        // ------------------------------------------------------------------ utilitare decor

        static void SetLights(GameObject go, bool on, bool shadows)
        {
            if (go == null) return;
            foreach (Light light in go.GetComponentsInChildren<Light>(true))
            {
                light.enabled = on;
                if (!shadows) light.shadows = LightShadows.None;
            }
        }

        static void StripColliders(GameObject go)
        {
            foreach (Collider col in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
        }

        static void MarkStatic(GameObject go)
        {
            foreach (MeshRenderer r in go.GetComponentsInChildren<MeshRenderer>(true))
                if (r.GetComponentInParent<LightFlicker>() == null)
                    GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }

        static void CollectOccupied(Transform root)
        {
            occupied.Clear();
            foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>())
                if (Mathf.Max(r.bounds.size.x, r.bounds.size.z) < 9f) occupied.Add(r.bounds);
        }

        static void Track(GameObject go)
        {
            if (go != null && TryBounds(go, out Bounds b)) occupied.Add(b);
        }

        static bool Free(Vector3 p, float radius)
        {
            foreach (Bounds b in occupied)
                if (p.x > b.min.x - radius && p.x < b.max.x + radius && p.z > b.min.z - radius && p.z < b.max.z + radius) return false;
            return true;
        }

        static bool OffRoad(Vector3 p, float radius)
        {
            RoadHit hit = NearestRoad(p.x - Origin.x, p.z - Origin.z);
            return hit.dist - radius > hit.halfW + 0.1f;
        }

        static bool OffRoad(GameObject go, float margin)
        {
            if (!TryBounds(go, out Bounds b)) return true;
            Vector3[] corners = { new Vector3(b.min.x, 0f, b.min.z), new Vector3(b.min.x, 0f, b.max.z), new Vector3(b.max.x, 0f, b.min.z), new Vector3(b.max.x, 0f, b.max.z), b.center };
            foreach (Vector3 corner in corners)
                if (!OffRoad(corner, margin)) return false;
            return true;
        }

        // cel mai plat loc de langa margine: cauta pe drum +-6 m si mai departe de margine, pastreaza prima varianta sub panta maxima.
        // maxRise opreste cautarea sa urce pe peretii defileului (locul trebuie sa ramana la nivelul drumului)
        static Vector3 FlatEdge(ref float s, int side, float extra, float footprint, float maxSlope = 12f, float maxRise = 1f)
        {
            float bestS = s, bestSlope = float.MaxValue;
            Vector3 best = Edge(s, side, extra);
            float[] shifts = { 0f, 2f, -2f, 4f, -4f, 6f, -6f };
            float[] extras = { 0f, 1.5f, 3f };
            foreach (float more in extras)
            {
                foreach (float shift in shifts)
                {
                    Vector3 p = Edge(s + shift, side, extra + more);
                    Vector3 mid = RoadPoint(s + shift);
                    if (Mathf.Abs(p.y - Ground(mid.x, mid.z)) > maxRise) continue;
                    float slope = Slope(p, footprint);
                    if (slope < bestSlope - 0.5f)
                    {
                        bestSlope = slope;
                        best = p;
                        bestS = s + shift;
                    }
                }
                if (bestSlope <= maxSlope) break;
            }
            s = bestS;
            return best;
        }

        // panta in grade pe un cerc de raza r (diferenta maxima de inaltime pe diametru)
        static float Slope(Vector3 p, float r)
        {
            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 0.25f;
                float h = Ground(p.x + Mathf.Cos(a) * r, p.z + Mathf.Sin(a) * r);
                min = Mathf.Min(min, h);
                max = Mathf.Max(max, h);
            }
            float center = Ground(p.x, p.z);
            min = Mathf.Min(min, center);
            max = Mathf.Max(max, center);
            return Mathf.Atan2(max - min, 2f * r) * Mathf.Rad2Deg;
        }

        // punctul de langa margine aflat inca pe fundul drumului (pantele defileului incep chiar la margine);
        // r = cat se retrage obiectul spre drum ca sa nu intre in perete
        static Vector3 FloorEdge(float s, int side, float r)
        {
            RoadFrame(s, out Vector3 pos, out Vector3 right, out float halfW);
            Vector3 mid = Origin + pos;
            float floor = Ground(mid.x, mid.z);
            for (float d = halfW + 1.5f; d > 0.5f; d -= 0.1f)
            {
                Vector3 q = mid + right * side * d;
                if (Ground(q.x, q.z) - floor > 0.25f) continue;
                Vector3 p = mid + right * side * Mathf.Max(0.5f, d - r);
                p.y = Ground(p.x, p.z);
                return p;
            }
            return Edge(s, side, 0f);
        }

        // punctul a urcat pe un mal abrupt fata de nivelul drumului
        static bool Steep(Vector3 p, float s)
        {
            Vector3 mid = RoadPoint(s);
            return p.y - Ground(mid.x, mid.z) > 0.6f;
        }

        // punct pe sol la marginea drumului + extra
        static Vector3 Edge(float s, int side, float extra)
        {
            RoadFrame(s, out Vector3 pos, out Vector3 right, out float halfW);
            Vector3 p = Origin + pos + right * side * (halfW + extra);
            p.y = Ground(p.x, p.z);
            return p;
        }

        static Vector3 RoadPoint(float s)
        {
            RoadFrame(s, out Vector3 pos, out _, out _);
            return Origin + pos;
        }

        static Vector3 RoadRight(float s)
        {
            RoadFrame(s, out _, out Vector3 right, out _);
            return right;
        }

        static Vector3 RoadForward(float s)
        {
            Vector3 right = RoadRight(s);
            return new Vector3(-right.z, 0f, right.x);
        }

        static float EntryAngle(Vector3 c)
        {
            Vector3 entry = Origin + RoadCtrl[RoadCtrl.Length - 2] - c;
            return Mathf.Atan2(entry.x, entry.z) * Mathf.Rad2Deg;
        }

        static Vector3 Around(Vector3 c, float angle, float radius)
        {
            Vector3 p = c + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
            p.y = Ground(p.x, p.z);
            return p;
        }

        static float FlatDistance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;
    }
}
