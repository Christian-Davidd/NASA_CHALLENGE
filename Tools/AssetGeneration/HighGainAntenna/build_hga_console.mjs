import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const outDir = path.resolve(__dirname, '../../../My project (4)/Assets/Art/Environment/HighGainAntenna/Resources/HighGainAntenna');
fs.mkdirSync(outDir, { recursive: true });

const edgePath = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const scratchDir = path.join(__dirname, '.tmp_console');
fs.mkdirSync(scratchDir, { recursive: true });

function renderSvg(filename, w, h, svg) {
  const htmlPath = path.join(scratchDir, `${filename}.html`);
  const outPng = path.join(outDir, filename);
  fs.writeFileSync(htmlPath, `<!doctype html><html><body style="margin:0;padding:0;background:transparent;overflow:hidden">${svg}</body></html>`, 'utf8');
  execFileSync(edgePath, [
    '--headless=new',
    '--disable-gpu',
    '--hide-scrollbars',
    '--force-device-scale-factor=1',
    '--default-background-color=00000000',
    `--window-size=${w},${h}`,
    `--screenshot=${outPng}`,
    `file:///${htmlPath.replace(/\\/g, '/')}`
  ], { stdio: 'ignore' });
  console.log(`Generated ${filename} (${w}x${h})`);
}

// 1. Main Mars Industrial Chassis Panel (920x580)
const W = 920;
const H = 580;
const chassisSvg = `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}" viewBox="0 0 ${W} ${H}">
  <defs>
    <linearGradient id="metalBg" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0%" stop-color="#4e5e6e"/>
      <stop offset="50%" stop-color="#425160"/>
      <stop offset="100%" stop-color="#354250"/>
    </linearGradient>
    <linearGradient id="rustFrame" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%" stop-color="#d2713e"/>
      <stop offset="50%" stop-color="#b0552a"/>
      <stop offset="100%" stop-color="#873c1b"/>
    </linearGradient>
    <linearGradient id="bayDark" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0%" stop-color="#0d1724"/>
      <stop offset="100%" stop-color="#070e17"/>
    </linearGradient>
    <linearGradient id="bayPlate" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0%" stop-color="#344252"/>
      <stop offset="100%" stop-color="#25303d"/>
    </linearGradient>
  </defs>

  <!-- Outer dark silhouette -->
  <rect x="4" y="14" width="912" height="560" rx="22" fill="#161f29" stroke="#090d12" stroke-width="4"/>

  <!-- Inner steel console plate -->
  <rect x="24" y="34" width="872" height="520" rx="14" fill="url(#metalBg)" stroke="#1e2936" stroke-width="4"/>

  <!-- Subtle Mars dust / wear -->
  <ellipse cx="110" cy="110" rx="48" ry="22" fill="#9a4b25" opacity="0.20"/>
  <ellipse cx="820" cy="490" rx="54" ry="26" fill="#9a4b25" opacity="0.22"/>
  <ellipse cx="460" cy="518" rx="90" ry="18" fill="#7c3a1d" opacity="0.18"/>

  <!-- Heavy rusty-orange corner armor brackets -->
  <path d="M 10 92 L 10 30 Q 10 18 22 18 L 116 18 L 100 42 L 38 42 L 38 108 Z" fill="url(#rustFrame)" stroke="#1a1f26" stroke-width="3"/>
  <path d="M 910 92 L 910 30 Q 910 18 898 18 L 804 18 L 820 42 L 882 42 L 882 108 Z" fill="url(#rustFrame)" stroke="#1a1f26" stroke-width="3"/>
  <path d="M 10 488 L 10 554 Q 10 568 22 568 L 116 568 L 100 544 L 38 544 L 38 472 Z" fill="url(#rustFrame)" stroke="#1a1f26" stroke-width="3"/>
  <path d="M 910 488 L 910 554 Q 910 568 898 568 L 804 568 L 820 544 L 882 544 L 882 472 Z" fill="url(#rustFrame)" stroke="#1a1f26" stroke-width="3"/>

  <!-- Edge rails -->
  <rect x="112" y="18" width="696" height="22" rx="4" fill="url(#rustFrame)" stroke="#1a1f26" stroke-width="2.5"/>
  <rect x="112" y="546" width="696" height="22" rx="4" fill="url(#rustFrame)" stroke="#1a1f26" stroke-width="2.5"/>
  <rect x="12" y="102" width="22" height="376" rx="4" fill="url(#rustFrame)" stroke="#1a1f26" stroke-width="2.5"/>
  <rect x="886" y="102" width="22" height="376" rx="4" fill="url(#rustFrame)" stroke="#1a1f26" stroke-width="2.5"/>

  <!-- Corner bolts -->
  <g fill="#7c8b9c" stroke="#182028" stroke-width="2">
    <circle cx="28" cy="36" r="6"/><circle cx="892" cy="36" r="6"/>
    <circle cx="28" cy="548" r="6"/><circle cx="892" cy="548" r="6"/>
    <circle cx="54" cy="54" r="5"/><circle cx="866" cy="54" r="5"/>
    <circle cx="54" cy="534" r="5"/><circle cx="866" cy="534" r="5"/>
  </g>

  <!-- Wide Top Header Nameplate -->
  <path d="M 244 4 L 676 4 L 692 42 L 228 42 Z" fill="url(#rustFrame)" stroke="#182028" stroke-width="3"/>
  <rect x="256" y="8" width="408" height="29" rx="7" fill="#16212e" stroke="#7c4527" stroke-width="2"/>

  <!-- Playful Space Stickers (kid-friendly look without cluttering controls) -->
  <g transform="translate(866, 318) rotate(12)">
    <ellipse cx="0" cy="0" rx="18" ry="14" fill="#ffffff" stroke="#18212b" stroke-width="2"/>
    <circle cx="0" cy="0" r="8" fill="#f59e0b" stroke="#78350f" stroke-width="1.4"/>
    <ellipse cx="0" cy="1" rx="14" ry="4" fill="none" stroke="#fde68a" stroke-width="2"/>
  </g>
  <g transform="translate(474, 286) rotate(18)">
    <circle cx="0" cy="0" r="15" fill="#ffffff" stroke="#18212b" stroke-width="2"/>
    <path d="M -8 -2 Q -9 -10 0 -10 Q 9 -10 8 -2 Q 7 8 0 10 Q -7 8 -8 -2 Z" fill="#84cc16" stroke="#365314" stroke-width="1.3"/>
    <ellipse cx="-3.2" cy="-2" rx="2.3" ry="3.4" fill="#0f172a" transform="rotate(-15 -3.2 -2)"/>
    <ellipse cx="3.2" cy="-2" rx="2.3" ry="3.4" fill="#0f172a" transform="rotate(15 3.2 -2)"/>
  </g>

  <!-- ======================================================================= -->
  <!-- BAY 1: TOP-LEFT (44, 58, 428, 218) - Paso 1: Vertical Fader + RF Scope  -->
  <!-- ======================================================================= -->
  <rect x="44" y="58" width="428" height="218" rx="14" fill="url(#bayPlate)" stroke="#19222d" stroke-width="3"/>
  <rect x="54" y="64" width="408" height="24" rx="6" fill="#18222e" stroke="#334155" stroke-width="1.5"/>

  <!-- DJ Fader Track slot -->
  <rect x="64" y="98" width="76" height="162" rx="10" fill="#17202b" stroke="#64748b" stroke-width="2.2"/>
  <rect x="96" y="116" width="12" height="122" rx="5" fill="#060c13" stroke="#334155" stroke-width="1.5"/>
  <!-- 100% / 0% crisp vector markings -->
  <text x="102" y="112" text-anchor="middle" font-family="Arial, sans-serif" font-weight="bold" font-size="10" fill="#38bdf8">100%</text>
  <text x="102" y="254" text-anchor="middle" font-family="Arial, sans-serif" font-weight="bold" font-size="10" fill="#4ade80">0%</text>
  <g stroke="#94a3b8" stroke-width="1.8">
    <line x1="78" y1="124" x2="90" y2="124"/><line x1="114" y1="124" x2="126" y2="124"/>
    <line x1="81" y1="150" x2="90" y2="150"/><line x1="114" y1="150" x2="123" y2="150"/>
    <line x1="78" y1="177" x2="90" y2="177"/><line x1="114" y1="177" x2="126" y2="177"/>
    <line x1="81" y1="204" x2="90" y2="204"/><line x1="114" y1="204" x2="123" y2="204"/>
    <line x1="78" y1="230" x2="90" y2="230"/><line x1="114" y1="230" x2="126" y2="230"/>
  </g>

  <!-- Oscilloscope inset screen -->
  <rect x="156" y="98" width="302" height="162" rx="12" fill="url(#bayDark)" stroke="#64748b" stroke-width="2.5"/>
  <rect x="164" y="104" width="286" height="22" rx="5" fill="#0f1c2d" stroke="#1e3a5f" stroke-width="1"/>
  <!-- Subtle scope grid lines -->
  <g stroke="#16324a" stroke-width="1" opacity="0.7">
    <line x1="168" y1="160" x2="446" y2="160"/>
    <line x1="168" y1="194" x2="446" y2="194"/>
    <line x1="168" y1="228" x2="446" y2="228"/>
    <line x1="238" y1="132" x2="238" y2="252"/>
    <line x1="307" y1="132" x2="307" y2="252"/>
    <line x1="376" y1="132" x2="376" y2="252"/>
  </g>

  <!-- ======================================================================= -->
  <!-- BAY 2: BOTTOM-LEFT (44, 300, 292, 242) - Paso 2: Socket + Plug + Diagram-->
  <!-- ======================================================================= -->
  <rect x="44" y="300" width="292" height="242" rx="14" fill="url(#bayPlate)" stroke="#19222d" stroke-width="3"/>
  <rect x="54" y="306" width="272" height="24" rx="6" fill="#18222e" stroke="#334155" stroke-width="1.5"/>

  <!-- Socket Target Plate at center (110, 386) -->
  <rect x="64" y="340" width="92" height="92" rx="12" fill="#e2e8f0" stroke="#334155" stroke-width="3"/>
  <rect x="72" y="348" width="76" height="76" rx="9" fill="#162130" stroke="#475569" stroke-width="2"/>
  <!-- Prong insertion slots cleanly matching plug prongs -->
  <rect x="92" y="356" width="12" height="24" rx="3" fill="#070c14" stroke="#94a3b8" stroke-width="1.8"/>
  <rect x="116" y="356" width="12" height="24" rx="3" fill="#070c14" stroke="#94a3b8" stroke-width="1.8"/>
  <!-- Crisp vector Earth Ground symbol below the prong holes -->
  <g stroke="#facc15" stroke-width="3.2" stroke-linecap="round">
    <line x1="110" y1="386" x2="110" y2="398"/>
    <line x1="92" y1="398" x2="128" y2="398"/>
    <line x1="98" y1="406" x2="122" y2="406"/>
    <line x1="104" y1="414" x2="116" y2="414"/>
  </g>

  <!-- Plug Parking Dock below socket so user sees where plug starts and goes -->
  <rect x="72" y="452" width="76" height="78" rx="10" fill="#16202b" stroke="#334155" stroke-width="2" stroke-dasharray="5 4"/>

  <!-- Mini Grounding Diagram Screen at (168, 340, 154, 190) -->
  <rect x="168" y="340" width="154" height="190" rx="12" fill="url(#bayDark)" stroke="#64748b" stroke-width="2.5"/>
  <rect x="175" y="346" width="140" height="22" rx="5" fill="#121e2e" stroke="#26374d" stroke-width="1"/>

  <!-- ======================================================================= -->
  <!-- BAY 3: TOP-RIGHT (488, 58, 388, 256) - Paso 3: Radar Dish + Joystick    -->
  <!-- ======================================================================= -->
  <rect x="488" y="58" width="388" height="256" rx="14" fill="url(#bayPlate)" stroke="#19222d" stroke-width="3"/>
  <rect x="498" y="64" width="368" height="24" rx="6" fill="#18222e" stroke="#334155" stroke-width="1.5"/>

  <!-- Radar dish mini-screen (502, 96, 214, 206) -->
  <rect x="502" y="96" width="214" height="206" rx="14" fill="#07191d" stroke="#64748b" stroke-width="3"/>
  <rect x="510" y="102" width="198" height="22" rx="5" fill="#0d2930" stroke="#1b4d54" stroke-width="1"/>
  <circle cx="609" cy="212" r="76" fill="#0a2428" stroke="#1e565e" stroke-width="2"/>
  <circle cx="609" cy="212" r="50" fill="none" stroke="#1b4d54" stroke-width="1.2" stroke-dasharray="4 4"/>
  <line x1="533" y1="212" x2="685" y2="212" stroke="#164248" stroke-width="1.4"/>
  <line x1="609" y1="136" x2="609" y2="288" stroke="#164248" stroke-width="1.4"/>

  <!-- ======================================================================= -->
  <!-- BAY 4: BOTTOM-CENTER (350, 300, 252, 242) - Paso 4: Safe Button         -->
  <!-- ======================================================================= -->
  <rect x="350" y="300" width="252" height="242" rx="14" fill="url(#bayPlate)" stroke="#19222d" stroke-width="3"/>
  <rect x="360" y="306" width="232" height="24" rx="6" fill="#18222e" stroke="#334155" stroke-width="1.5"/>
  <rect x="376" y="340" width="200" height="190" rx="16" fill="#1c2633" stroke="#0f172a" stroke-width="3"/>

  <!-- ======================================================================= -->
  <!-- BAY 5: BOTTOM-RIGHT (616, 330, 260, 212) - Overload Analog Meter        -->
  <!-- ======================================================================= -->
  <rect x="616" y="330" width="260" height="212" rx="14" fill="url(#bayPlate)" stroke="#19222d" stroke-width="3"/>
  <rect x="626" y="336" width="240" height="22" rx="6" fill="#18222e" stroke="#334155" stroke-width="1.5"/>
  <!-- Dial recess -->
  <path d="M 636 500 L 636 448 A 110 100 0 0 1 856 448 L 856 500 Z" fill="#0c131d" stroke="#64748b" stroke-width="3"/>
  <path d="M 654 472 A 92 84 0 0 1 706 390" fill="none" stroke="#22c55e" stroke-width="18" stroke-linecap="butt"/>
  <path d="M 710 388 A 92 84 0 0 1 782 388" fill="none" stroke="#eab308" stroke-width="18" stroke-linecap="butt"/>
  <path d="M 786 390 A 92 84 0 0 1 838 472" fill="none" stroke="#ef4444" stroke-width="18" stroke-linecap="butt"/>
  <circle cx="746" cy="486" r="14" fill="#334155" stroke="#0f172a" stroke-width="3"/>
</svg>`;

// 2. DJ Vertical Fader Knob (72x44)
const knobSvg = `<svg xmlns="http://www.w3.org/2000/svg" width="72" height="44" viewBox="0 0 72 44">
  <defs>
    <linearGradient id="kGrad" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0%" stop-color="#f1f5f9"/>
      <stop offset="50%" stop-color="#94a3b8"/>
      <stop offset="100%" stop-color="#475569"/>
    </linearGradient>
  </defs>
  <rect x="3" y="3" width="66" height="38" rx="8" fill="url(#kGrad)" stroke="#0f172a" stroke-width="3"/>
  <rect x="10" y="18" width="52" height="8" rx="3" fill="#38bdf8" stroke="#0284c7" stroke-width="1.5"/>
  <line x1="14" y1="11" x2="58" y2="11" stroke="#334155" stroke-width="2"/>
  <line x1="14" y1="33" x2="58" y2="33" stroke="#1e293b" stroke-width="2"/>
</svg>`;

// 3. Chunky Orange Grounding Plug (84x96, prongs pointing UP)
const plugSvg = `<svg xmlns="http://www.w3.org/2000/svg" width="84" height="96" viewBox="0 0 84 96">
  <defs>
    <linearGradient id="plugBody" x1="0" y1="0" x2="1" y2="0">
      <stop offset="0%" stop-color="#fdba74"/>
      <stop offset="50%" stop-color="#f97316"/>
      <stop offset="100%" stop-color="#9a3412"/>
    </linearGradient>
  </defs>
  <rect x="25" y="4" width="11" height="23" rx="2.5" fill="#f1f5f9" stroke="#0f172a" stroke-width="2.5"/>
  <rect x="48" y="4" width="11" height="23" rx="2.5" fill="#f1f5f9" stroke="#0f172a" stroke-width="2.5"/>
  <rect x="13" y="24" width="58" height="45" rx="11" fill="url(#plugBody)" stroke="#0f172a" stroke-width="3"/>
  <rect x="21" y="34" width="42" height="22" rx="5" fill="#c2410c" stroke="#7c2d12" stroke-width="1.8"/>
  <!-- Grip ribs on plug so kids want to grab it -->
  <g stroke="#fed7aa" stroke-width="2" stroke-linecap="round">
    <line x1="29" y1="40" x2="55" y2="40"/>
    <line x1="29" y1="45" x2="55" y2="45"/>
    <line x1="29" y1="50" x2="55" y2="50"/>
  </g>
  <path d="M 28 69 L 56 69 L 50 91 L 34 91 Z" fill="#475569" stroke="#0f172a" stroke-width="2.5"/>
</svg>`;

// 4. Translucent Safety Hazard Cover with Clear Viewing Glass Center (176x120)
const coverSvg = `<svg xmlns="http://www.w3.org/2000/svg" width="176" height="120" viewBox="0 0 176 120">
  <defs>
    <pattern id="haz" width="22" height="22" patternTransform="rotate(45 0 0)" patternUnits="userSpaceOnUse">
      <rect width="11" height="22" fill="#facc15"/>
      <rect x="11" width="11" height="22" fill="#18181b"/>
    </pattern>
  </defs>
  <!-- Top steel hinge -->
  <rect x="8" y="2" width="160" height="14" rx="5" fill="#64748b" stroke="#0f172a" stroke-width="2.5"/>
  <!-- Hazard striped outer frame with real transparent viewing window (evenodd cutout) -->
  <path fill-rule="evenodd" d="M 20 14 L 156 14 Q 168 14 168 26 L 168 102 Q 168 114 156 114 L 20 114 Q 8 114 8 102 L 8 26 Q 8 14 20 14 Z M 28 28 L 148 28 Q 152 28 152 32 L 152 96 Q 152 100 148 100 L 28 100 Q 24 100 24 96 L 24 32 Q 24 28 28 28 Z" fill="url(#haz)" stroke="#0f172a" stroke-width="3"/>
  <!-- Clear tinted plexiglass window inside so the red button stays clearly visible underneath -->
  <rect x="24" y="28" width="128" height="72" rx="6" fill="#fef08a" fill-opacity="0.24" stroke="#fde047" stroke-width="2"/>
  <!-- Crisp lift-up arrow icon painted on glass -->
  <polygon points="88,44 74,60 83,60 83,78 93,78 93,60 102,60" fill="#ffffff" fill-opacity="0.92" stroke="#0f172a" stroke-width="2"/>
</svg>`;

// 5. Big Glossy Red Emergency Button (144x144)
const redBtnSvg = `<svg xmlns="http://www.w3.org/2000/svg" width="144" height="144" viewBox="0 0 144 144">
  <defs>
    <radialGradient id="redCap" cx="42%" cy="35%" r="60%">
      <stop offset="0%" stop-color="#ff8585"/>
      <stop offset="55%" stop-color="#ef4444"/>
      <stop offset="100%" stop-color="#991b1b"/>
    </radialGradient>
  </defs>
  <circle cx="72" cy="72" r="66" fill="#64748b" stroke="#0f172a" stroke-width="4"/>
  <circle cx="72" cy="72" r="56" fill="#334155" stroke="#0f172a" stroke-width="3"/>
  <circle cx="72" cy="72" r="48" fill="url(#redCap)" stroke="#450a0a" stroke-width="3"/>
  <ellipse cx="62" cy="48" rx="22" ry="11" fill="#ffffff" opacity="0.30" transform="rotate(-18 62 48)"/>
</svg>`;

// 6. Overload Gauge Needle pointing RIGHT from center (120x32, pivot at x=16,y=16)
const needleSvg = `<svg xmlns="http://www.w3.org/2000/svg" width="120" height="32" viewBox="0 0 120 32">
  <polygon points="14,10 112,16 14,22" fill="#f97316" stroke="#1e293b" stroke-width="2"/>
  <circle cx="16" cy="16" r="10" fill="#475569" stroke="#0f172a" stroke-width="2.5"/>
  <circle cx="16" cy="16" r="4" fill="#cbd5e1"/>
</svg>`;

// 7. Radar Dish Silhouette (128x96, pivot near bottom center x=64,y=84)
const radarDishSvg = `<svg xmlns="http://www.w3.org/2000/svg" width="128" height="96" viewBox="0 0 128 96">
  <defs>
    <linearGradient id="dishBody" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%" stop-color="#f8fafc"/>
      <stop offset="100%" stop-color="#94a3b8"/>
    </linearGradient>
  </defs>
  <!-- Dish back cradle -->
  <rect x="54" y="54" width="20" height="24" rx="4" fill="#475569" stroke="#0f172a" stroke-width="2.5"/>
  <!-- Parabolic cup bowl facing UP (0 deg) -->
  <path d="M 14 34 Q 64 78 114 34 L 106 26 Q 64 62 22 26 Z" fill="url(#dishBody)" stroke="#0f172a" stroke-width="2.5"/>
  <!-- Feedhorn struts & emitter tip -->
  <line x1="36" y1="42" x2="64" y2="12" stroke="#38bdf8" stroke-width="2.5"/>
  <line x1="92" y1="42" x2="64" y2="12" stroke="#38bdf8" stroke-width="2.5"/>
  <line x1="64" y1="48" x2="64" y2="14" stroke="#64748b" stroke-width="3"/>
  <circle cx="64" cy="11" r="6.5" fill="#38bdf8" stroke="#0f172a" stroke-width="2"/>
</svg>`;

renderSvg('hga_panel_chassis.png', W, H, chassisSvg);
renderSvg('hga_fader_knob.png', 72, 44, knobSvg);
renderSvg('hga_ground_plug.png', 84, 96, plugSvg);
renderSvg('hga_hazard_cover.png', 176, 120, coverSvg);
renderSvg('hga_red_button.png', 144, 144, redBtnSvg);
renderSvg('hga_gauge_needle.png', 120, 32, needleSvg);
renderSvg('hga_radar_dish.png', 128, 96, radarDishSvg);

fs.rmSync(scratchDir, { recursive: true, force: true });
