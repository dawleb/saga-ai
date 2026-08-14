using UnityEngine;
using UnityEngine.UI;

// Simple world-space health bar.
//
// Attach it to a character root that has a Health component. It builds its own
// world-space Canvas at runtime, so there is no prefab to keep in sync and
// nothing to wire in the scene: the same component works on Player and Monster.
//
// Health is only read here, never modified.
public class HealthBar : MonoBehaviour
{
    [Header("Placement")]
    [Tooltip("Height above the character's pivot, in local units.")]
    public float heightOffset = 1.6f;

    [Header("Size in world units")]
    public float width = 1f;
    public float height = 0.12f;

    [Header("Colours")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.65f);
    public Color fullHealthColor = new Color(0.25f, 0.85f, 0.25f, 1f);
    public Color noHealthColor = new Color(0.85f, 0.2f, 0.2f, 1f);

    // The Canvas is authored at this pixel width and then scaled down, which
    // keeps the RectTransform numbers readable.
    private const float ReferencePixelWidth = 100f;

    private Health health;
    private Camera targetCamera;
    private Transform barRoot;
    private RectTransform fillRect;
    private Image fillImage;

    private void Awake()
    {
        health = GetComponentInParent<Health>();

        if (health == null)
        {
            Debug.LogWarning(
                $"[UI] {name}: no Health found in self or parents, " +
                "health bar disabled."
            );

            enabled = false;

            return;
        }

        Build();
    }

    private void Build()
    {
        // Adding a Canvas replaces the plain Transform with a RectTransform,
        // which destroys the original Transform. So the component is added
        // first and every transform reference is taken afterwards.
        GameObject canvasObject = new GameObject("HealthBar");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // No GraphicRaycaster on purpose: the bar is decoration and must not
        // take part in input at all.
        RectTransform canvasRect =
            canvasObject.GetComponent<RectTransform>();

        canvasRect.SetParent(transform, false);

        barRoot = canvasRect;

        float referencePixelHeight =
            ReferencePixelWidth *
            (height / Mathf.Max(width, 0.0001f));

        canvasRect.sizeDelta =
            new Vector2(
                ReferencePixelWidth,
                referencePixelHeight
            );

        canvasRect.localScale =
            Vector3.one * (width / ReferencePixelWidth);

        Image background =
            CreateStretchedImage(
                "Background",
                canvasRect,
                backgroundColor
            );

        fillImage =
            CreateStretchedImage(
                "Fill",
                background.rectTransform,
                fullHealthColor
            );

        fillRect = fillImage.rectTransform;
    }

    private static Image CreateStretchedImage(
        string objectName,
        RectTransform parent,
        Color color
    )
    {
        // Same ordering rule as Build: Image brings its own RectTransform, so
        // it is added before the transform is touched or parented.
        GameObject imageObject = new GameObject(objectName);

        Image image = imageObject.AddComponent<Image>();
        image.color = color;

        // Must never absorb a click meant for the ground.
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.SetParent(parent, false);

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        return image;
    }

    private void Update()
    {
        float fraction =
            health.maxHealth > 0f
                ? Mathf.Clamp01(
                    health.CurrentHealth / health.maxHealth
                )
                : 0f;

        // Shrinking through the anchor keeps the bar left aligned and needs no
        // sprite, which Image.fillAmount would require.
        fillRect.anchorMax = new Vector2(fraction, 1f);

        fillImage.color =
            Color.Lerp(
                noHealthColor,
                fullHealthColor,
                fraction
            );
    }

    private void LateUpdate()
    {
        // Applied every frame so the offset can be tuned while playing.
        barRoot.localPosition =
            new Vector3(0f, heightOffset, 0f);

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        // Face the camera, so the character's own rotation never turns the
        // bar edge-on.
        barRoot.rotation = targetCamera.transform.rotation;
    }
}
