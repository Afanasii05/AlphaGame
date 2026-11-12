using System;
using System.Collections;
using Alpha.Combat;
using Alpha.Input;
using UnityEngine;

namespace Alpha.World
{
    // Arena 2: poarta se tranteste, cavalerul se ridica de pe tron, prezentarea, lupta.
    // Cand ingenuncheaza, bara dispare; portile se deschid abia dupa cioplire (Arena2Seals cheama OpenGates).
    // Daca jucatorul moare, Arena2Respawn cheama ResetEncounter si totul se poate relua.
    public class Arena2Encounter : MonoBehaviour
    {
        [SerializeField] CastleGate gate;
        [SerializeField] CastleGate postern;
        [SerializeField] CastleGateTrigger trigger;
        [SerializeField] OathboundKnight knight;
        [SerializeField] KnightAI ai;
        [SerializeField] BossIntro intro;
        [SerializeField] BossHealthBar healthBar;
        [SerializeField] string bossName = "The Oathbound Knight";
        [SerializeField] float beat = 1f;
        [SerializeField] float calmBeat = 2.5f;
        [SerializeField] bool openMainGate = true;
        [SerializeField] bool waitForCarving = true;

        public event Action Defeated;
        public bool Started { get; private set; }
        public bool Cleared { get; private set; }
        public bool InFight => Started && !Cleared;
        public OathboundKnight Knight => knight;

        void OnEnable()
        {
            if (gate != null) gate.Closed += HandleGateClosed;
            if (knight != null) knight.Defeated += HandleDefeated;
        }

        void OnDisable()
        {
            if (gate != null) gate.Closed -= HandleGateClosed;
            if (knight != null) knight.Defeated -= HandleDefeated;
        }

        void HandleGateClosed()
        {
            if (Started || knight == null || ai == null) return;
            Started = true;
            StartCoroutine(runIntro());
        }

        IEnumerator runIntro()
        {
            InputHandler input = FindInput();
            if (input != null) input.disableInputs();
            ArmPlayer(input);
            yield return new WaitForSeconds(beat);

            Coroutine rise = ai.Rise();
            if (intro != null) intro.Play(knight.transform.position, knight.transform.forward);
            yield return rise;
            while (intro != null && intro.IsPlaying) yield return null;

            if (input != null) input.enableInputs();
            knight.Awaken();
            ai.Engage();
            if (healthBar != null) healthBar.Show(knight, bossName);
        }

        void HandleDefeated(OathboundKnight _)
        {
            if (Cleared) return;
            Cleared = true;
            Defeated?.Invoke();
            StartCoroutine(runOutro());
        }

        IEnumerator runOutro()
        {
            if (healthBar != null) healthBar.Hide(0.6f);
            if (waitForCarving) yield break;
            yield return new WaitForSeconds(calmBeat);
            OpenGates();
        }

        public void OpenGates()
        {
            if (postern != null) postern.Open();
            if (openMainGate && gate != null && gate.IsClosed) gate.Open();
        }

        // incercare noua: cavalerul pe tron, poarta deschisa, pragul armat
        public void ResetEncounter()
        {
            if (Cleared) return;
            StopAllCoroutines();
            Started = false;
            if (healthBar != null) healthBar.Hide(0f);
            if (knight != null) knight.ResetForRetry();
            if (ai != null) ai.ResetToThrone();
            if (gate != null) gate.Open();
            if (trigger != null) trigger.Rearm();
            InputHandler input = FindInput();
            if (input != null) input.enableInputs();
        }

        // ciocanul trebuie sa fie in mana cand incepe lupta (daca jucatorul il are deja deblocat)
        static void ArmPlayer(InputHandler input)
        {
            Transform player = input != null ? input.transform : null;
            if (player == null) return;
            WeaponSocket socket = player.GetComponentInChildren<WeaponSocket>();
            EnquipWeapon enquip = player.GetComponentInChildren<EnquipWeapon>();
            if (socket == null || enquip == null || socket.HasWeapon) return;
            enquip.Equip();
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
