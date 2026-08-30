"""
Declarative definition of the 24 WiViz lesson-1 **v2** conditions
(3 tasks x 2 counterbalanced sets x 4 MCQ options).

All coordinates are Sionna coordinates (x, y, z in metres, z up).

v2 vs lesson1
* heatmap on the floor (0.1 m) -> routers rest on furniture (desk / table / shelf)
* receiver of los_creation and reflection_creation is the TV of the demo data
* slim cabinets (1.2 x 0.4 x 2.2 m); the four cabinets of a set never overlap
  each other, furniture or walls (wiviz_scene.placement_problems)
* each condition carries `rx_label`, `rss_mode` and `rss_point`:
  phones: RSS_dBm = floor heatmap around the phone's xy ("floor");
  TV:     RSS_dBm = a radio map at the TV's height (2.0 m) around the panel
          ("rx_height") -- the floor under the TV lies inside the TV stand and
          the floor in front of it does not react to what blocks/mirrors the
          line to the TV (verified: margins of 0.1 dB with floor cells)
"""
import numpy as np

from wiviz_scene import CABINET_MATERIAL

CAB_SIZE = (1.2, 0.4, 2.2)            # metal cabinet: width x depth x height (m) - reflection task (v2 data)
LOS_CAB_SIZE = (1.6, 0.4, 2.2)        # v3 los_creation: a slim 1.6 m sideboard - wide enough to also block the
                                      # wall bounce next to the direct ray (a 1.2 m one left only ~4 dB contrast)

# Receivers
Z_PHONE = 1.1
TV = (5.0, -7.6, 2.0)                 # demo Rx: 0.2 m in front of the TV panel's top-right corner
TV_RSS_POINT = (5.0, -7.45)           # the two 0.3 m cells right in front of the panel (panel y = -7.63) at 2.0 m
TV_FLOOR_POINT = (5.0, -6.6)          # open floor directly in front of the TV stand (front y = -6.87), for reference

# Routers (all resting on furniture; tops: desks/table 0.76 m, shelf board 1.0 m)
TX_LIVING_DESK = (7.309, 2.938, 0.9)  # demo Tx: living-room desk, next to the iMac
TX_CONF_TABLE = (-3.9, 0.8, 1.0)      # conference table in the office, just south of the projector
TX_OFFICE_DESK = (-6.0, -5.6, 0.9)    # office desk in the south-west corner of the office
TX_SHELF = (0.15, -4.9, 1.1)          # 1.0 m board of the wall shelf on the west wall of the living room


def cab(center, yaw_deg, size=CAB_SIZE):
    return dict(name="Metal_Cabinet", size=tuple(size), center=tuple(float(v) for v in center),
                yaw_deg=float(yaw_deg), z0=0.0, material=CABINET_MATERIAL)


def lcab(center, yaw_deg):
    return cab(center, yaw_deg, size=LOS_CAB_SIZE)


def mirror_yaw(tx, rx, center):
    """Yaw (deg) of a cabinet at `center` whose front face mirrors tx onto rx:
    the face normal is the bisector of (center->tx) and (center->rx); the
    cabinet's long axis is perpendicular to it."""
    c = np.asarray(center[:2], float)
    i = c - np.asarray(tx[:2], float); i /= np.linalg.norm(i)       # incoming direction
    o = np.asarray(rx[:2], float) - c; o /= np.linalg.norm(o)       # outgoing direction
    n = o - i                                                        # reflection law: o - i || normal
    return float(np.degrees(np.arctan2(n[1], n[0])) + 90.0)


def mirror_cab(tx, rx, center):
    return cab(center, mirror_yaw(tx, rx, center))


def phone(x, y):
    return (float(x), float(y), Z_PHONE)


# Which MCQ letter is correct, per task/set (same assignment as lesson1).
CORRECT = {
    ("phone_optimization", 1): "B", ("phone_optimization", 2): "D",
    ("los_creation", 1): "C",        ("los_creation", 2): "A",
    ("reflection_creation", 1): "D", ("reflection_creation", 2): "B",
}

CONDITIONS = []


def add(task, set_id, option, tx, rx, boxes=(), note="", rx_label="phone", rss_point=None,
        rss_mode="floor", floor_point=None):
    rx = tuple(float(v) for v in rx)
    CONDITIONS.append(dict(
        name=f"{task}_{set_id}_{option}", task=task, set=set_id, option=option,
        tx=tuple(float(v) for v in tx), rx=rx, rx_label=rx_label, rss_mode=rss_mode,
        rss_point=tuple(float(v) for v in (rss_point if rss_point is not None else rx[:2])),
        floor_point=(tuple(float(v) for v in floor_point) if floor_point is not None else None),
        boxes=list(boxes), correct=(CORRECT[(task, set_id)] == option), note=note,
    ))


# -----------------------------------------------------------------------------
# phone_optimization: router fixed, 4 candidate phone spots (1.1 m), pick the best.
# All spots are on open floor (the floor heatmap under the phone is what counts).
# -----------------------------------------------------------------------------
# Set 1 - router on the living-room desk.
add("phone_optimization", 1, "A", TX_LIVING_DESK, phone(1.5, 5.5),  note="kitchen, behind the kitchen wall (close to the router but NLoS)")
add("phone_optimization", 1, "B", TX_LIVING_DESK, phone(4.0, 0.0),  note="living room, open floor, direct line of sight to the router")
add("phone_optimization", 1, "C", TX_LIVING_DESK, phone(-5.0, -4.5), note="office, south of the conference table, behind the dividing wall")
add("phone_optimization", 1, "D", TX_LIVING_DESK, phone(-2.5, -6.5), note="office, south-east corner, behind the dividing wall")
# Set 2 - router on the conference table in the office.
add("phone_optimization", 2, "A", TX_CONF_TABLE, phone(4.0, -5.5),  note="living room between couch and TV, behind the dividing wall")
add("phone_optimization", 2, "B", TX_CONF_TABLE, phone(2.5, 5.2),   note="kitchen, behind the kitchen wall")
add("phone_optimization", 2, "C", TX_CONF_TABLE, phone(6.5, -1.5),  note="living room east side, far and behind the dividing wall")
add("phone_optimization", 2, "D", TX_CONF_TABLE, phone(-4.0, -4.0), note="office, open floor south of the conference table, line of sight")

# -----------------------------------------------------------------------------
# los_creation (v3): router + TV fixed with LoS; a metal cabinet goes to one
# of 4 furniture-anchored spots (behind/beside the couch, foot of the desk,
# beside the TV stand, next to / in front of the wall shelf).  Three spots cut
# the direct router->TV line at different places along the link; the fourth
# sits among them but just off the line and keeps LoS.
# -----------------------------------------------------------------------------
TVK = dict(rx_label="TV", rss_point=TV_RSS_POINT, rss_mode="rx_height", floor_point=TV_FLOOR_POINT)
# Set 1 - router on the living-room desk (line runs from the desk over the couch to the TV).
add("los_creation", 1, "A", TX_LIVING_DESK, TV, [lcab((6.25, -1.95), 0.0)], note="console cabinet behind the couch's back - on the line, blocks LoS", **TVK)
add("los_creation", 1, "B", TX_LIVING_DESK, TV, [lcab((7.2, 0.95), 0.0)],   note="wardrobe at the foot of the desk, butted against the east wall, right in front of the router - blocks LoS", **TVK)
add("los_creation", 1, "C", TX_LIVING_DESK, TV, [lcab((6.9, -3.4), 90.0)],  note="side unit at the couch's east end, beside the line - LoS kept", **TVK)
add("los_creation", 1, "D", TX_LIVING_DESK, TV, [lcab((5.3, -6.0), 90.0)],  note="tall cabinet standing in front of the TV stand's east end, beside the screen - on the line, blocks LoS", **TVK)
# Set 2 - router on the wall shelf (line runs diagonally from the west wall to the TV).
add("los_creation", 2, "A", TX_SHELF, TV, [lcab((0.25, -7.05), 90.0)], note="against the west wall just south of the shelf, beside the router - LoS kept", **TVK)
add("los_creation", 2, "B", TX_SHELF, TV, [lcab((0.55, -5.3), 90.0)], note="tall cabinet placed right in front of the shelf - blocks LoS", **TVK)
add("los_creation", 2, "C", TX_SHELF, TV, [lcab((1.65, -5.74), 0.0)], note="free-standing divider between the shelf area and the TV area - blocks LoS", **TVK)
add("los_creation", 2, "D", TX_SHELF, TV, [lcab((2.9, -6.45), 0.0)],  note="media cabinet beside the TV stand's west end - blocks LoS", **TVK)

# -----------------------------------------------------------------------------
# reflection_creation: router in the office (no LoS to the TV), one cabinet
# position mirrors the router through the 3 m doorway onto the TV.
# -----------------------------------------------------------------------------
# Set 1 - router on the conference table; mirror cabinet in the office, north of the doorway.
add("reflection_creation", 1, "A", TX_CONF_TABLE, TV, [cab((-6.5, 4.5), 0.0)],  note="office north-west - reflects towards the wrong side", **TVK)
add("reflection_creation", 1, "B", TX_CONF_TABLE, TV, [cab((0.9, 2.2), 90.0)],  note="living-room side of the doorway - obstructs the doorway instead", **TVK)
add("reflection_creation", 1, "C", TX_CONF_TABLE, TV, [cab((6.9, -7.5), 0.0)],  note="next to the TV, wrong side - no useful reflection", **TVK)
add("reflection_creation", 1, "D", TX_CONF_TABLE, TV, [mirror_cab(TX_CONF_TABLE, TV, (-1.0, 3.5))], note="office, north of the doorway, face towards the doorway - mirrors the router onto the TV", **TVK)
# Set 2 - router on the office desk (SW corner); mirror cabinet on the living-room side, near the kitchen wall.
add("reflection_creation", 2, "A", TX_OFFICE_DESK, TV, [cab((6.5, -6.5), 90.0)], note="living room south-east corner, next to the TV - no useful reflection", **TVK)
add("reflection_creation", 2, "B", TX_OFFICE_DESK, TV, [mirror_cab(TX_OFFICE_DESK, TV, (1.3, 3.4))], note="living room, below the kitchen wall, face towards the doorway/TV - mirrors the router onto the TV", **TVK)
add("reflection_creation", 2, "C", TX_OFFICE_DESK, TV, [cab((-0.9, 2.2), 90.0)], note="office side of the doorway - no useful reflection", **TVK)
add("reflection_creation", 2, "D", TX_OFFICE_DESK, TV, [cab((-7.0, -0.5), 90.0)], note="office, north of the router along the west wall - no useful reflection", **TVK)

assert len(CONDITIONS) == 24
assert sum(c["correct"] for c in CONDITIONS) == 6


def siblings(cond):
    """The other three options of the same task/set."""
    return [c for c in CONDITIONS if c["task"] == cond["task"] and c["set"] == cond["set"] and c is not cond]
