using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Alpha.World
{
    // Crapatura in pamant: creste cand pamantul se rupe si se stinge dupa ce piatra s-a scufundat.
    // Merge cu DecalProjector (URP) sau, ca rezerva, cu un quad plat transparent.
    public class GroundCrack : MonoBehaviour
    {
        [SerializeField] DecalProjector decal;
        [SerializeField] Renderer flatRenderer;
        [SerializeField] float startScale = 0.2f;

        static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        Vector3 fullSize, fullScale;
        Material flatMaterial;
        Color flatColor;
        float growth, opacity;

        void Awake()
        {
            if (decal != null) fullSize = decal.size;
            fullScale = transform.localScale;
            if (flatRenderer != null)
            {
                flatMaterial = flatRenderer.material;
                flatColor = flatMaterial.GetColor(ColorId);
            }
            Apply(0f, 0f);
        }

        // diametrul final al crapaturii, in metri
        public void SetSize(float size)
        {
            if (decal != null) fullSize = new Vector3(size, size, fullSize.z);
            else fullScale = new Vector3(size, fullScale.y, size);
            Apply(growth, opacity);
        }

        public void GrowTo(float target, float duration)
        {
            StopAllCoroutines();
            StartCoroutine(grow(target, duration));
        }

        public void FadeOut(float delay, float duration)
        {
            StopAllCoroutines();
            StartCoroutine(fade(delay, duration));
        }

        IEnumerator grow(float target, float duration)
        {
            float fromGrowth = growth, fromOpacity = opacity;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                Apply(Mathf.Lerp(fromGrowth, target, 1f - (1f - k) * (1f - k)), Mathf.Lerp(fromOpacity, 1f, Mathf.Clamp01(k * 3f)));
                yield return null;
            }
            Apply(target, 1f);
        }

        IEnumerator fade(float delay, float duration)
        {
            yield return new WaitForSeconds(delay);
            float from = opacity;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                Apply(growth, Mathf.Lerp(from, 0f, t / duration));
                yield return null;
            }
            Destroy(gameObject);
        }

        void Apply(float g, float a)
        {
            growth = g;
            opacity = a;
            float s = Mathf.Lerp(startScale, 1f, g);
            if (decal != null)
            {
                decal.size = new Vector3(fullSize.x * s, fullSize.y * s, fullSize.z);
                decal.fadeFactor = a;
            }
            else
            {
                transform.localScale = new Vector3(fullScale.x * s, fullScale.y, fullScale.z * s);
                if (flatMaterial != null) flatMaterial.SetColor(ColorId, new Color(flatColor.r, flatColor.g, flatColor.b, flatColor.a * a));
            }
        }
    }
}
