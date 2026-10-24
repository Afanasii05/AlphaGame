using UnityEngine;
namespace Alpha.Combat
{
    
    [RequireComponent(typeof(Weapon))]
    [DefaultExecutionOrder(1001)] //Dupa restul LateUpdate-urilor, ca sa vada pozitia finala a armei
    public class WeaponTrail : MonoBehaviour
    {
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private float minSpeed = 6f;

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
            Transform current = weapon.IsHeld && weapon.combatStateOfPlayer != null ? weapon.combatStateOfPlayer.transform : null; //Playerul, nu transform.root: in zborul de recall arma nu e copilul lui
            Vector3 point = current != null ? current.InverseTransformPoint(trail.transform.position) : trail.transform.position;
            if (current != reference)
            {
                reference = current;
                lastPoint = point;
                return;
            }
            float speed = Time.deltaTime > 0f ? (point - lastPoint).magnitude / Time.deltaTime : 0f;
            lastPoint = point;
            trail.emitting = speed > minSpeed;
        }
    }
}
