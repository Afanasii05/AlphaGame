using Alpha.Combat;
using Alpha.Input;
using UnityEngine;

public class TriggerAttack : MonoBehaviour
{
    private Animator anim;
    private InputHandler input;
    private WeaponSocket socket;
    private PlayerInAttackAnimation attackState;
    void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        input = GetComponent<InputHandler>();
        socket = GetComponentInChildren<WeaponSocket>();
        attackState = GetComponent<PlayerInAttackAnimation>();
    }
    private void OnEnable()
    {
        input.onClick += HandleAttackTrigger;
    }
    private void OnDisable()
    {
        input.onClick -= HandleAttackTrigger;
    }

    private void HandleAttackTrigger()
    {

        if (!socket.HasWeapon || attackState.isAttacking())
            return;
        anim.SetTrigger("Click");

    }
    void Update()
    {
        
    }
}
