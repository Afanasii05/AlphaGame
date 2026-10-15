using Alpha.Combat;
using UnityEngine;

public class DebugingScript : MonoBehaviour
{
    [SerializeField] Weapon weaponP;
    [SerializeField] bool enquipAtStart = true;
    EnquipWeapon wp;
    void Awake()
    {
        wp = GetComponentInChildren<EnquipWeapon>();
    }

    void Start()
    {
        if(enquipAtStart)
            wp.UnlockAndEnquip(weaponP);
    }
    void Update()
    {
        
    }
}
