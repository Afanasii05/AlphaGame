using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class GameplayTests
{
    [UnityTest]
    public IEnumerator FirstBuildSceneLoads()
    {
        Assert.Greater(
            SceneManager.sceneCountInBuildSettings,
            0,
            "Add at least one scene to Build Settings."
        );

        yield return SceneManager.LoadSceneAsync(0);
        yield return null;

        Scene scene = SceneManager.GetActiveScene();

        Assert.IsTrue(scene.isLoaded);
        Assert.IsFalse(string.IsNullOrWhiteSpace(scene.name));
    }

    [UnityTest]
    public IEnumerator FirstBuildSceneHasActiveCamera()
    {
        Assert.Greater(
            SceneManager.sceneCountInBuildSettings,
            0,
            "Add at least one scene to Build Settings."
        );

        yield return SceneManager.LoadSceneAsync(0);
        yield return null;

        Assert.IsNotNull(
            Camera.main,
            "The first build scene needs an active camera tagged MainCamera."
        );
    }

    [UnityTest]
    public IEnumerator FirstBuildSceneLoadsWithoutUnexpectedErrors()
    {
        LogAssert.NoUnexpectedReceived();

        yield return SceneManager.LoadSceneAsync(0);
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator PlayerCombatDefinitionsAreLoaded()
    {
        yield return LoadFirstScene();

        GameObject player = GameObject.Find("Player");
        Assert.IsNotNull(player, "The build scene has no Player object.");

        Component combatState = FindComponent(player, "PlayerCombatState");
        Assert.IsNotNull(
            combatState,
            "The Player has no PlayerCombatState component."
        );

        IDictionary definitions = GetFieldValue(combatState, "combatDef") as IDictionary;
        Assert.IsNotNull(
            definitions,
            "PlayerCombatState.combatDef was not populated in Awake."
        );

        HashSet<string> actions = new HashSet<string>();
        foreach (DictionaryEntry definition in definitions)
            actions.Add(definition.Key.ToString());

        foreach (string required in new[]
                 {
                     "Light",
                     "Heavy",
                     "HeavyFall",
                     "Throw",
                     "Ability",
                     "Charge",
                     "Recall",
                     "Teleport",
                     "Block",
                     "GetHit",
                     "Death"
                 })
        {
            Assert.IsTrue(
                actions.Contains(required),
                $"Player action '{required}' was not loaded at runtime."
            );
        }
    }

    [UnityTest]
    public IEnumerator EnemiesAndAlliesRegisterAndHaveHealthLights()
    {
        yield return LoadFirstScene();

        Type hittableType = Type.GetType(
            "Alpha.Interfaces.HittableCharacter, Assembly-CSharp"
        );
        Assert.IsNotNull(hittableType, "Could not find HittableCharacter runtime type.");

        IList enemies = GetStaticField(hittableType, "enemies") as IList;
        IList allies = GetStaticField(hittableType, "allys") as IList;
        Assert.IsNotNull(enemies, "HittableCharacter.enemies is missing.");
        Assert.IsNotNull(allies, "HittableCharacter.allys is missing.");

        List<object> livingEnemies = AliveObjects(enemies);
        List<object> livingAllies = AliveObjects(allies);

        Assert.Greater(livingEnemies.Count, 0, "The scene registered no enemies.");
        Assert.Greater(livingAllies.Count, 0, "The scene registered no allies.");

        int enemiesWithLight = 0;
        foreach (object enemy in livingEnemies)
        {
            Light light = GetFieldValue(enemy, "healthLight") as Light;
            if (light == null)
                continue;

            enemiesWithLight++;
            Assert.IsTrue(
                light.color.r > 0f || light.color.g > 0f || light.color.b > 0f,
                "Enemy health light is black at full health."
            );
        }

        Assert.Greater(
            enemiesWithLight,
            0,
            "No enemy has a health light assigned."
        );
    }

    [UnityTest]
    public IEnumerator EnemyTakesHitAndHealthGlowChanges()
    {
        yield return LoadFirstScene();

        Type hittableType = Type.GetType(
            "Alpha.Interfaces.HittableCharacter, Assembly-CSharp"
        );
        Assert.IsNotNull(hittableType, "Could not find HittableCharacter runtime type.");

        IList enemies = GetStaticField(hittableType, "enemies") as IList;
        object enemy = AliveObjects(enemies).FirstOrDefault();
        Assert.IsNotNull(enemy, "The scene has no living enemy to test.");

        Light healthLight = GetFieldValue(enemy, "healthLight") as Light;
        Assert.IsNotNull(healthLight, "The test enemy has no health light.");

        GameObject player = GameObject.FindWithTag("Player");
        Assert.IsNotNull(player, "The scene has no Player.");
        Component playerAttributes = player.GetComponent("PlayerAtributes");
        Assert.IsNotNull(playerAttributes, "The Player has no PlayerAtributes.");

        Type attackType = Type.GetType(
            "Alpha.Combat.PlayerActionDefinition, Assembly-CSharp"
        );
        Assert.IsNotNull(attackType, "Could not find PlayerActionDefinition type.");
        object attack = Activator.CreateInstance(attackType);
        SetFieldValue(attack, "damage", 50f);
        SetFieldValue(attack, "knockBackPower", 0f);

        // Make sure the cooldown from a previous scene cannot swallow the hit.
        SetFieldValue(enemy, "lastKnockBackTime", Time.time - 10f);
        SetFieldValue(enemy, "isInKnockBack", false);

        Color before = healthLight.color;
        MethodInfo handleGotHit = hittableType.GetMethod("HandleGotHit");
        Assert.IsNotNull(handleGotHit, "HittableCharacter.HandleGotHit is missing.");

        handleGotHit.Invoke(enemy, new[] { attack, playerAttributes });
        yield return null;

        Assert.AreNotEqual(
            before,
            healthLight.color,
            "Health light colour did not change after damage."
        );
    }

    [UnityTest]
    public IEnumerator BlockSpawnsWall()
    {
        yield return LoadFirstScene();
        yield return WaitForGrounded();

        GameObject player = GameObject.Find("Player");
        Assert.IsNotNull(player, "The scene has no Player.");

        Component trigger = FindComponent(player, "TriggerAttack");
        Assert.IsNotNull(trigger, "The Player has no TriggerAttack.");

        Type wallType = Type.GetType("BlockWall, Assembly-CSharp");
        Assert.IsNotNull(wallType, "Could not find BlockWall runtime type.");

        InvokePrivate(trigger, "HandleBlock", true);

        bool spawned = false;
        for (int frame = 0; frame < 180; frame++)
        {
            if (UnityEngine.Object.FindObjectsByType(
                    wallType,
                    FindObjectsSortMode.None
                ).Length > 0)
            {
                spawned = true;
                break;
            }
            yield return null;
        }

        InvokePrivate(trigger, "HandleBlock", false);

        Assert.IsTrue(
            spawned,
            "Holding F did not create a BlockWall within 3 seconds."
        );
    }

    [UnityTest]
    public IEnumerator RightClickStartsHeavyAttack()
    {
        yield return LoadFirstScene();
        yield return WaitForGrounded();

        GameObject player = GameObject.Find("Player");
        Assert.IsNotNull(player, "The scene has no Player.");

        Component trigger = FindComponent(player, "TriggerAttack");
        Component combatState = FindComponent(player, "PlayerCombatState");
        Assert.IsNotNull(trigger, "The Player has no TriggerAttack.");
        Assert.IsNotNull(combatState, "The Player has no PlayerCombatState.");

        Component socket = FindComponent(player, "WeaponSocket");
        Assert.IsNotNull(socket, "The Player has no WeaponSocket.");
        Assert.IsTrue(
            (bool)GetPropertyValue(socket, "HasWeapon"),
            "The debug start weapon was not equipped."
        );

        InvokePrivate(trigger, "HandleRightClickTrigger");
        yield return null;

        object action = GetPropertyValue(combatState, "currentAction");
        Assert.AreEqual(
            "Heavy",
            action.ToString(),
            "Right click should start the grounded Heavy attack."
        );
    }

    private static IEnumerator LoadFirstScene()
    {
        Assert.Greater(
            SceneManager.sceneCountInBuildSettings,
            0,
            "Add at least one scene to Build Settings."
        );

        yield return SceneManager.LoadSceneAsync(0);
        yield return null;
        yield return new WaitForSeconds(0.5f);
    }

    private static IEnumerator WaitForGrounded()
    {
        GameObject player = GameObject.Find("Player");
        Assert.IsNotNull(player, "The scene has no Player.");
        Component combatState = FindComponent(player, "PlayerCombatState");
        Assert.IsNotNull(combatState, "The Player has no PlayerCombatState.");

        for (int frame = 0; frame < 180; frame++)
        {
            if ((bool)GetPropertyValue(combatState, "Grounded"))
                yield break;
            yield return null;
        }

        Assert.Fail("The player never became grounded.");
    }

    private static List<object> AliveObjects(IList source)
    {
        List<object> result = new List<object>();
        if (source == null)
            return result;

        foreach (object item in source)
        {
            if (item is UnityEngine.Object unityObject && unityObject != null)
                result.Add(item);
        }

        return result;
    }

    private static object GetStaticField(Type type, string name)
    {
        FieldInfo field = type.GetField(
            name,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
        );
        Assert.IsNotNull(field, $"{type.Name}.{name} is missing.");
        return field.GetValue(null);
    }

    private static object GetFieldValue(object target, string name)
    {
        if (target == null)
            return null;

        Type type = target.GetType();
        while (type != null)
        {
            FieldInfo field = type.GetField(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            if (field != null)
                return field.GetValue(target);
            type = type.BaseType;
        }

        return null;
    }

    private static void SetFieldValue(object target, string name, object value)
    {
        Type type = target.GetType();
        while (type != null)
        {
            FieldInfo field = type.GetField(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }
            type = type.BaseType;
        }

        Assert.Fail($"Could not find field '{name}' on {target.GetType().Name}.");
    }

    private static object GetPropertyValue(object target, string name)
    {
        Type type = target.GetType();
        while (type != null)
        {
            PropertyInfo property = type.GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            if (property != null)
                return property.GetValue(target);
            type = type.BaseType;
        }

        Assert.Fail($"Could not find property '{name}' on {target.GetType().Name}.");
        return null;
    }

    private static object InvokePrivate(
        object target,
        string methodName,
        params object[] arguments
    )
    {
        Type type = target.GetType();
        while (type != null)
        {
            MethodInfo method = type.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            if (method != null)
                return method.Invoke(target, arguments);
            type = type.BaseType;
        }

        Assert.Fail($"Could not find method '{methodName}' on {target.GetType().Name}.");
        return null;
    }

    private static Component FindComponent(GameObject root, string typeName)
    {
        foreach (Component component in root.GetComponents<Component>())
        {
            if (component != null && component.GetType().Name == typeName)
                return component;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            foreach (Component component in child.GetComponents<Component>())
            {
                if (component != null && component.GetType().Name == typeName)
                    return component;
            }
        }

        return null;
    }
}
