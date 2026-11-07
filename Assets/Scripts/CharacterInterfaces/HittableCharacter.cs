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


        [SerializeField] Light healthLight;
        [SerializeField] ParticleSystem deathSmoke;
        [SerializeField] protected float knockBackDuration = 0.5f;
        [SerializeField] float knockBackCooldown = 1f;
        [SerializeField] float armor = 1.0f;
        [SerializeField] float Max_Health = 100f;
        [SerializeField] float knockBackProtection = 1.0f;
        float currentHealth;
        protected float lastKnockBackTime;
        protected bool isInKnockBack;
        Color fullColor;
        Rigidbody rb;
        //E pt domain  reload oprin sa mi se reseteze astea statice
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetLists() { allys.Clear(); enemies.Clear(); }

        Color HealthColor() => Color.Lerp(Color.red, fullColor, Mathf.InverseLerp(0f, 0.6f, currentHealth / Max_Health));
        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if(healthLight != null)
            {
                fullColor = healthLight.color;
            }
            currentHealth = Max_Health;
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
            //Logica pe npc + player de take damage
            if (attacker == null || rb == null) return;   
            currentHealth -= attack.damage / armor;
            UpdateHealthLight();
            if (currentHealth <= 0)
            {
                HandleDeath();
                return;
            }
            StartCoroutine(colorPulse());
            StartHitState();
            //Logica separata pe clasele derivate pentru knockback.
            if (isInKnockBack || Time.time - lastKnockBackTime < knockBackCooldown) return;
            Vector3 knowkBackDirection = -(attacker.transform.position - transform.position);
            knowkBackDirection.y = 0f;
            knowkBackDirection = knowkBackDirection.normalized;
            Vector3 totalKnokback = knowkBackDirection * attack.knockBackPower / knockBackProtection;
            handleKnockbackLogic(totalKnokback);
        }
        protected abstract void StartHitState();

        protected abstract void EndCharacter();
        protected virtual void handleKnockbackLogic(Vector3 totalKnockback)
        {
            StartCoroutine(handleKnockBack(totalKnockback));
        }
        protected virtual void endHitState() {}
        

        protected void HandleDeath()
        {
            EndCharacter();                   //Functie handled de clasele derivate
            StartCoroutine(spawnDeathSmoke());
        }
        protected void UpdateHealthLight()
        {
            if (healthLight != null) healthLight.color = HealthColor();
        }
        IEnumerator spawnDeathSmoke()
        {
            yield return new WaitForSeconds(2.2f);   
            if (deathSmoke) Instantiate(deathSmoke, transform.position + Vector3.up, Quaternion.identity);
        }
        protected virtual IEnumerator handleKnockBack(Vector3 totalKnockback)
        {
            isInKnockBack = true;
            lastKnockBackTime = Time.time;
            Vector3 start = rb.position;
            for (float step = 0; step < 1f; step += Time.fixedDeltaTime / knockBackDuration)
            {
                Vector3 target = Vector3.Lerp(start, start + totalKnockback, step);
                target.y = rb.position.y;
                rb.MovePosition(target);
                yield return new WaitForFixedUpdate();
            }
            isInKnockBack = false;
            endHitState();
        }
        IEnumerator colorPulse()
        {
            if (healthLight == null) yield break;
            float baseIntensity = healthLight.intensity;
            healthLight.intensity = baseIntensity * 3f;      
            yield return new WaitForSeconds(0.15f);
            healthLight.intensity = baseIntensity;           
        }

    }
}
