"""Create pose variants from the supplied NASA Z-2-inspired vector model."""
from pathlib import Path

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

print(f"Wrote {4 + len(POSES_SPEC) * 2} SVG sprites to {POSES}")
