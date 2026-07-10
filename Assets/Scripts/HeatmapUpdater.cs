using UnityEngine;
using System.Collections.Generic;

public class HeatmapUpdater : MonoBehaviour
{
    [SerializeField] public Material material;

    public float defaultRadius = 0.35f;

    // User provides:
    // Position -> Color
    public Dictionary<Vector3, Color> points = new();

    const int MAX_POINTS = 128;

    public void Upload()
    {
        Vector4[] positions = new Vector4[MAX_POINTS];
        Vector4[] colors = new Vector4[MAX_POINTS];

        int count = 0;

        foreach (var kvp in points)
        {
            if (count >= MAX_POINTS)
                break;

            Vector3 p = kvp.Key;
            Color c = kvp.Value;

            positions[count] = new Vector4(
                p.x,
                p.y,
                p.z,
                defaultRadius);

            colors[count] = new Vector4(
                c.r,
                c.g,
                c.b,
                c.a);

            count++;
        }

        material.SetInt("_PointCount", count);
        material.SetVectorArray("_Points", positions);
        material.SetVectorArray("_Colors", colors);
    }
}

/*using UnityEngine;
using System.Collections.Generic;

public class HeatmapUpdater : MonoBehaviour
{
    [SerializeField] public Material material;

    [System.Serializable]
    public struct HeatPoint
    {
        public Vector3 position;
        public float radius;
    }

    public List<HeatPoint> points = new();

    void Start()
    {
        // are these shader points in world coordinates or something relative to the start point of the material
        // that might be why the points aren't showing up on the cube.

        // Ok so setting this point at 0,0,0 is actually setting a heatmap, now its just a radius thing I think
        // But for now just test with the 0 point
        // These are world coordinates.
*//*        points = new List<HeatPoint>()
                    {
                        new()
                        {
                            position = new Vector3(0,0,0),
                            radius = 0.02f
                        },

                        new()
                        {
                            position = new Vector3(-2.7f, 0.415f, -1.19f),
                            radius = 0.25f
                        }
                    };*//*

        Upload();
        }

    public void Upload()
    {
        Vector4[] data = new Vector4[points.Count];

        for (int i = 0; i < points.Count; i++)
        {
            data[i] = new Vector4(
                points[i].position.x,
                points[i].position.y,
                points[i].position.z,
                points[i].radius
            );
        }

        material.SetInt("_PointCount", data.Length);
        material.SetVectorArray("_Points", data);


        Debug.Log("Set heatpoints");
        Debug.Log(material.GetInt("_PointCount"));

    }
}*/