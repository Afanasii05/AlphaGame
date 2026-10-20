using System.Collections;
using Alpha.Input;
using UnityEngine;
namespace Alpha.Combat
{
    [DefaultExecutionOrder(1000)]
    public class WeaponRecall : MonoBehaviour
    {
        [SerializeField] private GameObject catchFxPrefab;
        [SerializeField] private float minRecallDistance = 1.5f;
        [SerializeField] private float recallSpeed = 28f;
        [SerializeField] private float spinDegrees = 720f;

        private WeaponSocket socket;
        private Animator animator;
        private Weapon tracked;
        private WeaponFX trackedFX;
        private Vector3 storedPosition;
        private Quaternion storedRotation;
        private bool hasStored;
        private bool flying;
        private float flightTime;
        private float flightDuration;
        private float flightDistance;

        private void Awake()
        {
            InputHandler inputHandler = GetComponentInParent<InputHandler>();
            socket = inputHandler.GetComponentInChildren<WeaponSocket>();
            animator = inputHandler.GetComponentInChildren<Animator>();
        }

        private void OnDisable()
        {
            if (tracked != null)
                tracked.onHeldChange -= HandleHeldChange;
            tracked = null;
            EndFlight();
        }

        private void Update()
        {
            Weapon current = socket.CurrentWeapon;
            if (current == null || current == tracked)
                return;
            if (tracked != null)
                tracked.onHeldChange -= HandleHeldChange;
            tracked = current;
            trackedFX = tracked.GetComponent<WeaponFX>();
            tracked.onHeldChange += HandleHeldChange;
            hasStored = false;
        }

        private void HandleHeldChange(bool held)
        {
            if (!held)
            {
                EndFlight(); //Aruncata din nou in timpul zborului
                return;
            }
            if (!hasStored)
                return;
            hasStored = false;
            Vector3 end = socket.transform.position;
            float distance = Vector3.Distance(storedPosition, end);
            if (distance <= minRecallDistance)
                return;
            flightDistance = distance;
            flightDuration = Mathf.Clamp(distance / recallSpeed, 0.25f, 0.8f);
            flightTime = 0f;
            flying = true;
            animator.SetBool("Recalling", true);
            if (trackedFX != null)
                trackedFX.ForceEmit = true;
        }

        private void LateUpdate()
        {
            if (tracked == null)
                return;
            if (flying)
            {
                UpdateFlight();
                return;
            }
            if (!tracked.IsHeld) //In lume: retin ultima pozitie pentru zborul de intoarcere
            {
                storedPosition = tracked.transform.position;
                storedRotation = tracked.transform.rotation;
                hasStored = true;
            }
        }

        private void UpdateFlight()
        {
            flightTime += Time.deltaTime;
            float t = Mathf.Clamp01(flightTime / flightDuration);
            float eased = t * t;
            Vector3 end = socket.transform.position; //Mana se misca, tinta urmareste socket-ul
            Vector3 control = (storedPosition + end) * 0.5f + Vector3.up * flightDistance * 0.25f;
            Vector3 position = Vector3.Lerp(Vector3.Lerp(storedPosition, control, eased), Vector3.Lerp(control, end, eased), eased);
            Quaternion rotation = Quaternion.AngleAxis(spinDegrees * eased, Camera.main.transform.right) * storedRotation;
            if (t > 0.85f)
                rotation = Quaternion.Slerp(rotation, socket.transform.rotation, (t - 0.85f) / 0.15f);
            tracked.transform.SetPositionAndRotation(position, rotation);
            if (t >= 1f)
                Catch();
        }

        private void Catch()
        {
            flying = false;
            tracked.transform.localPosition = Vector3.zero;
            tracked.transform.localRotation = Quaternion.identity;
            if (catchFxPrefab != null)
                Destroy(Instantiate(catchFxPrefab, socket.transform.position, socket.transform.rotation), 2f);
            CombatFeedback feedback = CombatFeedback.Instance;
            if (feedback != null)
            {
                feedback.Shake(0.2f, 0.2f);
                feedback.FovKick(4f);
                feedback.Hitstop(0.04f);
            }
            StartCoroutine(ClearFlags(trackedFX));
        }

        private IEnumerator ClearFlags(WeaponFX fx)
        {
            yield return new WaitForSeconds(0.15f);
            if (!flying)
            {
                animator.SetBool("Recalling", false);
                if (fx != null)
                    fx.ForceEmit = false;
            }
        }

        private void EndFlight()
        {
            if (!flying)
                return;
            flying = false;
            if (animator != null)
                animator.SetBool("Recalling", false);
            if (trackedFX != null)
                trackedFX.ForceEmit = false;
        }
    }
}
