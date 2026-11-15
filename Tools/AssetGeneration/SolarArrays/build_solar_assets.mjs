import fs from 'fs';
import path from 'path';
import { execFileSync } from 'child_process';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRoot = path.resolve(__dirname, '../../..');
const svgDir = path.join(__dirname, 'svg');
const outDir = path.join(repoRoot, 'My project (4)', 'Assets', 'Art', 'Environment', 'SolarArrays', 'Sprites');

fs.mkdirSync(svgDir, { recursive: true });
fs.mkdirSync(outDir, { recursive: true });

const EDGE_CANDIDATES = [
  'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
  'C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe',
  'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
];
const browserExe = EDGE_CANDIDATES.find(p => fs.existsSync(p));
if (!browserExe) {
  throw new Error('Microsoft Edge/Chrome not found for headless SVG rasterization.');
}

const OUTLINE = '#2a221e';
const RED_TRIM = '#b6432e';
const DUST = '#a66d48';

function svgWrap(w, h, defs, body) {
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">
  <defs>${defs}</defs>
  ${body}
</svg>`;
}

// Helper to draw chamfered GaInP/GaAs/Ge triple-junction space solar cells inside a quad wing
function buildRigidPvWing(x0, y0, x1, y1, x2, y2, x3, y3, cols, rows, gradId) {
  // Bilinear interpolation across quad corners: TL(0), TR(1), BR(2), BL(3)
  function lerpPt(u, v) {
    const tx = x0 + (x1 - x0) * u;
    const ty = y0 + (y1 - y0) * u;
    const bx = x3 + (x2 - x3) * u;
    const by = y3 + (y2 - y3) * u;
    return [tx + (bx - tx) * v, ty + (by - ty) * v];
  }

  let out = '';
  // Aluminum frame outer casing
  out += `<polygon points="${x0},${y0} ${x1},${y1} ${x2},${y2} ${x3},${y3}" fill="url(#wingFrame)" stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round"/>`;

  // Dark Kapton/carbon substrate tray inset
  const pad = 0.045;
  const i0 = lerpPt(pad, pad * 1.3);
  const i1 = lerpPt(1 - pad, pad * 1.3);
  const i2 = lerpPt(1 - pad, 1 - pad * 1.3);
  const i3 = lerpPt(pad, 1 - pad * 1.3);
  out += `<polygon points="${i0[0].toFixed(1)},${i0[1].toFixed(1)} ${i1[0].toFixed(1)},${i1[1].toFixed(1)} ${i2[0].toFixed(1)},${i2[1].toFixed(1)} ${i3[0].toFixed(1)},${i3[1].toFixed(1)}" fill="#162133" stroke="${OUTLINE}" stroke-width="4" stroke-linejoin="round"/>`;

  // Individual chamfered triple-junction GaAs wafers
  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      const u0 = pad + 0.015 + (c / cols) * (1 - 2 * (pad + 0.015)) + 0.008;
      const u1 = pad + 0.015 + ((c + 1) / cols) * (1 - 2 * (pad + 0.015)) - 0.008;
      const v0 = pad * 1.3 + 0.02 + (r / rows) * (1 - 2 * (pad * 1.3 + 0.02)) + 0.012;
      const v1 = pad * 1.3 + 0.02 + ((r + 1) / rows) * (1 - 2 * (pad * 1.3 + 0.02)) - 0.012;

      const chU = (u1 - u0) * 0.14;
      const chV = (v1 - v0) * 0.14;
      const pts = [
        lerpPt(u0 + chU, v0),
        lerpPt(u1 - chU, v0),
        lerpPt(u1, v0 + chV),
        lerpPt(u1, v1 - chV),
        lerpPt(u1 - chU, v1),
        lerpPt(u0 + chU, v1),
        lerpPt(u0, v1 - chV),
        lerpPt(u0, v0 + chV),
      ];
      const poly = pts.map(p => `${p[0].toFixed(1)},${p[1].toFixed(1)}`).join(' ');
      out += `<polygon points="${poly}" fill="url(#${gradId})" stroke="#0b172a" stroke-width="2"/>`;

      // Silver/gold fine interconnect busbar lines inside each cell
      const bA = lerpPt((u0 + u1) * 0.5, v0 + 0.01);
      const bB = lerpPt((u0 + u1) * 0.5, v1 - 0.01);
      out += `<line x1="${bA[0].toFixed(1)}" y1="${bA[1].toFixed(1)}" x2="${bB[0].toFixed(1)}" y2="${bB[1].toFixed(1)}" stroke="#93c5fd" stroke-width="1.8" opacity="0.72"/>`;

      const f1A = lerpPt(u0 + 0.01, v0 + (v1 - v0) * 0.34);
      const f1B = lerpPt(u1 - 0.01, v0 + (v1 - v0) * 0.34);
      const f2A = lerpPt(u0 + 0.01, v0 + (v1 - v0) * 0.68);
      const f2B = lerpPt(u1 - 0.01, v0 + (v1 - v0) * 0.68);
      out += `<line x1="${f1A[0].toFixed(1)}" y1="${f1A[1].toFixed(1)}" x2="${f1B[0].toFixed(1)}" y2="${f1B[1].toFixed(1)}" stroke="#e0f2fe" stroke-width="1.1" opacity="0.42"/>`;
      out += `<line x1="${f2A[0].toFixed(1)}" y1="${f2A[1].toFixed(1)}" x2="${f2B[0].toFixed(1)}" y2="${f2B[1].toFixed(1)}" stroke="#e0f2fe" stroke-width="1.1" opacity="0.42"/>`;

      // Amber Kapton copper interconnect tabs at bottom seam of cell
      const tab = lerpPt((u0 + u1) * 0.5, v1);
      out += `<circle cx="${tab[0].toFixed(1)}" cy="${tab[1].toFixed(1)}" r="2.7" fill="#f59e0b" opacity="0.88"/>`;
    }
  }

  // Diagonal sunlight specular sheen across cells
  const s0 = lerpPt(0.10, 0.08);
  const s1 = lerpPt(0.42, 0.08);
  const s2 = lerpPt(0.24, 0.92);
  const s3 = lerpPt(0.06, 0.92);
  out += `<polygon points="${s0[0].toFixed(1)},${s0[1].toFixed(1)} ${s1[0].toFixed(1)},${s1[1].toFixed(1)} ${s2[0].toFixed(1)},${s2[1].toFixed(1)} ${s3[0].toFixed(1)},${s3[1].toFixed(1)}" fill="#ffffff" opacity="0.11"/>`;

  // Martian iron-oxide dust accumulation along lower frame lip
  const d0 = lerpPt(0.08, 0.93);
  const d1 = lerpPt(0.92, 0.93);
  out += `<line x1="${d0[0].toFixed(1)}" y1="${d0[1].toFixed(1)}" x2="${d1[0].toFixed(1)}" y2="${d1[1].toFixed(1)}" stroke="${DUST}" stroke-width="7" stroke-linecap="round" opacity="0.42"/>`;

  return out;
}

// Helper to draw a 10-gore InSight-style UltraFlex circular/decagonal Martian solar wing
function buildUltraFlexDecagon(cx, cy, rx, ry, tiltDeg, goreCount = 10) {
  const radTilt = (tiltDeg * Math.PI) / 180;
  function pt(angle, scale = 1) {
    const lx = Math.cos(angle) * rx * scale;
    const ly = Math.sin(angle) * ry * scale;
    const x = cx + lx * Math.cos(radTilt) - ly * Math.sin(radTilt);
    const y = cy + lx * Math.sin(radTilt) + ly * Math.cos(radTilt);
    return [x, y];
  }

  let out = '';
  // Outer protective rim shadow & structural polygon
  const outerPts = [];
  for (let i = 0; i < goreCount; i++) {
    const a = (i / goreCount) * Math.PI * 2 - Math.PI / 2;
    outerPts.push(pt(a, 1.03));
  }
  out += `<polygon points="${outerPts.map(p => `${p[0].toFixed(1)},${p[1].toFixed(1)}`).join(' ')}" fill="#c4cbd2" stroke="${OUTLINE}" stroke-width="7.5" stroke-linejoin="round"/>`;

  // Each fan-folded triangular gore (GaAs flexible blanket)
  for (let i = 0; i < goreCount; i++) {
    const a0 = (i / goreCount) * Math.PI * 2 - Math.PI / 2;
    const a1 = ((i + 1) / goreCount) * Math.PI * 2 - Math.PI / 2;
    const p0 = pt(a0, 0.97);
    const p1 = pt(a1, 0.97);
    const pIn0 = pt(a0, 0.22);
    const pIn1 = pt(a1, 0.22);

    // Subtle light variation per facet angle to give crisp 3D fan-fold depth
    const lightBoost = Math.sin((i / goreCount) * Math.PI * 2 - 0.6);
    const fill = lightBoost > 0.25 ? 'url(#pvCellBright)' : lightBoost < -0.25 ? 'url(#pvCellDark)' : 'url(#pvCellMid)';
    out += `<polygon points="${pIn0[0].toFixed(1)},${pIn0[1].toFixed(1)} ${p0[0].toFixed(1)},${p0[1].toFixed(1)} ${p1[0].toFixed(1)},${p1[1].toFixed(1)} ${pIn1[0].toFixed(1)},${pIn1[1].toFixed(1)}" fill="${fill}" stroke="#0d1b33" stroke-width="2.4" stroke-linejoin="round"/>`;

    // Concentric silver cell grid arcs inside each gore
    for (const ring of [0.44, 0.64, 0.82]) {
      const r0 = pt(a0 + 0.04, ring);
      const r1 = pt(a1 - 0.04, ring);
      out += `<line x1="${r0[0].toFixed(1)}" y1="${r0[1].toFixed(1)}" x2="${r1[0].toFixed(1)}" y2="${r1[1].toFixed(1)}" stroke="#93c5fd" stroke-width="1.6" opacity="0.62"/>`;
    }

    // Radial Kapton amber hinge tape & carbon-epoxy structural spar rib
    const sparEnd = pt(a0, 1.0);
    const sparStart = pt(a0, 0.16);
    out += `<line x1="${sparStart[0].toFixed(1)}" y1="${sparStart[1].toFixed(1)}" x2="${sparEnd[0].toFixed(1)}" y2="${sparEnd[1].toFixed(1)}" stroke="#e6a832" stroke-width="4.2" opacity="0.9"/>`;
    out += `<line x1="${sparStart[0].toFixed(1)}" y1="${sparStart[1].toFixed(1)}" x2="${sparEnd[0].toFixed(1)}" y2="${sparEnd[1].toFixed(1)}" stroke="${OUTLINE}" stroke-width="2" opacity="0.85"/>`;
  }

  // Light Martian red dust settled across lower-outer petals
  for (let i = 3; i <= 6; i++) {
    const a0 = (i / goreCount) * Math.PI * 2 - Math.PI / 2;
    const a1 = ((i + 1) / goreCount) * Math.PI * 2 - Math.PI / 2;
    const d0 = pt(a0, 0.83);
    const d1 = pt(a0, 0.96);
    const d2 = pt(a1, 0.96);
    const d3 = pt(a1, 0.83);
    out += `<polygon points="${d0[0].toFixed(1)},${d0[1].toFixed(1)} ${d1[0].toFixed(1)},${d1[1].toFixed(1)} ${d2[0].toFixed(1)},${d2[1].toFixed(1)} ${d3[0].toFixed(1)},${d3[1].toFixed(1)}" fill="${DUST}" opacity="0.28"/>`;
  }

  // Central deployment hub & aluminum tension collar
  out += `<ellipse cx="${cx}" cy="${cy}" rx="${(rx * 0.23).toFixed(1)}" ry="${(ry * 0.23).toFixed(1)}" fill="#d8dee3" stroke="${OUTLINE}" stroke-width="6"/>`;
  out += `<ellipse cx="${cx}" cy="${cy}" rx="${(rx * 0.14).toFixed(1)}" ry="${(ry * 0.14).toFixed(1)}" fill="#596068" stroke="${OUTLINE}" stroke-width="4"/>`;
  out += `<circle cx="${cx}" cy="${cy}" r="10" fill="#f59e0b" stroke="${OUTLINE}" stroke-width="3.5"/>`;

  return out;
}

// =========================================================================
// 1. NASA SURFACE HABITAT DUAL-WING SUN-TRACKING SOLAR ARRAY (1120x896)
//    Reference: NASA Mars Surface Power Array Wing + Kapton MLI MPPT Hub
// =========================================================================
const trackingDefs = `
  <linearGradient id="hullCream" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#faf6ec"/>
    <stop offset="58%" stop-color="#e4dbc8"/>
    <stop offset="100%" stop-color="#bfb39c"/>
  </linearGradient>
  <linearGradient id="legMetal" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0%" stop-color="#b9c0c7"/>
    <stop offset="50%" stop-color="#838b94"/>
    <stop offset="100%" stop-color="#585f68"/>
  </linearGradient>
  <linearGradient id="wingFrame" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#e6ebef"/>
    <stop offset="55%" stop-color="#b6bfc8"/>
    <stop offset="100%" stop-color="#7c848e"/>
  </linearGradient>
  <linearGradient id="goldMli" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#fcd34d"/>
    <stop offset="48%" stop-color="#eab308"/>
    <stop offset="100%" stop-color="#b45309"/>
  </linearGradient>
  <linearGradient id="pvCellMid" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#2c60a3"/>
    <stop offset="55%" stop-color="#1b3c6c"/>
    <stop offset="100%" stop-color="#102344"/>
  </linearGradient>
  <linearGradient id="pvCellBright" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#3773be"/>
    <stop offset="55%" stop-color="#214880"/>
    <stop offset="100%" stop-color="#132b50"/>
  </linearGradient>
  <linearGradient id="screenGlow" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0%" stop-color="#071b2e"/>
    <stop offset="100%" stop-color="#103657"/>
  </linearGradient>
`;

const trackingBody = `
  <!-- Contact shadow on Martian regolith -->
  <ellipse cx="560" cy="722" rx="340" ry="104" fill="#462b1c" opacity="0.33"/>
  <ellipse cx="560" cy="712" rx="250" ry="74" fill="#3a2215" opacity="0.22"/>

  <!-- Coiled EVA Suit / Habitat Power Umbilical Cable on left regolith -->
  <g fill="none" stroke-linecap="round" stroke-linejoin="round">
    <path d="M446 668 C372 688, 305 745, 226 732 C178 724, 178 680, 224 676" stroke="${OUTLINE}" stroke-width="20"/>
    <path d="M446 668 C372 688, 305 745, 226 732 C178 724, 178 680, 224 676" stroke="#363c44" stroke-width="11"/>
    <path d="M454 684 C382 710, 314 764, 244 756" stroke="${OUTLINE}" stroke-width="16"/>
    <path d="M454 684 C382 710, 314 764, 244 756" stroke="${RED_TRIM}" stroke-width="8"/>
  </g>
  <!-- Quick-disconnect charging coupler terminal -->
  <rect x="206" y="656" width="52" height="32" rx="8" fill="#d8dee3" stroke="${OUTLINE}" stroke-width="6"/>
  <rect x="192" y="663" width="18" height="18" rx="4" fill="#eab308" stroke="${OUTLINE}" stroke-width="5"/>
  <circle cx="234" cy="672" r="5" fill="#22c55e"/>

  <!-- Back Lander Legs -->
  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round">
    <polygon points="485,595 390,552 346,612 414,634" fill="url(#legMetal)"/>
    <ellipse cx="358" cy="622" rx="40" ry="18" fill="#6f7780"/>
    <ellipse cx="358" cy="616" rx="30" ry="11" fill="#b4bcc4"/>

    <polygon points="635,595 730,552 774,612 706,634" fill="url(#legMetal)"/>
    <ellipse cx="762" cy="622" rx="40" ry="18" fill="#6f7780"/>
    <ellipse cx="762" cy="616" rx="30" ry="11" fill="#b4bcc4"/>
  </g>

  <!-- Central MPPT Power Conditioning & Battery Chassis -->
  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round">
    <!-- Top bevel housing -->
    <polygon points="452,556 668,556 710,596 410,596" fill="url(#hullCream)"/>
    <!-- Front face -->
    <polygon points="410,596 710,596 704,672 416,672" fill="#d8ceba"/>
    <!-- Left / Right red corner brackets -->
    <polygon points="412,596 446,596 446,672 416,672" fill="${RED_TRIM}"/>
    <polygon points="674,596 708,596 704,672 674,672" fill="${RED_TRIM}"/>
  </g>

  <!-- Crinkled Gold Kapton MLI Thermal Blanket pocket on right panel -->
  <g stroke="${OUTLINE}" stroke-width="5" stroke-linejoin="round">
    <rect x="602" y="606" width="64" height="54" rx="6" fill="url(#goldMli)"/>
    <path d="M608 614 L632 630 L658 612 M610 648 L636 634 L660 652" fill="none" stroke="#fef08a" stroke-width="2.5" opacity="0.72"/>
  </g>

  <!-- Telemetry Power Monitor + Status LEDs -->
  <rect x="462" y="606" width="128" height="54" rx="7" fill="#2c333c" stroke="${OUTLINE}" stroke-width="5"/>
  <rect x="470" y="613" width="84" height="32" rx="4" fill="url(#screenGlow)" stroke="#16202a" stroke-width="2.5"/>
  <path d="M476 631 L492 631 L500 621 L510 638 L522 618 L532 629 L548 629" fill="none" stroke="#38bdf8" stroke-width="3.5" stroke-linecap="round" stroke-linejoin="round"/>
  <rect x="562" y="614" width="18" height="8" rx="3" fill="#4ade80" stroke="${OUTLINE}" stroke-width="2"/>
  <rect x="562" y="626" width="18" height="8" rx="3" fill="#facc15" stroke="${OUTLINE}" stroke-width="2"/>
  <rect x="562" y="638" width="18" height="8" rx="3" fill="#f97316" stroke="${OUTLINE}" stroke-width="2"/>
  <!-- High voltage decal bar -->
  <rect x="470" y="649" width="110" height="6" rx="2" fill="#eab308"/>

  <!-- Front Left / Right / Center Anchored Legs + Dust cuffs -->
  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round">
    <!-- Front Left -->
    <polygon points="444,648 476,668 416,742 374,724" fill="url(#legMetal)"/>
    <ellipse cx="390" cy="744" rx="52" ry="22" fill="#596068"/>
    <ellipse cx="390" cy="736" rx="40" ry="15" fill="#b9c2ca"/>
    <path d="M342 746 Q390 768 438 746 Q418 758 390 758 Q360 758 342 746 Z" fill="${DUST}" stroke="none" opacity="0.88"/>

    <!-- Front Right -->
    <polygon points="676,648 644,668 704,742 746,724" fill="url(#legMetal)"/>
    <ellipse cx="730" cy="744" rx="52" ry="22" fill="#596068"/>
    <ellipse cx="730" cy="736" rx="40" ry="15" fill="#b9c2ca"/>
    <path d="M682 746 Q730 768 778 746 Q758 758 730 758 Q700 758 682 746 Z" fill="${DUST}" stroke="none" opacity="0.88"/>

    <!-- Front Center Stabilizer Strut -->
    <polygon points="540,664 580,664 592,756 528,756" fill="url(#legMetal)"/>
    <ellipse cx="560" cy="762" rx="46" ry="18" fill="#596068"/>
    <ellipse cx="560" cy="755" rx="34" ry="12" fill="#b9c2ca"/>
  </g>

  <!-- Central Sun-Tracking Azimuth & Elevation Gimbal Mast -->
  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round">
    <ellipse cx="560" cy="568" rx="74" ry="24" fill="#889098"/>
    <rect x="524" y="458" width="72" height="106" rx="12" fill="url(#legMetal)"/>
    <!-- Kapton gold wrap around elevation actuator -->
    <rect x="530" y="492" width="60" height="44" rx="6" fill="url(#goldMli)"/>
    <!-- Dual-axis horizontal crossbar boom supporting Left & Right Solar Wings -->
    <rect x="330" y="394" width="460" height="24" rx="10" fill="#9ca4ac"/>
    <!-- Central Elevation Motor Drum -->
    <circle cx="560" cy="406" r="36" fill="#d8dee3"/>
    <circle cx="560" cy="406" r="18" fill="#4e5660"/>
    <circle cx="560" cy="406" r="7" fill="#38bdf8"/>
    <!-- Support yoke struts from mast to wings -->
    <line x1="528" y1="452" x2="380" y2="412" stroke="${OUTLINE}" stroke-width="14"/>
    <line x1="528" y1="452" x2="380" y2="412" stroke="#aeb6be" stroke-width="6"/>
    <line x1="592" y1="452" x2="740" y2="412" stroke="${OUTLINE}" stroke-width="14"/>
    <line x1="592" y1="452" x2="740" y2="412" stroke="#aeb6be" stroke-width="6"/>
  </g>

  <!-- Tilted Left & Right Rigid Triple-Junction Solar Array Wings (3/4 sun-tracking angle) -->
  ${buildRigidPvWing(78, 198, 520, 152, 492, 434, 48, 486, 4, 3, 'pvCellBright')}
  ${buildRigidPvWing(600, 152, 1042, 198, 1072, 486, 628, 434, 4, 3, 'pvCellMid')}

  <!-- Center Hinge Clamps tying wings to tracking boom & upper strut -->
  <g stroke="${OUTLINE}" stroke-width="6" stroke-linejoin="round" fill="#cfd6de">
    <rect x="498" y="348" width="36" height="22" rx="5" transform="rotate(-6 516 359)"/>
    <rect x="586" y="348" width="36" height="22" rx="5" transform="rotate(6 604 359)"/>
    <rect x="492" y="394" width="38" height="24" rx="5"/>
    <rect x="590" y="394" width="38" height="24" rx="5"/>
  </g>

  <!-- Small Sun-Sensor & Dust Pyranometer on top of center mast -->
  <g stroke="${OUTLINE}" stroke-width="6">
    <line x1="560" y1="370" x2="560" y2="296" stroke-width="12" stroke="#889098"/>
    <line x1="560" y1="370" x2="560" y2="296" stroke-width="18" stroke="${OUTLINE}"/>
    <line x1="560" y1="370" x2="560" y2="296" stroke-width="9" stroke="#cfd6de"/>
    <rect x="536" y="264" width="48" height="36" rx="8" fill="url(#hullCream)"/>
    <circle cx="560" cy="282" r="10" fill="#38bdf8"/>
  </g>
`;

// =========================================================================
// 2. NASA INSIGHT / PHOENIX DUAL ULTRAFLEX 10-GORE SOLAR ARRAY (1120x896)
//    Reference: NASA JPL InSight Lander Dual Decagonal UltraFlex™ Wings
// =========================================================================
const ultraDefs = `
  <linearGradient id="hullCream" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#faf6ec"/>
    <stop offset="58%" stop-color="#e4dbc8"/>
    <stop offset="100%" stop-color="#bfb39c"/>
  </linearGradient>
  <linearGradient id="legMetal" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0%" stop-color="#b9c0c7"/>
    <stop offset="50%" stop-color="#838b94"/>
    <stop offset="100%" stop-color="#585f68"/>
  </linearGradient>
  <linearGradient id="goldMli" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#fcd34d"/>
    <stop offset="48%" stop-color="#eab308"/>
    <stop offset="100%" stop-color="#b45309"/>
  </linearGradient>
  <linearGradient id="pvCellBright" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#3a78c9"/>
    <stop offset="55%" stop-color="#204a87"/>
    <stop offset="100%" stop-color="#132c52"/>
  </linearGradient>
  <linearGradient id="pvCellMid" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#2b60a3"/>
    <stop offset="55%" stop-color="#193b6b"/>
    <stop offset="100%" stop-color="#0f2343"/>
  </linearGradient>
  <linearGradient id="pvCellDark" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#204a84"/>
    <stop offset="60%" stop-color="#142e57"/>
    <stop offset="100%" stop-color="#0a1932"/>
  </linearGradient>
  <linearGradient id="screenGlow" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0%" stop-color="#071b2e"/>
    <stop offset="100%" stop-color="#103657"/>
  </linearGradient>
`;

const ultraBody = `
  <!-- Martian soil contact shadow -->
  <ellipse cx="560" cy="706" rx="390" ry="118" fill="#462b1c" opacity="0.33"/>
  <ellipse cx="560" cy="696" rx="280" ry="82" fill="#3a2215" opacity="0.22"/>

  <!-- Ground Power Conduit Cable leading toward Agricultural Module -->
  <g fill="none" stroke-linecap="round">
    <path d="M640 660 C720 688, 795 732, 875 718" stroke="${OUTLINE}" stroke-width="20"/>
    <path d="M640 660 C720 688, 795 732, 875 718" stroke="#363c44" stroke-width="11"/>
    <path d="M635 676 C710 708, 785 752, 860 744" stroke="${OUTLINE}" stroke-width="15"/>
    <path d="M635 676 C710 708, 785 752, 860 744" stroke="#2563eb" stroke-width="7"/>
  </g>
  <rect x="852" y="704" width="46" height="30" rx="7" fill="#d8dee3" stroke="${OUTLINE}" stroke-width="6"/>
  <circle cx="878" cy="719" r="5" fill="#22c55e"/>

  <!-- Left & Right Deployment Truss Booms behind/supporting UltraFlex discs -->
  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round">
    <polygon points="485,548 292,418 310,390 508,512" fill="url(#legMetal)"/>
    <polygon points="635,548 828,418 810,390 612,512" fill="url(#legMetal)"/>
  </g>

  <!-- Central Lander-style Hex Power Pedestal (with Radiator Fins + Kapton MLI + Monitor) -->
  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round">
    <!-- Rear legs -->
    <polygon points="498,590 420,554 388,610 450,628" fill="url(#legMetal)"/>
    <ellipse cx="398" cy="616" rx="34" ry="15" fill="#6f7780"/>
    <polygon points="622,590 700,554 732,610 670,628" fill="url(#legMetal)"/>
    <ellipse cx="722" cy="616" rx="34" ry="15" fill="#6f7780"/>

    <!-- Octagonal deck & thermal wall -->
    <polygon points="452,534 668,534 704,580 416,580" fill="url(#hullCream)"/>
    <polygon points="416,580 704,580 694,664 426,664" fill="#d8ceba"/>
    <polygon points="416,580 444,580 448,664 426,664" fill="${RED_TRIM}"/>
    <polygon points="676,580 704,580 694,664 672,664" fill="${RED_TRIM}"/>
  </g>

  <!-- Kapton gold MLI thermal wrap + Inverter monitor -->
  <rect x="456" y="592" width="54" height="58" rx="6" fill="url(#goldMli)" stroke="${OUTLINE}" stroke-width="5"/>
  <path d="M462 604 L484 622 L504 602 M462 638 L486 626 L504 642" fill="none" stroke="#fef08a" stroke-width="2.5" opacity="0.72"/>

  <rect x="520" y="592" width="144" height="58" rx="7" fill="#2c333c" stroke="${OUTLINE}" stroke-width="5"/>
  <rect x="528" y="600" width="94" height="32" rx="4" fill="url(#screenGlow)" stroke="#16202a" stroke-width="2.5"/>
  <!-- Solar output bar graph on screen -->
  <rect x="538" y="616" width="12" height="12" rx="2" fill="#38bdf8"/>
  <rect x="556" y="610" width="12" height="18" rx="2" fill="#38bdf8"/>
  <rect x="574" y="606" width="12" height="22" rx="2" fill="#4ade80"/>
  <rect x="592" y="612" width="12" height="16" rx="2" fill="#4ade80"/>
  <circle cx="642" cy="608" r="6" fill="#4ade80" stroke="${OUTLINE}" stroke-width="2"/>
  <circle cx="642" cy="624" r="6" fill="#facc15" stroke="${OUTLINE}" stroke-width="2"/>
  <circle cx="642" cy="640" r="6" fill="#f97316" stroke="${OUTLINE}" stroke-width="2"/>

  <!-- Front splayed anchor legs + Martian dust -->
  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round">
    <polygon points="452,642 484,662 434,734 394,716" fill="url(#legMetal)"/>
    <ellipse cx="410" cy="736" rx="48" ry="20" fill="#596068"/>
    <ellipse cx="410" cy="729" rx="36" ry="13" fill="#b9c2ca"/>
    <path d="M366 738 Q410 758 454 738 Q434 750 410 750 Q384 750 366 738 Z" fill="${DUST}" stroke="none" opacity="0.88"/>

    <polygon points="668,642 636,662 686,734 726,716" fill="url(#legMetal)"/>
    <ellipse cx="710" cy="736" rx="48" ry="20" fill="#596068"/>
    <ellipse cx="710" cy="729" rx="36" ry="13" fill="#b9c2ca"/>
    <path d="M666 738 Q710 758 754 738 Q734 750 710 750 Q684 750 666 738 Z" fill="${DUST}" stroke="none" opacity="0.88"/>
  </g>

  <!-- Central Environmental/Wind/Radiation Sensor Dome above bus -->
  <g stroke="${OUTLINE}" stroke-width="6">
    <rect x="536" y="482" width="48" height="52" rx="10" fill="url(#legMetal)"/>
    <rect x="524" y="454" width="72" height="34" rx="10" fill="url(#hullCream)"/>
    <circle cx="560" cy="471" r="10" fill="#38bdf8"/>
  </g>

  <!-- Dual Iconic NASA InSight 10-Gore Decagonal UltraFlex Solar Wings -->
  ${buildUltraFlexDecagon(292, 368, 224, 186, -8, 10)}
  ${buildUltraFlexDecagon(828, 368, 224, 186, 8, 10)}
`;

// =========================================================================
// 3. TACTILE PHOTOVOLTAIC CHARGING PULSE OVERLAY (512x512)
// =========================================================================
const pulseDefs = `
  <radialGradient id="chargeHalo" cx="50%" cy="50%" r="50%">
    <stop offset="0%" stop-color="#38bdf8" stop-opacity="0.65"/>
    <stop offset="55%" stop-color="#facc15" stop-opacity="0.28"/>
    <stop offset="100%" stop-color="#38bdf8" stop-opacity="0"/>
  </radialGradient>
`;
const pulseBody = `
  <circle cx="256" cy="256" r="230" fill="url(#chargeHalo)"/>
  <g fill="none" stroke-linecap="round">
    <path d="M116 206 A150 150 0 0 1 396 206" stroke="#7dd3fc" stroke-width="10" stroke-dasharray="28 20" opacity="0.85"/>
    <path d="M116 306 A150 150 0 0 0 396 306" stroke="#fde047" stroke-width="10" stroke-dasharray="28 20" opacity="0.85"/>
    <!-- Electric bolt icon in center -->
    <polygon points="274,176 218,266 258,266 238,340 302,244 260,244" fill="#fde047" stroke="#0c4a6e" stroke-width="8" stroke-linejoin="round"/>
  </g>
`;

const assets = [
  { name: 'mars_solar_array_tracking', w: 1120, h: 896, svg: svgWrap(1120, 896, trackingDefs, trackingBody) },
  { name: 'mars_solar_array_ultraflex', w: 1120, h: 896, svg: svgWrap(1120, 896, ultraDefs, ultraBody) },
  { name: 'mars_solar_charge_pulse', w: 512, h: 512, svg: svgWrap(512, 512, pulseDefs, pulseBody) },
];

const tmpDir = path.join(__dirname, '.tmp_raster');
fs.mkdirSync(tmpDir, { recursive: true });
const profileDir = path.join(tmpDir, 'edge_profile');
fs.mkdirSync(profileDir, { recursive: true });

for (const item of assets) {
  const svgPath = path.join(svgDir, `${item.name}.svg`);
  const htmlPath = path.join(tmpDir, `${item.name}.html`);
  const pngPath = path.join(outDir, `${item.name}.png`);

  fs.writeFileSync(svgPath, item.svg, 'utf8');
  fs.writeFileSync(
    htmlPath,
    `<!DOCTYPE html><html><head><style>html,body{margin:0;padding:0;width:${item.w}px;height:${item.h}px;overflow:hidden;background:transparent!important;}</style></head><body>${item.svg}</body></html>`,
    'utf8'
  );

  execFileSync(
    browserExe,
    [
      '--headless=new',
      '--disable-gpu',
      '--hide-scrollbars',
      '--force-device-scale-factor=1',
      '--default-background-color=00000000',
      `--window-size=${item.w},${item.h}`,
      `--user-data-dir=${profileDir}`,
      `--screenshot=${pngPath}`,
      htmlPath,
    ],
    { stdio: 'pipe' }
  );
  console.log(`Generated ${item.name}.png (${item.w}x${item.h})`);
}

fs.rmSync(tmpDir, { recursive: true, force: true });
