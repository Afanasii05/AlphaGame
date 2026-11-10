using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controlează cutscene-ul: oprește scripturile de gameplay ale camerei, ridică temporar calitatea
/// (render scale, umbre, LOD, anti-aliasing, sharpen) și restaurează totul la final.
/// Boost-ul se aplică doar în Play Mode / build, ca să nu modifice permanent asset-ul URP din Editor.
/// </summary>
[RequireComponent(typeof(PlayableDirector))]
public class CutsceneController : MonoBehaviour
{
    public PlayableDirector director;

    [Header("Gameplay oprit în timpul cutscene-ului")]
    public Behaviour[] disableDuringCutscene;

    [Header("Boost calitate (doar Play Mode / build)")]
    public bool boostQuality = true;
    [Range(1f, 2f)] public float renderScale = 1.5f;
    public float shadowDistance = 80f;
    [Range(1, 4)] public int shadowCascades = 4;
    public float lodBias = 3f;
    public Camera targetCamera;
    public ScriptableRendererFeature sharpenFeature;

    [Tooltip("Volume-uri (look cinematic, fade) readuse la weight 0 când cutscene-ul se termină.")]
    public Volume[] zeroWeightOnEnd;

    [Header("Final")]
    [Tooltip("Secunde de ținut ultimul cadru înainte de a reda controlul jucătorului. 0 = rămâne pe ultimul cadru.")]
    public float endAfterHold = 0f;

    bool _active;
    float _holdTimer;
    float _prevScale, _prevShadow, _prevLod;
    int _prevCascades;
    AntialiasingMode _prevAA;
    AntialiasingQuality _prevAAQ;
    bool _prevSharpen;
    bool[] _prevEnabled;

    void Awake()
    {
        if (!director) director = GetComponent<PlayableDirector>();
        if (!targetCamera) targetCamera = Camera.main;
    }

    void OnEnable()
    {
        director.played += OnPlayed;
        director.stopped += OnStopped;
        if (Application.isPlaying && director.state == PlayState.Playing) OnPlayed(director);
    }

    void OnDisable()
    {
        director.played -= OnPlayed;
        director.stopped -= OnStopped;
        Restore();
    }

    void OnApplicationQuit() { Restore(); }

    public void PlayCutscene() { director.time = 0; director.Play(); }
    public void EndCutscene() { director.Stop(); }

    void OnPlayed(PlayableDirector d)
    {
        if (_active || !Application.isPlaying) return;
        _active = true;
        _holdTimer = 0f;

        if (disableDuringCutscene != null)
        {
            _prevEnabled = new bool[disableDuringCutscene.Length];
            for (int i = 0; i < disableDuringCutscene.Length; i++)
            {
                if (!disableDuringCutscene[i]) continue;
                _prevEnabled[i] = disableDuringCutscene[i].enabled;
                SetEnabledSafe(disableDuringCutscene[i], false);
            }
        }

        if (!boostQuality) return;
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp)
        {
            _prevScale = urp.renderScale; _prevShadow = urp.shadowDistance; _prevCascades = urp.shadowCascadeCount;
            urp.renderScale = renderScale; urp.shadowDistance = shadowDistance; urp.shadowCascadeCount = shadowCascades;
        }
        _prevLod = QualitySettings.lodBias; QualitySettings.lodBias = lodBias;

        var camData = targetCamera ? targetCamera.GetUniversalAdditionalCameraData() : null;
        if (camData)
        {
            _prevAA = camData.antialiasing; _prevAAQ = camData.antialiasingQuality;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.antialiasingQuality = AntialiasingQuality.High;
        }
        if (sharpenFeature) { _prevSharpen = sharpenFeature.isActive; sharpenFeature.SetActive(true); }
    }

    void OnStopped(PlayableDirector d) { Restore(); }

    void Update()
    {
        if (!_active || endAfterHold <= 0f) return;
        if (director.time >= director.duration - 0.01)
        {
            _holdTimer += Time.deltaTime;
            if (_holdTimer >= endAfterHold) director.Stop();
        }
    }

    void Restore()
    {
        if (!_active) return;
        _active = false;

        if (zeroWeightOnEnd != null)
            foreach (var v in zeroWeightOnEnd) if (v) v.weight = 0f;

        if (disableDuringCutscene != null && _prevEnabled != null)
            for (int i = 0; i < disableDuringCutscene.Length; i++)
                if (disableDuringCutscene[i]) SetEnabledSafe(disableDuringCutscene[i], _prevEnabled[i]);

        if (!boostQuality) return;
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp) { urp.renderScale = _prevScale; urp.shadowDistance = _prevShadow; urp.shadowCascadeCount = _prevCascades; }
        QualitySettings.lodBias = _prevLod;
        var camData = targetCamera ? targetCamera.GetUniversalAdditionalCameraData() : null;
        if (camData) { camData.antialiasing = _prevAA; camData.antialiasingQuality = _prevAAQ; }
        if (sharpenFeature) sharpenFeature.SetActive(_prevSharpen);
    }

    // Un script de gameplay cu referințe goale poate arunca excepții în OnEnable/OnDisable;
    // le prindem ca boost-ul și restaurarea să ruleze oricum.
    static void SetEnabledSafe(Behaviour b, bool value)
    {
        try { b.enabled = value; }
        catch (System.Exception e) { Debug.LogWarning("[CutsceneController] " + b.GetType().Name + " a aruncat o excepție la enable/disable: " + e.Message, b); }
    }
}
