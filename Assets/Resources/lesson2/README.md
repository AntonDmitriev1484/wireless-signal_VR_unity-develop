# WiViz lesson 2 – communication & interference datasets (Sionna RT)

Generated 2026‑08‑24 with Sionna RT 1.2.1 (`sionna-rt` conda env, Mitsuba `cuda_ad_mono_polarized`)
from the immutable base scene `xml_3d_models/Home Office With Doors xml/home_office_doorways.xml`.
Same CSV contract, coordinate conversion, heatmap convention and folder style as `../lesson1_v2`.

```bash
cd WiViz_v2/lesson2
/home/saif/miniconda3/envs/sionna-rt/bin/python scripts/generate_lesson2.py --dry-run   # geometry checks only
/home/saif/miniconda3/envs/sionna-rt/bin/python scripts/generate_lesson2.py             # ~30 s for all 6 conditions
/home/saif/miniconda3/envs/sionna-rt/bin/python scripts/check_lesson2.py                # validation (0 failures / 0 warnings)
/home/saif/miniconda3/envs/sionna-rt/bin/python scripts/make_previews.py                # previews/lesson2_overview.png
```

## 1. What the lesson teaches and what Unity does

Signals carry messages (Phone A "Hello", Phone B "Goodbye"); two phones transmitting at once collide
at the router – the router hears a **strong but garbled** signal (strength ≠ readability); moving the
interferer away / behind walls fixes it **spatially**; taking turns fixes it **in time** (TDMA).

Only **router → phone** Sionna data exists. Unity reverses these paths for phone → router playback
(reciprocity: identical geometry and per-path power), plays both phones simultaneously or in turns,
and renders the messages. **No reverse-direction or TDMA datasets were generated on purpose.**

Because of reciprocity, the floor-heatmap value under Phone B *is* Phone B's received power at the
router (both phones transmit at the same 10 dBm as the router). So the existing `RSS_dBm`
convention doubles as the interference measure: **SIR at the router = RSS(Phone A) − RSS(Phone B).**

## 2. What is here (12 final CSVs + support)

```
lesson2/
├── communication_single.csv / _heatmap.csv        router -> Phone A (normal communication)
├── interference_baseline.csv / _heatmap.csv       Phone A + Phone B at its initial spot
├── interference_space_A..D.csv / _heatmap.csv     Phone A + Phone B at option A..D
├── README.md                                       this file
├── unity_models/base_scene/home_office_doorways.obj|.mtl   the (unmodified) scene for Unity
├── intermediate/<condition>/                       Sionna-coordinate raw / power-filtered / final CSVs
│                                                   + <condition>_heatmap_sionna.npy (x,y,z,dBm)
├── metrics/<condition>.json, summary.json, check_report.json
├── previews/lesson2_overview.png                   2x3 figure, all six conditions
└── scripts/  conditions.py · wiviz_sim.py · wiviz_scene.py ·
              generate_lesson2.py · check_lesson2.py · make_previews.py
```

**The scene is never modified** (no extra objects), so there is no `scenes/` directory, and all six
heatmap CSVs are the **byte-identical** router-coverage map (the generator computes the radio map
once and shares it across conditions; regenerate all six together to keep that property).

### CSV contract (unchanged from lesson 1)

```
Rx_Number,Path_Power_dBm,Interaction_Description,Total_Interactions_for_Path,RSS_dBm,Interaction_Coordinates
```

written by `utils2.export_paths_to_csv` / `export_radio_map_to_csv` and converted with
`utils2.transform_csv_for_unity` (Sionna `(x, y, z)` → Unity `(−x, z, −y)`, 5-decimal serialisation).
Final CSVs are Unity-ready; do not convert again.

* Path CSV: `Rx_Number = 1` is **always Phone A**, `Rx_Number = 2` (two-phone files) is Phone B.
  `Path_Power_dBm` is relative **per receiver** (0 dB = strongest path of that phone).
  `RSS_dBm` is constant within an Rx group = floor-heatmap spot under that phone.
  Rows are ordered Rx 1 first, strongest path first within each group.
* Heatmap CSV: one row per 0.3 m × 0.3 m cell of the 18 × 15 m scene at **z = 0.1 m**
  (Unity y = 0.1), 3000 rows, −120 dBm noise floor – exactly the lesson1_v2 convention.

## 3. Devices (Sionna coordinates)

| device | position | notes |
|---|---|---|
| router (Tx, fixed everywhere) | **(−6.0, −5.6, 0.9)** | on the office desk in the **SW corner of the office/conference room** – the largest room (≈130 m² incl. the kitchenette alcove, vs living room 96 m²). Same spot as lesson1_v2's reflection set 2. |
| Phone A (fixed everywhere) | **(−3.0, −3.0, 1.1)** | office open floor, clear LoS to the router, ~4 m |
| Phone B baseline | **(−2.5, −6.5, 1.1)** | office SE, clear LoS, ~3.6 m – as loud as Phone A at the router |
| Phone B option A | (−1.0, −5.5, 1.1) | small move within the office, still LoS |
| Phone B option B | (−6.5, 4.5, 1.1) | office far NW corner (10 m away along the west wall), still LoS |
| Phone B option C | (4.0, −1.5, 1.1) | living-room centre, behind the dividing wall |
| Phone B option D ✔ | **(6.5, 6.0, 1.1)** | kitchen, ~13 m away and behind the kitchen + dividing walls |

All phones stand on open floor (mesh-accurate vertical-column check in
`wiviz_scene.on_open_floor`; the kitchen spot lies between the counters).

## 4. Results and the answer key

`RSS` = floor heatmap under the phone = that phone's power at the router (reciprocity).
Phone A is **−47.3 dBm** at the router in every condition.

| condition | Phone B RSS (dBm) | SIR at router | Phone B rays | LoS row for B |
|---|---|---|---|---|
| interference_baseline | −48.4 | **+1.1 dB → garbled** | 6 | yes |
| interference_space_A | −51.5 | +4.2 dB | 6 | yes |
| interference_space_B | −51.9 | +4.6 dB | 6 | yes |
| interference_space_C | −65.9 | +18.6 dB | 4 | no |
| **interference_space_D ✔** | **−82.5** | **+35.3 dB → clean** | **2** | no |

**Option D is correct**: the kitchen phone is far from the router *and* behind two concrete walls, so
its power at the router is **16.7 dB below the runner-up (C)** and 34 dB below the baseline – the
router hears Phone A's message clearly. Options A and B teach that staying in the same room (still
line of sight) barely helps, even 10 m away: the concrete room is too reflective. Option C (one
wall) helps but visibly less. The single margin the study relies on (D vs C) is 16.7 dB –
unambiguous.

Per-condition ray counts, RSS values and SIR are also in `metrics/summary.json`
(`rss_rx_dbm`, `sir_at_router_db`, `ray_caps`).

## 5. Solver, materials, path curation

| item | value |
|---|---|
| radio | 5 GHz / 1 MHz / 10 dBm, 1×1 iso antennas, V-pol (as lessons 0/1) |
| `itu_concrete` thickness | 0.2 m (walls attenuate ≈ 20 dB → the kitchen option is clearly cold) |
| PathSolver | depth 4, LoS + specular + refraction, no diffuse/diffraction, `max_num_paths_per_src = 1e6`, `samples_per_src = 4e6`, seed 41 (lesson1_v2 settings; see its README for the pitfalls) |
| RadioMapSolver | depth 5, LoS + specular + refraction + diffraction, 128 M samples, 0.3 m cells at z = 0.1 m, computed **once** and shared |
| power filter | per receiver, paths within 30 dB of that receiver's strongest |
| geometric de-dup | 20-point resampled-polyline distance ≤ 1.5 → "visibly the same route", skipped |
| **ray budget per receiver** | scales with link quality (`wiviz_sim.rays_for_rss`): RSS ≥ −55 → 6 rays, ≥ −65 → 5, ≥ −72 → 4, ≥ −80 → 3, below → 2. The LoS row is always kept when it exists. So a distant/blocked phone visibly loses strong rays (Phone B: 6 → 6 → 6 → 4 → 2 across baseline/A/B/C/D) while Phone A always shows 6. |

## 6. Validation (`scripts/check_lesson2.py`, 0 failures / 0 warnings)

* all 12 final CSVs exist, schema identical to `utils2.CSV_COLUMNS_WITH_RSS`, 5-decimal coordinate
  triples, every final CSV equals its Sionna intermediate after the Unity transform;
* Tx fixed at (−6.0, −5.6, 0.9) and Phone A at (−3.0, −3.0, 1.1) in every condition; every phone on
  open floor; Phone B positions pairwise distinct;
* Rx groups exactly {1} (single) / {1, 2} (two-phone); per-Rx strongest path at 0.0000 dB; per-Rx
  row counts within the ray budget; path endpoints equal router/phone in Unity coordinates;
* per-Rx `RSS_dBm` equals the 0.3 m floor-heatmap spot under that phone (open, un-clamped cells);
* LoS rows exactly where the geometry has line of sight (baseline, A, B – not C, D);
* heatmaps: 3000 cells at Unity y = 0.1 covering the whole scene, byte-identical across all six files;
* answer key: option D's Phone-B power lowest, ≥ 8 dB (actual 16.7 dB) below every other option.

## 7. Caveats worth a manual look in Unity

1. **Reversed playback.** Unity flips Tx↔Rx per row; per-path geometry/power are reciprocal so this
   is exact, but remember `Rx_Number = 1` is Phone A in every file when wiring the two message streams.
2. **Through-wall rays.** Refraction is on, so Phone B's two kitchen rays (and C's) cross walls with
   the bend point on the wall; particles will visibly pass through the wall carrying the wall loss –
   same behaviour as lesson 1. Set `refraction=False` in `wiviz_sim.PATH_SOLVER_CONFIG` to forbid it
   (deep-NLoS files then get very few or zero rays).
3. **Identical heatmaps.** All six heatmap files are the same map by design (fixed router, unchanged
   scene). If Unity diffs files by content it can safely cache one copy.
4. **The heatmap shows the router's coverage**, which is the right visual for both directions of the
   lesson (downlink signal strength and, by reciprocity, how loudly each floor position talks back to
   the router). Near the router (its own room) the floor is saturated dark at the display clip (−40).
5. **Option B's spot** (office NW) is intentionally almost as bad as A despite being 10 m away –
   distance alone in a reflective LoS room buys ~4 dB. That is the teaching point, not an error.
6. Previews are top-down in **Sionna** coordinates (not Unity).
