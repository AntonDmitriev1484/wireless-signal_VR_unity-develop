"""
End-to-end generator for the WiViz lesson-1 **v2** dataset.

Run with the sionna-rt conda env:
    /home/saif/miniconda3/envs/sionna-rt/bin/python generate_lesson1.py [--only NAME ...] [--dry-run]

--dry-run only checks the geometry (cabinet overlaps with furniture / walls /
the other options, which cabinets cut the direct router->receiver line) and
exits; nothing is simulated or written.

Outputs (relative to WiViz_v2/lesson1_v2/):
    <task>_<set>_<option>.csv            Unity path CSV (rays + particles, <= 7 rows)
    <task>_<set>_<option>_heatmap.csv    Unity heatmap CSV (floor, 0.1 m, 0.3 m cells)
    scenes/<condition>/                  modified Sionna scene (XML + meshes)
    unity_models/<condition>/            OBJ+MTL of the scene and of the cabinet
    intermediate/<condition>/            Sionna-coordinate raw / filtered CSVs
    metrics/<condition>.json, metrics/summary.json
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

from conditions import CONDITIONS, siblings  # noqa: E402
from wiviz_scene import (BASE_XML, box_corners_xy, create_scene_variant, export_scene_obj,  # noqa: E402
                         placement_problems, segment_hits_box)

LESSON_DIR = HERE.parent
SCENES_DIR = LESSON_DIR / "scenes"
UNITY_DIR = LESSON_DIR / "unity_models"
INTER_DIR = LESSON_DIR / "intermediate"
METRICS_DIR = LESSON_DIR / "metrics"


def geometry_report(cond):
    """Placement problems of the condition's cabinet(s) and whether a cabinet
    cuts the straight Tx->Rx segment (pure geometry, no ray tracing)."""
    problems, cuts_los = [], False
    for b in cond["boxes"]:
        others = [ob for sib in siblings(cond) for ob in sib["boxes"]]
        names = [sib["name"] for sib in siblings(cond) for _ in sib["boxes"]]
        problems += placement_problems(b, others=others, names=names,
                                       keep_out_points=[cond["tx"][:2], cond["rx"][:2], cond["rss_point"]])
        if segment_hits_box(cond["tx"], cond["rx"], b["size"], b["center"], b["yaw_deg"], b.get("z0", 0.0)):
            cuts_los = True
    return problems, cuts_los


def check_geometry(conds):
    bad = 0
    for c in conds:
        problems, cuts = geometry_report(c)
        flag = "cuts LoS" if cuts else "LoS free"
        status = "OK " if not problems else "BAD"
        print(f"{status} {c['name']:26s} {flag:9s} {'correct' if c['correct'] else '       '}  {'; '.join(problems)}")
        bad += bool(problems)
    return bad


def run_one(cond):
    import wiviz_sim as ws
    name = cond["name"]
    t0 = time.time()
    scene_dir = SCENES_DIR / name
    xml = create_scene_variant(scene_dir, cond["boxes"], base_xml=BASE_XML)

    box_corners = None
    if cond["boxes"]:
        b = cond["boxes"][0]
        box_corners = box_corners_xy(b["size"], b["center"], b["yaw_deg"])

    metrics, cells, rss = ws.run_condition(
        name=name, xml_path=xml, tx=cond["tx"], rx=cond["rx"],
        work_dir=INTER_DIR / name, final_dir=LESSON_DIR,
        box_corners=box_corners, rss_point=cond["rss_point"], rss_mode=cond["rss_mode"],
        floor_point=cond.get("floor_point"), verbose=False,
    )
    np.save(INTER_DIR / name / f"{name}_heatmap_sionna.npy",
            np.column_stack([cells, rss]).astype(np.float32))

    # Unity models: whole scene + cabinet only (if any).
    udir = UNITY_DIR / name
    export_scene_obj(xml, udir / f"{name}.obj")
    if cond["boxes"]:
        export_scene_obj(xml, udir / f"{name}_cabinet.obj",
                         include={f"mesh-{b['name']}" for b in cond["boxes"]})

    problems, cuts_los = geometry_report(cond)
    metrics.update({
        "task": cond["task"], "set": cond["set"], "option": cond["option"],
        "correct": cond["correct"], "note": cond["note"], "rx_label": cond["rx_label"],
        "boxes": cond["boxes"], "geometry": {"placement_problems": problems, "cabinet_cuts_los": cuts_los},
        "seconds": round(time.time() - t0, 1),
        "paths_csv": f"{name}.csv", "heatmap_csv": f"{name}_heatmap.csv",
        "scene_xml": str(xml.relative_to(LESSON_DIR)),
    })
    METRICS_DIR.mkdir(parents=True, exist_ok=True)
    (METRICS_DIR / f"{name}.json").write_text(json.dumps(metrics, indent=1, default=float))
    return metrics


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", nargs="*", default=None, help="condition names (or task names) to run")
    ap.add_argument("--dry-run", action="store_true", help="geometry checks only, no simulation")
    args = ap.parse_args()

    conds = CONDITIONS
    if args.only:
        conds = [c for c in CONDITIONS if c["name"] in args.only or c["task"] in args.only]

    print("Geometry check (cabinet vs furniture / walls / sibling options; straight Tx->Rx line):")
    bad = check_geometry(conds)
    if bad:
        print(f"\n{bad} condition(s) with placement problems - fix conditions.py first.")
        return 1
    if args.dry_run:
        return 0

    LESSON_DIR.mkdir(parents=True, exist_ok=True)
    # Base scene OBJ (unchanged geometry) for Unity.
    export_scene_obj(BASE_XML, UNITY_DIR / "base_scene" / "home_office_doorways.obj")

    print()
    for c in conds:
        m = run_one(c)
        f = m["final"]
        print(f"{m['name']:28s} RSS={m['rss_rx_dbm']:7.1f} dBm  paths raw={m['raw']['n_paths']:4d} final={f['n_paths']:2d} "
              f"LoS={f['has_los']!s:5s} cab-bounce={f['box_bounce_paths']}  correct={m['correct']}  ({m['seconds']}s)")

    # Merge with previously generated metrics (when running a subset).
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
