# WiViz lesson 1 – Part 1 datasets, **v3** (Sionna RT)

A copy of `../lesson1_v2` with two changes (everything else – file format, naming, folder layout,
heights, heatmap convention, solver settings, answer key – is exactly as in v2; read
`../lesson1_v2/README.md` for those):

1. **`los_creation` redesigned** (both sets): furniture-anchored cabinet spots spread along the whole
   router→TV link, the non-blocking spot embedded among the blockers. These 8 conditions are the only
   ones that were re-simulated (new scenes, cabinet OBJs, heatmaps, TV-height maps, RSS).
2. **`phone_optimization` and `reflection_creation`: rays re-selected, data unchanged.** Router, TV/phone
   positions, correct answers, RSS values, heatmaps and raw path exports are byte-identical to v2 (the
   validator enforces it); only the 7-ray selection changed so that every cross-room file shows
   **exactly one ray that transmits through a wall** and all other rays are reflections.

```bash
cd WiViz_v2/lesson1_v3
/home/saif/miniconda3/envs/sionna-rt/bin/python scripts/generate_lesson1.py --dry-run   # cabinet geometry checks
/home/saif/miniconda3/envs/sionna-rt/bin/python scripts/generate_lesson1.py             # re-simulates los_creation, re-selects the rest
/home/saif/miniconda3/envs/sionna-rt/bin/python scripts/check_lesson1.py                # 0 failures / 0 warnings
/home/saif/miniconda3/envs/sionna-rt/bin/python scripts/make_previews.py                # previews/*.png
```
(`--simulate-all` re-simulates every condition; note that the radio map is not bit-reproducible across
runs, so the heatmap CSVs would then differ from v2 in the 4th decimal.)

## 1. The v3 ray rule (all tasks)

Per file, at most 7 rays: (a) the direct (LoS) ray if it exists; (b) the strongest *reflection-only* ray
that bounces on the cabinet (cabinet tasks); (c) **exactly one through-wall ray** – the strongest path
that transmits through a wall – but only when router and receiver are in **different rooms**
(office / living room / kitchen); (d) the rest filled with reflection-only paths, strongest first,
geometric near-duplicates skipped. A path is "through-wall" when one of its interaction points has
collinear in/out directions (Sionna's slab transmission goes straight on); v2 files contained 0–2 such
rays unlabeled – now it is exactly one for cross-room links and none for same-room links.

| task | through-wall ray in | rays per file |
|---|---|---|
| phone_optimization | every NLoS option (A, C, D of set 1; A, B, C of set 2); none in the LoS option | 7 (6 in 1_B) |
| reflection_creation | every option (router in the office, TV in the living room) | 7 (5 in 1_B, 4 in 2_A – few reflection-only paths reach those distractors) |
| los_creation | none (router and TV in the living room) | 7 (1 in 2_B, see §2) |

## 2. `los_creation` v3 – "you're thinking of placing a cabinet in one of these 4 spots"

Receiver = TV (5.0, −7.6, 2.0) on the south wall; cabinet **1.6 × 0.4 × 2.2 m** (`LOS_CAB_SIZE`; a slim
sideboard – the 1.2 m box of the reflection task left only ~4 dB contrast because the wall bounce next to
the direct ray squeezed past it). All spots pass the placement checks (no overlap with furniture, walls
or the other three options) and the straight-line test decides who cuts the ray. `RSS` = TV-height map.

**Set 1 – router on the living-room desk** (7.309, 2.938, 0.9); the direct ray runs from the desk
over the couch to the TV.

| opt | cabinet centre (x, y), yaw | where | cuts ray? | RSS | rays |
|---|---|---|---|---|---|
| A | (6.25, −1.95), 0° | console behind the couch's back | yes | −63.1 | 7 |
| B | (7.20, 0.95), 0° | wardrobe at the foot of the desk, butted against the east wall, in front of the router | yes | −63.9 | 7 |
| **C ✔** | (6.90, −3.40), 90° | side unit at the couch's east end – right beside the ray | **no** | **−53.7 L** | 7 |
| D | (5.30, −6.00), 90° | tall cabinet in front of the TV stand's east end, beside the screen | yes | −60.8 | 7 |

Margin **7.2 dB**. The blockers are spread over the whole link (near the TV, behind the couch, at the
router); the correct spot sits *between* them and closest to the ray.

**Set 2 – router on the wall shelf** (0.15, −4.9, 1.1); the direct ray runs diagonally across the
south-west of the living room to the TV.

| opt | cabinet centre (x, y), yaw | where | cuts ray? | RSS | rays |
|---|---|---|---|---|---|
| **A ✔** | (0.25, −7.05), 90° | against the west wall just south of the shelf – right next to the router | **no** | **−49.5 L** | 7 |
| B | (0.55, −5.30), 90° | tall cabinet placed directly in front of the shelf | yes | −78.0 | 1 |
| C | (1.65, −5.74), 0° | free-standing divider between the shelf area and the TV area | yes | −63.1 | 7 |
| D | (2.90, −6.45), 0° | media cabinet beside the TV stand's west end | yes | −61.1 | 7 |

Margin **11.6 dB**. Option B swallows almost everything (8 raw paths, one cabinet-bounce ray left,
−78 dBm) – the "cabinet in front of the router" mistake; A is its neighbour along the same wall and
keeps the ray.

Correct letters unchanged: **C (set 1), A (set 2)**.

## 3. Validation (`scripts/check_lesson1.py`, 0 failures / 0 warnings)

Everything v2 checked (48 files, schema, Unity transform, 3000-cell floor heatmaps at 0.1 m, ≤ 7 rows,
RSS = radio-map spot, placement, answer keys, margin ≥ 6 dB) plus: per file exactly one through-wall
row iff router and receiver are in different rooms, LoS rows only where geometry says so; for the
unchanged tasks the heatmap CSV is byte-identical to v2 and `RSS_dBm` unchanged. A same-room file may
show its strongest ray slightly below 0 dB when the raw 0 dB path was a transmission through furniture
(warned, never more than −3 dB; none in the current data).

## 4. Caveats worth a manual look in Unity

1. The through-wall ray bends *on* the wall and continues straight; particles will pass through the
   wall there (dashed magenta in the previews). That is the intended "signals also go through objects".
2. `los_creation_2_B` has a single ray and `reflection_creation_2_A` four – sparse by physics, which is
   the point (weak link → few strong rays).
3. `los_creation` cabinets are 1.6 m wide; the reflection-task cabinets (v2 scenes) stay 1.2 m.
