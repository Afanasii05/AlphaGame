using UnityEngine;
using System;
using Alpha.Input;
public class TriggerCutscene : MonoBehaviour
{
    private bool hasBeenUsed = false;
    public event Action<Transform> onTrigger;
  
    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenUsed || other.GetComponentInParent<InputHandler>()==null)
            return;
        Debug.Log("Is Triggered");
        hasBeenUsed = true;
        onTrigger?.Invoke(other.transform);
    }
}
