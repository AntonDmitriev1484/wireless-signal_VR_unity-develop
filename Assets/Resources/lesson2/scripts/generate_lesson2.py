"""
End-to-end generator for the WiViz lesson-2 dataset (communication & interference).

Run with the sionna-rt conda env:
    /home/saif/miniconda3/envs/sionna-rt/bin/python generate_lesson2.py [--only NAME ...] [--dry-run]

--dry-run only checks the geometry (phones on open floor, geometric LoS report
router->phone, Phone B spots pairwise distinct) and exits; nothing is simulated.

Outputs (relative to WiViz_v2/lesson2/):
    communication_single.csv / _heatmap.csv          router -> Phone A baseline
    interference_baseline.csv / _heatmap.csv         Phone A + Phone B (initial spot)
    interference_space_{A..D}.csv / _heatmap.csv     Phone A + Phone B at option spot
    unity_models/base_scene/                         OBJ+MTL of the (unmodified) scene
    intermediate/<condition>/                        Sionna-coordinate raw / filtered CSVs + heatmap .npy
    metrics/<condition>.json, metrics/summary.json   incl. per-Rx RSS and SIR at the router

The scene is never modified, so there is no scenes/ directory and every
heatmap is the identical router-coverage map.
"""
from __future__ import annotations

import argparse
import json
import sys
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

import numpy as np  # noqa: E402

from conditions import CONDITIONS, PHONE_A, TX  # noqa: E402
from wiviz_scene import BASE_XML, export_scene_obj, los_blockers, on_open_floor  # noqa: E402

LESSON_DIR = HERE.parent
UNITY_DIR = LESSON_DIR / "unity_models"
INTER_DIR = LESSON_DIR / "intermediate"
METRICS_DIR = LESSON_DIR / "metrics"


def check_geometry(conds):
    """Pure-geometry report; returns the number of problematic conditions."""
    bad = 0
    seen_b = {}
    for c in conds:
        problems = []
        for label, rx in zip(c["rx_labels"], c["rxs"]):
            inside = on_open_floor(rx[:2])
            if inside:
                problems.append(f"{label} at {rx[:2]} stands inside {inside}")
            blockers = los_blockers(c["tx"], rx)
            print(f"    {c['name']:24s} {label:6s} {tuple(rx[:2])}: "
                  f"{'LoS' if not blockers else 'NLoS via ' + ', '.join(blockers)}")
        if len(c["rxs"]) > 1:
            b = tuple(c["rxs"][1][:2])
            if b in seen_b:
                problems.append(f"Phone B position duplicates {seen_b[b]}")
            seen_b[b] = c["name"]
            if np.linalg.norm(np.array(b) - np.array(PHONE_A[:2])) < 1.0:
                problems.append("Phone B closer than 1 m to Phone A")
        for p in problems:
            print(f"    PROBLEM {c['name']}: {p}")
        bad += bool(problems)
    return bad


_HEATMAP_CACHE = None  # (cells, rss_dbm): the Tx/scene never change -> one shared floor map


def run_one(cond):
    global _HEATMAP_CACHE
    import wiviz_sim as ws
    name = cond["name"]
    t0 = time.time()
    metrics, cells, rss = ws.run_condition(
        name=name, xml_path=BASE_XML, tx=cond["tx"], rxs=cond["rxs"],
        work_dir=INTER_DIR / name, final_dir=LESSON_DIR,
        heatmap=_HEATMAP_CACHE, verbose=False,
    )
    _HEATMAP_CACHE = (cells, rss)
    np.save(INTER_DIR / name / f"{name}_heatmap_sionna.npy",
            np.column_stack([cells, rss]).astype(np.float32))

    sir = None
    if len(cond["rxs"]) > 1:
        sir = metrics["rss_rx_dbm"][0] - metrics["rss_rx_dbm"][1]
    metrics.update({
        "rx_labels": cond["rx_labels"], "note": cond["note"],
        "correct": cond["correct"], "option": cond["option"],
        "sir_at_router_db": sir,
        "seconds": round(time.time() - t0, 1),
        "paths_csv": f"{name}.csv", "heatmap_csv": f"{name}_heatmap.csv",
    })
    METRICS_DIR.mkdir(parents=True, exist_ok=True)
    (METRICS_DIR / f"{name}.json").write_text(json.dumps(metrics, indent=1, default=float))
    return metrics


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", nargs="*", default=None, help="condition names to run")
    ap.add_argument("--dry-run", action="store_true", help="geometry checks only, no simulation")
    args = ap.parse_args()

    conds = CONDITIONS
    if args.only:
        conds = [c for c in CONDITIONS if c["name"] in args.only]

    print(f"Geometry check (router {TX}, Phone A {tuple(PHONE_A)}):")
    bad = check_geometry(conds)
    if bad:
        print(f"\n{bad} condition(s) with geometry problems - fix conditions.py first.")
        return 1
    if args.dry_run:
        return 0

    LESSON_DIR.mkdir(parents=True, exist_ok=True)
    export_scene_obj(BASE_XML, UNITY_DIR / "base_scene" / "home_office_doorways.obj")

    print()
    for c in conds:
        m = run_one(c)
        rss = ", ".join(f"{lab}={v:.1f}" for lab, v in zip(c["rx_labels"], m["rss_rx_dbm"]))
        rays = ", ".join(f"Rx{k}:{g['n_paths']}" for k, g in m["final"].items())
        sir = f"  SIR={m['sir_at_router_db']:+5.1f} dB" if m["sir_at_router_db"] is not None else ""
        print(f"{m['name']:24s} RSS[{rss}] dBm{sir}  rays[{rays}]  correct={m['correct']}  ({m['seconds']}s)")

    all_metrics = {}
    for p in sorted(METRICS_DIR.glob("*.json")):
        if p.name in ("summary.json", "check_report.json"):
            continue
        all_metrics[p.stem] = json.loads(p.read_text())
    (METRICS_DIR / "summary.json").write_text(json.dumps(all_metrics, indent=1, default=float))
    print(f"\n{len(all_metrics)} conditions in metrics/summary.json")
    return 0


if __name__ == "__main__":
    sys.exit(main())
