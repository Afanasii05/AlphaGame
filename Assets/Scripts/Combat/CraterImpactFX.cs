using System.Collections;
using UnityEngine;
namespace Alpha.Combat
{
    public class CraterImpactFX : MonoBehaviour
    {
        [SerializeField] private GameObject dustBurstPrefab;
        [SerializeField] private GameObject debrisPrefab;
        [SerializeField] private Color flashColor = new Color(1f, 0.6f, 0.3f);
        [SerializeField] private float flashIntensity = 6f;
        [SerializeField] private float flashTime = 0.25f;

        private void Start()
        {
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, transform.up);
            Spawn(dustBurstPrefab, rotation);
            Spawn(debrisPrefab, rotation);
            StartCoroutine(FlashLight());
            if (CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.Shake(0.3f, 0.3f);
                CombatFeedback.Instance.Hitstop(0.05f);
            }
        }

        private void Spawn(GameObject prefab, Quaternion rotation)
        {
            if (prefab != null)
                Destroy(Instantiate(prefab, transform.position, rotation), 3f);
        }

        private IEnumerator FlashLight()
        {
            GameObject lightObject = new GameObject("ImpactLight");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = Vector3.up * 0.5f;
            Light flash = lightObject.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = flashColor;
            flash.range = 6f;
            for (float t = 0f; t < flashTime; t += Time.unscaledDeltaTime)
            {
                flash.intensity = Mathf.Lerp(flashIntensity, 0f, t / flashTime);
                yield return null;
            }
            Destroy(lightObject);
        }
    }
}
