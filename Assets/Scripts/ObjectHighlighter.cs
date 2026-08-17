using UnityEngine;
using System.Collections.Generic;

public class ObjectHighlighter : MonoBehaviour
{
    private Material highlightMaterial;

    [SerializeField] private float highlightScale = 3.5f;

    private Dictionary<GameObject, GameObject> highlights = new();

    public void SetOutlineMaterial(Material material)
    {
        highlightMaterial = material;
    }

    public void SetHighlighted(GameObject target, bool highlighted)
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
        MeshFilter source = target.GetComponent<MeshFilter>();

        if (source == null || source.sharedMesh == null)
        {
            Debug.LogWarning($"No MeshFilter found on {target.name}");
            return;
        }

        GameObject highlight = new GameObject(
            target.name + "_Highlight"
        );

        highlight.transform.position = target.transform.position;
        highlight.transform.rotation = target.transform.rotation;
        highlight.transform.localScale =
            target.transform.localScale * highlightScale;

        MeshFilter meshFilter =
            highlight.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer =
            highlight.AddComponent<MeshRenderer>();

        meshFilter.sharedMesh = source.sharedMesh;
        meshRenderer.sharedMaterial = highlightMaterial;

        highlights.Add(target, highlight);
    }

    private void RemoveHighlight(GameObject target)
    {
        if (highlights.TryGetValue(target, out GameObject highlight))
        {
            Destroy(highlight);
            highlights.Remove(target);
        }
    }

    public void ClearAllHighlights()
    {
        foreach (GameObject highlight in highlights.Values)
        {
            if (highlight != null)
                Destroy(highlight);
        }

        highlights.Clear();
    }
}