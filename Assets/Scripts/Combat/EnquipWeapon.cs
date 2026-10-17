using Alpha.Input;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
namespace Alpha.Combat
{
    public class EnquipWeapon : MonoBehaviour
    {
        [SerializeField] private WeaponSocket socket;
        [SerializeField] private Transform playerTr;
        [SerializeField] private float waitUntilThrow = 0.7f;
        private Animator animat;
        private PlayerInAttackAnimation anim;
        private InputHandler inputHandler;

        private List<Weapon>spawned = new List<Weapon>();             //Instantele la prefabs
        private HashSet<Weapon> WeaponPrefabs = new HashSet<Weapon>();//Prefaburile in sine(non duplicate)
        private int IndexEnquip;

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
            Rigidbody weaponRb = weapon.GetComponent<Rigidbody>();
            Quaternion plRot = Camera.main.transform.rotation;
            playerTr.rotation = Quaternion.Euler(0, Camera.main.transform.rotation.y,0);
            weapon.transform.rotation = plRot * Quaternion.Euler(90,0,0);
            weaponRb.linearVelocity = Camera.main.transform.forward * 20f;
            weaponRb.angularVelocity = playerTr.right * 0.2f;

        }
        private IEnumerator ThrowRoutine()
        {
            StartCoroutine(TurnToCamera());
            animat.SetTrigger("Throw");                
            yield return new WaitForSeconds(waitUntilThrow);        
            Unequip();                                     
        }

        private IEnumerator TurnToCamera()
        {
            Rigidbody rb = playerTr.GetComponent<Rigidbody>();
            Quaternion start = rb.rotation;
            Quaternion target = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);

            for (float t = 0f; t < 0.15f; t += Time.fixedDeltaTime)
            {
                rb.MoveRotation(Quaternion.Slerp(start, target, t / 0.15f));
                yield return new WaitForFixedUpdate();
            }
            rb.MoveRotation(target);
        }
    }
}
