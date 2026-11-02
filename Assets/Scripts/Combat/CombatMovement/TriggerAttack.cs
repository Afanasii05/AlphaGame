using Alpha.Combat;
using Alpha.Input;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

public class TriggerAttack : MonoBehaviour
{

    [SerializeField] private float minRecallDistance = 1.5f;
    [SerializeField] private float recallSpeed = 28f;
    [SerializeField] private float spinDegrees = 720f;
    [SerializeField] private float animHoldAfterCatch = 0.15f;
    [SerializeField] private float waitUntilThrow = 0.7f;
    [SerializeField] BlockWall WallCreateScript;
    [SerializeField] Transform playerTr;

    private static readonly int RecallingHash = Animator.StringToHash("Recalling");

    private Animator animator;
    private Weapon thrown;
    public Weapon Thrown => thrown; 
    private InputHandler input;
    private WeaponSocket socket;
    private PlayerCombatState combatState;
    
    EnquipWeapon enquip;
    ChargeShockwave chargeShockwave;
    TeleportToWeapon teleport;
    Coroutine turnRoutine;

    
    private bool blockHeld;
    private bool wallBuilt = false;
    BlockWall currentWall;
    void Awake()
    {
        input = GetComponent<InputHandler>();
        socket = GetComponentInChildren<WeaponSocket>();
        combatState = GetComponent<PlayerCombatState>();
        enquip = GetComponentInChildren<EnquipWeapon>();
        chargeShockwave = GetComponent<ChargeShockwave>();
        animator = GetComponentInChildren<Animator>();
        teleport = GetComponent<TeleportToWeapon>();

    }
    private void OnEnable()
    {
        input.onClick += HandleClickAttackTrigger;
        input.onRightClick += HandleRightClickTrigger;
        input.onToggleWeapon += HandleThrowAttack;
        input.onToggleSpecialMove += HandleSpecialMove;
        input.onCharge += HandleChargeAttack;
        input.onTeleport += HandleTeleportation;
        input.onBlockHold += HandleBlock;
        
    }
    private void OnDisable()
    {
        input.onClick -= HandleClickAttackTrigger;
        input.onRightClick -= HandleRightClickTrigger;
        input.onToggleWeapon -= HandleThrowAttack;
        input.onToggleSpecialMove -= HandleSpecialMove;
        input.onCharge -= HandleChargeAttack;
        input.onTeleport -= HandleTeleportation;
        input.onBlockHold -= HandleBlock;
    }

    private void HandleClickAttackTrigger()
    {
        if (!socket.HasWeapon)
            HandleMeleCombat(PlayerActions.LightPunch);
        else
            HandleMeleCombat(PlayerActions.Light);
    }
    private void HandleRightClickTrigger()
    {
        if (!socket.HasWeapon)
            return;
        if (combatState.TryStart(PlayerActions.Heavy))
        {
            if (socket.CurrentWeapon.TryGetComponent(out HeavyImpactDetection hid)) hid.Begin();
            StartTurn();
        }
    }

    private void HandleMeleCombat(PlayerActions calledAction)
    {
        PlayerActions move = combatState.ComboFrom(calledAction);
        if (combatState.TryStart(move))
            StartTurn();
    }
     
    private void HandleSpecialMove()
    {
        if (socket.HasWeapon&&combatState.TryStart(PlayerActions.Ability))
            StartTurn();
    }

    private void HandleChargeAttack()
    {
        if (combatState.TryStart(PlayerActions.Charge))
        {
            if (chargeShockwave != null)
                chargeShockwave.Play();
            StartTurn();
        }
    }
    private void HandleBlock(bool isBlocking) {
        blockHeld = isBlocking;
        if (!isBlocking)
        {
            animator.SetTrigger("Intrerupt");
            wallBuilt = false;
            if (currentWall != null)
            {
                currentWall.stopWallBuild();
                currentWall = null;
            }
            
        }
        
 
    }

    private void Update()
    {
        if (blockHeld)
        {
            if (combatState.TryStart(PlayerActions.Block))
            {
                StartTurn();
                if (!wallBuilt)
                {
                    currentWall = WallCreateScript.instantiateWall(playerTr);
                    wallBuilt = true;
                }
                animator.ResetTrigger("Intrerupt");
            }
            
        }
    }
    

    private void HandleThrowAttack()
    {

        if (socket.HasWeapon)
        {
            if (combatState.TryStart(PlayerActions.Throw))
                StartCoroutine(ThrowRoutine());
        }
        else if (thrown != null && combatState.TryStart(PlayerActions.Recall))
            StartCoroutine(RecallRoutine());
    }

    private void HandleTeleportation()
    {
        if(!socket.HasWeapon&& teleport.canTeleport(thrown) &&combatState.TryStart(PlayerActions.Teleport))
        {
            teleport.teleport(thrown);
            StartTurn();
        }
    }
    private IEnumerator RecallRoutine()
    {
        Weapon w = thrown;
        thrown = null;
        Transform wt = w.transform;
        Vector3 start = wt.position;
        Quaternion startRot = wt.rotation;

        enquip.Equip(w);   
        float dist = Vector3.Distance(start, socket.transform.position);
        if (dist <= minRecallDistance) yield break;  
        wt.SetParent(null, true);   // in zbor arma nu mai e copilul mainii, rotatia bratului nu o mai trage

        animator.SetBool(RecallingHash, true);
        float duration = Mathf.Clamp(dist / recallSpeed, 0.25f, 0.8f);
        Vector3 spinAxis = Camera.main.transform.right;
        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            float t = time / duration, e = t * t;
            Vector3 end = socket.transform.position;
            Vector3 control = (start + end) * 0.5f + Vector3.up * dist * 0.25f;
            Vector3 pos = Vector3.Lerp(Vector3.Lerp(start, control, e), Vector3.Lerp(control, end, e), e);
            Quaternion rot = Quaternion.AngleAxis(spinDegrees * e, spinAxis) * startRot;
            if (t > 0.85f) rot = Quaternion.Slerp(rot, socket.transform.rotation, (t - 0.85f) / 0.15f);
            wt.SetPositionAndRotation(pos, rot);
            yield return null;
        }
        wt.SetParent(socket.transform, false);
        wt.localPosition = Vector3.zero;
        wt.localRotation = Quaternion.identity;

        yield return new WaitForSeconds(animHoldAfterCatch);
        animator.SetBool(RecallingHash, false);
    }

    private IEnumerator ThrowRoutine()
    {
        StartTurn();
        yield return new WaitForSeconds(waitUntilThrow);

        Weapon weapon = enquip.Unequip();
        if (weapon == null) yield break;
        thrown = weapon;
        Rigidbody weaponRb = weapon.GetComponent<Rigidbody>();
        Quaternion plRot = Camera.main.transform.rotation;
        weapon.transform.rotation = plRot * Quaternion.Euler(90, 0, 0);
        weaponRb.linearVelocity = Camera.main.transform.forward * 20f;
        weaponRb.angularVelocity = playerTr.right * 0.2f;

    }

    private IEnumerator TurnToCamera()
    {
        Rigidbody rb = playerTr.GetComponent<Rigidbody>();
        Quaternion start = rb.rotation;
        Quaternion target = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
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
