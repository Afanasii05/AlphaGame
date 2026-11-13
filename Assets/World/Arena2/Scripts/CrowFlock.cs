using Alpha.Input;
using UnityEngine;

namespace Alpha.World
{
    // Ciori care ciugulesc printre morti si se ridica in stol cand se apropie jucatorul.
    // Dupa o vreme se intorc pe acelasi loc, daca jucatorul s-a indepartat.
    public class CrowFlock : MonoBehaviour
    {
        [SerializeField] Transform[] crows;
        [SerializeField] float scareRadius = 9f;
        [SerializeField] float flySpeed = 9f;
        [SerializeField] float climb = 0.45f;
        [SerializeField] float flightTime = 7f;
        [SerializeField] float returnDelay = 25f;
        [SerializeField] float flapSpeed = 16f;
        [SerializeField] AudioClip cawClip;

        class Crow
        {
            public Transform body, wingL, wingR;
            public Vector3 home;
            public Quaternion homeRot;
            public Vector3 dir;
            public float delay, peck, phase, fold = 1f;
        }

        Crow[] flock;
        Transform player;
        bool flying;
        float timer;

        void Awake()
        {
            Init();
        }

        void Init()
        {
            flock = new Crow[crows != null ? crows.Length : 0];
            for (int i = 0; i < flock.Length; i++)
            {
                Transform c = crows[i];
                if (c == null) continue;
                flock[i] = new Crow
                {
                    body = c,
                    wingL = c.Find("WingL"),
                    wingR = c.Find("WingR"),
                    home = c.position,
                    homeRot = c.rotation,
                    phase = Random.value * 10f,
                    peck = Random.Range(0.5f, 3f)
                };
            }
        }

        void Update()
        {
            if (flock == null) Init();
            if (player == null)
            {
                InputHandler input = FindAnyObjectByType<InputHandler>();
                if (input != null) player = input.transform;
            }
            timer += Time.deltaTime;
            if (!flying)
            {
                Ground();
                if (player != null && (player.position - transform.position).sqrMagnitude < scareRadius * scareRadius) TakeOff();
            }
            else
            {
                Fly();
                if (timer > returnDelay && (player == null || (player.position - transform.position).sqrMagnitude > scareRadius * scareRadius * 9f)) Land();
            }
        }

        void Ground()
        {
            foreach (Crow c in flock)
            {
                if (c == null) continue;
                // ciugulit scurt, din cand in cand
                c.peck -= Time.deltaTime;
                float dip = 0f;
                if (c.peck < 0f)
                {
                    dip = Mathf.Sin(-c.peck * 18f) * 18f;
                    if (c.peck < -0.35f) c.peck = Random.Range(0.8f, 3.5f);
                }
                c.body.rotation = c.homeRot * Quaternion.Euler(Mathf.Max(0f, dip), 0f, 0f);
                c.fold = Mathf.MoveTowards(c.fold, 1f, Time.deltaTime * 3f);
                SetWings(c, 4f);
            }
        }

        void TakeOff()
        {
            flying = true;
            timer = 0f;
            Vector3 away = player != null ? transform.position - player.position : transform.forward;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
            away.Normalize();
            foreach (Crow c in flock)
            {
                if (c == null) continue;
                Vector3 d = Quaternion.Euler(0f, Random.Range(-55f, 55f), 0f) * away;
                c.dir = (d + Vector3.up * Random.Range(climb * 0.7f, climb * 1.4f)).normalized;
                c.delay = Random.Range(0f, 0.45f);
                c.body.gameObject.SetActive(true);
            }
            if (cawClip != null) AudioSource.PlayClipAtPoint(cawClip, transform.position);
        }

        void Fly()
        {
            foreach (Crow c in flock)
            {
                if (c == null) continue;
                if (timer < c.delay)
                {
                    SetWings(c, 4f);
                    continue;
                }
                c.fold = Mathf.MoveTowards(c.fold, 0f, Time.deltaTime * 5f);
                float t = timer - c.delay;
                if (t > flightTime)
                {
                    if (c.body.gameObject.activeSelf) c.body.gameObject.SetActive(false);
                    continue;
                }
                // arc: urca abrupt la inceput, apoi se indreapta si vireaza usor
                Vector3 dir = Quaternion.Euler(0f, Mathf.Sin(t * 0.6f + c.phase) * 25f * Time.deltaTime, 0f) * c.dir;
                dir.y = Mathf.Lerp(dir.y, 0.12f, Time.deltaTime * 0.4f);
                c.dir = dir.normalized;
                float speed = flySpeed * Mathf.Clamp01(0.4f + t * 0.8f);
                c.body.position += c.dir * speed * Time.deltaTime;
                Vector3 flat = new Vector3(c.dir.x, c.dir.y * 0.5f, c.dir.z);
                c.body.rotation = Quaternion.LookRotation(flat, Vector3.up);
                SetWings(c, Mathf.Sin((t + c.phase) * flapSpeed) * 55f);
            }
        }

        void Land()
        {
            flying = false;
            timer = 0f;
            foreach (Crow c in flock)
            {
                if (c == null) continue;
                c.body.position = c.home;
                c.body.rotation = c.homeRot;
                c.fold = 1f;
                c.body.gameObject.SetActive(true);
            }
        }

        // aripile stau stranse pe spate la sol (fold = 1) si se desfac in zbor
        static void SetWings(Crow c, float angle)
        {
            float back = c.fold * 82f;
            if (c.wingL != null) c.wingL.localRotation = Quaternion.Euler(0f, -back, 0f) * Quaternion.Euler(0f, 0f, -angle);
            if (c.wingR != null) c.wingR.localRotation = Quaternion.Euler(0f, back, 0f) * Quaternion.Euler(0f, 0f, angle);
        }
    }
}
