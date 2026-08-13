using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

public static class JsonTypelessParser
{
    public static Dictionary<string, object> Parse(string json)
    {
        JObject obj = JObject.Parse(json);
        return (Dictionary<string, object>)ConvertToken(obj);
    }

    private static object ConvertToken(JToken token)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                {
                    var dict = new Dictionary<string, object>();

                    foreach (JProperty property in token.Children<JProperty>())
                    {
                        dict[property.Name] = ConvertToken(property.Value);
                    }

                    return dict;
                }

            case JTokenType.Array:
                {
                    var list = new List<object>();

                    foreach (JToken child in token.Children())
                    {
                        list.Add(ConvertToken(child));
                    }

                    return list;
                }

            default:
                return ((JValue)token).Value;
        }
    }
}

public class RayInterpolator : MonoBehaviour
{
    [SerializeField] private GameObject TxObj; // The object to instantiate for Transmiter
    [SerializeField] private GameObject RxObj; // The object to instantiate for Receiver
    [SerializeField] private GameObject trackObjectRx;

    // Unity.Mathematics

    private Dictionary<Tuple<int, int, int>, List<Matrix<double>>> interpolations =
    new Dictionary<Tuple<int, int, int>, List<Matrix<double>>>();

    private Vector3 vol_min;
    private float spacing;

    void LoadInterpolationGrid(string path)
    {
        string json = System.IO.File.ReadAllText(path);

        Dictionary<string, object> root = JsonTypelessParser.Parse(json);

        var voxels = (List<object>)root["voxels"];

        foreach (Dictionary<string, object> voxelEntry in voxels)
        {
            // Parse voxel index
            List<object> idxList = (List<object>)voxelEntry["idx"];

            var idx = Tuple.Create(
                Convert.ToInt32(idxList[0]),
                Convert.ToInt32(idxList[1]),
                Convert.ToInt32(idxList[2]));

            Dictionary<string, object> voxel =
                (Dictionary<string, object>)voxelEntry["voxel"];

            // Array of matrices
            List<object> matrixList =
                (List<object>)voxel["path_interpolations"];

            List<Matrix<double>> parsedMatrices = new();

            foreach (List<object> matrixObj in matrixList)
            {
                int rows = matrixObj.Count;
                int cols = ((List<object>)matrixObj[0]).Count;

                var matrix = Matrix<double>.Build.Dense(rows, cols);

                for (int r = 0; r < rows; r++)
                {
                    List<object> row = (List<object>)matrixObj[r];

                    for (int c = 0; c < cols; c++)
                    {
                        matrix[r, c] = Convert.ToDouble(row[c]);
                    }
                }

                parsedMatrices.Add(matrix);
            }

            interpolations.Add(idx, parsedMatrices);
        }

         // 11, 17, 3 has two path interpolations
            Debug.Log(interpolations);
    }

    Tuple<int, int, int> position_to_index()
    {
        Vector3 obj_pos = trackObjectRx.transform.position;
        Vector3 relative = (obj_pos - vol_min) / spacing;
        Tuple<int, int, int> idx = Tuple.Create((int)relative[0], (int)relative[1], (int)relative[2]);
        return idx;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoadInterpolationGrid("C:\\Users\\antond2\\Desktop\\WiViz_Study_VR_2.0\\wireless-signal_VR_unity-develop\\Assets\\Resources\\HomeOfficeGrid.json");
        current_idx = position_to_index(); // Assuming that trackObjectRx already has its position set.
    }

    private Tuple<int, int, int> current_idx;

    List<GameObject> pathObjects = new();



    void drawRays(List<Vector3[]> paths)
    {
        int i = 0;
        foreach (Vector3[] path in paths)
        {
            // Create a new GameObject for each pathLine
            GameObject pathObject = new GameObject("PathLine_" + i); // Why always 6?
            LineRenderer lineRenderer = pathObject.AddComponent<LineRenderer>();

            // Set the LineRenderer properties
            lineRenderer.startWidth = 0.003f;
            lineRenderer.endWidth = 0.003f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = Color.red;
            lineRenderer.positionCount = 0; // Initialize with zero positions
            lineRenderer.useWorldSpace = true; // Use world space for the positions
            lineRenderer.numCapVertices = 3; // Set the number of cap vertices for smoother ends
            lineRenderer.numCornerVertices = 3; // Set the number of corner vertices for smoother corners


            // Set the number of positions for the LineRenderer
            lineRenderer.positionCount = path.Length;

            // Set the positions for the LineRenderer
            lineRenderer.SetPositions(path);

            pathObjects.Add(pathObject);
            i++;
        }
       
    }

    void clearRays()
    {
        Debug.Log("Clearing Count " + pathObjects.Count);
        if (pathObjects.Count != 0)
        {
            foreach (GameObject pathObject in pathObjects)
            {
                if (pathObject.GetComponent<LineRenderer>())
                {
                    Debug.Log("Got linerenderer component");
                    pathObject.GetComponent<LineRenderer>().positionCount = 0;
                    pathObject.GetComponent<LineRenderer>().SetPositions(new Vector3[0]);
                }
            }

        }
        pathObjects = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        current_idx = position_to_index();

        clearRays();

        List<Vector3[]> rays = new();
        Vector3 localPosition = trackObjectRx.transform.position;

        if (interpolations.TryGetValue(current_idx, out List<Matrix<double>> pathInterpolations))
        {
            // Compute local coordinates within the voxel.
            // Replace these with however you compute the local voxel coordinates.
            float x = localPosition.x;
            float y = localPosition.y;
            float z = localPosition.z;

            Vector<double> basis = Vector<double>.Build.Dense(new double[]
            {
            1.0,
            x,
            y,
            z,
            x * y,
            x * z,
            y * z,
            x * y * z
            });

            foreach (Matrix<double> coeffs in pathInterpolations)
            {
                // basis (1x8) * coeffs (8x3N) -> (3N)
                Vector<double> flat = basis * coeffs;

                int pointCount = flat.Count / 3;
                Vector3[] path = new Vector3[pointCount];

                for (int i = 0; i < pointCount; i++)
                {
                    path[i] = new Vector3(
                        (float)flat[3 * i],
                        (float)flat[3 * i + 1],
                        (float)flat[3 * i + 2]);
                }

                rays.Add(path);
            }
        }
        else
        {
            Debug.Log("No interpolations available");
        }

        drawRays(rays);

        
    }
}
