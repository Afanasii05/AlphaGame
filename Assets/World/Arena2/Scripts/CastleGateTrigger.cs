using Alpha.Input;
using UnityEngine;
using UnityEngine.Events;

namespace Alpha.World
{
    // Pragul curtii: cand jucatorul trece de el, poarta se tranteste in urma lui. Se declanseaza o singura data (Rearm il pregateste pentru o noua incercare).
    // Fara collider: pozitia jucatorului se verifica direct, ca sa nu se amestece cu trigger-ele de lovire.
    public class CastleGateTrigger : MonoBehaviour
    {
        [SerializeField] CastleGate gate;
        [SerializeField] Vector3 size = new Vector3(12f, 4f, 4f);
        [SerializeField] float closeDelay = 0.15f;
        [SerializeField] UnityEvent onPlayerEntered;

        public bool Fired { get; private set; }
        public CastleGate Gate => gate;

        public event System.Action PlayerEntered;

        Transform player;
        float timer = -1f;

        void Update()
        {
            if (Fired) return;
            if (timer >= 0f)
            {
                timer -= Time.deltaTime;
                if (timer < 0f) Fire();
                return;
            }
            if (player == null) player = FindPlayer();
            if (player == null) return;
            Vector3 local = transform.InverseTransformPoint(player.position);
            if (Mathf.Abs(local.x) <= size.x * 0.5f && Mathf.Abs(local.y) <= size.y * 0.5f && Mathf.Abs(local.z) <= size.z * 0.5f)
                timer = closeDelay;
        }

        // incercare noua: pragul se arma din nou
        public void Rearm()
        {
            Fired = false;
            timer = -1f;
        }

        void Fire()
        {
            Fired = true;
            if (gate != null) gate.Close();
            onPlayerEntered?.Invoke();
            PlayerEntered?.Invoke();
        }

        static Transform FindPlayer()
        {
            InputHandler input = FindAnyObjectByType<InputHandler>();
            if (input != null) return input.transform;
            GameObject tagged = GameObject.FindWithTag("Player");
            return tagged != null ? tagged.transform : null;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, size);
        }
    }
}
