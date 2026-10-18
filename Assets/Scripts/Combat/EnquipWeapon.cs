using Alpha.Input;
using System.Collections.Generic;

using UnityEngine;
namespace Alpha.Combat
{
    public class EnquipWeapon : MonoBehaviour
    {
        [SerializeField] private WeaponSocket socket;
        [SerializeField] private Transform playerTr;

        private PlayerCombatState combatState;
        private InputHandler inputHandler;

        private List<Weapon>spawned = new List<Weapon>();             //Instantele la prefabs
        private HashSet<Weapon> WeaponPrefabs = new HashSet<Weapon>();//Prefaburile in sine(non duplicate)
        private int IndexEnquip;
        Animator animator;

        private void Awake()
        {
            inputHandler = GetComponentInParent<InputHandler>();
            combatState = GetComponentInParent<PlayerCombatState>();
            animator = GetComponentInParent<Animator>();
        }
        private void OnEnable(){inputHandler.onToggleWeapon += HandleToggleWeapon;}
        private void OnDisable(){inputHandler.onToggleWeapon -= HandleToggleWeapon;}

        private void HandleToggleWeapon()
        {
            if (!canEnquip())
                return;
            if (!socket.HasWeapon)
                Equip();
        }

        private bool canEnquip()
        {
            return (combatState.CurrentAction() == CombatAction.None || combatState.CurrentAction() == CombatAction.NonSemiCombat);
        }
        private void Equip()
        {
            if (spawned.Count == 0)
                return;
            socket.Attach(spawned[IndexEnquip]);
            animator.SetBool("HasWeapon", true);
        }
        private void Equip(Weapon spawnedPrefab)
        {
            if (!spawned.Contains(spawnedPrefab))
                return;
            IndexEnquip = spawned.IndexOf(spawnedPrefab);
            socket.Attach(spawned[IndexEnquip]);
            animator.SetBool("HasWeapon", true);
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
        public Weapon Unequip()
        {
            if (!socket.HasWeapon)
                return null;
            animator.SetBool("HasWeapon", false);
            return socket.Detach();
        }
       
    }
}
