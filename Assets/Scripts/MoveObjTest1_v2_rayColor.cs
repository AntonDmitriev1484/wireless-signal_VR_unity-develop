/*
 * this uses the "RayPathSet_v2.cs" which handles 5 colums CSV. 
 * Tried with gameobject as ray, but too heavy
 */


using System.IO;
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class MoveObjTest1_v2_rayColor: MonoBehaviour
{
    // Public variable to specify the CSV filename in the Inspector
    [SerializeField, Tooltip("a CSV file name to read data from")]
    private string csvFileName = "ray_path_data_Test_5cols.csv";

    [SerializeField, Tooltip("a Ray GameObject")]
    private GameObject objRay; // The object to instantiate and move along paths
    [SerializeField] private GameObject TxObj; // The object to instantiate for Transmiter
    [SerializeField] private GameObject RxObj; // The object to instantiate for Receiver

    [SerializeField, Tooltip("a GameObject to mark the path")]
    private GameObject objToMark; // The object to instantiate
    [SerializeField] private float RaySpeed; // init 1.5f;


    private int numRays; // Number of rays
    private int numFields = 5; // Number of fields in the data file
    private float raySize = 0.03f; // Size of ray object
    private float[] startTime; // Start time for each ray object

    // List to hold all instantiated ray objects
    private List<GameObject> rayObjects = new List<GameObject>();

    // A list to hold all the loaded data from the CSV
    private List<RayPathSet_v2> loadedRaysPath = new List<RayPathSet_v2>();

    // = Color Control ======================================================== BEGIN
    // Add a ColorHelper component to your scene and assign it in the Inspector. 
    [SerializeField] private ColorHelper colorHelper;

    // Constants for power interpolation
    // each ray's power value is updated toward its min power val in UpdateParticles()
    private const float POWER_MAX_dBm = 20.0f;
    private float lowestPowerValRays_dBm; //  lowest power value among all rays from data file. calculate once at begin.
    private const float TEST_LOWEST_POWER_VAL_dBm = -100.0f; // expect to get automatically in code. Lowest possible power value if no rays are loaded

    // Constants for color interpolation
    // note: color index moves from Max to Min as well as power value moves from POWER_MAX_dBm(20) to ray's min
    private const int COLOR_IDX_MAX = 220; //Start Red. ColorHelper.PALETTE_COLOR_COUNT is Max
    private const int COLOR_IDX_MIN = 0;


    // Arrays to track path distances
    private float[] pathTotalLengths;  // Total length of each ray path
    private float[] pathDistanceTraveled;  // Distance traveled by each ray
    // = Color Control ======================================================== END


    // Ray moving play/pause toggle control ===========================
    private bool isRayMovementPaused = true; // Start with rays paused
    // store segment progress time on pause of each ray in its a segment-path out of its multiple paths
    private float[] rayPathSegmentProgressTimeOnPause;

    private void InitializeRayObjects()
    {
        // Make sure objRay is assigned
        if (objRay == null)
        {
            Debug.LogError("objRay game object is not assigned!");
            return;
        }

        // Set numRay value by counting the number list of loadedRaysPath
        numRays = loadedRaysPath.Count;

        // Display numRays
        Debug.Log("DBG: Number of rays: " + numRays);

        // Data should be available by now
        if (numRays > 0)
        {
            // Initialize startTime array
            this.startTime = new float[this.numRays];

            // Initialize segment progress time tracking array
            this.rayPathSegmentProgressTimeOnPause = new float[this.numRays]; // allocate for all rays

            // Instantiate ray objects for each path
            for (int i = 0; i < numRays; i++)
            {
                // Initialize ray game objects
                InitializeRayObject(i);
            }
        }
        else
        {
            Debug.LogError("Error: No ray path data is available!");
        }
    }

    // Initializes a ray game object for the specified path index
    private void InitializeRayObject(int pathIndex)
    {
        // Get the path positions for this ray
        RayPathSet_v2 rayPath = loadedRaysPath[pathIndex];
        List<Vector3> pathPositions = rayPath.PathPositions;

        // Check if there are enough path positions
        if (pathPositions.Count > 0)
        {
            // Instantiate the ray object at the first position in the path
            GameObject rayInstance = Instantiate(objRay, pathPositions[0], Quaternion.identity);
            rayInstance.name = $"RayObj_{rayPath.RxNum}_{pathIndex}";

            // You can customize the ray object appearance here if needed
            // For example, set color, scale, etc.

            // Set scale to match the raySize
            rayInstance.transform.localScale = new Vector3(raySize, raySize, raySize);

            // Try to set color if the object has a renderer
            Renderer renderer = rayInstance.GetComponent<Renderer>();
            //if (renderer != null)
            //{
            //    renderer.material.color = Color.red;
            //}

            // Add to our list of ray objects
            rayObjects.Add(rayInstance);

            // Record the start time for this object
            this.startTime[pathIndex] = Time.time;
        }
        else
        {
            Debug.LogWarning($"No path positions available for ray {pathIndex}.");
        }
    }

    // = Color Control ======================================================== BEGIN

    // Calculate the total distance of each ray's path
    private void InitializePathDistances()
    {
        pathTotalLengths = new float[loadedRaysPath.Count];
        pathDistanceTraveled = new float[loadedRaysPath.Count];

        // Calculate total length for each path
        for (int i = 0; i < loadedRaysPath.Count; i++)
        {
            float totalLength = 0f;
            var positions = loadedRaysPath[i].PathPositions;

            // Sum up the distances between consecutive points
            for (int j = 0; j < positions.Count - 1; j++)
            {
                totalLength += Vector3.Distance(positions[j], positions[j + 1]);
            }

            pathTotalLengths[i] = totalLength;
            pathDistanceTraveled[i] = 0f;

            //Debug.Log($"Ray {i}: Total path length = {totalLength}, Min power = {loadedRaysPath[i].PowerNum}");
        }
    }

    private void InitializeColorPalette()
    {
        // Calculate the lowest power value and cache it
        lowestPowerValRays_dBm = GetLowestPowerVal_dBm();

        CheckColorVariables();
    }


    // check if color_IDX_MIN and COLOR_IDX_MAX are set correctly
    private void CheckColorVariables()
    {
        Debug.Log($"Color index range: {COLOR_IDX_MIN} to {COLOR_IDX_MAX}");

        if (COLOR_IDX_MIN < 0 || COLOR_IDX_MAX < 0 || COLOR_IDX_MAX >= ColorHelper.PALETTE_COLOR_COUNT)
        {
            // display color idx variables in the console
            //Debug.LogError($"Color index range is not set correctly! COLOR_IDX_MIN: {COLOR_IDX_MIN}, COLOR_IDX_MAX: {COLOR_IDX_MAX}");

            throw new ArgumentOutOfRangeException($"Color index range is not set correctly! COLOR_IDX_MIN: {COLOR_IDX_MIN}, COLOR_IDX_MAX: {COLOR_IDX_MAX}");

        }

    }

    // Calculate interpolated power value based on distance traveled
    private float GetPowerValOfRay_dBm(int rayIndex, float distanceTraveled)
    {
        if (rayIndex < 0 || rayIndex >= loadedRaysPath.Count)
            return POWER_MAX_dBm;

        float totalDistance = pathTotalLengths[rayIndex];
        if (totalDistance <= 0)
            return POWER_MAX_dBm;

        float minPowerVal = loadedRaysPath[rayIndex].PowerNum;  // dBm value of the ray
        float progress = distanceTraveled / totalDistance;

        // Linear interpolation from max power (at start) to min power (at end)
        return POWER_MAX_dBm - progress * (POWER_MAX_dBm - minPowerVal);
    }


    // Find the smallest power value among all rays
    private float GetLowestPowerVal_dBm()
    {
        // Start with the first ray's power as minimum
        float lowestPower_AllRays = loadedRaysPath[0].PowerNum;

        // Iterate through all rays to find the minimum power value
        foreach (var rayPath in loadedRaysPath)
        {
            if (rayPath.PowerNum < lowestPower_AllRays)
                lowestPower_AllRays = rayPath.PowerNum;
        }

        // deispaly the lowest power value in the console
        Debug.Log($"DBG: Lowest power value among all rays: {lowestPower_AllRays}");

        //return TEST_LOWEST_POWER_VAL_dBm;  // for Testing
        return lowestPower_AllRays;
    }

    // Convert power value to color index for the palette
    private int GetColorIndexFromPower(float powerVal_dBm, float powerMin_dBm)
    {
        // Use the cached lowest power value instead of calling GetLowestPowerVal_dBm() repeatedly
        float POWER_RANGE_dBm = POWER_MAX_dBm - lowestPowerValRays_dBm;

        // Calculate normalized position in power range (0 to 1)
        float normalizedPower = (powerVal_dBm - lowestPowerValRays_dBm) / POWER_RANGE_dBm;

        // Convert to color index
        int colorIdx = Mathf.RoundToInt(normalizedPower * (COLOR_IDX_MAX - COLOR_IDX_MIN) + COLOR_IDX_MIN);

        // Ensure the result is within valid range
        return Mathf.Clamp(colorIdx, COLOR_IDX_MIN, COLOR_IDX_MAX);
    }

    // = Color Control ======================================================== END



    /* Fill simple data from code for testing
     * plan position: Vector3(0,0,0)
     * the player world transform: 
     * UnityEditor.TransformWorldPlacementJSON:{"position":{"x":1.190000057220459,"y":0.0,"z":-4.739999771118164},"rotation":{"x":0.0,"y":0.009160500019788742,"z":0.0,"w":0.9999580979347229},"scale":{"x":1.0,"y":1.0,"z":1.0}}
     */
    void ReadDataFromCode_Test1()
    {
        loadedRaysPath.Clear();

        Debug.Log("DBG: Simulating reading CSV data...");
        // Simulate reading a few lines of CSV data with 5 columns
        string csvLine1 = "1,0,Tx-Rx,2,\"0 0 0, 1.5 2 0, 3 1 0\"";
        string csvLine2 = "1,0,Tx-Rx,4,\"0 0 0, 1 3 0, 2 1 0, 3 2 0\"";
        string csvLine3 = "2,0,Tx-Rx,2,\"0 0 0, 3 3 0\"";
        string csvLine4 = "2,0,Tx-Rx,3,\"0 0 0, 2 1.5 0, 3 3.5 0\"";

        LoadDataFromCSVLine(csvLine1);
        LoadDataFromCSVLine(csvLine2);
        LoadDataFromCSVLine(csvLine3);
        LoadDataFromCSVLine(csvLine4);
    }



    // to read data from a CSV file located in the Resources folder, which works for VR. This took a while. (NEW)
    // filename should not contain extention name
    void ReadDataFromCSVFile(string filename)
    {
        loadedRaysPath.Clear();

        
        // Below code looks to be correct, but it only work on PC, not on VR device!
        //----------------------- START
        ////add ".csv" to the filename and name it filenameExt
        //string filenameExt = filename + ".csv";
        //Debug.Log("CSV filenameExt: " + filenameExt);


        ////check file exist.Assets/Resources/ray_path_data.csv
        //string filePath = Path.Combine(Application.dataPath, "Resources", filenameExt);
        //Debug.Log("CSV FilePath: " + filePath);

        //if (!File.Exists(filePath))
        //{
        //    Debug.LogError("CSV file not found at path: " + filePath);
        //    return;
        //}

        // remove any extension from the filename. This is needed because the Resources.Load method does not require the file extension.
        //string filenameNoExt = Path.GetFileNameWithoutExtension(filename);
        //Debug.Log($"DBG ReadDataFromCSVFile> filenameNoExt: {filenameNoExt}");
        //------------------------ END

        try
        {
            // Load the file located in the resources folder
            string csvData = Resources.Load<TextAsset>(filename).text;
            //string csvData = Resources.Load<TextAsset>(filenameNoExt).text;


            // Split the CSV data into lines
            string[] lines = csvData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // To skip the header line, start processing from the second line (index 1) 
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

    // read data from Asset/Data/ which works on PC but not on VR device (OLD)
    void ReadDataFromCSVFile_Data(string filename)
    {
        loadedRaysPath.Clear();

        // If your file is in a subfolder, e.g., Assets/Data/ray_path_data.csv,
        // use: Path.Combine(Application.dataPath, "Data", filename);
        string filePath = Path.Combine(Application.dataPath, "Data", filename);
        Debug.Log("CSV FilePath: " + filePath);

        if (!File.Exists(filePath))
        {
            Debug.LogError("CSV file not found at path: " + filePath);
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(filePath);

            // To skip the header line, start processing from the second line (index 1) 
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
        string[] fields = line.Split(',');

        if (fields.Length >= numFields)
        {
            // --- Parse Rx Number ---
            if (int.TryParse(fields[0].Trim(), out int rxNum))
            {
                // Create a new RayPathData object
                RayPathSet_v2 rayPathDat = new RayPathSet_v2();
                rayPathDat.RxNum = rxNum;
                rayPathDat.PathPositionsIdx = 0; // Explicitly initialize path position index to 0

                // --- Parse Power Number ---
                if (float.TryParse(fields[1].Trim(), out float powNum))
                {
                    // Store the min power value for this ray at Rx position
                    rayPathDat.PowerNum = powNum;

                    // --- Parse Interaction Description ---
                    rayPathDat.Interaction_Description = fields[2].Trim();

                    // --- Parse Total Interactions for Path ---
                    if (int.TryParse(fields[3].Trim(), out int totalInteractions))
                    {
                        rayPathDat.Total_Interactions_for_Path = totalInteractions;
                    }
                    else
                    {
                        Debug.LogError($"Failed to parse Total Interactions for Path from line: {line}");
                    }

                    // --- Parse Path Positions String ---
                    string positionsStringRaw = string.Join(",", fields, 4, fields.Length - 4).Trim();

                    // Remove potential surrounding quotes
                    if (positionsStringRaw.StartsWith("\"") && positionsStringRaw.EndsWith("\""))
                    {
                        positionsStringRaw = positionsStringRaw.Substring(1, positionsStringRaw.Length - 2);
                    }

                    // Use the ParsePathPositionsString method from our data structure
                    rayPathDat.ParsePathPositionsString(positionsStringRaw);

                    // --- Add to our list ---
                    loadedRaysPath.Add(rayPathDat);
                }
                else
                {
                    Debug.LogError($"Failed to parse Power Num {powNum} from line: {line}");
                }
            }
            else
            {
                Debug.LogError($"Failed to parse RxNum from line: {line}");
            }
        }
        else
        {
            Debug.LogError($"Line does not have enough fields (expected at least 4): {line}");
        }
    }

    // Mark all path change positions with objToMark prefab 
    void MarkPathPositions()
    {
        // Check if the objectToInstantiate is assigned
        if (objToMark == null)
        {
            Debug.LogError("Object to Instantiate is not assigned!");
            return;
        }

        // TEST ---------------------------------------IN
        // define markPosList in code
        //List<Vector3> markPosList = new List<Vector3>
        //{
        //    new Vector3(3, 1, 0),
        //    new Vector3(6, 2, 0)
        //};

        //// Iterate through each position in the list
        //foreach (Vector3 pos in markPosList)
        //{
        //    // Instantiate the object at the current position with no rotation (identity)
        //    Instantiate(objToMark, pos, Quaternion.identity);

        //    // If you want to parent the instantiated objects to this script's GameObject,
        //    // you can use the following line instead:
        //    // Instantiate(objToMark, pos, Quaternion.identity, this.transform);
        //}
        // TEST ---------------------------------------OUT


        foreach (RayPathSet_v2 rayPath in loadedRaysPath)
        {
            if (rayPath.PathPositions.Count > 0)
            {
                // Iterate through positions of each path, skipping the first and last positions
                for (int i = 1; i < rayPath.PathPositions.Count - 1; i++)
                //for (int i = 0; i < rayPath.PathPositions.Count; i++)
                {
                    // Instantiate the object at the current position with no rotation (identity)
                    Instantiate(objToMark, rayPath.PathPositions[i], Quaternion.identity);
                }
            }
        }
    }

    // mark at the first point from the first element of loadedRaysPath
    void MarkStartPoint_Tx()
    {
        // Check if TxObj is assigned
        if (TxObj == null)
        {
            Debug.LogError("TxObj is not assigned in the Inspector! Cannot mark start point.");
            return;
        }

        // Check if there are any paths loaded first
        if (loadedRaysPath.Count <= 0 || loadedRaysPath[0].PathPositions.Count <= 0)
            return;

        // Get the position from the first path's first position
        RayPathSet_v2 rayPathFirst = loadedRaysPath[0];
        Vector3 startPosition = rayPathFirst.PathPositions[0];

        // Instantiate the TxObj at the start position
        GameObject startMark = Instantiate(TxObj, startPosition, Quaternion.identity);

        // Name the marker for easy identification
        startMark.name = "Tx_Obj";
    }




    // Marks the end points of all ray paths with RxObj instances
    void MarkEndPoints_Rx()
    {
        // Check if RxObj is assigned
        if (RxObj == null)
        {
            Debug.LogError("RxObj is not assigned in the Inspector! Cannot mark end points.");
            return;
        }

        // Check if there are any paths loaded
        if (loadedRaysPath.Count <= 0)
        {
            Debug.LogWarning("No ray paths available to mark end points.");
            return;
        }

        // Dictionary to track unique end positions to avoid duplicate markers
        Dictionary<Vector3, bool> markedPositions = new Dictionary<Vector3, bool>();

        // Iterate through each path to mark its end point
        foreach (RayPathSet_v2 rayPath in loadedRaysPath)
        {
            if (rayPath.PathPositions.Count > 0)
            {
                // Get the last position in the path (the end point)
                Vector3 endPosition = rayPath.PathPositions[rayPath.PathPositions.Count - 1];

                // Skip if we've already marked this position (avoid duplicates)
                if (markedPositions.ContainsKey(endPosition))
                    continue;

                // Mark this position as processed
                markedPositions[endPosition] = true;

                // Instantiate the RxObj at the end position
                GameObject endMarker = Instantiate(RxObj, endPosition, Quaternion.identity);

                // Name the marker for easy identification
                endMarker.name = $"Rx_Obj_{rayPath.RxNum}";
            }
        }

        Debug.Log($"Marked {markedPositions.Count} unique end points with RxObj instances.");
    }

    void MarkViaLines_DEBUG()
    {
        // Iterate through each path in the loaded data
        foreach (RayPathSet_v2 rayPath in loadedRaysPath)
        {
            if (rayPath.PathPositions.Count > 0)
            {
                for (int i = 0; i < rayPath.PathPositions.Count - 1; i++)
                {
                    // Draw a line between the current position and the next position
                    Debug.DrawLine(rayPath.PathPositions[i], rayPath.PathPositions[i + 1], Color.red, 5f);
                }
            }
        }
    }

    // Draw lines with multiple LineRenderers from loadedRaysPath
    void MarkPathLine_MultiPaths()
    {
        // Iterate through each path in the loaded data
        foreach (RayPathSet_v2 rayPath in loadedRaysPath)
        {
            // Create a new GameObject for each pathLine
            GameObject pathObject = new GameObject("PathLine_" + rayPath.RxNum);
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
            lineRenderer.positionCount = rayPath.PathPositions.Count;

            // Set the positions for the LineRenderer
            lineRenderer.SetPositions(rayPath.PathPositions.ToArray());
        }
    }

    // Check if a file exists in the Resources directory
    private bool CheckIfFileExistsInResources(string filename)
    {
        try
        {
            // display filename without extension
            Debug.Log("Data filename (no ext): " + filename);

            // Resources.Load returns null if the file doesn't exist
            TextAsset textAsset = Resources.Load<TextAsset>(filename);
            return textAsset != null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error checking for file existence: {e.Message}");
            return false;
        }
    }

    void GetData()
    {
        //-----------------------------------------
        // Test data from code for quick change
        //-----------------------------------------
        //ReadDataFromCode_Test2(); 

        //-----------------------------------------------
        // Read data from the specified CSV file
        //----------------------------------------------

        // check if csvFileName exist in Asset/Resources dir, csvFileName should not contain extension name
        if (CheckIfFileExistsInResources(csvFileName))
        {
            ReadDataFromCSVFile(csvFileName);
            //DisplayLoadedData();
        }
        else
        {
            Debug.LogError($"CSV file '{csvFileName}' not found in Resources folder. Please check the file name and location.");
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetData();

        // TEST --- BEGIN
        // Move this to a separate method if needed. Test_colorIndexFromPower();
        //int colorIdx = 0;

        //colorIdx = GetColorIndexFromPower_dBm(20, -10);
        //Debug.Log($"Color Index for Power 20 and Min Power -10: {colorIdx}");

        //colorIdx = GetColorIndexFromPower_dBm(10, -10);
        //Debug.Log($"Color Index for Power 10 and Min Power -10: {colorIdx}");

        //colorIdx = GetColorIndexFromPower_dBm(0, -10);
        //Debug.Log($"Color Index for Power 0 and Min Power -10: {colorIdx}");

        //colorIdx = GetColorIndexFromPower_dBm(-10, -10);
        //Debug.Log($"Color Index for Power -10 and Min Power -10: {colorIdx}");
        // TEST --- END

        //MarkStartPoint_Tx();
        //MarkEndPoints_Rx();

        //MarkPathPositions(); // Mark all path positions with objToMark prefab

        // Draw lines to visualize the paths
        //MarkPathLine_MultiPaths();
        //MarkViaLines_DEBUG();

        // Initialize ray objects
        InitializeRayObjects();

        // Initialize path distances for power calculations
        InitializePathDistances();

        // Init color palette related things
        InitializeColorPalette();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateRayObjects();
    }

    // toggle play/pause state of entire rays movement
    public void RayPlayPause()
{
    // Toggle the pause state
    isRayMovementPaused = !isRayMovementPaused;
    
    // If pausing, store the current segment progress for each ray
    if (isRayMovementPaused)
    {
        // allocate the array if needed
        if (rayPathSegmentProgressTimeOnPause == null || rayPathSegmentProgressTimeOnPause.Length != rayObjects.Count)
        {
            rayPathSegmentProgressTimeOnPause = new float[rayObjects.Count];
        }
        
        // save the progressed time since the start of the segment for each ray object
        float currentTime = Time.time;
        for (int i = 0; i < rayObjects.Count; i++)
        {
            rayPathSegmentProgressTimeOnPause[i] = currentTime - startTime[i];
        }
        
        Debug.Log("Ray movement PAUSED");
    }
    // If resuming, adjust the start times based on stored progress
    else
    {
        if (rayPathSegmentProgressTimeOnPause != null && startTime != null)
        {
            float currentTime = Time.time;
            for (int i = 0; i < startTime.Length && i < rayPathSegmentProgressTimeOnPause.Length; i++)
            {
                    // restore the start time for each particle along with the progressed time of the segment
                    startTime[i] = currentTime - rayPathSegmentProgressTimeOnPause[i];
            }
        }
        
        Debug.Log("Ray movement PLAYING");
    }
}

    // Update the ray objects' positions along their paths
    private void UpdateRayObjects()
    {
        // if rays move paused, don't update ray positions
        if (isRayMovementPaused)
            return;

        if (colorHelper == null)
        {
            Debug.LogWarning("ColorHelper reference is missing. Please add a ColorHelper component to your scene and assign it in the Inspector. Ray colors will not be updated.");
        }

        // Loop through each ray object
        for (int i = 0; i < rayObjects.Count; i++)
        {
            // Get the current ray object
            GameObject rayObject = rayObjects[i];

            // Get the path data for this ray
            RayPathSet_v2 rayPath = loadedRaysPath[i];
            List<Vector3> pathPositions = rayPath.PathPositions;

            // Check if the path exists and has at least two positions, and if the ray is not yet at the last position
            if (pathPositions != null && pathPositions.Count > 1 && rayPath.PathPositionsIdx < pathPositions.Count - 1)
            {
                // Get the start and end positions for the current segment
                Vector3 currentSegmentStart = pathPositions[rayPath.PathPositionsIdx];
                Vector3 currentSegmentEnd = pathPositions[rayPath.PathPositionsIdx + 1];

                // Calculate time elapsed since the start of the current segment
                float timeSinceSegmentStart = Time.time - startTime[i];


                // Calculate the distance and duration for the current segment
                float currentSegmentDistance = Vector3.Distance(currentSegmentStart, currentSegmentEnd);

                // Handle zero distance segments to avoid division by zero. Treat as instant transition.
                float currentSegmentDuration = (RaySpeed <= 0 || currentSegmentDistance < Mathf.Epsilon) ? 0f : currentSegmentDistance / RaySpeed;

                // Determine the interpolation factor (t) for the current segment
                // This value goes from 0 to 1 over the duration of the segment
                float segment_t = (currentSegmentDuration > 0) ? Mathf.Clamp01(timeSinceSegmentStart / currentSegmentDuration) : 1f; // If duration is 0, snap instantly

                // Calculate the object's position using Lerp for the current segment
                Vector3 newPos = Vector3.Lerp(currentSegmentStart, currentSegmentEnd, segment_t);

                // Update the ray object's position
                rayObject.transform.position = newPos;


                //== Ray Color ========================= START
                // Update distance traveled
                float currentSegmentProgress = currentSegmentDistance * segment_t;
                if (rayPath.PathPositionsIdx == 0)
                {
                    pathDistanceTraveled[i] = currentSegmentProgress;
                }
                else
                {
                    float previousDistance = 0f;
                    for (int j = 0; j < rayPath.PathPositionsIdx; j++)
                    {
                        previousDistance += Vector3.Distance(pathPositions[j], pathPositions[j + 1]);
                    }
                    pathDistanceTraveled[i] = previousDistance + currentSegmentProgress;
                }

                // Calculate power value based on distance traveled
                float powerVal = GetPowerValOfRay_dBm(i, pathDistanceTraveled[i]);
                float minPowerVal = rayPath.PowerNum;

                // Calculate color index based on power value
                int colorIdx = GetColorIndexFromPower(powerVal, minPowerVal);

                // JK_DBG display debug info
                //Debug.Log($"Ray {i} - PathIdx: {rayPath.PathPositionsIdx}, Segment_t: {segment_t}, PowerVal: {powerVal_dBm}, MinPowerVal: {minPowerVal}, ColorIdx: {colorIdx}");

                // Apply color if ColorHelper is available
                if (colorHelper != null)
                {
                    // Get color from palette using ColorHelper
                    Color rayColor = colorHelper.GetPaletteColor(colorIdx);
                    
                    // Apply color to the ray object
                    Renderer renderer = rayObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = rayColor;
                    }
                }
                //== Ray Color ========================= END

                // Check if the object has completed the current segment
                if (segment_t >= 1.0f)
                {
                    // The object has reached the end of the current segment
                    // Snap the object exactly to the end position
                    rayObject.transform.position = currentSegmentEnd;

                    // Move to the next point in the path
                    rayPath.PathPositionsIdx++;

                    // If there are more segments to move along, reset the timer for the new segment
                    if (rayPath.PathPositionsIdx < pathPositions.Count - 1)
                    {
                        startTime[i] = Time.time; // Reset timer so the next segment starts timing from now
                    }
                }
            }
            else if (pathPositions != null && pathPositions.Count > 0)
            {
                // If the object was at the last point or the path had only one point,
                // ensure its position is set to the final position
                rayObject.transform.position = pathPositions[pathPositions.Count - 1];
                
            }
        }
    }

    // Helper method to move at constant speed between two points
    private Vector3 MoveAtConstantSpeed(Vector3 pos1, Vector3 pos2, float speed, float elapsedTime)
    {
        float distance = Vector3.Distance(pos1, pos2);
        float duration = (speed <= 0) ? float.MaxValue : distance / speed;
        float t = Mathf.Clamp01(elapsedTime / duration);
        return Vector3.Lerp(pos1, pos2, t);
    }

    // Method to display values from loadedRaysPath for testing
    void DisplayLoadedData()
    {
        foreach (RayPathSet_v2 rayPath in loadedRaysPath)
        {
            Debug.Log($"RxNum: {rayPath.RxNum}, PowerNum: {rayPath.PowerNum}, Interaction_Description: {rayPath.Interaction_Description}, Total_Interactions_for_Path: {rayPath.Total_Interactions_for_Path}");
            Debug.Log("Path Positions:");
            foreach (Vector3 pos in rayPath.PathPositions)
            {
                Debug.Log(pos);
            }
        }
    }
}
