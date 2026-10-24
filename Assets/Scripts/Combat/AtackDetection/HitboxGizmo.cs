using UnityEngine;

namespace Alpha.Combat
{
    // Deseneaza colliderele obiectului (si ale copiilor) in Scene view si in Game view (cu Gizmos pornit).
    // Rosu = trigger activ (hitbox de atac), Albastru = collider solid activ (hurtbox, sabia aruncata), Gri = dezactivat.
    public class HitboxGizmo : MonoBehaviour
    {
        [SerializeField] private bool includeChildren = true;
        [SerializeField] private bool onlyWhenSelected = false;
        [SerializeField] private bool showDisabled = true;
        [SerializeField] private Color triggerColor = new Color(1f, 0.2f, 0.2f, 0.35f);
        [SerializeField] private Color solidColor = new Color(0.2f, 0.6f, 1f, 0.3f);
        [SerializeField] private Color disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.12f);

        void OnDrawGizmos() { if (!onlyWhenSelected) Draw(); }
        void OnDrawGizmosSelected() { if (onlyWhenSelected) Draw(); }

        void Draw()
        {
            Collider[] cols = includeChildren ? GetComponentsInChildren<Collider>(true) : GetComponents<Collider>();
            foreach (Collider c in cols)
            {
                bool active = c.enabled && c.gameObject.activeInHierarchy;
                if (!active && !showDisabled) continue;

                Color col = !active ? disabledColor : (c.isTrigger ? triggerColor : solidColor);
                Gizmos.matrix = c.transform.localToWorldMatrix;

                switch (c)
                {
                    case BoxCollider b: DrawBox(b.center, b.size, col); break;
                    case SphereCollider s: DrawSphere(s.center, s.radius, col); break;
                    case CapsuleCollider cap: DrawCapsule(cap, col); break;
                    default:
                        Gizmos.matrix = Matrix4x4.identity;
                        DrawBox(c.bounds.center, c.bounds.size, col);
                        break;
                }
            }
            Gizmos.matrix = Matrix4x4.identity;
        }

        void DrawBox(Vector3 center, Vector3 size, Color col)
        {
            Gizmos.color = col;
            Gizmos.DrawCube(center, size);
            Gizmos.color = new Color(col.r, col.g, col.b, 1f);
            Gizmos.DrawWireCube(center, size);
        }

        void DrawSphere(Vector3 center, float radius, Color col)
        {
            Gizmos.color = col;
            Gizmos.DrawSphere(center, radius);
            Gizmos.color = new Color(col.r, col.g, col.b, 1f);
            Gizmos.DrawWireSphere(center, radius);
        }

        void DrawCapsule(CapsuleCollider cap, Color col)
        {
            Vector3 axis = cap.direction == 0 ? Vector3.right : cap.direction == 1 ? Vector3.up : Vector3.forward;
            Vector3 u = cap.direction == 0 ? Vector3.up : Vector3.right;
            Vector3 v = Vector3.Cross(axis, u);
            float r = cap.radius;
            float half = Mathf.Max(0f, cap.height * 0.5f - r);
            Vector3 top = cap.center + axis * half;
            Vector3 bottom = cap.center - axis * half;

            DrawSphere(top, r, col);
            DrawSphere(bottom, r, col);

            Gizmos.color = new Color(col.r, col.g, col.b, 1f);
            Gizmos.DrawLine(top + u * r, bottom + u * r);
            Gizmos.DrawLine(top - u * r, bottom - u * r);
            Gizmos.DrawLine(top + v * r, bottom + v * r);
            Gizmos.DrawLine(top - v * r, bottom - v * r);
        }
    }
}
