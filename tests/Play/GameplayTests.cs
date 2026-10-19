using System.Collections;
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
}
