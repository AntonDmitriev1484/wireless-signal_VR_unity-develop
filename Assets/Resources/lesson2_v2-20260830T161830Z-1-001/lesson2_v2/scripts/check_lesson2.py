"""
Sanity checks for the generated WiViz lesson-2 dataset.

    /home/saif/miniconda3/envs/sionna-rt/bin/python check_lesson2.py

Checks: file inventory / naming, exact CSV schema, coordinate serialisation and
Unity conversion, floor-heatmap coverage (3000 cells at 0.1 m, identical across
conditions), per-Rx path groups and ray budgets, per-Rx RSS_dBm == floor spot
under that phone, Phone A / router fixed everywhere, and that option D is the
clearly best spatial-interference answer (lowest Phone-B power at the router).
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
from conditions import CONDITIONS, CORRECT, PHONE_A, TX  # noqa: E402
from utils2 import CSV_COLUMNS_WITH_RSS, sionna_xyz_to_unity  # noqa: E402
from wiviz_scene import los_blockers, on_open_floor  # noqa: E402
from wiviz_sim import HEATMAP_HEIGHT, RSS_SPOT_RADIUS_M, n_transmissions, rays_for_rss, room_of  # noqa: E402

LESSON_DIR = HERE.parent
INTER_DIR = LESSON_DIR / "intermediate"
V1_DIR = LESSON_DIR.parent / "lesson2"           # v2 must keep lesson2's heatmaps and RSS
MIN_MARGIN_DB = 8.0        # correct option's Phone-B power must undercut every other by this
EXPECTED_CELLS = 3000
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
    print(f"inventory: {len(expected & present)}/{len(expected)} expected files present")

    # ---- per file -----------------------------------------------------------
    info = {}
    ref_hm = None
    for c in CONDITIONS:
        n = c["name"]
        p_paths = LESSON_DIR / f"{n}.csv"; p_hm = LESSON_DIR / f"{n}_heatmap.csv"
        if not (p_paths.exists() and p_hm.exists()):
            continue
        h, rows = read(p_paths); check_schema(p_paths, h, rows)
        hh, hrows = read(p_hm); check_schema(p_hm, hh, hrows)
        check_unity_conversion(p_paths, INTER_DIR / n / f"{n}_paths_final_sionna.csv")
        check_unity_conversion(p_hm, INTER_DIR / n / f"{n}_heatmap_clamped_sionna.csv")

        # devices fixed / on open floor
        if tuple(c["tx"]) != tuple(float(v) for v in TX):
            fail(f"{n}: Tx moved")
        if tuple(c["rxs"][0]) != tuple(float(v) for v in PHONE_A):
            fail(f"{n}: Phone A moved")
        for label, rx in zip(c["rx_labels"], c["rxs"]):
            inside = on_open_floor(rx[:2])
            if inside:
                fail(f"{n}: {label} stands inside {inside}")

        # per-Rx path groups
        by_rx = {}
        for r in rows:
            by_rx.setdefault(int(r[0]), []).append(r)
        if sorted(by_rx) != list(range(1, len(c["rxs"]) + 1)):
            fail(f"{n}: Rx groups {sorted(by_rx)} != expected {list(range(1, len(c['rxs']) + 1))}")
        hc = np.array([parse_coords(r[5])[1] for r in hrows])
        hm_rss = np.array([float(r[4]) for r in hrows])
        tx_u = np.array(sionna_xyz_to_unity(*c["tx"]))
        rx_info = []
        for rx_num, rx in enumerate(c["rxs"], start=1):
            grp = by_rx.get(rx_num, [])
            if not grp:
                continue
            rss_vals = {r[4] for r in grp}
            if len(rss_vals) != 1:
                fail(f"{n}: RSS_dBm differs within Rx {rx_num}")
            rss = float(grp[0][4])
            if abs(max(float(r[1]) for r in grp)) > 1e-6:
                fail(f"{n}: Rx {rx_num} strongest relative power != 0.0000")
            cap = rays_for_rss(rss)
            if len(grp) > cap:
                fail(f"{n}: Rx {rx_num} has {len(grp)} rows > budget {cap} (RSS {rss:.1f})")
            if len(grp) < 2:
                fail(f"{n}: Rx {rx_num} has fewer than 2 rows")
            rx_u = np.array(sionna_xyz_to_unity(*rx))
            cs = [parse_coords(r[5]) for r in grp]
            if any(not np.allclose(cc[0], tx_u, atol=1e-4) or not np.allclose(cc[-1], rx_u, atol=1e-4) for cc in cs):
                fail(f"{n}: Rx {rx_num} endpoints do not match Tx/phone in Unity coordinates")
            # RSS = floor spot under the phone, open-floor cells
            d = np.linalg.norm(hc[:, [0, 2]] - rx_u[[0, 2]], axis=1)
            sel = hm_rss[d <= RSS_SPOT_RADIUS_M + 1e-6]
            hv = 10 * np.log10(np.mean(10 ** (sel / 10))) if sel.size else hm_rss[np.argmin(d)]
            if abs(hv - rss) > 0.05:
                fail(f"{n}: Rx {rx_num} floor spot ({hv:.2f}) != RSS_dBm ({rss:.2f})")
            if sel.size and np.any(sel <= -119.999):
                fail(f"{n}: Rx {rx_num} floor cells under the phone are clamped (not open floor)")
            has_los = any(int(r[3]) == 0 for r in grp)
            geo_los = not los_blockers(c["tx"], rx)
            if has_los != geo_los:
                fail(f"{n}: Rx {rx_num} LoS row present={has_los} but geometry says LoS={geo_los}")
            # v2 through-wall rule: exactly one transmission row iff router and phone are in different rooms
            n_thru = sum(1 for r, cc in zip(grp, cs) if int(r[3]) >= 1 and n_transmissions(cc) > 0)
            cross = room_of(c["tx"]) != room_of(rx)
            if n_thru != (1 if cross else 0):
                fail(f"{n}: Rx {rx_num} has {n_thru} through-wall rows, expected {1 if cross else 0} "
                     f"({room_of(c['tx'])} -> {room_of(rx)})")
            rx_info.append(dict(rss=rss, rows=len(grp), los=has_los))

        # heatmap coverage + identical across conditions (scene never changes)
        if len(hrows) != EXPECTED_CELLS:
            fail(f"{n}: heatmap has {len(hrows)} cells (expected {EXPECTED_CELLS})")
        yv = np.unique(hc[:, 1])
        xr = (hc[:, 0].min(), hc[:, 0].max()); zr = (hc[:, 2].min(), hc[:, 2].max())
        if not (xr[0] < -7.8 and xr[1] > 9.8 and zr[0] < -6.8 and zr[1] > 7.8):
            fail(f"{n}: heatmap does not cover the whole scene (x {xr}, z {zr})")
        if len(yv) != 1 or abs(yv[0] - HEATMAP_HEIGHT) > 1e-3:
            fail(f"{n}: heatmap height(s) {yv} (expected {HEATMAP_HEIGHT} m)")
        if hm_rss.min() < -120.0001:
            fail(f"{n}: heatmap below the -120 dBm floor")
        v1_hm = V1_DIR / f"{n}_heatmap.csv"
        if v1_hm.exists() and v1_hm.read_bytes() != p_hm.read_bytes():
            fail(f"{n}: heatmap differs from lesson2 (v2 must keep it)")
        if ref_hm is None:
            ref_hm = hm_rss
        elif not np.array_equal(ref_hm, hm_rss):
            fail(f"{n}: heatmap differs from the other conditions (should be identical)")
        info[n] = dict(rx=rx_info, correct=c["correct"], option=c["option"])

    # ---- interference answer key -------------------------------------------
    print("\nPhone-B power at the router (reciprocity: floor RSS under Phone B) per option:")
    opt_rss = {}
    for c in CONDITIONS:
        n = c["name"]
        if c["option"] is not None and n in info and len(info[n]["rx"]) == 2:
            opt_rss[c["option"]] = info[n]["rx"][1]["rss"]
    base = info.get("interference_baseline")
    rss_a = info["communication_single"]["rx"][0]["rss"] if "communication_single" in info else np.nan
    for o in sorted(opt_rss):
        mark = "*" if o == CORRECT else " "
        print(f"  option {o}{mark}: PhoneB {opt_rss[o]:7.1f} dBm   SIR {rss_a - opt_rss[o]:+5.1f} dB")
    if base is not None and len(base["rx"]) == 2:
        print(f"  baseline : PhoneB {base['rx'][1]['rss']:7.1f} dBm   SIR {rss_a - base['rx'][1]['rss']:+5.1f} dB")
    if opt_rss:
        best = min(opt_rss, key=opt_rss.get)
        margin = min(opt_rss[o] for o in opt_rss if o != CORRECT) - opt_rss[CORRECT]
        if best != CORRECT:
            fail(f"lowest Phone-B power is option {best}, not the designated {CORRECT}")
        elif margin < MIN_MARGIN_DB:
            warn(f"margin of option {CORRECT} is {margin:.1f} dB < {MIN_MARGIN_DB} dB")
        else:
            print(f"  option {CORRECT} undercuts the runner-up by {margin:.1f} dB")
    rss_as = {n: d["rx"][0]["rss"] for n, d in info.items() if d["rx"]}
    if len(set(rss_as.values())) != 1:
        fail(f"Phone A RSS varies across conditions: {rss_as}")

    print(f"\n{len(fails)} failures, {len(warns)} warnings")
    (LESSON_DIR / "metrics").mkdir(exist_ok=True)
    (LESSON_DIR / "metrics" / "check_report.json").write_text(
        json.dumps(dict(failures=fails, warnings=warns, info=info), indent=1))
    return 1 if fails else 0


if __name__ == "__main__":
    sys.exit(main())
