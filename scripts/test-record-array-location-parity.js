"use strict";
// Focused production regressions; no npm, no generated-code rewriting.
const fs = require("fs"), path = require("path"), cp = require("child_process"), crypto = require("crypto");
const args = process.argv.slice(2), options = {};
for (let i = 0; i < args.length; i += 2) {
    if (!['--repo', '--out', '--compiler'].includes(args[i]) || !args[i + 1]) throw new Error('Unknown/missing option.');
    options[args[i]] = args[i + 1];
}
const repo = path.resolve(options['--repo'] || path.join(__dirname, '..'));
const root = path.resolve(options['--out'] || path.join(repo, 'artifacts', 'temp', 'record-array-parity', 'run-' + crypto.randomUUID()));
const compiler = path.resolve(options['--compiler'] || path.join(repo, 'artifacts', 'compiler', 'smilec.exe'));
const fixtureRoot = path.join(repo, 'examples', 'RecordArrayLocationParity');
if (process.platform !== 'win32') throw new Error('This command requires Windows for actual native parity.');
fs.mkdirSync(root, {recursive: true});
const cases = JSON.parse(fs.readFileSync(path.join(fixtureRoot, 'cases.json'), 'utf8'));
const result = {status: 'IN-PROGRESS', cases: [], compilerSha256: hash(compiler), output: root};
const normalize = s => s.replace(/\r\n/g, '\n');
function hash(file) { return crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex').toUpperCase(); }
function execute(label, exe, args, cwd, timeout = 120000, native = false) {
    const env = {...process.env};
    if (native) {
        env.SMILE_CLASS_LIFETIME_DIAGNOSTICS = '1';
        env.SMILE_IMAGE_LIFETIME_DIAGNOSTICS = '1';
        env.SMILE_TEXT_LIFETIME_DIAGNOSTICS = '1';
    }
    const run = cp.spawnSync(exe, args, {cwd, env, encoding:'utf8', timeout, maxBuffer: 4 * 1024 * 1024, windowsHide:true});
    fs.writeFileSync(path.join(root, label + '.stdout.log'), run.stdout || '');
    fs.writeFileSync(path.join(root, label + '.stderr.log'), run.stderr || '');
    if (run.error || run.signal || run.status === null) throw new Error(label + ': bounded execution failed: ' + (run.error || run.signal));
    return run;
}
function successful(label, run) {
    if (run.status !== 0) throw new Error(label + ': exit ' + run.status + '; see logs.');
}
function copyImage(destination) {
    fs.mkdirSync(path.join(destination, 'Assets'), {recursive:true});
    fs.copyFileSync(path.join(repo, 'examples', 'MenuGallery', 'Assets', 'Cursor.png'), path.join(destination, 'Assets', 'Proof.png'));
}
try {
    for (const test of cases) {
        if (!/^[A-Za-z][A-Za-z0-9]+$/.test(test.name)) throw new Error('Invalid fixture name.');
        const dir = path.join(root, test.name), web = path.join(dir, 'web');
        fs.mkdirSync(dir, {recursive:true});
        const source = path.join(dir, test.name + '.smile'), expected = path.join(dir, 'expected.txt');
        fs.copyFileSync(path.join(fixtureRoot, test.name + '.smile'), source);
        fs.writeFileSync(expected, test.expected.join('\n') + '\n');
        const exe = path.join(dir, test.name + '.exe');
        const appId = 'smile.tests.h23.run-' + crypto.randomUUID().replace(/-/g, '');
        successful(test.name + ' native compile', execute(test.name + '-native-compile', compiler,
            [source, '--target', 'windows-x64', '--configuration', 'Release', '--graphics', 'GDI', '--application-id', appId, '-o', exe], repo));
        copyImage(dir);
        const native = execute(test.name + '-native', exe, [], dir, 20000, true);
        const expectedNative = [...test.expected, ...(test.nativeError ? [test.nativeError] : []),
            'SMILE_CLASS_LIVE=0', 'SMILE_IMAGE_LIVE=0', 'SMILE_TEXT_LIVE=0'].join('\n') + '\n';
        if (native.status !== (test.error ? 4 : 0) || native.stderr !== '' || normalize(native.stdout) !== expectedNative)
            throw new Error(test.name + ': native output/exit differs. Expected: ' + JSON.stringify(expectedNative));
        successful(test.name + ' Web compile', execute(test.name + '-web-compile', compiler,
            [source, '--target', 'web', '--application-id', appId, '--output-dir', web], repo));
        copyImage(web);
        successful(test.name + ' JS syntax', execute(test.name + '-syntax', process.execPath, ['--check', path.join(web, 'game.js')], repo));
        const runnerArgs = [path.join(repo, 'scripts', 'run-web-test.js'), web, '--expected', expected, '--timeout', '10000'];
        if (test.error) runnerArgs.push('--expected-runtime-error', test.error);
        successful(test.name + ' Web trace', execute(test.name + '-web', process.execPath, runnerArgs, repo, 20000));
        result.cases.push({name: test.name, status:'PASS', expected: test.expected, expectedError:test.error,
            nativeExit: native.status, sourceSha256:hash(source), generatedSha256:hash(path.join(web, 'game.js')), webDirectory:web, chrome:test.chrome});
        fs.writeFileSync(path.join(root, 'parity-results.json'), JSON.stringify(result, null, 2));
        console.log('PASS ' + test.name + ': compiled native + generated Web; exact output/error trace.');
    }
    result.status = 'PASS';
    console.log('H02/H03 focused native/Web parity passed: ' + result.cases.length + ' cases.');
} catch (error) { result.status = 'FAILED'; result.error = String(error.stack || error); console.error(result.error); process.exitCode = 1; }
finally { fs.writeFileSync(path.join(root, 'parity-results.json'), JSON.stringify(result, null, 2)); }
