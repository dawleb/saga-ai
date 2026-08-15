using UnityEngine;

// Turns the click marker into a proper flat ring lying on the ground.
//
// The object was a built-in Cylinder squashed to a solid disc, positioned at
// the player's own height so it floated at waist level. This builds a real
// ring mesh at runtime and pins it to the ground.
//
// Attach to the SelectionRing object. PlayerClickController keeps driving the
// marker's X/Z exactly as before, so nothing else needs to change.
public class SelectionRing : MonoBehaviour
{
    [Header("Shape")]
    public float outerRadius = 0.6f;
    public float thickness = 0.06f;

    [Range(12, 128)]
    public int segments = 64;

    [Header("Placement")]
    [Tooltip("Height above the ground, just enough to avoid z-fighting.")]
    public float groundHeight = 0.02f;

    [Header("Look")]
    public Color ringColor = new Color(0.35f, 1f, 0.55f, 0.85f);

    [Header("Pulse")]
    public float pulseAmount = 0.06f;
    public float pulseSpeed = 4f;

    [Header("Auto hide")]
    public bool hideAfterDelay = true;
    public float visibleSeconds = 1.5f;

    private MeshRenderer meshRenderer;
    private float shownAt;
    private Vector3 lastPosition;

    private void Awake()
    {
        // The old squashed-disc scale would distort the new mesh.
        transform.localScale = Vector3.one;

        BuildMesh();

        meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        ApplyDecalLook();

        // Purely visual: it must never take part in physics or raycasts.
        Collider[] colliders = GetComponents<Collider>();

        for (int i = 0; i < colliders.Length; i++)
            Destroy(colliders[i]);
    }

    // The ring shared the scene's default lit material, so it was shaded and
    // cast a shadow, which is what made it read as a solid object sitting on
    // the ground rather than a mark drawn on it.
    private void ApplyDecalLook()
    {
        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader != null)
        {
            Material decal = new Material(shader);
            decal.name = "SelectionRingDecal";
            decal.color = ringColor;

            meshRenderer.material = decal;
        }
        else if (meshRenderer.sharedMaterial != null)
        {
            meshRenderer.material.color = ringColor;
        }

        // A mark on the ground neither casts nor receives shadows.
        meshRenderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage =
            UnityEngine.Rendering.LightProbeUsage.Off;

        meshRenderer.reflectionProbeUsage =
            UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    private void OnEnable()
    {
        shownAt = Time.time;
        lastPosition = transform.position;

        if (meshRenderer != null)
            meshRenderer.enabled = true;
    }

    private void BuildMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        float innerRadius =
            Mathf.Max(0.001f, outerRadius - thickness);

        meshFilter.mesh =
            CreateRingMesh(
                innerRadius,
                outerRadius,
                segments
            );
    }

    private static Mesh CreateRingMesh(
        float innerRadius,
        float outerRadius,
        int segments
    )
    {
        Mesh mesh = new Mesh();
        mesh.name = "SelectionRing";

        Vector3[] vertices = new Vector3[segments * 2];
        Vector3[] normals = new Vector3[segments * 2];

        // Both windings are emitted so the ring is visible from either side.
        // A flat ring reads the same both ways, and this removes any doubt
        // about which winding Unity treats as front facing.
        int[] triangles = new int[segments * 12];

        for (int i = 0; i < segments; i++)
        {
            float angle =
                (i / (float)segments) * Mathf.PI * 2f;

            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            int inner = i * 2;
            int outer = i * 2 + 1;

            vertices[inner] =
                new Vector3(cos * innerRadius, 0f, sin * innerRadius);

            vertices[outer] =
                new Vector3(cos * outerRadius, 0f, sin * outerRadius);

            normals[inner] = Vector3.up;
            normals[outer] = Vector3.up;

            int next = (i + 1) % segments;
            int nextInner = next * 2;
            int nextOuter = next * 2 + 1;

            int t = i * 12;

            // Front faces.
            triangles[t + 0] = inner;
            triangles[t + 1] = nextInner;
            triangles[t + 2] = outer;

            triangles[t + 3] = outer;
            triangles[t + 4] = nextInner;
            triangles[t + 5] = nextOuter;

            // Back faces.
            triangles[t + 6] = inner;
            triangles[t + 7] = outer;
            triangles[t + 8] = nextInner;

            triangles[t + 9] = outer;
            triangles[t + 10] = nextOuter;
            triangles[t + 11] = nextInner;
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        return mesh;
    }

    // LateUpdate so the height correction always runs after
    // PlayerClickController has placed the marker for this frame.
    private void LateUpdate()
    {
        Vector3 position = transform.position;

        position.y = groundHeight;

        transform.position = position;

        // A new order moves the marker, which restarts the visible window.
        // Detecting it here avoids having to change PlayerClickController.
        bool moved =
            Mathf.Abs(position.x - lastPosition.x) > 0.001f ||
            Mathf.Abs(position.z - lastPosition.z) > 0.001f;

        if (moved)
        {
            shownAt = Time.time;

            if (meshRenderer != null)
                meshRenderer.enabled = true;
        }

        lastPosition = position;

        float pulse =
            1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

        transform.localScale = new Vector3(pulse, 1f, pulse);

        if (meshRenderer == null)
            return;

        meshRenderer.enabled =
            !hideAfterDelay ||
            Time.time - shownAt < visibleSeconds;
    }
}
