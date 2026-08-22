using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Smile.Compiler;

internal static class WebOutputWriter
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);
    internal static readonly IReadOnlyList<string> ManagedFileNames =
        new[] { "index.html", "smile-runtime.js", "game.js", "smile.css" };

    public static void Write(string outputDirectory, WebEmitter emitter)
        => Write(outputDirectory, emitter, null);

    internal static void Write(string outputDirectory, WebEmitter emitter, Action<string>? afterFileWrite)
    {
        var game = emitter.Emit();
        var buildVersion = BuildVersion(emitter.Title, game);
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(outputDirectory, "index.html"), Index(emitter.Title, buildVersion), Utf8WithoutBom);
        afterFileWrite?.Invoke("index.html");
        File.WriteAllText(Path.Combine(outputDirectory, "smile-runtime.js"), Runtime, Utf8WithoutBom);
        afterFileWrite?.Invoke("smile-runtime.js");
        File.WriteAllText(Path.Combine(outputDirectory, "game.js"), game, Utf8WithoutBom);
        afterFileWrite?.Invoke("game.js");
        File.WriteAllText(Path.Combine(outputDirectory, "smile.css"), Style, Utf8WithoutBom);
        afterFileWrite?.Invoke("smile.css");
    }

    private static string BuildVersion(string title, string game)
    {
        var unversionedIndex = Index(title, string.Empty);
        var content = string.Join('\0', unversionedIndex, Runtime, game, Style);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }

    private static string Index(string title, string buildVersion) => $$"""
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
          <meta http-equiv="Cache-Control" content="no-cache, no-store, must-revalidate">
          <meta http-equiv="Pragma" content="no-cache">
          <meta http-equiv="Expires" content="0">
          <meta name="smile-build" content="{{buildVersion}}">
          <title>{{WebUtility.HtmlEncode(title)}}</title>
          <link rel="stylesheet" href="smile.css?v={{buildVersion}}">
        </head>
        <body>
          <main id="smile-shell">
            <canvas id="smile-canvas" width="960" height="540" tabindex="0" aria-label="{{WebUtility.HtmlEncode(title)}}"></canvas>
            <pre id="smile-console" hidden aria-live="polite"></pre>
            <pre id="smile-error" hidden></pre>
            <section id="smile-controls" hidden aria-hidden="true" aria-label="Game controls">
              <div class="smile-dpad" aria-label="Directional controls">
                <button class="smile-control-up" type="button" data-smile-control="up" aria-label="Move up" aria-pressed="false"><span aria-hidden="true"></span></button>
                <button class="smile-control-left" type="button" data-smile-control="left" aria-label="Move left" aria-pressed="false"><span aria-hidden="true"></span></button>
                <button class="smile-control-right" type="button" data-smile-control="right" aria-label="Move right" aria-pressed="false"><span aria-hidden="true"></span></button>
                <button class="smile-control-down" type="button" data-smile-control="down" aria-label="Move down" aria-pressed="false"><span aria-hidden="true"></span></button>
              </div>
              <div class="smile-action-controls" aria-label="Action controls">
                <button class="smile-control-y" type="button" data-smile-control="y" aria-label="Gamepad Y button" aria-pressed="false">Y</button>
                <button class="smile-control-x" type="button" data-smile-control="x" aria-label="Gamepad X button" aria-pressed="false">X</button>
                <button class="smile-control-b" type="button" data-smile-control="b" aria-label="Gamepad B button" aria-pressed="false">B</button>
                <button class="smile-control-a" type="button" data-smile-control="a" aria-label="Gamepad A button" aria-pressed="false">A</button>
              </div>
            </section>
          </main>
          <script src="smile-runtime.js?v={{buildVersion}}"></script>
          <script src="game.js?v={{buildVersion}}"></script>
        </body>
        </html>
        """;

    private const string Style = """
        :root { color-scheme: dark; font-family: "Segoe UI", Arial, sans-serif; }
        * { box-sizing: border-box; }
        html, body { width: 100%; height: 100%; min-height: 100dvh; margin: 0; overflow: hidden; background: #05070c; }
        body { display: grid; place-items: center; }
        #smile-shell { position: relative; width: 100vw; width: 100dvw; height: 100vh; height: 100dvh; display: grid; place-items: center; background: #05070c; }
        #smile-canvas { display: block; max-width: 100vw; max-width: 100dvw; max-height: 100vh; max-height: 100dvh; width: auto; height: auto; aspect-ratio: 16 / 9; background: #000; outline: none; touch-action: none; }
        #smile-canvas:focus-visible { box-shadow: inset 0 0 0 2px #46e6ff; }
        #smile-console { width: min(72rem, 100vw); height: 100vh; margin: 0; padding: 1rem; overflow: auto; color: #f2f4f8; background: #05070c; font: 16px/1.4 Consolas, monospace; white-space: pre-wrap; }
        #smile-error { position: absolute; z-index: 20; left: 1rem; right: 1rem; bottom: 1rem; max-height: 35vh; overflow: auto; margin: 0; padding: 1rem; color: #fff; background: #761b25; border: 1px solid #ff8794; white-space: pre-wrap; }
        #smile-controls[hidden] { display: none; }
        #smile-controls { position: absolute; z-index: 10; inset: 0; pointer-events: none; user-select: none; -webkit-user-select: none; -webkit-touch-callout: none; }
        #smile-controls button { pointer-events: auto; touch-action: none; display: grid; place-items: center; min-width: 56px; min-height: 56px; padding: 0; border: 2px solid rgba(220, 247, 255, .56); border-radius: 50%; color: #fff; background: rgba(9, 20, 36, .66); box-shadow: 0 3px 12px rgba(0, 0, 0, .4); font: 700 clamp(18px, 4vmin, 28px)/1 "Segoe UI", Arial, sans-serif; }
        #smile-controls button:focus-visible { outline: 3px solid #46e6ff; outline-offset: 3px; }
        #smile-controls button.smile-control-active { background: rgba(27, 157, 190, .9); border-color: #fff; transform: translateY(2px) scale(.97); }
        .smile-dpad, .smile-action-controls { position: absolute; bottom: max(24px, env(safe-area-inset-bottom, 0px)); display: grid; grid-template: repeat(3, clamp(56px, 12vmin, 78px)) / repeat(3, clamp(56px, 12vmin, 78px)); gap: 4px; }
        .smile-dpad { left: max(14px, env(safe-area-inset-left, 0px)); }
        .smile-action-controls { right: max(14px, env(safe-area-inset-right, 0px)); }
        .smile-dpad button, .smile-action-controls button { width: clamp(56px, 12vmin, 78px); height: clamp(56px, 12vmin, 78px); }
        .smile-dpad button span { display: block; width: 0; height: 0; border-right: clamp(7px, 1.5vmin, 10px) solid transparent; border-bottom: clamp(12px, 2.6vmin, 17px) solid currentColor; border-left: clamp(7px, 1.5vmin, 10px) solid transparent; transform-origin: center; }
        .smile-control-left span { transform: rotate(-90deg); }
        .smile-control-right span { transform: rotate(90deg); }
        .smile-control-down span { transform: rotate(180deg); }
        .smile-control-up, .smile-control-y { grid-column: 2; grid-row: 1; }
        .smile-control-left, .smile-control-x { grid-column: 1; grid-row: 2; }
        .smile-control-right, .smile-control-b { grid-column: 3; grid-row: 2; }
        .smile-control-down, .smile-control-a { grid-column: 2; grid-row: 3; }
        @media (orientation: portrait) {
          #smile-shell.smile-controls-visible { display: flex; flex-direction: column; justify-content: center; align-items: center; gap: clamp(8px, 2dvh, 18px); padding-top: max(8px, env(safe-area-inset-top, 0px)); padding-bottom: max(8px, env(safe-area-inset-bottom, 0px)); }
          #smile-shell.smile-controls-visible #smile-controls { position: relative; inset: auto; width: 100%; height: clamp(168px, 36vmin, 234px); flex: 0 0 clamp(168px, 36vmin, 234px); }
          .smile-dpad, .smile-action-controls { gap: 0; }
          #smile-shell.smile-controls-visible .smile-dpad { top: 0; bottom: auto; left: max(8px, env(safe-area-inset-left, 0px)); }
          #smile-shell.smile-controls-visible .smile-action-controls { top: 0; right: max(8px, env(safe-area-inset-right, 0px)); bottom: auto; }
        }
        @media (orientation: portrait) and (max-width: 359px) {
          #smile-shell.smile-controls-visible #smile-controls { height: 224px; flex-basis: 224px; }
          #smile-shell.smile-controls-visible .smile-action-controls { top: 56px; }
        }
        """;

    private const string Runtime = """
        "use strict";

        let webFreshnessCheckPending = false;

        function checkForWebUpdate() {
            if (webFreshnessCheckPending || !window.location || !window.location.href) return;
            const marker = document.querySelector && document.querySelector('meta[name="smile-build"]');
            const currentBuild = marker && marker.content;
            if (!/^[a-f0-9]{16}$/.test(currentBuild || "")) return;
            webFreshnessCheckPending = true;
            try {
                const request = new URL(window.location.href);
                request.hash = "";
                request.searchParams.set("smile-cache-check", String(Date.now()));
                fetch(request.toString(), { cache: "no-store" })
                    .then(response => response.ok ? response.text() : "")
                    .then(html => {
                        const latest = html.match(/<meta name="smile-build" content="([a-f0-9]{16})">/i);
                        if (!latest || latest[1].toLowerCase() === currentBuild) return;
                        const destination = new URL(window.location.href);
                        destination.searchParams.set("smile-version", latest[1].toLowerCase());
                        window.location.replace(destination.toString());
                    })
                    .catch(() => { })
                    .finally(() => { webFreshnessCheckPending = false; });
            } catch (_) {
                webFreshnessCheckPending = false;
            }
        }

        window.addEventListener("pageshow", checkForWebUpdate);

        window.__smileWeb = { status: "starting", frameCount: 0 };

        window.smile = (() => {
            const MAX_SAFE = Number.MAX_SAFE_INTEGER;
            const STOP = Object.freeze({ smileStop: true });
            const canvas = document.getElementById("smile-canvas");
            const visible = canvas.getContext("2d", { alpha: false });
            const backCanvas = document.createElement("canvas");
            const back = backCanvas.getContext("2d", { alpha: false });
            const renderer3DCanvas = document.createElement("canvas");
            const consoleOutput = document.getElementById("smile-console");
            const errorPanel = document.getElementById("smile-error");
            const shell = document.getElementById("smile-shell");
            const virtualControls = document.getElementById("smile-controls");
            const virtualControlButtons = virtualControls
                ? Array.from(virtualControls.querySelectorAll("button[data-smile-control]"))
                : [];
            const keys = [];
            const inputSources = new Map();
            const heldKeyCounts = new Map();
            const activeVirtualPointers = new Map();
            const activeCanvasPointers = new Map();
            const memoryStorage = new Map();
            const imageCache = new Map();
            const sfxCache = new Map();
            const sfxChannels = new Array(16).fill(null);
            const sfxGenerations = new Array(16).fill(0);
            const clipStack = [];
            const assetPaths = new Set();
            const MAX_QUEUED_KEYS = 256;
            const MAX_ACTIVE_INPUT_SOURCES = 32;
            const MAX_BACKING_DIMENSION = 8192;
            const MAX_BACKING_PIXELS = 33554432;
            const virtualControlProfiles = Object.freeze({
                standard: Object.freeze({
                    up: 10, down: 11, left: 12, right: 13,
                    a: 23, b: 24, x: 25, y: 26
                })
            });
            const activeVirtualControlProfile = virtualControlProfiles.standard;
            const virtualControlsMode = readVirtualControlsMode();
            const initiallyTouchFirst = initialTouchFirstCapability();
            let logicalWidth = 960;
            let logicalHeight = 540;
            let backingWidth = 960;
            let backingHeight = 540;
            let imageDecodeCount = 0;
            let imageCacheHitCount = 0;
            const classMetadata = new WeakMap();
            let classLiveObjects = 0;
            let shutdownImageReferences = 0;
            let shutdownImageCacheEntries = 0;
            let sfxCompletionCount = 0;
            let closed = false;
            let active = true;
            let mediaStopped = false;
            let userInteracted = false;
            let audioContext = null;
            let currentMusic = null;
            let musicVolume = 100;
            let musicRequested = false;
            let musicPaused = false;
            let consoleText = "";
            let storageNamespace = "smile2:web";
            let gameWindowCreated = false;
            let touchInteractionObserved = false;
            let virtualControlsVisible = false;
            let pointerXValue = 0;
            let pointerYValue = 0;
            let pointerDeltaXValue = 0;
            let pointerDeltaYValue = 0;
            let pointerWheelDeltaValue = 0;
            let pointerInsideValue = false;
            let pointerPositionValid = false;
            let pointerHeldButtons = 0;
            let pointerPressedButtons = 0;
            let pointerReleasedButtons = 0;
            let renderer3DGl = null;
            let renderer3DProgram = null;
            let renderer3DLastError = 0;
            let renderer3DNextHandle = 1;
            let renderer3DFrameActive = false;
            const renderer3DMeshes = new Map();
            const renderer3DObjects = new Map();
            const renderer3DTextures = new Map();
            const renderer3DMaterials = new Map();
            const renderer3DModels = new Map();
            const renderer3DSkeletons = new Map();
            const renderer3DClips = new Map();
            const renderer3DAnimators = new Map();
            const renderer3DStaticBones = new Float32Array(32 * 16);
            for (let bone = 0; bone < 32; bone += 1)
                renderer3DStaticBones[bone * 16] = renderer3DStaticBones[bone * 16 + 5] =
                    renderer3DStaticBones[bone * 16 + 10] = renderer3DStaticBones[bone * 16 + 15] = 1;
            const renderer3DCamera = {
                position: [0, 300, -800], target: [0, 0, 0], fov: 55, near: 1, far: 10000
            };

            function readVirtualControlsMode() {
                try {
                    const search = globalThis.location && typeof globalThis.location.search === "string"
                        ? globalThis.location.search
                        : "";
                    const values = new URLSearchParams(search).getAll("smile-controls");
                    if (values.length !== 1) return "auto";
                    const value = values[0].toLowerCase();
                    return value === "on" || value === "off" || value === "auto" ? value : "auto";
                } catch (_) { return "auto"; }
            }

            function mediaMatches(query) {
                try { return typeof globalThis.matchMedia === "function" && Boolean(globalThis.matchMedia(query).matches); }
                catch (_) { return false; }
            }

            function initialTouchFirstCapability() {
                let touchPoints = 0;
                try { touchPoints = Number(globalThis.navigator && globalThis.navigator.maxTouchPoints || 0); }
                catch (_) { }
                return touchPoints > 0 && (mediaMatches("(pointer: coarse)") || mediaMatches("(hover: none)"));
            }

            function enqueueKey(key) {
                if (!Number.isSafeInteger(key)) return false;
                keys.push(key);
                if (keys.length > MAX_QUEUED_KEYS) keys.shift();
                return true;
            }

            function pressInput(sourceId, key, enqueue = true) {
                if (typeof sourceId !== "string" || sourceId.length === 0 || !Number.isSafeInteger(key)) return false;
                const previous = inputSources.get(sourceId);
                if (previous === key) return false;
                if (previous !== undefined) releaseInput(sourceId);
                if (inputSources.size >= MAX_ACTIVE_INPUT_SOURCES) return false;
                inputSources.set(sourceId, key);
                heldKeyCounts.set(key, (heldKeyCounts.get(key) || 0) + 1);
                if (enqueue) enqueueKey(key);
                return true;
            }

            function releaseInput(sourceId) {
                const key = inputSources.get(sourceId);
                if (key === undefined) return false;
                inputSources.delete(sourceId);
                const count = heldKeyCounts.get(key) || 0;
                if (count <= 1) heldKeyCounts.delete(key);
                else heldKeyCounts.set(key, count - 1);
                return true;
            }

            function releaseInputsByPrefix(prefix) {
                for (const sourceId of Array.from(inputSources.keys()))
                    if (sourceId.startsWith(prefix)) releaseInput(sourceId);
            }

            function resetVirtualButton(button) {
                button.classList.remove("smile-control-active");
                button.setAttribute("aria-pressed", "false");
            }

            function releaseAllInputs() {
                inputSources.clear();
                heldKeyCounts.clear();
                activeVirtualPointers.clear();
                for (const button of virtualControlButtons) resetVirtualButton(button);
                releaseCanvasPointers(false);
                pointerDeltaXValue = 0;
                pointerDeltaYValue = 0;
                pointerWheelDeltaValue = 0;
                pointerPressedButtons = 0;
                pointerReleasedButtons = 0;
            }

            function pointerButtonMask(button) {
                button = safe(button);
                return button >= 1 && button <= 3 ? 1 << (button - 1) : 0;
            }

            function canvasButton(event) {
                if (event.pointerType === "touch" || event.pointerType === "pen") return 1;
                if (event.button === 0) return 1;
                if (event.button === 2) return 2;
                if (event.button === 1) return 3;
                return 0;
            }

            function updatePointerPosition(event) {
                if (!Number.isFinite(event.clientX) || !Number.isFinite(event.clientY) ||
                    typeof canvas.getBoundingClientRect !== "function") return;
                const bounds = canvas.getBoundingClientRect();
                if (!(bounds.width > 0) || !(bounds.height > 0)) return;
                const nextX = Math.round((event.clientX - bounds.left) * logicalWidth / bounds.width);
                const nextY = Math.round((event.clientY - bounds.top) * logicalHeight / bounds.height);
                if (pointerPositionValid) {
                    pointerDeltaXValue = safe(pointerDeltaXValue + nextX - pointerXValue);
                    pointerDeltaYValue = safe(pointerDeltaYValue + nextY - pointerYValue);
                }
                pointerXValue = safe(nextX);
                pointerYValue = safe(nextY);
                pointerInsideValue = event.clientX >= bounds.left && event.clientY >= bounds.top &&
                    event.clientX < bounds.right && event.clientY < bounds.bottom;
                pointerPositionValid = true;
            }

            function refreshPointerButtons() {
                let buttons = 0;
                for (const entry of activeCanvasPointers.values()) buttons |= entry.mask;
                pointerHeldButtons = buttons;
            }

            function releaseCanvasPointer(pointerId, releaseCapture = true) {
                const entry = activeCanvasPointers.get(pointerId);
                if (!entry) return false;
                activeCanvasPointers.delete(pointerId);
                const wasHeld = (pointerHeldButtons & entry.mask) !== 0;
                refreshPointerButtons();
                if (wasHeld && (pointerHeldButtons & entry.mask) === 0) pointerReleasedButtons |= entry.mask;
                if (releaseCapture && typeof canvas.releasePointerCapture === "function") {
                    try { canvas.releasePointerCapture(pointerId); } catch (_) { }
                }
                return true;
            }

            function releaseCanvasPointers(recordRelease = true) {
                if (recordRelease) pointerReleasedButtons |= pointerHeldButtons;
                activeCanvasPointers.clear();
                pointerHeldButtons = 0;
                pointerInsideValue = false;
                pointerPositionValid = false;
            }

            function handleCanvasPointerDown(event) {
                noteTouchInteraction(event);
                if (!gameWindowCreated || closed || mediaStopped || !active ||
                    !Number.isSafeInteger(event.pointerId) || activeCanvasPointers.has(event.pointerId)) return;
                const button = canvasButton(event);
                const mask = pointerButtonMask(button);
                if (mask === 0 || activeCanvasPointers.size >= MAX_ACTIVE_INPUT_SOURCES) return;
                updatePointerPosition(event);
                const wasHeld = (pointerHeldButtons & mask) !== 0;
                activeCanvasPointers.set(event.pointerId, { mask });
                refreshPointerButtons();
                if (!wasHeld) pointerPressedButtons |= mask;
                if (typeof canvas.setPointerCapture === "function") {
                    try { canvas.setPointerCapture(event.pointerId); } catch (_) { }
                }
                event.preventDefault();
                userInteracted = true;
                syncMusic();
            }

            function handleCanvasPointerMove(event) {
                if (!gameWindowCreated || closed) return;
                updatePointerPosition(event);
                if (activeCanvasPointers.has(event.pointerId)) event.preventDefault();
            }

            function handleCanvasPointerEnd(event, releaseCapture = true) {
                if (!Number.isSafeInteger(event.pointerId) || !activeCanvasPointers.has(event.pointerId)) return;
                updatePointerPosition(event);
                event.preventDefault();
                releaseCanvasPointer(event.pointerId, releaseCapture);
            }

            function handleCanvasWheel(event) {
                if (!gameWindowCreated || closed || !active) return;
                updatePointerPosition(event);
                const direction = event.deltaY < 0 ? 1 : event.deltaY > 0 ? -1 : 0;
                pointerWheelDeltaValue = safe(pointerWheelDeltaValue + direction);
                if (direction !== 0) event.preventDefault();
                userInteracted = true;
                syncMusic();
            }

            function pointerX() { return pointerXValue; }
            function pointerY() { return pointerYValue; }
            function pointerDeltaX() { return pointerDeltaXValue; }
            function pointerDeltaY() { return pointerDeltaYValue; }
            function pointerWheelDelta() { return pointerWheelDeltaValue; }
            function pointerInside() { return pointerInsideValue ? 1 : 0; }
            function pointerHeld(button) { const mask = pointerButtonMask(button); return mask !== 0 && (pointerHeldButtons & mask) !== 0 ? 1 : 0; }
            function pointerPressed(button) { const mask = pointerButtonMask(button); return mask !== 0 && (pointerPressedButtons & mask) !== 0 ? 1 : 0; }
            function pointerReleased(button) { const mask = pointerButtonMask(button); return mask !== 0 && (pointerReleasedButtons & mask) !== 0 ? 1 : 0; }

            function releaseVirtualPointers() {
                releaseInputsByPrefix("pointer:");
                activeVirtualPointers.clear();
                for (const button of virtualControlButtons) resetVirtualButton(button);
            }

            function setVirtualControlsVisible(visibleState) {
                const next = Boolean(visibleState && gameWindowCreated && !closed && virtualControls);
                if (virtualControlsVisible === next) return;
                if (!next) releaseVirtualPointers();
                virtualControlsVisible = next;
                if (virtualControls) {
                    virtualControls.hidden = !next;
                    virtualControls.setAttribute("aria-hidden", next ? "false" : "true");
                }
                if (shell) shell.classList.toggle("smile-controls-visible", next);
            }

            function updateVirtualControlsVisibility() {
                const shouldShow = virtualControlsMode === "on" ||
                    (virtualControlsMode === "auto" && (initiallyTouchFirst || touchInteractionObserved));
                setVirtualControlsVisible(virtualControlsMode !== "off" && shouldShow);
            }

            function noteTouchInteraction(event) {
                if (virtualControlsMode !== "auto" || touchInteractionObserved) return;
                if (event.pointerType !== "touch" && event.pointerType !== "pen") return;
                touchInteractionObserved = true;
                updateVirtualControlsVisibility();
            }

            function refreshVirtualButton(button) {
                const pressed = Array.from(activeVirtualPointers.values()).some(pointer => pointer.button === button);
                button.classList.toggle("smile-control-active", pressed);
                button.setAttribute("aria-pressed", pressed ? "true" : "false");
            }

            function releaseVirtualPointer(pointerId, releaseCapture = true) {
                const pointer = activeVirtualPointers.get(pointerId);
                if (!pointer) return false;
                activeVirtualPointers.delete(pointerId);
                releaseInput(pointer.sourceId);
                if (releaseCapture && typeof pointer.button.releasePointerCapture === "function") {
                    try { pointer.button.releasePointerCapture(pointerId); } catch (_) { }
                }
                refreshVirtualButton(pointer.button);
                return true;
            }

            function handleVirtualPointerDown(event, button) {
                noteTouchInteraction(event);
                if (!active || closed || mediaStopped || !virtualControlsVisible ||
                    !Number.isSafeInteger(event.pointerId) || activeVirtualPointers.has(event.pointerId)) return;
                const isTouchOrPen = event.pointerType === "touch" || event.pointerType === "pen";
                const isPrimaryMouse = event.pointerType === "mouse" && event.button === 0;
                if (!isTouchOrPen && !isPrimaryMouse) return;
                const controlName = button.dataset.smileControl;
                if (!Object.prototype.hasOwnProperty.call(activeVirtualControlProfile, controlName)) return;
                const key = activeVirtualControlProfile[controlName];
                const sourceId = `pointer:${event.pointerId}`;
                if (!pressInput(sourceId, key, true)) return;
                event.preventDefault();
                if (typeof button.setPointerCapture === "function") {
                    try { button.setPointerCapture(event.pointerId); } catch (_) { }
                }
                activeVirtualPointers.set(event.pointerId, { sourceId, button });
                refreshVirtualButton(button);
                userInteracted = true;
                syncMusic();
            }

            function handleVirtualPointerEnd(event, releaseCapture = true) {
                if (!Number.isSafeInteger(event.pointerId) || !activeVirtualPointers.has(event.pointerId)) return;
                event.preventDefault();
                releaseVirtualPointer(event.pointerId, releaseCapture);
            }

            for (const button of virtualControlButtons) {
                button.addEventListener("pointerdown", event => handleVirtualPointerDown(event, button));
                button.addEventListener("pointerup", event => handleVirtualPointerEnd(event));
                button.addEventListener("pointercancel", event => handleVirtualPointerEnd(event));
                button.addEventListener("lostpointercapture", event => handleVirtualPointerEnd(event, false));
            }

            function configure(appIdentity, manifest) {
                storageNamespace = `smile2:${sha256Hex(utf8(String(appIdentity)))}`;
                assetPaths.clear();
                for (const path of manifest || []) assetPaths.add(canonicalAssetPath(path));
            }

            function safe(value) {
                if (!Number.isSafeInteger(value))
                    throw new Error(`SMILE Web Number is outside the safe integer range: ${value}`);
                return value;
            }

            function operands(left, right) {
                return [safe(left), safe(right)];
            }

            function add(left, right) { [left, right] = operands(left, right); return safe(left + right); }
            function sub(left, right) { [left, right] = operands(left, right); return safe(left - right); }
            function mul(left, right) { [left, right] = operands(left, right); return safe(left * right); }
            function neg(value) { return safe(-safe(value)); }
            function div(left, right) {
                [left, right] = operands(left, right);
                if (right === 0) throw new Error("SMILE Web division by zero.");
                return safe(Math.trunc(left / right));
            }
            function mod(left, right) {
                [left, right] = operands(left, right);
                if (right === 0) throw new Error("SMILE Web Mod by zero.");
                return safe(left % right);
            }

            function isTrue(value) { return typeof value === "boolean" ? value : safe(value) !== 0; }
            function booleanText(value) { return isTrue(value) ? "True" : "False"; }
            function abs(value) { return safe(Math.abs(safe(value))); }
            function min(left, right) { [left, right] = operands(left, right); return Math.min(left, right); }
            function max(left, right) { [left, right] = operands(left, right); return Math.max(left, right); }
            function timer() { return safe(Math.trunc(performance.now())); }
            function rgb(red, green, blue) {
                red = safe(red) & 255;
                green = safe(green) & 255;
                blue = safe(blue) & 255;
                return safe(red | (green << 8) | (blue << 16));
            }
            function random(minimum, maximum) {
                minimum = safe(minimum);
                maximum = safe(maximum);
                if (maximum < minimum) throw new Error("SMILE Web Random maximum is below its minimum.");
                return safe(minimum + Math.floor(Math.random() * (maximum - minimum + 1)));
            }

            function array(dimensions, initialValue = 0) {
                let total = 1;
                for (const dimension of dimensions) {
                    if (!Number.isSafeInteger(dimension) || dimension <= 0)
                        throw new Error("SMILE Web array dimensions must be positive safe integers.");
                    total = safe(total * dimension);
                }
                const data = typeof initialValue === "function"
                    ? Array.from({ length: total }, () => initialValue())
                    : new Array(total).fill(initialValue);
                return { dimensions: dimensions.slice(), data };
            }

            function arrayOffset(target, indices) {
                if (!target || !Array.isArray(target.dimensions) || indices.length !== target.dimensions.length)
                    throw new Error("SMILE Web array rank mismatch.");
                let offset = 0;
                for (let index = 0; index < indices.length; index += 1) {
                    const value = safe(indices[index]);
                    if (value < 0 || value >= target.dimensions[index])
                        throw new Error(`SMILE Web array index ${value} is outside dimension ${index + 1}.`);
                    offset = index === 0 ? value : safe(offset * target.dimensions[index] + value);
                }
                return offset;
            }

            function get(target, indices) { return target.data[arrayOffset(target, indices)]; }
            function set(target, indices, value) { target.data[arrayOffset(target, indices)] = value; }
            function ref(getter, setter) { return { get: getter, set: setter, release() { } }; }
            function refArray(target, indices) {
                const offset = arrayOffset(target, indices);
                return { get: () => target.data[offset], set: value => { target.data[offset] = value; }, release() { } };
            }
            function invalidRef() { throw new Error("Invalid SMILE ByRef argument."); }

            function classCreate(payload, finalizer) {
                if (!payload || typeof payload !== "object") throw new Error("SMILE Class allocation failed.");
                classMetadata.set(payload, { references: 1, finalizer, disposed: false });
                classLiveObjects += 1;
                return payload;
            }

            function classRequire(value) {
                const metadata = value && classMetadata.get(value);
                if (!metadata || metadata.disposed)
                    throw new Error("Object reference is Nothing.");
                return value;
            }

            function classRetain(value) {
                if (value === null || value === undefined) return null;
                const metadata = classMetadata.get(value);
                if (!metadata || metadata.disposed) throw new Error("Object reference is Nothing.");
                metadata.references += 1;
                return value;
            }

            function classRelease(value) {
                if (value === null || value === undefined) return;
                const metadata = classMetadata.get(value);
                if (!metadata || metadata.disposed) return;
                metadata.references -= 1;
                if (metadata.references !== 0) return;
                metadata.disposed = true;
                try {
                    if (typeof metadata.finalizer === "function") metadata.finalizer(value);
                } finally {
                    classMetadata.delete(value);
                    classLiveObjects -= 1;
                }
            }

            function classMoveAssign(previous, ownedValue) {
                classRelease(previous);
                return ownedValue;
            }

            function classOwnedRef(ownedRoot, getter, setter) {
                let root = classRequire(ownedRoot);
                return {
                    get: () => { classRequire(root); return getter(root); },
                    set: value => { classRequire(root); setter(root, value); },
                    release: () => { const previous = root; root = null; classRelease(previous); }
                };
            }

            function classLiveCount() { return classLiveObjects; }

            function color(value) {
                value = safe(value);
                const red = value & 255;
                const green = (value >>> 8) & 255;
                const blue = (value >>> 16) & 255;
                return `rgb(${red}, ${green}, ${blue})`;
            }

            function gameWindow(title, width, height) {
                logicalWidth = safe(width);
                logicalHeight = safe(height);
                if (logicalWidth <= 0 || logicalHeight <= 0) throw new Error("Game Window dimensions must be positive.");
                gameWindowCreated = true;
                canvas.style.aspectRatio = `${logicalWidth} / ${logicalHeight}`;
                document.title = title;
                canvas.setAttribute("aria-label", title);
                canvas.hidden = false;
                consoleOutput.hidden = true;
                updateVirtualControlsVisibility();
                resizeCanvas();
            }

            function restoreBackState() {
                back.setTransform(backingWidth / logicalWidth, 0, 0, backingHeight / logicalHeight, 0, 0);
                back.globalAlpha = 1;
                back.imageSmoothingEnabled = true;
                for (const clip of clipStack) {
                    back.save();
                    back.beginPath();
                    back.rect(clip.x, clip.y, clip.width, clip.height);
                    back.clip();
                }
            }

            function restoreVisibleState() {
                visible.setTransform(backingWidth / logicalWidth, 0, 0, backingHeight / logicalHeight, 0, 0);
                visible.globalAlpha = 1;
                visible.imageSmoothingEnabled = true;
            }

            function resizeCanvas() {
                const scale = Math.min(window.innerWidth / logicalWidth, window.innerHeight / logicalHeight);
                const cssWidth = Math.max(1, Math.floor(logicalWidth * scale));
                const cssHeight = Math.max(1, Math.floor(logicalHeight * scale));
                const dpr = Math.max(1, Number(window.devicePixelRatio) || 1);
                let width = Math.max(1, Math.round(cssWidth * dpr));
                let height = Math.max(1, Math.round(cssHeight * dpr));
                const capScale = Math.min(1, MAX_BACKING_DIMENSION / width, MAX_BACKING_DIMENSION / height,
                    Math.sqrt(MAX_BACKING_PIXELS / (width * height)));
                width = Math.max(1, Math.floor(width * capScale));
                height = Math.max(1, Math.floor(height * capScale));
                canvas.style.width = `${cssWidth}px`;
                canvas.style.height = `${cssHeight}px`;
                if (width !== backingWidth || height !== backingHeight || canvas.width !== width || backCanvas.width !== width) {
                    backingWidth = width;
                    backingHeight = height;
                    canvas.width = backCanvas.width = width;
                    canvas.height = backCanvas.height = height;
                    renderer3DCanvas.width = width;
                    renderer3DCanvas.height = height;
                    restoreVisibleState();
                    restoreBackState();
                }
            }

            function renderer3DContext() {
                if (renderer3DGl) return renderer3DGl;
                let context = null;
                try {
                    context = renderer3DCanvas.getContext("webgl2", {
                        alpha: false, antialias: true, depth: true, preserveDrawingBuffer: true
                    });
                } catch (_) { }
                if (!context || typeof context.createShader !== "function") return null;
                renderer3DGl = context;
                return context;
            }

            function renderer3DCompile(gl, type, source) {
                const shader = gl.createShader(type);
                gl.shaderSource(shader, source);
                gl.compileShader(shader);
                if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
                    const detail = gl.getShaderInfoLog(shader) || "unknown shader error";
                    gl.deleteShader(shader);
                    throw new Error(`Renderer3D WebGL2 shader compilation failed: ${detail}`);
                }
                return shader;
            }

            function renderer3DInitialize() {
                const gl = renderer3DContext();
                if (!gl) { renderer3DLastError = 20; return false; }
                if (renderer3DProgram) return true;
                const vertex = renderer3DCompile(gl, gl.VERTEX_SHADER, `#version 300 es
                    precision highp float;
                    layout(location=0) in vec3 position;
                    layout(location=1) in vec3 normal;
                    layout(location=2) in vec2 textureUv;
                    layout(location=3) in vec4 joints;
                    layout(location=4) in vec4 weights;
                    uniform mat4 model;
                    uniform mat4 mvp;
                    uniform mat4 bones[32];
                    uniform float skinning;
                    out vec3 surfaceNormal;
                    out vec2 surfaceUv;
                    void main(){vec4 localPosition=vec4(position,1.0);vec3 localNormal=normal;if(skinning>.5){mat4 skin=bones[int(joints.x)]*weights.x+bones[int(joints.y)]*weights.y+bones[int(joints.z)]*weights.z+bones[int(joints.w)]*weights.w;localPosition=skin*localPosition;localNormal=mat3(skin)*localNormal;}gl_Position=mvp*localPosition;surfaceNormal=normalize(mat3(model)*localNormal);surfaceUv=textureUv;}`);
                const fragment = renderer3DCompile(gl, gl.FRAGMENT_SHADER, `#version 300 es
                    precision highp float;
                    in vec3 surfaceNormal;
                    in vec2 surfaceUv;
                    uniform vec4 tint;
                    uniform vec4 material;
                    uniform sampler2D baseTexture;
                    out vec4 outputColor;
                    void main(){vec4 base=tint;if(material.x>.5)base*=texture(baseTexture,surfaceUv);if(material.w>=0.0&&base.a<material.w)discard;float lit=.28+.72*max(0.0,dot(normalize(surfaceNormal),normalize(vec3(-.35,.8,-.45))));float light=material.y>.5?1.0:lit+material.z;outputColor=vec4(base.rgb*light,base.a);}`);
                const program = gl.createProgram();
                gl.attachShader(program, vertex); gl.attachShader(program, fragment); gl.linkProgram(program);
                gl.deleteShader(vertex); gl.deleteShader(fragment);
                if (!gl.getProgramParameter(program, gl.LINK_STATUS))
                    throw new Error(`Renderer3D WebGL2 program link failed: ${gl.getProgramInfoLog(program) || "unknown link error"}`);
                renderer3DProgram = {
                    handle: program,
                    model: gl.getUniformLocation(program, "model"),
                    mvp: gl.getUniformLocation(program, "mvp"),
                    tint: gl.getUniformLocation(program, "tint"),
                    material: gl.getUniformLocation(program, "material"),
                    baseTexture: gl.getUniformLocation(program, "baseTexture"),
                    bones: gl.getUniformLocation(program, "bones[0]"),
                    skinning: gl.getUniformLocation(program, "skinning")
                };
                gl.enable(gl.DEPTH_TEST); gl.depthFunc(gl.LESS); gl.disable(gl.CULL_FACE);
                gl.enable(gl.BLEND); gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
                return true;
            }

            function renderer3DHandle() { return safe(renderer3DNextHandle++); }

            function renderer3DCreateMesh(vertexCount, indexCount) {
                vertexCount = safe(vertexCount); indexCount = safe(indexCount);
                if (renderer3DMeshes.size >= 128 || vertexCount <= 0 || vertexCount > 65535 ||
                    indexCount <= 0 || indexCount > 196608 || indexCount % 3 !== 0) {
                    renderer3DLastError = 2; return 0;
                }
                const handle = renderer3DHandle();
                const vertices = new Float32Array(vertexCount * 16);
                for (let index = 0; index < vertexCount; index += 1) vertices[index * 16 + 12] = 1;
                renderer3DMeshes.set(handle, {
                    vertexCount, indexCount, vertices,
                    indices: new Uint32Array(indexCount), committed: false, explicitNormals: false,
                    maxJoint: 0, vertexBuffer: null, indexBuffer: null
                });
                return handle;
            }

            function renderer3DRequireMesh(handle) {
                const mesh = renderer3DMeshes.get(safe(handle));
                if (!mesh) renderer3DLastError = 5;
                return mesh || null;
            }

            function renderer3DRequireObject(handle) {
                const object = renderer3DObjects.get(safe(handle));
                if (!object) renderer3DLastError = 5;
                return object || null;
            }

            function renderer3DRequireTexture(handle) {
                const texture = renderer3DTextures.get(safe(handle));
                if (!texture) renderer3DLastError = 5;
                return texture || null;
            }

            function renderer3DRequireMaterial(handle) {
                const material = renderer3DMaterials.get(safe(handle));
                if (!material) renderer3DLastError = 5;
                return material || null;
            }

            function renderer3DMeshReferenceCount(handle) {
                handle = safe(handle);
                if (!renderer3DMeshes.has(handle)) return 0;
                let count = 0;
                for (const object of renderer3DObjects.values()) if (object.mesh === handle) count += 1;
                return count;
            }

            function renderer3DTextureReferenceCount(handle) {
                handle = safe(handle);
                if (!renderer3DTextures.has(handle)) return 0;
                let count = 0;
                for (const material of renderer3DMaterials.values()) if (material.texture === handle) count += 1;
                return count;
            }

            function renderer3DMaterialReferenceCount(handle) {
                handle = safe(handle);
                if (!renderer3DMaterials.has(handle)) return 0;
                let count = 0;
                for (const object of renderer3DObjects.values()) if (object.material === handle) count += 1;
                return count;
            }

            function renderer3DSetVertex(mesh, index, x, y, z) {
                index = safe(index);
                if (index < 0 || index >= mesh.vertexCount) { renderer3DLastError = 5; return false; }
                const offset = index * 16;
                mesh.vertices[offset] = safe(x); mesh.vertices[offset + 1] = safe(y); mesh.vertices[offset + 2] = safe(z);
                mesh.committed = false;
                return true;
            }

            function renderer3DSetUv(mesh, index, u, v) {
                index = safe(index);
                if (index < 0 || index >= mesh.vertexCount) { renderer3DLastError = 5; return false; }
                const offset = index * 16;
                mesh.vertices[offset + 6] = safe(u) / 1000;
                mesh.vertices[offset + 7] = safe(v) / 1000;
                mesh.committed = false;
                return true;
            }

            function renderer3DSetNormal(mesh, index, x, y, z) {
                index = safe(index);
                if (index < 0 || index >= mesh.vertexCount) { renderer3DLastError = 5; return false; }
                const offset = index * 16;
                mesh.vertices[offset + 3] = safe(x) / 1000;
                mesh.vertices[offset + 4] = safe(y) / 1000;
                mesh.vertices[offset + 5] = safe(z) / 1000;
                mesh.explicitNormals = true;
                mesh.committed = false;
                return true;
            }

            function renderer3DSetSkin(mesh,index,j0,j1,j2,j3,w0,w1,w2,w3){
                [index,j0,j1,j2,j3,w0,w1,w2,w3]=[index,j0,j1,j2,j3,w0,w1,w2,w3].map(safe);
                const joints=[j0,j1,j2,j3],weights=[w0,w1,w2,w3];
                if(index<0||index>=mesh.vertexCount||weights.reduce((a,b)=>a+b,0)!==1000||
                    joints.some(value=>value<0||value>=32)||weights.some(value=>value<0||value>1000)){
                    renderer3DLastError=34;return false;
                }
                const offset=index*16;
                for(let influence=0;influence<4;influence+=1){mesh.vertices[offset+8+influence]=joints[influence];mesh.vertices[offset+12+influence]=weights[influence]/1000;if(weights[influence]!==0)mesh.maxJoint=Math.max(mesh.maxJoint,joints[influence]);}
                mesh.committed=false;return true;
            }

            function renderer3DDeleteTextureGpu(texture) {
                const gl = renderer3DGl;
                if (gl && texture.gpu) gl.deleteTexture(texture.gpu);
                texture.gpu = null;
            }

            function renderer3DCreateTexture(image, filter, wrap) {
                filter = safe(filter); wrap = safe(wrap);
                if (!imageLoadedRaw(image) || renderer3DTextures.size >= 128 ||
                    image.entry.width > 8192 || image.entry.height > 8192 ||
                    filter < 0 || filter > 1 || wrap < 0 || wrap > 1) {
                    imageRelease(image); renderer3DLastError = 17; return 0;
                }
                const handle = renderer3DHandle();
                renderer3DTextures.set(handle, { image, filter, wrap, gpu: null });
                return handle;
            }

            function renderer3DSetMaterial(material, alphaMode, red, green, blue, opacity, unlit, emissive, cutoff) {
                [alphaMode,red,green,blue,opacity,unlit,emissive,cutoff]=
                    [alphaMode,red,green,blue,opacity,unlit,emissive,cutoff].map(safe);
                if (!material || alphaMode < 0 || alphaMode > 3 || opacity < 0 || opacity > 100 ||
                    emissive < 0 || emissive > 400 || cutoff < 0 || cutoff > 100) {
                    renderer3DLastError = 19; return false;
                }
                material.alphaMode=alphaMode;material.color=[(red&255)/255,(green&255)/255,(blue&255)/255,opacity/100];
                material.unlit=unlit!==0;material.emissive=emissive/100;material.cutoff=cutoff/100;return true;
            }

            function renderer3DCreateMaterial(texture, alphaMode, red, green, blue, opacity, unlit, emissive, cutoff) {
                texture = safe(texture);
                if ((texture !== 0 && !renderer3DTextures.has(texture)) || renderer3DMaterials.size >= 128) {
                    renderer3DLastError = 20; return 0;
                }
                const material={texture,alphaMode:0,color:[1,1,1,1],unlit:false,emissive:0,cutoff:.5};
                if (!renderer3DSetMaterial(material,alphaMode,red,green,blue,opacity,unlit,emissive,cutoff))return 0;
                const handle=renderer3DHandle();renderer3DMaterials.set(handle,material);return handle;
            }

            function renderer3DSetTriangle(mesh, triangle, a, b, c) {
                triangle = safe(triangle); a = safe(a); b = safe(b); c = safe(c);
                const offset = triangle * 3;
                if (triangle < 0 || offset + 2 >= mesh.indexCount || a < 0 || b < 0 || c < 0) {
                    renderer3DLastError = 5; return false;
                }
                mesh.indices[offset] = a; mesh.indices[offset + 1] = b; mesh.indices[offset + 2] = c;
                mesh.committed = false;
                return true;
            }

            function renderer3DDeleteGpu(mesh) {
                const gl = renderer3DGl;
                if (gl && mesh.vertexBuffer) gl.deleteBuffer(mesh.vertexBuffer);
                if (gl && mesh.indexBuffer) gl.deleteBuffer(mesh.indexBuffer);
                mesh.vertexBuffer = null; mesh.indexBuffer = null;
            }

            function renderer3DCommit(mesh) {
                if (!mesh.explicitNormals)
                    mesh.vertices.forEach((_, index) => { if (index % 16 >= 3 && index % 16 < 6) mesh.vertices[index] = 0; });
                for (let offset = 0; offset < mesh.indexCount; offset += 3) {
                    const ia = mesh.indices[offset], ib = mesh.indices[offset + 1], ic = mesh.indices[offset + 2];
                    if (ia >= mesh.vertexCount || ib >= mesh.vertexCount || ic >= mesh.vertexCount) {
                        renderer3DLastError = 6; return false;
                    }
                    const a = ia * 16, b = ib * 16, c = ic * 16;
                    const ux = mesh.vertices[b] - mesh.vertices[a], uy = mesh.vertices[b + 1] - mesh.vertices[a + 1], uz = mesh.vertices[b + 2] - mesh.vertices[a + 2];
                    const vx = mesh.vertices[c] - mesh.vertices[a], vy = mesh.vertices[c + 1] - mesh.vertices[a + 1], vz = mesh.vertices[c + 2] - mesh.vertices[a + 2];
                    if (!mesh.explicitNormals) {
                        const nx = uy * vz - uz * vy, ny = uz * vx - ux * vz, nz = ux * vy - uy * vx;
                        for (const vertex of [a, b, c]) {
                            mesh.vertices[vertex + 3] += nx; mesh.vertices[vertex + 4] += ny; mesh.vertices[vertex + 5] += nz;
                        }
                    }
                }
                for (let index = 0; index < mesh.vertexCount; index += 1) {
                    const offset = index * 16 + 3;
                    if (![mesh.vertices[offset],mesh.vertices[offset+1],mesh.vertices[offset+2]].every(Number.isFinite)) {
                        renderer3DLastError = 6; return false;
                    }
                    const length = Math.hypot(mesh.vertices[offset], mesh.vertices[offset + 1], mesh.vertices[offset + 2]);
                    if (length > .000001) {
                        mesh.vertices[offset] /= length; mesh.vertices[offset + 1] /= length; mesh.vertices[offset + 2] /= length;
                    } else mesh.vertices[offset + 1] = 1;
                }
                renderer3DDeleteGpu(mesh); mesh.committed = true; return true;
            }

            function renderer3DChecksum(bytes, start) {
                let result = 2166136261;
                for (let index = start; index < bytes.length; index += 1)
                    result = Math.imul((result ^ bytes[index]) >>> 0, 16777619) >>> 0;
                return result;
            }

            async function renderer3DLoadModel(path) {
                if (renderer3DModels.size >= 64) { renderer3DLastError = 25; return 0; }
                let buffer;
                try {
                    const response = await fetch(logicalPath(path), { cache: "no-store" });
                    if (!response.ok) { renderer3DLastError = 26; return 0; }
                    buffer = await response.arrayBuffer();
                } catch (_) { renderer3DLastError = 26; return 0; }
                if (!(buffer instanceof ArrayBuffer) || buffer.byteLength < 32 || buffer.byteLength > 16*1024*1024) {
                    renderer3DLastError = 24; return 0;
                }
                const bytes = new Uint8Array(buffer), view = new DataView(buffer);
                const u16 = offset => view.getUint16(offset, true), u32 = offset => view.getUint32(offset, true);
                const partCount=u32(8),vertexCount=u32(12),indexCount=u32(16),materialCount=u32(20);
                const partBytes=partCount*24,vertexBytes=vertexCount*32,expected=32+partBytes+vertexBytes+indexCount*4;
                if(bytes[0]!==83||bytes[1]!==77||bytes[2]!==51||bytes[3]!==68||u16(4)!==1||u16(6)!==32||
                    partCount<1||partCount>16||vertexCount<1||vertexCount>16*65535||
                    indexCount<1||indexCount>16*196608||materialCount<1||materialCount>64||
                    u32(24)!==buffer.byteLength||expected!==buffer.byteLength||u32(28)!==renderer3DChecksum(bytes,32)) {
                    renderer3DLastError=24;return 0;
                }
                const parts=[];
                for(let partIndex=0;partIndex<partCount;partIndex+=1){
                    const offset=32+partIndex*24,firstVertex=u32(offset),partVertices=u32(offset+4),
                        firstIndex=u32(offset+8),partIndices=u32(offset+12),material=u32(offset+16);
                    if(partVertices<1||partVertices>65535||partIndices<1||partIndices>196608||partIndices%3!==0||
                        firstVertex>vertexCount||partVertices>vertexCount-firstVertex||firstIndex>indexCount||
                        partIndices>indexCount-firstIndex||material>=materialCount||u32(offset+20)!==0){
                        renderer3DLastError=24;return 0;
                    }
                    for(let index=0;index<partVertices*8;index+=1)
                        if(!Number.isFinite(view.getFloat32(32+partBytes+(firstVertex*8+index)*4,true))){
                            renderer3DLastError=24;return 0;
                        }
                    for(let index=0;index<partIndices;index+=1)
                        if(u32(32+partBytes+vertexBytes+(firstIndex+index)*4)>=partVertices){
                            renderer3DLastError=24;return 0;
                        }
                    parts.push({firstVertex,vertexCount:partVertices,firstIndex,indexCount:partIndices,material});
                }
                if(renderer3DMeshes.size+partCount>128){renderer3DLastError=3;return 0;}
                const meshHandles=[];
                for(const part of parts){
                    const handle=renderer3DCreateMesh(part.vertexCount,part.indexCount),mesh=renderer3DMeshes.get(handle);
                    if(!mesh){for(const value of meshHandles){const old=renderer3DMeshes.get(value);if(old)renderer3DDeleteGpu(old);renderer3DMeshes.delete(value);}return 0;}
                    for(let vertex=0;vertex<part.vertexCount;vertex+=1){
                        const source=32+partBytes+(part.firstVertex+vertex)*32,target=vertex*16;
                        for(let field=0;field<8;field+=1)mesh.vertices[target+field]=view.getFloat32(source+field*4,true);
                    }
                    mesh.explicitNormals=true;
                    for(let index=0;index<part.indexCount;index+=1)
                        mesh.indices[index]=u32(32+partBytes+vertexBytes+(part.firstIndex+index)*4);
                    if(!renderer3DCommit(mesh)){renderer3DDeleteGpu(mesh);renderer3DMeshes.delete(handle);for(const value of meshHandles){const old=renderer3DMeshes.get(value);if(old)renderer3DDeleteGpu(old);renderer3DMeshes.delete(value);}return 0;}
                    meshHandles.push(handle);
                }
                const handle=renderer3DHandle();
                renderer3DModels.set(handle,{parts:meshHandles,materials:parts.map(part=>part.material),materialCount});
                return handle;
            }

            function renderer3DDeleteModel(handle) {
                const model=renderer3DModels.get(handle);if(!model)return false;
                for(const mesh of model.parts)if(renderer3DMeshReferenceCount(mesh)!==0)return false;
                for(const handle of model.parts){const mesh=renderer3DMeshes.get(handle);if(mesh)renderer3DDeleteGpu(mesh);renderer3DMeshes.delete(handle);}
                renderer3DModels.delete(handle);return true;
            }

            function renderer3DCreateSkeleton(boneCount){boneCount=safe(boneCount);if(boneCount<1||boneCount>32||renderer3DSkeletons.size>=64){renderer3DLastError=28;return 0;}const handle=renderer3DHandle();renderer3DSkeletons.set(handle,{boneCount,parents:new Array(boneCount).fill(-2),bind:Array.from({length:boneCount},()=>[0,0,0]),inverse:Array.from({length:boneCount},()=>[0,0,0]),committed:false});return handle;}
            function renderer3DCommitSkeleton(skeleton){for(let bone=0;bone<skeleton.boneCount;bone+=1){const parent=skeleton.parents[bone];if(parent< -1||parent>=bone){renderer3DLastError=30;return false;}const global=[...skeleton.bind[bone]];if(parent>=0)for(let axis=0;axis<3;axis+=1)global[axis]-=skeleton.inverse[parent][axis];skeleton.inverse[bone]=global.map(value=>-value);}skeleton.committed=true;return true;}
            function renderer3DCreateClip(skeletonHandle,duration){duration=safe(duration);const skeleton=renderer3DSkeletons.get(skeletonHandle);if(!skeleton||!skeleton.committed||duration<1||duration>600000||renderer3DClips.size>=128){renderer3DLastError=31;return 0;}const handle=renderer3DHandle();renderer3DClips.set(handle,{skeleton:skeletonHandle,duration,translation:new Array(skeleton.boneCount).fill(null),rotation:new Array(skeleton.boneCount).fill(null),scale:new Array(skeleton.boneCount).fill(null),events:[]});return handle;}
            function renderer3DCreateAnimator(skeletonHandle){const skeleton=renderer3DSkeletons.get(skeletonHandle);if(!skeleton||!skeleton.committed||renderer3DAnimators.size>=128){renderer3DLastError=33;return 0;}const handle=renderer3DHandle();renderer3DAnimators.set(handle,{skeleton:skeletonHandle,clip:0,loop:false,complete:false,time:0,previous:0,speed:100,pending:0,bones:Array.from({length:32},()=>renderer3DIdentity()),palette:new Float32Array(32*16)});renderer3DUpdatePose(renderer3DAnimators.get(handle));return handle;}
            function renderer3DPose(tx,ty,tz,qx,qy,qz,qw,sx,sy,sz){let length=Math.hypot(qx,qy,qz,qw);if(length<.000001){qx=qy=qz=0;qw=1;}else{qx/=length;qy/=length;qz/=length;qw/=length;}const result=renderer3DIdentity();result[0]=(1-2*qy*qy-2*qz*qz)*sx;result[1]=(2*qx*qy+2*qw*qz)*sx;result[2]=(2*qx*qz-2*qw*qy)*sx;result[4]=(2*qx*qy-2*qw*qz)*sy;result[5]=(1-2*qx*qx-2*qz*qz)*sy;result[6]=(2*qy*qz+2*qw*qx)*sy;result[8]=(2*qx*qz+2*qw*qy)*sz;result[9]=(2*qy*qz-2*qw*qx)*sz;result[10]=(1-2*qx*qx-2*qy*qy)*sz;result[12]=tx;result[13]=ty;result[14]=tz;return result;}
            function renderer3DUpdatePose(animator){const skeleton=renderer3DSkeletons.get(animator.skeleton),clip=renderer3DClips.get(animator.clip),amount=clip?animator.time/clip.duration:0,global=[];if(!skeleton)return;const lerp=(a,b)=>a+(b-a)*amount;for(let bone=0;bone<skeleton.boneCount;bone+=1){let [tx,ty,tz]=skeleton.bind[bone],qx=0,qy=0,qz=0,qw=1,sx=1,sy=1,sz=1;const translation=clip&&clip.translation[bone],rotation=clip&&clip.rotation[bone],scale=clip&&clip.scale[bone];if(translation){tx=lerp(translation[0][0],translation[1][0]);ty=lerp(translation[0][1],translation[1][1]);tz=lerp(translation[0][2],translation[1][2]);}if(rotation){const dot=rotation[0].reduce((sum,value,index)=>sum+value*rotation[1][index],0),direction=dot<0?-1:1;qx=lerp(rotation[0][0],rotation[1][0]*direction);qy=lerp(rotation[0][1],rotation[1][1]*direction);qz=lerp(rotation[0][2],rotation[1][2]*direction);qw=lerp(rotation[0][3],rotation[1][3]*direction);}if(scale){sx=lerp(scale[0][0],scale[1][0]);sy=lerp(scale[0][1],scale[1][1]);sz=lerp(scale[0][2],scale[1][2]);}const local=renderer3DPose(tx,ty,tz,qx,qy,qz,qw,sx,sy,sz),parent=skeleton.parents[bone];global[bone]=parent<0?local:renderer3DMultiply(global[parent],local);const inverse=renderer3DIdentity();inverse[12]=skeleton.inverse[bone][0];inverse[13]=skeleton.inverse[bone][1];inverse[14]=skeleton.inverse[bone][2];animator.bones[bone]=renderer3DMultiply(global[bone],inverse);animator.palette.set(animator.bones[bone],bone*16);}for(let bone=skeleton.boneCount;bone<32;bone+=1){animator.bones[bone]=renderer3DIdentity();animator.palette.set(animator.bones[bone],bone*16);}}
            function renderer3DUpdateAnimator(animator,delta){delta=safe(delta);if(!animator||delta<0||delta>600000){renderer3DLastError=35;return false;}const clip=renderer3DClips.get(animator.clip);if(!clip){renderer3DUpdatePose(animator);return true;}animator.previous=animator.time;const advance=Math.trunc(delta*animator.speed/100),total=animator.time+advance,wrapped=animator.loop&&total>=clip.duration;if(animator.loop){animator.time=total%clip.duration;animator.complete=false;}else{animator.time=Math.min(total,clip.duration);animator.complete=total>=clip.duration;}for(const event of clip.events)if((!wrapped&&event.time>animator.previous&&event.time<=animator.time)||(wrapped&&(event.time>animator.previous||event.time<=animator.time))||(animator.loop&&advance>=clip.duration))animator.pending=event.id;renderer3DUpdatePose(animator);return true;}
            function renderer3DSkeletonReferences(handle){let count=0;for(const clip of renderer3DClips.values())if(clip.skeleton===handle)count+=1;for(const animator of renderer3DAnimators.values())if(animator.skeleton===handle)count+=1;return count;}
            function renderer3DClipReferences(handle){let count=0;for(const animator of renderer3DAnimators.values())if(animator.clip===handle)count+=1;return count;}
            function renderer3DAnimatorReferences(handle){let count=0;for(const object of renderer3DObjects.values())if(object.animator===handle)count+=1;return count;}

            function renderer3DPrimitive(kind, first, second, segments, rings) {
                kind = safe(kind); first = safe(first); second = safe(second); segments = safe(segments); rings = safe(rings);
                let handle = 0, mesh = null, triangle = 0;
                const vertex = (index, x, y, z) => renderer3DSetVertex(mesh, index, Math.round(x), Math.round(y), Math.round(z));
                const uv = (index, u, v) => renderer3DSetUv(mesh, index, Math.round(u * 1000), Math.round(v * 1000));
                const face = (a, b, c) => renderer3DSetTriangle(mesh, triangle++, a, b, c);
                if (first <= 0 || (kind !== 1 && second <= 0)) { renderer3DLastError = 7; return 0; }
                if (kind === 1) {
                    handle = renderer3DCreateMesh(24, 36); mesh = renderer3DRequireMesh(handle);
                    const p = [-1,-1,-1,1,-1,-1,1,1,-1,-1,1,-1,-1,-1,1,-1,1,1,1,1,1,1,-1,1,
                        -1,-1,-1,-1,1,-1,-1,1,1,-1,-1,1,1,-1,-1,1,-1,1,1,1,1,1,1,-1,
                        -1,1,-1,1,1,-1,1,1,1,-1,1,1,-1,-1,-1,-1,-1,1,1,-1,1,1,-1,-1];
                    for (let index = 0; index < 24; index += 1) vertex(index, p[index*3]*first/2, p[index*3+1]*first/2, p[index*3+2]*first/2);
                    for (let side = 0; side < 6; side += 1) { const offset=side*4;uv(offset,0,1);uv(offset+1,0,0);uv(offset+2,1,0);uv(offset+3,1,1);face(offset,offset+1,offset+2);face(offset,offset+2,offset+3); }
                } else if (kind === 2) {
                    handle = renderer3DCreateMesh(4, 6); mesh = renderer3DRequireMesh(handle);
                    vertex(0,-first/2,0,-second/2);vertex(1,-first/2,0,second/2);vertex(2,first/2,0,second/2);vertex(3,first/2,0,-second/2);uv(0,0,0);uv(1,0,1);uv(2,1,1);uv(3,1,0);face(0,1,2);face(0,2,3);
                } else if (kind === 3) {
                    handle = renderer3DCreateMesh(5, 18); mesh = renderer3DRequireMesh(handle);
                    vertex(0,-first/2,-second/2,-first/2);vertex(1,first/2,-second/2,-first/2);vertex(2,first/2,-second/2,first/2);vertex(3,-first/2,-second/2,first/2);vertex(4,0,second/2,0);
                    face(0,2,1);face(0,3,2);face(0,1,4);face(1,2,4);face(2,3,4);face(3,0,4);
                } else if (kind === 4) {
                    segments=Math.max(6,Math.min(48,segments));rings=Math.max(3,Math.min(32,rings));
                    handle=renderer3DCreateMesh((rings+1)*(segments+1),rings*segments*6);mesh=renderer3DRequireMesh(handle);
                    for(let ring=0;ring<=rings;ring+=1){const lat=-Math.PI/2+Math.PI*ring/rings,rr=Math.cos(lat)*first,y=Math.sin(lat)*first;for(let segment=0;segment<=segments;segment+=1){const index=ring*(segments+1)+segment,lon=2*Math.PI*segment/segments;vertex(index,Math.cos(lon)*rr,y,Math.sin(lon)*rr);uv(index,segment/segments,1-ring/rings);}}
                    for(let ring=0;ring<rings;ring+=1)for(let segment=0;segment<segments;segment+=1){const a=ring*(segments+1)+segment,b=a+1,c=a+segments+1,d=c+1;face(a,c,b);face(b,c,d);}
                } else if (kind === 5) {
                    segments=Math.max(6,Math.min(64,segments));handle=renderer3DCreateMesh(segments*2+2,segments*12);mesh=renderer3DRequireMesh(handle);
                    for(let segment=0;segment<segments;segment+=1){const angle=2*Math.PI*segment/segments,x=Math.cos(angle)*first,z=Math.sin(angle)*first;vertex(segment,x,-second/2,z);vertex(segment+segments,x,second/2,z);uv(segment,segment/segments,1);uv(segment+segments,segment/segments,0);}vertex(segments*2,0,-second/2,0);vertex(segments*2+1,0,second/2,0);
                    for(let segment=0;segment<segments;segment+=1){const next=(segment+1)%segments,top=segment+segments,nextTop=next+segments;face(segment,top,next);face(next,top,nextTop);face(segments*2,next,segment);face(segments*2+1,top,nextTop);}
                } else if (kind === 6) {
                    segments=Math.max(6,Math.min(48,segments));rings=Math.max(4,Math.min(24,rings));handle=renderer3DCreateMesh((segments+1)*(rings+1),segments*rings*6);mesh=renderer3DRequireMesh(handle);
                    for(let major=0;major<=segments;major+=1){const a=2*Math.PI*major/segments;for(let minor=0;minor<=rings;minor+=1){const index=major*(rings+1)+minor,b=2*Math.PI*minor/rings,rr=first+second*Math.cos(b);vertex(index,Math.cos(a)*rr,second*Math.sin(b),Math.sin(a)*rr);uv(index,major/segments,minor/rings);}}
                    for(let major=0;major<segments;major+=1)for(let minor=0;minor<rings;minor+=1){const a=major*(rings+1)+minor,b=a+1,c=a+rings+1,d=c+1;face(a,c,b);face(b,c,d);}
                } else { renderer3DLastError = 8; return 0; }
                return mesh && renderer3DCommit(mesh) ? handle : 0;
            }

            function renderer3DIdentity() { return [1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1]; }
            function renderer3DMultiply(a,b){const r=new Array(16).fill(0);for(let col=0;col<4;col+=1)for(let row=0;row<4;row+=1)for(let k=0;k<4;k+=1)r[col*4+row]+=a[k*4+row]*b[col*4+k];return r;}
            function renderer3DNormalize(v){const l=Math.hypot(v[0],v[1],v[2]);return l>.000001?[v[0]/l,v[1]/l,v[2]/l]:[0,1,0];}
            function renderer3DCross(a,b){return[a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]];}
            function renderer3DDot(a,b){return a[0]*b[0]+a[1]*b[1]+a[2]*b[2];}
            function renderer3DModel(object){const [sx,sy,sz]=object.scale,[rx,ry,rz]=object.rotation.map(value=>value*Math.PI/180);let s=renderer3DIdentity(),x=renderer3DIdentity(),y=renderer3DIdentity(),z=renderer3DIdentity(),t=renderer3DIdentity();s[0]=sx;s[5]=sy;s[10]=sz;x[5]=Math.cos(rx);x[6]=-Math.sin(rx);x[9]=Math.sin(rx);x[10]=Math.cos(rx);y[0]=Math.cos(ry);y[2]=Math.sin(ry);y[8]=-Math.sin(ry);y[10]=Math.cos(ry);z[0]=Math.cos(rz);z[1]=-Math.sin(rz);z[4]=Math.sin(rz);z[5]=Math.cos(rz);t[12]=object.position[0];t[13]=object.position[1];t[14]=object.position[2];return renderer3DMultiply(t,renderer3DMultiply(z,renderer3DMultiply(y,renderer3DMultiply(x,s))));}
            function renderer3DView(){const eye=renderer3DCamera.position,target=renderer3DCamera.target,z=renderer3DNormalize([target[0]-eye[0],target[1]-eye[1],target[2]-eye[2]]),x=renderer3DNormalize(renderer3DCross([0,1,0],z)),y=renderer3DCross(z,x);return[x[0],y[0],z[0],0,x[1],y[1],z[1],0,x[2],y[2],z[2],0,-renderer3DDot(x,eye),-renderer3DDot(y,eye),-renderer3DDot(z,eye),1];}
            function renderer3DProjection(aspect){const f=1/Math.tan(renderer3DCamera.fov*Math.PI/360),near=renderer3DCamera.near,far=renderer3DCamera.far;return[f/aspect,0,0,0,0,f,0,0,0,0,(far+near)/(far-near),1,0,0,-2*far*near/(far-near),0];}

            function renderer3DUpload(mesh) {
                const gl=renderer3DGl;if(mesh.vertexBuffer&&mesh.indexBuffer)return true;if(!gl||!mesh.committed)return false;
                mesh.vertexBuffer=gl.createBuffer();gl.bindBuffer(gl.ARRAY_BUFFER,mesh.vertexBuffer);gl.bufferData(gl.ARRAY_BUFFER,mesh.vertices,gl.STATIC_DRAW);
                mesh.indexBuffer=gl.createBuffer();gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,mesh.indexBuffer);gl.bufferData(gl.ELEMENT_ARRAY_BUFFER,mesh.indices,gl.STATIC_DRAW);return true;
            }

            function renderer3DUploadTexture(texture) {
                const gl=renderer3DGl;if(texture.gpu)return true;if(!gl||!imageLoadedRaw(texture.image))return false;
                texture.gpu=gl.createTexture();gl.activeTexture(gl.TEXTURE0);gl.bindTexture(gl.TEXTURE_2D,texture.gpu);
                gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL,true);
                gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA,gl.RGBA,gl.UNSIGNED_BYTE,texture.image.entry.resource);
                const filter=texture.filter===0?gl.NEAREST:gl.LINEAR,address=texture.wrap===0?gl.CLAMP_TO_EDGE:gl.REPEAT;
                gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,filter);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,filter);
                gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,address);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,address);
                return true;
            }

            function renderer3DBegin(red,green,blue){if(renderer3DFrameActive)return 1;if(!renderer3DInitialize())return 0;const gl=renderer3DGl;if(renderer3DCanvas.width!==backingWidth||renderer3DCanvas.height!==backingHeight){renderer3DCanvas.width=backingWidth;renderer3DCanvas.height=backingHeight;}gl.viewport(0,0,backingWidth,backingHeight);gl.enable(gl.DEPTH_TEST);gl.depthFunc(gl.LESS);gl.depthMask(true);gl.disable(gl.BLEND);gl.disable(gl.CULL_FACE);gl.clearColor((safe(red)&255)/255,(safe(green)&255)/255,(safe(blue)&255)/255,1);gl.clearDepth(1);gl.clear(gl.COLOR_BUFFER_BIT|gl.DEPTH_BUFFER_BIT);gl.useProgram(renderer3DProgram.handle);renderer3DFrameActive=true;return 1;}
            function renderer3DDraw(handle){const object=renderer3DRequireObject(handle);if(!renderer3DFrameActive||!object){renderer3DLastError=14;return 0;}if(!object.visible)return 1;const mesh=renderer3DRequireMesh(object.mesh);if(!mesh||!renderer3DUpload(mesh))return 0;const material=object.material?renderer3DRequireMaterial(object.material):null,texture=material&&material.texture?renderer3DRequireTexture(material.texture):null,animator=object.animator?renderer3DAnimators.get(object.animator):null,skeleton=animator?renderer3DSkeletons.get(animator.skeleton):null;if(texture&&!renderer3DUploadTexture(texture))return 0;if(object.animator&&(!animator||!skeleton||mesh.maxJoint>=skeleton.boneCount)){renderer3DLastError=36;return 0;}const gl=renderer3DGl,model=renderer3DModel(object),mvp=renderer3DMultiply(renderer3DProjection(backingWidth/backingHeight),renderer3DMultiply(renderer3DView(),model)),tint=object.color.map((value,index)=>value*(material?material.color[index]:1)),alphaMode=material?material.alphaMode:(tint[3]<.999?2:0);if(alphaMode===2||alphaMode===3){gl.enable(gl.BLEND);gl.blendFunc(gl.SRC_ALPHA,alphaMode===3?gl.ONE:gl.ONE_MINUS_SRC_ALPHA);gl.depthMask(false);}else{gl.disable(gl.BLEND);gl.depthMask(true);}gl.bindBuffer(gl.ARRAY_BUFFER,mesh.vertexBuffer);gl.enableVertexAttribArray(0);gl.vertexAttribPointer(0,3,gl.FLOAT,false,64,0);gl.enableVertexAttribArray(1);gl.vertexAttribPointer(1,3,gl.FLOAT,false,64,12);gl.enableVertexAttribArray(2);gl.vertexAttribPointer(2,2,gl.FLOAT,false,64,24);gl.enableVertexAttribArray(3);gl.vertexAttribPointer(3,4,gl.FLOAT,false,64,32);gl.enableVertexAttribArray(4);gl.vertexAttribPointer(4,4,gl.FLOAT,false,64,48);gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,mesh.indexBuffer);gl.activeTexture(gl.TEXTURE0);gl.bindTexture(gl.TEXTURE_2D,texture?texture.gpu:null);gl.uniform1i(renderer3DProgram.baseTexture,0);gl.uniformMatrix4fv(renderer3DProgram.model,false,new Float32Array(model));gl.uniformMatrix4fv(renderer3DProgram.mvp,false,new Float32Array(mvp));gl.uniformMatrix4fv(renderer3DProgram.bones,false,animator?animator.palette:renderer3DStaticBones);gl.uniform1f(renderer3DProgram.skinning,animator?1:0);gl.uniform4fv(renderer3DProgram.tint,new Float32Array(tint));gl.uniform4fv(renderer3DProgram.material,new Float32Array([texture?1:0,material&&material.unlit?1:0,material?material.emissive:0,material&&material.alphaMode===1?material.cutoff:-1]));gl.drawElements(gl.TRIANGLES,mesh.indexCount,gl.UNSIGNED_INT,0);return 1;}
            function renderer3DEnd(){if(!renderer3DFrameActive)return 1;const gl=renderer3DGl;gl.depthMask(true);gl.bindTexture(gl.TEXTURE_2D,null);renderer3DFrameActive=false;back.drawImage(renderer3DCanvas,0,0,logicalWidth,logicalHeight);return 1;}
            function renderer3DReset(){renderer3DFrameActive=false;renderer3DObjects.clear();for(const model of [...renderer3DModels.keys()])renderer3DDeleteModel(model);renderer3DAnimators.clear();renderer3DClips.clear();renderer3DSkeletons.clear();for(const mesh of renderer3DMeshes.values())renderer3DDeleteGpu(mesh);for(const texture of renderer3DTextures.values()){renderer3DDeleteTextureGpu(texture);imageRelease(texture.image);}renderer3DMeshes.clear();renderer3DModels.clear();renderer3DMaterials.clear();renderer3DTextures.clear();renderer3DLastError=0;return 1;}

            function renderer3D(command,a,b,c,d,e,f,g,h,i,j) {
                [command,a,b,c,d,e,f,g,h,i,j]=[command,a,b,c,d,e,f,g,h,i,j].map(safe);
                let mesh,object,texture,material,model,skeleton,clip,animator;
                switch(command){
                    case 1:return renderer3DInitialize()?1:0;
                    case 2:return renderer3DReset();
                    case 3:return renderer3DCreateMesh(a,b);
                    case 4:mesh=renderer3DRequireMesh(a);return mesh&&renderer3DSetVertex(mesh,b,c,d,e)?1:0;
                    case 5:mesh=renderer3DRequireMesh(a);return mesh&&renderer3DSetTriangle(mesh,b,c,d,e)?1:0;
                    case 6:mesh=renderer3DRequireMesh(a);return mesh&&renderer3DCommit(mesh)?1:0;
                    case 7:return renderer3DPrimitive(a,b,c,d,e);
                    case 8:if(!renderer3DRequireMesh(a)||renderer3DObjects.size>=512){renderer3DLastError=9;return 0;}const handle=renderer3DHandle();renderer3DObjects.set(handle,{mesh:a,material:0,animator:0,position:[0,0,0],rotation:[0,0,0],scale:[1,1,1],color:[1,1,1,1],visible:true});return handle;
                    case 9:if(renderer3DObjects.delete(a))return 1;if(renderer3DModels.has(a)){if(!renderer3DDeleteModel(a)){renderer3DLastError=27;return 0;}return 1;}if(renderer3DAnimators.has(a)){if(renderer3DAnimatorReferences(a)!==0){renderer3DLastError=37;return 0;}renderer3DAnimators.delete(a);return 1;}if(renderer3DClips.has(a)){if(renderer3DClipReferences(a)!==0){renderer3DLastError=37;return 0;}renderer3DClips.delete(a);return 1;}if(renderer3DSkeletons.has(a)){if(renderer3DSkeletonReferences(a)!==0){renderer3DLastError=37;return 0;}renderer3DSkeletons.delete(a);return 1;}mesh=renderer3DMeshes.get(a);if(mesh){if(renderer3DMeshReferenceCount(a)!==0){renderer3DLastError=16;return 0;}renderer3DDeleteGpu(mesh);renderer3DMeshes.delete(a);return 1;}material=renderer3DMaterials.get(a);if(material){if(renderer3DMaterialReferenceCount(a)!==0){renderer3DLastError=22;return 0;}renderer3DMaterials.delete(a);return 1;}texture=renderer3DTextures.get(a);if(texture){if(renderer3DTextureReferenceCount(a)!==0){renderer3DLastError=23;return 0;}renderer3DDeleteTextureGpu(texture);imageRelease(texture.image);renderer3DTextures.delete(a);return 1;}renderer3DLastError=5;return 0;
                    case 10:renderer3DCamera.position=[a,b,c];renderer3DCamera.target=[d,e,f];renderer3DCamera.fov=g;renderer3DCamera.near=h;renderer3DCamera.far=i;if(g<10||g>160||h<=0||i<=h){renderer3DLastError=15;return 0;}return 1;
                    case 11:case 12:case 13:object=renderer3DRequireObject(a);if(!object)return 0;if(command===11)object.position=[b,c,d];else if(command===12)object.rotation=[b,c,d];else object.scale=[b/100,c/100,d/100];return 1;
                    case 14:object=renderer3DRequireObject(a);if(!object)return 0;object.color=[(b&255)/255,(c&255)/255,(d&255)/255,Math.max(0,Math.min(100,e))/100];return 1;
                    case 15:object=renderer3DRequireObject(a);if(!object)return 0;object.visible=b!==0;return 1;
                    case 16:return renderer3DBegin(a,b,c);
                    case 17:return renderer3DDraw(a);
                    case 18:return renderer3DEnd();
                    case 19:mesh=renderer3DRequireMesh(a);return mesh?mesh.vertexCount:0;
                    case 20:mesh=renderer3DRequireMesh(a);return mesh?mesh.indexCount:0;
                    case 21:return renderer3DLastError;
                    case 22:return renderer3DMeshes.size;
                    case 23:return renderer3DObjects.size;
                    case 24:return 128;
                    case 25:return 512;
                    case 26:return renderer3DMeshes.has(a)?1:0;
                    case 27:return renderer3DObjects.has(a)?1:0;
                    case 28:return renderer3DMeshReferenceCount(a);
                    case 29:return renderer3DCreateMaterial(a,b,c,d,e,f,g,h,i);
                    case 30:object=renderer3DRequireObject(a);if(!object||(b!==0&&!renderer3DMaterials.has(b))){renderer3DLastError=5;return 0;}object.material=b;return 1;
                    case 31:mesh=renderer3DRequireMesh(a);return mesh&&renderer3DSetUv(mesh,b,c,d)?1:0;
                    case 32:return renderer3DTextures.size;
                    case 33:return renderer3DMaterials.size;
                    case 34:return 128;
                    case 35:return 128;
                    case 36:return renderer3DTextures.has(a)?1:0;
                    case 37:return renderer3DMaterials.has(a)?1:0;
                    case 38:texture=renderer3DTextures.get(a);return texture?texture.image.entry.width:0;
                    case 39:texture=renderer3DTextures.get(a);return texture?texture.image.entry.height:0;
                    case 40:return renderer3DTextureReferenceCount(a);
                    case 41:return renderer3DMaterialReferenceCount(a);
                    case 42:material=renderer3DRequireMaterial(a);return renderer3DSetMaterial(material,b,c,d,e,f,g,h,i)?1:0;
                    case 43:mesh=renderer3DRequireMesh(a);return mesh&&renderer3DSetNormal(mesh,b,c,d,e)?1:0;
                    case 44:return renderer3DModels.size;
                    case 45:return 64;
                    case 46:return renderer3DModels.has(a)?1:0;
                    case 47:model=renderer3DModels.get(a);return model?model.parts.length:0;
                    case 48:model=renderer3DModels.get(a);return model?model.materialCount:0;
                    case 49:model=renderer3DModels.get(a);if(!model||b<0||b>=model.parts.length||renderer3DObjects.size>=512){renderer3DLastError=9;return 0;}const partHandle=renderer3DHandle();renderer3DObjects.set(partHandle,{mesh:model.parts[b],material:0,animator:0,position:[0,0,0],rotation:[0,0,0],scale:[1,1,1],color:[1,1,1,1],visible:true});return partHandle;
                    case 50:model=renderer3DModels.get(a);if(!model||b<0||b>=model.parts.length){renderer3DLastError=5;return -1;}return model.materials[b];
                    case 51:mesh=renderer3DRequireMesh(a);return mesh&&renderer3DSetSkin(mesh,b,c,d,e,f,g,h,i,j)?1:0;
                    case 52:return renderer3DCreateSkeleton(a);
                    case 53:skeleton=renderer3DSkeletons.get(a);if(!skeleton||b<0||b>=skeleton.boneCount||c< -1||c>=b){renderer3DLastError=30;return 0;}skeleton.parents[b]=c;skeleton.bind[b]=[d,e,f];skeleton.committed=false;return 1;
                    case 54:skeleton=renderer3DSkeletons.get(a);return skeleton&&renderer3DCommitSkeleton(skeleton)?1:0;
                    case 55:return renderer3DCreateClip(a,b);
                    case 56:clip=renderer3DClips.get(a);skeleton=clip&&renderer3DSkeletons.get(clip.skeleton);if(!clip||!skeleton||b<0||b>=skeleton.boneCount){renderer3DLastError=31;return 0;}clip.translation[b]=[[c,d,e],[f,g,h]];return 1;
                    case 57:clip=renderer3DClips.get(a);skeleton=clip&&renderer3DSkeletons.get(clip.skeleton);if(!clip||!skeleton||b<0||b>=skeleton.boneCount){renderer3DLastError=31;return 0;}clip.rotation[b]=[[c/1000,d/1000,e/1000,f/1000],[g/1000,h/1000,i/1000,j/1000]];return 1;
                    case 58:clip=renderer3DClips.get(a);skeleton=clip&&renderer3DSkeletons.get(clip.skeleton);if(!clip||!skeleton||b<0||b>=skeleton.boneCount||[c,d,e,f,g,h].some(value=>value<=0)){renderer3DLastError=31;return 0;}clip.scale[b]=[[c/100,d/100,e/100],[f/100,g/100,h/100]];return 1;
                    case 59:clip=renderer3DClips.get(a);if(!clip||b<=0||b>clip.duration||c<=0||clip.events.length>=16||(clip.events.length&&b<clip.events[clip.events.length-1].time)){renderer3DLastError=31;return 0;}clip.events.push({time:b,id:c});return 1;
                    case 60:return renderer3DCreateAnimator(a);
                    case 61:animator=renderer3DAnimators.get(a);clip=renderer3DClips.get(b);if(!animator||!clip||clip.skeleton!==animator.skeleton||d<=0||d>1000){renderer3DLastError=35;return 0;}animator.clip=b;animator.loop=c!==0;animator.complete=false;animator.time=0;animator.previous=0;animator.speed=d;animator.pending=0;renderer3DUpdatePose(animator);return 1;
                    case 62:animator=renderer3DAnimators.get(a);return renderer3DUpdateAnimator(animator,b)?1:0;
                    case 63:animator=renderer3DAnimators.get(a);return animator&&animator.complete?1:0;
                    case 64:animator=renderer3DAnimators.get(a);return animator?animator.time:0;
                    case 65:animator=renderer3DAnimators.get(a);if(!animator)return 0;const eventValue=animator.pending;animator.pending=0;return eventValue;
                    case 66:object=renderer3DObjects.get(a);animator=b===0?null:renderer3DAnimators.get(b);mesh=object&&renderer3DMeshes.get(object.mesh);skeleton=animator&&renderer3DSkeletons.get(animator.skeleton);if(!object||!mesh||(b!==0&&(!animator||!skeleton||mesh.maxJoint>=skeleton.boneCount))){renderer3DLastError=36;return 0;}object.animator=b;return 1;
                    case 67:return renderer3DSkeletons.size;
                    case 68:return renderer3DClips.size;
                    case 69:return renderer3DAnimators.size;
                    case 70:return 32;
                    case 71:return renderer3DSkeletons.has(a)?1:0;
                    case 72:return renderer3DClips.has(a)?1:0;
                    case 73:return renderer3DAnimators.has(a)?1:0;
                    case 74:animator=renderer3DAnimators.get(a);if(!animator)return 0;animator.clip=0;animator.time=0;animator.previous=0;animator.complete=false;animator.pending=0;renderer3DUpdatePose(animator);return 1;
                    case 75:return 64;
                    case 76:return 128;
                    case 77:return 128;
                    default:renderer3DLastError=1;return 0;
                }
            }

            function renderer3DImage(command,image,a,b,c,d,e,f,g,h) {
                [command,a,b,c,d,e,f,g,h]=[command,a,b,c,d,e,f,g,h].map(safe);
                if(command===1)return renderer3DCreateTexture(image,a,b);
                imageRelease(image);renderer3DLastError=1;return 0;
            }

            async function renderer3DText(command,value,a,b,c,d,e,f,g,h) {
                [command,a,b,c,d,e,f,g,h]=[command,a,b,c,d,e,f,g,h].map(safe);
                if(command===1)return renderer3DLoadModel(String(value));
                renderer3DLastError=1;return 0;
            }

            function clear(fillColor) {
                back.fillStyle = color(fillColor);
                back.fillRect(0, 0, logicalWidth, logicalHeight);
            }

            function fillRectangle(x, y, width, height, fillColor) {
                back.fillStyle = color(fillColor);
                back.fillRect(safe(x), safe(y), safe(width), safe(height));
            }

            function drawRectangle(x, y, width, height, strokeColor) {
                back.strokeStyle = color(strokeColor);
                back.lineWidth = 1;
                back.strokeRect(safe(x) + 0.5, safe(y) + 0.5, safe(width) - 1, safe(height) - 1);
            }

            function roundedPath(x, y, width, height, radius) {
                x = safe(x); y = safe(y); width = safe(width); height = safe(height); radius = safe(radius);
                radius = Math.max(0, Math.min(radius, Math.abs(width) / 2, Math.abs(height) / 2));
                back.beginPath();
                back.moveTo(x + radius, y);
                back.lineTo(x + width - radius, y);
                back.quadraticCurveTo(x + width, y, x + width, y + radius);
                back.lineTo(x + width, y + height - radius);
                back.quadraticCurveTo(x + width, y + height, x + width - radius, y + height);
                back.lineTo(x + radius, y + height);
                back.quadraticCurveTo(x, y + height, x, y + height - radius);
                back.lineTo(x, y + radius);
                back.quadraticCurveTo(x, y, x + radius, y);
                back.closePath();
            }

            function fillRoundedRectangle(x, y, width, height, radius, fillColor) {
                roundedPath(x, y, width, height, radius);
                back.fillStyle = color(fillColor);
                back.fill();
            }

            function drawRoundedRectangle(x, y, width, height, radius, strokeColor) {
                roundedPath(x, y, width, height, radius);
                back.strokeStyle = color(strokeColor);
                back.lineWidth = 1;
                back.stroke();
            }

            function circlePath(x, y, radius) {
                back.beginPath();
                back.arc(safe(x), safe(y), Math.max(0, safe(radius)), 0, Math.PI * 2);
            }

            function fillCircle(x, y, radius, fillColor) {
                circlePath(x, y, radius);
                back.fillStyle = color(fillColor);
                back.fill();
            }

            function drawCircle(x, y, radius, strokeColor) {
                circlePath(x, y, radius);
                back.strokeStyle = color(strokeColor);
                back.lineWidth = 1;
                back.stroke();
            }

            function drawArc(x, y, radius, startAngle, sweepAngle, strokeColor) {
                const start = safe(startAngle) * Math.PI / 180;
                const end = safe(startAngle + sweepAngle) * Math.PI / 180;
                back.beginPath();
                back.arc(safe(x), safe(y), Math.max(0, safe(radius)), start, end, safe(sweepAngle) < 0);
                back.strokeStyle = color(strokeColor);
                back.lineWidth = 1;
                back.stroke();
            }

            function quadrilateralPath(x1, y1, x2, y2, x3, y3, x4, y4) {
                back.beginPath();
                back.moveTo(safe(x1), safe(y1));
                back.lineTo(safe(x2), safe(y2));
                back.lineTo(safe(x3), safe(y3));
                back.lineTo(safe(x4), safe(y4));
                back.closePath();
            }

            function fillQuadrilateral(x1, y1, x2, y2, x3, y3, x4, y4, fillColor) {
                quadrilateralPath(x1, y1, x2, y2, x3, y3, x4, y4);
                back.fillStyle = color(fillColor);
                back.fill();
            }

            function drawQuadrilateral(x1, y1, x2, y2, x3, y3, x4, y4, strokeColor) {
                quadrilateralPath(x1, y1, x2, y2, x3, y3, x4, y4);
                back.strokeStyle = color(strokeColor);
                back.lineWidth = 1;
                back.stroke();
            }

            function drawLine(x1, y1, x2, y2, strokeColor) {
                back.beginPath();
                back.moveTo(safe(x1) + 0.5, safe(y1) + 0.5);
                back.lineTo(safe(x2) + 0.5, safe(y2) + 0.5);
                back.strokeStyle = color(strokeColor);
                back.lineWidth = 1;
                back.stroke();
            }

            function textStyle(size, textColor, centered) {
                back.font = `${safe(size)}px "Segoe UI", Arial, sans-serif`;
                back.fillStyle = color(textColor);
                back.textAlign = centered ? "center" : "left";
                back.textBaseline = "top";
            }

            function drawText(text, x, y, size, textColor, centered) {
                textStyle(size, textColor, centered);
                back.fillText(text, safe(x), safe(y));
            }

            function drawNumber(value, x, y, size, textColor, centered) {
                textStyle(size, textColor, centered);
                back.fillText(String(safe(value)), safe(x), safe(y));
            }

            function utf8(value) {
                const bytes = [];
                for (const character of String(value)) {
                    const code = character.codePointAt(0);
                    if (code <= 0x7f) bytes.push(code);
                    else if (code <= 0x7ff) bytes.push(0xc0 | (code >>> 6), 0x80 | (code & 63));
                    else if (code <= 0xffff) bytes.push(0xe0 | (code >>> 12), 0x80 | ((code >>> 6) & 63), 0x80 | (code & 63));
                    else bytes.push(0xf0 | (code >>> 18), 0x80 | ((code >>> 12) & 63),
                        0x80 | ((code >>> 6) & 63), 0x80 | (code & 63));
                }
                return new Uint8Array(bytes);
            }

            function sha256(value) {
                const constants = [
                    0x428a2f98,0x71374491,0xb5c0fbcf,0xe9b5dba5,0x3956c25b,0x59f111f1,0x923f82a4,0xab1c5ed5,
                    0xd807aa98,0x12835b01,0x243185be,0x550c7dc3,0x72be5d74,0x80deb1fe,0x9bdc06a7,0xc19bf174,
                    0xe49b69c1,0xefbe4786,0x0fc19dc6,0x240ca1cc,0x2de92c6f,0x4a7484aa,0x5cb0a9dc,0x76f988da,
                    0x983e5152,0xa831c66d,0xb00327c8,0xbf597fc7,0xc6e00bf3,0xd5a79147,0x06ca6351,0x14292967,
                    0x27b70a85,0x2e1b2138,0x4d2c6dfc,0x53380d13,0x650a7354,0x766a0abb,0x81c2c92e,0x92722c85,
                    0xa2bfe8a1,0xa81a664b,0xc24b8b70,0xc76c51a3,0xd192e819,0xd6990624,0xf40e3585,0x106aa070,
                    0x19a4c116,0x1e376c08,0x2748774c,0x34b0bcb5,0x391c0cb3,0x4ed8aa4a,0x5b9cca4f,0x682e6ff3,
                    0x748f82ee,0x78a5636f,0x84c87814,0x8cc70208,0x90befffa,0xa4506ceb,0xbef9a3f7,0xc67178f2
                ];
                const length = value.length;
                const paddedLength = (length + 9 + 63) & ~63;
                const bytes = new Uint8Array(paddedLength);
                bytes.set(value);
                bytes[length] = 0x80;
                const view = new DataView(bytes.buffer);
                view.setUint32(paddedLength - 8, Math.floor(length / 0x20000000), false);
                view.setUint32(paddedLength - 4, (length * 8) >>> 0, false);
                const hash = [0x6a09e667,0xbb67ae85,0x3c6ef372,0xa54ff53a,0x510e527f,0x9b05688c,0x1f83d9ab,0x5be0cd19];
                const words = new Uint32Array(64);
                const rotate = (word, count) => (word >>> count) | (word << (32 - count));
                for (let offset = 0; offset < paddedLength; offset += 64) {
                    for (let index = 0; index < 16; index += 1) words[index] = view.getUint32(offset + index * 4, false);
                    for (let index = 16; index < 64; index += 1) {
                        const a = words[index - 15], b = words[index - 2];
                        const s0 = rotate(a, 7) ^ rotate(a, 18) ^ (a >>> 3);
                        const s1 = rotate(b, 17) ^ rotate(b, 19) ^ (b >>> 10);
                        words[index] = (words[index - 16] + s0 + words[index - 7] + s1) >>> 0;
                    }
                    let [a,b,c,d,e,f,g,h] = hash;
                    for (let index = 0; index < 64; index += 1) {
                        const s1 = rotate(e, 6) ^ rotate(e, 11) ^ rotate(e, 25);
                        const choice = (e & f) ^ (~e & g);
                        const first = (h + s1 + choice + constants[index] + words[index]) >>> 0;
                        const s0 = rotate(a, 2) ^ rotate(a, 13) ^ rotate(a, 22);
                        const majority = (a & b) ^ (a & c) ^ (b & c);
                        const second = (s0 + majority) >>> 0;
                        h=g; g=f; f=e; e=(d+first)>>>0; d=c; c=b; b=a; a=(first+second)>>>0;
                    }
                    hash[0]=(hash[0]+a)>>>0; hash[1]=(hash[1]+b)>>>0; hash[2]=(hash[2]+c)>>>0; hash[3]=(hash[3]+d)>>>0;
                    hash[4]=(hash[4]+e)>>>0; hash[5]=(hash[5]+f)>>>0; hash[6]=(hash[6]+g)>>>0; hash[7]=(hash[7]+h)>>>0;
                }
                const output = new Uint8Array(32);
                const outputView = new DataView(output.buffer);
                hash.forEach((word, index) => outputView.setUint32(index * 4, word, false));
                return output;
            }

            function sha256Hex(value) { return Array.from(sha256(value), byte => byte.toString(16).padStart(2, "0")).join(""); }

            function canonicalAssetPath(value) {
                const original = String(value);
                if (!original || original.includes("\0") || /^[a-z][a-z0-9+.-]*:/i.test(original) ||
                    /^[a-z]:/i.test(original) || /^[\\/]{1,2}/.test(original))
                    throw new Error(`Media path must be a project-relative asset path: ${original}`);
                const parts = [];
                for (const part of original.replaceAll("\\", "/").split("/")) {
                    if (!part || part === ".") continue;
                    if (part === "..") {
                        if (parts.length === 0) throw new Error(`Media path escapes the project asset root: ${original}`);
                        parts.pop();
                    } else parts.push(part);
                }
                const logical = parts.join("/");
                if (!logical) throw new Error("Media path must not be empty.");
                return logical;
            }

            function logicalPath(value) {
                const logical = canonicalAssetPath(value);
                if (assetPaths.size !== 0 && !assetPaths.has(logical))
                    throw new Error(`Media asset is not declared with its exact project path and case: ${logical}`);
                return logical;
            }

            async function loadImage(path) {
                const logical = logicalPath(path);
                if (!logical) throw new Error("Load Image path must not be empty.");
                let entry = imageCache.get(logical);
                if (!entry) {
                    entry = { logical, refs: 0, resource: null, width: 0, height: 0, promise: null, disposed: false };
                    imageDecodeCount += 1;
                    entry.promise = new Promise((resolve, reject) => {
                        const resource = new Image();
                        resource.onload = () => {
                            entry.resource = resource;
                            entry.width = safe(resource.naturalWidth || resource.width);
                            entry.height = safe(resource.naturalHeight || resource.height);
                            if (entry.width <= 0 || entry.height <= 0) reject(new Error(`Load Image decoded invalid dimensions: ${logical}`));
                            else resolve(entry);
                        };
                        resource.onerror = () => reject(new Error(`Load Image failed: ${logical}`));
                        resource.src = logical;
                    }).catch(error => { if (entry.refs === 0) imageCache.delete(logical); throw error; });
                    imageCache.set(logical, entry);
                } else imageCacheHitCount += 1;
                await entry.promise;
                if (mediaStopped || imageCache.get(logical) !== entry) {
                    if (entry.resource && typeof entry.resource.close === "function") entry.resource.close();
                    imageCache.delete(logical);
                    throw STOP;
                }
                entry.refs += 1;
                return { entry };
            }

            function imageRetain(handle) {
                if (handle && handle.entry && !handle.entry.disposed) handle.entry.refs += 1;
                return handle;
            }

            function imageRelease(handle) {
                if (!handle || !handle.entry || handle.entry.disposed) return;
                const entry = handle.entry;
                entry.refs -= 1;
                if (entry.refs <= 0) {
                    entry.refs = 0;
                    imageCache.delete(entry.logical);
                    if (entry.resource && typeof entry.resource.close === "function") entry.resource.close();
                }
            }

            function imageAssign(previous, value) {
                imageRetain(value);
                imageRelease(previous);
                return value;
            }

            function imageMoveAssign(previous, ownedValue) {
                imageRelease(previous);
                return ownedValue;
            }

            function imageLoadedRaw(handle) { return Boolean(handle && handle.entry && !handle.entry.disposed && handle.entry.resource); }
            function imageLoaded(handle) {
                try { return imageLoadedRaw(handle); }
                finally { imageRelease(handle); }
            }
            function imageWidth(handle) {
                try { return imageLoadedRaw(handle) ? safe(handle.entry.width) : 0; }
                finally { imageRelease(handle); }
            }
            function imageHeight(handle) {
                try { return imageLoadedRaw(handle) ? safe(handle.entry.height) : 0; }
                finally { imageRelease(handle); }
            }

            function drawImage(handle, sourceX, sourceY, sourceWidth, sourceHeight, destinationX, destinationY,
                destinationWidth, destinationHeight, opacity, filter, flip, anchorX, anchorY) {
                try {
                    if (!imageLoadedRaw(handle)) throw new Error("Draw Image requires a loaded Image.");
                    const entry = handle.entry;
                    sourceX = safe(sourceX); sourceY = safe(sourceY);
                    sourceWidth = safe(sourceWidth); sourceHeight = safe(sourceHeight);
                    destinationX = safe(destinationX); destinationY = safe(destinationY);
                    destinationWidth = safe(destinationWidth); destinationHeight = safe(destinationHeight);
                    opacity = safe(opacity); filter = safe(filter); flip = safe(flip);
                    anchorX = safe(anchorX); anchorY = safe(anchorY);
                    if (sourceWidth < 0) sourceWidth = entry.width;
                    if (sourceHeight < 0) sourceHeight = entry.height;
                    if (destinationWidth < 0) destinationWidth = sourceWidth;
                    if (destinationHeight < 0) destinationHeight = sourceHeight;
                    if (sourceX < 0 || sourceY < 0 || sourceWidth <= 0 || sourceHeight <= 0 ||
                        sourceX + sourceWidth > entry.width || sourceY + sourceHeight > entry.height)
                        throw new Error("Draw Image source rectangle is outside the image.");
                    if (destinationWidth <= 0 || destinationHeight <= 0 || opacity < 0 || opacity > 100 ||
                        (filter !== 0 && filter !== 1) || (flip & ~3) !== 0)
                        throw new Error("Draw Image destination, opacity, filter, or flip is invalid.");
                    const left = destinationX - anchorX;
                    const top = destinationY - anchorY;
                    const flipX = (flip & 1) !== 0;
                    const flipY = (flip & 2) !== 0;
                    back.save();
                    try {
                        back.globalAlpha = opacity / 100;
                        back.imageSmoothingEnabled = filter === 0;
                        if (flipX || flipY) {
                            back.translate(left + (flipX ? destinationWidth : 0), top + (flipY ? destinationHeight : 0));
                            back.scale(flipX ? -1 : 1, flipY ? -1 : 1);
                            back.drawImage(entry.resource, sourceX, sourceY, sourceWidth, sourceHeight,
                                0, 0, destinationWidth, destinationHeight);
                        } else {
                            back.drawImage(entry.resource, sourceX, sourceY, sourceWidth, sourceHeight,
                                left, top, destinationWidth, destinationHeight);
                        }
                    } finally { back.restore(); }
                } finally { imageRelease(handle); }
            }

            function pushClip(x, y, width, height) {
                x = safe(x); y = safe(y); width = safe(width); height = safe(height);
                if (width <= 0 || height <= 0) throw new Error("Clip Rectangle width and height must be positive.");
                clipStack.push({ x, y, width, height });
                back.save();
                back.beginPath();
                back.rect(x, y, width, height);
                back.clip();
            }

            function popClip() {
                if (clipStack.length === 0) return;
                clipStack.pop();
                back.restore();
            }

            function textWidth(text, size) {
                size = safe(size);
                if (size <= 0) return 0;
                if (String(text).length === 0) return 0;
                textStyle(size, 0, false);
                return safe(Math.ceil(back.measureText(String(text)).width));
            }

            function textHeight(text, size) {
                size = safe(size);
                if (size <= 0) return 0;
                if (String(text).length === 0) return size;
                textStyle(size, 0, false);
                const metrics = back.measureText(String(text));
                const height = (metrics.actualBoundingBoxAscent || 0) + (metrics.actualBoundingBoxDescent || 0);
                return safe(Math.ceil(height > 0 ? height : safe(size)));
            }

            function textLength(text) { return safe(Array.from(String(text)).length); }

            function textCodeAt(text, index) {
                index = safe(index);
                if (index < 0) return -1;
                const values = Array.from(String(text));
                return index >= values.length ? -1 : safe(values[index].codePointAt(0));
            }

            function textSlice(text, start, count) {
                start = safe(start); count = safe(count);
                if (start < 0 || count <= 0) return "";
                const values = Array.from(String(text));
                if (start >= values.length) return "";
                const end = count >= values.length - start ? values.length : safe(start + count);
                return values.slice(start, end).join("");
            }

            function print(items, suppressNewLine) {
                canvas.hidden = true;
                consoleOutput.hidden = false;
                consoleText += items.map(item => String(item)).join("");
                if (!suppressNewLine) consoleText += "\n";
                consoleOutput.textContent = consoleText;
                consoleOutput.scrollTop = consoleOutput.scrollHeight;
            }

            function clearScreen() {
                consoleText = "";
                consoleOutput.textContent = "";
            }

            function wait(duration) {
                duration = safe(duration);
                return new Promise(resolve => setTimeout(resolve, Math.max(0, duration)));
            }

            function showScreen() {
                if (closed) endProgram();
                visible.clearRect(0, 0, logicalWidth, logicalHeight);
                visible.drawImage(backCanvas, 0, 0, logicalWidth, logicalHeight);
                window.__smileWeb.frameCount += 1;
                pointerDeltaXValue = 0;
                pointerDeltaYValue = 0;
                pointerWheelDeltaValue = 0;
                pointerPressedButtons = 0;
                pointerReleasedButtons = 0;
                return new Promise(resolve => requestAnimationFrame(resolve));
            }

            function mapKey(event) {
                switch (event.code) {
                    case "KeyW": return 1;
                    case "KeyA": return 2;
                    case "KeyS": return 3;
                    case "KeyD": return 4;
                    case "ArrowUp": return 10;
                    case "ArrowDown": return 11;
                    case "ArrowLeft": return 12;
                    case "ArrowRight": return 13;
                    case "Enter": return 14;
                    case "Escape": return 15;
                    case "Space": return 16;
                    case "Digit1": return 17;
                    case "Digit2": return 18;
                    case "Digit3": return 20;
                    case "Digit4": return 22;
                    case "Tab": return 21;
                    default: return 19;
                }
            }

            function controlledKey(event) {
                return event.code.startsWith("Arrow") || event.code === "Space" || event.code === "Enter" ||
                    event.code === "Escape" || event.code === "Tab" || /^Key[WASD]$/.test(event.code);
            }

            async function toggleFullScreen() {
                try {
                    if (document.fullscreenElement) await document.exitFullscreen();
                    else await document.getElementById("smile-shell").requestFullscreen();
                } catch (_) { }
            }

            window.addEventListener("keydown", event => {
                userInteracted = true;
                const key = mapKey(event);
                const newlyPressed = pressInput(`keyboard:${event.code}`, key, false);
                syncMusic();
                if (event.altKey && event.code === "Enter") {
                    event.preventDefault();
                    void toggleFullScreen();
                    return;
                }
                if (event.repeat || event.ctrlKey || event.altKey || event.metaKey) return;
                if (controlledKey(event)) event.preventDefault();
                if (newlyPressed) enqueueKey(key);
            });

            window.addEventListener("keyup", event => { releaseInput(`keyboard:${event.code}`); });

            canvas.addEventListener("click", () => { userInteracted = true; canvas.focus(); syncMusic(); });
            canvas.addEventListener("pointerdown", handleCanvasPointerDown);
            canvas.addEventListener("pointermove", handleCanvasPointerMove);
            canvas.addEventListener("pointerup", event => handleCanvasPointerEnd(event));
            canvas.addEventListener("pointercancel", event => handleCanvasPointerEnd(event));
            canvas.addEventListener("lostpointercapture", event => handleCanvasPointerEnd(event, false));
            canvas.addEventListener("pointerleave", () => { if (activeCanvasPointers.size === 0) pointerInsideValue = false; });
            canvas.addEventListener("wheel", handleCanvasWheel, { passive: false });
            window.addEventListener("pointerdown", noteTouchInteraction);
            window.addEventListener("pointerup", event => handleVirtualPointerEnd(event));
            window.addEventListener("pointercancel", event => handleVirtualPointerEnd(event));
            window.addEventListener("resize", resizeCanvas);
            if (window.visualViewport) window.visualViewport.addEventListener("resize", resizeCanvas);
            if (window.screen && window.screen.orientation) window.screen.orientation.addEventListener("change", () => {
                releaseVirtualPointers();
                resizeCanvas();
            });
            document.addEventListener("fullscreenchange", resizeCanvas);
            window.addEventListener("focus", () => {
                active = !document.hidden;
                if (active) checkForWebUpdate();
                syncMusic();
            });
            window.addEventListener("blur", () => { active = false; keys.length = 0; releaseAllInputs(); stopSound(); syncMusic(); });
            document.addEventListener("visibilitychange", () => {
                active = !document.hidden && document.hasFocus();
                if (!active) { keys.length = 0; releaseAllInputs(); stopSound(); }
                syncMusic();
            });
            window.addEventListener("pagehide", () => {
                closed = true;
                keys.length = 0;
                releaseAllInputs();
                setVirtualControlsVisible(false);
                mediaShutdown();
            });

            function getKey() { return keys.length === 0 ? 0 : keys.shift(); }
            function keyHeld(key) { return (heldKeyCounts.get(safe(key)) || 0) > 0 ? 1 : 0; }

            function checkedChannel(channel) {
                channel = safe(channel);
                if (channel < 0 || channel >= 16) throw new Error("Sound channel must be from 0 through 15.");
                return channel;
            }

            function invalidateSoundChannel(index) {
                sfxGenerations[index] += 1;
                const slot = sfxChannels[index];
                if (slot) {
                    const voice = slot.voice;
                    try {
                        if (voice.stop) voice.stop();
                        else { voice.pause(); voice.currentTime = 0; }
                    } catch (_) { }
                }
                sfxChannels[index] = null;
                return sfxGenerations[index];
            }

            function stopSound(channel = null) {
                const first = channel === null ? 0 : checkedChannel(channel);
                const last = channel === null ? 15 : first;
                for (let index = first; index <= last; index += 1) invalidateSoundChannel(index);
            }

            async function loadSfx(path) {
                const logical = logicalPath(path);
                let pending = sfxCache.get(logical);
                if (pending) return pending;
                pending = (async () => {
                    const AudioContextType = window.AudioContext || window.webkitAudioContext;
                    if (!AudioContextType) return { logical, buffer: null };
                    if (!audioContext) audioContext = new AudioContextType();
                    const response = await fetch(logical);
                    if (!response.ok) throw new Error(`Play Sound failed: ${logical}`);
                    const bytes = await response.arrayBuffer();
                    const buffer = await audioContext.decodeAudioData(bytes.slice(0));
                    return { logical, buffer };
                })();
                sfxCache.set(logical, pending);
                try { return await pending; }
                catch (error) { sfxCache.delete(logical); throw error; }
            }

            async function playSound(path, channel = 0) {
                channel = checkedChannel(channel);
                const generation = invalidateSoundChannel(channel);
                if (mediaStopped || !active || !userInteracted) return;
                const cached = await loadSfx(path);
                const current = () => !mediaStopped && active && userInteracted &&
                    sfxGenerations[channel] === generation;
                if (!current()) return;
                if (cached.buffer && audioContext) {
                    if (audioContext.state === "suspended") await audioContext.resume();
                    if (!current()) return;
                    const source = audioContext.createBufferSource();
                    source.buffer = cached.buffer;
                    source.connect(audioContext.destination);
                    const slot = { voice: source, generation };
                    source.onended = () => {
                        if (sfxChannels[channel] === slot && sfxGenerations[channel] === generation) {
                            sfxChannels[channel] = null;
                            sfxCompletionCount += 1;
                        }
                    };
                    sfxChannels[channel] = slot;
                    source.start();
                    return;
                }
                const sound = new Audio(cached.logical);
                if (!current()) return;
                const slot = { voice: sound, generation };
                sfxChannels[channel] = slot;
                sound.addEventListener("ended", () => {
                    if (sfxChannels[channel] === slot && sfxGenerations[channel] === generation) {
                        sfxChannels[channel] = null;
                        sfxCompletionCount += 1;
                    }
                });
                const playback = sound.play();
                if (playback) playback.catch(() => {
                    if (sfxChannels[channel] === slot && sfxGenerations[channel] === generation)
                        sfxChannels[channel] = null;
                });
            }

            function syncMusic() {
                if (!currentMusic) return;
                currentMusic.volume = active ? Math.max(0, Math.min(100, musicVolume)) / 100 : 0;
                if (!active || !musicRequested || musicPaused) {
                    currentMusic.pause();
                    return;
                }
                if (!userInteracted) return;
                const playback = currentMusic.play();
                if (playback) playback.catch(() => { });
            }

            function playMusic(path, loop) {
                stopMusic();
                try {
                    currentMusic = new Audio(logicalPath(path));
                    currentMusic.loop = Boolean(loop);
                    musicRequested = true;
                    musicPaused = false;
                    syncMusic();
                } catch (_) {
                    currentMusic = null;
                    musicRequested = false;
                }
            }

            function pauseMusic() {
                musicPaused = true;
                if (currentMusic) currentMusic.pause();
            }

            function resumeMusic() {
                musicPaused = false;
                syncMusic();
            }

            function stopMusic() {
                musicRequested = false;
                musicPaused = false;
                if (!currentMusic) return;
                currentMusic.pause();
                currentMusic.currentTime = 0;
                currentMusic = null;
            }

            function setMusicVolume(volume) {
                musicVolume = Math.max(0, Math.min(100, safe(volume)));
                syncMusic();
            }

            async function loadTextFile(path, target) {
                if (!target || !Array.isArray(target.data)) throw new Error("Load Text File requires a one-dimensional array.");
                target.data.fill(0);
                try {
                    const response = await fetch(logicalPath(path), { cache: "no-store" });
                    if (!response.ok) return 0;
                    const bytes = new Uint8Array(await response.arrayBuffer());
                    let source = 0;
                    if (bytes.length >= 3 && bytes[0] === 0xef && bytes[1] === 0xbb && bytes[2] === 0xbf) source = 3;
                    const count = Math.min(target.data.length, bytes.length - source);
                    for (let index = 0; index < count; index += 1) target.data[index] = bytes[source + index];
                    return safe(count);
                } catch (_) {
                    target.data.fill(0);
                    return 0;
                }
            }

            function storageKey(key) { return `${storageNamespace}:${key}`; }
            function dataStorageKey(key) { return `${storageNamespace}:data:${sha256Hex(utf8(String(key)))}`; }

            function loadInt(key, defaultValue) {
                defaultValue = safe(defaultValue);
                const fullKey = storageKey(key);
                let text = memoryStorage.has(fullKey) ? memoryStorage.get(fullKey) : null;
                try { text = localStorage.getItem(fullKey) ?? text; } catch (_) { }
                if (typeof text !== "string" || !/^-?\d+$/.test(text)) return defaultValue;
                const value = Number(text);
                return Number.isSafeInteger(value) ? value : defaultValue;
            }

            function saveInt(key, value) {
                value = safe(value);
                const fullKey = storageKey(key);
                const text = String(value);
                memoryStorage.set(fullKey, text);
                try { localStorage.setItem(fullKey, text); } catch (_) { }
            }

            function encodeBytes(values) {
                let binary = "";
                for (let index = 0; index < values.length; index += 1) binary += String.fromCharCode(values[index]);
                return btoa(binary);
            }

            function decodeBytes(text) {
                const binary = atob(text);
                return Array.from(binary, character => character.charCodeAt(0));
            }

            function dataEnvelope(payload) {
                const envelope = new Uint8Array(44 + payload.length);
                envelope.set([0x53, 0x4d, 0x44, 0x34], 0);
                const view = new DataView(envelope.buffer);
                view.setUint32(4, 1, true);
                view.setUint32(8, payload.length, true);
                envelope.set(sha256(payload), 12);
                envelope.set(payload, 44);
                return envelope;
            }

            function dataPayload(envelope) {
                if (envelope.length < 44 || envelope[0] !== 0x53 || envelope[1] !== 0x4d ||
                    envelope[2] !== 0x44 || envelope[3] !== 0x34)
                    throw new Error("Load Data encountered an invalid persistent-data envelope.");
                const view = new DataView(envelope.buffer, envelope.byteOffset, envelope.byteLength);
                const version = view.getUint32(4, true);
                const length = view.getUint32(8, true);
                if (version !== 1 || length > 1024 * 1024 || envelope.length !== 44 + length)
                    throw new Error("Load Data encountered an unsupported or malformed persistent-data envelope.");
                const payload = envelope.slice(44);
                const digest = sha256(payload);
                for (let index = 0; index < digest.length; index += 1)
                    if (digest[index] !== envelope[12 + index])
                        throw new Error("Load Data persistent-data checksum mismatch.");
                return payload;
            }

            function saveData(target, count, key) {
                if (!target || !Array.isArray(target.data) || target.dimensions.length !== 1)
                    throw new Error("Save Data source must be a one-dimensional Number array.");
                count = safe(count);
                if (count < 0 || count > target.data.length || count > 1024 * 1024)
                    throw new Error("Save Data Count is outside the buffer or DATA_BLOCK_MAX_BYTES.");
                const bytes = target.data.slice(0, count).map(value => {
                    value = safe(value);
                    if (value < 0 || value > 255) throw new Error("Save Data values must be bytes from 0 through 255.");
                    return value;
                });
                const fullKey = dataStorageKey(key);
                const text = encodeBytes(dataEnvelope(new Uint8Array(bytes)));
                memoryStorage.set(fullKey, text);
                localStorage.setItem(fullKey, text);
            }

            function loadData(key, target) {
                if (!target || !Array.isArray(target.data) || target.dimensions.length !== 1)
                    throw new Error("Load Data destination must be a one-dimensional Number array.");
                target.data.fill(0);
                const fullKey = dataStorageKey(key);
                let text = memoryStorage.has(fullKey) ? memoryStorage.get(fullKey) : null;
                text = localStorage.getItem(fullKey) ?? text;
                if (text === null) return 0;
                let bytes;
                try { bytes = dataPayload(new Uint8Array(decodeBytes(text))); }
                catch (error) { target.data.fill(0); throw error; }
                if (bytes.length > 1024 * 1024 || bytes.length > target.data.length)
                    throw new Error("Load Data block exceeds the destination capacity.");
                for (let index = 0; index < bytes.length; index += 1) target.data[index] = bytes[index];
                return safe(bytes.length);
            }

            function gameClosed() { return closed ? 1 : 0; }
            function mediaDiagnostics() {
                let references = 0;
                for (const entry of imageCache.values()) references += entry.refs;
                return {
                    backingWidth, backingHeight, logicalWidth, logicalHeight,
                    devicePixelRatio: Math.max(1, Number(window.devicePixelRatio) || 1),
                    clipDepth: clipStack.length,
                    imageCacheCount: imageCache.size,
                    imageReferenceCount: references,
                    classLiveCount: classLiveObjects,
                    imageDecodeCount, imageCacheHitCount,
                    shutdownImageCacheEntries, shutdownImageReferences,
                    sfxActiveCount: sfxChannels.filter(Boolean).length,
                    sfxCacheCount: sfxCache.size,
                    sfxCompletionCount,
                    mediaStopped,
                    virtualControlsMode,
                    virtualControlsVisible,
                    virtualActivePointerCount: activeVirtualPointers.size,
                    canvasActivePointerCount: activeCanvasPointers.size,
                    activeInputSourceCount: inputSources.size,
                    queuedKeyCount: keys.length,
                    maximumQueuedKeyCount: MAX_QUEUED_KEYS,
                    maximumActiveInputSourceCount: MAX_ACTIVE_INPUT_SOURCES,
                    storageNamespace
                };
            }

            function mediaShutdown() {
                if (mediaStopped) return;
                mediaStopped = true;
                shutdownImageCacheEntries = imageCache.size;
                shutdownImageReferences = 0;
                for (const entry of imageCache.values()) {
                    shutdownImageReferences += entry.refs;
                    if (entry.resource && typeof entry.resource.close === "function") entry.resource.close();
                    entry.refs = 0;
                    entry.disposed = true;
                }
                imageCache.clear();
                stopSound();
                stopMusic();
                sfxCache.clear();
                if (audioContext) {
                    try { void audioContext.close(); } catch (_) { }
                    audioContext = null;
                }
            }

            function endProgram() { closed = true; throw STOP; }

            function finish() {
                closed = true;
                keys.length = 0;
                releaseAllInputs();
                setVirtualControlsVisible(false);
                mediaShutdown();
                window.__smileWeb.status = "stopped";
            }

            function fail(error) {
                if (error === STOP) { finish(); return; }
                closed = true;
                keys.length = 0;
                releaseAllInputs();
                setVirtualControlsVisible(false);
                mediaShutdown();
                window.__smileWeb.status = "error";
                const message = error && error.stack ? error.stack : String(error);
                console.error(error);
                errorPanel.textContent = `SMILE Web runtime error\n\n${message}`;
                errorPanel.hidden = false;
            }

            function run(main) {
                window.__smileWeb.status = "running";
                Promise.resolve().then(main).then(finish).catch(fail);
            }

            return {
                safe, add, sub, mul, div, mod, neg, isTrue, booleanText, abs, min, max, timer, rgb, random,
                array, get, set, ref, refArray, invalidRef, classCreate, classRequire, classRetain, classRelease,
                classMoveAssign, classOwnedRef, classLiveCount, configure, gameWindow, clear, fillRectangle, drawRectangle,
                fillRoundedRectangle, drawRoundedRectangle, fillCircle, drawCircle, drawArc,
                fillQuadrilateral, drawQuadrilateral, drawLine, drawText, drawNumber, loadImage, imageRetain,
                imageRelease, imageAssign, imageMoveAssign, imageLoaded, imageWidth, imageHeight, drawImage,
                pushClip, popClip, textWidth, textHeight, textLength, textCodeAt, textSlice, showScreen,
                print, clearScreen, wait, getKey, keyHeld, pointerX, pointerY, pointerDeltaX, pointerDeltaY,
                pointerWheelDelta, pointerInside, pointerHeld, pointerPressed, pointerReleased, playSound, stopSound,
                playMusic, pauseMusic, resumeMusic, stopMusic, setMusicVolume, loadTextFile,
                loadInt, saveInt, loadData, saveData, renderer3D, renderer3DImage, renderer3DText,
                gameClosed, endProgram, mediaShutdown, mediaDiagnostics, run
            };
        })();
        """;
}
