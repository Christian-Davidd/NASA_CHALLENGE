"""Create pose variants from the supplied NASA Z-2-inspired vector model."""
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent
MASTER = ROOT / "master"
POSES = ROOT / "poses"
POSES.mkdir(parents=True, exist_ok=True)
WIDTH, HEIGHT, PAD_X, PAD_Y = 640, 960, 64, 32
COLOR_CORRECTIONS = {
    "#3A7F97": "#28363F",  # dark reflective visor glass
    "#25586D": "#17252D",
    "#173C50": "#0E171D",
    "#91CDD0": "#879AA3",  # subdued visor reflections and indicators
}


def prepare(svg):
    for old, new in COLOR_CORRECTIONS.items():
        svg = svg.replace(old, new)
    svg = svg.replace('width="512" height="896" viewBox="0 0 512 896"',
                      f'width="{WIDTH}" height="{HEIGHT}" viewBox="0 0 {WIDTH} {HEIGHT}"', 1)
    svg = svg.replace("><defs>", f"><g transform=\"translate({PAD_X},{PAD_Y})\"><defs>", 1)
    return svg[:-6] + "</g></svg>"


for direction, filename in (
    ("frente", "char_astronauta_frente_neutro_v02.svg"),
    ("espalda", "char_astronauta_espalda_neutro_v02.svg"),
    ("lado_derecho", "char_astronauta_lado_derecho_neutro_v02.svg"),
    ("lado_izquierdo", "char_astronauta_lado_izquierdo_neutro_v02.svg"),
):
    base_svg = (MASTER / filename).read_text(encoding="utf-8")
    out_name = f"char_astronauta_{direction}_neutro_z2_v01.svg"
    (POSES / out_name).write_text(prepare(base_svg), encoding="utf-8")

POSES_SPEC = {
    "caminar_01": (14, -18, 18, -14),
    "caminar_02": (-18, 18, -18, -14),
    "interactuar": (-52, 0, 0, -14),
    "reparar": (-35, 24, -22, -34),
    "alerta": (-128, 0, 0, -14),
}

for side, source_name in (
    ("derecho", "char_astronauta_lado_derecho_neutro_v02.svg"),
    ("izquierdo", "char_astronauta_lado_izquierdo_neutro_v02.svg"),
):
    source = (MASTER / source_name).read_text(encoding="utf-8")
    for pose, (arm, back_leg, front_leg, elbow) in POSES_SPEC.items():
        svg = source
        replacements = (
            ('translate(262,274) rotate(-6)', f'translate(262,274) rotate({arm})'),
            ('translate(240,524)', f'translate(240,524) rotate({back_leg})'),
            ('translate(270,524)', f'translate(270,524) rotate({front_leg})'),
            ('rotate(-14 0 128)', f'rotate({elbow} 0 128)'),
        )
        for old, new in replacements:
            if old not in svg:
                raise ValueError(f"Could not find expected pose transform {old!r} in {source_name}")
            svg = svg.replace(old, new, 1)
        # Keep a common 640 x 960 canvas so stretched poses retain full limbs,
        # while the suit's feet stay on the same sprite pivot as the idle art.
        svg = prepare(svg)
        name = f"char_astronauta_lado_{side}_{pose}_z2_v01.svg"
        (POSES / name).write_text(svg, encoding="utf-8")

# Two subtle walk phases for each vertical-facing view. Keep the hip swing small
# and flex the lower leg at the knee so the padded suit bends without looking
# like the entire straight leg has been rotated from the waist.
SVG_NS = "http://www.w3.org/2000/svg"
ET.register_namespace("", SVG_NS)
for direction in ("frente", "espalda"):
    source_path = MASTER / f"char_astronauta_{direction}_neutro_v02.svg"
    for phase, left_hip, right_hip, left_knee, right_knee in (
        ("caminar_01", 2, 0, 7, 0),
        ("caminar_02", 0, -2, 0, -7),
    ):
        root = ET.parse(source_path).getroot()
        legs = {
            leg.attrib.get("transform"): leg
            for leg in root.iter(f"{{{SVG_NS}}}g")
            if leg.attrib.get("transform", "").startswith("translate(212,524)")
            or leg.attrib.get("transform", "").startswith("translate(300,524)")
        }
        for x, hip_angle, knee_angle in (
            (212, left_hip, left_knee), (300, right_hip, right_knee)
        ):
            leg = next((g for t, g in legs.items() if t.startswith(f"translate({x},524)")), None)
            if leg is None:
                raise ValueError(f"Could not find expected {direction} leg pivot at x={x}")
            leg.set("transform", f"translate({x},524) rotate({hip_angle})")
            children = list(leg)
            # The first six elements draw the thigh. The rest form the padded
            # knee, shin and boot, which pivot together around the knee joint.
            if len(children) < 15:
                raise ValueError(f"Unexpected leg structure in {source_path.name}")
            lower_leg = ET.Element(f"{{{SVG_NS}}}g", {
                "transform": f"rotate({knee_angle} 0 142)"
            })
            for child in children[6:]:
                leg.remove(child)
                lower_leg.append(child)
            leg.append(lower_leg)
        svg = ET.tostring(root, encoding="unicode")
        out_name = f"char_astronauta_{direction}_{phase}_z2_v01.svg"
        (POSES / out_name).write_text(prepare(svg), encoding="utf-8")

print(f"Wrote {4 + len(POSES_SPEC) * 2 + 4} SVG sprites to {POSES}")
