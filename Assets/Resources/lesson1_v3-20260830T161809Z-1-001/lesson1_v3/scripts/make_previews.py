"""
Static preview figures (Sionna coordinates, top-down) for manual inspection:
one 2x2 panel per task/set showing, for each MCQ option, the floor heatmap
(0.1 m), the final path set that Unity will draw, the router, the receiver
(phone or TV), the option's cabinet and - dashed - the other options' cabinets.

    /home/saif/miniconda3/envs/sionna-rt/bin/python make_previews.py
"""
from __future__ import annotations

import csv
import glob
import json
import sys
from pathlib import Path

import numpy as np
import trimesh
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.collections import PolyCollection
from matplotlib.lines import Line2D

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from conditions import CONDITIONS, CORRECT, siblings  # noqa: E402
from wiviz_scene import BASE_SCENE_DIR, box_corners_xy  # noqa: E402
sys.path.insert(0, "/home/saif/wireless_NeRF_experiments/SIONNA")
from utils2 import _parse_interaction_coordinates  # noqa: E402
from wiviz_sim import n_transmissions  # noqa: E402

LESSON_DIR = HERE.parent
OUT = LESSON_DIR / "previews"
OUT.mkdir(exist_ok=True)

# Palette: sequential single hue for RSS magnitude, neutral ink for text/geometry,
# two fixed categorical hues for rays (blue) and the cabinet (purple).
CMAP = "Oranges"
VMIN, VMAX = -100.0, -40.0
INK, INK_MUTED, WALL = "#2b2b2b", "#6b6b6b", "#1f1f1f"
RAY, RAY_LOS, CABINET, ROUTER, PHONE = "#2f6fb3", "#0b3d91", "#7b3fa0", "#c0392b", "#1b7f4c"
THRU = "#d6338f"   # through-wall (transmission) ray

# ---- static geometry -----------------------------------------------------------
walls, furniture = [], []
for f in sorted(glob.glob(str(BASE_SCENE_DIR / "meshes" / "*.ply"))):
    m = trimesh.load(f, force="mesh")
    if "Floor_plan" in f:
        for fi, face in enumerate(m.faces):
            if abs(m.face_normals[fi][2]) < 0.5:
                p = m.vertices[face]
                walls.append(p[:, :2])
    else:
        furniture.append(m.vertices[m.faces][:, :, :2])


def draw_scene(ax):
    for tri in furniture:
        ax.add_collection(PolyCollection(tri, facecolor="#d9d9d9", edgecolor="none", alpha=0.9, zorder=2))
    for p in walls:
        ax.plot(p[[0, 1, 2, 0], 0], p[[0, 1, 2, 0], 1], color=WALL, lw=2.0, zorder=3)


def load_heat(name):
    arr = np.load(LESSON_DIR / "intermediate" / name / f"{name}_heatmap_sionna.npy")
    xs = np.unique(np.round(arr[:, 0], 3)); ys = np.unique(np.round(arr[:, 1], 3))
    grid = np.full((len(ys), len(xs)), np.nan)
    xi = np.searchsorted(xs, np.round(arr[:, 0], 3)); yi = np.searchsorted(ys, np.round(arr[:, 1], 3))
    grid[yi, xi] = np.maximum(arr[:, 3], -120.0)
    hx = 0.5 * float(np.min(np.diff(xs))); hy = 0.5 * float(np.min(np.diff(ys)))
    return grid, (xs.min() - hx, xs.max() + hx, ys.min() - hy, ys.max() + hy)


def load_paths(name):
    p = LESSON_DIR / "intermediate" / name / f"{name}_paths_final_sionna.csv"
    rows = []
    with open(p, newline="") as f:
        for r in csv.DictReader(f):
            rows.append((float(r["Path_Power_dBm"]), int(r["Total_Interactions_for_Path"]),
                         _parse_interaction_coordinates(r["Interaction_Coordinates"])))
    return rows


metrics = json.loads((LESSON_DIR / "metrics" / "summary.json").read_text())
by_name = {c["name"]: c for c in CONDITIONS}

for (task, s), corr in sorted(CORRECT.items()):
    fig, axes = plt.subplots(2, 2, figsize=(16, 13.5))
    fig.patch.set_facecolor("white")
    im = None
    for ax, opt in zip(axes.ravel(), "ABCD"):
        name = f"{task}_{s}_{opt}"
        c = by_name[name]; m = metrics[name]
        grid, ext = load_heat(name)
        im = ax.imshow(grid, origin="lower", extent=ext, cmap=CMAP, vmin=VMIN, vmax=VMAX, zorder=1, interpolation="nearest")
        draw_scene(ax)
        for sib in siblings(c):
            for b in sib["boxes"]:
                k = box_corners_xy(b["size"], b["center"], b["yaw_deg"]); k = np.vstack([k, k[:1]])
                ax.plot(k[:, 0], k[:, 1], color=CABINET, lw=1.2, ls="--", alpha=0.7, zorder=4)
                ax.text(b["center"][0], b["center"][1], sib["option"], color=CABINET, fontsize=8,
                        ha="center", va="center", zorder=4)
        for b in c["boxes"]:
            k = box_corners_xy(b["size"], b["center"], b["yaw_deg"]); k = np.vstack([k, k[:1]])
            ax.fill(k[:, 0], k[:, 1], facecolor=CABINET, edgecolor=CABINET, lw=2, alpha=0.85, zorder=5)
        for rel, n_int, pts in sorted(load_paths(name), key=lambda r: r[0]):
            los = n_int == 0
            thru = (not los) and n_transmissions(pts) > 0
            if thru:
                ax.plot(pts[:, 0], pts[:, 1], color=THRU, lw=2.0, ls=(0, (4, 2)), alpha=0.95, zorder=6.5)
            else:
                ax.plot(pts[:, 0], pts[:, 1], color=RAY_LOS if los else RAY, lw=2.6 if los else 1.2,
                        alpha=1.0 if los else 0.55 + 0.45 * max(0.0, 1.0 + rel / 35.0), zorder=6)
            if n_int:
                ax.scatter(pts[1:-1, 0], pts[1:-1, 1], s=10, color=RAY, zorder=7, linewidths=0)
        ax.scatter([c["tx"][0]], [c["tx"][1]], marker="*", s=260, color=ROUTER, edgecolor="white", linewidths=0.8, zorder=9)
        is_tv = c["rx_label"] == "TV"
        ax.scatter([c["rx"][0]], [c["rx"][1]], marker="s" if is_tv else "o", s=140 if is_tv else 120, color=PHONE,
                   edgecolor="white", linewidths=0.8, zorder=9)
        f = m["final"]
        tag = "CORRECT" if c["correct"] else "distractor"
        where = f"at TV ({c['rx'][2]:.1f} m)" if is_tv else "at phone (floor cells)"
        ax.set_title(f"Option {opt} — {tag}\nRSS {where} {m['rss_rx_dbm']:.1f} dBm · {f['n_paths']} rays · "
                     f"{'direct path' if f['has_los'] else 'no direct path'}"
                     + (f" · {f['box_bounce_paths']} cabinet bounce(s)" if c['boxes'] else ""),
                     fontsize=11, color=INK, loc="left")
        ax.set_xlim(-10.2, 8.2); ax.set_ylim(-8.2, 7.2); ax.set_aspect("equal")
        ax.set_xticks(range(-10, 9, 2)); ax.set_yticks(range(-8, 8, 2))
        ax.tick_params(colors=INK_MUTED, labelsize=8)
        for sp in ax.spines.values():
            sp.set_color("#cccccc")
        ax.set_xlabel("x (m, Sionna)", color=INK_MUTED, fontsize=8); ax.set_ylabel("y (m, Sionna)", color=INK_MUTED, fontsize=8)
    rx_kind = "TV" if task != "phone_optimization" else "phone"
    handles = [Line2D([], [], marker="*", color=ROUTER, ls="", ms=14, label="router (Tx)"),
               Line2D([], [], marker="s" if rx_kind == "TV" else "o", color=PHONE, ls="", ms=9, label=f"{rx_kind} (Rx)"),
               Line2D([], [], color=RAY_LOS, lw=2.6, label="direct (LoS) ray"),
               Line2D([], [], color=RAY, lw=1.2, label="reflection rays (opacity ~ relative power)"),
               Line2D([], [], color=THRU, lw=2.0, ls=(0, (4, 2)), label="through-wall ray (one per cross-room file)"),
               Line2D([], [], marker="s", color=CABINET, ls="", ms=10, label="metal cabinet (this option)"),
               Line2D([], [], color=CABINET, ls="--", lw=1.2, label="cabinets of the other options")]
    fig.legend(handles=handles, loc="lower center", ncol=6, frameon=False, fontsize=10, bbox_to_anchor=(0.42, 0.005))
    cb = fig.colorbar(im, ax=axes.ravel().tolist(), fraction=0.025, pad=0.02)
    cb.set_label("floor heatmap RSS at 0.1 m (dBm, clipped to −100…−40 for display)", color=INK_MUTED)
    cb.ax.tick_params(colors=INK_MUTED)
    fig.suptitle(f"{task}  ·  set {s}  ·  correct option: {corr}", fontsize=15, color=INK, x=0.42)
    fname = OUT / f"{task}_{s}_overview.png"
    fig.savefig(fname, dpi=80, bbox_inches="tight", facecolor="white")
    plt.close(fig)
    print("wrote", fname)
