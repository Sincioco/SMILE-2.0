"use strict";

const fs = require("fs");
const path = require("path");
const vm = require("vm");

function fail(message) {
    process.stderr.write(`Web execution failed: ${message}\n`);
    process.exit(1);
}

const args = process.argv.slice(2);
if (args.length === 0) fail("usage: node scripts/run-web-test.js <web-directory> [--expected <file>] [--native-output <file>] [--draw-text <value> | --draw-text-file <file>] [--frames <count>] [--timeout <ms>] [--phase4-media|--phase4-ownership|--phase4-clip|--phase4-audio|--phase5-ui|--phase5-hardening]");

const webDirectory = path.resolve(args.shift());
let expectedPath = null;
let nativeOutputPath = null;
let expectedDrawText = null;
let maximumFrames = 3;
let timeoutMilliseconds = 5000;
let verifyPhase4Media = false;
let verifyPhase4Ownership = false;
let verifyPhase4Clip = false;
let verifyPhase4Audio = false;
let verifyPhase5Ui = false;
let verifyPhase5Hardening = false;
while (args.length !== 0) {
    const option = args.shift();
    if (option === "--phase4-media") {
        verifyPhase4Media = true;
        continue;
    }
    if (option === "--phase4-ownership") { verifyPhase4Ownership = true; continue; }
    if (option === "--phase4-clip") { verifyPhase4Clip = true; continue; }
    if (option === "--phase4-audio") { verifyPhase4Audio = true; continue; }
    if (option === "--phase5-ui") { verifyPhase5Ui = true; continue; }
    if (option === "--phase5-hardening") { verifyPhase5Hardening = true; continue; }
    const value = args.shift();
    if (value === undefined) fail(`missing value for ${option}`);
    if (option === "--expected") expectedPath = path.resolve(value);
    else if (option === "--native-output") nativeOutputPath = path.resolve(value);
    else if (option === "--draw-text") expectedDrawText = value;
    else if (option === "--draw-text-file") expectedDrawText = fs.readFileSync(path.resolve(value), "utf8").replace(/\r?\n$/, "");
    else if (option === "--frames") maximumFrames = Number(value);
    else if (option === "--timeout") timeoutMilliseconds = Number(value);
    else fail(`unknown option ${option}`);
}
if (!Number.isInteger(maximumFrames) || maximumFrames < 1) fail("--frames must be a positive integer");
if (!Number.isInteger(timeoutMilliseconds) || timeoutMilliseconds < 1) fail("--timeout must be a positive integer");

const runtimePath = path.join(webDirectory, "smile-runtime.js");
const gamePath = path.join(webDirectory, "game.js");
if (!fs.existsSync(runtimePath) || !fs.existsSync(gamePath)) fail(`generated Web files were not found under ${webDirectory}`);

const drawnText = [];
const windowListeners = new Map();
const documentListeners = new Map();
const storage = new Map();
let requestedFrames = 0;
let imageConstructions = 0;
let audioConstructions = 0;
let audioPlays = 0;
let audioPauses = 0;
const audioSources = [];
let clipCalls = 0;
let measurementCalls = 0;
let negativeScaleCalls = 0;
let transformCalls = 0;
let bufferSourceConstructions = 0;
let bufferSourceStarts = 0;
let bufferSourceStops = 0;
const imageDraws = [];
const textDraws = [];
const fillRectangleDraws = [];
let backCanvasElement = null;
let virtualNow = 0;
const phase5Keys = [];

function addListener(target, type, listener) {
    if (!target.has(type)) target.set(type, []);
    target.get(type).push(listener);
}
function dispatch(target, type, event = {}) {
    for (const listener of target.get(type) || []) listener({ type, ...event });
}
function context2d(name) {
    const noop = () => {};
    const context = {
        beginPath: noop, closePath: noop, moveTo: noop, lineTo: noop, quadraticCurveTo: noop,
        arc: noop, rect: noop,
        clip: () => { clipCalls += 1; }, save: noop, restore: noop, translate: noop,
        setTransform: () => { transformCalls += 1; },
        scale: (x, y) => { if (x < 0 || y < 0) negativeScaleCalls += 1; },
        fill: noop, stroke: noop,
        fillRect: (...values) => {
            if (name === "back") fillRectangleDraws.push({ frame: requestedFrames, values, fillStyle: context.fillStyle });
        },
        strokeRect: noop, clearRect: noop,
        drawImage: (resource, ...values) => {
            if (name === "back" && resource && typeof resource.src === "string") {
                imageDraws.push({ source: resource.src, values, smoothing: context.imageSmoothingEnabled,
                    alpha: context.globalAlpha, frame: requestedFrames });
            }
        },
        measureText: value => {
            measurementCalls += 1;
            return { width: String(value).length * 8, actualBoundingBoxAscent: 12, actualBoundingBoxDescent: 4 };
        },
        fillText: (value, x, y) => {
            drawnText.push(String(value));
            if (name === "back") textDraws.push({ value: String(value), x, y, frame: requestedFrames,
                fillStyle: context.fillStyle, font: context.font, alignment: context.textAlign });
        },
        fillStyle: "", strokeStyle: "", lineWidth: 1, font: "", textAlign: "left",
        textBaseline: "top", imageSmoothingEnabled: false, globalAlpha: 1
    };
    return context;
}
function canvas(name = "offscreen") {
    const drawing = context2d(name);
    return {
        width: 0, height: 0, hidden: true, style: {},
        getContext: () => drawing,
        addEventListener: () => {}, setAttribute: () => {}, focus: () => {}
    };
}

const visibleCanvas = canvas("visible");
const consoleElement = { hidden: true, textContent: "", scrollTop: 0, scrollHeight: 0 };
const errorElement = { hidden: true, textContent: "" };
const shellElement = { requestFullscreen: async () => {} };
const elements = new Map([
    ["smile-canvas", visibleCanvas], ["smile-console", consoleElement],
    ["smile-error", errorElement], ["smile-shell", shellElement]
]);

const hostConsoleErrors = [];
const host = {
    console: { log: () => {}, warn: () => {}, error: error => hostConsoleErrors.push(String(error)) },
    document: {
        title: "", hidden: false, fullscreenElement: null,
        getElementById: id => elements.get(id) || null,
        createElement: tag => {
            if (tag !== "canvas") return {};
            backCanvasElement = canvas("back");
            return backCanvasElement;
        },
        addEventListener: (type, listener) => addListener(documentListeners, type, listener),
        hasFocus: () => true,
        exitFullscreen: async () => {}
    },
    localStorage: {
        getItem: key => storage.has(key) ? storage.get(key) : null,
        setItem: (key, value) => storage.set(key, String(value))
    },
    performance: { now: () => verifyPhase5Ui ? virtualNow : Date.now() },
    Audio: class {
        constructor(source) {
            audioConstructions += 1;
            this.src = source || "";
            audioSources.push(String(this.src));
            this.loop = false;
            this.volume = 1;
            this.currentTime = 0;
        }
        addEventListener() {}
        play() { audioPlays += 1; return Promise.resolve(); }
        pause() { audioPauses += 1; }
    },
    Image: class {
        constructor() { imageConstructions += 1; this.naturalWidth = this.width = 1920; this.naturalHeight = this.height = 1080; }
        set src(value) {
            this._src = value;
            const normalized = String(value).replace(/\\/g, "/");
            if (normalized.endsWith("/Background.png")) {
                this.naturalWidth = this.width = verifyPhase5Ui ? 1920 : 2304;
                this.naturalHeight = this.height = verifyPhase5Ui ? 1080 : 1296;
            }
            else if (normalized.endsWith("/WindowSkin.png")) { this.naturalWidth = this.width = 768; this.naturalHeight = this.height = 768; }
            else if (normalized.endsWith("/BitmapFont.png")) { this.naturalWidth = this.width = 1024; this.naturalHeight = this.height = 384; }
            else if (normalized.endsWith("/Cursor.png")) { this.naturalWidth = this.width = 128; this.naturalHeight = this.height = 128; }
            else if (normalized.endsWith("/Continue.png")) { this.naturalWidth = this.width = 96; this.naturalHeight = this.height = 96; }
            else if (normalized.endsWith("/CharacterSheet.png")) { this.naturalWidth = this.width = 2048; this.naturalHeight = this.height = 1024; }
            else if (normalized.endsWith("/Foreground.png")) { this.naturalWidth = this.width = 1920; this.naturalHeight = this.height = 1080; }
            else if (normalized.endsWith("/PixelProof.png")) { this.naturalWidth = this.width = 37; this.naturalHeight = this.height = 53; }
            setImmediate(() => { if (this.onload) this.onload(); });
        }
        get src() { return this._src; }
    },
    fetch: async source => {
        if (verifyPhase4Audio) {
            await new Promise(resolve => setTimeout(resolve, String(source).includes("ToneOne") ? 35 : 5));
            return { ok: true, arrayBuffer: async () => new ArrayBuffer(8) };
        }
        return { ok: false, arrayBuffer: async () => new ArrayBuffer(0) };
    },
    btoa: value => Buffer.from(value, "binary").toString("base64"),
    atob: value => Buffer.from(value, "base64").toString("binary"),
    setTimeout, clearTimeout, setImmediate, Promise, Map, Set, Uint8Array, Uint32Array, ArrayBuffer, DataView,
    innerWidth: 1280, innerHeight: 720, devicePixelRatio: 2,
    screen: { orientation: { addEventListener: () => {} } },
    visualViewport: { addEventListener: () => {} },
    addEventListener: (type, listener) => addListener(windowListeners, type, listener),
    dispatchEvent: event => dispatch(windowListeners, event.type, event),
    requestAnimationFrame: callback => {
        requestedFrames += 1;
        return setImmediate(() => {
            if (verifyPhase5Ui) {
                virtualNow = requestedFrames * 280;
                const scriptedCode = new Map([
                    [2, "ArrowDown"], [3, "ArrowDown"], [4, "ArrowDown"], [5, "ArrowDown"],
                    [6, "ArrowDown"], [7, "ArrowUp"], [8, "Digit2"], [9, "Enter"],
                    [10, "Digit3"], [11, "Digit1"], [12, "Space"], [13, "Escape"]
                ]).get(requestedFrames);
                if (scriptedCode) {
                    const event = { code: scriptedCode, repeat: false, ctrlKey: false, altKey: false,
                        metaKey: false, preventDefault: () => {} };
                    phase5Keys.push(scriptedCode);
                    dispatch(windowListeners, "keydown", event);
                    dispatch(windowListeners, "keyup", event);
                }
            }
            if (verifyPhase4Clip && requestedFrames === 2) {
                host.innerWidth = 1000;
                host.innerHeight = 700;
                dispatch(windowListeners, "resize");
            }
            if (requestedFrames >= maximumFrames) dispatch(windowListeners, "pagehide");
            callback(Date.now());
        });
    }
};
if (verifyPhase4Audio) {
    host.AudioContext = class {
        constructor() { this.state = "running"; this.destination = {}; }
        async resume() { this.state = "running"; }
        async decodeAudioData() { return {}; }
        createBufferSource() {
            bufferSourceConstructions += 1;
            return {
                connect: () => {},
                start: () => { bufferSourceStarts += 1; },
                stop: () => { bufferSourceStops += 1; },
                onended: null,
                buffer: null
            };
        }
        close() { this.state = "closed"; return Promise.resolve(); }
    };
}
host.window = host;

const context = vm.createContext(host);
try {
    vm.runInContext(fs.readFileSync(runtimePath, "utf8"), context, { filename: runtimePath });
    vm.runInContext(fs.readFileSync(gamePath, "utf8"), context, { filename: gamePath });
    if (verifyPhase4Media || verifyPhase4Audio) {
        dispatch(windowListeners, "keydown", {
            code: "KeyX", repeat: false, ctrlKey: false, altKey: false, metaKey: false,
            preventDefault: () => {}
        });
    }
    if (verifyPhase4Audio) {
        host.smile.playSound("Assets/ToneOne.wav", 5);
        host.smile.playSound("Assets/ToneTwo.wav", 5);
    }
} catch (error) {
    fail(error && error.stack ? error.stack : String(error));
}

function normalizeNewlines(value) { return value.replace(/\r\n/g, "\n"); }
function readUtf8Strict(file, rejectBom) {
    const bytes = fs.readFileSync(file);
    if (rejectBom && bytes.length >= 3 && bytes[0] === 0xef && bytes[1] === 0xbb && bytes[2] === 0xbf)
        fail(`${file} unexpectedly starts with a UTF-8 BOM`);
    try {
        return new TextDecoder("utf-8", { fatal: true }).decode(bytes);
    } catch (error) {
        fail(`${file} is not valid UTF-8: ${error.message}`);
    }
}
const started = Date.now();
(async () => {
    while (host.__smileWeb && !["stopped", "error", "completed"].includes(host.__smileWeb.status)) {
        if (Date.now() - started > timeoutMilliseconds) fail(`timed out after ${timeoutMilliseconds} ms`);
        await new Promise(resolve => setTimeout(resolve, 5));
    }
    if (!host.__smileWeb) fail("runtime did not publish window.__smileWeb");
    if (host.__smileWeb.status === "error")
        fail(errorElement.textContent || hostConsoleErrors.join("\n") || "runtime reported an unknown error");

    const actual = normalizeNewlines(consoleElement.textContent);
    let expected = null;
    if (expectedPath !== null) {
        expected = normalizeNewlines(readUtf8Strict(expectedPath, false));
        if (actual !== expected)
            fail(`console output did not match ${expectedPath}\nEXPECTED:\n${JSON.stringify(expected)}\nACTUAL:\n${JSON.stringify(actual)}`);
    }
    if (nativeOutputPath !== null) {
        const nativeOutput = normalizeNewlines(readUtf8Strict(nativeOutputPath, true));
        if (expected !== null && nativeOutput !== expected)
            fail(`native output did not match ${expectedPath}\nEXPECTED:\n${JSON.stringify(expected)}\nNATIVE:\n${JSON.stringify(nativeOutput)}`);
        if (actual !== nativeOutput)
            fail(`native/Web output differed\nNATIVE:\n${JSON.stringify(nativeOutput)}\nWEB:\n${JSON.stringify(actual)}`);
    }
    if (expectedDrawText !== null && !drawnText.includes(expectedDrawText))
        fail(`DRAW TEXT did not contain ${JSON.stringify(expectedDrawText)}; recorded ${JSON.stringify(drawnText)}`);
    if (verifyPhase4Media) {
        if (imageConstructions !== 4) fail(`Phase 4 image cache expected 4 decodes, found ${imageConstructions}`);
        if (imageDraws.length < 5) fail(`Phase 4 expected image draws, found ${imageDraws.length}`);
        const firstFrame = imageDraws.slice(0, 5).map(draw => path.basename(draw.source));
        const expectedOrder = ["Background.png", "CharacterSheet.png", "Foreground.png", "Foreground.png", "PixelProof.png"];
        if (firstFrame.join("|") !== expectedOrder.join("|"))
            fail(`Phase 4 painter order differed: ${firstFrame.join(", ")}`);
        if (!imageDraws.some(draw => draw.values.length === 8))
            fail("Phase 4 explicit source/destination rectangle was not recorded");
        if (!imageDraws.some(draw => draw.smoothing) || !imageDraws.some(draw => !draw.smoothing))
            fail("Phase 4 smooth and pixel filters were not both recorded");
        if (!imageDraws.some(draw => draw.alpha > 0 && draw.alpha < 1))
            fail("Phase 4 opacity was not recorded");
        if (negativeScaleCalls < 1) fail("Phase 4 horizontal flip was not recorded");
        if (clipCalls < 2) fail(`Phase 4 nested clips expected at least 2 clips, found ${clipCalls}`);
        if (measurementCalls < 2) fail("Phase 4 text measurement was not recorded");
        const dataKey = [...storage.keys()].find(key => key.includes(":data:"));
        if (!dataKey)
            fail("Phase 4 persistent DATA storage was not recorded");
        const envelope = Buffer.from(storage.get(dataKey), "base64");
        if (envelope.length !== 52 || envelope.subarray(0, 4).toString("ascii") !== "SMD4" ||
            envelope.readUInt32LE(4) !== 1 || envelope.readUInt32LE(8) !== 8)
            fail("Phase 4.1 Web persistent DATA envelope was malformed");
        envelope[envelope.length - 1] ^= 1;
        storage.set(dataKey, envelope.toString("base64"));
        const corruptTarget = host.smile.array([8], 9);
        let corruptRejected = false;
        try { host.smile.loadData("Phase4VisualSlice", corruptTarget); }
        catch (_) { corruptRejected = true; }
        if (!corruptRejected || corruptTarget.data.some(value => value !== 0))
            fail("Phase 4.1 Web corrupt DATA was not rejected with a zeroed destination");
        if (audioConstructions < 3 || audioPlays < 3)
            fail("Phase 4 music and overlapping SFX were not recorded");
        if (audioPauses < 3)
            fail("Phase 4 page-hide audio shutdown did not stop music and SFX channels");
        for (const invalidPath of ["../escape.png", "C:\\escape.png", "/escape.png", "\\\\server\\asset.png",
            "file:///asset.png", "https://example.invalid/asset.png", "data:image/png;base64,AA==",
            "Assets/background.png", "Assets/Bad\0Name.png"]) {
            let rejected = false;
            try { await host.smile.loadImage(invalidPath); }
            catch (_) { rejected = true; }
            if (!rejected) fail(`Phase 4.1 invalid or undeclared media path was accepted: ${JSON.stringify(invalidPath)}`);
        }
    }
    const diagnostics = host.smile.mediaDiagnostics();
    if (verifyPhase4Media || verifyPhase4Ownership || verifyPhase4Clip) {
        if (diagnostics.backingWidth !== visibleCanvas.width || diagnostics.backingHeight !== visibleCanvas.height ||
            diagnostics.backingWidth !== backCanvasElement.width || diagnostics.backingHeight !== backCanvasElement.height)
            fail("Phase 4.1 visible/backing canvas physical sizes diverged");
        if (diagnostics.backingWidth <= diagnostics.logicalWidth || diagnostics.backingHeight <= diagnostics.logicalHeight)
            fail(`Phase 4.1 DPR backing store was not high resolution: ${diagnostics.backingWidth}x${diagnostics.backingHeight}`);
        if (transformCalls < 2) fail("Phase 4.1 logical-to-physical canvas transforms were not restored");
    }
    if (verifyPhase4Ownership) {
        if (diagnostics.shutdownImageCacheEntries !== 0 || diagnostics.shutdownImageReferences !== 0 ||
            diagnostics.imageCacheCount !== 0 || diagnostics.imageReferenceCount !== 0)
            fail(`Phase 4.1 IMAGE ownership leaked: ${JSON.stringify(diagnostics)}`);
        if (diagnostics.imageDecodeCount !== 1)
            fail(`Phase 4.1 IMAGE ownership expected one decode, found ${diagnostics.imageDecodeCount}`);
    }
    if (verifyPhase4Clip) {
        if (clipCalls < 2) fail(`Phase 4.1 clip was not reapplied after resize: ${clipCalls}`);
        if (diagnostics.clipDepth !== 0) fail(`Phase 4.1 clip stack did not unwind: ${diagnostics.clipDepth}`);
    }
    if (verifyPhase4Audio) {
        if (bufferSourceConstructions !== 1 || bufferSourceStarts !== 1)
            fail(`Phase 4.1 stale same-channel audio started (${bufferSourceConstructions} sources, ${bufferSourceStarts} starts)`);
        if (bufferSourceStops < 1 || diagnostics.sfxActiveCount !== 0 || !diagnostics.mediaStopped)
            fail(`Phase 4.1 audio shutdown was incomplete: ${JSON.stringify(diagnostics)}`);
    }
    if (verifyPhase5Ui) {
        const expectedKeys = ["ArrowDown", "ArrowDown", "ArrowDown", "ArrowDown", "ArrowDown", "ArrowUp",
            "Digit2", "Enter", "Digit3", "Digit1", "Space", "Escape"];
        if (phase5Keys.join("|") !== expectedKeys.join("|"))
            fail(`Phase 5 scripted key sequence differed: ${phase5Keys.join(", ")}`);
        const basenames = imageDraws.map(draw => path.basename(draw.source));
        for (const required of ["Background.png", "WindowSkin.png", "Cursor.png", "Continue.png", "BitmapFont.png"])
            if (!basenames.includes(required)) fail(`Phase 5 Web draws did not include ${required}`);
        const frameImageNames = new Map();
        for (const draw of imageDraws) {
            if (!frameImageNames.has(draw.frame)) frameImageNames.set(draw.frame, []);
            frameImageNames.get(draw.frame).push(path.basename(draw.source));
        }
        for (const [frame, names] of frameImageNames) {
            if (names.length !== 0 && names[0] !== "Background.png")
                fail(`Phase 5 painter order did not begin with Background.png on frame ${frame}: ${names[0]}`);
        }
        const cursorYs = new Set(imageDraws.filter(draw => path.basename(draw.source) === "Cursor.png" && draw.values.length === 8)
            .map(draw => draw.values[5]));
        const cursorByFrame = new Map(imageDraws.filter(draw => path.basename(draw.source) === "Cursor.png" && draw.values.length === 8)
            .map(draw => [draw.frame, draw.values[5]]));
        if (cursorYs.size < 5 || cursorByFrame.get(4) !== 230 || cursorByFrame.get(5) !== 316)
            fail(`Phase 5 disabled-item skipping/scroll cursor positions differed: ${JSON.stringify([...cursorYs])}`);
        const frameSixText = textDraws.filter(draw => draw.frame === 6).map(draw => draw.value);
        if (!frameSixText.includes("OPTIONS") || frameSixText.includes("ITEM"))
            fail(`Phase 5 scrolling did not expose OPTIONS and remove ITEM: ${JSON.stringify(frameSixText)}`);
        if (clipCalls < maximumFrames)
            fail(`Phase 5 expected structured clipping on each frame, found ${clipCalls} clips across ${maximumFrames} frames`);
        if (fillRectangleDraws.length < 1)
            fail("Phase 5 vector fallback did not record rectangle drawing");
        for (const required of ["Move.wav", "Confirm.wav", "Cancel.wav"])
            if (!audioSources.some(source => path.basename(source) === required))
                fail(`Phase 5 event-driven SFX did not include ${required}: ${JSON.stringify(audioSources)}`);
        if (diagnostics.backingWidth <= diagnostics.logicalWidth || diagnostics.backingHeight <= diagnostics.logicalHeight)
            fail(`Phase 5 DPR backing store was not high resolution: ${diagnostics.backingWidth}x${diagnostics.backingHeight}`);
        if (diagnostics.imageCacheCount !== 0 || diagnostics.imageReferenceCount !== 0 ||
            diagnostics.sfxActiveCount !== 0 || !diagnostics.mediaStopped)
            fail(`Phase 5 Web ownership/shutdown was incomplete: ${JSON.stringify(diagnostics)}`);
        if (hostConsoleErrors.length !== 0)
            fail(`Phase 5 Web console reported errors: ${hostConsoleErrors.join("\n")}`);
    }
    if (verifyPhase5Hardening) {
        const basenames = imageDraws.map(draw => path.basename(draw.source));
        for (const required of ["WindowSkin.png", "Cursor.png", "BitmapFont.png"])
            if (!basenames.includes(required)) fail(`Phase 5.1 Web draws did not include ${required}`);
        if (!imageDraws.some(draw => draw.smoothing === true) || !imageDraws.some(draw => draw.smoothing === false))
            fail("Phase 5.1 smooth and pixel UI filters were not both recorded");
        const bitmapLineYs = new Set(imageDraws.filter(draw => path.basename(draw.source) === "BitmapFont.png")
            .map(draw => draw.values[5]));
        if (bitmapLineYs.size < 2)
            fail(`Phase 5.1 bitmap multiline drawing did not span two lines: ${JSON.stringify([...bitmapLineYs])}`);
        const systemLines = textDraws.map(draw => draw.value);
        if (!systemLines.includes("SYSTEM") || !systemLines.includes("MULTILINE") || systemLines.includes("HIDDEN"))
            fail(`Phase 5.1 system multiline/opacity drawing differed: ${JSON.stringify(systemLines)}`);
        const tinyScrollbar = fillRectangleDraws.filter(draw => draw.values[0] === 625 && draw.values[2] === 4);
        if (tinyScrollbar.length !== 2 || tinyScrollbar.some(draw => draw.values[1] < 350 || draw.values[3] < 0 || draw.values[1] + draw.values[3] > 353))
            fail(`Phase 5.1 tiny scrollbar escaped its track: ${JSON.stringify(tinyScrollbar)}`);
        if (diagnostics.backingWidth <= diagnostics.logicalWidth || diagnostics.backingHeight <= diagnostics.logicalHeight)
            fail(`Phase 5.1 DPR backing store was not high resolution: ${diagnostics.backingWidth}x${diagnostics.backingHeight}`);
        if (diagnostics.imageCacheCount !== 0 || diagnostics.imageReferenceCount !== 0 || !diagnostics.mediaStopped)
            fail(`Phase 5.1 Web ownership/shutdown was incomplete: ${JSON.stringify(diagnostics)}`);
        if (hostConsoleErrors.length !== 0)
            fail(`Phase 5.1 Web console reported errors: ${hostConsoleErrors.join("\n")}`);
    }

    process.stdout.write(`Web execution passed: ${webDirectory}`);
    if (expectedPath !== null || nativeOutputPath !== null) process.stdout.write(" (exact console parity)");
    if (expectedDrawText !== null) process.stdout.write(" (dynamic DRAW TEXT parity)");
    if (verifyPhase4Media) process.stdout.write(" (Phase 4 media/cache/clip/data/audio parity)");
    if (verifyPhase4Ownership) process.stdout.write(" (Phase 4.1 IMAGE ownership/high-DPI parity)");
    if (verifyPhase4Clip) process.stdout.write(" (Phase 4.1 clip/high-DPI resize parity)");
    if (verifyPhase4Audio) process.stdout.write(" (Phase 4.1 audio generation/shutdown parity)");
    if (verifyPhase5Ui) process.stdout.write(" (Phase 5 scripted UI/high-DPI/painter/audio/ownership parity)");
    if (verifyPhase5Hardening) process.stdout.write(" (Phase 5.1 validation/reflow/multiline/high-DPI/ownership parity)");
    process.stdout.write("\n");
})().catch(error => fail(error && error.stack ? error.stack : String(error)));
