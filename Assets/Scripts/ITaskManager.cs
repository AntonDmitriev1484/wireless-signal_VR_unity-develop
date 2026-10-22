/*
 *  Common interface for the Next-button driven task managers (DemoTaskManager, Task1Manager, ...).
 *  MoveAsParticleTest1_v2 holds the current manager and forwards UI button presses to it,
 *  so swapping between managers needs no special-case logic.
 */
public interface ITaskManager
{
    // Move to the state chosen by the previous DoState() call (called on Next).
    void Advance();

    // Step back to the state this manager was in before the last Advance (called on Back). A
    // manager never rewinds past its own first state; crossing into the previous manager is done
    // by the host with ExitBackward / ReEnterFromNext.
    void Back();

    // False on this manager's first screen, where Back has to hand over to the previous manager.
    bool CanGoBack { get; }

    // Back was pressed on this manager's first screen: remove everything it put in the scene
    // (candidates, answer buttons, temporary animations, highlights) before the previous manager
    // takes over.
    void ExitBackward();

    // The next manager was just left backwards: return to this manager's last screen and rebuild
    // the world for it (dataset, receiver model, scene objects). DoState is called afterwards.
    void ReEnterFromNext();

    // Apply the current state to the UI / visualisation.
    void DoState();

    // Called when one of the answer buttons (A=0 .. D=3) is pressed. Selection only, no commit.
    void OnAnswerSelected(int answerIdx);

    // True once the manager has reached its terminal state.
    bool IsComplete { get; }
}
