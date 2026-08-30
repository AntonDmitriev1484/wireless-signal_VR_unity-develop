"""
Scene-variant builder for the WiViz lesson-1 (v2) dataset.

* The base scene (XML + PLY meshes) is treated as immutable. Every condition
  gets its own copy under  scenes/<condition>/  with optional extra objects
  (simple boxes such as a metal cabinet) appended to the XML.
* Unity-compatible OBJ/MTL exports follow the convention of the old study's
  Blender export (forward=-Z, up=Y):  (x, y, z)_sionna -> (x, z, -y)_obj.
  Unity negates X on OBJ import, which yields the CSV convention (-x, z, -y).
"""
from __future__ import annotations

import os
import re
import shutil
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

import numpy as np
import trimesh

SIONNA_DIR = Path("/home/saif/wireless_NeRF_experiments/SIONNA")
sys.path.insert(0, str(SIONNA_DIR))
import utils2 as utils  # noqa: E402  (reuse the existing transform parser)

BASE_SCENE_DIR = SIONNA_DIR / "xml_3d_models" / "Home Office With Doors xml"
BASE_XML = BASE_SCENE_DIR / "home_office_doorways.xml"

# Default "metal cabinet" used by los_creation / reflection_creation.
CABINET_SIZE = (1.2, 0.4, 2.2)   # width (local x), depth (local y), height -- slim (v2)
CABINET_MATERIAL = "itu_metal"

# Blender-style MTL colours (same values as the old study's .mtl file).
MTL_KD = {
    "itu_glass": (0.8, 0.8, 0.8),
    "itu_metal": (0.046665, 0.046665, 0.046665),
    "itu_wood": (0.8, 0.8, 0.8),
    "itu_concrete": (0.779848, 0.800266, 0.759736),
    "itu_brick": (0.8, 0.8, 0.8),
    "itu_plywood": (0.8, 0.8, 0.8),
    "itu_marble": (0.8, 0.8, 0.8),
    "itu_wet_ground": (0.8, 0.8, 0.8),
    "itu_chipboard": (0.8, 0.8, 0.8),
}


# -----------------------------------------------------------------------------
# Box objects
# -----------------------------------------------------------------------------
def box_mesh(size, center_xy, yaw_deg=0.0, z0=0.0):
    """Axis-aligned box of `size` rotated by yaw (deg, about +z) and placed so
    that its footprint centre is `center_xy` and its bottom is at z0."""
    sx, sy, sz = [float(v) for v in size]
    mesh = trimesh.creation.box(extents=(sx, sy, sz))
    rot = trimesh.transformations.rotation_matrix(np.deg2rad(yaw_deg), [0, 0, 1])
    mesh.apply_transform(rot)
    mesh.apply_translation([float(center_xy[0]), float(center_xy[1]), z0 + sz / 2.0])
    return mesh


def box_corners_xy(size, center_xy, yaw_deg=0.0):
    """Footprint polygon (4x2) of the rotated box."""
    sx, sy = float(size[0]), float(size[1])
    c, s = np.cos(np.deg2rad(yaw_deg)), np.sin(np.deg2rad(yaw_deg))
    R = np.array([[c, -s], [s, c]])
    local = np.array([[-sx / 2, -sy / 2], [sx / 2, -sy / 2], [sx / 2, sy / 2], [-sx / 2, sy / 2]])
    return local @ R.T + np.asarray(center_xy, dtype=float)


def segment_hits_box(p, q, size, center_xy, yaw_deg=0.0, z0=0.0):
    """True if the 3D segment p->q intersects the (yaw-rotated) box.
    Slab test in the box's local frame; no external ray library needed."""
    p = np.asarray(p, float); q = np.asarray(q, float)
    sx, sy, sz = [float(v) for v in size]
    c = np.array([center_xy[0], center_xy[1], z0 + sz / 2.0], float)
    th = np.deg2rad(yaw_deg)
    R = np.array([[np.cos(th), np.sin(th), 0.0], [-np.sin(th), np.cos(th), 0.0], [0.0, 0.0, 1.0]])
    pl = R @ (p - c)
    ql = R @ (q - c)
    d = ql - pl
    half = np.array([sx, sy, sz]) / 2.0
    t0, t1 = 0.0, 1.0
    for i in range(3):
        if abs(d[i]) < 1e-12:
            if abs(pl[i]) > half[i]:
                return False
            continue
        ta = (-half[i] - pl[i]) / d[i]
        tb = (half[i] - pl[i]) / d[i]
        if ta > tb:
            ta, tb = tb, ta
        t0 = max(t0, ta)
        t1 = min(t1, tb)
        if t0 > t1:
            return False
    return True


# -----------------------------------------------------------------------------
# Scene variant creation
# -----------------------------------------------------------------------------
def _shape_xml(name, ply_rel, material):
    return (
        f'\t<shape type="ply" id="mesh-{name}" name="mesh-{name}">\n'
        f'\t\t<string name="filename" value="{ply_rel}"/>\n'
        f'\t\t<boolean name="face_normals" value="true"/>\n'
        f'\t\t<ref id="mat-{material}" name="bsdf"/>\n'
        f'\t</shape>\n'
    )


def create_scene_variant(out_dir, extra_boxes=(), base_xml=BASE_XML, xml_name=None):
    """
    Copy the base scene into `out_dir` and append box objects.

    extra_boxes: iterable of dicts with keys
        name, size=(sx,sy,sz), center=(x,y), yaw_deg=0, z0=0, material
    Returns the path of the new XML.
    """
    out_dir = Path(out_dir)
    base_xml = Path(base_xml)
    base_dir = base_xml.parent
    if out_dir.resolve() == base_dir.resolve():
        raise ValueError("Refusing to write into the base scene directory.")
    out_dir.mkdir(parents=True, exist_ok=True)

    # Copy meshes (fresh copy so every condition is self-contained).
    dst_meshes = out_dir / "meshes"
    if dst_meshes.exists():
        shutil.rmtree(dst_meshes)
    shutil.copytree(base_dir / "meshes", dst_meshes)

    text = base_xml.read_text()
    inserts = []
    for b in extra_boxes:
        name = b["name"]
        mesh = box_mesh(b["size"], b["center"], b.get("yaw_deg", 0.0), b.get("z0", 0.0))
        ply_rel = f"meshes/{name}.ply"
        mesh.export(dst_meshes / f"{name}.ply")
        inserts.append(_shape_xml(name, ply_rel, b.get("material", CABINET_MATERIAL)))

    if inserts:
        marker = "<!-- Volumes -->"
        if marker not in text:
            raise ValueError("Base XML lacks the '<!-- Volumes -->' marker.")
        text = text.replace(marker, "".join(inserts) + "\n" + marker)

    xml_path = out_dir / (xml_name or base_xml.name)
    xml_path.write_text(text)
    return xml_path


# -----------------------------------------------------------------------------
# Unity OBJ export
# -----------------------------------------------------------------------------
def _sionna_to_obj(v):
    v = np.asarray(v, float)
    return np.stack([v[:, 0], v[:, 2], -v[:, 1]], axis=1)


def _parse_shapes(xml_path):
    root = ET.parse(xml_path).getroot()
    shapes = []
    for shape in root.iter("shape"):
        if shape.get("type") != "ply":
            continue
        ply_rel = None
        for s in shape.findall("string"):
            if s.get("name") == "filename":
                ply_rel = s.get("value")
        ref = shape.find("ref")
        mat = ref.get("id").replace("mat-", "") if ref is not None else "default"
        shapes.append((shape.get("id") or os.path.basename(ply_rel), ply_rel, mat, shape))
    return shapes


def export_scene_obj(xml_path, obj_path, include=None, exclude=None):
    """
    Write a Unity-ready OBJ + MTL for the scene described by `xml_path`.

    include / exclude: optional sets of shape ids (e.g. {"mesh-Metal_Cabinet"})
    to restrict the export (used for the per-option object-only OBJ files).
    """
    xml_path = Path(xml_path)
    obj_path = Path(obj_path)
    obj_path.parent.mkdir(parents=True, exist_ok=True)
    mtl_path = obj_path.with_suffix(".mtl")
    scene_dir = xml_path.parent

    used_mats = []
    v_offset = 0
    n_groups = 0
    with open(obj_path, "w") as f:
        f.write("# WiViz lesson-1 v2 export (Sionna -> Unity: x, z, -y)\n")
        f.write(f"mtllib {mtl_path.name}\n")
        for sid, ply_rel, mat, shape in _parse_shapes(xml_path):
            if include is not None and sid not in include:
                continue
            if exclude is not None and sid in exclude:
                continue
            mesh = trimesh.load(scene_dir / ply_rel, force="mesh")
            if mesh is None or len(mesh.faces) == 0:
                continue
            scale_vec, rotation, translation = utils._parse_xml_transform(shape)
            verts = utils._apply_transform(mesh.vertices, scale_vec, rotation, translation)
            verts = _sionna_to_obj(verts)
            gname = re.sub(r"^mesh-", "", sid)
            f.write(f"g {gname}\n")
            f.write(f"usemtl {mat}\n")
            if mat not in used_mats:
                used_mats.append(mat)
            for v in verts:
                f.write(f"v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}\n")
            for face in mesh.faces:
                a, b, c = (int(i) + 1 + v_offset for i in face)
                f.write(f"f {a} {b} {c}\n")
            v_offset += len(verts)
            n_groups += 1

    with open(mtl_path, "w") as f:
        f.write("# WiViz lesson-1 v2 MTL\n\n")
        for mat in used_mats:
            kd = MTL_KD.get(mat, (0.8, 0.8, 0.8))
            f.write(f"newmtl {mat}\nNs 360.000000\nKa 1.000000 1.000000 1.000000\n")
            f.write(f"Kd {kd[0]:.6f} {kd[1]:.6f} {kd[2]:.6f}\n")
            f.write("Ks 0.500000 0.500000 0.500000\nKe 0.000000 0.000000 0.000000\nNi 1.000000\nd 1.000000\nillum 2\n\n")
    return obj_path, n_groups


# -----------------------------------------------------------------------------
# Placement checks (v2): cabinets must not overlap each other, furniture or
# walls.  Pure numpy (trimesh.proximity needs rtree, which is not installed).
# -----------------------------------------------------------------------------
def box_corners(box):
    """Footprint polygon (4x2) of a box dict (size, center, yaw_deg)."""
    return box_corners_xy(box["size"], box["center"], box.get("yaw_deg", 0.0))


def polygons_overlap(a, b, margin=0.0):
    """Separating-axis test for two convex xy polygons.  With margin > 0 the
    polygons also count as overlapping when their gap is smaller than margin."""
    a = np.asarray(a, float); b = np.asarray(b, float)
    for poly in (a, b):
        n = len(poly)
        for i in range(n):
            edge = poly[(i + 1) % n] - poly[i]
            axis = np.array([-edge[1], edge[0]])
            L = np.linalg.norm(axis)
            if L < 1e-12:
                continue
            axis /= L
            pa, pb = a @ axis, b @ axis
            if pa.max() + margin <= pb.min() or pb.max() + margin <= pa.min():
                return False
    return True


def point_in_polygon(pt, poly, tol=0.0):
    """Point-in-convex-polygon (xy); tol > 0 shrinks the polygon."""
    poly = np.asarray(poly, float); p = np.asarray(pt[:2], float)
    signs = []
    n = len(poly)
    for i in range(n):
        a, b = poly[i], poly[(i + 1) % n]
        ab = b - a
        L = max(np.linalg.norm(ab), 1e-12)
        signs.append((ab[0] * (p[1] - a[1]) - ab[1] * (p[0] - a[0])) / L)
    signs = np.asarray(signs)
    return bool(np.all(signs >= tol) or np.all(signs <= -tol))


def segments_intersect(p1, p2, q1, q2):
    def orient(a, b, c):
        return (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])
    d1, d2 = orient(q1, q2, p1), orient(q1, q2, p2)
    d3, d4 = orient(p1, p2, q1), orient(p1, p2, q2)
    return (d1 * d2 < 0) and (d3 * d4 < 0)


def _transformed_shapes(base_xml=BASE_XML):
    base_xml = Path(base_xml)
    out = []
    for sid, ply_rel, mat, shape in _parse_shapes(base_xml):
        mesh = trimesh.load(base_xml.parent / ply_rel, force="mesh", process=False)
        scale_vec, rotation, translation = utils._parse_xml_transform(shape)
        verts = utils._apply_transform(mesh.vertices, scale_vec, rotation, translation)
        out.append((re.sub(r"^mesh-", "", sid), verts, mesh.faces))
    return out


_FOOT_CACHE = {}


def furniture_footprints(base_xml=BASE_XML, z_max=1.5):
    """xy bounding rectangles (4x2) of every non-floor-plan shape that has
    geometry below z_max (ceiling lamps are skipped that way)."""
    key = (str(base_xml), float(z_max))
    if key not in _FOOT_CACHE:
        foot = {}
        for name, verts, _ in _transformed_shapes(base_xml):
            if "Floor_plan" in name:
                continue
            v = verts[verts[:, 2] < z_max]
            if len(v) == 0:
                continue
            (x0, y0), (x1, y1) = v[:, :2].min(0), v[:, :2].max(0)
            foot[name] = np.array([[x0, y0], [x1, y0], [x1, y1], [x0, y1]])
        _FOOT_CACHE[key] = foot
    return _FOOT_CACHE[key]


_WALL_CACHE = {}


def wall_segments(base_xml=BASE_XML):
    """xy segments of the vertical faces of the floor plan (walls, doors)."""
    key = str(base_xml)
    if key not in _WALL_CACHE:
        segs = set()
        for name, verts, faces in _transformed_shapes(base_xml):
            if "Floor_plan" not in name:
                continue
            tri = verts[faces]
            n = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])
            n /= np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-12)
            for t in tri[np.abs(n[:, 2]) < 0.5]:
                xy = t[:, :2]
                d = np.linalg.norm(xy[:, None] - xy[None], axis=2)
                i, j = np.unravel_index(np.argmax(d), d.shape)
                if d[i, j] < 1e-6:
                    continue
                a, b = tuple(np.round(xy[i], 3)), tuple(np.round(xy[j], 3))
                segs.add((a, b) if a <= b else (b, a))
        _WALL_CACHE[key] = [np.array(s) for s in sorted(segs)]
    return _WALL_CACHE[key]


def placement_problems(box, others=(), base_xml=BASE_XML, keep_out_points=(),
                       margin=0.05, names=None):
    """
    List of human-readable problems for one cabinet:
    overlaps furniture, crosses a wall, overlaps another option's cabinet, or
    contains one of keep_out_points (router / receiver).  Empty list = OK.
    """
    corners = box_corners(box)
    problems = []
    for fname, rect in furniture_footprints(base_xml).items():
        if polygons_overlap(corners, rect, margin):
            problems.append(f"overlaps furniture {fname}")
    for seg in wall_segments(base_xml):
        for i in range(4):
            if segments_intersect(corners[i], corners[(i + 1) % 4], seg[0], seg[1]):
                problems.append(f"crosses wall segment {seg[0].tolist()}-{seg[1].tolist()}")
                break
        else:
            continue
        break
    for k, other in enumerate(others):
        if polygons_overlap(corners, box_corners(other), margin):
            label = names[k] if names is not None else f"#{k}"
            problems.append(f"overlaps cabinet of {label}")
    for p in keep_out_points:
        if point_in_polygon(p, corners, tol=-margin):
            problems.append(f"contains point {tuple(np.round(p, 3))}")
    return problems
