using UnityEngine;

// Fire-like flicker for point lights (braziers, torches, candles). Added automatically by Tools > RogueKit > Setup.
[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [Tooltip("0 = steady (crystals/mushrooms), 1 = fire")] [Range(0f, 1f)] public float amount = 0.35f;
    public float speed = 6f;
    [Tooltip("small position wobble (meters) for moving shadows")] public float wobble = 0.03f;

    Light _light; float _base; Vector3 _pos; float _seed;

    void Awake()
    {
        _light = GetComponent<Light>();
        _base = _light.intensity;
        _pos = transform.localPosition;
        _seed = Random.value * 100f;
    }

    void Update()
    {
        float t = Time.time * speed + _seed;
        float n = Mathf.PerlinNoise(t, _seed) * 0.7f + Mathf.PerlinNoise(t * 2.7f, _seed + 7f) * 0.3f; // 0..1
        _light.intensity = _base * (1f - amount * 0.5f + amount * n);
        if (wobble > 0f)
            transform.localPosition = _pos + new Vector3(Mathf.PerlinNoise(t * 0.8f, 1f) - 0.5f, Mathf.PerlinNoise(t * 0.8f, 3f) - 0.5f, Mathf.PerlinNoise(t * 0.8f, 5f) - 0.5f) * wobble;
    }
}
