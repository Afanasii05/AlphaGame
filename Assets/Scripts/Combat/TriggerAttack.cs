using Alpha.Combat;
using Alpha.Input;
using UnityEngine;

public class TriggerAttack : MonoBehaviour
{
    private Animator anim;
    private InputHandler input;
    private WeaponSocket socket;
    private PlayerInAttackAnimation attackState;
    Weapon weapon;
    [SerializeField] float doubleClickWindow = 0.25f;
    float lastClickTime = 0f;
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
        if (!socket.HasWeapon) return;
            weapon = socket.CurrentWeapon;

        float now = Time.unscaledTime;
        bool isDouble = now - lastClickTime <= doubleClickWindow;
        lastClickTime = now;

        if (isDouble)
        {
            lastClickTime = -10f;             
            anim.ResetTrigger("Click");     
            anim.SetBool("DoubleClick", true);
            

            return;
        }

        if (!attackState.isAttacking() && !attackState.inPlaceAnimation())
            anim.SetTrigger("Click");

    }

    void Update()
    {
        
    }
}
