using System.Collections;
using Alpha.Combat;
using Alpha.Input;
using UnityEngine;
namespace Alpha.Player
{
    [DefaultExecutionOrder(100)]
    public class PlayerDash : MonoBehaviour
    {
        [SerializeField] private GameObject dustPrefab;
        [SerializeField] private Material ghostMaterial;
        [SerializeField] private float dashSpeed = 22f;
        [SerializeField] private float dashDuration = 0.18f;
        [SerializeField] private float dashCooldown = 0.6f;
        [SerializeField] private float ghostInterval = 0.035f;
        [SerializeField] private float ghostLifetime = 0.3f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public bool IsDashing { get; private set; }

        private InputHandler inputHandler;
        private ExtraInputHandler extraInput;
        private PlayerCombatState combatState;
        private Animator animator;
        private Rigidbody body;
        private CapsuleCollider capsule;
        private SkinnedMeshRenderer[] skins;
        private Vector2 moveInput;
        private Vector3 dashDirection;
        private bool airDash;
        private bool airDashUsed;
        private float dashEnd;
        private float nextDash;
        private float nextGhost;

        private void Awake()
        {
            inputHandler = GetComponentInParent<InputHandler>();
            extraInput = GetComponent<ExtraInputHandler>();
            combatState = inputHandler.GetComponent<PlayerCombatState>();
            animator = inputHandler.GetComponentInChildren<Animator>();
            body = inputHandler.GetComponent<Rigidbody>();
            capsule = inputHandler.GetComponent<CapsuleCollider>();
            skins = animator.GetComponentsInChildren<SkinnedMeshRenderer>();
        }

        private void OnEnable()
        {
            extraInput.onDash += HandleDash;
            inputHandler.onMoveUpdate += HandleMove;
        }

        private void OnDisable()
        {
            extraInput.onDash -= HandleDash;
            inputHandler.onMoveUpdate -= HandleMove;
            IsDashing = false;
        }

        private void HandleMove(Vector2 move) { moveInput = move; }

        private void HandleDash()
        {
            if (IsDashing || Time.time < nextDash || body.isKinematic)
                return;
            CombatAction action = combatState.CurrentAction();
            if (action != CombatAction.None && action != CombatAction.NonCombat && action != CombatAction.NonSemiCombat)
                return;
            bool grounded = animator.GetBool("IsGrounded");
            if (!grounded && airDashUsed)
                return;
            airDash = !grounded;
            airDashUsed = airDash || airDashUsed;
            dashDirection = DashDirection();
            IsDashing = true;
            dashEnd = Time.time + dashDuration;
            nextDash = Time.time + dashCooldown;
            nextGhost = 0f;
            if (!airDash && dustPrefab != null)
                Instantiate(dustPrefab, PlayerFeet.Position(capsule), Quaternion.identity);
            if (CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.FovKick(8f);
                CombatFeedback.Instance.Shake(0.08f, 0.1f);
            }
        }

        private Vector3 DashDirection()
        {
            Vector3 direction = body.transform.forward;
            if (moveInput.sqrMagnitude > 0.01f)
            {
                Transform cam = Camera.main.transform;
                Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
                Vector3 right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
                direction = forward * moveInput.y + right * moveInput.x;
            }
            direction.y = 0f;
            return direction.normalized;
        }

        private void FixedUpdate()
        {
            if (!IsDashing)
                return;
            if (Time.time >= dashEnd || body.isKinematic)
            {
                IsDashing = false;
                return;
            }
            Vector3 velocity = dashDirection * dashSpeed;
            velocity.y = airDash ? 0f : body.linearVelocity.y;
            body.linearVelocity = velocity;
            animator.SetFloat("Speed", 8f); //Dupa PlayerController: pornesc animatia de alergare
        }

        private void Update()
        {
            if (animator.GetBool("IsGrounded") && !IsDashing)
                airDashUsed = false;
            if (IsDashing && Time.time >= nextGhost)
            {
                SpawnGhosts();
                nextGhost = Time.time + ghostInterval;
            }
        }

        private void SpawnGhosts()
        {
            if (ghostMaterial == null)
                return;
            foreach (SkinnedMeshRenderer skin in skins)
            {
                if (!skin.enabled || !skin.gameObject.activeInHierarchy)
                    continue;
                Mesh mesh = new Mesh();
                skin.BakeMesh(mesh, true);
                GameObject ghost = new GameObject("AfterImage");
                ghost.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
                ghost.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = ghost.AddComponent<MeshRenderer>();
                Material material = new Material(ghostMaterial);
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                StartCoroutine(FadeGhost(ghost, mesh, material));
            }
        }

        private IEnumerator FadeGhost(GameObject ghost, Mesh mesh, Material material)
        {
            Color color = material.GetColor(BaseColorId);
            float startAlpha = color.a;
            for (float t = 0f; t < ghostLifetime; t += Time.deltaTime)
            {
                color.a = Mathf.Lerp(startAlpha, 0f, t / ghostLifetime);
                material.SetColor(BaseColorId, color);
                yield return null;
            }
            Destroy(ghost);
            Destroy(mesh);
            Destroy(material);
        }
    }
}
