// Deterministic masonry albedo: a portable authored texture, never review evidence.
import fs from 'node:fs';
import zlib from 'node:zlib';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const size=1024, rgba=Buffer.alloc(size*size*4);
for(let y=0;y<size;y++) for(let x=0;x<size;x++){
 const row=Math.floor(y/256), px=(x+(row%2)*128)%256, py=y%256;
 const joint=px<3||py<3, lip=px===3||py===3, edge=px>251||py>251;
 const block=Math.floor((x+(row%2)*128)/256);
 const variation=((row*7+block*13)%11-5)*.42;
 const noise=((x*17+y*31)%19-9)*.10;
 const factor=joint?.78:lip?1.018:edge?.97:1;
 const i=(y*size+x)*4;
 [232,224,210].forEach((v,c)=>rgba[i+c]=Math.max(0,Math.min(255,Math.round((v+variation+noise)*factor))));
 rgba[i+3]=255;
}
function crc32(data){let c=0xffffffff;for(const b of data){c^=b;for(let i=0;i<8;i++)c=(c>>>1)^((c&1)?0xedb88320:0);}return(c^0xffffffff)>>>0;}
function chunk(type,data){const b=Buffer.alloc(data.length+12);b.writeUInt32BE(data.length);b.write(type,4);data.copy(b,8);b.writeUInt32BE(crc32(b.subarray(4,-4)),b.length-4);return b;}
const header=Buffer.alloc(13);header.writeUInt32BE(size);header.writeUInt32BE(size,4);header[8]=8;header[9]=6;
const scan=Buffer.alloc((size*4+1)*size);for(let y=0;y<size;y++)rgba.copy(scan,y*(size*4+1)+1,y*size*4,(y+1)*size*4);
const out=path.join(root,'textures','neris-ivory-courses-1024.png');
fs.writeFileSync(out,Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',header),chunk('IDAT',zlib.deflateSync(scan)),chunk('IEND',Buffer.alloc(0))]));
console.log(out);
