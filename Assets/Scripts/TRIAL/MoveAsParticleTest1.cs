/*
 *  Works OK.
 *  However, this uses the "RayPathSet.cs" which handles different CSV cols. 
 * 
 */


using System.IO;
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class MoveAsParticleTest1 : MonoBehaviour
{

    private ParticleSystem particleSys;
    private ParticleSystem.Particle[] particles; // each particle is a struct

    private int numRays; // Number of rays
    private int numFields = 4; // Number of fields in the data file
    private float raySize = 0.5f; // Size of ray
    private float[] startTime; // Start time for each particle



    // A list to hold all the loaded data from the CSV
    private List<RayPathSet> loadedRaysPath = new List<RayPathSet>();

    private void InitializeParticles()
    {
        // Get the ParticleSystem component
        this.particleSys = GetComponent<ParticleSystem>();
        if (this.particleSys == null)
        {
            Debug.LogError("ParticleSystem component is missing or not attached to the GameObject.");
            return;
        }

        var partSysMain = particleSys.main;
        partSysMain.startSize = 0;

        // Enable culling
        //particleSys.GetComponent<Renderer>().enabled = IsVisibleFrom(particleSys.GetComponent<Renderer>(), Camera.main);


        // TODO - Get file header values from data file

        // set numRay value by counting the number list of csvRaysData
        numRays = loadedRaysPath.Count;
        
        //display numRays
        Debug.Log("DBG: Number of rays: " + numRays);


        // Data should be available by now
        if (numRays > 0)
        {
            // Initialize particles array
            this.particles = new ParticleSystem.Particle[this.numRays];


            // Configure particle system
            partSysMain.maxParticles = this.numRays;
            this.particleSys.Emit(this.numRays);
            this.particleSys.GetParticles(this.particles);

            this.startTime = new float[this.numRays];

            // Set initial positions for particles
            InitializeParticlePositions();
        }
        else
        {
            Debug.LogError("Error: rayData is not available!");
        }
    }

    // sets the initial positions for each particle
    private void InitializeParticlePositions()
    {
        // Loop through each particle
        for (int i = 0; i < this.numRays; i++)
        {
            // Get the current particle
            ParticleSystem.Particle particle = this.particles[i];

            // Get the path positions for this particle
            RayPathSet rayPath = loadedRaysPath[i];
            List<Vector3> pathPositions = rayPath.PathPositions;

            // Check if there are enough path positions
            if (pathPositions.Count > 0)
            {
                // Set the initial position of the particle to the first position in the path
                particle.position = pathPositions[0];
                // set this particle color to red
                particle.startColor = new Color(1, 0, 0, 1f); // Red color
                // set this particle size to raySize
                particle.startSize = this.raySize;

                this.startTime[i] = Time.time;
            }
            else
            {
                Debug.LogWarning($"No path positions available for particle {i}.");
            }

            // Update the particle in the system
            this.particles[i] = particle;
        }

        // Set the updated particles back to the ParticleSystem
        this.particleSys.SetParticles(this.particles, this.numRays);
    }

    //private void InitializeParticlePositions()
    //{
    //    // Set initial positions for each particle
    //    Vector3 satPos = new Vector3(0, 0, 0);
    //    Color color = new Color(1, 1, 1, 1f); // Color values in Unity are normalized 0-1
    //    //Color color = new Color(1, 0, 0, 1f); // Color values in Unity are normalized 0-1
    //    for (int satIdx = 0; satIdx < this.numRays; satIdx++)
    //    {
    //        satPos.x = rayData[satIdx * this.numFields];
    //        satPos.y = rayData[satIdx * this.numFields + 1];
    //        satPos.z = rayData[satIdx * this.numFields + 2];
    //        particles[satIdx].position = satPos;
    //        particles[satIdx].startColor = color;
    //        particles[satIdx].startSize = this.raySize;

    //        startTime[satIdx] = Time.time;
    //    }

    //    particleSys.SetParticles(particles, particles.Length);
    //}




    // fill data from code for testing
    void ReadDataFromCode()
    {
        loadedRaysPath.Clear();

        Debug.Log("DBG: Simulating reading CSV data...");
        // Simulate reading a few lines of CSV data
        string csvLine1 = "1,2,3,\"0 0 0, 1.5 2 0, 3 1 0\"";
        string csvLine2 = "1,1,4,\"0 0 0, 1 3 0, 2 1 0, 3 2 0\"";
        string csvLine3 = "2,1,2,\"0 0 0, 3 3 0\""; 
        string csvLine4 = "2,2,3,\"0 0 0, 2 1.5 0, 3 3.5 0\"";

        LoadDataFromCSVLine(csvLine1);
        LoadDataFromCSVLine(csvLine2);
        LoadDataFromCSVLine(csvLine3);
        LoadDataFromCSVLine(csvLine4);
    }

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
        // TODO: rename parts to cols
        // We expect three parts: GridNum, PathPosNum, PositionsString
        string[] parts = line.Split(',');

        if (parts.Length >= 4) // TODO: Check for at least 4 parts now
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




    public GameObject objToMark; // The object to instantiate
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

        
        foreach (RayPathSet rayPath in loadedRaysPath)
        {
            if (rayPath.PathPositions.Count > 0)
            {
                for (int i = 0; i < rayPath.PathPositions.Count; i++)
                {
                    // Instantiate the object at the current position with no rotation (identity)
                    Instantiate(objToMark, rayPath.PathPositions[i], Quaternion.identity);
                }
            }
        }
    }

    void MarkViaLines_DEBUG()
    {
        // Iterate through each path in the loaded data
        foreach (RayPathSet rayPath in loadedRaysPath)
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

    // write a method 'MarkPathLine_SinglePath' to draw green lines between the points in the path but using LineRenderer instead of Denug.DrawLine  to show in the Game
    void MarkPathLine_SinglePath()
    {
        LineRenderer lineRenderer;
        // Get the LineRenderer component, or add one if it doesn't exist
        lineRenderer = GetComponent<LineRenderer>();

        // Iterate through each path in the loaded data
        foreach (RayPathSet rayPath in loadedRaysPath)
        {
            //if (rayPath.PathPositions.Count > 0)
            {
                // Set the number of positions for the LineRenderer
                lineRenderer.positionCount = rayPath.PathPositions.Count;

                // Set the positions for the LineRenderer
                lineRenderer.SetPositions(rayPath.PathPositions.ToArray());
            }
        }
    }

    // add a method MarkPathLine_MultiPaths() to draw green lines with multiple LineRenderers from loadedRaysPath
    void MarkPathLine_MultiPaths()
    {
        // Iterate through each path in the loaded data
        foreach (RayPathSet rayPath in loadedRaysPath)
        {
            // Create a new GameObject for each pathLine
            GameObject pathObject = new GameObject("PathLine_" + rayPath.RxNum);
            LineRenderer lineRenderer = pathObject.AddComponent<LineRenderer>();

            // Set the LineRenderer properties
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.01f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.green;
            lineRenderer.endColor = Color.green;
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




    // Public variable to specify the CSV filename in the Inspector
    private string csvFileName = "ray_path_data_Test_4cols.csv";

    void GetData()
    {
        //--------------------------------------------------
        ReadDataFromCode(); // Test data from code for quick change


        //-----------------------------------------------
        // Read data from the specified CSV file
        //----------------------------------------------
        // e.g., Assets/Data/ray_path_data.csv,
        string filePath = Path.Combine(Application.dataPath, "Data", csvFileName);
        Debug.Log("CSV FilePath: " + filePath); // Debugging line to check the file path

        if (!File.Exists(filePath))
        {
            Debug.LogError("CSV file not found at path: " + filePath);
            return;
        }
        //ReadDataFromCSVFile(filePath);

        //-----------------------------------------------
        // Now, let's demonstrate accessing the loaded data
        DisplayLoadedData();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetData();

        MarkPathPositions();

        //MarkPathLine_SinglePath();
        MarkPathLine_MultiPaths();
        MarkViaLines_DEBUG();

        // Initialize particle system
        InitializeParticles();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateParticles();
    }

    //  updates the particles position from csvRaysData  to move particles from one position to another 

    private void UpdateParticles_T1()
    {
        // Get the current time
        float elapsedTime = Time.time;

        // Loop through each particle
        for (int i = 0; i < particles.Length; i++)
        {
            // Get the current particle
            ParticleSystem.Particle particle = particles[i];

            // Get the path positions for this particle
            RayPathSet rayPath = loadedRaysPath[i];
            List<Vector3> pathPositions = rayPath.PathPositions;

            // pathPositions can have more than 2 positions, first check if there are more than 2 positions, if so move this particle from 1st postion to 2nd position and to 3rd position and so on. Use MoveAtConstantSpeed() and keep moving until the last position
            if (pathPositions.Count > 1)
            {
                // Move the particle between the first two positions at a constant speed
                Vector3 newPos = MoveAtConstantSpeed(pathPositions[0], pathPositions[1], 1.0f, elapsedTime - startTime[i]);
                particle.position = newPos;

                // Check if the particle has reached the second position
                if (Vector3.Distance(particle.position, pathPositions[1]) < 0.1f)
                {
                    // Move to the next position in the path
                    if (pathPositions.Count > 2)
                    {
                        pathPositions.RemoveAt(0); // Remove the first position
                    }
                    else
                    {
                        // Reset to the first position if only two positions are left
                        pathPositions[0] = pathPositions[1];
                        pathPositions.RemoveAt(1);
                    }
                }
            }




            // Update the particle in the system
            particles[i] = particle;
        }
        particleSys.SetParticles(particles, particles.Length);
    }

    // ray moves first two positions but after that it just jumps to next position until the last position
    private void UpdateParticles_v2()
    {
        //// Get the current time
        //float elapsedTime = Time.time;

        // Loop through each particle
        for (int i = 0; i < particles.Length; i++)
        {
            // Get the current time of this particle
            float elapsedTime = Time.time - startTime[i];

            // Get the current particle
            ParticleSystem.Particle particle = particles[i];

            // Get the path positions for this particle
            RayPathSet rayPath = loadedRaysPath[i];
            List<Vector3> pathPositions = rayPath.PathPositions;
            Vector3 startPos = pathPositions[0];
            Vector3 nextPos = pathPositions[1];

            // pathPositions can have more than 2 positions, first check if there are more than 2 positions, if so move this particle from 1st postion to 2nd position and to 3rd position and so on. Use MoveAtConstantSpeed() and keep moving until the last position
            if (pathPositions.Count > 1)
            {
                // Move the particle between the first two positions at a constant speed
                Vector3 newPos = MoveAtConstantSpeed(startPos, nextPos, 1.0f, elapsedTime - startTime[i]);
                particle.position = newPos;

                // Check if the particle has reached the second position
                if (Vector3.Distance(particle.position, nextPos) < 0.1f)
                {
                    // Move to the next position in the path
                    if (pathPositions.Count > 2)
                    {
                        pathPositions.RemoveAt(0); // Remove the first position

                        // reset the elapsed time for this particle
                        startTime[i] = Time.time;

                    }
                    else
                    {
                        // Reset to the first position if only two positions are left
                        //pathPositions[0] = pathPositions[1];
                        //pathPositions.RemoveAt(1);
                        startPos = nextPos;
                    }
                }
            }




            // Update the particle in the system
            particles[i] = particle;
        }
        particleSys.SetParticles(particles, particles.Length);
    }


    // ray moves first two positions but after that it just jumps to next position until the last position
    private void UpdateParticles_v3()
    {

        // Get the current time of this particle
        //float elapsedTime = Time.time - startTime[0];

        // Loop through each particle
        for (int i = 0; i < particles.Length; i++)
        {

            // Get the current time of this particle
            float elapsedTime = Time.time - startTime[i];

            // Get the current particle
            ParticleSystem.Particle particle = particles[i];

            // Get the path positions for this particle
            RayPathSet rayPath = loadedRaysPath[i];
            List<Vector3> pathPositions = rayPath.PathPositions;
            Vector3 startPos = pathPositions[rayPath.PathPositionsIdx];
            Vector3 nextPos = pathPositions[rayPath.PathPositionsIdx+1];

            // pathPositions can have more than 2 positions, first check if there are more than 2 positions, if so move this particle from 1st postion to 2nd position and to 3rd position and so on. Use MoveAtConstantSpeed() and keep moving until the last position
            if (pathPositions.Count > 1)
            {
                // display elapsedTime and startTime[i]
                Debug.Log($"DBG: elapsedTime: {elapsedTime}, startTime[i]: {startTime[i]}");

                // Move the particle between the first two positions at a constant speed
                Vector3 newPos = MoveAtConstantSpeed(startPos, nextPos, 1f, elapsedTime - startTime[i]);
                particle.position = newPos;

                // Check if the particle has reached the second position
                if (Vector3.Distance(particle.position, nextPos) < 0.1f)
                {
                    // display rayPath.PathPositionsIdx
                    Debug.Log($"RayPath.PathPositionsIdx: {rayPath.PathPositionsIdx}");
                    // Move to the next position in the path
                    if (pathPositions.Count > rayPath.PathPositionsIdx+2)
                    {
                        //pathPositions.RemoveAt(0); // Remove the first position
                        rayPath.PathPositionsIdx++;

                        // reset the elapsed time for this particle
                        startTime[i] = Time.time;
                    }
                    else
                    {
                        // Reset to the first position if only two positions are left
                        startPos = nextPos;
                    }
                }
            }



            // Update the particle in the system
            particles[i] = particle;
        }
        particleSys.SetParticles(particles, particles.Length);
    }

    // ray moves first two positions but after that it just jumps to next position until the last position - OK
    private void UpdateParticles()
    {
        // Loop through each particle
        for (int i = 0; i < particles.Length; i++)
        {
            // Get the current particle
            ParticleSystem.Particle particle = particles[i];

            // Get the path data for this particle
            RayPathSet rayPath = loadedRaysPath[i]; 
            List<Vector3> pathPositions = rayPath.PathPositions;

            // Check if the path exists and has at least two positions, and if the particle is not yet at the last position
            if (pathPositions != null && pathPositions.Count > 1 && rayPath.PathPositionsIdx < pathPositions.Count - 1)
            {
                // Get the start and end positions for the current segment
                Vector3 currentSegmentStart = pathPositions[rayPath.PathPositionsIdx];
                Vector3 currentSegmentEnd = pathPositions[rayPath.PathPositionsIdx + 1];

                // Calculate time elapsed since the start of the current segment
                float timeSinceSegmentStart = Time.time - startTime[i];

                // Assuming constant speed of 1f as in your original code
                float speed = 1f;

                // Calculate the distance and duration for the current segment
                float currentSegmentDistance = Vector3.Distance(currentSegmentStart, currentSegmentEnd);

                // Handle zero distance segments to avoid division by zero. Treat as instant transition.
                float currentSegmentDuration = (speed <= 0 || currentSegmentDistance < Mathf.Epsilon) ? 0f : currentSegmentDistance / speed;

                // Determine the interpolation factor (t) for the current segment
                // This value goes from 0 to 1 over the duration of the segment
                float segment_t = (currentSegmentDuration > 0) ? Mathf.Clamp01(timeSinceSegmentStart / currentSegmentDuration) : 1f; // If duration is 0, snap instantly

                // Calculate the particle's position using Lerp for the current segment
                Vector3 newPos = Vector3.Lerp(currentSegmentStart, currentSegmentEnd, segment_t);

                // Update the particle's position
                particle.position = newPos;

                // Check if the particle has completed the current segment (or should instantly move)
                // This condition is met when segment_t reaches or exceeds 1.0
                if (segment_t >= 1.0f)
                {
                     // The particle has reached the end of the current segment.
                     // Snap the particle exactly to the end position to ensure accuracy at segment boundaries.
                     particle.position = currentSegmentEnd;

                     // Move to the next point in the path
                     rayPath.PathPositionsIdx++;

                     // If there are more segments to move along, reset the timer for the new segment.
                     // The particle is still moving if the new index is not the last point.
                     if (rayPath.PathPositionsIdx < pathPositions.Count - 1)
                     {
                         startTime[i] = Time.time; // Reset timer so the next segment starts timing from now
                         // Debug.Log($"Particle {i} completed segment. Moving to segment starting at index {rayPath.PathPositionsIdx}. Resetting timer."); // Optional Debugging
                     }
                     else
                     {
                         // Particle has reached the last point in the path, movement is finished for this particle
                         // Debug.Log($"Particle {i} reached the end of the path at index {rayPath.PathPositionsIdx}."); // Optional Debugging
                         // The particle will now stay at this last position in subsequent frames because the outer if condition will be false.
                         // TODO: add logic here to despawn the particle as its movement is complete.
                     }
                }
            }
            else if (pathPositions != null && pathPositions.Count > 0)
            {
                // If the particle was at the last point or the path had only one point,
                // ensure its position is set to the final position in case it wasn't exactly there before.
                // This handles the state after the particle finishes the last segment.
                 particle.position = pathPositions[pathPositions.Count - 1];
                 // Particle movement is considered finished for this path.
                 // Add despawn/disable logic here if needed.
            }
            // If pathPositions is null or empty, the particle position won't be updated, which is appropriate.


            // Update the particle in the system array
            particles[i] = particle;
        }

        // Apply the updated particles back to the particle system
        // Only call SetParticles if there are actual particles to update
        if (particles.Length > 0)
        {
            particleSys.SetParticles(particles, particles.Length);
        }
    }


    // get moving position between two points in the same speed no matter the distance
    private Vector3 MoveAtConstantSpeed(Vector3 pos1, Vector3 pos2, float speed, float elapsedTime)
    {
        float distance = Vector3.Distance(pos1, pos2);
        float duration = (speed <= 0) ? float.MaxValue : distance / speed;
        float t = Mathf.Clamp01(elapsedTime / duration);
        return Vector3.Lerp(pos1, pos2, t);
    }

    // Method to use (display) the data loaded for testing
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
            Debug.Log($"Path Positions Index: {rayPath.PathPositionsIdx}");

            if (rayPath.PathPositions.Count > 0)
            {
                Debug.Log("Ray Path Positions:");
                for (int i = 0; i < rayPath.PathPositions.Count; i++)
                {
                    Debug.Log($"- Position {i}: {rayPath.PathPositions[i]}");
                }
            }
        }

        Debug.Log("--- End of Display ---");
    }
}
