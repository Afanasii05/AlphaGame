using UnityEngine;

public class MirageAttack : MonoBehaviour
{
    [SerializeField] HeavyHitSwordsTransition mirageSwordPrefab;
    [SerializeField] int numberOfMirageSwords = 6;
    [SerializeField] float distanceFromImpactPoint = 5;
    [SerializeField] float swordsIntoGround = 0.1f;
    [SerializeField] float swordsOutFromGround = 2f;
    [SerializeField] float timeForMirageSwordsToStay = 2f;
    [SerializeField] LayerMask groundMask;
    Quaternion rotationBias = Quaternion.identity;
    float distanceForMirages;
    float posOfGround;
    Transform impact;
    private void Awake()
    {

        posOfGround = swordsOutFromGround;
        distanceForMirages = -5f;
    }
    public void mirageFromGround(Transform impactPoint)
    {
        impact = impactPoint;
        distanceForMirages = 2f;
        posOfGround = swordsIntoGround;
        rotationBias = Quaternion.identity;
        mirageAttack();
    }
    public void mirageFromSky(Transform impactPoint)
    {
        impact = impactPoint;
        distanceForMirages = -5f;
        posOfGround = swordsOutFromGround;
        rotationBias = Quaternion.Euler(180, 0, 0);
        mirageAttack();
    }
    void mirageAttack()
    {
        int angleBetweenSwords = 360 / numberOfMirageSwords;
        for (int i = 0; i < numberOfMirageSwords; i++)
        {
            Quaternion rotation = Quaternion.Euler(0, i * angleBetweenSwords, 0);
            Vector3 spawnPosition = impact.position + rotation * Vector3.forward * distanceFromImpactPoint;
            if (Physics.Raycast(spawnPosition + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 10f, groundMask, QueryTriggerInteraction.Ignore))
                spawnPosition = hit.point + hit.normal * 0.05f;
            HeavyHitSwordsTransition mir = Instantiate(mirageSwordPrefab, spawnPosition - Vector3.up * posOfGround, rotation * rotationBias);
            mir.BeginTransition(distanceForMirages);
            Destroy(mir.gameObject, timeForMirageSwordsToStay);
        }
    }
}
