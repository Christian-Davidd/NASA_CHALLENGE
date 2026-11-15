import fs from 'fs';
import path from 'path';
import { execFileSync } from 'child_process';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRoot = path.resolve(__dirname, '../../..');
const svgDir = path.join(__dirname, 'svg');
const outDir = path.join(repoRoot, 'My project (4)', 'Assets', 'Art', 'Environment', 'IceISRU', 'Sprites');

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

function svgWrap(w, h, defs, body) {
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">
  <defs>${defs}</defs>
  ${body}
</svg>`;
}

// =========================================================================
// 1. PHOENIX-STYLE PERMAFROST ICE TRENCH + NASA ISRU RODWELL MELTER (960x820)
//    Reference: NASA Phoenix Mars Lander "Dodo-Goldilocks" Water-Ice Trench
//    + NASA ISRU Subsurface Rodriguez Well (Rodwell) Thermal Extractor
// =========================================================================
const trenchDefs = `
  <radialGradient id="soilFade" cx="50%" cy="56%" r="48%">
    <stop offset="0%" stop-color="#4e2312" stop-opacity="0.72"/>
    <stop offset="68%" stop-color="#7c3a1d" stop-opacity="0.48"/>
    <stop offset="100%" stop-color="#9e4d28" stop-opacity="0"/>
  </radialGradient>
  <linearGradient id="regolithSlope" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0%" stop-color="#b45a31"/>
    <stop offset="55%" stop-color="#873e1e"/>
    <stop offset="100%" stop-color="#512211"/>
  </linearGradient>
  <linearGradient id="pitShadow" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0%" stop-color="#2e150b"/>
    <stop offset="100%" stop-color="#4f2514"/>
  </linearGradient>
  <linearGradient id="iceTop" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#f4fcff"/>
    <stop offset="52%" stop-color="#bae6fd"/>
    <stop offset="100%" stop-color="#60c8fa"/>
  </linearGradient>
  <linearGradient id="iceDeep" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0%" stop-color="#7dd3fc"/>
    <stop offset="60%" stop-color="#0284c7"/>
    <stop offset="100%" stop-color="#0c4a6e"/>
  </linearGradient>
  <linearGradient id="hullCream" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#faf6ec"/>
    <stop offset="58%" stop-color="#e4dbc8"/>
    <stop offset="100%" stop-color="#bfb39c"/>
  </linearGradient>
  <linearGradient id="legMetal" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0%" stop-color="#c6cdd4"/>
    <stop offset="50%" stop-color="#879099"/>
    <stop offset="100%" stop-color="#596169"/>
  </linearGradient>
  <linearGradient id="goldMli" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#fde047"/>
    <stop offset="50%" stop-color="#eab308"/>
    <stop offset="100%" stop-color="#b45309"/>
  </linearGradient>
  <linearGradient id="waterTank" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0%" stop-color="#e0f2fe"/>
    <stop offset="35%" stop-color="#38bdf8"/>
    <stop offset="100%" stop-color="#0369a1"/>
  </linearGradient>
`;

const trenchBody = `
  <!-- Soft natural blend onto Martian desert floor (no artificial plate rim) -->
  <ellipse cx="480" cy="536" rx="430" ry="208" fill="url(#soilFade)"/>

  <!-- Natural Martian permafrost polygonal soil cracks radiating outward -->
  <g fill="none" stroke="#421d0f" stroke-width="5" stroke-linecap="round" opacity="0.58">
    <path d="M168 492 L110 478 L64 504"/>
    <path d="M150 564 L96 602 L58 594"/>
    <path d="M262 632 L226 686 L186 704"/>
    <path d="M776 484 L842 468 L890 492"/>
    <path d="M768 584 L830 622 L878 614"/>
    <path d="M674 636 L712 686 L748 696"/>
  </g>

  <!-- Irregular fractured regolith overhang / excavated trench scarp -->
  <path d="M154 504 L182 446 L256 410 L356 382 L496 376 L632 388 L734 420 L796 472 L810 536 L768 602 L674 644 L548 662 L392 658 L264 632 L176 574 Z"
        fill="url(#regolithSlope)" stroke="${OUTLINE}" stroke-width="6" stroke-linejoin="round"/>

  <!-- Sunken excavation pit shadow (exposing shallow 5cm Martian ice table) -->
  <path d="M196 516 L224 466 L306 432 L442 418 L596 420 L712 444 L760 496 L744 562 L656 608 L516 626 L360 620 L236 582 Z"
        fill="url(#pitShadow)" stroke="${OUTLINE}" stroke-width="6" stroke-linejoin="round"/>

  <!-- Stratified Real Martian H2O Subsurface Ice Table (Phoenix "Dodo-Goldilocks" look) -->
  <path d="M214 532 L252 480 L344 454 L498 448 L644 454 L728 494 L724 558 L628 604 L468 616 L310 598 L226 566 Z"
        fill="url(#iceDeep)" stroke="${OUTLINE}" stroke-width="6.5" stroke-linejoin="round"/>

  <!-- Stepped Crystalline Ice Terraces with realistic Martian regolith dust wisps -->
  <polygon points="222,528 262,480 356,458 424,492 386,546 266,558" fill="url(#iceTop)" stroke="#075985" stroke-width="3.5" stroke-linejoin="round"/>
  <polygon points="356,458 506,452 572,494 494,552 424,492" fill="url(#iceTop)" stroke="#075985" stroke-width="3.5" stroke-linejoin="round"/>
  <polygon points="506,452 648,458 722,498 672,548 572,494" fill="url(#iceTop)" stroke="#075985" stroke-width="3.5" stroke-linejoin="round"/>
  <polygon points="266,558 386,546 494,552 572,494 646,568 518,602 346,594" fill="#7dd3fc" stroke="#075985" stroke-width="3.5" stroke-linejoin="round"/>

  <!-- Natural fracture planes & frosty specular sheen on ice shelf (no cartoon boxes) -->
  <g fill="none" stroke="#f0f9ff" stroke-width="3" stroke-linecap="round" opacity="0.88">
    <path d="M254 514 L318 486 L372 496"/>
    <path d="M402 478 L468 504 L544 478"/>
    <path d="M588 486 L654 476 L696 508"/>
    <path d="M350 568 L452 562 L526 576"/>
  </g>
  <!-- Subtle red Martian wind-blown dust settled in ice crevices -->
  <g fill="#9e4d28" opacity="0.34">
    <ellipse cx="322" cy="542" rx="34" ry="8"/>
    <ellipse cx="614" cy="546" rx="38" ry="9"/>
  </g>

  <!-- Angular Martian basalt pebbles & frost-dusted clods around pit rim -->
  <g stroke="${OUTLINE}" stroke-width="5" stroke-linejoin="round">
    <polygon points="150,554 198,538 224,570 172,588" fill="#7c3a1d"/>
    <polygon points="222,602 260,592 276,618 238,628" fill="#9a4b25"/>
    <polygon points="736,546 788,532 812,568 756,584" fill="#7c3a1d"/>
    <polygon points="684,598 726,590 740,616 698,626" fill="#9a4b25"/>
    <!-- Frost patches along sheltered northern ledge -->
    <path d="M236 458 Q304 430 392 438" fill="none" stroke="#e0f2fe" stroke-width="5.5" stroke-linecap="round" opacity="0.68"/>
    <path d="M566 434 Q646 432 706 462" fill="none" stroke="#e0f2fe" stroke-width="5.5" stroke-linecap="round" opacity="0.68"/>
  </g>

  <!-- Ground EVA Cryo-Water Quick Coupler Hose on Left Side -->
  <g fill="none" stroke-linecap="round">
    <path d="M402 404 C318 408, 224 442, 166 520 C138 558, 128 608, 178 622" stroke="${OUTLINE}" stroke-width="20"/>
    <path d="M402 404 C318 408, 224 442, 166 520 C138 558, 128 608, 178 622" stroke="#0284c7" stroke-width="11"/>
  </g>
  <rect x="164" y="602" width="54" height="32" rx="8" fill="#d8dee3" stroke="${OUTLINE}" stroke-width="6"/>
  <rect x="210" y="608" width="16" height="20" rx="4" fill="#38bdf8" stroke="${OUTLINE}" stroke-width="4.5"/>
  <circle cx="186" cy="618" r="5" fill="#4ade80"/>

  <!-- =================================================================== -->
  <!-- NASA ISRU PORTABLE RODWELL THERMAL DRILL & WATER CONDENSER RIG      -->
  <!-- =================================================================== -->
  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round">
    <polygon points="434,352 346,422 372,438 456,368" fill="url(#legMetal)"/>
    <ellipse cx="352" cy="434" rx="34" ry="14" fill="#596068"/>
    <ellipse cx="352" cy="428" rx="24" ry="9" fill="#c6cdd4"/>

    <polygon points="562,352 650,422 624,438 540,368" fill="url(#legMetal)"/>
    <ellipse cx="644" cy="434" rx="34" ry="14" fill="#596068"/>
    <ellipse cx="644" cy="428" rx="24" ry="9" fill="#c6cdd4"/>
  </g>

  <!-- Thermal Heated Drill Shaft plunging directly into the subsurface ice -->
  <g stroke="${OUTLINE}" stroke-width="7" stroke-linejoin="round">
    <ellipse cx="498" cy="488" rx="42" ry="16" fill="#0ea5e9" stroke="#e0f2fe" stroke-width="4" opacity="0.88"/>
    <rect x="484" y="344" width="28" height="146" rx="8" fill="url(#legMetal)"/>
    <path d="M482 396 L514 410 M482 424 L514 438 M482 452 L514 466" stroke="${OUTLINE}" stroke-width="5" stroke-linecap="round"/>
    <rect x="478" y="366" width="40" height="22" rx="5" fill="#f97316"/>
  </g>

  <!-- Main Rodwell Pump & Meltwater Separator Chassis -->
  <g stroke="${OUTLINE}" stroke-width="8" stroke-linejoin="round">
    <polygon points="410,214 586,214 612,248 384,248" fill="url(#hullCream)"/>
    <rect x="384" y="248" width="228" height="114" rx="12" fill="url(#hullCream)"/>
    <rect x="384" y="254" width="24" height="102" rx="6" fill="${RED_TRIM}"/>
    <rect x="588" y="254" width="24" height="102" rx="6" fill="${RED_TRIM}"/>
  </g>

  <!-- Insulated Sight-Glass H2O Reservoir Column + Gold Kapton Heater Jacket -->
  <rect x="418" y="262" width="54" height="86" rx="8" fill="url(#waterTank)" stroke="${OUTLINE}" stroke-width="5"/>
  <line x1="424" y1="286" x2="438" y2="286" stroke="#f0f9ff" stroke-width="3" stroke-linecap="round"/>
  <line x1="424" y1="308" x2="442" y2="308" stroke="#f0f9ff" stroke-width="3" stroke-linecap="round"/>
  <line x1="424" y1="330" x2="438" y2="330" stroke="#f0f9ff" stroke-width="3" stroke-linecap="round"/>
  <circle cx="454" cy="316" r="4" fill="#ffffff" opacity="0.82"/>
  <circle cx="448" cy="294" r="3" fill="#ffffff" opacity="0.82"/>

  <rect x="482" y="262" width="44" height="86" rx="6" fill="url(#goldMli)" stroke="${OUTLINE}" stroke-width="5"/>
  <path d="M488 274 L508 296 L520 274 M488 334 L508 310 L520 334" fill="none" stroke="#fef08a" stroke-width="2.5" opacity="0.72"/>

  <rect x="534" y="262" width="46" height="86" rx="6" fill="#2c333c" stroke="${OUTLINE}" stroke-width="5"/>
  <circle cx="557" cy="288" r="14" fill="#0c2238" stroke="#9ca4ac" stroke-width="3"/>
  <path d="M557 288 L565 280" stroke="#38bdf8" stroke-width="3" stroke-linecap="round"/>
  <circle cx="548" cy="318" r="5" fill="#4ade80"/>
  <circle cx="566" cy="318" r="5" fill="#38bdf8"/>
  <rect x="541" y="332" width="32" height="7" rx="3" fill="#eab308"/>

  <!-- Top Cryo-Condenser Cap & Beacon -->
  <g stroke="${OUTLINE}" stroke-width="7" stroke-linejoin="round">
    <rect x="446" y="168" width="104" height="46" rx="10" fill="url(#legMetal)"/>
    <line x1="466" y1="176" x2="466" y2="206" stroke="#475569" stroke-width="5"/>
    <line x1="488" y1="176" x2="488" y2="206" stroke="#475569" stroke-width="5"/>
    <line x1="510" y1="176" x2="510" y2="206" stroke="#475569" stroke-width="5"/>
    <line x1="532" y1="176" x2="532" y2="206" stroke="#475569" stroke-width="5"/>
    <circle cx="498" cy="148" r="16" fill="#38bdf8"/>
  </g>
`;

// =========================================================================
// 2. EXPOSED MARTIAN CRATER GLACIER SCARP + VAPOR HARVESTER (960x820)
//    Reference: MRO/HiRISE Eroded Martian Subsurface Water-Ice Scarp
// =========================================================================
const glacierDefs = trenchDefs + `
  <linearGradient id="basaltRock" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#8a523b"/>
    <stop offset="55%" stop-color="#5d3323"/>
    <stop offset="100%" stop-color="#3c1f14"/>
  </linearGradient>
`;

const glacierBody = `
  <!-- Soft terrain gradient onto Martian regolith -->
  <ellipse cx="480" cy="532" rx="430" ry="204" fill="url(#soilFade)"/>

  <!-- Permafrost ground fractures -->
  <g fill="none" stroke="#421d0f" stroke-width="5" stroke-linecap="round" opacity="0.55">
    <path d="M138 488 L76 474 L48 498"/>
    <path d="M174 584 L112 624 L74 614"/>
    <path d="M784 484 L848 468 L888 492"/>
    <path d="M776 588 L838 626 L882 618"/>
  </g>

  <!-- Eroded Basalt/Regolith Overburden Ridge Behind the Exposed Ice Scarp -->
  <path d="M134 484 L178 408 L268 358 L410 334 L574 338 L718 370 L802 436 L822 522 L768 594 L646 644 L476 658 L296 640 L182 586 Z"
        fill="url(#regolithSlope)" stroke="${OUTLINE}" stroke-width="6" stroke-linejoin="round"/>

  <!-- Dark Stratified Basalt Cap Rock Overhanging the Ice Ledge -->
  <path d="M168 456 L246 378 L382 350 L548 358 L708 386 L786 458 L722 496 L528 476 L322 486 Z"
        fill="url(#basaltRock)" stroke="${OUTLINE}" stroke-width="6.5" stroke-linejoin="round"/>

  <!-- Exposed Layered Martian Water-Ice Glacier Face (HiRISE terraced strata) -->
  <path d="M176 508 L234 448 L366 424 L558 428 L732 450 L788 518 L734 588 L590 626 L386 626 L224 588 Z"
        fill="url(#iceDeep)" stroke="${OUTLINE}" stroke-width="6.5" stroke-linejoin="round"/>

  <!-- Upper Bright Terraced Ice Shelf Facets -->
  <polygon points="186,512 246,454 372,434 416,494 316,548 228,556" fill="url(#iceTop)" stroke="#075985" stroke-width="3.5" stroke-linejoin="round"/>
  <polygon points="372,434 548,438 594,498 486,556 416,494" fill="url(#iceTop)" stroke="#075985" stroke-width="3.5" stroke-linejoin="round"/>
  <polygon points="548,438 718,460 774,518 704,564 594,498" fill="url(#iceTop)" stroke="#075985" stroke-width="3.5" stroke-linejoin="round"/>

  <!-- Stepped Subsurface Ice Shelf Ledges (horizontal stratified layers) -->
  <polygon points="244,548 342,516 462,534 584,510 694,548 632,598 478,612 318,596" fill="#93e5ff" stroke="#075985" stroke-width="3.5" stroke-linejoin="round"/>
  <polygon points="292,584 420,564 552,566 648,588 574,616 392,616" fill="#38bdf8" stroke="#075985" stroke-width="3" stroke-linejoin="round" opacity="0.88"/>

  <!-- Crisp ice shelf fracture veins & specular highlights -->
  <g fill="none" stroke="#f0f9ff" stroke-width="3.5" stroke-linecap="round" opacity="0.9">
    <path d="M236 494 L322 468 L384 484"/>
    <path d="M408 468 L486 488 L558 466"/>
    <path d="M608 478 L678 488 L734 514"/>
    <path d="M332 544 L452 554 L568 536"/>
  </g>
  <!-- Wind-blown red Martian dust bands settled along glacial terraces -->
  <g fill="#9e4d28" opacity="0.34">
    <ellipse cx="386" cy="526" rx="46" ry="7"/>
    <ellipse cx="598" cy="534" rx="44" ry="7"/>
  </g>

  <!-- Fractured Basalt Clasts & Regolith Stones on Lower Rim -->
  <g stroke="${OUTLINE}" stroke-width="5.5" stroke-linejoin="round">
    <polygon points="154,548 212,534 236,576 174,590" fill="url(#basaltRock)"/>
    <polygon points="734,544 796,530 818,574 756,592" fill="url(#basaltRock)"/>
    <polygon points="264,606 308,596 324,622 276,630" fill="#8f4523"/>
    <polygon points="642,608 688,598 702,626 654,634" fill="#8f4523"/>
  </g>

  <!-- Right Side Heated Vapor Harvester & Pressurized Water Tap -->
  <g fill="none" stroke-linecap="round">
    <path d="M558 378 C646 392, 742 436, 788 514 C812 554, 820 598, 774 616" stroke="${OUTLINE}" stroke-width="20"/>
    <path d="M558 378 C646 392, 742 436, 788 514 C812 554, 820 598, 774 616" stroke="#0284c7" stroke-width="11"/>
  </g>
  <rect x="734" y="598" width="54" height="32" rx="8" fill="#d8dee3" stroke="${OUTLINE}" stroke-width="6"/>
  <rect x="722" y="604" width="16" height="20" rx="4" fill="#38bdf8" stroke="${OUTLINE}" stroke-width="4.5"/>
  <circle cx="766" cy="614" r="5" fill="#4ade80"/>

  <!-- Compact Solar-Thermal Condenser Rig Mounted on Upper Basalt Shelf -->
  <g stroke="${OUTLINE}" stroke-width="7.5" stroke-linejoin="round">
    <polygon points="414,328 346,386 376,396 438,340" fill="url(#legMetal)"/>
    <polygon points="542,328 610,386 580,396 518,340" fill="url(#legMetal)"/>
    <rect x="466" y="330" width="24" height="122" rx="6" fill="url(#legMetal)"/>
    <rect x="458" y="358" width="40" height="20" rx="5" fill="#f97316"/>
    <rect x="404" y="216" width="148" height="118" rx="22" fill="url(#hullCream)"/>
    <rect x="404" y="238" width="148" height="22" fill="${RED_TRIM}"/>
    <rect x="422" y="268" width="58" height="54" rx="8" fill="url(#waterTank)" stroke-width="5"/>
    <line x1="430" y1="292" x2="452" y2="292" stroke="#ffffff" stroke-width="3" stroke-linecap="round"/>
    <rect x="490" y="268" width="44" height="54" rx="6" fill="url(#goldMli)" stroke-width="5"/>
    <path d="M428 216 C432 162, 524 162, 528 216 Z" fill="url(#legMetal)"/>
    <circle cx="478" cy="176" r="14" fill="#38bdf8"/>
  </g>
`;

// =========================================================================
// 3. ACTIVE ISRU WATER-MELT EXTRACTION PULSE OVERLAY (512x512)
// =========================================================================
const pulseDefs = `
  <radialGradient id="iceHalo" cx="50%" cy="50%" r="50%">
    <stop offset="0%" stop-color="#38bdf8" stop-opacity="0.62"/>
    <stop offset="58%" stop-color="#bae6fd" stop-opacity="0.24"/>
    <stop offset="100%" stop-color="#38bdf8" stop-opacity="0"/>
  </radialGradient>
`;
const pulseBody = `
  <circle cx="256" cy="256" r="226" fill="url(#iceHalo)"/>
  <g fill="none" stroke-linecap="round">
    <path d="M114 212 A148 148 0 0 1 398 212" stroke="#7dd3fc" stroke-width="10" stroke-dasharray="28 20" opacity="0.88"/>
    <path d="M114 300 A148 148 0 0 0 398 300" stroke="#e0f2fe" stroke-width="10" stroke-dasharray="28 20" opacity="0.88"/>
    <path d="M256 172 C256 172, 306 238, 306 276 C306 306, 284 328, 256 328 C228 328, 206 306, 206 276 C206 238, 256 172, 256 172 Z"
          fill="#38bdf8" stroke="#0c4a6e" stroke-width="8" stroke-linejoin="round"/>
    <path d="M236 254 C232 272, 236 292, 248 302" stroke="#f0f9ff" stroke-width="7"/>
  </g>
`;

const assets = [
  { name: 'mars_ice_outcrop_phoenix', w: 960, h: 820, svg: svgWrap(960, 820, trenchDefs, trenchBody) },
  { name: 'mars_ice_glacier_shelf', w: 960, h: 820, svg: svgWrap(960, 820, glacierDefs, glacierBody) },
  { name: 'mars_ice_extract_pulse', w: 512, h: 512, svg: svgWrap(512, 512, pulseDefs, pulseBody) },
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
