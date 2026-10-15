using Alpha.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Alpha.Combat
{
    public class EnquipWeapon : MonoBehaviour
    {
        [SerializeField] private WeaponSocket socket;
        [SerializeField] private Transform playerTr;
        private Animator animat;
        private PlayerInAttackAnimation anim;
        private InputHandler inputHandler;

        private List<Weapon>spawned = new List<Weapon>();          //Instantele la prefabs
        private HashSet<Weapon> WeaponPrefabs = new HashSet<Weapon>();//Prefaburile in sine(non duplicate)
        private int IndexEnquip;
        private bool isThrowing = false;
        private void Awake()
        {
            inputHandler = GetComponentInParent<InputHandler>();
            anim = GetComponentInParent<PlayerInAttackAnimation>();
            animat = GetComponent<Animator>();
        }
        private void OnEnable(){inputHandler.onToggleWeapon += HandleToggleWeapon;}
        private void OnDisable(){inputHandler.onToggleWeapon -= HandleToggleWeapon;}

        private void HandleToggleWeapon()
        {
            if (anim.isAttacking() || anim.isThrowing())
                return;
            if (socket.HasWeapon)
                StartCoroutine(ThrowRoutine());
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
            foreach (var c in weapon.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(c, playerTr.GetComponent<Collider>());
            Rigidbody weaponRb = weapon.GetComponent<Rigidbody>();
            Quaternion plRot = playerTr.rotation;
            weaponRb.linearVelocity = Camera.main.transform.forward * 30f;
            weaponRb.angularVelocity = playerTr.right * 3f;

        }
        private IEnumerator ThrowRoutine()
        {
            isThrowing = true;
            animat.SetTrigger("Throw");                
            yield return new WaitForSeconds(0.8f);        
            Unequip();                                     
            yield return new WaitForSeconds(0.4f);
            isThrowing = false;
        }
    }
}
