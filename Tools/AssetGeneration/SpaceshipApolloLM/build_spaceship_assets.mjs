import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const outDir = path.resolve(
  __dirname,
  '../../../My project (4)/Assets/Art/Buildings/Spaceship_ApolloLM/Sprites'
);
fs.mkdirSync(outDir, { recursive: true });

const edgeCandidates = [
  'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
  'C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe',
  'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe'
];
const browserPath = edgeCandidates.find((p) => fs.existsSync(p));
if (!browserPath) {
  throw new Error('No Chromium browser (Edge/Chrome) found for headless SVG rasterization.');
}

const scratchDir = path.join(__dirname, '.tmp_apollo_lm');
fs.mkdirSync(scratchDir, { recursive: true });

function renderSvg(filename, w, h, defs, body) {
  const htmlPath = path.join(scratchDir, `${filename}.html`);
  const outPng = path.join(outDir, filename);
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">
  <defs>${defs}</defs>
  ${body}
</svg>`;
  const html = `<!doctype html><html><body style="margin:0;padding:0;background:transparent;overflow:hidden">${svg}</body></html>`;
  fs.writeFileSync(htmlPath, html, 'utf8');

  execFileSync(
    browserPath,
    [
      '--headless=new',
      '--disable-gpu',
      '--hide-scrollbars',
      '--force-device-scale-factor=1',
      '--default-background-color=00000000',
      `--window-size=${w},${h}`,
      `--screenshot=${outPng}`,
      `file:///${htmlPath.replace(/\\/g, '/')}`
    ],
    { stdio: 'ignore' }
  );
  console.log(`Generated ${filename} (${w}x${h})`);
}

const OUTLINE = '#2a221e';
const HULL_DARK = '#333b42';
const GOLD_BRIGHT = '#f5c451';
const GOLD_MID = '#d69928';
const GOLD_DEEP = '#946110';
const ALUM_LIGHT = '#e3ded3';
const ALUM_MID = '#c4bdb0';
const ALUM_DARK = '#8c8578';
const PANEL_SLATE = '#47515c';
const RED_TRIM = '#b5442e';
const DUST = '#b55435';

const sharedDefs = `
  <linearGradient id="kaptonGold" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#ffe07d"/>
    <stop offset="35%" stop-color="${GOLD_BRIGHT}"/>
    <stop offset="70%" stop-color="${GOLD_MID}"/>
    <stop offset="100%" stop-color="${GOLD_DEEP}"/>
  </linearGradient>
  <linearGradient id="kaptonDark" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0%" stop-color="#d89b2b"/>
    <stop offset="100%" stop-color="#7c4f0c"/>
  </linearGradient>
  <linearGradient id="ascentHull" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0%" stop-color="#ece8de"/>
    <stop offset="60%" stop-color="${ALUM_MID}"/>
    <stop offset="100%" stop-color="${ALUM_DARK}"/>
  </linearGradient>
  <linearGradient id="inconelDark" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0%" stop-color="#4e5661"/>
    <stop offset="100%" stop-color="#272c33"/>
  </linearGradient>
  <linearGradient id="windowGlass" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#2997d8"/>
    <stop offset="45%" stop-color="#0f4c75"/>
    <stop offset="100%" stop-color="#082036"/>
  </linearGradient>
  <linearGradient id="legMetal" x1="0" y1="0" x2="1" y2="0">
    <stop offset="0%" stop-color="#d5dce2"/>
    <stop offset="50%" stop-color="#8b949e"/>
    <stop offset="100%" stop-color="#586069"/>
  </linearGradient>
  <linearGradient id="screenDark" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0%" stop-color="#0f2438"/>
    <stop offset="100%" stop-color="#071320"/>
  </linearGradient>
  <linearGradient id="marsOutside" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0%" stop-color="#451a0c"/>
    <stop offset="55%" stop-color="#9a3412"/>
    <stop offset="100%" stop-color="#c2410c"/>
  </linearGradient>
`;

// =========================================================================
// 1. EXTERIOR: NASA GRUMMAN APOLLO LUNAR MODULE (LM-5 "EAGLE") ON MARS
// =========================================================================
const exteriorBody = `
  <ellipse cx="640" cy="836" rx="495" ry="134" fill="#462b1c" opacity="0.35"/>
  <ellipse cx="640" cy="828" rx="350" ry="92" fill="#2c1910" opacity="0.28"/>

  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round" stroke-linecap="round">
    <line x1="392" y1="620" x2="212" y2="736" stroke-width="18"/>
    <line x1="392" y1="620" x2="212" y2="736" stroke="#c09236" stroke-width="9"/>
    <line x1="358" y1="686" x2="212" y2="736" stroke-width="14"/>
    <line x1="358" y1="686" x2="212" y2="736" stroke="#8b949e" stroke-width="6"/>
    <ellipse cx="206" cy="744" rx="58" ry="24" fill="#596068"/>
    <ellipse cx="206" cy="736" rx="44" ry="16" fill="url(#kaptonGold)"/>

    <line x1="888" y1="620" x2="1068" y2="736" stroke-width="18"/>
    <line x1="888" y1="620" x2="1068" y2="736" stroke="#c09236" stroke-width="9"/>
    <line x1="922" y1="686" x2="1068" y2="736" stroke-width="14"/>
    <line x1="922" y1="686" x2="1068" y2="736" stroke="#8b949e" stroke-width="6"/>
    <ellipse cx="1074" cy="744" rx="58" ry="24" fill="#596068"/>
    <ellipse cx="1074" cy="736" rx="44" ry="16" fill="url(#kaptonGold)"/>
  </g>

  <polygon points="566,734 714,734 746,812 534,812" fill="url(#inconelDark)" stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round"/>
  <line x1="552" y1="774" x2="728" y2="774" stroke="#64748b" stroke-width="5"/>
  <line x1="544" y1="794" x2="736" y2="794" stroke="#64748b" stroke-width="5"/>

  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round">
    <polygon points="416,528 864,528 938,578 342,578" fill="#e6c365"/>
    <polygon points="342,578 468,578 468,742 352,722" fill="url(#kaptonDark)"/>
    <polygon points="812,578 938,578 928,722 812,742" fill="url(#kaptonDark)"/>
    <rect x="468" y="578" width="344" height="164" fill="url(#kaptonGold)"/>
  </g>

  <g fill="none" stroke="#fef08a" stroke-width="3" opacity="0.68">
    <path d="M366 602 L448 648 L372 702"/>
    <path d="M486 596 L568 652 L492 718"/>
    <path d="M794 596 L712 652 L788 718"/>
    <path d="M914 602 L832 648 L908 702"/>
  </g>

  <rect x="486" y="602" width="82" height="68" rx="6" fill="#d8dee3" stroke="${OUTLINE}" stroke-width="6"/>
  <rect x="497" y="614" width="60" height="18" rx="3" fill="${RED_TRIM}"/>
  <text x="527" y="654" font-family="Arial,sans-serif" font-weight="900" font-size="14" fill="#1e293b" text-anchor="middle" stroke="none">APOLLO</text>
  <rect x="706" y="602" width="88" height="68" rx="6" fill="url(#inconelDark)" stroke="${OUTLINE}" stroke-width="6"/>
  <circle cx="732" cy="636" r="12" fill="#f59e0b" stroke="${OUTLINE}" stroke-width="4"/>
  <circle cx="766" cy="636" r="12" fill="#38bdf8" stroke="${OUTLINE}" stroke-width="4"/>

  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round" stroke-linecap="round">
    <line x1="464" y1="588" x2="294" y2="824" stroke-width="22"/>
    <line x1="464" y1="588" x2="294" y2="824" stroke="url(#kaptonGold)" stroke-width="12"/>
    <line x1="468" y1="716" x2="326" y2="782" stroke-width="16"/>
    <line x1="468" y1="716" x2="326" y2="782" stroke="#9ca4ac" stroke-width="7"/>
    <line x1="392" y1="678" x2="464" y2="716" stroke-width="14"/>
    <line x1="392" y1="678" x2="464" y2="716" stroke="#9ca4ac" stroke-width="6"/>
    <ellipse cx="288" cy="836" rx="74" ry="28" fill="#47515c"/>
    <ellipse cx="288" cy="826" rx="58" ry="20" fill="url(#kaptonGold)"/>
    <path d="M222 840 Q288 868 354 840 Q328 856 288 856 Q248 856 222 840 Z" fill="${DUST}" stroke="none" opacity="0.9"/>
    <path d="M242 842 L208 874 L182 868" fill="none" stroke="#64748b" stroke-width="5"/>

    <line x1="816" y1="588" x2="986" y2="824" stroke-width="22"/>
    <line x1="816" y1="588" x2="986" y2="824" stroke="url(#kaptonGold)" stroke-width="12"/>
    <line x1="812" y1="716" x2="954" y2="782" stroke-width="16"/>
    <line x1="812" y1="716" x2="954" y2="782" stroke="#9ca4ac" stroke-width="7"/>
    <line x1="888" y1="678" x2="816" y2="716" stroke-width="14"/>
    <line x1="888" y1="678" x2="816" y2="716" stroke="#9ca4ac" stroke-width="6"/>
    <ellipse cx="992" cy="836" rx="74" ry="28" fill="#47515c"/>
    <ellipse cx="992" cy="826" rx="58" ry="20" fill="url(#kaptonGold)"/>
    <path d="M926 840 Q992 868 1058 840 Q1032 856 992 856 Q952 856 926 840 Z" fill="${DUST}" stroke="none" opacity="0.9"/>
    <path d="M1038 842 L1072 874 L1098 868" fill="none" stroke="#64748b" stroke-width="5"/>
  </g>

  <g stroke="${OUTLINE}" stroke-width="7" stroke-linejoin="round" stroke-linecap="round">
    <polygon points="580,554 700,554 714,586 566,586" fill="#9ca4ac"/>
    <line x1="598" y1="586" x2="574" y2="858" stroke-width="15"/>
    <line x1="598" y1="586" x2="574" y2="858" stroke="#d5dce2" stroke-width="7"/>
    <line x1="682" y1="586" x2="706" y2="858" stroke-width="15"/>
    <line x1="682" y1="586" x2="706" y2="858" stroke="#d5dce2" stroke-width="7"/>
    <line x1="594" y1="616" x2="686" y2="616" stroke="#d5dce2" stroke-width="6"/>
    <line x1="591" y1="646" x2="689" y2="646" stroke="#d5dce2" stroke-width="6"/>
    <line x1="588" y1="676" x2="692" y2="676" stroke="#d5dce2" stroke-width="6"/>
    <line x1="585" y1="706" x2="695" y2="706" stroke="#d5dce2" stroke-width="6"/>
    <line x1="582" y1="736" x2="698" y2="736" stroke="#d5dce2" stroke-width="6"/>
    <line x1="579" y1="766" x2="701" y2="766" stroke="#d5dce2" stroke-width="6"/>
    <line x1="576" y1="796" x2="704" y2="796" stroke="#d5dce2" stroke-width="6"/>
    <line x1="574" y1="826" x2="706" y2="826" stroke="#d5dce2" stroke-width="6"/>
  </g>

  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round">
    <polygon points="392,276 474,218 474,528 382,486" fill="url(#inconelDark)"/>
    <polygon points="888,276 806,218 806,528 898,486" fill="url(#inconelDark)"/>
    <polygon points="512,198 768,198 836,268 836,488 776,554 504,554 444,488 444,268" fill="url(#ascentHull)"/>
    <polygon points="548,224 732,224 764,268 516,268" fill="url(#kaptonGold)"/>
    <circle cx="640" cy="212" r="26" fill="#374151"/>
    <circle cx="640" cy="212" r="14" fill="#64748b"/>

    <polygon points="478,298 588,284 568,366 468,352" fill="url(#windowGlass)"/>
    <line x1="494" y1="308" x2="546" y2="348" stroke="#7dd3fc" stroke-width="5" stroke-linecap="round"/>

    <polygon points="802,298 692,284 712,366 812,352" fill="url(#windowGlass)"/>
    <line x1="726" y1="308" x2="778" y2="348" stroke="#7dd3fc" stroke-width="5" stroke-linecap="round"/>

    <rect x="576" y="396" width="128" height="138" rx="12" fill="#cfd6de"/>
    <rect x="590" y="410" width="100" height="110" rx="8" fill="#e5e0d3"/>
    <circle cx="640" cy="464" r="22" fill="#47515c"/>
    <circle cx="640" cy="464" r="9" fill="#f59e0b"/>
    <line x1="618" y1="464" x2="662" y2="464" stroke="#d4dbe2" stroke-width="5"/>
    <line x1="640" y1="442" x2="640" y2="486" stroke="#d4dbe2" stroke-width="5"/>
    <rect x="616" y="376" width="48" height="14" rx="5" fill="#22c55e" stroke-width="4"/>
  </g>

  <g stroke="${OUTLINE}" stroke-width="6" stroke-linejoin="round">
    <rect x="342" y="328" width="54" height="58" rx="10" fill="#d4dbe2"/>
    <polygon points="342,344 312,334 312,358 342,352" fill="#47515c"/>
    <polygon points="362,328 352,298 378,298 372,328" fill="#47515c"/>
    <polygon points="362,386 352,416 378,416 372,386" fill="#47515c"/>
    <circle cx="368" cy="357" r="11" fill="#1e293b"/>

    <rect x="884" y="328" width="54" height="58" rx="10" fill="#d4dbe2"/>
    <polygon points="938,344 968,334 968,358 938,352" fill="#47515c"/>
    <polygon points="908,328 902,298 928,298 918,328" fill="#47515c"/>
    <polygon points="908,386 902,416 928,416 918,386" fill="#47515c"/>
    <circle cx="912" cy="357" r="11" fill="#1e293b"/>
  </g>

  <g stroke="${OUTLINE}" stroke-width="6" stroke-linejoin="round" stroke-linecap="round">
    <line x1="528" y1="198" x2="482" y2="136" stroke-width="12"/>
    <line x1="528" y1="198" x2="482" y2="136" stroke="#c6cdd4" stroke-width="6"/>
    <ellipse cx="468" cy="124" rx="52" ry="48" fill="#e5e0d3"/>
    <ellipse cx="468" cy="124" rx="36" ry="32" fill="#9ca4ac"/>
    <circle cx="468" cy="124" r="10" fill="#f59e0b"/>
    <line x1="748" y1="198" x2="778" y2="108" stroke-width="10"/>
    <line x1="748" y1="198" x2="778" y2="108" stroke="#d4dbe2" stroke-width="5"/>
    <line x1="746" y1="128" x2="810" y2="112" stroke="#d4dbe2" stroke-width="5"/>
    <line x1="752" y1="110" x2="804" y2="134" stroke="#d4dbe2" stroke-width="5"/>
    <circle cx="778" cy="104" r="9" fill="#ef4444"/>
  </g>
`;

// =========================================================================
// 2. INTERIOR DECK: APOLLO LM PRESSURIZED CABIN (2240x1480, PPU 125)
//    Features:
//    - Forward Flight Deck Alcove (North) with Dual Angled Triangular Windows
//      looking out at Martian surface + Circuit Breaker angled bulkheads
//    - Left (Port) Life-Support Bays: MOXIE (North-West) & UPA (South-West)
//    - Right (Starboard) Bays: Sample Lab (North-East) & Thermal Bench (South-East)
//    - Central Walkable Iso-Grid Deck + Recessed Ascent Engine Floor Hatch
//    - Aft/Forward EVA Airlock Vestibule (South)
// =========================================================================
const interiorDeckDefs = sharedDefs + `
  <pattern id="isoGrid" width="64" height="64" patternUnits="userSpaceOnUse">
    <rect width="64" height="64" fill="#d1cbc0"/>
    <path d="M 64 0 L 0 0 0 64" fill="none" stroke="#b9b2a5" stroke-width="2.5"/>
    <circle cx="32" cy="32" r="3" fill="#9e978b"/>
  </pattern>
`;

const interiorDeckBody = `
  <!-- Deep space / spacecraft shadow border -->
  <rect width="2240" height="1480" fill="#131922"/>

  <!-- Outer Gold Kapton Thermal Blanket Octagonal Hull -->
  <polygon points="390,46 1850,46 2180,340 2180,1140 1850,1434 390,1434 60,1140 60,340"
           fill="url(#kaptonGold)" stroke="${OUTLINE}" stroke-width="12" stroke-linejoin="round"/>

  <!-- Structural Aluminum-Lithium Pressure Hull Ring -->
  <polygon points="420,82 1820,82 2134,366 2134,1114 1820,1398 420,1398 106,1114 106,366"
           fill="#555e68" stroke="#232930" stroke-width="10" stroke-linejoin="round"/>

  <!-- Beta-Cloth Quilted Acoustic & Thermal Liner Bulkhead -->
  <polygon points="452,120 1788,120 2086,392 2086,1088 1788,1360 452,1360 154,1088 154,392"
           fill="#e3ddd1" stroke="#3c444d" stroke-width="8" stroke-linejoin="round"/>

  <!-- Walkable Iso-Grid Cabin Floor -->
  <polygon points="484,162 1756,162 2036,418 2036,1062 1756,1318 484,1318 204,1062 204,418"
           fill="url(#isoGrid)" stroke="#7a7468" stroke-width="6" stroke-linejoin="round"/>

  <!-- =================================================================== -->
  <!-- NORTH: APOLLO LM FORWARD FLIGHT DECK ALCOVE & TRIANGULAR WINDOWS    -->
  <!-- =================================================================== -->
  <path d="M 520 162 L 1720 162 L 1660 430 L 580 430 Z" fill="#c5beb1" stroke="#646b73" stroke-width="5"/>

  <!-- Left Angled Triangular Landing Observation Window (looking at Mars regolith) -->
  <g stroke="#1e293b" stroke-width="7" stroke-linejoin="round">
    <polygon points="478,94 756,94 708,160 518,160" fill="url(#marsOutside)"/>
    <circle cx="560" cy="126" r="14" fill="#fdba74" opacity="0.45" stroke="none"/>
    <path d="M 490 142 Q 590 120 712 146" fill="none" stroke="#fed7aa" stroke-width="3" opacity="0.6"/>
    <!-- Apollo LPD (Landing Point Designator) Reticle Etched on Left Window -->
    <line x1="615" y1="98" x2="615" y2="156" stroke="#38bdf8" stroke-width="2.5" opacity="0.8"/>
    <line x1="575" y1="127" x2="655" y2="127" stroke="#38bdf8" stroke-width="2.5" opacity="0.8"/>
  </g>

  <!-- Right Angled Triangular Landing Observation Window -->
  <g stroke="#1e293b" stroke-width="7" stroke-linejoin="round">
    <polygon points="1484,94 1762,94 1722,160 1532,160" fill="url(#marsOutside)"/>
    <path d="M 1510 144 Q 1630 118 1738 142" fill="none" stroke="#fed7aa" stroke-width="3" opacity="0.6"/>
    <line x1="1625" y1="98" x2="1625" y2="156" stroke="#38bdf8" stroke-width="2.5" opacity="0.8"/>
    <line x1="1585" y1="127" x2="1665" y2="127" stroke="#38bdf8" stroke-width="2.5" opacity="0.8"/>
  </g>

  <!-- Diagonal Bulkhead Circuit Breaker Panels (Apollo LM Panel 11 & Panel 16) -->
  <g stroke="#262e38" stroke-width="5">
    <polygon points="184,386 436,158 474,196 226,422" fill="#334155"/>
    <polygon points="2056,386 1804,158 1766,196 2014,422" fill="#334155"/>
  </g>
  <!-- Rows of white circuit breaker buttons on diagonal bulkheads -->
  <g fill="#e2e8f0">
    <circle cx="268" cy="340" r="5"/><circle cx="288" cy="322" r="5"/><circle cx="308" cy="304" r="5"/><circle cx="328" cy="286" r="5"/><circle cx="348" cy="268" r="5"/><circle cx="368" cy="250" r="5"/>
    <circle cx="284" cy="356" r="5"/><circle cx="304" cy="338" r="5"/><circle cx="324" cy="320" r="5"/><circle cx="344" cy="302" r="5"/><circle cx="364" cy="284" r="5"/><circle cx="384" cy="266" r="5"/>
    <circle cx="1972" cy="340" r="5"/><circle cx="1952" cy="322" r="5"/><circle cx="1932" cy="304" r="5"/><circle cx="1912" cy="286" r="5"/><circle cx="1892" cy="268" r="5"/><circle cx="1872" cy="250" r="5"/>
    <circle cx="1956" cy="356" r="5"/><circle cx="1936" cy="338" r="5"/><circle cx="1916" cy="320" r="5"/><circle cx="1896" cy="302" r="5"/><circle cx="1876" cy="284" r="5"/><circle cx="1856" cy="266" r="5"/>
  </g>

  <!-- Sub-floor Conduit Channels connecting the 4 Lateral Stations + Flight Deck to Central Hub -->
  <g fill="none" stroke-width="14" stroke-linecap="round" stroke-linejoin="round" opacity="0.82">
    <!-- Flight Deck (North) telemetry trunk -->
    <path d="M 1120 310 V 720" stroke="#facc15"/>
    <!-- Port Upper: MOXIE (x=-5.5 -> 432, y=+1.35 -> 571) -->
    <path d="M 432 571 H 880 L 980 680" stroke="#38bdf8"/>
    <!-- Port Lower: UPA Water (x=-5.5 -> 432, y=-1.65 -> 946) -->
    <path d="M 432 946 H 880 L 980 800" stroke="#60a5fa"/>
    <!-- Starboard Upper: Sample Lab (x=+5.5 -> 1808, y=+1.35 -> 571) -->
    <path d="M 1808 571 H 1360 L 1260 680" stroke="#4ade80"/>
    <!-- Starboard Lower: Thermal Bench (x=+5.5 -> 1808, y=-1.65 -> 946) -->
    <path d="M 1808 946 H 1360 L 1260 800" stroke="#fb923c"/>
  </g>

  <!-- Station Floor Mounting Pads -->
  <g fill="#bab3a5" stroke="#686e75" stroke-width="6">
    <!-- North Flight Command Console Pad -->
    <rect x="860" y="185" width="520" height="190" rx="18"/>
    <!-- Left & Right Crew Seat Floor Rails -->
    <rect x="850" y="425" width="170" height="150" rx="14" fill="#b0a99b"/>
    <rect x="1220" y="425" width="170" height="150" rx="14" fill="#b0a99b"/>

    <!-- Port Upper Pad: MOXIE (x=432, y=571) -->
    <rect x="276" y="435" width="312" height="272" rx="22"/>
    <!-- Port Lower Pad: UPA (x=432, y=946) -->
    <rect x="276" y="810" width="312" height="272" rx="22"/>
    <!-- Starboard Upper Pad: Sample Lab (x=1808, y=571) -->
    <rect x="1652" y="435" width="312" height="272" rx="22"/>
    <!-- Starboard Lower Pad: Thermal Bench (x=1808, y=946) -->
    <rect x="1652" y="810" width="312" height="272" rx="22"/>
  </g>

  <!-- Central Recessed Ascent Engine Inspection Cover (Flush Walkable Floor Hub) -->
  <g stroke="#4b535c" stroke-width="7">
    <circle cx="1120" cy="740" r="118" fill="#c3bcb0"/>
    <circle cx="1120" cy="740" r="84" fill="#949ba3"/>
    <circle cx="1120" cy="740" r="42" fill="#374151"/>
    <circle cx="1120" cy="740" r="14" fill="#38bdf8"/>
  </g>

  <!-- South: Forward EVA Airlock Vestibule & Regolith Decon Grate (y=-3.55 -> 1184) -->
  <g stroke="#525a62" stroke-width="7">
    <rect x="900" y="1030" width="440" height="265" rx="22" fill="#b4ada0"/>
    <line x1="924" y1="1068" x2="1316" y2="1068" stroke="#eab308" stroke-width="8" stroke-dasharray="22 14"/>
  </g>

  <!-- NASA Technical Deck Markings -->
  <g font-family="Arial,sans-serif" font-weight="800" font-size="20" fill="#475569" letter-spacing="1.8" text-anchor="middle">
    <text x="1120" y="615" font-size="22" fill="#334155">FLIGHT DECK — COMMAND &amp; PILOT STATIONS</text>
    <text x="432" y="418">PORT BAY 1: MOXIE O₂</text>
    <text x="432" y="794">PORT BAY 2: UPA WATER</text>
    <text x="1808" y="418">STBD BAY 1: SAMPLE LAB</text>
    <text x="1808" y="794">STBD BAY 2: PLSS THERMAL</text>
    <text x="1120" y="1010">EVA AIRLOCK VESTIBULE</text>
  </g>
`;

// =========================================================================
// 3. STATION 1: 2.5D NASA MOXIE + APOLLO ECS OXYGEN GENERATOR (640x640)
// =========================================================================
const moxieBody = `
  <ellipse cx="320" cy="566" rx="248" ry="52" fill="#1e293b" opacity="0.32"/>
  <!-- 2.5D Isometric Base Platform -->
  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round">
    <polygon points="86,496 554,496 584,548 56,548" fill="#64748b"/>
    <rect x="56" y="548" width="528" height="24" rx="6" fill="#334155"/>
    <!-- Upright Equipment Cabinet -->
    <rect x="88" y="76" width="464" height="432" rx="26" fill="url(#legMetal)"/>
    <rect x="110" y="98" width="420" height="390" rx="18" fill="url(#ascentHull)"/>
    <rect x="110" y="98" width="420" height="46" rx="14" fill="#0284c7"/>
  </g>
  <text x="320" y="128" font-family="Arial,sans-serif" font-weight="900" font-size="22" fill="#ffffff" text-anchor="middle" letter-spacing="1.5">MOXIE / ECS O₂</text>

  <!-- Left: Dual 3D Spherical High-Pressure Cryo O2 Tanks with Pipes -->
  <g stroke="${OUTLINE}" stroke-width="6.5">
    <circle cx="206" cy="230" r="58" fill="#22c55e"/>
    <path d="M168 206 A42 42 0 0 1 230 190" fill="none" stroke="#bbf7d0" stroke-width="6" stroke-linecap="round"/>
    <rect x="178" y="218" width="56" height="24" rx="5" fill="#f0fdf4" stroke-width="3"/>
    <text x="206" y="236" font-family="Arial,sans-serif" font-weight="900" font-size="16" fill="#14532d" text-anchor="middle" stroke="none">O₂</text>

    <circle cx="206" cy="368" r="58" fill="#06b6d4"/>
    <path d="M168 344 A42 42 0 0 1 230 328" fill="none" stroke="#cffafe" stroke-width="6" stroke-linecap="round"/>
    <rect x="178" y="356" width="56" height="24" rx="5" fill="#ecfeff" stroke-width="3"/>
    <text x="206" y="374" font-family="Arial,sans-serif" font-weight="900" font-size="16" fill="#164e63" text-anchor="middle" stroke="none">O₂</text>
  </g>

  <!-- Right: Gold Kapton SOXE 800°C Ceramic Electrolysis Reactor -->
  <g stroke="${OUTLINE}" stroke-width="6.5" stroke-linejoin="round">
    <rect x="296" y="166" width="204" height="156" rx="14" fill="url(#kaptonGold)"/>
    <rect x="314" y="184" width="168" height="36" rx="6" fill="#78350f"/>
    <text x="398" y="208" font-family="Arial,sans-serif" font-weight="900" font-size="15" fill="#fde047" text-anchor="middle" stroke="none">SOXE 800 °C</text>
    <line x1="316" y1="244" x2="480" y2="244" stroke="#78350f" stroke-width="5"/>
    <line x1="316" y1="276" x2="480" y2="276" stroke="#78350f" stroke-width="5"/>
    <line x1="316" y1="304" x2="480" y2="304" stroke="#78350f" stroke-width="5"/>

    <!-- Live Electrolysis Waveform Monitor -->
    <rect x="296" y="338" width="204" height="94" rx="10" fill="url(#screenDark)"/>
    <path d="M312 396 L346 372 L380 402 L416 358 L448 378 L484 360" fill="none" stroke="#38bdf8" stroke-width="5" stroke-linecap="round" stroke-linejoin="round"/>
    <circle cx="332" cy="414" r="6" fill="#4ade80"/>
    <circle cx="358" cy="414" r="6" fill="#38bdf8"/>
    <circle cx="384" cy="414" r="6" fill="#facc15"/>
  </g>

  <!-- Bottom PLSS Suit Umbilical Manifold -->
  <g stroke="${OUTLINE}" stroke-width="6.5" stroke-linejoin="round">
    <rect x="138" y="444" width="364" height="40" rx="10" fill="#334155"/>
    <circle cx="186" cy="464" r="12" fill="#38bdf8"/>
    <circle cx="454" cy="464" r="12" fill="#4ade80"/>
    <path d="M214 464 H426" stroke="#38bdf8" stroke-width="8" stroke-linecap="round"/>
  </g>
`;

// =========================================================================
// 4. STATION 2: 2.5D NASA ECLSS UPA WATER RECOVERY STATION (640x640)
// =========================================================================
const upaBody = `
  <ellipse cx="320" cy="566" rx="248" ry="52" fill="#1e293b" opacity="0.32"/>
  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round">
    <polygon points="86,496 554,496 584,548 56,548" fill="#64748b"/>
    <rect x="56" y="548" width="528" height="24" rx="6" fill="#334155"/>
    <rect x="88" y="76" width="464" height="432" rx="26" fill="url(#legMetal)"/>
    <rect x="110" y="98" width="420" height="390" rx="18" fill="url(#ascentHull)"/>
    <rect x="110" y="98" width="420" height="46" rx="14" fill="#2563eb"/>
  </g>
  <text x="320" y="128" font-family="Arial,sans-serif" font-weight="900" font-size="21" fill="#ffffff" text-anchor="middle" letter-spacing="1.2">ECLSS UPA — H₂O FILTER</text>

  <!-- Left Tank: Wastewater / Urine Brine Column -->
  <g stroke="${OUTLINE}" stroke-width="6.5">
    <rect x="134" y="170" width="96" height="170" rx="14" fill="#334155"/>
    <rect x="146" y="218" width="72" height="110" rx="8" fill="#eab308"/>
    <line x1="156" y1="244" x2="184" y2="244" stroke="#fef08a" stroke-width="4" stroke-linecap="round"/>
    <line x1="156" y1="274" x2="192" y2="274" stroke="#fef08a" stroke-width="4" stroke-linecap="round"/>
    <line x1="156" y1="304" x2="184" y2="304" stroke="#fef08a" stroke-width="4" stroke-linecap="round"/>
  </g>

  <!-- Center: Vapor Compression Distillation (VCD) Centrifuge Drum -->
  <g stroke="${OUTLINE}" stroke-width="6.5" stroke-linejoin="round">
    <circle cx="320" cy="254" r="68" fill="#475569"/>
    <circle cx="320" cy="254" r="46" fill="#93c5fd"/>
    <path d="M286 254 H354 M320 220 V288" stroke="#1e3a8a" stroke-width="6"/>
    <circle cx="320" cy="254" r="14" fill="#38bdf8"/>
    <rect x="268" y="338" width="44" height="90" rx="8" fill="#94a3b8"/>
    <rect x="328" y="338" width="44" height="90" rx="8" fill="#94a3b8"/>
  </g>

  <!-- Right Tank: Distilled Potable H2O Output -->
  <g stroke="${OUTLINE}" stroke-width="6.5">
    <rect x="410" y="170" width="96" height="170" rx="14" fill="#1e293b"/>
    <rect x="422" y="186" width="72" height="142" rx="8" fill="#38bdf8"/>
    <line x1="432" y1="220" x2="460" y2="220" stroke="#f0f9ff" stroke-width="4" stroke-linecap="round"/>
    <line x1="432" y1="254" x2="468" y2="254" stroke="#f0f9ff" stroke-width="4" stroke-linecap="round"/>
    <line x1="432" y1="288" x2="460" y2="288" stroke="#f0f9ff" stroke-width="4" stroke-linecap="round"/>
  </g>

  <!-- Bottom Potable Water Dispenser Manifold -->
  <g stroke="${OUTLINE}" stroke-width="6.5">
    <rect x="138" y="444" width="364" height="40" rx="10" fill="#1e293b"/>
    <path d="M186 464 H454" stroke="#38bdf8" stroke-width="9" stroke-linecap="round"/>
    <circle cx="196" cy="464" r="11" fill="#eab308"/>
    <circle cx="444" cy="464" r="11" fill="#22c55e"/>
  </g>
`;

// =========================================================================
// 5. STATION 3: 2.5D LUNAR-MARS SEALED SAMPLE GLOVEBOX LAB (640x640)
// =========================================================================
const labBody = `
  <ellipse cx="320" cy="566" rx="248" ry="52" fill="#1e293b" opacity="0.32"/>
  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round">
    <polygon points="86,496 554,496 584,548 56,548" fill="#64748b"/>
    <rect x="56" y="548" width="528" height="24" rx="6" fill="#334155"/>
    <rect x="84" y="76" width="472" height="432" rx="26" fill="url(#legMetal)"/>
    <rect x="106" y="98" width="428" height="390" rx="18" fill="url(#ascentHull)"/>
    <rect x="106" y="98" width="428" height="46" rx="14" fill="#16a34a"/>
  </g>
  <text x="320" y="128" font-family="Arial,sans-serif" font-weight="900" font-size="21" fill="#ffffff" text-anchor="middle" letter-spacing="1.2">SAMPLE ANALYZER LAB</text>

  <!-- Sealed Bio-Isolation Glovebox Chamber -->
  <g stroke="${OUTLINE}" stroke-width="7" stroke-linejoin="round">
    <rect x="132" y="160" width="376" height="194" rx="16" fill="url(#windowGlass)"/>
    <!-- Overhead UV Raman Laser Emitter -->
    <rect x="286" y="168" width="68" height="22" rx="6" fill="#94a3b8"/>
    <line x1="320" y1="190" x2="320" y2="262" stroke="#4ade80" stroke-width="5" stroke-dasharray="8 6"/>
    <!-- Martian Core Sample on Pedestal -->
    <ellipse cx="320" cy="304" rx="64" ry="18" fill="#64748b"/>
    <polygon points="284,298 306,258 348,264 360,300 314,310" fill="#c25934" stroke="#2a221e" stroke-width="5"/>
    <circle cx="326" cy="280" r="7" fill="#4ade80"/>

    <!-- Dual Glove Ports + Black Neoprene Gloves Reaching In -->
    <circle cx="204" cy="284" r="34" fill="#334155" stroke="#cbd5e1" stroke-width="6"/>
    <circle cx="204" cy="284" r="19" fill="#0f172a"/>
    <circle cx="436" cy="284" r="34" fill="#334155" stroke="#cbd5e1" stroke-width="6"/>
    <circle cx="436" cy="284" r="19" fill="#0f172a"/>
  </g>

  <!-- Lower Sample Tube Airlock Receiver (1/1) & Spectrometer Graph -->
  <g stroke="${OUTLINE}" stroke-width="6.5" stroke-linejoin="round">
    <rect x="132" y="370" width="144" height="106" rx="12" fill="#475569"/>
    <circle cx="204" cy="423" r="32" fill="#0f172a" stroke="#4ade80" stroke-width="5"/>
    <circle cx="204" cy="423" r="13" fill="#38bdf8"/>

    <rect x="294" y="370" width="214" height="106" rx="12" fill="url(#screenDark)"/>
    <rect x="316" y="420" width="18" height="40" rx="3" fill="#4ade80"/>
    <rect x="346" y="394" width="18" height="66" rx="3" fill="#38bdf8"/>
    <rect x="376" y="408" width="18" height="52" rx="3" fill="#facc15"/>
    <rect x="406" y="384" width="18" height="76" rx="3" fill="#4ade80"/>
    <rect x="436" y="426" width="18" height="34" rx="3" fill="#f97316"/>
    <circle cx="480" cy="396" r="8" fill="#4ade80"/>
  </g>
`;

// =========================================================================
// 6. STATION 4: 2.5D PLSS / LCVG SUIT THERMAL CONTROL BENCH (640x640)
// =========================================================================
const thermalBody = `
  <ellipse cx="320" cy="566" rx="248" ry="52" fill="#1e293b" opacity="0.32"/>
  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round">
    <polygon points="86,496 554,496 584,548 56,548" fill="#64748b"/>
    <rect x="56" y="548" width="528" height="24" rx="6" fill="#334155"/>
    <rect x="88" y="76" width="464" height="432" rx="26" fill="url(#legMetal)"/>
    <rect x="110" y="98" width="420" height="390" rx="18" fill="url(#ascentHull)"/>
    <rect x="110" y="98" width="420" height="46" rx="14" fill="#ea580c"/>
  </g>
  <text x="320" y="128" font-family="Arial,sans-serif" font-weight="900" font-size="20" fill="#ffffff" text-anchor="middle" letter-spacing="1.2">PLSS THERMAL BENCH</text>

  <!-- Left Bay: Suit LCVG Torso Rig -->
  <g stroke="${OUTLINE}" stroke-width="6.5" stroke-linejoin="round">
    <rect x="134" y="162" width="178" height="242" rx="16" fill="#1e293b"/>
    <path d="M184 190 H264 L282 238 L264 378 H184 L166 238 Z" fill="#cbd5e1" stroke-width="5"/>
    <path d="M192 218 Q224 234 256 218 M188 254 Q224 272 260 254 M192 290 Q224 308 256 290 M196 326 Q224 342 252 326"
          fill="none" stroke="#0284c7" stroke-width="6" stroke-linecap="round"/>
    <path d="M196 236 Q224 252 252 236 M192 272 Q224 290 256 272 M196 308 Q224 326 252 308"
          fill="none" stroke="#f97316" stroke-width="5" stroke-linecap="round"/>
  </g>

  <!-- Right Bay: Porous Plate Water Sublimator & Kapton Upgrader -->
  <g stroke="${OUTLINE}" stroke-width="6.5" stroke-linejoin="round">
    <rect x="328" y="162" width="178" height="114" rx="12" fill="url(#kaptonGold)"/>
    <rect x="346" y="180" width="142" height="78" rx="8" fill="#334155"/>
    <line x1="366" y1="194" x2="366" y2="244" stroke="#38bdf8" stroke-width="6"/>
    <line x1="390" y1="194" x2="390" y2="244" stroke="#38bdf8" stroke-width="6"/>
    <line x1="414" y1="194" x2="414" y2="244" stroke="#f97316" stroke-width="6"/>
    <line x1="438" y1="194" x2="438" y2="244" stroke="#f97316" stroke-width="6"/>
    <line x1="462" y1="194" x2="462" y2="244" stroke="#facc15" stroke-width="6"/>

    <rect x="328" y="292" width="178" height="112" rx="12" fill="url(#screenDark)"/>
    <path d="M346 364 L382 328 L416 350 L454 314 L484 330" fill="none" stroke="#f97316" stroke-width="5" stroke-linecap="round"/>
    <rect x="348" y="374" width="38" height="12" rx="4" fill="#4ade80"/>
    <rect x="396" y="374" width="38" height="12" rx="4" fill="#facc15"/>
    <rect x="444" y="374" width="38" height="12" rx="4" fill="#f97316"/>
  </g>

  <!-- Bottom Coolant Purge Manifold -->
  <g stroke="${OUTLINE}" stroke-width="6.5">
    <rect x="134" y="420" width="372" height="60" rx="12" fill="#334155"/>
    <circle cx="180" cy="450" r="16" fill="#38bdf8"/>
    <circle cx="236" cy="450" r="16" fill="#f97316"/>
    <rect x="282" y="432" width="204" height="36" rx="8" fill="#eab308"/>
    <text x="384" y="456" font-family="Arial,sans-serif" font-weight="900" font-size="16" fill="#1e293b" text-anchor="middle" stroke="none">LCVG / SUBLIMATOR</text>
  </g>
`;

// =========================================================================
// 7. FORWARD EVA AIRLOCK HATCH (APOLLO LM FORWARD HATCH, 680x560)
// =========================================================================
const hatchBody = `
  <ellipse cx="340" cy="494" rx="268" ry="48" fill="#1e293b" opacity="0.32"/>
  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round">
    <rect x="86" y="46" width="508" height="456" rx="34" fill="url(#legMetal)"/>
    <rect x="116" y="74" width="448" height="400" rx="22" fill="#d8d2c5"/>
    <rect x="146" y="88" width="388" height="38" rx="10" fill="${RED_TRIM}"/>
  </g>
  <text x="340" y="114" font-family="Arial,sans-serif" font-weight="900" font-size="20" fill="#ffffff" text-anchor="middle" letter-spacing="1.5">FORWARD EVA HATCH</text>

  <g stroke="${OUTLINE}" stroke-width="7.5" stroke-linejoin="round">
    <rect x="174" y="142" width="332" height="278" rx="24" fill="url(#ascentHull)"/>
    <rect x="198" y="164" width="284" height="234" rx="16" fill="#c7c0b2"/>
    <circle cx="340" cy="242" r="44" fill="url(#windowGlass)" stroke="#475569" stroke-width="7"/>
    <path d="M316 226 A28 28 0 0 1 360 218" fill="none" stroke="#bae6fd" stroke-width="5" stroke-linecap="round"/>

    <circle cx="340" cy="336" r="34" fill="#475569"/>
    <line x1="298" y1="336" x2="382" y2="336" stroke="#facc15" stroke-width="9" stroke-linecap="round"/>
    <line x1="340" y1="294" x2="340" y2="378" stroke="#facc15" stroke-width="9" stroke-linecap="round"/>
    <circle cx="340" cy="336" r="12" fill="#ef4444"/>

    <rect x="158" y="200" width="24" height="42" rx="5" fill="#64748b"/>
    <rect x="158" y="316" width="24" height="42" rx="5" fill="#64748b"/>
    <rect x="498" y="200" width="24" height="42" rx="5" fill="#64748b"/>
    <rect x="498" y="316" width="24" height="42" rx="5" fill="#64748b"/>
  </g>

  <g stroke="${OUTLINE}" stroke-width="6">
    <rect x="154" y="432" width="372" height="32" rx="8" fill="#334155"/>
    <line x1="184" y1="432" x2="184" y2="464" stroke="#94a3b8" stroke-width="5"/>
    <line x1="224" y1="432" x2="224" y2="464" stroke="#94a3b8" stroke-width="5"/>
    <line x1="264" y1="432" x2="264" y2="464" stroke="#94a3b8" stroke-width="5"/>
    <line x1="304" y1="432" x2="304" y2="464" stroke="#94a3b8" stroke-width="5"/>
    <line x1="344" y1="432" x2="344" y2="464" stroke="#94a3b8" stroke-width="5"/>
    <line x1="384" y1="432" x2="384" y2="464" stroke="#94a3b8" stroke-width="5"/>
    <line x1="424" y1="432" x2="424" y2="464" stroke="#94a3b8" stroke-width="5"/>
    <line x1="464" y1="432" x2="464" y2="464" stroke="#94a3b8" stroke-width="5"/>
    <line x1="500" y1="432" x2="500" y2="464" stroke="#94a3b8" stroke-width="5"/>
  </g>
`;

// =========================================================================
// 8. ACTIVE STATION OPERATION PULSE HALO (512x512)
// =========================================================================
const pulseDefs = `
  <radialGradient id="stationPulse" cx="50%" cy="50%" r="50%">
    <stop offset="0%" stop-color="#38bdf8" stop-opacity="0.0"/>
    <stop offset="55%" stop-color="#38bdf8" stop-opacity="0.32"/>
    <stop offset="82%" stop-color="#4ade80" stop-opacity="0.78"/>
    <stop offset="100%" stop-color="#4ade80" stop-opacity="0.0"/>
  </radialGradient>
`;
const pulseBody = `
  <circle cx="256" cy="256" r="236" fill="url(#stationPulse)"/>
  <circle cx="256" cy="256" r="204" fill="none" stroke="#e0f2fe" stroke-width="8" stroke-dasharray="28 18" opacity="0.88"/>
`;

// =========================================================================
// 9. NEW: APOLLO LM FLIGHT COMMAND CONSOLE (PGNCS / DSKY / FDAI 8-BALL) (960x560)
// =========================================================================
const flightConsoleBody = `
  <ellipse cx="480" cy="496" rx="410" ry="48" fill="#1e293b" opacity="0.35"/>
  <!-- 2.5D Angled Flight Deck Main Control Desk -->
  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round">
    <polygon points="88,430 872,430 908,494 52,494" fill="#475569"/>
    <rect x="52" y="494" width="856" height="24" rx="6" fill="#1e293b"/>
    <rect x="76" y="54" width="808" height="390" rx="26" fill="url(#legMetal)"/>
    <rect x="98" y="76" width="764" height="346" rx="18" fill="#2c3540"/>
    <rect x="98" y="76" width="764" height="44" rx="12" fill="#ca8a04"/>
  </g>
  <text x="480" y="105" font-family="Arial,sans-serif" font-weight="900" font-size="21" fill="#ffffff" text-anchor="middle" letter-spacing="1.8">APOLLO PGNCS — FLIGHT COMMAND CONSOLE</text>

  <!-- Left Section: Commander Attitude Indicator (FDAI 8-Ball) + Thrust TTCA Lever -->
  <g stroke="${OUTLINE}" stroke-width="6" stroke-linejoin="round">
    <rect x="124" y="138" width="216" height="196" rx="14" fill="#1e293b"/>
    <circle cx="232" cy="224" r="62" fill="#0f172a" stroke="#94a3b8" stroke-width="6"/>
    <!-- 8-Ball Horizon -->
    <path d="M172 224 A60 60 0 0 1 292 224 Z" fill="#38bdf8" stroke="none"/>
    <path d="M172 224 A60 60 0 0 0 292 224 Z" fill="#475569" stroke="none"/>
    <line x1="172" y1="224" x2="292" y2="224" stroke="#facc15" stroke-width="5"/>
    <circle cx="232" cy="224" r="62" fill="none" stroke="#94a3b8" stroke-width="6"/>
    <circle cx="232" cy="224" r="9" fill="#ef4444"/>
    <text x="232" y="316" font-family="Arial,sans-serif" font-weight="900" font-size="14" fill="#bae6fd" text-anchor="middle" stroke="none">FDAI ATTITUDE</text>
  </g>

  <!-- Center Section: Apollo Guidance Computer (AGC DSKY) Green Electroluminescent Display & Keypad -->
  <g stroke="${OUTLINE}" stroke-width="6" stroke-linejoin="round">
    <rect x="360" y="138" width="240" height="196" rx="14" fill="#374151"/>
    <!-- Green DSKY Numeric Registers -->
    <rect x="378" y="154" width="124" height="118" rx="8" fill="#052e16" stroke="#22c55e" stroke-width="4"/>
    <text x="392" y="180" font-family="Courier New,monospace" font-weight="900" font-size="16" fill="#4ade80" stroke="none">PROG 64</text>
    <text x="392" y="204" font-family="Courier New,monospace" font-weight="900" font-size="15" fill="#4ade80" stroke="none">VERB 16</text>
    <text x="392" y="226" font-family="Courier New,monospace" font-weight="900" font-size="15" fill="#4ade80" stroke="none">NOUN 36</text>
    <text x="392" y="254" font-family="Courier New,monospace" font-weight="900" font-size="16" fill="#86efac" stroke="none">+00382</text>
    <!-- Caution & Warning Lamps Matrix -->
    <rect x="514" y="154" width="68" height="118" rx="6" fill="#111827"/>
    <rect x="522" y="164" width="22" height="14" rx="2" fill="#22c55e" stroke="none"/>
    <rect x="550" y="164" width="22" height="14" rx="2" fill="#facc15" stroke="none"/>
    <rect x="522" y="186" width="22" height="14" rx="2" fill="#facc15" stroke="none"/>
    <rect x="550" y="186" width="22" height="14" rx="2" fill="#ef4444" stroke="none"/>
    <rect x="522" y="208" width="22" height="14" rx="2" fill="#38bdf8" stroke="none"/>
    <rect x="550" y="208" width="22" height="14" rx="2" fill="#22c55e" stroke="none"/>
    <!-- DSKY Pushbutton Keypad -->
    <rect x="378" y="282" width="204" height="38" rx="6" fill="#1f2937"/>
    <text x="480" y="306" font-family="Arial,sans-serif" font-weight="900" font-size="14" fill="#fde047" text-anchor="middle" stroke="none">AGC / DSKY KEYPAD</text>
  </g>

  <!-- Right Section: Space Weather Radar & Lunar-Mars Landing Telemetry -->
  <g stroke="${OUTLINE}" stroke-width="6" stroke-linejoin="round">
    <rect x="620" y="138" width="216" height="196" rx="14" fill="url(#screenDark)"/>
    <circle cx="728" cy="218" r="54" fill="#062229" stroke="#0ea5e9" stroke-width="4"/>
    <circle cx="728" cy="218" r="32" fill="none" stroke="#0284c7" stroke-width="2" stroke-dasharray="6 4"/>
    <line x1="728" y1="218" x2="768" y2="188" stroke="#4ade80" stroke-width="4" stroke-linecap="round"/>
    <circle cx="752" cy="200" r="6" fill="#facc15" stroke="none"/>
    <text x="728" y="316" font-family="Arial,sans-serif" font-weight="900" font-size="14" fill="#4ade80" text-anchor="middle" stroke="none">DONKI RADAR LINK</text>
  </g>

  <!-- Lower Control Desk: Dual Joysticks (TTCA & ACA) + Master Alarm -->
  <g stroke="${OUTLINE}" stroke-width="6" stroke-linejoin="round">
    <rect x="124" y="350" width="712" height="54" rx="10" fill="#1e293b"/>
    <circle cx="204" cy="377" r="16" fill="#f97316"/>
    <rect x="380" y="361" width="200" height="32" rx="6" fill="#dc2626"/>
    <text x="480" y="382" font-family="Arial,sans-serif" font-weight="900" font-size="14" fill="#ffffff" text-anchor="middle" stroke="none">MASTER ALARM / TELEMETRY</text>
    <circle cx="756" cy="377" r="16" fill="#38bdf8"/>
  </g>
`;

// =========================================================================
// 10. NEW: NASA CREW FLIGHT CHAIR / SEAT (COMMANDER & PILOT) (480x480)
// =========================================================================
const chairBody = `
  <ellipse cx="240" cy="408" rx="154" ry="44" fill="#1e293b" opacity="0.34"/>
  <!-- Floor Mounting Rails & Swivel Pedestal -->
  <g stroke="${OUTLINE}" stroke-width="7" stroke-linejoin="round">
    <rect x="116" y="346" width="248" height="56" rx="14" fill="#475569"/>
    <line x1="154" y1="346" x2="154" y2="402" stroke="#94a3b8" stroke-width="6"/>
    <line x1="326" y1="346" x2="326" y2="402" stroke="#94a3b8" stroke-width="6"/>
    <!-- Left & Right Rotational Armrests with Mini Hand-Controllers -->
    <rect x="74" y="196" width="54" height="136" rx="16" fill="#334155"/>
    <circle cx="101" cy="218" r="12" fill="#f97316"/>
    <rect x="352" y="196" width="54" height="136" rx="16" fill="#334155"/>
    <circle cx="379" cy="218" r="12" fill="#38bdf8"/>

    <!-- Seat Cushion & Contoured Beta-Cloth Backrest -->
    <rect x="118" y="226" width="244" height="134" rx="26" fill="#d8d2c5"/>
    <rect x="132" y="88" width="216" height="164" rx="28" fill="url(#ascentHull)"/>
    <!-- Headrest with NASA Red/Gold Trim -->
    <rect x="164" y="54" width="152" height="58" rx="18" fill="#334155"/>
    <rect x="184" y="68" width="112" height="28" rx="8" fill="${RED_TRIM}"/>

    <!-- 4-Point Blue Aerospace Restraint Harness & Center Buckle -->
    <path d="M188 112 V336 M292 112 V336" fill="none" stroke="#0284c7" stroke-width="18" stroke-linecap="round"/>
    <path d="M142 284 H338" fill="none" stroke="#0284c7" stroke-width="16" stroke-linecap="round"/>
    <circle cx="240" cy="264" r="24" fill="#eab308"/>
    <circle cx="240" cy="264" r="10" fill="#1e293b"/>
  </g>
`;

renderSvg('spaceship_exterior_apollo_lm.png', 1280, 1024, sharedDefs, exteriorBody);
renderSvg('spaceship_interior_deck_apollo_lm.png', 2240, 1480, interiorDeckDefs, interiorDeckBody);
renderSvg('station_moxie_oxygen.png', 640, 640, sharedDefs, moxieBody);
renderSvg('station_upa_water_filter.png', 640, 640, sharedDefs, upaBody);
renderSvg('station_sample_analyzer_lab.png', 640, 640, sharedDefs, labBody);
renderSvg('station_thermal_control_bench.png', 640, 640, sharedDefs, thermalBody);
renderSvg('station_airlock_hatch_apollo.png', 680, 560, sharedDefs, hatchBody);
renderSvg('station_active_pulse.png', 512, 512, pulseDefs, pulseBody);
renderSvg('station_flight_command_console.png', 960, 560, sharedDefs, flightConsoleBody);
renderSvg('prop_command_chair.png', 480, 480, sharedDefs, chairBody);
