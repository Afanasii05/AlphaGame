using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ProjectHealthTests
{
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string InputActionsPath =
        "Assets/PlayerInputActions/PlayerActions.inputactions";
    private const string WallPrefabPath =
        "Assets/Weapons/Sword_Unity/Assets/Sword/Stone_Wall.prefab";
    private const string GolemPrefabPath = "Assets/Enemies/StoneGolem/StoneGolem.prefab";
    private const string SwordPrefabPath =
        "Assets/Weapons/Sword_Unity/SwordHammer.prefab";

    [Test]
    public void ProjectHasEnabledBuildScene()
    {
        int enabledScenes = 0;

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
                enabledScenes++;
        }

        Assert.Greater(
            enabledScenes,
            0,
            "Add at least one enabled scene to Build Settings."
        );
    }

    [Test]
    public void EnabledBuildScenesExist()
    {
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled)
                continue;

            Assert.IsTrue(
                File.Exists(scene.path),
                $"Build scene does not exist: {scene.path}"
            );
        }
    }

    [Test]
    public void PrefabsHaveNoMissingScripts()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
                continue;

            int missingScripts =
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab);

            Assert.Zero(
                missingScripts,
                $"{path} has {missingScripts} missing script reference(s)."
            );
        }
    }

    [Test]
    public void ScenesHaveNoMissingScripts()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/", StringComparison.Ordinal))
                continue;

            Scene scene = OpenScene(path);
            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    {
                        int missingScripts =
                            GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                                child.gameObject
                            );

                        Assert.Zero(
                            missingScripts,
                            $"{path}: {child.name} has {missingScripts} missing script reference(s)."
                        );
                    }
                }
            }
            finally
            {
                CloseScene(scene);
            }
        }
    }

    [Test]
    public void AllAssetsHaveMetaFilesAndUniqueGuids()
    {
        string assetsRoot = Application.dataPath;
        Dictionary<string, string> guids = new Dictionary<string, string>();

        foreach (string file in Directory.GetFiles(
                     assetsRoot,
                     "*",
                     SearchOption.AllDirectories
                 ))
        {
            string name = Path.GetFileName(file);
            if (name.StartsWith(".") || file.EndsWith(".meta", StringComparison.Ordinal))
                continue;

            Assert.IsTrue(
                File.Exists(file + ".meta"),
                $"Missing .meta file for {file}"
            );
        }

        foreach (string meta in Directory.GetFiles(
                     assetsRoot,
                     "*.meta",
                     SearchOption.AllDirectories
                 ))
        {
            string assetPath = meta.Substring(0, meta.Length - ".meta".Length);
            Assert.IsTrue(
                File.Exists(assetPath) || Directory.Exists(assetPath),
                $"Orphan .meta file: {meta}"
            );

            Match match = Regex.Match(
                File.ReadAllText(meta),
                @"^guid:\s*([0-9a-f]{32})\s*$",
                RegexOptions.Multiline
            );
            if (!match.Success)
                continue;

            string guid = match.Groups[1].Value;
            Assert.IsFalse(
                guids.ContainsKey(guid),
                $"Duplicate guid {guid} in {guids.GetValueOrDefault(guid)} and {meta}"
            );
            guids[guid] = meta;
        }
    }

    [Test]
    public void InputActionsExposeExpectedBindings()
    {
        InputActionAsset actions =
            AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        Assert.IsNotNull(actions, $"Could not load {InputActionsPath}.");

        AssertBinding(actions, "Player", "Block", "<Keyboard>/f");
        AssertBinding(actions, "Player", "Crouch", "<Keyboard>/leftShift");
        AssertBinding(actions, "Player", "Sprint", "<Keyboard>/leftCtrl");
        AssertBinding(actions, "Player", "Jump", "<Keyboard>/space");
        AssertBinding(actions, "Player", "TogglePerspective", "<Keyboard>/p");

        AssertBinding(actions, "Weapon", "Toggle", "<Keyboard>/e");
        AssertBinding(actions, "Weapon", "Click", "<Mouse>/leftButton");
        AssertBinding(actions, "Weapon", "RightClick", "<Mouse>/rightButton");
        AssertBinding(actions, "Weapon", "Special", "<Keyboard>/z");
        AssertBinding(actions, "Weapon", "Charge", "<Keyboard>/q");
        AssertBinding(actions, "Weapon", "Teleport", "<Keyboard>/t");
    }

    [Test]
    public void PlayerSceneHasRequiredCombatWiring()
    {
        Scene scene = OpenScene(SampleScenePath);
        try
        {
            GameObject player = FindGameObject(scene, "Player");
            Assert.IsNotNull(player, "SampleScene does not contain a Player object.");

            foreach (string typeName in new[]
                     {
                         "PlayerAtributes",
                         "PlayerCombatState",
                         "TriggerAttack",
                         "InputHandler",
                         "PlayerController",
                         "TeleportToWeapon",
                         "ChargeShockwave",
                         "CombatAssist"
                     })
            {
                Assert.IsNotNull(
                    player.GetComponent(typeName),
                    $"Player is missing {typeName}."
                );
            }

            Assert.IsNotNull(
                FindComponentInChildren(player, "EnquipWeapon"),
                "Player children are missing EnquipWeapon."
            );
            Assert.IsNotNull(
                FindComponentInChildren(player, "WeaponSocket"),
                "Player children are missing WeaponSocket."
            );

            Component combatState = player.GetComponent("PlayerCombatState");
            SerializedObject state = new SerializedObject(combatState);
            SerializedProperty actions = state.FindProperty("actionsDetails");
            Assert.IsNotNull(actions, "PlayerCombatState has no actionsDetails.");

            Dictionary<int, SerializedProperty> byAction =
                new Dictionary<int, SerializedProperty>();
            for (int i = 0; i < actions.arraySize; i++)
            {
                SerializedProperty entry = actions.GetArrayElementAtIndex(i);
                int action = entry.FindPropertyRelative("action").intValue;
                Assert.IsFalse(
                    byAction.ContainsKey(action),
                    $"Duplicate player action definition: {(PlayerActionIds)action}"
                );
                byAction[action] = entry;
            }

            AssertPlayerAction(byAction, PlayerActionIds.Light);
            AssertPlayerAction(byAction, PlayerActionIds.Heavy);
            AssertPlayerAction(byAction, PlayerActionIds.Throw);
            AssertPlayerAction(byAction, PlayerActionIds.Ability);
            AssertPlayerAction(byAction, PlayerActionIds.Charge);
            AssertPlayerAction(byAction, PlayerActionIds.Recall);
            AssertPlayerAction(byAction, PlayerActionIds.Teleport);
            AssertPlayerAction(byAction, PlayerActionIds.Block);
            AssertPlayerAction(byAction, PlayerActionIds.HeavyFall);
            AssertPlayerAction(byAction, PlayerActionIds.GetHit);
            AssertPlayerAction(byAction, PlayerActionIds.Death);

            Assert.IsTrue(
                byAction[(int)PlayerActionIds.Heavy]
                    .FindPropertyRelative("requiresGrounded")
                    .boolValue,
                "Heavy must require the player to be grounded."
            );
            Assert.IsFalse(
                byAction[(int)PlayerActionIds.HeavyFall]
                    .FindPropertyRelative("requiresGrounded")
                    .boolValue,
                "HeavyFall must work while airborne."
            );
            Assert.Greater(
                byAction[(int)PlayerActionIds.Heavy]
                    .FindPropertyRelative("damage")
                    .floatValue,
                0f,
                "Heavy must deal damage."
            );
            Assert.Greater(
                byAction[(int)PlayerActionIds.HeavyFall]
                    .FindPropertyRelative("damage")
                    .floatValue,
                0f,
                "HeavyFall must deal damage."
            );
            Assert.AreEqual(
                "Block",
                byAction[(int)PlayerActionIds.Block]
                    .FindPropertyRelative("trigger")
                    .stringValue,
                "Block must use the Block animator trigger."
            );

            Animator animator = player.GetComponentInChildren<Animator>(true);
            Assert.IsNotNull(animator, "Player has no Animator.");
            AnimatorController controller =
                animator.runtimeAnimatorController as AnimatorController;
            Assert.IsNotNull(controller, "Player Animator has no controller.");

            HashSet<string> parameters =
                new HashSet<string>(controller.parameters.Select(parameter => parameter.name));
            foreach (SerializedProperty action in byAction.Values)
            {
                string trigger = action.FindPropertyRelative("trigger").stringValue;
                if (string.IsNullOrWhiteSpace(trigger))
                    continue;
                Assert.IsTrue(
                    parameters.Contains(trigger),
                    $"Player animator is missing trigger parameter '{trigger}'."
                );
            }
        }
        finally
        {
            CloseScene(scene);
        }
    }

    [TestCase("Assets/Enemies/Stumphorn/Stumphorn.prefab", "EnemyAtributes", false)]
    [TestCase("Assets/Enemies/Shacklewraith/Shacklewraith.prefab", "EnemyAtributes", false)]
    [TestCase("Assets/Enemies/StoneSentinel/StoneSentinel.prefab", "AllyAtributes", true)]
    [TestCase(GolemPrefabPath, "AllyAtributes", true)]
    public void EnemyPrefabsHaveHealthCombatAndAnimations(
        string prefabPath,
        string attributesType,
        bool isAlly
    )
    {
        GameObject prefab = LoadPrefab(prefabPath);

        Component attributes = prefab.GetComponent(attributesType);
        Assert.IsNotNull(
            attributes,
            $"{prefabPath} is missing {attributesType}."
        );

        SerializedObject attributeData = new SerializedObject(attributes);
        Assert.IsNotNull(
            attributeData.FindProperty("healthLight").objectReferenceValue,
            $"{prefabPath} has no healthLight assigned."
        );
        Assert.IsNotNull(
            attributeData.FindProperty("deathSmoke").objectReferenceValue,
            $"{prefabPath} has no deathSmoke assigned."
        );
        Assert.Greater(
            attributeData.FindProperty("Max_Health").floatValue,
            0f,
            $"{prefabPath} must have positive Max_Health."
        );

        Component ai = prefab.GetComponent("EnemyAI");
        Assert.IsNotNull(ai, $"{prefabPath} is missing EnemyAI.");
        SerializedObject aiData = new SerializedObject(ai);
        Assert.AreEqual(
            isAlly,
            aiData.FindProperty("Ally").boolValue,
            $"{prefabPath} has the wrong Ally flag."
        );

        Component combatState = prefab.GetComponent("EnemyCombatState");
        Assert.IsNotNull(
            combatState,
            $"{prefabPath} is missing EnemyCombatState."
        );

        SerializedObject stateData = new SerializedObject(combatState);
        SerializedProperty actions = stateData.FindProperty("actionsDetails");
        Assert.IsNotNull(actions, $"{prefabPath} has no actionsDetails.");

        Dictionary<int, SerializedProperty> byAction =
            new Dictionary<int, SerializedProperty>();
        for (int i = 0; i < actions.arraySize; i++)
        {
            SerializedProperty entry = actions.GetArrayElementAtIndex(i);
            byAction[entry.FindPropertyRelative("action").intValue] = entry;
        }

        Assert.IsTrue(
            byAction.ContainsKey((int)EnemyActionIds.GetHit),
            $"{prefabPath} has no GetHit action."
        );
        Assert.IsTrue(
            byAction.ContainsKey((int)EnemyActionIds.Death),
            $"{prefabPath} has no Death action."
        );
        Assert.IsTrue(
            byAction.ContainsKey((int)EnemyActionIds.Attack),
            $"{prefabPath} has no Attack action."
        );

        foreach (SerializedProperty action in byAction.Values)
        {
            int id = action.FindPropertyRelative("action").intValue;
            float damage = action.FindPropertyRelative("damage").floatValue;
            bool damagingMelee =
                damage > 0f
                && (id == (int)EnemyActionIds.Attack
                    || id == (int)EnemyActionIds.Stomp
                    || id == (int)EnemyActionIds.Roll
                    || id == (int)EnemyActionIds.Spin);

            if (!damagingMelee)
                continue;

            Assert.Greater(
                action.FindPropertyRelative("hitboxes").arraySize,
                0,
                $"{prefabPath}: damaging action {(EnemyActionIds)id} has no hitboxes."
            );
        }

        Animator animator = prefab.GetComponentInChildren<Animator>(true);
        Assert.IsNotNull(animator, $"{prefabPath} has no Animator.");
        Assert.IsNotNull(
            animator.runtimeAnimatorController,
            $"{prefabPath} has no AnimatorController assigned."
        );

        string controllerPath = AssetDatabase.GetAssetPath(
            animator.runtimeAnimatorController
        );
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        Assert.IsNotNull(controller, $"Could not load controller for {prefabPath}.");

        HashSet<string> parameters =
            new HashSet<string>(controller.parameters.Select(parameter => parameter.name));
        foreach (string required in new[] { "Attack", "Hit", "Death" })
        {
            Assert.IsTrue(
                parameters.Contains(required),
                $"{controllerPath} is missing parameter '{required}'."
            );
        }
    }

    [Test]
    public void WallAndGolemPrefabsAreWired()
    {
        GameObject wall = LoadPrefab(WallPrefabPath);
        Component blockWall = wall.GetComponent("BlockWall");
        Assert.IsNotNull(blockWall, "Stone_Wall.prefab is missing BlockWall.");

        SerializedObject wallData = new SerializedObject(blockWall);
        GameObject golemPrefab =
            wallData.FindProperty("golemPrefab").objectReferenceValue as GameObject;
        Assert.IsNotNull(
            golemPrefab,
            "BlockWall does not reference the golem prefab."
        );
        Assert.IsNotNull(
            golemPrefab.GetComponent("GolemStonesSlots"),
            "The wall's golem prefab is missing GolemStonesSlots."
        );
        Assert.AreNotEqual(
            0,
            wallData.FindProperty("groundMask").intValue,
            "BlockWall has no ground mask."
        );
        Assert.Greater(
            wall.transform.childCount,
            31,
            "BlockWall needs more than 31 stones to reach the golem build threshold."
        );

        GameObject golem = LoadPrefab(GolemPrefabPath);
        Assert.IsNotNull(
            golem.GetComponent("GolemStonesSlots"),
            "StoneGolem.prefab is missing GolemStonesSlots."
        );
        Assert.IsNotNull(
            golem.GetComponent("AllyAtributes"),
            "StoneGolem.prefab is missing AllyAtributes."
        );
        Assert.IsNotNull(
            golem.GetComponent("GolemController"),
            "StoneGolem.prefab is missing GolemController."
        );

        SerializedObject aiData = new SerializedObject(golem.GetComponent("EnemyAI"));
        Assert.IsTrue(
            aiData.FindProperty("Ally").boolValue,
            "The golem must be flagged as an ally."
        );
    }

    [Test]
    public void SwordPrefabHasHitDetectionWiring()
    {
        GameObject sword = LoadPrefab(SwordPrefabPath);

        Component weapon = sword.GetComponent("Weapon");
        Assert.IsNotNull(weapon, "SwordHammer.prefab is missing Weapon.");

        SerializedObject weaponData = new SerializedObject(weapon);
        Assert.IsNotNull(
            weaponData.FindProperty("<headHitbox>k__BackingField").objectReferenceValue,
            "Weapon has no headHitbox."
        );
        Assert.IsNotNull(
            weaponData.FindProperty("impactPoint1").objectReferenceValue,
            "Weapon has no impactPoint1."
        );
        Assert.IsNotNull(
            weaponData.FindProperty("impactPoint2").objectReferenceValue,
            "Weapon has no impactPoint2."
        );

        Component heavyImpact = FindComponentInChildren(sword, "HeavyImpactDetection");
        Assert.IsNotNull(
            heavyImpact,
            "SwordHammer.prefab is missing HeavyImpactDetection."
        );

        SerializedObject impactData = new SerializedObject(heavyImpact);
        foreach (string field in new[] { "wp", "cr", "mirageAttack" })
        {
            Assert.IsNotNull(
                impactData.FindProperty(field).objectReferenceValue,
                $"HeavyImpactDetection has no '{field}' reference."
            );
        }
        Assert.AreNotEqual(
            0,
            impactData.FindProperty("impactMask").intValue,
            "HeavyImpactDetection has no impact mask."
        );

        Component playerHit = FindComponentInChildren(sword, "PlayerHitEnemy");
        Assert.IsNotNull(playerHit, "SwordHammer.prefab is missing PlayerHitEnemy.");

        SerializedObject hitData = new SerializedObject(playerHit);
        foreach (string field in new[] { "hitbox", "hitParticlesPrefab" })
        {
            Assert.IsNotNull(
                hitData.FindProperty(field).objectReferenceValue,
                $"PlayerHitEnemy has no '{field}' reference."
            );
        }
        Assert.AreNotEqual(
            0,
            hitData.FindProperty("enemyLayer").intValue,
            "PlayerHitEnemy has no enemy layer mask."
        );
    }

    private enum PlayerActionIds
    {
        None = 0,
        Light = 1,
        Heavy = 2,
        Throw = 3,
        Ability = 4,
        Charge = 10,
        Recall = 12,
        Teleport = 13,
        Block = 14,
        HeavyFall = 15,
        GetHit = 8,
        Death = 16
    }

    private enum EnemyActionIds
    {
        Attack = 1,
        GetHit = 2,
        Stomp = 3,
        Roll = 4,
        Death = 5,
        Special = 6,
        Spin = 8
    }

    private static void AssertBinding(
        InputActionAsset actions,
        string mapName,
        string actionName,
        string expectedPath
    )
    {
        InputActionMap map = actions.FindActionMap(mapName, false);
        Assert.IsNotNull(map, $"Input action map '{mapName}' is missing.");

        InputAction action = map.FindAction(actionName, false);
        Assert.IsNotNull(action, $"Input action '{mapName}/{actionName}' is missing.");
        Assert.IsTrue(
            action.bindings.Any(binding => binding.path == expectedPath),
            $"Input action '{mapName}/{actionName}' is not bound to {expectedPath}."
        );
    }

    private static void AssertPlayerAction(
        Dictionary<int, SerializedProperty> actions,
        PlayerActionIds action
    )
    {
        Assert.IsTrue(
            actions.ContainsKey((int)action),
            $"Player is missing action definition {action}."
        );
    }

    private static GameObject LoadPrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.IsNotNull(prefab, $"Could not load prefab: {path}");
        return prefab;
    }

    private static Scene OpenScene(string path)
    {
        Scene scene = SceneManager.GetSceneByPath(path);
        if (scene.IsValid() && scene.isLoaded)
            return scene;

        return EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
    }

    private static void CloseScene(Scene scene)
    {
        if (scene.IsValid()
            && scene.isLoaded
            && scene != SceneManager.GetActiveScene())
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static GameObject FindGameObject(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
                return root;

            Transform child = root
                .GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => transform.name == name);
            if (child != null)
                return child.gameObject;
        }

        return null;
    }

    private static Component FindComponentInChildren(GameObject root, string typeName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            Component component = child.GetComponent(typeName);
            if (component != null)
                return component;
        }

        return null;
    }
}
