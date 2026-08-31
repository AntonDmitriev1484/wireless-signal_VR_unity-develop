/*
 *  Per-run trial log.
 *
 *  Collects two things for the whole session and writes them out as JSON when Lesson 2 finishes:
 *
 *    Trial_<MM_DD_YY>_<HH_MM>_viz_usage.json   when the heatmap, the ray lines and the particle
 *                                              animation were on screen
 *    Trial_<MM_DD_YY>_<HH_MM>_answers.json     every multiple-choice option press, and which of
 *                                              them were submitted with Next
 *
 *  Both land in <game root>/Trials - the project folder in the editor, the folder holding the
 *  executable in a build. Times are Unix epoch seconds with a fractional part, so two events in the
 *  same second stay apart; the timestamp in the file name is local time.
 *
 *  Static and scene-independent on purpose: the managers are plain C# objects rebuilt as the lesson
 *  chain advances, and the log has to outlive all of them.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public static class TrialLog
{
    private const string OUTPUT_FOLDER = "Trials";

    private static readonly DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private struct VizEvent
    {
        public string viz;      // "session", "heatmap", "rays", "animation"
        public bool on;
        public double time;
    }

    private struct AnswerEvent
    {
        public string task;     // manager class name
        public string name;     // question name
        public int set;         // counterbalancing set, 0 where the task has none
        public char answer;
        public bool submitted;  // false = the option was previewed, true = it was sent with Next
        public double time;
    }

    private static readonly List<VizEvent> vizEvents = new List<VizEvent>();
    private static readonly List<AnswerEvent> answerEvents = new List<AnswerEvent>();

    // Last recorded visibility per viz name, so a caller can report the current state every frame
    // and only real changes reach the log.
    private static readonly Dictionary<string, bool> vizState = new Dictionary<string, bool>();

    private static bool sessionStarted;
    private static DateTime sessionStartLocal;
    private static bool animationRunOpen;

    public static double Now => (DateTime.UtcNow - UNIX_EPOCH).TotalSeconds;

    public static bool SessionStarted => sessionStarted;

    // Start of the run. Everything logged before this is discarded, so a domain reload in the editor
    // cannot leak the previous play session's events into this one.
    public static void BeginSession()
    {
        vizEvents.Clear();
        answerEvents.Clear();
        vizState.Clear();
        animationRunOpen = false;

        sessionStartLocal = DateTime.Now;
        sessionStarted = true;

        vizEvents.Add(new VizEvent { viz = "session", on = true, time = Now });
    }

    // Report whether a visualisation is currently on screen. Safe to call every frame: an unchanged
    // state adds nothing, which is what keeps the tear-down-and-redraw that every dataset swap
    // performs from showing up as a spurious off/on pair.
    public static void SetVizState(string viz, bool shown)
    {
        if (!sessionStarted) return;

        if (vizState.TryGetValue(viz, out bool was) && was == shown) return;

        vizState[viz] = shown;
        vizEvents.Add(new VizEvent { viz = viz, on = shown, time = Now });
    }

    // One "on" per animation run: a run opens the first frame particles are moving and closes when
    // they all arrive, so pausing and resuming part-way through stays inside the same pair.
    public static void AnimationStarted()
    {
        if (!sessionStarted || animationRunOpen) return;

        animationRunOpen = true;
        vizEvents.Add(new VizEvent { viz = "animation", on = true, time = Now });
    }

    public static void AnimationCompleted()
    {
        if (!sessionStarted || !animationRunOpen) return;

        animationRunOpen = false;
        vizEvents.Add(new VizEvent { viz = "animation", on = false, time = Now });
    }

    // One entry per option press. submitted marks the press that Next was pressed on.
    public static void Answer(string task, string name, int set, char answer, bool submitted)
    {
        if (!sessionStarted) return;

        answerEvents.Add(new AnswerEvent
        {
            task = task,
            name = name,
            set = set,
            answer = answer,
            submitted = submitted,
            time = Now,
        });
    }

    // ------------------------------------------------------------------
    // Output
    // ------------------------------------------------------------------
    // Writes both files. Safe to call more than once - a second call simply overwrites the same two
    // files, since their names come from the session start rather than from the moment of writing.
    public static void Write()
    {
        if (!sessionStarted)
        {
            Debug.LogWarning("TrialLog: no session was started, nothing to write.");
            return;
        }

        string root = Directory.GetParent(Application.dataPath)?.FullName;

        if (string.IsNullOrEmpty(root))
        {
            Debug.LogError($"TrialLog: cannot resolve a game root above {Application.dataPath}.");
            return;
        }

        string dir = Path.Combine(root, OUTPUT_FOLDER);
        string stamp = sessionStartLocal.ToString("MM_dd_yy_HH_mm", CultureInfo.InvariantCulture);

        try
        {
            Directory.CreateDirectory(dir);

            File.WriteAllText(Path.Combine(dir, $"Trial_{stamp}_viz_usage.json"), VizJson());
            File.WriteAllText(Path.Combine(dir, $"Trial_{stamp}_answers.json"), AnswerJson());

            Debug.Log($"TrialLog: wrote {vizEvents.Count} viz events and {answerEvents.Count} answers to {dir}");
        }
        catch (Exception e)
        {
            Debug.LogError($"TrialLog: could not write to {dir}: {e.Message}");
        }
    }

    private static string VizJson()
    {
        StringBuilder json = new StringBuilder("[\n");

        for (int i = 0; i < vizEvents.Count; i++)
        {
            VizEvent e = vizEvents[i];

            json.Append("\t{\n")
                .Append($"\t\t\"viz\": \"{Escape(e.viz)}\",\n")
                .Append($"\t\t\"toggle\": \"{(e.on ? "on" : "off")}\",\n")
                .Append($"\t\t\"time\": {Seconds(e.time)}\n")
                .Append(i < vizEvents.Count - 1 ? "\t},\n" : "\t}\n");
        }

        return json.Append("]\n").ToString();
    }

    private static string AnswerJson()
    {
        StringBuilder json = new StringBuilder("[\n");

        for (int i = 0; i < answerEvents.Count; i++)
        {
            AnswerEvent e = answerEvents[i];

            json.Append("\t{\n")
                .Append($"\t\t\"task\": \"{Escape(e.task)}\",\n")
                .Append($"\t\t\"name\": \"{Escape(e.name)}\",\n")
                .Append($"\t\t\"set\": {e.set.ToString(CultureInfo.InvariantCulture)},\n")
                .Append($"\t\t\"answer\": \"{e.answer}\",\n")
                .Append($"\t\t\"submitted\": {(e.submitted ? "true" : "false")},\n")
                .Append($"\t\t\"time\": {Seconds(e.time)}\n")
                .Append(i < answerEvents.Count - 1 ? "\t},\n" : "\t}\n");
        }

        return json.Append("]\n").ToString();
    }

    // Epoch seconds to millisecond resolution, never in scientific notation and never with a
    // comma for a decimal point - the machine's locale must not change the file.
    private static string Seconds(double time) => time.ToString("F3", CultureInfo.InvariantCulture);

    private static string Escape(string s) =>
        string.IsNullOrEmpty(s) ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
