# WiViz lesson 2 – **v2** (communication & interference)

A copy of `../lesson2` with one change: **the ray sets were re-selected so that every
cross-room receiver shows exactly one through-wall ray**. Nothing was re-simulated – router,
phones, RSS values, heatmaps, raw path exports and the answer key are byte-for-byte those of
`lesson2` (the validator enforces it). Read `../lesson2/README.md` for the full design, positions,
solver settings and the interference answer key; this file only documents the delta.

```bash
cd WiViz_v2/lesson2_v2
/home/saif/miniconda3/envs/sionna-rt/bin/python scripts/generate_lesson2.py    # re-selects rays from intermediate/ (no Sionna)
/home/saif/miniconda3/envs/sionna-rt/bin/python scripts/check_lesson2.py       # 0 failures / 0 warnings
/home/saif/miniconda3/envs/sionna-rt/bin/python scripts/make_previews.py       # previews/lesson2_overview.png
```
(`generate_lesson2.py --simulate-all` re-simulates everything instead.)

## The v2 ray rule (per receiver)

1. the direct (LoS) ray, if it exists;
2. **exactly one through-wall ray** – the strongest path that *transmits through a wall* – but only
   when the router and that phone are in **different rooms** (office / living room / kitchen);
3. the remaining budget (`rays_for_rss`: 6 → 2 rays with falling RSS, unchanged) filled with
   **reflection-only** paths, strongest first, geometric near-duplicates skipped.

A path counts as "through-wall" when one of its interaction points has collinear in/out directions
(Sionna's slab transmission continues straight); reflections change direction. Same-room links get
no through-wall ray (lesson2 had 0–1 such leave-and-return rays mixed in; they are gone now).

## What changed per file

| condition | Phone A (office, same room as router) | Phone B | through-wall rays |
|---|---|---|---|
| communication_single | 6 rays, LoS + 5 reflections | – | 0 |
| interference_baseline | 6 (same) | office: 6 rays, LoS + 5 reflections | 0 / 0 |
| interference_space_A | 6 | office: 6, LoS + reflections | 0 / 0 |
| interference_space_B | 6 | office NW: 6, LoS + reflections | 0 / 0 |
| interference_space_C | 6 | living room: 4 = **1 through the dividing wall** + 3 reflections through the doorway | 0 / 1 |
| interference_space_D ✔ | 6 | kitchen: 2 = **1 through the kitchen wall** + 1 reflection via the kitchen door | 0 / 1 |

RSS / SIR / answer key: identical to lesson2 (Phone B at the router −48.4 / −51.5 / −51.9 / −65.9 /
**−82.5 dBm**, option D correct by 16.7 dB).

## Validation (`scripts/check_lesson2.py`)
Everything lesson2 checked, plus: per receiver exactly one through-wall row iff router and phone are
in different rooms, all other rows reflection-only; every heatmap CSV byte-identical to lesson2's;
RSS unchanged. Result: 0 failures, 0 warnings.

## Caveat
The through-wall ray is drawn like any other ray: its bend point lies *on* the wall surface and the
ray continues straight behind it (Unity particles pass through the wall there). In the previews it is
the dashed magenta ray.
