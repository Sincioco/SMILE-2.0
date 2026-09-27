// Patch terrain cells only in a new portable town; preserve every assembly and road.
import fs from 'node:fs';
import crypto from 'node:crypto';
const [input, output, auditPath] = process.argv.slice(2);
const hash = bytes => crypto.createHash('sha256').update(bytes).digest();
const raw = fs.readFileSync(input), payload = raw.subarray(44);
if (raw.toString('ascii', 0, 4) !== 'SMD4' || raw.readUInt32LE(4) !== 1 ||
    raw.readUInt32LE(8) !== payload.length || !hash(payload).equals(raw.subarray(12, 44)))
  throw Error('Invalid town envelope');
if (!payload.subarray(0, 4).equals(Buffer.from([84, 87, 78, 1]))) throw Error('Invalid town payload');
let at = 4;
function readInteger() {
  let n = 0n;
  for (let i = 0; i < 8; i++) {
    const byte = payload[at++];
    n |= BigInt(byte & 127) << BigInt(i * 7);
    if (byte < 128) return Number(n & 1n ? -(n >> 1n) - 1n : n >> 1n);
  }
  throw Error('Invalid integer');
}
const readName = () => Array.from({length: readInteger()}, () => String.fromCodePoint(readInteger())).join('');
const fingerprint = readName(), name = readName();
if (!name.startsWith('Neris Town')) throw Error('Not a Neris document');
at++; // Preserve dirty flag and all other non-terrain bytes exactly.
const cols = readInteger(), rows = readInteger(), size = readInteger() / 1e6;
const xs = Array.from({length: cols + 1}, () => readInteger() / 1e6);
const zs = Array.from({length: rows + 1}, () => readInteger() / 1e6);
const start = at, cells = [];
for (let i = 0; i < Math.ceil(cols * rows / 16); i++) {
  let packed = BigInt(readInteger());
  for (let j = 0; j < 16; j++) { cells.push(Number(packed & 7n)); packed >>= 3n; }
}
const end = at, before = [...cells];
let groundAdded = 0, roadAdded = 0;
for (let z = 0; z < rows; z++) for (let x = 0; x < cols; x++) {
  const cx = (xs[x] + xs[x + 1]) / 2, cz = (zs[z] + zs[z + 1]) / 2, index = z * cols + x;
  const castleMoat = cx >= -4840 && cx < -2460 && cz >= -2280 && cz < 542;
  if (cx >= xs[0] && cx < -2400 && !castleMoat && cells[index] === 2) {
    cells[index] = 1; groundAdded++;
  }
  const mainRoad = (cx >= -3740 && cx <= -2300 && cz >= -2870 && cz <= -2690) ||
    (cx >= -3740 && cx <= -3560 && cz >= -2780 && cz < -2278);
  if (mainRoad && cells[index] === 1) { cells[index] = 3; roadAdded++; }
}
if (!groundAdded || !roadAdded) throw Error('Expected new ground and road');
if (before.some((v, i) => v === 3 && cells[i] !== 3)) throw Error('An existing road changed');
function encodeInteger(value) {
  let n = BigInt(value); n = n < 0n ? -n * 2n - 1n : n * 2n;
  const bytes = [];
  do { const digit = Number(n & 127n); n >>= 7n; bytes.push(digit + (n ? 128 : 0)); } while (n);
  return Buffer.from(bytes);
}
const packs = [];
for (let i = 0; i < cells.length; i += 16) {
  let n = 0n;
  for (let j = 0; j < 16; j++) n |= BigInt(cells[i + j]) << BigInt(j * 3);
  packs.push(encodeInteger(n));
}
const revised = Buffer.concat([payload.subarray(0, start), ...packs, payload.subarray(end)]);
const header = Buffer.alloc(44); header.write('SMD4'); header.writeUInt32LE(1, 4);
header.writeUInt32LE(revised.length, 8); hash(revised).copy(header, 12);
fs.writeFileSync(output, Buffer.concat([header, revised]), {flag: 'wx'});
const report = {name, fingerprint, cols, rows, size, input, output,
  inputSha256: hash(raw).toString('hex'), outputSha256: hash(fs.readFileSync(output)).toString('hex'),
  groundAdded, roadAdded, originalRoadsPreserved: true, nonTerrainBytesPreserved: true,
  grassHeight: 20.9, roadHeight: 23.12, castleDeckHeight: 23.12,
  castleOrigin: {x: -3650, y: 23.12, z: -852}, bridgeTip: {x: -3650, y: 23.12, z: -2280},
  landBounds: {x: [xs[0], xs.at(-1)], z: [zs[0], zs.at(-1)]},
  note: 'West land extends to the rectangular map edges. Castle moat, town canals, all assemblies, lighting, catalog identity and existing roads are preserved.'};
fs.writeFileSync(auditPath, JSON.stringify(report, null, 2) + '\n', {flag: 'wx'});
console.log(JSON.stringify(report, null, 2));
