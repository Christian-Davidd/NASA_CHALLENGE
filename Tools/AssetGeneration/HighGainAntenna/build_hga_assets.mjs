import fs from 'fs';
import path from 'path';
import { execFileSync } from 'child_process';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRoot = path.resolve(__dirname, '../../..');
const svgDir = path.join(__dirname, 'svg');
const outDir = path.join(repoRoot, 'My project (4)', 'Assets', 'Art', 'Environment', 'HighGainAntenna', 'Sprites');

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
const METAL_DARK = '#596068';
const METAL_MID = '#889098';
const METAL_LIGHT = '#c4cbd2';
const CREAM_TOP = '#f5f0e4';
const CREAM_MID = '#e4dbc8';
const CREAM_SHADOW = '#c2b69f';
const RED_TRIM = '#b6432e';
const SOLAR_DARK = '#162c4f';
const SOLAR_LIGHT = '#274f87';
const DUST = '#a66d48';

function svgWrap(w, h, defs, body) {
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">
  <defs>${defs}</defs>
  ${body}
</svg>`;
}

// 1. BASE & ARTICULATED GIMBAL MAST (1120x1120 -> 2.5 x 2.5 u at 448 PPU)
// Designed strictly from user turnaround: 4 splayed heavy feet, Martian dust, 3 left cables,
// left weather mast, front telemetry screen + buttons, right blue solar panel, red trim,
// and heavy articulated gimbal arm ending at mount socket (560, 470).
const baseDefs = `
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
  <linearGradient id="solarPanel" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#2e609e"/>
    <stop offset="50%" stop-color="#1c3c6b"/>
    <stop offset="100%" stop-color="#112545"/>
  </linearGradient>
  <linearGradient id="armMetal" x1="0" y1="0" x2="1" y2="0.6">
    <stop offset="0%" stop-color="#d8dee3"/>
    <stop offset="55%" stop-color="#9ca4ac"/>
    <stop offset="100%" stop-color="#697078"/>
  </linearGradient>
  <linearGradient id="screenGlow" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0%" stop-color="#0a2239"/>
    <stop offset="100%" stop-color="#123a5c"/>
  </linearGradient>
`;

const baseBody = `
  <!-- Ground contact shadow & Martian regolith scuff -->
  <ellipse cx="560" cy="860" rx="345" ry="128" fill="#462b1c" opacity="0.34"/>
  <ellipse cx="560" cy="852" rx="280" ry="96" fill="#3a2215" opacity="0.22"/>

  <!-- Left trailing power/data cables anchored onto soil -->
  <g fill="none" stroke-linecap="round">
    <path d="M368 785 C300 792, 232 820, 152 802" stroke="${OUTLINE}" stroke-width="18"/>
    <path d="M368 785 C300 792, 232 820, 152 802" stroke="#363a40" stroke-width="10"/>
    <path d="M376 804 C312 825, 240 854, 174 848" stroke="${OUTLINE}" stroke-width="16"/>
    <path d="M376 804 C312 825, 240 854, 174 848" stroke="#a63c2a" stroke-width="9"/>
    <path d="M388 820 C330 850, 265 884, 210 886" stroke="${OUTLINE}" stroke-width="16"/>
    <path d="M388 820 C330 850, 265 884, 210 886" stroke="#2d5582" stroke-width="9"/>
  </g>
  <!-- Cable gland socket on left side of chassis -->
  <rect x="352" y="766" width="38" height="62" rx="8" fill="#494f56" stroke="${OUTLINE}" stroke-width="7"/>

  <!-- Back-left & Back-right legs (drawn behind chassis) -->
  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round">
    <!-- Back Left Leg -->
    <polygon points="445,675 340,625 292,695 362,720" fill="url(#legMetal)"/>
    <ellipse cx="305" cy="705" rx="44" ry="20" fill="#6f7780"/>
    <ellipse cx="305" cy="698" rx="34" ry="13" fill="#b4bcc4"/>
    <!-- Back Right Leg -->
    <polygon points="675,675 780,625 828,695 758,720" fill="url(#legMetal)"/>
    <ellipse cx="815" cy="705" rx="44" ry="20" fill="#6f7780"/>
    <ellipse cx="815" cy="698" rx="34" ry="13" fill="#b4bcc4"/>
  </g>

  <!-- Left Weather / Anemometer Pole (rotor sprite attaches at (260, 560)) -->
  <g stroke="${OUTLINE}" stroke-width="7" stroke-linejoin="round" stroke-linecap="round">
    <path d="M398 730 L282 730 L260 708 L260 568" fill="none" stroke="${OUTLINE}" stroke-width="18"/>
    <path d="M398 730 L282 730 L260 708 L260 568" fill="none" stroke="#9ea6ae" stroke-width="8"/>
    <circle cx="260" cy="566" r="13" fill="#cfd6dc"/>
    <rect x="298" y="646" width="18" height="34" rx="4" fill="#d5cdb9"/>
  </g>

  <!-- Right Articulated Solar Panel Assembly -->
  <g transform="translate(765, 650) rotate(-14)" stroke="${OUTLINE}" stroke-width="7" stroke-linejoin="round">
    <!-- Support arm -->
    <path d="M-85 52 L-22 25" stroke="${OUTLINE}" stroke-width="18"/>
    <path d="M-85 52 L-22 25" stroke="#808890" stroke-width="8"/>
    <!-- Aluminum frame -->
    <polygon points="-28,-48 118,-72 152,42 4,70" fill="#cfd6de"/>
    <!-- Blue PV cells -->
    <polygon points="-16,-38 108,-60 138,34 12,58" fill="url(#solarPanel)"/>
    <!-- Crisp cell grid lines -->
    <g stroke="#7eb8e8" stroke-width="2.5" opacity="0.65">
      <line x1="24" y1="-45" x2="54" y2="50"/>
      <line x1="65" y1="-53" x2="96" y2="42"/>
      <line x1="-8" y1="-8" x2="122" y2="-28"/>
      <line x1="2" y1="24" x2="131" y2="4"/>
    </g>
  </g>

  <!-- Main Octagonal Turret Housing -->
  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round">
    <!-- Lower heavy skirt -->
    <polygon points="374,734 438,672 682,672 746,734 746,826 672,884 448,884 374,826" fill="#d4cab5"/>
    <!-- Red oxide accent band -->
    <polygon points="384,724 444,668 676,668 736,724 720,750 662,704 458,704 400,750" fill="${RED_TRIM}"/>
    <!-- Upper cream deck -->
    <polygon points="402,718 456,668 664,668 718,718 672,764 448,764" fill="url(#hullCream)"/>
    <!-- Martian dust streaks near lower hull -->
    <path d="M398 812 Q435 842 478 836" fill="none" stroke="${DUST}" stroke-width="10" stroke-linecap="round" opacity="0.45"/>
    <path d="M654 832 Q696 838 724 806" fill="none" stroke="${DUST}" stroke-width="10" stroke-linecap="round" opacity="0.45"/>
  </g>

  <!-- Front Telemetry Console (angled panel) -->
  <g stroke="${OUTLINE}" stroke-width="7" stroke-linejoin="round">
    <polygon points="452,756 668,756 686,872 434,872" fill="#e6dfd0"/>
    <!-- Screen housing -->
    <rect x="468" y="768" width="184" height="62" rx="8" fill="#525a63"/>
    <rect x="478" y="776" width="132" height="46" rx="5" fill="url(#screenGlow)" stroke="${OUTLINE}" stroke-width="4"/>
    <!-- Waveform trace on monitor -->
    <polyline points="486,804 502,804 510,788 520,814 530,792 542,806 554,796 566,804 584,788 598,802"
      fill="none" stroke="#4ade80" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"/>
    <!-- Status bar LEDs right of screen -->
    <rect x="618" y="778" width="24" height="10" rx="3" fill="#38bdf8" stroke="none"/>
    <rect x="618" y="793" width="24" height="10" rx="3" fill="#4ade80" stroke="none"/>
    <rect x="618" y="808" width="24" height="10" rx="3" fill="#fbbf24" stroke="none"/>
    <!-- Tactile control buttons row -->
    <rect x="474" y="840" width="92" height="18" rx="4" fill="#9da5ae" stroke-width="4"/>
    <circle cx="588" cy="849" r="7" fill="#38bdf8" stroke-width="4"/>
    <circle cx="614" cy="849" r="7" fill="#ef4444" stroke-width="4"/>
    <circle cx="640" cy="849" r="7" fill="#22c55e" stroke-width="4"/>
  </g>

  <!-- Front-Left & Front-Right Anchored Legs (over chassis corners for depth) -->
  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round">
    <!-- Front Left Leg -->
    <polygon points="442,782 326,842 292,892 356,906 462,824" fill="url(#legMetal)"/>
    <path d="M350 865 L396 832" stroke="#c8d0d8" stroke-width="6" stroke-linecap="round"/>
    <!-- Footpad & regolith dust -->
    <ellipse cx="304" cy="912" rx="56" ry="22" fill="#5e6670"/>
    <ellipse cx="304" cy="902" rx="42" ry="15" fill="#aeb7c0"/>
    <ellipse cx="304" cy="924" rx="54" ry="11" fill="${DUST}" stroke="none" opacity="0.58"/>

    <!-- Front Right Leg -->
    <polygon points="678,782 794,842 828,892 764,906 658,824" fill="url(#legMetal)"/>
    <path d="M770 865 L724 832" stroke="#c8d0d8" stroke-width="6" stroke-linecap="round"/>
    <!-- Footpad & regolith dust -->
    <ellipse cx="816" cy="912" rx="56" ry="22" fill="#5e6670"/>
    <ellipse cx="816" cy="902" rx="42" ry="15" fill="#aeb7c0"/>
    <ellipse cx="816" cy="924" rx="54" ry="11" fill="${DUST}" stroke="none" opacity="0.58"/>

    <!-- Front Center Anchor Brace -->
    <polygon points="530,858 590,858 602,916 518,916" fill="url(#legMetal)"/>
    <ellipse cx="560" cy="930" rx="52" ry="18" fill="#5e6670"/>
    <ellipse cx="560" cy="922" rx="38" ry="12" fill="#aeb7c0"/>
  </g>

  <!-- Central Articulated Gimbal Pedestal / Mast (rising up to mount dish at (560, 470)) -->
  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round">
    <!-- Turret rotary collar -->
    <ellipse cx="560" cy="692" rx="92" ry="30" fill="#707880"/>
    <ellipse cx="560" cy="680" rx="80" ry="24" fill="#c7cfd6"/>
    <!-- Hydraulic conduit hoses curving along pedestal -->
    <path d="M510 670 C480 618, 495 568, 526 530" fill="none" stroke="${OUTLINE}" stroke-width="15"/>
    <path d="M510 670 C480 618, 495 568, 526 530" fill="none" stroke="#363c42" stroke-width="7"/>
    <path d="M610 670 C642 618, 632 560, 598 525" fill="none" stroke="${OUTLINE}" stroke-width="14"/>
    <path d="M610 670 C642 618, 632 560, 598 525" fill="none" stroke="#a63c2a" stroke-width="6"/>

    <!-- Main angled mast beam -->
    <polygon points="518,676 602,676 618,568 584,468 536,468 502,568" fill="url(#armMetal)"/>
    <!-- Toothed elevation gear wheel on mast joint -->
    <circle cx="595" cy="570" r="36" fill="#6b737c"/>
    <circle cx="595" cy="570" r="22" fill="#cfd6dd"/>
    <circle cx="595" cy="570" r="9" fill="#4f565e"/>
    <!-- Hydraulic piston cylinder -->
    <rect x="524" y="544" width="32" height="74" rx="8" fill="#5e656d"/>
    <rect x="532" y="502" width="16" height="52" rx="4" fill="#e2e8ee"/>
    <!-- Upper dish gimbal saddle -->
    <rect x="518" y="454" width="84" height="36" rx="12" fill="#7c848d"/>
    <circle cx="560" cy="470" r="14" fill="#e6ecf2"/>
  </g>
`;

// 2. OCTAGONAL PARABOLIC HIGH-GAIN DISH (896x896 -> 2.0 x 2.0 u at 448 PPU)
// Slightly foreshortened vertically & tilted up-right so it clearly points toward the Martian sky/orbiter!
const dishDefs = `
  <radialGradient id="dishConcave" cx="44%" cy="42%" r="62%">
    <stop offset="0%" stop-color="#d2c7b1"/>
    <stop offset="45%" stop-color="#ebe3d2"/>
    <stop offset="86%" stop-color="#faf6ec"/>
    <stop offset="100%" stop-color="#ded4be"/>
  </radialGradient>
  <linearGradient id="rimBevel" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#ffffff"/>
    <stop offset="50%" stop-color="#e5dcc9"/>
    <stop offset="100%" stop-color="#b5aa95"/>
  </linearGradient>
  <linearGradient id="strutGrad" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#d2d8de"/>
    <stop offset="100%" stop-color="#6b737c"/>
  </linearGradient>
`;

// Build octagon points centered at (448, 448), tilted slightly back (ry = 0.84 * rx) so it aims up into the sky!
function octPoints(cx, cy, rx, ry) {
  const pts = [];
  for (let i = 0; i < 8; i++) {
    const a = (Math.PI / 8) + i * (Math.PI / 4);
    pts.push([cx + Math.cos(a) * rx, cy + Math.sin(a) * ry]);
  }
  return pts;
}
function ptsStr(pts) {
  return pts.map(p => `${p[0].toFixed(1)},${p[1].toFixed(1)}`).join(' ');
}

const outerBack = octPoints(432, 476, 360, 310);
const outerFront = octPoints(448, 440, 355, 304);
const innerDish = octPoints(448, 440, 312, 266);
const midRing = octPoints(448, 440, 186, 158);
const hubRing = octPoints(448, 440, 82, 70);

const feedX = 486; // Shifted slightly up-right to show parabolic dish aiming skyward toward orbiter!
const feedY = 392;

const dishBody = `
  <!-- Rear housing shell giving true 3D thickness to the tilted parabolic dish -->
  <polygon points="${ptsStr(outerBack)}" fill="#8b8273" stroke="${OUTLINE}" stroke-width="10" stroke-linejoin="round"/>
  <!-- Outer bevel rim of octagonal parabolic dish -->
  <polygon points="${ptsStr(outerFront)}" fill="url(#rimBevel)" stroke="${OUTLINE}" stroke-width="10" stroke-linejoin="round"/>
  <!-- Concave segmented dish face -->
  <polygon points="${ptsStr(innerDish)}" fill="url(#dishConcave)" stroke="${OUTLINE}" stroke-width="6" stroke-linejoin="round"/>

  <!-- 8 radial panel seams & concentric octagonal structural ribs -->
  <g stroke="#b3a790" stroke-width="4" fill="none">
    ${innerDish.map((p, i) => `<line x1="${p[0].toFixed(1)}" y1="${p[1].toFixed(1)}" x2="${hubRing[i][0].toFixed(1)}" y2="${hubRing[i][1].toFixed(1)}"/>`).join('')}
    <polygon points="${ptsStr(midRing)}"/>
    <polygon points="${ptsStr(hubRing)}" fill="#c8bda8" stroke="#857b68" stroke-width="5"/>
  </g>

  <!-- Rim rivets matching technical blueprint -->
  <g fill="#8c8374">
    ${innerDish.map(p => `<circle cx="${((p[0] + 448) * 0.5).toFixed(1)}" cy="${((p[1] + 440) * 0.5).toFixed(1)}" r="5"/>`).join('')}
  </g>

  <!-- Central transceiver emitter well -->
  <ellipse cx="448" cy="440" rx="52" ry="44" fill="#687078" stroke="${OUTLINE}" stroke-width="6"/>
  <ellipse cx="448" cy="436" rx="32" ry="27" fill="#3a4046" stroke="${OUTLINE}" stroke-width="5"/>

  <!-- 4 Quadrupod Support Struts converging to the Feed Horn pointing up-right toward satellite orbit -->
  <g stroke="${OUTLINE}" stroke-linejoin="round" stroke-linecap="round">
    ${[
      [252, 266],
      [644, 266],
      [644, 610],
      [252, 610],
    ].map(([sx, sy]) => `
      <line x1="${sx}" y1="${sy}" x2="${feedX}" y2="${feedY}" stroke="${OUTLINE}" stroke-width="24"/>
      <line x1="${sx}" y1="${sy}" x2="${feedX}" y2="${feedY}" stroke="url(#strutGrad)" stroke-width="12"/>
      <circle cx="${sx}" cy="${sy}" r="14" fill="#aeb6be" stroke="${OUTLINE}" stroke-width="6"/>
    `).join('')}
  </g>

  <!-- Feed Horn Tip / Sub-reflector pointing toward the sky -->
  <g stroke="${OUTLINE}" stroke-width="7">
    <ellipse cx="${feedX}" cy="${feedY + 6}" rx="42" ry="36" fill="#7c848d"/>
    <ellipse cx="${feedX + 6}" cy="${feedY - 4}" rx="34" ry="28" fill="#d9dfe5"/>
    <circle cx="${feedX + 10}" cy="${feedY - 8}" r="16" fill="#0ea5e9"/>
    <circle cx="${feedX + 10}" cy="${feedY - 8}" r="7" fill="#e0f2fe" stroke="none"/>
  </g>
`;

// 3. ANEMOMETER ROTOR (256x256 -> 0.57 x 0.57 u) - spins on top of left sensor mast
const anemoBody = `
  <g stroke="${OUTLINE}" stroke-width="7" stroke-linecap="round" stroke-linejoin="round">
    <line x1="128" y1="128" x2="128" y2="34" stroke="${OUTLINE}" stroke-width="14"/>
    <line x1="128" y1="128" x2="128" y2="34" stroke="#b8c0c8" stroke-width="6"/>
    <circle cx="128" cy="34" r="18" fill="#6f7882"/>
    <path d="M112 24 A18 18 0 0 1 144 24 Z" fill="#e2e8ee"/>

    <line x1="128" y1="128" x2="209" y2="175" stroke="${OUTLINE}" stroke-width="14"/>
    <line x1="128" y1="128" x2="209" y2="175" stroke="#b8c0c8" stroke-width="6"/>
    <circle cx="209" cy="175" r="18" fill="#6f7882"/>
    <path d="M214 157 A18 18 0 0 1 225 188 Z" fill="#e2e8ee"/>

    <line x1="128" y1="128" x2="47" y2="175" stroke="${OUTLINE}" stroke-width="14"/>
    <line x1="128" y1="128" x2="47" y2="175" stroke="#b8c0c8" stroke-width="6"/>
    <circle cx="47" cy="175" r="18" fill="#6f7882"/>
    <path d="M31 188 A18 18 0 0 1 42 157 Z" fill="#e2e8ee"/>

    <circle cx="128" cy="128" r="19" fill="#d5dde4"/>
    <circle cx="128" cy="128" r="8" fill="#38bdf8" stroke="none"/>
  </g>
`;

// 4. SIGNAL PULSE ARCS (384x256) - travels from dish feed horn up toward orbiter
const waveBody = `
  <g fill="none" stroke-linecap="round">
    <path d="M96 178 Q192 78 288 178" stroke="#ffffff" stroke-width="26" opacity="0.25"/>
    <path d="M96 178 Q192 78 288 178" stroke="#38bdf8" stroke-width="15" opacity="0.95"/>
    <path d="M132 198 Q192 128 252 198" stroke="#bae6fd" stroke-width="11" opacity="0.9"/>
    <path d="M164 216 Q192 178 220 216" stroke="#ffffff" stroke-width="8" opacity="0.95"/>
  </g>
`;

// 5. MARS ORBITER RELAY SATELLITE (448x320 -> 1.0 x 0.71 u) - shows real Mars orbital relay!
const orbiterDefs = `
  <linearGradient id="goldFoil" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#fde047"/>
    <stop offset="55%" stop-color="#eab308"/>
    <stop offset="100%" stop-color="#a16207"/>
  </linearGradient>
  <linearGradient id="satSolar" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#3b82f6"/>
    <stop offset="100%" stop-color="#1e3a8a"/>
  </linearGradient>
`;
const orbiterBody = `
  <g stroke="${OUTLINE}" stroke-width="7" stroke-linejoin="round">
    <!-- Solar Wings -->
    <polygon points="28,118 158,136 146,198 16,180" fill="url(#satSolar)"/>
    <polygon points="290,136 420,118 432,180 302,198" fill="url(#satSolar)"/>
    <g stroke="#93c5fd" stroke-width="2.5" opacity="0.7">
      <line x1="72" y1="124" x2="60" y2="186"/>
      <line x1="116" y1="130" x2="104" y2="192"/>
      <line x1="334" y1="130" x2="346" y2="192"/>
      <line x1="378" y1="124" x2="390" y2="186"/>
    </g>
    <!-- Connecting booms -->
    <line x1="152" y1="166" x2="188" y2="166" stroke-width="10"/>
    <line x1="260" y1="166" x2="296" y2="166" stroke-width="10"/>
    <!-- Gold thermal foil bus -->
    <polygon points="186,122 262,122 274,196 174,196" fill="url(#goldFoil)"/>
    <!-- Downward high-gain dish facing Mars surface antenna -->
    <path d="M188 198 Q224 242 260 198 Z" fill="#f5f0e4"/>
    <line x1="224" y1="220" x2="224" y2="246" stroke-width="6"/>
    <circle cx="224" cy="248" r="8" fill="#38bdf8"/>
    <!-- Top sensor dome + status LED -->
    <rect x="206" y="96" width="36" height="26" rx="6" fill="#d1d5db"/>
    <circle cx="224" cy="148" r="9" fill="#4ade80"/>
  </g>
`;

// 6. CME ELECTROMAGNETIC INTERFERENCE SPARKS (256x256)
const sparkBody = `
  <g fill="none" stroke-linecap="round" stroke-linejoin="round">
    <path d="M36 92 L88 70 L74 114 L126 98" stroke="#f472b6" stroke-width="14" opacity="0.55"/>
    <path d="M36 92 L88 70 L74 114 L126 98" stroke="#fdf4ff" stroke-width="6"/>
    <path d="M220 82 L166 108 L184 140 L130 148" stroke="#e879f9" stroke-width="14" opacity="0.6"/>
    <path d="M220 82 L166 108 L184 140 L130 148" stroke="#ffffff" stroke-width="6"/>
    <path d="M74 202 L112 162 L138 188 L178 142" stroke="#c084fc" stroke-width="14" opacity="0.6"/>
    <path d="M74 202 L112 162 L138 188 L178 142" stroke="#ffffff" stroke-width="6"/>
  </g>
`;

const assets = [
  { name: 'hga_base_mast', w: 1120, h: 1120, svg: svgWrap(1120, 1120, baseDefs, baseBody) },
  { name: 'hga_dish_head', w: 896, h: 896, svg: svgWrap(896, 896, dishDefs, dishBody) },
  { name: 'hga_anemometer_rotor', w: 256, h: 256, svg: svgWrap(256, 256, '', anemoBody) },
  { name: 'hga_signal_wave', w: 384, h: 256, svg: svgWrap(384, 256, '', waveBody) },
  { name: 'hga_orbiter_relay', w: 448, h: 320, svg: svgWrap(448, 320, orbiterDefs, orbiterBody) },
  { name: 'hga_cme_spark', w: 256, h: 256, svg: svgWrap(256, 256, '', sparkBody) },
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
