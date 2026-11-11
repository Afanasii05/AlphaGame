using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;

/// <summary>
/// Redă cutscene-ul înregistrat (video) la pornirea scenei, oprește gameplay-ul cât rulează,
/// permite skip cu orice tastă și la final pornește din nou jocul.
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public class IntroVideo : MonoBehaviour
{
    [Tooltip("Obiecte ascunse cât rulează video-ul și activate la final (jucătorul, UI-ul).")]
    public GameObject[] enableAfterVideo;

    [Tooltip("Scripturi oprite cât rulează video-ul (ex. controllerul camerei).")]
    public Behaviour[] disableDuringVideo;

    [Tooltip("Jucătorul poate sări peste video apăsând orice tastă.")]
    public bool allowSkip = true;

    VideoPlayer _video;
    bool _finished;

    void Awake()
    {
        _video = GetComponent<VideoPlayer>();
        _video.playOnAwake = false;
        _video.isLooping = false;
    }

    void Start()
    {
        foreach (var b in disableDuringVideo) if (b) b.enabled = false;
        foreach (var g in enableAfterVideo) if (g) g.SetActive(false);

        _video.loopPointReached += _ => Finish();
        _video.prepareCompleted += v => v.Play();
        _video.Prepare();
    }

    void Update()
    {
        if (!allowSkip || _finished || !_video.isPlaying) return;
        bool key = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool pad = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
        if (key || click || pad) Finish();
    }

    void Finish()
    {
        if (_finished) return;
        _finished = true;
        _video.Stop();
        _video.enabled = false;
        foreach (var b in disableDuringVideo) if (b) b.enabled = true;
        foreach (var g in enableAfterVideo) if (g) g.SetActive(true);
    }
}
