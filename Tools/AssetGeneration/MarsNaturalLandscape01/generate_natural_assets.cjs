const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const project = path.resolve(__dirname, '../../../My project (4)');
const root = path.join(project, 'Assets', 'Art', 'Environment', 'MarsNaturalLandscape01');
const dirs = {
  ground: path.join(root, 'Ground'),
  detail: path.join(root, 'Ground', 'Details'),
  regions: path.join(root, 'Ground', 'Regions'),
  rocks: path.join(root, 'Geology', 'Rocks'),
  formations: path.join(root, 'Geology', 'Formations'),
  clusters: path.join(root, 'Geology', 'Clusters'),
};
Object.values(dirs).forEach(p => fs.mkdirSync(p, { recursive: true }));

let seed = 421907;
function rnd() { seed = (seed * 1664525 + 1013904223) >>> 0; return seed / 4294967296; }
function pick(a) { return a[Math.floor(rnd() * a.length)]; }
function svg(w, h, body, defs = '') {
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><defs>${defs}</defs>${body}</svg>`;
}
const outline = '#74675b';
const gradients = `<linearGradient id="soil" x1="0" y1="0" x2=".85" y2="1"><stop stop-color="#d4aa80"/><stop offset=".48" stop-color="#bd8c67"/><stop offset="1" stop-color="#a97759"/></linearGradient>
<linearGradient id="rock" x1=".1" y1="0" x2=".75" y2="1"><stop stop-color="#d4ad86"/><stop offset=".36" stop-color="#a97a5b"/><stop offset=".75" stop-color="#806553"/><stop offset="1" stop-color="#66574d"/></linearGradient>
<linearGradient id="rockLight" x1="0" y1="0" x2=".9" y2="1"><stop stop-color="#e0bd91"/><stop offset=".55" stop-color="#c4946d"/><stop offset="1" stop-color="#916d55"/></linearGradient>
<linearGradient id="sand" x1="0" y1="0" x2=".85" y2="1"><stop stop-color="#e1bc8d"/><stop offset=".56" stop-color="#c99b70"/><stop offset="1" stop-color="#a67a5b"/></linearGradient>
<linearGradient id="scree" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#876b59"/><stop offset="1" stop-color="#aa8060"/></linearGradient>
<filter id="shadow" x="-25%" y="-25%" width="160%" height="175%"><feGaussianBlur stdDeviation="14"/></filter>
<pattern id="grain" width="96" height="96" patternUnits="userSpaceOnUse">${Array.from({length:42},()=>`<circle cx="${(rnd()*96).toFixed(1)}" cy="${(rnd()*96).toFixed(1)}" r="${(.7+rnd()*2.2).toFixed(1)}" fill="${pick(['#f1d0a4','#765c4b','#9f7354','#f7dfbb'])}" opacity="${(.09+rnd()*.17).toFixed(2)}"/>`).join('')}${Array.from({length:8},()=>`<path d="M${(rnd()*96).toFixed(0)} ${(rnd()*96).toFixed(0)}l${(3+rnd()*13).toFixed(0)} ${(-2+rnd()*4).toFixed(0)}" stroke="#725849" stroke-opacity=".12" stroke-width="1.3" stroke-linecap="round"/>`).join('')}</pattern>
<pattern id="fineDust" width="256" height="256" patternUnits="userSpaceOnUse">${Array.from({length:10},()=>{const x=(rnd()*256).toFixed(0),y=(rnd()*256).toFixed(0),rx=(4+rnd()*12).toFixed(0);return `<ellipse cx="${x}" cy="${y}" rx="${rx}" ry="${(1+rnd()*2.5).toFixed(1)}" fill="#efd1a8" opacity=".09" transform="rotate(${(rnd()*180).toFixed(0)} ${x} ${y})"/>`;}).join('')}</pattern>`;

const files = [];
function add(rel, w, h, body, defs = gradients, category = 'ground') {
  files.push({ rel, w, h, text: svg(w, h, body, defs), target: path.join(dirs[category], rel) });
}
function tileBase(extra = '', base = 'url(#soil)') {
  return `<rect width="1024" height="1024" fill="${base}"/><rect width="1024" height="1024" fill="url(#grain)"/><rect width="1024" height="1024" fill="url(#fineDust)"/>${extra}`;
}
function pebble(cx, cy, rx, ry, tone = '#aa7f5e', rot = 0, detailed = true) {
  const d = `M${cx-rx*.84} ${cy-ry*.12} Q${cx-rx*.72} ${cy-ry*.85} ${cx-rx*.12} ${cy-ry*.92} Q${cx+rx*.62} ${cy-ry*.88} ${cx+rx*.91} ${cy-ry*.08} Q${cx+rx*.94} ${cy+ry*.48} ${cx+rx*.3} ${cy+ry*.82} Q${cx-rx*.5} ${cy+ry*.96} ${cx-rx*.92} ${cy+ry*.35}Z`;
  const inner = `M${cx-rx*.48} ${cy-ry*.5}q${rx*.5} ${-ry*.33} ${rx*.9} ${ry*.08}q${rx*.24} ${ry*.24} ${rx*.18} ${ry*.5}`;
  return `<g transform="rotate(${rot} ${cx} ${cy})"><ellipse cx="${cx+rx*.12}" cy="${cy+ry*.42}" rx="${rx*1.05}" ry="${ry*.68}" fill="#55483f" opacity=".23"/><path d="${d}" fill="#705d4e" opacity=".23" transform="translate(0 ${Math.max(5,ry*.24)})"/><path d="${d}" fill="${tone}" stroke="${outline}" stroke-width="${Math.max(2,rx*.055)}" stroke-linejoin="round"/><path d="${inner}" fill="none" stroke="#ecd0a7" stroke-width="${Math.max(2,rx*.045)}" stroke-linecap="round" opacity=".58"/>${detailed?`<path d="M${cx-rx*.25} ${cy-ry*.61}q${rx*.23} ${-ry*.15} ${rx*.47} ${-ry*.07}M${cx+rx*.18} ${cy+ry*.19}l${rx*.22} ${ry*.19}" fill="none" stroke="#604f43" stroke-width="${Math.max(1.4,rx*.035)}" opacity=".48"/>`:''}</g>`;
}
function angularBlock(cx,cy,rx,ry,tone='#a67b5b',rotation=0,detail=1) {
  const pts=[];
  const count=9;
  for(let i=0;i<count;i++){
    const a=-Math.PI/2+i*Math.PI*2/count;
    const f=.78+rnd()*.38;
    pts.push([cx+Math.cos(a)*rx*f,cy+Math.sin(a)*ry*f]);
  }
  const path='M'+pts.map(p=>`${p[0].toFixed(1)} ${p[1].toFixed(1)}`).join('L')+'Z';
  const poly=(inds)=>inds.map(i=>`${pts[i%count][0].toFixed(1)},${pts[i%count][1].toFixed(1)}`).join(' ');
  const st=Math.max(4,rx*.026);
  return `<g transform="rotate(${rotation} ${cx} ${cy})"><path d="${path}" transform="translate(4 ${Math.max(15,ry*.22)})" fill="#51443a" opacity=".46"/><path d="${path}" fill="${tone}" stroke="${outline}" stroke-width="${st}" stroke-linejoin="round"/><polygon points="${poly([0,1,2,3,4])}" fill="#e0bc91" opacity=".68"/><polygon points="${poly([3,4,5,6])}" fill="#745a49" opacity=".7"/><polygon points="${poly([6,7,8,0,1])}" fill="#8a684f" opacity=".43"/><path d="M${pts[1][0]} ${pts[1][1]}L${(cx+rx*.1).toFixed(1)} ${(cy-ry*.08).toFixed(1)}L${pts[4][0]} ${pts[4][1]}M${pts[6][0]} ${pts[6][1]}L${(cx+rx*.18).toFixed(1)} ${(cy+ry*.22).toFixed(1)}L${pts[8][0]} ${pts[8][1]}" fill="none" stroke="#5f4c40" stroke-width="${Math.max(3,rx*.018)}" opacity=".72" stroke-linecap="round"/><path d="M${(cx-rx*.44).toFixed(1)} ${(cy-ry*.32).toFixed(1)}l${(rx*.21).toFixed(1)} ${(-ry*.15).toFixed(1)}M${(cx+rx*.12).toFixed(1)} ${(cy-ry*.42).toFixed(1)}l${(rx*.17).toFixed(1)} ${(-ry*.04).toFixed(1)}" fill="none" stroke="#f0d3ab" stroke-width="${Math.max(2,rx*.018)}" opacity=".75" stroke-linecap="round"/>${detail>1?`<path d="M${(cx-rx*.12).toFixed(1)} ${cy.toFixed(1)}l${(rx*.08).toFixed(1)} ${(-ry*.15).toFixed(1)}l${(rx*.12).toFixed(1)} ${(-ry*.03).toFixed(1)}M${(cx+rx*.3).toFixed(1)} ${(cy+ry*.22).toFixed(1)}l${(rx*.08).toFixed(1)} ${(-ry*.12).toFixed(1)}" fill="none" stroke="#59483d" stroke-width="${Math.max(2,rx*.014)}" opacity=".66"/>`:''}</g>`;
}

// Seamless ground materials: their grain and micro-fractures repeat without a visible hard tile edge.
add('regolith_sandstone_tile.svg',1024,1024,tileBase(Array.from({length:70},()=>{const x=(rnd()*1024).toFixed(0),y=(rnd()*1024).toFixed(0),r=(2+rnd()*13).toFixed(1);return `<ellipse cx="${x}" cy="${y}" rx="${r}" ry="${(r*.36).toFixed(1)}" fill="${pick(['#745a49','#ead0a6','#925f48','#d8b48b'])}" opacity=".${(10+Math.floor(rnd()*15))}" transform="rotate(${Math.floor(rnd()*180)} ${x} ${y})"/>`;}).join('')));
add('rocky_regolith_tile.svg',1024,1024,tileBase(Array.from({length:58},()=>pebble(Math.round(rnd()*1024),Math.round(rnd()*1024),8+rnd()*28,5+rnd()*15,pick(['#b18968','#90715a','#c49b73','#806858']),Math.round(rnd()*160),false)).join('')));
add('fractured_bedrock_tile.svg',1024,1024,tileBase(`<g fill="none" stroke-linecap="round">${Array.from({length:18},()=>{let x=Math.round(rnd()*1024),y=Math.round(rnd()*1024);return `<path d="M${x} ${y}q${Math.round(rnd()*80-40)} ${Math.round(rnd()*50-25)} ${Math.round(rnd()*70-35)} ${Math.round(rnd()*75+18)}m0 0l${Math.round(rnd()*35-18)} ${Math.round(rnd()*50+15)}" stroke="#765c49" stroke-width="${1.4+rnd()*2}" opacity=".42"/>`;}).join('')}</g>`));
add('wind_ripple_tile.svg',1024,1024,`<rect width="1024" height="1024" fill="url(#sand)"/><rect width="1024" height="1024" fill="url(#grain)"/>${Array.from({length:18},(_,i)=>{let y=i*63-100,x=(i%2)*35;return `<path d="M${x-40} ${y+40} C220 ${y-44},590 ${y+102},1065 ${y-18}" fill="none" stroke="#765849" stroke-width="12" opacity=".17"/><path d="M${x-20} ${y+26} C230 ${y-55},620 ${y+90},1050 ${y-30}" fill="none" stroke="#f2d0a1" stroke-width="6" opacity=".52"/>`;}).join('')}`);
add('dust_deposit_patch.svg',1100,820,`<path d="M85 421Q34 298 186 238Q220 96 391 126Q514 32 642 149Q817 102 875 244Q1056 278 1003 430Q1051 568 886 620Q801 753 645 669Q499 794 358 683Q186 728 149 582Q52 555 85 421Z" fill="#dab48a" opacity=".82"/><path d="M153 413Q124 315 253 272Q303 167 412 191Q519 120 631 211Q767 171 831 285Q955 309 929 421Q967 532 829 565Q753 668 624 597Q504 702 384 598Q244 650 207 535Q120 511 153 413Z" fill="#e4c49b" opacity=".44"/><path d="M134 466q100-60 169-25M723 529q77-45 142-22M364 237q94-34 149-11" fill="none" stroke="#f2d3ad" stroke-width="9" opacity=".44"/>`);
add('wind_scour_detail.svg',1024,720,`<path d="M74 426Q224 354 361 380T636 354Q787 313 950 371" fill="none" stroke="#745849" stroke-width="28" opacity=".24"/><path d="M62 391Q211 322 360 347T635 322Q783 284 970 338" fill="none" stroke="#e7c49a" stroke-width="18" opacity=".54"/><path d="M92 484q135-44 260-20t244-17q132-29 303 4M116 531q164-48 289-18t253-22q100-17 249 2" fill="none" stroke="#745849" stroke-width="7" opacity=".31" stroke-linecap="round"/>`,'<linearGradient id="soil" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#d4aa80"/><stop offset="1" stop-color="#a97759"/></linearGradient>'+gradients.slice(gradients.indexOf('<linearGradient id="rock"')),'detail');
add('fractures_fine.svg',920,720,`<g fill="none" stroke="#655448" stroke-linecap="round" stroke-linejoin="round" opacity=".62">${Array.from({length:11},()=>{let x=100+rnd()*720,y=80+rnd()*560;return `<path d="M${x.toFixed(0)} ${y.toFixed(0)}l${(rnd()*30-15).toFixed(0)} ${(30+rnd()*45).toFixed(0)}l${(rnd()*24-12).toFixed(0)} ${(16+rnd()*40).toFixed(0)}m0 0l${(rnd()*24-12).toFixed(0)} ${(18+rnd()*36).toFixed(0)}m${-5} ${-35}l${(rnd()*30-15).toFixed(0)} ${(-22-rnd()*24).toFixed(0)}" stroke-width="${2+rnd()*3}"/>`;}).join('')}</g><g fill="#d9b88e" opacity=".58">${Array.from({length:35},()=>`<circle cx="${Math.round(rnd()*920)}" cy="${Math.round(rnd()*720)}" r="${1+rnd()*3}"/>`).join('')}</g>`,'<linearGradient id="soil"><stop stop-color="#fff"/></linearGradient>'+gradients.slice(gradients.indexOf('<linearGradient id="rock"')),'detail');

const edgeMaskDefs=(id,w,h)=>`<filter id="edgeFade" x="-20%" y="-20%" width="140%" height="140%"><feGaussianBlur stdDeviation="30"/></filter><mask id="${id}" maskUnits="userSpaceOnUse" x="0" y="0" width="${w}" height="${h}"><path d="M90 ${h*.54}Q20 ${h*.32} ${w*.16} ${h*.27}Q${w*.26} ${h*.04} ${w*.39} ${h*.18}Q${w*.53} ${h*.02} ${w*.62} ${h*.2}Q${w*.87} ${h*.08} ${w*.91} ${h*.32}Q${w*.99} ${h*.49} ${w*.88} ${h*.63}Q${w*.78} ${h*.91} ${w*.62} ${h*.8}Q${w*.45} ${h*.99} ${w*.35} ${h*.81}Q${w*.16} ${h*.94} ${w*.14} ${h*.7}Z" fill="white" stroke="white" stroke-width="100" filter="url(#edgeFade)"/></mask>`;
{
  const w=1850,h=1380,mask=edgeMaskDefs('softEdge',w,h);
  const body=`<g mask="url(#softEdge)"><rect width="${w}" height="${h}" fill="#8d6e56"/><rect width="${w}" height="${h}" fill="url(#grain)" opacity=".86"/><rect width="${w}" height="${h}" fill="url(#fineDust)" opacity=".6"/><path d="M100 770Q300 478 620 540T1070 450Q1420 380 1770 710L1710 1100Q1420 1260 1100 1130T510 1250Q200 1130 100 770Z" fill="#755c49" opacity=".22"/>${rockScatter(130,{x:60,y:80,w:1730,h:1240},[9,37])}</g>`;
  add('rocky_regolith_region.svg',w,h,body,gradients+mask,'regions');
}
{
  const w=1850,h=1380,mask=edgeMaskDefs('softEdge',w,h);
  const layers=Array.from({length:17},(_,i)=>{const y=280+i*59,amp=30+rnd()*38;return `<path d="M${180+rnd()*120} ${y}Q${510} ${y-amp} ${790} ${y+amp*.3}T${1380} ${y-amp*.35}Q${1570} ${y-amp} ${1720} ${y+20}" fill="none" stroke="${i%3===0?'#e4c39d':'#745b49'}" stroke-width="${i%3===0?10:5}" opacity="${i%3===0?.42:.28}"/>`;}).join('');
  const cracks=Array.from({length:18},()=>{const x=120+rnd()*1600,y=150+rnd()*1100;return `<path d="M${x} ${y}l${Math.round(rnd()*40-20)} 54l${Math.round(rnd()*28-14)} 45m0 0l${Math.round(rnd()*36-18)} 37" fill="none" stroke="#685448" stroke-width="4" opacity=".46"/>`;}).join('');
  const body=`<g mask="url(#softEdge)"><rect width="${w}" height="${h}" fill="#9a7558"/><rect width="${w}" height="${h}" fill="url(#grain)" opacity=".9"/><path d="M155 630Q377 392 586 492T1020 390Q1370 416 1707 560L1656 972Q1315 1110 1063 986T598 1142Q296 1000 155 630Z" fill="#b5906c" opacity=".74"/><path d="M200 735Q473 560 696 668T1140 588Q1462 568 1680 720" fill="none" stroke="#745a49" stroke-width="72" opacity=".35"/><path d="M174 690Q446 516 675 618T1125 540Q1437 518 1690 665" fill="none" stroke="#dfba90" stroke-width="27" opacity=".57"/>${layers}${cracks}${rockScatter(62,{x:90,y:120,w:1690,h:1150},[7,26])}</g>`;
  add('sedimentary_bedrock_region.svg',w,h,body,gradients+mask,'regions');
}
{
  const w=1900,h=1240,mask=edgeMaskDefs('softEdge',w,h);
  let lines='';
  for(let i=0;i<23;i++){const y=185+i*42,x=(i%3)*-40,curve=Math.round(rnd()*110-55);lines+=`<path d="M${x} ${y+55}C${w*.23} ${y+curve} ${w*.57} ${y+110+curve} ${w+80} ${y+curve*.25}" fill="none" stroke="#${i%3===0?'785d49':'f0d2a7'}" stroke-width="${i%3===0?13:7}" opacity="${i%3===0?.25:.48}" stroke-linecap="round"/>`;}
  const body=`<g mask="url(#softEdge)"><rect width="${w}" height="${h}" fill="#c7976d"/><rect width="${w}" height="${h}" fill="url(#grain)" opacity=".55"/><path d="M160 650Q350 460 580 531T990 524Q1340 398 1725 572L1710 875Q1400 1030 1090 903T580 998Q270 922 160 650Z" fill="#d9b488" opacity=".61"/>${lines}<path d="M105 779q223-136 382-93t339-4q246 80 439-7t524 2" fill="none" stroke="#7b604a" stroke-width="11" opacity=".3"/>${rockScatter(32,{x:90,y:190,w:1710,h:980},[6,18])}</g>`;
  add('wind_ripple_region.svg',w,h,body,gradients+mask,'regions');
}

// A map-sized natural substrate keeps the broad biome transitions coherent while
// individual landforms remain separate editable sprites above it.
{
  const w=4096,h=2730;
  const contour=(cx,cy,rx,ry,count,step,color,opacity,width=7)=>Array.from({length:count},(_,i)=>{
    const a=step*i,rx2=rx-i*step*1.25,ry2=ry-i*step*.76;
    if(rx2<24||ry2<24)return '';
    return `<path d="M${cx-rx2} ${cy} C${cx-rx2*.94} ${cy-ry2*.68} ${cx-rx2*.33} ${cy-ry2} ${cx} ${cy-ry2} C${cx+rx2*.75} ${cy-ry2*.97} ${cx+rx2} ${cy-ry2*.4} ${cx+rx2} ${cy} C${cx+rx2*.92} ${cy+ry2*.64} ${cx+rx2*.4} ${cy+ry2} ${cx} ${cy+ry2} C${cx-rx2*.77} ${cy+ry2*.94} ${cx-rx2} ${cy+ry2*.38} ${cx-rx2} ${cy}Z" fill="none" stroke="${color}" stroke-width="${width}" opacity="${opacity}"/>`;
  }).join('');
  const zones=`<path d="M0 0H4096V2730H0Z" fill="url(#soil)"/>
  <path d="M0 0H1575Q1490 294 1603 562T1494 1101Q1384 1460 1513 1737T1355 2266L1432 2730H0Z" fill="#ae7959" opacity=".32"/>
  <path d="M2650 0H4096V1295Q3820 1225 3665 1395T3288 1404Q3110 1304 2914 1413T2587 1321Q2474 1090 2678 920T2600 530Q2466 286 2650 0Z" fill="#d0a47a" opacity=".27"/>
  <path d="M2820 0H4096V1015Q3894 1092 3721 1000T3385 1035Q3233 1167 3052 1038T2776 903Q2915 737 2758 565T2820 0Z" fill="#d9b488" opacity=".4"/>
  <path d="M0 1940Q319 1710 612 1830T1114 1981Q1395 2148 1625 2048L1790 2730H0Z" fill="#826651" opacity=".32"/>
  <path d="M2640 1720Q2872 1558 3096 1667T3555 1658Q3825 1534 4096 1680V2730H2550Q2751 2496 2632 2287T2640 1720Z" fill="#99775d" opacity=".35"/>
  <path d="M0 0H4096V2730H0Z" fill="url(#grain)" opacity=".62"/><path d="M0 0H4096V2730H0Z" fill="url(#fineDust)" opacity=".9"/>
  <path d="M0 0H4096V2730H0Z" fill="url(#softGrain)" opacity=".32"/>
  ${contour(3570,560,580,356,9,24,'#efd0a1',.25,10)}
  ${contour(685,2110,525,300,8,25,'#594b40',.26,11)}
  <path d="M3035 0Q3002 198 3179 331T3408 680Q3597 820 3844 792M3130 0Q3097 193 3285 306T3523 618Q3725 766 3956 720M3226 0Q3191 158 3386 270T3640 552Q3826 675 4030 655" fill="none" stroke="#805f49" stroke-width="14" opacity=".23"/>
  <path d="M2700 2000Q2822 1907 2928 1974T3152 1948Q3264 1885 3389 1973T3611 1950Q3760 1878 3940 1941M2680 2080Q2810 1984 2930 2055T3154 2028Q3268 1968 3392 2054T3620 2033Q3770 1963 3950 2021" fill="none" stroke="#dfb78b" stroke-width="11" opacity=".5"/>
  ${contour(1110,2170,505,325,7,28,'#dfb58b',.27,8)}`;
  const definition=gradients+`<pattern id="softGrain" width="512" height="512" patternUnits="userSpaceOnUse">${Array.from({length:65},()=>{let x=Math.round(rnd()*512),y=Math.round(rnd()*512),rx=2+rnd()*10;return `<circle cx="${x}" cy="${y}" r="${rx.toFixed(1)}" fill="${pick(['#684f42','#ecd0a5','#937054'])}" opacity=".${(8+Math.floor(rnd()*12))}"/>`;}).join('')}${Array.from({length:8},()=>{let x=rnd()*512,y=rnd()*512;return `<path d="M${x.toFixed(0)} ${y.toFixed(0)}q${(10+rnd()*44).toFixed(0)} -6 ${(-10+rnd()*45).toFixed(0)} 5" stroke="#755849" stroke-width="2" opacity=".16" fill="none"/>`;}).join('')}</pattern>`;
  add('landscape_regolith_base.svg',w,h,zones,definition,'ground');
}

// Rock specimens: faceted, layered forms with warm gray linework and restrained highlights.
for (const [name, rx, ry, tone] of [
  ['stone_small_angular',230,165,'#b78963'],['stone_small_round',225,155,'#c49a70'],
  ['rock_medium_stratified',360,260,'#9c7659'],['boulder_angular_layered',470,330,'#a67b5a'],
  ['boulder_weathered',460,340,'#8a6e59'],['rock_plate_fragment',420,240,'#ae8260']]) {
  const w=rx*2+100,h=ry*2+120,cx=w/2,cy=h/2;
  const path1=`M${cx-rx*.92} ${cy-ry*.12}Q${cx-rx*.83} ${cy-ry*.74} ${cx-rx*.28} ${cy-ry*.89}L${cx+rx*.1} ${cy-ry*.76}Q${cx+rx*.63} ${cy-ry*.89} ${cx+rx*.88} ${cy-ry*.36}Q${cx+rx*1.03} ${cy+.12*ry} ${cx+rx*.68} ${cy+ry*.68}Q${cx+rx*.3} ${cy+ry*.91} ${cx-rx*.13} ${cy+ry*.76}Q${cx-rx*.81} ${cy+ry*.82} ${cx-rx*.96} ${cy+ry*.28}Z`;
  const body=`<ellipse cx="${cx+30}" cy="${cy+ry*.52}" rx="${rx*1.02}" ry="${ry*.64}" fill="#50463f" opacity=".28" filter="url(#shadow)"/><path d="${path1}" transform="translate(0 30)" fill="#54473e" opacity=".25"/><path d="${path1}" fill="url(#rockLight)" stroke="${outline}" stroke-width="12" stroke-linejoin="round"/><path d="M${cx-rx*.72} ${cy-ry*.29}Q${cx-rx*.5} ${cy-ry*.78} ${cx-rx*.12} ${cy-ry*.68}L${cx+rx*.1} ${cy-ry*.73}Q${cx+rx*.51} ${cy-ry*.72} ${cx+rx*.68} ${cy-ry*.28}L${cx+rx*.27} ${cy-ry*.16}Q${cx-rx*.03} ${cy-ry*.37} ${cx-rx*.32} ${cy-ry*.16}Z" fill="#e3c29a" opacity=".65"/><path d="M${cx-rx*.91} ${cy+ry*.13}Q${cx-rx*.52} ${cy+ry*.25} ${cx-rx*.2} ${cy+ry*.1}Q${cx+rx*.14} ${cy+ry*.23} ${cx+rx*.9} ${cy+ry*.02}L${cx+rx*.69} ${cy+ry*.58}Q${cx+.1*rx} ${cy+ry*.83} ${cx-rx*.47} ${cy+ry*.61}Z" fill="#745a49" opacity=".52"/><path d="M${cx-rx*.54} ${cy+ry*.27}q${rx*.18} ${ry*.1} ${rx*.34} ${ry*.04}t${rx*.49} ${-.08}M${cx-rx*.29} ${cy+ry*.49}q${rx*.23} ${-.11} ${rx*.41} ${-.02}" fill="none" stroke="#e2bd91" stroke-width="8" opacity=".66" stroke-linecap="round"/><path d="M${cx-rx*.58} ${cy-ry*.36}l${-rx*.12} ${ry*.27}l${rx*.09} ${ry*.12}M${cx+rx*.38} ${cy-ry*.3}l${-rx*.09} ${ry*.23}l${rx*.14} ${ry*.1}" fill="none" stroke="#5f5046" stroke-width="7" opacity=".56"/><circle cx="${cx-rx*.1}" cy="${cy+ry*.04}" r="${rx*.026}" fill="#624f42" opacity=".45"/><path d="${path1}" fill="url(#grain)" opacity=".34"/>`;
  add(`${name}.svg`,w,h,body,gradients,'rocks');
}

function rockScatter(n, area, sizes=[12,28]) {
  let out='';
  for(let i=0;i<n;i++) { const x=area.x+rnd()*area.w,y=area.y+rnd()*area.h,rx=sizes[0]+rnd()*(sizes[1]-sizes[0]),ry=rx*(.43+rnd()*.33); out+=pebble(x,y,rx,ry,pick(['#b68b66','#96745a','#c39a72','#806756','#d0a980']),Math.round(rnd()*180),true); }
  return out;
}

// Wide top-down formations sized for flexible use as sprites at 448 PPU.
{
  const w=1900,h=1120;
  const mesa=`M180 573Q142 413 282 327L453 275L548 179L713 203L841 136L1000 196L1157 171L1276 255L1469 235L1609 329L1690 481L1663 646L1528 755L1386 806L1295 926L1118 904L990 986L824 919L689 965L551 863L399 875L284 754Z`;
  const body=`<path d="${mesa}" transform="translate(20 54)" fill="#51463f" opacity=".34" filter="url(#shadow)"/><path d="${mesa}" transform="translate(0 55)" fill="#725948" stroke="${outline}" stroke-width="14" stroke-linejoin="round"/><path d="${mesa}" fill="url(#rock)" stroke="${outline}" stroke-width="12" stroke-linejoin="round"/><path d="M286 548Q268 430 374 376L503 325L596 241L730 258L859 197L982 248L1128 216L1252 306L1431 285L1556 374L1622 494L1593 621L1488 706L1347 735L1261 843L1114 826L991 901L845 848L698 895L571 794L433 814L330 713Z" fill="#c2956d" stroke="#846650" stroke-width="8"/><path d="M387 513Q465 413 613 414T870 370Q1040 328 1212 397T1487 397L1553 487Q1466 539 1376 527T1204 585Q1012 637 839 571T568 634L417 629Z" fill="#d3aa7d" opacity=".78"/><path d="M346 683Q472 622 605 674T855 691Q1007 740 1179 650T1498 645L1404 728Q1270 759 1201 837L1114 826L990 901L845 848L698 895L571 794L433 814Z" fill="#795c49" opacity=".74"/><path d="M416 740Q525 688 631 728T829 760M975 809q112 18 204-55t180-56M617 387q118-67 237-12t213-16M1217 360q98 18 173 76" fill="none" stroke="#e0b98e" stroke-width="13" stroke-linecap="round" opacity=".7"/><path d="M506 323l63 57-31 55 83 47M855 199l-12 83 67 38-43 66M1250 308l-62 54 24 72-74 49M672 793l58-41 54 19 35-57" fill="none" stroke="#675547" stroke-width="8" opacity=".66" stroke-linecap="round"/><g opacity=".9">${rockScatter(52,{x:190,y:170,w:1510,h:800},[10,25])}</g>`;
  add('mesa_layered_terrace.svg',w,h,body,gradients,'formations');
}
{
  const w=1800,h=920;
  const ridge=`M95 502Q215 450 280 331L405 365Q477 243 619 305L749 225Q855 314 969 264Q1080 335 1190 300Q1287 408 1411 372Q1508 466 1690 402L1723 572Q1559 628 1427 557Q1307 661 1170 572Q1049 656 929 559Q790 649 674 543Q525 628 413 529Q259 609 111 585Z`;
  const body=`<path d="${ridge}" transform="translate(0 44)" fill="#554940" opacity=".38"/><path d="${ridge}" fill="url(#rock)" stroke="${outline}" stroke-width="14" stroke-linejoin="round"/><path d="M163 494Q267 451 333 368L421 395Q500 295 612 347L749 276Q853 351 973 307Q1081 377 1190 337Q1300 451 1406 408Q1510 500 1660 452L1669 521Q1533 575 1422 503Q1305 603 1182 515Q1059 608 930 497Q803 597 677 481Q539 574 425 465Q290 553 169 541Z" fill="#bd9068" stroke="#836650" stroke-width="7"/><path d="M327 371Q418 344 478 363Q545 307 620 337M749 276q74 60 153 59t141 28M1192 339q80 82 151 69t111 54" fill="none" stroke="#e4c29a" stroke-width="12" stroke-linecap="round" opacity=".78"/><path d="M233 492q72-37 132-23t109-14M614 454q89-49 153-25t122-5M1030 455q72-55 151-30t116 9M1396 486q93-44 195-5" fill="none" stroke="#705745" stroke-width="14" opacity=".78"/><path d="M470 406l-29 56 47 31m362-161-31 57 55 31m350-32-38 49 58 39" fill="none" stroke="#655448" stroke-width="8"/>${rockScatter(42,{x:130,y:250,w:1530,h:430},[10,22])}`;
  add('wind_cut_rock_ridge.svg',w,h,body,gradients,'formations');
}
{
  const w=1320,h=1160;
  const outer='M660 94Q831 101 951 210Q1093 297 1119 457Q1186 616 1087 760Q1022 908 866 987Q688 1089 520 1029Q348 1007 238 882Q97 752 120 587Q83 416 207 290Q351 126 520 130Z';
  const rim='M656 196Q808 199 909 294Q1019 382 1037 503Q1075 633 996 749Q941 861 811 920Q670 994 535 945Q396 914 313 809Q211 706 229 581Q205 451 300 359Q405 222 536 230Z';
  const bowl='M665 316Q785 310 860 392Q951 461 947 559Q979 652 900 733Q844 805 750 840Q634 887 531 827Q430 795 382 713Q324 626 354 532Q368 430 447 377Q540 303 665 316Z';
  const body=`<path d="${outer}" transform="translate(12 35)" fill="#554940" opacity=".32" filter="url(#shadow)"/><path d="${outer}" fill="#b98b66" stroke="${outline}" stroke-width="12"/><path d="${rim}" fill="#7f6250" stroke="#66564b" stroke-width="9"/><path d="${rim}" fill="#bd906b" opacity=".75"/><path d="${bowl}" fill="url(#sand)" stroke="#746051" stroke-width="8"/><path d="M333 550Q393 461 498 435Q582 405 648 431Q741 454 812 429Q887 461 937 536Q956 630 874 712Q798 791 692 801Q560 828 460 755Q370 703 333 620Z" fill="#b28562" opacity=".66"/><path d="M414 568Q470 501 548 492Q624 471 694 506Q772 537 839 520M402 637q106-61 184-32t175-12q61 0 104 54" fill="none" stroke="#e1bd91" stroke-width="13" opacity=".62" stroke-linecap="round"/><path d="M265 472Q311 361 423 308M909 348q94 60 100 151M972 735q-75 96-178 112M277 717q42 105 134 153" fill="none" stroke="#e0b58a" stroke-width="15" opacity=".7"/><path d="M242 336l72-44 45 31m601 558 72-32 6-55m-754-80-46 55 23 42" fill="none" stroke="#645348" stroke-width="9" opacity=".75"/>${rockScatter(68,{x:140,y:150,w:1040,h:820},[7,21])}`;
  add('eroded_impact_crater.svg',w,h,body,gradients,'formations');
}
{
  const w=1660,h=960;
  const d1='M82 565Q148 440 295 404Q417 367 545 408Q663 441 777 400Q926 349 1085 390Q1257 431 1572 342L1580 566Q1325 647 1132 573Q980 530 837 588Q686 641 552 580Q405 525 296 581Q172 642 82 565Z';
  const d2='M190 748Q303 635 452 637Q580 627 695 664Q833 708 946 645Q1102 566 1274 626Q1410 664 1537 615L1541 755Q1363 832 1225 777Q1092 731 977 799Q827 887 662 801Q515 734 399 795Q270 863 190 748Z';
  const body=`<path d="${d1}" transform="translate(0 26)" fill="#5e4c40" opacity=".29"/><path d="${d1}" fill="url(#sand)" stroke="#82634e" stroke-width="8"/><path d="M94 536Q223 427 334 435T546 449Q680 482 797 435T1081 427Q1274 460 1560 372" fill="none" stroke="#e7c99f" stroke-width="17" opacity=".76" stroke-linecap="round"/><path d="M96 572Q227 461 344 474t215 24q135 33 259-16t259-21q168 37 424-47" fill="none" stroke="#84634d" stroke-width="15" opacity=".42"/><path d="${d2}" transform="translate(0 24)" fill="#5e4c40" opacity=".27"/><path d="${d2}" fill="url(#sand)" stroke="#82634e" stroke-width="8"/><path d="M204 718q123-101 251-63t237 36q134 0 244-61t236-20q127 49 353-30" fill="none" stroke="#ead0a6" stroke-width="16" opacity=".69"/><path d="M231 777q115-99 229-65t235 45q148 15 265-59t243-12" fill="none" stroke="#89674f" stroke-width="12" opacity=".42"/>${Array.from({length:38},()=>{let x=120+rnd()*1400,y=360+rnd()*490;return `<path d="M${x} ${y}q65-23 130 ${-8+rnd()*18}" fill="none" stroke="#f0d5ae" stroke-width="3" opacity=".48"/>`;}).join('')}`;
  add('barchan_dune_field.svg',w,h,body,gradients,'formations');
}
{
  const w=1740,h=1000;
  const outer='M90 491Q244 406 382 464Q497 527 619 421Q757 315 860 395Q991 480 1124 405Q1261 329 1370 406Q1512 494 1647 430L1644 588Q1478 667 1350 581Q1232 507 1111 600Q966 703 827 600Q713 516 625 623Q497 755 360 643Q235 545 95 625Z';
  const inner='M128 513Q272 453 393 503Q515 556 638 467Q750 381 851 439Q978 511 1121 453Q1253 399 1352 456Q1482 536 1612 477';
  const body=`<path d="${outer}" transform="translate(0 34)" fill="#4f453e" opacity=".32"/><path d="${outer}" fill="#85664f" stroke="${outline}" stroke-width="10"/><path d="M96 504Q239 424 384 483Q502 549 626 444Q756 350 858 418Q985 495 1120 431Q1260 358 1368 433Q1503 513 1638 453" fill="none" stroke="#c1946e" stroke-width="56"/><path d="${inner}" fill="none" stroke="#c9a07a" stroke-width="90"/><path d="${inner}" fill="none" stroke="#a17a5e" stroke-width="47"/><path d="${inner}" fill="none" stroke="#6f5948" stroke-width="19"/><path d="M120 551q154-53 261 4t261-53q102-65 197-19m201 29q115-78 217-34t249 17" fill="none" stroke="#e2bc90" stroke-width="11" opacity=".75"/>${rockScatter(74,{x:90,y:370,w:1560,h:400},[8,26])}`;
  add('dry_erosion_channel.svg',w,h,body,gradients,'formations');
}
{
  const w=1500,h=1150;
  const body=`<path d="M164 763Q209 599 360 532Q425 349 603 326Q749 193 901 303Q1069 276 1168 407Q1344 476 1311 647Q1415 799 1277 918Q1114 1050 945 977Q784 1091 633 989Q427 1066 325 920Q171 899 164 763Z" transform="translate(8 28)" fill="#54483e" opacity=".34"/><path d="M164 763Q209 599 360 532Q425 349 603 326Q749 193 901 303Q1069 276 1168 407Q1344 476 1311 647Q1415 799 1277 918Q1114 1050 945 977Q784 1091 633 989Q427 1066 325 920Q171 899 164 763Z" fill="url(#rock)" stroke="${outline}" stroke-width="12"/><path d="M223 754Q274 633 397 575Q465 411 619 397Q745 273 883 366Q1030 335 1112 457Q1268 512 1237 656Q1324 790 1204 880Q1082 977 933 911Q786 1014 654 916Q470 985 377 867Q246 855 223 754Z" fill="#c49a72" stroke="#80634e" stroke-width="9"/><path d="M319 716Q394 633 486 608Q559 517 665 514Q759 425 863 473Q977 444 1060 525Q1167 572 1134 675Q1202 774 1119 830Q1017 888 918 827Q790 893 689 824Q544 877 472 800Q362 789 319 716Z" fill="#80634f" opacity=".78"/><path d="M393 704Q470 648 541 651Q616 572 704 590Q793 518 873 568Q970 548 1033 615Q1103 658 1071 724Q1100 782 1038 809Q955 844 891 789Q802 841 725 780Q619 814 561 762Q468 766 393 704Z" fill="#c2966f"/><path d="M405 704q80-51 153-30t149-35q83-51 161-3t151 11" fill="none" stroke="#e4c39b" stroke-width="13" opacity=".66"/>${rockScatter(82,{x:190,y:300,w:1110,h:700},[9,28])}`;
  add('layered_bedrock_shelf.svg',w,h,body,gradients,'formations');
}

// Top-down mountain-scale forms model real Martian relief as contour-banded
// mesas, escarpments, ridges and talus rather than a side-view skyline.
{
  const w=2300,h=1580;
  const massif='M128 894L175 767 151 662 239 578 277 453 400 421 462 326 610 353 701 250 834 302 947 207 1082 281 1213 219 1329 319 1474 278 1550 386 1711 393 1778 503 1919 535 1995 657 2127 716 2098 833 2174 926 2096 1017 2069 1141 1954 1200 1885 1317 1725 1288 1622 1382 1493 1296 1358 1360 1215 1278 1087 1350 971 1266 814 1338 700 1251 547 1289 463 1182 319 1180 288 1067 157 1021Z';
  const contour1='M255 874L291 777 274 683 348 608 375 509 483 481 533 398 644 420 727 340 837 378 946 294 1069 354 1191 301 1296 389 1431 352 1497 448 1641 456 1700 550 1822 582 1887 685 2004 739 1980 831 2041 916 1971 987 1947 1086 1850 1128 1789 1221 1652 1198 1555 1272 1436 1192 1325 1248 1195 1171 1081 1234 976 1158 839 1222 735 1144 601 1170 526 1081 403 1075 376 986 273 950Z';
  const terrace='M432 850L465 771 455 700 518 645 541 566 632 540 670 480 761 498 830 436 924 467 1014 405 1118 455 1221 414 1306 488 1420 458 1472 536 1595 546 1642 625 1744 654 1796 741 1896 789 1877 863 1920 929 1860 982 1842 1062 1759 1095 1708 1170 1590 1149 1508 1206 1407 1139 1311 1187 1197 1125 1099 1177 1007 1117 887 1171 796 1106 679 1123 614 1045 508 1035 488 964 415 931Z';
  const summit='M658 835L685 770 678 713 731 671 750 608 823 589 854 540 931 554 988 505 1067 530 1142 484 1229 528 1310 500 1377 558 1463 538 1511 602 1607 612 1644 677 1724 702 1765 770 1842 808 1828 865 1859 917 1811 959 1797 1022 1730 1048 1685 1105 1592 1091 1527 1136 1445 1081 1366 1119 1285 1068 1207 1112 1131 1062 1036 1103 965 1051 873 1061 820 998 737 989 723 933 649 909Z';
  const body=`<path d="${massif}" transform="translate(24 65)" fill="#40372f" opacity=".4"/><path d="${massif}" fill="#785b46" stroke="#5e4d40" stroke-width="19" stroke-linejoin="bevel"/><path d="${contour1}" fill="#a17a5c" stroke="#725947" stroke-width="12" stroke-linejoin="bevel"/><path d="${terrace}" fill="#c09972" stroke="#81634d" stroke-width="10" stroke-linejoin="bevel"/><path d="${summit}" fill="#9b7658" stroke="#695444" stroke-width="10" stroke-linejoin="bevel"/><path d="M780 802L811 748 810 705 854 667 873 620 930 610M964 562l54 36 48-5 55 38m57-82 41 41 58-1m-730 350 68 24 58-13 53 36m233 28 62-28 63 9 54-30m230-74 56-25 57 11" fill="none" stroke="#e4c197" stroke-width="16" opacity=".74" stroke-linecap="round" stroke-linejoin="round"/><path d="M493 837l58-54 50-11 43-55m710-96 34 55-14 52 48 38m-920 128 61-7 39 43-16 53m1009 82-65-39-52 7-45-47M1164 653l-38 49 12 51-43 42" fill="none" stroke="#55463b" stroke-width="12" opacity=".76" stroke-linecap="round" stroke-linejoin="round"/><path d="M670 452l28 58-41 42 57 44m845 164-54 38-9 54 52 36m-375-633 16 55-42 38" fill="none" stroke="#dab58a" stroke-width="10" opacity=".7"/>${rockScatter(210,{x:180,y:200,w:1950,h:1190},[9,38])}`;
  add('mountain_foothills_complex.svg',w,h,body,gradients,'formations');
}
{
  const w=2180,h=1060;
  const body=`<path d="M100 644Q238 575 342 437Q464 385 560 466Q665 306 839 365Q978 241 1120 338Q1272 268 1385 380Q1541 301 1653 425Q1836 367 2078 480L2052 718Q1844 821 1687 696Q1549 786 1397 656Q1260 771 1117 640Q981 763 838 632Q692 776 553 639Q419 751 297 648L119 751Z" transform="translate(0 50)" fill="#50443b" opacity=".34"/><path d="M100 644Q238 575 342 437Q464 385 560 466Q665 306 839 365Q978 241 1120 338Q1272 268 1385 380Q1541 301 1653 425Q1836 367 2078 480L2052 718Q1844 821 1687 696Q1549 786 1397 656Q1260 771 1117 640Q981 763 838 632Q692 776 553 639Q419 751 297 648L119 751Z" fill="#81624c" stroke="${outline}" stroke-width="14"/><path d="M139 622Q265 558 367 426Q472 392 569 446Q687 321 835 390Q975 276 1115 361Q1273 301 1387 405Q1536 339 1652 449Q1825 402 2043 500L2025 638Q1855 722 1684 636Q1544 714 1397 592Q1262 699 1116 580Q980 686 838 573Q694 698 553 584Q421 689 302 593L152 685Z" fill="#bb906b" stroke="#84664f" stroke-width="8"/><path d="M224 586Q322 534 403 431Q487 417 575 470Q700 358 833 421Q970 324 1107 399Q1260 350 1377 447Q1526 387 1642 489Q1806 451 2005 525" fill="none" stroke="#ead0a6" stroke-width="19" opacity=".72" stroke-linecap="round"/><path d="M210 667Q330 608 413 510Q500 493 588 541Q708 433 839 489Q978 393 1115 467Q1264 419 1383 515Q1524 455 1647 553Q1804 516 2016 594" fill="none" stroke="#695546" stroke-width="18" opacity=".68" stroke-linecap="round"/>${rockScatter(94,{x:120,y:320,w:1950,h:520},[8,25])}`;
  add('stair_stepped_mountain_ridge.svg',w,h,body,gradients,'formations');
}
{
  const w=1750,h=1450;
  let stones='';
  for(let i=0;i<150;i++){
    const t=rnd(),y=180+t*1080,x=275+(1-t)*rnd()*1180+t*170+(rnd()-.5)*370;
    const scale=(.3+t*.9)*(15+rnd()*33),ry=scale*(.43+rnd()*.5);
    stones+=pebble(x,y,scale,ry,pick(['#b38a66','#8f7158','#c6a078','#765f4e','#d4ae83']),Math.round(rnd()*180),true);
  }
  const body=`<path d="M251 270Q438 183 638 257Q841 312 1037 231Q1271 232 1486 366L1431 481Q1216 395 1048 441Q837 496 656 429Q446 384 297 482Z" fill="#77604c" stroke="${outline}" stroke-width="12"/><path d="M280 322Q471 250 655 318T1039 292Q1249 281 1450 403" fill="none" stroke="#e0bc91" stroke-width="17" opacity=".75"/><path d="M265 471Q509 407 680 488T1036 453Q1241 412 1462 535" fill="none" stroke="#6c5544" stroke-width="28" opacity=".42"/><path d="M110 1240Q255 902 415 676Q632 499 840 504Q1118 442 1514 644" fill="none" stroke="#57483e" stroke-width="64" opacity=".13"/>${stones}<g fill="none" stroke="#6f5745" stroke-width="8" opacity=".52"><path d="M460 464q-28 180 48 280m292-255q34 137 4 239m372-228q-68 132-14 259M628 754q-78 105-66 208m706-226q72 107 47 203"/></g>`;
  add('landslide_talus_boulder_apron.svg',w,h,body,gradients,'clusters');
}
{
  const w=1720,h=1350;
  let boulders='';
  const placements=[[480,400,260,195],[850,330,330,235],[1215,460,265,200],[350,740,315,235],[760,745,380,275],[1215,800,330,245],[550,1080,245,180],[1015,1115,270,205]];
  for(let i=0;i<placements.length;i++){
    const [x,y,rx,ry]=placements[i];
    boulders+=angularBlock(x,y,rx,ry,pick(['#8c7059','#a37d5d','#c19a71','#796352']),Math.round(rnd()*40-20),2);
  }
  const body=`<ellipse cx="900" cy="760" rx="735" ry="500" fill="#584b41" opacity=".22" filter="url(#shadow)"/>${boulders}<path d="M215 674q99-114 184-111m709-237q115-31 215 36M1298 947q-58 144-194 166M474 1078q85 67 185 45" fill="none" stroke="#e2c29a" stroke-width="17" opacity=".56" stroke-linecap="round"/>`;
  add('mega_boulder_cluster.svg',w,h,body,gradients,'clusters');
}
for (const [name,tone] of [['giant_angular_block','#a67b5b'],['giant_layered_block','#927055'],['giant_split_boulder','#b58a63']]) {
  const w=900,h=720;
  const form=angularBlock(450,355,350,258,tone,0,2);
  const strata=name==='giant_layered_block'?'<path d="M190 388l160-43 142 22 184-67M220 456l166-39 139 19 173-66" fill="none" stroke="#e2bd92" stroke-width="15" opacity=".68"/>':'';
  const split=name==='giant_split_boulder'?'<path d="M484 170l-42 112 31 74-68 94 45 112" fill="none" stroke="#59483d" stroke-width="18" opacity=".8"/>':'';
  add(`${name}.svg`,w,h,`<ellipse cx="450" cy="390" rx="370" ry="280" fill="#493d34" opacity=".16"/>${form}${strata}${split}`,gradients,'rocks');
}

// Concentrated scree field with irregular density gradients, not a uniform sprinkle.
{
  const w=1460,h=1100;
  let rocks='';
  for(let i=0;i<85;i++) { const x=210+rnd()*1040,y=220+rnd()*700,d=Math.hypot((x-760)/610,(y-560)/425),prob=d<.66?1:d<.94?.5:.12;if(rnd()>prob)continue;const r=10+rnd()*38;rocks+=pebble(x,y,r,r*(.42+rnd()*.42),pick(['#b48a65','#96765c','#c59d75','#89705a','#d0a77e']),Math.round(rnd()*180),true); }
  const body=`<path d="M140 567Q243 357 463 360Q598 228 783 281Q1019 236 1188 407Q1357 518 1296 716Q1215 911 1016 915Q825 1029 629 921Q410 994 252 826Q106 737 140 567Z" fill="#8a6951" opacity=".17"/>${rocks}<path d="M281 433q134-109 270-63m429-45q127 17 202 112M235 739q82 106 212 116m633-5q138-26 204-153" fill="none" stroke="#e1bd91" stroke-width="12" opacity=".48" stroke-linecap="round"/>`;
  add('concentrated_rockfall_field.svg',w,h,body,gradients,'clusters');
}
{
  const w=1280,h=960;
  const body=`<path d="M155 600Q240 399 433 382Q582 225 761 317Q954 278 1084 432Q1201 574 1127 723Q1035 880 851 851Q675 954 509 837Q312 875 195 739Z" fill="#67594e" opacity=".28"/>${rockScatter(23,{x:180,y:270,w:930,h:600},[28,88])}<path d="M218 629q86-133 224-161m488-66q117 22 177 123M369 814q117 55 237 9m334-43q75-39 113-117" fill="none" stroke="#e4c29a" stroke-width="12" opacity=".55"/>`;
  add('cluster_buried_boulders.svg',w,h,body,gradients,'clusters');
}

// Natural lower-left / central-region ground detail clusters, transparent and reusable.
{
  const w=1100,h=700;
  add('embedded_pebble_scatter.svg',w,h,rockScatter(65,{x:70,y:60,w:960,h:580},[6,24]),gradients,'detail');
}

async function main() {
  for (const f of files) {
    fs.writeFileSync(f.target, f.text, 'utf8');
    await sharp(Buffer.from(f.text)).png().toFile(f.target.replace(/\.svg$/, '.png'));
    const png=f.target.replace(/\.svg$/,'.png');
    if (!fs.existsSync(png+'.meta')) {
      const guid=require('crypto').randomBytes(16).toString('hex');
      const spriteId=require('crypto').randomBytes(16).toString('hex');
      fs.writeFileSync(png+'.meta', `fileFormatVersion: 2\nguid: ${guid}\nTextureImporter:\n  internalIDToNameTable: []\n  externalObjects: {}\n  serializedVersion: 13\n  mipmaps:\n    mipMapMode: 0\n    enableMipMap: 0\n  isReadable: 0\n  streamingMipmaps: 0\n  textureFormat: 1\n  maxTextureSize: 4096\n  textureSettings:\n    serializedVersion: 2\n    filterMode: 1\n    aniso: 1\n    mipBias: 0\n    wrapU: 0\n    wrapV: 0\n    wrapW: 0\n  spriteMode: 1\n  spriteExtrude: 1\n  spriteMeshType: 1\n  alignment: 9\n  spritePivot: {x: 0.5, y: 0.5}\n  spritePixelsToUnits: 448\n  spriteBorder: {x: 0, y: 0, z: 0, w: 0}\n  alphaUsage: 1\n  alphaIsTransparency: 1\n  textureType: 8\n  textureShape: 1\n  spriteSheet:\n    serializedVersion: 2\n    sprites: []\n    outline: []\n    physicsShape: []\n    bones: []\n    spriteID: ${spriteId}\n    internalID: 0\n    vertices: []\n    indices: \n    edges: []\n    weights: []\n    secondaryTextures: []\n    nameFileIdTable: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`,'utf8');
    }
    const svgMeta=png.replace(/\.png$/,'.svg.meta');
    if (!fs.existsSync(svgMeta)) fs.writeFileSync(svgMeta,`fileFormatVersion: 2\nguid: ${require('crypto').randomBytes(16).toString('hex')}\nScriptedImporter:\n  internalIDToNameTable: []\n  externalObjects: {}\n  serializedVersion: 2\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n  script: {fileID: 12408, guid: 0000000000000000e000000000000000, type: 0}\n  svgType: 3\n  texturedSpriteMeshType: 0\n  svgPixelsPerUnit: 448\n  gradientResolution: 64\n  alignment: 9\n  customPivot: {x: 0, y: 0}\n`,'utf8');
  }
  for(const d of Object.values(dirs)) {
    if(!fs.existsSync(d+'.meta')) fs.writeFileSync(d+'.meta',`fileFormatVersion: 2\nguid: ${require('crypto').randomBytes(16).toString('hex')}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
  }
  console.log(`Generated ${files.length} editable SVG masters and matching PNGs in ${root}`);
}
main().catch(e=>{console.error(e);process.exit(1);});
