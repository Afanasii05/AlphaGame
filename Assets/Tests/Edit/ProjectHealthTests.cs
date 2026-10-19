using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class ProjectHealthTests
{
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
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in prefabGuids)
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
}
