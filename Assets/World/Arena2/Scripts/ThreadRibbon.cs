using UnityEngine;

namespace Alpha.World
{
    // Tine firul de lumina subtire din orice unghi: dara e orientata spre camera, deci cand trece pe langa ea
    // capatul apropiat se umfla intr-un con luminos. Latimea scade cu distanta pana la cel mai apropiat punct al darei,
    // iar lumina firului se stinge cand e chiar in fata camerei.
    [RequireComponent(typeof(TrailRenderer))]
    public class ThreadRibbon : MonoBehaviour
    {
        [SerializeField] float fullWidthDistance = 9f;
        [SerializeField] float minScale = 0.06f;
        [SerializeField] float lightFadeDistance = 4f;

        TrailRenderer trail;
        Light glow;
        float glowIntensity;
        Vector3[] points = new Vector3[256];

        void Awake()
        {
            trail = GetComponent<TrailRenderer>();
            glow = GetComponentInChildren<Light>(true);
            if (glow != null) glowIntensity = glow.intensity;
        }

        void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 eye = cam.transform.position;
            float head = Vector3.Distance(eye, transform.position), nearest = head;
            int count = trail.positionCount;
            if (count > points.Length) points = new Vector3[count * 2];
            count = trail.GetPositions(points);
            for (int i = 0; i < count; i++) nearest = Mathf.Min(nearest, Vector3.Distance(eye, points[i]));
            trail.widthMultiplier = Mathf.Clamp(nearest / fullWidthDistance, minScale, 1f);
            if (glow != null) glow.intensity = glowIntensity * Mathf.Clamp01((head - 1f) / lightFadeDistance);
        }
    }
}
