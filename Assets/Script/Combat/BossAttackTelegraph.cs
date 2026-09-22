using UnityEngine;

public class BossAttackTelegraph : MonoBehaviour
{
    private enum Shape { Circle, Line, Fan }
    private LineRenderer lineRenderer;
    private MeshFilter fillMeshFilter;
    private MeshRenderer fillMeshRenderer;
    private Shape shape;
    private float radius;
    private float length;
    private float width;
    private float angle;
    private float progress;
    private Color color;
    private int segmentCount;
    private bool showFullLine;
    private bool rotationFrozen;
    private Quaternion frozenRotation;

    public static BossAttackTelegraph CreateCircle(Vector3 center, float radius, float duration, Color color) => Create(center, Shape.Circle, radius, 0f, 0f, 360f, color, 48);

    public static BossAttackTelegraph CreateVerticalCircle(Vector3 center, Vector3 direction, float radius, float duration, Color color)
    {
        BossAttackTelegraph telegraph = Create(center, Shape.Circle, radius, 0f, 0f, 360f, color, 48);
        telegraph.transform.right = Flatten(direction);
        telegraph.transform.Rotate(Vector3.right, 90f, Space.Self);
        telegraph.FreezeRotation();
        return telegraph;
    }

    public static BossAttackTelegraph CreateLine(Vector3 origin, Vector3 direction, float length, float width, float duration, Color color)
    {
        BossAttackTelegraph telegraph = Create(origin, Shape.Line, 0f, length, width, 0f, color, 5);
        telegraph.transform.right = Flatten(direction);
        telegraph.FreezeRotation();
        return telegraph;
    }

    public static BossAttackTelegraph CreateFan(Vector3 origin, Vector3 direction, float radius, float angle, float duration, Color color)
    {
        BossAttackTelegraph telegraph = Create(origin, Shape.Fan, radius, 0f, 0f, angle, color, 28);
        telegraph.transform.right = Flatten(direction);
        telegraph.FreezeRotation();
        return telegraph;
    }

    public void FreezeRotation()
    {
        frozenRotation = transform.rotation;
        rotationFrozen = true;
    }

    private void LateUpdate()
    {
        if (rotationFrozen)
            transform.rotation = frozenRotation;
    }

    public void SetProgress(float value)
    {
        progress = Mathf.Clamp01(value);
        UpdateVisual();
        if (progress >= 1f)
            Destroy(gameObject);
    }

    public void ShowFullLine()
    {
        showFullLine = true;
        UpdateVisual();
    }

    private static BossAttackTelegraph Create(Vector3 position, Shape shape, float radius, float length, float width, float angle, Color color, int segmentCount)
    {
        GameObject telegraphObject = new("Boss Attack Telegraph");
        telegraphObject.transform.position = position;
        BossAttackTelegraph telegraph = telegraphObject.AddComponent<BossAttackTelegraph>();
        telegraph.shape = shape;
        telegraph.radius = radius;
        telegraph.length = length;
        telegraph.width = width;
        telegraph.angle = angle;
        telegraph.color = color;
        telegraph.segmentCount = segmentCount;
        telegraph.lineRenderer = telegraphObject.AddComponent<LineRenderer>();
        telegraph.lineRenderer.useWorldSpace = false;
        telegraph.lineRenderer.loop = shape != Shape.Line;
        telegraph.lineRenderer.alignment = LineAlignment.TransformZ;
        telegraph.lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        telegraph.lineRenderer.startWidth = 0.08f;
        telegraph.lineRenderer.endWidth = 0.08f;

        if (shape == Shape.Circle || shape == Shape.Line)
        {
            telegraph.fillMeshFilter = telegraphObject.AddComponent<MeshFilter>();
            telegraph.fillMeshRenderer = telegraphObject.AddComponent<MeshRenderer>();
            telegraph.fillMeshRenderer.material = new Material(Shader.Find("Sprites/Default"));
            telegraph.fillMeshRenderer.sortingOrder = -1;
        }

        telegraph.UpdateVisual();
        return telegraph;
    }

    private void UpdateVisual()
    {
        Color currentColor = color;
        currentColor.a = Mathf.Lerp(0.45f, 1f, progress);
        lineRenderer.startColor = currentColor;
        lineRenderer.endColor = currentColor;
        lineRenderer.startWidth = Mathf.Lerp(0.08f, 0.18f, progress);
        lineRenderer.endWidth = lineRenderer.startWidth;

        if (fillMeshRenderer != null)
        {
            Color fillColor = color;
            fillColor.a = Mathf.Lerp(0.08f, 0.42f, progress);
            fillMeshRenderer.material.color = fillColor;
        }

        if (shape == Shape.Circle) DrawCircle();
        else if (shape == Shape.Line) DrawLine();
        else DrawFan();
    }

    private void DrawCircle()
    {
        lineRenderer.positionCount = segmentCount;
        float inwardRadius = Mathf.Max(0f, radius - lineRenderer.startWidth * 0.5f);
        for (int index = 0; index < segmentCount; index++)
        {
            float radians = index * Mathf.PI * 2f / segmentCount;
            lineRenderer.SetPosition(index, new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * inwardRadius);
        }

        DrawFilledCircle();
    }

    private void DrawFilledCircle()
    {
        int visibleSegments = Mathf.Max(3, Mathf.CeilToInt(segmentCount * Mathf.Max(progress, 0.02f)));
        Vector3[] vertices = new Vector3[visibleSegments + 1];
        int[] triangles = new int[visibleSegments * 3];

        vertices[0] = Vector3.zero;
        float visibleAngle = Mathf.PI * 2f * Mathf.Clamp01(progress);
        for (int index = 0; index < visibleSegments; index++)
        {
            float radians = visibleSegments <= 1
                ? 0f
                : visibleAngle * index / (visibleSegments - 1);
            vertices[index + 1] = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * radius;

            int triangleIndex = index * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = index + 1;
            triangles[triangleIndex + 2] = index + 2 <= visibleSegments ? index + 2 : 1;
        }

        Mesh mesh = fillMeshFilter.mesh;
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    private void DrawLine()
    {
        float visibleLength = showFullLine ? length : length * progress;
        float halfWidth = width * 0.5f;
        lineRenderer.loop = true;
        lineRenderer.positionCount = 5;
        lineRenderer.SetPosition(0, new Vector3(0f, 0f, -halfWidth));
        lineRenderer.SetPosition(1, new Vector3(visibleLength, 0f, -halfWidth));
        lineRenderer.SetPosition(2, new Vector3(visibleLength, 0f, halfWidth));
        lineRenderer.SetPosition(3, new Vector3(0f, 0f, halfWidth));
        lineRenderer.SetPosition(4, new Vector3(0f, 0f, -halfWidth));

        DrawFilledLine(visibleLength, halfWidth);
    }

    private void DrawFilledLine(float visibleLength, float halfWidth)
    {
        Vector3[] vertices =
        {
            new(0f, 0f, -halfWidth),
            new(visibleLength, 0f, -halfWidth),
            new(visibleLength, 0f, halfWidth),
            new(0f, 0f, halfWidth)
        };

        Mesh mesh = fillMeshFilter.mesh;
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
    }

    private void DrawFan()
    {
        float visibleAngle = angle * progress;
        float startAngle = -visibleAngle * 0.5f;
        lineRenderer.positionCount = segmentCount;
        for (int index = 0; index < segmentCount; index++)
        {
            float radians = Mathf.Lerp(startAngle, -startAngle, index / (float)(segmentCount - 1)) * Mathf.Deg2Rad;
            lineRenderer.SetPosition(index, new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * radius);
        }
    }

    private static Vector3 Flatten(Vector3 direction)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.right;
    }
}
