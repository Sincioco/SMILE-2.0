"use strict";

const fs = require("fs");
const path = require("path");
const vm = require("vm");

function fail(message) {
    process.stderr.write(`Web execution failed: ${message}\n`);
    process.exit(1);
}

const args = process.argv.slice(2);
if (args.length === 0) fail("usage: node scripts/run-web-test.js <web-directory> [--expected <file>] [--native-output <file>] [--expected-runtime-error <text>] [--draw-text <value> | --draw-text-file <file>] [--frames <count>] [--timeout <ms>] [--phase4-media|--phase4-ownership|--phase4-clip|--phase4-audio|--phase5-ui|--phase5-hardening|--phase5-submenus|--phase5-submenu-viewport|--mobile-controls|--renderer3d|--renderer3d-gpu-particles|--force-renderer3d-gpu-particle-shader-failure|--force-renderer3d-gpu-particle-attribute-failure|--force-renderer3d-pbr-failure|--force-renderer3d-hdr-failure|--force-renderer3d-shadow-failure|--force-renderer3d-soft-depth-failure|--force-renderer3d-distortion-failure|--neon-cycles-input]");

const webDirectory = path.resolve(args.shift());
let expectedPath = null;
let nativeOutputPath = null;
let expectedRuntimeError = null;
let expectedDrawText = null;
let maximumFrames = 3;
let timeoutMilliseconds = 5000;
let verifyPhase4Media = false;
let verifyPhase4Ownership = false;
let verifyPhase4Clip = false;
let verifyPhase4Audio = false;
let verifyPhase5Ui = false;
let verifyPhase5Hardening = false;
let verifyPhase5Submenus = false;
let verifyPhase5SubmenuViewport = false;
let verifyMobileControls = false;
let verifyFileTransfer = false;
let verifyDataStatus = false;
let deniedDataKey = null;
let verifyRenderer3D = false;
let renderer3DStateOnly = false;
let verifyRenderer3DGpuParticles = false;
let forceRenderer3DGpuParticleShaderFailure = false;
let forceRenderer3DGpuParticleAttributeFailure = false;
let forceRenderer3DPbrFailure = false;
let forceRenderer3DHdrFailure = false;
let forceRenderer3DShadowFailure = false;
let forceRenderer3DSoftDepthFailure = false;
let forceRenderer3DDistortionFailure = false;
let verifyNeonCyclesInput = false;
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
    if (option === "--phase5-submenus") { verifyPhase5Submenus = true; continue; }
    if (option === "--phase5-submenu-viewport") { verifyPhase5SubmenuViewport = true; continue; }
    if (option === "--mobile-controls") { verifyMobileControls = true; continue; }
    if (option === "--file-transfer") { verifyFileTransfer = true; continue; }
    if (option === "--data-status") { verifyDataStatus = true; continue; }
    if (option === "--renderer3d") { verifyRenderer3D = true; continue; }
    // Model/calibration console fixtures need the GL double but do not present a 3D frame.
    if (option === "--renderer3d-state") { verifyRenderer3D = true; renderer3DStateOnly = true; continue; }
    if (option === "--renderer3d-gpu-particles") {
        verifyRenderer3D = true;
        verifyRenderer3DGpuParticles = true;
        continue;
    }
    if (option === "--force-renderer3d-gpu-particle-shader-failure") {
        forceRenderer3DGpuParticleShaderFailure = true;
        continue;
    }
    if (option === "--force-renderer3d-gpu-particle-attribute-failure") {
        forceRenderer3DGpuParticleAttributeFailure = true;
        continue;
    }
    if (option === "--force-renderer3d-pbr-failure") { forceRenderer3DPbrFailure = true; continue; }
    if (option === "--force-renderer3d-hdr-failure") { forceRenderer3DHdrFailure = true; continue; }
    if (option === "--force-renderer3d-shadow-failure") { forceRenderer3DShadowFailure = true; continue; }
    if (option === "--force-renderer3d-soft-depth-failure") { forceRenderer3DSoftDepthFailure = true; continue; }
    if (option === "--force-renderer3d-distortion-failure") { forceRenderer3DDistortionFailure = true; continue; }
    if (option === "--neon-cycles-input") { verifyNeonCyclesInput = true; continue; }
    const value = args.shift();
    if (value === undefined) fail(`missing value for ${option}`);
    if (option === "--expected") expectedPath = path.resolve(value);
    else if (option === "--deny-data-key") deniedDataKey = require("crypto").createHash("sha256").update(value).digest("hex");
    else if (option === "--native-output") nativeOutputPath = path.resolve(value);
    else if (option === "--expected-runtime-error") expectedRuntimeError = value;
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

function mobileAssert(condition, message) {
    if (!condition) throw new Error(`mobile-controls: ${message}`);
}

function mobileEqual(actual, expected, message) {
    if (actual !== expected)
        throw new Error(`mobile-controls: ${message}; expected ${JSON.stringify(expected)}, found ${JSON.stringify(actual)}`);
}

function createMobileEventTarget(initial = {}) {
    const listeners = new Map();
    const attributes = new Map();
    const classes = new Set();
    const captures = new Set();
    const target = { hidden: false, style: {}, dataset: {}, ...initial };
    target.addEventListener = (type, listener) => {
        if (!listeners.has(type)) listeners.set(type, []);
        listeners.get(type).push(listener);
    };
    target.dispatch = (type, event = {}) => {
        const payload = { type, target, currentTarget: target, defaultPrevented: false, ...event };
        payload.preventDefault = () => { payload.defaultPrevented = true; };
        for (const listener of listeners.get(type) || []) listener(payload);
        return payload;
    };
    target.removeEventListener = (type, listener) => {
        listeners.set(type, (listeners.get(type) || []).filter(item => item !== listener));
    };
    target.setAttribute = (name, value) => attributes.set(name, String(value));
    target.getAttribute = name => attributes.has(name) ? attributes.get(name) : null;
    target.classList = {
        add: name => classes.add(name),
        remove: name => classes.delete(name),
        contains: name => classes.has(name),
        toggle: (name, force) => {
            const enabled = force === undefined ? !classes.has(name) : Boolean(force);
            if (enabled) classes.add(name); else classes.delete(name);
            return enabled;
        }
    };
    target.setPointerCapture = pointerId => captures.add(pointerId);
    target.releasePointerCapture = pointerId => captures.delete(pointerId);
    target.hasPointerCapture = pointerId => captures.has(pointerId);
    return target;
}

function createMobileControlsHost(options = {}) {
    const windowListeners = new Map();
    const documentListeners = new Map();
    const orientationListeners = new Map();
    const add = (listeners, type, listener) => {
        if (!listeners.has(type)) listeners.set(type, []);
        listeners.get(type).push(listener);
    };
    const dispatch = (listeners, target, type, event = {}) => {
        const payload = { type, target, currentTarget: target, defaultPrevented: false, ...event };
        payload.preventDefault = () => { payload.defaultPrevented = true; };
        for (const listener of listeners.get(type) || []) listener(payload);
        return payload;
    };
    const drawing = {
        setTransform() {}, clearRect() {}, drawImage() {}, save() {}, beginPath() {}, rect() {}, clip() {},
        restore() {}, fillRect() {}, strokeRect() {}, moveTo() {}, lineTo() {}, quadraticCurveTo() {},
        closePath() {}, fill() {}, stroke() {}, arc() {}, translate() {}, scale() {}, fillText() {},
        measureText: value => ({ width: String(value).length * 8, actualBoundingBoxAscent: 12, actualBoundingBoxDescent: 4 }),
        globalAlpha: 1, imageSmoothingEnabled: true
    };
    const canvas = createMobileEventTarget({ hidden: true, width: 960, height: 540 });
    canvas.getContext = () => drawing;
    canvas.focus = () => {};
    canvas.getBoundingClientRect = () => ({ left: 100, top: 50, right: 1060, bottom: 590, width: 960, height: 540 });
    const consoleElement = createMobileEventTarget({ hidden: true, textContent: "", scrollTop: 0, scrollHeight: 0 });
    const errorElement = createMobileEventTarget({ hidden: true, textContent: "" });
    const shellElement = createMobileEventTarget();
    shellElement.requestFullscreen = async () => {};
    const names = ["up", "down", "left", "right", "a", "b", "x", "y"];
    const buttons = new Map(names.map(name => {
        const button = createMobileEventTarget({ dataset: { smileControl: name } });
        button.setAttribute("aria-pressed", "false");
        return [name, button];
    }));
    const unknownButton = createMobileEventTarget({ dataset: { smileControl: "unknown" } });
    unknownButton.setAttribute("aria-pressed", "false");
    const controls = createMobileEventTarget({ hidden: true });
    controls.setAttribute("aria-hidden", "true");
    controls.querySelectorAll = selector => selector === "button[data-smile-control]"
        ? [...buttons.values(), unknownButton]
        : [];
    const elements = new Map([
        ["smile-canvas", canvas], ["smile-console", consoleElement], ["smile-error", errorElement],
        ["smile-shell", shellElement], ["smile-controls", controls]
    ]);
    let audioPlays = 0;
    let audioPauses = 0;
    const transferElements = [];
    const transferUrls = new Map();
    const downloads = [];
    const host = {
        console: { log() {}, warn() {}, error() {} },
        document: {
            title: "", hidden: false, fullscreenElement: null,
            body: { appendChild(element) { transferElements.push(element); } },
            getElementById: id => elements.get(id) || null,
            createElement: tag => {
                if (tag !== "canvas") {
                    const element = createMobileEventTarget();
                    element.remove = () => { const index = transferElements.indexOf(element); if (index >= 0) transferElements.splice(index, 1); };
                    element.click = () => {
                        if (tag === "a") downloads.push({ name: element.download, blob: transferUrls.get(element.href) });
                    };
                    return element;
                }
                const offscreen = createMobileEventTarget({ width: 0, height: 0 });
                offscreen.getContext = () => drawing;
                return offscreen;
            },
            addEventListener: (type, listener) => add(documentListeners, type, listener),
            hasFocus: () => !host.document.hidden,
            exitFullscreen: async () => {}
        },
        navigator: { maxTouchPoints: options.maxTouchPoints || 0 },
        location: { search: options.search || "" },
        matchMedia: query => ({
            matches: query === "(pointer: coarse)" ? Boolean(options.coarsePointer) :
                query === "(hover: none)" ? Boolean(options.noHover) : false
        }),
        localStorage: options.localStorage || { getItem: () => null, setItem() {} },
        performance: { now: () => 0 },
        Audio: class {
            constructor(source) { this.src = source; this.loop = false; this.volume = 1; this.currentTime = 0; }
            addEventListener() {}
            play() { audioPlays += 1; return Promise.resolve(); }
            pause() { audioPauses += 1; }
        },
        Image: class {},
        fetch: async () => ({ ok: false, arrayBuffer: async () => new ArrayBuffer(0) }),
        btoa: value => Buffer.from(value, "binary").toString("base64"),
        atob: value => Buffer.from(value, "base64").toString("binary"),
        URLSearchParams,
        Blob, TextDecoder,
        URL: {
            createObjectURL(blob) { const key = `blob:test-${transferUrls.size}`; transferUrls.set(key, blob); return key; },
            revokeObjectURL(key) { transferUrls.delete(key); }
        },
        setTimeout, clearTimeout, setImmediate, Promise, Map, Set, Uint8Array, Uint32Array, ArrayBuffer, DataView,
        innerWidth: 1280, innerHeight: 720, devicePixelRatio: 2,
        screen: { orientation: { addEventListener: (type, listener) => add(orientationListeners, type, listener) } },
        visualViewport: { addEventListener() {} },
        addEventListener: (type, listener) => add(windowListeners, type, listener),
        requestAnimationFrame: callback => setImmediate(() => callback(0))
    };
    host.window = host;
    const context = vm.createContext(host);
    vm.runInContext(fs.readFileSync(runtimePath, "utf8"), context, { filename: runtimePath });
    const dispatchWindow = (type, event = {}) => dispatch(windowListeners, host, type, event);
    const dispatchDocument = (type, event = {}) => dispatch(documentListeners, host.document, type, event);
    const dispatchOrientation = () => dispatch(orientationListeners, host.screen.orientation, "change");
    const pointer = (controlName, type, pointerId, pointerType = "touch", button = 0) => {
        const control = controlName === "unknown" ? unknownButton : buttons.get(controlName);
        const event = control.dispatch(type, { pointerId, pointerType, button });
        if (type === "pointerdown" || type === "pointerup" || type === "pointercancel")
            dispatchWindow(type, { target: control, pointerId, pointerType, button });
        return event;
    };
    const keyboard = (type, code, extras = {}) => dispatchWindow(type, {
        code, repeat: false, ctrlKey: false, altKey: false, metaKey: false, ...extras
    });
    const canvasPointer = (type, pointerId, clientX, clientY, pointerType = "mouse", button = 0) =>
        canvas.dispatch(type, { pointerId, clientX, clientY, pointerType, button });
    const canvasWheel = (clientX, clientY, deltaY) => canvas.dispatch("wheel", { clientX, clientY, deltaY });
    return {
        host, canvas, controls, buttons, unknownButton, errorElement,
        transferElements, transferUrls, downloads,
        dispatchWindow, dispatchDocument, dispatchOrientation, pointer, keyboard, canvasPointer, canvasWheel,
        audioPlays: () => audioPlays, audioPauses: () => audioPauses
    };
}

function drainKeys(smile) {
    const values = [];
    for (;;) {
        const value = smile.getKey();
        if (value === 0) return values;
        values.push(value);
    }
}

function assertReleased(environment, message) {
    const diagnostics = environment.host.smile.mediaDiagnostics();
    mobileEqual(diagnostics.virtualActivePointerCount, 0, `${message} active pointer count`);
    mobileEqual(diagnostics.activeInputSourceCount, 0, `${message} active source count`);
    for (const button of environment.buttons.values())
        mobileEqual(button.getAttribute("aria-pressed"), "false", `${message} aria-pressed state`);
}

async function runMobileControlsTests() {
    const desktop = createMobileControlsHost();
    mobileEqual(desktop.controls.hidden, true, "Desktop Auto starts hidden before Game Window");
    desktop.host.smile.gameWindow("Desktop", 960, 540);
    mobileEqual(desktop.controls.hidden, true, "Desktop Auto remains hidden after Game Window");
    desktop.keyboard("keydown", "ArrowUp");
    mobileEqual(desktop.host.smile.keyHeld(10), 1, "desktop keyboard held state");
    mobileEqual(desktop.host.smile.getKey(), 10, "desktop keyboard queue value");
    desktop.keyboard("keyup", "ArrowUp");
    mobileEqual(desktop.host.smile.keyHeld(10), 0, "desktop keyboard release");
    mobileEqual(desktop.host.smile.mediaDiagnostics().virtualControlsMode, "auto", "Desktop Auto diagnostics mode");

    desktop.canvasPointer("pointermove", 1, 580, 320);
    mobileEqual(desktop.host.smile.pointerX(), 480, "canvas pointer maps center X to logical pixels");
    mobileEqual(desktop.host.smile.pointerY(), 270, "canvas pointer maps center Y to logical pixels");
    mobileEqual(desktop.host.smile.pointerInside(), 1, "canvas pointer reports inside");
    desktop.canvasPointer("pointermove", 1, 1060, 590);
    mobileEqual(desktop.host.smile.pointerDeltaX(), 480, "canvas pointer accumulates X delta");
    mobileEqual(desktop.host.smile.pointerDeltaY(), 270, "canvas pointer accumulates Y delta");
    mobileEqual(desktop.host.smile.pointerInside(), 0, "bottom-right exclusive edge reports outside");
    desktop.canvasPointer("pointerdown", 2, 300, 200, "mouse", 0);
    mobileEqual(desktop.host.smile.pointerHeld(1), 1, "primary canvas pointer held state");
    mobileEqual(desktop.host.smile.pointerPressed(1), 1, "primary canvas pointer pressed transition");
    desktop.canvasPointer("pointerup", 2, 320, 210, "mouse", 0);
    mobileEqual(desktop.host.smile.pointerHeld(1), 0, "primary canvas pointer release clears held state");
    mobileEqual(desktop.host.smile.pointerReleased(1), 1, "primary canvas pointer released transition");
    desktop.canvasPointer("pointerdown", 3, 320, 210, "mouse", 2);
    mobileEqual(desktop.canvas.dispatch("contextmenu", {}).defaultPrevented, true,
        "canvas context menu is suppressed without suppressing secondary input");
    mobileEqual(desktop.dispatchWindow("contextmenu", {}).defaultPrevented, false,
        "context menu outside the canvas remains a browser concern");
    desktop.canvasPointer("pointerdown", 4, 340, 220, "mouse", 1);
    mobileEqual(desktop.host.smile.pointerHeld(2), 1, "secondary canvas pointer held state");
    mobileEqual(desktop.host.smile.pointerHeld(3), 1, "middle canvas pointer held state");
    desktop.canvasPointer("pointercancel", 3, 320, 210, "mouse", 2);
    desktop.canvasPointer("lostpointercapture", 4, 340, 220, "mouse", 1);
    mobileEqual(desktop.host.smile.pointerHeld(2), 0, "canvas pointercancel clears secondary");
    mobileEqual(desktop.host.smile.pointerHeld(3), 0, "canvas lost capture clears middle");
    desktop.canvasWheel(580, 320, -100);
    desktop.canvasWheel(580, 320, 100);
    desktop.canvasWheel(580, 320, -100);
    mobileEqual(desktop.host.smile.pointerWheelDelta(), 1, "canvas wheel accumulates signed steps");
    await desktop.host.smile.showScreen();
    mobileEqual(desktop.host.smile.pointerDeltaX(), 0, "Show Screen clears canvas X delta");
    mobileEqual(desktop.host.smile.pointerDeltaY(), 0, "Show Screen clears canvas Y delta");
    mobileEqual(desktop.host.smile.pointerWheelDelta(), 0, "Show Screen clears canvas wheel delta");
    mobileEqual(desktop.host.smile.pointerPressed(1), 0, "Show Screen clears pressed transitions");
    mobileEqual(desktop.host.smile.pointerReleased(1), 0, "Show Screen clears released transitions");

    desktop.canvasPointer("pointerdown", 5, 400, 300, "touch", 0);
    desktop.pointer("a", "pointerdown", 6, "touch", 0);
    mobileEqual(desktop.host.smile.pointerHeld(1), 1, "canvas touch is tracked as pointer input");
    mobileEqual(desktop.host.smile.getKey(), 23, "virtual button remains isolated as key input");
    desktop.pointer("a", "pointerup", 6, "touch", 0);
    desktop.dispatchWindow("blur");
    mobileEqual(desktop.host.smile.pointerHeld(1), 0, "window blur clears canvas pointer state");
    mobileEqual(desktop.host.smile.mediaDiagnostics().canvasActivePointerCount, 0,
        "window blur clears active canvas pointers");

    const touchFirst = createMobileControlsHost({ maxTouchPoints: 5, coarsePointer: true, noHover: true });
    mobileEqual(touchFirst.controls.hidden, true, "touch-first Auto starts hidden before Game Window");
    touchFirst.host.smile.gameWindow("Touch", 960, 540);
    mobileEqual(touchFirst.controls.hidden, false, "touch-first Auto shows after Game Window");
    mobileEqual(touchFirst.controls.getAttribute("aria-hidden"), "false", "visible controls expose accessible state");
    mobileEqual(touchFirst.host.document.getElementById("smile-shell").classList.contains("smile-controls-visible"), true,
        "visible controls enable the portrait layout hook");
    touchFirst.pointer("a", "pointerdown", 1, "mouse", 0);
    mobileEqual(touchFirst.host.smile.getKey(), 23, "visible Auto controls accept primary mouse input");
    touchFirst.pointer("a", "pointerup", 1, "mouse", 0);

    const forcedOff = createMobileControlsHost({ search: "?smile-controls=off", maxTouchPoints: 5, coarsePointer: true });
    forcedOff.host.smile.gameWindow("Off", 960, 540);
    forcedOff.dispatchWindow("pointerdown", { pointerId: 1, pointerType: "touch", button: 0 });
    mobileEqual(forcedOff.controls.hidden, true, "Forced Off wins over capabilities and observed touch");
    forcedOff.pointer("up", "pointerdown", 2, "touch", 0);
    mobileEqual(forcedOff.host.smile.getKey(), 0, "Forced Off cannot create virtual input");

    const hybrid = createMobileControlsHost({ maxTouchPoints: 5 });
    hybrid.host.smile.gameWindow("Hybrid", 960, 540);
    mobileEqual(hybrid.controls.hidden, true, "hybrid Auto initially hides controls");
    hybrid.dispatchWindow("pointerdown", { pointerId: 8, pointerType: "pen", button: 0 });
    mobileEqual(hybrid.controls.hidden, false, "observed touch or pen reveals hybrid controls");
    mobileEqual(hybrid.host.smile.getKey(), 0, "hybrid reveal does not enqueue input");

    const unknownMode = createMobileControlsHost({ search: "?smile-controls=unexpected" });
    unknownMode.host.smile.gameWindow("Unknown", 960, 540);
    mobileEqual(unknownMode.controls.hidden, true, "unknown visibility mode falls back to Auto");
    mobileEqual(unknownMode.host.smile.mediaDiagnostics().virtualControlsMode, "auto", "unknown mode diagnostics fallback");
    const duplicateMode = createMobileControlsHost({ search: "?smile-controls=on&smile-controls=off" });
    duplicateMode.host.smile.gameWindow("Duplicate", 960, 540);
    mobileEqual(duplicateMode.controls.hidden, true, "duplicated visibility mode falls back to Auto");

    const controls = createMobileControlsHost({ search: "?smile-controls=on" });
    mobileEqual(controls.controls.hidden, true, "Forced On starts hidden before Game Window");
    controls.host.smile.gameWindow("Controls", 960, 540);
    mobileEqual(controls.controls.hidden, false, "Forced On shows after Game Window");
    const mapping = new Map([
        ["up", 10], ["down", 11], ["left", 12], ["right", 13],
        ["a", 23], ["b", 24], ["x", 25], ["y", 26]
    ]);
    let pointerId = 20;
    for (const [controlName, key] of mapping) {
        const down = controls.pointer(controlName, "pointerdown", pointerId, "touch", 0);
        mobileEqual(down.defaultPrevented, true, `${controlName} pointerdown gesture containment`);
        mobileEqual(controls.host.smile.keyHeld(key), 1, `${controlName} held state`);
        mobileEqual(controls.host.smile.getKey(), key, `${controlName} queue mapping`);
        controls.pointer(controlName, "pointerdown", pointerId, "touch", 0);
        mobileEqual(controls.host.smile.getKey(), 0, `${controlName} duplicate pointerdown is idempotent`);
        controls.pointer(controlName, "pointerup", pointerId, "touch", 0);
        mobileEqual(controls.host.smile.keyHeld(key), 0, `${controlName} release state`);
        pointerId += 1;
    }
    controls.pointer("unknown", "pointerdown", 100, "touch", 0);
    mobileEqual(controls.host.smile.getKey(), 0, "unknown symbolic control is ignored");
    controls.controls.dispatch("pointerdown", { pointerId: 101, pointerType: "touch", button: 0 });
    mobileEqual(controls.host.smile.getKey(), 0, "blank overlay space does not enqueue input");
    controls.pointer("a", "pointerdown", 102, "mouse", 2);
    mobileEqual(controls.host.smile.getKey(), 0, "non-primary mouse button is ignored");

    controls.pointer("up", "pointerdown", 110, "touch", 0);
    controls.pointer("a", "pointerdown", 111, "touch", 0);
    mobileEqual(controls.host.smile.keyHeld(10), 1, "multi-touch direction held");
    mobileEqual(controls.host.smile.keyHeld(23), 1, "multi-touch action held");
    mobileAssert(JSON.stringify(drainKeys(controls.host.smile)) === JSON.stringify([10, 23]), "multi-touch queue order");
    controls.pointer("up", "pointerup", 110, "touch", 0);
    mobileEqual(controls.host.smile.keyHeld(23), 1, "releasing direction preserves action");
    controls.pointer("a", "pointerup", 111, "touch", 0);
    assertReleased(controls, "multi-touch release");

    controls.pointer("a", "pointerdown", 112, "touch", 0);
    controls.pointer("a", "pointerdown", 115, "touch", 0);
    mobileEqual(controls.host.smile.keyHeld(23), 1, "two pointers share a held pad key");
    controls.pointer("a", "pointerup", 112, "touch", 0);
    mobileEqual(controls.host.smile.keyHeld(23), 1, "one pointer release preserves the second pointer owner");
    controls.pointer("a", "pointerup", 115, "touch", 0);
    mobileEqual(controls.host.smile.keyHeld(23), 0, "final same-pad-key owner release clears held state");
    drainKeys(controls.host.smile);

    controls.pointer("x", "pointerdown", 113, "touch", 0);
    controls.pointer("b", "pointerdown", 114, "touch", 0);
    mobileAssert(JSON.stringify(drainKeys(controls.host.smile)) === JSON.stringify([25, 24]),
        "X and B queue distinct virtual-controller key values");
    controls.pointer("b", "pointerup", 114, "touch", 0);
    mobileEqual(controls.host.smile.keyHeld(24), 0, "releasing B clears only the B key");
    mobileEqual(controls.host.smile.keyHeld(25), 1, "releasing B preserves the distinct X key");
    controls.pointer("x", "pointerup", 113, "touch", 0);
    mobileEqual(controls.host.smile.keyHeld(25), 0, "releasing X clears the X key");

    controls.keyboard("keydown", "Escape");
    mobileEqual(controls.host.smile.getKey(), 15, "physical Escape keeps its existing queue mapping");
    mobileEqual(controls.host.smile.keyHeld(15), 1, "physical Escape keeps its existing held mapping");
    controls.keyboard("keyup", "Escape");
    mobileEqual(controls.host.smile.keyHeld(15), 0, "physical Escape release remains unchanged");

    controls.pointer("left", "pointerdown", 120, "touch", 0);
    controls.pointer("left", "pointercancel", 120, "touch", 0);
    assertReleased(controls, "pointercancel");
    drainKeys(controls.host.smile);
    controls.pointer("right", "pointerdown", 121, "touch", 0);
    controls.pointer("right", "lostpointercapture", 121, "touch", 0);
    assertReleased(controls, "lostpointercapture");
    drainKeys(controls.host.smile);
    controls.pointer("up", "pointerdown", 122, "touch", 0);
    controls.dispatchWindow("blur");
    assertReleased(controls, "window blur");
    controls.dispatchWindow("focus");
    controls.pointer("down", "pointerdown", 123, "touch", 0);
    controls.host.document.hidden = true;
    controls.dispatchDocument("visibilitychange");
    assertReleased(controls, "document visibility loss");
    controls.host.document.hidden = false;
    controls.dispatchWindow("focus");
    controls.pointer("x", "pointerdown", 124, "touch", 0);
    controls.dispatchOrientation();
    assertReleased(controls, "orientation change");

    const queueBounds = createMobileControlsHost({ search: "?smile-controls=on" });
    queueBounds.host.smile.gameWindow("Bounds", 960, 540);
    for (let index = 0; index < 300; index += 1) {
        queueBounds.keyboard("keydown", "Digit1");
        queueBounds.keyboard("keyup", "Digit1");
    }
    let diagnostics = queueBounds.host.smile.mediaDiagnostics();
    mobileEqual(diagnostics.queuedKeyCount, 256, "Get Key queue remains bounded");
    mobileEqual(drainKeys(queueBounds.host.smile).length, 256, "bounded queue keeps exactly the newest capacity");
    for (let index = 0; index < 33; index += 1)
        queueBounds.pointer("up", "pointerdown", 200 + index, "touch", 0);
    diagnostics = queueBounds.host.smile.mediaDiagnostics();
    mobileEqual(diagnostics.activeInputSourceCount, 32, "active input source count remains bounded");
    mobileEqual(diagnostics.virtualActivePointerCount, 32, "excess virtual pointers are ignored");
    queueBounds.dispatchWindow("blur");
    assertReleased(queueBounds, "bounded-source blur cleanup");

    for (const [index, controlName] of ["up", "down", "left", "right", "a", "b", "x", "y"].entries()) {
        const buttonAudio = createMobileControlsHost({ search: "?smile-controls=on" });
        buttonAudio.host.smile.gameWindow(`Audio ${controlName}`, 960, 540);
        buttonAudio.host.smile.playMusic("Music.ogg", 1);
        mobileEqual(buttonAudio.audioPlays(), 0, `showing controls does not unlock music before ${controlName}`);
        buttonAudio.pointer(controlName, "pointerdown", 300 + index, "touch", 0);
        mobileAssert(buttonAudio.audioPlays() > 0, `${controlName} press unlocks requested music`);
        buttonAudio.pointer(controlName, "pointerup", 300 + index, "touch", 0);
    }

    const audio = createMobileControlsHost({ search: "?smile-controls=on" });
    audio.host.smile.gameWindow("Audio lifecycle", 960, 540);
    audio.host.smile.playMusic("Music.ogg", 1);
    audio.pointer("b", "pointerdown", 320, "touch", 0);
    audio.pointer("b", "pointerup", 320, "touch", 0);
    const playsBeforeBackground = audio.audioPlays();
    audio.host.document.hidden = true;
    audio.dispatchDocument("visibilitychange");
    mobileAssert(audio.audioPauses() > 0, "visibility loss pauses requested background music");
    mobileEqual(audio.audioPlays(), playsBeforeBackground, "visibility loss does not restart background music");
    audio.host.document.hidden = false;
    audio.dispatchWindow("focus");
    mobileEqual(audio.audioPlays(), playsBeforeBackground + 1, "foreground focus resumes requested background music");

    const pageHide = createMobileControlsHost({ search: "?smile-controls=on" });
    pageHide.host.smile.gameWindow("Page hide", 960, 540);
    pageHide.pointer("a", "pointerdown", 310, "touch", 0);
    pageHide.dispatchWindow("pagehide");
    assertReleased(pageHide, "pagehide");
    mobileEqual(pageHide.controls.hidden, true, "pagehide hides controls");
    mobileEqual(pageHide.host.document.getElementById("smile-shell").classList.contains("smile-controls-visible"), false,
        "hiding controls disables the portrait layout hook");

    const finish = createMobileControlsHost({ search: "?smile-controls=on" });
    finish.host.smile.gameWindow("Finish", 960, 540);
    finish.pointer("a", "pointerdown", 320, "touch", 0);
    finish.host.smile.run(() => {});
    await new Promise(resolve => setImmediate(resolve));
    assertReleased(finish, "runtime finish");
    mobileEqual(finish.controls.hidden, true, "runtime finish hides controls");
    mobileEqual(finish.host.__smileWeb.status, "stopped", "runtime finish status");

    const failure = createMobileControlsHost({ search: "?smile-controls=on" });
    failure.host.smile.gameWindow("Failure", 960, 540);
    failure.pointer("a", "pointerdown", 330, "touch", 0);
    failure.host.smile.run(() => { throw new Error("expected mobile-controls failure"); });
    await new Promise(resolve => setImmediate(resolve));
    assertReleased(failure, "runtime failure");
    mobileEqual(failure.controls.hidden, true, "runtime failure hides controls");
    mobileEqual(failure.host.__smileWeb.status, "error", "runtime failure status");
    mobileEqual(failure.errorElement.hidden, false, "runtime failure displays the error panel");
}

async function runFileTransferTests() {
    const env = createMobileControlsHost();
    const runtime = env.host.smile;
    env.host.navigator.userActivation = { isActive: true };
    const text = '{"name":"Arin 臺灣","value":-27}\r\n';
    mobileEqual(runtime.fileExport("pose 臺灣.json", text), true, "download initiated");
    mobileEqual(env.downloads[0].name, "pose 臺灣.json", "download filename retained");
    mobileEqual(await env.downloads[0].blob.text(), text, "download preserves UTF-8 and line endings");
    mobileEqual(env.transferElements.length, 0, "download anchor removed");
    for (const name of ["", "../pose.json", "D:\\pose.json", "pose?json", "pose.", "pose ", "臺".repeat(67)])
        mobileEqual(runtime.fileExport(name, text), false, `invalid filename ${name}`);
    mobileEqual(runtime.fileExport("pose.json", "x".repeat(8 * 1024 * 1024 + 1)), false, "oversized export rejected");
    env.host.navigator.userActivation.isActive = false;
    mobileEqual(runtime.fileExport("pose.json", text), false, "download needs activation");
    mobileEqual(await runtime.fileImport(), "", "import needs activation");
    env.host.navigator.userActivation.isActive = true;
    async function importBytes(bytes, advertisedSize = bytes.length) {
        const pending = runtime.fileImport();
        const input = env.transferElements[0];
        input.files = [{ size: advertisedSize, arrayBuffer: async () => Uint8Array.from(bytes).buffer }];
        input.dispatch("change");
        const result = await pending;
        mobileEqual(env.transferElements.length, 0, "file input removed");
        return result;
    }
    mobileEqual(await importBytes(Buffer.from("\ufeff" + text)), text, "UTF-8 BOM accepted and removed");
    mobileEqual(await importBytes([0xc0, 0x80]), "", "invalid UTF-8 rejected");
    mobileEqual(await importBytes([], 8 * 1024 * 1024 + 1), "", "oversized import rejected");
    const canceled = runtime.fileImport();
    mobileEqual(await runtime.fileImport(), "", "second pending picker rejected");
    env.transferElements[0].dispatch("cancel");
    mobileEqual(await canceled, "", "cancel leaves empty result");
    const disposed = runtime.fileImport();
    runtime.mediaShutdown();
    mobileEqual(await disposed, "", "shutdown cancels pending import");
    mobileEqual(env.transferUrls.size, 0, "shutdown revokes download URLs");
    mobileEqual(env.transferElements.length, 0, "shutdown removes picker");
    process.stdout.write("Web file transfer checks passed.\n");
}

function runDataStatusTests() {
    const stored = new Map();
    let denyRead = false, denyWrite = "";
    const localStorage = {
        getItem(key) { if (denyRead) throw new Error("SecurityError"); return stored.get(key) ?? null; },
        setItem(key, value) { if (denyWrite === "all" || denyWrite === key) throw new Error("QuotaExceededError"); stored.set(key, value); }
    };
    const createRuntime = () => {
        const runtime = createMobileControlsHost({ localStorage }).host.smile;
        runtime.configure("smile.tests.data-status.disposable", []);
        return runtime;
    };
    const runtime = createRuntime();
    const source = { data: [17, 23], dimensions: [2] };
    const destination = { data: [99, 98], dimensions: [2] };
    const load = (status, count, expected = "99,98") => {
        destination.data = [99, 98];
        const result = runtime.loadDataChecked("slot", destination);
        mobileEqual(result.status, status, "checked load status");
        mobileEqual(result.count, count, "checked load count");
        mobileEqual(destination.data.join(","), expected, "load contents / failure atomicity");
    };
    load(1, 0);
    mobileEqual(runtime.saveDataChecked(source, 2, "slot"), 0, "first durable save");
    const key = [...stored.keys()][0];
    const first = stored.get(key);
    source.data = [41, 43];
    mobileEqual(runtime.saveDataChecked(source, 2, "slot"), 0, "second durable save");
    const second = stored.get(key);
    mobileEqual(stored.get(key + ".bak"), first, "previous-good backup");
    load(0, 2, "41,43");
    const reloaded = createRuntime();
    mobileEqual(reloaded.loadDataChecked("slot", destination).status, 0, "fresh runtime reads persistent state");
    mobileEqual(destination.data.join(","), "41,43", "fresh runtime exact bytes");
    source.data = [51, 53];
    denyWrite = key;
    mobileEqual(runtime.saveDataChecked(source, 2, "slot"), 4, "quota at primary write");
    mobileEqual(stored.get(key), second, "failed write kept primary");
    mobileEqual(stored.get(key + ".bak"), second, "failed primary write still has a good backup");
    denyWrite = "all";
    mobileEqual(runtime.saveDataChecked(source, 2, "slot"), 4, "quota at backup write");
    denyWrite = "";
    load(0, 2, "41,43");
    denyRead = true;
    load(4, 0);
    mobileEqual(runtime.saveDataChecked(source, 2, "slot"), 4, "denied storage save");
    denyRead = false;
    source.data[0] = 256;
    mobileEqual(runtime.saveDataChecked(source, 2, "slot"), 3, "invalid byte");
    mobileEqual(runtime.saveDataChecked(source, -1, "slot"), 3, "invalid count");
    mobileEqual(stored.get(key), second, "invalid input kept primary");
    mobileEqual(runtime.loadDataChecked("slot", { data: [99], dimensions: [1] }).status, 6, "too-small destination");
    stored.set(key, "invalid base64");
    load(2, 2, "41,43");
    mobileEqual(runtime.loadDataChecked("slot", { data: [99], dimensions: [1] }).status, 6,
        "too-small destination also rejects a valid backup");
    mobileEqual(stored.get(key), "invalid base64", "backup recovery is read-only");
    let strictFailed = false;
    try { runtime.loadData("slot", destination); } catch (_) { strictFailed = true; }
    mobileEqual(strictFailed, true, "legacy strict load still fails on corrupt primary");
    mobileEqual(destination.data.join(","), "0,0", "strict load still clears destination");
    source.data = [61, 63];
    mobileEqual(runtime.saveDataChecked(source, 2, "slot"), 0, "explicit save after recovery");
    mobileEqual(stored.get(key + ".bak"), second, "corrupt primary never replaces good backup");
    stored.set(key, "corrupt"); stored.set(key + ".bak", "also corrupt");
    load(5, 0);
    mobileEqual(runtime.saveDataChecked(source, 2, "slot"), 5, "unrecoverable corruption blocks save");
    stored.set(key, "x".repeat(1398161));
    load(5, 0);
    stored.delete(key); stored.set(key + ".bak", first);
    load(2, 2, "17,23");
    stored.delete(key + ".bak");
    load(1, 0); // Do not resurrect the previous in-memory save after external deletion.
    // Strict memory fallback must also never receive an unsuccessful candidate.
    destination.data = [0, 0];
    runtime.loadData("slot", destination);
    mobileEqual(destination.data.join(","), "61,63", "failed writes never poisoned legacy memory fallback");
    process.stdout.write("Web checked Data denial, quota, corruption, backup, reload and atomicity tests passed (disposable VM storage).\n");
}

if (verifyDataStatus) {
    try { runDataStatusTests(); } catch (error) { fail(error.stack || error.message); }
} else if (verifyFileTransfer) {
    runFileTransferTests().then(() => process.exit(0)).catch(error => fail(error.stack || error.message));
} else if (verifyMobileControls) {
    runMobileControlsTests()
        .then(() => process.stdout.write(`Web execution passed: ${webDirectory} (mobile virtual controls)\n`))
        .catch(error => fail(error && error.stack ? error.stack : String(error)));
} else {
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
let drawOrder = 0;
let backCanvasElement = null;
let renderer3DCanvasElement = null;
let createdCanvasCount = 0;
let virtualNow = 0;
const phase5Keys = [];
let renderer3DDepthEnables = 0;
let renderer3DBufferUploads = 0;
let renderer3DDrawCalls = 0;
let renderer3DComposites = 0;
let renderer3DTransformFeedbackDispatches = 0;
let renderer3DTransformFeedbackVaryings = [];
let renderer3DReadbacks = 0;

function contextWebGL2() {
    const noop = () => {};
    return {
        VERTEX_SHADER: 0x8b31, FRAGMENT_SHADER: 0x8b30, COMPILE_STATUS: 0x8b81, LINK_STATUS: 0x8b82,
        DEPTH_TEST: 0x0b71, LESS: 0x0201, CULL_FACE: 0x0b44, ARRAY_BUFFER: 0x8892,
        ELEMENT_ARRAY_BUFFER: 0x8893, STATIC_DRAW: 0x88e4, DYNAMIC_DRAW: 0x88e8,
        DYNAMIC_COPY: 0x88ea, POINTS: 0x0000, RASTERIZER_DISCARD: 0x8c89,
        TRANSFORM_FEEDBACK: 0x8e22, TRANSFORM_FEEDBACK_BUFFER: 0x8c8e,
        INTERLEAVED_ATTRIBS: 0x8c8c, MAX_VERTEX_ATTRIBS: 0x8869,
        MAX_TRANSFORM_FEEDBACK_INTERLEAVED_COMPONENTS: 0x8c8a,
        COLOR_BUFFER_BIT: 0x4000, DEPTH_BUFFER_BIT: 0x0100, TRIANGLES: 0x0004,
        TRIANGLE_STRIP: 0x0005, UNSIGNED_INT: 0x1405, UNSIGNED_SHORT: 0x1403, FLOAT: 0x1406,
        BLEND: 0x0be2, ONE: 1, SRC_ALPHA: 0x0302, ONE_MINUS_SRC_ALPHA: 0x0303,
        TEXTURE_2D: 0x0de1, TEXTURE0: 0x84c0, RGBA: 0x1908, RGBA8: 0x8058,
        SRGB8_ALPHA8: 0x8c43, UNSIGNED_BYTE: 0x1401, BACK: 0x0405, NONE: 0,
        FRAMEBUFFER: 0x8d40, RENDERBUFFER: 0x8d41, FRAMEBUFFER_COMPLETE: 0x8cd5,
        COLOR_ATTACHMENT0: 0x8ce0, DEPTH_ATTACHMENT: 0x8d00, DEPTH_COMPONENT24: 0x81a6,
        DEPTH_COMPONENT: 0x1902, RGBA16F: 0x881a, HALF_FLOAT: 0x140b, MAX_TEXTURE_SIZE: 0x0d33,
        TEXTURE_COMPARE_MODE: 0x884c, COMPARE_REF_TO_TEXTURE: 0x884e,
        TEXTURE_COMPARE_FUNC: 0x884d, LEQUAL: 0x0203, POLYGON_OFFSET_FILL: 0x8037,
        TEXTURE_MIN_FILTER: 0x2801, TEXTURE_MAG_FILTER: 0x2800,
        TEXTURE_WRAP_S: 0x2802, TEXTURE_WRAP_T: 0x2803,
        NEAREST: 0x2600, LINEAR: 0x2601, LINEAR_MIPMAP_LINEAR: 0x2703,
        CLAMP_TO_EDGE: 0x812f, REPEAT: 0x2901, NO_ERROR: 0,
        UNPACK_FLIP_Y_WEBGL: 0x9240, UNPACK_PREMULTIPLY_ALPHA_WEBGL: 0x9241,
        UNPACK_COLORSPACE_CONVERSION_WEBGL: 0x9243,
        createShader: () => ({}), shaderSource: noop, compileShader: noop,
        getShaderParameter: () => true, getShaderInfoLog: () => "", deleteShader: noop,
        createProgram: () => ({}), attachShader: noop, linkProgram: noop, deleteProgram: noop,
        transformFeedbackVaryings: (_program, varyings) => { renderer3DTransformFeedbackVaryings = [...varyings]; },
        getProgramParameter: () => true, getProgramInfoLog: () => "", getUniformLocation: () => ({}),
        enable: value => { if (value === 0x0b71) renderer3DDepthEnables += 1; },
        depthFunc: noop, depthMask: noop, disable: noop, blendFunc: noop, cullFace: noop,
        createBuffer: () => ({}), bindBuffer: noop,
        bufferData: () => { renderer3DBufferUploads += 1; },
        bufferSubData: () => { renderer3DBufferUploads += 1; }, deleteBuffer: noop,
        createTexture: () => ({}), bindTexture: noop, deleteTexture: noop, activeTexture: noop,
        pixelStorei: noop, texImage2D: noop, texSubImage2D: noop, texParameteri: noop, texParameterf: noop,
        generateMipmap: noop, getError: () => 0,
        getExtension: name => name.includes("texture_filter_anisotropic")
            ? { MAX_TEXTURE_MAX_ANISOTROPY_EXT: 0x84ff, TEXTURE_MAX_ANISOTROPY_EXT: 0x84fe }
            : (name === "EXT_color_buffer_float" ? {} : null),
        getParameter: value => value === 0x84ff ? 8 : (value === 0x0d33 ? 4096 :
            (value === 0x8869 ? 16 : (value === 0x8c8a ? 64 : 0))),
        viewport: noop, clearColor: noop, clearDepth: noop, clear: noop, useProgram: noop,
        createFramebuffer: () => ({}), bindFramebuffer: noop, framebufferTexture2D: noop,
        drawBuffers: noop, readBuffer: noop, checkFramebufferStatus: () => 0x8cd5,
        deleteFramebuffer: noop, createRenderbuffer: () => ({}), bindRenderbuffer: noop,
        renderbufferStorage: noop, framebufferRenderbuffer: noop, deleteRenderbuffer: noop,
        colorMask: noop, polygonOffset: noop,
        enableVertexAttribArray: noop, disableVertexAttribArray: noop, vertexAttribPointer: noop,
        vertexAttribDivisor: noop,
        createVertexArray: () => ({}), bindVertexArray: noop, deleteVertexArray: noop,
        createTransformFeedback: () => ({}), bindTransformFeedback: noop, deleteTransformFeedback: noop,
        bindBufferBase: noop, beginTransformFeedback: noop, endTransformFeedback: noop,
        uniformMatrix4fv: noop, uniformMatrix3fv: noop, uniform4fv: noop, uniform3fv: noop,
        uniform4f: noop, uniform3f: noop, uniform2f: noop, uniform1i: noop, uniform1f: noop,
        drawElements: () => { renderer3DDrawCalls += 1; },
        drawElementsInstanced: () => { renderer3DDrawCalls += 1; },
        drawArrays: mode => {
            renderer3DDrawCalls += 1;
            if (mode === 0x0000) renderer3DTransformFeedbackDispatches += 1;
        },
        getBufferSubData: () => { renderer3DReadbacks += 1; }
    };
}

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
            if (name === "back") fillRectangleDraws.push({ frame: requestedFrames, values, fillStyle: context.fillStyle, order: ++drawOrder });
        },
        strokeRect: noop, clearRect: noop,
        drawImage: (resource, ...values) => {
            if (name === "back" && resource === renderer3DCanvasElement)
                renderer3DComposites += 1;
            if (name === "back" && resource && typeof resource.src === "string") {
                imageDraws.push({ source: resource.src, values, smoothing: context.imageSmoothingEnabled,
                    alpha: context.globalAlpha, frame: requestedFrames, order: ++drawOrder });
            }
        },
        measureText: value => {
            measurementCalls += 1;
            return { width: String(value).length * 8, actualBoundingBoxAscent: 12, actualBoundingBoxDescent: 4 };
        },
        fillText: (value, x, y) => {
            drawnText.push(String(value));
            if (name === "back") textDraws.push({ value: String(value), x, y, frame: requestedFrames,
                fillStyle: context.fillStyle, font: context.font, alignment: context.textAlign, order: ++drawOrder });
        },
        fillStyle: "", strokeStyle: "", lineWidth: 1, font: "", textAlign: "left",
        textBaseline: "top", imageSmoothingEnabled: false, globalAlpha: 1
    };
    return context;
}
function canvas(name = "offscreen") {
    const drawing = context2d(name);
    const listeners = new Map();
    const result = {
        width: 0, height: 0, hidden: true, style: {},
        getContext: () => drawing,
        addEventListener: (type, listener) => addListener(listeners, type, listener),
        dispatch: (type, event = {}) => {
            const payload = { type, defaultPrevented: false, ...event };
            payload.preventDefault = () => { payload.defaultPrevented = true; };
            dispatch(listeners, type, payload);
            return payload;
        },
        setAttribute: () => {}, focus: () => {}
    };
    return result;
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
            createdCanvasCount += 1;
            if (createdCanvasCount === 1) {
                backCanvasElement = canvas("back");
                return backCanvasElement;
            }
            renderer3DCanvasElement = canvas("renderer3d");
            const fallbackDrawing = renderer3DCanvasElement.getContext();
            const webGL2 = contextWebGL2();
            renderer3DCanvasElement.getContext = type => verifyRenderer3D && type === "webgl2"
                ? webGL2
                : fallbackDrawing;
            return renderer3DCanvasElement;
        },
        addEventListener: (type, listener) => addListener(documentListeners, type, listener),
        hasFocus: () => true,
        exitFullscreen: async () => {}
    },
    localStorage: {
        getItem: key => {
            if (deniedDataKey && key.endsWith(":data:" + deniedDataKey)) throw new Error("Test-only storage denial");
            return storage.has(key) ? storage.get(key) : null;
        },
        setItem: (key, value) => {
            if (deniedDataKey && key.endsWith(":data:" + deniedDataKey)) throw new Error("Test-only storage denial");
            storage.set(key, String(value));
        }
    },
    performance: { now: () => verifyPhase5Ui || verifyPhase5Submenus ? virtualNow : Date.now() },
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
            else if (normalized.includes("/Pbr-") && normalized.endsWith(".png")) {
                this.naturalWidth = this.width = 4; this.naturalHeight = this.height = 4;
            }
            setImmediate(() => { if (this.onload) this.onload(); });
        }
        get src() { return this._src; }
    },
    fetch: async source => {
        if (verifyPhase4Audio) {
            await new Promise(resolve => setTimeout(resolve, String(source).includes("ToneOne") ? 35 : 5));
            return { ok: true, arrayBuffer: async () => new ArrayBuffer(8) };
        }
        const relative = String(source).replace(/\\/g, "/");
        const candidate = path.resolve(webDirectory, relative);
        const webPrefix = `${webDirectory}${path.sep}`;
        if (candidate !== webDirectory && !candidate.startsWith(webPrefix))
            return { ok: false, arrayBuffer: async () => new ArrayBuffer(0) };
        try {
            const bytes = fs.readFileSync(candidate);
            const payload = bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength);
            return { ok: true, arrayBuffer: async () => payload };
        } catch (_) {
            return { ok: false, arrayBuffer: async () => new ArrayBuffer(0) };
        }
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
            if (verifyPhase5Ui || verifyPhase5Submenus) {
                virtualNow = requestedFrames * 280;
                const scriptedCode = (verifyPhase5Submenus ? new Map([
                    [2, "ArrowRight"], [3, "Enter"], [4, "Space"], [5, "ArrowDown"],
                    [6, "ArrowDown"], [7, "ArrowDown"], [8, "ArrowDown"], [9, "ArrowDown"],
                    [10, "ArrowDown"], [11, "ArrowDown"], [12, "KeyD"], [13, "KeyD"],
                    [14, "KeyW"], [15, "KeyX"], [16, "ArrowRight"], [17, "KeyA"],
                    [18, "Enter"], [19, "KeyS"], [20, "Space"], [21, "ArrowLeft"],
                    [22, "Escape"], [23, "Escape"], [24, "Escape"], [25, "ArrowRight"],
                    [26, "Digit2"], [27, "Digit3"], [28, "Digit1"], [29, "Enter"],
                    [30, "Space"], [31, "Enter"], [32, "KeyQ"], [33, "KeyE"],
                    [34, "KeyU"], [35, "KeyT"], [36, "KeyY"], [37, "KeyI"], [38, "KeyP"]
                ]) : new Map([
                    [2, "ArrowDown"], [3, "ArrowDown"], [4, "ArrowDown"], [5, "ArrowDown"],
                    [6, "ArrowDown"], [7, "ArrowDown"], [8, "ArrowUp"], [9, "Digit2"],
                    [10, "Enter"], [11, "Digit3"], [12, "Digit1"], [13, "Space"], [14, "Escape"]
                ])).get(requestedFrames);
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
            if (verifyRenderer3D && requestedFrames === 1) {
                const event = { code: verifyNeonCyclesInput ? "Digit2" : "Digit1", repeat: false,
                    ctrlKey: false, altKey: false,
                    metaKey: false, preventDefault: () => {} };
                dispatch(windowListeners, "keydown", event);
                dispatch(windowListeners, "keyup", event);
            }
            if (verifyNeonCyclesInput) {
                const code = new Map([[2, "KeyA"], [3, "KeyD"], [4, "Space"], [5, "Space"]])
                    .get(requestedFrames);
                if (code) {
                    const event = { code, repeat: false, ctrlKey: false, altKey: false,
                        metaKey: false, preventDefault: () => {} };
                    dispatch(windowListeners, "keydown", event);
                    dispatch(windowListeners, "keyup", event);
                }
            }
            if (verifyRenderer3D && requestedFrames === 3) {
                host.innerWidth = 1000;
                host.innerHeight = 700;
                dispatch(windowListeners, "resize");
            }
            if (requestedFrames >= maximumFrames) dispatch(windowListeners, "pagehide");
            callback(Date.now());
        });
    }
};
host.SMILE_TEST_RENDERER3D_FORCE_GPU_PARTICLE_SHADER_FAILURE = forceRenderer3DGpuParticleShaderFailure;
host.SMILE_TEST_RENDERER3D_FORCE_GPU_PARTICLE_ATTRIBUTE_FAILURE = forceRenderer3DGpuParticleAttributeFailure;
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
if (forceRenderer3DPbrFailure) host.SMILE_TEST_RENDERER3D_FORCE_PBR_FAILURE = true;
if (forceRenderer3DHdrFailure) host.SMILE_TEST_RENDERER3D_FORCE_HDR_FAILURE = true;
if (forceRenderer3DShadowFailure) host.SMILE_TEST_RENDERER3D_FORCE_SHADOW_FAILURE = true;
if (forceRenderer3DSoftDepthFailure) host.SMILE_TEST_RENDERER3D_FORCE_SOFT_DEPTH_FAILURE = true;
if (forceRenderer3DDistortionFailure) host.SMILE_TEST_RENDERER3D_FORCE_DISTORTION_FAILURE = true;

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
    if (host.__smileWeb.status === "error") {
        const runtimeError = errorElement.textContent || hostConsoleErrors.join("\n") ||
            "runtime reported an unknown error";
        if (expectedRuntimeError === null) fail(runtimeError);
        if (!runtimeError.includes(expectedRuntimeError))
            fail(`runtime error did not contain ${JSON.stringify(expectedRuntimeError)}: ${runtimeError}`);
        const failureDiagnostics = host.smile.mediaDiagnostics();
        if (failureDiagnostics.classLiveCount !== 0 || host.smile.classLiveCount() !== 0)
            fail(`SMILE Class ownership leaked on runtime failure: ${JSON.stringify(failureDiagnostics)}`);
        process.stdout.write(`Web execution passed: ${webDirectory} (expected runtime failure)\n`);
        return;
    }
    if (expectedRuntimeError !== null)
        fail(`expected runtime error ${JSON.stringify(expectedRuntimeError)}, but execution completed`);

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
        fail(`Draw Text did not contain ${JSON.stringify(expectedDrawText)}; recorded ${JSON.stringify(drawnText)}`);
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
            fail("Phase 4 persistent Data storage was not recorded");
        const envelope = Buffer.from(storage.get(dataKey), "base64");
        if (envelope.length !== 52 || envelope.subarray(0, 4).toString("ascii") !== "SMD4" ||
            envelope.readUInt32LE(4) !== 1 || envelope.readUInt32LE(8) !== 8)
            fail("Phase 4.1 Web persistent Data envelope was malformed");
        envelope[envelope.length - 1] ^= 1;
        storage.set(dataKey, envelope.toString("base64"));
        const corruptTarget = host.smile.array([8], 9);
        let corruptRejected = false;
        try { host.smile.loadData("Phase4VisualSlice", corruptTarget); }
        catch (_) { corruptRejected = true; }
        if (!corruptRejected || corruptTarget.data.some(value => value !== 0))
            fail("Phase 4.1 Web corrupt Data was not rejected with a zeroed destination");
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
    if (diagnostics.classLiveCount !== 0 || host.smile.classLiveCount() !== 0)
        fail(`SMILE Class ownership leaked: ${JSON.stringify(diagnostics)}`);
    if (verifyRenderer3D && hostConsoleErrors.length !== 0)
        fail(`Renderer3D Web console reported errors: ${hostConsoleErrors.join("\n")}`);
    if (verifyRenderer3D && !renderer3DStateOnly) {
        if (renderer3DDepthEnables < 1)
            fail("Renderer3D WebGL2 did not enable depth testing");
        if (renderer3DBufferUploads < 2)
            fail(`Renderer3D WebGL2 did not upload indexed mesh buffers (${renderer3DBufferUploads})`);
        if (renderer3DDrawCalls < 1)
            fail("Renderer3D WebGL2 did not issue indexed triangle draws");
        if (renderer3DComposites < 1)
            fail("Renderer3D WebGL2 canvas was not composited into Renderer2D");
        if (hostConsoleErrors.length !== 0)
            fail(`Renderer3D Web console reported errors: ${hostConsoleErrors.join("\n")}`);
        if (verifyRenderer3DGpuParticles && !forceRenderer3DGpuParticleShaderFailure &&
            !forceRenderer3DGpuParticleAttributeFailure) {
            const expectedVaryings = ["nextPositionAge", "nextVelocityLifetime", "nextSizeRotationAngular",
                "nextThermalDensityNoise", "nextSeedFlagsGradientFrame"];
            if (renderer3DTransformFeedbackDispatches < 1)
                fail("Renderer3D WebGL2 GPU particles did not dispatch transform feedback");
            if (JSON.stringify(renderer3DTransformFeedbackVaryings) !== JSON.stringify(expectedVaryings))
                fail(`Renderer3D WebGL2 transform-feedback varying order was ${JSON.stringify(renderer3DTransformFeedbackVaryings)}`);
            if (renderer3DReadbacks !== 0)
                fail(`Renderer3D WebGL2 GPU particles performed ${renderer3DReadbacks} GPU readbacks`);
            renderer3DCanvasElement.dispatch("webglcontextlost");
            const restartCount = host.smile.renderer3D(127, 10, 18, 0, 0, 0, 0, 0, 0, 0, 0);
            if (restartCount < 1)
                fail("Renderer3D WebGL2 GPU particles did not record context-loss recovery");
            renderer3DCanvasElement.dispatch("webglcontextrestored");
        }
        if (verifyNeonCyclesInput && (!drawnText.includes("P1") || !drawnText.includes("P2")))
            fail("Neon Cycles two-player input path did not reach the active HUD");

        for (let lifecycle = 0; lifecycle < 2; lifecycle += 1) {
            const meshes = [];
            const objects = [];
            for (let primitive = 1; primitive <= 6; primitive += 1) {
                const mesh = host.smile.renderer3D(7, primitive, 100, 50, 8, 6, 0, 0, 0, 0, 0);
                const vertexCount = host.smile.renderer3D(19, mesh, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                const indexCount = host.smile.renderer3D(20, mesh, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                if (mesh === 0 || vertexCount <= 0 || indexCount <= 0 || indexCount % 3 !== 0)
                    fail(`Renderer3D primitive ${primitive} produced invalid indexed geometry`);
                const object = host.smile.renderer3D(8, mesh, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                if (object === 0) fail(`Renderer3D primitive ${primitive} object allocation failed`);
                meshes.push(mesh);
                objects.push(object);
            }
            if (host.smile.renderer3D(16, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0) === 0)
                fail("Renderer3D repeated lifecycle could not begin a frame");
            for (const object of objects)
                if (host.smile.renderer3D(17, object, 0, 0, 0, 0, 0, 0, 0, 0, 0) === 0)
                    fail("Renderer3D repeated lifecycle could not draw an object");
            host.smile.renderer3D(18, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            for (const object of objects)
                host.smile.renderer3D(9, object, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            for (const mesh of meshes)
                host.smile.renderer3D(9, mesh, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            if (host.smile.renderer3D(17, objects[0], 0, 0, 0, 0, 0, 0, 0, 0, 0) !== 0)
                fail("Renderer3D accepted a deleted object handle");
        }
        host.smile.renderer3D(2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }
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
            fail(`Phase 4.1 Image ownership leaked: ${JSON.stringify(diagnostics)}`);
        if (diagnostics.imageDecodeCount !== 1)
            fail(`Phase 4.1 Image ownership expected one decode, found ${diagnostics.imageDecodeCount}`);
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
        const expectedKeys = ["ArrowDown", "ArrowDown", "ArrowDown", "ArrowDown", "ArrowDown", "ArrowDown", "ArrowUp",
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
        const rootCursorDraws = imageDraws.filter(draw => path.basename(draw.source) === "Cursor.png" &&
            draw.values.length === 8 && draw.values[4] === 88);
        const cursorYs = new Set(rootCursorDraws
            .map(draw => draw.values[5]));
        const cursorByFrame = new Map(rootCursorDraws
            .map(draw => [draw.frame, draw.values[5]]));
        if (cursorYs.size < 6 || cursorByFrame.get(5) !== 273 || cursorByFrame.get(6) !== 316)
            fail(`Phase 5 disabled-item skipping/scroll cursor positions differed: ${JSON.stringify([...cursorByFrame])}`);
        const frameSevenText = textDraws.filter(draw => draw.frame === 7).map(draw => draw.value);
        if (!frameSevenText.includes("OPTIONS") || frameSevenText.includes("ITEM"))
            fail(`Phase 5 scrolling did not expose OPTIONS and remove ITEM: ${JSON.stringify(frameSevenText)}`);
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
    if (verifyPhase5Submenus) {
        const expectedKeys = ["ArrowRight", "Enter", "Space", "ArrowDown", "ArrowDown", "ArrowDown", "ArrowDown",
            "ArrowDown", "ArrowDown", "ArrowDown", "KeyD", "KeyD", "KeyW", "KeyX", "ArrowRight", "KeyA",
            "Enter", "KeyS", "Space", "ArrowLeft", "Escape", "Escape", "Escape", "ArrowRight", "Digit2",
            "Digit3", "Digit1", "Enter", "Space", "Enter", "KeyQ", "KeyE", "KeyU", "KeyT", "KeyY",
            "KeyI", "KeyP"];
        if (phase5Keys.join("|") !== expectedKeys.join("|"))
            fail(`Phase 5.2.2 scripted key sequence differed: ${phase5Keys.join(", ")}`);

        const basenames = imageDraws.map(draw => path.basename(draw.source));
        for (const required of ["Background.png", "WindowSkin.png", "Cursor.png", "BitmapFont.png"])
            if (!basenames.includes(required)) fail(`Phase 5.2.2 Web draws did not include ${required}`);

        const frameImageNames = new Map();
        for (const draw of imageDraws) {
            if (!frameImageNames.has(draw.frame)) frameImageNames.set(draw.frame, []);
            frameImageNames.get(draw.frame).push(path.basename(draw.source));
        }
        for (const [frame, names] of frameImageNames) {
            if (names.length !== 0 && names[0] !== "Background.png")
                fail(`Phase 5.2.2 painter order did not begin with Background.png on frame ${frame}: ${names[0]}`);
        }

        const cursorByFrame = new Map();
        for (const draw of imageDraws.filter(draw => path.basename(draw.source) === "Cursor.png" && draw.values.length === 8))
            cursorByFrame.set(draw.frame, (cursorByFrame.get(draw.frame) || 0) + 1);
        const expectedCursorDepth = new Map([[1, 1], [2, 2], [3, 3], [4, 4], [5, 4], [6, 4], [7, 4], [8, 4],
            [9, 4], [10, 4], [11, 4], [12, 4], [13, 4], [14, 4], [15, 1], [16, 1], [17, 2], [18, 3],
            [19, 3], [20, 4], [21, 3], [22, 2], [23, 1], [24, 1], [25, 2], [26, 2], [27, 2], [28, 2],
            [29, 3], [30, 4], [31, 4]]);
        for (const [frame, expected] of expectedCursorDepth) {
            if ((cursorByFrame.get(frame) || 0) !== expected)
                fail(`Phase 5.2.2 cursor depth differed on frame ${frame}: expected ${expected}, got ${cursorByFrame.get(frame) || 0}`);
        }

        const initialLabels = textDraws.filter(draw => draw.frame === 1 && draw.y >= 230 && draw.y < 450 && draw.value !== " >");
        if (initialLabels.length < 3 || new Set(initialLabels.map(draw => draw.x)).size !== 1)
            fail(`Phase 5.2.2 fixed cursor gutter shifted row text: ${JSON.stringify(initialLabels)}`);
        for (const cursor of imageDraws.filter(draw => path.basename(draw.source) === "Cursor.png" && draw.values.length === 8 && draw.frame <= 31 && draw.frame !== 26)) {
            const cursorRight = cursor.values[4] + cursor.values[6];
            const rowLabels = textDraws.filter(draw => draw.frame === cursor.frame && draw.value !== " >" &&
                draw.x >= cursorRight && draw.y >= cursor.values[5] - 4 && draw.y <= cursor.values[5] + cursor.values[7] + 8);
            if (rowLabels.length === 0)
                fail(`Phase 5.2.2 cursor overlapped or lost its row label: ${JSON.stringify(cursor)}`);
        }

        for (const frame of [14, 15, 16]) {
            if (textDraws.some(draw => draw.frame === frame && draw.value === " >"))
                fail(`Phase 5.2.2 hidden submenu indicator remained visible on frame ${frame}`);
        }
        const windowCount = frame => imageDraws.filter(draw => draw.frame === frame && path.basename(draw.source) === "WindowSkin.png" &&
            draw.values.length === 8 && draw.values[0] === 0 && draw.values[1] === 0).length;
        if (windowCount(14) !== 4 || windowCount(15) !== 1 || windowCount(16) !== 2)
            fail(`Phase 5.2.2 edge pruning or hidden-indicator navigation differed: ${JSON.stringify({ before: windowCount(14), pruned: windowCount(15), reopened: windowCount(16) })}`);
        const afterMarkers = textDraws.filter(draw => draw.frame === 17 && draw.value === " >");
        if (afterMarkers.length < 2 || !afterMarkers.every(marker => textDraws.some(label => label.frame === marker.frame &&
            label.y === marker.y && label.value !== " >" && marker.x === label.x + label.value.length * 8)))
            fail(`Phase 5.2.2 after-text indicators did not follow rendered labels: ${JSON.stringify(afterMarkers)}`);
        const rightMarkers = textDraws.filter(draw => draw.frame === 19 && draw.value === " >");
        if (rightMarkers.length < 3)
            fail(`Phase 5.2.2 right-aligned indicators were missing: ${JSON.stringify(rightMarkers)}`);
        for (const prefix of ["A Very Long Shared", "Disabled Library", "Open Hierarchy"]) {
            const label = textDraws.find(draw => draw.frame === 1 && draw.value.startsWith(prefix));
            const marker = label && textDraws.find(draw => draw.frame === 1 && draw.value === " >" &&
                draw.y >= label.y && draw.y < label.y + 43 && draw.fillStyle === label.fillStyle);
            if (!label || !marker)
                fail(`Phase 5.2.2 normal/disabled/selected indicator style differed for ${prefix}`);
        }
        if (!textDraws.some(draw => draw.value === " >"))
            fail("Phase 5.2.2 submenu indicator was not drawn as exact literal ' >'");
        if (!textDraws.some(draw => draw.value.endsWith("...")))
            fail("Phase 5.2.2 long-label ellipsis was not drawn");
        if (!textDraws.some(draw => draw.value === "A Very Long"))
            fail(`Phase 5.2.2 bounded wrapped label was not drawn: ${JSON.stringify([...new Set(textDraws.map(draw => draw.value))])}`);

        const rootSelection = fillRectangleDraws.find(draw => draw.frame === 1 && draw.values[0] === 234 &&
            draw.values[2] === 538 && draw.values[3] === 43);
        const oneLineLabel = textDraws.find(draw => draw.frame === 1 && draw.value === "Open Hierarchy");
        const oneLineMarker = oneLineLabel && textDraws.find(draw => draw.frame === 1 && draw.value === " >" &&
            draw.y === oneLineLabel.y && draw.fillStyle === oneLineLabel.fillStyle);
        const oneLineCursor = imageDraws.find(draw => draw.frame === 1 && path.basename(draw.source) === "Cursor.png" &&
            draw.values.length === 8 && rootSelection && draw.values[4] === rootSelection.values[0]);
        if (!rootSelection || !oneLineLabel || !oneLineMarker || !oneLineCursor || oneLineLabel.x !== 282 ||
            oneLineLabel.y !== rootSelection.values[1] + Math.trunc((43 - 25) / 2) ||
            oneLineCursor.values[5] !== rootSelection.values[1] + Math.trunc((43 - 30) / 2) - 2)
            fail(`Phase 5.2.2 one-line row centering differed: ${JSON.stringify({ rootSelection, oneLineLabel, oneLineMarker, oneLineCursor })}`);

        const wrappedSelection = fillRectangleDraws.find(draw => draw.frame === 2 && draw.values[0] === 46 &&
            draw.values[3] === 78);
        const wrappedLines = wrappedSelection ? textDraws.filter(draw => draw.frame === 2 && draw.value !== " >" &&
            draw.x === 94 && draw.y >= wrappedSelection.values[1] && draw.y < wrappedSelection.values[1] + 78) : [];
        const wrappedYs = [...new Set(wrappedLines.map(draw => draw.y))].sort((left, right) => left - right);
        const wrappedMarker = wrappedSelection && textDraws.find(draw => draw.frame === 2 && draw.value === " >" &&
            draw.y >= wrappedSelection.values[1] && draw.y < wrappedSelection.values[1] + 78 &&
            wrappedLines.length !== 0 && draw.fillStyle === wrappedLines[0].fillStyle);
        const wrappedCursor = wrappedSelection && imageDraws.find(draw => draw.frame === 2 &&
            path.basename(draw.source) === "Cursor.png" && draw.values.length === 8 && draw.values[4] === wrappedSelection.values[0]);
        const wrappedFirstY = wrappedSelection ? wrappedSelection.values[1] + Math.trunc((78 - (25 * 2 + 5)) / 2) : -1;
        if (!wrappedSelection || wrappedLines.length < 2 || wrappedYs.length !== 2 || wrappedYs[0] !== wrappedFirstY ||
            wrappedYs[1] !== wrappedFirstY + 30 || !wrappedMarker || wrappedMarker.y !== wrappedFirstY || !wrappedCursor ||
            wrappedCursor.values[5] !== wrappedSelection.values[1] + Math.trunc((78 - 30) / 2) - 2 ||
            new Set(wrappedLines.map(draw => draw.x)).size !== 1)
            fail(`Phase 5.2.2 wrapped system row geometry differed: ${JSON.stringify({ wrappedSelection, wrappedLines, wrappedMarker, wrappedCursor })}`);

        const bitmapSelection = fillRectangleDraws.find(draw => draw.frame === 26 && draw.values[0] === 46 &&
            draw.values[3] === 78);
        const bitmapLineStarts = bitmapSelection ? imageDraws.filter(draw => draw.frame === 26 &&
            path.basename(draw.source) === "BitmapFont.png" && draw.values.length === 8 && draw.values[4] === 94 &&
            draw.values[5] >= bitmapSelection.values[1] && draw.values[5] < bitmapSelection.values[1] + 78) : [];
        const bitmapYs = [...new Set(bitmapLineStarts.map(draw => draw.values[5]))].sort((left, right) => left - right);
        const bitmapFirstY = bitmapSelection ? bitmapSelection.values[1] + Math.trunc((78 - (30 * 2 + 5)) / 2) : -1;
        const bitmapMarker = bitmapSelection && imageDraws.find(draw => draw.frame === 26 &&
            path.basename(draw.source) === "BitmapFont.png" && draw.values.length === 8 &&
            draw.values[0] === 896 && draw.values[1] === 64 && draw.values[5] === bitmapFirstY);
        if (!bitmapSelection || bitmapYs.length !== 2 || bitmapYs[0] !== bitmapFirstY ||
            bitmapYs[1] !== bitmapFirstY + 35 || !bitmapMarker)
            fail(`Phase 5.2.2 wrapped bitmap row geometry differed: ${JSON.stringify({ bitmapSelection, bitmapLineStarts, bitmapMarker })}`);

        const expectedOffsets = new Map([[32, 0], [33, 8], [34, -20], [36, -2]]);
        for (const [frame, offset] of expectedOffsets) {
            const cursors = imageDraws.filter(draw => draw.frame === frame && path.basename(draw.source) === "Cursor.png" &&
                draw.values.length === 8);
            if (cursors.length !== 4) fail(`Phase 5.2.2 expected four visible cursors on frame ${frame}, found ${cursors.length}`);
            for (const cursor of cursors) {
                const row = fillRectangleDraws.find(draw => draw.frame === frame && draw.values[0] === cursor.values[4] &&
                    draw.values[2] > cursor.values[6] && (draw.values[3] === 43 || draw.values[3] === 78));
                if (!row) fail(`Phase 5.2.2 cursor row was not traced on frame ${frame}: ${JSON.stringify(cursor)}`);
                const maximumY = row.values[1] + row.values[3] - cursor.values[7];
                let expectedY = row.values[1] + Math.max(0, Math.trunc((row.values[3] - cursor.values[7]) / 2)) + offset;
                expectedY = maximumY >= row.values[1] ? Math.max(row.values[1], Math.min(maximumY, expectedY)) : row.values[1];
                if (cursor.values[5] !== expectedY)
                    fail(`Phase 5.2.2 centered cursor offset differed on frame ${frame}: ${JSON.stringify({ cursor, row, expectedY })}`);
            }
        }
        const oversizedCursors = imageDraws.filter(draw => draw.frame === 35 && path.basename(draw.source) === "Cursor.png" &&
            draw.values.length === 8 && draw.values[7] === 96);
        if (oversizedCursors.length !== 4 || !oversizedCursors.every(cursor => fillRectangleDraws.some(draw =>
            draw.frame === 35 && draw.values[0] === cursor.values[4] && draw.values[1] === cursor.values[5] &&
            (draw.values[3] === 43 || draw.values[3] === 78))))
            fail(`Phase 5.2.2 oversized cursors were not clamped to row tops: ${JSON.stringify(oversizedCursors)}`);
        if (!textDraws.some(draw => draw.frame === 37 && draw.value === "A Very Long Shared Library Category"))
            fail("Phase 5.2.2 clip mode did not draw the full first source line inside the row clip");

        const scrollbar = (frame, x) => fillRectangleDraws.filter(draw => draw.frame === frame && draw.values[0] === x && draw.values[2] === 4);
        const topScroll = scrollbar(4, 915);
        const middleScroll = scrollbar(9, 915);
        const bottomScroll = scrollbar(11, 915);
        if (topScroll.length !== 2 || middleScroll.length !== 2 || bottomScroll.length !== 2)
            fail(`Phase 5.2.2 detail scrollbar track/thumb count differed: ${JSON.stringify({ topScroll, middleScroll, bottomScroll })}`);
        const topThumb = topScroll.find(draw => draw.values[3] < 172);
        const middleThumb = middleScroll.find(draw => draw.values[3] < 172);
        const bottomThumb = bottomScroll.find(draw => draw.values[3] < 172);
        if (!topThumb || !middleThumb || !bottomThumb || topThumb.values[3] !== 86 ||
            topThumb.values[1] !== 326 || middleThumb.values[1] !== 369 || bottomThumb.values[1] !== 412)
            fail(`Phase 5.2.2 proportional scrollbar geometry differed: ${JSON.stringify({ topThumb, middleThumb, bottomThumb })}`);
        if (fillRectangleDraws.some(draw => draw.frame === 12 && draw.values[2] === 4))
            fail("Phase 5.2.2 ShowScrollbar False left track/thumb drawing");
        const restoredScrollbars = fillRectangleDraws.filter(draw => draw.frame === 13 && draw.values[2] === 4);
        if (scrollbar(13, 915).length !== 2 || restoredScrollbars.length < 4)
            fail(`Phase 5.2.2 ShowScrollbar True did not restore overflowing scrollbars: ${JSON.stringify(restoredScrollbars)}`);
        const frameFourMarkers = textDraws.filter(draw => draw.frame === 4 && draw.value === " >");
        const frameFourTrackXs = fillRectangleDraws.filter(draw => draw.frame === 4 && draw.values[2] === 4).map(draw => draw.values[0]);
        if (!frameFourMarkers.some(marker => frameFourTrackXs.some(trackX => trackX >= marker.x + 16 && trackX - marker.x < 100)))
            fail(`Phase 5.2.2 marker/scrollbar regions overlapped: ${JSON.stringify({ frameFourMarkers, frameFourTrackXs })}`);
        const frameFourLastDetailRowDraw = Math.max(...textDraws.filter(draw => draw.frame === 4 && draw.x >= 580).map(draw => draw.order),
            ...imageDraws.filter(draw => draw.frame === 4 && path.basename(draw.source) === "Cursor.png" && draw.values[4] >= 548).map(draw => draw.order));
        if (Math.min(...scrollbar(4, 915).map(draw => draw.order)) <= frameFourLastDetailRowDraw)
            fail("Phase 5.2.2 scrollbars were not drawn after row cursor/text/indicator content");

        const frameFourWindows = imageDraws.filter(draw => draw.frame === 4 && path.basename(draw.source) === "WindowSkin.png" &&
            draw.values.length === 8 && draw.values[0] === 0 && draw.values[1] === 0)
            .map(draw => [draw.values[4], draw.values[5]]);
        const expectedWindows = [[200, 250], [12, 258], [300, 286], [548, 298]];
        if (JSON.stringify(frameFourWindows.slice(0, 4)) !== JSON.stringify(expectedWindows))
            fail(`Phase 5.2.2 viewport placement/painter stack differed: ${JSON.stringify(frameFourWindows)}`);
        if (fillRectangleDraws.filter(draw => draw.frame === 4).length < 4)
            fail("Phase 5.2.2 ancestor path selection fills were not retained");
        if (!fillRectangleDraws.some(draw => draw.frame >= 27))
            fail("Phase 5.2.2 vector fallback drawing was not recorded");
        for (const required of ["Move.wav", "Confirm.wav", "Cancel.wav"])
            if (!audioSources.some(source => path.basename(source) === required))
                fail(`Phase 5.2.2 event-driven SFX did not include ${required}: ${JSON.stringify(audioSources)}`);
        if (clipCalls < maximumFrames)
            fail(`Phase 5.2.2 expected structured clipping on each frame, found ${clipCalls} clips across ${maximumFrames} frames`);
        if (diagnostics.backingWidth <= diagnostics.logicalWidth || diagnostics.backingHeight <= diagnostics.logicalHeight)
            fail(`Phase 5.2.2 DPR backing store was not high resolution: ${diagnostics.backingWidth}x${diagnostics.backingHeight}`);
        if (diagnostics.imageCacheCount !== 0 || diagnostics.imageReferenceCount !== 0 ||
            diagnostics.sfxActiveCount !== 0 || !diagnostics.mediaStopped)
            fail(`Phase 5.2.2 Web ownership/shutdown was incomplete: ${JSON.stringify(diagnostics)}`);
        if (hostConsoleErrors.length !== 0)
            fail(`Phase 5.2.2 Web console reported errors: ${hostConsoleErrors.join("\n")}`);
    }
    if (verifyPhase5SubmenuViewport) {
        const scrollbar = x => fillRectangleDraws.filter(draw => draw.frame === 1 && draw.values[0] === x && draw.values[2] === 4);
        const five = scrollbar(91);
        const twenty = scrollbar(189);
        const sixtyFour = scrollbar(287);
        const allVisible = scrollbar(385);
        const hidden = scrollbar(483);
        const tiny = scrollbar(551);
        if (five.length !== 2 || twenty.length !== 2 || sixtyFour.length !== 2 || tiny.length !== 2 ||
            allVisible.length !== 0 || hidden.length !== 0)
            fail(`Phase 5.2.1 viewport scrollbar visibility differed: ${JSON.stringify({ five, twenty, sixtyFour, allVisible, hidden, tiny })}`);
        const thumb = draws => draws.find(draw => draw.values[3] < 48);
        const fiveThumb = thumb(five);
        const twentyThumb = thumb(twenty);
        const sixtyFourThumb = thumb(sixtyFour);
        if (!fiveThumb || !twentyThumb || !sixtyFourThumb ||
            fiveThumb.values[3] !== 38 || twentyThumb.values[3] !== 9 || sixtyFourThumb.values[3] !== 8 ||
            !(fiveThumb.values[3] > twentyThumb.values[3] && twentyThumb.values[3] > sixtyFourThumb.values[3]))
            fail(`Phase 5.2.1 4-of-5/20/64 proportional thumb sizes differed: ${JSON.stringify({ fiveThumb, twentyThumb, sixtyFourThumb })}`);
        if (tiny.some(draw => draw.values[1] < 62 || draw.values[3] < 0 || draw.values[1] + draw.values[3] > 65))
            fail(`Phase 5.2.1 tiny scrollbar escaped its three-pixel track: ${JSON.stringify(tiny)}`);
        if (diagnostics.backingWidth <= diagnostics.logicalWidth || diagnostics.backingHeight <= diagnostics.logicalHeight)
            fail(`Phase 5.2.1 viewport DPR backing store was not high resolution: ${diagnostics.backingWidth}x${diagnostics.backingHeight}`);
        if (diagnostics.imageCacheCount !== 0 || diagnostics.imageReferenceCount !== 0 || !diagnostics.mediaStopped)
            fail(`Phase 5.2.1 viewport ownership/shutdown was incomplete: ${JSON.stringify(diagnostics)}`);
        if (hostConsoleErrors.length !== 0)
            fail(`Phase 5.2.1 viewport Web console reported errors: ${hostConsoleErrors.join("\n")}`);
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
        if (!systemLines.includes("System") || !systemLines.includes("Multiline") || systemLines.includes("Hidden"))
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
    if (expectedDrawText !== null) process.stdout.write(" (dynamic Draw Text parity)");
    if (verifyPhase4Media) process.stdout.write(" (Phase 4 media/cache/clip/data/audio parity)");
    if (verifyPhase4Ownership) process.stdout.write(" (Phase 4.1 Image ownership/high-DPI parity)");
    if (verifyPhase4Clip) process.stdout.write(" (Phase 4.1 clip/high-DPI resize parity)");
    if (verifyPhase4Audio) process.stdout.write(" (Phase 4.1 audio generation/shutdown parity)");
    if (verifyPhase5Ui) process.stdout.write(" (Phase 5 scripted UI/high-DPI/painter/audio/ownership parity)");
    if (verifyPhase5Hardening) process.stdout.write(" (Phase 5.1 validation/reflow/multiline/high-DPI/ownership parity)");
    process.stdout.write("\n");
})().catch(error => fail(error && error.stack ? error.stack : String(error)));
}
