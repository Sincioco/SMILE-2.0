import fs from 'node:fs';
import path from 'node:path';
import {createHash} from 'node:crypto';
const work='D:/Projects/Sin-Star-I-Assets/Neris-Castle';
const repo='D:/SMILE 2.0';
const castle=repo+'/games/SinStarI/SourceAssets/Towns/Neris/NerisCastleM06V2';
const town=repo+'/games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1';
const evidence=castle+'/Checkpoints/M06-r006';
fs.mkdirSync(evidence,{recursive:true});
for(const name of ['entrance-day.png','entrance-night.png','entrance-checks.json','publish-checks.json'])
  fs.copyFileSync(work+'/checkpoints/M06-r006/'+name,evidence+'/'+name);
fs.copyFileSync(work+'/exports/M06-r006/offline-validation.json',evidence+'/offline-validation.json');
for(const name of ['town-day.png','native-night.jpg','town-checks.json','round-trip.log'])
  fs.copyFileSync(work+'/checkpoints/Town-r003/'+name,evidence+'/'+name);
for(const [source,destination] of [
  ['artifacts/tests/town-editor/Foundations.exe.log','town-foundations.log'],
  ['artifacts/tests/town-editor/EditedRoutes.exe.log','town-routes.log'],
  ['artifacts/tests/town-editor/TownRenderTests.exe.log','town-render.log'],
  ['tools/Character3DViewer/bin/Release/TownSessionTests.exe.log','town-session.log'],
  ['artifacts/royal-court-water-tests.log','water-tests.log'],
  ['artifacts/royal-court-pbr-tests.log','pbr-tests.log'],
  ['artifacts/royal-court-final-style.log','style-check.log'],
  ['artifacts/royal-court-vsix-check.log','vsix-check.log']])
  fs.copyFileSync(repo+'/'+source,evidence+'/'+destination);
const exportLine=fs.readFileSync(work+'/checkpoints/M06-r006/export.log','utf8').split(/\r?\n/).find(s=>s.startsWith('NC_RESULT '));
fs.writeFileSync(castle+'/export-settings.json',JSON.stringify(JSON.parse(exportLine.slice(10)),null,2)+'\n');
for(const name of ['blender-background.mjs','flush-entrance.mjs','export-r006.mjs','publish-r006.mjs',
  'inspect-royal-stairs.mjs','remove-royal-stairs.mjs','update-royal-court-town.mjs',
  'city-hall-shrubs.mjs','update-city-hall-town.mjs','pack-city-shrubs.mjs','tower-dishes.mjs','update-tower-town.mjs',
  'prepare-relocated-town.mjs','install-relocated-town.mjs','check-town-r003.mjs','record-r006.mjs'])
  fs.copyFileSync(work+'/automation/'+name,castle+'/Automation/'+name);
fs.copyFileSync(work+'/native-integration/Neris-Town-Royal-Castle-r003.blend',town+'/Blend/Neris-Town-Royal-Castle-r003.blend');
fs.copyFileSync(work+'/native-integration/Neris-Town-Royal-Castle-r003.town',town+'/Authoring/Neris-Town-Royal-Castle-r003.town');
fs.copyFileSync(work+'/native-integration/relocated-town-document-r003.json',town+'/Authoring/Neris-Town-Royal-Castle-r003.json');
for(const revision of ['r004','r005'])
  fs.copyFileSync(work+'/native-integration/Neris-Town-Royal-Castle-'+revision+'.blend',town+'/Blend/Neris-Town-Royal-Castle-'+revision+'.blend');
for(const [folder,names] of [
  ['City-Hall-r001',['city-hall-shrubs.png','checks.json','packing.json','town-update-fixed.log']],
  ['Tower-r001',['tower-dishes.png','checks.json','town-update-baked.log']]]){
  fs.mkdirSync(evidence+'/'+folder,{recursive:true});
  for(const name of names)fs.copyFileSync(work+'/checkpoints/'+folder+'/'+name,evidence+'/'+folder+'/'+name);
}
const files=[];
function walk(directory){
  for(const entry of fs.readdirSync(directory,{withFileTypes:true})){
    const file=path.join(directory,entry.name);
    if(entry.isDirectory())walk(file);
    else if(entry.name!=='package-manifest.json'){
      const bytes=fs.readFileSync(file);
      files.push({path:path.relative(castle,file).replaceAll('\\','/'),bytes:bytes.length,
        sha256:createHash('sha256').update(bytes).digest('hex')});
    }
  }
}
walk(castle);
fs.writeFileSync(castle+'/package-manifest.json',JSON.stringify({revision:'M06-r006',
  name:'Royal Court',source:'Source/neris-castle-M06-r006.blend',dimensions:'proposed-unapproved',files},null,2)+'\n');
console.log('Recorded Royal Court r006 / town r005 with '+files.length+' checked package files.');
