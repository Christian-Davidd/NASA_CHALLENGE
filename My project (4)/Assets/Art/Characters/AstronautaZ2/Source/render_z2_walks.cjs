const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const sharp = require('sharp');

const root = path.resolve(__dirname, '..');
const poses = path.join(__dirname, 'poses');
const sprites = path.join(root, 'Sprites');
const metaTemplate = fs.readFileSync(
  path.join(sprites, 'char_astronauta_frente_neutro_z2_v01.png.meta'), 'utf8'
);
const names = [
  'char_astronauta_frente_caminar_01_z2_v01',
  'char_astronauta_frente_caminar_02_z2_v01',
  'char_astronauta_espalda_caminar_01_z2_v01',
  'char_astronauta_espalda_caminar_02_z2_v01',
];

async function main() {
  for (const name of names) {
    const source = path.join(poses, `${name}.svg`);
    const target = path.join(sprites, `${name}.png`);
    const meta = path.join(sprites, `${name}.png.meta`);
    await sharp(fs.readFileSync(source)).png().toFile(target);
    // Keep Unity identities stable when regenerating the artwork.
    const generatedMeta = fs.existsSync(meta)
      ? fs.readFileSync(meta, 'utf8')
      : metaTemplate
          .replace(/^guid: [0-9a-f]{32}$/m, crypto.randomUUID().replaceAll('-', ''))
          .replace(/^    spriteID: [0-9a-f]{32}$/m, crypto.randomUUID().replaceAll('-', ''));
    fs.writeFileSync(meta, generatedMeta, 'utf8');
  }
  console.log(`Rendered ${names.length} front/back walking sprites into ${sprites}`);
}

main().catch(error => { console.error(error); process.exitCode = 1; });
