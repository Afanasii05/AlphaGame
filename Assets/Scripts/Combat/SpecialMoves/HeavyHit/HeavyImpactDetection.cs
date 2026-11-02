using Alpha.Combat;
using UnityEngine;

public class HeavyImpactDetection : MonoBehaviour
{
    [SerializeField] Weapon wp;
    [SerializeField] CraterSpawner cr;
    [SerializeField] float impactRadius = 0.15f;
    [SerializeField] LayerMask groundMask;
    [SerializeField] GameObject mirageSwordPrefab;
    [SerializeField] int numberOfMirageSwords = 6;
    [SerializeField] float distanceFromImpactPoint = 5;
    [SerializeField] float sowrdsIntoGround = 0.1f;
    [SerializeField] float timeForMirageSwordsToStay = 2f;
    Vector3 lastPos;

    void Awake() { enabled = false; }     

    public void Begin()
    {
        lastPos = wp.ImpactPoint.position;
        enabled = true;                          
    }

    void LateUpdate()
    {
        if (wp.playerState == null || wp.playerState.CurrentAction() != PlayerActions.Heavy)
        {
            enabled = false;               
            return;
        }

        Vector3 cur = wp.ImpactPoint.position;
        Vector3 move = cur - lastPos;
        if (move.sqrMagnitude > 0.0001f && wp.playerState.inActiveWindow() && Physics.SphereCast(lastPos, impactRadius, move.normalized, out RaycastHit hit, move.magnitude, groundMask, QueryTriggerInteraction.Ignore))
        {
            cr.SpawnCrater(Quaternion.FromToRotation(Vector3.up, hit.normal), hit.point + hit.normal * 0.05f);
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
            Vector3 spawnPosition = wp.ImpactPoint.position  + rotation * Vector3.forward * distanceFromImpactPoint;
            if (Physics.Raycast(spawnPosition + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 10f, groundMask, QueryTriggerInteraction.Ignore))
                spawnPosition = hit.point;
            Destroy(Instantiate(mirageSwordPrefab, spawnPosition - Vector3.up * sowrdsIntoGround, rotation), timeForMirageSwordsToStay);
        }
    }
}
