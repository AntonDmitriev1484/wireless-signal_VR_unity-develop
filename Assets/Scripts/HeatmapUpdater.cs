using UnityEngine;
using System.Collections.Generic;

public class HeatmapUpdater : MonoBehaviour
{
    [SerializeField] public Material material;

    // Position -> Color
    public Dictionary<Vector3, Color> points = new();

    public Texture2D heatmapTexture;

    private float minX;
    private float maxX;
    private float minZ;
    private float maxZ;

    public void Upload()
    {
        if (points == null || points.Count == 0)
        {
            Debug.LogWarning("No heatmap points provided.");
            return;
        }

        CalculateBounds();

        // Find the unique X/Z coordinates.
        HashSet<float> xValues = new();
        HashSet<float> zValues = new();

        foreach (Vector3 position in points.Keys)
        {
            xValues.Add(position.x);
            zValues.Add(position.z);
        }

        float[] xCoords = new float[xValues.Count];
        float[] zCoords = new float[zValues.Count];

        xValues.CopyTo(xCoords);
        zValues.CopyTo(zCoords);

        System.Array.Sort(xCoords);
        System.Array.Sort(zCoords);

        int width = xCoords.Length;
        int height = zCoords.Length;

        // Create texture.
        heatmapTexture = new Texture2D(
            width,
            height,
            TextureFormat.RGBA32,
            false
        );

        heatmapTexture.wrapMode = TextureWrapMode.Clamp;
        heatmapTexture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[width * height];

        // Initialize texture.
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // Put each receiver into the appropriate texture pixel.
        foreach (var kvp in points)
        {
            Vector3 position = kvp.Key;
            Color color = kvp.Value;

            int xIndex = FindClosestIndex(xCoords, position.x);
            int zIndex = FindClosestIndex(zCoords, position.z);

            int pixelIndex = zIndex * width + xIndex;

            pixels[pixelIndex] = color;
        }

        heatmapTexture.SetPixels(pixels);
        heatmapTexture.Apply();

        // Send texture to shader.
        material.SetTexture("_HeatmapTex", heatmapTexture);

        // Tell shader the world-space bounds.
        material.SetVector(
            "_HeatmapBounds",
            new Vector4(
                minX,
                minZ,
                maxX,
                maxZ
            )
        );
    }

    private void CalculateBounds()
    {
        minX = float.MaxValue;
        maxX = float.MinValue;

        minZ = float.MaxValue;
        maxZ = float.MinValue;

        foreach (Vector3 position in points.Keys)
        {
            minX = Mathf.Min(minX, position.x);
            maxX = Mathf.Max(maxX, position.x);

            minZ = Mathf.Min(minZ, position.z);
            maxZ = Mathf.Max(maxZ, position.z);
        }
    }

    private int FindClosestIndex(float[] values, float value)
    {
        int closestIndex = 0;
        float closestDistance = Mathf.Abs(values[0] - value);

        for (int i = 1; i < values.Length; i++)
        {
            float distance = Mathf.Abs(values[i] - value);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private void OnDestroy()
    {
        if (heatmapTexture != null)
        {
            Destroy(heatmapTexture);
        }
    }
}