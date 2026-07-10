/*
 * This keeps data sturcture for keeping the ray path data with grid number
 */
using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class RayPathSet : MonoBehaviour
{
    [HideInInspector] public int GridNumber;
    [HideInInspector] public int RxNum;
    [HideInInspector] public List<Vector3> PathPositions = new List<Vector3>();
    // intenal variable to keep the path positions index
    [HideInInspector] public int PathPositionsIdx = 0;

    public void ParsePathPositionsString(string positionsString)
    {
        PathPositions.Clear();

        if (string.IsNullOrEmpty(positionsString))
        {
            Debug.LogWarning("Positions string is empty or null.");
            return;
        }

        string[] positionStrings = positionsString.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string posStr in positionStrings)
        {
            string[] coords = posStr.Trim().Split(' ');

            if (coords.Length == 3)
            {
                if (float.TryParse(coords[0], out float x) &&
                    float.TryParse(coords[1], out float y) &&
                    float.TryParse(coords[2], out float z))
                {
                    PathPositions.Add(new Vector3(x, y, z));
                }
                else
                {
                    Debug.LogError($"Failed to parse coordinates: {posStr}");
                }
            }
            else
            {
                Debug.LogError($"Invalid number of coordinates ({coords.Length}) in position string: {posStr}");
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
