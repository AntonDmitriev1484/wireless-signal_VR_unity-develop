using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ObjectHighlighter : MonoBehaviour
{
    private Camera playerCamera;
    private Sprite highlightSprite;
    private float padding = 20f;

    private Canvas highlightCanvas;

    private Dictionary<GameObject, Image> highlights = new();

    public void Initialize(
        Camera camera,
        Sprite circleSprite,
        float circlePadding = 20f)
    {
        playerCamera = camera;
        highlightSprite = circleSprite;
        padding = circlePadding;

        CreateHighlightCanvas();
    }

    private void LateUpdate()
    {
        UpdateHighlights();
    }

    private void CreateHighlightCanvas()
    {
        GameObject canvasObject =
            new GameObject("HighlightCanvas");

        highlightCanvas =
            canvasObject.AddComponent<Canvas>();

        highlightCanvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler =
            canvasObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        canvasObject.AddComponent<GraphicRaycaster>();
    }

    public void SetHighlighted(
        GameObject target,
        bool highlighted)
    {
        if (target == null)
            return;

        if (highlighted)
        {
            if (!highlights.ContainsKey(target))
                CreateHighlight(target);
        }
        else
        {
            RemoveHighlight(target);
        }
    }

    private void CreateHighlight(GameObject target)
    {
        if (highlightSprite == null)
        {
            Debug.LogError(
                "Highlight sprite has not been initialized."
            );

            return;
        }

        if (highlightCanvas == null)
        {
            Debug.LogError(
                "Highlight canvas has not been created."
            );

            return;
        }

        GameObject highlightObject =
            new GameObject(
                target.name + "_Highlight"
            );

        highlightObject.transform.SetParent(
            highlightCanvas.transform,
            false
        );

        Image highlight =
            highlightObject.AddComponent<Image>();

        highlight.sprite = highlightSprite;
        highlight.preserveAspect = true;

        highlightObject.SetActive(true);

        highlights.Add(
            target,
            highlight
        );
    }

    private void RemoveHighlight(GameObject target)
    {
        if (highlights.TryGetValue(
            target,
            out Image highlight))
        {
            if (highlight != null)
                Destroy(highlight.gameObject);

            highlights.Remove(target);
        }
    }

    private void UpdateHighlights()
    {
        foreach (
            KeyValuePair<GameObject, Image> pair
            in highlights)
        {
            if (pair.Key == null ||
                pair.Value == null)
            {
                continue;
            }

            UpdateHighlight(
                pair.Key,
                pair.Value
            );
        }
    }

    private void UpdateHighlight(
        GameObject target,
        Image highlight)
    {
        Renderer[] renderers =
            target.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            highlight.gameObject.SetActive(false);
            return;
        }

        highlight.gameObject.SetActive(true);

        Bounds bounds =
            renderers[0].bounds;

        for (int i = 1;
             i < renderers.Length;
             i++)
        {
            bounds.Encapsulate(
                renderers[i].bounds
            );
        }

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        Vector3[] corners =
        {
            new Vector3(
                min.x,
                min.y,
                min.z
            ),

            new Vector3(
                min.x,
                min.y,
                max.z
            ),

            new Vector3(
                min.x,
                max.y,
                min.z
            ),

            new Vector3(
                min.x,
                max.y,
                max.z
            ),

            new Vector3(
                max.x,
                min.y,
                min.z
            ),

            new Vector3(
                max.x,
                min.y,
                max.z
            ),

            new Vector3(
                max.x,
                max.y,
                min.z
            ),

            new Vector3(
                max.x,
                max.y,
                max.z
            )
        };

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (Vector3 corner in corners)
        {
            Vector3 screenPoint =
                playerCamera.WorldToScreenPoint(
                    corner
                );

            minX = Mathf.Min(
                minX,
                screenPoint.x
            );

            maxX = Mathf.Max(
                maxX,
                screenPoint.x
            );

            minY = Mathf.Min(
                minY,
                screenPoint.y
            );

            maxY = Mathf.Max(
                maxY,
                screenPoint.y
            );
        }

        float width =
            maxX - minX;

        float height =
            maxY - minY;

        float circleSize =
            Mathf.Max(
                width,
                height
            ) + padding * 2f;

        highlight.rectTransform.position =
            new Vector3(
                (minX + maxX) / 2f,
                (minY + maxY) / 2f,
                0f
            );

        highlight.rectTransform.sizeDelta =
            new Vector2(
                circleSize,
                circleSize
            );
    }

    public void ClearAllHighlights()
    {
        foreach (
            Image highlight
            in highlights.Values)
        {
            if (highlight != null)
                Destroy(highlight.gameObject);
        }

        highlights.Clear();
    }

    private void OnDestroy()
    {
        if (highlightCanvas != null)
        {
            Destroy(
                highlightCanvas.gameObject
            );
        }
    }
}