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
                     "The other locations are behind one or two walls, which weaken the signal a lot." },
            },
            wrongText =
            {
                { (1, 'A'), "Not quite. This spot is close to the router, but the kitchen wall is in the way. Walls weaken the signal - look for a location that can see the router directly." },
                { (1, 'C'), "Not quite. This spot is far from the router and behind the dividing wall, so only weak signals reach it. Look for a location that can see the router directly." },
                { (1, 'D'), "Not quite. This spot is behind the dividing wall and the projector screen, so the signal is weak. Look for a location that can see the router directly." },
                { (2, 'A'), "Not quite. The couch is behind the dividing wall between the living room and the office, so the signal is weak. Look for a location that can see the router directly." },
                { (2, 'B'), "Not quite. The kitchen is two walls away from the router, so the signal is weak. Look for a location that can see the router directly." },
                { (2, 'C'), "Not quite. This spot is far from the router and behind the dividing wall, so only weak signals reach it. Look for a location that can see the router directly." },
            },
        },
        new TaskDef
        {
            name = "los_creation",
            prompt = "The TV isn't getting a very strong signal. This is because the furniture and walls are all impacting your signal. " +
                     "Move the furniture around to give the TV the strongest signal.",
            hasCabinet = true,
            correct = { { 1, 'C' }, { 2, 'A' } },
            correctText =
            {
                { 1, "Correct! With the cabinet behind the TV, the direct path from the router to the TV stays open and the TV gets a much stronger signal. " +
                     "Anything placed between the router and the TV blocks that direct path." },
                { 2, "Correct! With the cabinet beside the TV, the direct path from the router to the TV stays open and the TV gets a much stronger signal. " +
                     "Anything placed between the router and the TV blocks that direct path." },
            },
            wrongText =
            {
                { (1, 'A'), "Not quite. Here the cabinet sits between the router and the TV and blocks the direct path, so the TV only gets weaker signals that bounce off the walls. Try a spot that is not in the way." },
                { (1, 'B'), "Not quite. The cabinet is right in front of the TV, blocking the direct path from the router. Only weaker reflected signals reach the TV. Try a spot that is not in the way." },
                { (1, 'D'), "Not quite. Here the cabinet sits between the router and the TV and blocks the direct path, so the TV only gets weaker signals that bounce off the walls. Try a spot that is not in the way." },
                { (2, 'B'), "Not quite. Here the cabinet sits between the router and the TV and blocks the direct path, so the TV only gets weaker signals that bounce off the walls. Try a spot that is not in the way." },
                { (2, 'C'), "Not quite. The cabinet is right in front of the TV, blocking the direct path from the router. Only weaker reflected signals reach the TV. Try a spot that is not in the way." },
                { (2, 'D'), "Not quite. Here the cabinet sits between the router and the TV and blocks the direct path, so the TV only gets weaker signals that bounce off the walls. Try a spot that is not in the way." },
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
                { 1, "Correct! The metal cabinet acts like a mirror: the signal from the router bounces off it, passes through the doorway and reaches the TV. " +
                     "That reflected path is the strongest one arriving at the TV, which is why the signal improves so much." },
                { 2, "Correct! The metal cabinet acts like a mirror: the signal from the router bounces off it, passes through the doorway and reaches the TV. " +
                     "That reflected path is the strongest one arriving at the TV, which is why the signal improves so much." },
            },
            wrongText =
            {
                { (1, 'A'), "Not quite. In this corner of the office the cabinet reflects the signal away from the doorway, so no strong bounce reaches the TV. Try to place it so the router 'mirrors' onto the TV." },
                { (1, 'B'), "Not quite. Here the cabinet stands in the doorway and blocks it instead of reflecting the signal toward the TV. Try to place it so the router 'mirrors' onto the TV." },
                { (1, 'C'), "Not quite. Next to the TV the cabinet cannot redirect the router's signal toward it - the signal still has to pass through the wall. Try to place it so the router 'mirrors' onto the TV." },
                { (2, 'A'), "Not quite. In this corner the cabinet reflects the signal away from the doorway, so no strong bounce reaches the TV. Try to place it so the router 'mirrors' onto the TV." },
                { (2, 'C'), "Not quite. On this side of the doorway the cabinet does not reflect the router's signal toward the TV. Try to place it so the router 'mirrors' onto the TV." },
                { (2, 'D'), "Not quite. Next to the TV the cabinet cannot redirect the router's signal toward it - the signal still has to pass through the wall. Try to place it so the router 'mirrors' onto the TV." },
            },
        },
    };

    private static string ConditionName(string task, int set, char option) => $"{task}_{set}_{option}";

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

    // candidate objects shown for the current MCQ (index = option)
    private readonly GameObject[] candidates = new GameObject[4];
    private bool candidatesSpawned;
    private Material[] normalMats;
    private Material[] activeMats;

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
        for (int i = 0; i < 4; i++)
        {
            normalMats[i] = src[i];
            activeMats[i] = new Material(src[i]);
            Color c = OptionColor(i); c.a = 1f;
            activeMats[i].color = c;
            if (activeMats[i].HasProperty("_BaseColor")) activeMats[i].SetColor("_BaseColor", c);
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
                selected = -1;                                  // retry same MCQ; dataset stays on last viewed option
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
        selected = -1;
        attempt = 0;
        // Every task starts on option A so there is always something to look at.
        m.SetCurrentDataSet(ConditionName(Task.name, currentSet, 'A'));
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
            GameObject go = Task.hasCabinet ? SpawnCabinet(cond) : SpawnLocationMarker(cond);
            if (go == null) continue;
            go.name = $"Candidate_{cond}";
            ApplyMaterial(go, normalMats[i]);
            candidates[i] = go;
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

    // Phone location = Rx position = last coordinate of the first data row of the path CSV.
    private GameObject SpawnLocationMarker(string cond)
    {
        if (!TryReadRxPosition(cond, out Vector3 pos))
        {
            Debug.LogError($"Task1Manager: could not read Rx position from {cond}");
            return null;
        }
        // Slightly below and larger than the real Rx marker so the colour ring stays visible around it.
        GameObject go = UnityEngine.Object.Instantiate(m.RxPrefab, pos + new Vector3(0, -0.02f, 0), Quaternion.identity);
        go.transform.localScale = go.transform.localScale * 1.6f;
        foreach (Transform child in go.transform) child.gameObject.SetActive(false); // no MessageDisplay on candidates
        return go;
    }

    private static bool TryReadRxPosition(string cond, out Vector3 pos)
    {
        pos = Vector3.zero;
        TextAsset ta = Resources.Load<TextAsset>(cond);
        if (ta == null) return false;

        string[] lines = ta.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return false;

        // Same layout as LoadDataFromCSVLine: fields 0..4, then the quoted "x y z, x y z, ..." list.
        string[] fields = lines[1].Split(',');
        if (fields.Length < 6) return false;
        string coords = string.Join(",", fields, 5, fields.Length - 5).Trim().Trim('"');
        string[] points = coords.Split(',');
        string[] xyz = points[points.Length - 1].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (xyz.Length < 3) return false;

        var inv = System.Globalization.CultureInfo.InvariantCulture;
        if (!float.TryParse(xyz[0], System.Globalization.NumberStyles.Float, inv, out float x) ||
            !float.TryParse(xyz[1], System.Globalization.NumberStyles.Float, inv, out float y) ||
            !float.TryParse(xyz[2], System.Globalization.NumberStyles.Float, inv, out float z))
            return false;

        pos = new Vector3(x, y, z);
        return true;
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
            if (candidates[i] != null) ApplyMaterial(candidates[i], i == sel ? activeMats[i] : normalMats[i]);

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
