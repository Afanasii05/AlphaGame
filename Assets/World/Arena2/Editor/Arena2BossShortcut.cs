using Alpha.Combat;
using UnityEditor;
using UnityEngine;

namespace Alpha.World.EditorTools
{
    // Scurtatura de test pentru lupta cu cavalerul: in Play, jucatorul primeste ciocanul ca dupa
    // scena cu sabia si e mutat la intrarea in curte, chiar inainte de prag.
    public static class Arena2BossShortcut
    {
        const string MenuPath = "Tools/Alpha/World/Arena 2 Boss Test";
        const string HammerPath = "Assets/Weapons/Sword_Unity/SwordHammer.prefab";
        static readonly Vector3 Entrance = new Vector3(420f, 13.1f, 386f);

        [MenuItem(MenuPath)]
        static void Run()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) { Debug.LogWarning("[Arena2BossShortcut] Nu exista jucator activ."); return; }

            Arm(player);
            // dupa Arena 1 ciocanul are deja primul strat cioplit
            if (HammerStages.Instance != null && HammerStages.CurrentStage < 1) HammerStages.Instance.SetStage(1);
            float y = Entrance.y;
            if (Physics.Raycast(Entrance + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 8f, ~((1 << 2) | (1 << 6) | (1 << 7) | (1 << 8)), QueryTriggerInteraction.Ignore))
                y = hit.point.y + 0.05f;
            Vector3 at = new Vector3(Entrance.x, y, Entrance.z);
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.position = at;
                rb.rotation = Quaternion.identity;
            }
            player.transform.SetPositionAndRotation(at, Quaternion.identity);
            Physics.SyncTransforms();
        }

        [MenuItem(MenuPath, true)]
        static bool Validate() => Application.isPlaying;

        // acelasi drum ca la finalul scenei cu sabia: deblocare + echipare pe jucatorul activ
        static void Arm(GameObject player)
        {
            WeaponSocket socket = player.GetComponentInChildren<WeaponSocket>();
            EnquipWeapon enquip = player.GetComponentInChildren<EnquipWeapon>();
            if (socket == null || enquip == null) { Debug.LogWarning("[Arena2BossShortcut] Jucatorul nu are socket de arma."); return; }
            if (socket.HasWeapon) return;

            Weapon prefab = null;
            PlayCutscene cutscene = Object.FindAnyObjectByType<PlayCutscene>(FindObjectsInactive.Include);
            if (cutscene != null)
                prefab = new SerializedObject(cutscene).FindProperty("UnlockedWeapon").objectReferenceValue as Weapon;
            if (prefab == null) prefab = AssetDatabase.LoadAssetAtPath<Weapon>(HammerPath);
            enquip.UnlockAndEnquip(prefab);
            if (!socket.HasWeapon) enquip.Equip();
        }
    }
}
