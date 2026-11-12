using System.Collections;
using Alpha.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace Alpha.World
{
    // Bara de viata a boss-ului, jos pe centru. Urmareste HP-ul, iar o bara mai deschisa
    // ramane in urma ca sa se vada lovitura. La moarte se goleste si dispare.
    public class BossHealthBar : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] Image fill;
        [SerializeField] Image trail;
        [SerializeField] Text nameLabel;
        [SerializeField] float fadeTime = 0.6f;
        [SerializeField] float fillSpeed = 2.5f;
        [SerializeField] float trailDelay = 0.6f;
        [SerializeField] float trailSpeed = 0.5f;
        [SerializeField] float hideDelay = 1.5f;

        BossHealthReader reader;
        float shown, trailHold;
        bool dying;

        void Awake() => SetAlpha(0f);

        public void Show(HittableCharacter boss, string title)
        {
            StopAllCoroutines();
            reader = new BossHealthReader(boss);
            dying = false;
            if (nameLabel != null && !string.IsNullOrEmpty(title)) nameLabel.text = title;
            shown = reader.Fraction;
            SetFill(fill, shown);
            SetFill(trail, shown);
            gameObject.SetActive(true);
            StartCoroutine(fade(1f, 0f));
        }

        public void Hide(float delay)
        {
            StopAllCoroutines();
            StartCoroutine(fade(0f, delay));
        }

        void Update()
        {
            if (reader == null || dying) return;
            float target = reader.IsAlive ? reader.Fraction : 0f;
            if (target < shown) trailHold = trailDelay;
            shown = Mathf.MoveTowards(shown, target, fillSpeed * Time.deltaTime);
            SetFill(fill, shown);

            // bara din urma asteapta putin, apoi coboara lin
            if (trail != null)
            {
                trailHold -= Time.deltaTime;
                float current = trail.fillAmount;
                if (current < shown) current = shown;
                else if (trailHold <= 0f) current = Mathf.MoveTowards(current, shown, trailSpeed * Time.deltaTime);
                SetFill(trail, current);
            }

            float trailLeft = trail != null ? trail.fillAmount : shown;
            if (!reader.IsAlive && shown <= 0f && trailLeft <= 0f)
            {
                dying = true;
                Hide(hideDelay);
            }
        }

        IEnumerator fade(float to, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            float from = group != null ? group.alpha : 0f;
            for (float t = 0f; t < fadeTime; t += Time.deltaTime)
            {
                SetAlpha(Mathf.Lerp(from, to, t / fadeTime));
                yield return null;
            }
            SetAlpha(to);
            if (to <= 0f) reader = null;
        }

        void SetAlpha(float a)
        {
            if (group != null) group.alpha = a;
        }

        static void SetFill(Image image, float amount)
        {
            if (image != null) image.fillAmount = amount;
        }
    }
}
