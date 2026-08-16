using UnityEngine;
using UnityEngine.UI;

// Simple world-space health bar.
//
// Player:
// Character
// └── Model
//     └── HealthBarAnchor
//         └── HealthBar
//
// Monster:
// Character
// └── HealthBarAnchor
//     └── HealthBar
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
    public Color backgroundColor =
        new Color(0f, 0f, 0f, 0.65f);

    public Color fullHealthColor =
        new Color(0.25f, 0.85f, 0.25f, 1f);

    public Color noHealthColor =
        new Color(0.85f, 0.2f, 0.2f, 1f);

    private const float ReferencePixelWidth = 100f;

    private Health health;

    private Camera targetCamera;

    private Transform barAnchor;

    private RectTransform barRoot;

    private RectTransform fillRect;

    private Image fillImage;

    private bool barHidden;

    private void Awake()
    {
        health =
            GetComponentInParent<Health>();

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

    // ====================================
    // BUILD
    // ====================================

    private void Build()
    {
        Transform anchorParent =
            transform;

        // Player gets one extra hierarchy level:
        //
        // Player
        // └── Model
        //     └── HealthBarAnchor
        //
        // Zombie:
        //
        // Zombie
        // └── HealthBarAnchor

        PlayerClickController playerController =
            GetComponent<PlayerClickController>();

        if (playerController == null)
        {
            playerController =
                GetComponentInParent<PlayerClickController>();
        }

        if (playerController != null &&
            transform.childCount > 0)
        {
            anchorParent =
                transform.GetChild(0);
        }

        // --------------------------------
        // CREATE ANCHOR
        // --------------------------------

        GameObject anchorObject =
            new GameObject(
                "HealthBarAnchor"
            );

        barAnchor =
            anchorObject.transform;

        barAnchor.SetParent(
            anchorParent,
            false
        );

        barAnchor.localPosition =
            new Vector3(
                0f,
                heightOffset,
                0f
            );

        barAnchor.localRotation =
            Quaternion.identity;

        barAnchor.localScale =
            Vector3.one;

        // --------------------------------
        // CREATE CANVAS
        // --------------------------------

        GameObject canvasObject =
            new GameObject(
                "HealthBar"
            );

        Canvas canvas =
            canvasObject.AddComponent<Canvas>();

        canvas.renderMode =
            RenderMode.WorldSpace;

        RectTransform canvasRect =
            canvasObject.GetComponent<RectTransform>();

        canvasRect.SetParent(
            barAnchor,
            false
        );

        barRoot =
            canvasRect;

        float referencePixelHeight =
            ReferencePixelWidth *
            (
                height /
                Mathf.Max(
                    width,
                    0.0001f
                )
            );

        canvasRect.sizeDelta =
            new Vector2(
                ReferencePixelWidth,
                referencePixelHeight
            );

        canvasRect.localScale =
            Vector3.one *
            (
                width /
                ReferencePixelWidth
            );

        // --------------------------------
        // BACKGROUND
        // --------------------------------

        Image background =
            CreateStretchedImage(
                "Background",
                canvasRect,
                backgroundColor
            );

        // --------------------------------
        // FILL
        // --------------------------------

        fillImage =
            CreateStretchedImage(
                "Fill",
                background.rectTransform,
                fullHealthColor
            );

        fillRect =
            fillImage.rectTransform;
    }

    // ====================================
    // CREATE IMAGE
    // ====================================

    private static Image CreateStretchedImage(
        string objectName,
        RectTransform parent,
        Color color
    )
    {
        GameObject imageObject =
            new GameObject(
                objectName
            );

        Image image =
            imageObject.AddComponent<Image>();

        image.color =
            color;

        image.raycastTarget =
            false;

        RectTransform rect =
            image.rectTransform;

        rect.SetParent(
            parent,
            false
        );

        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        rect.localScale =
            Vector3.one;

        return image;
    }

    // ====================================
    // HEALTH
    // ====================================

    private void Update()
    {
        if (barHidden)
        {
            return;
        }

        if (health == null ||
            fillRect == null ||
            fillImage == null)
        {
            return;
        }

        float fraction =
            health.maxHealth > 0f
                ? Mathf.Clamp01(
                    health.CurrentHealth /
                    health.maxHealth
                )
                : 0f;

        fillRect.anchorMax =
            new Vector2(
                fraction,
                1f
            );

        fillImage.color =
            Color.Lerp(
                noHealthColor,
                fullHealthColor,
                fraction
            );
    }

    // ====================================
    // POSITION / CAMERA
    // ====================================

    private void LateUpdate()
    {
        if (barHidden)
        {
            return;
        }

        if (barAnchor == null)
        {
            return;
        }

        barAnchor.localPosition =
            new Vector3(
                0f,
                heightOffset,
                0f
            );

        if (targetCamera == null)
        {
            targetCamera =
                Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        if (barRoot == null)
        {
            return;
        }

        barRoot.rotation =
            targetCamera.transform.rotation;
    }

    // ====================================
    // HIDE BAR
    // ====================================

    public void HideBar()
    {
        if (barHidden)
        {
            return;
        }

        barHidden = true;

        // This disables the generated Canvas,
        // Background and Fill together.
        if (barRoot != null)
        {
            barRoot.gameObject.SetActive(false);
        }

        Debug.Log(
            $"[UI] Health bar hidden for {name}."
        );
    }

    // ====================================
    // SHOW BAR
    // ====================================

    public void ShowBar()
    {
        barHidden = false;

        if (barRoot != null)
        {
            barRoot.gameObject.SetActive(true);
        }
    }
}