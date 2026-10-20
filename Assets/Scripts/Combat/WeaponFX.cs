using UnityEngine;
namespace Alpha.Combat
{
    [RequireComponent(typeof(Weapon))]
    [RequireComponent(typeof(Rigidbody))]
    public class WeaponFX : MonoBehaviour
    {
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private float throwSpin = 25f;
        public bool ForceEmit { get; set; }

        private Weapon weapon;
        private Rigidbody body;
        private bool released;
        private float releasedAt;

        private void Awake()
        {
            weapon = GetComponent<Weapon>();
            body = GetComponent<Rigidbody>();
        }

        private void OnEnable() { weapon.onHeldChange += HandleHeldChange; }
        private void OnDisable() { weapon.onHeldChange -= HandleHeldChange; }

        private void HandleHeldChange(bool held)
        {
            released = !held;
            releasedAt = Time.time;
        }

        private bool IsAttacking()
        {
            if (weapon.combatStateOfPlayer == null)
                return false;
            CombatAction action = weapon.combatStateOfPlayer.CurrentAction();
            return action == CombatAction.Light || action == CombatAction.Heavy
                || action == CombatAction.Ability || action == CombatAction.Throw;
        }

        private void Update()
        {
            bool emit = weapon.IsHeld ? IsAttacking() : body.linearVelocity.sqrMagnitude > 16f;
            trail.emitting = ForceEmit || emit;
        }

        private void FixedUpdate()
        {
            if (!released)
                return;
            if (Time.time - releasedAt > 0.3f)
            {
                released = false;
                return;
            }
            if (body.linearVelocity.magnitude > 8f) //Aruncarea seteaza viteza la un frame dupa ce arma e eliberata
            {
                body.maxAngularVelocity = 50f;
                body.angularVelocity = Camera.main.transform.right * throwSpin;
                released = false;
            }
        }
    }
}
