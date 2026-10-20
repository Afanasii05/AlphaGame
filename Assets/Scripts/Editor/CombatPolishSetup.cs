using System;
using System.Collections.Generic;
using System.Linq;
using Alpha.Combat;
using Alpha.Input;
using Alpha.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
namespace Alpha.EditorTools
{
    public static class CombatPolishSetup
    {
        private const string FxFolder = "Assets/Weapons/CombatFX";
        private const string SwordPath = "Assets/Weapons/Sword_Unity/SwordHammer.prefab";
        private const string HitAreaPath = "Assets/Environment/HeavyWeaponHitArea.prefab";
        private const string PlayerModelPath = "Assets/Character/MainPlayerModel/Assets/Character/Lich_Character_Prefab.prefab";
        private const string ControllerPath = "Assets/Character/Animations/PlayerAnimation.controller";
        private const string JumpAttackFbx = "Assets/Character/Animations/Standing Melee Run Jump Attack.fbx";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string ShockGlowPath = "Assets/Weapons/Shockwave/M_ShockGlow.mat";
        private const string ShockRingPath = "Assets/Weapons/Shockwave/M_ShockRing.mat";
        private const string MistPath = "Assets/RogueKit/Materials/FX/M_FX_Mist.mat";
        private const string ShockwavePrefabPath = "Assets/Weapons/Shockwave/FX_ChargeShockwave.prefab";
        private const string GatherPrefabPath = "Assets/Weapons/Shockwave/FX_ChargeGather.prefab";

        private static readonly List<string> log = new List<string>();

        [MenuItem("Tools/Alpha/Setup Combat Polish")]
        public static void Run()
        {
            log.Clear();
            try
            {
                EnsureFolder();
                Color rune = ReadRuneColor();
                Material trailMaterial = CreateMaterial("M_WeaponTrail", ShockGlowPath, new Color(rune.r * 2.2f, rune.g * 2.2f, rune.b * 2.2f, 1f), true);
                Material ghostMaterial = CreateMaterial("M_AfterImage", ShockGlowPath, new Color(rune.r * 1.2f, rune.g * 1.2f, rune.b * 1.2f, 0.45f), true);
                Material dustMaterial = CreateMaterial("M_Dust", MistPath, new Color(0.55f, 0.5f, 0.45f, 0.55f), false);
                Material speedLineMaterial = CreateMaterial("M_SpeedLine", ShockGlowPath, new Color(1.6f, 1.6f, 1.6f, 0.35f), true);
                Material ringMaterial = AssetDatabase.LoadAssetAtPath<Material>(ShockRingPath);

                GameObject dustBurst = BuildDustBurst(dustMaterial);
                GameObject debris = BuildDebris();
                GameObject dustRing = BuildDustRing(dustMaterial);
                GameObject footDust = BuildFootDust(dustMaterial);
                GameObject dustTrail = BuildDustTrail(dustMaterial);
                GameObject speedLines = BuildSpeedLines(speedLineMaterial);
                GameObject jumpRing = BuildJumpRing(ringMaterial);

                SetupSword(trailMaterial);
                SetupHitArea(dustBurst, debris);
                SetupAnimator();
                Dictionary<string, float> aim = MeasureAim();
                SetupPlayer(dustBurst, dustRing, footDust, dustTrail, speedLines, jumpRing, ghostMaterial, aim);
                SetupScene();
            }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPolish] Setup oprit: " + exception);
                return;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CombatPolish] Setup complet:\n - " + string.Join("\n - ", log));
        }

        //---------------------------------------------------------------- materiale

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(FxFolder))
            {
                AssetDatabase.CreateFolder("Assets/Weapons", "CombatFX");
                log.Add("Folder creat: " + FxFolder);
            }
        }

        private static Color ReadRuneColor()
        {
            GameObject sword = AssetDatabase.LoadAssetAtPath<GameObject>(SwordPath);
            if (sword != null)
            {
                foreach (Light light in sword.GetComponentsInChildren<Light>(true))
                    return light.color;
            }
            return new Color(0.35f, 1f, 0.5f, 1f);
        }

        private static Material CreateMaterial(string name, string sourcePath, Color color, bool clearTexture)
        {
            string path = FxFolder + "/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;
            Material source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
            if (source == null)
                throw new InvalidOperationException("Lipseste materialul sursa: " + sourcePath);
            Material material = new Material(source);
            material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (clearTexture)
                material.SetTexture("_BaseMap", null);
            AssetDatabase.CreateAsset(material, path);
            log.Add("Material creat: " + path);
            return material;
        }

        //---------------------------------------------------------------- particule

        private static bool FxExists(string name, out GameObject existing)
        {
            existing = AssetDatabase.LoadAssetAtPath<GameObject>(FxFolder + "/" + name + ".prefab");
            return existing != null;
        }

        private static GameObject SaveFx(GameObject temp, string name)
        {
            string path = FxFolder + "/" + name + ".prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            log.Add("Prefab FX creat: " + path);
            return saved;
        }

        private static ParticleSystem NewSystem(string name, Material material, bool loop)
        {
            GameObject go = new GameObject(name);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.duration = 1f;
            main.loop = loop;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = loop ? ParticleSystemStopAction.None : ParticleSystemStopAction.Destroy;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        private static Gradient FadeGradient(float peakAlpha, float peakTime)
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colors = { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) };
            GradientAlphaKey[] alphas;
            if (peakTime <= 0f)
                alphas = new[] { new GradientAlphaKey(peakAlpha, 0f), new GradientAlphaKey(0f, 1f) };
            else
                alphas = new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(peakAlpha, peakTime), new GradientAlphaKey(0f, 1f) };
            gradient.SetKeys(colors, alphas);
            return gradient;
        }

        private static void SetColorFade(ParticleSystem ps, float peakAlpha, float peakTime)
        {
            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(FadeGradient(peakAlpha, peakTime));
        }

        private static void SetSizeCurve(ParticleSystem ps, AnimationCurve curve)
        {
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        private static void SetDrag(ParticleSystem ps, float limit, float dampen)
        {
            ParticleSystem.LimitVelocityOverLifetimeModule drag = ps.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.limit = limit;
            drag.dampen = dampen;
        }

        private static GameObject BuildDustBurst(Material dust)
        {
            if (FxExists("FX_DustBurst", out GameObject existing))
                return existing;
            ParticleSystem ps = NewSystem("FX_DustBurst", dust, false);
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = 60;
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)25) });
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.4f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            SetColorFade(ps, 0.6f, 0.1f);
            SetSizeCurve(ps, AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1f));
            SetDrag(ps, 0.8f, 0.08f);
            return SaveFx(ps.gameObject, "FX_DustBurst");
        }

        private static GameObject BuildDebris()
        {
            if (FxExists("FX_Debris", out GameObject existing))
                return existing;
            Material gravel = FindGravelMaterial();
            ParticleSystem ps = NewSystem("FX_Debris", gravel, false);
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            main.gravityModifier = 2f;
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = 30;
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)15) });
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.2f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            SetSizeCurve(ps, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0f)));
            ParticleSystem.CollisionModule collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.bounce = 0.3f;
            collision.dampen = 0.5f;
            collision.lifetimeLoss = 0.1f;
            return SaveFx(ps.gameObject, "FX_Debris");
        }

        private static Material FindGravelMaterial()
        {
            foreach (string guid in AssetDatabase.FindAssets("Gravel043 t:Material"))
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material != null && material.name == "Gravel043")
                    return material;
            }
            foreach (string guid in AssetDatabase.FindAssets("Gravel t:Material"))
                return AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            return AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
        }

        private static GameObject BuildDustRing(Material dust)
        {
            if (FxExists("FX_DustRing", out GameObject existing))
                return existing;
            ParticleSystem ps = NewSystem("FX_DustRing", dust, false);
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            main.startSpeed = 4f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.2f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = 40;
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)24) });
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;
            shape.radiusThickness = 0f;
            shape.arc = 360f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            SetColorFade(ps, 0.55f, 0.1f);
            SetSizeCurve(ps, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.3f));
            SetDrag(ps, 1.2f, 0.1f);
            return SaveFx(ps.gameObject, "FX_DustRing");
        }

        private static GameObject BuildFootDust(Material dust)
        {
            if (FxExists("FX_FootDust", out GameObject existing))
                return existing;
            ParticleSystem ps = NewSystem("FX_FootDust", dust, false);
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = 12;
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)6) });
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 0.1f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            SetColorFade(ps, 0.45f, 0.1f);
            SetSizeCurve(ps, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.2f));
            return SaveFx(ps.gameObject, "FX_FootDust");
        }

        private static GameObject BuildDustTrail(Material dust)
        {
            if (FxExists("FX_DustTrail", out GameObject existing))
                return existing;
            ParticleSystem ps = NewSystem("FX_DustTrail", dust, true);
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = 80;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverDistance = 6f;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;
            SetColorFade(ps, 0.4f, 0.1f);
            SetSizeCurve(ps, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.4f));
            return SaveFx(ps.gameObject, "FX_DustTrail");
        }

        private static GameObject BuildSpeedLines(Material speedLine)
        {
            if (FxExists("FX_SpeedLines", out GameObject existing))
                return existing;
            ParticleSystem ps = NewSystem("FX_SpeedLines", speedLine, true);
            ps.transform.rotation = Quaternion.Euler(0f, 180f, 0f); //+Z al sistemului arata spre camera cand e copil al ei
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 0.2f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(25f, 35f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.04f);
            main.maxParticles = 80;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 40f;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 0f;
            shape.radius = 2.5f;
            shape.radiusThickness = 0.35f;
            SetColorFade(ps, 0.5f, 0.2f);
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 10f;
            renderer.velocityScale = 0f;
            return SaveFx(ps.gameObject, "FX_SpeedLines");
        }

        private static GameObject BuildJumpRing(Material ring)
        {
            if (FxExists("FX_JumpRing", out GameObject existing))
                return existing;
            ParticleSystem ps = NewSystem("FX_JumpRing", ring, false);
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = 0.35f;
            main.startSpeed = 0f;
            main.startSize = 2.5f; //Marimea finala, curba o porneste de la 0.2
            main.maxParticles = 2;
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = false;
            SetSizeCurve(ps, AnimationCurve.EaseInOut(0f, 0.08f, 1f, 1f));
            SetColorFade(ps, 1f, 0f);
            ps.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            return SaveFx(ps.gameObject, "FX_JumpRing");
        }

        //---------------------------------------------------------------- prefab-uri

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        private static void SetRef(Object target, string field, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogError("[CombatPolish] Campul '" + field + "' lipseste pe " + target.GetType().Name);
                return;
            }
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name)
                    return child;
            return null;
        }

        private static void SetupSword(Material trailMaterial)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SwordPath);
            try
            {
                Transform impact = null;
                Weapon weapon = root.GetComponent<Weapon>();
                if (weapon != null)
                    impact = new SerializedObject(weapon).FindProperty("impactPoint").objectReferenceValue as Transform;
                if (impact == null)
                    impact = FindDeep(root.transform, "ImpactPoint");
                if (impact == null)
                    throw new InvalidOperationException("ImpactPoint negasit in SwordHammer");

                Transform trailTransform = impact.Find("Trail");
                if (trailTransform == null)
                {
                    GameObject trailObject = new GameObject("Trail");
                    trailObject.transform.SetParent(impact, false);
                    trailTransform = trailObject.transform;
                }
                TrailRenderer trail = GetOrAdd<TrailRenderer>(trailTransform.gameObject);
                trail.time = 0.18f;
                trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(1f, 0f));
                trail.minVertexDistance = 0.05f;
                trail.sharedMaterial = trailMaterial;
                trail.colorGradient = FadeGradient(1f, 0f);
                trail.emitting = false;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;

                WeaponFX fx = GetOrAdd<WeaponFX>(root);
                SetRef(fx, "trail", trail);
                PrefabUtility.SaveAsPrefabAsset(root, SwordPath);
                log.Add("SwordHammer: Trail + WeaponFX");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupHitArea(GameObject dustBurst, GameObject debris)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HitAreaPath);
            try
            {
                CraterImpactFX fx = GetOrAdd<CraterImpactFX>(root);
                SetRef(fx, "dustBurstPrefab", dustBurst);
                SetRef(fx, "debrisPrefab", debris);
                PrefabUtility.SaveAsPrefabAsset(root, HitAreaPath);
                log.Add("HeavyWeaponHitArea: CraterImpactFX");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject FindCatchFx()
        {
            string[] candidates = { "Assets/ParticleAssets/FX_PullBurst.prefab", "Assets/Environment/FX_PullBurst.prefab" };
            foreach (string path in candidates)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.GetComponentInChildren<ParticleSystem>(true) != null)
                    return prefab;
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(GatherPrefabPath);
        }

        private static void SetupPlayer(GameObject dustBurst, GameObject dustRing, GameObject footDust, GameObject dustTrail,
                                        GameObject speedLines, GameObject jumpRing, Material ghostMaterial,
                                        Dictionary<string, float> aim)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerModelPath);
            try
            {
                Transform polish = root.transform.Find("CombatPolish");
                if (polish == null)
                {
                    GameObject polishObject = new GameObject("CombatPolish");
                    polishObject.transform.SetParent(root.transform, false);
                    polish = polishObject.transform;
                }
                GameObject go = polish.gameObject;
                GetOrAdd<ExtraInputHandler>(go);
                GetOrAdd<CombatFeedback>(go);
                GetOrAdd<HeavyAttackInput>(go);
                GetOrAdd<AttackMotion>(go);
                GetOrAdd<ActionLock>(go);
                ApplyAim(GetOrAdd<AttackAim>(go), aim);
                SetRef(GetOrAdd<AirAttack>(go), "shockwavePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(ShockwavePrefabPath));
                SetRef(GetOrAdd<WeaponRecall>(go), "catchFxPrefab", FindCatchFx());
                PlayerDash dash = GetOrAdd<PlayerDash>(go);
                SetRef(dash, "dustPrefab", dustBurst);
                SetRef(dash, "ghostMaterial", ghostMaterial);
                SetRef(GetOrAdd<PlayerSlide>(go), "dustTrailPrefab", dustTrail);
                SprintFeel sprint = GetOrAdd<SprintFeel>(go);
                SetRef(sprint, "speedLinesPrefab", speedLines);
                SetRef(sprint, "footDustPrefab", footDust);
                LandingImpact landing = GetOrAdd<LandingImpact>(go);
                SetRef(landing, "dustRingPrefab", dustRing);
                SetRef(landing, "footDustPrefab", footDust);
                SetRef(GetOrAdd<DoubleJump>(go), "jumpRingPrefab", jumpRing);

                Animator animator = root.GetComponentInChildren<Animator>(true);
                if (animator == null)
                    throw new InvalidOperationException("Animator negasit in " + PlayerModelPath);
                GetOrAdd<RootMotionRelay>(animator.gameObject);
                GetOrAdd<LeftHandGrip>(animator.gameObject);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerModelPath);
                log.Add("Lich_Character_Prefab: child CombatPolish cu componentele noi; RootMotionRelay + LeftHandGrip pe " + animator.name);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        //---------------------------------------------------------------- masurare directie atac

        private const int AimSteps = 48;

        private static void ApplyAim(AttackAim component, Dictionary<string, float> aim)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty locked = serialized.FindProperty("lockOffsets");
            if (locked != null && locked.boolValue)
            {
                log.Add("AttackAim: lockOffsets bifat, offseturile raman neschimbate");
                return;
            }
            if (aim == null || aim.Count == 0)
            {
                log.Add("AttackAim: nu s-a masurat nimic, offseturile raman neschimbate");
                return;
            }
            foreach (KeyValuePair<string, float> pair in aim)
            {
                SerializedProperty property = serialized.FindProperty(pair.Key);
                if (property == null)
                {
                    Debug.LogError("[CombatPolish] Campul '" + pair.Key + "' lipseste pe AttackAim");
                    continue;
                }
                property.floatValue = pair.Value;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AnimationClip ClipOf(AnimatorState state)
        {
            return state != null ? state.motion as AnimationClip : null;
        }

        //Pozitiile ImpactPoint (in cadrul fix al modelului, fara miscarea radacinii) pe o portiune din clip
        private static List<Vector3> SampleImpact(Animator animator, Transform impact, AnimationClip clip, float from, float to)
        {
            Transform frame = animator.transform;
            Vector3 framePosition = frame.position;
            Quaternion frameRotation = frame.rotation;
            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i <= AimSteps; i++)
            {
                float u = Mathf.Lerp(from, to, i / (float)AimSteps);
                clip.SampleAnimation(animator.gameObject, u * clip.length);
                frame.SetPositionAndRotation(framePosition, frameRotation); //In joc miscarea radacinii o aplica scriptul, transformul ramane pe loc
                points.Add(frame.InverseTransformPoint(impact.position));
            }
            return points;
        }

        private static float YawOf(Vector3 local)
        {
            return Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        }

        //Lovitura puternica: punctul cel mai de jos al varfului, departe de corp
        private static bool MeasureLowest(Animator animator, Transform impact, AnimationClip clip, float from, float to, out float offset)
        {
            offset = 0f;
            List<Vector3> points = SampleImpact(animator, impact, clip, from, to);
            bool found = false;
            Vector3 best = Vector3.zero;
            foreach (Vector3 point in points)
            {
                if (new Vector2(point.x, point.z).magnitude <= 0.25f)
                    continue;
                if (!found || point.y < best.y)
                {
                    best = point;
                    found = true;
                }
            }
            if (found)
                offset = -YawOf(best);
            return found;
        }

        //Lovitura usoara: directia medie a portiunii celei mai extinse din arc
        private static bool MeasureSweep(Animator animator, Transform impact, AnimationClip clip, float from, float to, out float offset)
        {
            offset = 0f;
            List<Vector3> points = SampleImpact(animator, impact, clip, from, to);
            float maxRadius = 0f;
            foreach (Vector3 point in points)
                maxRadius = Mathf.Max(maxRadius, new Vector2(point.x, point.z).magnitude);
            if (maxRadius < 0.3f)
                return false;
            Vector2 sum = Vector2.zero;
            foreach (Vector3 point in points)
            {
                Vector2 flat = new Vector2(point.x, point.z);
                if (flat.magnitude >= maxRadius * 0.75f)
                    sum += flat.normalized;
            }
            if (sum.sqrMagnitude < 0.0001f)
                return false;
            offset = -Mathf.Atan2(sum.x, sum.y) * Mathf.Rad2Deg;
            return true;
        }

        private static void StoreAim(Dictionary<string, float> aim, string label, bool ok, float offset, params string[] fields)
        {
            if (!ok)
            {
                Debug.LogWarning("[CombatPolish] Masurare '" + label + "': nu s-a putut determina directia, offsetul ramane neschimbat");
                log.Add("Aim " + label + ": nemasurat");
                return;
            }
            if (Mathf.Abs(offset) > 45f)
            {
                Debug.LogWarning("[CombatPolish] Masurare '" + label + "': " + offset.ToString("F1") + " grade e prea mult, ignorat (seteaza manual pe AttackAim)");
                log.Add("Aim " + label + ": " + offset.ToString("F1") + " ignorat (peste 45)");
                return;
            }
            foreach (string field in fields)
                aim[field] = offset;
            log.Add("Aim " + label + ": offset " + offset.ToString("F1") + " grade");
        }

        private static Dictionary<string, float> MeasureAim()
        {
            Dictionary<string, float> aim = new Dictionary<string, float>();
            if (Application.isPlaying)
            {
                log.Add("Aim: sarit (editorul e in Play)");
                return aim;
            }
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerModelPath);
            GameObject swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwordPath);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (playerPrefab == null || swordPrefab == null || controller == null)
            {
                log.Add("Aim: lipsesc prefab-urile sau controllerul, sarit");
                return aim;
            }

            AnimatorStateMachine baseMachine = controller.layers[0].stateMachine;
            AnimatorControllerLayer upperLayer = controller.layers.FirstOrDefault(l => l.name == "New Layer");
            AnimationClip heavyClip = ClipOf(FindState(baseMachine, "HeavyDoubleClickHit"));
            AnimationClip slamClip = ClipOf(FindState(baseMachine, "AirSlam"));
            AnimationClip lightClip = upperLayer != null ? ClipOf(FindState(upperLayer.stateMachine, "Hitting")) : null;

            Scene preview = EditorSceneManager.NewPreviewScene();
            GameObject playerInstance = null;
            GameObject swordInstance = null;
            try
            {
                playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, preview);
                swordInstance = (GameObject)PrefabUtility.InstantiatePrefab(swordPrefab, preview);
                Animator animator = playerInstance.GetComponentInChildren<Animator>(true);
                WeaponSocket socket = playerInstance.GetComponentInChildren<WeaponSocket>(true);
                Transform socketTransform = socket != null ? socket.transform : FindDeep(playerInstance.transform, "Socket_Weapon_R");
                Transform impact = FindDeep(swordInstance.transform, "ImpactPoint");
                if (animator == null || socketTransform == null || impact == null)
                {
                    log.Add("Aim: lipseste Animator/socket/ImpactPoint, sarit");
                    return aim;
                }
                swordInstance.transform.SetParent(socketTransform, false);
                swordInstance.transform.localPosition = Vector3.zero;
                swordInstance.transform.localRotation = Quaternion.identity;
                swordInstance.transform.localScale = Vector3.one;

                float offset;
                bool ok;
                if (heavyClip != null)
                {
                    ok = MeasureLowest(animator, impact, heavyClip, 0.2f, 0.85f, out offset);
                    StoreAim(aim, "Heavy", ok, offset, "heavyYaw");
                }
                if (slamClip != null)
                {
                    ok = MeasureLowest(animator, impact, slamClip, 0.2f, 0.85f, out offset);
                    StoreAim(aim, "AirSlam", ok, offset, "airSlamYaw");
                }
                if (lightClip != null)
                {
                    ok = MeasureSweep(animator, impact, lightClip, 0.15f, 0.7f, out offset);
                    StoreAim(aim, "Light", ok, offset, "lightYaw", "airSlashYaw");
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[CombatPolish] Masurarea directiei a esuat, offseturile raman neschimbate: " + exception.Message);
                aim.Clear();
            }
            finally
            {
                if (swordInstance != null)
                    Object.DestroyImmediate(swordInstance);
                if (playerInstance != null)
                    Object.DestroyImmediate(playerInstance);
                EditorSceneManager.ClosePreviewScene(preview);
            }
            return aim;
        }

        //---------------------------------------------------------------- animator

        private static AnimatorState FindState(AnimatorStateMachine machine, string name)
        {
            foreach (ChildAnimatorState child in machine.states)
                if (child.state.name == name)
                    return child.state;
            return null;
        }

        private static AnimatorState GetOrAddState(AnimatorStateMachine machine, string name, Motion motion, float speed, string tag, bool writeDefaults)
        {
            AnimatorState state = FindState(machine, name);
            if (state != null)
                return state;
            state = machine.AddState(name);
            state.motion = motion;
            state.speed = speed;
            state.tag = tag;
            state.writeDefaultValues = writeDefaults;
            log.Add("Animator: stare noua " + name);
            return state;
        }

        private static void AddParameterIfMissing(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            if (controller.parameters.Any(p => p.name == name))
                return;
            controller.AddParameter(name, type);
            log.Add("Animator: parametru nou " + name);
        }

        private static bool HasCondition(AnimatorStateTransition transition, string parameter)
        {
            return transition.conditions.Any(c => c.parameter == parameter);
        }

        private static AnimatorStateTransition GetOrAddTransition(AnimatorState from, AnimatorState to, string parameter,
                                                                  AnimatorConditionMode mode, float duration, float exitTime, bool hasExitTime, float offset = 0f)
        {
            AnimatorStateTransition transition = from.transitions.FirstOrDefault(t => t.destinationState == to && HasCondition(t, parameter));
            if (transition != null)
                return transition;
            transition = from.AddTransition(to);
            transition.hasExitTime = hasExitTime;
            transition.exitTime = exitTime;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.offset = offset;
            transition.AddCondition(mode, 0f, parameter);
            log.Add("Animator: tranzitie " + from.name + " -> " + to.name);
            return transition;
        }

        private static bool Near(float value, float original, float tolerance = 0.011f)
        {
            return Mathf.Abs(value - original) < tolerance;
        }

        private static void SetupAnimator()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                throw new InvalidOperationException("Lipseste " + ControllerPath);

            AddParameterIfMissing(controller, "AirSlash", AnimatorControllerParameterType.Trigger);
            AddParameterIfMissing(controller, "AirCombat", AnimatorControllerParameterType.Bool);
            AddParameterIfMissing(controller, "AirSlam", AnimatorControllerParameterType.Bool);
            AddParameterIfMissing(controller, "Recalling", AnimatorControllerParameterType.Bool);
            AddParameterIfMissing(controller, "ActionLock", AnimatorControllerParameterType.Bool);
            AddParameterIfMissing(controller, "AirPunch", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine baseMachine = controller.layers[0].stateMachine;
            AnimatorStateMachine upperMachine = controller.layers.First(l => l.name == "New Layer").stateMachine;

            //Layer 0: saltul nu mai taie atacul din aer
            AnimatorState jump = FindState(baseMachine, "Jump ");
            AnimatorState idle = FindState(baseMachine, "Idle");
            AnimatorState heavy = FindState(baseMachine, "HeavyDoubleClickHit");
            foreach (AnimatorStateTransition any in baseMachine.anyStateTransitions)
            {
                if (any.destinationState != jump)
                    continue;
                if (!HasCondition(any, "AirCombat"))
                    any.AddCondition(AnimatorConditionMode.IfNot, 0f, "AirCombat");
                if (!HasCondition(any, "AirSlam"))
                    any.AddCondition(AnimatorConditionMode.IfNot, 0f, "AirSlam");
            }

            //Abilitatea (Z) nu mai e intrerupta de mers/alergare: tranzitiile din Any State spre locomotie asteapta ActionLock = false
            string[] locomotion = { "Crouch_stay", "walk ", "run", "crouch_walk " };
            foreach (AnimatorStateTransition any in baseMachine.anyStateTransitions)
            {
                if (any.destinationState == null || !locomotion.Contains(any.destinationState.name) || HasCondition(any, "ActionLock"))
                    continue;
                any.AddCondition(AnimatorConditionMode.IfNot, 0f, "ActionLock");
                log.Add("Animator: Any -> " + any.destinationState.name + " + IfNot ActionLock");
            }

            Motion slamClip = AssetDatabase.LoadAllAssetsAtPath(JumpAttackFbx).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (slamClip == null)
                Debug.LogWarning("[CombatPolish] Clipul din " + JumpAttackFbx + " nu a fost gasit, starea AirSlam ramane fara motion");
            AnimatorState slam = GetOrAddState(baseMachine, "AirSlam", slamClip, 1f, "Heavy", true);
            if (!baseMachine.anyStateTransitions.Any(t => t.destinationState == slam))
            {
                AnimatorStateTransition toSlam = baseMachine.AddAnyStateTransition(slam);
                toSlam.hasExitTime = false;
                toSlam.hasFixedDuration = true;
                toSlam.duration = 0.1f;
                toSlam.canTransitionToSelf = false;
                toSlam.AddCondition(AnimatorConditionMode.If, 0f, "AirSlam");
                log.Add("Animator: Any -> AirSlam");
            }
            GetOrAddTransition(slam, idle, "AirSlam", AnimatorConditionMode.IfNot, 0.2f, 0f, false);

            //Layer 1: lovitura din aer si recall
            AnimatorState newState = FindState(upperMachine, "New State");
            AnimatorState hitting = FindState(upperMachine, "Hitting");
            AnimatorState throwing = FindState(upperMachine, "Throwing");
            AnimatorState airSlash = GetOrAddState(upperMachine, "AirSlash", hitting.motion, 1.8f, "Light", true);
            GetOrAddTransition(newState, airSlash, "AirSlash", AnimatorConditionMode.If, 0.08f, 0f, false);
            if (!airSlash.transitions.Any(t => t.destinationState == newState))
            {
                AnimatorStateTransition back = airSlash.AddTransition(newState);
                back.hasExitTime = true;
                back.exitTime = 0.75f;
                back.hasFixedDuration = true;
                back.duration = 0.2f;
                log.Add("Animator: AirSlash -> New State");
            }

            //Lovitura din aer fara arma: pumnul din layer 0, redat pe jumatatea de sus
            AnimatorState punch = FindState(baseMachine, "punch");
            if (punch == null || punch.motion == null)
                Debug.LogWarning("[CombatPolish] Starea 'punch' sau clipul ei lipseste, AirPunch ramane fara animatie");
            AnimatorState airPunch = GetOrAddState(upperMachine, "AirPunch", punch != null ? punch.motion : null, 1.6f, "Light", true);
            GetOrAddTransition(newState, airPunch, "AirPunch", AnimatorConditionMode.If, 0.08f, 0f, false);
            if (!airPunch.transitions.Any(t => t.destinationState == newState))
            {
                AnimatorStateTransition backPunch = airPunch.AddTransition(newState);
                backPunch.hasExitTime = true;
                backPunch.exitTime = 0.8f;
                backPunch.hasFixedDuration = true;
                backPunch.duration = 0.15f;
                log.Add("Animator: AirPunch -> New State");
            }

            AnimatorState recall = GetOrAddState(upperMachine, "Recall", throwing.motion, -1f, "Throw", true);
            GetOrAddTransition(newState, recall, "Recalling", AnimatorConditionMode.If, 0.05f, 0f, false, 0.99f);
            GetOrAddTransition(recall, newState, "Recalling", AnimatorConditionMode.IfNot, 0.2f, 0f, false);

            //Heavy anuleaza imediat lovitura usoara
            GetOrAddTransition(hitting, newState, "DoubleClick", AnimatorConditionMode.If, 0.08f, 0f, false);

            //Tranzitii mai fine, doar daca sunt inca la valorile originale
            if (Near(hitting.speed, 2f))
                hitting.speed = 1.7f;
            AnimatorStateTransition toHitting = newState.transitions.FirstOrDefault(t => t.destinationState == hitting);
            if (toHitting != null && Near(toHitting.duration, 0.25f))
                toHitting.duration = 0.12f;
            AnimatorStateTransition fromHitting = hitting.transitions.FirstOrDefault(t => t.destinationState == newState && t.hasExitTime && t.conditions.Length == 0);
            if (fromHitting != null)
            {
                if (Near(fromHitting.exitTime, 0.65f))
                    fromHitting.exitTime = 0.7f;
                if (Near(fromHitting.duration, 0.45f))
                    fromHitting.duration = 0.3f;
            }
            AnimatorStateTransition anyToHeavy = baseMachine.anyStateTransitions.FirstOrDefault(t => t.destinationState == heavy);
            if (anyToHeavy != null && Near(anyToHeavy.duration, 0.1f))
                anyToHeavy.duration = 0.15f;
            AnimatorStateTransition heavyToIdle = heavy.transitions.FirstOrDefault(t => t.destinationState == idle && t.hasExitTime && t.conditions.Length == 0);
            if (heavyToIdle != null)
            {
                if (Near(heavyToIdle.exitTime, 0.9f, 0.02f))
                    heavyToIdle.exitTime = 0.85f;
                if (Near(heavyToIdle.duration, 0.15f))
                    heavyToIdle.duration = 0.25f;
            }

            //IK Pass pe toate layerele: LeftHandGrip primeste OnAnimatorIK
            AnimatorControllerLayer[] layers = controller.layers;
            bool ikChanged = false;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].iKPass)
                    continue;
                layers[i].iKPass = true;
                ikChanged = true;
            }
            if (ikChanged)
            {
                controller.layers = layers;
                log.Add("Animator: IK Pass pornit pe layere");
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            log.Add("PlayerAnimation.controller salvat");
        }

        //---------------------------------------------------------------- scena

        private static void SetupScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                log.Add("Scena: sarita (utilizatorul a anulat salvarea scenei curente)");
                return;
            }
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            bool changed = false;

            PlayerCombatState combatState = Object.FindAnyObjectByType<PlayerCombatState>();
            if (combatState == null)
            {
                log.Add("Scena: PlayerCombatState negasit");
            }
            else
            {
                SerializedObject serialized = new SerializedObject(combatState);
                SerializedProperty details = serialized.FindProperty("actionsDetails");
                bool detailsChanged = false;
                for (int i = 0; i < details.arraySize; i++)
                {
                    SerializedProperty element = details.GetArrayElementAtIndex(i);
                    if (element.FindPropertyRelative("action").intValue != (int)CombatAction.Ability)
                        continue;
                    SerializedProperty mode = element.FindPropertyRelative("mode");
                    if (mode.intValue != (int)MoveMode.SlowDown)
                    {
                        mode.intValue = (int)MoveMode.SlowDown;
                        detailsChanged = true;
                    }
                }
                if (detailsChanged)
                {
                    serialized.ApplyModifiedProperties();
                    changed = true;
                    log.Add("Scena: Ability -> SlowDown");
                }
                else
                {
                    log.Add("Scena: Ability era deja SlowDown");
                }
            }

            //Dublu-click dezactivat: fereastra 0 face ca "now - lastClickTime <= window" sa fie mereu fals
            TriggerAttack trigger = Object.FindAnyObjectByType<TriggerAttack>();
            if (trigger == null)
            {
                log.Add("Scena: TriggerAttack negasit");
            }
            else
            {
                SerializedObject serializedTrigger = new SerializedObject(trigger);
                SerializedProperty window = serializedTrigger.FindProperty("doubleClickWindow");
                if (window == null)
                {
                    Debug.LogError("[CombatPolish] Campul 'doubleClickWindow' lipseste pe TriggerAttack");
                }
                else if (!Mathf.Approximately(window.floatValue, 0f))
                {
                    window.floatValue = 0f;
                    serializedTrigger.ApplyModifiedProperties();
                    changed = true;
                    log.Add("Scena: TriggerAttack.doubleClickWindow -> 0 (dublu-click dezactivat)");
                }
                else
                {
                    log.Add("Scena: dublu-click era deja dezactivat");
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
