/*
 *  Lesson 2 - communication and interference.
 *
 *  T1 "communication_single": the router texts the phone and the phone texts back, using the main
 *     animation for router -> phone and a MoveTempParticles instance for the reversed reply.
 *
 *  T2 "interference_*": two phones (Rx_Number 1 = You, 2 = Friend) transmit to the router at once.
 *     Only router -> phone data exists, so both directions are played by reversing the paths onto
 *     MoveTempParticles instances - You in blue, Friend in green. An MCQ then moves the Friend
 *     until the interference clears, and a final turn-taking (TDMA) beat plays them one after
 *     the other with the Friend's arrived text raised so the two messages stay readable.
 *
 *  Datasets live in Assets/Resources/lesson2/, so every name is prefixed with "lesson2/".
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Task2Manager : ITaskManager
{
    // ------------------------------------------------------------------
    // Datasets and answer key
    // ------------------------------------------------------------------
    private const string DIR = "lesson2/";
    private const string COMM_SINGLE = DIR + "communication_single";
    private const string INTERFERENCE_BASELINE = DIR + "interference_baseline";
    private const string INTERFERENCE_PREFIX = DIR + "interference_space_";

    private static readonly char[] LETTERS = { 'A', 'B', 'C', 'D' };
    private const char CORRECT = 'D';

    private const int RX_YOU = 1;       // Phone A - fixed
    private const int RX_FRIEND = 2;    // Phone B - moves between options

    private static readonly Color32 COLOR_YOU = Color.blue;
    private static readonly Color32 COLOR_FRIEND = Color.green;

    private const string MSG_YOU = "Hello";
    private const string MSG_FRIEND = "Goodbye";

    // Friend's arrived text sits 0.5 m above You's so the two do not overlap during turn-taking.
    private static readonly Vector3 FRIEND_TEXT_OFFSET = new Vector3(0f, 0.6f, 0f);

    private static readonly Dictionary<char, string> WRONG_TEXT = new Dictionary<char, string>
    {
        { 'A', "Not quite. Your friend barely moved - they are still in the same room with a clear line to the router, " +
               "so their signal arrives almost as loudly as yours and the message stays garbled." },
        { 'B', "Not quite. Your friend is much further away now, but still in the same room with a clear line to the router. " +
               "The room bounces the signal around so well that the extra distance hardly helps." },
        { 'C', "Closer! Putting a wall between your friend and the router does help a lot, but their signal still arrives " +
               "strongly enough to muddle yours. Try somewhere further away with more in the way." },
    };

    private static string InterferenceCondition(char option) => INTERFERENCE_PREFIX + option;

    // ------------------------------------------------------------------
    // State
    // ------------------------------------------------------------------
    private enum State
    {
        T1_Start, T1_Antennas, T1_Texting, T1_FriendTexts, T1_YouText,
        T2_SameRoom, T2_BothTransmit, T2_Garbled, T2_MCQ, T2_Explain,
        T2_MoveHassle, T2_TakeTurns, T2_TurnsPlaying, T2_TurnsFriend, T2_TurnsDone,
        Complete
    }

    private readonly MoveAsParticleTest1_v2 m;

    private State state = State.T1_Start;
    private bool pendingRender = true;
    private bool complete;

    private int selected = -1;
    private int attempt;

    private MoveTempParticles tempYou;
    private MoveTempParticles tempFriend;

    // One transparent option-coloured cube per candidate Friend location, shown during the MCQ.
    private readonly GameObject[] candidateCubes = new GameObject[4];
    private readonly GameObject[] candidateBadges = new GameObject[4];   // coloured letter above each cube
    private Material[] cubeMats;
    private Material[] cubeSelectedMats;

    // answer button bookkeeping
    private readonly Button[] buttons = new Button[4];
    private readonly Image[] buttonImages = new Image[4];
    private readonly TextMeshProUGUI[] buttonLabels = new TextMeshProUGUI[4];
    private readonly Color[] buttonOrigColors = new Color[4];

    public bool IsComplete => complete;

    public Task2Manager(MoveAsParticleTest1_v2 m)
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

        BuildCubeMaterials();
        ValidateDatasets();

        // Every receiver in Lesson 2 is a phone. Set before SetCurrentDataSet: the Rx markers are
        // spawned during the load.
        m.SetReceiverModel(MoveAsParticleTest1_v2.ReceiverModel.Phone);

        // SetMessage must precede SetCurrentDataSet: the particle text objects are built during
        // particle initialisation.
        m.SetMessage("Hey want to hang out tonight?");
        m.SetCurrentDataSet(COMM_SINGLE);
    }

    private void ValidateDatasets()
    {
        List<string> needed = new List<string> { COMM_SINGLE, INTERFERENCE_BASELINE };
        foreach (char opt in LETTERS) needed.Add(InterferenceCondition(opt));

        foreach (string cond in needed)
        {
            TextAsset ta = Resources.Load<TextAsset>(cond);
            if (ta == null) Debug.LogError($"Task2Manager: dataset missing in Resources: {cond}");
            else Resources.UnloadAsset(ta);
        }
    }

    // ------------------------------------------------------------------
    // ITaskManager
    // ------------------------------------------------------------------
    public void Advance()
    {
        switch (state)
        {
            case State.T1_Start:
                SetState(State.T1_Antennas);
                break;

            case State.T1_Antennas:
                SetState(State.T1_Texting);
                break;

            case State.T1_Texting:
                SetState(State.T1_FriendTexts);
                break;

            case State.T1_FriendTexts:
                SetState(State.T1_YouText);
                break;

            case State.T1_YouText:
                StopTempAnimations();
                EnterInterference();
                break;

            case State.T2_SameRoom:
                SetState(State.T2_BothTransmit);
                break;

            case State.T2_BothTransmit:
                SetState(State.T2_Garbled);
                break;

            case State.T2_Garbled:
                // Clear before the load: SetCurrentDataSet snapshots the heatmap and would put it
                // straight back otherwise.
                m.ClearHeatmap();

                // Enter the question on option A. Loading its dataset is what actually moves the
                // Friend's phone to A's location - MarkEndPoints_Rx respawns the Rx markers from
                // the new CSV - so the highlighted button, the highlighted cube and the phone on
                // the floor all agree from the first frame of the question.
                selected = 0;
                LoadInterference(InterferenceCondition(LETTERS[0]));

                SetState(State.T2_MCQ);
                break;

            case State.T2_MCQ:
                if (selected < 0) return;   // Next does nothing until an option is chosen
                {
                    char chosen = LETTERS[selected];
                    bool correct = chosen == CORRECT;
                    attempt++;
                    m.LogAnswer($"Lesson2: interference attempt {attempt} answer {chosen} -> {(correct ? "correct" : "incorrect")}");
                    SetState(correct ? State.T2_MoveHassle : State.T2_Explain);
                }
                break;

            case State.T2_Explain:
                // Retry the same question. The Friend is still standing at the rejected option, so
                // keep it selected - the highlight must always match what is on screen.
                SetState(State.T2_MCQ);
                break;

            case State.T2_MoveHassle:
                SetState(State.T2_TakeTurns);
                break;

            case State.T2_TakeTurns:
                SetState(State.T2_TurnsPlaying);
                break;

            case State.T2_TurnsPlaying:
                SetState(State.T2_TurnsFriend);
                break;

            case State.T2_TurnsFriend:
                SetState(State.T2_TurnsDone);
                break;

            case State.T2_TurnsDone:
                Cleanup();
                complete = true;
                break;

            case State.Complete:
                break;
        }
    }

    public void DoState()
    {
        if (!pendingRender) return;
        pendingRender = false;

        ObjectHighlighter h;

        switch (state)
        {
            case State.T1_Start:
                ShowButtons(0);
                SetText("You've finished the first task. Move to the dining room area for the next.");
                break;

            case State.T1_Antennas:
                ShowButtons(0);
                SetText("Like we showed in the earlier example. Your phone and TV have antennas that let them receive signal. " +
                        "They can also use this to transmit signal!");
                break;

            case State.T1_Texting:
                SetText(MoveAsParticleTest1_v2.HighlightSubstrings(
                            "When you're texting your friend over the internet, your phone sends and receives signals from the router!",
                            new List<string> { "friend", "internet", "router" },
                            COLOR_FRIEND));

                h = m.Highlighter;
                if (h == null) return;

                h.ClearAllHighlights();
                h.SetHighlighted(m.TxMarker, true);

                break;

            case State.T1_FriendTexts:
                h = m.Highlighter;
                if (h == null) return;

                h.ClearAllHighlights();
                SetText("Your friend texts you: \"Hey want to hang out tonight?\"" +
                    "\n\n Your router sends that text in a signal.");
                StartConversation();
                break;

            case State.T1_YouText:
                SetText("You respond: \"Yeah sure, lets hang out at 8?\"" +
                    "\n\n Your phone sends a signal back to the router.");
                StartReply();

                break;

            case State.T2_SameRoom:

                // "friend" is tinted to match the green their signal is drawn in.
                SetText(MoveAsParticleTest1_v2.HighlightSubstrings(
                    "What happens when you and your friend are both texting while in the same room?",
                    new List<string> { "friend" },
                    COLOR_FRIEND));
                
                h = m.Highlighter;
                if (h == null) return;

                h.ClearAllHighlights();
                h.SetHighlighted(m.RxMarkerFor(RX_FRIEND), true);

                break;

            case State.T2_BothTransmit:
                
                SetText(MoveAsParticleTest1_v2.HighlightSubstrings(
                    "What if both your phones transmit at the same time?" +
                    "\n\nYou send \"Hello\", your friend sends \"Goodbye\"",
                    new List<string> { "friend" },
                    COLOR_FRIEND));
                PrepareInterference();
                break;

            case State.T2_Garbled:

                SetText("You can see that the message is much harder to read. This is called interference, " +
                        "and happens when transmitters send signals at the same time.");
                break;

            case State.T2_MCQ:
                SetText("You tell your friend to move to interfere less with your message. You want \"Hello\" to appear as clearly as possible." +
                    "\n\nWhere would you tell them to move?" +
                    "\n\n(Each colored cube is a possible answer)");
                ShowButtons(4);
                SetButtonLabels(new[] { "A", "B", "C", "D" });
                SpawnCandidateCubes();          // show all four places the Friend could move to
                ClearButtonHighlights();
                selected = 0; // Force it to select A as default, because sometimes it ends up with D. NOTE: Hardcoded!!!!
                //HighlightCandidateCube(selected);
                HighlightActiveReceiver();
                if (selected >= 0) HighlightButton(selected);
                break;

            case State.T2_Explain:
                ShowButtons(0);
                SetText(WRONG_TEXT.TryGetValue(LETTERS[selected], out string txt)
                    ? txt
                    : "Not quite. Try moving your friend further away, with more walls in between.");
                break;

            case State.T2_MoveHassle:
                ShowButtons(0);
                ClearCandidateCubes();      // the question is settled; drop the option markers
                ClearButtonHighlights();
                selected = -1;
                SetText("Correct! But, moving is a hassle, surely your friend doesn't want to move all the way to the other room " +
                        "just so you can send your text message.");

                // Put the friend back where they started - the same dataset T2_BothTransmit used -
                // so the turn-taking beat that follows solves the original crowded case in time
                // rather than in space. Must precede HighlightBothPhones(): loading the dataset
                // destroys and respawns the Rx markers the highlights are keyed on.
                LoadInterference(INTERFERENCE_BASELINE);

                HighlightBothPhones();
                break;

            case State.T2_TakeTurns:
                SetText("Another way to avoid interference is by programming your phones to take turns sending their signal.");
                break;

            case State.T2_TurnsPlaying:
                SetText("Your phone goes first, then your friend's.");
                PrepareTurnTaking();
                break;

            case State.T2_TurnsFriend:
                SetText("Now your friend's phone takes its turn.");
                PrepareFriendTurn();
                break;

            case State.T2_TurnsDone:
                SetText("You can see that this makes the messages clear, but this takes longer! " +
                        "This kind of turn-taking slows down your wireless communication, making your connection slower.");
                break;

            case State.Complete:
                break;
        }
    }

    public void OnAnswerSelected(int answerIdx)
    {
        if (state != State.T2_MCQ) return;
        if (answerIdx < 0 || answerIdx > 3) return;

        selected = answerIdx;
        ClearButtonHighlights();
        HighlightButton(answerIdx);
        HighlightCandidateCube(answerIdx);

        // Moving the Friend reloads the dataset; LoadInterference rebuilds both uplink animations.
        LoadInterference(InterferenceCondition(LETTERS[answerIdx]));
        HighlightActiveReceiver();
    }

    // Ring the phone the selected option is currently moving - the Friend's. LoadInterference
    // has already dropped the previous answer's ring: the marker it was keyed on no longer exists.
    private void HighlightActiveReceiver()
    {
        ObjectHighlighter h = m.Highlighter;
        if (h == null) return;

        h.ClearAllHighlights();
        h.SetHighlighted(m.RxMarkerFor(RX_FRIEND), true, Color.black);
    }

    private void SetState(State s)
    {
        state = s;
        pendingRender = true;
    }

    // ------------------------------------------------------------------
    // T1 - one phone, both directions
    // ------------------------------------------------------------------
    // Beat 1 - the router delivers your friend's text to your phone, on the main animation.
    //
    // Nothing plays on its own: WaitPlay() highlights Play/Restart and locks Next until one is
    // pressed, so the participant starts it, watches it, then presses Next when ready.
    //
    // The reply is deliberately NOT chained off OnAllRaysCompleted any more. Doing that called
    // WaitPlay() a second time inside this same state, re-locking Next after it had already been
    // released - the "double halting". Each direction is now its own state.
    private void StartConversation()
    {
        StopTempAnimations();

        m.SetMessage("Hey want to hang out tonight?");
        m.WaitPlay();
    }

    // Beat 2 - your phone answers, so the same paths run in reverse. Gated the same way.
    private void StartReply()
    {
        StopTempAnimations();

        tempYou = m.SpawnTempParticles(MoveTempParticles.ReversePaths(m.LoadedRaysPath), COLOR_YOU);
        if (tempYou == null) return;

        // This is the text the particles carry - the narration lives in the QA panel instead.
        tempYou.SetMessage("Yeah sure, lets hang out at 8?");
        m.WaitPlay();
    }

    // ------------------------------------------------------------------
    // T2 - two phones
    // ------------------------------------------------------------------
    private void EnterInterference()
    {

        // T2 only shows the reversed phone -> router animations; park the main particles.
        m.HideAllMovingRays();

        LoadInterference(INTERFERENCE_BASELINE);
        SetState(State.T2_SameRoom);
    }

    private void LoadInterference(string condition)
    {
        StopTempAnimations();

        // Drop highlights first: SetCurrentDataSet destroys the Rx markers, and ObjectHighlighter
        // keeps its circles keyed on those GameObjects - stale keys leave frozen circles on screen.
        m.Highlighter?.ClearAllHighlights();

        if (!m.RaysPaused) m.RayPlayPause();   // keep the (hidden) main animation idle
        m.SetCurrentDataSet(condition);
        m.HideAllMovingRays();

        // Build the two uplink animations immediately, so a live MoveTempParticles instance exists
        // for the whole of T2. The transport buttons drive the temps whenever any are alive, so this
        // is what stops Play from falling through to the main router -> phone animation: without it
        // there is a window (T2_SameRoom) where pressing Play would run that downlink instead.
        BuildPhoneAnimations(raiseFriendText: false);
        SyncRaysToPhoneAnimations();
    }

    // Build one reversed animation per phone. Nothing plays until the caller says so.
    // The main static rays are drawn in viz_color (red) for whatever dataset is loaded, and
    // SetCurrentDataSet rebuilds them on every load. Once the phone animations exist, swap those
    // red lines for the per-phone rays so each set of paths matches the colour of its message.
    //
    // A no-op while rays are switched off, so the participant's own Rays choice is preserved: if
    // they turn rays on later, ToggleRays() already delegates to the live animations.
    private void SyncRaysToPhoneAnimations()
    {
        if (!m.AreRaysShown) return;

        m.HideMainRays();
        MoveTempParticles.ShowRaysAll();
    }

    private void BuildPhoneAnimations(bool raiseFriendText)
    {
        StopTempAnimations();

        tempYou = m.SpawnTempParticles(MoveTempParticles.ReversePaths(m.PathsForRx(RX_YOU)), COLOR_YOU);
        if (tempYou != null) tempYou.SetMessage(MSG_YOU);

        tempFriend = m.SpawnTempParticles(MoveTempParticles.ReversePaths(m.PathsForRx(RX_FRIEND)), COLOR_FRIEND);
        if (tempFriend != null)
        {
            tempFriend.SetMessage(MSG_FRIEND);
            if (raiseFriendText) tempFriend.EndpointTextOffset = FRIEND_TEXT_OFFSET;
        }
    }

    // Both phones transmit at once - the messages pile up on the router and become unreadable.
    // Built paused; Play starts both together (TogglePlayPauseAll drives every live instance).
    private void PrepareInterference()
    {
        BuildPhoneAnimations(raiseFriendText: false);

        // Both phones are built paused. WaitPlay highlights Play/Restart and locks Next until one is
        // pressed, so nobody can skip past "What if both your phones transmit at the same time?"
        // without actually watching the two messages collide.
        m.WaitPlay();
    }

    // Turn taking, turn 1 - your phone transmits. Built paused and gated by WaitPlay, so Play sends
    // it and Next stays locked until then. The message it delivers is left on the router: turn 2 is
    // reached by pressing Next, not by the animation finishing, so the participant can look at the
    // result for as long as they like.
    private void PrepareTurnTaking()
    {
        StopTempAnimations();

        tempYou = m.SpawnTempParticles(MoveTempParticles.ReversePaths(m.PathsForRx(RX_YOU)), COLOR_YOU);
        if (tempYou == null) return;

        tempYou.SetMessage(MSG_YOU);

        // Next must stay locked until the message has actually landed, not merely until Play is
        // pressed - otherwise the turn can be skipped mid-flight. HandleYourTurnComplete opens it.
        tempYou.OnComplete += HandleYourTurnComplete;

        SyncRaysToPhoneAnimations();
        m.WaitPlay(releaseOnPlayPress: false);
    }

    // Your phone's message has arrived; let the participant move on to the friend's turn.
    private void HandleYourTurnComplete()
    {
        if (tempYou != null) tempYou.OnComplete -= HandleYourTurnComplete;

        m.ReleaseWaitPlay();
    }

    // Turn taking, turn 2 - the friend's phone transmits, entered on Next.
    private void PrepareFriendTurn()
    {
        // Tearing down turn 1 also removes the message it delivered, so the friend's turn starts
        // from a clean router.
        StopTempAnimations();

        tempFriend = m.SpawnTempParticles(MoveTempParticles.ReversePaths(m.PathsForRx(RX_FRIEND)), COLOR_FRIEND);
        if (tempFriend == null) return;

        // No raised offset any more - with the router reset between turns there is nothing below to
        // stack above, so both turns deliver their text at the same height.
        tempFriend.SetMessage(MSG_FRIEND);

        SyncRaysToPhoneAnimations();
        m.WaitPlay();
    }

    private void StopTempAnimations()
    {
        if (tempYou != null) tempYou.OnComplete -= HandleYourTurnComplete;

        MoveTempParticles.StopAll();
        tempYou = null;
        tempFriend = null;
    }

    // ------------------------------------------------------------------
    // Candidate location cubes (MCQ options = where the Friend moves to)
    // ------------------------------------------------------------------
    private void BuildCubeMaterials()
    {
        Material[] src = m.OptionMaterials;

        cubeMats = new Material[4];
        cubeSelectedMats = new Material[4];

        for (int i = 0; i < 4; i++)
        {
            // Plain white and very faint for every option - the colour is carried by the letter
            // badge above each cube. src[i] is only borrowed for its transparent shader settings.
            cubeMats[i] = CandidateMarkers.Tint(src[i], CandidateMarkers.CUBE_COLOR, CandidateMarkers.ALPHA_NORMAL);
            cubeSelectedMats[i] = CandidateMarkers.Tint(src[i], CandidateMarkers.CUBE_COLOR, CandidateMarkers.ALPHA_SELECTED);
        }
    }

    private Color OptionColor(int i) => CandidateMarkers.OptionColor(m.OptionMaterials[i]);

    private void SpawnCandidateCubes()
    {
        ClearCandidateCubes();

        for (int i = 0; i < 4; i++)
        {
            string cond = InterferenceCondition(LETTERS[i]);

            // The Friend is Rx_Number 2; its endpoint is where that option would move them to.
            if (!CandidateMarkers.TryReadRxPosition(cond, RX_FRIEND, out Vector3 pos))
            {
                Debug.LogError($"Task2Manager: could not read Friend position from {cond}");
                continue;
            }

            //candidateCubes[i] = CandidateMarkers.SpawnCube(pos, cubeMats[i], $"Candidate_{cond}");

            candidateBadges[i] = CandidateMarkers.SpawnBadge(
                pos, LETTERS[i], OptionColor(i), m.OptionMaterials[i], $"Badge_{cond}");
        }
    }

    private void HighlightCandidateCube(int sel)
    {
        for (int i = 0; i < 4; i++)
        {
            if (candidateCubes[i] == null) continue;

            candidateCubes[i].GetComponent<MeshRenderer>().sharedMaterial =
                i == sel ? cubeSelectedMats[i] : cubeMats[i];
        }
    }

    private void ClearCandidateCubes()
    {
        for (int i = 0; i < 4; i++)
        {
            if (candidateCubes[i] != null) Object.Destroy(candidateCubes[i]);
            if (candidateBadges[i] != null) Object.Destroy(candidateBadges[i]);

            candidateCubes[i] = null;
            candidateBadges[i] = null;
        }
    }

    // ------------------------------------------------------------------
    // Highlighting
    // ------------------------------------------------------------------
    private void HighlightBothPhones()
    {
        ObjectHighlighter h = m.Highlighter;
        if (h == null) return;

        h.ClearAllHighlights();
        h.SetHighlighted(m.RxMarkerFor(RX_YOU), true);
        h.SetHighlighted(m.RxMarkerFor(RX_FRIEND), true);
    }

    // ------------------------------------------------------------------
    // Teardown
    // ------------------------------------------------------------------
    private void Cleanup()
    {
        StopTempAnimations();
        ClearCandidateCubes();
        ClearButtonHighlights();

        m.Highlighter?.ClearAllHighlights();

        // Restore what the following managers expect: main particles visible and paused.
        m.ShowAllMovingRays();
        if (!m.RaysPaused) m.RayPlayPause();

        ShowButtons(0);
    }

    // ------------------------------------------------------------------
    // UI helpers
    // ------------------------------------------------------------------
    private void SetText(string text)
    {
        var t = m.QuestionText;
        if (t != null) t.text = text;
    }

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

    // The selected option's button takes its cube's colour at full opacity.
    private void HighlightButton(int i)
    {

        Debug.Log("Highlhting " + i);
        if (buttonImages[i] == null) return;

        Color c = OptionColor(i);
        c.a = 1f;
        buttonImages[i].color = c;
    }

    private void ClearButtonHighlights()
    {
        for (int i = 0; i < 4; i++)
        {
            if (buttonImages[i] == null) continue;

            // During the MCQ each button keeps a faint version of its option colour, so it reads as
            // belonging to the cube of the same colour; elsewhere the buttons look normal again.
            if (state == State.T2_MCQ)
            {
                Color c = OptionColor(i);
                c.a = buttonOrigColors[i].a;
                buttonImages[i].color = c;
            }
            else
            {
                buttonImages[i].color = buttonOrigColors[i];
            }
        }
    }
}
