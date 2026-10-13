using UnityEngine;
namespace Alpha.Combat
{
    [DisallowMultipleComponent]
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private Transform gripRight;
        [SerializeField] private Transform gripLeft;
        [SerializeField] private Transform impactPoint;
        [SerializeField] private Collider[] worldColliders; //Colidere solide, active doar cat arma sta in lume
        [SerializeField] private Collider headHitbox; //Trigger folosit de atacuri

        public Transform GripRight { get { return gripRight; } }
        public Transform GripLeft { get { return gripLeft; } }
        public Transform ImpactPoint { get { return impactPoint; } }
        public bool IsHeld { get; private set; }

        private void Awake()
        {
            if (headHitbox != null)
            {
                headHitbox.isTrigger = true;
                headHitbox.enabled = false;
            }
        }

        public void SetHeld(bool held)
        {
            IsHeld = held;
            if (worldColliders != null)
            {
                for (int i = 0; i < worldColliders.Length; i++)
                {
                    if (worldColliders[i] != null)
                        worldColliders[i].enabled = !held; //Tinuta in mana, colliderele solide ar intra in compusul playerului
                }
            }
            if (TryGetComponent(out Rigidbody body))
            {
                if (held)
                {
                    body.linearVelocity = Vector3.zero; //Se reseteaza inainte de kinematic, altfel nu mai e permis
                    body.angularVelocity = Vector3.zero;
                }
                body.isKinematic = held;
                body.detectCollisions = !held;
            }
            SetHitboxActive(false);
        }

        public void SetHitboxActive(bool active)
        {
            if (headHitbox == null)
                return;
            if (active && !IsHeld)
                return;
            headHitbox.enabled = active;
        }
    }
}
