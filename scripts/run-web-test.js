"use strict";

const fs = require("fs");
const path = require("path");
const vm = require("vm");

function fail(message) {
    process.stderr.write(`Web execution failed: ${message}\n`);
    process.exit(1);
}

const args = process.argv.slice(2);
if (args.length === 0) fail("usage: node scripts/run-web-test.js <web-directory> [--expected <file>] [--native-output <file>] [--draw-text <value> | --draw-text-file <file>] [--frames <count>] [--timeout <ms>] [--phase4-media]");

const webDirectory = path.resolve(args.shift());
let expectedPath = null;
let nativeOutputPath = null;
let expectedDrawText = null;
let maximumFrames = 3;
let timeoutMilliseconds = 5000;
let verifyPhase4Media = false;
while (args.length !== 0) {
    const option = args.shift();
    if (option === "--phase4-media") {
        verifyPhase4Media = true;
        continue;
    }
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
let clipCalls = 0;
let measurementCalls = 0;
let negativeScaleCalls = 0;
const imageDraws = [];

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
        scale: (x, y) => { if (x < 0 || y < 0) negativeScaleCalls += 1; },
        fill: noop, stroke: noop, fillRect: noop, strokeRect: noop, clearRect: noop,
        drawImage: (resource, ...values) => {
            if (name === "back" && resource && typeof resource.src === "string") {
                imageDraws.push({ source: resource.src, values, smoothing: context.imageSmoothingEnabled,
                    alpha: context.globalAlpha });
            }
        },
        measureText: value => {
            measurementCalls += 1;
            return { width: String(value).length * 8, actualBoundingBoxAscent: 12, actualBoundingBoxDescent: 4 };
        },
        fillText: value => drawnText.push(String(value)),
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
        createElement: tag => tag === "canvas" ? canvas("back") : {},
        addEventListener: (type, listener) => addListener(documentListeners, type, listener),
        hasFocus: () => true,
        exitFullscreen: async () => {}
    },
    localStorage: {
        getItem: key => storage.has(key) ? storage.get(key) : null,
        setItem: (key, value) => storage.set(key, String(value))
    },
    performance: { now: () => Date.now() },
    Audio: class {
        constructor(source) { audioConstructions += 1; this.src = source || ""; this.loop = false; this.volume = 1; this.currentTime = 0; }
        addEventListener() {}
        play() { audioPlays += 1; return Promise.resolve(); }
        pause() { audioPauses += 1; }
    },
    Image: class {
        constructor() { imageConstructions += 1; this.naturalWidth = this.width = 1920; this.naturalHeight = this.height = 1080; }
        set src(value) {
            this._src = value;
            const normalized = String(value).replace(/\\/g, "/");
            if (normalized.endsWith("/Background.png")) { this.naturalWidth = this.width = 2304; this.naturalHeight = this.height = 1296; }
            else if (normalized.endsWith("/CharacterSheet.png")) { this.naturalWidth = this.width = 2048; this.naturalHeight = this.height = 1024; }
            else if (normalized.endsWith("/Foreground.png")) { this.naturalWidth = this.width = 1920; this.naturalHeight = this.height = 1080; }
            else if (normalized.endsWith("/PixelProof.png")) { this.naturalWidth = this.width = 37; this.naturalHeight = this.height = 53; }
            setImmediate(() => { if (this.onload) this.onload(); });
        }
        get src() { return this._src; }
    },
    fetch: async () => ({ ok: false, arrayBuffer: async () => new ArrayBuffer(0) }),
    btoa: value => Buffer.from(value, "binary").toString("base64"),
    atob: value => Buffer.from(value, "base64").toString("binary"),
    setTimeout, clearTimeout, setImmediate, Promise, Map, Set, Uint8Array, ArrayBuffer,
    innerWidth: 1280, innerHeight: 720,
    addEventListener: (type, listener) => addListener(windowListeners, type, listener),
    dispatchEvent: event => dispatch(windowListeners, event.type, event),
    requestAnimationFrame: callback => {
        requestedFrames += 1;
        return setImmediate(() => {
            if (requestedFrames >= maximumFrames) dispatch(windowListeners, "pagehide");
            callback(Date.now());
        });
    }
};
host.window = host;

const context = vm.createContext(host);
try {
    vm.runInContext(fs.readFileSync(runtimePath, "utf8"), context, { filename: runtimePath });
    vm.runInContext(fs.readFileSync(gamePath, "utf8"), context, { filename: gamePath });
    if (verifyPhase4Media) {
        dispatch(windowListeners, "keydown", {
            code: "KeyX", repeat: false, ctrlKey: false, altKey: false, metaKey: false,
            preventDefault: () => {}
        });
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
        if (![...storage.keys()].some(key => key.includes("data:Phase4VisualSlice")))
            fail("Phase 4 persistent DATA storage was not recorded");
        if (audioConstructions < 3 || audioPlays < 3)
            fail("Phase 4 music and overlapping SFX were not recorded");
        if (audioPauses < 3)
            fail("Phase 4 page-hide audio shutdown did not stop music and SFX channels");
    }

    process.stdout.write(`Web execution passed: ${webDirectory}`);
    if (expectedPath !== null || nativeOutputPath !== null) process.stdout.write(" (exact console parity)");
    if (expectedDrawText !== null) process.stdout.write(" (dynamic DRAW TEXT parity)");
    if (verifyPhase4Media) process.stdout.write(" (Phase 4 media/cache/clip/data/audio parity)");
    process.stdout.write("\n");
})().catch(error => fail(error && error.stack ? error.stack : String(error)));
