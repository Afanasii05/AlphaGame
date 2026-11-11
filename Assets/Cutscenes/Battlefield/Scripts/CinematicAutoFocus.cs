using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;

/// <summary>
/// Depth of Field automat: focusul stă pe ținta LookAt a camerei Cinemachine active.
/// La schimbarea camerei (tăietură) focusul sare instant pe noua țintă, fără "căutare";
/// în interiorul unui cadru urmărește ținta lin. Rulează în Play Mode.
/// </summary>
public class CinematicAutoFocus : MonoBehaviour
{
    public Volume volume;
    public CinemachineBrain brain;
    [Tooltip("Cât de repede urmărește focusul ținta în interiorul unui cadru.")]
    public float focusSpeed = 6f;
    public float minDistance = 0.5f;
    public float maxDistance = 60f;

    DepthOfField _dof;
    ICinemachineCamera _lastCam;

    void LateUpdate()
    {
        if (!Application.isPlaying || !volume || !brain) return;
        if (_dof == null && !volume.profile.TryGet(out _dof)) return;

        var active = brain.ActiveVirtualCamera;
        var vcam = active as CinemachineVirtualCameraBase;
        Transform target = vcam ? vcam.LookAt : null;
        if (!target) return;

        float d = Mathf.Clamp(Vector3.Distance(brain.transform.position, target.position), minDistance, maxDistance);
        if (active != _lastCam)
        {
            _lastCam = active;
            _dof.focusDistance.Override(d);
            return;
        }
        float t = 1f - Mathf.Exp(-focusSpeed * Time.deltaTime);
        _dof.focusDistance.Override(Mathf.Lerp(_dof.focusDistance.value, d, t));
    }
}
