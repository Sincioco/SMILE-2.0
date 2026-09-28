import fs from 'node:fs';
import path from 'node:path';
import {createHash} from 'node:crypto';
const root='D:/Projects/Sin-Star-I-Assets/Neris-Castle';
const repo='D:/SMILE 2.0';
const packageRoot=repo+'/games/SinStarI/SourceAssets/Towns/Neris/NerisCastleM06V2';
const exportLog=fs.readFileSync(root+'/checkpoints/M06-r005/export.log','utf8');
const settings=JSON.parse(exportLog.split('\n').find(line=>line.startsWith('NC_RESULT ')).slice(10));
const validation=JSON.parse(fs.readFileSync(root+'/exports/M06-r005/offline-validation.json'));
settings.source='Source/neris-castle-M06-r005.blend';
settings.gltf_triangles=validation.checks[0].triangles;
settings.exporter_omitted_triangles=settings.export_triangles-settings.gltf_triangles;
fs.writeFileSync(packageRoot+'/export-settings.json',JSON.stringify(settings,null,2)+'\n');
for(const folder of ['M06-r004','M06-r005','Town-r002'])
  fs.cpSync(root+'/checkpoints/'+folder,packageRoot+'/Checkpoints/'+folder,{recursive:true});
fs.copyFileSync(root+'/exports/M06-r005/offline-validation.json',packageRoot+'/Checkpoints/M06-r005/offline-validation.json');
fs.cpSync(root+'/automation',packageRoot+'/Automation',{recursive:true});
const town=repo+'/games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1';
for(const [source,dest] of [
  ['Neris-Town-Royal-Castle-r002.blend','Blend/Neris-Town-Royal-Castle-r002.blend'],
  ['Neris-Town-Royal-Castle-r002.town','Authoring/Neris-Town-Royal-Castle-r002.town'],
  ['relocated-town-document-r002.json','Authoring/Neris-Town-Royal-Castle-r002.json']]) {
    const target=town+'/'+dest;
    if(fs.existsSync(target))throw Error('Preserve versioned town: '+target);
    fs.copyFileSync(root+'/native-integration/'+source,target,fs.constants.COPYFILE_EXCL);
  }
const entries=[];
function walk(directory){
  for(const file of fs.readdirSync(directory,{withFileTypes:true})){
    const name=path.join(directory,file.name);
    if(file.isDirectory())walk(name);
    else if(file.name!=='package-manifest.json'){
      const bytes=fs.readFileSync(name);
      entries.push({path:path.relative(packageRoot,name).replaceAll('\\','/'),bytes:bytes.length,sha256:createHash('sha256').update(bytes).digest('hex')});
    }
  }
}
walk(packageRoot);
fs.writeFileSync(packageRoot+'/package-manifest.json',JSON.stringify({revision:'M06-r005',source:settings.source,dimensions:'proposed-unapproved',files:entries},null,2)+'\n');
console.log('Recorded r005 source, real renders, validation, orchestration and r002 town revision.');
