import fs from 'node:fs';
import path from 'node:path';
import zlib from 'node:zlib';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const outDir = path.resolve(__dirname, '../../../My project (4)/Assets/Art/VFX/PlasmaClouds/Sprites');
fs.mkdirSync(outDir, { recursive: true });

function crc32(buf) {
  let crc = 0xffffffff;
  for (let i = 0; i < buf.length; i++) {
    crc ^= buf[i];
    for (let k = 0; k < 8; k++) {
      crc = (crc >>> 1) ^ (crc & 1 ? 0xedb88320 : 0);
    }
  }
  return (crc ^ 0xffffffff) >>> 0;
}

function makeChunk(type, data) {
  const len = Buffer.alloc(4);
  len.writeUInt32BE(data.length, 0);
  const t = Buffer.from(type, 'ascii');
  const crcBuf = Buffer.concat([t, data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(crcBuf), 0);
  return Buffer.concat([len, t, data, crc]);
}

function writePng(filename, width, height, rgba) {
  const stride = width * 4;
  const raw = Buffer.alloc((stride + 1) * height);
  for (let y = 0; y < height; y++) {
    const rowStart = y * (stride + 1);
    raw[rowStart] = 0;
    rgba.copy(raw, rowStart + 1, y * stride, (y + 1) * stride);
  }
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr[8] = 8;
  ihdr[9] = 6;
  ihdr[10] = 0;
  ihdr[11] = 0;
  ihdr[12] = 0;

  const compressed = zlib.deflateSync(raw, { level: 6 });
  const png = Buffer.concat([
    Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]),
    makeChunk('IHDR', ihdr),
    makeChunk('IDAT', compressed),
    makeChunk('IEND', Buffer.alloc(0))
  ]);
  const outPath = path.join(outDir, filename);
  fs.writeFileSync(outPath, png);
  console.log(`Generated ${filename} (${width}x${height})`);
}

function hash2(ix, iy, seed) {
  let h = (ix * 374761393 + iy * 668265263 + seed * 1442695041) | 0;
  h = ((h ^ (h >>> 13)) * 1274126177) | 0;
  return ((h ^ (h >>> 16)) >>> 0) / 4294967295;
}

function smoothstep(t) {
  const c = Math.max(0, Math.min(1, t));
  return c * c * (3 - 2 * c);
}

function lerp(a, b, t) {
  return a + (b - a) * t;
}

function valueNoise(u, v, freq, seed) {
  const x = u * freq;
  const y = v * freq;
  const ix = Math.floor(x);
  const iy = Math.floor(y);
  const fx = smoothstep(x - ix);
  const fy = smoothstep(y - iy);

  const n00 = hash2(ix, iy, seed);
  const n10 = hash2(ix + 1, iy, seed);
  const n01 = hash2(ix, iy + 1, seed);
  const n11 = hash2(ix + 1, iy + 1, seed);

  return lerp(lerp(n00, n10, fx), lerp(n01, n11, fx), fy);
}

function fbm(u, v, baseFreq, octaves, seed) {
  let sum = 0;
  let amp = 0.5;
  let norm = 0;
  let freq = baseFreq;
  for (let o = 0; o < octaves; o++) {
    sum += amp * valueNoise(u, v, freq, seed + o * 97);
    norm += amp;
    amp *= 0.5;
    freq *= 2;
  }
  return sum / norm;
}

// Generador 100% libre de bordes cuadrados: máscara radial elíptica suave + lóbulos gaussianos asimétricos + fBm
function generateOrganicCloud(filename, w, h, seed, lobes, opts = {}) {
  const softness = opts.softness ?? 0.65;
  const warpAmp = opts.warpAmp ?? 0.18;
  const maxOpacity = opts.maxOpacity ?? 1.0;

  const buf = Buffer.alloc(w * h * 4);
  for (let py = 0; py < h; py++) {
    const ny = (py + 0.5) / h;
    for (let px = 0; px < w; px++) {
      const nx = (px + 0.5) / w;

      // Máscara radial puramente elíptica: garantiza alpha=0 antes de acercarse a CUALQUIER borde del PNG (cero cuadrados)
      const ex = (nx - 0.5) / 0.44;
      const ey = (ny - 0.5) / 0.44;
      const radialDist = Math.sqrt(ex * ex + ey * ey);
      const radialMask = 1.0 - smoothstep((radialDist - 0.55) / 0.43);

      if (radialMask <= 0.001) {
        continue; // Buffer ya inicializado en 0 RGBA transparente
      }

      const wx = (fbm(nx, ny, 3.2, 3, seed + 17) - 0.5) * warpAmp;
      const wy = (fbm(nx + 0.4, ny + 0.2, 3.2, 3, seed + 53) - 0.5) * warpAmp;
      const u = nx + wx;
      const v = ny + wy;

      let shape = 0;
      for (const [cx, cy, rx, ry, rotDeg, weight] of lobes) {
        const rad = (rotDeg * Math.PI) / 180;
        const cosR = Math.cos(rad);
        const sinR = Math.sin(rad);
        const ox = u - cx;
        const oy = v - cy;
        const rxCoord = (ox * cosR + oy * sinR) / rx;
        const ryCoord = (-ox * sinR + oy * cosR) / ry;
        const d2 = rxCoord * rxCoord + ryCoord * ryCoord;
        shape += Math.exp(-d2 * 2.2) * weight * 0.58;
      }
      shape = Math.min(1.25, shape);

      const n1 = fbm(nx, ny, 4.2, 4, seed);
      const n2 = fbm(nx + 0.37, ny + 0.61, 8.5, 3, seed + 500);

      const modulated = shape * lerp(0.42, 1.38, n1) * radialMask;
      const density = Math.max(0, Math.min(1, (modulated - 0.14) / softness));
      const alpha = smoothstep(density) * radialMask * maxOpacity;

      const highlight = Math.max(0, Math.min(1, n2 * 0.68 + shape * 0.32 - 0.08));
      const r = Math.round(lerp(132, 214, highlight));
      const g = Math.round(lerp(48, 122, highlight));
      const b = Math.round(lerp(208, 252, highlight));

      const idx = (py * w + px) * 4;
      buf[idx] = r;
      buf[idx + 1] = g;
      buf[idx + 2] = b;
      buf[idx + 3] = Math.round(alpha * 255);
    }
  }
  writePng(filename, w, h, buf);
}

// 8 siluetas orgánicas distintas (todas con bordes 100% libres de marco cuadrado)
generateOrganicCloud('cme_cloud_large.png', 512, 384, 101, [
  [0.48, 0.54, 0.25, 0.21, -8, 1.00],
  [0.32, 0.56, 0.17, 0.16, 12, 0.88],
  [0.65, 0.51, 0.18, 0.17, -15, 0.92],
  [0.44, 0.39, 0.16, 0.15, 6, 0.82],
  [0.57, 0.64, 0.16, 0.13, 10, 0.74]
], { softness: 0.60 });

generateOrganicCloud('cme_cloud_medium.png', 448, 352, 202, [
  [0.51, 0.52, 0.22, 0.20, 14, 1.00],
  [0.36, 0.57, 0.15, 0.15, -10, 0.86],
  [0.64, 0.46, 0.16, 0.14, 18, 0.84],
  [0.47, 0.39, 0.14, 0.13, -6, 0.76]
], { softness: 0.58 });

generateOrganicCloud('cme_cloud_wispy.png', 576, 320, 303, [
  [0.50, 0.50, 0.30, 0.16, -5, 0.96],
  [0.31, 0.54, 0.18, 0.12, 8, 0.82],
  [0.68, 0.46, 0.19, 0.12, -10, 0.84],
  [0.46, 0.41, 0.17, 0.11, 4, 0.74]
], { softness: 0.64, warpAmp: 0.22 });

generateOrganicCloud('cme_vanguard_wisp.png', 576, 320, 404, [
  [0.50, 0.50, 0.29, 0.14, 12, 0.95],
  [0.30, 0.44, 0.18, 0.11, 18, 0.86],
  [0.69, 0.57, 0.18, 0.11, 10, 0.84],
  [0.52, 0.58, 0.15, 0.12, -8, 0.72]
], { softness: 0.66, warpAmp: 0.22 });

generateOrganicCloud('cme_plasma_wave.png', 448, 512, 505, [
  [0.52, 0.50, 0.22, 0.27, -6, 1.00],
  [0.42, 0.34, 0.16, 0.17, 15, 0.90],
  [0.58, 0.66, 0.17, 0.18, -12, 0.90],
  [0.38, 0.56, 0.14, 0.15, 8, 0.82]
], { softness: 0.62, warpAmp: 0.20 });

generateOrganicCloud('cme_foreground_band.png', 576, 384, 606, [
  [0.46, 0.52, 0.27, 0.18, 6, 0.96],
  [0.64, 0.48, 0.19, 0.16, -14, 0.88],
  [0.33, 0.46, 0.17, 0.14, 10, 0.82],
  [0.54, 0.62, 0.18, 0.13, -4, 0.78]
], { softness: 0.64 });

generateOrganicCloud('cme_interference_arc.png', 512, 384, 707, [
  [0.48, 0.48, 0.25, 0.17, -18, 0.96],
  [0.62, 0.58, 0.17, 0.14, 22, 0.86],
  [0.35, 0.40, 0.16, 0.13, -10, 0.82],
  [0.51, 0.36, 0.14, 0.11, 8, 0.72]
], { softness: 0.65, warpAmp: 0.21 });

generateOrganicCloud('cme_mist_overlay.png', 512, 512, 808, [
  [0.50, 0.50, 0.29, 0.28, 12, 0.92],
  [0.38, 0.42, 0.18, 0.18, -20, 0.78],
  [0.62, 0.58, 0.18, 0.18, 16, 0.78]
], { softness: 0.78, warpAmp: 0.20, maxOpacity: 0.85 });
