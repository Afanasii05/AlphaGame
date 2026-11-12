using System;
using System.Collections;
using Alpha.Enemies;
using Alpha.Input;
using Alpha.Interfaces;
using UnityEngine;
using UnityEngine.AI;

namespace Alpha.World
{
    public class Arena1Encounter : MonoBehaviour
    {
        [SerializeField] RiddleStone riddle;
        [SerializeField] ArenaSeal seal;
        [SerializeField] GameObject bossPrefab;
        [SerializeField] Transform bossSpawn;
        [SerializeField] ParticleSystem dustPrefab;
        [SerializeField] BossIntro intro;
        [SerializeField] BossHealthBar healthBar;
        [SerializeField] GolemHeart heart;
        [SerializeField] Arena1Carving carving;
        [SerializeField] Road2Gate gate;
        [SerializeField] string bossName = "The Sleeper";
        [SerializeField] float lockDuration = 1.2f;
        [SerializeField] float stoneSinkDepth = 6f;
        [SerializeField] float stoneSinkDuration = 3f;
        [SerializeField] float bossRiseDepth = 4.5f;
        [SerializeField] float bossRiseDuration = 2.6f;
        [SerializeField] float openDelay = 2f;
        [SerializeField] float heartDelay = 1.6f;

        public event Action onArenaCleared;
        public HittableCharacter Boss { get; private set; }

        bool started;

        void OnEnable() { if (riddle != null) riddle.onRiddleSolved += HandleRiddleSolved; }
        void OnDisable() { if (riddle != null) riddle.onRiddleSolved -= HandleRiddleSolved; }

        void HandleRiddleSolved()
        {
            if (started) return;
            started = true;
            StartCoroutine(runEncounter());
        }

        IEnumerator runEncounter()
        {
            InputHandler input = riddle.SolvedBy;
            if (input != null) input.disableInputs();
            Rumble(0.04f, 0.6f);
            yield return new WaitForSeconds(lockDuration);

            seal.Seal();
            StartCoroutine(sinkStone());
            yield return new WaitForSeconds(seal.SealDuration);

            // boss-ul iese din pamant in timp ce camera il prezinta
            Coroutine rise = StartCoroutine(spawnBoss());
            if (Boss != null && intro != null) intro.Play(bossSpawn.position, bossSpawn.forward);
            yield return rise;
            while (intro != null && intro.IsPlaying) yield return null;

            WakeBoss();
            if (input != null) input.enableInputs();
            if (Boss == null)
            {
                Debug.LogError("[Arena1Encounter] Boss-ul nu a putut fi creat, arena se redeschide.");
            }
            else if (healthBar != null)
            {
                healthBar.Show(Boss, bossName);
            }

            // EnemyAtributes se scoate singur din lista cand moare
            Vector3 fallen = bossSpawn != null ? bossSpawn.position : transform.position;
            while (Boss != null && HittableCharacter.enemies.Contains(Boss))
            {
                fallen = Boss.transform.position;
                yield return new WaitForSeconds(0.25f);
            }

            // din golem ramane inima: jucatorul o prinde cu ciocanul aruncat si o cioplim in ciocan
            if (heart != null)
            {
                yield return new WaitForSeconds(heartDelay);
                heart.Rise(fallen, transform.position);
                while (!heart.IsDelivered) yield return null;
                if (carving != null) yield return carving.Play(heart, FindInput(input));
            }
            else
            {
                yield return new WaitForSeconds(openDelay);
            }
            seal.Open();
            if (gate != null) gate.Open();
            onArenaCleared?.Invoke();
        }

        static InputHandler FindInput(InputHandler known)
        {
            if (known != null) return known;
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) return FindAnyObjectByType<InputHandler>();
            InputHandler handler = player.GetComponentInParent<InputHandler>();
            return handler != null ? handler : player.GetComponentInChildren<InputHandler>();
        }

        IEnumerator sinkStone()
        {
            Transform stone = riddle.transform;
            Vector3 from = stone.position;
            Vector3 to = from + Vector3.down * stoneSinkDepth;
            SpawnDust(from);
            for (float t = 0f; t < stoneSinkDuration; t += Time.deltaTime)
            {
                float k = t / stoneSinkDuration;
                Vector2 shake = UnityEngine.Random.insideUnitCircle * 0.06f;
                stone.position = Vector3.Lerp(from, to, k * k) + new Vector3(shake.x, 0f, shake.y);
                yield return null;
            }
            stone.gameObject.SetActive(false);
        }

        IEnumerator spawnBoss()
        {
            if (bossPrefab == null || bossSpawn == null) yield break;
            Vector3 pos = bossSpawn.position;
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 8f, NavMesh.AllAreas)) pos = hit.position;
            else Debug.LogWarning("[Arena1Encounter] Nu exista NavMesh langa BossSpawn.");

            // GolemStonesSlots dezactiveaza golemul in Awake, il trezim noi dupa ce oprim EnemyAI
            GameObject boss = Instantiate(bossPrefab, pos, bossSpawn.rotation);
            EnemyAI ai = boss.GetComponent<EnemyAI>();
            NavMeshAgent agent = boss.GetComponent<NavMeshAgent>();
            if (ai != null) ai.enabled = false;
            if (agent != null) agent.enabled = false;
            GolemStonesSlots slots = boss.GetComponent<GolemStonesSlots>();
            if (slots != null) slots.Activate();
            else boss.SetActive(true);
            Boss = boss.GetComponent<HittableCharacter>();

            Vector3 under = pos + Vector3.down * bossRiseDepth;
            boss.transform.position = under;
            SpawnDust(pos);
            Rumble(0.12f, 0.4f);
            CameraRumble rumble = CameraRumble.Main;
            if (rumble != null) rumble.ImpulseAt(pos, 0.35f, 0.6f, 10f, 60f);
            for (float t = 0f; t < bossRiseDuration; t += Time.deltaTime)
            {
                float k = t / bossRiseDuration;
                boss.transform.position = Vector3.Lerp(under, pos, 1f - (1f - k) * (1f - k));
                yield return null;
            }
            boss.transform.position = pos;
            SpawnDust(pos);
            Rumble(0f, 0.8f);
        }

        void WakeBoss()
        {
            if (Boss == null) return;
            NavMeshAgent agent = Boss.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = true;
                agent.Warp(Boss.transform.position);
            }
            EnemyAI ai = Boss.GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.enabled = true;
                ai.Aggro();
            }
        }

        void SpawnDust(Vector3 point)
        {
            if (dustPrefab == null) return;
            ParticleSystem fx = Instantiate(dustPrefab, point, Quaternion.identity);
            fx.Play(true);
            Destroy(fx.gameObject, 3f);
        }

        static void Rumble(float strength, float fade)
        {
            CameraRumble rumble = CameraRumble.Main;
            if (rumble != null) rumble.SetRumble(strength, fade);
        }
    }
}
