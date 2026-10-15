using UnityEngine;

// Slow pendulum-like sway (hanging chains, lanterns). Put it on a pivot placed at the hanging point.
public class GentleSway : MonoBehaviour
{
    public Vector3 axis = new Vector3(1f, 0f, 0.35f);
    public float angle = 3.5f;
    public float speed = 0.45f;
    Quaternion _rest; float _seed;

    void Awake() { _rest = transform.localRotation; _seed = Random.value * 10f; }

    void Update()
    {
        float t = Time.time;
        float a = Mathf.Sin(t * speed * 6.2832f + _seed) * angle + (Mathf.PerlinNoise(t * 0.25f, _seed) - 0.5f) * angle;
        transform.localRotation = _rest * Quaternion.AngleAxis(a, axis.normalized);
    }
}
