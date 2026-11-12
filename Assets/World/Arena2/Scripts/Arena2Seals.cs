using System.Collections;
using Alpha.Input;
using UnityEngine;

namespace Alpha.World
{
    // Dupa ce cavalerul ingenuncheaza, cele trei peceti ale juramantului se aprind pe ziduri.
    // Fiecare pecete sparta trimite un fir de lumina in cavaler; dupa ultima urmeaza cioplirea, apoi se deschid portile.
    public class Arena2Seals : MonoBehaviour
    {
        [SerializeField] Arena2Encounter encounter;
        [SerializeField] OathSeal[] seals;
        [SerializeField] Arena2Carving carving;
        [SerializeField] Arena2Screen screen;
        [SerializeField] Light knightGlow;

        [Header("Ritm")]
        [SerializeField] float awakenDelay = 2.5f;
        [SerializeField] float lightStep = 0.8f;
        [SerializeField] float remindEvery = 20f;
        [SerializeField] float glowPerSeal = 1.6f;
        [SerializeField] float carveDelay = 0.6f;

        [Header("Indicii")]
        [SerializeField] string litHint = "Three oath-seals burn upon the walls. Break them with your hammer.";
        [SerializeField] string wardHint = "The last seal is warded. Throw your hammer into it... then call it back, or follow it.";
        [SerializeField] string lodgedHint = "The hammer is caught in the ward. Call it back... or follow it.";
        [SerializeField] string doneHint = "The second layer falls. The way beyond lies open.";

        int broken;
        float lastAction, glowTarget;
        bool running, lodgedShown;

        public bool Done { get; private set; }

        void OnEnable()
        {
            if (encounter != null) encounter.Defeated += HandleDefeated;
            foreach (OathSeal seal in seals)
                if (seal != null) seal.Broken += HandleBroken;
        }

        void OnDisable()
        {
            if (encounter != null) encounter.Defeated -= HandleDefeated;
            foreach (OathSeal seal in seals)
                if (seal != null) seal.Broken -= HandleBroken;
        }

        void HandleDefeated()
        {
            if (running || Done) return;
            running = true;
            StartCoroutine(awaken());
        }

        IEnumerator awaken()
        {
            yield return new WaitForSeconds(awakenDelay);
            Transform chest = Chest();
            foreach (OathSeal seal in seals)
            {
                if (seal == null) continue;
                seal.Ignite(chest);
                yield return new WaitForSeconds(lightStep);
            }
            Hint(litHint);
            while (!Done)
            {
                OathSeal last = Last();
                if (Time.time - lastAction > remindEvery)
                {
                    if (last != null && last.Current == OathSeal.State.Lodged) Hint(lodgedHint);
                    else if (broken >= seals.Length - 1) Hint(wardHint);
                    else Hint(litHint);
                }
                yield return null;
            }
        }

        void Update()
        {
            if (!running || Done) return;
            if (knightGlow != null)
            {
                knightGlow.intensity = Mathf.MoveTowards(knightGlow.intensity, glowTarget, Time.deltaTime * 2f);
                knightGlow.enabled = knightGlow.intensity > 0.01f;
            }
            OathSeal last = Last();
            if (!lodgedShown && last != null && last.Current == OathSeal.State.Lodged)
            {
                lodgedShown = true;
                Hint(lodgedHint);
            }
        }

        void HandleBroken(OathSeal seal)
        {
            broken++;
            if (seal.Warded || broken >= seals.Length)
            {
                StartCoroutine(finish(seal));
                return;
            }
            StartCoroutine(feed(seal.ThreadTime));
            if (broken == seals.Length - 1)
            {
                OathSeal last = Last();
                if (last != null) last.Unward();
                Hint(wardHint);
            }
            else lastAction = Time.time;
        }

        IEnumerator feed(float delay)
        {
            yield return new WaitForSeconds(delay);
            glowTarget += glowPerSeal;
        }

        IEnumerator finish(OathSeal seal)
        {
            if (screen != null)
            {
                screen.Hide(lodgedHint);
                screen.Hide(wardHint);
            }
            yield return feed(seal.ThreadTime);
            if (knightGlow != null) knightGlow.intensity = glowTarget;
            Done = true;
            yield return new WaitForSeconds(carveDelay);

            InputHandler input = FindInput();
            if (carving != null) yield return carving.Play(input);
            if (encounter != null) encounter.OpenGates();
            Hint(doneHint);
        }

        OathSeal Last() => seals != null && seals.Length > 0 ? seals[seals.Length - 1] : null;

        void Hint(string message)
        {
            lastAction = Time.time;
            if (screen != null) screen.Show(message, 6f);
        }

        Transform Chest()
        {
            if (encounter == null || encounter.Knight == null) return null;
            Animator animator = encounter.Knight.GetComponentInChildren<Animator>();
            Transform chest = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Chest) : null;
            return chest != null ? chest : encounter.Knight.transform;
        }

        static InputHandler FindInput()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) return FindAnyObjectByType<InputHandler>();
            InputHandler handler = player.GetComponentInParent<InputHandler>();
            return handler != null ? handler : player.GetComponentInChildren<InputHandler>();
        }
    }
}
