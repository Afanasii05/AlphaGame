using UnityEngine;
using System;
public class TriggerCutscene : MonoBehaviour
{
    private bool hasBeenUsed = false;
    public event Action<Transform> onTrigger;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenUsed)
            return;
        Debug.Log("Is Triggered");
        hasBeenUsed = true;
        onTrigger?.Invoke(other.transform);
    }
}
