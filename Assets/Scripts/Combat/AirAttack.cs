using System.Collections;
using Alpha.Input;
using UnityEngine;
namespace Alpha.Combat
{
    [DefaultExecutionOrder(100)]
    public class AirAttack : MonoBehaviour
    {
        private enum SlamPhase { None, Hang, Dive }

        [SerializeField] private GameObject shockwavePrefab;
        [SerializeField] private float airHangTime = 0.35f;
        [SerializeField] private float airSlashCooldown = 0.45f;
        [SerializeField] private float minSlamHeight = 1.5f;
        [SerializeField] private float slamSpeed = 30f;
        [SerializeField] private float slamHangTime = 0.18f;
        [SerializeField] private float slamHoldTime = 0.35f;
        [SerializeField] private float upperBlendTime = 0.08f;
        [SerializeField] private bool debugLog;   //Scrie in Console de ce o lovitura din aer a fost refuzata

        private InputHandler inputHandler;
        private ExtraInputHandler extraInput;
        private WeaponSocket socket;
        private PlayerCombatState combatState;
        private Animator animator;
        private Rigidbody body;
        private CapsuleCollider capsule;
        private readonly RaycastHit[] hitBuffer = new RaycastHit[16];
        private float hangUntil;
        private float nextAirSlash;
        private bool slamming;
        private SlamPhase slamPhase = SlamPhase.None;
        private bool hasSlashTrigger;
        private bool hasPunchTrigger;

        private static readonly int AirSlashHash = Animator.StringToHash("AirSlash");
        private static readonly int AirPunchHash = Animator.StringToHash("AirPunch");

        private void Awake()
        {
            inputHandler = GetComponentInParent<InputHandler>();
            extraInput = GetComponent<ExtraInputHandler>();
            socket = inputHandler.GetComponentInChildren<WeaponSocket>();
            combatState = inputHandler.GetComponent<PlayerCombatState>();
            animator = inputHandler.GetComponentInChildren<Animator>();
            body = inputHandler.GetComponent<Rigidbody>();
            capsule = inputHandler.GetComponent<CapsuleCollider>();
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == AirSlashHash) hasSlashTrigger = true;
                if (parameter.nameHash == AirPunchHash) hasPunchTrigger = true;
            }
        }

        private void OnEnable()
        {
            inputHandler.onClick += HandleClick;
            extraInput.onHeavyAttack += HandleHeavy;
        }

        private void OnDisable()
        {
            inputHandler.onClick -= HandleClick;
            extraInput.onHeavyAttack -= HandleHeavy;
            if (slamming) //Nu las animatorul blocat in plonjon
            {
                slamming = false;
                slamPhase = SlamPhase.None;
                if (animator != null)
                {
                    animator.SetBool("AirSlam", false);
                    animator.SetBool("AirCombat", false);
                }
            }
        }

        private bool Reject(string reason)
        {
            if (debugLog)
                Debug.Log("[AirAttack] refuzat: " + reason);
            return false;
        }

        private bool CanAirAttack()
        {
            if (body.isKinematic)
                return Reject("rigidbody kinematic");
            if (slamming)
                return Reject("plonjon in curs");
            if (animator.GetBool("IsGrounded"))
                return Reject("pe sol (IsGrounded)");
            CombatAction action = combatState.CurrentAction();
            if (action == CombatAction.None || action == CombatAction.NonCombat || action == CombatAction.NonSemiCombat
                || action == CombatAction.LightPunch || action == CombatAction.LightPunch2 || action == CombatAction.LightKick3)
                return true;
            if (action == CombatAction.Light && animator.layerCount > 1) //O lovitura din aer poate fi urmata de alta
            {
                int current = animator.GetCurrentAnimatorStateInfo(1).shortNameHash;
                if (current == AirSlashHash || current == AirPunchHash)
                    return true;
            }
            return Reject("actiune curenta " + action);
        }

        private void PlayUpperBody(int stateHash, bool hasTrigger, string trigger)
        {
            if (animator.layerCount > 1 && animator.HasState(1, stateHash)) //Direct in stare: nu depinde de trigger sau de starea layerului
            {
                animator.CrossFadeInFixedTime(stateHash, upperBlendTime, 1, 0f);
                return;
            }
            if (hasTrigger)
                animator.SetTrigger(trigger);
            else
                Debug.LogWarning("[AirAttack] Starea/trigger-ul " + trigger + " lipseste din Animator. Ruleaza Tools/Alpha/Setup Combat Polish.");
        }

        private void HandleClick()
        {
            if (!CanAirAttack())
                return;
            if (Time.time < nextAirSlash)
            {
                Reject("cooldown");
                return;
            }
            nextAirSlash = Time.time + airSlashCooldown;
            if (socket.HasWeapon)
                PlayUpperBody(AirSlashHash, hasSlashTrigger, "AirSlash");
            else
                PlayUpperBody(AirPunchHash, hasPunchTrigger, "AirPunch");
            if (debugLog)
                Debug.Log("[AirAttack] pornit: " + (socket.HasWeapon ? "AirSlash" : "AirPunch"));
            hangUntil = Time.time + airHangTime; //Playerul pluteste putin in aer cat tine lovitura
            animator.SetBool("AirCombat", true);
        }

        private void HandleHeavy()
        {
            if (!socket.HasWeapon || !CanAirAttack())
                return;
            if (!FindGround(capsule.bounds.center, out RaycastHit ground) || ground.distance - capsule.bounds.extents.y < minSlamHeight)
                return;
            StartCoroutine(SlamRoutine());
        }

        private void Update()
        {
            if (slamming)
                return;
            bool grounded = animator.GetBool("IsGrounded");
            if (grounded) //Un trigger ramas netrecut nu trebuie sa porneasca dupa aterizare
            {
                if (hasSlashTrigger) animator.ResetTrigger(AirSlashHash);
                if (hasPunchTrigger) animator.ResetTrigger(AirPunchHash);
            }
            bool hanging = Time.time < hangUntil && !grounded;
            if (!hanging && animator.GetBool("AirCombat"))
                animator.SetBool("AirCombat", false);
        }

        private void FixedUpdate()
        {
            if (body.isKinematic)
                return;
            Vector3 velocity = body.linearVelocity;
            if (slamPhase == SlamPhase.Hang)
                body.linearVelocity = new Vector3(0f, 0.5f, 0f);
            else if (slamPhase == SlamPhase.Dive)
                body.linearVelocity = new Vector3(0f, -slamSpeed, 0f);
            else if (Time.time < hangUntil && !animator.GetBool("IsGrounded") && velocity.y < -1f)
                body.linearVelocity = new Vector3(velocity.x, -1f, velocity.z); //Plutesc putin in timpul atacului
        }

        private IEnumerator SlamRoutine()
        {
            slamming = true;
            animator.SetBool("AirSlam", true);
            animator.SetBool("AirCombat", true);
            StartCoroutine(TurnToCamera());
            if (CombatFeedback.Instance != null)
                CombatFeedback.Instance.FovKick(6f);

            slamPhase = SlamPhase.Hang;
            yield return new WaitForSeconds(slamHangTime);

            slamPhase = SlamPhase.Dive;
            float timeout = Time.time + 2f;
            while (!animator.GetBool("IsGrounded") && Time.time < timeout)
                yield return null;
            slamPhase = SlamPhase.None;

            Land();
            yield return new WaitForSeconds(slamHoldTime);
            animator.SetBool("AirSlam", false);
            animator.SetBool("AirCombat", false);
            slamming = false;
        }

        private void Land()
        {
            if (socket.HasWeapon)
            {
                CraterSpawner spawner = socket.CurrentWeapon.GetComponentInChildren<CraterSpawner>();
                Vector3 origin = body.position + body.transform.forward * 1.2f + Vector3.up;
                if (spawner != null && FindGround(origin, out RaycastHit hit))
                    spawner.SpawnCrater(Quaternion.FromToRotation(Vector3.up, hit.normal), hit.point + hit.normal * 0.05f);
            }
            if (shockwavePrefab != null)
                Destroy(Instantiate(shockwavePrefab, body.position, Quaternion.identity), 2.5f);
            CombatFeedback feedback = CombatFeedback.Instance;
            if (feedback != null)
            {
                feedback.Hitstop(0.08f);
                feedback.Shake(0.5f, 0.45f);
                feedback.Flash(1f);
                feedback.SlowMotion(0.35f, 0.25f);
            }
        }

        private bool FindGround(Vector3 origin, out RaycastHit ground) //Cel mai apropiat sol, fara trigger-e si fara colliderele playerului
        {
            ground = default;
            bool found = false;
            float best = float.MaxValue;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, hitBuffer, 200f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (hitBuffer[i].collider.transform.IsChildOf(body.transform) || hitBuffer[i].distance >= best)
                    continue;
                best = hitBuffer[i].distance;
                ground = hitBuffer[i];
                found = true;
            }
            return found;
        }

        private IEnumerator TurnToCamera()
        {
            Quaternion start = body.rotation;
            Quaternion target = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
            for (float t = 0f; t < 0.15f; t += Time.fixedDeltaTime)
            {
                body.MoveRotation(Quaternion.Slerp(start, target, t / 0.15f));
                yield return new WaitForFixedUpdate();
            }
            body.MoveRotation(target);
        }
    }
}
