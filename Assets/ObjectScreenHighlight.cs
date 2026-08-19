using UnityEngine;
using UnityEngine.UI;

public class ObjectScreenHighlight : MonoBehaviour
{
    [Header("Highlight")]
    [SerializeField] private Texture2D highlightTexture;
    [SerializeField] private float padding = 20f;

    private Camera playerCamera;
    private GameObject objectToHighlight;

    private Image highlightImage;
    private Sprite highlightSprite;

    void Start()
    {
        // Find camera through the scene hierarchy
        GameObject player = GameObject.Find("Player");

        if (player == null)
        {
            Debug.LogError("Could not find Player in scene.");
            return;
        }

        Transform cameraTransform = player.transform.Find("Camera");

        if (cameraTransform == null)
        {
            Debug.LogError("Could not find Camera under Player.");
            return;
        }

        playerCamera = cameraTransform.GetComponent<Camera>();

        if (playerCamera == null)
        {
            Debug.LogError("No Camera component found on Player/Camera.");
            return;
        }

        CreateHighlightImage();
    }

    void LateUpdate()
    {
        if (objectToHighlight != null &&
            highlightImage != null)
        {
            ShowHighlight(objectToHighlight);
        }
    }

    private void CreateHighlightImage()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("No Canvas found in the scene.");
            return;
        }

        if (highlightTexture == null)
        {
            Debug.LogError("No highlight texture assigned.");
            return;
        }

        // Convert Texture2D to Sprite
        highlightSprite = Sprite.Create(
            highlightTexture,
            new Rect(
                0,
                0,
                highlightTexture.width,
                highlightTexture.height
            ),
            new Vector2(0.5f, 0.5f)
        );

        // Create UI object
        GameObject highlightObject =
            new GameObject("ObjectHighlight");

        highlightObject.transform.SetParent(
            canvas.transform,
            false
        );

        // Add Image component
        highlightImage =
            highlightObject.AddComponent<Image>();

        highlightImage.sprite = highlightSprite;

        // Don't block interaction with the 3D scene
        highlightImage.raycastTarget = false;

        highlightImage.gameObject.SetActive(false);
    }

    private void ShowHighlight(GameObject target)
    {
        Renderer[] renderers =
            target.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            highlightImage.gameObject.SetActive(false);
            return;
        }

        // Combine all renderer bounds
        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        // 8 corners of the world-space bounds
        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, max.y, max.z),

            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z)
        };

        float minX = float.MaxValue;
        float maxX = float.MinValue;

        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (Vector3 corner in corners)
        {
            Vector3 screenPoint =
                playerCamera.WorldToScreenPoint(corner);

            minX = Mathf.Min(minX, screenPoint.x);
            maxX = Mathf.Max(maxX, screenPoint.x);

            minY = Mathf.Min(minY, screenPoint.y);
            maxY = Mathf.Max(maxY, screenPoint.y);
        }

        float width = maxX - minX;
        float height = maxY - minY;

        // Make the highlight large enough to surround
        // the entire object.
        float size =
            Mathf.Max(width, height) +
            padding * 2f;

        // Center of object's screen bounds
        float centerX = (minX + maxX) * 0.5f;
        float centerY = (minY + maxY) * 0.5f;

        RectTransform rectTransform =
            highlightImage.rectTransform;

        rectTransform.position =
            new Vector3(centerX, centerY, 0f);

        rectTransform.sizeDelta =
            new Vector2(size, size);

        highlightImage.gameObject.SetActive(true);
    }

    public void SetObjectToHighlight(GameObject target)
    {
        objectToHighlight = target;

        if (target == null)
        {
            ClearHighlight();
        }
    }

    public void ClearHighlight()
    {
        objectToHighlight = null;

        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (highlightSprite != null)
        {
            Destroy(highlightSprite);
        }
    }
}