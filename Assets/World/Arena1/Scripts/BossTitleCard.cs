using System.Collections;
using UnityEngine;

namespace Alpha.World
{
    // Titlul boss-ului: numele apare cu literele strangandu-se, linia ornamentala se deschide din centru,
    // subtitlul vine putin mai tarziu, apoi totul se stinge.
    public class BossTitleCard : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] CanvasGroup subtitleGroup;
        [SerializeField] UILetterSpacing titleSpacing;
        [SerializeField] RectTransform ornament;
        [SerializeField] float spacingFrom = 26f;
        [SerializeField] float spacingTo = 14f;
        [SerializeField] float fadeIn = 0.7f;
        [SerializeField] float subtitleDelay = 0.45f;
        [SerializeField] float hold = 2.2f;
        [SerializeField] float fadeOut = 0.9f;

        public float Duration => fadeIn + hold + fadeOut;

        void Awake() => Hide();

        public void Show()
        {
            StopAllCoroutines();
            gameObject.SetActive(true);
            StartCoroutine(play());
        }

        public void Hide()
        {
            StopAllCoroutines();
            ResetVisuals();
        }

        // fara StopAllCoroutines, altfel corutina s-ar opri singura
        void ResetVisuals()
        {
            if (group != null) group.alpha = 0f;
            if (subtitleGroup != null) subtitleGroup.alpha = 0f;
            if (ornament != null) ornament.localScale = new Vector3(0f, 1f, 1f);
        }

        IEnumerator play()
        {
            ResetVisuals();
            float total = fadeIn + hold;
            for (float t = 0f; t < total; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / fadeIn);
                float ease = 1f - (1f - k) * (1f - k);
                if (group != null) group.alpha = ease;
                if (titleSpacing != null) titleSpacing.Spacing = Mathf.Lerp(spacingFrom, spacingTo, 1f - Mathf.Pow(1f - Mathf.Clamp01(t / (fadeIn * 2.5f)), 3f));
                if (ornament != null) ornament.localScale = new Vector3(Smooth(Mathf.Clamp01((t - fadeIn * 0.3f) / (fadeIn * 1.4f))), 1f, 1f);
                if (subtitleGroup != null) subtitleGroup.alpha = Smooth(Mathf.Clamp01((t - subtitleDelay) / fadeIn));
                yield return null;
            }
            if (subtitleGroup != null) subtitleGroup.alpha = 1f;
            for (float t = 0f; t < fadeOut; t += Time.deltaTime)
            {
                if (group != null) group.alpha = 1f - Smooth(t / fadeOut);
                yield return null;
            }
            ResetVisuals();
        }

        static float Smooth(float k) => k * k * (3f - 2f * k);
    }
}
