using System;
using UnityEngine;
namespace Alpha.Combat
{
    public class WeaponSocket : MonoBehaviour
    {
        public Weapon CurrentWeapon { get; private set; }
        public bool HasWeapon { get { return CurrentWeapon != null; } }
        
        public void Attach(Weapon weapon)
        {
            if (weapon == null)
                return;
            Snap(weapon);
            weapon.SetHeld(true);
            CurrentWeapon = weapon;
        }

        public Weapon Detach()
        {
            if (CurrentWeapon == null)
                return null;
            Weapon weapon = CurrentWeapon;
            weapon.transform.SetParent(null, true); 
            weapon.SetHeld(false);
            CurrentWeapon = null;
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

       
    }
}
