using System.Collections;
using UnityEngine;

namespace Alpha.World
{
    // Bucata de piatra aruncata din pamant: zboara cu fizica, apoi se micsoreaza si dispare
    [RequireComponent(typeof(Rigidbody))]
    public class DebrisChunk : MonoBehaviour
    {
        [SerializeField] Vector2 lifetime = new Vector2(2.2f, 3.4f);
        [SerializeField] float shrinkTime = 0.5f;

        public void Launch(Vector3 velocity, float size)
        {
            transform.localScale = Vector3.one * size;
            Rigidbody body = GetComponent<Rigidbody>();
            body.mass = Mathf.Max(1f, size * size * size * 60f);
            body.linearVelocity = velocity;
            body.angularVelocity = Random.onUnitSphere * Random.Range(4f, 12f);
            StartCoroutine(expire(Random.Range(lifetime.x, lifetime.y)));
        }

        IEnumerator expire(float life)
        {
            yield return new WaitForSeconds(life);
            Vector3 start = transform.localScale;
            for (float t = 0f; t < shrinkTime; t += Time.deltaTime)
            {
                transform.localScale = start * (1f - t / shrinkTime);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
