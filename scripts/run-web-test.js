"use strict";

const fs = require("fs");
const path = require("path");
const vm = require("vm");

function fail(message) {
    process.stderr.write(`Web execution failed: ${message}\n`);
    process.exit(1);
}

const args = process.argv.slice(2);
if (args.length === 0) fail("usage: node scripts/run-web-test.js <web-directory> [--expected <file>] [--native-output <file>] [--draw-text <value> | --draw-text-file <file>] [--frames <count>] [--timeout <ms>]");

const webDirectory = path.resolve(args.shift());
let expectedPath = null;
let nativeOutputPath = null;
let expectedDrawText = null;
let maximumFrames = 3;
let timeoutMilliseconds = 5000;
while (args.length !== 0) {
    const option = args.shift();
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

function addListener(target, type, listener) {
    if (!target.has(type)) target.set(type, []);
    target.get(type).push(listener);
}
function dispatch(target, type, event = {}) {
    for (const listener of target.get(type) || []) listener({ type, ...event });
}
function context2d() {
    const noop = () => {};
    return {
        beginPath: noop, closePath: noop, moveTo: noop, lineTo: noop, quadraticCurveTo: noop,
        arc: noop, fill: noop, stroke: noop, fillRect: noop, strokeRect: noop, clearRect: noop,
        drawImage: noop,
        fillText: value => drawnText.push(String(value)),
        fillStyle: "", strokeStyle: "", lineWidth: 1, font: "", textAlign: "left",
        textBaseline: "top", imageSmoothingEnabled: false
    };
}
function canvas() {
    const drawing = context2d();
    return {
        width: 0, height: 0, hidden: true, style: {},
        getContext: () => drawing,
        addEventListener: () => {}, setAttribute: () => {}, focus: () => {}
    };
}

const visibleCanvas = canvas();
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
        createElement: tag => tag === "canvas" ? canvas() : {},
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
        constructor() { this.loop = false; this.volume = 1; this.currentTime = 0; }
        addEventListener() {}
        play() { return Promise.resolve(); }
        pause() {}
    },
    fetch: async () => ({ ok: false, arrayBuffer: async () => new ArrayBuffer(0) }),
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

    process.stdout.write(`Web execution passed: ${webDirectory}`);
    if (expectedPath !== null || nativeOutputPath !== null) process.stdout.write(" (exact console parity)");
    if (expectedDrawText !== null) process.stdout.write(" (dynamic DRAW TEXT parity)");
    process.stdout.write("\n");
})().catch(error => fail(error && error.stack ? error.stack : String(error)));
