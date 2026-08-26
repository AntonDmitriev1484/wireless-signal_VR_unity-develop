/*
 *  Test harness for MoveTempParticles. Runs before DemoTaskManager and Task1Manager.
 *
 *  Sequence:
 *    1. load the demo dataset and play the normal Tx -> Rx animation on MoveAsParticleTest1_v2;
 *    2. when every ray has arrived, spawn a MoveTempParticles instance with the reversed paths
 *       in blue and play it, so the message visibly travels Rx -> Tx;
 *    3. Next ends the test and hands over to DemoTaskManager.
 *
 *  While the temporary animation is alive, the menu's Play/Pause, Restart and Rays buttons drive
 *  it instead of the main animation (handled in MoveAsParticleTest1_v2).
 */

using System.Collections.Generic;
using UnityEngine;

public class TaskCommTestManager : ITaskManager
{
    private enum State { Sending, Replying, Done }

    private readonly MoveAsParticleTest1_v2 m;

    private State state = State.Sending;
    private bool complete;
    private bool started;
    private MoveTempParticles temp;

    public bool IsComplete => complete;

    public TaskCommTestManager(MoveAsParticleTest1_v2 m)
    {
        this.m = m;

        m.SetMessage("Hello");

        // SetCurrentDataSet calls GetData for both the paths and the heatmap, then rebuilds
        // the Tx/Rx markers and the particle systems for that dataset.
        m.SetCurrentDataSet(m.DemoCsvFile);
    }

    // Next always ends the comm test, whether or not the animations have finished.
    public void Advance()
    {
        Cleanup();
        complete = true;
    }

    public void DoState()
    {
        if (!started)
        {
            started = true;
            StartForwardAnimation();
        }

        switch (state)
        {
            case State.Sending:
                SetText("Comm test: the router is sending \"Hello\" to the TV. Press Next to skip.");
                break;
            case State.Replying:
                SetText("Comm test: the TV is replying - the blue particles run the same paths backwards. Press Next to skip.");
                break;
            case State.Done:
                SetText("Comm test complete. Press Next to start the demo.");
                break;
        }
    }

    public void OnAnswerSelected(int answerIdx) { }

    // ------------------------------------------------------------------
    // Step 1 - forward animation on the main component
    // ------------------------------------------------------------------
    private void StartForwardAnimation()
    {
        m.OnAllRaysCompleted += HandleForwardComplete;

        // RayPlayPause toggles; the rays start paused after a dataset load.
        // Loaded paused; the participant starts it with Play/Pause or Restart.
    }

    private void HandleForwardComplete()
    {
        m.OnAllRaysCompleted -= HandleForwardComplete;

        StartReverseAnimation();

        state = State.Replying;
        DoState();
    }

    // ------------------------------------------------------------------
    // Step 2 - reversed animation on a temporary instance
    // ------------------------------------------------------------------
    private void StartReverseAnimation()
    {
        List<RayPathSet_v2> reversed = MoveTempParticles.ReversePaths(m.LoadedRaysPath);

        temp = m.SpawnTempParticles(reversed, Color.blue);
        if (temp == null)
        {
            Debug.LogError("TaskCommTestManager: could not create the temporary animation.");
            state = State.Done;
            return;
        }

        // Same message treatment as the forward animation: text rides the particles and builds up
        // at the far end of each path (here the router, since the paths run Rx -> Tx).
        temp.SetMessage("Hi!");

        temp.OnComplete += HandleReverseComplete;
        // Built paused - press Play to send the reply.
    }

    private void HandleReverseComplete()
    {
        state = State.Done;
        DoState();
    }

    // ------------------------------------------------------------------
    // Teardown
    // ------------------------------------------------------------------
    private void Cleanup()
    {
        m.OnAllRaysCompleted -= HandleForwardComplete;

        if (temp != null)
        {
            temp.OnComplete -= HandleReverseComplete;
            temp.Stop();
            temp = null;
        }
        MoveTempParticles.StopAll();

        // Leave the main animation paused so DemoTaskManager's own Play/Pause calls behave normally.
        if (!m.RaysPaused) m.RayPlayPause();
    }

    private void SetText(string text)
    {
        var t = m.QuestionText;
        if (t != null) t.text = text;
    }
}
