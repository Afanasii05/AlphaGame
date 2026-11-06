using UnityEngine;
using Alpha.Interfaces;
using System.Collections;
using Alpha.Combat;
public class CombatAssist : MonoBehaviour
{
    [SerializeField] float maxDistanceAllowedToAssist=10.0f;
    [SerializeField] float angleToAssist = 120;
    float cossedAngle;
    HittableCharacter closesestEnemyInFov;
    PlayerCombatState playerState;
    Rigidbody rb;
    Coroutine turnRoutine;
    public static bool inCombatAssist = false;
    void Awake()
    {
        playerState=GetComponent<PlayerCombatState>();
        rb = GetComponent<Rigidbody>();
        cossedAngle = Mathf.Cos(angleToAssist * 0.5f * Mathf.Deg2Rad);
    }
    void LateUpdate()
    {
        if (playerState == null || playerState.combatDef[playerState.currentAction].nonDamaging) 
        {
            closesestEnemyInFov = null;
            inCombatAssist = false;
            return;
        }
        
        closesestInFov();
        if (closesestEnemyInFov == null) 
        { 
            inCombatAssist = false;
            return; 
        }
        inCombatAssist = true;
        StartTurn();
        
    }
    void closesestInFov()
    {
        float smallestDistance = 10000;
        closesestEnemyInFov = null;
        Vector3 forwardCameraDirection = Camera.main.transform.forward;
        forwardCameraDirection.y = 0;
        forwardCameraDirection.Normalize();
        foreach (HittableCharacter c in HittableCharacter.enemies)
        {
            Vector3 directionTwardsEnemy = c.transform.position - transform.position;
            directionTwardsEnemy.y = 0;
            float directionDistanceSqrd = directionTwardsEnemy.sqrMagnitude;
            if (directionDistanceSqrd > maxDistanceAllowedToAssist*maxDistanceAllowedToAssist) continue;
            
            directionTwardsEnemy.Normalize();
            
            if (Vector3.Dot(directionTwardsEnemy, forwardCameraDirection) < cossedAngle) continue;
            if(smallestDistance>directionDistanceSqrd)
            {
                smallestDistance = directionDistanceSqrd;
                closesestEnemyInFov = c;
            }
        }
    }
    private IEnumerator TurnToCamera()
    {
        Quaternion start = rb.rotation;
        Vector3 targetLook = closesestEnemyInFov.transform.position - transform.position;
        targetLook.y = 0;
        Quaternion target = Quaternion.LookRotation(targetLook);
        for (float t = 0f; t < 0.15f; t += Time.fixedDeltaTime)
        {
            rb.MoveRotation(Quaternion.Slerp(start, target, t / 0.15f));
            yield return new WaitForFixedUpdate();
        }
        rb.MoveRotation(target);
    }
    void StartTurn()
    {
        if (turnRoutine != null) StopCoroutine(turnRoutine);
        turnRoutine = StartCoroutine(TurnToCamera());
    }

    
}
