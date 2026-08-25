"""
Static preview figure (Sionna coordinates, top-down) for manual inspection:
one 2x3 panel figure showing, for each of the six lesson-2 conditions, the
floor heatmap, the final path sets Unity will draw (Phone A rays vs Phone B
rays in different colours), the router and the phones.

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
from conditions import CONDITIONS  # noqa: E402
from wiviz_scene import BASE_SCENE_DIR  # noqa: E402
sys.path.insert(0, "/home/saif/wireless_NeRF_experiments/SIONNA")
from utils2 import _parse_interaction_coordinates  # noqa: E402

LESSON_DIR = HERE.parent
OUT = LESSON_DIR / "previews"
OUT.mkdir(exist_ok=True)

# Palette: sequential single hue for RSS magnitude, neutral ink for geometry,
# one categorical hue per phone's ray set.
CMAP = "Oranges"
VMIN, VMAX = -100.0, -40.0
INK, INK_MUTED, WALL = "#2b2b2b", "#6b6b6b", "#1f1f1f"
ROUTER = "#c0392b"
PHONE_COLORS = {"PhoneA": "#1b7f4c", "PhoneB": "#7b3fa0"}
RAY_COLORS = {"1": "#2f6fb3", "2": "#7b3fa0"}
RAY_LOS = {"1": "#0b3d91", "2": "#4a1f6e"}

# ---- static geometry --------------------------------------------------------
walls, furniture = [], []
for f in sorted(glob.glob(str(BASE_SCENE_DIR / "meshes" / "*.ply"))):
    m = trimesh.load(f, force="mesh")
    if "Floor_plan" in f:
        for fi, face in enumerate(m.faces):
            if abs(m.face_normals[fi][2]) < 0.5:
                walls.append(m.vertices[face][:, :2])
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
    rows = []
    with open(LESSON_DIR / "intermediate" / name / f"{name}_paths_final_sionna.csv", newline="") as f:
        for r in csv.DictReader(f):
            rows.append((r["Rx_Number"], float(r["Path_Power_dBm"]), int(r["Total_Interactions_for_Path"]),
                         _parse_interaction_coordinates(r["Interaction_Coordinates"])))
    return rows


metrics = json.loads((LESSON_DIR / "metrics" / "summary.json").read_text())

fig, axes = plt.subplots(2, 3, figsize=(24, 13.5))
fig.patch.set_facecolor("white")
im = None
for ax, c in zip(axes.ravel(), CONDITIONS):
    name = c["name"]; m = metrics[name]
    grid, ext = load_heat(name)
    im = ax.imshow(grid, origin="lower", extent=ext, cmap=CMAP, vmin=VMIN, vmax=VMAX, zorder=1,
                   interpolation="nearest")
    draw_scene(ax)
    for rx_num, rel, n_int, pts in sorted(load_paths(name), key=lambda r: r[1]):
        los = n_int == 0
        ax.plot(pts[:, 0], pts[:, 1], color=RAY_LOS[rx_num] if los else RAY_COLORS[rx_num],
                lw=2.6 if los else 1.2, alpha=1.0 if los else 0.55 + 0.45 * max(0.0, 1.0 + rel / 35.0), zorder=6)
        if n_int:
            ax.scatter(pts[1:-1, 0], pts[1:-1, 1], s=10, color=RAY_COLORS[rx_num], zorder=7, linewidths=0)
    ax.scatter([c["tx"][0]], [c["tx"][1]], marker="*", s=280, color=ROUTER, edgecolor="white",
               linewidths=0.8, zorder=9)
    for label, rx in zip(c["rx_labels"], c["rxs"]):
        ax.scatter([rx[0]], [rx[1]], marker="o", s=130, color=PHONE_COLORS[label], edgecolor="white",
                   linewidths=0.8, zorder=9)
        ax.annotate(label[-1], (rx[0], rx[1]), textcoords="offset points", xytext=(8, 6),
                    color=PHONE_COLORS[label], fontsize=11, fontweight="bold", zorder=9)
    rss = " · ".join(f"{lab} {v:.1f} dBm" for lab, v in zip(c["rx_labels"], m["rss_rx_dbm"]))
    rays = " + ".join(f"{g['n_paths']}" for g in m["final"].values())
    sir = f" · SIR at router {m['sir_at_router_db']:+.1f} dB" if m.get("sir_at_router_db") is not None else ""
    tag = " — CORRECT" if c["correct"] else (f" — option {c['option']}" if c["option"] else "")
    ax.set_title(f"{name}{tag}\n{rss}{sir} · {rays} rays", fontsize=11, color=INK, loc="left")
    ax.set_xlim(-10.2, 8.2); ax.set_ylim(-8.2, 7.2); ax.set_aspect("equal")
    ax.set_xticks(range(-10, 9, 2)); ax.set_yticks(range(-8, 8, 2))
    ax.tick_params(colors=INK_MUTED, labelsize=8)
    for sp in ax.spines.values():
        sp.set_color("#cccccc")
    ax.set_xlabel("x (m, Sionna)", color=INK_MUTED, fontsize=8)
    ax.set_ylabel("y (m, Sionna)", color=INK_MUTED, fontsize=8)

handles = [Line2D([], [], marker="*", color=ROUTER, ls="", ms=14, label="router (Tx, fixed)"),
           Line2D([], [], marker="o", color=PHONE_COLORS["PhoneA"], ls="", ms=9, label="Phone A (fixed)"),
           Line2D([], [], marker="o", color=PHONE_COLORS["PhoneB"], ls="", ms=9, label="Phone B (moves)"),
           Line2D([], [], color=RAY_COLORS["1"], lw=1.6, label="Phone A rays (thick dark = LoS)"),
           Line2D([], [], color=RAY_COLORS["2"], lw=1.6, label="Phone B rays (count shrinks with link quality)")]
fig.legend(handles=handles, loc="lower center", ncol=5, frameon=False, fontsize=11,
           bbox_to_anchor=(0.42, 0.005))
cb = fig.colorbar(im, ax=axes.ravel().tolist(), fraction=0.02, pad=0.02)
cb.set_label("floor heatmap RSS at 0.1 m (dBm, clipped to −100…−40 for display; identical in all panels)",
             color=INK_MUTED)
cb.ax.tick_params(colors=INK_MUTED)
fig.suptitle("lesson 2 · communication & interference · correct spatial option: D (kitchen)",
             fontsize=15, color=INK, x=0.42)
fname = OUT / "lesson2_overview.png"
fig.savefig(fname, dpi=80, bbox_inches="tight", facecolor="white")
plt.close(fig)
print("wrote", fname)
