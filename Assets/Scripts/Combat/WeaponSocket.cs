using System;
using UnityEngine;
namespace Alpha.Combat
{
    public class WeaponSocket : MonoBehaviour
    {
        public Weapon CurrentWeapon { get; private set; }
        public bool HasWeapon { get { return CurrentWeapon != null; } }

        public event Action<Weapon> onAttached;
        public event Action<Weapon> onDetached;

        private void Awake()
        {
            Weapon existing = GetComponentInChildren<Weapon>(true); //Arma pusa deja sub socket in prefab
            if (existing != null)
                Attach(existing);
        }

        public void Attach(Weapon weapon)
        {
            if (weapon == null)
                return;
            if (CurrentWeapon != null && CurrentWeapon != weapon)
                Detach();
            bool sameWeapon = CurrentWeapon == weapon;
            Snap(weapon);
            weapon.SetHeld(true);
            CurrentWeapon = weapon;
            if (!sameWeapon)
                onAttached?.Invoke(weapon);
        }

        public Weapon Detach(Transform newParent = null)
        {
            if (CurrentWeapon == null)
                return null;
            Weapon weapon = CurrentWeapon;
            weapon.transform.SetParent(newParent, true); //Pastrez pozitia din lume
            weapon.SetHeld(false);
            CurrentWeapon = null;
            onDetached?.Invoke(weapon);
            return weapon;
        }

        private void Snap(Weapon weapon)
        {
            Transform t = weapon.transform;
            t.SetParent(transform, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }

        private void OnDrawGizmos()
        {
            const float size = 0.05f;
            Vector3 p = transform.position;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(p, p + transform.right * size);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(p, p + transform.up * size);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(p, p + transform.forward * size);
        }
    }
}
