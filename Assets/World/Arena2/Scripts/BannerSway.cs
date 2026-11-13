using UnityEngine;

namespace Alpha.World
{
    // Stindard rupt in vant: segmentele inlantuite se leagana cu zgomot Perlin, fiecare putin mai mult decat cel de sus.
    public class BannerSway : MonoBehaviour
    {
        [SerializeField] Transform[] segments;
        [SerializeField] float angle = 5f;
        [SerializeField] float twist = 3f;
        [SerializeField] float speed = 0.6f;
        [SerializeField] float gust = 0.5f;

        Quaternion[] rest;
        float seed;

        void Awake()
        {
            rest = new Quaternion[segments.Length];
            for (int i = 0; i < segments.Length; i++)
                if (segments[i] != null) rest[i] = segments[i].localRotation;
            seed = Random.value * 100f;
        }

        void Update()
        {
            float t = Time.time * speed;
            // rafalele vin rar si ridica tot stindardul
            float wind = 1f + gust * Mathf.Max(0f, Mathf.PerlinNoise(seed, t * 0.25f) * 2f - 1f) * 2f;
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == null) continue;
                float k = (i + 1f) / segments.Length;
                float px = (Mathf.PerlinNoise(seed + i * 1.7f, t) - 0.5f) * 2f;
                float pz = (Mathf.PerlinNoise(t + i * 0.9f, seed + 3f) - 0.5f) * 2f;
                float py = (Mathf.PerlinNoise(seed + 7f, t * 0.7f + i) - 0.5f) * 2f;
                segments[i].localRotation = rest[i] * Quaternion.Euler(px * angle * wind * k + angle * 0.4f * wind, py * twist * k, pz * angle * 0.6f * k);
            }
        }
    }
}
