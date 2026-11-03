using Alpha.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Alpha.Interfaces
{
    public abstract class HittableCharacter : MonoBehaviour
    {
        public static readonly List<HittableCharacter> allys = new List<HittableCharacter>();
        public static readonly List<HittableCharacter> enemies = new List<HittableCharacter>();
        bool isInKnockBack;
        Rigidbody rb;
        [SerializeField] float knockBackDuration = 0.5f;

        //E pt domain  reload oprin sa mi se reseteze astea statice
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetLists() { allys.Clear(); enemies.Clear(); }

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }
        
        

        public static HittableCharacter Closest(List<HittableCharacter> list, Vector3 from)
        {
            HittableCharacter best = null;
            float bestSqr = float.MaxValue;
            foreach (var h in list)
            {
                if (h == null || !h.isActiveAndEnabled) continue;
                float d = (h.transform.position - from).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = h; }
            }
            return best;
        }

        public virtual void HandleGotHit(CharacterActionDefinition attack, HittableCharacter attacker)
        {
            if (isInKnockBack) return;
            StartHitState(attack);
            if (attacker == null || rb == null) return;
            Vector3 knowkBackDirection = -(attacker.transform.position - transform.position);
            knowkBackDirection.y = 0f;
            knowkBackDirection = knowkBackDirection.normalized;
            Vector3 totalKnokback = knowkBackDirection * attack.knockBackPower;
            StartCoroutine(knockBackEnemy(totalKnokback));
        }
        protected abstract void StartHitState(CharacterActionDefinition attack);
        IEnumerator knockBackEnemy(Vector3 totalKnockback)
        {
            isInKnockBack = true;
            Vector3 start = rb.position;
            for (float step = 0; step < 1f; step += Time.fixedDeltaTime / knockBackDuration)
            {
                Vector3 target = Vector3.Lerp(start, start + totalKnockback, step);
                target.y = rb.position.y;
                rb.MovePosition(target);
                yield return new WaitForFixedUpdate();
            }
            isInKnockBack = false;
        }
    }
}
