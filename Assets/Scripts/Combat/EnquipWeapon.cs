using UnityEngine;
using Alpha.Input;
using System.Collections.Generic;
namespace Alpha.Combat
{
    public class EnquipWeapon : MonoBehaviour
    {
        [SerializeField] private WeaponSocket socket;
        private InputHandler inputHandler;

        private List<Weapon>spawned = new List<Weapon>();          //Instantele la prefabs
        private HashSet<Weapon> WeaponPrefabs = new HashSet<Weapon>();//Prefaburile in sine(non duplicate)
        private int IndexEnquip;

        private void Awake(){inputHandler = GetComponentInParent<InputHandler>();}
        private void OnEnable(){inputHandler.onToggleWeapon += HandleToggleWeapon;}
        private void OnDisable(){inputHandler.onToggleWeapon -= HandleToggleWeapon;}

        private void HandleToggleWeapon()
        {
            if (socket.HasWeapon)
                Unequip();
            else
                Equip();
        }

        
        private void Equip()
        {
            if (spawned.Count == 0)
                return;
            socket.Attach(spawned[IndexEnquip]);
        }
        private void Equip(Weapon spawnedPrefab)
        {
            if (!spawned.Contains(spawnedPrefab))
                return;
            IndexEnquip = spawned.IndexOf(spawnedPrefab);
            socket.Attach(spawned[IndexEnquip]);
        }

        public Weapon Unlock(Weapon UnlockedWeaponPrefab)
        {
            if (UnlockedWeaponPrefab == null || WeaponPrefabs.Contains(UnlockedWeaponPrefab))
                return null;
            WeaponPrefabs.Add(UnlockedWeaponPrefab);
            Weapon spawn = Instantiate(UnlockedWeaponPrefab);
            spawned.Add(spawn);
            return spawn;
            
        }


        public void UnlockAndEnquip(Weapon UnlockedWeaponPrefab)
        {
            Weapon spawnedFromPrefab = Unlock(UnlockedWeaponPrefab);
            if (spawnedFromPrefab == null)
                return;
            Equip(spawnedFromPrefab);

        }
        private void Unequip()
        {
            if (!socket.HasWeapon)
                return;
            Weapon weapon = socket.Detach();
            Quaternion rot = Quaternion.identity;
            rot *= Quaternion.Euler(180, 0, 0);
            weapon.transform.SetPositionAndRotation(transform.position + transform.forward * 1f + Vector3.up * 1.5f, rot); //Il las vertical in fata personajului, sa nu intre in capsula
            
        }
    }
}
