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

    private bool rotationFrozen;
    private Quaternion frozenRotation;
    private Vector3 frozenPosition;

    // ระยะยกจากพื้นเล็กน้อยป้องกันการจมลงไปในฉาก/พื้น
    private const float Y_OFFSET = 0.05f;

    public static BossAttackTelegraph CreateCircle(Vector3 center, float radius, float duration, Color color)
        => Create(center, Shape.Circle, radius, 0f, 0f, 360f, color, 48);

    public static BossAttackTelegraph CreateLine(Vector3 origin, Vector3 direction, float length, float width, float duration, Color color)
    {
        BossAttackTelegraph telegraph = Create(origin, Shape.Line, 0f, length, width, 0f, color, 5);
        telegraph.UpdatePositionAndDirection(origin, direction);
        return telegraph;
    }

    public static BossAttackTelegraph CreateFan(Vector3 origin, Vector3 direction, float radius, float angle, float duration, Color color)
    {
        BossAttackTelegraph telegraph = Create(origin, Shape.Fan, radius, 0f, 0f, angle, color, 28);
        telegraph.UpdatePositionAndDirection(origin, direction);
        return telegraph;
    }

    public void UpdatePositionAndDirection(Vector3 position, Vector3 direction)
    {
        if (rotationFrozen) return;

        // ยกพิกัด Y ขึ้นเล็กน้อยเพื่อให้ลอยอยู่เหนือพื้น
        position.y += Y_OFFSET;
        transform.position = position;

        Vector3 flatDir = Flatten(direction);
        if (flatDir.sqrMagnitude > 0.001f)
        {
            transform.right = flatDir;
        }
    }

    public void FreezeRotation()
    {
        frozenRotation = transform.rotation;
        frozenPosition = transform.position;
        rotationFrozen = true;
    }

    private void LateUpdate()
    {
        if (rotationFrozen)
        {
            transform.rotation = frozenRotation;
            transform.position = frozenPosition;
        }
    }

    public void SetProgress(float value)
    {
        progress = Mathf.Clamp01(value);
        UpdateVisual();
        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private static BossAttackTelegraph Create(Vector3 position, Shape shape, float radius, float length, float width, float angle, Color color, int segmentCount)
    {
        // สร้างตำแหน่งให้อยู่เหนือพื้นเล็กน้อย
        position.y += Y_OFFSET;

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

        // ค้นหา Shader ที่ใช้ได้ทั้ง Built-in และ URP
        Shader defaultShader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Unlit/Color");
        Material sharedMaterial = new Material(defaultShader);

        // ตั้งค่า LineRenderer (เส้นขอบ)
        telegraph.lineRenderer = telegraphObject.AddComponent<LineRenderer>();
        telegraph.lineRenderer.useWorldSpace = false;
        telegraph.lineRenderer.loop = shape != Shape.Line;
        telegraph.lineRenderer.alignment = LineAlignment.TransformZ;
        telegraph.lineRenderer.material = sharedMaterial;
        telegraph.lineRenderer.startWidth = 0.1f;
        telegraph.lineRenderer.endWidth = 0.1f;
        telegraph.lineRenderer.sortingOrder = 10; // กำหนดให้อยู่สูงกว่าพื้น

        // ตั้งค่า Fill Mesh (สีเต็มด้านใน)
        telegraph.fillMeshFilter = telegraphObject.AddComponent<MeshFilter>();
        telegraph.fillMeshRenderer = telegraphObject.AddComponent<MeshRenderer>();
        telegraph.fillMeshRenderer.material = new Material(sharedMaterial);
        telegraph.fillMeshRenderer.sortingOrder = 9; // กำหนดให้อยู่ใต้เส้นขอบเล็กน้อย แต่อยู่เหนือพื้น

        telegraph.UpdateVisual();
        return telegraph;
    }

    private void UpdateVisual()
    {
        Color currentColor = color;
        currentColor.a = Mathf.Lerp(0.5f, 1f, progress);
        lineRenderer.startColor = currentColor;
        lineRenderer.endColor = currentColor;
        lineRenderer.startWidth = Mathf.Lerp(0.08f, 0.18f, progress);
        lineRenderer.endWidth = lineRenderer.startWidth;

        if (fillMeshRenderer != null)
        {
            Color fillColor = color;
            fillColor.a = Mathf.Lerp(0.2f, 0.6f, progress);
            fillMeshRenderer.material.color = fillColor;
        }

        if (shape == Shape.Circle) DrawCircle();
        else if (shape == Shape.Line) DrawLine();
        else DrawFan();
    }

    private void DrawCircle()
    {
        lineRenderer.positionCount = segmentCount;
        for (int index = 0; index < segmentCount; index++)
        {
            float radians = index * Mathf.PI * 2f / segmentCount;
            lineRenderer.SetPosition(index, new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * radius);
        }

        DrawFilledCircle();
    }

    private void DrawFilledCircle()
    {
        float currentRadius = radius * Mathf.Clamp01(progress);
        if (currentRadius <= 0.001f)
        {
            if (fillMeshFilter.mesh != null) fillMeshFilter.mesh.Clear();
            return;
        }

        Vector3[] vertices = new Vector3[segmentCount + 1];
        int[] triangles = new int[segmentCount * 3];

        vertices[0] = Vector3.zero;
        for (int index = 0; index < segmentCount; index++)
        {
            float radians = index * Mathf.PI * 2f / segmentCount;
            vertices[index + 1] = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * currentRadius;

            int triangleIndex = index * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = index + 1;
            triangles[triangleIndex + 2] = (index + 2 <= segmentCount) ? index + 2 : 1;
        }

        Mesh mesh = fillMeshFilter.mesh;
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    private void DrawLine()
    {
        float halfWidth = width * 0.5f;
        lineRenderer.loop = true;
        lineRenderer.positionCount = 5;
        lineRenderer.SetPosition(0, new Vector3(0f, 0f, -halfWidth));
        lineRenderer.SetPosition(1, new Vector3(length, 0f, -halfWidth));
        lineRenderer.SetPosition(2, new Vector3(length, 0f, halfWidth));
        lineRenderer.SetPosition(3, new Vector3(0f, 0f, halfWidth));
        lineRenderer.SetPosition(4, new Vector3(0f, 0f, -halfWidth));

        DrawFilledLine(halfWidth);
    }

    private void DrawFilledLine(float halfWidth)
    {
        float visibleLength = length * Mathf.Clamp01(progress);
        if (visibleLength <= 0.001f)
        {
            if (fillMeshFilter.mesh != null) fillMeshFilter.mesh.Clear();
            return;
        }

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
        float halfAngle = angle * 0.5f;
        lineRenderer.loop = true;
        lineRenderer.positionCount = segmentCount + 2;
        lineRenderer.SetPosition(0, Vector3.zero);

        for (int index = 0; index < segmentCount; index++)
        {
            float radians = Mathf.Lerp(-halfAngle, halfAngle, index / (float)(segmentCount - 1)) * Mathf.Deg2Rad;
            lineRenderer.SetPosition(index + 1, new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * radius);
        }
        lineRenderer.SetPosition(segmentCount + 1, Vector3.zero);

        DrawFilledFan(halfAngle);
    }

    private void DrawFilledFan(float halfAngle)
    {
        float currentRadius = radius * Mathf.Clamp01(progress);
        if (currentRadius <= 0.001f)
        {
            if (fillMeshFilter.mesh != null) fillMeshFilter.mesh.Clear();
            return;
        }

        Vector3[] vertices = new Vector3[segmentCount + 1];
        int[] triangles = new int[(segmentCount - 1) * 3];

        vertices[0] = Vector3.zero;
        for (int index = 0; index < segmentCount; index++)
        {
            float radians = Mathf.Lerp(-halfAngle, halfAngle, index / (float)(segmentCount - 1)) * Mathf.Deg2Rad;
            vertices[index + 1] = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * currentRadius;

            if (index < segmentCount - 1)
            {
                int triangleIndex = index * 3;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = index + 1;
                triangles[triangleIndex + 2] = index + 2;
            }
        }

        Mesh mesh = fillMeshFilter.mesh;
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    private static Vector3 Flatten(Vector3 direction)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.right;
    }
}