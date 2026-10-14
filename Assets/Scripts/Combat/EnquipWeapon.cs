using UnityEngine;
using Alpha.Input;
namespace Alpha.Combat
{
    public class EnquipWeapon : MonoBehaviour
    {
        [SerializeField] private WeaponSocket socket;
        [SerializeField] private Weapon weaponPrefab; //Asset de prefab, instantiat la prima echipare
        [SerializeField] private bool equipOnStart = false;

        private InputHandler inputHandler;
        private Weapon spawned;

        private void Awake()
        {
            inputHandler = GetComponentInParent<InputHandler>();
        }

        private void Start()
        {
            if (equipOnStart)
                Equip();
        }
        private void OnEnable()
        {
            inputHandler.onToggleWeapon += HandleToggleWeapon;
        }
        private void OnDisable()
        {
            inputHandler.onToggleWeapon -= HandleToggleWeapon;
        }

        private void HandleToggleWeapon()
        {
            if (socket.HasWeapon)
                Unequip();
            else
                Equip();
        }
        
        private void Equip()
        {
            if (spawned == null)
                spawned = Instantiate(weaponPrefab);
            socket.Attach(spawned);
        }

        private void Unequip()
        {
            Weapon weapon = socket.Detach();
            Quaternion rot = Quaternion.identity;
            rot *= Quaternion.Euler(180, 0, 0);
            weapon.transform.SetPositionAndRotation(transform.position + transform.forward * 1f + Vector3.up * 1.5f, rot); //Il las vertical in fata personajului, sa nu intre in capsula
           
        }
    }
}
