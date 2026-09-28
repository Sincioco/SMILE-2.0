// Publish the measured r005 asset into its existing native package.
import fs from 'node:fs';
import path from 'node:path';
import { createHash } from 'node:crypto';
const workspace = 'D:/Projects/Sin-Star-I-Assets/Neris-Castle';
const repo = 'D:/SMILE 2.0';
const packageRoot = repo + '/games/SinStarI/SourceAssets/Towns/Neris/NerisCastleM06V2';
const source = workspace + '/exports/M06-r005';
const layout = JSON.parse(fs.readFileSync(source + '/Native/native-layout.json'));
if (layout.native_chunks.length !== 12 || layout.total_static_parts !== 85) throw Error('Unexpected r005 layout');
for (const file of ['castle.glb', 'neris-castle.glb', 'drawbridge.glb', 'door-west.glb', 'door-east.glb'])
  fs.copyFileSync(source + '/' + file, packageRoot + '/' + file);
fs.cpSync(source + '/Native', packageRoot + '/Native', { recursive: true });
const active = new Set([...layout.native_chunks.map(c => c.file), 'native-layout.json', layout.texture.file]);
// Preserve obsolete uncommitted derivative chunks in the dedicated workspace.
const archive = workspace + '/exports/M06-r003/obsolete-package-chunks';
fs.mkdirSync(archive, { recursive: true });
for (const file of fs.readdirSync(packageRoot + '/Native')) if (!active.has(file)) {
  const target = archive + '/' + file;
  if (fs.existsSync(target)) throw Error('Archive already exists: ' + target);
  fs.renameSync(packageRoot + '/Native/' + file, target);
}
fs.copyFileSync(workspace + '/source/neris-castle-M06-r005.blend', packageRoot + '/Source/neris-castle-M06-r005.blend');
function edit(file, change) {
  const name = repo + '/' + file;
  const before = fs.readFileSync(name, 'utf8');
  const after = change(before);
  if (after === before) throw Error('No edit: ' + file);
  fs.writeFileSync(name, after);
}
edit('games/SinStarI/SourceAssets/Towns/Neris/NerisCastleM06V2/prepare-native.mjs', text => {
  text = text.replaceAll('r003', 'r005').replace('length !== 11', 'length !== 12')
    .replace('total_static_parts !== 81', 'total_static_parts !== 85')
    .replace('11 static chunks, 81 static parts', '12 static chunks, 85 static parts');
  for (const file of ['drawbridge.glb', 'door-west.glb', 'door-east.glb']) {
    const hash = createHash('sha256').update(fs.readFileSync(source + '/' + file)).digest('hex');
    const expression = new RegExp('("from": "' + file.replace('.', '\\.') + '",\\s+"file": "[^"]+",\\s+"sha256": ")[^"]+');
    text = text.replace(expression, '$1' + hash);
  }
  return text;
});
edit('tools/Character3DViewer/NerisCastlePreview.smile', text => text
  .replace('Models[14]', 'Models[15]').replace('Objects[94]', 'Objects[98]')
  .replaceAll('0 To 13', '0 To 14')
  .replaceAll('Index = 13', 'Index = 14').replaceAll('Index = 12', 'Index = 13')
  .replaceAll('Index = 11', 'Index = 12').replaceAll('Index >= 12', 'Index >= 13')
  .replaceAll('Index - 12', 'Index - 13'));
edit('tools/Character3DViewer/Character3DViewer.smileproj', text => {
  const line = text.split('\n').find(l => l.includes('CastleV2\\castle-10.glb'));
  if (!line) throw Error('Missing chunk declaration');
  return text.replace(line, line + '\n' + line.replaceAll('10', '11')).replaceAll('neris-castle.m06-r003', 'neris-castle.m06-r005');
});
edit('tools/Character3DViewer/NerisCastleRoute.smile', text => text
  .replace('ORIGIN_X = -3650.0', 'ORIGIN_X = -3660.0').replace('ORIGIN_Z = -852.0', 'ORIGIN_Z = 2440.0'));
edit('tools/Character3DViewer/NerisTown.smile', text => text
  .replace('CastleRoute.DECK_Y, -2410.0', 'CastleRoute.DECK_Y, CastleRoute.ORIGIN_Z - 1558.0'));
edit('tools/Character3DViewer/NerisTownTests.smile', text => {
  const begin = text.indexOf('Sub CheckCastle()');
  const end = text.indexOf('End Sub', begin) + 7;
  let part = text.slice(begin, end);
  const values = {'-3650.0':'Castle.ORIGIN_X', '-3480.0':'Castle.ORIGIN_X + 170.0', '-3000.0':'Castle.ORIGIN_X + 650.0',
    '-2142.0':'Castle.ORIGIN_Z - 1290.0', '-2410.0':'Castle.ORIGIN_Z - 1558.0', '-2280.0':'Castle.ORIGIN_Z - 1428.0',
    '-1646.0':'Castle.ORIGIN_Z - 794.0', '-1900.0':'Castle.ORIGIN_Z - 1048.0', '-1156.0':'Castle.ORIGIN_Z - 304.0',
    '-850.0':'Castle.ORIGIN_Z + 2.0', '-816.0':'Castle.ORIGIN_Z + 36.0', '-614.0':'Castle.ORIGIN_Z + 238.0', '-608.9':'Castle.ORIGIN_Z + 243.1'};
  for (const [from, to] of Object.entries(values)) part = part.replaceAll(from, to);
  part = part.replace('Castle.Contains(Castle.ORIGIN_X, -2780.0)', 'Castle.Contains(Castle.ORIGIN_X, Castle.ORIGIN_Z - 1928.0)');
  return text.slice(0, begin) + part + text.slice(end);
});
edit('tools/Character3DViewer/NerisTownSceneTests.smile', text => text
  .replace('ObjectCount = 94', 'ObjectCount = 98').replace('Loads 81 Static Parts', 'Loads 85 Static Parts')
  .replace('Abs(Pose.Position.Z + 1940.0)', 'Abs(Pose.Position.Z - CastleRoute.ORIGIN_Z + 64.0 * CastleRoute.SCALE)')
  .replace('CastleRoute.DECK_Y, -2410.0', 'CastleRoute.DECK_Y, CastleRoute.ORIGIN_Z - 1558.0'));
edit('tools/Character3DViewer/TownSessionTests.smile', text => text.replace('0 To 13', '0 To 14'));
console.log('Published r005 source, 12 static chunks, 98 objects and relocated route.');
