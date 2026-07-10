/* 
 * This handles reading a csv file and parse its values 
 */

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class CsvReader : MonoBehaviour
{
    // the CSV filename
    //private string csvFileName = "ray_path_data_Test_5cols.csv";

    // Struct to hold the parsed data for each row
    public struct RayPathStruct
    {
        public int Rx_Number;
        public float Path_Power;
        public string Interaction_Description;
        public int Total_Interactions_for_Path;
        public List<Vector3> Interaction_Coordinates; // Using Vector3 for coordinates
        public int PathPositionsIdx; // to keep track of the path positions index per ray

        public RayPathStruct(int rxNum, float pathPower, string interactionDesc, int totalInteractions, List<Vector3> interactionCoords, int pathPositionsIdx)
        {
            Rx_Number = rxNum;
            Path_Power = pathPower;
            Interaction_Description = interactionDesc;
            Total_Interactions_for_Path = totalInteractions;
            Interaction_Coordinates = interactionCoords;
            PathPositionsIdx = pathPositionsIdx;
        }
    }

    // to store all the parsed row data
    private List<RayPathStruct> parsedData = new List<RayPathStruct>();


    /* 
     * Reads a CSV file from the specified path and parses its content.
     * Skips the first line (header).
     * return the csvRaysData list
     */
    public List<RayPathStruct> ReadAndParseCsv(string filePath)
    //public void ReadAndParseCsv(string filePath)

    {
        parsedData.Clear(); // Clear any previous data

        if (!File.Exists(filePath))
        {
            Debug.LogError("CSV file not found at: " + filePath);
            return null;
        }

        try
        {
            // Read all lines from the file
            string[] lines = File.ReadAllLines(filePath);

            // show the number of lines in the file
            Debug.Log("Number of lines in the file: " + lines.Length);

            // Skip the header line (start from index 1)
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];

                // Split the line by comma, handling quoted strings
                string[] values = SplitCsvLine(line);

                if (values.Length >= 5) // Ensure we have at least 5 columns
                {
                    // Parse the values and create a RayPathSet object
                    if (int.TryParse(values[0], out int rxNumber) &&
                        float.TryParse(values[1], out float pathPower) &&
                        int.TryParse(values[3], out int totalInteractions))
                    {
                        string interactionDescription = values[2];
                        List<Vector3> interactionCoordinates = ParseInteractionCoordinates(values[4]);
                        int pathPositionsIdx = 0; // Default value, not from CSV

                        parsedData.Add(new RayPathStruct(rxNumber, pathPower, interactionDescription, totalInteractions, interactionCoordinates, pathPositionsIdx));
                    }
                    else
                    {
                        Debug.LogWarning($"Skipping line {i + 1} due to parsing error: {line}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Skipping line {i + 1} due to incorrect number of columns: {line}");
                }
            }

            Debug.Log($"Successfully parsed {parsedData.Count} data rows from {filePath}");

            return parsedData; // Return the parsed data list
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error reading or parsing CSV file: {e.Message}");

            return null;
        }
    }

    // Splits a CSV line, correctly handling commas within double quotes.
    // Return an array of strings representing the column values
    private string[] SplitCsvLine(string line)
    {
        var values = new List<string>();
        bool inQuotes = false;
        string currentValue = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(currentValue.Trim()); // Add the collected value, trimmed
                currentValue = ""; // Reset for the next value
            }
            else
            {
                currentValue += c; // Append character to the current value
            }
        }

        // Add the last value after the loop finishes
        values.Add(currentValue.Trim());

        return values.ToArray();
    }


    // Parses the string representation of interaction coordinates into a list of Vector3.
    // Expected format: "x y z, x y z, ..."
    // param "coordinatesString": a string containing the coordinate list.
    // Return a list of Vector3 objects.
    private List<Vector3> ParseInteractionCoordinates(string coordinatesString)
    {
        // display the coordinatesString
        Debug.Log("Coordinates String: " + coordinatesString);

        List<Vector3> coordinatesList = new List<Vector3>();

        string[] coordStrings = coordinatesString.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string coordString in coordStrings)
        {
            // Split each coordinate string by space
            string[] axes = coordString.Trim().Split(' ');

            if (axes.Length >= 3)
            {
                if (float.TryParse(axes[0], out float x) &&
                    float.TryParse(axes[1], out float y) &&
                    float.TryParse(axes[2], out float z))
                {
                    coordinatesList.Add(new Vector3(x, y, z));
                }
                else
                {
                    Debug.LogWarning($"Failed to parse coordinate values: {coordString}");
                }
            }
        }

        return coordinatesList;
    }


    // display the data to utilize the csvRaysData list for example,
    public void DisplayParsedData(List<RayPathStruct> parsedData)

    //public void DisplayParsedData()
    {
        // Example of iterating through all parsed data
        foreach (RayPathStruct row in parsedData)
        {
            Debug.Log($"--- Row Data ---");
            Debug.Log($"Rx Number: {row.Rx_Number}");
            Debug.Log($"Path Power: {row.Path_Power}");
            Debug.Log($"Interaction Description: {row.Interaction_Description}");
            Debug.Log($"Total Interactions: {row.Total_Interactions_for_Path}");
            Debug.Log($"Interaction Coordinates: ");
            foreach (Vector3 coord in row.Interaction_Coordinates)
            {
                Debug.Log($"- {coord}");
            }
        }
    }

    // This is for self testing
    void Start()
    {
        ////----------------------------------------------
        //Debug.Log("Attempting to read CSV data from file: " + csvFileName);

        //// If your file is in a subfolder, e.g., Assets/Data/ray_path_data.csv,
        //// use: Path.Combine(Application.dataPath, "Data", filename);
        //string filePath = Path.Combine(Application.dataPath, "Data", csvFileName);
        //Debug.Log("CSV FilePath: " + filePath); // Debugging line to check the file path

        //if (!File.Exists(filePath))
        //{
        //    Debug.LogError("CSV file not found at path: " + filePath);
        //    return;
        //}

        //ReadAndParseCsv(filePath);

        //// Now you can access the parsed data:
        //DisplayParsedData(parsedData);
    }


}
