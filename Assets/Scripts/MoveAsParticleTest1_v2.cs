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

    [SerializeField] private GameObject NextButton;

    //WiViz on/off text boxes

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


    [SerializeField] private GameObject TxObj; // The object to instantiate for Transmiter
    [SerializeField] private GameObject AntennaObj; // The object to instantiate for Receiver
    [SerializeField] private GameObject PhoneObj; // The object to instantiate for Receiver
    private GameObject RxObj; // set via SetReceiverModel(); defaults to AntennaObj in Start()

    // The phone comes out of a shared FBX, so it needs the asset pack's palette material applied
    // and is small next to the antenna marker.
    [SerializeField, Tooltip("Material applied to the phone receiver (ToonTastic ColorPalette_128_URP).")]
    private Material phoneMaterial;

    private const float PHONE_RECEIVER_SCALE = 2f;

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
    private const float Rx_POWER_MAX_dBm = -40.0f;
    private float LowestPowerValRays_dBm; //  lowest power value among all rays from data file. calculate once at begin.
    private float LowestPowerValRx_dBm = -100f;
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

    [SerializeField]
    public GameObject tv_obj;

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
    private float changeSize;
    private GameObject[] particle_text_objs;
    bool[] completed_particles;
    Vector3 particle_text_translation = new Vector3(0f, 0.1f, 0f);
    Vector3 endpoint_text_translation = new Vector3(0f, 0.5f, 0f);

    // Reference distance for the arrival weighting in AddMessageToEndpoint.
    private const float MESSAGE_DISTANCE_REF = 15f;

    public void SetMessage(string message, float message_fontsize = 1f, float changeAlpha = 0.05f, float changeSize = 0.05f)
    {
        // We're going to assume that none of the examples will ever have the message change midway through rays bouncing
        // Or, honestly I don't see why we would need the user to control the messages at all.
        this.message_fontsize = message_fontsize;
        this.changeAlpha = changeAlpha;
        this.message = message;
        this.changeSize = changeSize;

        // Give the particles labels for the new message straight away, so a state that only calls
        // SetMessage and then waits for Play (e.g. the demo's D4) is still captioned.
        RebuildParticleTexts();
    }

    // (Re)create the text riding on each particle at its current position. Created hidden - each is
    // revealed by UpdateParticleText once its particle leaves the transmitter, so the labels never
    // stack up on the emitter before playback.
    private void RebuildParticleTexts()
    {
        if (particles == null) return;

        if (particle_text_objs != null)
        {
            for (int i = 0; i < particle_text_objs.Length; i++)
                if (particle_text_objs[i] != null) Destroy(particle_text_objs[i]);
        }

        particle_text_objs = new GameObject[particles.Length];

        if (string.IsNullOrEmpty(message)) return;

        for (int i = 0; i < particles.Length; i++)
        {
            // A ray that has already arrived has delivered its message; it needs no label.
            if (completed_particles != null && i < completed_particles.Length && completed_particles[i]) continue;

            particle_text_objs[i] = MakeMessageParticleText(particles[i], i);
        }
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

        // Hidden until the particle actually leaves the transmitter - otherwise every ray's message
        // stacks up on the Tx before playback even starts.
        textObject.SetActive(false);

        return textObject;
    }

    // Keep a particle's message text with it, revealing it once the particle has left its start point.
    private void UpdateParticleText(int i, Vector3 particlePos, List<Vector3> pathPositions)
    {
        if (particle_text_objs == null || i >= particle_text_objs.Length) return;

        GameObject textObj = particle_text_objs[i];
        if (textObj == null) return;

        textObj.transform.position = particlePos + particle_text_translation;

        if (!textObj.activeSelf &&
            pathPositions != null && pathPositions.Count > 0 &&
            (particlePos - pathPositions[0]).sqrMagnitude > 0.0001f)
        {
            textObj.SetActive(true);
        }
    }

    private void clearMessageVisuals()
    {
        if (particles == null) return;

        // Clear completed particles
        completed_particles = new bool[particles.Length];

        // Clear all arrived-message text
        ClearMessageAnchors();

        // Destroy text still riding on particles (including any left in mid-air)
        if (particle_text_objs != null)
        {
            for (int i = 0; i < particle_text_objs.Length; i++)
            {
                if (particle_text_objs[i] != null) Destroy(particle_text_objs[i]);
            }
        }

        particle_text_objs = new GameObject[particles.Length];
    }

    // Message text that has arrived, keyed by the world position of the ray endpoint it arrived at.
    private readonly Dictionary<Vector3, GameObject> messageAnchors = new Dictionary<Vector3, GameObject>();
    private GameObject messageAnchorGrp;

    // Accumulate the message at a world position - the last point of a ray path.
    // Each further arrival at the same point makes the text brighter and larger.
    //
    // This replaces the old addMessageToRx(message, rxGameObject): parenting the text to the Rx prefab's
    // "MessageDisplay" made it inherit that hierarchy's scale (root 0.02/-0.15/0.02 x child 4/33.3/4 =
    // 0.08/-5/0.08, i.e. squashed and vertically mirrored), and MessageDisplay is a Canvas RectTransform,
    // which a 3D TextMeshPro is not meant to live under. Anchoring in world space avoids both problems.
    // pathDistance: total length of the path this arrival travelled. A ray that took a long way
    // round contributes less to how readable the message becomes.
    private void AddMessageToEndpoint(string message, Vector3 endpoint, float pathDistance)
    {
        if (string.IsNullOrEmpty(message)) return;

        float weight = Mathf.Max(MESSAGE_DISTANCE_REF - pathDistance, 1f);

        if (messageAnchorGrp == null)
        {
            messageAnchorGrp = new GameObject("Message_Anchors_Group");
        }

        if (!messageAnchors.TryGetValue(endpoint, out GameObject anchor) || anchor == null)
        {
            anchor = new GameObject("MessageAnchor");
            anchor.transform.SetParent(messageAnchorGrp.transform, false);
            anchor.transform.position = endpoint + endpoint_text_translation;
            messageAnchors[endpoint] = anchor;
        }

        // ------------------------------------------------------------
        // Reinforce the existing message
        // ------------------------------------------------------------

        TextMeshPro existing = anchor.GetComponentInChildren<TextMeshPro>();

        // ------------------------------------------------------------
        // Message doesn't exist here yet - create it, fully transparent, so that this arrival and
        // every later one contribute the same weighted increment below.
        // ------------------------------------------------------------

        if (existing == null || existing.text != message)
        {
            GameObject textObject = new GameObject("MessageText");
            textObject.transform.SetParent(anchor.transform, false);

            // Add the same camera-facing script
            textObject.AddComponent<FaceCamera>();

            existing = textObject.AddComponent<TextMeshPro>();
            existing.text = message;
            existing.fontSize = message_fontsize;   // SAME SIZE AS PARTICLE MESSAGE
            existing.alignment = TextAlignmentOptions.Center;

            Color textColor = Color.white;
            textColor.a = 0f;
            existing.color = textColor;
        }

        // ------------------------------------------------------------
        // Reinforce the message by this arrival's weight
        // ------------------------------------------------------------

        Color color = existing.color;
        color.a = Mathf.Clamp01(color.a + weight * changeAlpha);
        existing.color = color;

        // Guard the floor: a non-positive font size stops TextMeshPro rendering entirely.
        existing.fontSize = Mathf.Max(existing.fontSize + weight * changeSize, 0.01f);
    }

    // Remove every arrived-message anchor (called when restarting or switching dataset).
    // Destroy every message visual currently on screen - text riding on particles and text already
    // delivered to an endpoint - here and in any live MoveTempParticles animation.
    //
    // Called before a task manager advances, so that interrupting an animation (Next or an MCQ
    // option pressed early) never leaves labels hovering in mid-air. Unlike clearMessageVisuals()
    // this deliberately does NOT reset completed_particles: particles that have already arrived
    // must not deliver their message a second time.
    public void ClearAllMessageText()
    {
        // Rebuild rather than just destroy: the labels come back hidden and attached to the current
        // particles, so whatever plays next is still captioned, while text left over from an
        // interrupted run (and anything already delivered to an endpoint) is gone.
        RebuildParticleTexts();

        ClearMessageAnchors();
        MoveTempParticles.ClearAllMessageText();
    }

    private void ClearMessageAnchors()
    {
        foreach (GameObject anchor in messageAnchors.Values)
        {
            if (anchor != null) Destroy(anchor);
        }

        messageAnchors.Clear();
    }




    // sets the initial positions for each particle of ParticleSystem1

    private void InitializeParticlePositions()
    {
        // Destroy the previous dataset's labels before dropping the array, otherwise they are
        // orphaned and hang in the scene forever.
        if (particle_text_objs != null)
        {
            for (int i = 0; i < particle_text_objs.Length; i++)
                if (particle_text_objs[i] != null) Destroy(particle_text_objs[i]);
        }

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

    // Rx_Number -> its spawned marker, so callers can address a specific receiver (e.g. to highlight
    // both phones of a two-receiver dataset).
    private readonly Dictionary<int, GameObject> rxMarkersByNum = new Dictionary<int, GameObject>();
    // Marks the end points of all ray paths with RxObj instances
    void MarkEndPoints_Rx()
    {

        path_idx_to_rx_obj = new GameObject[loadedRaysPath.Count()]; // Maps ray index -> its Rx marker. No longer used for message text (see AddMessageToEndpoint); kept as bookkeeping.

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

        rxMarkersByNum.Clear();

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
                    rxMarkersByNum[rayPath.RxNum] = markedPositions[endPosition];
                    continue;
                }

                // Instantiate the RxObj at the end position as a child of RxObjGrp
                GameObject endMarker = Instantiate(RxObj, endPosition, Quaternion.identity, RxObjGrp.transform);
                StyleReceiverMarker(endMarker);

                // Bookkeeping for highlighting
                this.rx_obj = endMarker;
                rxMarkersByNum[rayPath.RxNum] = endMarker;


                // Ed - Set color of recievers based on power
                MeshRenderer endMarkRend = endMarker.GetComponent<MeshRenderer>();
                int rxColorIdx = GetColorIndexFromRx_dBm(rayPath.TotalPowerNum, 0);
                Color rxColor = colorHelper.GetPaletteColor(rxColorIdx);
                rxColor.a = 0.6f;
/*                endMarkRend.material.SetColor("_BaseColor", rxColor);
                endMarkRend.material.SetColor("_EmissionColor", rxColor);*/

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

        path_idx_to_rx_obj = new GameObject[loadedHeatmapPath.Count()]; // Maps ray index -> its Rx marker. No longer used for message text (see AddMessageToEndpoint); kept as bookkeeping.

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
                StyleReceiverMarker(endMarker);

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
        Debug.Log("Clearing heatmap");
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
        if (MoveTempParticles.ToggleRaysAll()) return;

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

    // The task manager currently driving the Q&A panel (demo first, then Lesson 1, ...).
    private ITaskManager CurrentTaskManager;

    // ---- Accessors used by task managers living outside this class (e.g. Task1Manager) ----
    public TextMeshProUGUI QuestionText => qTextObj.GetComponent<TextMeshProUGUI>();
    public GameObject[] AnswerButtons => new[] { a1TextObj, a2TextObj, a3TextObj, a4TextObj };
    public GameObject NextButtonObj => NextButton;
    public Material[] OptionMaterials => new[] { mat_obj1, mat_obj2, mat_obj3, mat_obj4 };
    public GameObject RxPrefab => RxObj;
    public ObjectHighlighter Highlighter => highlighter;
    public GameObject TxMarker => tx_obj;
    public GameObject RxMarker => rx_obj;
    public bool IsHeatmapShown => heatmap_obj != null;
    public bool AreRaysShown => ray_objects != null;

    // Clear the main static rays directly, without the MoveTempParticles delegation that
    // ToggleRays() applies. Used when a lesson replaces them with its own per-animation rays:
    // SetCurrentDataSet rebuilds these in viz_color for whatever dataset was just loaded, so they
    // have to be taken down explicitly rather than toggled.
    public void HideMainRays()
    {
        ClearPathLine_MultiPaths();
    }
    public bool RaysPaused => isRayMovementPaused;
    public string DemoCsvFile => csvFile_Demo;
    public List<RayPathSet_v2> LoadedRaysPath => loadedRaysPath;

    // Rays belonging to one receiver of a multi-Rx dataset (Rx_Number 1, 2, ...).
    public List<RayPathSet_v2> PathsForRx(int rxNum) =>
        loadedRaysPath.Where(p => p.RxNum == rxNum).ToList();

    // Distinct Rx_Number values in the current dataset, ascending.
    public List<int> RxNumbers()
    {
        List<int> nums = loadedRaysPath.Select(p => p.RxNum).Distinct().ToList();
        nums.Sort();
        return nums;
    }

    // The spawned Rx marker for a given Rx_Number (rx_obj only ever holds the last one created).
    public GameObject RxMarkerFor(int rxNum) =>
        rxMarkersByNum.TryGetValue(rxNum, out GameObject go) ? go : null;

    // Raised once when every ray of the current dataset has reached its receiver.
    // Reset whenever the dataset is reloaded or the rays are restarted.
    public event Action OnAllRaysCompleted;
    private bool allRaysCompletedFired;

    // Spawn a temporary animation over the supplied paths, reusing this component's
    // particle system settings, ray speed and ray size.
    public MoveTempParticles SpawnTempParticles(List<RayPathSet_v2> paths, Color32 color)
    {
        return MoveTempParticles.Create(paths, particleSystem1, color, RaySpeed, raySize);
    }

    // Load a dataset's ray paths / heatmap rows into this component (no particle re-initialisation).
    public void LoadData(string fileName, bool forHeatmap) => GetData(fileName, forHeatmap);

    public void LogAnswer(string line)
    {
        answerLog += "\n" + line;

    }

    // ------------------------------------------------------------------
    // WaitPlay - hold the lesson until the participant plays the animation
    // ------------------------------------------------------------------
    // Highlights Play/Pause and Restart and locks Next (and the QA answer buttons) until one of
    // them is pressed, so a state cannot be skipped past before its animation has been watched.

    // The Play/Pause and Restart pair, discovered once. The default thing to wait on.
    private readonly List<UnityEngine.UI.Button> transportButtons = new List<UnityEngine.UI.Button>();

    private UnityEngine.UI.Button heatmapButton;
    private UnityEngine.UI.Button raysButton;

    // The buttons this gate is currently highlighting and waiting on - the transport pair, or a
    // specific button such as Heatmap or Rays.
    private readonly List<UnityEngine.UI.Button> awaitedButtons = new List<UnityEngine.UI.Button>();
    private readonly List<Color> awaitedSavedColors = new List<Color>();

    private bool awaitingTransport;       // true when the gate is the default Play/Restart pair
    private bool highlightingAwaited;     // drives the per-frame re-apply of the highlight
    private bool qaLocked;                // Next and the answer buttons are held non-selectable
    private bool releaseOnPlayPress = true;

    // Runtime onClick hook used when waiting on a button we cannot intercept in code.
    private UnityEngine.UI.Button hookedButton;
    private UnityEngine.Events.UnityAction hookedListener;

    public bool WaitingForPlay => qaLocked;

    // Discover the menu buttons wired to RayPlayPause / Restart, so this needs no Inspector setup.
    // Runs after WireAnswerButtons(): the answer buttons carry stale RayPlayPause calls which that
    // method switches off, and disabled listeners are skipped here.
    private void FindAllButtons()
    {
        transportButtons.Clear();

        UnityEngine.UI.Button[] all = FindObjectsByType<UnityEngine.UI.Button>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (UnityEngine.UI.Button button in all)
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentListenerState(i) == UnityEngine.Events.UnityEventCallState.Off)
                    continue;

                if (!ReferenceEquals(button.onClick.GetPersistentTarget(i), this)) continue;

                string method = button.onClick.GetPersistentMethodName(i);

                if (method == nameof(RayPlayPause) || method == nameof(Restart))
                {
                    transportButtons.Add(button);
                    break;
                }

                else if (method == nameof(ToggleRays))
                {
                    raysButton = button;
                }
                else if (method == nameof(ToggleHeatmap)) {
                    heatmapButton = button;
                }
            }
        }

        Debug.Log($"Transport buttons found: {transportButtons.Count}");


    }

    // Buttons locked while waiting. Everything in the QA panel, minus anything we are waiting on -
    // locking a transport button would make the wait impossible to satisfy.
    private List<UnityEngine.UI.Button> GatedQAButtons()
    {
        List<UnityEngine.UI.Button> gated = new List<UnityEngine.UI.Button>();

        Transform panel = a1TextObj != null ? a1TextObj.transform.parent : null;
        if (panel == null) return gated;

        foreach (UnityEngine.UI.Button button in panel.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            if (!transportButtons.Contains(button) && !awaitedButtons.Contains(button))
                gated.Add(button);

        return gated;
    }

    // Find the menu button wired to a given method on this component, e.g. nameof(ToggleHeatmap)
    // or nameof(ToggleRays), so callers can pass one to WaitPlay without any Inspector wiring.
    public UnityEngine.UI.Button FindMenuButton(string methodName)
    {
        UnityEngine.UI.Button[] all = FindObjectsByType<UnityEngine.UI.Button>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (UnityEngine.UI.Button button in all)
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentListenerState(i) == UnityEngine.Events.UnityEventCallState.Off)
                    continue;

                if (!ReferenceEquals(button.onClick.GetPersistentTarget(i), this)) continue;

                if (button.onClick.GetPersistentMethodName(i) == methodName) return button;
            }
        }

        Debug.LogWarning($"MoveAsParticleTest1_v2: no menu button found for {methodName}.");
        return null;
    }

    // Wait on Play/Pause or Restart (the default gate).
    //
    // releaseOnPlayPress: true  - Next unlocks as soon as one is pressed.
    //                     false - Next stays locked after the press; the caller must call
    //                             ReleaseWaitPlay() itself, e.g. when the animation has finished.
    public void WaitPlay(bool releaseOnPlayPress = true)
    {
        BeginWait(null, releaseOnPlayPress);
    }

    // Wait on some other menu button instead - Heatmap, Rays, anything with a Button on it. Only
    // that button satisfies the gate; pressing Play will not. Passing null falls back to the
    // transport pair, so WaitPlay(null) behaves like WaitPlay().
    public void WaitPlay(UnityEngine.UI.Button awaitedButton, bool releaseOnPress = true)
    {
        BeginWait(awaitedButton, releaseOnPress);
    }

    private void BeginWait(UnityEngine.UI.Button awaitedButton, bool releaseOnPress)
    {
        if (transportButtons.Count == 0) FindAllButtons();

        // Drop any hook from a previous gate before re-targeting.
        UnhookAwaitedButton();

        awaitedButtons.Clear();

        if (awaitedButton != null)
        {
            awaitedButtons.Add(awaitedButton);
            awaitingTransport = false;

            // Play/Restart are released from inside RayPlayPause()/Restart(); anything else has to
            // be observed through its own onClick.
            hookedButton = awaitedButton;
            hookedListener = EndWaitPlay;
            hookedButton.onClick.AddListener(hookedListener);
        }
        else
        {
            awaitedButtons.AddRange(transportButtons);
            awaitingTransport = true;
        }

        // Saved after the target is chosen, and only when not already highlighting, so a second
        // call cannot store the highlight colours as if they were the originals.
        if (!highlightingAwaited)
        {
            awaitedSavedColors.Clear();

            foreach (UnityEngine.UI.Button button in awaitedButtons)
                awaitedSavedColors.Add(button.targetGraphic != null
                    ? button.targetGraphic.color
                    : Color.white);
        }

        this.releaseOnPlayPress = releaseOnPress;

        highlightingAwaited = true;
        qaLocked = true;

        ApplyAwaitedHighlight();

        foreach (UnityEngine.UI.Button button in GatedQAButtons())
            button.interactable = false;
    }

    // Called when the awaited button is pressed. The highlight has done its job either way; whether
    // Next unlocks now depends on how the gate was opened.
    private void EndWaitPlay()
    {
        StopAwaitedHighlight();

        if (releaseOnPlayPress) ReleaseQAButtons();
    }

    // Open a gate that was set with releaseOnPress: false.
    public void ReleaseWaitPlay()
    {
        StopAwaitedHighlight();
        ReleaseQAButtons();
    }

    private void StopAwaitedHighlight()
    {
        if (!highlightingAwaited) return;

        highlightingAwaited = false;

        for (int i = 0; i < awaitedButtons.Count; i++)
        {
            UnityEngine.UI.Button button = awaitedButtons[i];

            if (button != null && button.targetGraphic != null && i < awaitedSavedColors.Count)
                button.targetGraphic.color = awaitedSavedColors[i];
        }

        UnhookAwaitedButton();
    }

    private void UnhookAwaitedButton()
    {
        if (hookedButton != null && hookedListener != null)
            hookedButton.onClick.RemoveListener(hookedListener);

        hookedButton = null;
        hookedListener = null;
    }

    private void ReleaseQAButtons()
    {
        if (!qaLocked) return;

        qaLocked = false;

        foreach (UnityEngine.UI.Button button in GatedQAButtons())
            button.interactable = true;
    }

    // Re-applied every frame while waiting: MenuManager's look-to-hover handling drives the Button's
    // own colour transitions, which would otherwise wipe the highlight as soon as one is glanced at.
    private void ApplyAwaitedHighlight()
    {
        foreach (UnityEngine.UI.Button button in awaitedButtons)
            if (button != null && button.targetGraphic != null)
                button.targetGraphic.color = button.colors.highlightedColor;
    }

    // The answer buttons in the scene still carry stale persistent onClick calls (RayPlayPause).
    // Turn those off and route the buttons to ButtonAnswer(idx) instead, without editing the scene.
    private void WireAnswerButtons()
    {
        GameObject[] btns = AnswerButtons;
        for (int k = 0; k < btns.Length; k++)
        {
            if (btns[k] == null) continue;
            var button = btns[k].GetComponent<UnityEngine.UI.Button>();
            if (button == null) continue;

            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                button.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);

            int idx = k; // capture per button
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ButtonAnswer(idx));
        }
    }

    // bookkeeping.
    private GameObject tx_obj;
    private GameObject rx_obj;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    // Which prefab MarkEndPoints_Rx spawns at the end of each ray path.
    public enum ReceiverModel { Antenna, Phone }

    // Must be set BEFORE SetCurrentDataSet: the Rx markers are built during the dataset load, so a
    // change made afterwards would not show until the next load.
    public void SetReceiverModel(ReceiverModel model)
    {
        GameObject prefab = model == ReceiverModel.Phone ? PhoneObj : AntennaObj;

        if (prefab == null)
        {
            Debug.LogError($"MoveAsParticleTest1_v2: {model} receiver prefab is not assigned in the Inspector.");
            return;
        }

        RxObj = prefab;
        currentReceiverModel = model;
    }

    private ReceiverModel currentReceiverModel = ReceiverModel.Antenna;

    // Adjust a freshly spawned Rx marker. Only the phone needs it - the antenna prefab is already
    // authored the way it should look.
    private void StyleReceiverMarker(GameObject marker)
    {
        if (marker == null || currentReceiverModel != ReceiverModel.Phone) return;

        marker.transform.localScale *= PHONE_RECEIVER_SCALE;

        if (phoneMaterial == null)
        {
            Debug.LogWarning("MoveAsParticleTest1_v2: phoneMaterial is not assigned; " +
                             "the phone will render with whatever material the FBX imported.");
            return;
        }

        // The model is one object out of a multi-mesh FBX, so the renderers may sit on children.
        foreach (Renderer renderer in marker.GetComponentsInChildren<Renderer>(true))
        {
            // Never shrink the array to zero - a renderer with no materials draws nothing.
            Material[] mats = new Material[Mathf.Max(renderer.sharedMaterials.Length, 1)];

            for (int i = 0; i < mats.Length; i++) mats[i] = phoneMaterial;

            renderer.sharedMaterials = mats;
        }
    }

    void Start()
    {
        // Antenna is the default receiver; the task managers switch to the phone where they need it.
        SetReceiverModel(ReceiverModel.Antenna);

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


        WireAnswerButtons();
        FindAllButtons();     // must follow WireAnswerButtons - see that method's note

        // Current order: Lesson 2, then the demo, then Lesson 1.
        // (TaskCommTestManager still exists but is not in the chain.)
                CurrentTaskManager = new DemoTaskManager(this);
        //CurrentTaskManager = new Task2Manager(this);
        CurrentTaskManager.DoState();
        //MarkPathPositions_obj();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateParticles();

        if (highlightingAwaited) ApplyAwaitedHighlight();
    }

    // toggle play/pause state of entire rays movement
    public void RayPlayPause()
    {
        // The participant has started the animation; release the gate, but only if it was Play or
        // Restart it was waiting on. A gate set on Heatmap or Rays is not satisfied by pressing Play.
        if (awaitingTransport) EndWaitPlay();

        // Temporary animations take over the transport controls while any are alive.
        if (MoveTempParticles.TogglePlayPauseAll()) return;

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
        if (awaitingTransport) EndWaitPlay();

        if (MoveTempParticles.RestartAll()) return;

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

        allRaysCompletedFired = false;

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

                UpdateParticleText(i, particle.position, pathPositions);

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
                    UpdateParticleText(i, particle.position, pathPositions);

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
            if (!particleReachedEnd)
            {
                UpdateParticleText(i, particle.position, pathPositions);
            }

            // Handle particles that have reached their end position
            if (particleReachedEnd)
            {
                // Make particle invisible after some time or right away if desired
                particle.startSize = 0f;
                particle.remainingLifetime = 0f;

                // Mark particle as completed so it will not be added to Rx in next iteration
                if (!completed_particles[i] && pathPositions != null && pathPositions.Count > 0)
                {
                    AddMessageToEndpoint(
                        message,
                        pathPositions[pathPositions.Count - 1],
                        (pathTotalLengths != null && i < pathTotalLengths.Length) ? pathTotalLengths[i] : 0f);
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

        // Notify listeners once every ray has arrived at its receiver.
        if (!allRaysCompletedFired && AllRaysCompleted())
        {
            allRaysCompletedFired = true;
            OnAllRaysCompleted?.Invoke();
        }
    }

    private bool AllRaysCompleted()
    {
        if (completed_particles == null || completed_particles.Length == 0) return false;
        foreach (bool done in completed_particles) if (!done) return false;
        return true;
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

    // Switch the active dataset (<fileName>.csv + <fileName>_heatmap.csv in Resources).
    // If the heatmap / static rays were shown before the switch, they are re-created for the
    // new dataset so the participant can compare options without re-toggling.
    public void SetCurrentDataSet(string fileName)
    {
        bool heatmapWasShown = heatmap_obj != null;
        bool raysWereShown = ray_objects != null;

        Debug.Log($"SetCurrentDataSet({fileName})");

        ClearPathLine_MultiPaths();
        ClearHeatmap();
        ClearMessageAnchors();  // endpoints belong to the outgoing dataset

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
        this.allRaysCompletedFired = false;

        // Initialize path distances for power calculations
        InitializePathDistances();

        for (int i = 0; i < this.particles.Length; i++) {
            this.particles[i].startColor = this.viz_color;
            this.particles[i].color = this.viz_color;
        }

        if (heatmapWasShown) MakeHeatmap();
        if (raysWereShown) MarkPathLine_MultiPaths();

        // Loading a dataset never starts playback. isRayMovementPaused is sticky - once the
        // participant has pressed Play it stays false - so without this the freshly seeded
        // particles would animate on their own as soon as the next task loaded its data.
        isRayMovementPaused = true;
        if (particleSystem1 != null) particleSystem1.Pause();
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
        

    }

    // Wrap each substring in a TextMeshPro colour tag. Public so managers that are not nested in
    // this class (Task1Manager, Task2Manager) can use the same wording treatment as the demo.
    public static string HighlightSubstrings(
        string text,
        List<string> substrings,
        string colorHex = "#00FF00")
    {
        if (string.IsNullOrEmpty(text) ||
            substrings == null ||
            substrings.Count == 0)
        {
            return text;
        }

        foreach (string substring in substrings)
        {
            if (string.IsNullOrEmpty(substring))
                continue;

            text = text.Replace(
                substring,
                "<color=" + colorHex + ">" + substring + "</color>"
            );
        }

        return text;
    }

    // Same, taking the colour the thing is drawn in - so the wording matches its signal on screen.
    public static string HighlightSubstrings(
        string text,
        List<string> substrings,
        Color color)
    {
        return HighlightSubstrings(text, substrings, "#" + ColorUtility.ToHtmlStringRGB(color));
    }

    public class DemoTaskManager : ITaskManager
    {
        // Listed and handled in the order they run: D1 -> D2 -> ... -> D9 -> END.
        private enum State
        {
            D1, D2, D3, D4, D5, D6, D7, D8, D9, D10, D11, END
        }

        private State state, next_state;

        MoveAsParticleTest1_v2 m;
        List<string> highlights;

        public bool IsComplete => state == State.END;

        // The demo has no MCQ; answer buttons are hidden during it.
        public void OnAnswerSelected(int answerIdx) { }

        public DemoTaskManager(MoveAsParticleTest1_v2 m)
        {
            this.m = m;
            state = State.D1;
            next_state = State.D1;

            // The demo's receiver is the TV antenna. Stated explicitly rather than relying on the
            // Start() default, so the demo is correct wherever it sits in the manager chain.
            m.SetReceiverModel(ReceiverModel.Antenna);

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
                    highlights = new List<string>
                        {
                            "TV"
                        };
                    m.qTextObj.GetComponent<TextMeshProUGUI>().text = HighlightSubstrings("You're in your living room watching YouTube right now." +
                        "\n\nHow does your TV use WiFi to stream videos from the internet? Press Next to find out!", highlights);

                    // Maybe remove mention of 'The wave' and just refere to it as a signal only

                    // Add a highlight on the TV mesh.
                    m.highlighter.SetHighlighted(m.tv_obj, true); // Can access these, even though they are private??

                    next_state = State.D2;
                    break;
                case State.D2:

                    highlights = new List<string>
                        {
                            "router"
                        };

                    m.qTextObj.GetComponent<TextMeshProUGUI>().text = 
                        HighlightSubstrings("" +
                        "Your WiFi router has a cable that connects it to the internet. " +
                        "It turns the video data into a signal, and sends it out into the environment." +
                        "\n\nPress Play/Pause to see what this wave looks like.", highlights);

                    m.highlighter.SetHighlighted(m.tv_obj, false); // Can access these, even though they are private??
                    m.highlighter.SetHighlighted(m.tx_obj, true); // Can access these, even though they are private??
                    m.highlighter.SetHighlighted(m.rx_obj, false);

                    m.WaitPlay();
                    // Halt here
                    next_state = State.D3;
                    break;

                case State.D3:
                    highlights = new List<string>
                        {
                            "transmits (tx)",
                            "receiver (rx)"
                        };


                    m.qTextObj.GetComponent<TextMeshProUGUI>().text = 
                        HighlightSubstrings("Your router turns the video into a signal, and transmits (tx) it." +
                        "\n\nYour TV has a receiver (rx) that lets it hear that signal.", highlights);

                    m.highlighter.SetHighlighted(m.tv_obj, false);
                    m.highlighter.SetHighlighted(m.tx_obj, true); 
                    m.highlighter.SetHighlighted(m.rx_obj, true);
                    next_state = State.D4;
                    break;
                case State.D4:
                    highlights = new List<string>{"rx", "tx"};
                    m.highlighter.SetHighlighted(m.tx_obj, true);
                    m.highlighter.SetHighlighted(m.rx_obj, true);
                    m.qTextObj.GetComponent<TextMeshProUGUI>().text =
                        HighlightSubstrings("Let's look at how the tx sends a signal to the rx. " +
                        "\n\nWhen you press Play, the router is going to say \"Hello\" to your TV!", highlights);
                    m.SetMessage("Hello");
                    // Playback is the participant's: they start it with Play/Pause or Restart.

                    m.WaitPlay();
                    next_state = State.D6;
                    break;
/*                case State.D5:

                    highlights = new List<string> { "rx"};
                    m.highlighter.SetHighlighted(m.tx_obj, false);
                    m.highlighter.SetHighlighted(m.rx_obj, true);
                    m.qTextObj.GetComponent<TextMeshProUGUI>().text =
                        HighlightSubstrings("Play the signal using the menu and take a look at the rx."
                        + "You can see that the message becomes clearer to read as more of the signal arrives.", highlights);
                    next_state = State.D6;
                    break;*/
                case State.D6:
                    m.qTextObj.GetComponent<TextMeshProUGUI>().text =
                       HighlightSubstrings("This is how wireless communication works!" +
                       "\n\nWiFi is a kind of wireless communication where devices can access the internet by communicating with your router.", highlights);

                    next_state = State.D7;
                    break;
                case State.D7:
                    m.highlighter.SetHighlighted(m.tx_obj, false);
                    m.highlighter.SetHighlighted(m.rx_obj, false);
                    m.qTextObj.GetComponent<TextMeshProUGUI>().text = "If you press 'Rays' you can see that the signal takes multiple paths through the entire room." +
                        "\n\nIf you press Re-start, you can see how each path moves on its own through the environment.";
                    m.WaitPlay(m.raysButton);

                    next_state = State.D8;
                    break;
                case State.D8:

                    highlights = new List<string> { "above the receiver" };

                    m.qTextObj.GetComponent<TextMeshProUGUI>().text = HighlightSubstrings("To the receiver, the signal gets stronger as more paths arrive." +
                        "\n\nWhen you play the signal, you can see how the message gets clearer and easier to read above the receiver.", highlights);
                    m.highlighter.SetHighlighted(m.tx_obj, false);
                    m.highlighter.SetHighlighted(m.rx_obj, true);


                    next_state = State.D9;
                    break;

                //+ " A high received signal strength is like someone talking loudly. The louder you hear someone speak - the easier it is for you to understand them.";
                case State.D9:

                    m.qTextObj.GetComponent<TextMeshProUGUI>().text = "Receiving a strong signal is like hearing someone talking loudly." +
                        "\n\nThe louder you hear someone speak - the easier it is for you to understand them.";
                    next_state = State.D10;
                    break;
                case State.D10:
                    m.highlighter.SetHighlighted(m.tx_obj, false);
                    m.highlighter.SetHighlighted(m.rx_obj, false);
                    m.qTextObj.GetComponent<TextMeshProUGUI>().text = 
                                                                "The signal only moves through parts of the room, so some places might get lower signal strength than others" +
                                                                    "\n\nPress the \'Heatmap\' button to see the signal strength in each location.";
                    m.WaitPlay(m.heatmapButton);
                    next_state = State.D11;
                    break;
                case State.D11:
                    m.qTextObj.GetComponent<TextMeshProUGUI>().text = "To see the signal strength of a color, check the color bar at the bottom right-hand side of this menu." +
                        "\n\nex. Red indicates a strong signal, blue indicates a weak signal.";
                    next_state = State.END;
                    break;
                    //  "To see the signal strength of a color, check the color bar at the bottom right-hand side of this menu."
            }

        }
    }



    public void ButtonAnswer(int answer)
    {
        if (CurrentTaskManager == null) return;

        // Selecting an option reloads the dataset and restarts the animation - drop any text the
        // interrupted run left behind before the new state puts its own on screen.
        ClearAllMessageText();

        CurrentTaskManager.OnAnswerSelected(answer);
    }

    public void ButtonNext()
    {
        if (CurrentTaskManager == null) return;

        // Same for advancing: clear first, so a state entered part-way through an animation starts
        // from a clean screen.
        ClearAllMessageText();

        CurrentTaskManager.Advance();

        // Hand over to the next manager in the sequence when the current one finishes.
        // Task1Manager is last, so its completion matches neither case and it simply stays.
        if (CurrentTaskManager.IsComplete)
        {
            // Can manually change ordering for debugging.
            /*            if (CurrentTaskManager is Task2Manager)
                            CurrentTaskManager = new Task1Manager(this);
                        else if (CurrentTaskManager is Task1Manager)
                            CurrentTaskManager = new DemoTaskManager(this);*/

            if (CurrentTaskManager is DemoTaskManager)
                CurrentTaskManager = new Task1Manager(this);
            else if (CurrentTaskManager is Task1Manager)
                CurrentTaskManager = new Task2Manager(this);
        }

        CurrentTaskManager.DoState();
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
