/*
 *  Common interface for the Next-button driven task managers (DemoTaskManager, Task1Manager, ...).
 *  MoveAsParticleTest1_v2 holds the current manager and forwards UI button presses to it,
 *  so swapping between managers needs no special-case logic.
 */
public interface ITaskManager
{
    // Move to the state chosen by the previous DoState() call (called on Next).
    void Advance();

    // Apply the current state to the UI / visualisation.
    void DoState();

    // Called when one of the answer buttons (A=0 .. D=3) is pressed. Selection only, no commit.
    void OnAnswerSelected(int answerIdx);

    // True once the manager has reached its terminal state.
    bool IsComplete { get; }
}
