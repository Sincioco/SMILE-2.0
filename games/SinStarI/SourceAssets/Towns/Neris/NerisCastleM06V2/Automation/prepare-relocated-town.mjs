// Write a versioned portable document; live installation is a separate step after graceful close.
import fs from 'node:fs';
import { createHash } from 'node:crypto';
const root = 'D:/Projects/Sin-Star-I-Assets/Neris-Castle/native-integration';
const revision = process.argv[2] || 'r001';
if (!/^r\d{3}$/.test(revision)) throw Error('Invalid revision');
const doc = JSON.parse(fs.readFileSync(root + '/relocated-town-document-' + revision + '.json'));
const fingerprint = fs.readFileSync('D:/SMILE 2.0/tools/Character3DViewer/TownCatalogData.smile', 'utf8').match(/FINGERPRINT = "([a-f0-9]+)"/)[1];
const data = [84, 87, 78, doc.sun.length === 9 ? 2 : 1];
function integer(input) {
  let value = BigInt(input);
  value = value < 0n ? -2n * value - 1n : 2n * value;
  do { const digit = Number(value % 128n); value /= 128n; data.push(digit + (value > 0n ? 128 : 0)); } while (value > 0n);
}
const precise = value => integer(Math.round(value * 1e6));
function name(value) { const chars = [...value]; integer(chars.length); for (const c of chars) integer(c.codePointAt(0)); }
name(fingerprint); name(doc.name); data.push(0);
integer(doc.columns); integer(doc.rows); precise(doc.cell_size);
doc.xs.forEach(precise); doc.zs.forEach(precise);
for (let i = 0; i < doc.cells.length; i += 16) {
  let packed = 0n;
  for (let j = 0; j < 16 && i + j < doc.cells.length; j++) packed += BigInt(doc.cells[i + j]) << BigInt(j * 3);
  integer(packed);
}
integer(doc.items.length);
for (const item of doc.items) {
  integer(item.identity); integer(item.template); integer(item.source);
  [...item.position, ...item.scale, item.yaw].forEach(precise);
}
doc.sun.slice(0, 5).forEach(integer); precise(doc.sun[5]); precise(doc.sun[6]); data.push(doc.sun[7]);
if (doc.sun.length === 9) data.push(doc.sun[8] + 1);
const payload = Buffer.from(data), header = Buffer.alloc(44);
header.write('SMD4'); header.writeUInt32LE(1, 4); header.writeUInt32LE(payload.length, 8);
createHash('sha256').update(payload).digest().copy(header, 12);
const output = root + '/Neris-Town-Royal-Castle-' + revision + '.town';
fs.writeFileSync(output, Buffer.concat([header, payload]), { flag: 'wx' });
console.log(JSON.stringify({ output, bytes: payload.length + 44, items: doc.items.length, tripoInstances: doc.items.filter(i => i.template === 6).length }));
