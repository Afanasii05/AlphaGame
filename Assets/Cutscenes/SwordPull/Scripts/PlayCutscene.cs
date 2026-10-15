using Alpha.Input;
using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections;
using Alpha.Combat;

public class PlayCutscene : MonoBehaviour
{
    [SerializeField] private Transform startingPoint;
    [SerializeField] private CinemachineCamera cam;
    [SerializeField] private PlayableDirector timelineDirector;
    [SerializeField] private Transform Sword;
    [SerializeField] private Weapon UnlockedWeapon;
    private TriggerCutscene trigger;

    void Awake()
    {
        if (trigger == null)
            trigger = GetComponent<TriggerCutscene>();
    }
    private void OnEnable()
    {
        trigger.onTrigger += Begin;
    }
    private void OnDisable()
    {
        trigger.onTrigger -= Begin;
    }
    
    private void Begin(Transform player)
    {
        StartCoroutine(playCutscene(player));
    }
    private IEnumerator playCutscene(Transform player)
    {

        InputHandler input = player.GetComponentInParent<InputHandler>();
        input.disableInputs();
        Rigidbody rb = player.GetComponent<Rigidbody>();
        rb.isKinematic = true;

        yield return FadeTransitionScript.Instance.FadeOut(1f);
        player.SetPositionAndRotation(startingPoint.position, startingPoint.rotation);
        
        cam.enabled = true;
        timelineDirector.enabled = true;
        timelineDirector.Play();
        yield return FadeTransitionScript.Instance.FadeIn(0.8f);

        yield return new WaitUntil(() => timelineDirector.time >= timelineDirector.duration - 1.8f);
        yield return FadeTransitionScript.Instance.FadeOut(1.8f);
        timelineDirector.Stop();
        timelineDirector.enabled = false;
        input.enableInputs();
        Destroy(Sword.gameObject);
        EnquipWeapon wp = player.GetComponentInChildren<EnquipWeapon>();
        wp.UnlockAndEnquip(UnlockedWeapon);
        rb.isKinematic = false;
        cam.enabled = false;
        
        yield return FadeTransitionScript.Instance.FadeIn(0.8f);
        

    }

    
    void Update()
    {
        
    }
}
