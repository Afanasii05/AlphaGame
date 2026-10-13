using UnityEngine;
using UnityEngine.InputSystem;
namespace Alpha.Combat
{
    public class DebugEquip : MonoBehaviour
    {
        [SerializeField] private WeaponSocket socket;
        [SerializeField] private Weapon weaponPrefab; //Asset de prefab, instantiat la prima echipare
        [SerializeField] private bool equipOnStart = false;

        private Weapon spawned;
        private bool warned = false;

        private void Start()
        {
            if (equipOnStart)
                Equip();
        }

        private void Update()
        {
            if (!Debug.isDebugBuild) //Doar in Editor si in build-uri de development
                return;
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                if (socket != null && socket.HasWeapon)
                    Unequip();
                else
                    Equip();
            }
        }

        private bool Validate()
        {
            if (socket != null && weaponPrefab != null)
                return true;
            if (!warned)
            {
                warned = true;
                Debug.LogWarning("DebugEquip: lipseste socket-ul sau prefabul armei", this);
            }
            return false;
        }

        private void Equip()
        {
            if (!Validate())
                return;
            if (spawned == null)
                spawned = Instantiate(weaponPrefab);
            socket.Attach(spawned);
            Debug.Log("DebugEquip: arma echipata");
        }

        private void Unequip()
        {
            Weapon weapon = socket.Detach();
            if (weapon == null)
                return;
            weapon.transform.SetPositionAndRotation(transform.position + transform.forward * 1f + Vector3.up * 0.5f, Quaternion.identity); //Il las vertical in fata personajului, sa nu intre in capsula
            Debug.Log("DebugEquip: arma lasata jos");
        }
    }
}
