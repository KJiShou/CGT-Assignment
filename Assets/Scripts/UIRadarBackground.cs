using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UIRadarBackground : Graphic
{
    [Header("Background Settings")]
    [Tooltip("Background max radius must same with the UIRadarPolygonRenderer maxRadius")]
    public float maxRadius = 100f;

    [Tooltip("Outer color")]
    public Color backgroundColor = Color.white;

    [Header("Grid Settings")]
    [Tooltip("The number of concentric graduations. 5 indicates that the radius is divided into 5 equal parts (20%, 40%, 60%, 80%, 100%).")]
    [Range(1, 10)] public int gridCount = 5;

    [Tooltip("The colors of the alternating grid for delineate scales")]
    public Color altBackgroundColor = new Color(0.93f, 0.93f, 0.93f, 1f);

    private const int numVertices = 5;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        // Draw concentric pentagons from the outside in, the inner layer covering the outer layer
        for (int step = gridCount; step > 0; step--)
        {
            float radiusRatio = (float)step / gridCount;
            float currentRadius = maxRadius * radiusRatio;

            // Alternating colors in odd and even layers create a radar-like scale effect.
            Color currentColor = (step % 2 == 0) ? altBackgroundColor : backgroundColor;

            DrawPentagon(vh, currentRadius, currentColor);
        }
    }

    /// <summary>
    /// Draw single polygon layer
    /// </summary>
    private void DrawPentagon(VertexHelper vh, float radius, Color color)
    {
        int startIndex = vh.currentVertCount;

        // Center vertex
        UIVertex centerVert = UIVertex.simpleVert;
        centerVert.color = color;
        centerVert.position = Vector2.zero;
        vh.AddVert(centerVert);

        float angleStepDeg = 360f / numVertices;

        // Edges vertex
        for (int i = 0; i < numVertices; i++)
        {
            float angleRad = (90f + i * angleStepDeg) * Mathf.Deg2Rad;
            float x = Mathf.Cos(angleRad) * radius;
            float y = Mathf.Sin(angleRad) * radius;

            UIVertex vert = UIVertex.simpleVert;
            vert.color = color;
            vert.position = new Vector2(x, y);
            vh.AddVert(vert);
        }

        // Fill triangle
        for (int i = 1; i <= numVertices; i++)
        {
            int currentOuterIdx = startIndex + i;
            int nextOuterIdx = startIndex + ((i % numVertices) + 1);
            vh.AddTriangle(startIndex, currentOuterIdx, nextOuterIdx);
        }
    }

#if UNITY_EDITOR
    // When updating parameter in Inspector, real-time refresh Scene viewport
    // Only for editing mode
    protected override void OnValidate()
    {
        base.OnValidate();
        SetAllDirty();
    }
#endif
}