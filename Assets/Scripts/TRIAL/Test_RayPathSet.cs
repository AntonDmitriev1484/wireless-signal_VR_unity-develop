// This script is used to test the RayPathSet class.
using System.Collections.Generic;
using UnityEngine;
using System; // Required for StringSplitOptions
using System.IO;

public class Test_RayPathSet : MonoBehaviour
{
    // A list to hold all the loaded data from the CSV
    private List<RayPathSet> loadedRaysPath = new List<RayPathSet>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    // Public variable to specify the CSV filename in the Inspector
    private string csvFileName = "ray_path_data_Test.csv";

    void Start()
    {
        //--------------------------------------------------
        ReadDataFromCode();

        //----------------------------------------------
        Debug.Log("Attempting to read CSV data from file: " + csvFileName);

        // Read data from the specified CSV file
        //ReadDataFromCSVFile(csvFileName);
        Debug.Log("CSV data simulation complete. Loaded " + loadedRaysPath.Count + " grid paths.");

        //-----------------------------------------------
        // Now, let's demonstrate accessing the loaded data
        DisplayLoadedData();
    }

    // fill data from code for testing
    void ReadDataFromCode()
    {
        loadedRaysPath.Clear();

        Debug.Log("Simulating reading CSV data...");
        // Simulate reading a few lines of CSV data
        string csvLine1 = "1,1,3,\"0 0 0, 3.0 5.5 0, 5.0 5.5 0\"";
        string csvLine2 = "1,2,2,\"0 0 0, 5.0 5.5 0\"";
        string csvLine3 = "2,1,2,\"0 0 0, 3.0 3.1 3.2\""; // Example with no path positions
        string csvLine4 = "2,2,2,\"0 0 0, 2.0 2.1 2.2\""; // Example with one position

        LoadDataFromCSVLine(csvLine1);
        LoadDataFromCSVLine(csvLine2);
        LoadDataFromCSVLine(csvLine3);
        LoadDataFromCSVLine(csvLine4);
    }

    // Method to read data from a CSV file
    void ReadDataFromCSVFile(string filename)
    {
        loadedRaysPath.Clear();

        // display Application.dataPath
        //Debug.Log("Application.dataPath: " + Application.dataPath); // Debugging line to check the data path

        // If your file is in a subfolder, e.g., Assets/Data/ray_path_data.csv,
        // use: Path.Combine(Application.dataPath, "Data", filename);
        string filePath = Path.Combine(Application.dataPath, "Data", filename);
        Debug.Log("CSV FilePath: " + filePath); // Debugging line to check the file path

        if (!File.Exists(filePath))
        {
            Debug.LogError("CSV file not found at path: " + filePath);
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(filePath);

            // to skip the header line, start processing from the second line (index 1) 
            for (int i = 1; i < lines.Length; i++)
            {
                LoadDataFromCSVLine(lines[i]); // Pass each data line to the processing method
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error reading CSV file: " + e.Message);
        }
    }

    // Method to process a single line of CSV data
    void LoadDataFromCSVLine(string line)
    {
        // Skip empty, comment, or the header line content
        if (string.IsNullOrEmpty(line) || line.TrimStart().StartsWith("//"))
        {
            return;
        }

        // Split the line by the primary comma delimiter
        string[] parts = line.Split(',');

        if (parts.Length >= 4) // Check for at least 4 parts now
        {
            // --- Parse Grid Number ---
            if (int.TryParse(parts[0].Trim(), out int gridNum))
            {
                // Create a new GridPathData object
                RayPathSet gridPath = new RayPathSet();
                gridPath.GridNumber = gridNum;

                // --- Parse Rx Number (New Column) ---
                if (int.TryParse(parts[1].Trim(), out int rxNum))
                {
                    gridPath.RxNum = rxNum;

                    // --- Parse Path Positions String ---
                    // PathPosNum is parts[2]. Positions string starts from parts[3].
                    // Reconstruct the positions string from parts[3] onwards.
                    string positionsStringRaw = string.Join(",", parts, 3, parts.Length - 3).Trim();

                    // Remove potential surrounding quotes
                    if (positionsStringRaw.StartsWith("\"") && positionsStringRaw.EndsWith("\""))
                    {
                        positionsStringRaw = positionsStringRaw.Substring(1, positionsStringRaw.Length - 2);
                    }

                    // Use the ParsePathPositionsString method from our data structure
                    gridPath.ParsePathPositionsString(positionsStringRaw);

                    // --- Add to our list ---
                    loadedRaysPath.Add(gridPath);

                    // Optional: Validate pathPosNum if needed (parts[2])
                    // int declaredPathPosNum;
                    // if (int.TryParse(parts[2].Trim(), out declaredPathPosNum))
                    // {
                    //     if (gridPath.PathPositions.Count != declaredPathPosNum)
                    //     {
                    //         Debug.LogWarning($"Grid {gridNum}: Declared pathPosNum ({declaredPathPosNum}) does not match actual parsed positions ({gridPath.PathPositions.Count}).");
                    //     }
                    // }
                    // else
                    // {
                    //     Debug.LogError($"Failed to parse pathPosNum for grid {gridNum}: {parts[2]}");
                    // }
                }
                else
                {
                    Debug.LogError($"Failed to parse RxNumber for grid {gridNum} from line: {line}");
                }
            }
            else
            {
                Debug.LogError($"Failed to parse GridNumber from line: {line}");
            }
        }
        else
        {
            Debug.LogError($"Line does not have enough parts (expected at least 4): {line}");
        }
    }

    // Method to use (display) the data loaded
    void DisplayLoadedData()
    {
        Debug.Log("\n--- Displaying Loaded Data ---");

        if (loadedRaysPath.Count == 0)
        {
            Debug.Log("No grid path data loaded.");
            return;
        }

        foreach (RayPathSet rayPath in loadedRaysPath)
        {
            Debug.Log($"Grid Number: {rayPath.GridNumber}");
            Debug.Log($"Number of Path Positions: {rayPath.PathPositions.Count}");
            Debug.Log($"Rx Number: {rayPath.RxNum}");

            if (rayPath.PathPositions.Count > 0)
            {
                Debug.Log("Ray Path Positions:");
                for (int i = 0; i < rayPath.PathPositions.Count; i++)
                {
                    Debug.Log($"- Position {i}: {rayPath.PathPositions[i]}");
                }
            }
            Debug.Log("---"); // Separator between entries
        }

        Debug.Log("--- End of Display ---");
    }
}
