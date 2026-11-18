using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

// Dupa ce timeline-ul din Death Valley ajunge la final, incarca scena jucabila.
// CutsceneController tine ultimul cadru (endAfterHold = 0), deci asteptam timpul directorului.
[RequireComponent(typeof(PlayableDirector))]
public class CutsceneSceneLoader : MonoBehaviour
{
    [SerializeField] string nextScene = "World";
    [SerializeField, Min(0f)] float delay = 0.5f;

    PlayableDirector director;
    bool loading;
    float timer;

    void Awake()
    {
        director = GetComponent<PlayableDirector>();
    }

    void Update()
    {
        if (loading)
        {
            timer -= Time.unscaledDeltaTime;
            if (timer <= 0f) LoadNext();
            return;
        }

        if (director == null || director.duration <= 0.01f) return;
        if (director.time < director.duration - 0.01f) return;

        loading = true;
        timer = delay;
    }

    [ContextMenu("Load Next Scene Now")]
    public void LoadNext()
    {
        if (!Application.CanStreamedLevelBeLoaded(nextScene))
        {
            Debug.LogError(
                $"[CutsceneSceneLoader] Scene '{nextScene}' is not in Build Settings."
            );
            enabled = false;
            return;
        }

        Debug.Log($"[CutsceneSceneLoader] Loading '{nextScene}'.");
        SceneManager.LoadScene(nextScene, LoadSceneMode.Single);
    }
}
