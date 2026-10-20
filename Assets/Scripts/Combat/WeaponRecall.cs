using Alpha.Input;
using UnityEngine;
namespace Alpha.Combat
{
    //Cand arma e chemata inapoi (E), in loc sa apara direct in mana zboara in arc spre socket,
    //iar animatorul reda starea "Recall" (Throwing invers) cat timp Recalling = true.
    [DefaultExecutionOrder(1000)] //Dupa EnquipWeapon, ca sa suprascriem pozitia din frame-ul in care a fost atasata
    public class WeaponRecall : MonoBehaviour
    {
        [SerializeField] private float minRecallDistance = 1.5f; //Sub distanta asta arma apare direct in mana
        [SerializeField] private float recallSpeed = 28f;
        [SerializeField] private float spinDegrees = 720f;
        [SerializeField] private float animHoldAfterCatch = 0.15f;

        private static readonly int RecallingHash = Animator.StringToHash("Recalling");

        private WeaponSocket socket;
        private Animator animator;
        private Weapon tracked;
        private Vector3 storedPosition;
        private Quaternion storedRotation;
        private bool hasStored;
        private bool flying;
        private float flightTime;
        private float flightDuration;
        private float flightDistance;
        private float releaseAnimAt = -1f;

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
            StopFlight();
        }

        private void Update()
        {
            //Urmarim arma curenta din socket ca sa aflam cand e aruncata / chemata
            Weapon current = socket.CurrentWeapon;
            if (current != null && current != tracked)
            {
                if (tracked != null)
                    tracked.onHeldChange -= HandleHeldChange;
                tracked = current;
                tracked.onHeldChange += HandleHeldChange;
                hasStored = false;
            }

            if (releaseAnimAt > 0f && Time.time >= releaseAnimAt)
            {
                releaseAnimAt = -1f;
                animator.SetBool(RecallingHash, false);
            }
        }

        private void HandleHeldChange(bool held)
        {
            if (!held)
            {
                StopFlight(); //Aruncata din nou in timpul zborului
                return;
            }
            if (!hasStored)
                return;
            hasStored = false;
            float distance = Vector3.Distance(storedPosition, socket.transform.position);
            if (distance <= minRecallDistance)
                return;
            flightDistance = distance;
            flightDuration = Mathf.Clamp(distance / recallSpeed, 0.25f, 0.8f);
            flightTime = 0f;
            flying = true;
            releaseAnimAt = -1f;
            animator.SetBool(RecallingHash, true);
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
            if (!tracked.IsHeld) //In lume: retinem ultima pozitie, de acolo porneste zborul inapoi
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
            Vector3 spinAxis = Camera.main != null ? Camera.main.transform.right : Vector3.right;
            Quaternion rotation = Quaternion.AngleAxis(spinDegrees * eased, spinAxis) * storedRotation;
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
            releaseAnimAt = Time.time + animHoldAfterCatch;
        }

        private void StopFlight()
        {
            if (!flying)
                return;
            flying = false;
            releaseAnimAt = -1f;
            if (animator != null)
                animator.SetBool(RecallingHash, false);
        }
    }
}
