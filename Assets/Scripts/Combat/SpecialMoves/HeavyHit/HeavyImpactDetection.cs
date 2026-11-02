using Alpha.Combat;
using UnityEngine;

public class HeavyImpactDetection : MonoBehaviour
{
    [SerializeField] Weapon wp;
    [SerializeField] CraterSpawner cr;
    [SerializeField] float impactRadius = 0.15f;
    [SerializeField] LayerMask impactMask;
    [SerializeField] LayerMask groundMask;
    [SerializeField] HeavyHitSwordsTransition mirageSwordPrefab;
    [SerializeField] int numberOfMirageSwords = 6;
    [SerializeField] float distanceFromImpactPoint = 5;
    [SerializeField] float swordsIntoGround = 0.1f;
    [SerializeField] float swordsOutFromGround = 2f;
    [SerializeField] float timeForMirageSwordsToStay = 2f;
    Vector3 lastPos;
    Transform impact;
    Quaternion rotationBias = Quaternion.identity;
    PlayerActions expected;
    float distanceForMirages;
    float posOfGround;
    void Awake() { enabled = false; }     

    public void BeginHeavyGround()
    {
        
        impact = wp.ImpactPoint1;
        lastPos = impact.position;
        expected = PlayerActions.Heavy;
        distanceForMirages= 2f;
        rotationBias = Quaternion.identity;
        posOfGround = swordsIntoGround;
        enabled = true;                          
    }
    public void BeginHeavyFall() 
    {
        impact = wp.ImpactPoint2;
        lastPos = impact.position;
        expected = PlayerActions.HeavyFall;
        distanceForMirages = -5f;
        rotationBias = Quaternion.Euler(180, 0, 0);
        posOfGround = swordsOutFromGround;
        enabled = true;
    }

    void LateUpdate()
    {
        if (wp.playerState == null || (wp.playerState.CurrentAction() != expected))
        {
            enabled = false;               
            return;
        }

        Vector3 cur = impact.position;
        Vector3 move = cur - lastPos;
        if (move.sqrMagnitude > 0.0001f && wp.playerState.inActiveWindow()  && Physics.SphereCast(lastPos, impactRadius, move.normalized, out RaycastHit hit, move.magnitude, impactMask, QueryTriggerInteraction.Ignore))
        {
            cr.SpawnCrater(Quaternion.FromToRotation(Vector3.up, hit.normal), hit.point);
            mirageAttack();
            enabled = false;                     
            return;
        }
        lastPos = cur;
    }
    void mirageAttack()
    {
        int angleBetweenSwords = 360 / numberOfMirageSwords;
        for(int i = 0; i < numberOfMirageSwords; i++)
        {
            Quaternion rotation = Quaternion.Euler(0, i * angleBetweenSwords, 0);
            Vector3 spawnPosition = impact.position  + rotation * Vector3.forward * distanceFromImpactPoint;
            if (Physics.Raycast(spawnPosition + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 10f, groundMask, QueryTriggerInteraction.Ignore))
                spawnPosition = hit.point + hit.normal * 0.05f;
            HeavyHitSwordsTransition mir = Instantiate(mirageSwordPrefab, spawnPosition - Vector3.up * posOfGround, rotation * rotationBias);
            mir.BeginTransition(distanceForMirages);
            Destroy(mir.gameObject, timeForMirageSwordsToStay);
        }
    }
}
