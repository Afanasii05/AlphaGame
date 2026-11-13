using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Alpha.World.EditorTools
{
    // Campul Juramintelor Rupte: ramasitele unei batalii pierdute in fata castelului. Sageti infipte in valuri,
    // sulite rupte, scuturi si coifuri, o catapulta arsa, movile cu sabii, copaci arsi, stindarde rupte si ciori.
    public static partial class Arena2Builder
    {
        static readonly string[] Volleys = { FieldArt + "SM_A2_ArrowVolley_A.fbx", FieldArt + "SM_A2_ArrowVolley_B.fbx", FieldArt + "SM_A2_ArrowVolley_C.fbx" };
        static readonly string[] Pikes = { FieldArt + "SM_A2_Pike_Shaft.fbx", FieldArt + "SM_A2_Pike_Broken.fbx" };
        static readonly string[] Helmets = { FieldArt + "SM_A2_Helmet_Great.fbx", FieldArt + "SM_A2_Helmet_Kettle.fbx" };
        static readonly string[] Cliffs = { WorldKit + "SM_Cliff_A.prefab", WorldKit + "SM_Cliff_B.prefab", WorldKit + "SM_Cliff_Low.prefab" };
        static readonly string[] BannerCloth = { FieldArt + "SM_A2_BannerCloth_0.fbx", FieldArt + "SM_A2_BannerCloth_1.fbx", FieldArt + "SM_A2_BannerCloth_2.fbx" };

        const string LoreTitle = "The Field of Broken Oaths";
        const string LoreBody = "Here knelt the Oathbound,\nsworn to a throne\nthat would not fall.\n\nThey broke.\nHe did not.";

        static Material charred, paintBlue, paintOchre;

        static void BuildField()
        {
            Transform root = new GameObject(RoadRoot).transform;
            charred = Load<Material>(A2Mat + "M_A2_WoodCharred.mat");
            paintBlue = Load<Material>(A2Mat + "M_A2_ShieldPaint_Blue.mat");
            paintOchre = Load<Material>(A2Mat + "M_A2_ShieldPaint_Ochre.mat");

            Marker(root, "Road2_Path_Start", RoadPoint(0f), RoadYaw(0f));
            Marker(root, "Road2_Path_End", RoadPoint(roadLength), RoadYaw(roadLength));

            Gorge(Group(root, "Gorge"));
            Battlefield(Group(root, "Battlefield"));
            Landmarks(Group(root, "Landmarks"));
            Banners(Group(root, "Banners"));
            CrowFlocks(Group(root, "CrowFlocks"));
            OuterWall(Group(root, "OuterWall"));
            LoreStone(root);
        }

        // ------------------------------------------------------------------ defileul de la iesirea din Arena 1

        static void Gorge(Transform parent)
        {
            for (float s = 4f; s < 40f; s += R(6f, 8f))
                foreach (int side in new[] { -1, 1 })
                {
                    float fade = 1f - Smooth(20f, 40f, s);
                    if (fade < 0.25f && rng.Next(2) == 0) continue;
                    Vector3 p = Edge(s, side, R(6f, 9f));
                    Vector3 toRoad = Flat(RoadPoint(s) - p).normalized;
                    GameObject cliff = Prop(Pick(Cliffs), parent, p, Facing(toRoad) + R(-20f, 20f), R(1.5f, 3f), R(7f, 12f) * Mathf.Lerp(0.6f, 1f, fade), 0f, true);
                    if (cliff == null) continue;
                    cliff.transform.localScale *= R(0.9f, 1.15f);
                    KeepOffRoad(cliff, s, side);
                }
            // bolovani prabusiti la gura defileului
            for (int i = 0; i < 10; i++)
            {
                float s = R(6f, 42f);
                Prop(Pick(Boulders), parent, Edge(s, Side(), R(0.8f, 4f)), R(0f, 360f), R(0.2f, 0.5f), R(0.8f, 2f), 0f, true);
            }
            for (int i = 0; i < 16; i++)
                Prop(Pick(Rocks), parent, Edge(R(4f, 45f), Side(), R(-0.4f, 3f)), R(0f, 360f), 0.05f, R(0.2f, 0.5f));
        }

        // stancile mari scalate pot intra peste drum: le impingem spre exterior pana cand drumul ramane liber
        static void KeepOffRoad(GameObject go, float s, int side)
        {
            Vector3 outward = RoadRight(s) * side;
            for (int step = 0; step < 20 && OverRoad(go, s); step++)
                go.transform.position += outward;
        }

        static bool OverRoad(GameObject go, float s)
        {
            Physics.SyncTransforms();
            for (float ds = -10f; ds <= 10f; ds += 2f)
            {
                float t = Mathf.Clamp(s + ds, 0f, roadLength);
                Vector3 c = RoadPoint(t), r = RoadRight(t);
                float half = RoadHalfW(t) + 0.8f;
                for (float lat = -half; lat <= half; lat += half * 0.5f)
                {
                    Vector3 from = c + r * lat + Vector3.up * 60f;
                    foreach (RaycastHit hit in Physics.RaycastAll(from, Vector3.down, 80f))
                        if (hit.collider.transform.IsChildOf(go.transform)) return true;
                }
            }
            return false;
        }

        // ------------------------------------------------------------------ campul de lupta

        static void Battlefield(Transform parent)
        {
            Transform arrows = Group(parent, "Arrows"), pikes = Group(parent, "Pikes"), shields = Group(parent, "Shields"), helmets = Group(parent, "Helmets");
            Transform bones = Group(parent, "Bones"), rocks = Group(parent, "Rocks");
            // valurile de sageti vin dinspre castel
            Vector3 castle = new Vector3(420f, 0f, 420f);
            for (int c = 0; c < 22; c++)
            {
                float s = Mathf.Lerp(30f, roadLength - 18f, c / 21f) + R(-4f, 4f);
                if (!FieldPoint(s, 0.6f, 22f, out Vector3 center)) continue;
                float from = Facing(Flat(castle - center).normalized);
                int n = rng.Next(3, 7);
                for (int i = 0; i < n; i++)
                {
                    Vector3 p = center + new Vector3(R(-3.5f, 3.5f), 0f, R(-3.5f, 3.5f));
                    if (!Clear(p, 0.4f)) continue;
                    Prop(Pick(Volleys), arrows, p, from + R(-25f, 25f), R(0.02f, 0.12f));
                }
            }
            // sageti razlete chiar pe drum (o singura sageata, nu un val)
            for (int i = 0; i < 12; i++)
            {
                float s = R(30f, roadLength - 10f);
                Vector3 p = Edge(s, Side(), R(-RoadHalfW(s) * 1.6f, -0.3f));
                Prop(Volleys[0], arrows, p, R(0f, 360f), 0.05f);
            }

            for (int i = 0; i < 34; i++)
            {
                if (!FieldPoint(R(28f, roadLength - 12f), 0.4f, 24f, out Vector3 p)) continue;
                GameObject pike = Prop(Pick(Pikes), pikes, p, R(0f, 360f), R(0.25f, 0.5f), 0f, R(10f, 35f));
                if (pike != null) pike.transform.localScale *= R(0.9f, 1.1f);
            }
            for (int i = 0; i < 28; i++)
            {
                if (!FieldPoint(R(26f, roadLength - 10f), 0.3f, 20f, out Vector3 p)) continue;
                GameObject shield = Prop(FieldArt + "SM_A2_Shield_Heater.fbx", shields, p, R(0f, 360f), 0.03f, 0f, R(0f, 25f));
                int paint = rng.Next(3);
                if (paint == 1) Swap(shield, "M_A2_ShieldPaint", paintBlue);
                else if (paint == 2) Swap(shield, "M_A2_ShieldPaint", paintOchre);
            }
            for (int i = 0; i < 16; i++)
            {
                if (!FieldPoint(R(26f, roadLength - 10f), 0.2f, 18f, out Vector3 p)) continue;
                Prop(Pick(Helmets), helmets, p, R(0f, 360f), 0.04f, 0f, R(0f, 50f));
            }
            for (int i = 0; i < 14; i++)
            {
                if (!FieldPoint(R(30f, roadLength - 15f), 1f, 18f, out Vector3 p)) continue;
                Prop(Kit + "Bones_Pile.prefab", bones, p, R(0f, 360f), 0.05f, R(0.4f, 0.7f));
            }
            for (int i = 0; i < 12; i++)
            {
                if (!FieldPoint(R(40f, roadLength - 15f), 4f, 34f, out Vector3 p)) continue;
                Prop(Pick(Boulders), rocks, p, R(0f, 360f), R(0.3f, 0.6f), R(1.2f, 2.6f), 0f, true);
            }
            for (int i = 0; i < 26; i++)
            {
                if (!FieldPoint(R(30f, roadLength - 8f), 0.2f, 26f, out Vector3 p)) continue;
                Prop(Pick(Rocks), rocks, p, R(0f, 360f), 0.05f, R(0.2f, 0.6f));
            }
        }

        // ------------------------------------------------------------------ repere: catapulta, baricada, movile, copaci arsi

        static void Landmarks(Transform parent)
        {
            // catapulta arsa, inca fumeganda, pe dealul din dreapta
            Vector3 treb = Edge(98f, 1, 22f);
            GameObject trebuchet = Prop(FieldArt + "SM_A2_Trebuchet_Wreck.fbx", parent, treb, RoadYaw(98f) + 140f, 0.15f, 0f, 0f, true);
            if (trebuchet != null)
            {
                ParticleSystem smoke = Loop("Smoke_Trebuchet", trebuchet.transform.parent, treb + Vector3.up * 2.5f, Quaternion.identity, art.fog, 40, 8f, 12f, 2f, 5f, 3.5f);
                Box(smoke, new Vector3(3f, 1f, 3f));
                var main = smoke.main;
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
                main.startColor = new Color(0.16f, 0.15f, 0.15f, 0.35f);
                var shape = smoke.shape;
                shape.rotation = new Vector3(-90f, 0f, 0f);
                var size = smoke.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 2.2f));
                Fade(smoke, 0.3f);
                Wander(smoke, 0.5f, 0.2f);
                Ember(parent, treb + Vector3.up * 0.8f, 2.2f, 6f);
                for (int i = 0; i < 5; i++) Prop(Pick(Rocks), parent, treb + new Vector3(R(-5f, 5f), 0f, R(-5f, 5f)), R(0f, 360f), 0.05f, R(0.3f, 0.6f));
                for (int i = 0; i < 6; i++) Prop(Pick(Volleys), parent, treb + new Vector3(R(-6f, 6f), 0f, R(-6f, 6f)), R(0f, 360f), 0.05f);
            }

            // baricada sparta de tarusi, de-a curmezisul drumului (cu o bresa pe mijloc)
            foreach (int side in new[] { -1, 1 })
            {
                Vector3 p = Edge(60f, side, R(-1.4f, -0.6f));
                Prop(FieldArt + "SM_A2_Barricade_Stakes.fbx", parent, p, RoadYaw(60f) + R(-12f, 12f) + (side > 0 ? 180f : 0f), 0.1f, 0f, 0f, true);
                Prop(FieldArt + "SM_A2_Barricade_Stakes.fbx", parent, Edge(60f + R(2f, 4f), side, R(1.5f, 3f)), RoadYaw(60f) + R(-30f, 30f), 0.1f, 0f, R(5f, 15f), true);
            }

            // movile de piatra cu sabii: mormintele celor care au jurat
            float[] cairns = { 40f, 110f, 150f };
            for (int i = 0; i < cairns.Length; i++)
            {
                int side = i % 2 == 0 ? 1 : -1;
                Vector3 p = Edge(cairns[i], side, R(1.2f, 2.2f));
                Prop(FieldArt + "SM_A2_Cairn_Sword.fbx", parent, p, Facing(Flat(RoadPoint(cairns[i]) - p).normalized), 0.1f, 0f, 0f, true);
                for (int k = 0; k < 2; k++)
                    Prop(FieldArt + "SM_A2_Cairn.fbx", parent, p + RoadForward(cairns[i]) * R(2.5f, 4f) * (k == 0 ? 1f : -1f) + RoadRight(cairns[i]) * side * R(0.5f, 2f), R(0f, 360f), 0.1f, 0f, 0f, true);
            }
            for (int i = 0; i < 6; i++)
            {
                if (!FieldPoint(R(50f, roadLength - 20f), 5f, 26f, out Vector3 p)) continue;
                Prop(FieldArt + "SM_A2_Cairn.fbx", parent, p, R(0f, 360f), 0.1f, 0f, 0f, true);
            }

            // copaci arsi
            Transform trees = Group(parent, "BurntTrees");
            for (int i = 0; i < 16; i++)
            {
                if (!FieldPoint(R(30f, roadLength - 12f), 7f, 36f, out Vector3 p)) continue;
                GameObject tree = Prop(Kit + "Tree_Dead.prefab", trees, p, R(0f, 360f), 0.2f, R(4f, 7.5f), R(0f, 8f), true);
                Swap(tree, "M_Wood075", charred);
                if (rng.Next(3) == 0) Prop(Kit + "Stump_Roots.prefab", trees, p + new Vector3(R(-4f, 4f), 0f, R(-4f, 4f)), R(0f, 360f), 0.1f, 0f, 0f, true);
            }
            for (int i = 0; i < 5; i++)
            {
                if (!FieldPoint(R(40f, roadLength - 20f), 4f, 20f, out Vector3 p)) continue;
                GameObject log = Prop(Kit + "Log_Fallen.prefab", trees, p, R(0f, 360f), 0.1f, 0f, 0f, true);
                Swap(log, "M_Wood075", charred);
            }
        }

        // jar sub o carcasa arsa: lumina calda slaba care palpaie
        static void Ember(Transform parent, Vector3 p, float intensity, float range)
        {
            GameObject go = new GameObject("Embers");
            go.transform.SetParent(parent, false);
            go.transform.position = p;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.45f, 0.18f);
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            go.AddComponent<LightFlicker>();
            ParticleSystem sparks = Loop("Sparks", go.transform, p, Quaternion.Euler(-90f, 0f, 0f), art.glow, 30, 1.5f, 3f, 0.03f, 0.06f, 8f);
            var main = sparks.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
            main.startColor = new Color(1f, 0.55f, 0.2f, 1f);
            var shape = sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 20f;
            shape.radius = 1.2f;
            Wander(sparks, 0.6f, 0.8f);
            Fade(sparks, 0.2f);
        }

        // ------------------------------------------------------------------ stindarde rupte

        static void Banners(Transform parent)
        {
            float[] spots = { 34f, 64f, 92f, 118f, 146f, 172f };
            for (int i = 0; i < spots.Length; i++)
            {
                int side = i % 2 == 0 ? -1 : 1;
                Vector3 p = Edge(spots[i], side, R(1f, 3f));
                // panza sta cu fata spre cel care vine pe drum
                Banner(parent, p, RoadYaw(spots[i]) + R(-20f, 20f), R(3f, 14f), rng.Next(3) == 0 ? 2 : 3, R(0.5f, 0.6f));
            }
            for (int i = 0; i < 4; i++)
            {
                if (!FieldPoint(R(40f, roadLength - 30f), 10f, 30f, out Vector3 p)) continue;
                Banner(parent, p, R(0f, 360f), R(8f, 30f), rng.Next(1, 3), R(0.45f, 0.55f));
            }
        }

        // stalp + panza in 1-3 segmente inlantuite (pivot sus, fiecare segment sub cel de deasupra)
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
                // panza atarna vertical chiar daca stalpul e aplecat
                if (i == 0) seg.rotation = Quaternion.Euler(0f, yaw, 0f);
                Child(BannerCloth[i], seg, Vector3.zero);
                segs[i] = seg;
                holder = seg;
            }
            StripColliders(root);
            BannerSway sway = root.AddComponent<BannerSway>();
            SetRefs(sway, "segments", segs);
            SetFloat(sway, "angle", R(5f, 8f));
            SetFloat(sway, "speed", R(0.5f, 0.8f));
            ClearStatic(root);
            return root;
        }

        // ------------------------------------------------------------------ ciori

        static void CrowFlocks(Transform parent)
        {
            float[] spots = { 35f, 75f, 115f, 150f };
            for (int i = 0; i < spots.Length; i++)
            {
                Vector3 p = Edge(spots[i], i % 2 == 0 ? 1 : -1, R(2.5f, 5f));
                CrowFlockAt(parent, p, rng.Next(5, 8));
            }
        }

        static CrowFlock CrowFlockAt(Transform parent, Vector3 center, int count)
        {
            GameObject flockGo = new GameObject("CrowFlock");
            flockGo.transform.SetParent(parent, false);
            flockGo.transform.position = new Vector3(center.x, Ground(center.x, center.z), center.z);
            Transform[] crows = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                Vector3 q = center + new Vector3(R(-2.5f, 2.5f), 0f, R(-2.5f, 2.5f));
                Transform crow = new GameObject("Crow").transform;
                crow.SetParent(flockGo.transform, false);
                crow.SetPositionAndRotation(new Vector3(q.x, Ground(q.x, q.z), q.z), Quaternion.Euler(0f, R(0f, 360f), 0f));
                crow.localScale = Vector3.one * R(0.95f, 1.15f);
                Child(FieldArt + "SM_A2_Crow_Body.fbx", crow, Vector3.zero);
                foreach (int side in new[] { -1, 1 })
                {
                    Transform wing = new GameObject(side < 0 ? "WingL" : "WingR").transform;
                    wing.SetParent(crow, false);
                    wing.localPosition = new Vector3(side * 0.04f, 0.17f, 0.03f);
                    GameObject mesh = Child(FieldArt + "SM_A2_Crow_Wing.fbx", wing, Vector3.zero);
                    if (mesh != null && side < 0) mesh.transform.localScale = Vector3.Scale(mesh.transform.localScale, new Vector3(-1f, 1f, 1f));
                }
                crows[i] = crow;
            }
            StripColliders(flockGo);
            foreach (Renderer r in flockGo.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            CrowFlock flock = flockGo.AddComponent<CrowFlock>();
            SetRefs(flock, "crows", crows);
            ClearStatic(flockGo);
            return flock;
        }

        // ------------------------------------------------------------------ zidul exterior prabusit, cu bresa pentru drum

        static void OuterWall(Transform parent)
        {
            float z = 348f;
            float[] west = { 380f, 392.5f }, east = { 430f, 442.5f, 455f };
            foreach (float x in west)
                Prop(CastleArt + (x < 385f ? "SM_Castle_WallRuin.fbx" : "SM_Castle_Wall.fbx"), parent, new Vector3(x, 0f, z + R(-0.8f, 0.8f)), R(-4f, 4f), 1.2f, 0f, 0f, true);
            foreach (float x in east)
                Prop(CastleArt + (x > 450f ? "SM_Castle_Wall.fbx" : "SM_Castle_WallRuin.fbx"), parent, new Vector3(x, 0f, z + R(-0.8f, 0.8f)), R(-4f, 4f) + 180f, 1.2f, 0f, 0f, true);
            Prop(CastleArt + "SM_Castle_TowerRuin.fbx", parent, new Vector3(401.5f, 0f, 349f), 200f, 1.2f, 0f, 0f, true);
            Prop(CastleArt + "SM_Castle_Tower.fbx", parent, new Vector3(464f, 0f, 349f), 0f, 1.2f, 0f, 0f, true);
            // daramaturi in bresa, pe langa drum
            for (int i = 0; i < 6; i++)
            {
                Vector3 p = new Vector3(R(404f, 424f), 0f, z + R(-3f, 3f));
                if (!Clear(p, 1.5f)) continue;
                Prop(Kit + "Rubble_Pile.prefab", parent, p, R(0f, 360f), 0.1f, R(0.6f, 1.2f), 0f, true);
            }
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = new Vector3(R(402f, 426f), 0f, z + R(-5f, 5f));
                if (!Clear(p, 0.8f)) continue;
                Prop(Pick(Rocks), parent, p, R(0f, 360f), 0.05f, R(0.3f, 0.7f));
            }
        }

        // ------------------------------------------------------------------ tablita cu inscriptie

        static void LoreStone(Transform root)
        {
            float s = 22f;
            Vector3 p = Edge(s, 1, 0.9f);
            GameObject tablet = Prop(FieldArt + "SM_A2_LoreTablet.fbx", root, p, Facing(Flat(RoadPoint(s) - p).normalized) + 15f, 0.25f, 0f, 0f, true);
            if (tablet == null) return;
            tablet.name = "LoreTablet_BrokenOaths";
            Ember(tablet.transform, tablet.transform.position + Vector3.up * 0.4f - Flat(tablet.transform.position - RoadPoint(s)).normalized * 0.9f, 0.8f, 3f);
            Prop(Kit + "Candles.prefab", root, p + RoadForward(s) * 0.9f - RoadRight(s) * 0.5f, R(0f, 360f), 0.02f);

            LorePanel(tablet, LoreTitle, LoreBody, 4.5f);
        }

        // panoul cu textul tablitei, aratat cand jucatorul e aproape
        static void LorePanel(GameObject tablet, string titleText, string bodyText, float radius)
        {
            GameObject canvasGo = new GameObject("LoreCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(tablet.transform, false);
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 11;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panelGo = new GameObject("Panel", typeof(RectTransform), typeof(CanvasGroup));
            panelGo.transform.SetParent(canvasGo.transform, false);
            RectTransform panelRect = (RectTransform)panelGo.transform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, 60f);
            panelRect.sizeDelta = new Vector2(900f, 420f);
            CanvasGroup group = panelGo.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text title = LoreText(panelGo.transform, "Title", font, 40, new Vector2(0f, 150f), new Vector2(900f, 60f), new Color(0.92f, 0.82f, 0.6f));
            title.fontStyle = FontStyle.Bold;
            Text body = LoreText(panelGo.transform, "Body", font, 30, new Vector2(0f, -30f), new Vector2(900f, 300f), new Color(0.9f, 0.9f, 0.88f));
            body.fontStyle = FontStyle.Italic;
            body.lineSpacing = 1.15f;

            LoreTablet lore = tablet.AddComponent<LoreTablet>();
            SetString(lore, "title", titleText);
            SetString(lore, "body", bodyText);
            SetFloat(lore, "radius", radius);
            SetRef(lore, "panel", group);
            SetRef(lore, "titleText", title);
            SetRef(lore, "bodyText", body);
            panelGo.SetActive(false);
        }

        static Text LoreText(Transform parent, string name, Font font, int size, Vector2 pos, Vector2 box, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = box;
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        // ------------------------------------------------------------------ utilitare pentru camp

        // modelele au fata spre -z: yaw-ul care le intoarce spre directia d
        static float Facing(Vector3 d) => Mathf.Atan2(-d.x, -d.z) * Mathf.Rad2Deg;

        // punct pe camp langa drum, pe o parte aleasa la intamplare; refuza pantele abrupte, santul si curtea
        static bool FieldPoint(float s, float minOff, float maxOff, out Vector3 p)
        {
            p = Edge(s, Side(), R(minOff, maxOff));
            return Clear(p, minOff * 0.8f);
        }

        static bool Clear(Vector3 p, float roadMargin)
        {
            RoadHit hit = NearestRoad(p.x, p.z);
            if (hit.dist < RoadHalfW(hit.s) + roadMargin) return false;
            if (p.z > 352f && p.x > 376f) return false;   // santul, zidul si castelul au decorul lor
            if (p.z < TerrainPos.z + 2f || p.x < TerrainPos.x + 2f || p.x > TerrainPos.x + TerrainSize.x - 2f) return false;
            return Steepness(p.x, p.z) < 30f;
        }

        static float Steepness(float x, float z)
        {
            if (terrain == null) return 0f;
            Vector3 tp = terrain.transform.position, size = terrain.terrainData.size;
            float u = (x - tp.x) / size.x, v = (z - tp.z) / size.z;
            if (u < 0f || u > 1f || v < 0f || v > 1f) return 0f;
            return terrain.terrainData.GetSteepness(u, v);
        }
    }
}
