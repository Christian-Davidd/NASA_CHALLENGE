const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const project = path.resolve(__dirname, '../../../My project (4)');
const art = path.join(project, 'Assets/Art/Buildings/Agricultural_Module');
const previewDir = path.resolve(__dirname, '../../../Previews');
const palette = {
  outline:'#716e68', ink:'#53565a', ivory:'#f0eee8', white:'#fbfaf5',
  shade:'#d3d1ca', shadow:'#77716a', steel:'#a5a7a6', steelDark:'#6f7374',
  blue:'#31577b', blueLite:'#7ea5bc', red:'#b63a32', gold:'#d4a244',
  soil:'#564c43', soil2:'#756454', green:'#628458', greenLight:'#91ad72',
  greenDark:'#35523a', magenta:'#d25cad', cyan:'#70c4cf'
};
const items=[];

function add(folder,name,w,h,body,ppu=100){items.push({folder,name,w,h,body,ppu});}
function svg(w,h,body){return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><defs><linearGradient id="shell" x2="0" y2="1"><stop stop-color="#fffefa"/><stop offset=".48" stop-color="#eeece4"/><stop offset="1" stop-color="#c9c7c0"/></linearGradient><linearGradient id="steel" x2="0" y2="1"><stop stop-color="#c7c9c6"/><stop offset="1" stop-color="#777d7e"/></linearGradient><linearGradient id="glass" x2="0" y2="1"><stop stop-color="#5d737b"/><stop offset="1" stop-color="#25353b"/></linearGradient><filter id="shadow" x="-30%" y="-30%" width="160%" height="170%"><feGaussianBlur stdDeviation="18"/></filter><pattern id="perfs" width="28" height="28" patternUnits="userSpaceOnUse"><circle cx="14" cy="14" r="3.2" fill="#303536"/><circle cx="14" cy="12" r="1.2" fill="#bbc0bd"/></pattern></defs>${body}</svg>`;}
function bolts(xs,ys,r=6){return xs.flatMap(x=>ys.map(y=>`<circle cx="${x}" cy="${y}" r="${r+3}" fill="#777975"/><circle cx="${x}" cy="${y}" r="${r}" fill="#d9d7d0" stroke="#646663" stroke-width="2"/><path d="M${x-r/2} ${y}h${r}" stroke="#777975" stroke-width="2"/>`)).join('');}
function label(text,x,y,size=22,color='#4d5253',weight='700'){return `<text x="${x}" y="${y}" font-family="Arial,sans-serif" font-size="${size}" font-weight="${weight}" fill="${color}" letter-spacing="1">${text}</text>`;}
function connector(x1,y1,x2,y2){return `<path d="M${x1} ${y1}H${x2}V${y2}" fill="none" stroke="#64696a" stroke-width="20" stroke-linecap="round"/><path d="M${x1} ${y1}H${x2}V${y2}" fill="none" stroke="#c4c5bf" stroke-width="11" stroke-linecap="round"/><circle cx="${x1}" cy="${y1}" r="19" fill="#858988" stroke="#555a5b" stroke-width="5"/><circle cx="${x2}" cy="${y2}" r="19" fill="#858988" stroke="#555a5b" stroke-width="5"/>`;}

// Low, connected pressure modules: based on the modular surface-habitat language in NASA concepts.
{
 const body=`<ellipse cx="610" cy="695" rx="515" ry="115" fill="#4d4038" opacity=".24" filter="url(#shadow)"/>
 <g stroke-linejoin="round"><path d="M126 490Q126 300 298 250L887 250Q1055 305 1055 490V587H126Z" fill="#77716a" opacity=".22" transform="translate(10 22)"/>
 <rect x="100" y="318" width="290" height="300" rx="126" fill="url(#shell)" stroke="#666a6b" stroke-width="15"/><rect x="122" y="337" width="246" height="259" rx="108" fill="none" stroke="#b7b8b4" stroke-width="7"/>
 <rect x="295" y="245" width="573" height="373" rx="136" fill="url(#shell)" stroke="#666a6b" stroke-width="17"/><rect x="319" y="264" width="525" height="332" rx="120" fill="none" stroke="#c4c5c0" stroke-width="8"/>
 <path d="M348 286q-34 110 0 291M406 270q-28 148 0 310M758 272q34 147 0 308M814 292q31 119 0 280" fill="none" stroke="#aaa9a2" stroke-width="12" opacity=".75"/>
 <rect x="851" y="335" width="272" height="277" rx="112" fill="url(#shell)" stroke="#65696a" stroke-width="15"/><rect x="871" y="354" width="232" height="240" rx="95" fill="none" stroke="#babbb5" stroke-width="7"/>
 ${connector(370,430,292,430)}${connector(850,444,920,444)}
 <rect x="535" y="495" width="118" height="91" rx="18" fill="url(#steel)" stroke="#515658" stroke-width="8"/><rect x="547" y="507" width="94" height="66" rx="11" fill="#3e4c50" stroke="#dad8d0" stroke-width="6"/><path d="M562 526h64m-64 13h48" stroke="#a7c0c1" stroke-width="5"/><circle cx="626" cy="555" r="6" fill="#63b8a8"/>
 <rect x="493" y="287" width="196" height="45" rx="16" fill="#d5d5cf" stroke="#747775" stroke-width="5"/><path d="M512 309h31m14 0h31m14 0h31m14 0h22" stroke="#727777" stroke-width="4"/>
 <rect x="441" y="367" width="194" height="71" rx="13" fill="#eceae2" stroke="#858783" stroke-width="5"/>${label('PLANT LAB',459,397,20,'#445c66')}${label('PRESSURIZED',459,423,15,'#6d7475')}
 <rect x="705" y="353" width="98" height="102" rx="12" fill="#4b5557" stroke="#747879" stroke-width="6"/><rect x="717" y="365" width="74" height="56" rx="7" fill="url(#glass)"/><circle cx="731" cy="438" r="7" fill="#65b3c0"/><circle cx="750" cy="438" r="7" fill="#d4a244"/><circle cx="769" cy="438" r="7" fill="#b63a32"/>
 <rect x="456" y="605" width="274" height="53" rx="24" fill="url(#steel)" stroke="#5a5e60" stroke-width="8"/><rect x="467" y="617" width="252" height="28" rx="13" fill="url(#perfs)"/>
 <path d="M450 672v70m70-70v70m210-70v70m72-70v70" stroke="#777d7e" stroke-width="16" stroke-linecap="round"/><path d="M434 741h379" stroke="#515658" stroke-width="15" stroke-linecap="round"/>
 <rect x="132" y="388" width="140" height="191" rx="16" fill="url(#steel)" stroke="#51595b" stroke-width="9"/><rect x="144" y="401" width="116" height="166" rx="12" fill="url(#shell)" stroke="#777d7e" stroke-width="6"/><rect x="158" y="416" width="88" height="137" rx="10" fill="#bbc0bd" stroke="#626a6c" stroke-width="6"/><path d="M169 433h65" stroke="#b63a32" stroke-width="12"/><path d="M168 444h67" stroke="#d4a244" stroke-width="5" stroke-dasharray="8 6"/><rect x="177" y="468" width="50" height="50" rx="9" fill="url(#glass)" stroke="#e5e6e0" stroke-width="5"/><circle cx="232" cy="535" r="5" fill="#50585a"/><path d="M137 412v138" stroke="#e1e3dd" stroke-width="5"/><circle cx="135" cy="456" r="6" fill="#63b8a8"/><circle cx="135" cy="479" r="6" fill="#d4a244"/>
 <path d="M154 579 72 704m118-125 84 125" fill="none" stroke="#575d5f" stroke-width="24" stroke-linecap="round"/><path d="M154 577 72 698m118-121 84 121" fill="none" stroke="#c8cac4" stroke-width="13" stroke-linecap="round"/><path d="M62 704h223" stroke="#555b5d" stroke-width="13"/><path d="M79 688h188m-168-28 12 7m14-29 12 7m14-29 12 7m14-29 12 7" stroke="#777d7e" stroke-width="6"/>
 <g>${bolts([160,330,518,700,865,1060],[367,568],5)}</g>
 <rect x="596" y="675" width="160" height="38" rx="11" fill="#eceae2" stroke="#777975" stroke-width="5"/><path d="M618 694h24" stroke="#6ea979" stroke-width="8"/><path d="M675 694h53" stroke="#8c8b85" stroke-width="5"/>
 </g><path d="M495 743h217" stroke="#b63a32" stroke-width="8"/><path d="M495 754h217" stroke="#31577b" stroke-width="6"/>`;
 add('Exterior','Agricultural_Module_Exterior',1220,820,body,140);
}

// Three connected, pressurized modules viewed from above: decon, greenhouse, and systems.
{
 const b=`<rect width="2600" height="1280" fill="#bbb9b1"/>
 <path d="M45 330Q45 280 100 280H375V60Q375 32 410 32H2190Q2225 32 2225 60V180H2500Q2555 180 2555 235V1045Q2555 1100 2500 1100H2225V1220Q2225 1248 2190 1248H410Q375 1248 375 1220V1000H100Q45 1000 45 950Z" fill="#666b6b" stroke="#555a5c" stroke-width="22"/>
 <path d="M72 345Q72 308 110 308H405V82Q405 65 428 65H2172Q2195 65 2195 82V210H2488Q2522 210 2522 246V1034Q2522 1070 2488 1070H2195V1198Q2195 1215 2172 1215H428Q405 1215 405 1198V972H110Q72 972 72 935Z" fill="#e4e0d7" stroke="#969792" stroke-width="9"/>
 <path d="M100 370Q100 340 130 340H435V110H2165V240H2460V1040H2165V1170H435V940H130Q100 940 100 910Z" fill="#d5d0c5" stroke="#aaa59c" stroke-width="5"/>
 <path d="M385 375V905M2215 275V1005" stroke="#666b6b" stroke-width="27"/><path d="M385 405V875M2215 305V975" stroke="#c6c3bb" stroke-width="15"/>
 <path d="M445 150H2155M445 1130H2155M160 410H355M160 870H355M2245 330H2440M2245 950H2440" stroke="#b0aa9e" stroke-width="7" opacity=".8"/>
 <g fill="#e9e5dc" stroke="#6e7372" stroke-width="7"><rect x="90" y="330" width="265" height="35" rx="11"/><rect x="90" y="915" width="265" height="35" rx="11"/><rect x="2260" y="205" width="265" height="35" rx="11"/><rect x="2260" y="1040" width="265" height="35" rx="11"/><rect x="930" y="77" width="260" height="36" rx="11"/><rect x="1410" y="1185" width="260" height="36" rx="11"/></g>
 ${bolts([85,415,800,1300,1800,2185,2510],[75,1205],5)}
 <g font-family="Arial,sans-serif" fill="#88877e" font-size="22" font-weight="700" letter-spacing="3"><text x="90" y="640" transform="rotate(-90 90 640)">ENTRY / DECONTAMINATION</text><text x="1030" y="150">CROP PRODUCTION MODULE</text><text x="2290" y="640" transform="rotate(-90 2290 640)">SYSTEMS / CONTROL</text></g>`;
 add('Interior','interior_floor_walls',2600,1280,b,100);
}
// Pressure vestibule viewed from above, with distinct exterior and inner hatches.
{
 const b=`<ellipse cx="360" cy="302" rx="300" ry="240" fill="#453c35" opacity=".16"/><rect x="83" y="38" width="554" height="505" rx="66" fill="url(#steel)" stroke="#555b5d" stroke-width="17"/><rect x="111" y="65" width="498" height="451" rx="48" fill="#e8e5dd" stroke="#a7a79f" stroke-width="7"/><rect x="160" y="105" width="400" height="365" rx="35" fill="#d7d3c9" stroke="#777c7c" stroke-width="8"/><path d="M180 146h360M180 430h360" stroke="#bbb6ab" stroke-width="5"/><ellipse cx="360" cy="288" rx="138" ry="137" fill="#c4c2bc" stroke="#5d6263" stroke-width="12"/><ellipse cx="360" cy="288" rx="112" ry="111" fill="url(#glass)" stroke="#e7e5dd" stroke-width="13"/><ellipse cx="360" cy="288" rx="87" ry="86" fill="#26353a" stroke="#62696a" stroke-width="6"/><path d="M314 251q42-43 87-30" fill="none" stroke="#b9d2d3" stroke-width="9" opacity=".65"/><rect x="273" y="66" width="174" height="30" rx="10" fill="#b63a32" stroke="#656867" stroke-width="4"/><rect x="306" y="480" width="108" height="30" rx="10" fill="#31577b" stroke="#656867" stroke-width="4"/><rect x="474" y="168" width="73" height="117" rx="10" fill="#51595b" stroke="#777c7c" stroke-width="5"/><rect x="486" y="181" width="50" height="34" rx="5" fill="url(#glass)"/><circle cx="498" cy="235" r="7" fill="#61c0b0"/><circle cx="521" cy="235" r="7" fill="#d4a244"/><circle cx="498" cy="258" r="7" fill="#b63a32"/><path d="M220 302H130m460 0h-92" stroke="#737879" stroke-width="8"/><g>${bolts([119,601],[99,484],5)}</g><path d="M236 549v27m248-27v27" stroke="#747a7a" stroke-width="10"/>`;
 add('Airlock','airlock_topdown',720,620,b,110);
}

// Mars entry sequence hardware: top-down, edge-on pressure bulkhead and dust-removal grate.
{
 const side=`<ellipse cx="150" cy="365" rx="92" ry="315" fill="#403832" opacity=".18"/><rect x="92" y="20" width="116" height="680" rx="23" fill="#555d5f" stroke="#30383a" stroke-width="10"/><rect x="105" y="32" width="90" height="656" rx="17" fill="#e6e4dc" stroke="#a8aaa5" stroke-width="6"/><rect x="126" y="45" width="48" height="630" rx="12" fill="#8b9290" stroke="#535b5d" stroke-width="6"/><path d="M137 71h26M137 648h26" stroke="#e1dfd7" stroke-width="7"/><path d="M150 94v528" stroke="#656d6e" stroke-width="7"/><path d="M129 180h42m-42 360h42" stroke="#b63a32" stroke-width="7"/><circle cx="150" cy="355" r="13" fill="#c8ccc7" stroke="#51595b" stroke-width="5"/><circle cx="150" cy="355" r="4" fill="#526064"/>${bolts([98,202],[70,355,650],4)}`;
 add('Airlock','airlock_pressure_gate_side',300,720,side,120);
}
{
 const bars=Array.from({length:12},(_,i)=>`<path d="M${72+i*39} 64v190" stroke="#7d8280" stroke-width="10"/>`).join('');
 const b=`<ellipse cx="320" cy="170" rx="290" ry="112" fill="#403832" opacity=".18"/><rect x="16" y="25" width="608" height="286" rx="30" fill="url(#steel)" stroke="#555d5f" stroke-width="13"/><rect x="38" y="46" width="564" height="244" rx="19" fill="#333b3d" stroke="#d5d2c9" stroke-width="7"/><rect x="57" y="67" width="526" height="202" rx="12" fill="#252d30" stroke="#111a1d" stroke-width="5"/>${bars}<path d="M66 75H574M66 260H574" stroke="#b7b9b3" stroke-width="13"/><path d="M53 50h-25v238h25m532-238h25v238h-25" fill="none" stroke="#8c9290" stroke-width="8"/>${bolts([55,585],[53,282],5)}`;
 add('Airlock','airlock_decon_grate',640,340,b,130);
}
{
 const b=`<ellipse cx="210" cy="145" rx="196" ry="128" fill="#403832" opacity=".17"/><rect x="14" y="18" width="392" height="236" rx="26" fill="url(#steel)" stroke="#555d5f" stroke-width="11"/><rect x="30" y="34" width="360" height="204" rx="17" fill="#e9e6de" stroke="#707778" stroke-width="6"/><rect x="50" y="55" width="142" height="103" rx="12" fill="#303a3d" stroke="#737978" stroke-width="5"/><circle cx="121" cy="105" r="37" fill="#95b9bc" stroke="#e3e4de" stroke-width="6"/><path d="M121 105l17-20" stroke="#b63a32" stroke-width="7" stroke-linecap="round"/><path d="M92 146q29-15 58 0" fill="none" stroke="#e3e4de" stroke-width="4"/><rect x="214" y="58" width="148" height="42" rx="8" fill="#c7c9c4" stroke="#717777" stroke-width="4"/><path d="M228 72h119m-119 14h84" stroke="#727879" stroke-width="4"/><rect x="218" y="121" width="146" height="82" rx="9" fill="#343c3e" stroke="#747a79" stroke-width="4"/><path d="M232 141h116m-116 17h82m-82 17h102" stroke="#9ba7a4" stroke-width="4"/>${bolts([29,391],[34,238],4)}`;
 add('Airlock','airlock_cycle_console',420,270,b,115);
}
{
 const b=`<circle cx="96" cy="96" r="83" fill="#343b3c" stroke="#afb2ad" stroke-width="9"/><circle cx="96" cy="96" r="63" fill="#818783" stroke="#4d5657" stroke-width="8"/><circle cx="96" cy="96" r="48" fill="#d8d5cd" stroke="#eff0e9" stroke-width="5"/><circle cx="96" cy="96" r="34" fill="#b63a32"/><circle cx="82" cy="80" r="13" fill="#fff" opacity=".65"/>`;
 add('Airlock','airlock_cycle_lamp',192,192,b,120);
}

// Hydroponic bench top: flat plant pillow modules, individual ports and LED frames.
{
 const trays=Array.from({length:12},(_,i)=>{const x=55+(i%3)*208,y=55+Math.floor(i/3)*151;return `<g><rect x="${x}" y="${y}" width="164" height="126" rx="17" fill="#293235" stroke="#656d6c" stroke-width="7"/><rect x="${x+10}" y="${y+10}" width="144" height="106" rx="12" fill="url(#perfs)" stroke="#141d20" stroke-width="4"/>${[0,1,2,3,4,5].map(k=>{const px=x+34+(k%3)*48,py=y+35+Math.floor(k/3)*48;return `<ellipse cx="${px}" cy="${py}" rx="21" ry="16" fill="#c3b49c" stroke="#ebe1d1" stroke-width="4"/><ellipse cx="${px}" cy="${py-2}" rx="14" ry="10" fill="#64594c"/>`;}).join('')}<rect x="${x+124}" y="${y+17}" width="24" height="16" rx="4" fill="#b63a32"/><text x="${x+21}" y="${y+103}" font-family="Arial" font-size="14" font-weight="700" fill="#d9d6cb">PILLOW ARRAY ${String(i+1).padStart(2,'0')}</text></g>`;}).join('');
 const feedLines=`<path d="M49 80V640M671 80V640" fill="none" stroke="#31577b" stroke-width="12" stroke-linecap="round"/><path d="M49 112h24m-24 151h24m-24 151h24m-24 151h24M647 112h24m-24 151h24m-24 151h24m-24 151h24" fill="none" stroke="#58a5b7" stroke-width="9" stroke-linecap="round"/>${[0,1,2,3].map(i=>`<circle cx="49" cy="${112+i*151}" r="11" fill="#a9d2d5" stroke="#31577b" stroke-width="5"/><circle cx="671" cy="${112+i*151}" r="11" fill="#d4a244" stroke="#715b35" stroke-width="5"/>`).join('')}`;
 const b=`<ellipse cx="360" cy="380" rx="323" ry="332" fill="#443c35" opacity=".18"/><rect x="20" y="20" width="680" height="680" rx="42" fill="url(#steel)" stroke="#565b5d" stroke-width="14"/><rect x="38" y="38" width="644" height="644" rx="31" fill="#eae8e1" stroke="#bbb9b2" stroke-width="7"/>${feedLines}${trays}<path d="M50 48H670M50 670H670" stroke="#cbd0ce" stroke-width="12"/><path d="M59 50H660" stroke="#d25cad" stroke-width="6" opacity=".8"/><path d="M60 661H660" stroke="#31577b" stroke-width="6" opacity=".85"/><g fill="#d7d5cf" stroke="#54595b" stroke-width="6"><rect x="6" y="244" width="38" height="220" rx="15"/><rect x="676" y="244" width="38" height="220" rx="15"/></g><path d="M25 275v157m670-157v157" stroke="#b63a32" stroke-width="8"/>${bolts([30,690],[35,685],5)}`;
 add('Cultivation','hydroponic_bench_pillow_array',720,720,b,100);
}

// Enclosed rack / growth chamber panel (compact APH-like unit).
{
 const b=`<ellipse cx="355" cy="357" rx="292" ry="291" fill="#403832" opacity=".18"/><rect x="52" y="34" width="606" height="626" rx="44" fill="url(#steel)" stroke="#51595b" stroke-width="15"/><rect x="78" y="58" width="554" height="572" rx="30" fill="#eeeae1" stroke="#a7a79f" stroke-width="7"/><rect x="112" y="97" width="486" height="362" rx="22" fill="#30393c" stroke="#6f7778" stroke-width="8"/><rect x="134" y="118" width="442" height="323" rx="15" fill="#66726d" stroke="#20292b" stroke-width="7"/><rect x="146" y="130" width="418" height="14" rx="7" fill="#d25cad"/><rect x="146" y="415" width="418" height="14" rx="7" fill="#d25cad"/><path d="M171 350q50-20 83-101t81 16q46 64 90-25t90 67" fill="none" stroke="#789964" stroke-width="25" stroke-linecap="round"/><path d="M171 350q50-20 83-101t81 16q46 64 90-25t90 67" fill="none" stroke="#a7be7a" stroke-width="10" stroke-linecap="round"/><rect x="135" y="476" width="440" height="130" rx="17" fill="#4c5557" stroke="#888c88" stroke-width="6"/><rect x="154" y="493" width="252" height="86" rx="10" fill="url(#glass)"/><path d="M172 518h213m-213 20h140m-140 20h192" stroke="#9ec0c6" stroke-width="5"/><circle cx="461" cy="522" r="13" fill="#63b8a8"/><circle cx="501" cy="522" r="13" fill="#d4a244"/><circle cx="541" cy="522" r="13" fill="#b63a32"/><path d="M479 556h74" stroke="#ccd0c9" stroke-width="5"/><text x="177" y="599" font-family="Arial" font-size="17" font-weight="700" fill="#e3e2d9">SEALED GROW CHAMBER</text>${bolts([82,628],[74,620],5)}`;
 add('Cultivation','sealed_growth_chamber',710,700,b,110);
}

// Water and nutrient reservoir assembly with visible supply/return loops.
{
 const b=`<ellipse cx="410" cy="365" rx="350" ry="290" fill="#443c35" opacity=".18"/><rect x="43" y="68" width="734" height="560" rx="43" fill="url(#steel)" stroke="#545a5b" stroke-width="14"/><rect x="63" y="88" width="694" height="520" rx="30" fill="#e8e5dc" stroke="#a3a39c" stroke-width="7"/><rect x="93" y="119" width="235" height="407" rx="53" fill="#d8d6cf" stroke="#616768" stroke-width="10"/><ellipse cx="210" cy="146" rx="117" ry="30" fill="#f6f3eb" stroke="#686e6f" stroke-width="8"/><ellipse cx="210" cy="508" rx="117" ry="30" fill="#b6b7b2" stroke="#686e6f" stroke-width="8"/><rect x="110" y="267" width="201" height="183" rx="20" fill="#9bb7b5" stroke="#596365" stroke-width="7"/><path d="M118 344q95-24 184 0v90H118Z" fill="#6babb1" opacity=".8"/><path d="M118 343q95-24 184 0" fill="none" stroke="#d8ece8" stroke-width="6"/><rect x="379" y="120" width="339" height="179" rx="22" fill="#596365" stroke="#383f41" stroke-width="8"/><rect x="401" y="142" width="205" height="80" rx="12" fill="url(#glass)"/><path d="M419 161h170m-170 22h130m-130 22h147" stroke="#8cc5ca" stroke-width="5"/><circle cx="659" cy="170" r="14" fill="#60b7a5"/><circle cx="659" cy="213" r="14" fill="#d4a244"/><circle cx="659" cy="256" r="14" fill="#b63a32"/><rect x="391" y="343" width="315" height="216" rx="28" fill="#c7c6bf" stroke="#676d6e" stroke-width="8"/><circle cx="493" cy="449" r="61" fill="#666d6e" stroke="#43494b" stroke-width="10"/><circle cx="493" cy="449" r="39" fill="#a9bfbd" stroke="#e0dfd6" stroke-width="7"/><path d="M493 419v60m-29-30h58m-50-21 42 42m0-42-42 42" stroke="#42494a" stroke-width="8"/><rect x="584" y="384" width="90" height="127" rx="15" fill="#e6e2d8" stroke="#727777" stroke-width="6"/><path d="M608 405v82m24-82v82m24-82v82" stroke="#a5a69f" stroke-width="6"/><path d="M328 305h45v-62h-45m-54 249h73v55h44" fill="none" stroke="#428a9b" stroke-width="17" stroke-linejoin="round"/><path d="M328 316h30v-61h-35m-55 236h62v52h61" fill="none" stroke="#31577b" stroke-width="7" stroke-linejoin="round"/><path d="M710 350h47v223h-43" fill="none" stroke="#b63a32" stroke-width="13"/><text x="126" y="226" font-family="Arial" font-size="21" font-weight="700" fill="#4e5a5b">WATER</text><text x="403" y="326" font-family="Arial" font-size="18" font-weight="700" fill="#50585a">PUMP + FLOW CONTROL</text>${bolts([59,761],[106,590],5)}`;
 add('Water_System','water_reservoir_pump_unit',820,700,b,112);
}

// Compact nutrient cartridge rack, separated from the water tank.
{
 const b=`<ellipse cx="240" cy="248" rx="188" ry="190" fill="#433b34" opacity=".16"/><rect x="39" y="38" width="402" height="438" rx="35" fill="url(#steel)" stroke="#545a5c" stroke-width="13"/><rect x="59" y="58" width="362" height="398" rx="24" fill="#e8e5dc" stroke="#a5a49d" stroke-width="6"/><rect x="95" y="102" width="113" height="270" rx="34" fill="#d8d6cf" stroke="#666c6d" stroke-width="8"/><rect x="269" y="102" width="113" height="270" rx="34" fill="#d8d6cf" stroke="#666c6d" stroke-width="8"/><ellipse cx="151" cy="123" rx="52" ry="17" fill="#f5f3ec" stroke="#687071" stroke-width="6"/><ellipse cx="325" cy="123" rx="52" ry="17" fill="#f5f3ec" stroke="#687071" stroke-width="6"/><path d="M103 266q47-16 96 0v94h-96m173-94q47-16 96 0v94h-96" fill="#83a7a7" opacity=".82"/><path d="M105 266q47-16 92 0m83 0q47-16 92 0" fill="none" stroke="#e0efea" stroke-width="5"/><rect x="111" y="394" width="259" height="39" rx="10" fill="#4f5b5d"/><circle cx="135" cy="413" r="7" fill="#b63a32"/><circle cx="160" cy="413" r="7" fill="#d4a244"/><circle cx="185" cy="413" r="7" fill="#63b8a8"/><text x="219" y="420" font-family="Arial" font-size="15" font-weight="700" fill="#f1f0e9">NUTRIENT CARTRIDGES</text>${bolts([47,433],[47,467],4)}`;
 add('Water_System','nutrient_cartridge_rack',480,520,b,110);
}

// Environmental monitor / fans / LED and floor interactable markers.
{
 const b=`<ellipse cx="295" cy="210" rx="250" ry="172" fill="#443c35" opacity=".15"/><rect x="40" y="38" width="510" height="344" rx="31" fill="url(#steel)" stroke="#555b5c" stroke-width="13"/><rect x="61" y="59" width="468" height="302" rx="20" fill="#e8e5dc" stroke="#aaa9a1" stroke-width="6"/><rect x="89" y="91" width="283" height="156" rx="15" fill="#4a5557" stroke="#6c7271" stroke-width="6"/><rect x="108" y="110" width="245" height="117" rx="9" fill="url(#glass)"/><path d="M125 204l41-43 40 20 41-49 40 35 48-42" fill="none" stroke="#71c2c2" stroke-width="8"/><path d="M126 211h207" stroke="#d3d5d0" stroke-width="4"/><circle cx="424" cy="127" r="21" fill="#d9dbd3" stroke="#676d6d" stroke-width="6"/><path d="M424 111v32m-16-16h32m-27-11 22 22m0-22-22 22" stroke="#626869" stroke-width="5"/><circle cx="424" cy="211" r="21" fill="#d9dbd3" stroke="#676d6d" stroke-width="6"/><path d="M424 195v32m-16-16h32m-27-11 22 22m0-22-22 22" stroke="#626869" stroke-width="5"/><rect x="96" y="277" width="90" height="50" rx="9" fill="#d1d0c8" stroke="#777b79" stroke-width="4"/><rect x="203" y="277" width="90" height="50" rx="9" fill="#d1d0c8" stroke="#777b79" stroke-width="4"/><rect x="310" y="277" width="90" height="50" rx="9" fill="#d1d0c8" stroke="#777b79" stroke-width="4"/><text x="106" y="309" font-family="Arial" font-size="16" font-weight="700" fill="#3b525a">TEMP</text><text x="213" y="309" font-family="Arial" font-size="16" font-weight="700" fill="#3b525a">HUM</text><text x="317" y="309" font-family="Arial" font-size="16" font-weight="700" fill="#3b525a">CO2</text><circle cx="480" cy="301" r="13" fill="#64b7a6" stroke="#f5f0e5" stroke-width="4"/><text x="87" y="351" font-family="Arial" font-size="13" font-weight="700" fill="#737775">ENVIRONMENTAL CONTROL</text>${bolts([50,540],[48,371],4)}`;
 add('Lighting','environmental_control_panel',590,420,b,115);
}
// Dedicated operator displays for the water/nutrient loop and plant root-zone telemetry.
{
 const b=`<ellipse cx="340" cy="222" rx="311" ry="198" fill="#403832" opacity=".17"/><rect x="28" y="28" width="624" height="384" rx="31" fill="url(#steel)" stroke="#51595b" stroke-width="13"/><rect x="49" y="49" width="582" height="342" rx="20" fill="#e8e5dc" stroke="#aaa9a1" stroke-width="6"/><rect x="76" y="76" width="369" height="217" rx="15" fill="#303a3d" stroke="#727a79" stroke-width="6"/><rect x="91" y="91" width="339" height="187" rx="10" fill="url(#glass)"/><path d="M111 239h300M111 120h300M111 158h300M111 198h300" stroke="#8ba5a3" stroke-width="3" opacity=".65"/><path d="M120 225l55-28 53 13 51-55 52 26 64-52" fill="none" stroke="#67c7d1" stroke-width="7"/><path d="M120 244l55-4 53-18 51-17 52-12 64-34" fill="none" stroke="#d7b262" stroke-width="5"/><text x="104" y="117" font-family="Arial" font-size="17" font-weight="700" fill="#e4eee7">WATER LOOP TELEMETRY</text><g fill="#d2d0c8" stroke="#636a6a" stroke-width="4"><rect x="465" y="85" width="135" height="69" rx="11"/><rect x="465" y="166" width="135" height="69" rx="11"/><rect x="465" y="247" width="135" height="69" rx="11"/></g><text x="484" y="112" font-family="Arial" font-size="15" font-weight="700" fill="#465153">RESERVOIR</text><text x="484" y="137" font-family="Arial" font-size="18" font-weight="700" fill="#31577b">LEVEL / L</text><text x="484" y="193" font-family="Arial" font-size="15" font-weight="700" fill="#465153">FEED FLOW</text><text x="484" y="218" font-family="Arial" font-size="18" font-weight="700" fill="#31577b">L / MIN</text><text x="484" y="274" font-family="Arial" font-size="15" font-weight="700" fill="#465153">NUTRIENT</text><text x="484" y="299" font-family="Arial" font-size="18" font-weight="700" fill="#31577b">EC / pH</text><circle cx="111" cy="345" r="10" fill="#64b7a6"/><circle cx="142" cy="345" r="10" fill="#d4a244"/><circle cx="173" cy="345" r="10" fill="#b63a32"/><text x="202" y="351" font-family="Arial" font-size="16" font-weight="700" fill="#51595a">SUPPLY · RETURN · FILTER</text>${bolts([38,642],[38,402],4)}`;
 add('Water_System','water_loop_monitor_panel',680,440,b,120);
}
{
 const b=`<ellipse cx="340" cy="222" rx="311" ry="198" fill="#403832" opacity=".17"/><rect x="28" y="28" width="624" height="384" rx="31" fill="url(#steel)" stroke="#51595b" stroke-width="13"/><rect x="49" y="49" width="582" height="342" rx="20" fill="#e8e5dc" stroke="#aaa9a1" stroke-width="6"/><rect x="75" y="75" width="345" height="225" rx="15" fill="#303a3d" stroke="#727a79" stroke-width="6"/><rect x="91" y="91" width="313" height="193" rx="10" fill="url(#glass)"/><path d="M111 251h274M111 126h274M111 168h274M111 210h274" stroke="#8ba5a3" stroke-width="3" opacity=".65"/><path d="M122 229l49-44 46 18 52-58 44 31 62-46" fill="none" stroke="#78ca9c" stroke-width="7"/><text x="106" y="117" font-family="Arial" font-size="16" font-weight="700" fill="#e4eee7">CROP / ROOT-ZONE SENSORS</text><g fill="#d2d0c8" stroke="#636a6a" stroke-width="4"><rect x="440" y="75" width="164" height="65" rx="10"/><rect x="440" y="151" width="164" height="65" rx="10"/><rect x="440" y="227" width="164" height="65" rx="10"/></g><text x="459" y="101" font-family="Arial" font-size="15" font-weight="700" fill="#465153">AIR</text><text x="459" y="126" font-family="Arial" font-size="18" font-weight="700" fill="#31577b">T · RH · CO2</text><text x="459" y="177" font-family="Arial" font-size="15" font-weight="700" fill="#465153">ROOT ZONE</text><text x="459" y="202" font-family="Arial" font-size="18" font-weight="700" fill="#31577b">MOISTURE · O2</text><text x="459" y="253" font-family="Arial" font-size="15" font-weight="700" fill="#465153">CANOPY</text><text x="459" y="278" font-family="Arial" font-size="18" font-weight="700" fill="#31577b">LIGHT · TEMP</text><circle cx="108" cy="345" r="10" fill="#64b7a6"/><circle cx="139" cy="345" r="10" fill="#d4a244"/><circle cx="170" cy="345" r="10" fill="#b63a32"/><text x="199" y="351" font-family="Arial" font-size="16" font-weight="700" fill="#51595a">SENSORS · IMAGING · AIR FILTER</text>${bolts([38,642],[38,402],4)}`;
 add('Lighting','crop_root_zone_monitor_panel',680,440,b,120);
}
{
 let blades=''; for(let i=0;i<8;i++) blades+=`<path d="M0-12C45-75 95-38 42 1C25 14 9 13 0 0Z" transform="rotate(${i*45})" fill="#c8c8c1" stroke="#555d5e" stroke-width="4"/>`;
 const b=`<ellipse cx="160" cy="160" rx="140" ry="137" fill="#443b34" opacity=".16"/><circle cx="160" cy="160" r="132" fill="url(#steel)" stroke="#555c5e" stroke-width="13"/><circle cx="160" cy="160" r="108" fill="#dcdad2" stroke="#989a96" stroke-width="6"/><circle cx="160" cy="160" r="82" fill="#626a6b" stroke="#404749" stroke-width="7"/><g transform="translate(160 160)">${blades}<circle r="28" fill="#b8bab4" stroke="#4a5152" stroke-width="7"/><circle r="10" fill="#31577b"/></g>${bolts([160],[48,272],5)}`;
 add('Lighting','circulation_fan_filter',320,320,b,120);
}
{
 const b=`<ellipse cx="260" cy="55" rx="244" ry="37" fill="#33393a" opacity=".4"/><rect x="26" y="34" width="468" height="42" rx="19" fill="#333a3b" stroke="#585d5d" stroke-width="7"/><rect x="42" y="42" width="436" height="23" rx="11" fill="#d25cad"/><path d="M55 49h408" stroke="#f7caed" stroke-width="4" opacity=".72"/>${bolts([48,472],[55],4)}`;
 add('Lighting','led_growth_bar',520,100,b,120);
}
{
 const b=`<ellipse cx="180" cy="130" rx="165" ry="117" fill="#413a34" opacity=".18"/><rect x="24" y="21" width="312" height="227" rx="34" fill="url(#steel)" stroke="#565b5d" stroke-width="12"/><rect x="45" y="42" width="270" height="185" rx="23" fill="#edeae2" stroke="#aaa9a1" stroke-width="6"/><rect x="84" y="60" width="192" height="135" rx="17" fill="#c4c4be" stroke="#62696b" stroke-width="8"/><circle cx="180" cy="126" r="53" fill="#e1ded6" stroke="#555d5f" stroke-width="8"/><circle cx="180" cy="126" r="40" fill="url(#glass)" stroke="#b9bbb6" stroke-width="7"/><path d="M155 103q24-19 46-8" stroke="#e4eeee" stroke-width="7" opacity=".65"/><rect x="148" y="7" width="64" height="26" rx="9" fill="#b63a32" stroke="#565b5d" stroke-width="5"/><rect x="148" y="235" width="64" height="26" rx="9" fill="#31577b" stroke="#565b5d" stroke-width="5"/>${bolts([36,324],[34,237],5)}`;
 add('Airlock','pressure_hatch_topdown',360,275,b,115);
}

// Plant artwork: species-specific top-down silhouettes and six stages with consistent centers.
const species=[
 {key:'romaine_red',dark:'#563c52',mid:'#7e526d',light:'#bb7182',vein:'#e1aa9d',leaf:[32,39,44]},
 {key:'lettuce_green',dark:'#35553b',mid:'#5f824d',light:'#91ae68',vein:'#d2d89b',leaf:[34,42,47]},
 {key:'mizuna',dark:'#334f37',mid:'#628858',light:'#a4b875',vein:'#e0dfa8',leaf:[33,49,58]},
 {key:'dwarf_wheat',dark:'#705b39',mid:'#9e9257',light:'#c7b76d',vein:'#ead78b',leaf:[29,36,42]}
];
const stages=['seed','germination','sprout','young','developed','mature'];
function leafPath(cx,cy,angle,len,width,kind,fill,vein){
 if(kind==='mizuna') return `<g transform="rotate(${angle} ${cx} ${cy})"><path d="M${cx} ${cy}C${cx-width} ${cy-len*.18} ${cx-width*.72} ${cy-len*.78} ${cx+4} ${cy-len}C${cx+width*.62} ${cy-len*.78} ${cx+width} ${cy-len*.26} ${cx} ${cy}Z" fill="${fill}" stroke="#4e6650" stroke-width="3"/><path d="M${cx} ${cy-5}l4 ${-len+12}m-2 ${len*.25}l-13 -${len*.22}m14 -${len*.27}l14 -${len*.17}" fill="none" stroke="${vein}" stroke-width="3" stroke-linecap="round"/></g>`;
 if(kind==='dwarf_wheat') return `<g transform="rotate(${angle} ${cx} ${cy})"><path d="M${cx} ${cy}Q${cx+width*.55} ${cy-len*.45} ${cx} ${cy-len}Q${cx-width*.5} ${cy-len*.46} ${cx} ${cy}Z" fill="${fill}" stroke="#675a40" stroke-width="3"/><path d="M${cx} ${cy-3}v${-len+8}" stroke="${vein}" stroke-width="3"/></g>`;
 return `<g transform="rotate(${angle} ${cx} ${cy})"><path d="M${cx} ${cy}C${cx-width*.68} ${cy-len*.15} ${cx-width} ${cy-len*.7} ${cx} ${cy-len}C${cx+width} ${cy-len*.72} ${cx+width*.82} ${cy-len*.19} ${cx} ${cy}Z" fill="${fill}" stroke="#4c6247" stroke-width="3"/><path d="M${cx} ${cy-5}Q${cx-2} ${cy-len*.5} ${cx} ${cy-len+10}M${cx} ${cy-len*.34}l${-width*.58} ${-len*.2}m${width*.6} ${len*.34}l${width*.58} ${-len*.21}" fill="none" stroke="${vein}" stroke-width="3" stroke-linecap="round"/></g>`;
}
function plantSvg(sp,stageIndex){
 const c={x:180,y:180};
 if(stageIndex===0){return `<ellipse cx="180" cy="207" rx="52" ry="20" fill="#322c29" opacity=".2"/><path d="M125 177q55-37 110 0l-7 25q-48 20-96 0Z" fill="#826d58" stroke="#62574d" stroke-width="6"/><ellipse cx="180" cy="177" rx="55" ry="16" fill="#bba688" stroke="#66594b" stroke-width="6"/><ellipse cx="180" cy="175" rx="13" ry="9" fill="#dbc797" stroke="#776749" stroke-width="4"/>`}
 if(stageIndex===1){return `<ellipse cx="180" cy="211" rx="76" ry="24" fill="#322c29" opacity=".18"/><path d="M110 178q70-42 140 0v26q-68 31-140 0Z" fill="#876f57" stroke="#65564a" stroke-width="6"/><ellipse cx="180" cy="178" rx="70" ry="19" fill="#bfa98a" stroke="#685a4c" stroke-width="6"/><path d="M180 177q-8-42 11-55q17-8 22 8q3 15-18 27q-14 8-15 20Z" fill="#f1e5c8" stroke="#5f7650" stroke-width="5"/><path d="M181 178q8-26 26-37" fill="none" stroke="#7e9e64" stroke-width="7"/>`}
 const count=stageIndex===2?5:stageIndex===3?8:stageIndex===4?10:12;
 const radius=stageIndex===2?34:stageIndex===3?46:stageIndex===4?60:72;
 let leaves='';
 for(let i=0;i<count;i++){
  const a=(360/count)*i+(sp.key==='mizuna'?13:0), len=radius*(.82+((i*17)%5)*.075), width=sp.key==='mizuna'?len*.32:len*.46;
  const fill=[sp.dark,sp.mid,sp.light][i%3]; leaves+=leafPath(c.x,c.y,a,len,width,sp.key,fill,sp.vein);
 }
 let inner='';
 const innerCount=stageIndex===2?3:stageIndex===3?5:stageIndex===4?7:8;
 for(let i=0;i<innerCount;i++){
  const a=i*360/innerCount+22, len=radius*(.49+((i*13)%4)*.045), width=sp.key==='mizuna'?len*.31:len*.44;
  inner+=leafPath(c.x,c.y,a,len,width,sp.key,[sp.mid,sp.light,sp.dark][i%3],sp.vein);
 }
 const pillowRx=stageIndex===2?80:stageIndex===3?94:104, pillowRy=stageIndex===2?23:stageIndex===3?28:32, pillowBottom=stageIndex===2?214:stageIndex===3?229:237;
 const pillow=`<ellipse cx="180" cy="207" rx="${pillowRx+7}" ry="34" fill="#322c29" opacity=".19"/><path d="M${180-pillowRx} 172q${pillowRx} -35 ${pillowRx*2} 0v${pillowBottom-172}q-${pillowRx} 38 -${pillowRx*2} 0Z" fill="#88745d" stroke="#5e5348" stroke-width="6"/><ellipse cx="180" cy="172" rx="${pillowRx}" ry="${pillowRy}" fill="#bca789" stroke="#695b4b" stroke-width="6"/><path d="M${180-pillowRx*.72} 174q${pillowRx*.72} -21 ${pillowRx*1.44} 0" fill="none" stroke="#e1d0b1" stroke-width="4" opacity=".8"/><path d="M${180+pillowRx*.72} 174h18v20h-18" fill="#e6e2d8" stroke="#6e7775" stroke-width="4"/><circle cx="${180+pillowRx*.92}" cy="184" r="5" fill="#4b9db0" stroke="#f2eee5" stroke-width="2"/><path d="M${180-pillowRx*.72} 210h${pillowRx*1.44}" stroke="#675a4c" stroke-width="3" opacity=".7"/>`;
 return `<ellipse cx="180" cy="219" rx="126" ry="35" fill="#332e29" opacity=".2"/>${pillow}<g>${leaves}${inner}</g><circle cx="180" cy="180" r="${stageIndex===2?11:13}" fill="#c0c88b" stroke="#52684b" stroke-width="4"/>`;
}
function plantSvgHighRes(sp,stageIndex){return `<g transform="scale(2)">${plantSvg(sp,stageIndex)}</g>`;}
for(const sp of species){
 for(let stage=0;stage<stages.length;stage++){
  add('Plants',`plant_${sp.key}_${stages[stage]}`,720,720,plantSvgHighRes(sp,stage),400);
 }
}

async function main(){
 fs.mkdirSync(previewDir,{recursive:true});
 for(const x of items){
  const folder=path.join(art,x.folder);fs.mkdirSync(folder,{recursive:true});
  const text=svg(x.w,x.h,x.body);const dest=path.join(folder,`${x.name}.png`);
  await sharp(Buffer.from(text)).png().toFile(dest);
 }
 // Review board draws the same three-room footprint and crop-bed layout as Unity.
 const width=1800,height=720,roomLeft=746,roomTop=228,roomWidth=680,roomHeight=335,roomScale=roomWidth/26;
 const centerX=roomLeft+roomWidth/2,centerY=roomTop+roomHeight/2;
 const board=`<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}"><rect width="100%" height="100%" fill="#282e31"/><text x="48" y="58" fill="#f0eee8" font-family="Arial" font-size="31" font-weight="700">AGRICULTURAL MODULE | SCENE PREVIEW</text><text x="50" y="91" fill="#c2b8a8" font-family="Arial" font-size="18">Three connected pressure rooms: decontamination, crop production and systems control</text><rect x="32" y="125" width="680" height="575" rx="20" fill="#353d40"/><rect x="728" y="125" width="714" height="575" rx="20" fill="#353d40"/><rect x="1458" y="125" width="310" height="575" rx="20" fill="#353d40"/><text x="56" y="164" fill="#d9c9af" font-family="Arial" font-size="20" font-weight="700">SURFACE MODULE</text><text x="752" y="153" fill="#d9c9af" font-family="Arial" font-size="20" font-weight="700">THREE-ROOM PRESSURIZED INTERIOR</text><text x="1480" y="164" fill="#d9c9af" font-family="Arial" font-size="19" font-weight="700">CROP STAGES</text></svg>`;
 const comp=[{input:Buffer.from(board),left:0,top:0}];
 const read=async(folder,name,w,h)=>sharp(path.join(art,folder,`${name}.png`)).resize({width:w,height:h,fit:'contain',background:{r:0,g:0,b:0,alpha:0}}).png().toBuffer();
 comp.push({input:await read('Exterior','Agricultural_Module_Exterior',620,445),left:62,top:196});
 comp.push({input:await sharp(path.join(art,'Interior','interior_floor_walls.png')).resize(roomWidth,roomHeight,{fit:'fill'}).png().toBuffer(),left:roomLeft,top:roomTop});
 const benchWidth=Math.round((720/100)*.64*roomScale),benchHeight=Math.round((720/100)*.55*roomScale);
 for(const bx of [-5.8,0,5.8]) for(const cy of [-3.2,3.2]){
  const benchX=centerX+bx*roomScale,benchY=centerY-cy*roomScale;
  comp.push({input:await sharp(path.join(art,'Cultivation','hydroponic_bench_pillow_array.png')).resize(benchWidth,benchHeight,{fit:'fill'}).png().toBuffer(),left:Math.round(benchX-benchWidth/2),top:Math.round(benchY-benchHeight/2)});
  const south=cy<0;
  for(let row=0;row<4;row++)for(let col=0;col<3;col++)for(let pocket=0;pocket<6;pocket++){
   const trayIndex=row*3+col,offset=bx>1?2:bx===0?1:0;
   const sp=species[(trayIndex+pocket+(south?1:0)+offset)%species.length],st=stages[(row+col+pocket+(south?1:0)+offset)%stages.length];
   const svgX=89+col*208+(pocket%3)*48,svgY=90+row*151+Math.floor(pocket/3)*48;
   const worldX=bx+(svgX-360)/100*.70,worldY=cy+(360-svgY)/100*.55;
   const size=Math.round(720/400*.58*roomScale),input=await sharp(path.join(art,'Plants',`plant_${sp.key}_${st}.png`)).resize(size,size).png().toBuffer();
   const px=centerX+worldX*roomScale,py=centerY-worldY*roomScale;
   comp.push({input,left:Math.round(px-size/2),top:Math.round(py-size/2)});
  }
 }
 const utility=[
  ['Water_System','water_reservoir_pump_unit',10.8,-2.9,.12,820,700],
  ['Water_System','nutrient_cartridge_rack',12.15,-3.55,.11,480,520],
  ['Lighting','environmental_control_panel',12.0,3.35,.12,590,420],
  ['Lighting','crop_root_zone_monitor_panel',12.0,2.0,.12,680,440],
  ['Water_System','water_loop_monitor_panel',12.0,.65,.12,680,440],
  ['Cultivation','sealed_growth_chamber',12.0,-.75,.11,710,700],
  ['Lighting','circulation_fan_filter',10.8,.85,.14,320,320],
  ['Airlock','airlock_topdown',-11,0,.43,720,620],
  ['Airlock','airlock_decon_grate',-11,-.72,.30,640,340],
  ['Airlock','airlock_cycle_console',-10.1,2.15,.21,420,270]
 ];
 for(const [folder,name,x,y,s,nw,nh] of utility){
  const ppu={water_reservoir_pump_unit:112,nutrient_cartridge_rack:110,environmental_control_panel:115,water_loop_monitor_panel:120,crop_root_zone_monitor_panel:120,sealed_growth_chamber:110,circulation_fan_filter:120,airlock_topdown:110,airlock_decon_grate:130,airlock_cycle_console:110}[name];
  const w=Math.round(nw/ppu*s*roomScale),h=Math.round(nh/ppu*s*roomScale),input=await sharp(path.join(art,folder,`${name}.png`)).resize(w,h,{fit:'fill'}).png().toBuffer();
  const px=centerX+x*roomScale,py=centerY-y*roomScale;comp.push({input,left:Math.round(px-w/2),top:Math.round(py-h/2)});
 }
 const gate=await sharp(path.join(art,'Airlock','airlock_pressure_gate_side.png')).resize(10,54).png().toBuffer();
 for(const x of [-13,-9]) comp.push({input:gate,left:Math.round(centerX+x*roomScale-15),top:Math.round(centerY-27)}); for(let s=0;s<4;s++)for(let st=0;st<6;st++){
  const input=await sharp(path.join(art,'Plants',`plant_${species[s].key}_${stages[st]}.png`)).resize(54,54).png().toBuffer();
  comp.push({input,left:1481+st*48,top:197+s*116});
 }
 await sharp({create:{width,height,channels:4,background:{r:0,g:0,b:0,alpha:1}}}).composite(comp).png().toFile(path.join(previewDir,'Agricultural_Module_Overview.png'));
 console.log(`Rendered ${items.length} transparent sprites under ${art}`);
}
main().catch(e=>{console.error(e);process.exit(1);});
