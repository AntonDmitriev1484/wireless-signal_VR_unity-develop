"""
Sanity checks for the generated WiViz lesson-1 **v2** dataset.

    /home/saif/miniconda3/envs/sionna-rt/bin/python check_lesson1.py

Checks: file inventory / naming, exact CSV schema, coordinate serialisation and
Unity conversion, floor-heatmap coverage (3000 cells at 0.1 m), <= 7 path rows,
RSS_dBm == radio-map spot at the condition's rss_point (floor map for phones,
receiver-height map for the TV), cabinet placement (no overlaps with furniture,
walls or the sibling options), and that the designated correct option is
supported by the data (RSS margin, LoS / cabinet-bounce rows).
"""
from __future__ import annotations

import csv
import json
import re
import sys
from pathlib import Path

import numpy as np

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, "/home/saif/wireless_NeRF_experiments/SIONNA")
from conditions import CONDITIONS, CORRECT  # noqa: E402
from generate_lesson1 import geometry_report  # noqa: E402
from utils2 import CSV_COLUMNS_WITH_RSS, sionna_xyz_to_unity  # noqa: E402
from wiviz_scene import box_corners_xy  # noqa: E402
from wiviz_sim import HEATMAP_HEIGHT, MAX_PATH_ROWS, RSS_SPOT_RADIUS_M, n_transmissions, point_in_box_xy, room_of  # noqa: E402

LESSON_DIR = HERE.parent
INTER_DIR = LESSON_DIR / "intermediate"
V2_DIR = LESSON_DIR.parent / "lesson1_v2"        # unchanged tasks must keep v2's heatmaps
RESIMULATED_TASKS = {"los_creation"}
MIN_MARGIN_DB = 6.0        # correct option must beat every distractor by this
EXPECTED_CELLS = 3000      # 18 x 15 m at 0.3 m
COORD_RE = re.compile(r"^-?\d+\.\d{5} -?\d+\.\d{5} -?\d+\.\d{5}$")

fails, warns = [], []


def fail(msg):
    fails.append(msg); print("FAIL:", msg)


def warn(msg):
    warns.append(msg); print("WARN:", msg)


def read(path):
    with open(path, newline="") as f:
        r = csv.reader(f)
        header = next(r)
        rows = list(r)
    return header, rows


def parse_coords(s):
    parts = s.split(", ")
    if not all(COORD_RE.match(p) for p in parts):
        return None
    return np.array([[float(v) for v in p.split()] for p in parts])


def spot_mean(xy_cells, vals, point, radius=RSS_SPOT_RADIUS_M):
    d = np.linalg.norm(xy_cells - np.asarray(point, float)[None, :], axis=1)
    sel = vals[d <= radius + 1e-6]
    if sel.size == 0:
        sel = vals[[int(np.argmin(d))]]
    return float(10 * np.log10(np.mean(10 ** (sel / 10)))), sel


def check_schema(path, header, rows):
    if header != CSV_COLUMNS_WITH_RSS:
        fail(f"{path.name}: header {header} != {CSV_COLUMNS_WITH_RSS}")
    for i, row in enumerate(rows):
        if len(row) != 6:
            fail(f"{path.name}: row {i} has {len(row)} columns"); break
        if parse_coords(row[5]) is None:
            fail(f"{path.name}: row {i} coordinate serialisation invalid: {row[5][:60]}"); break
        try:
            int(row[0]); float(row[1]); int(row[3]); float(row[4])
        except ValueError:
            fail(f"{path.name}: row {i} non-numeric field"); break
        if not re.fullmatch(r"Tx(-I)*-Rx", row[2]):
            fail(f"{path.name}: row {i} bad Interaction_Description {row[2]}"); break
        if row[2].count("-I") != int(row[3]):
            fail(f"{path.name}: row {i} interaction count mismatch"); break


def check_unity_conversion(final_path, sionna_path):
    if not sionna_path.exists():
        warn(f"{final_path.name}: no Sionna intermediate to compare against"); return
    _, fr = read(final_path); _, sr = read(sionna_path)
    if len(fr) != len(sr):
        fail(f"{final_path.name}: row count differs from intermediate ({len(fr)} vs {len(sr)})"); return
    for a, b in zip(fr[:200], sr[:200]):
        if a[:5] != b[:5]:
            fail(f"{final_path.name}: non-coordinate fields changed by Unity transform"); return
        ca, cb = parse_coords(a[5]), parse_coords(b[5])
        exp = np.array([sionna_xyz_to_unity(*p) for p in cb])
        if not np.allclose(ca, exp, atol=1e-5):
            fail(f"{final_path.name}: Unity coordinate conversion mismatch"); return


def main():
    names = [c["name"] for c in CONDITIONS]
    print(f"Checking {len(names)} conditions in {LESSON_DIR}\n")

    # ---- inventory ----------------------------------------------------------
    expected = set()
    for n in names:
        expected |= {f"{n}.csv", f"{n}_heatmap.csv"}
    present = {p.name for p in LESSON_DIR.glob("*.csv")}
    for f in sorted(expected - present):
        fail(f"missing file {f}")
    extra = present - expected
    if extra:
        warn(f"unexpected CSVs in output dir: {sorted(extra)}")
    pattern = re.compile(r"^(phone_optimization|los_creation|reflection_creation)_[12]_[ABCD](_heatmap)?\.csv$")
    for f in expected:
        if not pattern.match(f):
            fail(f"name does not follow storyboard pattern: {f}")
    print(f"inventory: {len(expected & present)}/{len(expected)} expected files present")

    # ---- per file -------------------------------------------------------------
    info = {}
    for c in CONDITIONS:
        n = c["name"]
        p_paths = LESSON_DIR / f"{n}.csv"; p_hm = LESSON_DIR / f"{n}_heatmap.csv"
        if not (p_paths.exists() and p_hm.exists()):
            continue
        h, rows = read(p_paths); check_schema(p_paths, h, rows)
        hh, hrows = read(p_hm); check_schema(p_hm, hh, hrows)
        check_unity_conversion(p_paths, INTER_DIR / n / f"{n}_paths_final_sionna.csv")
        check_unity_conversion(p_hm, INTER_DIR / n / f"{n}_heatmap_clamped_sionna.csv")

        # geometry: cabinet placement and straight-line LoS blocking
        problems, cuts_los = geometry_report(c)
        for pr in problems:
            fail(f"{n}: cabinet placement - {pr}")
        if c["task"] == "los_creation" and cuts_los == c["correct"]:
            fail(f"{n}: cabinet {'cuts' if cuts_los else 'does not cut'} the Tx->Rx line but correct={c['correct']}")

        # paths
        if not rows:
            fail(f"{n}: path CSV has no rows")
        if len(rows) > MAX_PATH_ROWS:
            fail(f"{n}: {len(rows)} path rows (> {MAX_PATH_ROWS})")
        rx_numbers = {r[0] for r in rows}
        if rx_numbers != {"1"}:
            fail(f"{n}: Rx_Number values {rx_numbers} (expected only '1')")
        rss_vals = {r[4] for r in rows}
        if len(rss_vals) != 1:
            fail(f"{n}: RSS_dBm differs between rows of the same Rx")
        rss = float(rows[0][4]) if rows else np.nan
        has_los = any(int(r[3]) == 0 for r in rows)
        strongest_rel = max(float(r[1]) for r in rows) if rows else np.nan
        # 0 dB = strongest RAW path of the receiver; the v3 rule may drop it when it is a
        # through-wall path in a same-room file, so the strongest SHOWN ray may sit slightly below 0.
        if rows and (strongest_rel > 1e-6 or strongest_rel < -3.0):
            fail(f"{n}: strongest path relative power is {strongest_rel}, expected 0.0000 (or > -3 dB)")
        elif rows and strongest_rel < -1e-6:
            warn(f"{n}: strongest shown ray is {strongest_rel:.2f} dB (the 0 dB raw path is a through-wall path excluded by the rule)")
        # Unity-space Tx/Rx consistency
        cs = [parse_coords(r[5]) for r in rows]
        tx_u = np.array(sionna_xyz_to_unity(*c["tx"])); rx_u = np.array(sionna_xyz_to_unity(*c["rx"]))
        if any(not np.allclose(cc[0], tx_u, atol=1e-4) or not np.allclose(cc[-1], rx_u, atol=1e-4) for cc in cs):
            fail(f"{n}: path endpoints do not match the condition's Tx/Rx in Unity coordinates")
        # v3 through-wall rule: exactly one transmission row iff Tx and Rx are in different rooms
        n_thru = sum(1 for r, cc in zip(rows, cs) if int(r[3]) >= 1 and n_transmissions(cc) > 0)
        cross = room_of(c["tx"]) != room_of(c["rx"])
        if n_thru != (1 if cross else 0):
            fail(f"{n}: {n_thru} through-wall rows, expected {1 if cross else 0} "
                 f"({room_of(c['tx'])} -> {room_of(c['rx'])})")
        if c["task"] not in RESIMULATED_TASKS:
            v2_hm = V2_DIR / f"{n}_heatmap.csv"
            if v2_hm.exists() and v2_hm.read_bytes() != p_hm.read_bytes():
                fail(f"{n}: heatmap differs from lesson1_v2 (unchanged tasks must keep their data)")
            v2_paths = V2_DIR / f"{n}.csv"
            if v2_paths.exists():
                _, v2rows = read(v2_paths)
                if {r[4] for r in v2rows} != rss_vals:
                    fail(f"{n}: RSS_dBm differs from lesson1_v2")
        # cabinet bounce (Unity coords of the cabinet footprint)
        cab_bounce = False
        if c["boxes"]:
            b = c["boxes"][0]
            corners = box_corners_xy(b["size"], b["center"], b["yaw_deg"])
            cu = np.array([[-x, -y] for x, y in corners])  # Unity (x,z) = (-x_s, -y_s)
            for cc in cs:
                for v in cc[1:-1]:
                    if point_in_box_xy((v[0], v[2]), cu[::-1]) or point_in_box_xy((v[0], v[2]), cu):
                        cab_bounce = True
        # heatmap (floor)
        hc = np.array([parse_coords(r[5])[1] for r in hrows])
        hm_rss = np.array([float(r[4]) for r in hrows])
        if len(hrows) != EXPECTED_CELLS:
            fail(f"{n}: heatmap has {len(hrows)} cells (expected {EXPECTED_CELLS} for 18x15 m at 0.3 m)")
        xr = (hc[:, 0].min(), hc[:, 0].max()); zr = (hc[:, 2].min(), hc[:, 2].max()); yv = np.unique(hc[:, 1])
        if not (xr[0] < -7.8 and xr[1] > 9.8 and zr[0] < -6.8 and zr[1] > 7.8):
            fail(f"{n}: heatmap does not cover the whole scene (x {xr}, z {zr})")
        if len(yv) != 1 or abs(yv[0] - HEATMAP_HEIGHT) > 1e-3:
            fail(f"{n}: heatmap height(s) {yv} (expected {HEATMAP_HEIGHT} m)")
        if hm_rss.min() < -120.0001:
            fail(f"{n}: heatmap below the -120 dBm floor")
        # RSS_dBm must equal the spot mean of the radio map around rss_point
        # (floor heatmap for phones; receiver-height map for the TV).
        rp_u = np.array([-c["rss_point"][0], -c["rss_point"][1]])      # Unity (x, z)
        if c["rss_mode"] == "floor":
            hv, sel = spot_mean(hc[:, [0, 2]], hm_rss, rp_u)
            if np.any(sel <= -119.999):
                fail(f"{n}: floor cells under the receiver are not open floor (clamped cells in the RSS spot)")
            src = "floor heatmap"
        else:
            npy = INTER_DIR / n / f"{n}_rxheight_map_sionna.npy"
            if not npy.exists():
                fail(f"{n}: rss_mode={c['rss_mode']} but {npy.name} is missing"); hv = np.nan
            else:
                arr = np.load(npy)
                if abs(float(np.unique(np.round(arr[:, 2], 3))[0]) - c["rx"][2]) > 1e-3:
                    fail(f"{n}: receiver-height map is not at the receiver height {c['rx'][2]}")
                hv, _ = spot_mean(arr[:, :2], np.maximum(arr[:, 3], -120.0), c["rss_point"])
            src = f"radio map at {c['rx'][2]} m"
        if abs(hv - rss) > 0.05:
            fail(f"{n}: {src} spot mean at rss_point ({hv:.2f}) != path RSS_dBm ({rss:.2f})")
        info[n] = dict(rss=rss, rows=len(rows), los=has_los, cab_bounce=cab_bounce, cuts_los=cuts_los,
                       rss_mode=c["rss_mode"], hm_max=float(hm_rss.max()),
                       hm_floor_cells=int(np.sum(hm_rss <= -119.999)))

    # ---- answer-key support -------------------------------------------------
    print("\nOption ranking (RSS_dBm of the path files):")
    for (task, s), corr in sorted(CORRECT.items()):
        opts = {o: info.get(f"{task}_{s}_{o}") for o in "ABCD"}
        if any(v is None for v in opts.values()):
            fail(f"{task} set {s}: missing conditions"); continue
        line = "  ".join(f"{o}={opts[o]['rss']:7.1f}{'*' if o == corr else ' '}{'L' if opts[o]['los'] else ' '}{'R' if opts[o]['cab_bounce'] else ' '}" for o in "ABCD")
        best = max("ABCD", key=lambda o: opts[o]["rss"])
        margin = opts[corr]["rss"] - max(opts[o]["rss"] for o in "ABCD" if o != corr)
        print(f"  {task:20s} set {s}: {line}   margin={margin:+.1f} dB")
        if best != corr:
            fail(f"{task} set {s}: best RSS is option {best}, not the designated {corr}")
        elif margin < MIN_MARGIN_DB:
            warn(f"{task} set {s}: margin {margin:.1f} dB < {MIN_MARGIN_DB} dB")
        if task in ("los_creation", "phone_optimization"):
            for o in "ABCD":
                if (o == corr) != opts[o]["los"]:
                    fail(f"{task} set {s} option {o}: LoS row present={opts[o]['los']} but correct={o == corr}")
        if task == "reflection_creation":
            if not opts[corr]["cab_bounce"]:
                fail(f"{task} set {s}: correct option has no cabinet-bounce path row")
            for o in "ABCD":
                if opts[o]["los"]:
                    fail(f"{task} set {s} option {o}: unexpected LoS row")
    print("  (* = designated correct, L = LoS row present, R = cabinet-bounce row present)")

    print(f"\n{len(fails)} failures, {len(warns)} warnings")
    (LESSON_DIR / "metrics").mkdir(exist_ok=True)
    (LESSON_DIR / "metrics" / "check_report.json").write_text(json.dumps(dict(failures=fails, warnings=warns, info=info), indent=1))
    return 1 if fails else 0


if __name__ == "__main__":
    sys.exit(main())
