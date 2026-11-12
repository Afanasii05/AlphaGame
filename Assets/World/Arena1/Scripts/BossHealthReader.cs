using System.Reflection;
using Alpha.Interfaces;
using UnityEngine;

namespace Alpha.World
{
    // Citire HP pentru bara boss-ului.
    // HittableCharacter nu expune viata public, asa ca aici (si doar aici) o citim prin reflexie.
    // Daca campurile se redenumesc, Fraction intoarce doar 1 (viu) sau 0 (mort).
    public class BossHealthReader
    {
        static readonly FieldInfo CurrentField = typeof(HittableCharacter).GetField("currentHealth", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo MaxField = typeof(HittableCharacter).GetField("Max_Health", BindingFlags.Instance | BindingFlags.NonPublic);
        static bool warned;

        readonly HittableCharacter target;

        public BossHealthReader(HittableCharacter target)
        {
            this.target = target;
            if (!Available && !warned)
            {
                warned = true;
                Debug.LogWarning("[BossHealthReader] Campurile de viata nu au fost gasite, bara arata doar daca boss-ul traieste.");
            }
        }

        public static bool Available => CurrentField != null && MaxField != null;

        public bool IsAlive => target != null && HittableCharacter.enemies.Contains(target);

        // viata ramasa intre 0 si 1
        public float Fraction
        {
            get
            {
                if (target == null) return 0f;
                if (!Available) return IsAlive ? 1f : 0f;
                float max = (float)MaxField.GetValue(target);
                if (max <= 0f) return IsAlive ? 1f : 0f;
                return Mathf.Clamp01((float)CurrentField.GetValue(target) / max);
            }
        }
    }
}
