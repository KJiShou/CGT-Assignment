using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UIRadarPolygonRenderer : Graphic
{
    [Header("Polygon Settings")]
    [Tooltip("Polygon maximum radius, match with 1.0")]
    public float maxRadius = 100f;

    [Tooltip("Polygon minimum radius to prevent to prevent the graphic from collapsing completely to the center when the value is -1.")]
    public float minRadius = 15f;

    [Tooltip("Polygon vertices")]
    private const int numVertices = 5;

    private float[] normalizedValues = new float[numVertices] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f };

    /// <summary>
    /// Update polygon value and trigger repaint
    /// </summary>
    /// <param name="values">OCEAN Value, Range: -1.0 ~ 1.0</param>
    public void UpdateValues(float[] values)
    {
        if (values.Length != numVertices)
        {
            Debug.LogError($"UIRadarPolygonRenderer: error parameters，expected {numVertices}，actual {values.Length}.");
            return;
        }

        // ================== Data Mapping Logic ==================
        // Map [-1, 1] to [0, 1], -1 = Inner, 0 = Middle, 1 = Outer 
        // NormalizedValue = (OriginalValue + 1.0) / 2.0;

        for (int i = 0; i < numVertices; i++)
        {
            float rawValue = values[i];
            float clampedValue = Mathf.Clamp(rawValue, -1f, 1f);
            normalizedValues[i] = (clampedValue + 1f) / 2f;
        }

        // trigger Unity call OnPopulateMesh in next frame
        SetAllDirty();
    }

    /// <summary>
    /// Customize mesh generation
    /// </summary>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); // clear old mesh

        // Center point vertex
        UIVertex centralVertex = UIVertex.simpleVert;
        centralVertex.color = color;
        centralVertex.position = Vector2.zero;
        centralVertex.uv0 = new Vector2(0.5f, 0.5f);
        vh.AddVert(centralVertex);

        // 5 dimensions outer vertex
        // angle interval: 360 / 5 = 72 deg
        float angleStepDeg = 360f / numVertices;

        for (int i = 0; i < numVertices; i++)
        {
            float currentRadius = Mathf.Lerp(minRadius, maxRadius, normalizedValues[i]);

            // calculate radian
            // first vertex at 90 deg
            float angleRad = (90f + i * angleStepDeg) * Mathf.Deg2Rad;

            // Converting Polar Coordinates to Cartesian Coordinates
            float x = Mathf.Cos(angleRad) * currentRadius;
            float y = Mathf.Sin(angleRad) * currentRadius;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = new Vector2(x, y);

            float uvX = (vertex.position.x / maxRadius + 1f) / 2f;
            float uvY = (vertex.position.y / maxRadius + 1f) / 2f;
            vertex.uv0 = new Vector2(uvX, uvY);

            vh.AddVert(vertex);
        }

        // define triangle list, fill polygon
        // (Center, 1, 2), (Center, 2, 3), (Center, 3, 4), (Center, 4, 5), (Center, 5, 1)
        for (int i = 1; i <= numVertices; i++)
        {
            int centerIdx = 0;
            int currentOuterIdx = i;
            int nextOuterIdx = (i % numVertices) + 1;

            // Clockwise
            vh.AddTriangle(centerIdx, currentOuterIdx, nextOuterIdx);
        }
    }
}
