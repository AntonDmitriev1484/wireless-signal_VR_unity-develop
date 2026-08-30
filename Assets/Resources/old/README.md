# WiViz lesson 1 – Part 1 datasets, **v2** (Sionna RT)

Generated 2026‑08‑21 with Sionna RT 1.2.1 (`sionna-rt` conda env, Mitsuba `cuda_ad_mono_polarized`)
from the immutable base scene
`xml_3d_models/Home Office With Doors xml/home_office_doorways.xml`. Same file format, naming and
folder layout as `../lesson1`; only the content decisions below changed.

```bash
cd WiViz_v2/lesson1_v2
/home/saif/miniconda3/envs/sionna-rt/bin/python scripts/generate_lesson1.py --dry-run   # geometry checks only
/home/saif/miniconda3/envs/sionna-rt/bin/python scripts/generate_lesson1.py             # ~40 s for all 24 conditions
/home/saif/miniconda3/envs/sionna-rt/bin/python scripts/check_lesson1.py                # sanity checks + answer-key support
/home/saif/miniconda3/envs/sionna-rt/bin/python scripts/make_previews.py                # previews/*.png
```

## Changes vs lesson1

| | lesson1 | **lesson1_v2** |
|---|---|---|
| heatmap plane | 1.1 m (phone height) | **0.1 m – on the floor**, like `demo_heatmap_v2` |
| heatmap cells | 0.1 m, 27 000 rows, 2.3 MB per file | **0.3 m, 3 000 rows, 250 kB per file** |
| rays per file | 12–13 | **exactly 7** (LoS ray and strongest cabinet‑bounce ray always kept, the rest strongest‑first with geometric de‑duplication) |
| receiver of `los_creation` / `reflection_creation` | phone floating at 1.1 m | **the TV of the demo data, (5.0, −7.6, 2.0)**, fixed in both sets |
| routers | 1.25 m, floating above the furniture | **resting on furniture**: living‑room desk (demo Tx), conference table, office desk, wall shelf |
| metal cabinet | 2.0 × 0.6 × 2.2 m | **1.2 × 0.4 × 2.2 m**; the four cabinets of a set never overlap each other, furniture or walls (checked automatically) |
| `RSS_dBm` | heatmap at the phone | phones: **floor heatmap under the phone**; TV: radio map **at the TV's height** (see §4) |
| phone spots | one on the couch | all on open floor |

---

## 1. What is here

```
lesson1_v2/
├── <task>_<set>_<option>.csv           24 Unity path CSVs   (7 rays each: static rays + animated particles)
├── <task>_<set>_<option>_heatmap.csv   24 Unity heatmap CSVs (whole scene, floor, 3 000 cells)
├── README.md                            this file
├── scenes/<condition>/                  modified Sionna scene per condition (XML + meshes/, base untouched)
├── unity_models/
│     ├── base_scene/home_office_doorways.obj|.mtl        unmodified scene
│     └── <condition>/<condition>.obj|.mtl                 full scene of that condition
│                      <condition>_cabinet.obj|.mtl        only the metal cabinet (los/reflection tasks)
├── intermediate/<condition>/            Sionna‑coordinate CSVs: paths raw → power‑filtered → final,
│                                        heatmap raw / floor‑clamped, heatmap .npy (x,y,z,dBm),
│                                        TV conditions also: <condition>_rxheight_map_sionna.npy (map at 2.0 m)
├── metrics/<condition>.json, summary.json, check_report.json
├── previews/<task>_<set>_overview.png   2×2 figure per task/set: floor heatmap + final rays + router/receiver/cabinets
└── scripts/
      conditions.py        declarative table of the 24 conditions (edit here to move things)
      wiviz_scene.py       scene‑variant builder + Unity OBJ/MTL export + placement checks (overlap / walls)
      wiviz_sim.py         Sionna run + CSV export for one condition (uses utils2.py unchanged)
      generate_lesson1.py  driver (--dry-run = geometry only)   check_lesson1.py  validator   make_previews.py  figures
```

File naming follows the storyboard exactly: `<taskname>_<set>_<MCQ option>[_heatmap].csv`
with `taskname ∈ {phone_optimization, los_creation, reflection_creation}`, `set ∈ {1, 2}`,
`option ∈ {A, B, C, D}`. 3 tasks × 2 sets × 4 options × 2 files = **48 CSVs** (6 MB in total).

### CSV contract (unchanged)

Both file types use the existing six‑column schema written by `utils2.export_paths_to_csv` /
`utils2.export_radio_map_to_csv` and converted with `utils2.transform_csv_for_unity`
(Sionna `(x, y, z)` → Unity `(−x, z, −y)`, 5‑decimal serialisation):

```
Rx_Number,Path_Power_dBm,Interaction_Description,Total_Interactions_for_Path,RSS_dBm,Interaction_Coordinates
```

* Path CSV: one receiver per file, so `Rx_Number` is always `1`; **7 rows**. `Path_Power_dBm` is the
  *relative* per‑path value (0 dB = strongest path of that file). `RSS_dBm` is the same number on every
  row of a file (definition in §4).
* Heatmap CSV: one row per **0.3 m × 0.3 m** cell of the 18 m × 15 m scene at **z = 0.1 m** (Unity y = 0.1),
  `Interaction_Coordinates = "Tx, cell"`, `Path_Power_dBm = 0`, `Total_Interactions_for_Path = 0` – exactly
  as in `demo_heatmap_v2/radio_map_grid_room_unity.csv`, just coarser.

### 3D models for Unity

As in lesson1: `unity_models/*.obj` are written directly from the PLY meshes with the old study's Blender
convention (`forward = −Z, up = Y`, OBJ vertex = `(x, z, −y)`); Unity negates X on import, which yields the
CSV convention `(−x, z, −y)`. Per condition you get the full scene **and** a cabinet‑only OBJ
(`Metal_Cabinet` group, `itu_metal`).

---

## 2. Scene, radio and solver settings

| item | value | why |
|---|---|---|
| base scene | `home_office_doorways.xml` – office (x −8…0, kitchenette alcove to x −10), living room (x 0…8, y −8…4), kitchen (x 0…8, y 4…7). Dividing wall x = 0 with a 3 m doorway at y 0.68…3.68 and a 1 m gap at y 4.26…5.26; kitchen wall y = 4 with a 1 m door at x 2…3 | as provided |
| TV (receiver of los/reflection tasks) | **(5.0, −7.6, 2.0)** – the demo Rx, 0.2 m in front of the top‑right corner of the TV panel (panel x 3.18…4.97, y ≈ −7.63, top 2.2 m) on the south wall of the living room | same object/location Unity already uses for the demo |
| phone (receiver of phone_optimization) | 1.1 m above open floor | hand‑held |
| routers | `TX_LIVING_DESK = (7.309, 2.938, 0.9)` demo Tx, on the living‑room desk; `TX_CONF_TABLE = (−3.9, 0.8, 1.0)` on the conference table; `TX_OFFICE_DESK = (−6.0, −5.6, 0.9)` on the office desk (SW corner); `TX_SHELF = (0.15, −4.9, 1.1)` on the 1.0 m board of the wall shelf (west wall of the living room) | routers rest on furniture now that the heatmap plane is on the floor (in lesson1 they had to float above the 1.1 m heatmap plane) |
| frequency / bandwidth / Tx power | 5 GHz / 1 MHz / 10 dBm, 1×1 iso antennas, V‑pol | as before |
| `itu_concrete` slab thickness | 0.2 m | walls attenuate ≈ 20 dB → NLoS options are clearly cold |
| metal cabinet | box **1.2 × 0.4 × 2.2 m**, `itu_metal`, `meshes/Metal_Cabinet.ply` + one `<shape>` | slim ("not dense looking"); 2.2 m tall so that it still cuts the line to the 2.0 m TV |
| PathSolver | depth 4, LoS + specular + refraction, no diffuse / diffraction, `max_num_paths_per_src = 1e6`, `samples_per_src = 4e6`, seed 41 | as lesson1 (see its README for the pitfalls) |
| RadioMapSolver | depth 5, LoS + specular + refraction + diffraction, `samples_per_tx = 1.28e8`, **cell 0.3 m, plane z = 0.1 m**, full scene bbox → 60 × 50 cells | floor heatmap like the demo; 9× fewer cells load much faster in Unity |
| heatmap floor | cells below −120 dBm set to −120 dBm (5–43 cells per file, inside furniture / outside the house) | keeps Unity colour scales usable |

### Path selection (7 rays)

`wiviz_sim.select_final_rows`: (1) the LoS row, if it exists, and the strongest row that bounces on the
cabinet are always included (taken from the raw export even if weak); (2) the remaining slots are filled
strongest‑first from the paths within 30 dB of the strongest one, skipping any path whose 20‑point
resampled polyline lies within 1.5 m (Euclidean, all points) of an already selected one – so the seven
rays take visibly different routes. Raw valid paths per receiver: 23–494.

Because refraction is on, some rays in NLoS files pass *through* a wall (interaction point on the wall,
ray continues straight); Unity renders that point like a bounce and `Path_Power_dBm` carries the wall loss.

---

## 3. Conditions, answer key and mechanism

All coordinates Sionna (x, y[, z]); yaw = rotation of the cabinet's long axis about z. `L` = the path CSV
contains a direct `Tx-Rx` row, `R` = it contains a ray bouncing on the cabinet. "floor in front" = the
floor heatmap value 1 m in front of the TV stand, for reference only (see §4).

### phone_optimization – "where should you stand to get the best signal?"

| set | router | option | phone (x, y) | RSS | | why |
|---|---|---|---|---|---|---|
| 1 | living desk | A | (1.5, 5.5) kitchen | −64.7 | | close to the router but behind the kitchen wall |
| 1 | living desk | **B ✔** | (4.0, 0.0) living room | **−57.1** | L | same room, open floor, direct line of sight |
| 1 | living desk | C | (−5.0, −4.5) office, south | −75.3 | | behind the dividing wall |
| 1 | living desk | D | (−2.5, −6.5) office, SE corner | −78.3 | | behind the dividing wall |
| 2 | conference table | A | (4.0, −5.5) living room, between couch and TV | −80.4 | | behind the dividing wall |
| 2 | conference table | B | (2.5, 5.2) kitchen | −65.2 | | behind the kitchen wall (the doorway leaks some signal towards the kitchen door) |
| 2 | conference table | C | (6.5, −1.5) living room, east | −70.0 | | far and behind the wall |
| 2 | conference table | **D ✔** | (−4.0, −4.0) office, south of the table | **−58.6** | L | same room, direct line of sight |

Margin correct‑vs‑best‑distractor: **7.6 dB (set 1), 6.6 dB (set 2)**. The floor heatmap is hot throughout the
router's room and drops behind every wall; only the correct option shows the thick direct ray. The heatmap is
identical for the four options of a set (same scene and router).

### los_creation – "where can the metal cabinet go without hurting the TV?"

Router and TV are fixed with line of sight. Three cabinet spots stand **on the direct line** (staggered
left/right of it at different distances from the TV – with a 1.2 m wide cabinet an off‑line spot would not
block anything); the fourth stands beside the TV stand along the south wall and keeps the direct ray.
The free floor in front of the TV is only the strip between the TV stand (front y = −6.87) and the couch /
coffee table (y = −4.6), which is why the spots are 1.3–4.3 m from the TV instead of lesson1's 1.5 m arc.

| set | router | option | cabinet centre (x, y), yaw | RSS at TV | | floor in front | why |
|---|---|---|---|---|---|---|---|
| 1 | living desk | A | (5.75, −5.65), 0° | −63.1 | R | −69.1 | on the line, 2.0 m from the TV – blocks LoS |
| 1 | living desk | B | (5.00, −6.30), 0° | −60.4 | R | −85.1 | on the line, 1.3 m in front of the TV – blocks LoS |
| 1 | living desk | **C ✔** | (1.50, −7.50), 0° | **−52.9** | L | −65.6 | west of the TV stand – LoS kept |
| 1 | living desk | D | (5.30, −5.00), 0° | −59.5 | R | −65.7 | on the line, just in front of the couch – blocks LoS |
| 2 | wall shelf | **A ✔** | (6.90, −7.50), 0° | **−49.6** | L R | −49.3 | east of the TV stand – LoS kept |
| 2 | wall shelf | B | (2.99, −6.48), 0° | −60.8 | R | −49.4 | on the line, 2.3 m from the TV – blocks LoS |
| 2 | wall shelf | C | (2.12, −6.00), 60.9° | −65.3 | R | −59.0 | on the line, 3.3 m from the TV – blocks LoS |
| 2 | wall shelf | D | (1.24, −5.51), 60.9° | −71.0 | R | −70.0 | on the line, 4.3 m from the TV (near the shelf) – blocks LoS |

Margin: **6.6 dB (set 1), 11.2 dB (set 2)**. In the correct option the thick direct ray is present; in the three
blocking options it is gone and the strongest rays arrive via walls/ceiling or via the cabinet face. The
"floor in front" column shows why the TV's RSS cannot be read from the floor: a cabinet that cuts the line to
the 2.0 m TV often leaves the floor cell 1 m in front of the stand untouched (set 1 D: −65.7 vs −65.6 without
the cabinet), while a cabinet standing right in front of the stand darkens it by 20 dB (set 1 B).

### reflection_creation – "place the metal cabinet so the TV gets the best signal"

Router in the office with no line of sight to the TV (the direct line hits the x = 0 wall south of the
doorway). One cabinet position mirrors the router through the 3 m doorway onto the TV
(`conditions.mirror_yaw`: the face normal bisects router→cabinet and cabinet→TV; the reflected ray crosses
x = 0 at y ≈ 1.4 (set 1) / 1.8 (set 2), well inside the doorway, and passes above the couch).

| set | router | option | cabinet centre (x, y), yaw | RSS at TV | | why |
|---|---|---|---|---|---|---|
| 1 | conference table | A | (−6.5, 4.5), 0° | −76.5 | R | office NW – reflects towards the wrong side |
| 1 | conference table | B | (0.9, 2.2), 90° | −77.6 | | living‑room side of the doorway – obstructs the doorway |
| 1 | conference table | C | (6.9, −7.5), 0° | −76.9 | R | next to the TV, wrong side |
| 1 | conference table | **D ✔** | (−1.0, 3.5), −9.3° | **−59.3** | R | office, north of the doorway, face towards the doorway – mirrors the router onto the TV (+17.2 dB) |
| 2 | office desk | A | (6.5, −6.5), 90° | −73.8 | R | living‑room SE corner, next to the TV |
| 2 | office desk | **B ✔** | (1.3, 3.4), −10.2° | **−61.5** | R | living room below the kitchen wall, face towards the doorway – mirrors the router onto the TV (+10.7 dB) |
| 2 | office desk | C | (−0.9, 2.2), 90° | −72.2 | R | office side of the doorway |
| 2 | office desk | D | (−7.0, −0.5), 90° | −72.6 | | office, north of the router along the west wall |

Margin: **17.2 dB (set 1), 10.7 dB (set 2)**. In the correct option the strongest ray (0 dB relative) is
`Tx → cabinet → TV` and the floor heatmap shows a bright band leaving the cabinet, crossing the doorway and
reaching the TV stand; the distractors only get wall‑penetrating / multi‑bounce rays 10–17 dB weaker.

### Answer key (unchanged)

| task | set 1 | set 2 |
|---|---|---|
| phone_optimization | **B** | **D** |
| los_creation | **C** | **A** |
| reflection_creation | **D** | **B** |

---

## 4. How `RSS_dBm` is defined

* **Phones** (`rss_mode = "floor"`): linear mean of the floor‑heatmap cells within 0.3 m of the phone's
  (x, y) – i.e. the number Unity's heatmap shows under the phone. The validator checks that those cells are
  open floor (not clamped) and that the value matches the CSV exactly.
* **TV** (`rss_mode = "rx_height"`): the floor directly under the TV lies inside the TV stand, and the floor
  in front of the stand does not react to what blocks or mirrors the line to the TV (see the "floor in front"
  column – with floor cells the los_creation margins were 0.1 dB and two answer keys failed). `RSS_dBm`
  is therefore the linear mean of the cells within 0.3 m of (5.0, −7.45) of a **second radio map computed
  at the TV's height (2.0 m)** with identical solver settings (`intermediate/<condition>/
  <condition>_rxheight_map_sionna.npy`). The displayed floor heatmap is unchanged. This is a per‑condition
  switch in `conditions.py` (`rss_mode`, `rss_point`); set it to `"floor"` and regenerate to go back to
  floor cells.

---

## 5. Validation performed (`scripts/check_lesson1.py`, 0 failures / 0 warnings)

* all 48 expected files exist, names match the storyboard pattern, no stray CSVs;
* header identical to `utils2.CSV_COLUMNS_WITH_RSS`, 6 columns per row, numeric fields parse,
  `Interaction_Description` ↔ `Total_Interactions_for_Path` consistent, every coordinate is a 5‑decimal triple;
* every final CSV equals its Sionna‑coordinate intermediate after `(x, y, z) → (−x, z, −y)`; first/last points of
  every ray equal the condition's router / receiver in Unity coordinates;
* heatmaps: 3 000 cells at Unity y = 0.1 spanning the whole scene, none below −120 dBm;
* path files: exactly 7 rows, `Rx_Number = 1`, strongest row at 0.0000 dB;
* `RSS_dBm` equals the radio‑map spot mean defined in §4 (floor heatmap / 2.0 m map);
* cabinet placement: no cabinet overlaps furniture, crosses a wall, contains the router/receiver, or overlaps
  a cabinet of the other three options of its set (≥ 5 cm gap); in `los_creation` the cabinet cuts the straight
  router→TV segment exactly for the three distractors;
* answer key: the designated option has the highest RSS in every task/set (margin ≥ 6 dB); LoS rows exist
  **only** for the correct option in `phone_optimization` and `los_creation`; the correct
  `reflection_creation` option contains a cabinet‑bounce row and no option has a LoS row.

---

## 6. Caveats / things worth a manual look

1. **TV RSS vs floor heatmap.** Unity shows the floor; the TV's number comes from the 2.0 m map (§4). Around the
   TV stand the floor is dark (inside the stand) – consider not drawing cells inside furniture.
2. **Floor heatmap with low routers.** Routers at 0.9–1.1 m light the floor of their own room very strongly
   (many cells above −50 dBm) and chair/table legs cast small shadows; the previews clip at −40…−100 dBm.
3. **los_creation set 1** cabinets all stand in the 2.2 m strip between TV stand and couch (there is no other
   free floor on the line); the lesson is carried by the rays (direct ray present/absent) and the RSS margin
   (6.6 dB), the floor shadow is only visible for option B.
4. **Through‑wall rays** (refraction on) as in lesson1 – set `refraction=False` in
   `wiviz_sim.PATH_SOLVER_CONFIG` if rays must never cross walls (deep‑NLoS files then get very few rays).
5. **Monte‑Carlo noise**: fixed seed, regeneration is deterministic.
6. Heatmap previews are top‑down in **Sionna** coordinates (not Unity).
