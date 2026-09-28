import fs from 'node:fs';
import {build} from './luxury-export.mjs';
import {runBlender} from './blender-background.mjs';
import {packNative} from './native-pack.mjs';
const root='D:/Projects/Sin-Star-I-Assets/Neris-Castle';
const output=root+'/exports/M06-r006';
fs.mkdirSync(output);
await runBlender(root+'/source/neris-castle-M06-r006.blend',build.replaceAll('M06-r003','M06-r006')+
    '\nprint("NC_RESULT "+json.dumps(result))',root+'/checkpoints/M06-r006/export.log');
const layout=packNative(output+'/castle.glb',output+'/Native');
console.log(JSON.stringify({chunks:layout.native_chunks.length,parts:layout.total_static_parts}));
