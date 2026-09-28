// Reuse the verified installed Blender when the interactive MCP is disconnected.
// Ephemeral bpy is passed in process arguments; no Python file or setup changes.
import { spawn } from 'node:child_process';
import fs from 'node:fs';

export async function runBlender(file, code, log) {
  const executable = 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe';
  if (!fs.existsSync(executable) || !fs.existsSync(file)) throw Error('Verified Blender/input missing.');
  const output = fs.createWriteStream(log, { flags: 'wx' });
  const child = spawn(executable, ['--background', file, '--python-exit-code', '1', '--python-expr', code], { windowsHide: true });
  child.stdout.pipe(output, { end: false }); child.stderr.pipe(output, { end: false });
  const exit = await new Promise((resolve, reject) => { child.on('error', reject); child.on('close', resolve); });
  await new Promise(resolve => output.end(resolve));
  if (exit !== 0) throw Error(`Blender failed (${exit}); inspect ${log}`);
  console.log(`Blender completed: ${log}`);
}
