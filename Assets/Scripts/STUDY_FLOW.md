# WiViz study flow

Complete walkthrough of the VR study as a participant experiences it, screen by screen, with the
exact on-screen text and every asset each screen loads.

Written to serve two readers: someone documenting or writing about the study (the text is verbatim
and the order is the order it is shown), and someone working on the code (state names, transitions
and file paths are given for every screen).

**Source of truth.** Everything here is read out of the code, not from notes:

| Part | File |
|---|---|
| Host component, demo | [MoveAsParticleTest1_v2.cs](MoveAsParticleTest1_v2.cs) (`DemoTaskManager` is a nested class) |
| Lesson 1 | [Task1Manager.cs](Task1Manager.cs) |
| Lesson 2 | [Task2Manager.cs](Task2Manager.cs) |
| Manager contract | [ITaskManager.cs](ITaskManager.cs) |
| Temporary particle animations | [MoveTempParticles.cs](MoveTempParticles.cs) |
| Option markers and letter badges | [CandidateMarkers.cs](CandidateMarkers.cs) |
| Receiver rings | [ObjectHighlighter.cs](ObjectHighlighter.cs) |
| Session logging | [TrialLog.cs](TrialLog.cs) |

---

## 1. How the study is driven

### 1.1 Manager chain

One `ITaskManager` is active at a time, held in `MoveAsParticleTest1_v2.CurrentTaskManager`. When a
manager reports `IsComplete`, `ButtonNext()` swaps in the next one:

```
DemoTaskManager  ->  Task1Manager  ->  Task2Manager  ->  (stays)
```

Every manager implements four methods:

- `Advance()` — commit the current screen and choose the next (called by **Next**)
- `Back()` — return to the previous screen (called by **Back**)
- `DoState()` — render the current screen: text, buttons, scene objects, data
- `OnAnswerSelected(int)` — an answer button A–D was pressed (0–3); selection only, no commit

### 1.2 The four menu buttons

| Button | Method | Effect |
|---|---|---|
| **Next** | `ButtonNext()` | Advance the manager, then render |
| **Back** | `ButtonBack()` | Step back one screen within the current manager, then render |
| **Play / Pause** | `RayPlayPause()` | Start, pause or resume the particle animation. Its label tracks whether anything is moving |
| **Re-start** | `Restart()` | Replay the animation from the beginning |
| **Rays** | `ToggleRays()` | Show/hide the static ray lines for the loaded dataset |
| **Heatmap** | `ToggleHeatmap()` | Show/hide the floor heatmap for the loaded dataset |

Buttons are found by the method name their `onClick` calls, so no Inspector wiring is needed beyond
pointing each button at its method.

### 1.3 Gates

A screen can block **Next** until the participant does something. Three mechanisms:

| Gate | Call | Opens when |
|---|---|---|
| Transport gate | `WaitPlay()` | Play/Pause or Re-start is pressed |
| Named-button gate | `WaitPlay(button)` | That specific button is pressed (used for Rays and Heatmap) |
| Deferred gate | `WaitPlay(releaseOnPlayPress: false)` | The caller releases it — used where the animation must *finish*, not merely start |
| Timer gate | `WaitTimer(seconds)` | A fixed time has elapsed |

While a gate is closed the QA panel is locked and the awaited button is tinted with its highlight
colour. **Back is never gated** — a participant can always step out of a screen.

**Animations never auto-play.** Every animation in the study is started by the participant pressing
Play or Re-start.

### 1.4 Back

Each manager keeps a history stack, pushed in `Advance()` only when the state actually moved. `Back`
pops it and rebuilds the world for that screen: candidate objects cleared and respawned, ray
highlights and receiver rings dropped, heatmap and rays taken down, and the dataset for the restored
task/option reloaded. `ButtonBack()` additionally clears all `ObjectHighlighter` rings and sets the
particle message to `""`.

**Back crosses manager boundaries.** On a manager's first screen (`CanGoBack` is false), Back
calls `ExitBackward()` on it, which removes everything it put in the scene, and discards it. Then
it returns to the previous manager via `ReEnterFromNext()`, which steps back onto its last screen
and rebuilds that screen's world:

| From | Back lands on |
|---|---|
| Lesson 1 `SetSelect` | Demo `D11` (trimmed `demo_lit` set reloaded, antenna receiver) |
| Lesson 2 `T1_Start` | Lesson 1 `ExplainTransmission` of `reflection_creation` (correct cabinet, rays and router-to-TV line rebuilt) |

The host keeps finished managers on a stack (`previousTaskManagers`) for this. Pressing Next from
the restored screen builds a *fresh* next manager, exactly as the first time through. Only the
demo's `D1` is a dead end.

### 1.5 Data format

Each condition is a pair of CSVs in `Assets/Resources`:

- `<condition>.csv` — one row per ray path
  Columns: `Rx_Number, Path_Power_dBm, Interaction_Description, Total_Interactions_for_Path, RSS_dBm, Interaction_Coordinates`
  `Interaction_Coordinates` is a quoted list of `x y z` points, already in Unity coordinates,
  running Tx → interactions → Rx. `Interaction_Description` is of the form `Tx-I-I-Rx`.
- `<condition>_heatmap.csv` — the floor heatmap cells for the same scene

`SetCurrentDataSet(condition)` loads both, rebuilds the Tx/Rx markers, re-seeds the particles, and
restores the heatmap and ray lines if they were showing before the swap. It always leaves playback
paused.

### 1.6 Scene assets used throughout

| Role | Asset |
|---|---|
| Transmitter (router) marker | `Assets/Objects/Transmitter_PF2.prefab` |
| Receiver — antenna | `Assets/Objects/Receiver_PF2.prefab` |
| Receiver — phone | `Assets/Akduman/ToonTastic - Electronic Devices/Models/electronicdevices.fbx`, scaled 2×, painted with `ColorPalette_128_URP.mat` |
| Option colours A–D | `Assets/Objects/M_obj1.mat` … `M_obj4.mat` |
| Option colours, selected | `Assets/Objects/M_obj1 1.mat`, `M_obj2 1.mat`, `M_obj3 1.mat` |
| Heatmap cells | `Assets/Objects/M_Heatmap.mat` |
| Highlight material | `Assets/Objects/M_Highlight.mat` |
| Receiver ring sprite | `Assets/Objects/HighlightSprite2.png` (GameObject name `Highlight Sprite`) |
| Signal-strength colour ramp | `Assets/Resources/colorPalette_256RedtoBlue.txt` |

Receiver colour scale: `Rx_POWER_MAX_dBm = -40`, `LowestPowerValRx_dBm = -100`. Red is strong, blue
is weak.

### 1.7 Text conventions in this document

Text is quoted exactly as the participant sees it. Where a string is passed through
`HighlightSubstrings`, the listed words are wrapped in colour tags at runtime — the words are noted
under the quote, not shown as markup. Default highlight colour is green (`#00FF00`); Lesson 2 uses
the friend's green (`COLOR_FRIEND`) for the word "friend".

### 1.8 Not part of this flow

`csvFile_T12_1`, `csvFile_T12_2`, `csvFile_T3_*`, `csvFile_T4_base`, `csvFile_T4_metal` and the
methods `T3dataButtonInput`, `ShowObjects_T1/T3`, `HideObjects_T1/T3` are left over from an earlier
version of the study. Nothing in the Demo → Lesson 1 → Lesson 2 chain calls them.

---

## 2. Demo — `DemoTaskManager`

Teaches the vocabulary: transmitter, receiver, paths, signal strength, and the two visualisations.
No questions. Eleven screens, `D1` → `D11`, then `END`, which hands over to Lesson 1.

**Receiver model:** antenna (`Receiver_PF2`).
**Dataset on entry:** `paths_dense_unity` — the dense path set, 150 paths.

> **Known gap:** `paths_dense_unity_heatmap.csv` does not exist. `GetData` logs an error at demo
> start. Nothing before D7 shows the heatmap (the Heatmap button is first introduced at D10, after
> the swap to `demo_lit`), so it is currently a console error only.

### D1

> You're in your living room watching YouTube right now.
>
> How does your TV use WiFi to stream videos from the internet? Press Next to find out!

Highlighted word: *TV*. Ring on the TV mesh.
**Data:** `paths_dense_unity.csv` (loaded in the constructor). **Gate:** none.

### D2

> Your WiFi router has a cable that connects it to the internet. It turns the video data into a signal, and sends it out into the environment.
>
> Press Play to see what this signal looks like.

Highlighted word: *router*. Ring moves from the TV to the router.
**Gate:** `WaitPlay()` — Next locked until Play or Re-start.

### D3

> Your router turns the video into a signal, and transmits (tx) it.
>
> Your TV has a receiver (rx) that lets it hear that signal.

Highlighted words: *transmits (tx)*, *receiver (rx)*. Rings on both the router and the receiver.
**Gate:** none.

### D4

> Let's look at how the tx sends a message to the rx.
>
> When you press Play, the router is going to say "Hello" to your TV!

Highlighted words: *rx*, *tx*. Rings on both.
**Particle message:** `"Hello"` — carried by the particles and revealed above the receiver as paths
arrive. **Gate:** `WaitPlay()`.

### D5

> This is how wireless communication works!
>
> WiFi is a kind of wireless communication where devices can access the internet by communicating with your router.

**Gate:** none.

### D6

> If you press 'Rays' you can see that the signal takes multiple paths through the entire room.
>
> If you press Re-start, you can see how each path moves on its own through the environment.

Rings cleared. **Gate:** `WaitPlay(raysButton)` — Next locked until **Rays** is pressed. This is the
screen where the participant sees all 150 dense paths at once.

### D7

> As you've seen, signals have a ton of paths, and interact with the entire room.
>
> For this study, we only show you the strongest paths in the signal.

**Data swap:** `paths_dense_unity` → `demo_lit` (`demo_lit.csv` + `demo_lit_heatmap.csv`), 10 paths.
If Rays was left on, the lines are rebuilt from the trimmed set, so the reduction is visible on this
screen. **Gate:** `WaitPlay()`.

### D8

> To the receiver, the signal gets stronger as more paths arrive.
>
> When you play the signal, you can see how the message gets clearer and easier to read above the receiver.

Highlighted words: *above the receiver*. Ring on the receiver only.
**Gate:** none.

### D9

> Receiving a strong signal is like hearing someone talking loudly.
>
> The louder you hear someone speak - the easier it is for you to understand them.

**Gate:** none.

### D10

> Some paths in the signal are stronger than others, so some parts of the room get lower signal strength.
>
> Press the 'Heatmap' button to see the signal strength in each location.

Rings cleared. **Gate:** `WaitPlay(heatmapButton)` — Next locked until **Heatmap** is pressed.
**Data:** heatmap built from `demo_lit_heatmap.csv`.

### D11

> To see the signal strength of a color, check the color bar at the bottom right-hand side of this menu.
>
> ex. Red indicates a strong signal, blue indicates a weak signal.

**Gate:** none. Next hands over to `Task1Manager`.

---

## 3. Lesson 1 — `Task1Manager`

Three multiple-choice tasks in fixed order, each with its own dataset family:

```
phone_optimization  ->  los_creation  ->  reflection_creation
```

Two counterbalanced sets. Condition naming: `<task>_<set>_<option>`, e.g. `los_creation_1_C`, with a
matching `_heatmap.csv`.

### 3.1 State machine

```
SetSelect -> [Intro] -> MCQ -> PromptExplanation -> ExplainCorrect -> (ExplainTransmission) -> next task
                         ^                       \-> ExplainWrong  -> back to MCQ
                         |______________________________________________|
```

| State | Purpose |
|---|---|
| `SetSelect` | Researcher picks the counterbalancing set |
| `Intro` | Optional scene-setting screen (only `reflection_creation` has one) |
| `MCQ` | The question; A–D preview their datasets, Next commits |
| `PromptExplanation` | Think-aloud prompt, 10-second timer gate |
| `ExplainCorrect` | Response to a correct answer |
| `ExplainTransmission` | Extra screen after `reflection_creation` only |
| `ExplainWrong` | Response to a wrong answer; Next returns to `MCQ` |
| `Complete` | Lesson finished; Next hands over to Lesson 2 |

### 3.2 Behaviour common to every task

**Entering a task** (`GoToTaskStart`): rings cleared, receiver model chosen (phone for
`phone_optimization`, antenna for the other two), heatmap cleared and rays hidden — both *before*
the load, since `SetCurrentDataSet` would otherwise restore them — then the preview dataset is
loaded.

**Preview option.** A task opens on a deliberately *incorrect* option so the correct layout is never
the default: `PreviewOption` returns `'B'` when the correct answer is `'A'`, otherwise `'A'`.

**Selecting an option** (`OnAnswerSelected`) reloads that option's dataset, which moves the cabinet
or the phone, retints the buttons, and re-rings the active receiver. It does **not** commit an
answer — Next does that.

**Option markers.** Each option gets a letter badge: a thin slab in the option's colour carrying a
black letter, camera-facing, 0.4 m above the option's position (cabinet tasks: above the top face of
the cabinet mesh). Cabinet tasks additionally show the four cabinet meshes themselves, painted in
the option colours. *The transparent floor cubes for phone-placement options are currently commented
out in `SpawnCandidates` — only the badges appear.*

**Receiver ring.** On `phone_optimization` only, a black `Highlight Sprite` circle rings the phone at
the currently selected option.

**On a correct answer** (`ExplainCorrect`): all candidates except the correct one are hidden, and
the ray lines are turned on if they were off. For `reflection_creation` the paths that bounce off the
metal cabinet are highlighted green at 3× thickness; for `los_creation` the rays are simply shown.

**Answer logging.** Every option press is written to the answers log as a preview
(`"submitted": false`); the press that Next commits is written again with `"submitted": true`.

### 3.3 `SetSelect`

> Please select the set your researcher gave you, then press Next.

Two buttons: **Set 1**, **Set 2**. No dataset is loaded until a set is chosen.

### 3.4 Task 1 — `phone_optimization`

Receiver: **phone**. Four candidate floor positions, options A–D.

> You're using your phone. Which one of these locations will give you the best signal strength?

**Files, per option** — `phone_optimization_<set>_<A|B|C|D>.csv` + `_heatmap.csv`.
Opens on the preview option (A in both sets).

#### Set 1 — correct answer **B**

Correct:

> Correct! This spot is in the same room as the router with a direct line-of-sight path, so the signal is strongest here. The other locations are behind walls, which weaken the signal a lot.

| Wrong | Text |
|---|---|
| A | Not quite. This spot is close to the router, but the kitchen wall is in the way. Walls weaken the signal - look for a location that can see the router directly. |
| C | Not quite. This spot is over in the office, behind the dividing wall, so only weak signals reach it. Look for a location that can see the router directly. |
| D | Not quite. This is the far corner of the office - behind the dividing wall and the furthest of all four spots from the router. Look for a location that can see the router directly. |

#### Set 2 — correct answer **D**

Correct:

> Correct! This spot is in the same room as the router with a direct line-of-sight path, so the signal is strongest here. The other locations are all behind a wall, which weakens the signal a lot.

| Wrong | Text |
|---|---|
| A | Not quite. This spot is over by the TV in the living room, behind the dividing wall, so the signal is weak. Look for a location that can see the router directly. |
| B | Not quite. The kitchen is behind the kitchen wall - a little signal leaks through the doorway, but it is still weak. Look for a location that can see the router directly. |
| C | Not quite. This spot is far from the router and behind the dividing wall, so only weak signals reach it. Look for a location that can see the router directly. |

### 3.5 Task 2 — `los_creation`

Receiver: **antenna** (the TV). Four candidate cabinet placements, options A–D.

> The TV isn't getting a very strong signal - the furniture and walls are all impacting it. Where can you put this cabinet so that the TV gets the strongest signal?

**Files, per option**
`los_creation_<set>_<A|B|C|D>.csv` + `_heatmap.csv`
`Assets/Resources/Models/Lesson1/los_creation_<set>_<option>_cabinet.obj` (+ `.mtl`)

Opens on the preview option — A in set 1, **B in set 2** (A is the correct answer there).

#### Set 1 — correct answer **C**

Correct:

> Correct! The cabinet is off to the side of the TV, against the same wall, so it never crosses the line between the router and the TV. The direct path stays open and the TV keeps a strong signal - anything standing on that line blocks it.

| Wrong | Text |
|---|---|
| A | Not quite. The cabinet stands on the line between the router and the TV, about two metres short of it, so the direct path is blocked. Only weaker signals that bounce off the walls reach the TV. Try a spot that is out of that line. |
| B | Not quite. The cabinet is right in front of the TV, blocking the direct path from the router. Only weaker reflected signals reach the TV. Try a spot that is out of that line. |
| D | Not quite. The cabinet is further back, just in front of the couch, but it is still on the line between the router and the TV, so the direct path is blocked. Try a spot that is out of that line. |

#### Set 2 — correct answer **A**

Correct:

> Correct! The cabinet is off to the side of the TV, against the same wall, so it never crosses the line between the router and the TV. The direct path stays open, and the cabinet even bounces an extra path toward the TV - anything standing on that line would block it instead.

| Wrong | Text |
|---|---|
| B | Not quite. The cabinet stands on the line between the router and the TV, a couple of metres short of it, so the direct path is blocked. Only weaker signals that bounce off the walls reach the TV. Try a spot that is out of that line. |
| C | Not quite. The cabinet is about halfway along the line between the router and the TV, so it still blocks the direct path. Try a spot that is out of that line. |
| D | Not quite. The cabinet is over near the router, but it is still on the line to the TV - it blocks the direct path right where the signal starts out. Try a spot that is out of that line. |

> **Data note.** The cabinet models in `Resources/Models/Lesson1` are the v3 set, where all eight
> `los_creation` cabinets were repositioned and widened to 1.6 m. The explanation text above still
> describes the earlier v2 placements.

### 3.6 Task 3 — `reflection_creation`

Receiver: **antenna** (the TV). Router is in the other room, with no line of sight to the TV. Four
candidate cabinet placements, options A–D. This is the only task with an intro screen.

**Intro:**

> Someone glued your router to the table in the other room, so now you're getting a bad signal again. You can't move it back, but you might not need to in order to get a good signal.

**Question:**

> Some materials, like metal, can reflect signals! It seems like placing this metal cabinet somewhere can improve the signal strength at the TV. Can you place the cabinet so that the TV gets the best signal strength?

**Files, per option**
`reflection_creation_<set>_<A|B|C|D>.csv` + `_heatmap.csv`
`Assets/Resources/Models/Lesson1/reflection_creation_<set>_<option>_cabinet.obj` (+ `.mtl`)

Opens on the preview option (A in both sets).

#### Set 1 — correct answer **D**; Set 2 — correct answer **B**

Both sets use the same correct text, with *reflected path* highlighted:

> Correct! The metal cabinet acts like a mirror: the signal from the router bounces off it, travels through the doorway and reaches the TV. That reflected path is now the strongest one arriving at the TV, which is why the signal improves so much.

| Set | Wrong | Text |
|---|---|---|
| 1 | A | Not quite. Over in the far corner of the office the cabinet faces the wrong way - its reflection goes off to the side instead of through the doorway. Try to place it so the router 'mirrors' onto the TV. |
| 1 | B | Not quite. Here the cabinet stands in the doorway and blocks it, instead of reflecting the signal through it. Try to place it so the router 'mirrors' onto the TV. |
| 1 | C | Not quite. Right next to the TV the cabinet has nothing useful to reflect - the signal still has to get through the wall to reach it. Try to place it so the router 'mirrors' onto the TV. |
| 2 | A | Not quite. In the corner of the living room next to the TV the cabinet has nothing useful to reflect - the signal still has to get through the wall to reach it. Try to place it so the router 'mirrors' onto the TV. |
| 2 | C | Not quite. On the office side of the doorway the cabinet does not aim the router's signal through it. Try to place it so the router 'mirrors' onto the TV. |
| 2 | D | Not quite. Back in the office beside the router, the cabinet has no angle on the doorway, so no strong bounce reaches the TV. Try to place it so the router 'mirrors' onto the TV. |

#### `ExplainCorrect` visualisation

`HighlightReflectionRays_CreateReflection()` highlights every path with an interaction point inside
the correct cabinet's bounds — green at 3× thickness. The CSV labels interactions only as `I`, so the
cabinet's live renderer bounds (expanded 0.15 m per side) are what identify them.

Paths highlighted: **3 of 7** in set 1 option D, **6 of 7** in set 2 option B.

#### `ExplainTransmission` — the extra screen

Shown only after `reflection_creation`, between `ExplainCorrect` and `Complete`:

> Some materials like walls, can let signal through, but absorb part of it, and weaken it substantially. This wall absorbs a lot of the signal, and only has a single strong path that passes through it.
>
> Its much easier for the signal to travel around the wall, with the help of the cabinet you just placed!

The cabinet-reflection highlights are cleared, then `HighlightLoS_CreateReflection()` draws a green
3×-thickness straight line from the router to the TV. **This line is synthetic** — no path in either
correct dataset runs straight from router to TV, since the wall blocks it. That is the point: it
shows the route the signal *cannot* take.

### 3.7 `PromptExplanation`

Shown after every committed MCQ answer in Lesson 1, before the outcome is revealed:

> Explain to the researcher why you chose your answer.

Answer buttons hidden. **Gate:** `WaitTimer(10)` — Next is locked for 10 seconds. Back stays live.

### 3.8 `Complete`

> Lesson 1 complete! Thank you. Please wait for the researcher.

Next hands over to `Task2Manager`.

---

## 4. Lesson 2 — `Task2Manager`

Two halves. **T1** is a single phone exchanging texts with the router; **T2** introduces a second
phone, interference, and the two ways of avoiding it (space, then time).

**Receiver model:** phone, for every receiver in the lesson.
**Dataset on entry:** `lesson2/communication_single` (+ `_heatmap`).
**Initial particle message:** `"Hey want to hang out tonight?"`

Receivers: `RX_YOU` = `Rx_Number` 1, `RX_FRIEND` = `Rx_Number` 2.
Colours: your signal **blue**, your friend's **green**. Messages: yours `"Hello"`, theirs `"Goodbye"`.

All T2 animations run as `MoveTempParticles` instances built from the *reversed* paths — the phones
transmit up to the router, so the CSV's router→phone paths are played backwards. The main particle
system is parked (`HideAllMovingRays`) for the whole of T2, and the transport buttons drive the
temporary animations whenever any are alive.

### 4.1 State machine

```
T1_Start -> T1_Antennas -> T1_Texting -> T1_FriendTexts -> T1_YouText
   -> T2_SameRoom -> T2_BothTransmit -> T2_Garbled
   -> T2_MCQ -> T2_PromptExplanation -> T2_MoveHassle      (correct)
                                    \-> T2_Explain -> back to T2_MCQ  (wrong)
   -> T2_TakeTurns -> T2_TurnsPlaying -> T2_TurnsFriend -> T2_TurnsDone -> Complete
```

### 4.2 `T1_Start`

> You've finished the first task. Move to the dining room area for the next.

**Gate:** none.

### 4.3 `T1_Antennas`

> Like we showed in the earlier example. Your phone and TV have antennas that let them receive signal. They can also use this to transmit signal!

**Gate:** none.

### 4.4 `T1_Texting`

> When you're texting your friend over the internet, your phone sends and receives signals from the router!

Highlighted words: *friend*, *internet*, *router* — in green. Ring on the router.
**Gate:** none.

### 4.5 `T1_FriendTexts`

> Your friend texts you: "Hey want to hang out tonight?"
>
>  Your router sends that text in a signal.

Rings cleared. Runs on the **main** particle system, router → phone.
**Particle message:** `"Hey want to hang out tonight?"` **Gate:** `WaitPlay()`.

### 4.6 `T1_YouText`

> You respond: "Yeah sure, lets hang out at 8?"
>
>  Your phone sends a signal back to the router.

A `MoveTempParticles` instance runs the same paths reversed, in blue.
**Particle message:** `"Yeah sure, lets hang out at 8?"` **Gate:** `WaitPlay()`.

Next enters interference: the main particles are parked and
`lesson2/interference_baseline` is loaded.

### 4.7 `T2_SameRoom`

> What happens when you and your friend are both texting while in the same room?

Highlighted word: *friend* — in green. Ring on the friend's phone.
**Data:** `lesson2/interference_baseline.csv` + `_heatmap.csv`. **Gate:** none.

### 4.8 `T2_BothTransmit`

> What if both your phones transmit at the same time?
>
> You send "Hello", your friend sends "Goodbye"

Highlighted word: *friend* — in green. Two animations are built, paused: yours in blue carrying
`"Hello"`, your friend's in green carrying `"Goodbye"`. Play starts both together, so the two
messages arrive at the router at once.
**Gate:** `WaitPlay()` — the collision cannot be skipped.

### 4.9 `T2_Garbled`

> You can see that the message is much harder to read. This is called interference, and happens when transmitters send signals at the same time.

**Gate:** none. Next clears the heatmap, sets the selection to **A**, and loads
`lesson2/interference_space_A` so the friend's phone is standing at option A when the question
appears.

### 4.10 `T2_MCQ` — `friend_placement`

> You tell your friend to move to interfere less with your message. You want "Hello" to appear as clearly as possible.
>
> Where would you tell them to move?

Four options A–D, each a place the friend could move to; letter badges mark them.
**Correct answer: D.**

**Files, per option** — `lesson2/interference_space_<A|B|C|D>.csv` + `_heatmap.csv`.
Selecting an option loads that dataset, which moves the friend's phone and rebuilds both animations;
a black `Highlight Sprite` ring marks the friend's phone. Defaults to **A** every time the screen is
entered.

| Wrong | Text |
|---|---|
| A | Not quite. Your friend barely moved - they are still in the same room with a clear line to the router, so their signal arrives almost as loudly as yours and the message stays garbled. |
| B | Not quite. Your friend is much further away now, but still in the same room with a clear line to the router. The room bounces the signal around so well that the extra distance hardly helps. |
| C | Closer! Putting a wall between your friend and the router does help a lot, but their signal still arrives strongly enough to muddle yours. Try somewhere further away with more in the way. |

Fallback, if `selected` is somehow outside A–C on a wrong answer:

> Not quite. Try moving your friend further away, with more walls in between.

### 4.11 `T2_PromptExplanation`

> Explain to the researcher why you chose your answer.

Answer buttons hidden. **Gate:** `WaitTimer(10)`. Wrong answers go to `T2_Explain` (which returns to
`T2_MCQ`); a correct answer goes to `T2_MoveHassle`.

### 4.12 `T2_MoveHassle`

> Correct! But, moving is a hassle, surely your friend doesn't want to move all the way to the other room just so you can send your text message.

Option markers dropped. **Data:** reloads `lesson2/interference_baseline` — the friend goes back to
where they started, so the turn-taking beat that follows solves the *original* crowded case in time
rather than in space. Rings on both phones. **Gate:** none.

### 4.13 `T2_TakeTurns`

> Another way to avoid interference is by programming your phones to take turns sending their signal.

**Gate:** none.

### 4.14 `T2_TurnsPlaying` — your turn

> Your phone goes first, then your friend's.

Only your phone's animation is built, in blue, carrying `"Hello"`.
**Gate:** deferred — `WaitPlay(releaseOnPlayPress: false)`. Next stays locked until the message has
actually *arrived*, not merely until Play is pressed, so the turn cannot be skipped mid-flight.

### 4.15 `T2_TurnsFriend` — your friend's turn

> Now your friend's phone takes its turn.

Turn 1 is torn down first, which clears the message it delivered, so the router starts clean. Your
friend's animation is built in green, carrying `"Goodbye"`, at the same height as turn 1.
**Gate:** `WaitPlay()`.

### 4.16 `T2_TurnsDone`

> You can see that this makes the messages clear, but this takes longer! This kind of turn-taking slows down your wireless communication, making your connection slower.

**Gate:** none. Next runs `Cleanup()` — animations stopped, markers cleared, main particles restored
and paused — and enters `Complete`.

### 4.17 `Complete`

> You've finished the study!

`TrialLog.Write()` fires here: the whole session is written to disk.

---

## 5. Session logging — `TrialLog`

Started from `MoveAsParticleTest1_v2.Start()`, written when Lesson 2 reaches `Complete` (and again
from `OnApplicationQuit` as a safety net for a run that ends early). Both files land in
`<game root>/Trials` — the project folder in the editor, the folder holding the executable in a
build. Times are Unix epoch seconds to millisecond resolution; the filename stamp is local time.

### `Trial_<MM_DD_YY>_<HH_MM>_viz_usage.json`

When each visualisation was on screen.

```json
[
	{ "viz": "session",   "toggle": "on",  "time": 1756482301.472 },
	{ "viz": "heatmap",   "toggle": "on",  "time": 1756482355.118 },
	{ "viz": "rays",      "toggle": "on",  "time": 1756482361.904 },
	{ "viz": "animation", "toggle": "on",  "time": 1756482370.221 },
	{ "viz": "animation", "toggle": "off", "time": 1756482378.663 }
]
```

- `session` / `on` — one entry, the start of the run.
- `heatmap`, `rays` — sampled every frame from the actual on-screen state, so manager-driven and
  dataset-driven changes are captured as well as button presses. Unchanged states record nothing, so
  the tear-down-and-redraw inside a dataset swap produces no spurious pair.
- `animation` — `on` the first frame particles move; `off` only when they all arrive. Pausing and
  resuming stays inside one on/off pair. In Lesson 2, where two animations run side by side, the run
  closes when the last of them finishes.

### `Trial_<MM_DD_YY>_<HH_MM>_answers.json`

Every multiple-choice press.

```json
[
	{ "task": "Task1Manager", "name": "los_creation",     "set": 1, "answer": "A", "submitted": false, "time": 1756482401.310 },
	{ "task": "Task1Manager", "name": "los_creation",     "set": 1, "answer": "D", "submitted": false, "time": 1756482412.887 },
	{ "task": "Task1Manager", "name": "los_creation",     "set": 1, "answer": "D", "submitted": true,  "time": 1756482419.045 },
	{ "task": "Task2Manager", "name": "friend_placement", "set": 0, "answer": "D", "submitted": true,  "time": 1756482980.512 }
]
```

- `submitted: false` — the option was pressed, which previews its dataset. `true` — Next was pressed
  on it, committing the answer. A committed option therefore appears twice.
- `set` is the Lesson 1 counterbalancing set; Lesson 2 is not counterbalanced and uses `0`.
- Lesson 1's set-selection buttons are not logged as answers.
