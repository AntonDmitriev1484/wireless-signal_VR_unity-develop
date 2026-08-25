/*
 *  Temporary, self-contained particle animation over a list of ray paths.
 *
 *  Takes the same data shape as MoveAsParticleTest1_v2.loadedRaysPath (List<RayPathSet_v2>) and
 *  animates one particle along each path, plus optional static ray lines - all drawn in viz_color.
 *
 *  Differences from the animation loop inside MoveAsParticleTest1_v2:
 *    - it owns its playback cursor (segmentIdx) instead of writing RayPathSet_v2.PathPositionsIdx,
 *      so it can animate the same path objects without disturbing the main visualisation;
 *    - no intersection marks and no power-based colouring - just movement plus the message text;
 *    - the arriving message accumulates on world-space anchors it creates at each path's endpoint,
 *      rather than on a "MessageDisplay" child of an Rx prefab (the Tx prefab has no such child);
 *    - it clones the main ParticleSystem so material / renderer settings match, and destroys
 *      everything it created when it is stopped.
 *
 *  While at least one instance is alive, MoveAsParticleTest1_v2's Play/Pause, Restart and
 *  Toggle Rays buttons drive that instance instead of the main animation (see MoveTempParticles.Current).
 */

using System;
using TMPro;
using System.Collections.Generic;
using UnityEngine;

public class MoveTempParticles : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Active-instance registry
    // ------------------------------------------------------------------
    private static readonly List<MoveTempParticles> active = new List<MoveTempParticles>();

    // Most recently created instance that is still alive, or null.
    public static MoveTempParticles Current
    {
        get
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i] == null) { active.RemoveAt(i); continue; }
                return active[i];
            }
            return null;
        }
    }

    public static bool AnyActive => Current != null;

    // Every live instance, with destroyed ones pruned.
    public static List<MoveTempParticles> ActiveInstances()
    {
        for (int i = active.Count - 1; i >= 0; i--)
            if (active[i] == null) active.RemoveAt(i);

        return new List<MoveTempParticles>(active);
    }

    // Transport controls applied to every live instance, so two phones animating together stay in
    // sync. Each returns false when nothing is active, letting MoveAsParticleTest1_v2 fall through
    // to its own animation.
    public static bool TogglePlayPauseAll()
    {
        List<MoveTempParticles> all = ActiveInstances();
        if (all.Count == 0) return false;

        // If anything is running, the button reads as "pause"; otherwise it starts everything.
        bool anyPlaying = all.Exists(t => t.IsPlaying);

        foreach (MoveTempParticles t in all)
        {
            if (anyPlaying) t.Pause();
            else t.Play();
        }
        return true;
    }

    public static bool RestartAll()
    {
        List<MoveTempParticles> all = ActiveInstances();
        if (all.Count == 0) return false;

        foreach (MoveTempParticles t in all) t.Restart();
        return true;
    }

    public static bool ToggleRaysAll()
    {
        List<MoveTempParticles> all = ActiveInstances();
        if (all.Count == 0) return false;

        foreach (MoveTempParticles t in all) t.ToggleRays();
        return true;
    }

    // Destroy every live instance (used when a task manager tears its state down).
    public static void StopAll()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (active[i] != null) active[i].Stop();
        }
        active.Clear();
    }

    // ------------------------------------------------------------------
    // Configuration
    // ------------------------------------------------------------------
    public Color32 viz_color = Color.blue;

    private float raySpeed = 2f;
    private float raySize = 0.08f;

    // ------------------------------------------------------------------
    // State
    // ------------------------------------------------------------------
    private List<RayPathSet_v2> paths;
    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;

    private int[] segmentIdx;           // this animator's own cursor along each path
    private float[] pathTotalLengths;   // total distance each particle travels
    private float[] startTime;          // when the current segment started, per path
    private float[] progressOnPause;    // elapsed segment time stored across a pause
    private bool[] completed;

    private bool isPaused = true;
    private bool completeFired;

    private LineRenderer[] rayLines;    // static rays, created on demand by ToggleRays()

    // ---- message rendering (mirrors MoveAsParticleTest1_v2) ----
    // Reference distance for the arrival weighting in AddMessageToEndpoint.
    private const float MESSAGE_DISTANCE_REF = 15f;

    private string message;
    private float messageFontSize = 1f;
    private float changeAlpha = 0.05f;
    private float changeSize = 0.05f;
    // Offset of the text riding on a particle, and of the message that accumulates where a path ends.
    // The endpoint offset is per-instance so two animations delivering to the same point (e.g. both
    // phones talking to the router) can stack their text instead of overlapping.
    private Vector3 particleTextOffset = new Vector3(0f, 0.1f, 0f);
    private Vector3 endpointTextOffset = new Vector3(0f, 0.5f, 0f); // Hard coded to mirror MoveAsParticleTest1_v2

    public Vector3 EndpointTextOffset
    {
        get => endpointTextOffset;
        set => endpointTextOffset = value;
    }

    private GameObject[] particleTextObjs;                  // text riding on each particle
    private readonly Dictionary<Vector3, GameObject> endpointAnchors = new Dictionary<Vector3, GameObject>();

    // Text carried by the particles and accumulated at the far end of each path.
    // Call before Play(). Passing null or "" disables message rendering.
    public void SetMessage(string message, float fontSize = 1f, float changeAlpha = 0.05f, float changeSize = 0.05f)
    {
        this.message = message;
        this.messageFontSize = fontSize;
        this.changeAlpha = changeAlpha;
        this.changeSize = changeSize;

        RebuildParticleTexts();
    }

    private bool HasMessage => !string.IsNullOrEmpty(message);

    public event Action OnComplete;

    public bool IsPlaying => !isPaused;

    public bool IsComplete
    {
        get
        {
            if (completed == null || completed.Length == 0) return false;
            foreach (bool c in completed) if (!c) return false;
            return true;
        }
    }

    // ------------------------------------------------------------------
    // Creation
    // ------------------------------------------------------------------
    // template: the ParticleSystem to clone (normally MoveAsParticleTest1_v2's particleSystem1),
    // so the temporary particles use the same material and renderer settings.
    public static MoveTempParticles Create(
        List<RayPathSet_v2> paths,
        ParticleSystem template,
        Color32 color,
        float raySpeed,
        float raySize)
    {
        if (paths == null || paths.Count == 0)
        {
            Debug.LogError("MoveTempParticles.Create: no paths supplied.");
            return null;
        }

        if (template == null)
        {
            Debug.LogError("MoveTempParticles.Create: no ParticleSystem template supplied.");
            return null;
        }

        GameObject host = new GameObject("TempParticles");
        MoveTempParticles temp = host.AddComponent<MoveTempParticles>();

        temp.paths = paths;
        temp.viz_color = color;
        temp.raySpeed = raySpeed;
        temp.raySize = raySize;

        // The main particle systems sit at the origin with identity rotation, and their particles are
        // simulated in local space - keep the clone at identity so path coordinates land in the same place.
        temp.ps = Instantiate(template, host.transform);
        temp.ps.name = "TempParticleSystem";
        temp.ps.transform.localPosition = Vector3.zero;
        temp.ps.transform.localRotation = Quaternion.identity;
        temp.ps.transform.localScale = Vector3.one;

        temp.InitializeParticles();

        active.Add(temp);
        return temp;
    }

    private void InitializeParticles()
    {
        int numRays = paths.Count;

        var main = ps.main;
        main.startSize = 0;
        main.maxParticles = numRays;

        // The clone must never spawn particles of its own - every particle here is placed by hand.
        var emission = ps.emission;
        emission.enabled = false;

        // The template's renderer may be switched off (MoveAsParticleTest1_v2.HideAllMovingRays parks
        // the main animation that way), and Instantiate copies that state. A temporary animation is
        // always meant to be seen, so turn it back on.
        Renderer psRenderer = ps.GetComponent<Renderer>();
        if (psRenderer != null) psRenderer.enabled = true;

        // Emit / read back / clear is how the base implementation sizes its particle array.
        particles = new ParticleSystem.Particle[numRays];
        ps.Emit(numRays);
        ps.GetParticles(particles);
        ps.Clear();

        // Without this the ParticleSystem keeps simulating and moves the particles by itself.
        ps.Pause();

        // Total length of each path, used to weight how much its arrival reinforces the message.
        pathTotalLengths = new float[numRays];
        for (int i = 0; i < numRays; i++)
        {
            List<Vector3> pts = paths[i].PathPositions;
            float total = 0f;

            if (pts != null)
                for (int j = 0; j < pts.Count - 1; j++) total += Vector3.Distance(pts[j], pts[j + 1]);

            pathTotalLengths[i] = total;
        }

        segmentIdx = new int[numRays];
        startTime = new float[numRays];
        progressOnPause = new float[numRays];
        completed = new bool[numRays];

        ResetToStart();
    }

    // Place every particle back on the first point of its path.
    private void ResetToStart()
    {
        Color32 c = viz_color;

        for (int i = 0; i < particles.Length; i++)
        {
            List<Vector3> pathPositions = paths[i].PathPositions;

            segmentIdx[i] = 0;
            progressOnPause[i] = 0f;
            completed[i] = false;
            startTime[i] = Time.time;

            ParticleSystem.Particle particle = particles[i];

            if (pathPositions != null && pathPositions.Count > 0)
            {
                particle.position = pathPositions[0];
                particle.startSize = raySize;
                particle.remainingLifetime = float.MaxValue;
            }
            else
            {
                particle.startSize = 0f;
                completed[i] = true;
                Debug.LogWarning($"MoveTempParticles: path {i} has no positions.");
            }

            particle.startColor = c;
            particles[i] = particle;
        }

        completeFired = false;
        ps.SetParticles(particles, particles.Length);

        RebuildParticleTexts();
        ClearEndpointAnchors();
    }

    // ------------------------------------------------------------------
    // Message text
    // ------------------------------------------------------------------
    // (Re)create the text object that rides along with each particle.
    private void RebuildParticleTexts()
    {
        DestroyParticleTexts();

        if (!HasMessage || particles == null) return;

        particleTextObjs = new GameObject[particles.Length];

        for (int i = 0; i < particles.Length; i++)
        {
            if (completed[i]) continue;

            particleTextObjs[i] = MakeText(particles[i].position + particleTextOffset, message, messageFontSize, 1f);

            // Hidden until the particle leaves its start point, so the messages do not stack up on
            // the emitting object before playback begins.
            particleTextObjs[i].SetActive(false);
        }
    }

    private GameObject MakeText(Vector3 position, string text, float fontSize, float alpha)
    {
        GameObject textObject = new GameObject("TempParticleMessage");
        textObject.transform.SetParent(transform, false);
        textObject.transform.position = position;

        textObject.AddComponent<FaceCamera>();

        TextMeshPro tmp = textObject.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;

        // White, like the demo's message text - viz_color drives the particles and rays, not the text.
        Color c = Color.white;
        c.a = alpha;
        tmp.color = c;

        return textObject;
    }

    // Accumulate the message above the end of a path: repeated arrivals make it brighter and bigger,
    // the same way MoveAsParticleTest1_v2.addMessageToRx builds the text up on the receiver.
    // pathDistance: total length of the path this arrival travelled. A ray that took a long way
    // round contributes less to how readable the message becomes.
    private void AddMessageToEndpoint(Vector3 endpoint, float pathDistance)
    {
        if (!HasMessage) return;

        float weight = Mathf.Max(MESSAGE_DISTANCE_REF - pathDistance, 1f);

        if (!endpointAnchors.TryGetValue(endpoint, out GameObject anchor) || anchor == null)
        {
            // A world-space anchor of our own: the Tx/Rx prefabs carry non-uniform scales that would
            // distort the text if it were parented to them.
            anchor = new GameObject("TempMessageAnchor");
            anchor.transform.SetParent(transform, false);
            anchor.transform.position = endpoint + endpointTextOffset;
            endpointAnchors[endpoint] = anchor;
        }

        TextMeshPro existing = anchor.GetComponentInChildren<TextMeshPro>();

        // Create it fully transparent, so this arrival and every later one contribute the same
        // weighted increment below.
        if (existing == null || existing.text != message)
        {
            GameObject textObject = MakeText(anchor.transform.position, message, messageFontSize, 0f);
            textObject.name = "TempMessageText";
            textObject.transform.SetParent(anchor.transform, true);

            existing = textObject.GetComponent<TextMeshPro>();
        }

        Color color = existing.color;
        color.a = Mathf.Clamp01(color.a + weight * changeAlpha);
        existing.color = color;

        // Guard the floor: a non-positive font size stops TextMeshPro rendering entirely.
        existing.fontSize = Mathf.Max(existing.fontSize + weight * changeSize, 0.01f);
    }

    private void DestroyParticleTexts()
    {
        if (particleTextObjs == null) return;

        foreach (GameObject go in particleTextObjs)
            if (go != null) Destroy(go);

        particleTextObjs = null;
    }

    private void ClearEndpointAnchors()
    {
        foreach (GameObject anchor in endpointAnchors.Values)
            if (anchor != null) Destroy(anchor);

        endpointAnchors.Clear();
    }

    // ------------------------------------------------------------------
    // Playback control
    // ------------------------------------------------------------------
    public void Play()
    {
        if (!isPaused) return;

        // Resume each path where it left off.
        float now = Time.time;
        for (int i = 0; i < startTime.Length; i++)
            startTime[i] = now - progressOnPause[i];

        isPaused = false;
    }

    public void Pause()
    {
        if (isPaused) return;

        float now = Time.time;
        for (int i = 0; i < startTime.Length; i++)
            progressOnPause[i] = now - startTime[i];

        isPaused = true;
    }

    public void TogglePlayPause()
    {
        if (isPaused) Play();
        else Pause();
    }

    public void Restart()
    {
        bool wasPlaying = !isPaused;
        isPaused = true;
        ResetToStart();
        if (wasPlaying) Play();
    }

    // Destroy this animation and everything it created.
    public void Stop()
    {
        active.Remove(this);
        if (this != null && gameObject != null) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        active.Remove(this);
        ClearRays();
    }

    // ------------------------------------------------------------------
    // Static rays
    // ------------------------------------------------------------------
    public void ToggleRays()
    {
        if (rayLines == null) ShowRays();
        else ClearRays();
    }

    public void ShowRays()
    {
        if (rayLines != null) return;

        rayLines = new LineRenderer[paths.Count];

        for (int i = 0; i < paths.Count; i++)
        {
            List<Vector3> pathPositions = paths[i].PathPositions;
            if (pathPositions == null || pathPositions.Count < 2) continue;

            GameObject lineObj = new GameObject($"TempPathLine_{i}");
            lineObj.transform.SetParent(transform, false);

            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.startWidth = 0.01f;
            line.endWidth = 0.01f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = viz_color;
            line.endColor = viz_color;
            line.useWorldSpace = true;
            line.numCapVertices = 3;
            line.numCornerVertices = 3;
            line.positionCount = pathPositions.Count;
            line.SetPositions(pathPositions.ToArray());

            rayLines[i] = line;
        }
    }

    public void ClearRays()
    {
        if (rayLines == null) return;

        foreach (LineRenderer line in rayLines)
            if (line != null) Destroy(line.gameObject);

        rayLines = null;
    }

    // ------------------------------------------------------------------
    // Animation
    // ------------------------------------------------------------------
    private void Update()
    {
        if (isPaused || particles == null) return;

        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem.Particle particle = particles[i];
            List<Vector3> pathPositions = paths[i].PathPositions;

            bool reachedEnd = false;

            if (pathPositions != null && pathPositions.Count > 1 && segmentIdx[i] < pathPositions.Count - 1)
            {
                Vector3 segStart = pathPositions[segmentIdx[i]];
                Vector3 segEnd = pathPositions[segmentIdx[i] + 1];

                float segDistance = Vector3.Distance(segStart, segEnd);
                float segDuration = (raySpeed <= 0 || segDistance < Mathf.Epsilon) ? 0f : segDistance / raySpeed;

                float t = (segDuration > 0)
                    ? Mathf.Clamp01((Time.time - startTime[i]) / segDuration)
                    : 1f;

                particle.position = Vector3.Lerp(segStart, segEnd, t);

                if (t >= 1.0f)
                {
                    particle.position = segEnd;
                    segmentIdx[i]++;

                    if (segmentIdx[i] < pathPositions.Count - 1)
                        startTime[i] = Time.time;   // start timing the next segment
                    else
                        reachedEnd = true;
                }
            }
            else if (pathPositions != null && pathPositions.Count > 0)
            {
                particle.position = pathPositions[pathPositions.Count - 1];
                reachedEnd = true;
            }

            // Keep this particle's message text riding along with it, revealing it once it has moved.
            if (particleTextObjs != null && i < particleTextObjs.Length && particleTextObjs[i] != null)
            {
                GameObject textObj = particleTextObjs[i];
                textObj.transform.position = particle.position + particleTextOffset;

                if (!textObj.activeSelf &&
                    pathPositions != null && pathPositions.Count > 0 &&
                    (particle.position - pathPositions[0]).sqrMagnitude > 0.0001f)
                {
                    textObj.SetActive(true);
                }
            }

            if (reachedEnd)
            {
                particle.startSize = 0f;
                particle.remainingLifetime = 0f;

                // Deliver the message once, on the frame this particle arrives.
                if (!completed[i] && pathPositions != null && pathPositions.Count > 0)
                    AddMessageToEndpoint(pathPositions[pathPositions.Count - 1], pathTotalLengths[i]);

                completed[i] = true;

                if (particleTextObjs != null && i < particleTextObjs.Length && particleTextObjs[i] != null)
                {
                    Destroy(particleTextObjs[i]);
                    particleTextObjs[i] = null;
                }
            }

            particles[i] = particle;
        }

        ps.SetParticles(particles, particles.Length);

        if (!completeFired && IsComplete)
        {
            completeFired = true;
            isPaused = true;
            OnComplete?.Invoke();
        }
    }

    // ------------------------------------------------------------------
    // Path utilities
    // ------------------------------------------------------------------
    // Returns a copy of the supplied paths with every polyline reversed:
    // a path p1 -> p2 -> p3 becomes p3 -> p2 -> p1, so the animation runs Rx -> Tx.
    // The source list and its RayPathSet_v2 objects are left untouched.
    public static List<RayPathSet_v2> ReversePaths(List<RayPathSet_v2> source)
    {
        List<RayPathSet_v2> reversed = new List<RayPathSet_v2>();
        if (source == null) return reversed;

        foreach (RayPathSet_v2 path in source)
        {
            // NOTE: RayPathSet_v2 derives from MonoBehaviour but the loader creates it with `new`,
            // so it has no native object behind it and Unity's overloaded `==` reports it as null.
            // ReferenceEquals is the only way to test these for real null.
            if (ReferenceEquals(path, null)) continue;

            RayPathSet_v2 copy = new RayPathSet_v2
            {
                RxNum = path.RxNum,
                PowerNum = path.PowerNum,
                Interaction_Description = path.Interaction_Description,
                Total_Interactions_for_Path = path.Total_Interactions_for_Path,
                TotalPowerNum = path.TotalPowerNum,
                PathPositionsIdx = 0,
            };

            if (path.PathPositions != null)
            {
                copy.PathPositions = new List<Vector3>(path.PathPositions);
                copy.PathPositions.Reverse();
            }

            reversed.Add(copy);
        }

        return reversed;
    }
}
