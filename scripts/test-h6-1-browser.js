// Bounded real-browser checks for the existing Viewer/Labs. No browser/GL mocks.
// Requires an existing Playwright package (NODE_PATH is supported). Chrome and
// Edge are the current acceptance browsers; Firefox is optional diagnostics only.
// Windows are deliberately visible for the live presentation. Align the browser
// with the Viewer before running when a stream capture depends on window bounds.
"use strict";
const { chromium, firefox } = require("playwright");
const fs = require("node:fs");
const path = require("node:path");
const http = require("node:http");
const crypto = require("node:crypto");
const assert = require("node:assert/strict");
const args = process.argv.slice(2);
const option = (name, fallback) => args.includes(name) ? args[args.indexOf(name) + 1] : fallback;
const engine = option("--engine", "chromium");
assert(["chromium", "edge", "firefox"].includes(engine), "Use --engine chromium, edge or firefox");
const root = path.resolve(option("--web-root", "artifacts/web"));
const output = path.resolve(option("--output", `artifacts/tests/h6-1-browser/${engine}`));
const only = option("--only", "tools");
const hash = file => crypto.createHash("sha256").update(fs.readFileSync(file)).digest("hex");
fs.mkdirSync(output, { recursive: true });
const server = http.createServer((request, response) => {
    let file;
    try { file = path.resolve(root, "." + decodeURIComponent(new URL(request.url, "http://localhost").pathname)); }
    catch { response.writeHead(400).end(); return; }
    if (!file.startsWith(root + path.sep)) { response.writeHead(403).end(); return; }
    if (request.url === "/favicon.ico") { response.writeHead(204).end(); return; }
    if (fs.existsSync(file) && fs.statSync(file).isDirectory()) file = path.join(file, "index.html");
    if (!fs.existsSync(file) || !fs.statSync(file).isFile()) { response.writeHead(404).end(); return; }
    const mime = { ".html": "text/html", ".js": "text/javascript", ".css": "text/css", ".png": "image/png", ".wav": "audio/wav" };
    response.writeHead(200, { "Content-Type": mime[path.extname(file)] || "application/octet-stream", "Cache-Control": "no-store" });
    fs.createReadStream(file).pipe(response);
});

async function instrumentation(page) {
    await page.addInitScript(() => {
        window.__h61 = { text: [], numbers: [], lastText: [], lastNumbers: [], camera: null, contexts: [], targetMismatches: [] };
        const getContext = HTMLCanvasElement.prototype.getContext;
        HTMLCanvasElement.prototype.getContext = function (kind, ...rest) {
            const result = getContext.call(this, kind, ...rest);
            if (kind === "webgl2" && result && !window.__h61.contexts.includes(result)) {
                window.__h61.contexts.push(result);
                // Observe real target sizes without substituting any GL operation.
                const gl = result, sizes = new Map(), attachments = new Map();
                let framebuffer = null, texture = null, viewport = [0, 0, this.width, this.height];
                for (const name of ["bindTexture", "texImage2D", "bindFramebuffer", "framebufferTexture2D", "viewport", "drawArrays", "drawElements", "drawElementsInstanced"]) {
                    const original = gl[name].bind(gl);
                    gl[name] = (...a) => {
                        if (name === "bindTexture") texture = a[1];
                        if (name === "texImage2D") sizes.set(texture, a.length === 9 ? [a[3], a[4]] : [a[5].width, a[5].height]);
                        if (name === "bindFramebuffer" && a[0] !== gl.READ_FRAMEBUFFER) framebuffer = a[1];
                        if (name === "framebufferTexture2D" && a[1] === gl.COLOR_ATTACHMENT0) attachments.set(framebuffer, a[3]);
                        if (name === "viewport") viewport = a;
                        if (name.startsWith("draw")) {
                            const size = framebuffer ? sizes.get(attachments.get(framebuffer)) : [gl.canvas.width, gl.canvas.height];
                            if (size && (viewport[2] > size[0] || viewport[3] > size[1]) && window.__h61.targetMismatches.length < 8)
                                window.__h61.targetMismatches.push({ name, viewport, size, stack: new Error().stack });
                        }
                        return original(...a);
                    };
                }
            }
            return result;
        };
        let api;
        Object.defineProperty(window, "smile", { get: () => api, set: value => {
            api = value;
            const text = api.drawText, number = api.drawNumber, show = api.showScreen, renderer = api.renderer3D;
            api.showScreen = (...a) => {
                window.__h61.lastText = window.__h61.text; window.__h61.lastNumbers = window.__h61.numbers;
                window.__h61.text = []; window.__h61.numbers = [];
                return show(...a);
            };
            api.drawText = (...a) => { window.__h61.text.push(a); return text(...a); };
            api.drawNumber = (...a) => { window.__h61.numbers.push(a); return number(...a); };
            api.renderer3D = (...a) => { const result = renderer(...a); if (a[0] === 10 && result) window.__h61.camera = a.slice(1); return result; };
        }});
    });
}

async function frames(page, count = 12) {
    const frame = await page.evaluate(() => window.__smileWeb.frameCount);
    await page.waitForFunction(start => window.__smileWeb.status !== "running" || window.__smileWeb.frameCount >= start,
        frame + count, { timeout: 45000 });
}

async function state(page) {
    return page.evaluate(() => {
        const gl = window.__h61.contexts[0];
        return { runtime: window.__smileWeb, focus: { focused: document.hasFocus(), visibility: document.visibilityState }, media: window.smile.mediaDiagnostics(), camera: window.__h61.camera,
            text: window.__h61.lastText, numbers: window.__h61.lastNumbers, targetMismatches: window.__h61.targetMismatches,
            gpu: gl ? { version: gl.getParameter(gl.VERSION), renderer: gl.getParameter(gl.RENDERER),
                shadingLanguage: gl.getParameter(gl.SHADING_LANGUAGE_VERSION), extensions: gl.getSupportedExtensions(),
                attributes: gl.getParameter(gl.MAX_VERTEX_ATTRIBS), feedbackComponents: gl.getParameter(gl.MAX_TRANSFORM_FEEDBACK_INTERLEAVED_COMPONENTS),
                error: gl.getError() } : null,
            particles: { systems: smile.renderer3D(127,10,1,0,0,0,0,0,0,0,0), dispatches: smile.renderer3D(127,10,14,0,0,0,0,0,0,0,0),
                gpuBytes: smile.renderer3D(127,10,17,0,0,0,0,0,0,0,0) } };
    });
}

async function clickText(page, label, maxY = Infinity) {
    const point = await page.evaluate(({ label, maxY }) => {
        const t = window.__h61.lastText.find(t => t[0] === label && t[2] < maxY);
        if (!t) throw new Error(`Visible canvas label not found: ${label}`);
        const box = document.querySelector("#smile-canvas").getBoundingClientRect(), m = smile.mediaDiagnostics();
        return { x: box.x + (t[1] + 8) * box.width / m.logicalWidth, y: box.y + (t[2] + 6) * box.height / m.logicalHeight };
    }, { label, maxY });
    await page.mouse.click(point.x, point.y);
    await frames(page);
}

function healthy(result) {
    assert.equal(result.runtime.status, "running", JSON.stringify(result.runtime));
    assert(!result.runtime.rendererFailure, JSON.stringify(result.runtime.rendererFailure));
    assert(!result.text.some(t => /Could Not Load|Viewer Recovery|Renderer Unavailable/i.test(t[0])), "Scene recovery UI is visible");
    assert(result.gpu && result.gpu.error === 0, "WebGL2 is unavailable or reports an error");
}

async function run() {
    await new Promise(resolve => server.listen(0, "127.0.0.1", resolve));
    const base = `http://127.0.0.1:${server.address().port}`;
    const browser = await (engine !== "firefox" ? chromium.launch({ executablePath: engine === "edge"
        ? option("--edge", "C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe")
        : option("--chrome", "C:/Program Files/Google/Chrome/Application/chrome.exe"),
        headless: false, chromiumSandbox: true }) : firefox.launch({ headless: false }));
    const report = { engine, browserVersion: browser.version(), visible: true, platform: process.platform,
        viewport: { width: 1360, height: 900 }, base, observations: [], failures: [] };
    try {
        const names = only === "fixtures" ? ["h6-1/CalibrationTests", "Character3DViewerActorIsolationTests"] : only !== "tools" ? [`h6-1/${only}`] :
            ["h6-1/Character3DViewer", "h6-1/AdvancedFireVfxLab", "h6-1/AdvancedLightningVfxLab"];
        for (const name of names) {
            const page = await browser.newPage({ viewport: report.viewport });
            const messages = [], observation = { name, messages, checks: [], hashes: {} };
            for (const file of ["index.html", "game.js", "smile-runtime.js"]) observation.hashes[file] = hash(path.join(root,name,file));
            report.observations.push(observation);
            page.on("console", message => { if (["error","warning"].includes(message.type())) messages.push({ type: message.type(), text: message.text() }); });
            page.on("pageerror", error => messages.push({ type: "pageerror", text: String(error.stack || error) }));
            page.on("response", response => { if (response.status() >= 400) messages.push({ type: "http", text: `${response.status()} ${response.url()}` }); });
            await instrumentation(page);
            try {
                await page.goto(`${base}/${name}/`);
                await page.bringToFront();
                console.log(`${engine} visible test window ready: ${name}`);
                if (args.includes("--await-focus")) await page.waitForFunction(() => document.hasFocus(), {}, { timeout: 45000, polling: 100 });
                if (only === "fixtures") {
                    await page.waitForFunction(() => ["stopped","error"].includes(window.__smileWeb?.status), {}, { timeout: 60000 });
                    observation.runtime = await page.evaluate(() => window.__smileWeb);
                    observation.console = await page.locator("#smile-console").textContent();
                    assert.equal(observation.runtime.status, "stopped");
                    const expected = name.includes("CalibrationTests") ? "Viewer calibration isolation passed" : "Character Viewer two-Orin isolation tests passed";
                    assert.equal(observation.console.trim(), expected);
                    observation.checks.push("Actual browser fixture exact output and teardown");
                } else {
                    await page.waitForFunction(() => window.__smileWeb?.frameCount >= 50 || window.__smileWeb?.status === "error", {}, { timeout: 45000, polling: 100 });
                    observation.initial = await state(page);
                    healthy(observation.initial);
                    if (name.endsWith("Character3DViewer")) {
                        for (const tab of ["Arin", "Orin", "Dragon", "Party"]) {
                            await clickText(page, tab, 110);
                            const current = await state(page); healthy(current);
                            assert(current.camera, `${tab} has no rendered camera`);
                            await page.screenshot({ path: path.join(output,`${tab}.png`) });
                            observation.checks.push(`${tab} renders without recovery`);
                        }
                    } else {
                        await page.screenshot({ path: path.join(output,path.basename(name)+".png") });
                        if (name.endsWith("AdvancedFireVfxLab")) {
                            assert(observation.initial.particles.dispatches > 0 && observation.initial.particles.gpuBytes > 0, "High Fire did not execute GPU simulation");
                            observation.checks.push("High Fire uses actual transform feedback and GPU buffers");
                        }
                    }
                    // Normal input events, including a genuinely held middle button.
                    await page.mouse.move(600, 350); await page.mouse.down({button:"middle"});
                    await page.mouse.move(625, 365, {steps:12}); await page.mouse.move(750, 435, {steps:8}); await page.mouse.up({button:"middle"});
                    await page.mouse.wheel(0, -120); await frames(page);
                    await page.mouse.wheel(0, 120); await frames(page);
                    await page.mouse.move(650, 400); await page.mouse.down(); await page.mouse.move(690, 425, {steps:10}); await page.mouse.up();
                    await frames(page); observation.navigation = await state(page); healthy(observation.navigation);
                    assert.equal(observation.navigation.media.canvasActivePointerCount, 0);
                    observation.checks.push("Slow/moderate held-middle orbit, pan, wheel in/out and release");
                    await page.setViewportSize({ width: 1100, height: 740 }); await frames(page);
                    observation.resized = await state(page); healthy(observation.resized);
                    observation.checks.push("Resize preserves scene and logical input mapping");
                }
                assert.equal(messages.length,0,JSON.stringify(messages));
            } catch (error) {
                observation.failure = String(error.stack || error); report.failures.push(name);
                observation.failureState = await state(page).catch(error => ({ failure: error.message }));
            }
            finally {
                await page.screenshot({path:path.join(output,path.basename(name)+"-final.png"),timeout:10000}).catch(error=>{observation.captureFailure=error.message;});
                await page.close();
                fs.writeFileSync(path.join(output,"report.json"),JSON.stringify(report,null,2));
                console.log(`${engine} ${name}: ${observation.failure ? "FAIL " + observation.failure.split("\n")[0] : "PASS"}`);
            }
        }
        assert.equal(report.failures.length,0,`${report.failures.join(", ")} failed; see ${output}`);
    } finally { await browser.close(); }
}

run().catch(error => { console.error(error); process.exitCode=1; }).finally(()=>server.close());
