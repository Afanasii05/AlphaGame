using System.Collections.Generic;
using Alpha.Combat;
using Alpha.Interfaces;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alpha.World.EditorTools
{
    // Pune modelul rafinat pe jucatorul activ din World, ca varianta comutabila pe scheletul existent,
    // plus scripturile de feel. Doar override-uri de scena: prefab-urile echipei raman neatinse.
    // Acelasi setup pe o copie a PlayerRig-ului: Player_Refined.prefab. Rulat de mai multe ori da acelasi rezultat.
    public static class PlayerSetup
    {
        const string WorldScenePath = "Assets/Scenes/World.unity";
        const string TeamPrefabPath = "Assets/Prefabs/PlayerRig.prefab";
        const string PrefabFolder = PlayerArtSetup.Folder + "/Prefabs";
        const string PrefabPath = PrefabFolder + "/Player_Refined.prefab";
        const string DustMaterialPath = "Assets/World/Arena1/Art/FX/M_FX_Dust.mat";
        const string OriginalBody = "Character_Body";
        public const string RefinedBody = "Character_Body_Refined";

        [MenuItem("Tools/Alpha/World/Setup Player")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[PlayerSetup] Opreste Play inainte de setup.");
                return;
            }
            if (PlayerArtSetup.Run() == null) return;

            Scene world = OpenWorld();
            if (!world.IsValid()) return;
            GameObject player = FindPlayer();
            if (player == null)
            {
                Debug.LogError("[PlayerSetup] Nu gasesc jucatorul activ in World.");
                return;
            }
            PlayerSkin existing = player.GetComponent<PlayerSkin>();
            PlayerSkin.Variant variant = existing != null ? existing.Current : PlayerSkin.Variant.Refined;
            if (!Apply(player, variant, true)) return;
            EditorSceneManager.MarkSceneDirty(world);
            EditorSceneManager.SaveScene(world);

            BuildPrefab();
            AssetDatabase.SaveAssets();
            Debug.Log($"[PlayerSetup] Gata. Jucator: {Path(player.transform)}, varianta {variant}, prefab {PrefabPath}.");
        }

        [MenuItem("Tools/Alpha/World/Player Skin/Refined")]
        static void UseRefined() => Switch(PlayerSkin.Variant.Refined);

        [MenuItem("Tools/Alpha/World/Player Skin/Original")]
        static void UseOriginal() => Switch(PlayerSkin.Variant.Original);

        public static void Switch(PlayerSkin.Variant variant)
        {
            if (EditorApplication.isPlaying)
            {
                // in Play doar comutam pe loc, fara salvare
                foreach (PlayerSkin live in Object.FindObjectsByType<PlayerSkin>()) live.Use(variant);
                return;
            }
            Scene world = OpenWorld();
            if (!world.IsValid()) return;
            GameObject player = FindPlayer();
            PlayerSkin skin = player != null ? player.GetComponent<PlayerSkin>() : null;
            if (skin == null)
            {
                Debug.LogWarning("[PlayerSetup] Jucatorul nu are PlayerSkin: ruleaza intai Setup Player.");
                return;
            }
            Undo.RecordObject(skin, "Player Skin");
            if (skin.RefinedBody != null) Undo.RecordObject(skin.RefinedBody, "Player Skin");
            Renderer original = FindBody(player.GetComponentInChildren<Animator>(true).transform, OriginalBody);
            if (original != null) Undo.RecordObject(original, "Player Skin");
            skin.Use(variant);
            EditorUtility.SetDirty(skin);
            EditorSceneManager.MarkSceneDirty(world);
            EditorSceneManager.SaveScene(world);
            Debug.Log($"[PlayerSetup] Corpul jucatorului: {variant}.");
        }

        // ------------------------------------------------------------------ scena si prefab

        static Scene OpenWorld()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == WorldScenePath) return active;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return default(Scene);
            Scene world = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);
            SceneManager.SetActiveScene(world);
            return world;
        }

        static GameObject FindPlayer()
        {
            foreach (PlayerCombatState state in Object.FindObjectsByType<PlayerCombatState>(FindObjectsInactive.Exclude))
                if (state.CompareTag("Player")) return state.gameObject;
            return null;
        }

        static void BuildPrefab()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder(PlayerArtSetup.Folder, "Prefabs");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null && !AssetDatabase.CopyAsset(TeamPrefabPath, PrefabPath))
            {
                Debug.LogError($"[PlayerSetup] Nu pot copia {TeamPrefabPath}.");
                return;
            }
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                PlayerCombatState state = root.GetComponentInChildren<PlayerCombatState>(true);
                if (state == null) Debug.LogError("[PlayerSetup] Copia PlayerRig nu are PlayerCombatState.");
                else if (Apply(state.gameObject, PlayerSkin.Variant.Refined, false)) PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ------------------------------------------------------------------ setup pe un jucator

        static bool Apply(GameObject player, PlayerSkin.Variant variant, bool undo)
        {
            Animator animator = player.GetComponentInChildren<Animator>(true);
            SkinnedMeshRenderer original = animator != null ? FindBody(animator.transform, OriginalBody) : null;
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerArtSetup.ModelPath);
            SkinnedMeshRenderer source = model != null ? model.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
            if (original == null || source == null)
            {
                Debug.LogError($"[PlayerSetup] Lipseste {(original == null ? OriginalBody + " sub " + player.name : PlayerArtSetup.ModelPath)}.");
                return false;
            }
            Transform character = animator.transform;
            SkinnedMeshRenderer refined = BuildRefinedBody(character, original, source, undo);
            if (refined == null) return false;

            PlayerSkin skin = Ensure<PlayerSkin>(player, undo);
            skin.Setup(refined, new Renderer[] { original });
            skin.Use(variant);
            EditorUtility.SetDirty(skin);
            EditorUtility.SetDirty(original);

            Dictionary<string, Transform> bones = Skeleton(character);
            PlayerCombatState combat = player.GetComponent<PlayerCombatState>();

            PlayerPresence presence = Ensure<PlayerPresence>(player, undo);
            Set(presence, "combat", combat);
            Set(presence, "body", character);
            Set(presence, "head", Bone(bones, "mixamorig:Head"));
            Set(presence, "back", Bone(bones, "mixamorig:Spine"));
            Set(presence, "eyeSource", original);
            // lumini mici si locale: ochii si sigiliul nu trebuie sa spele tot corpul
            SetFloat(presence, "eyeIntensity", 0.6f);
            SetFloat(presence, "eyeRange", 1.2f);
            SetFloat(presence, "sigilIntensity", 0.16f);
            SetFloat(presence, "sigilRange", 0.8f);

            PlayerDust dust = Ensure<PlayerDust>(player, undo);
            Set(dust, "combat", combat);
            Set(dust, "body", player.GetComponent<Rigidbody>());
            Set(dust, "leftFoot", Bone(bones, "mixamorig:LeftFoot"));
            Set(dust, "rightFoot", Bone(bones, "mixamorig:RightFoot"));
            Set(dust, "material", AssetDatabase.LoadAssetAtPath<Material>(DustMaterialPath));
            SetFloat(dust, "plantHeight", 0.45f);
            SetFloat(dust, "strideLength", 1.2f);
            SetFloat(dust, "alpha", 0.55f);
            SetFloat(dust, "lighten", 0.4f);
            SetFloat(dust, "softFade", 0.25f);

            HammerCatchFx catchFx = Ensure<HammerCatchFx>(player, undo);
            WeaponSocket socket = player.GetComponentInChildren<WeaponSocket>(true);
            Set(catchFx, "hand", socket != null ? socket.transform : Bone(bones, "mixamorig:RightHand"));

            DamageVignette vignette = Ensure<DamageVignette>(player, undo);
            Set(vignette, "target", player.GetComponent<HittableCharacter>());
            return true;
        }

        // corpul rafinat e copilul lui MainCharacter, legat pe nume de oasele scheletului existent;
        // numele difera de Character_Body ca animatiile de cutscene pe acel corp sa nu-l atinga
        static SkinnedMeshRenderer BuildRefinedBody(Transform character, SkinnedMeshRenderer original, SkinnedMeshRenderer source, bool undo)
        {
            Dictionary<string, Transform> skeleton = Skeleton(character);
            Transform[] bones = new Transform[source.bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                Transform bone = source.bones[i] != null ? Bone(skeleton, source.bones[i].name) : null;
                if (bone == null)
                {
                    Debug.LogError($"[PlayerSetup] Osul {(source.bones[i] != null ? source.bones[i].name : "#" + i)} lipseste din scheletul jucatorului.");
                    return null;
                }
                bones[i] = bone;
            }
            Transform root = source.rootBone != null ? Bone(skeleton, source.rootBone.name) : original.rootBone;

            Transform body = character.Find(RefinedBody);
            if (body == null)
            {
                body = new GameObject(RefinedBody).transform;
                if (undo) Undo.RegisterCreatedObjectUndo(body.gameObject, "Setup Player");
                body.SetParent(character, false);
            }
            body.localPosition = source.transform.localPosition;
            body.localRotation = source.transform.localRotation;
            body.localScale = source.transform.localScale;
            body.gameObject.layer = original.gameObject.layer;
            body.gameObject.tag = original.gameObject.tag;

            SkinnedMeshRenderer smr = Ensure<SkinnedMeshRenderer>(body.gameObject, undo);
            smr.sharedMesh = source.sharedMesh;
            smr.sharedMaterials = source.sharedMaterials;
            smr.bones = bones;
            smr.rootBone = root;
            smr.localBounds = original.localBounds;
            smr.updateWhenOffscreen = original.updateWhenOffscreen;
            smr.quality = original.quality;
            smr.skinnedMotionVectors = original.skinnedMotionVectors;
            smr.shadowCastingMode = original.shadowCastingMode;
            smr.receiveShadows = original.receiveShadows;
            smr.lightProbeUsage = original.lightProbeUsage;
            smr.reflectionProbeUsage = original.reflectionProbeUsage;
            smr.renderingLayerMask = original.renderingLayerMask;
            EditorUtility.SetDirty(smr);
            return smr;
        }

        // ------------------------------------------------------------------ utilitare

        static SkinnedMeshRenderer FindBody(Transform character, string name)
        {
            foreach (SkinnedMeshRenderer smr in character.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (smr.name == name) return smr;
            return null;
        }

        static Dictionary<string, Transform> Skeleton(Transform character)
        {
            Dictionary<string, Transform> map = new Dictionary<string, Transform>();
            foreach (Transform t in character.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("mixamorig:") && !map.ContainsKey(t.name)) map[t.name] = t;
            return map;
        }

        static Transform Bone(Dictionary<string, Transform> skeleton, string name)
        {
            Transform bone;
            return skeleton.TryGetValue(name, out bone) ? bone : null;
        }

        static T Ensure<T>(GameObject go, bool undo) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component != null)
            {
                if (undo) Undo.RecordObject(component, "Setup Player");
                return component;
            }
            return undo ? Undo.AddComponent<T>(go) : go.AddComponent<T>();
        }

        static void Set(Object target, string field, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[PlayerSetup] {target.GetType().Name} nu are campul {field}.");
                return;
            }
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Object target, string field, float value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[PlayerSetup] {target.GetType().Name} nu are campul {field}.");
                return;
            }
            property.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    }
}
