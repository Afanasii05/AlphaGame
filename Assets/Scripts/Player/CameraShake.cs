using UnityEngine;

[DefaultExecutionOrder(100)]  
public class CameraShake : MonoBehaviour
{
    [SerializeField] float frequency = 25f;   
    float timeLeft, duration, strength;

    public void Shake(float strength, float duration)
    {
        if (timeLeft > 0f && strength < this.strength) return;  
        this.strength = strength;
        this.duration = duration;
        timeLeft = duration;
    }

    void LateUpdate()
    {
        if (timeLeft <= 0f) return;
        timeLeft -= Time.deltaTime;

        float fade = Mathf.Clamp01(timeLeft / duration);
        float t = Time.time * frequency;
        Vector3 offset = new Vector3(
            Mathf.PerlinNoise(t, 0f) - 0.5f,
            Mathf.PerlinNoise(0f, t) - 0.5f,
            0f) * 2f * strength * fade;

        transform.position += transform.TransformDirection(offset);
    }
}