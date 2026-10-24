using System.Collections;
using UnityEngine;

namespace Alpha.Combat
{
    public class ChargeShockwave : MonoBehaviour
    {
        [SerializeField] private GameObject gatherPrefab;    
        [SerializeField] private GameObject shockwavePrefab;  
        [SerializeField] private float releaseDelay = 0.65f;
        [SerializeField] private float shockwaveLifetime = 2.5f;
        [SerializeField] private float lightIntensity = 8f;
        [SerializeField] private float lightFadeTime = 0.6f;
        [SerializeField] private float ChargeMoveDelay = 10f;
        private float lastTime = -10;

        private PlayerCombatState combatState;

        private void Awake()
        { combatState = GetComponent<PlayerCombatState>();}

        public void Play()
        {
            
            StartCoroutine(PlayRoutine());
             
        }

        private IEnumerator PlayRoutine()
        {
            
            GameObject gather = Instantiate(gatherPrefab, transform);
            yield return new WaitForSeconds(releaseDelay);
            Destroy(gather);

            
            if (combatState.CurrentAction() != CombatAction.Charge)
                yield break;

         
            GameObject shockwave = Instantiate(shockwavePrefab, transform.position, Quaternion.identity);
            Destroy(shockwave, shockwaveLifetime);

       
            Light shockLight = shockwave.GetComponentInChildren<Light>();
            for (float t = 0f; t < lightFadeTime; t += Time.deltaTime)
            {
                if (shockLight == null) yield break;
                shockLight.intensity = Mathf.Lerp(lightIntensity, 0f, t / lightFadeTime);
                yield return null;
            }
            if (shockLight != null)
                shockLight.intensity = 0f;
        }
    }
}
