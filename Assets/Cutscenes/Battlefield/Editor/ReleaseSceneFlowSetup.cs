using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace Alpha.Cutscenes.EditorTools
{
    // Configureaza flow-ul de release: Death Valley (cinematica) -> World (joc).
    // Ruleaza si din meniu, si din batch mode cu -executeMethod.
    public static class ReleaseSceneFlowSetup
    {
        const string DeathValleyScenePath = "Assets/Scenes/Death Valley.unity";
        const string WorldScenePath = "Assets/Scenes/World.unity";
        const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Tools/Alpha/World/Setup Release Scene Flow")]
        public static void Run()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (!File.Exists(DeathValleyScenePath) || !File.Exists(WorldScenePath))
            {
                Debug.LogError("[ReleaseSceneFlow] Death Valley or World scene is missing.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(DeathValleyScenePath, OpenSceneMode.Single);

            PlayableDirector director = Object.FindAnyObjectByType<PlayableDirector>(
                FindObjectsInactive.Include
            );
            if (director == null)
            {
                Debug.LogError("[ReleaseSceneFlow] Death Valley has no PlayableDirector.");
                return;
            }

            CutsceneSceneLoader loader = director.GetComponent<CutsceneSceneLoader>();
            if (loader == null) loader = director.gameObject.AddComponent<CutsceneSceneLoader>();

            SerializedObject serialized = new SerializedObject(loader);
            serialized.FindProperty("nextScene").stringValue = "World";
            serialized.FindProperty("delay").floatValue = 0.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Camera principala e necesara pentru Camera.main in cinematic si in build.
            Camera camera = Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
            if (camera != null && !camera.CompareTag("MainCamera"))
                camera.tag = "MainCamera";

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(DeathValleyScenePath, true),
                new EditorBuildSettingsScene(WorldScenePath, true),
                new EditorBuildSettingsScene(SampleScenePath, false),
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[ReleaseSceneFlow] Build order: Death Valley -> World. "
                    + "CutsceneSceneLoader added to Battlefield_Cutscene."
            );
        }

        // -batchmode -nographics -projectPath . -executeMethod Alpha.Cutscenes.EditorTools.ReleaseSceneFlowSetup.SetupFromCommandLine
        public static void SetupFromCommandLine()
        {
            try
            {
                Run();
                EditorApplication.Exit(0);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
