/* 
 *  This is from the MoveAsParticleTest1.cs file.
 *  However, this uses the "RayPathSet_v2.cs" which handles 5 colums CSV. 
 */


using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.ParticleSystem;

public class MoveAsParticleTest1_v2 : MonoBehaviour
{
    [SerializeField, Tooltip("Particle System to show rays move on paths")]
    private ParticleSystem particleSystem1;
    [SerializeField, Tooltip("Particle System to show all rays intersection marks")]
    private ParticleSystem particleSystem2;
    [SerializeField, Tooltip("Particle System to show rays intersection marks on pass")]
    private ParticleSystem particleSystem3;

    //demo CSVs
    [SerializeField, Tooltip("a CSV file name without .csv portion to read data from")]
    private string csvFile_Demo = "ray_path_data_Test_5cols";

    //CSVs
    [SerializeField, Tooltip("a CSV file name without .csv portion to read data from")]
    private string csvFile_T12_1 = "ray_path_data_Test_5cols";
    [SerializeField, Tooltip("a CSV file name without .csv portion to read data from")]
    private string csvFile_T12_2 = "ray_path_data_Test_5cols";
    [SerializeField, Tooltip("a CSV file name without .csv portion to read data from")]
    private string csvFile_T3_1a = "ray_path_data_Test_5cols";
    [SerializeField, Tooltip("a CSV file name without .csv portion to read data from")]
    private string csvFile_T3_1b = "ray_path_data_Test_5cols";
    [SerializeField, Tooltip("a CSV file name without .csv portion to read data from")]
    private string csvFile_T3_1c = "ray_path_data_Test_5cols";
    [SerializeField, Tooltip("a CSV file name without .csv portion to read data from")]
    private string csvFile_T3_2a = "ray_path_data_Test_5cols";
    [SerializeField, Tooltip("a CSV file name without .csv portion to read data from")]
    private string csvFile_T3_2b = "ray_path_data_Test_5cols";
    [SerializeField, Tooltip("a CSV file name without .csv portion to read data from")]
    private string csvFile_T3_2c = "ray_path_data_Test_5cols";
    [SerializeField, Tooltip("a CSV file name without .csv portion to read data from")]
    private string csvFile_T4_base = "ray_path_data_Test_5cols";
    [SerializeField, Tooltip("a CSV file name without .csv portion to read data from")]
    private string csvFile_T4_metal = "ray_path_data_Test_5cols";


    //Q and A text boxes + buttons
    [SerializeField] private GameObject qTextObj;
    [SerializeField] private GameObject a1TextObj;
    [SerializeField] private GameObject a2TextObj;
    [SerializeField] private GameObject a3TextObj;
    [SerializeField] private GameObject a4TextObj;

    [SerializeField] private GameObject AnswerButtons;
    [SerializeField] private GameObject NextButton;

    [SerializeField] private GameObject T3buttons;
    [SerializeField] private GameObject T4buttons;

    //WiViz on/off text boxes
    [SerializeField] private GameObject wiVizOnTextObj;
    [SerializeField] private GameObject wiVizOffTextObj;

    //objects for case 1 and 2
    [SerializeField] private GameObject T1obj1;
    [SerializeField] private GameObject T1obj2;
    [SerializeField] private GameObject T1obj3;
    [SerializeField] private GameObject T1obj4;
    [SerializeField] private GameObject T2obj1;
    [SerializeField] private GameObject T2obj2;
    [SerializeField] private GameObject T3obj1;
    [SerializeField] private GameObject T3obj2;
    [SerializeField] private GameObject T3obj3;

    [SerializeField] private GameObject RxAreaObj1;
    [SerializeField] private GameObject RxAreaObj2;

    //obj materials
    [SerializeField] private Material mat_obj1;
    [SerializeField] private Material mat_obj2;
    [SerializeField] private Material mat_obj3;
    [SerializeField] private Material mat_obj4;

    [SerializeField] private Material mat_obj1_ACTIVE;
    [SerializeField] private Material mat_obj2_ACTIVE;
    [SerializeField] private Material mat_obj3_ACTIVE;

    [SerializeField] private Material mat_objNeutral;
    [SerializeField] private Material mat_objDisabled;

    [SerializeField] private Material mat_heatmap;
    [SerializeField] private Material mat_highlight;
    [SerializeField] private UnityEngine.UI.Image highlightCirclePrefab;
    [SerializeField] private Sprite highlightCircleSprite;

    //case button objs (to be deleted after selection)
    [SerializeField] private GameObject case1button;
    [SerializeField] private GameObject case2button;


    //output string and text box
    private string answerLog = "Log: ";
    [SerializeField] private GameObject outputTextObj;



    [SerializeField] private GameObject TxObj; // The object to instantiate for Transmiter
    [SerializeField] private GameObject RxObj; // The object to instantiate for Receiver
    [SerializeField, Tooltip("a GameObject to mark the path")]
    private GameObject objToMark; // The intersection mark to instantiate
    [SerializeField] private float RaySpeed;
    [SerializeField] private float raySize; // Size of ray (ex: 0.04f)
    [Header("TOOL")]
    [SerializeField] private bool showAllMarksAtOnce_DBG;
    private float rayAllMarkSize; // size of all-mark intersection
    private float rayLiveMarkSize; // size of live-mark intersection

    private int caseState = 0; //0 = demo, 1-4 = different versions of study
    private int taskState = 0; //tasks 1-8, 9 is completion, 0 is before first question

    private ParticleSystem.Particle[] particles; // each particle is a struct

    private int numRays; // Number of rays
    private int numFields = 5; // Number of fields in the data file
    private float[] startTime; // Start time for each particle
    private bool isAllMovingRaysVisible = true; // Flag to check if all rays are visible


    // A list to hold all the loaded data from the CSV
    private List<RayPathSet_v2> loadedRaysPath = new List<RayPathSet_v2>();
    private List<RayPathSet_v2> loadedHeatmapPath = new List<RayPathSet_v2>();

    // = Color Control ======================================================== BEGIN
    // Add a ColorHelper component to your scene and assign it in the Inspector. 
    [SerializeField] private ColorHelper colorHelper;

    // Constants for power interpolation
    // each ray's power value is updated toward its min power val in UpdateParticles()
    private const float POWER_MAX_dBm = 20.0f;
    private const float Rx_POWER_MAX_dBm = -75.0f;
    private float LowestPowerValRays_dBm; //  lowest power value among all rays from data file. calculate once at begin.
    private float LowestPowerValRx_dBm = -90f;
    private const float TEST_LOWEST_POWER_VAL_dBm = -90f; // 129.7 , 96.8 for testing, set to a constant value

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

    // All rays intersection marks at once =================================
    private int totalNumOfIntersectionMarks = 0; // Total number of path positions to mark
    private bool isMarkAllIntersectionPositions_partsys2_AlreadyCalled = false; // Flag to check if path positions are already initialized
    private bool toggleAllMarksAtOnce; // Using this variable instead of showAllMark

    // on pass rays intersection marks in live ================================
    private bool isShowIntersectionMarksOnPass = false; // Flag to show intersection marks on pass                                                                      // Create array to hold pre-allocated particles for particleSystem3
    private ParticleSystem.Particle[] intersectionMarksParticlesOnPass;
    private int currentMarkIdxOnPass = 0; // Keep track of the current index for adding marks on pass
    private bool toggleOnOffLiveMarks = false;  // toggle on/off all live marks 

    private GameObject RxObjGrp; // Group to hold all Rx objects
    private GameObject TxObjGrp;

    // Init totalNumOfIntersectionMarks by counting all intersection points except start and end for each path
    private void InitTotalNumOfIntersectionMarks()
    {
        // Reset totalNumOfIntersectionMarks
        totalNumOfIntersectionMarks = 0;
        // Count the total number of points to mark (all waypoints except start and end for each path)
        foreach (RayPathSet_v2 rayPath in loadedRaysPath)
        {
            if (rayPath.PathPositions.Count > 2) // Only if there are waypoints between start and end
            {
                totalNumOfIntersectionMarks += rayPath.PathPositions.Count - 2; // Exclude first and last positions
            }
        }
        Debug.Log($"Total path positions to mark: {totalNumOfIntersectionMarks}");
    }


    // Init ParticleSystem1 to show moving rays on paths
    private void InitializeParticles1()
    {
        // Use the serialized particleSystem1 field instead of finding a component
        if (this.particleSystem1 == null)
        {
            Debug.LogError("ParticleSystem component is missing. Please assign a Particle System in the Inspector.");
            return;
        }

        var partSysMain = particleSystem1.main;
        partSysMain.startSize = 0;

        // set numRay value by counting the number list of csvRaysData
        numRays = loadedRaysPath.Count;

        //display numRays
        Debug.Log("DBG: Number of rays: " + numRays);

        // Check if there are any rays loaded
        if (numRays <= 0)
        {
            Debug.LogWarning("No rays loaded from the CSV data.");
            return;
        }

        // Data should be available by now
        // Initialize particles array
        this.particles = new ParticleSystem.Particle[this.numRays];

        // Configure particle system
        partSysMain.maxParticles = this.numRays;
        this.particleSystem1.Emit(this.numRays);
        this.particleSystem1.GetParticles(this.particles);

        this.startTime = new float[this.numRays];
        this.rayPathSegmentProgressTimeOnPause = new float[this.numRays]; // allocate for all rays

        particleSystem1.Clear(); // Clear any existing particles

        // pause the particle systems, so no internal emission simulation happens
        // if this is not done, entire particles can be moved byiself by emission of the Particle System
        particleSystem1.Pause();

        // Set initial positions for particles
        InitializeParticlePositions();

    }


    // Init is for ParticleSystem2 to show all intersection marks at once
    private void InitializeParticles2()
    {
        // Check if particleSystem2 is assigned
        if (particleSystem2 == null)
        {
            Debug.LogError("ParticleSystem2 is not assigned! Cannot mark path positions with particles.");
            return;
        }
        Debug.Log("DBG: Initializing ParticleSystem2 for path markers.");

        if (totalNumOfIntersectionMarks == 0)
        {
            Debug.Log("No intermediate path positions to mark with particles.");
            return;
        }

        rayAllMarkSize = raySize * 0.7f; // Slightly smaller than ray particles

        // Configure particle system for all marks
        var partSysMain2 = particleSystem2.main;
        partSysMain2.maxParticles = totalNumOfIntersectionMarks;
        partSysMain2.startLifetime = float.MaxValue; // Set long lifetime so particles stay visible
        partSysMain2.startSize = rayAllMarkSize;
        partSysMain2.startColor = Color.yellow; // Use yellow color for path markers
    }

    // Init is for ParticleSystem3 to show intersection marks on pass of each ray  (live-marks)
    private void InitializeParticles3()
    {
        // Check if particleSystem3 is assigned
        if (particleSystem3 == null)
        {
            Debug.LogError("ParticleSystem3 is not assigned! Cannot mark path positions with particles.");
            return;
        }
        Debug.Log("DBG: Initializing ParticleSystem3 for path markers.");


        if (totalNumOfIntersectionMarks == 0)
        {
            Debug.Log("No intermediate path positions to mark with particles.");
            return;
        }

        rayLiveMarkSize = raySize * 0.8f; // Slightly smaller than ray particles, but larger than all-marks


        // Configure particle system for marking
        var partSysMain3 = particleSystem3.main;
        partSysMain3.maxParticles = totalNumOfIntersectionMarks;
        partSysMain3.startLifetime = float.MaxValue; // Set long lifetime so particles stay visible
        partSysMain3.startColor = Color.green; // Use yellow color for path markers

        // Pre-allocate the array for all possible intersection marks
        intersectionMarksParticlesOnPass = new ParticleSystem.Particle[totalNumOfIntersectionMarks];

        // Initialize all particles (but make them invisible initially)
        for (int i = 0; i < totalNumOfIntersectionMarks; i++)
        {
            intersectionMarksParticlesOnPass[i] = new ParticleSystem.Particle();
            intersectionMarksParticlesOnPass[i].position = Vector3.zero;
            intersectionMarksParticlesOnPass[i].startSize = 0f; // Initially invisible
            intersectionMarksParticlesOnPass[i].remainingLifetime = 0f; // Initially not alive
        }

        // Reset the current index to 0
        currentMarkIdxOnPass = 0;

        // Apply the initialized particles to particleSystem3
        particleSystem3.SetParticles(intersectionMarksParticlesOnPass, totalNumOfIntersectionMarks);

        Debug.Log($"Initialized particleSystem3 with {totalNumOfIntersectionMarks} pre-allocated particles.");
    }


    // Code for rendering message transmission and reception on top of the wave
    private string message;
    private float message_fontsize;
    private float changeAlpha;
    private GameObject[] particle_text_objs;
    bool[] completed_particles;

    public void SetMessage(string message, float message_fontsize = 1f, float changeAlpha = 0.05f)
    {
        // We're going to assume that none of the examples will ever have the message change midway through rays bouncing
        // Or, honestly I don't see why we would need the user to control the messages at all.
        this.message_fontsize = message_fontsize;
        this.changeAlpha = changeAlpha;
        this.message = message;
    }

    private GameObject MakeMessageParticleText(ParticleSystem.Particle particle, int idx)
    {
        GameObject textObject =
                    new GameObject($"ParticleMessage_{idx}");

        textObject.AddComponent<FaceCamera>(); // Rotate to face camera.

        textObject.transform.position =
            particle.position;

        TextMeshPro text =
            textObject.AddComponent<TextMeshPro>();


        text.text = message;
        text.fontSize = message_fontsize;
        text.alignment = TextAlignmentOptions.Center;

        return textObject;
    }

    private void clearMessageVisuals()
    {
        if (particles == null) return;

        // Clear completed particles
        completed_particles = new bool[particles.Length];
        // clear all receiver text
        for (int i = 0; i < particles.Length; i++)
        {
            if (path_idx_to_rx_obj[i] == null)
                continue;

            /// What if we specifically go to the MessageDisplay
            // Destroy text on receiver
            TextMeshPro[] texts =
                path_idx_to_rx_obj[i].GetComponentsInChildren<TextMeshPro>(); // Texts is empty??? Should at least be filled the first time.
            foreach (TextMeshPro text in texts)
            {
                Destroy(text.gameObject);
            }

            // Destroy text on particles
            Destroy(particle_text_objs[i]);
        }

        // Note this goes through the loop, but it doesn't destroy any text thats midair or on the receiver.

        particle_text_objs = new GameObject[particles.Length];
    }

    private void addMessageToRx(string message, GameObject rx)
    {
        Transform messageDisplay =
            rx.transform.Find("MessageDisplay");

        if (messageDisplay == null)
        {
            Debug.LogWarning(
                "Could not find MessageDisplay on " + rx.name
            );
            return;
        }

        // ------------------------------------------------------------
        // Look for an existing message
        // ------------------------------------------------------------

        TextMeshPro[] existingTexts =
            messageDisplay.GetComponentsInChildren<TextMeshPro>();

        foreach (TextMeshPro existingText in existingTexts)
        {
            if (existingText.text == message)
            {
                Color color = existingText.color;

                float oldAlpha = color.a;
                float newAlpha =
                    Mathf.Min(oldAlpha + changeAlpha, 1.0f);

                color.a = newAlpha;
                existingText.color = color;

                return;
            }
        }

        // ------------------------------------------------------------
        // Message doesn't exist - create it
        // ------------------------------------------------------------

        GameObject textObject =
            new GameObject("MessageText");

        textObject.transform.SetParent(
            messageDisplay,
            false
        );

        // Add the same camera-facing script
        textObject.AddComponent<FaceCamera>();

        // Add TextMeshPro
        TextMeshPro text =
            textObject.AddComponent<TextMeshPro>();

        text.text = message;

        // SAME SIZE AS PARTICLE MESSAGE
        text.fontSize = message_fontsize;

        text.alignment =
            TextAlignmentOptions.Center;

        // Initial opacity
        Color textColor = Color.white;
        textColor.a = changeAlpha;
        text.color = textColor;

        // Position at the center of MessageDisplay
        textObject.transform.localPosition =
            Vector3.zero;

        Debug.Log(
            $"Created message '{message}' with opacity {text.color.a}"
        );
    }




    // sets the initial positions for each particle of ParticleSystem1

    private void InitializeParticlePositions()
    {
        particle_text_objs = new GameObject[this.numRays];

        // Loop through each particle
        for (int i = 0; i < this.numRays; i++)
        {
            // Get the current particle
            ParticleSystem.Particle particle = this.particles[i];

            // Get the path positions for this particle
            RayPathSet_v2 rayPath = loadedRaysPath[i];
            List<Vector3> pathPositions = rayPath.PathPositions;

            // Check if there are enough path positions
            if (pathPositions.Count > 0)
            {
                // Init the initial position of the particle to the first position in the path
                particle.position = pathPositions[0];
                // Init this particle color to red
                particle.startColor = new Color(1, 0, 0, 1f); // Red color

                // Init particle size to raySize to make sure the particle is visible
                particle.startSize = this.raySize;
                // Init remaining lifetime 
                particle.remainingLifetime = float.MaxValue;

                this.startTime[i] = Time.time;
            }
            else
            {
                Debug.LogWarning($"No path positions available for particle {i}.");
            }

            // Update the particle in the system
            this.particles[i] = particle;
            this.particle_text_objs[i] = MakeMessageParticleText(particle, i);
        }

        // Set the updated particles back to the ParticleSystem
        this.particleSystem1.SetParticles(this.particles, this.numRays);
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
        LowestPowerValRays_dBm = GetLowestPowerVal_dBm();
        //LowestPowerValRx_dBm = GetLowestRxPower_dBm();
        //LowestPowerValRx_dBm = -90f;

        CheckColorVariables();
    }

    // add a method Convert_dBm_to_mW to convert dBm to mW
    private float Convert_dBm_to_mW(float dBm)
    {
        return Mathf.Pow(10, dBm / 10);
    }

    // add a method to convert mW to dBm
    private float Convert_mW_to_dBm(float mW)
    {
        if (mW <= 0)
            return float.NegativeInfinity; // Return negative infinity for non-positive values
        return 10 * Mathf.Log10(mW);
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

        float minPowerVal = loadedRaysPath[rayIndex].PowerNum; // dBm value of the ray
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

    private float GetLowestRxPower_dBm()
    {
        // Start with the first ray's power as minimum
        float lowestPower_AllRx = loadedRaysPath[0].TotalPowerNum;

        // Iterate through all rays to find the minimum power value
        foreach (var rayPath in loadedRaysPath)
        {
            if (rayPath.PowerNum < lowestPower_AllRx)
                lowestPower_AllRx = rayPath.TotalPowerNum;
        }

        // deispaly the lowest power value in the console
        Debug.Log($"DBG: Lowest power value among all Rx: {lowestPower_AllRx}");

        //return TEST_LOWEST_POWER_VAL_dBm;  // for Testing
        return lowestPower_AllRx;
    }

    // Convert power value to color index for the palette
    private int GetColorIndexFromPower_dBm(float powerVal_dBm, float powerMin_dBm)
    {
        // Use the cached lowest power value 
        float POWER_RANGE = POWER_MAX_dBm - LowestPowerValRays_dBm;

        // Calculate normalized position in power range (0 to 1)
        float normalizedPower = (powerVal_dBm - LowestPowerValRays_dBm) / POWER_RANGE;

        // Convert to color index (0-255)
        int colorIdx = Mathf.RoundToInt(normalizedPower * (COLOR_IDX_MAX - COLOR_IDX_MIN) + COLOR_IDX_MIN);

        // Ensure the result is within valid range
        return Mathf.Clamp(colorIdx, COLOR_IDX_MIN, COLOR_IDX_MAX);
    }

    private int GetColorIndexFromRx_dBm(float powerVal_dBm, float powerMin_dBm)
    {
        // Use the cached lowest power value 
        float POWER_RANGE = Rx_POWER_MAX_dBm - LowestPowerValRx_dBm;

        // Calculate normalized position in power range (0 to 1)
        float normalizedPower = (powerVal_dBm - LowestPowerValRx_dBm) / POWER_RANGE;

        // Convert to color index (0-255)
        int colorIdx = Mathf.RoundToInt(normalizedPower * (COLOR_IDX_MAX - COLOR_IDX_MIN) + COLOR_IDX_MIN);

        // Ensure the result is within valid range
        return Mathf.Clamp(colorIdx, COLOR_IDX_MIN, COLOR_IDX_MAX);
    }

    void TEST_GetColorIndexFromPower()
    {
        int colorIdx = 0;

        colorIdx = GetColorIndexFromPower_dBm(20, -10);
        Debug.Log($"Color Index for Power 20 and Min Power -10: {colorIdx}");

        colorIdx = GetColorIndexFromPower_dBm(10, -10);
        Debug.Log($"Color Index for Power 10 and Min Power -10: {colorIdx}");

        colorIdx = GetColorIndexFromPower_dBm(0, -10);
        Debug.Log($"Color Index for Power 0 and Min Power -10: {colorIdx}");

        colorIdx = GetColorIndexFromPower_dBm(-10, -10);
        Debug.Log($"Color Index for Power -10 and Min Power -10: {colorIdx}");
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

        try
        {
            // Load the file located in the resources folder
            string csvData = Resources.Load<TextAsset>(filename).text;

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

                    if (float.TryParse(fields[4].Trim(), out float totPowNum))
                    {
                        rayPathDat.TotalPowerNum = totPowNum;
                    }

                    // --- Parse Path Positions String ---
                    string positionsStringRaw = string.Join(",", fields, 5, fields.Length - 5).Trim();

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


    // Changed to separate the heatmap data from the rx data.
    void ReadDataFromCSVFile_Heatmap(string filename)
    {
        loadedHeatmapPath.Clear();

        try
        {
            // Load the file located in the resources folder
            string csvData = Resources.Load<TextAsset>(filename).text;

            // Split the CSV data into lines
            string[] lines = csvData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // To skip the header line, start processing from the second line (index 1) 
            for (int i = 1; i < lines.Length; i++)
            {
                LoadDataFromCSVLine_Heatmap(lines[i]); // Pass each data line to the processing method
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error reading CSV file: " + e.Message);
        }
    }

    void LoadDataFromCSVLine_Heatmap(string line)
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

                    if (float.TryParse(fields[4].Trim(), out float totPowNum))
                    {
                        rayPathDat.TotalPowerNum = totPowNum;
                    }

                    // --- Parse Path Positions String ---
                    string positionsStringRaw = string.Join(",", fields, 5, fields.Length - 5).Trim();

                    // Remove potential surrounding quotes
                    if (positionsStringRaw.StartsWith("\"") && positionsStringRaw.EndsWith("\""))
                    {
                        positionsStringRaw = positionsStringRaw.Substring(1, positionsStringRaw.Length - 2);
                    }

                    // Use the ParsePathPositionsString method from our data structure
                    rayPathDat.ParsePathPositionsString(positionsStringRaw);



                    // NOTE: THEONLY CHANGE FROM OTHER FUNCTIONS
                    loadedHeatmapPath.Add(rayPathDat);

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
    void MarkPathPositions_obj()
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

        int markerCount = 0;
        foreach (RayPathSet_v2 rayPath in loadedRaysPath)
        {
            if (rayPath.PathPositions.Count > 0)
            {
                // Iterate through positions of each path, skipping the first and last positions
                for (int i = 1; i < rayPath.PathPositions.Count - 1; i++)
                {
                    // Instantiate the object at the current position with no rotation (identity)
                    GameObject marker = Instantiate(objToMark, rayPath.PathPositions[i], Quaternion.identity);
                    marker.name = $"PathMarker_{markerCount++}";
                }
            }
        }

        Debug.Log($"Marked {markerCount} path positions with objToMark prefabs.");
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


        // Create parent group for all Rx objects if it doesn't exist
        if (TxObjGrp == null)
        {
            TxObjGrp = new GameObject("Tx_Objects_Group");
        }
        else
        {
            // Clear any existing children
            foreach (Transform child in TxObjGrp.transform)
            {
                Destroy(child.gameObject);
            }
        }
        // Get the position from the first path's first position
        RayPathSet_v2 rayPathFirst = loadedRaysPath[0];
        Vector3 startPosition = rayPathFirst.PathPositions[0];

        // Instantiate the TxObj at the start position
        GameObject startMark = Instantiate(TxObj, startPosition, Quaternion.identity, TxObjGrp.transform);

        // Name the marker for easy identification
        startMark.name = "Tx_Obj";

        // keep Transmitter Surface Type as Opaque in the Inspector
        if (startMark.GetComponent<Renderer>() != null)
        {
            startMark.GetComponent<Renderer>().material.SetFloat("_Surface", 0); // Uncomment if you want to set it programmatically
        }


        //Book keeping for highlighting
        this.tx_obj = startMark;
    }



    private GameObject[] path_idx_to_rx_obj;
    // Marks the end points of all ray paths with RxObj instances
    void MarkEndPoints_Rx()
    {

        path_idx_to_rx_obj = new GameObject[loadedRaysPath.Count()]; // Later used for displaying message above Rx as particles arrive.

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

        // Create parent group for all Rx objects if it doesn't exist
        if (RxObjGrp == null)
        {
            RxObjGrp = new GameObject("Rx_Objects_Group");
        }
        else
        {
            // Clear any existing children
            foreach (Transform child in RxObjGrp.transform)
            {
                Destroy(child.gameObject);
            }
        }

        // Dictionary to track unique end positions to avoid duplicate RxObj instances
        Dictionary<Vector3, GameObject> markedPositions = new Dictionary<Vector3, GameObject>();


        // Iterate through each path to mark its end point
        for (int i = 0; i < loadedRaysPath.Count; i++)
        {
            RayPathSet_v2 rayPath = loadedRaysPath[i];
            if (rayPath.PathPositions.Count > 0)
            {
                // Get the last position in the path (the end point)
                Vector3 endPosition = rayPath.PathPositions[rayPath.PathPositions.Count - 1];

                // Skip if we've already marked this position (avoid duplicates)
                if (markedPositions.ContainsKey(endPosition))
                {
                    path_idx_to_rx_obj[i] = markedPositions[endPosition];
                    continue;
                }

                // Instantiate the RxObj at the end position as a child of RxObjGrp
                GameObject endMarker = Instantiate(RxObj, endPosition, Quaternion.identity, RxObjGrp.transform);

                // Bookkeeping for highlighting
                this.rx_obj = endMarker;


                // Ed - Set color of recievers based on power
                MeshRenderer endMarkRend = endMarker.GetComponent<MeshRenderer>();
                int rxColorIdx = GetColorIndexFromRx_dBm(rayPath.TotalPowerNum, 0);
                Color rxColor = colorHelper.GetPaletteColor(rxColorIdx);
                rxColor.a = 0.6f;
                endMarkRend.material.SetColor("_BaseColor", rxColor);
                endMarkRend.material.SetColor("_EmissionColor", rxColor);

                // Name the marker for easy identification
                endMarker.name = $"Rx_Obj_{rayPath.RxNum}";

                // Mark this position as processed
                markedPositions[endPosition] = endMarker;
                path_idx_to_rx_obj[i] = endMarker;
            }
        }
        Debug.Log($"Marked {markedPositions.Count} unique end points with RxObj instances.");
    }

    void MarkEndPoints_Rx_Heatmap()
    {

        path_idx_to_rx_obj = new GameObject[loadedHeatmapPath.Count()]; // Later used for displaying message above Rx as particles arrive.

        // Check if RxObj is assigned
        if (RxObj == null)
        {
            Debug.LogError("RxObj is not assigned in the Inspector! Cannot mark end points.");
            return;
        }

        // Check if there are any paths loaded
        if (loadedHeatmapPath.Count <= 0)
        {
            Debug.LogWarning("No ray paths available to mark end points.");
            return;
        }

        // Create parent group for all Rx objects if it doesn't exist
        if (RxObjGrp == null)
        {
            RxObjGrp = new GameObject("Rx_Objects_Group");
        }
        else
        {
            // Clear any existing children
            foreach (Transform child in RxObjGrp.transform)
            {
                Destroy(child.gameObject);
            }
        }

        // Dictionary to track unique end positions to avoid duplicate RxObj instances
        Dictionary<Vector3, GameObject> markedPositions = new Dictionary<Vector3, GameObject>();


        // Iterate through each path to mark its end point
        for (int i = 0; i < loadedHeatmapPath.Count; i++)
        {
            RayPathSet_v2 rayPath = loadedHeatmapPath[i];
            if (rayPath.PathPositions.Count > 0)
            {
                // Get the last position in the path (the end point)
                Vector3 endPosition = rayPath.PathPositions[rayPath.PathPositions.Count - 1];
                Debug.Log("Spawning rx at " + endPosition);

                // Skip if we've already marked this position (avoid duplicates)
                if (markedPositions.ContainsKey(endPosition))
                {
                    path_idx_to_rx_obj[i] = markedPositions[endPosition];
                    continue;
                }

                // Instantiate the RxObj at the end position as a child of RxObjGrp
                GameObject endMarker = Instantiate(RxObj, endPosition, Quaternion.identity, RxObjGrp.transform);

                // Bookkeeping for highlighting
                this.rx_obj = endMarker;


                // Ed - Set color of recievers based on power
                MeshRenderer endMarkRend = endMarker.GetComponent<MeshRenderer>();
                int rxColorIdx = GetColorIndexFromRx_dBm(rayPath.TotalPowerNum, 0);
                Color rxColor = colorHelper.GetPaletteColor(rxColorIdx);
                rxColor.a = 0.6f;
                endMarkRend.material.SetColor("_BaseColor", rxColor);
                endMarkRend.material.SetColor("_EmissionColor", rxColor);

                // Name the marker for easy identification
                endMarker.name = $"Rx_Obj_{rayPath.RxNum}";

                // Mark this position as processed
                markedPositions[endPosition] = endMarker;
                path_idx_to_rx_obj[i] = endMarker;
            }
        }
        Debug.Log($"Marked {markedPositions.Count} unique end points with RxObj instances.");
    }

    // Represents end Rx power with a single heatmap material applied to a slab
    GameObject heatmap_obj;

    public void ToggleHeatmap()
    {
        if (heatmap_obj == null)
        {
            Debug.Log("Making heatmap");
            MakeHeatmap();
           // MarkEndPoints_Rx_Heatmap();
        }
        else
        {
            Debug.Log("Clearing heatmap");
            ClearHeatmap();
        }
    }

    void MakeHeatmap()
    {

        // Dictionary to track unique end positions to avoid duplicate RxObj instances
        Dictionary<Vector3, float> position_to_power = new Dictionary<Vector3, float>();
        float Y_level = 0;

        // Iterate through each path store its position to its RX power
        foreach (RayPathSet_v2 rayPath in loadedHeatmapPath)
        {
            if (rayPath.PathPositions.Count > 0)
            {
                // Get the last position in the path (the end point)
                Vector3 endPosition = rayPath.PathPositions[rayPath.PathPositions.Count - 1];
                Y_level = endPosition.y; // all Rx have same Z.


                  // Skip if we've already marked this position (avoid duplicates)
                  if (position_to_power.ContainsKey(endPosition))
                      continue;

                  // Mark this position as processed
                  position_to_power[endPosition] = rayPath.TotalPowerNum;
              }
          }

          // Compute the bounds of all Rx positions
          float minX = float.MaxValue;
          float maxX = float.MinValue;
          float minZ = float.MaxValue;
          float maxZ = float.MinValue;
          float padding = 0.1f;


          Dictionary<Vector3, Color> position_to_color = new Dictionary<Vector3, Color>();
          foreach (KeyValuePair<Vector3, float> kvp in position_to_power)
          {
              Vector3 pos = kvp.Key;

              minX = Mathf.Min(minX, pos.x);
              maxX = Mathf.Max(maxX, pos.x);

              minZ = Mathf.Min(minZ, pos.z);
              maxZ = Mathf.Max(maxZ, pos.z);

              float power = kvp.Value;
              int rxColorIdx = GetColorIndexFromRx_dBm(power, 0);
              Color rxColor = colorHelper.GetPaletteColor(rxColorIdx);
              rxColor.a = 0.6f;
              position_to_color[pos] = rxColor;
          }

          foreach (KeyValuePair<Vector3, Color> kvp in position_to_color)
          {
              Debug.Log($"Rx position: {kvp.Key}");
          }


          Vector3 center = new Vector3(
              (minX + maxX) * 0.5f,
              Y_level,
              (minZ + maxZ) * 0.5f
          );

          float width = maxX - minX + (2 * padding);
          float height = maxZ - minZ + (2 * padding);
          float depth = 0.1f; // along y direction

        // X is correct now, but height is not
          // Create the heatmap plane as a cube
          heatmap_obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
          heatmap_obj.name = "Heatmap";
          heatmap_obj.GetComponent<Collider>().enabled = false;

          Debug.Log("width" + width);
          Debug.Log("height" + height); //height is 0?
          Debug.Log("Y_level" + Y_level);

          // Position and size it
          heatmap_obj.transform.position = center;
          //heatmap.transform.localScale = new Vector3(width, height, depth);
          heatmap_obj.transform.localScale = new Vector3(width, depth, height);
          // TODO: Apply your heatmap material here.
          Material heatmapMaterial = mat_heatmap;
          heatmap_obj.GetComponent<MeshRenderer>().material = heatmapMaterial;

            HeatmapUpdater heatmapUpdater =
                heatmap_obj.AddComponent<HeatmapUpdater>();

            heatmapUpdater.material = heatmapMaterial;
            heatmapUpdater.points = position_to_color;

            heatmapUpdater.Upload();

    }

    public void ClearHeatmap()
    {
        if (heatmap_obj == null) return;
        Destroy(heatmap_obj);
        heatmap_obj = null;
    }

    // Hide all Rx endpoints
    public void HideAllEndPoints_Rx()
    {
        if (RxObjGrp != null)
        {
            RxObjGrp.SetActive(false);
            Debug.Log("make Rx endpoint markers invisible");
        }
    }

    // Show all Rx endpoints
    public void ShowAllEndPoints_Rx()
    {
        if (RxObjGrp != null)
        {
            RxObjGrp.SetActive(true);
            Debug.Log("make Rx endpoint markers visible");
        }
        else
        {
            Debug.LogWarning("No Rx endpoint markers exist yet. Call MarkEndPoints_Rx() first.");
        }
    }


    // Toggle visibility of all Rx endpoints
    public void ToggleAllEndPoints_Rx()
    {
        if (RxObjGrp != null)
        {
            // Toggle the visibility state
            bool isCurrentlyVisible = RxObjGrp.activeSelf;

            if (isCurrentlyVisible)
            {
                HideAllEndPoints_Rx();
            }
            else
            {
                ShowAllEndPoints_Rx();
            }
        }
        else
        {
            Debug.LogWarning("No Rx endpoint markers exist yet. Call MarkEndPoints_Rx() first.");
        }
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



    LineRenderer[] ray_objects;
    public void ToggleRays()
    {
        if (ray_objects == null)
        {
            MarkPathLine_MultiPaths();
        }
        else
        {
            ClearPathLine_MultiPaths();
        }
    }
    // Draw lines with multiple LineRenderers from loadedRaysPath
    void MarkPathLine_MultiPaths()
    {
        ray_objects = new LineRenderer[loadedRaysPath.Count];

        // Iterate through each path in the loaded data
        for (int i = 0; i < loadedRaysPath.Count; i++) {
            RayPathSet_v2 rayPath = loadedRaysPath[i];
            // Create a new GameObject for each pathLine
            GameObject pathObject = new GameObject("PathLine_" + rayPath.RxNum);
            LineRenderer lineRenderer = pathObject.AddComponent<LineRenderer>();

            // Set the LineRenderer properties
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.01f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = this.viz_color;
            lineRenderer.endColor = this.viz_color;
            lineRenderer.positionCount = 0; // Initialize with zero positions
            lineRenderer.useWorldSpace = true; // Use world space for the positions
            lineRenderer.numCapVertices = 3; // Set the number of cap vertices for smoother ends
            lineRenderer.numCornerVertices = 3; // Set the number of corner vertices for smoother corners


            // Set the number of positions for the LineRenderer
            lineRenderer.positionCount = rayPath.PathPositions.Count;

            // Set the positions for the LineRenderer
            lineRenderer.SetPositions(rayPath.PathPositions.ToArray());
            ray_objects[i] = lineRenderer;
        }
    }

    void ClearPathLine_MultiPaths()
    {
        if (ray_objects == null) return;
        // Iterate through each path in the loaded data
        for (int i = 0; i < ray_objects.Length; i++)
        {
            LineRenderer r = ray_objects[i];
            r.positionCount = 0;

        }
        ray_objects = null; // Clear the line Renderer
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


    // Show or hide all particles in a ParticleSystem by enabling/disabling its renderer
    private void ShowHideParticleSystem(ParticleSystem partSys, bool isVisible)
    {
        if (partSys == null)
            return;

        // Get the renderer component of the particle system
        var renderer = partSys.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Enable or disable the renderer to show/hide particles
            renderer.enabled = isVisible;
            Debug.Log($"Particle System {partSys.name} is now {(isVisible ? "visible" : "hidden")}");
        }
        else
        {
            Debug.LogWarning($"Could not find Renderer component on ParticleSystem {partSys.name}");
        }
    }

    //====================================================
    // Moving Rays
    public void ShowAllMovingRays()
    {
        isAllMovingRaysVisible = true;
        ShowHideParticleSystem(particleSystem1, isAllMovingRaysVisible); // Show rays movement particles
    }

    public void HideAllMovingRays()
    {
        isAllMovingRaysVisible = false;
        ShowHideParticleSystem(particleSystem1, isAllMovingRaysVisible); // Hide rays movement particles
    }

    // Toggle visibility of all moving rays
    // refered from a button click in the UI
    public void ToggleAllMovingRaysVisibility()
    {
        // Toggle the visibility state
        isAllMovingRaysVisible = !isAllMovingRaysVisible;

        // Show or hide the moving rays
        if (isAllMovingRaysVisible)
            ShowAllMovingRays();
        else
            HideAllMovingRays();
    }


    //====================================================
    // All Intersection Marks At Once

    // Mark all path rays intersection positions with particleSystem2
    void MarkAllIntersectionPositions_partsys2()
    {
        // Check if particleSystem2 is assigned
        if (particleSystem2 == null)
        {
            Debug.LogError("ParticleSystem2 is not assigned! Cannot mark path positions with particles.");
            return;
        }

        if (isMarkAllIntersectionPositions_partsys2_AlreadyCalled)
            return; // Prevent multiple calls to this method

        // total number of points to mark (all waypoints except start and end for each path)
        int totalMarkPoints = totalNumOfIntersectionMarks;

        // Create an array to hold our marker particles
        ParticleSystem.Particle[] totalMarkerParticles = new ParticleSystem.Particle[totalMarkPoints];

        // Prepare each marker particle
        int particleIndex = 0;
        foreach (RayPathSet_v2 rayPath in loadedRaysPath)
        {
            if (rayPath.PathPositions.Count > 0)
            {
                // Iterate through positions of each path, skipping the first and last positions
                for (int i = 1; i < rayPath.PathPositions.Count - 1; i++)
                {
                    if (particleIndex < totalMarkPoints)
                    {
                        ParticleSystem.Particle particle = new ParticleSystem.Particle();

                        // Set particle position to path position
                        particle.position = rayPath.PathPositions[i];

                        // Set particle properties
                        particle.startColor = Color.yellow;
                        particle.startSize = rayAllMarkSize;
                        particle.remainingLifetime = float.MaxValue;

                        // Add to our array
                        totalMarkerParticles[particleIndex] = particle;
                        particleIndex++;
                    }
                }
            }
        }

        // Clear any existing particles
        particleSystem2.Clear();

        // Emit the new marker particles
        particleSystem2.SetParticles(totalMarkerParticles, totalMarkerParticles.Length);

        isMarkAllIntersectionPositions_partsys2_AlreadyCalled = true; // Set flag to prevent multiple calls

        Debug.Log($"Marked {particleIndex} path positions with particles from particleSystem2.");
    }

    public void ShowAllIntersectionMarks()
    {
        MarkAllIntersectionPositions_partsys2(); // Ensure path markers are created
        ShowHideParticleSystem(particleSystem2, true); // Show path markers
    }

    // hide all rays' intersection marks on the path
    public void HideAllIntersectionMarks()
    {
        ShowHideParticleSystem(particleSystem2, false); // Hide path markers
    }

    // Toggle visibility of all intersection marks at once
    public void ToggleAllIntersectionMarks()
    {
        // Toggle the visibility state
        toggleAllMarksAtOnce = !toggleAllMarksAtOnce;

        // Show or hide the intersection marks based on the toggled state
        if (toggleAllMarksAtOnce)
        {
            ShowAllIntersectionMarks();
        }
        else
        {
            HideAllIntersectionMarks();
        }
    }


    //====================================================
    // Live marks at intersection points on pass
    private void AddIntersectionMarkOnPass(Vector3 position, Color color)
    {
        // Check if particleSystem3 is assigned and enabled
        if (particleSystem3 == null)
        {
            Debug.LogWarning("ParticleSystem3 is not assigned! Cannot add intersection marks.");
            return;
        }

        // Check if we've reached the pre-calced total number of marks
        if (currentMarkIdxOnPass >= totalNumOfIntersectionMarks)
        {
            Debug.LogError($"Maximum intersection marks should not be bigger than pre-calced total intersection marks ({totalNumOfIntersectionMarks}). Fix the issue.");
            return;
        }

        // Update the particle at the current index
        intersectionMarksParticlesOnPass[currentMarkIdxOnPass].position = position;
        /*intersectionMarksParticlesOnPass[currentMarkIdxOnPass].startColor = color;*/
        intersectionMarksParticlesOnPass[currentMarkIdxOnPass].startColor = viz_color;
        intersectionMarksParticlesOnPass[currentMarkIdxOnPass].startSize = rayLiveMarkSize;
        intersectionMarksParticlesOnPass[currentMarkIdxOnPass].remainingLifetime = float.MaxValue;

        // Increment the current index
        currentMarkIdxOnPass++;

        //Debug.Log($"Added intersection mark at position {position}. Current mark count: {currentMarkIdxOnPass}/{totalNumOfIntersectionMarks}");
    }

    // referred from a button click in the UI
    // Enable marking at intersection position as rays pass through, and show marks 
    public void EnableMarksOnPass()
    {
        isShowIntersectionMarksOnPass = true; // Ensure we are showing marks on pass
        ShowHideParticleSystem(particleSystem3, true); // Hide path markers

    }

    // referred from a button click in the UI
    // Disable marking at intersection position as rays pass through, and hide marks
    public void DisableMarksOnPass()
    {
        isShowIntersectionMarksOnPass = false; // Ensure we are hiding marks on pass
        ShowHideParticleSystem(particleSystem3, false); // Hide path markers
    }

    // toggle Enable/Disable rays' live marks
    public void ToggleLiveMarksVisibility()
    {
        // Toggle the boolean value
        toggleOnOffLiveMarks = !toggleOnOffLiveMarks;

        // Enable or disable live marks based on the toggled state
        if (toggleOnOffLiveMarks)
        {
            EnableMarksOnPass();
            Debug.Log("Live intersection marks are now visible");
        }
        else
        {
            DisableMarksOnPass();
            Debug.Log("Live intersection marks are now hidden");
        }
    }

    // reset intersection marks when needed (e.g., when toggling or restarting)
    public void ResetIntersectionMarksOnPass()
    {
        if (particleSystem3 == null || intersectionMarksParticlesOnPass == null)
            return;

        // Reset all particles to invisible
        for (int i = 0; i < totalNumOfIntersectionMarks; i++)
        {
            intersectionMarksParticlesOnPass[i].startSize = 0f;
            intersectionMarksParticlesOnPass[i].remainingLifetime = 0f;
        }

        // Reset the current index
        currentMarkIdxOnPass = 0;

        // Apply the changes
        particleSystem3.SetParticles(intersectionMarksParticlesOnPass, totalNumOfIntersectionMarks);

        Debug.Log("Cleared all intersection marks");
    }


    //=====================================================

    void GetData(string fileName, bool for_heatmap)
    {

        //-----------------------------------------
        // Test data from code for quick change
        //-----------------------------------------
        //ReadDataFromCode_Test1();


        //-----------------------------------------------
        // Read data from the specified CSV file
        //----------------------------------------------

        // check if csvFileName exist in Asset/Resources dir, csvFileName should not contain extension name
        if (CheckIfFileExistsInResources(fileName))
        {
            if (for_heatmap)
            {
                // Just call a different version of the functions that load into loadedHeatmapPath
                ReadDataFromCSVFile_Heatmap(fileName);
            }
            else
            {
                ReadDataFromCSVFile(fileName);
            }
            //DisplayLoadedData();
        }
        else
        {
            Debug.LogError($"CSV file '{fileName}' not found in Resources folder. Please check the file name and location.");
        }

    }

    private ObjectHighlighter highlighter;
    private DemoTaskManager demoTask;

    // bookkeeping.
    private GameObject tx_obj;
    private GameObject rx_obj;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetData(csvFile_Demo, false);
        GetData(csvFile_Demo +"_heatmap", true);

        GameObject highlighterObject =
            new GameObject("ObjectHighlighter");

                highlighter =
                    highlighterObject.AddComponent<ObjectHighlighter>();

        GameObject player = GameObject.Find("Player");

        if (player == null)
        {
            Debug.LogError("Player not found.");
            return;
        }

        Transform cameraTransform = player.transform.Find("Camera");

        if (cameraTransform == null)
        {
            Debug.LogError("Camera not found under Player.");
            return;
        }

        Camera playerCamera = cameraTransform.GetComponent<Camera>();

        if (playerCamera == null)
        {
            Debug.LogError("No Camera component found on Player/Camera.");
            return;
        }

        highlighter.Initialize(
                    playerCamera,
                    highlightCircleSprite
                );


        RxAreaObj1.SetActive(false);
        RxAreaObj2.SetActive(false);
        /*NextButton.SetActive(true);*/
        a1TextObj.SetActive(false);
        a2TextObj.SetActive(false);
        a3TextObj.SetActive(false);
        a4TextObj.SetActive(false);


        HideObjects_T1();
        T2obj1.GetComponent<MeshRenderer>().material = mat_objDisabled;
        HideObjects_T3();
        T3buttons.SetActive(false);
        T4buttons.SetActive(false);
        wiVizOffTextObj.SetActive(false);

        // Init color palette related things
        InitializeColorPalette();

/*        SetMessage("TEST");
        // SetCurrentDataset calls InitParticle functions, that need to make the message text, so message text must be defined beforehhand
        SetCurrentDataSet(csvFile_Demo);*/

        if (showAllMarksAtOnce_DBG)
        {
            ShowAllIntersectionMarks();
        }
        ToggleLiveMarksVisibility();


        demoTask = new DemoTaskManager(this);
        //MarkPathPositions_obj();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateParticles();
    }

    // toggle play/pause state of entire rays movement
    public void RayPlayPause()
    {
        // Toggle the pause state
        isRayMovementPaused = !isRayMovementPaused;


        Debug.Log("loadedRaysPath " + loadedRaysPath.Count);
        Debug.Log("particles " + particles.Length);

        // If pausing, store the current segment progress time for each particle
        if (isRayMovementPaused)
        {
            // pause the particle systems, so no internal emission simulation happens
            // if this is not done, entire particles can be moved byiself by emission of the Particle System
            particleSystem1.Pause();

            // allocate the array if needed
            if (rayPathSegmentProgressTimeOnPause == null || rayPathSegmentProgressTimeOnPause.Length != particles.Length)
            {
                rayPathSegmentProgressTimeOnPause = new float[particles.Length];
            }

            // Store the elapsed time for each segment
            float currentTime = Time.time;
            for (int i = 0; i < particles.Length; i++)
            {
                // save the progressed time since the start of the segment for each particle
                rayPathSegmentProgressTimeOnPause[i] = currentTime - startTime[i];
            }
            Debug.Log("Ray movement PAUSED");
        }
        // If resuming, adjust the start times based on stored progress
        else
        {


            // Check if all rays are at their starting positions (index 0)
            bool allRaysAtStart = true;
            foreach (var rayPath in loadedRaysPath)
            {
                if (rayPath.PathPositionsIdx > 0)
                {
                    allRaysAtStart = false;
                    break;
                }
            }

            // If Play after a restart, don't use saved progress times
            if (allRaysAtStart)
            {
                // Reset progress times and start fresh
                for (int i = 0; i < startTime.Length; i++)
                {
                    startTime[i] = Time.time;
                    if (rayPathSegmentProgressTimeOnPause != null && i < rayPathSegmentProgressTimeOnPause.Length)
                    {
                        rayPathSegmentProgressTimeOnPause[i] = 0f;
                    }
                }
            }
            // Normal pause/resume - apply stored progress times if available
            else if (rayPathSegmentProgressTimeOnPause != null && startTime != null)
            {
                // Normal resume - apply stored progress times
                float currentTime = Time.time;
                for (int i = 0; i < startTime.Length && i < rayPathSegmentProgressTimeOnPause.Length; i++)
                {
                    // restore the start time for each particle along with the progressed time of the segment
                    startTime[i] = currentTime - rayPathSegmentProgressTimeOnPause[i];
                }
            }

            //particleSystem1.Play();  // don't need now, but may need later to turn on an particle system's effect if needed

            Debug.Log("Ray movement PLAYING");
        }
    }

    // Reset all rays to their initial positions
    public void Restart()
    {
        Debug.Log("Restarting rays...");

        clearMessageVisuals();

        // Pause rays while we reset them
        bool wasPlaying = !isRayMovementPaused;
        if (wasPlaying)
        {
            isRayMovementPaused = true;
        }

        // Reset all path indices to beginning
        foreach (var rayPath in loadedRaysPath)
        {
            rayPath.PathPositionsIdx = 0;
        }

        // Reset distance tracking
        if (pathDistanceTraveled != null)
        {
            for (int i = 0; i < pathDistanceTraveled.Length; i++)
            {
                pathDistanceTraveled[i] = 0f;
            }
        }

        // Reuse the initialization code for particle positions
        InitializeParticlePositions();

        // Reset all timing information
        for (int i = 0; i < startTime.Length; i++)
        {
            startTime[i] = Time.time;
        }

        // Resume playback if it was previously playing
        if (wasPlaying)
        {
            isRayMovementPaused = false;
        }

        // add this to ensure particles are updated immediately after reset
        particleSystem1.Clear(); // Clear any existing moving particles
        //particleSystem1.Play();  // don't need now, but may need later to turn on an particle system's effect if needed


        Debug.Log("Rays reset to initial positions");
    }



    // ray moves first two positions but after that it just continue to next positions until the last position, and remove self - OK
    // ray's color changes based on its power value toward its minimum power value as moving along the path

    private void UpdateParticles()
    {
        // if rays move paused, don't update ray positions
        if (isRayMovementPaused)
            return;

        if (colorHelper == null)
        {
            Debug.LogWarning("ColorHelper reference is missing. Please add a ColorHelper component to your scene and assign it in the Inspector. Particle colors will not be updated.");
        }

        // Loop through each particle to update
        for (int i = 0; i < particles.Length; i++)
        {
            // Get the current particle
            ParticleSystem.Particle particle = particles[i];

            // Get the path data for this particle
            RayPathSet_v2 rayPath = loadedRaysPath[i];
            List<Vector3> pathPositions = rayPath.PathPositions;

            bool particleReachedEnd = false;

            // Check if the path exists and has at least two positions, and if the particle is not yet at the last position
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

                //----- position interpolate ----
                // Calculate the particle's position using Lerp for the current segment
                Vector3 newPos = Vector3.Lerp(currentSegmentStart, currentSegmentEnd, segment_t);

                // Update the particle's position
                particle.position = newPos;

                // ------------------------------------------------------------
                // Update the text object to follow the particle
                // ------------------------------------------------------------
                if (particle_text_objs != null &&
                    i < particle_text_objs.Length &&
                    particle_text_objs[i] != null)
                {
                    particle_text_objs[i].transform.position =
                        particle.position;
                }

                //== Ray Color ===================================== START
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
                float minPowerVal = rayPath.PowerNum; //dBm

                // Calculate color index based on power value
                int colorIdx = GetColorIndexFromPower_dBm(powerVal, minPowerVal);

                // Debug.Log($"Ray {i} - PathIdx: {rayPath.PathPositionsIdx}, Segment_t: {segment_t}, PowerVal: {powerVal_dBm}, MinPowerVal: {minPowerVal}, ColorIdx: {colorIdx}");

                // Apply color if ColorHelper is available
                Color rayColor = Color.white;
                if (colorHelper != null)
                {
                    // Get color from ColorHelper using ColorHelper
                    rayColor = colorHelper.GetPaletteColor(colorIdx);

                    // Apply color to the particle
                    //particle.startColor = rayColor;
                    //particle.startColor = Color.green; // TEST
                }

                //== Ray Color ======================================== END

                // Check if the particle has completed the current segment (or should instantly move)
                // This condition is met when segment_t reaches or exceeds 1.0
                if (segment_t >= 1.0f)
                {
                    // The particle has reached the end of the current segment.
                    // Snap the particle exactly to the end position to ensure accuracy at segment boundaries.
                    particle.position = currentSegmentEnd;

                    // Update the text object to the exact segment endpoint
                    if (particle_text_objs != null &&
                        i < particle_text_objs.Length &&
                        particle_text_objs[i] != null)
                    {
                        particle_text_objs[i].transform.position =
                            particle.position;
                    }

                    // Mark intersection points if enabled. (except the first and last points)
                    if (isShowIntersectionMarksOnPass && (rayPath.PathPositionsIdx >= 0 && rayPath.PathPositionsIdx < pathPositions.Count - 2))
                    {
                        // because using currentSegmentEnd position, should start from rayPath.PathPositionsIdx == 0 to include the 1st intersection position
                        AddIntersectionMarkOnPass(currentSegmentEnd, rayColor);
                    }

                    // Move to the next point in the path
                    rayPath.PathPositionsIdx++;

                    // If there are more segments to move along, reset the timer for the new segment.
                    // The particle is still moving if the new index is not the last point.
                    if (rayPath.PathPositionsIdx < pathPositions.Count - 1)
                    {
                        startTime[i] = Time.time; // Reset timer so the next segment starts timing from now
                    }
                    else
                    {
                        // Particle has reached the last point in the path
                        particleReachedEnd = true;
                    }
                }
            }
            else if (pathPositions != null && pathPositions.Count > 0)
            {
                // If the particle is already at the last position
                particle.position = pathPositions[pathPositions.Count - 1];

                particleReachedEnd = true;
            }

            // ------------------------------------------------------------
            // Keep text object synchronized with final particle position
            // ------------------------------------------------------------
            if (!particleReachedEnd &&
                particle_text_objs != null &&
                i < particle_text_objs.Length &&
                particle_text_objs[i] != null)
            {
                particle_text_objs[i].transform.position =
                    particle.position;
            }

            // Handle particles that have reached their end position
            if (particleReachedEnd)
            {
                // Make particle invisible after some time or right away if desired
                particle.startSize = 0f;
                particle.remainingLifetime = 0f;

                // Mark particle as completed so it will not be added to Rx in next iteration
                if (!completed_particles[i])
                {
                    addMessageToRx(message, path_idx_to_rx_obj[i]);
                }
                completed_particles[i] = true;

                // Clear the particle's text object once it reaches the end
                if (particle_text_objs != null &&
                    i < particle_text_objs.Length &&
                    particle_text_objs[i] != null)
                {
                    Destroy(particle_text_objs[i]);
                    particle_text_objs[i] = null;
                }
            }

            // Update the particle in the system array
            particles[i] = particle;
        }

        // Apply the updated moving ray particles at once to the particlSystem1
        // Only call SetParticles if there are actual particles to update
        if (particles.Length > 0)
        {
            particleSystem1.SetParticles(particles, particles.Length);

            // Apply the updated ray marks (on pass) at once to particleSystem3
            // Only update the particles up to currentMarkIdxOnPass
            if (isShowIntersectionMarksOnPass)
            {
                // 2nd parameter is numbder of particles to show, and currentMarkIdxOnPass starts from 0 so added 1
                particleSystem3.SetParticles(intersectionMarksParticlesOnPass, currentMarkIdxOnPass + 1);
            }
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

    // Ed - switch current dataset
    public Color32 viz_color = Color.red;

    void SetCurrentDataSet(string fileName)
    {

        ClearPathLine_MultiPaths();
        ClearHeatmap();

        GetData(fileName, false);
        GetData(fileName + "_heatmap", true);


        MarkStartPoint_Tx();
        MarkEndPoints_Rx();
        /*MakeHeatmap();*/
        //HideAllEndPoints_Rx(); // make Rx markers invisible initially

        //MarkPathLine_MultiPaths();
        //MarkViaLines_DEBUG();

        // Initialize particle system
        InitializeParticles1(); // For rays movement
        InitTotalNumOfIntersectionMarks();
        InitializeParticles2(); // For all intersection markers
        InitializeParticles3(); // For intersection markers on pass
        this.completed_particles = new bool[particles.Length]; // All false by default, particles should be defined by now

        // Initialize path distances for power calculations
        InitializePathDistances();

        for (int i = 0; i < this.particles.Length; i++) {
            this.particles[i].startColor = this.viz_color;
            this.particles[i].color = this.viz_color;
        }


    }

    public class TaskNode {
        public string taskName;

        public int pre_text_idx;
        public Dictionary<string, Action> pre_slides;

        public string question;
        public int correct_answer; // index corresponding to correct answer.
        public List<string> answers;
        public List<string> responses; // parallel arrays

        public int post_text_idx;
        public List<string> post_text;

        public TaskNode[] nextTasks; // all possible next tasks
        // logic for selecting next task depends on the selected answer
    }

    public void StartTest(int testVer)
    {
        Destroy(case1button);
        Destroy(case2button);

        answerLog += "C" + testVer.ToString() + ".";

        taskState = 0;
        caseState = testVer;
        NextTask();

        if (caseState == 1)
        {
            HideAllEndPoints_Rx();
            RxAreaObj1.SetActive(true);
            wiVizOffTextObj.SetActive(false);
            wiVizOnTextObj.SetActive(true);
        }
        else if (caseState == 2)
        {
            HideAllMovingRays();
            DisableMarksOnPass();
            wiVizOffTextObj.SetActive(true);
            wiVizOnTextObj.SetActive(false);
        }


    }

    public class DemoTaskManager
    {
        private enum State
        {
            D1, D2, D3, D4, D5, D6, D7, D8, D9, END
        }

        private State state, next_state;

        MoveAsParticleTest1_v2 m;

        public DemoTaskManager(MoveAsParticleTest1_v2 m)
        {
            this.m = m;
            state = State.D1;
            next_state = State.D1;

            m.SetMessage("Hello");
            // SetCurrentDataset calls InitParticle functions, that need to make the message text, so message text must be defined beforehhand
            m.SetCurrentDataSet(m.csvFile_Demo);
        }

        public void Advance()
        {
            state = next_state;
        }

        public void DoState()
        {
            switch (state)
            {
                case State.D1:
                    m.qTextObj.GetComponent<TextMeshProUGUI>().text = "How does your Smart TV stream videos from the internet?";
                    // Maybe add the SerializeField highlightable object in this class.
                    next_state = State.D2;
                    break;
                case State.D2:
                    m.qTextObj.GetComponent<TextMeshProUGUI>().text = "Your router has a cable that connects it to the internet. It has a transmitter (tx) that can send signals!";
                    Debug.Log(m);
                    Debug.Log(m.highlighter);
                    m.highlighter.SetHighlighted(m.tx_obj, true); // Can access these, even though they are private??
                    m.highlighter.SetHighlighted(m.rx_obj, false);
                    next_state = State.D3;
                    break;
                case State.D3:
                    m.qTextObj.GetComponent<TextMeshProUGUI>().text = "Your TV has a receiver (rx) that lets it hear signals!";
                    m.highlighter.SetHighlighted(m.tx_obj, false); 
                    m.highlighter.SetHighlighted(m.rx_obj, true);
                    next_state = State.D4;
                    break;
                case State.D4:
                    m.highlighter.SetHighlighted(m.tx_obj, true);
                    m.highlighter.SetHighlighted(m.rx_obj, true);
                    m.qTextObj.GetComponent<TextMeshProUGUI>().text = "Let's look at how the tx communicates with the rx. The router is going to say \"Hello\" to your TV!";
                    m.NextButton.SetActive(false);
                    // yield return new WaitForSeconds(2f);
                    m.SetMessage("Hello");
                    m.RayPlayPause();
                    m.NextButton.SetActive(true);
                    next_state = State.D5;
                    break;
                case State.D5:
                    m.highlighter.SetHighlighted(m.tx_obj, false);
                    m.highlighter.SetHighlighted(m.rx_obj, false);
                    m.qTextObj.GetComponent<TextMeshProUGUI>().text = "You can see that the signal contains the message, and it interacts with the entire room." +
                        "The signal moving through the environment like this is called propagation.";
                    m.NextButton.SetActive(false);
                    // yield return new WaitForSeconds(2f);
                    m.RayPlayPause();
                    m.NextButton.SetActive(true);
                    next_state = State.D6;
                    break;
                case State.D6:
                    m.qTextObj.GetComponent<TextMeshProUGUI>().text = "You can Play, Pause, and Re-start the signal using the menu. " +
                        "Go ahead and play the signal using the menu, and take a look at the rx.";
                    m.highlighter.SetHighlighted(m.tx_obj, false);
                    m.highlighter.SetHighlighted(m.rx_obj, true);
                    next_state = State.D7;
                    break;
                case State.D7:
                    m.qTextObj.GetComponent<TextMeshProUGUI>().text = "As more signal reaches the receiver, you can see that the message becomes clearer. " +
                                                            "The yellow ring represents the signal strength. " +
                                                            "Think of receiving a strong signal, as someone talking very loudly - normally its clearer to hear someone the louder they speak";
                    next_state = State.D8;
                    break;
                case State.D8:
                    m.qTextObj.GetComponent<TextMeshProUGUI>().text = "Seeing the signal strength can give you an idea of how easy the signal is for the rx to understand. " +
                                                                    "Just like the signal is impacted by the environment, so is its signal strength. Press the \'Heatmap\' button to see how the signal strength varies across the room. ";
                    next_state = State.D9;
                    break;
                case State.D9:
                    m.qTextObj.GetComponent<TextMeshProUGUI>().text = "If you want to see the paths the signal takes, without seeing the . Press the \'Rays\' button in the menu.";
                    next_state = State.END;
                    break;
            }

        }
    }



    public void ButtonAnswer(int answer)
    {
        if((taskState > 0) && (taskState < 15))
        {
            answerLog += answer.ToString();
            NextTask();
        }
    }

    public void ButtonNext()
    {
        demoTask.Advance();
        demoTask.DoState();
    }


    public void NextTask()
    {
        taskState += 1;



        clearMessageVisuals();

        if (taskState == 1)
        {
            SetCurrentDataSet(csvFile_T12_1); //data set

            ShowObjects_T1();

            NextButton.SetActive(false);
            AnswerButtons.SetActive(true);

            qTextObj.GetComponent<TextMeshProUGUI>().text = "Q: Which highlighted object is having the biggest effect on the WiFi performance at the desk?";
            a1TextObj.GetComponent<TextMeshProUGUI>().text = "Couch (red)";
            a2TextObj.GetComponent<TextMeshProUGUI>().text = "TV/TV Stand (green)";
            a3TextObj.GetComponent<TextMeshProUGUI>().text = "Projector Stand (blue)";
            a4TextObj.GetComponent<TextMeshProUGUI>().text = "Wall Shelf (orange)";
        }
        else if (taskState == 2)
        {
            NextButton.SetActive(true);
            AnswerButtons.SetActive(false);

            qTextObj.GetComponent<TextMeshProUGUI>().text = "Q: (Verbal) Explain your answer and what in the visualization led you to it.";
            a1TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a2TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a3TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a4TextObj.GetComponent<TextMeshProUGUI>().text = "";
        }
        else if (taskState == 3)
        {
            HideObjects_T1();
            SetCurrentDataSet(csvFile_T12_1); //data set

            T2obj1.GetComponent<MeshRenderer>().material = mat_obj1;

            NextButton.SetActive(false);
            AnswerButtons.SetActive(true);

            qTextObj.GetComponent<TextMeshProUGUI>().text = "Q: If we were to add a metal cabinet in the highlighted location (red), how do you think it will affect WiFi performance at the desk?";
            a1TextObj.GetComponent<TextMeshProUGUI>().text = "Improve";
            a2TextObj.GetComponent<TextMeshProUGUI>().text = "Worsen";
            a3TextObj.GetComponent<TextMeshProUGUI>().text = "Stay about the same";
            a4TextObj.GetComponent<TextMeshProUGUI>().text = "------";
        }
        else if (taskState == 4)
        {
            NextButton.SetActive(true);
            AnswerButtons.SetActive(false);

            qTextObj.GetComponent<TextMeshProUGUI>().text = "Q: (Verbal) Explain your answer and what in the visualization led you to it.";
            a1TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a2TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a3TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a4TextObj.GetComponent<TextMeshProUGUI>().text = "";
        }
        else if (taskState == 5)
        {
            T2obj1.GetComponent<MeshRenderer>().material = mat_objDisabled;

            SetCurrentDataSet(csvFile_T12_1); //data set

            ShowObjects_T3();
            T3buttons.SetActive(true);

            NextButton.SetActive(false);
            AnswerButtons.SetActive(true);

            qTextObj.GetComponent<TextMeshProUGUI>().text = "Q: If you had to choose a spot to install a metal cabinet, which location do you think will have the biggest affect on the WiFi performance at the desk?";
            a1TextObj.GetComponent<TextMeshProUGUI>().text = "Location 1 (red)";
            a2TextObj.GetComponent<TextMeshProUGUI>().text = "Location 2 (green)";
            a3TextObj.GetComponent<TextMeshProUGUI>().text = "Location 3 (blue)";
            a4TextObj.GetComponent<TextMeshProUGUI>().text = "-----";
        }
        else if (taskState == 6)
        {
            NextButton.SetActive(true);
            AnswerButtons.SetActive(false);

            qTextObj.GetComponent<TextMeshProUGUI>().text = "Q: (Verbal) Explain your answer and what in the visualization led you to it.";
            a1TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a2TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a3TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a4TextObj.GetComponent<TextMeshProUGUI>().text = "";
        }
        else if (taskState == 7)
        {
            HideObjects_T3();
            T3buttons.SetActive(false);

            SetCurrentDataSet(csvFile_T12_2); //data set

            ShowObjects_T1();

            NextButton.SetActive(false);
            AnswerButtons.SetActive(true);

            qTextObj.GetComponent<TextMeshProUGUI>().text = "Q: Which highlighted object is having the biggest effect on the WiFi performance at the desk?";
            a1TextObj.GetComponent<TextMeshProUGUI>().text = "Couch (red)";
            a2TextObj.GetComponent<TextMeshProUGUI>().text = "TV/TV Stand (green)";
            a3TextObj.GetComponent<TextMeshProUGUI>().text = "Projector Stand (blue)";
            a4TextObj.GetComponent<TextMeshProUGUI>().text = "Wall Shelf (orange)";
        }
        else if (taskState == 8)
        {
            NextButton.SetActive(true);
            AnswerButtons.SetActive(false);

            qTextObj.GetComponent<TextMeshProUGUI>().text = "Q: (Verbal) Explain your answer and what in the visualization led you to it.";
            a1TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a2TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a3TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a4TextObj.GetComponent<TextMeshProUGUI>().text = "";
        }
        else if (taskState == 9)
        {
            HideObjects_T1();
            SetCurrentDataSet(csvFile_T12_2); //data set

            T2obj2.GetComponent<MeshRenderer>().material = mat_obj3;

            NextButton.SetActive(false);
            AnswerButtons.SetActive(true);

            qTextObj.GetComponent<TextMeshProUGUI>().text = "Q: If we were to remove the highlighted projector stand (blue), how do you think it will affect WiFi performance at the desk?";
            a1TextObj.GetComponent<TextMeshProUGUI>().text = "Improve";
            a2TextObj.GetComponent<TextMeshProUGUI>().text = "Worsen";
            a3TextObj.GetComponent<TextMeshProUGUI>().text = "Stay about the same";
            a4TextObj.GetComponent<TextMeshProUGUI>().text = "------";
        }
        else if (taskState == 10)
        {
            NextButton.SetActive(true);
            AnswerButtons.SetActive(false);

            qTextObj.GetComponent<TextMeshProUGUI>().text = "Q: (Verbal) Explain your answer and what in the visualization led you to it.";
            a1TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a2TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a3TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a4TextObj.GetComponent<TextMeshProUGUI>().text = "";
        }
        else if (taskState == 11)
        {
            T2obj2.GetComponent<MeshRenderer>().material = mat_objNeutral;

            SetCurrentDataSet(csvFile_T12_2); //data set

            ShowObjects_T3();
            T3buttons.SetActive(true);

            NextButton.SetActive(false);
            AnswerButtons.SetActive(true);

            qTextObj.GetComponent<TextMeshProUGUI>().text = "Q: If you had to choose a spot to install a metal cabinet, which location do you think will have the biggest affect on the WiFi performance at the desk?";
            a1TextObj.GetComponent<TextMeshProUGUI>().text = "Location 1 (red)";
            a2TextObj.GetComponent<TextMeshProUGUI>().text = "Location 2 (green)";
            a3TextObj.GetComponent<TextMeshProUGUI>().text = "Location 3 (blue)";
            a4TextObj.GetComponent<TextMeshProUGUI>().text = "-----";
        }
        else if (taskState == 12)
        {
            NextButton.SetActive(true);
            AnswerButtons.SetActive(false);

            qTextObj.GetComponent<TextMeshProUGUI>().text = "Q: (Verbal) Explain your answer and what in the visualization led you to it.";
            a1TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a2TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a3TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a4TextObj.GetComponent<TextMeshProUGUI>().text = "";
        }
        else if (taskState == 13)
        {
            HideObjects_T3();
            T3buttons.SetActive(false);

            if (caseState == 1)
            {
                RxAreaObj1.SetActive(false);
                RxAreaObj2.SetActive(true);
            }

            SetCurrentDataSet(csvFile_T4_base);

            NextButton.SetActive(true);
            AnswerButtons.SetActive(false);

            if (caseState == 1)
            {
                qTextObj.GetComponent<TextMeshProUGUI>().text = "Q: (Verbal) Let's now inspect the wireless propogation in the conference room. If this scene was changed to have all of the walls be made out of metal, what do you think will change about the WiFi behavior?";
            }
            else
            {
                qTextObj.GetComponent<TextMeshProUGUI>().text = "Q: (Verbal) Let's now inspect the signal power heatmap in the conference room. If this scene was changed to have all of the walls be made out of metal, what do you think will change about the WiFi behavior?";
            }

            a1TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a2TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a3TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a4TextObj.GetComponent<TextMeshProUGUI>().text = "";
        }
        else if (taskState == 14)
        {
            T4buttons.SetActive(true);

            NextButton.SetActive(true);
            AnswerButtons.SetActive(false);

            qTextObj.GetComponent<TextMeshProUGUI>().text = "Q: (Verbal) When we change to metal walls, the Wifi performance gets worse. Can you explain why based on the visualization?\n\nYou can toggle between both versions of the scene.";
            a1TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a2TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a3TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a4TextObj.GetComponent<TextMeshProUGUI>().text = "";
        }
        else if (taskState == 15)
        {
            T4buttons.SetActive(false);

            NextButton.SetActive(false);
            AnswerButtons.SetActive(false);

            qTextObj.GetComponent<TextMeshProUGUI>().text = "Complete";
            a1TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a2TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a3TextObj.GetComponent<TextMeshProUGUI>().text = "";
            a4TextObj.GetComponent<TextMeshProUGUI>().text = "";

            outputTextObj.GetComponent<TextMeshProUGUI>().text = answerLog;
        }
    }


    public void ShowObjects_T1()
    {
        T1obj1.GetComponent<MeshRenderer>().material = mat_obj1;
        T1obj2.GetComponent<MeshRenderer>().material = mat_obj2;
        T1obj3.GetComponent<MeshRenderer>().material = mat_obj3;
        T1obj4.GetComponent<MeshRenderer>().material = mat_obj4;
    }
    public void HideObjects_T1()
    {
        T1obj1.GetComponent<MeshRenderer>().material = mat_objNeutral;
        T1obj2.GetComponent<MeshRenderer>().material = mat_objNeutral;
        T1obj3.GetComponent<MeshRenderer>().material = mat_objNeutral;
        T1obj4.GetComponent<MeshRenderer>().material = mat_objNeutral;
    }

    public void ShowObjects_T3()
    {
        T3obj1.GetComponent<MeshRenderer>().material = mat_obj1;
        T3obj2.GetComponent<MeshRenderer>().material = mat_obj2;
        T3obj3.GetComponent<MeshRenderer>().material = mat_obj3;
    }
    public void HideObjects_T3()
    {
        T3obj1.GetComponent<MeshRenderer>().material = mat_objDisabled;
        T3obj2.GetComponent<MeshRenderer>().material = mat_objDisabled;
        T3obj3.GetComponent<MeshRenderer>().material = mat_objDisabled;
    }

    public void T3dataButtonInput(int num)
    {
        ShowObjects_T3();
        
        if(taskState == 5 || taskState == 6)
        {
            if (num == 0)
            {
                SetCurrentDataSet(csvFile_T12_1);
            }
            else if (num == 1)
            {
                SetCurrentDataSet(csvFile_T3_1a);
                T3obj1.GetComponent<MeshRenderer>().material = mat_obj1_ACTIVE;
            }
            else if (num == 2)
            {
                SetCurrentDataSet(csvFile_T3_1b);
                T3obj2.GetComponent<MeshRenderer>().material = mat_obj2_ACTIVE;
            }
            else if (num == 3)
            {
                SetCurrentDataSet(csvFile_T3_1c);
                T3obj3.GetComponent<MeshRenderer>().material = mat_obj3_ACTIVE;
            }
        }
        else
        {
            if (num == 0)
            {
                SetCurrentDataSet(csvFile_T12_2);
            }
            else if (num == 1)
            {
                SetCurrentDataSet(csvFile_T3_2a);
                T3obj1.GetComponent<MeshRenderer>().material = mat_obj1_ACTIVE;
            }
            else if (num == 2)
            {
                SetCurrentDataSet(csvFile_T3_2b);
                T3obj2.GetComponent<MeshRenderer>().material = mat_obj2_ACTIVE;
            }
            else if (num == 3)
            {
                SetCurrentDataSet(csvFile_T3_2c);
                T3obj3.GetComponent<MeshRenderer>().material = mat_obj3_ACTIVE;
            }
        }

    }

    public void T4dataButtonInput(bool metal){
        if(metal)
        {
            SetCurrentDataSet(csvFile_T4_metal);
        }
        else
        {
            SetCurrentDataSet(csvFile_T4_base);
        }
    }



}
