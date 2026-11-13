using Alpha.Combat;
using Alpha.Interfaces;
using UnityEngine;

namespace Alpha.World
{
    // Ciocanul rechemat are hitbox-ul activ doar in primele cadre, asa ca in zbor trece prin cavaler.
    // Aici urmarim capul armei cat zboara spre mana si, daca traverseaza cavalerul, lovitura merge
    // pe acelasi drum ca una normala (o singura data pe actiune).
    public class RecallStrike : MonoBehaviour
    {
        [SerializeField] HittableCharacter target;
        [SerializeField] Collider[] parts;
        [SerializeField] float radius = 0.25f;

        PlayerCombatState player;
        HittableCharacter playerHittable;
        WeaponSocket socket;
        TriggerAttack attack;
        Vector3 prev;
        bool tracking;

        void LateUpdate()
        {
            if (target == null || !FindPlayer()) return;

            // cat sta pe jos retinem de unde va porni: la fps mic zborul poate dura doar cateva cadre
            Weapon loose = attack != null ? attack.Thrown : null;
            if (loose != null)
            {
                tracking = loose.headHitbox != null;
                if (tracking) prev = loose.headHitbox.bounds.center;
                return;
            }

            Weapon weapon = socket.CurrentWeapon;
            if (!tracking || player.currentAction != PlayerActions.Recall || weapon == null || weapon.headHitbox == null)
            {
                tracking = false;
                return;
            }

            Vector3 head = weapon.headHitbox.bounds.center;
            if (Crosses(prev, head))
            {
                if (target is OathboundKnight knight) knight.NextHitFrom(prev);
                player.hitMeneger.HandleAttack(target, player.combatDef[PlayerActions.Recall], playerHittable);
            }
            prev = head;
            if (weapon.transform.parent == socket.transform) tracking = false;
        }

        bool Crosses(Vector3 from, Vector3 to)
        {
            Vector3 step = to - from;
            float length = step.magnitude;
            foreach (Collider part in parts)
            {
                if (part == null || !part.enabled) continue;
                if ((part.ClosestPoint(to) - to).sqrMagnitude <= radius * radius) return true;
                if (length > 0.001f && part.Raycast(new Ray(from, step / length), out _, length)) return true;
            }
            return false;
        }

        bool FindPlayer()
        {
            if (player != null && socket != null) return true;
            GameObject tagged = GameObject.FindWithTag("Player");
            if (tagged == null) return false;
            player = tagged.GetComponent<PlayerCombatState>();
            playerHittable = tagged.GetComponent<HittableCharacter>();
            socket = tagged.GetComponentInChildren<WeaponSocket>();
            attack = tagged.GetComponentInChildren<TriggerAttack>();
            return player != null && socket != null;
        }
    }
}
