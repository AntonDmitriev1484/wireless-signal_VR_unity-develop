/*
 *  Lesson 1 task manager (runs after DemoTaskManager completes).
 *
 *  Three sequential MCQ learning tasks, two counterbalanced sets:
 *      phone_optimization -> los_creation -> reflection_creation
 *  Dataset per condition:  <task>_<set>_<option>.csv (+ _heatmap.csv) in Assets/Resources,
 *  loaded through MoveAsParticleTest1_v2.SetCurrentDataSet().
 *
 *  FSM:  SetSelect -> [Intro] -> MCQ -> ExplainCorrect -> next task ... -> Complete
 *                                   \-> ExplainWrong  -> same MCQ
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
        public Dictionary<int, char> correct = new();                 // set -> correct letter
        public Dictionary<int, string> correctText = new();           // set -> explanation
        public Dictionary<(int set, char opt), string> wrongText = new(); // (set, letter) -> explanation
    }

    private static readonly char[] LETTERS = { 'A', 'B', 'C', 'D' };

    private static readonly TaskDef[] TASKS =
    {
        new TaskDef
        {
            name = "phone_optimization",
            prompt = "You're using your phone. Which one of these locations will give you the best signal strength?",
            hasCabinet = false,
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
            prompt = "Some materials, like metal, can actually reflect signals! It seems like placing this metal cabinet somewhere actually improves the signal strength at the TV. " +
                     "Can you place the cabinet so that the TV gets the best signal strength?",
            hasCabinet = true,
            correct = { { 1, 'D' }, { 2, 'B' } },
            correctText =
            {
                { 1, "Correct! The metal cabinet acts like a mirror: the signal from the router bounces off it, travels through the doorway and reaches the TV. " +
                     "That reflected path is now the strongest one arriving at the TV, which is why the signal improves so much." },
                { 2, "Correct! The metal cabinet acts like a mirror: the signal from the router bounces off it, travels through the doorway and reaches the TV. " +
                     "That reflected path is now the strongest one arriving at the TV, which is why the signal improves so much." },
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
    private enum State { SetSelect, Intro, MCQ, ExplainCorrect, ExplainWrong, Complete }

    private State state;
    private bool pendingRender;     // DoState() only re-renders after a state change

    private readonly MoveAsParticleTest1_v2 m;

    private int currentSet = 0;     // 1 or 2 once chosen
    private int taskIdx = 0;
    private int selected = -1;      // 0..3 selected option, -1 none
    private int attempt = 0;

    private TaskDef Task => TASKS[taskIdx];

    // Candidate objects shown for the current MCQ (index = option): a cabinet mesh where the
    // furniture moves, or a transparent option-coloured cube where the receiver moves.
    private readonly GameObject[] candidates = new GameObject[4];
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

            // Receiver-location cubes: same M_obj colour, kept translucent even when selected so
            // the receiver marker inside stays visible.
            cubeMats[i] = CandidateMarkers.Tint(src[i], OptionColor(i), CandidateMarkers.ALPHA_NORMAL);
            cubeSelectedMats[i] = CandidateMarkers.Tint(src[i], OptionColor(i), CandidateMarkers.ALPHA_SELECTED);
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
    public void Advance()
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
                SetState(State.MCQ);
                break;

            case State.MCQ:
                if (selected < 0) return;                       // Next without a selection does not advance
                {
                    char chosen = LETTERS[selected];
                    bool correct = chosen == Task.correct[currentSet];
                    attempt++;
                    m.LogAnswer($"Lesson1: {Task.name} set {currentSet} attempt {attempt} answer {chosen} -> {(correct ? "correct" : "incorrect")}");
                    SetState(correct ? State.ExplainCorrect : State.ExplainWrong);
                }
                break;

            case State.ExplainCorrect:
                ClearCandidates();
                taskIdx++;
                if (taskIdx >= TASKS.Length) SetState(State.Complete);
                else GoToTaskStart();
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
                m.QuestionText.text = Task.prompt;
                ShowButtons(4);
                SetButtonLabels(new[] { "A", "B", "C", "D" });
                ClearButtonHighlights();
                if (!candidatesSpawned) SpawnCandidates();
                if (selected >= 0) HighlightOption(selected);
                break;

            case State.ExplainCorrect:
                m.QuestionText.text = Task.correctText[currentSet];
                ShowButtons(0);
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
        m.SetCurrentDataSet(ConditionName(Task.name, currentSet, LETTERS[answerIdx]));
        HighlightOption(answerIdx);
    }

    // ------------------------------------------------------------------
    // Task / state helpers
    // ------------------------------------------------------------------
    private void SetState(State s)
    {
        state = s;
        pendingRender = true;
    }

    private void GoToTaskStart()
    {
        attempt = 0;

        // Start on an incorrect option so there is something to look at without giving the answer
        // away, and present it as the current choice: the receiver is standing in that option's
        // marker, so its button and object must be emphasised to match.
        char preview = PreviewOption(Task, currentSet);
        selected = Array.IndexOf(LETTERS, preview);

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
            }
            else
            {
                // Candidate phone locations get a transparent cube in the option's colour.
                if (!CandidateMarkers.TryReadRxPosition(cond, 1, out Vector3 pos))
                {
                    Debug.LogError($"Task1Manager: could not read Rx position from {cond}");
                    continue;
                }

                candidates[i] = CandidateMarkers.SpawnCube(pos, cubeMats[i], $"Candidate_{cond}");
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
            candidates[i] = null;
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
