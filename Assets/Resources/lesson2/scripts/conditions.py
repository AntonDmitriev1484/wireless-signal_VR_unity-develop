"""
Declarative definition of the 6 WiViz lesson-2 conditions
(communication & interference).

All coordinates are Sionna coordinates (x, y, z in metres, z up).

The router (Tx) is fixed on the office desk in the SW corner of the
office/conference room (the largest room, ~130 m^2).  Phone A is fixed in the
same room with line of sight; only Phone B moves.  All data is Tx -> phone(s);
Unity reverses the paths for phone -> router playback and handles the
simultaneous / take-turns (TDMA) interactions, so no reverse datasets exist.

By reciprocity the floor-heatmap value under Phone B equals Phone B's power at
the router, so RSS_dBm (floor cells under each phone, the lesson1_v2
convention) directly measures how strongly Phone B interferes at the router:
SIR at the router = RSS(Phone A) - RSS(Phone B).

The scene is never modified (no extra objects); every condition uses the
immutable base scene, so all six heatmaps are the identical router-coverage
map (deterministic, fixed seed).
"""

# Router: office desk, SW corner of the office (same as lesson1_v2 reflection set 2).
TX = (-6.0, -5.6, 0.9)

Z_PHONE = 1.1


def phone(x, y):
    return (float(x), float(y), Z_PHONE)


PHONE_A = phone(-3.0, -3.0)      # fixed: office open floor, LoS to the router, ~4 m
PHONE_B_BASE = phone(-2.5, -6.5)  # baseline interferer: office SE, LoS, RSS at the
                                  # router within ~1 dB of Phone A -> SIR ~ 0 dB

# Spatial task: where should Phone B move so the router hears Phone A clearly?
PHONE_B_OPTIONS = {
    "A": (phone(-1.0, -5.5), "small move within the office, still line of sight - barely helps"),
    "B": (phone(-6.5, 4.5),  "far north-west corner of the office, still line of sight - distance only"),
    "C": (phone(4.0, -1.5),  "living room centre, behind the dividing wall"),
    "D": (phone(6.5, 6.0),   "kitchen, far and behind two walls - interference effectively gone"),
}
CORRECT = "D"

CONDITIONS = []


def add(name, rxs, rx_labels, note="", correct=False, option=None):
    CONDITIONS.append(dict(
        name=name, tx=tuple(float(v) for v in TX),
        rxs=[tuple(float(v) for v in r) for r in rxs], rx_labels=list(rx_labels),
        note=note, correct=bool(correct), option=option,
    ))


add("communication_single", [PHONE_A], ["PhoneA"],
    note="baseline communication: router -> Phone A (Unity reverses for Phone A -> router)")
add("interference_baseline", [PHONE_A, PHONE_B_BASE], ["PhoneA", "PhoneB"],
    note="two phones transmit at once; Phone B as strong as Phone A at the router -> garbled")
for opt, (pos, why) in sorted(PHONE_B_OPTIONS.items()):
    add(f"interference_space_{opt}", [PHONE_A, pos], ["PhoneA", "PhoneB"],
        note=f"Phone B moved: {why}", correct=(opt == CORRECT), option=opt)

assert len(CONDITIONS) == 6
assert sum(c["correct"] for c in CONDITIONS) == 1
assert all(c["rxs"][0] == tuple(PHONE_A) for c in CONDITIONS)
