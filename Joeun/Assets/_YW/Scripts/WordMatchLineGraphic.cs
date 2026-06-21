using UnityEngine;
using UnityEngine.UI;

public sealed class WordMatchLineGraphic : MaskableGraphic
{
    private Vector2 start;
    private Vector2 end;
    private float width = 4f;

    public void SetPoints(Vector2 from, Vector2 to)
    {
        start = from;
        end = to;
        SetVerticesDirty();
    }

    public void SetVisual(Color lineColor, float lineWidth)
    {
        color = lineColor;
        width = Mathf.Max(1f, lineWidth);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Vector2 delta = end - start;
        if (delta.sqrMagnitude <= 0.01f || color.a <= 0f)
            return;

        Vector2 normal = new Vector2(-delta.y, delta.x).normalized * (width * 0.5f);
        Color32 vertexColor = color;

        int startIndex = vh.currentVertCount;
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = vertexColor;

        vertex.position = start - normal;
        vh.AddVert(vertex);
        vertex.position = start + normal;
        vh.AddVert(vertex);
        vertex.position = end + normal;
        vh.AddVert(vertex);
        vertex.position = end - normal;
        vh.AddVert(vertex);

        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }
}
