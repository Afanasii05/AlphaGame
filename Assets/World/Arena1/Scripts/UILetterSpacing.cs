using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Alpha.World
{
    // Spatiere intre litere pentru Text (UI legacy), pe un singur rand, centrat.
    // Se pune inaintea Outline/Shadow pe obiect, ca si conturul sa fie spatiat.
    [RequireComponent(typeof(Text))]
    public class UILetterSpacing : BaseMeshEffect
    {
        [SerializeField] float spacing = 8f;

        readonly List<UIVertex> verts = new List<UIVertex>();

        public float Spacing
        {
            get => spacing;
            set
            {
                if (Mathf.Approximately(spacing, value)) return;
                spacing = value;
                if (graphic != null) graphic.SetVerticesDirty();
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            verts.Clear();
            vh.GetUIVertexStream(verts);

            // 6 vertecsi pe litera; fiecare litera e impinsa cu index * spacing, apoi totul se recentreaza
            int glyphs = verts.Count / 6;
            float shift = -(glyphs - 1) * spacing * 0.5f;
            for (int i = 0; i < verts.Count; i++)
            {
                UIVertex v = verts[i];
                v.position.x += (i / 6) * spacing + shift;
                verts[i] = v;
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (graphic != null) graphic.SetVerticesDirty();
        }
#endif
    }
}
