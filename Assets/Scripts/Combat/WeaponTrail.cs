using UnityEngine;
namespace Alpha.Combat
{
    //Trail de lumina pe capul armei: apare cand arma se misca repede fata de player
    //(lovituri, aruncare, recall), nu si cand doar alergi cu ea in mana.
    [RequireComponent(typeof(Weapon))]
    [DefaultExecutionOrder(1001)] //Dupa WeaponRecall, ca sa vada pozitia reala din zbor
    public class WeaponTrail : MonoBehaviour
    {
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private float minSpeed = 6f; //m/s ai capului armei

        private Weapon weapon;
        private Transform reference;
        private Vector3 lastPoint;

        private void Awake()
        {
            weapon = GetComponent<Weapon>();
            if (trail == null)
                trail = GetComponentInChildren<TrailRenderer>(true);
            trail.emitting = false;
        }

        private void LateUpdate()
        {
            //In mana masuram fata de player, ca mersul sa nu porneasca trail-ul; in lume masuram fata de scena
            Transform current = weapon.IsHeld ? transform.root : null;
            Vector3 point = current != null ? current.InverseTransformPoint(trail.transform.position) : trail.transform.position;
            if (current != reference)
            {
                reference = current; //Schimbarea de referinta ar da un salt fals de viteza
                lastPoint = point;
                return;
            }
            float speed = Time.deltaTime > 0f ? (point - lastPoint).magnitude / Time.deltaTime : 0f;
            lastPoint = point;
            trail.emitting = speed > minSpeed;
        }
    }
}
