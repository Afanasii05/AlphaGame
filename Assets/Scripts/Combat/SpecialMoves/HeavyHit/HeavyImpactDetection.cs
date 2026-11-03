using Alpha.Combat;
using UnityEngine;

public class HeavyImpactDetection : MonoBehaviour
{
    [SerializeField] Weapon wp;
    [SerializeField] CraterSpawner cr;
    [SerializeField] float impactRadius = 0.15f;
    [SerializeField] LayerMask impactMask;
    [SerializeField] LayerMask groundMask;
    [SerializeField] MirageAttack mirageAttack;
    Vector3 lastPos;
    Transform impact;
    
    PlayerActions expected;
    
    void Awake() { enabled = false; }     

    public void BeginHeavyGround()
    {
        
        impact = wp.ImpactPoint1;
        lastPos = impact.position;
        expected = PlayerActions.Heavy;
        enabled = true;                          
    }
    public void BeginHeavyFall() 
    {
        impact = wp.ImpactPoint2;
        lastPos = impact.position;
        expected = PlayerActions.HeavyFall;
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
            if(expected==PlayerActions.Heavy)
                mirageAttack.mirageFromGround(impact);
            else
                mirageAttack.mirageFromSky(impact);
            enabled = false;                     
            return;
        }
        lastPos = cur;
    }
    
}
