// Run only after the native Viewer has closed normally and its working copy is saved.
import fs from 'node:fs';
import path from 'node:path';
import {execFileSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const saveRoot = execFileSync('pwsh', ['-NoProfile', '-File',
    fileURLToPath(new URL('../../../../../../../scripts/get-smile-data-root.ps1', import.meta.url))],
    {encoding:'utf8'}).trim();
import { createHash } from 'node:crypto';
const hash = value => createHash('sha256').update(value).digest('hex');
const root = 'D:/Projects/Sin-Star-I-Assets/Neris-Castle/native-integration';
const revision = process.argv[2] || 'r001';
if (!/^r\d{3}$/.test(revision)) throw Error('Invalid revision');
const source = root + '/Neris-Town-Royal-Castle-' + revision + '.town';
const bytes = fs.readFileSync(source);
if (bytes.toString('ascii', 0, 4) !== 'SMD4' || bytes.readUInt32LE(8) !== bytes.length - 44 ||
    hash(bytes.subarray(44)) !== bytes.subarray(12, 44).toString('hex')) throw Error('Invalid portable document');
const folder = path.join(saveRoot, hash('smile.tools.character3d-viewer'), 'Data');
if (process.argv[3]) {
  const expected = fs.readFileSync(process.argv[3], 'utf8').trim();
  const current = fs.readFileSync(path.join(folder, hash('TownEditor.PermanentNeris') + '.bin'));
  if (hash(current) !== expected) throw Error('The live town changed; recapture it before installation.');
}
const backup = root + '/before-royal-relocation-' + new Date().toISOString().replaceAll(':', '-');
fs.mkdirSync(backup);
const keys = ['TownEditor.PermanentNeris', 'TownEditor.Working', 'TownEditor.Current', 'TownEditor.Recovery.Neris Town', 'TownEditor.Town.Neris Town'];
const records = [];
for (const key of keys) {
  const file = path.join(folder, hash(key) + '.bin');
  if (fs.existsSync(file)) fs.copyFileSync(file, path.join(backup, key + '.town'), fs.constants.COPYFILE_EXCL);
  records.push({key, file});
}
fs.writeFileSync(backup + '/manifest.json', JSON.stringify({source, replacementSha256:hash(bytes), records}, null, 2));
for (const {file} of records) {
  const pending = file + '.relocated.pending';
  fs.writeFileSync(pending, bytes, {flag:'wx'});
  fs.renameSync(pending, file);
}
console.log(JSON.stringify({source, backup, installed:records.length}));
