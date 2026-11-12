using Alpha.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Alpha.World
{
    // Tablita de piatra de pe marginea drumului: cand jucatorul se apropie, inscriptia apare pe ecran.
    // Fara collider: distanta se verifica direct, ca la pragul castelului.
    public class LoreTablet : MonoBehaviour
    {
        [SerializeField] string title = "The Field of Broken Oaths";
        [SerializeField, TextArea(4, 10)] string body;
        [SerializeField] float radius = 4.5f;
        [SerializeField] float fadeSpeed = 2.5f;
        [SerializeField] CanvasGroup panel;
        [SerializeField] Text titleText;
        [SerializeField] Text bodyText;

        Transform player;
        float alpha;

        void Awake()
        {
            if (titleText != null) titleText.text = title.ToUpper();
            if (bodyText != null) bodyText.text = body;
            SetAlpha(0f);
        }

        void Update()
        {
            if (player == null)
            {
                InputHandler input = FindAnyObjectByType<InputHandler>();
                if (input != null) player = input.transform;
            }
            bool near = player != null && (player.position - transform.position).sqrMagnitude < radius * radius;
            float target = near ? 1f : 0f;
            if (Mathf.Approximately(alpha, target)) return;
            SetAlpha(Mathf.MoveTowards(alpha, target, fadeSpeed * Time.deltaTime));
        }

        void SetAlpha(float value)
        {
            alpha = value;
            if (panel == null) return;
            panel.alpha = value;
            panel.gameObject.SetActive(value > 0.001f);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.8f, 0.5f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
