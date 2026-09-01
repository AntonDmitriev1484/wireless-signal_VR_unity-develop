/*
 *  Lesson 1 task manager (runs after DemoTaskManager completes).
 *
 *  Three sequential MCQ learning tasks, two counterbalanced sets:
 *      phone_optimization -> los_creation -> reflection_creation
 *  Dataset per condition:  <task>_<set>_<option>.csv (+ _heatmap.csv) in Assets/Resources,
 *  loaded through MoveAsParticleTest1_v2.SetCurrentDataSet().
 *
 *  FSM:  SetSelect -> [Intro] -> MCQ -> PromptExplanation -> ExplainCorrect -> next task ... -> Complete
 *                                                          -> ExplainWrong  -> same MCQ
 *  Selecting an option only previews its dataset; Next commits the answer.
 */

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Task1Manager : ITaskManager
{
    // ------------------------------------------------------------------
    // Declarative task table
    // ------------------------------------------------------------------
    private class TaskDef
    {
        public string name;
        public string prompt;
        public string intro;            // optional intro screen before the MCQ
        public bool hasCabinet;         // true -> show 4 cabinet models, false -> 4 phone-location markers
        public bool phoneReceiver;      // true -> the receiver is the participant's phone, not the TV antenna
        public Dictionary<int, char> correct = new();                 // set -> correct letter
        public Dictionary<int, string> correctText = new();           // set -> explanation
        public Dictionary<(int set, char opt), string> wrongText = new(); // (set, letter) -> explanation
    }

    private static readonly char[] LETTERS = { 'A', 'B', 'C', 'D' };

    // Rx_Number of the phone in phone_optimization - the receiver the options move.
    private const int PHONE_RX = 1;

    private static readonly TaskDef[] TASKS =
    {
        new TaskDef
        {
            name = "phone_optimization",
            prompt = "You're using your phone. Which one of these locations will give you the best signal strength?",
            hasCabinet = false,
            phoneReceiver = true,       // the receiver being moved is the phone; los/reflection use the TV
            correct = { { 1, 'B' }, { 2, 'D' } },
            correctText =
            {
                { 1, "Correct! This spot is in the same room as the router with a direct line-of-sight path, so the signal is strongest here. " +
                     "The other locations are behind walls, which weaken the signal a lot." },
                { 2, "Correct! This spot is in the same room as the router with a direct line-of-sight path, so the signal is strongest here. " +
                     "The other locations are all behind a wall, which weakens the signal a lot." },
            },
            wrongText =
            {
                { (1, 'A'), "Not quite. This spot is close to the router, but the kitchen wall is in the way. Walls weaken the signal - look for a location that can see the router directly." },
                { (1, 'C'), "Not quite. This spot is over in the office, behind the dividing wall, so only weak signals reach it. Look for a location that can see the router directly." },
                { (1, 'D'), "Not quite. This is the far corner of the office - behind the dividing wall and the furthest of all four spots from the router. Look for a location that can see the router directly." },
                { (2, 'A'), "Not quite. This spot is over by the TV in the living room, behind the dividing wall, so the signal is weak. Look for a location that can see the router directly." },
                { (2, 'B'), "Not quite. The kitchen is behind the kitchen wall - a little signal leaks through the doorway, but it is still weak. Look for a location that can see the router directly." },
                { (2, 'C'), "Not quite. This spot is far from the router and behind the dividing wall, so only weak signals reach it. Look for a location that can see the router directly." },
            },
        },
        new TaskDef
        {
            name = "los_creation",
            prompt = "The TV isn't getting a very strong signal - the furniture and walls are all impacting it. " +
                     "Where can you put this cabinet so that the TV gets the strongest signal?",
            hasCabinet = true,
            correct = { { 1, 'C' }, { 2, 'A' } },
            correctText =
            {
                { 1, "Correct! The cabinet is off to the side of the TV, against the same wall, so it never crosses the line between the router and the TV. " +
                     "The direct path stays open and the TV keeps a strong signal - anything standing on that line blocks it." },
                { 2, "Correct! The cabinet is off to the side of the TV, against the same wall, so it never crosses the line between the router and the TV. " +
                     "The direct path stays open, and the cabinet even bounces an extra path toward the TV - anything standing on that line would block it instead." },
            },
            wrongText =
            {
                { (1, 'A'), "Not quite. The cabinet stands on the line between the router and the TV, about two metres short of it, so the direct path is blocked. Only weaker signals that bounce off the walls reach the TV. Try a spot that is out of that line." },
                { (1, 'B'), "Not quite. The cabinet is right in front of the TV, blocking the direct path from the router. Only weaker reflected signals reach the TV. Try a spot that is out of that line." },
                { (1, 'D'), "Not quite. The cabinet is further back, just in front of the couch, but it is still on the line between the router and the TV, so the direct path is blocked. Try a spot that is out of that line." },
                { (2, 'B'), "Not quite. The cabinet stands on the line between the router and the TV, a couple of metres short of it, so the direct path is blocked. Only weaker signals that bounce off the walls reach the TV. Try a spot that is out of that line." },
                { (2, 'C'), "Not quite. The cabinet is about halfway along the line between the router and the TV, so it still blocks the direct path. Try a spot that is out of that line." },
                { (2, 'D'), "Not quite. The cabinet is over near the router, but it is still on the line to the TV - it blocks the direct path right where the signal starts out. Try a spot that is out of that line." },
            },
        },
        new TaskDef
        {
            name = "reflection_creation",
            intro = "Someone glued your router to the table in the other room, so now you're getting a bad signal again. " +
                    "You can't move it back, but you might not need to in order to get a good signal.",
            prompt = "Some materials, like metal, can reflect signals! It seems like placing this metal cabinet somewhere can improve the signal strength at the TV. " +
                     "Can you place the cabinet so that the TV gets the best signal strength?" 
                    ,
            hasCabinet = true,
            correct = { { 1, 'D' }, { 2, 'B' } },

            correctText =
            {
                { 1,  MoveAsParticleTest1_v2.HighlightSubstrings("Correct! The metal cabinet acts like a mirror: the signal from the router bounces off it, travels through the doorway and reaches the TV. " +
                     "That reflected path is now the strongest one arriving at the TV, which is why the signal improves so much.", new List<string> { "reflected path" }) },
                { 2, MoveAsParticleTest1_v2.HighlightSubstrings("Correct! The metal cabinet acts like a mirror: the signal from the router bounces off it, travels through the doorway and reaches the TV. " +
                     "That reflected path is now the strongest one arriving at the TV, which is why the signal improves so much.", new List<string> { "reflected path" }) }
            },
            wrongText =
            {
                { (1, 'A'), "Not quite. Over in the far corner of the office the cabinet faces the wrong way - its reflection goes off to the side instead of through the doorway. Try to place it so the router 'mirrors' onto the TV." },
                { (1, 'B'), "Not quite. Here the cabinet stands in the doorway and blocks it, instead of reflecting the signal through it. Try to place it so the router 'mirrors' onto the TV." },
                { (1, 'C'), "Not quite. Right next to the TV the cabinet has nothing useful to reflect - the signal still has to get through the wall to reach it. Try to place it so the router 'mirrors' onto the TV." },
                { (2, 'A'), "Not quite. In the corner of the living room next to the TV the cabinet has nothing useful to reflect - the signal still has to get through the wall to reach it. Try to place it so the router 'mirrors' onto the TV." },
                { (2, 'C'), "Not quite. On the office side of the doorway the cabinet does not aim the router's signal through it. Try to place it so the router 'mirrors' onto the TV." },
                { (2, 'D'), "Not quite. Back in the office beside the router, the cabinet has no angle on the doorway, so no strong bounce reaches the TV. Try to place it so the router 'mirrors' onto the TV." },
            },
        },
    };

    private static string ConditionName(string task, int set, char option) => $"{task}_{set}_{option}";

    // Option previewed when a task is entered. Derived from the answer key so the correct layout is
    // never shown as the default (in the v2 data, A is the correct answer for los_creation set 2).
    private static char PreviewOption(TaskDef t, int set) => t.correct[set] == 'A' ? 'B' : 'A';

    // ------------------------------------------------------------------
    // State
    // ------------------------------------------------------------------
    private enum State { SetSelect, Intro, MCQ, PromptExplanation, ExplainCorrect, ExplainTransmission, ExplainWrong, Complete }

    private State state;
    private bool pendingRender;     // DoState() only re-renders after a state change

    private readonly MoveAsParticleTest1_v2 m;

    private int currentSet = 0;     // 1 or 2 once chosen
    private int taskIdx = 0;
    private int selected = -1;      // 0..3 selected option, -1 none
    private int attempt = 0;

    // Held between the MCQ and the explanation: the "Can you explain your answer?" screen sits in
    // between, so the branch has to be decided when the answer is committed and used a state later.
    private bool answerWasCorrect;

    private TaskDef Task => TASKS[taskIdx];

    // Candidate objects shown for the current MCQ (index = option): a cabinet mesh where the
    // furniture moves, or a transparent option-coloured cube where the receiver moves.
    private readonly GameObject[] candidates = new GameObject[4];
    private readonly GameObject[] candidateBadges = new GameObject[4];   // coloured letter above each cube
    private bool candidatesSpawned;
    private Material[] normalMats;
    private Material[] activeMats;
    private Material[] cubeMats;
    private Material[] cubeSelectedMats;

    // answer button bookkeeping
    private readonly Button[] buttons = new Button[4];
    private readonly Image[] buttonImages = new Image[4];
    private readonly TextMeshProUGUI[] buttonLabels = new TextMeshProUGUI[4];
    private readonly Color[] buttonOrigColors = new Color[4];

    public bool IsComplete => state == State.Complete;

    public Task1Manager(MoveAsParticleTest1_v2 m)
    {
        this.m = m;

        GameObject[] btnObjs = m.AnswerButtons;
        for (int i = 0; i < 4; i++)
        {
            buttons[i] = btnObjs[i].GetComponent<Button>();
            buttonImages[i] = btnObjs[i].GetComponent<Image>();
            buttonLabels[i] = btnObjs[i].GetComponentInChildren<TextMeshProUGUI>(true);
            buttonOrigColors[i] = buttonImages[i] != null ? buttonImages[i].color : Color.white;
        }

        BuildOptionMaterials();
        ValidateDatasets();

        state = State.SetSelect;
        pendingRender = true;
    }

    // Translucent (normal) and opaque (selected) versions of the four option colours.
    private void BuildOptionMaterials()
    {
        Material[] src = m.OptionMaterials;
        normalMats = new Material[4];
        activeMats = new Material[4];
        cubeMats = new Material[4];
        cubeSelectedMats = new Material[4];
        for (int i = 0; i < 4; i++)
        {
            normalMats[i] = src[i];
            activeMats[i] = CandidateMarkers.Tint(src[i], OptionColor(i), 1f);

            // Receiver-location cubes are plain white and very faint for every option - the colour
            // is carried by the letter badge above them instead. src[i] is only borrowed for its
            // transparent shader settings; the colour is overridden.
            cubeMats[i] = CandidateMarkers.Tint(src[i], CandidateMarkers.CUBE_COLOR, CandidateMarkers.ALPHA_NORMAL);
            cubeSelectedMats[i] = CandidateMarkers.Tint(src[i], CandidateMarkers.CUBE_COLOR, CandidateMarkers.ALPHA_SELECTED);
        }
    }

    private Color OptionColor(int i)
    {
        Material mat = m.OptionMaterials[i];
        return mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
    }

    // Log any missing dataset / model up front instead of failing mid-session.
    // Only the small path CSVs are probed (heatmaps are ~2 MB each and would stay cached).
    private void ValidateDatasets()
    {
        foreach (TaskDef t in TASKS)
            for (int set = 1; set <= 2; set++)
                foreach (char opt in LETTERS)
                {
                    string cond = ConditionName(t.name, set, opt);
                    TextAsset ta = Resources.Load<TextAsset>(cond);
                    if (ta == null) Debug.LogError($"Task1Manager: dataset missing in Resources: {cond}");
                    else Resources.UnloadAsset(ta);
                    if (t.hasCabinet && Resources.Load<GameObject>(CabinetResource(cond)) == null)
                        Debug.LogError($"Task1Manager: cabinet model missing in Resources: {CabinetResource(cond)}");
                }
    }

    private static string CabinetResource(string cond) => $"Models/Lesson1/{cond}_cabinet";

    // ------------------------------------------------------------------
    // ITaskManager
    // ------------------------------------------------------------------
    // ------------------------------------------------------------------
    // Back
    // ------------------------------------------------------------------
    // Everything Back has to put back. The lesson walks three tasks and two sets, so a step back can
    // cross a task boundary - the option and the task have to travel with the state, not just the
    // state itself.
    private struct Snapshot
    {
        public State state;
        public int taskIdx;
        public int currentSet;
        public int selected;
        public int attempt;
        public bool answerWasCorrect;

        public bool Matches(Snapshot other) =>
            state == other.state && taskIdx == other.taskIdx && currentSet == other.currentSet &&
            selected == other.selected && attempt == other.attempt &&
            answerWasCorrect == other.answerWasCorrect;
    }

    // Visited states, most recent last. Empty means the participant is on this lesson's first
    // screen, which is as far back as Back goes: it never returns to the demo.
    private readonly List<Snapshot> history = new List<Snapshot>();

    private Snapshot Capture() => new Snapshot
    {
        state = state,
        taskIdx = taskIdx,
        currentSet = currentSet,
        selected = selected,
        attempt = attempt,
        answerWasCorrect = answerWasCorrect,
    };

    public void Advance()
    {
        // Recorded around the transition rather than inside SetState: GoToTaskStart moves the task
        // and the option before it sets a state, so a snapshot taken there would already describe
        // where the participant is going instead of where they were.
        Snapshot before = Capture();

        AdvanceInternal();

        // A press that changed nothing - Next on an MCQ with no option chosen - leaves no step to
        // undo, so it must not push one.
        if (!before.Matches(Capture())) history.Add(before);
    }

    public void Back()
    {
        if (history.Count == 0) return;

        Snapshot previous = history[history.Count - 1];
        history.RemoveAt(history.Count - 1);

        Restore(previous);
    }

    // Rebuilds the world for a state being returned to. The screens are not independent - they hide
    // candidates, raise rays and swap datasets - so a step back has to undo all of that and let
    // DoState build the state again from a clean slate.
    private void Restore(Snapshot s)
    {
        taskIdx = s.taskIdx;
        currentSet = s.currentSet;
        selected = s.selected;
        attempt = s.attempt;
        answerWasCorrect = s.answerWasCorrect;

        // Candidates are dropped rather than re-shown: ExplainCorrect hides all but the right one,
        // and a later task's candidates belong to a different dataset entirely. DoState respawns
        // them for whichever task is now current.
        ClearCandidates();

        m.ClearRayHighlights();
        m.ClearDirectLine();
        m.Highlighter?.ClearAllHighlights();

        // Both have to go before the load, which would otherwise restore them for the new dataset.
        m.ClearHeatmap();
        m.HideMainRays();

        // No dataset until a set has been chosen - SetSelect runs before there is one.
        if (currentSet > 0)
        {
            m.SetReceiverModel(Task.phoneReceiver
                ? MoveAsParticleTest1_v2.ReceiverModel.Phone
                : MoveAsParticleTest1_v2.ReceiverModel.Antenna);

            char option = selected >= 0 ? LETTERS[selected] : PreviewOption(Task, currentSet);
            m.SetCurrentDataSet(ConditionName(Task.name, currentSet, option));
        }

        SetState(s.state);
    }

    private void AdvanceInternal()
    {
        switch (state)
        {
            case State.SetSelect:
                if (selected < 0) return;                       // nothing chosen -> Next ignored
                currentSet = selected + 1;
                m.LogAnswer($"Lesson1: set {currentSet} selected");
                selected = -1;
                taskIdx = 0;
                GoToTaskStart();
                break;

            case State.Intro:
                m.ClearHeatmap(); // Before entering an MCQ clear the heatmap
                SetState(State.MCQ);
                break;

            case State.MCQ:
                if (selected < 0) return;                       // Next without a selection does not advance
                {
                    char chosen = LETTERS[selected];
                    bool correct = chosen == Task.correct[currentSet];
                    attempt++;
                    m.LogAnswer($"Lesson1: {Task.name} set {currentSet} attempt {attempt} answer {chosen} -> {(correct ? "correct" : "incorrect")}");
                    TrialLog.Answer(nameof(Task1Manager), Task.name, currentSet, chosen, true);

                    // Ask them to talk through it before showing whether it was right.
                    answerWasCorrect = correct;
                    SetState(State.PromptExplanation);
                }
                break;

            case State.PromptExplanation:
                SetState(answerWasCorrect ? State.ExplainCorrect : State.ExplainWrong);
                break;

            case State.ExplainCorrect:
                // reflection_creation has one more screen to show before it hands on, and its text
                // points at the cabinet the participant just placed - so the candidates stay on
                // screen and the task is not advanced until that screen is done.
                if (Task.name == "reflection_creation")
                {
                    SetState(State.ExplainTransmission);
                    break;
                }


                GoToNextTask();
                break;

            case State.ExplainTransmission:
                GoToNextTask();
                break;
            case State.ExplainWrong:
                // Retry the same MCQ. The dataset still shows the option that was just rejected, so
                // keep it selected - the highlight must always match what is on screen.
                SetState(State.MCQ);
                break;

            case State.Complete:
                break;
        }
    }

    public void DoState()
    {
        if (!pendingRender) return;
        pendingRender = false;

        switch (state)
        {
            case State.SetSelect:
                m.Highlighter?.ClearAllHighlights();
                m.QuestionText.text = "Please select the set your researcher gave you, then press Next.";
                ShowButtons(2);
                SetButtonLabels(new[] { "Set 1", "Set 2" });
                ClearButtonHighlights();
                break;

            case State.Intro:
                m.QuestionText.text = Task.intro;
                ShowButtons(0);
                break;

            case State.MCQ:
                // Strips the green/thick styling if any is left over - it does NOT take the ray
                // lines down. Hiding them belongs to GoToTaskStart, which has to do it before the
                // dataset load or SetCurrentDataSet simply redraws them.
                m.ClearRayHighlights();
                m.QuestionText.text = Task.prompt;
                ShowButtons(4);
                SetButtonLabels(new[] { "A", "B", "C", "D" });
                ClearButtonHighlights();
                if (!candidatesSpawned) SpawnCandidates();
                if (selected >= 0) HighlightOption(selected);

                HighlightActiveReceiver();

                break;

            case State.PromptExplanation:
                // Asked before the answer is revealed, so what they say is their own reasoning
                // rather than a read-back of the explanation.
                m.QuestionText.text = "Can you explain your answer?";
                ShowButtons(0);
                break;

            case State.ExplainCorrect:
                m.QuestionText.text = Task.correctText[currentSet];
                ShowButtons(0);

                // Back drops the candidates when it steps into this state, and the MCQ may never
                // have run in this session - so spawn them here rather than assuming the question
                // already did.
                if (!candidatesSpawned) SpawnCandidates();

                // Clear the room down to the placement being explained: the three rejected cabinets
                // would otherwise still be standing there while the text talks about the one that works.
                ShowOnlyCorrectCandidate();

                // The explanation is about the paths, so they have to be on screen. Already-on stays on.
                if (Task.name == "reflection_creation")
                {

                    m.ShowMainRays();
                    HighlightReflectionRays_CreateReflection();
                }
                else if (Task.name == "los_creation")
                {
                    m.ShowMainRays();
                    //HighlightLoS_CreateReflection();
                }

                break;

            case State.ExplainTransmission:

                m.QuestionText.text =
                    MoveAsParticleTest1_v2.HighlightSubstrings(
                    "Some materials like walls, can let signal through, but absorb part of it, and weaken it substantially. " +
                    "This wall absorbs a lot of the signal, and only has a single strong path that passes directly through it.",
                    new List<string> { "single strong path", "directly" }
                    );

                ShowButtons(0);

                // Drop the cabinet reflections ExplainCorrect highlighted, so the only thing picked
                // out here is the straight router-to-TV line: the route the signal cannot take.
                m.ClearRayHighlights();
                HighlightLoS_CreateReflection();
                break;

            case State.ExplainWrong:
                m.QuestionText.text = Task.wrongText.TryGetValue((currentSet, LETTERS[selected]), out string txt)
                    ? txt
                    : "Not quite. Look at the rays and heatmap and try another option.";
                ShowButtons(0);
                break;

            case State.Complete:
                m.QuestionText.text = "Lesson 1 complete! Thank you. Please wait for the researcher.";
                ShowButtons(0);
                break;
        }
    }

    public void OnAnswerSelected(int answerIdx)
    {
        if (answerIdx < 0 || answerIdx > 3) return;

        if (state == State.SetSelect)
        {
            if (answerIdx > 1) return;
            selected = answerIdx;
            ClearButtonHighlights();
            HighlightButton(answerIdx, Color.white);
            return;
        }

        if (state != State.MCQ) return;

        selected = answerIdx;

        // Logged at the press, not at Next: pressing an option previews its dataset, so this is the
        // record of what the participant looked at. Advance() logs the one they settle on.
        TrialLog.Answer(nameof(Task1Manager), Task.name, currentSet, LETTERS[answerIdx], false);

        // Drop the ring before the load: SetCurrentDataSet destroys and respawns the Rx markers,
        // and ObjectHighlighter keys its circles on those GameObjects - a stale key would leave the
        // previous answer's circle frozen on screen next to the new one.
        m.Highlighter?.ClearAllHighlights();

        m.SetCurrentDataSet(ConditionName(Task.name, currentSet, LETTERS[answerIdx]));

        HighlightOption(answerIdx);
        HighlightActiveReceiver();
    }

    // Ring the receiver the selected option currently places, so the phone on the floor is tied to
    // the chosen answer. Only the phone-placement task moves a receiver; in the cabinet tasks the
    // receiver is the fixed TV, so there is nothing to point at.
    private void HighlightActiveReceiver()
    {
        if (Task.hasCabinet) return;

        ObjectHighlighter h = m.Highlighter;
        if (h == null) return;

        h.ClearAllHighlights();
        h.SetHighlighted(m.RxMarkerFor(PHONE_RX), true, Color.black);
    }

    // ------------------------------------------------------------------
    // reflection_creation ray highlights
    // ------------------------------------------------------------------
    // Both draw over the correct option's dataset, so call them once that dataset is loaded - the
    // explanation screen after a correct answer. Both work for either set and do nothing on the
    // other two tasks. Highlighted rays are drawn green at 3x the normal thickness
    // (MoveAsParticleTest1_v2.RAY_HIGHLIGHT_COLOR / RAY_HIGHLIGHT_WIDTH_SCALE), and turn the ray
    // view on if it is off, since otherwise there is nothing to highlight.

    // How far outside the cabinet's mesh an interaction point may sit and still count as a bounce
    // off it. Sionna places the point on the face; this only absorbs float error and the OBJ's
    // slightly irregular hull.
    private const float CABINET_HIT_MARGIN = 0.15f;

    // Every path that reflects off the metal cabinet in the correct option - the mirror effect this
    // task teaches. The CSV labels every interaction just "I", so which surface was hit has to come
    // from geometry: a path counts when one of its interaction points lands inside the cabinet.
    public void HighlightReflectionRays_CreateReflection()
    {
        if (Task.name != "reflection_creation") return;

        GameObject cabinet = CorrectCandidate();
        if (cabinet == null) return;

        Renderer[] renderers = cabinet.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("Task1Manager: the correct cabinet has no renderers to bound.");
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        bounds.Expand(CABINET_HIT_MARGIN * 2f);   // Expand takes the total growth per axis

        m.HighlightRays(path => BouncesInside(path, bounds));

        if (m.HighlightedRayCount == 0)
            Debug.LogWarning($"Task1Manager: no path bounces off the cabinet in {ConditionName(Task.name, currentSet, Task.correct[currentSet])}.");
    }

    // The line of sight from the router to the TV. In this task there is none - the router is in the
    // other room and the wall blocks the direct path, which is exactly why the cabinet's reflection
    // matters - so no CSV path can be highlighted. The straight router-to-TV line is drawn instead,
    // showing the route the signal cannot take.
    public void HighlightLoS_CreateReflection()
    {
        if (Task.name != "reflection_creation") return;

        m.ShowDirectLine();
    }

    // Does any of the path's interactions land inside the given volume? The first and last points
    // are the router and the TV, so only the ones in between can be a reflection.
    private static bool BouncesInside(RayPathSet_v2 path, Bounds bounds)
    {
        for (int i = 1; i < path.PathPositions.Count - 1; i++)
            if (bounds.Contains(path.PathPositions[i])) return true;

        return false;
    }

    // The candidate object spawned for this set's correct option.
    private GameObject CorrectCandidate()
    {
        int idx = Array.IndexOf(LETTERS, Task.correct[currentSet]);

        if (idx < 0 || candidates[idx] == null)
        {
            Debug.LogWarning("Task1Manager: the correct option's candidate object is not spawned.");
            return null;
        }

        return candidates[idx];
    }

    // Leave only the correct option's cabinet standing, letter badge included. Cabinet tasks only:
    // in phone_optimization the markers are floor cubes for locations the phone could occupy, and
    // the explanation reads better with all four still visible. Deactivated rather than destroyed,
    // so ClearCandidates still has them to tear down when the task ends.
    private void ShowOnlyCorrectCandidate()
    {
        if (!Task.hasCabinet) return;

        int correctIdx = Array.IndexOf(LETTERS, Task.correct[currentSet]);

        for (int i = 0; i < 4; i++)
        {
            bool keep = i == correctIdx;

            if (candidates[i] != null) candidates[i].SetActive(keep);
            if (candidateBadges[i] != null) candidateBadges[i].SetActive(keep);
        }
    }

    // ------------------------------------------------------------------
    // Task / state helpers
    // ------------------------------------------------------------------
    private void SetState(State s)
    {
        state = s;
        pendingRender = true;
    }

    // Leave the task just finished: its candidate objects go, and either the next task starts or
    // the lesson ends.
    private void GoToNextTask()
    {
        ClearCandidates();
        taskIdx++;

        if (taskIdx >= TASKS.Length) SetState(State.Complete);
        else GoToTaskStart();
    }

    private void GoToTaskStart()
    {
        attempt = 0;
        // Rings belong to the outgoing task's markers, which the load below destroys.
        m.Highlighter?.ClearAllHighlights();


        // Start on an incorrect option so there is something to look at without giving the answer
        // away, and present it as the current choice: the receiver is standing in that option's
        // marker, so its button and object must be emphasised to match.
        char preview = PreviewOption(Task, currentSet);
        selected = Array.IndexOf(LETTERS, preview);

        // Choose the receiver prefab before loading: the markers are spawned during the load.
        m.SetReceiverModel(Task.phoneReceiver
            ? MoveAsParticleTest1_v2.ReceiverModel.Phone
            : MoveAsParticleTest1_v2.ReceiverModel.Antenna);

        // Start each task with the heatmap down. This has to happen BEFORE the load:
        // SetCurrentDataSet snapshots whether the heatmap is showing and re-creates it afterwards,
        // so a clear placed after this line would simply be undone. Leaving a correct/wrong
        // response with the heatmap on would otherwise carry it into the next task's question.
        m.ClearHeatmap();

        // Same for the rays, and for the same reason: SetCurrentDataSet remembers that they were
        // showing and redraws them for the incoming dataset. ExplainCorrect turns them on for the
        // cabinet tasks, so without this they would follow the participant into the next question.
        m.HideMainRays();

        m.SetCurrentDataSet(ConditionName(Task.name, currentSet, preview));
        SetState(string.IsNullOrEmpty(Task.intro) ? State.MCQ : State.Intro);
    }

    // ------------------------------------------------------------------
    // Candidate objects (cabinets or phone locations), one per option
    // ------------------------------------------------------------------
    private void SpawnCandidates()
    {
        candidatesSpawned = true;
        for (int i = 0; i < 4; i++)
        {
            string cond = ConditionName(Task.name, currentSet, LETTERS[i]);

            if (Task.hasCabinet)
            {
                // The cabinet mesh itself carries the option colour.
                GameObject cabinet = SpawnCabinet(cond);
                if (cabinet == null) continue;

                cabinet.name = $"Candidate_{cond}";
                ApplyMaterial(cabinet, normalMats[i]);
                candidates[i] = cabinet;

                // Letter above the cabinet's top face. Spawned as a separate root object, not a
                // child - ApplyMaterial() walks children, and would otherwise repaint the badge
                // with the cabinet's translucent material every time the selection changes.
                candidateBadges[i] = CandidateMarkers.SpawnBadgeAbove(
                    cabinet, LETTERS[i], OptionColor(i), m.OptionMaterials[i], $"Badge_{cond}");
            }
            else
            {
                // Candidate phone locations get a transparent cube in the option's colour.
                if (!CandidateMarkers.TryReadRxPosition(cond, 1, out Vector3 pos))
                {
                    Debug.LogError($"Task1Manager: could not read Rx position from {cond}");
                    continue;
                }

                //candidates[i] = CandidateMarkers.SpawnCube(pos, cubeMats[i], $"Candidate_{cond}");

                candidateBadges[i] = CandidateMarkers.SpawnBadge(
                    pos, LETTERS[i], OptionColor(i), m.OptionMaterials[i], $"Badge_{cond}");
            }
        }
    }

    private GameObject SpawnCabinet(string cond)
    {
        GameObject prefab = Resources.Load<GameObject>(CabinetResource(cond));
        if (prefab == null)
        {
            Debug.LogError($"Task1Manager: cabinet model not found: {CabinetResource(cond)}");
            return null;
        }
        // OBJ is already in Unity coordinates after import; no transform applied.
        return UnityEngine.Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
    }

    private static void ApplyMaterial(GameObject go, Material mat)
    {
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }
    }

    private void ClearCandidates()
    {
        candidatesSpawned = false;
        for (int i = 0; i < 4; i++)
        {
            if (candidates[i] != null) UnityEngine.Object.Destroy(candidates[i]);
            if (candidateBadges[i] != null) UnityEngine.Object.Destroy(candidateBadges[i]);

            candidates[i] = null;
            candidateBadges[i] = null;
        }
    }

    // Highlight option i: opaque material on its object, tinted button; others back to normal.
    private void HighlightOption(int sel)
    {
        for (int i = 0; i < 4; i++)
        {
            if (candidates[i] == null) continue;

            ApplyMaterial(candidates[i], Task.hasCabinet
                ? (i == sel ? activeMats[i] : normalMats[i])
                : (i == sel ? cubeSelectedMats[i] : cubeMats[i]));
        }

        ClearButtonHighlights();
        HighlightButton(sel, OptionColor(sel));
    }

    // ------------------------------------------------------------------
    // Answer button helpers
    // ------------------------------------------------------------------
    private void ShowButtons(int count)
    {
        GameObject[] objs = m.AnswerButtons;
        for (int i = 0; i < 4; i++)
            if (objs[i] != null) objs[i].SetActive(i < count);
    }

    private void SetButtonLabels(string[] labels)
    {
        for (int i = 0; i < labels.Length && i < 4; i++)
            if (buttonLabels[i] != null) buttonLabels[i].text = labels[i];
    }

    private void HighlightButton(int i, Color tint)
    {
        if (buttonImages[i] == null) return;
        Color c = tint; c.a = 1f;
        buttonImages[i].color = c;
    }

    private void ClearButtonHighlights()
    {
        for (int i = 0; i < 4; i++)
        {
            if (buttonImages[i] == null) continue;
            // In an MCQ the unselected buttons keep a faint version of their option colour.
            if (state == State.MCQ)
            {
                Color c = OptionColor(i); c.a = buttonOrigColors[i].a;
                buttonImages[i].color = c;
            }
            else
            {
                buttonImages[i].color = buttonOrigColors[i];
            }
        }
    }
}
