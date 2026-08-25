"""
Sionna simulation + export wrapper for one WiViz **lesson 2** condition
(one router Tx, one or two phone receivers in the same CSV).

Everything that touches the CSV contract goes through the unchanged functions
in utils2.py (export_paths_to_csv, export_radio_map_to_csv, filter_csv,
transform_csv_for_unity).  export_paths_to_csv writes one Rx_Number per
receiver and Path_Power_dBm relative to the strongest path OF THAT RECEIVER,
so the -30 dB power filter and the per-Rx selection below stay per-phone.
"""
from __future__ import annotations

import csv
import json
import sys
from pathlib import Path

import numpy as np

SIONNA_DIR = Path("/home/saif/wireless_NeRF_experiments/SIONNA")
sys.path.insert(0, str(SIONNA_DIR))
import utils2 as utils  # noqa: E402

# -----------------------------------------------------------------------------
# Radio / solver configuration (kept close to the existing notebooks)
# -----------------------------------------------------------------------------
FREQUENCY_HZ = 5e9
BANDWIDTH_HZ = 1e6
TX_POWER_DBM = 10.0
ANTENNA = {"num_rows": 1, "num_cols": 1, "vertical_spacing": 0.5,
           "horizontal_spacing": 0.5, "pattern": "iso", "polarization": "V"}

HEATMAP_HEIGHT = 0.1     # radio-map plane height (m): on the floor, like the demo
                         # (demo_heatmap_v2).  Receivers keep their own height
                         # (phone 1.1 m, TV 2.0 m; see conditions.py).  Routers rest
                         # on furniture (0.9-1.1 m) -- no device may lie exactly in
                         # the heatmap plane (the RadioMapSolver would miss its rays).
WALL_THICKNESS_M = 0.2   # slab thickness used by Sionna's transmission model
                         # for the concrete walls/floor/ceiling (default 0.1)

PATH_SOLVER_CONFIG = dict(
    max_depth=4,
    los=True,
    specular_reflection=True,
    diffuse_reflection=False,   # random scatter paths only add visual noise
    refraction=True,            # through-wall rays, consistent with the heatmap
    diffraction=False,          # with diffraction on, the SBR sampling misses
    edge_diffraction=False,     # clean specular paths (e.g. the cabinet bounce)
    max_num_paths_per_src=1_000_000,  # a small cap silently drops strong paths
    samples_per_src=4_000_000,
    seed=41,
)

RADIO_MAP_CONFIG = dict(
    cell_size=(0.3, 0.3),        # 60 x 50 = 3000 cells for the 18 x 15 m scene (v2: was 0.1 m / 27000)
    tx_idx=0,
    max_depth=5,
    samples_per_tx=128 * 10**6,  # deep-shadow cells are noisy with fewer rays
)
RADIO_MAP_EXTRA = dict(refraction=True, diffraction=True)  # passed to RadioMapSolver

# Path-set reduction for readable rays / particles, applied PER RECEIVER:
# the LoS row is always kept; the rest is filled strongest-first from the
# paths within KEEP_WITHIN_DB_OF_STRONGEST of that receiver's strongest one,
# skipping paths whose geometry is close to an already selected one (Euclidean
# distance of the 20-point resampled polylines, as in lesson1_v2; 1.5 ~
# "visibly the same route").
#
# Lesson-2 twist: the number of rays kept for a receiver depends on its link
# quality, so a distant / wall-blocked phone visibly has fewer strong rays.
KEEP_WITHIN_DB_OF_STRONGEST = 30.0
MAX_PATH_ROWS = 6                          # per receiver
RAY_COUNT_STAIRCASE = [(-55.0, 6), (-65.0, 5), (-72.0, 4), (-80.0, 3)]
RAY_COUNT_MIN = 2
GEOMETRY_SIMILARITY = 1.5


def rays_for_rss(rss_dbm):
    """Ray budget for one receiver from its RSS (dBm): 6 for a strong link
    down to 2 for a deeply shadowed one."""
    for threshold, n in RAY_COUNT_STAIRCASE:
        if rss_dbm >= threshold:
            return n
    return RAY_COUNT_MIN

# RSS reported for the receiver = linear mean of the floor-heatmap cells within
# this radius of the condition's rss_point (the <= 4 cells around the point;
# avoids nearest-cell ties when the point sits on a cell boundary).
RSS_SPOT_RADIUS_M = 0.3

# Heatmap floor (dBm): cells the solver reports as exactly zero power become
# -270 dBm through the 1e-30 guard in utils2; clamp to a realistic noise floor.
HEATMAP_FLOOR_DBM = -120.0


class _ComplexPaths:
    """Proxy so utils2.export_paths_to_csv sees |a|^2 with BOTH real and
    imaginary parts (its tuple branch would otherwise use only paths.a[0])."""

    def __init__(self, paths):
        self._p = paths
        a_re, a_im = paths.a
        self.a = utils.to_numpy(a_re) + 1j * utils.to_numpy(a_im)

    def __getattr__(self, name):
        return getattr(self._p, name)


def build_scene(xml_path, tx_position, rx_positions):
    scene = utils.load_sionna_scene(xml_path)
    utils.configure_scene_radio(scene, FREQUENCY_HZ, BANDWIDTH_HZ)
    if WALL_THICKNESS_M is not None and "itu_concrete" in scene.radio_materials:
        scene.radio_materials["itu_concrete"].thickness = float(WALL_THICKNESS_M)
    utils.add_transmitter(scene, tx_position, antenna_config=ANTENNA, power_dbm=TX_POWER_DBM)
    utils.add_receivers(scene, rx_positions, antenna_config=ANTENNA)
    return scene


def compute_radio_map_grid_ext(scene, height, **kwargs):
    """compute_radio_map_grid with extra RadioMapSolver flags (diffraction,
    refraction).  Mirrors utils2.compute_radio_map_grid exactly otherwise."""
    from sionna.rt import RadioMapSolver
    dx, dy = RADIO_MAP_CONFIG["cell_size"]
    bbox = scene._scene.bbox()
    bmin = np.array([float(bbox.min[i]) for i in range(3)])
    bmax = np.array([float(bbox.max[i]) for i in range(3)])
    center = [float(0.5 * (bmin[0] + bmax[0])), float(0.5 * (bmin[1] + bmax[1])), float(height)]
    size = [float(bmax[0] - bmin[0]), float(bmax[1] - bmin[1])]
    dx, dy = float(dx), float(dy)
    rm = RadioMapSolver()(scene=scene, center=center, orientation=[0.0, 0.0, 0.0],
                          size=size, cell_size=[dx, dy],
                          max_depth=int(kwargs.get("max_depth", RADIO_MAP_CONFIG["max_depth"])),
                          samples_per_tx=int(kwargs.get("samples_per_tx", RADIO_MAP_CONFIG["samples_per_tx"])),
                          **RADIO_MAP_EXTRA)
    cells = np.asarray(utils.to_numpy(rm.cell_centers), float).reshape(-1, 3)
    rss_lin = np.asarray(utils.to_numpy(rm.transmitter_radio_map("rss", 0)), float).reshape(-1)
    rss_dbm = 10.0 * np.log10(np.maximum(rss_lin, 1e-30)) + 30.0
    return cells, rss_dbm, rm


def rss_lookup(cells, rss_dbm, points, radius=None):
    """Linear-mean RSS (dBm) of all cells within `radius` of each point
    (nearest cell if none falls inside the radius)."""
    radius = RSS_SPOT_RADIUS_M if radius is None else float(radius)
    pts = np.asarray(points, float).reshape(-1, 3)
    out = []
    for p in pts:
        d = np.sqrt(np.sum((cells[:, :2] - p[None, :2]) ** 2, axis=1))
        sel = rss_dbm[d <= radius + 1e-9]
        if sel.size == 0:
            sel = rss_dbm[[int(np.argmin(d))]]
        out.append(float(10.0 * np.log10(np.mean(10.0 ** (sel / 10.0)))))
    return np.asarray(out)


# -----------------------------------------------------------------------------
# Path analysis helpers (work on the CSV rows, schema untouched)
# -----------------------------------------------------------------------------
def read_rows(csv_path):
    with open(csv_path, newline="") as f:
        r = csv.DictReader(f)
        return r.fieldnames, list(r)


def write_rows(csv_path, fieldnames, rows):
    utils._write_rows_preserving_schema(csv_path, fieldnames, rows)


def row_points(row):
    return utils._parse_interaction_coordinates(row["Interaction_Coordinates"])


def row_bounces(row):
    """Interaction vertices (excluding Tx/Rx) of a CSV row as a list."""
    pts = row_points(row)
    return [] if pts is None else list(pts[1:-1])


def point_in_box_xy(pt, corners, tol=0.05):
    """Point-in-convex-polygon test (xy, CCW corners) with a distance tolerance
    so that points lying exactly on a face count as inside."""
    c = np.asarray(corners, float)
    p = np.asarray(pt[:2], float)
    n = len(c)
    for i in range(n):
        a, b = c[i], c[(i + 1) % n]
        ab = b - a
        L = max(np.linalg.norm(ab), 1e-12)
        cross = (ab[0] * (p[1] - a[1]) - ab[1] * (p[0] - a[0])) / L
        if cross < -tol:
            return False
    return True


def analyse_rows(rows, box_corners=None):
    """Summary of a path CSV: LoS presence, interaction histogram, strongest
    path type, and whether any path bounces on the given box footprint."""
    info = {"n_paths": len(rows), "has_los": False, "hist": {}, "box_bounce_paths": 0,
            "box_bounce_best_rel_db": None, "strongest_type": None}
    best = None
    for row in rows:
        k = int(row["Total_Interactions_for_Path"])
        info["hist"][k] = info["hist"].get(k, 0) + 1
        rel = float(row["Path_Power_dBm"])
        if k == 0:
            info["has_los"] = True
        if best is None or rel > best[0]:
            best = (rel, k)
        if box_corners is not None and k >= 1:
            pts = row_points(row)
            if any(point_in_box_xy(p, box_corners) for p in row_bounces(row)):
                info["box_bounce_paths"] += 1
                if info["box_bounce_best_rel_db"] is None or rel > info["box_bounce_best_rel_db"]:
                    info["box_bounce_best_rel_db"] = rel
    if best is not None:
        info["strongest_type"] = "LoS" if best[1] == 0 else f"{best[1]}-bounce"
    return info


def analyse_rows_by_rx(rows):
    """analyse_rows per Rx group, keyed by the Rx_Number string."""
    groups = {}
    for r in rows:
        groups.setdefault(r["Rx_Number"], []).append(r)
    return {k: analyse_rows(v) for k, v in sorted(groups.items(), key=lambda kv: int(kv[0]))}


def select_final_rows(powerfiltered_csv, raw_csv, out_csv, max_rows_by_rx=None,
                      similarity_threshold=GEOMETRY_SIMILARITY, n_samples=20):
    """Per receiver, pick at most its ray budget (max_rows_by_rx[rx_number],
    default MAX_PATH_ROWS): the LoS row first (taken from the raw export, even
    when weak), then the strongest remaining power-filtered paths whose
    geometry differs from everything already selected for that receiver.
    Rows are copied verbatim; output ordered Rx 1 first, strongest first
    within each group; the schema is untouched."""
    fn, src = read_rows(raw_csv)
    _, pf = read_rows(powerfiltered_csv)

    def power(r):
        return float(r["Path_Power_dBm"])

    def feat(r):
        return utils._resample_polyline(row_points(r), n_samples=n_samples).reshape(-1)

    def group(rows):
        out = {}
        for r in rows:
            out.setdefault(int(r["Rx_Number"]), []).append(r)
        return out

    src_by_rx, pf_by_rx = group(src), group(pf)
    final = []
    for rx_num in sorted(src_by_rx):
        cap = (max_rows_by_rx or {}).get(rx_num, MAX_PATH_ROWS)
        selected, keys, feats = [], set(), []

        def take(r):
            selected.append(r); keys.add(r["Interaction_Coordinates"]); feats.append(feat(r))

        for r in src_by_rx[rx_num]:
            if int(r["Total_Interactions_for_Path"]) == 0 and r["Interaction_Coordinates"] not in keys:
                take(r)
        for r in sorted(pf_by_rx.get(rx_num, []), key=lambda r: -power(r)):
            if len(selected) >= cap:
                break
            if r["Interaction_Coordinates"] in keys:
                continue
            f = feat(r)
            if feats and float(np.min(np.linalg.norm(np.asarray(feats) - f[None, :], axis=1))) <= similarity_threshold:
                continue
            take(r)
        selected.sort(key=power, reverse=True)
        final.extend(selected)
    write_rows(out_csv, fn, final)
    return len(final)


def clamp_heatmap_floor(csv_in, csv_out, floor_dbm=HEATMAP_FLOOR_DBM):
    """Clamp RSS_dBm to a noise floor.  Same schema; only the RSS value of
    cells below the floor changes (they were -270 dBm placeholders)."""
    fn, rows = read_rows(csv_in)
    n = 0
    for r in rows:
        if float(r["RSS_dBm"]) < floor_dbm:
            r["RSS_dBm"] = f"{floor_dbm:.4f}"
            n += 1
    write_rows(csv_out, fn, rows)
    return n


# -----------------------------------------------------------------------------
# One full condition
# -----------------------------------------------------------------------------
def run_condition(name, xml_path, tx, rxs, work_dir, final_dir, heatmap=None, verbose=True):
    """
    Simulate one lesson-2 condition (one router Tx, 1..N phone receivers) and
    write:
        final_dir/<name>.csv              Unity paths (all Rx groups in one file)
        final_dir/<name>_heatmap.csv      Unity floor heatmap
    plus Sionna-coordinate intermediates in work_dir.

    RSS_dBm of receiver i = floor-heatmap spot under that phone (lesson1_v2
    convention); by reciprocity this is also that phone's power at the router.
    The ray budget per receiver follows rays_for_rss (weaker link -> fewer rays).

    heatmap: optional (cells, rss_dbm) of a previously computed floor map.  The
    scene and Tx never change in lesson 2, so the generator computes the map
    once and shares it -- all heatmap CSVs are byte-identical and the RSS of a
    fixed phone is exactly constant (per-run GPU reductions otherwise differ in
    the 4th decimal).
    Returns (metrics dict, heatmap cells, heatmap dBm).
    """
    work_dir = Path(work_dir); work_dir.mkdir(parents=True, exist_ok=True)
    final_dir = Path(final_dir); final_dir.mkdir(parents=True, exist_ok=True)
    tx = [float(v) for v in tx]
    rxs = [[float(v) for v in rx] for rx in rxs]

    scene = build_scene(xml_path, tx, rxs)
    paths = utils.compute_paths(scene, **PATH_SOLVER_CONFIG)

    if heatmap is None:
        cells, rss_dbm, _ = compute_radio_map_grid_ext(scene, HEATMAP_HEIGHT)
    else:
        cells, rss_dbm = heatmap
    rss_rx = rss_lookup(cells, rss_dbm, [rx[:2] + [HEATMAP_HEIGHT] for rx in rxs])
    ray_caps = {i + 1: rays_for_rss(float(v)) for i, v in enumerate(rss_rx)}

    # ---- paths --------------------------------------------------------------
    raw_csv = work_dir / f"{name}_paths_raw_sionna.csv"
    utils.export_paths_to_csv(scene, _ComplexPaths(paths), str(raw_csv), tx_idx=0,
                              rx_ant_idx=0, rss_dbm=[float(v) for v in rss_rx])
    fn, raw_rows = read_rows(raw_csv)
    pf_csv = work_dir / f"{name}_paths_powerfiltered_sionna.csv"
    utils.filter_csv(str(raw_csv), str(pf_csv), min_path_power_dbm=-KEEP_WITHIN_DB_OF_STRONGEST)
    fin_csv = work_dir / f"{name}_paths_final_sionna.csv"
    select_final_rows(pf_csv, raw_csv, fin_csv, max_rows_by_rx=ray_caps)
    utils.transform_csv_for_unity(str(fin_csv), str(final_dir / f"{name}.csv"))

    # ---- heatmap ------------------------------------------------------------
    hm_csv = work_dir / f"{name}_heatmap_sionna.csv"
    utils.export_radio_map_to_csv(scene, cells, rss_dbm, str(hm_csv), tx_idx=0, drop_nonfinite=True)
    hm_clamped = work_dir / f"{name}_heatmap_clamped_sionna.csv"
    n_clamped = clamp_heatmap_floor(hm_csv, hm_clamped)
    utils.transform_csv_for_unity(str(hm_clamped), str(final_dir / f"{name}_heatmap.csv"))

    _, fin_rows = read_rows(fin_csv)
    finite = rss_dbm[np.isfinite(rss_dbm)]
    metrics = {
        "name": name, "xml": str(xml_path), "tx": tx, "rxs": rxs,
        "rss_rx_dbm": [float(v) for v in rss_rx],
        "ray_caps": {str(k): int(v) for k, v in ray_caps.items()},
        "raw": analyse_rows_by_rx(raw_rows),
        "final": analyse_rows_by_rx(fin_rows),
        "heatmap": {"cells": int(len(rss_dbm)), "max_dbm": float(finite.max()),
                    "p50_dbm": float(np.median(finite)), "clamped_cells": int(n_clamped)},
    }
    if verbose:
        print(json.dumps(metrics, indent=1))
    return metrics, cells, rss_dbm
