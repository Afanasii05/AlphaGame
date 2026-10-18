using Alpha.Combat;
using UnityEngine;

public class HeavyImpactDetection : MonoBehaviour
{
    [SerializeField] Weapon wp;
    [SerializeField] CraterSpawner cr;
    [SerializeField] float impactRadius = 0.15f;
    [SerializeField] LayerMask groundMask;
    Vector3 lastPos;

    void Awake() { enabled = false; }     

    public void Begin()
    {
        lastPos = wp.ImpactPoint.position;
        enabled = true;                          
    }

    void LateUpdate()
    {
        if (wp.Owner == null || wp.Owner.CurrentAction() != CombatAction.Heavy)
        {
            enabled = false;               
            return;
        }

        Vector3 cur = wp.ImpactPoint.position;
        Vector3 move = cur - lastPos;
        if (move.sqrMagnitude > 0.0001f &&
            Physics.SphereCast(lastPos, impactRadius, move.normalized, out RaycastHit hit,
                               move.magnitude, groundMask, QueryTriggerInteraction.Ignore))
        {
            cr.SpawnCrater(Quaternion.FromToRotation(Vector3.up, hit.normal),
                           hit.point + hit.normal * 0.05f);
            enabled = false;                     
            return;
        }
        lastPos = cur;
    }
}
