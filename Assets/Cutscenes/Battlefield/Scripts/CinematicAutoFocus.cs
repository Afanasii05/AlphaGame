using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;

/// <summary>
/// Depth of Field automat: focusul urmărește ținta LookAt a camerei Cinemachine active
/// (steag, cavaleri, suliță), cu tranziție lină între cadre. Rulează în Play Mode.
/// </summary>
public class CinematicAutoFocus : MonoBehaviour
{
    public Volume volume;
    public CinemachineBrain brain;
    [Tooltip("Cât de repede se mută focusul (mai mare = mai rapid).")]
    public float focusSpeed = 3f;
    public float minDistance = 0.5f;
    public float maxDistance = 60f;

    DepthOfField _dof;

    void LateUpdate()
    {
        if (!Application.isPlaying || !volume || !brain) return;
        if (_dof == null && !volume.profile.TryGet(out _dof)) return;

        var vcam = brain.ActiveVirtualCamera as CinemachineVirtualCameraBase;
        Transform target = vcam ? vcam.LookAt : null;
        if (!target) return;

        float d = Mathf.Clamp(Vector3.Distance(brain.transform.position, target.position), minDistance, maxDistance);
        float t = 1f - Mathf.Exp(-focusSpeed * Time.deltaTime);
        _dof.focusDistance.Override(Mathf.Lerp(_dof.focusDistance.value, d, t));
    }
}
