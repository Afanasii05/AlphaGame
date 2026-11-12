using System;
using Alpha.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Alpha.World
{
    [RequireComponent(typeof(SphereCollider))]
    public class RiddleStone : MonoBehaviour
    {
        [SerializeField] string riddleTitle = "The Sleeper's Stone";
        [SerializeField, TextArea(4, 8)] string riddleText =
            "The proud who stand before me fall.\nThe loud who strike me wake me not.\nLower your blade, bow down and wait,\nand stone shall answer stone.";
        [SerializeField] string idleHint = "Words are carved into the stone...";
        [SerializeField] string listeningHint = "The stone is listening...";
        [SerializeField] float holdTime = 3f;
        [SerializeField] float solveDistance = 4f;
        [SerializeField] float maxFacingAngle = 60f;
        [SerializeField] TextMesh riddleLabel;
        [SerializeField] Text screenHint;
        [SerializeField] Renderer[] glyphs;
        [SerializeField] Light glowLight;
        [SerializeField] float glowLightBoost = 6f;
        [SerializeField] float glyphBoost = 3f;
        [SerializeField] float kneelShake = 0.06f;

        public event Action onRiddleSolved;
        public InputHandler SolvedBy { get; private set; }

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        InputHandler input;
        Transform player;
        CameraShake cameraShake;
        Material[] glyphMats;
        Color[] glyphBase;
        float lightBase, holdTimer, releaseRadius;
        bool kneeling, solved;

        void Awake()
        {
            SphereCollider trigger = GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            releaseRadius = trigger.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z) + 1f;

            glyphMats = new Material[glyphs.Length];
            glyphBase = new Color[glyphs.Length];
            for (int i = 0; i < glyphs.Length; i++)
            {
                glyphMats[i] = glyphs[i].material;
                glyphBase[i] = glyphMats[i].GetColor(EmissionId);
            }
            if (glowLight != null) lightBase = glowLight.intensity;
            if (riddleLabel != null) riddleLabel.text = riddleTitle.ToUpper() + "\n\n" + riddleText;
            ShowUI(false);
            SetGlow(0f);
        }

        void OnDisable() => Release();

        void OnTriggerEnter(Collider other)
        {
            if (solved || input != null) return;
            InputHandler handler = other.GetComponentInParent<InputHandler>();
            if (handler == null) return;
            input = handler;
            player = handler.transform;
            input.onCrouchHold += HandleCrouch;
            input.onClick += HandleClick;
            if (cameraShake == null && Camera.main != null) cameraShake = Camera.main.GetComponent<CameraShake>();
            ShowUI(true);
        }

        void Update()
        {
            if (solved || player == null) return;
            Vector3 toStone = transform.position - player.position;
            toStone.y = 0f;
            float distance = toStone.magnitude;
            if (distance > releaseRadius) { Release(); return; }

            Vector3 facing = Vector3.ProjectOnPlane(player.forward, Vector3.up);
            bool bowing = kneeling && distance <= solveDistance && Vector3.Angle(facing, toStone) <= maxFacingAngle;
            holdTimer = bowing ? holdTimer + Time.deltaTime : Mathf.MoveTowards(holdTimer, 0f, Time.deltaTime);

            float k = Mathf.Clamp01(holdTimer / holdTime);
            SetGlow(k);
            if (screenHint != null) screenHint.text = bowing ? listeningHint : idleHint;
            if (bowing && cameraShake != null) cameraShake.Shake(kneelShake * k, 0.2f);
            if (holdTimer >= holdTime) Solve();
        }

        void LateUpdate()
        {
            // textul se intoarce mereu spre camera
            if (riddleLabel == null || !riddleLabel.gameObject.activeSelf || Camera.main == null) return;
            Transform label = riddleLabel.transform;
            label.rotation = Quaternion.LookRotation(label.position - Camera.main.transform.position);
        }

        void HandleCrouch(bool held)
        {
            kneeling = held;
            if (!held) holdTimer = 0f;
        }

        void HandleClick() => holdTimer = 0f;   // cine loveste piatra nu o trezeste

        void Solve()
        {
            solved = true;
            SolvedBy = input;
            SetGlow(1f);
            if (cameraShake != null) cameraShake.Shake(0.25f, 0.6f);
            Release();
            onRiddleSolved?.Invoke();
        }

        void Release()
        {
            if (input == null) return;
            input.onCrouchHold -= HandleCrouch;
            input.onClick -= HandleClick;
            input = null;
            player = null;
            kneeling = false;
            holdTimer = 0f;
            ShowUI(false);
            if (!solved) SetGlow(0f);
        }

        void ShowUI(bool on)
        {
            if (riddleLabel != null) riddleLabel.gameObject.SetActive(on && !solved);
            if (screenHint != null)
            {
                screenHint.text = idleHint;
                screenHint.gameObject.SetActive(on && !solved);
            }
        }

        void SetGlow(float k)
        {
            float boost = Mathf.Lerp(0.15f, glyphBoost, k);
            for (int i = 0; i < glyphMats.Length; i++) glyphMats[i].SetColor(EmissionId, glyphBase[i] * boost);
            if (glowLight != null) glowLight.intensity = lightBase + glowLightBoost * k;
        }
    }
}
