using Alpha.Combat;
using Alpha.Input;
using UnityEngine;
using System.Collections;

public class TriggerAttack : MonoBehaviour
{
    private Animator animator;
    private InputHandler input;
    private WeaponSocket socket;
    private PlayerCombatState combatState;
    
    [SerializeField] float doubleClickWindow = 0.25f;
    [SerializeField] Transform playerTr;
    EnquipWeapon enquip;
    ChargeShockwave chargeShockwave;



    [SerializeField] private float waitUntilThrow = 0.7f;
    float lastClickTime = 0f;
    [SerializeField] private float ChargeDelay = 10f;
    private float lastCharge = -10;
    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        input = GetComponent<InputHandler>();
        socket = GetComponentInChildren<WeaponSocket>();
        combatState = GetComponent<PlayerCombatState>();
        enquip = GetComponentInChildren<EnquipWeapon>();
        chargeShockwave = GetComponent<ChargeShockwave>();

    }
    private void OnEnable()
    {
        input.onClick += HandleClickAttackTrigger;
        input.onRightClick += HandleRightClickTrigger;
        input.onToggleWeapon += HandleThrowAttack;
        input.onToggleSpecialMove += HandleSpecialMove;
        input.onCharge += HandleChargeAttack;
        
    }
    private void OnDisable()
    {
        input.onClick -= HandleClickAttackTrigger;
        input.onRightClick -= HandleRightClickTrigger;
        input.onToggleWeapon -= HandleThrowAttack;
        input.onToggleSpecialMove -= HandleSpecialMove;
        input.onCharge -= HandleChargeAttack;
    }

    private void HandleClickAttackTrigger()
    {
        if (!socket.HasWeapon)
            HandleMeleCombat(CombatAction.LightPunch);

        else
            HandleMeleCombat(CombatAction.Light);

    }
    private void HandleRightClickTrigger()
    {
        if (!socket.HasWeapon)
            return;
        if (combatState.TryStart(CombatAction.Heavy))
        {
            animator.SetTrigger("RightClick");
            socket.CurrentWeapon.GetComponent<HeavyImpactDetection>()?.Begin();
        }
    }

    private void HandleMeleCombat(CombatAction calledAction)
    {
        CombatAction move = combatState.ComboFrom(calledAction);
        if (combatState.TryStart(move))
        {
            StartCoroutine(TurnToCamera());
            if (move == calledAction)
                animator.SetTrigger("Click"); //Am avut none deci pot incepe prima miscare din combo.
            else
                animator.SetTrigger("Combo"); //Click porneste lantul, combo il continua (any state nu l poate intercepta)
 
        }

    }
    





   
    private void HandleSpecialMove()
    {
        if (socket.HasWeapon&&combatState.TryStart(CombatAction.Ability))
            animator.SetTrigger("SpecialMove");
    }

    private void HandleChargeAttack()
    {
        if (Time.time-lastCharge>=ChargeDelay && combatState.TryStart(CombatAction.Charge))
        {
            animator.SetTrigger("Charge");
            if (chargeShockwave != null)
                chargeShockwave.Play();
            lastCharge = Time.time;
        }
    }


    private void HandleThrowAttack()
    {
        if (!socket.HasWeapon || !combatState.TryStart(CombatAction.Throw))
            return;
        StartCoroutine(ThrowRoutine());
       
    }

    private IEnumerator ThrowRoutine()
    {
        StartCoroutine(TurnToCamera());
        animator.SetTrigger("Throw");
        yield return new WaitForSeconds(waitUntilThrow);

        Weapon weapon = enquip.Unequip();
        if (weapon == null) yield break;
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
    void Update()
    {
        
    }
}
