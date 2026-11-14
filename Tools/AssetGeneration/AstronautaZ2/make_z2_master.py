"""Rebuild the Z-2 master views from the supplied 2D builder and photo references."""
from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path
import sys
import types

PROJECT = Path(__file__).resolve().parents[3] / "My project (4)"
ASSET_SOURCE = PROJECT / "Assets" / "Art" / "Characters" / "AstronautaZ2" / "Source"
ROOT = ASSET_SOURCE
SOURCE = Path(__file__).resolve().with_name("base_builder.py")
MASTER = ASSET_SOURCE / "master"
LAYER_SVG = ASSET_SOURCE / "layers"
MASTER.mkdir(parents=True, exist_ok=True)
LAYER_SVG.mkdir(parents=True, exist_ok=True)

# The source builder only needs CairoSVG when it rasterizes. We rasterize these
# SVGs through the bundled Sharp renderer, so the module itself is stubbed here.
sys.modules["cairosvg"] = types.SimpleNamespace()
spec = spec_from_file_location("z2_base", SOURCE)
m = module_from_spec(spec)
sys.modules[spec.name] = m
spec.loader.exec_module(m)

# Suit colors and line weights are taken from the supplied Z-2 views: warm white
# pressure garment, charcoal seals/gloves/boots, restrained red and blue bands.
for key, value in {
    "OUT": "#716E68", "IVORY": "#F0EEE8", "IVSH": "#D0CDC5", "IVDK": "#99968E",
    "METAL": "#A5A7A6", "METSH": "#6F7374", "METHI": "#E6E7E4",
    "BLUE": "#31577B", "BLUSH": "#263F59", "TURQ": "#818D91",
    "RED": "#B63A32", "REDSH": "#852B27", "ORNG": "#A55740",
    "SAND": "#B6A487", "GREEN": "#687366", "GRAY": "#62615E",
    "GRAYSH": "#3C3B39", "DEEP": "#202020", "CREAM": "#F8F7F2",
}.items():
    setattr(m, key, value)

# Fine edges keep the realistic suit silhouette without the thick cartoon border.
m.SW_OUT, m.SW_IN = 1.1, 0.8
m.part.__defaults__ = (8, 1.2, 0.7)
m.flat.__defaults__ = (1.0,)
m.seam.__defaults__ = (1.1, 0.34, None)
m.ridges = lambda x, y, w, h, n, op=0.28: "".join(
    f'<path d="M {x+w*i/n:.1f} {y+5} V {y+h-5}" stroke="{m.OUT}" stroke-opacity="{op}" stroke-width="1.1" stroke-linecap="round"/>'
    for i in range(1, n)
)
m.DEFS = (
    '<defs><linearGradient id="vis" x1="0" y1="0" x2="0" y2="1">'
    '<stop offset="0" stop-color="#293136"/><stop offset="0.5" stop-color="#141A1D"/>'
    '<stop offset="1" stop-color="#080B0D"/></linearGradient>'
    '<linearGradient id="ivoryFabric" x1="0" y1="0" x2="1" y2="1">'
    '<stop offset="0" stop-color="#F8F6F0"/><stop offset="0.45" stop-color="#EEECE5"/>'
    '<stop offset="0.78" stop-color="#E3E0D8"/><stop offset="1" stop-color="#D0CDC5"/>'
    '</linearGradient><linearGradient id="ivoryShadow" x1="0" y1="0" x2="0.8" y2="1">'
    '<stop offset="0" stop-color="#E0DDD5"/><stop offset="1" stop-color="#A7A49D"/>'
    '</linearGradient></defs>'
)
# Keep the photograph's warm, softly shaded textile/hard-shell appearance in
# the vector source, rather than the older flat cartoon fills.
m.IVORY = "url(#ivoryFabric)"
m.IVSH = "url(#ivoryShadow)"

# Raise the shoulders so the pressure torso meets the helmet without a neck.
m.HUT_F = (
    "M 178 184 Q 181 160 214 157 Q 256 148 298 157 Q 331 160 334 184 "
    "L 348 221 Q 368 246 368 284 L 358 338 Q 354 392 338 440 "
    "L 174 440 Q 158 392 154 338 L 144 284 Q 144 250 164 225 Z"
)

def visor_real(d, *args, **kwargs):
    return (
        f'<path d="{d}" fill="none" stroke="{m.OUT}" stroke-width="14" stroke-linejoin="round"/>'
        f'<path d="{d}" fill="none" stroke="{m.IVDK}" stroke-width="8" stroke-linejoin="round"/>'
        f'<path d="{d}" fill="url(#vis)" stroke="#30383B" stroke-width="2.2" stroke-linejoin="round"/>'
        '<path d="M 216 119 Q 230 108 249 110" fill="none" stroke="#A7B0B0" stroke-width="2.3" opacity=".24" stroke-linecap="round"/>'
    )

def helmet_front_real():
    s = [m.ell(m.CX, 125, 90, 82, m.IVORY, m.IVSH, band=9, sw=1.5)]
    # Smooth shell, low-profile top fittings, and small side hinges from the photos.
    s.append(m.seam("M 184 86 Q 202 42 256 40 Q 310 42 328 86", 1.25, .34))
    # Raised bezel gives the three optics a clear integrated front contour.
    s.append(m.rr(218, 40, 76, 30, 10, m.IVSH, m.IVDK, band=2, sw=.9))
    # Three forward optics in one compact helmet mount.
    s.append(m.rr(222, 43, 68, 25, 8, m.METAL, m.METSH, band=4, sw=1.2))
    for px in (238, 256, 274):
        s.append(m.fci(px, 55, 6.4, m.GRAYSH, .8))
        s.append(m.fci(px, 55, 3.6, m.DEEP, .6))
        s.append(m.fci(px-1.2, 53.5, 1.1, m.METHI, .35))
    for px in (174, 320):
        s.append(m.rr(px, 102, 18, 30, 8, m.METAL, m.METSH, band=3, sw=1.1))
    s.append(visor_real(m.VIS_F))
    s.append('<path d="M 208 116 Q 221 107 236 108" fill="none" stroke="#D5D8D5" stroke-width="2" opacity=".24"/>')
    return "".join(s)

def helmet_side_real(which):
    # Side profiles keep the clean shell and visor only: remove the dark brow
    # rail, dotted line, and camera-like side modules from the old drawing.
    s = [m.ell(262, 125, 84, 82, m.IVORY, m.IVSH, band=9, sw=1.3)]
    s.append(visor_real(m.VIS_S))
    # Thin shell contour at the forehead shows the front camera bezel in profile
    # without adding a separate side camera or protruding lens.
    s.append('<path d="M 299 51 Q 313 52 320 61 L 319 66 Q 311 60 300 60 Z" fill="none" stroke="#77756F" stroke-width="1.4" stroke-linejoin="round"/>')
    return "".join(s)

def nasa_insignia_real():
    # Classic NASA meatball, drawn as clean vector shapes to match the sprite.
    return (
        '<g transform="translate(256 236)">'
        '<ellipse rx="38" ry="27" fill="#0B3D91" stroke="#E6E4DE" stroke-width="1.5"/>'
        '<g fill="#FFFFFF">'
        '<circle cx="-25" cy="-10" r="1.4"/><circle cx="-17" cy="12" r="1.1"/>'
        '<circle cx="-7" cy="-18" r="1.2"/><circle cx="5" cy="16" r="1.1"/>'
        '<circle cx="19" cy="-14" r="1.3"/><circle cx="27" cy="8" r="1.1"/>'
        '<circle cx="12" cy="-4" r=".9"/><circle cx="-29" cy="4" r=".9"/>'
        '</g>'
        '<path d="M -31 13 Q -5 -3 13 -6 Q 29 -9 34 -18 Q 32 -4 16 5 Q -5 16 -31 13 Z" fill="#FC3D21"/>'
        '<text x="0" y="7" text-anchor="middle" fill="#FFFFFF" font-family="Arial,sans-serif" '
        'font-size="17" font-weight="700" font-style="italic" letter-spacing="-.8">NASA</text>'
        '</g>'
    )

def helmet_back_real():
    s = [m.ell(m.CX, 125, 90, 82, m.IVORY, m.IVSH, band=9, sw=1.5)]
    s.append(m.seam("M 174 126 Q 256 151 338 126", 1.3, .42))
    s.append(m.seam("M 192 81 Q 256 55 320 81", 1.1, .3))
    s.append(m.rr(240, 99, 32, 24, 10, m.METAL, m.METSH, band=3, sw=1.1))
    s.append(m.fci(256, 111, 5.2, m.GRAY, .9))
    s.append(m.rr(161, 111, 13, 28, 6, m.METAL, m.METSH, band=2, sw=1.0))
    s.append(m.rr(338, 111, 13, 28, 6, m.METAL, m.METSH, band=2, sw=1.0))
    return "".join(s)

def torso_front_real():
    s = [m.pth(m.HUT_F, m.IVORY, m.IVSH, band=10, sw=1.5)]
    s.append(m.seam("M 206 232 Q 187 270 166 297", 1.1, .28, "7 6"))
    s.append(m.seam("M 306 232 Q 325 270 346 297", 1.1, .28, "7 6"))
    s.append(m.seam("M 194 340 Q 190 389 190 434", 1.0, .24, "7 6"))
    s.append(m.seam("M 318 340 Q 322 389 322 434", 1.0, .24, "7 6"))
    # Compact chest service plate and connector cluster, matching the reference renders.
    s.append(m.rr(232, 285, 48, 52, 8, m.IVSH, m.IVDK, band=3, sw=1.25))
    s.append(m.rr(240, 292, 32, 20, 5, m.METAL, m.METSH, band=2, sw=1.0))
    s.append(m.fci(256, 302, 4.4, m.DEEP, .7))
    for px, color in ((240, m.BLUE), (256, m.IVORY), (272, m.RED)):
        s.append(m.fci(px, 326, 5.3, m.GRAY, .8))
        s.append(m.fci(px, 326, 2.6, color, .55))
    return "".join(s)

def rear_entry_shell():
    # Rounded, convex shell from the helmet crown down to the navel.
    s = [m.pth(
        "M 207 63 Q 256 43 305 63 C 339 78 357 111 359 151 "
        "Q 366 224 356 302 Q 351 357 338 389 L 174 389 "
        "Q 161 357 156 302 Q 146 224 153 151 C 155 111 173 78 207 63 Z",
        m.IVORY, m.IVSH, band=9, sw=1.5)]
    # Keep the rear-facing shell clean; its rounded outline defines the pack.
    s.append(nasa_insignia_real())
    return "".join(s)

def torso_side_real(which):
    # Side profile: a separate convex shell sits behind the suit, ending at
    # the navel seam. The suit torso begins at that divider.
    pack = (
        "M 219 72 Q 228 56 204 42 Q 174 35 146 64 "
        "C 122 92 117 143 117 203 Q 113 278 128 341 "
        "Q 136 374 158 389 Q 180 398 203 389 "
        "L 203 151 Q 205 101 219 72 Z"
    )
    body = (
        "M 196 190 Q 265 187 300 222 Q 333 240 339 275 L 334 351 "
        "Q 331 426 326 460 L 203 462 Q 197 448 196 430 L 196 190 Z"
    )
    s = [m.pth(pack, m.IVORY, m.IVSH, band=8, sw=1.4),
         m.pth(body, m.IVORY, m.IVSH, band=10, sw=1.5)]
    # The vertical divider runs the pack's full length and ends at the navel.
    s.append('<path d="M 196 389 C 197 281 196 183 198 135 Q 199 101 190 84" fill="none" stroke="#77756F" stroke-width="4" stroke-linecap="round"/>')
    # Flush service connector on the forward side, kept compact like the photo.
    s.append(m.ell(329, 321, 8, 17, m.IVSH, m.IVDK, band=2, sw=.9))
    s.append(m.fci(330, 316, 2.3, m.GRAY, .7))
    s.append(m.fci(330, 326, 2.3, m.RED, .6))
    return "".join(s)

def torso_back_real():
    back_torso = m.HUT_F.replace("440", "462")
    return m.pth(back_torso, m.IVORY, m.IVSH, band=10, sw=1.5) + rear_entry_shell()

def neck_ring_real(x, w):
    # No independent neck part: the torso's raised shoulder line joins the helmet.
    return ""

def waist_real(x, w):
    return m.rr(x, 434, w, 30, 12, m.GRAY, m.GRAYSH, band=3, sw=1.3) + m.seam(f"M {x+8} 441 H {x+w-8}", 1.2, .42)

def arm_real(view, patch=False, bend=0):
    side = view == "side"
    s = [m.ell(0, 4, 24 if side else 38, 34 if side else 31, m.METAL, m.METSH, band=5, sw=1.4)]
    s.append(m.ell(0, 4, 14 if side else 26, 24 if side else 20, m.IVORY, m.IVSH, band=3, sw=1.1))
    s.append(m.rr(-31, 14, 62, 108, 24, m.IVORY, m.IVSH, band=7, sw=1.4))
    s.append(m.seam("M -17 29 Q -21 62 -15 101", .95, .25))
    s.append(m.band(-31, 70, 62, 10, m.RED, 3))
    s.append(m.band(-31, 82, 62, 4, m.BLUE, 2))
    if patch:
        s.append(m.ell(0, 48, 14, 12, m.BLUE, m.BLUSH, band=2, sw=1.0))
        s.append(m.seam("M -10 49 Q 0 41 10 48", 1.0, .75))
        s.append(m.seam("M -10 51 Q 0 57 11 49", 1.2, .85))
    s.append(m.rr(-35, 114, 70, 28, 11, m.METAL, m.METSH, band=4, sw=1.4))
    f = ['<g transform="rotate(' + str(bend) + ' 0 128)">']
    f.append(m.rr(-28, 136, 56, 78, 19, m.IVORY, m.IVSH, band=6, sw=1.4))
    f.append(m.seam("M -15 150 Q -18 179 -13 204", .9, .24))
    f.append(m.rr(-32, 208, 64, 24, 9, m.METAL, m.METSH, band=4, sw=1.3))
    f.append(m.frr(-36, 214, 10, 12, 4, m.BLUE, .9)); f.append(m.frr(26, 214, 10, 12, 4, m.BLUE, .9))
    if not side:
        f.append(m.rr(-29, 228, 58, 44, 17, m.GRAY, m.GRAYSH, band=5, sw=1.4))
        f.append(m.rr(-18, 240, 36, 12, 6, m.METAL, m.METSH, band=2, sw=1.0))
        for i in range(4):
            x = -26 + i * 13.4
            f.append(m.rr(x, 256, 12.6, 46, 6.3, m.GRAY, m.GRAYSH, band=3, sw=1.1))
            f.append(m.rr(x, 288, 12.6, 14, 6.3, m.DEEP, m.GRAYSH, band=2, sw=.8))
    else:
        f.append(m.rr(-24, 228, 48, 66, 21, m.GRAY, m.GRAYSH, band=5, sw=1.4))
        for yy in (249, 263, 277): f.append(m.seam(f"M 2 {yy} H 18", .9, .48))
        f.append(m.rr(-24, 280, 48, 14, 7, m.DEEP, m.GRAYSH, band=2, sw=1.0))
    f.append('</g>')
    s.append("".join(f))
    return "".join(s)

def boot_front_real(heel=False):
    s = [m.rr(-42, 258, 84, 62, 22, m.GRAY, m.GRAYSH, band=5, sw=1.5)]
    s.append(m.rr(-47, 306, 94, 18, 8, m.DEEP, m.GRAYSH, band=2, sw=1.2))
    for i in range(7): s.append(f'<path d="M {-38+i*12.7:.1f} 310 V 320" stroke="{m.METSH}" stroke-width="1.6" stroke-linecap="round" opacity=".65"/>')
    s.append(m.seam("M -28 269 Q 0 261 27 269", .9, .28))
    return "".join(s)

def leg_front_real(view):
    s = [m.rr(-38, 6, 76, 126, 30, m.IVORY, m.IVSH, band=7, sw=1.5)]
    s.append(m.seam("M -22 25 Q -27 72 -20 110", .95, .26))
    s.append(m.band(-38, 80, 76, 11, m.RED, 3))
    s.append(m.band(-38, 93, 76, 4, m.BLUE, 2))
    s.append(m.seam("M -34 128 Q 0 139 34 128", 1.1, .35))
    s.append(m.rr(-34, 148, 68, 102, 20, m.IVORY, m.IVSH, band=6, sw=1.5))
    s.append(m.seam("M -18 164 Q -21 208 -15 237", .95, .24))
    # Soft pressure-suit wear around the knee and shin, sampled from the
    # mottled off-white fabric in the supplied photographs.
    s.append('<path d="M -25 177 Q -14 171 -4 179 Q 1 185 -8 190 Q -19 194 -27 188 Z" fill="#817B72" opacity=".12"/>')
    s.append('<path d="M 5 207 Q 16 201 23 207 Q 26 212 17 216 L 6 215 Z" fill="#817B72" opacity=".10"/>')
    s.append('<path d="M -27 63 Q -16 57 -7 63 Q -6 69 -17 72 L -27 70 Z" fill="#817B72" opacity=".09"/>')
    # The dark articulated knee seal bridges the two padded fabric sections.
    s.append(m.rr(-33, 126, 66, 29, 13, m.IVSH, m.IVDK, band=3, sw=1.0))
    s.append(m.seam("M -25 140 Q 0 146 25 140", .8, .35))
    s.append(m.rr(-37, 242, 74, 20, 9, m.BLUE, m.BLUSH, band=3, sw=1.25))
    s.append(boot_front_real(heel=(view == "back")))
    return "".join(s)

def boot_side_real():
    path = "M -40 262 L 38 262 Q 53 270 65 280 Q 91 285 107 299 Q 117 309 114 322 L -47 324 L -47 280 Q -47 263 -40 262 Z"
    s = [m.pth(path, m.GRAY, m.GRAYSH, band=5, sw=1.5)]
    sole = "M -52 311 L 110 311 Q 124 311 124 322 Q 124 333 108 333 L -44 333 Q -54 333 -54 323 Z"
    s.append(m.pth(sole, m.DEEP, m.GRAYSH, band=2, sw=1.2))
    for i in range(10): s.append(f'<path d="M {-40+i*16:.1f} 316 V 328" stroke="{m.METSH}" stroke-width="1.6" stroke-linecap="round" opacity=".65"/>')
    s.append(m.seam("M 46 274 Q 39 294 37 309", .9, .28))
    return "".join(s)

def leg_side_real(tone="front"):
    s = [m.rr(-40, 12, 80, 118, 30, m.IVORY, m.IVSH, band=7, sw=1.5)]
    s.append(m.seam("M -20 28 Q -24 76 -18 112", .95, .24))
    s.append(m.band(-40, 82, 80, 11, m.RED, 3)); s.append(m.band(-40, 95, 80, 4, m.BLUE, 2))
    s.append(m.seam("M -36 132 Q 0 144 36 132", 1.1, .34))
    s.append(m.rr(-34, 148, 68, 102, 20, m.IVORY, m.IVSH, band=6, sw=1.5))
    s.append(m.seam("M -17 165 Q -20 211 -14 238", .95, .24))
    s.append('<path d="M -25 181 Q -13 174 -3 181 Q 1 187 -8 192 Q -19 196 -27 189 Z" fill="#817B72" opacity=".12"/>')
    s.append('<path d="M 6 210 Q 16 204 23 210 Q 25 215 16 219 L 7 218 Z" fill="#817B72" opacity=".10"/>')
    s.append(m.rr(-33, 126, 66, 29, 13, m.IVSH, m.IVDK, band=3, sw=1.0))
    s.append(m.seam("M -25 140 Q 0 146 25 140", .8, .35))
    s.append(m.rr(-38, 242, 76, 20, 9, m.BLUE, m.BLUSH, band=3, sw=1.25))
    s.append(boot_side_real())
    return "".join(s)

def brief_real(x, w):
    return m.rr(x, 462, w, 76, 30, m.IVORY, m.IVSH, band=7, sw=1.5) + m.seam(f"M {x+w/2} 486 V 535", .95, .25)

def lower_real(view, which="right"):
    s = []
    if view in ("front", "back"):
        s.append(f'<g transform="translate({m.CX-44},{m.HIP_Y})">{leg_front_real(view)}</g>')
        s.append(f'<g transform="translate({m.CX+44},{m.HIP_Y})">{leg_front_real(view)}</g>')
        s.append(brief_real(172,168))
        if view == "front": s.append(waist_real(168,176))
    else:
        s.append(f'<g transform="translate({m.CX-16},{m.HIP_Y})">{leg_side_real("back")}</g>')
        s.append(f'<g transform="translate({m.CX+14},{m.HIP_Y})">{leg_side_real("front")}</g>')
        s.append(brief_real(192,140))
    return "".join(s)

m.visor = visor_real
m.helmet_front = helmet_front_real
m.helmet_back = helmet_back_real
m.helmet_side = helmet_side_real
m.torso_front = torso_front_real
m.plss_back = rear_entry_shell
m.torso_back = torso_back_real
m.torso_side = torso_side_real
m.neck_ring = neck_ring_real
m.waist = waist_real
m.arm = arm_real
m.boot_front = boot_front_real
m.leg_front = leg_front_real
m.boot_side = boot_side_real
m.leg_side = leg_side_real
m.lower = lower_real

for name, (view, which, flip) in m.VIEWS.items():
    m._n[0] = 0
    full = m.svg(lower_real(view, which) + m.upper(view, which), flip=flip)
    (MASTER / f"char_astronauta_{name}_neutro_v02.svg").write_text(full, encoding="utf-8")
    top = m.svg(m.upper(view, which), flip=flip)
    bottom = m.svg(lower_real(view, which), flip=flip)
    (LAYER_SVG / f"char_astronauta_{name}_superior_v02.svg").write_text(top, encoding="utf-8")
    (LAYER_SVG / f"char_astronauta_{name}_inferior_v02.svg").write_text(bottom, encoding="utf-8")

print("Rebuilt four integrated Z-2 master views and eight aligned cutout layers.")
