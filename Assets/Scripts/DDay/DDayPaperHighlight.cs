using UnityEngine;
using UnityEngine.UI;

namespace DDay
{
    // Feathered tint inside the original paper outline, without duplicate artwork.
    public sealed class DDayPaperHighlight : MaskableGraphic
    {
        public Vector2[] outline;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (outline == null || outline.Length < 3) return;
            var rect = rectTransform.rect;
            var vertex = UIVertex.simpleVert;
            vertex.position = rect.center; vertex.color = color; mesh.AddVert(vertex);
            for (int i = 0; i < outline.Length; i++)
            {
                vertex.position = rect.min + Vector2.Scale(rect.size, outline[i]);
                vertex.color = new Color(color.r, color.g, color.b, 0); mesh.AddVert(vertex);
            }
            for (int i = 0; i < outline.Length; i++) mesh.AddTriangle(0, i + 1, (i + 1) % outline.Length + 1);
        }
    }
}
