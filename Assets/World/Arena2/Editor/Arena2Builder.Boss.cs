using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Alpha.World.EditorTools
{
    // Lupta din sala tronului: cavalerul primeste viata, AI, efecte si controllerul de lupta,
    // iar radacina Arena2_Boss tine prezentarea (refolosita din Arena 1), bara de viata si secventa.
    public static partial class Arena2Builder
    {
        const string BossRoot = "Arena2_Boss";
        const string BossControllerPath = Arena2ArtSetup.KnightFolder + "/AC_Knight_Boss.controller";
        const string FxFolder = Folder + "/Art/FX";
        const string GlowMaterialPath = FxFolder + "/M_A2_FX_Glow.mat";
        const string HitParticlesPath = "Assets/VFX/HitParticles/HitParticles.prefab";
        const string SoftDotPath = "Assets/VFX/HitParticles/T_HitSoftDot.png";
        const string BossTitle = "THE OATHBOUND KNIGHT";
        const string BossSubtitle = "Last Warden of the Broken Oath";
        const string BossBarName = "The Oathbound Knight";
        const int EnemyLayer = 6;

        // tinuta armelor in pumn: lancea in lungul antebratului spre varf, scutul cu fata in afara
        static readonly Quaternion LanceGrip = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        static readonly Vector3 LanceOffset = new Vector3(0f, 0.04f, 0f);
        static readonly Quaternion ShieldGrip = Quaternion.LookRotation(Vector3.down, Vector3.back);
        const float LanceLength = 2.9f;

        static void BuildBoss()
        {
            GameObject knight = GameObject.Find(KnightRoot);
            if (knight == null) { Debug.LogError("[Arena2Builder] Cavalerul lipseste, lupta nu a fost construita."); return; }

            EnsureWalkInPlace();
            AnimatorController controller = BuildBossController();
            Animator animator = knight.GetComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            Transform lanceSocket = FindDeep(knight.transform, "Socket_Lance_R");
            Transform shieldSocket = FindDeep(knight.transform, "Socket_Shield_L");
            Transform lance = lanceSocket != null && lanceSocket.childCount > 0 ? lanceSocket.GetChild(0) : null;
            Transform shield = shieldSocket != null && shieldSocket.childCount > 0 ? shieldSocket.GetChild(0) : null;
            if (lance != null) lance.SetLocalPositionAndRotation(LanceOffset, LanceGrip);
            if (shield != null) shield.SetLocalPositionAndRotation(Vector3.zero, ShieldGrip);
            Transform tip = lanceSocket != null ? Marker(lanceSocket, "LanceTip", Vector3.zero, 0f).transform : null;
            if (tip != null) tip.SetLocalPositionAndRotation(LanceTipPoint(lanceSocket, lance), Quaternion.identity);

            foreach (Transform t in knight.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = EnemyLayer;

            Rigidbody rb = knight.GetComponent<Rigidbody>();
            if (rb == null) rb = knight.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.None;

            CapsuleCollider body = knight.AddComponent<CapsuleCollider>();
            body.center = new Vector3(0f, 1.35f, 0f);
            body.height = 2.7f;
            body.radius = 0.48f;
            BoxCollider guard = shield != null ? ShieldCollider(shield) : null;

            NavMeshAgent agent = knight.AddComponent<NavMeshAgent>();
            agent.radius = 0.6f;
            agent.height = 2.7f;
            agent.speed = 1.05f;
            agent.angularSpeed = 0f;
            agent.acceleration = 6f;
            agent.stoppingDistance = 2.2f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.enabled = false;

            Transform fxRoot = Group(knight.transform, "KnightFx");
            Material glow = GlowMaterial();
            ParticleSystem sparks = Sparks(fxRoot, glow);
            ParticleSystem backBurst = BackBurst(fxRoot, glow);
            ParticleSystem glint = tip != null ? Glint(tip, glow) : null;
            ParticleSystem embers = Embers(fxRoot, glow);
            Light flash = new GameObject("FX_Flash").AddComponent<Light>();
            flash.transform.SetParent(fxRoot, false);
            flash.type = LightType.Point;
            flash.range = 5f;
            flash.intensity = 0f;
            flash.shadows = LightShadows.None;
            AudioSource audio = fxRoot.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;
            audio.rolloffMode = AudioRolloffMode.Linear;
            audio.minDistance = 4f;
            audio.maxDistance = 45f;
            foreach (Transform t in fxRoot.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = EnemyLayer;

            KnightFx fx = knight.AddComponent<KnightFx>();
            SetRef(fx, "sparks", sparks);
            SetRef(fx, "backBurst", backBurst);
            SetRef(fx, "glint", glint);
            SetRef(fx, "embers", embers);
            SetRef(fx, "flash", flash);
            SetRef(fx, "audioSource", audio);
            SetRefs(fx, "shieldRenderers", shield != null ? shield.GetComponentsInChildren<Renderer>(true) : new Renderer[0]);
            SetRefs(fx, "bodyRenderers", knight.GetComponentsInChildren<SkinnedMeshRenderer>(true));

            OathboundKnight boss = knight.AddComponent<OathboundKnight>();
            SetFloat(boss, "Max_Health", 600f);
            SetFloat(boss, "armor", 1f);
            SetFloat(boss, "knockBackProtection", 10f);

            KnightAI ai = knight.AddComponent<KnightAI>();
            SetRef(ai, "knight", boss);
            SetRef(ai, "fx", fx);
            SetRef(ai, "animator", animator);
            SetRef(ai, "agent", agent);
            SetRef(ai, "lanceGrip", lanceSocket);
            SetRef(ai, "lanceTip", tip);
            SetRef(ai, "hitParticlesPrefab", Load<GameObject>(HitParticlesPath));

            SetRef(boss, "ai", ai);
            SetRef(boss, "fx", fx);
            SetRef(boss, "body", body);
            SetRef(boss, "shield", guard);

            RecallStrike recall = knight.AddComponent<RecallStrike>();
            SetRef(recall, "target", boss);
            SetRefs(recall, "parts", guard != null ? new Object[] { body, guard } : new Object[] { body });

            BuildBossStage(boss, ai);
        }

        // ------------------------------------------------------------------ prezentare + secventa

        static void BuildBossStage(OathboundKnight boss, KnightAI ai)
        {
            Transform root = new GameObject(BossRoot).transform;
            Arena1FxBuilder.Result ui = new Arena1FxBuilder.Result
            {
                ornament = Load<Sprite>("Assets/World/Arena1/Art/FX/T_UI_Ornament.png"),
                bar = Load<Sprite>("Assets/World/Arena1/Art/FX/T_UI_Bar.png")
            };
            BossIntro intro = Arena1BossUiBuilder.Build(root, ui, out BossHealthBar healthBar);
            intro.name = "Arena2_BossIntro";
            // cardul si blitul cioplirii din Arena 1; componenta de acolo nu ne trebuie, avem Arena2Carving
            Arena1Carving arena1Carving = Arena1BossUiBuilder.BuildCarving(root, ui, null);
            if (arena1Carving != null) Object.DestroyImmediate(arena1Carving.gameObject);
            Transform canvas = root.Find("Arena1_BossCanvas");
            if (canvas != null)
            {
                canvas.name = "Arena2_BossCanvas";
                SetText(canvas, "TitleCard/Title", BossTitle, 84);
                SetText(canvas, "TitleCard/Subtitle/Text", BossSubtitle, 0);
                SetText(canvas, "BossHealthBar/Name", BossBarName, 0);
            }
            SetFloat(intro, "duration", 5.4f);
            SetFloat(intro, "titleDelay", 2.6f);
            SetFloat(intro, "startDistance", 9f);
            SetFloat(intro, "endDistance", 6f);
            SetFloat(intro, "lookHeight", 2f);
            SetFloat(intro, "sideOffset", 2.2f);

            GameObject castle = GameObject.Find(CastleRoot);
            Transform gate = castle != null ? FindDeep(castle.transform, "CastleGate") : null;
            Transform postern = castle != null ? FindDeep(castle.transform, "PosternGate") : null;

            Arena2Encounter encounter = new GameObject("Arena2_Encounter").AddComponent<Arena2Encounter>();
            encounter.transform.SetParent(root, false);
            SetRef(encounter, "gate", gate != null ? gate.GetComponent<CastleGate>() : null);
            SetRef(encounter, "postern", postern != null ? postern.GetComponent<CastleGate>() : null);
            SetRef(encounter, "knight", boss);
            SetRef(encounter, "ai", ai);
            SetRef(encounter, "intro", intro);
            SetRef(encounter, "healthBar", healthBar);
            SetString(encounter, "bossName", BossBarName);
            Transform trigger = castle != null ? FindDeep(castle.transform, "Arena2_GateTrigger") : null;
            SetRef(encounter, "trigger", trigger != null ? trigger.GetComponent<CastleGateTrigger>() : null);
            SetFloat(healthBar, "fadeTime", 1.2f);
            BuildRitual(root, canvas, encounter, boss);
            if (gate == null || postern == null) Debug.LogError("[Arena2Builder] Portile castelului lipsesc, secventa boss-ului nu porneste.");
        }

        static void SetText(Transform root, string path, string value, int size)
        {
            Transform t = root.Find(path);
            Text text = t != null ? t.GetComponent<Text>() : null;
            if (text == null) { Debug.LogError($"[Arena2Builder] Textul {path} lipseste din canvas."); return; }
            text.text = value;
            if (size > 0) text.fontSize = size;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        // ------------------------------------------------------------------ animatie

        // mersul cu scutul ramane pe loc: cavalerul e deplasat din cod, cu viteza clipului
        static void EnsureWalkInPlace()
        {
            string path = $"{Arena2ArtSetup.KnightFolder}/A_Knight_ShieldWalk.fbx";
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return;
            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            bool changed = false;
            for (int i = 0; i < clips.Length; i++)
                if (clips[i].lockRootPositionXZ)
                {
                    clips[i].lockRootPositionXZ = false;
                    changed = true;
                }
            if (!changed) return;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        static AnimatorController BuildBossController()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(BossControllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(BossControllerPath);
            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in sm.states) sm.RemoveState(child.state);
            foreach (AnimatorControllerParameter p in controller.parameters) controller.RemoveParameter(p);
            controller.AddParameter(new AnimatorControllerParameter { name = "AttackSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
            controller.AddParameter(new AnimatorControllerParameter { name = "WalkSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });

            AnimationClip thrust = Arena2ArtSetup.LoadClip("A_Knight_LanceThrust");
            AnimationClip walk = Arena2ArtSetup.LoadClip("A_Knight_ShieldWalk");
            AnimatorState sit = State(sm, "Sit", Arena2ArtSetup.LoadClip(Arena2ArtSetup.SitClip), 0);
            State(sm, "Rise", Arena2ArtSetup.LoadClip("A_Knight_ThroneRise"), 1);
            AnimatorState guard = State(sm, "Guard", thrust, 2);
            guard.speed = 0f;
            AnimatorState walkState = State(sm, "Walk", walk, 3);
            walkState.speedParameterActive = true;
            walkState.speedParameter = "WalkSpeed";
            // clipul e redat in ritmul vitezei de mers din joc, ca sa nu alunece picioarele
            float clipSpeed = walk != null ? new Vector3(walk.averageSpeed.x, 0f, walk.averageSpeed.z).magnitude : 0f;
            walkState.speed = clipSpeed > 0.05f ? 1.05f / clipSpeed : 1f;
            foreach (string name in new[] { "Thrust", "Thrust2" })
                Attack(State(sm, name, thrust, name == "Thrust" ? 4 : 5));
            Attack(State(sm, "Sweep", Arena2ArtSetup.LoadClip("A_Knight_LanceSweep"), 6));
            State(sm, "Kneel", Arena2ArtSetup.LoadClip("A_Knight_Kneel_Defeated"), 7);
            sm.defaultState = sit;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        static AnimatorState State(AnimatorStateMachine sm, string name, AnimationClip clip, int row)
        {
            AnimatorState state = sm.AddState(name, new Vector3(300f, 60f * row, 0f));
            state.motion = clip;
            state.writeDefaultValues = true;
            if (clip == null) Debug.LogError($"[Arena2Builder] Clipul pentru starea {name} lipseste.");
            return state;
        }

        static void Attack(AnimatorState state)
        {
            state.speedParameterActive = true;
            state.speedParameter = "AttackSpeed";
        }

        // varful lancei: capatul axei lungi a mesh-ului cel mai departat de pumn
        static Vector3 LanceTipPoint(Transform socket, Transform lance)
        {
            MeshFilter mf = lance != null ? lance.GetComponentInChildren<MeshFilter>(true) : null;
            if (mf == null || mf.sharedMesh == null) return Vector3.up * LanceLength;
            Bounds b = mf.sharedMesh.bounds;
            Vector3 axis = b.size.x >= b.size.y && b.size.x >= b.size.z ? Vector3.right * b.extents.x
                : b.size.y >= b.size.z ? Vector3.up * b.extents.y : Vector3.forward * b.extents.z;
            Vector3 a = socket.InverseTransformPoint(mf.transform.TransformPoint(b.center + axis));
            Vector3 c = socket.InverseTransformPoint(mf.transform.TransformPoint(b.center - axis));
            return a.sqrMagnitude >= c.sqrMagnitude ? a : c;
        }

        // ------------------------------------------------------------------ coliziuni

        static BoxCollider ShieldCollider(Transform shield)
        {
            MeshFilter mf = shield.GetComponentInChildren<MeshFilter>(true);
            if (mf == null || mf.sharedMesh == null) return null;
            BoxCollider box = mf.gameObject.AddComponent<BoxCollider>();
            Bounds b = mf.sharedMesh.bounds;
            Vector3 size = b.size;
            // grosimea scutului e mica: o ingrosam ca lovitura sa nu treaca printre cadre
            if (size.x <= size.y && size.x <= size.z) size.x = Mathf.Max(size.x, 0.25f);
            else if (size.y <= size.z) size.y = Mathf.Max(size.y, 0.25f);
            else size.z = Mathf.Max(size.z, 0.25f);
            box.center = b.center;
            box.size = size;
            return box;
        }

        // ------------------------------------------------------------------ efecte

        static Material GlowMaterial()
        {
            EnsureFolder(Folder + "/Art", "FX");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(GlowMaterialPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, GlowMaterialPath);
            }
            mat.SetTexture("_BaseMap", Load<Texture2D>(SoftDotPath));
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 2f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.One);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static ParticleSystem Sparks(Transform parent, Material glow)
        {
            ParticleSystem ps = FxSystem("FX_Sparks", parent, glow, 0.5f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.45f), new Color(1f, 0.55f, 0.18f));
            main.gravityModifier = 1.6f;
            FxCone(ps, 38f, 0.05f);
            FxFade(ps);
            ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.05f;
            r.lengthScale = 1.5f;
            return ps;
        }

        static ParticleSystem BackBurst(Transform parent, Material glow)
        {
            ParticleSystem ps = FxSystem("FX_BackBurst", parent, glow, 0.7f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.22f, 0.12f), new Color(1f, 0.55f, 0.25f));
            main.gravityModifier = 0.6f;
            FxCone(ps, 55f, 0.15f);
            FxFade(ps);
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0.2f)));
            return ps;
        }

        static ParticleSystem Glint(Transform tip, Material glow)
        {
            ParticleSystem ps = FxSystem("FX_Glint", tip, glow, 0.4f);
            ps.transform.localPosition = Vector3.zero;
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 0.38f;
            main.startSpeed = 0f;
            main.startSize = 0.9f;
            main.startColor = new Color(1f, 0.93f, 0.75f, 1f);
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1), new ParticleSystem.Burst(0.05f, (short)3) });
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.1f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0f)));
            FxFade(ps);
            return ps;
        }

        static ParticleSystem Embers(Transform parent, Material glow)
        {
            ParticleSystem ps = FxSystem("FX_Embers", parent, glow, 2f);
            ps.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.35f, 0.12f), new Color(1f, 0.62f, 0.2f));
            main.gravityModifier = -0.12f;
            var emission = ps.emission;
            emission.rateOverTime = 16f;
            emission.SetBursts(new ParticleSystem.Burst[0]);
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1f, 1.8f, 0.7f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.8f;
            FxFade(ps);
            return ps;
        }

        static ParticleSystem FxSystem(string name, Transform parent, Material mat, float duration)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 200;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        static void FxCone(ParticleSystem ps, float angle, float radius)
        {
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = angle;
            shape.radius = radius;
        }

        static void FxFade(ParticleSystem ps)
        {
            var color = ps.colorOverLifetime;
            color.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            color.color = g;
        }
    }
}
