using UnityEngine;
using UnityEngine.Rendering;

// Feeds camera matrices to the PlagueAtmosphere full-screen shader (works in Scene view, Game view and builds).
[ExecuteAlways]
public class PlagueAtmosphereFeeder : MonoBehaviour
{
    static readonly int InvVP = Shader.PropertyToID("_PA_InvVP");
    static readonly int CamPos = Shader.PropertyToID("_PA_CamPos");

    void OnEnable()  { RenderPipelineManager.beginCameraRendering += OnBeginCamera; }
    void OnDisable() { RenderPipelineManager.beginCameraRendering -= OnBeginCamera; }

    static void OnBeginCamera(ScriptableRenderContext ctx, Camera cam)
    {
        Matrix4x4 proj = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true);
        Matrix4x4 vp = proj * cam.worldToCameraMatrix;
        Shader.SetGlobalMatrix(InvVP, vp.inverse);
        Vector3 p = cam.transform.position;
        Shader.SetGlobalVector(CamPos, new Vector4(p.x, p.y, p.z, cam.farClipPlane));
    }
}