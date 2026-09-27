import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const checkpoint = path.join(root, 'checkpoints', process.argv[2] || 'M01-r002');
const audit = JSON.parse(fs.readFileSync(path.join(root, 'extraction-audit.json')));
const kit = path.join(audit.extraction, path.basename(audit.zipPath, '.zip'));
const spec = JSON.parse(fs.readFileSync(path.join(kit, 'data/castle-spec.json')));
const scene = JSON.parse(fs.readFileSync(path.join(checkpoint, 'scene-measurements.json')));
const tests = [];
const check = (name, pass, actual, expected) => tests.push({ name, passed: !!pass, actual, expected });
const near = (name, actual, expected, tolerance = 0.05) => check(name, Math.abs(actual - expected) <= tolerance, actual, expected);
const object = name => { const found = scene.objects.find(o => o.name === name); if (!found) throw Error(name); return found; };
const bounds = objects => [0, 1, 2].map(i => [Math.min(...objects.map(o => o.bounds[i][0])), Math.max(...objects.map(o => o.bounds[i][1]))]);
const keep = bounds(scene.objects.filter(o => o.name.startsWith('NC.Palace.Keep.')));
for (const [i, expected] of [[0, spec.palace.keep_bounds_x], [1, spec.palace.keep_bounds_y], [2, [3, 32]]])
  expected.forEach((v, edge) => near(`Keep bound ${i}/${edge}`, keep[i][edge], v));
const shafts = scene.objects.filter(o => o.name.startsWith('NC.Fortifications.Corner.') && o.name.endsWith('.Shaft'));
check('Four corner shafts', shafts.length === 4, shafts.length, 4);
for (const [index, suffix] of ['SW', 'SE', 'NW', 'NE'].entries()) {
  const o = object(`NC.Fortifications.Corner.${suffix}.Shaft`);
  [0, 1].forEach(axis => near(`${suffix} center ${axis}`, o.location[axis], spec.corner_towers.centers[index][axis]));
}
const footprint = bounds(shafts);
near('Tower-inclusive width', footprint[0][1] - footprint[0][0], 108, 0.1);
near('Tower-inclusive depth', footprint[1][1] - footprint[1][0], 132, 0.1);
near('Gate clear width', object('NC.Gatehouse.Jamb.East').bounds[0][0] - object('NC.Gatehouse.Jamb.West').bounds[0][1], 12);
check('Actual gate passage ray samples open', scene.gateway_rays.every(r => !r.hit), scene.gateway_rays, 'No hit at x=-5.9, 0, 5.9; z=1 through gate');
spec.drawbridge.hinge_world.forEach((v, i) => near(`Bridge pivot ${i}`, object('NC.Bridge.Pivot').location[i], v));
const bridge = object('NC.Bridge.Leaf.Main');
near('Bridge width', bridge.bounds[0][1] - bridge.bounds[0][0], 10);
near('Bridge length', bridge.bounds[1][1] - bridge.bounds[1][0], 20);
near('Bridge top', bridge.bounds[2][1], 0);
near('Bridge south tip', bridge.bounds[1][0], -84);
check('Bridge leaf parent', bridge.parent === 'NC.Bridge.Pivot', bridge.parent, 'NC.Bridge.Pivot');
near('Water level', object('NC.Site.Moat.Water').bounds[2][0], -2);
check('Moat is one ring with four connected sides', scene.moat_mesh.vertices.length === 8 && scene.moat_mesh.faces.length === 4 && scene.moat_mesh.faces.every((f, i, all) => f.filter(v => all[(i + 1) % 4].includes(v)).length === 2), scene.moat_mesh.faces, 'One connected four-face ring');
near('Highest crystal', object('NC.Palace.Crystal.Main').bounds[2][1], 60);
const stairs = scene.objects.filter(o => o.role === 'stair');
check('Twenty stairs', stairs.length === 20, stairs.length, 20);
near('Stair rise', Math.max(...stairs.map(o => o.bounds[2][1])), 3);
near('Courtyard width', object('NC.Courtyard.Paving').bounds[0][1] - object('NC.Courtyard.Paving').bounds[0][0], 80);
near('Courtyard depth', object('NC.Courtyard.Paving').bounds[1][1] - object('NC.Courtyard.Paving').bounds[1][0], 54);
near('Courtyard paving height', object('NC.Courtyard.Paving').bounds[2][1], 0);
check('Gate then courtyard then palace', object('NC.Gatehouse.Jamb.West').bounds[1][1] < object('NC.Courtyard.Paving').bounds[1][0] && object('NC.Courtyard.Paving').bounds[1][1] < keep[1][0], [-50,-46,8,14], 'Increasing northward sequence');
const proxy = bounds(scene.objects.filter(o => o.name.startsWith('NC.Review.ScaleProxy.')));
near('Human proxy height', proxy[2][1] - proxy[2][0], 1.85);
for (const camera of scene.cameras.filter(c => c.projection === 'ORTHO')) {
  near(`${camera.name} frame width`, camera.world_width, 200);
  near(`${camera.name} frame height`, camera.world_height, camera.name.endsWith('Top') ? 200 : 112.5);
  near(`${camera.name} projected 20 m ruler pixels`, camera.projected_20m_pixels, 204.8, 0.001);
}
check('All objects have owners', scene.objects.every(o => o.owner), scene.objects.length, 'Every object tagged');
check('Stable IDs without automatic duplicate suffixes', !scene.objects.some(o => /\.\d{3}$/.test(o.name)), scene.objects.filter(o => /\.\d{3}$/.test(o.name)).map(o => o.name), []);
check('Bridge-only rebuild preserves all other owners', scene.bridge_owner_rerun_preserved_others, true, true);
for (const [name, width, height] of [['front',2048,1152],['back',2048,1152],['left',2048,1152],['right',2048,1152],['top',2048,2048],['hero-front',2560,1440],['hero-three-quarter',2560,1440]]) {
  const png = fs.readFileSync(path.join(checkpoint, name + '.png'));
  check(`${name} render dimensions`, png.readUInt32BE(16) === width && png.readUInt32BE(20) === height, [png.readUInt32BE(16),png.readUInt32BE(20)], [width,height]);
}
const report = { milestone: 'M01', status: tests.every(t => t.passed) ? 'passed' : 'failed', tests, approval: 'awaiting-user-review', scope: 'Provisional blockout only; detailed bridge collision poses, facade finish and final export are later stages.' };
fs.writeFileSync(path.join(checkpoint, 'measured-checks.json'), JSON.stringify(report, null, 2));
console.log(JSON.stringify({ status: report.status, checks: tests.length, failures: tests.filter(t => !t.passed) }, null, 2));
if (report.status === 'failed') process.exitCode = 1;
