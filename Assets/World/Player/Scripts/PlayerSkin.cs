using UnityEngine;

namespace Alpha.World
{
    // Alege corpul jucatorului: modelul rafinat sau cel original, pe acelasi schelet si acelasi Animator.
    // Corpul original doar se stinge, nu se scoate. TeleportToWeapon reaprinde toate corpurile dupa teleport,
    // asa ca varianta aleasa e reimpusa in LateUpdate (cat timp teleportul le tine ascunse, nu le atingem).
    public class PlayerSkin : MonoBehaviour
    {
        public enum Variant { Refined, Original }

        [SerializeField] Variant variant = Variant.Refined;
        [SerializeField] SkinnedMeshRenderer refined;
        [SerializeField] Renderer[] original = new Renderer[0];

        public Variant Current => variant;
        public SkinnedMeshRenderer RefinedBody => refined;
        public Renderer ActiveBody => variant == Variant.Refined && refined != null ? refined : original.Length > 0 ? original[0] : null;

        public void Setup(SkinnedMeshRenderer refinedBody, Renderer[] originalBodies)
        {
            refined = refinedBody;
            original = originalBodies ?? new Renderer[0];
        }

        public void Use(Variant next)
        {
            variant = next;
            Apply();
        }

        void Awake() => Apply();

        void LateUpdate()
        {
            if (Visible()) Apply();
        }

        bool Visible()
        {
            if (refined != null && refined.enabled) return true;
            foreach (Renderer body in original)
                if (body != null && body.enabled) return true;
            return false;
        }

        void Apply()
        {
            bool useRefined = variant == Variant.Refined && refined != null;
            if (refined != null && refined.enabled != useRefined) refined.enabled = useRefined;
            foreach (Renderer body in original)
                if (body != null && body.enabled == useRefined) body.enabled = !useRefined;
        }
    }
}
