using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Alpha.World
{
    // Indiciile de jos ale Arenei 2 (in stilul Arenei 1) si ecranul negru pentru reluare si cioplire.
    public class Arena2Screen : MonoBehaviour
    {
        [SerializeField] Text hint;
        [SerializeField] Image fade;
        [SerializeField] float hintFade = 0.4f;

        CanvasGroup group;
        string current;
        float until, alpha;

        void Awake()
        {
            if (hint != null)
            {
                group = hint.GetComponent<CanvasGroup>();
                if (group == null) group = hint.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                hint.gameObject.SetActive(false);
            }
            SetFade(0f);
        }

        public void Show(string message, float seconds)
        {
            if (hint == null) return;
            current = message;
            hint.text = message;
            until = Time.unscaledTime + seconds;
            hint.gameObject.SetActive(true);
        }

        // ascunde doar daca mesajul e inca cel afisat
        public void Hide(string message)
        {
            if (message == current) until = 0f;
        }

        void Update()
        {
            if (group == null) return;
            float target = Time.unscaledTime < until ? 1f : 0f;
            alpha = Mathf.MoveTowards(alpha, target, Time.unscaledDeltaTime / hintFade);
            group.alpha = alpha;
            if (alpha <= 0f && target <= 0f && hint.gameObject.activeSelf) hint.gameObject.SetActive(false);
        }

        public IEnumerator FadeTo(float to, float time)
        {
            float from = fade != null ? fade.color.a : 0f;
            for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
            {
                float k = t / time;
                SetFade(Mathf.Lerp(from, to, k * k * (3f - 2f * k)));
                yield return null;
            }
            SetFade(to);
        }

        void SetFade(float amount)
        {
            if (fade == null) return;
            Color c = fade.color;
            c.a = amount;
            fade.color = c;
            fade.enabled = amount > 0.001f;
        }
    }
}
