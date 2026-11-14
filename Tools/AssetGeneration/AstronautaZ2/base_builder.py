import os, cairosvg
from PIL import Image

W, H = 512, 896
CX = 256
# ---------------- Paleta (biblia + tonos derivados / reales del xEMU) ----------------
OUT   = "#344A5A"
IVORY = "#F2EBDD"; IVSH = "#D6CAB3"; IVDK = "#BFB39B"
METAL = "#A9BBC6"; METSH = "#7F95A3"; METHI = "#E4EEF3"
BLUE  = "#397B91"; BLUSH = "#2C6173"
TURQ  = "#91CDD0"
RED   = "#C8443A"; REDSH = "#A23329"
ORNG  = "#D97C55"
SAND  = "#EFA56C"
GREEN = "#67A86D"
GRAY  = "#66798A"; GRAYSH = "#4B5D6B"
DEEP  = "#203548"
CREAM = "#FBF7EF"
SW_OUT, SW_IN = 3.4, 2.2
_n = [0]
def uid():
    _n[0] += 1; return f"k{_n[0]}"

def attrs(a): return " ".join(f'{k.replace("_","-")}="{v}"' for k, v in a.items())
def el(tag, **a): return f"<{tag} {attrs(a)}/>"

def part(tag, fill, shade, band=8, sw=SW_OUT, dy=0.7, **a):
    cid = uid()
    return (f'<clipPath id="{cid}">{el(tag, **a)}</clipPath><g>{el(tag, fill=shade, **a)}'
            f'<g clip-path="url(#{cid})"><g transform="translate({-band},{-band*dy})">{el(tag, fill=fill, **a)}</g></g>'
            f'{el(tag, fill="none", stroke=OUT, stroke_width=sw, stroke_linejoin="round", **a)}</g>')

def rr(x, y, w, h, r, fill, shade, **k): return part("rect", fill, shade, x=x, y=y, width=w, height=h, rx=r, **k)
def ell(cx, cy, rx, ry, fill, shade, **k): return part("ellipse", fill, shade, cx=cx, cy=cy, rx=rx, ry=ry, **k)
def circ(cx, cy, r, fill, shade, **k): return part("circle", fill, shade, cx=cx, cy=cy, r=r, **k)
def pth(d, fill, shade, **k): return part("path", fill, shade, d=d, **k)
def flat(tag, fill, sw=SW_IN, **a): return el(tag, fill=fill, stroke=OUT, stroke_width=sw, stroke_linejoin="round", **a)
def frr(x, y, w, h, r, fill, sw=SW_IN): return flat("rect", fill, sw, x=x, y=y, width=w, height=h, rx=r)
def fci(cx, cy, r, fill, sw=SW_IN): return flat("circle", fill, sw, cx=cx, cy=cy, r=r)
def seam(d, w=2.2, op=.5, dash=None):
    ds = f' stroke-dasharray="{dash}"' if dash else ""
    return f'<path d="{d}" fill="none" stroke="{OUT}" stroke-opacity="{op}" stroke-width="{w}" stroke-linecap="round"{ds}/>'
def ridges(x, y, w, h, n, op=.35):
    return "".join(f'<path d="M {x+w*i/n:.1f} {y+5} V {y+h-5}" stroke="{OUT}" stroke-opacity="{op}" stroke-width="2.2" stroke-linecap="round"/>' for i in range(1, n))
def mring(x, y, w, h, n=8, r=None):
    r = r if r is not None else h / 2.3
    return (rr(x, y, w, h, r, METAL, METSH, band=5) + ridges(x, y, w, h, n) +
            f'<path d="M {x+r:.1f} {y+6} H {x+w-r:.1f}" stroke="{METHI}" stroke-width="3.2" stroke-linecap="round" opacity=".9"/>')
def band(x, y, w, h, color=RED, r=3):
    return frr(x, y, w, h, r, color, 2)

# =========================================================== HELMET
VIS_F = "M 186 128 Q 186 94 222 94 L 290 94 Q 326 94 326 128 L 326 150 Q 326 190 288 190 L 224 190 Q 186 190 186 150 Z"
def visor(d, hl_dx=0, hl_dy=0, head=(256, 142, 52, 40)):
    s = []
    s.append(f'<path d="{d}" fill="none" stroke="{OUT}" stroke-width="24" stroke-linejoin="round"/>')
    s.append(f'<path d="{d}" fill="none" stroke="{IVSH}" stroke-width="17" stroke-linejoin="round"/>')
    s.append(f'<path d="{d}" fill="none" stroke="{IVORY}" stroke-width="7" stroke-linejoin="round" transform="translate(-1.5,-1.5)" opacity=".9"/>')
    s.append(f'<path d="{d}" fill="url(#vis)" stroke="{OUT}" stroke-width="3" stroke-linejoin="round"/>')
    cx, cy, rx, ry = head
    s.append(f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="#12303F" opacity=".55"/>')
    return "".join(s)

def helmet_front():
    s = []
    s.append(ell(CX, 125, 90, 82, IVORY, IVSH, band=11))
    for px in (172, 322):                                    # luces laterales
        s.append(rr(px, 82, 18, 32, 8, IVSH, IVDK, band=3, sw=2.6))
        s.append(frr(px + 4, 88, 10, 20, 5, TURQ, 1.8))
    s.append(frr(204, 66, 104, 13, 6.5, DEEP))                # banda de luces xINFO
    for i in range(7): s.append(el("circle", cx=216 + i * 13.3, cy=72.5, r=3.2, fill=TURQ))
    s.append(visor(VIS_F))
    s.append(f'<path d="M 202 122 Q 204 108 226 106 L 246 106 Q 228 114 226 140 Z" fill="{TURQ}" opacity=".9"/>')
    s.append(f'<path d="M 254 104 L 270 104" stroke="{TURQ}" stroke-width="5" stroke-linecap="round" opacity=".9"/>')
    s.append(f'<path d="M 298 172 Q 312 164 316 148" fill="none" stroke="{TURQ}" stroke-width="4.5" stroke-linecap="round" opacity=".55"/>')
    # camara (lado derecho del astronauta = izquierda del espectador)
    s.append(rr(214, 46, 26, 20, 7, GRAY, GRAYSH, band=4, sw=2.8)); s.append(fci(227, 56, 7, DEEP, 1.8)); s.append(fci(227, 56, 3, TURQ, 1))
    return "".join(s)

def helmet_back():
    s = []
    s.append(ell(CX, 125, 90, 82, IVORY, IVSH, band=11))
    s.append(seam("M 172 132 Q 256 160 340 132", 2.6, .55))
    s.append(seam("M 190 84 Q 256 56 322 84", 2.4, .45, "7 6"))
    s.append(rr(222, 92, 68, 34, 12, IVSH, IVDK, band=4, sw=2.6))         # panel trasero
    for cx in (238, 256, 274): s.append(fci(cx, 109, 5.5, GRAY, 2))
    s.append(frr(236, 134, 40, 10, 5, DEEP)); 
    for i in range(4): s.append(el("circle", cx=244 + i * 8, cy=139, r=2.3, fill=TURQ))
    s.append(rr(214, 48, 26, 20, 7, GRAY, GRAYSH, band=4, sw=2.8))        # (camara asoma por detras)
    s.append(rr(160, 112, 14, 30, 6, IVSH, IVDK, band=3, sw=2.4)); s.append(rr(338, 112, 14, 30, 6, IVSH, IVDK, band=3, sw=2.4))
    return "".join(s)

VIS_S = "M 290 96 Q 346 98 352 132 Q 352 172 310 188 Q 262 194 250 160 Q 246 118 290 96 Z"
def helmet_side(which):
    s = []
    s.append(ell(262, 125, 84, 82, IVORY, IVSH, band=11))
    s.append(seam("M 190 108 Q 220 70 290 66", 2.4, .5, "7 6"))
    s.append(f'<path d="M 268 70 Q 316 70 340 100" fill="none" stroke="{OUT}" stroke-width="13" stroke-linecap="round"/>'
             f'<path d="M 268 70 Q 316 70 340 100" fill="none" stroke="{DEEP}" stroke-width="8" stroke-linecap="round"/>')
    for i in range(5):
        t = i / 4; px = (1-t)**2*272 + 2*(1-t)*t*316 + t*t*336; py = (1-t)**2*72 + 2*(1-t)*t*72 + t*t*98
        s.append(el("circle", cx=f"{px:.1f}", cy=f"{py:.1f}", r=2.8, fill=TURQ))
    s.append(visor(VIS_S, head=(298, 144, 46, 40)))
    s.append(f'<path d="M 280 116 Q 286 106 308 104 L 322 106 Q 300 114 294 136 Z" fill="{TURQ}" opacity=".9"/>')
    s.append(f'<path d="M 324 162 Q 336 154 338 140" fill="none" stroke="{TURQ}" stroke-width="4.5" stroke-linecap="round" opacity=".55"/>')
    if which == "right":   # camara en el lado derecho
        s.append(rr(246, 46, 40, 22, 8, GRAY, GRAYSH, band=4, sw=2.8)); s.append(frr(284, 50, 8, 14, 3, DEEP, 1.8)); s.append(fci(288, 57, 3, TURQ, 1))
    else:                  # lado izquierdo: luz + puerto de comunicaciones
        s.append(rr(250, 50, 36, 18, 8, IVSH, IVDK, band=3, sw=2.6)); s.append(frr(276, 53, 8, 12, 4, TURQ, 1.8))
        s.append(fci(222, 130, 13, GRAY, 2.4)); s.append(fci(222, 130, 6, DEEP, 1.6))
    s.append(rr(196, 112, 12, 34, 6, IVSH, IVDK, band=3, sw=2.4))         # asa trasera del casco
    return "".join(s)

# =========================================================== TORSO
HUT_F = ("M 206 226 Q 256 216 306 226 L 336 238 Q 368 250 368 284 L 358 338 Q 354 392 338 440 "
         "L 174 440 Q 158 392 154 338 L 144 284 Q 144 250 176 238 Z")
HUT_S = ("M 228 226 Q 256 218 288 226 L 320 246 Q 336 270 334 302 L 330 352 Q 328 402 324 440 "
         "L 196 440 Q 198 390 200 340 L 202 290 Q 202 250 228 226 Z")

def neck_ring(x, w):
    return mring(x, 196, w, 32, n=9) + frr(x - 8, 204, 14, 16, 5, BLUE, 2) + frr(x + w - 6, 204, 14, 16, 5, BLUE, 2)

def waist(x, w):
    s = mring(x, 434, w, 36, n=int(w / 14))
    s += fci(x + 6, 454, 9, GRAY, 2.4) + fci(x + w - 6, 454, 9, GRAY, 2.4)  # anillas de amarre
    return s

def torso_front():
    s = []
    s.append(pth(HUT_F, IVORY, IVSH, band=12))
    s.append(seam("M 206 232 Q 190 270 168 296", 2.4, .5, "7 6")); s.append(seam("M 306 232 Q 322 270 344 296", 2.4, .5, "7 6"))
    s.append(seam("M 196 330 Q 192 390 190 436", 2.4, .45, "7 6")); s.append(seam("M 316 330 Q 320 390 322 436", 2.4, .45, "7 6"))
    s.append(seam("M 222 400 Q 256 410 290 400", 2.2, .35))
    # unidad de control y pantalla (DCU)
    s.append(rr(212, 276, 88, 82, 14, IVSH, IVDK, band=4, sw=3))
    s.append(frr(222, 286, 68, 36, 8, DEEP, 2.4))
    for i, h in enumerate((10, 18, 13, 22, 16)): s.append(el("rect", x=230 + i * 12, y=316 - h, width=7, height=h, rx=2, fill=TURQ))
    s.append(f'<path d="M 226 292 H 250" stroke="{CREAM}" stroke-width="2.4" stroke-linecap="round" opacity=".8"/>')
    for i, c in enumerate((ORNG, BLUE, TURQ, ORNG)): s.append(fci(229 + i * 18, 340, 6, c, 1.8))
    # puertos de fluidos / oxigeno (azul=agua fria, rojo=agua caliente, blanco=O2)
    for px, c in ((222, BLUE), (256, IVORY), (290, RED)):
        s.append(fci(px, 392, 14, GRAY, 2.6)); s.append(fci(px, 392, 8, c, 2))
    s.append(frr(212, 416, 88, 8, 4, DEEP, 1.8))
    return "".join(s)

def plss_back():
    s = []
    s.append(rr(190, 236, 132, 238, 22, IVORY, IVSH, band=10))
    for i in range(4): s.append(frr(206, 248 + i * 9, 100, 5.5, 2.7, DEEP, 1.6))
    s.append(frr(214, 292, 84, 24, 12, DEEP, 2.4))
    for cx, c in ((234, TURQ), (256, SAND), (278, GREEN)): s.append(fci(cx, 304, 6, c, 1.6))
    s.append(rr(202, 330, 108, 104, 14, IVSH, IVDK, band=4, sw=2.6))
    s.append(seam("M 256 332 V 432", 2.4, .5))
    for (lx, ly) in ((184, 340), (184, 392)): s.append(rr(lx, ly, 20, 28, 7, METAL, METSH, band=3, sw=2.6))
    for (lx, ly) in ((308, 340), (308, 392)): s.append(rr(lx, ly, 20, 28, 7, METAL, METSH, band=3, sw=2.6))
    s.append(band(192, 442, 128, 11, RED)); s.append(band(192, 455, 128, 6, BLUE, 2))
    s.append(rr(212, 474, 88, 44, 16, METAL, METSH, band=5)); s.append(fci(256, 496, 11, DEEP, 2.4)); s.append(fci(256, 496, 5, RED, 1.4))
    s.append(f'<path d="M 224 480 H 288" stroke="{METHI}" stroke-width="3" stroke-linecap="round" opacity=".9"/>')
    return "".join(s)

def torso_back():
    return pth(HUT_F, IVORY, IVSH, band=12) + plss_back()

def torso_side(which):
    s = []
    s.append(pth(HUT_S, IVORY, IVSH, band=12))
    # PLSS (detras)
    s.append(rr(112, 240, 96, 234, 22, IVORY, IVSH, band=10))
    for i in range(4): s.append(frr(128, 252 + i * 9, 62, 5.5, 2.7, DEEP, 1.6))
    s.append(rr(124, 300, 72, 120, 14, IVSH, IVDK, band=4, sw=2.6))
    s.append(rr(112, 330, 30, 22, 8, METAL, METSH, band=3, sw=2.6)); s.append(rr(112, 384, 30, 22, 8, METAL, METSH, band=3, sw=2.6))
    s.append(f'<path d="M 156 372 H 190" stroke="{OUT}" stroke-width="12" stroke-linecap="round"/><path d="M 156 372 H 190" stroke="{METAL}" stroke-width="7" stroke-linecap="round"/>')
    s.append(frr(150, 304, 20, 14, 7, DEEP, 2)); s.append(el("circle", cx=160, cy=311, r=3.4, fill=TURQ if which == "right" else GREEN))
    s.append(band(114, 442, 94, 11, RED)); s.append(band(114, 455, 94, 6, BLUE, 2))
    s.append(rr(130, 474, 84, 44, 16, METAL, METSH, band=5)); s.append(fci(172, 496, 11, DEEP, 2.4)); s.append(fci(172, 496, 5, RED, 1.4))
    s.append(seam("M 232 232 Q 218 270 214 300", 2.4, .5, "7 6")); s.append(seam("M 214 340 Q 212 390 212 436", 2.4, .45, "7 6"))
    # DCU en perfil
    s.append(rr(326, 280, 30, 76, 10, IVSH, IVDK, band=4, sw=3)); s.append(frr(338, 290, 12, 34, 5, DEEP, 2)); s.append(el("rect", x=341, y=300, width=6, height=18, rx=2, fill=TURQ))
    for py, c in ((376, BLUE), (398, RED)): s.append(fci(334, py, 11, GRAY, 2.6)); s.append(fci(336, py, 5, c, 1.6))
    return "".join(s)

# =========================================================== ARMS (origen = centro del hombro)
def arm(view, patch=False, bend=0):
    s = []
    side = (view == "side")
    s.append(mring(-38, -20, 76, 44, n=0, r=20) if False else ell(0, 4, 24 if side else 38, 34 if side else 31, METAL, METSH, band=6))
    s.append(ell(0, 4, 14 if side else 26, 24 if side else 20, IVORY, IVSH, band=4, sw=2.6))
    s.append(rr(-31, 14, 62, 108, 24, IVORY, IVSH, band=8))
    s.append(seam("M -18 30 Q -21 60 -16 100", 2.2, .4)); s.append(seam("M 16 30 Q 20 60 15 100", 2.2, .3))
    s.append(band(-31, 70, 62, 12, RED)); s.append(band(-31, 84, 62, 5, BLUE, 2))
    if patch:
        s.append(frr(-17, 30, 34, 30, 9, BLUE, 2.4))
        s.append(f'<path d="M 0 35 L 10 54 L -10 54 Z" fill="{CREAM}" stroke="{OUT}" stroke-width="2" stroke-linejoin="round"/>')
    s.append(mring(-35, 114, 70, 28, n=7))
    f = [f'<g transform="rotate({bend} 0 128)">']
    f.append(rr(-28, 136, 56, 78, 19, IVORY, IVSH, band=7))
    f.append(seam("M -15 150 Q -17 180 -13 204", 2.2, .35)); f.append(seam("M 14 150 Q 17 180 13 204", 2.2, .3))
    f.append(mring(-32, 208, 64, 24, n=6, r=9)); f.append(frr(-36, 214, 10, 12, 4, BLUE, 2)); f.append(frr(26, 214, 10, 12, 4, BLUE, 2))
    if not side:
        for i in range(4):
            x = -26 + i * 13.4
            f.append(rr(x, 256, 12.6, 46, 6.3, IVORY, IVSH, band=3, sw=2.6)); f.append(rr(x, 288, 12.6, 14, 6.3, GRAY, GRAYSH, band=2, sw=2.6))
        f.append(rr(-29, 228, 58, 44, 17, IVORY, IVSH, band=6))
        f.append(rr(-18, 240, 36, 12, 6, GRAY, GRAYSH, band=2, sw=2.2))
        f.append(f'<g transform="rotate(-20 33 240)">' + rr(25, 238, 17, 40, 8.5, IVORY, IVSH, band=3, sw=2.6) + rr(25, 266, 17, 14, 7, GRAY, GRAYSH, band=2, sw=2.4) + '</g>')
    else:
        f.append(rr(-24, 228, 48, 66, 21, IVORY, IVSH, band=6))
        for yy in (250, 262, 274): f.append(seam(f"M 4 {yy} H 22", 2.4, .5))
        f.append(rr(-24, 280, 48, 14, 7, GRAY, GRAYSH, band=2, sw=2.4))
        f.append(rr(14, 236, 24, 34, 11, IVORY, IVSH, band=3, sw=2.6)); f.append(rr(14, 258, 24, 12, 6, GRAY, GRAYSH, band=2, sw=2.4))
    f.append('</g>')
    s.append("".join(f))
    return "".join(s)

# =========================================================== LEGS (origen = centro de la cadera)
def boot_front(heel=False):
    s = []
    s.append(rr(-42, 258, 84, 62, 22, IVORY, IVSH, band=7))
    s.append(rr(-42, 292, 84, 28, 14, GRAY, GRAYSH, band=4))
    s.append(band(-42, 270, 84, 11, RED))
    s.append(rr(-47, 310, 94, 16, 8, DEEP, "#16273A", band=3, sw=2.8))
    for i in range(7): s.append(f'<path d="M {-38+i*12.7:.1f} 314 V 322" stroke="{METSH}" stroke-width="3" stroke-linecap="round" opacity=".7"/>')
    if heel: s.append(seam("M -40 262 V 296 M 40 262 V 296", 2.2, .35))
    return "".join(s)

def leg_front(view):
    s = []
    s.append(rr(-38, 6, 76, 126, 30, IVORY, IVSH, band=8))
    s.append(seam("M -22 24 Q -26 70 -20 110", 2.2, .4)); s.append(seam("M 22 24 Q 27 70 20 110", 2.2, .3))
    s.append(band(-38, 80, 76, 12, RED)); s.append(band(-38, 94, 76, 5, BLUE, 2))
    s.append(mring(-41, 4, 82, 26, n=8, r=11))                        # cojinete cadera
    s.append(mring(-42, 122, 84, 30, n=8, r=12))                      # cojinete rodilla
    if view == "back": s.append(seam("M -22 138 Q 0 148 22 138", 2.2, .5))
    s.append(rr(-33, 148, 66, 102, 20, IVORY, IVSH, band=7))
    s.append(seam("M -18 166 Q -21 210 -16 236", 2.2, .35)); s.append(seam("M 18 166 Q 21 210 16 236", 2.2, .3))
    s.append(mring(-36, 242, 72, 24, n=7, r=9))                       # tobillo
    s.append(boot_front(heel=(view == "back")))
    return "".join(s)

def boot_side():
    s = []
    up = "M -40 262 L 38 262 L 46 278 Q 78 274 100 288 Q 114 298 114 314 L -46 316 L -46 280 Q -46 262 -40 262 Z"
    cid = uid()
    s.append(pth(up, IVORY, IVSH, band=7))
    s.append(f'<clipPath id="{cid}"><path d="{up}"/></clipPath>')
    s.append(f'<g clip-path="url(#{cid})"><path d="M -60 298 L 130 298 L 130 330 L -60 330 Z" fill="{GRAY}"/><path d="M -60 298 L 130 298 L 130 304 L -60 304 Z" fill="{GRAYSH}"/></g>')
    s.append(f'<path d="{up}" fill="none" stroke="{OUT}" stroke-width="{SW_OUT}" stroke-linejoin="round"/>')
    s.append(band(-46, 266, 90, 11, RED))
    s.append(seam("M 46 278 Q 38 296 36 312", 2.2, .45)); s.append(seam("M 72 282 Q 70 296 74 308", 2.2, .35))
    sole = "M -52 308 L 110 308 Q 124 308 124 320 Q 124 332 108 332 L -44 332 Q -54 332 -54 322 Z"
    s.append(pth(sole, DEEP, "#16273A", band=3, sw=3))
    for i in range(10): s.append(f'<path d="M {-40+i*16:.1f} 314 V 326" stroke="{METSH}" stroke-width="3" stroke-linecap="round" opacity=".7"/>')
    return "".join(s)

def leg_side(tone="front"):
    s = []
    s.append(rr(-40, 12, 80, 118, 30, IVORY, IVSH, band=8))
    s.append(seam("M -20 28 Q -24 76 -18 112", 2.2, .35))
    s.append(band(-40, 82, 80, 12, RED)); s.append(band(-40, 96, 80, 5, BLUE, 2))
    s.append(mring(-38, 4, 76, 26, n=7, r=11))
    s.append(mring(-44, 122, 88, 30, n=8, r=12))
    s.append(rr(-34, 148, 68, 102, 20, IVORY, IVSH, band=7))
    s.append(seam("M -16 166 Q -20 210 -14 238", 2.2, .35))
    s.append(mring(-38, 242, 76, 24, n=7, r=9))
    s.append(boot_side())
    return "".join(s)

# =========================================================== BRIEF + ensamblado
def brief(x, w):
    return rr(x, 462, w, 84, 34, IVORY, IVSH, band=9) + seam(f"M {x+w/2} 500 V 546", 2.4, .45)

HIP_Y = 524
def lower(view, which="right"):
    s = []
    if view in ("front", "back"):
        s.append(f'<g transform="translate({CX-44},{HIP_Y})">{leg_front(view)}</g>')
        s.append(f'<g transform="translate({CX+44},{HIP_Y})">{leg_front(view)}</g>')
        s.append(brief(172, 168)); s.append(waist(168, 176))
    else:
        global IVORY, IVSH
        _sv = (IVORY, IVSH); IVORY, IVSH = IVSH, IVDK
        _back = leg_side("back"); IVORY, IVSH = _sv
        s.append(f'<g transform="translate({CX-16},{HIP_Y})">{_back}</g>')
        s.append(f'<g transform="translate({CX+14},{HIP_Y})">{leg_side("front")}</g>')
        s.append(brief(192, 140)); s.append(waist(190, 146))
    return "".join(s)

def upper(view, which="right"):
    s = []
    if view == "front":
        s.append(torso_front()); s.append(neck_ring(204, 104)); s.append(helmet_front())
        s.append(f'<g transform="translate(148,274) rotate(7)">{arm("front", False, 3)}</g>')
        s.append(f'<g transform="translate(364,274) rotate(-7)">{arm("front", True, -3)}</g>')
    elif view == "back":
        s.append(torso_back()); s.append(neck_ring(204, 104)); s.append(helmet_back())
        s.append(f'<g transform="translate(148,274) rotate(7)">{arm("back", True, 3)}</g>')
        s.append(f'<g transform="translate(364,274) rotate(-7)">{arm("back", False, -3)}</g>')
    else:
        s.append(torso_side(which)); s.append(neck_ring(222, 76)); s.append(helmet_side(which))
        s.append(f'<g transform="translate(262,274) rotate(-6)">{arm("side", which == "left", -14)}</g>')
    return "".join(s)

DEFS = ('<defs><linearGradient id="vis" x1="0" y1="0" x2="0" y2="1">'
        '<stop offset="0" stop-color="#3A7F97"/><stop offset="0.55" stop-color="#25586D"/><stop offset="1" stop-color="#173C50"/></linearGradient></defs>')

def svg(inner, bg=None, flip=False):
    b = f'<rect width="{W}" height="{H}" fill="{bg}"/>' if bg else ""
    g = f'<g transform="translate({W},0) scale(-1,1)">{inner}</g>' if flip else inner
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">{DEFS}{b}{g}</svg>'

VIEWS = {  # nombre: (vista, lado, flip)
    "frente":    ("front", "right", False),
    "espalda":   ("back", "right", False),
    "lado_derecho":   ("side", "right", False),
    "lado_izquierdo": ("side", "left", True),
}

def render(sv, path, scale):
    cairosvg.svg2png(bytestring=sv.encode(), write_to=path, output_width=W * scale, output_height=H * scale)

if __name__ == "__main__":
    base = "/home/claude/astro/out2"
    for d in ("game", "master", "source", "capas"): os.makedirs(f"{base}/{d}", exist_ok=True)
    for name, (view, which, flip) in VIEWS.items():
        full = svg(lower(view, which) + upper(view, which), flip=flip)
        n = f"char_astronauta_{name}_neutro_v02"
        open(f"{base}/source/{n}.svg", "w").write(full)
        render(full, f"{base}/game/{n}.png", 1); render(full, f"{base}/master/{n}_2x.png", 2)
        render(svg(upper(view, which), flip=flip), f"{base}/capas/char_astronauta_{name}_superior_v02.png", 1)
        render(svg(lower(view, which), flip=flip), f"{base}/capas/char_astronauta_{name}_inferior_v02.png", 1)
        im = Image.open(f"{base}/game/{n}.png"); print(n, im.size, im.getchannel("A").getbbox())
    # hoja de contacto sobre fondo marciano
    sheet = Image.new("RGBA", (W * 4, H), (201, 106, 78, 255))
    for i, name in enumerate(VIEWS):
        sheet.alpha_composite(Image.open(f"{base}/game/char_astronauta_{name}_neutro_v02.png"), (i * W, 0))
    sheet.save(f"{base}/hoja_4_vistas.png")
