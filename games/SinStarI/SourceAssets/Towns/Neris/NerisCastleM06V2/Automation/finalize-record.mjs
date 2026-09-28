import fs from 'node:fs';
import path from 'node:path';
import {createHash} from 'node:crypto';
const root='D:/Projects/Sin-Star-I-Assets/Neris-Castle';
const repo='D:/SMILE 2.0';
const packageRoot=repo+'/games/SinStarI/SourceAssets/Towns/Neris/NerisCastleM06V2';
const evidence=packageRoot+'/Checkpoints/M06-r005';
const reports=['castle-final-town-tests.log','castle-final-camera.log','castle-water-lab-tests.log','castle-style-final.log','castle-vsix-verify.log'];
for(const name of reports)fs.copyFileSync(repo+'/artifacts/tests/'+name,evidence+'/'+name);
fs.copyFileSync(packageRoot+'/README.md',root+'/DELIVERY.md');
fs.copyFileSync(evidence+'/checkpoint.md',root+'/checkpoints/M06-r005/checkpoint.md');
fs.copyFileSync(root+'/automation/finalize-record.mjs',packageRoot+'/Automation/finalize-record.mjs');
const files=[];
function walk(directory){
  for(const entry of fs.readdirSync(directory,{withFileTypes:true})){
    const file=path.join(directory,entry.name);
    if(entry.isDirectory())walk(file);
    else if(entry.name!=='package-manifest.json'){
      const data=fs.readFileSync(file);
      files.push({path:path.relative(packageRoot,file).replaceAll('\\','/'),bytes:data.length,sha256:createHash('sha256').update(data).digest('hex')});
    }
  }
}
walk(packageRoot);
fs.writeFileSync(packageRoot+'/package-manifest.json',JSON.stringify({revision:'M06-r005',source:'Source/neris-castle-M06-r005.blend',dimensions:'proposed-unapproved',files},null,2)+'\n');
console.log('Final evidence and '+files.length+' package hashes recorded.');
