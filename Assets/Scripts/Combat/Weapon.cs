using System;
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
        [field: SerializeField] public Collider headHitbox { get; private set; } //Trigger folosit de atacuri
        [SerializeField] private CraterSpawner cr;
        public PlayerCombatState Owner { get; private set; }
        public Transform GripRight   { get { return gripRight; } }
        public Transform GripLeft    { get { return gripLeft; } }
        public Transform ImpactPoint { get { return impactPoint; } }
        public bool IsHeld { get; private set; }

        private void Awake()
        {

            if (headHitbox != null)
            {
                headHitbox.enabled = true;
            }
        }

        public void SetHeld(bool held)
        {
            IsHeld = held;
            if(held)
                Owner = GetComponentInParent<PlayerCombatState>();
            if (headHitbox != null)
                headHitbox.isTrigger = held;
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
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                    cr.setWasThrown(false);
                }
           
                body.isKinematic = held;
            }
        }

        
    }
}
