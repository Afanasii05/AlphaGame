using UnityEngine;

namespace Alpha.World
{
    // Cerul nu primeste ceata, asa ca in mlastina dealurile ceturite se taiau pe un cer portocaliu. O banda in jurul camerei,
    // colorata cu ceata, acopera cerul de la orizont in sus si se estompeaza spre zenit; tarie dupa drum (RouteAtmosphere).
    [DefaultExecutionOrder(110)]
    public class HorizonVeil : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] RouteAtmosphere route;
        [SerializeField] Renderer band;
        [Tooltip("Distanta pe drum de la care apare valul si cea de la care e complet")]
        [SerializeField] float startDistance = 10f;
        [SerializeField] float fullDistance = 90f;
        [SerializeField, Range(0f, 1f)] float strength = 0.95f;

        MaterialPropertyBlock block;

        void OnEnable()
        {
            block ??= new MaterialPropertyBlock();
            if (band != null) band.enabled = false;
        }

        void OnDisable()
        {
            if (band != null) band.enabled = false;
        }

        void LateUpdate()
        {
            if (route == null || band == null) return;
            Camera cam = Camera.main;
            float t = Mathf.Clamp01((route.Distance - startDistance) / Mathf.Max(fullDistance - startDistance, 0.01f));
            float alpha = route.Weight * t * t * (3f - 2f * t) * strength;
            band.enabled = cam != null && alpha > 0.005f;
            if (!band.enabled) return;

            // in jurul camerei, putin inainte de planul indepartat
            Transform bt = band.transform;
            bt.SetPositionAndRotation(cam.transform.position, Quaternion.identity);
            bt.localScale = Vector3.one * cam.farClipPlane * 0.8f;
            Color c = RenderSettings.fogColor;
            c.a = alpha;
            band.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            band.SetPropertyBlock(block);
        }
    }
}
