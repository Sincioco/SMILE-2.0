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
        var runtime = RuntimeFor(emitter.ResponsiveWindow);
        var buildVersion = BuildVersion(emitter.Title, game, runtime, emitter.WebLoadingAuthor, emitter.WebLoadingLogo);
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(outputDirectory, "index.html"), Index(emitter.Title, buildVersion,
            emitter.WebLoadingAuthor, emitter.WebLoadingLogo), Utf8WithoutBom);
        afterFileWrite?.Invoke("index.html");
        File.WriteAllText(Path.Combine(outputDirectory, "smile-runtime.js"), runtime, Utf8WithoutBom);
        afterFileWrite?.Invoke("smile-runtime.js");
        File.WriteAllText(Path.Combine(outputDirectory, "game.js"), game, Utf8WithoutBom);
        afterFileWrite?.Invoke("game.js");
        File.WriteAllText(Path.Combine(outputDirectory, "smile.css"), Style, Utf8WithoutBom);
        afterFileWrite?.Invoke("smile.css");
    }

    private static string BuildVersion(string title, string game, string runtime, string? author, string? logo)
    {
        var unversionedIndex = Index(title, string.Empty, author, logo);
        var content = string.Join('\0', unversionedIndex, runtime, game, Style);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }

    private static string RuntimeFor(bool responsiveWindow) => responsiveWindow
        ? Runtime.Replace("const responsiveWindowEnabled = false;",
            "const responsiveWindowEnabled = true;", StringComparison.Ordinal)
        : Runtime;

    private static string Index(string title, string buildVersion, string? author, string? logo) => $$"""
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
            <section id="smile-loading" aria-label="Loading program"
                     style="position:fixed;inset:0;z-index:25;display:flex;flex-direction:column;align-items:center;overflow:auto;text-align:center;padding:24px 20px max(20px,env(safe-area-inset-bottom,0px));color:#f4f7fc;background:radial-gradient(ellipse at 50% 36%,#172d48,#08101e 72%);font-family:Segoe UI,Arial,sans-serif">
              <div style="width:min(760px,100%);margin:auto 0;padding:16px 0 28px;flex-shrink:0">
                <h1 style="font-size:clamp(20px,3vw,32px);font-weight:600;margin:0 0 20px">{{WebUtility.HtmlEncode(title)}}</h1>
                {{(string.IsNullOrWhiteSpace(logo) ? string.Empty : $"<img id=\"smile-loading-logo\" src=\"{WebUtility.HtmlEncode(logo)}?v={buildVersion}\" alt=\"SMILE 2.0\" fetchpriority=\"high\" style=\"display:block;width:min(480px,80vw);height:min(38vh,400px);object-fit:contain;margin:0 auto 24px\">")}}
                <progress id="smile-loading-progress" aria-label="Loading assets" style="width:min(440px,80vw);height:12px;accent-color:#eec746"></progress>
                <div role="status" aria-live="polite" aria-atomic="true">
                  <p id="smile-loading-status" style="margin:14px 0 8px">Starting program…</p>
                  <p id="smile-loading-detail" style="font-size:13px;min-height:2.6em;color:#abbcd3;overflow-wrap:anywhere;margin:0 0 22px">Preparing the Web runtime. Large assets may take a moment.</p>
                </div>
                <p style="font-size:18px;margin:0 0 6px">Created in SMILE 2.0</p>
                {{(string.IsNullOrWhiteSpace(author) ? string.Empty : $"<p style=\"font-size:15px;margin:0\">Created by {WebUtility.HtmlEncode(author)}</p>")}}
                <noscript>JavaScript is required to run this SMILE program.</noscript>
              </div>
              <footer aria-label="SMILE 2.0 copyright and links" style="flex-shrink:0;max-width:1200px;font-size:12px;line-height:1.8;color:#9ca9be">
                <div>SMILE 2.0 — Simple Modern and Intuitive Language for Everyone. Copyright(c) 2026. All rights reserved. Programmed by: Louiery R. Sincioco (Sin) | <a href="mailto:louiery@gmail.com" target="_blank" rel="noopener noreferrer" style="color:inherit">louiery@gmail.com</a></div>
                <div><a href="https://github.com/Sincioco" target="_blank" rel="noopener noreferrer" style="color:inherit">github.com/Sincioco</a> | <a href="https://facebook.com/louiery.sincioco" target="_blank" rel="noopener noreferrer" style="color:inherit">facebook.com/louiery.sincioco</a> | <a href="https://linkedin.com/in/louierysincioco" target="_blank" rel="noopener noreferrer" style="color:inherit">linkedin.com/in/louierysincioco</a> | <a href="https://youtube.com/@TheSincioco" target="_blank" rel="noopener noreferrer" style="color:inherit">youtube.com/@TheSincioco</a> | <a href="https://tiktok.com/@sincioco" target="_blank" rel="noopener noreferrer" style="color:inherit">tiktok.com/@sincioco</a></div>
                <div><a href="https://github.com/sincioco/smile-2.0" target="_blank" rel="noopener noreferrer" style="color:inherit">github.com/sincioco/smile-2.0</a></div>
              </footer>
            </section>
            <button id="smile-fullscreen" type="button" hidden aria-controls="smile-shell" aria-pressed="false">Full Screen</button>
            <canvas id="smile-canvas" width="960" height="540" tabindex="0" aria-label="{{WebUtility.HtmlEncode(title)}}"></canvas>
            <pre id="smile-console" hidden tabindex="0" aria-live="polite"></pre>
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
          <script>
            window.addEventListener("error", event => {
              const loader = document.getElementById("smile-loading");
              if (!loader || loader.hidden) return;
              const script = event.target && event.target.tagName === "SCRIPT";
              if (!script && !event.error) return;
              document.getElementById("smile-loading-status").textContent = "Unable to start the program";
              document.getElementById("smile-loading-detail").textContent = script
                ? "A program file could not be downloaded. Check your connection and reload this page."
                : "A startup error occurred. Reload this page or report the browser console error.";
              document.getElementById("smile-loading-progress").hidden = true;
            }, true);
          </script>
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
        #smile-loading[hidden] { display: none !important; }
        #smile-loading a:hover { color: #fff !important; }
        #smile-loading a:focus-visible { outline: 2px solid #eec746; outline-offset: 3px; }
        #smile-fullscreen { position: absolute; z-index: 30; top: 8px; left: 50%; transform: translate(-50%, -150%); padding: 8px 14px; border: 2px solid #fff; border-radius: 4px; color: #fff; background: #14263a; font: 600 16px "Segoe UI", sans-serif; }
        #smile-fullscreen:focus { transform: translate(-50%, 0); outline: 2px solid #46e6ff; outline-offset: 2px; }
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
            const fullScreenButton = document.getElementById("smile-fullscreen");
            const loadingScreen = document.getElementById("smile-loading");
            const loadingStatus = document.getElementById("smile-loading-status");
            const loadingDetail = document.getElementById("smile-loading-detail");
            const startupAssets = new Map();
            // Encoded immutable model/image data only: no actor, animator, material or save state.
            // A page reload starts a fresh cache, so rebuilding at the same URL cannot retain old data.
            const assetDownloadCache = new Map();
            const MAX_ASSET_DOWNLOAD_CACHE_BYTES = 128 * 1024 * 1024;
            const MAX_ASSET_DOWNLOAD_CACHE_ENTRIES = 256;
            let assetDownloadCacheBytes = 0;
            let assetDownloadCacheHits = 0;
            let assetDownloadCount = 0;
            let startupPresented = false;

            function updateStartupLoading() {
                if (startupPresented || !loadingScreen) return;
                const entries = Array.from(startupAssets.entries());
                const ready = entries.filter(([, state]) => state === "ready").length;
                const pending = entries.filter(([, state]) => state === "loading");
                const failed = entries.filter(([, state]) => state === "failed").length;
                if (loadingStatus) loadingStatus.textContent = pending.length
                    ? `Loading assets — ${ready} ready, ${pending.length} downloading or decoding`
                    : `Preparing scene — ${ready} assets ready${failed ? `, ${failed} failed` : ""}`;
                if (loadingDetail) loadingDetail.textContent = pending.length
                    ? pending[pending.length - 1][0]
                    : (failed ? "An asset failed to load; the program is checking recovery." : "Preparing the first frame…");
            }

            function startupAsset(path, state) {
                if (startupPresented || !loadingScreen) return;
                startupAssets.set(path, state);
                updateStartupLoading();
            }

            function finishStartupLoading() {
                startupPresented = true;
                startupAssets.clear();
                if (loadingScreen) { loadingScreen.hidden = true; loadingScreen.style.display = "none"; }
            }

            function forgetAssetDownload(logical) {
                if (!assetDownloadCache.has(logical)) return;
                assetDownloadCacheBytes -= assetDownloadCache.get(logical).byteLength;
                assetDownloadCache.delete(logical);
            }

            async function fetchAssetBytes(path, options, retain = false) {
                const logical = logicalPath(path);
                if (retain && assetDownloadCache.has(logical)) {
                    const cached = assetDownloadCache.get(logical);
                    assetDownloadCache.delete(logical);
                    assetDownloadCache.set(logical, cached);
                    assetDownloadCacheHits += 1;
                    startupAsset(logical, "ready");
                    return cached;
                }
                startupAsset(logical, "loading");
                try {
                    assetDownloadCount += 1;
                    const response = await fetch(logical, options);
                    if (!response.ok) throw new Error(`Asset download failed (${response.status}): ${logical}`);
                    const bytes = await response.arrayBuffer();
                    if (mediaStopped) throw STOP;
                    if (retain && bytes.byteLength <= MAX_ASSET_DOWNLOAD_CACHE_BYTES) {
                        // Concurrent callers can finish the same path; replace rather than double-count it.
                        if (assetDownloadCache.has(logical)) {
                            assetDownloadCacheBytes -= assetDownloadCache.get(logical).byteLength;
                            assetDownloadCache.delete(logical);
                        }
                        while (assetDownloadCache.size &&
                            (assetDownloadCacheBytes + bytes.byteLength > MAX_ASSET_DOWNLOAD_CACHE_BYTES ||
                             assetDownloadCache.size >= MAX_ASSET_DOWNLOAD_CACHE_ENTRIES)) {
                            const oldest = assetDownloadCache.keys().next().value;
                            assetDownloadCacheBytes -= assetDownloadCache.get(oldest).byteLength;
                            assetDownloadCache.delete(oldest);
                        }
                        assetDownloadCache.set(logical, bytes);
                        assetDownloadCacheBytes += bytes.byteLength;
                    }
                    startupAsset(logical, "ready");
                    return bytes;
                } catch (error) {
                    startupAsset(logical, "failed");
                    throw error;
                }
            }
            const virtualControls = document.getElementById("smile-controls");
            const virtualControlButtons = virtualControls
                ? Array.from(virtualControls.querySelectorAll("button[data-smile-control]"))
                : [];
            const keys = [];
            let keyEventHeldKeys = new Set();
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
            const responsiveWindowEnabled = false;
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
            let renderer3DContextLost = false;
            let renderer3DProgram = null;
            let renderer3DPbrProgram = null;
            let renderer3DPbrAttempted = false;
            let renderer3DPbrState = 0;
            let renderer3DPbrFailure = 0;
            let renderer3DPbrAttemptCount = 0;
            let renderer3DAnisotropy = null;
            let renderer3DAnisotropyAttempted = false;
            let renderer3DMaximumAnisotropy = 1;
            let renderer3DContextEvents = false;
            let renderer3DLastError = 0;
            let renderer3DNextHandle = 1;
            let renderer3DFrameActive = false;
            let renderer3DBackdropTexture = 0;
            let renderer3DResourceEpoch = 1;
            let renderer3DDrawCallCount = 0;
            let renderer3DSubmittedTriangleCount = 0;
            let renderer3DPbrDrawCount = 0;
            let renderer3DSimpleDrawCount = 0;
            let renderer3DPbrTriangleCount = 0;
            let renderer3DMaterialInspection = 0;
            let renderer3DModelPaletteTexture = null;
            let renderer3DModelPaletteCachedAnimator = 0;
            let renderer3DModelPaletteCachedRevision = 0;
            let renderer3DModelPaletteCachedIgnoreOffsets = false;
            const renderer3DPivotScratch = new Float32Array(16);
            let renderer3DModelPaletteUploadCount = 0;
            let renderer3DShadowProgram = null;
            let renderer3DPostProgram = null;
            let renderer3DVfxProgram = null;
            let renderer3DDepthProgram = null;
            let renderer3DParticleQuadBuffer = null;
            let renderer3DParticleQuadIndexBuffer = null;
            let renderer3DShadowTexture = null;
            let renderer3DShadowFramebuffer = null;
            let renderer3DSceneTexture = null;
            let renderer3DSceneFramebuffer = null;
            let renderer3DMsaaTarget = null;
            let renderer3DSceneDepth = null;
            let renderer3DLinearDepthTexture = null;
            let renderer3DLinearDepthFramebuffer = null;
            let renderer3DDistortionTexture = null;
            let renderer3DDistortionFramebuffer = null;
            let renderer3DDistortionScratchTexture = null;
            let renderer3DDistortionScratchFramebuffer = null;
            let renderer3DBloomTextureA = null;
            let renderer3DBloomFramebufferA = null;
            let renderer3DBloomTextureB = null;
            let renderer3DBloomFramebufferB = null;
            let renderer3DPostRequested = false;
            let renderer3DHdrRequested = false;
            let renderer3DBloomRequested = false;
            let renderer3DShadowRequested = false;
            let renderer3DPostEffective = false;
            let renderer3DHdrEffective = false;
            let renderer3DBloomEffective = false;
            let renderer3DShadowEffective = false;
            let renderer3DToneMappingEffective = false;
            let renderer3DMultipassActive = false;
            let renderer3DShadowCaster = 1;
            let renderer3DShadowSlot = 0;
            let renderer3DShadowRequestedResolution = 2048;
            let renderer3DShadowResolution = 0;
            let renderer3DExposure = 100;
            let renderer3DBloomThreshold = 1200;
            let renderer3DBloomIntensity = 80;
            let renderer3DBloomDownsample = 2;
            let renderer3DBloomCycles = 2;
            let renderer3DRequestedSamples = 4;
            let renderer3DEffectiveSamples = 1;
            let renderer3DBloomWidth = 0;
            let renderer3DBloomHeight = 0;
            let renderer3DFallbackFlags = 0;
            let renderer3DM5ResourceGeneration = 1;
            let renderer3DM5ConfigurationRevision = 1;
            let renderer3DM5AppliedRevision = 0;
            let renderer3DM5Width = 0;
            let renderer3DM5Height = 0;
            let renderer3DLogicalSubmissionCount = 0;
            let renderer3DRejectedSubmissionCount = 0;
            let renderer3DShadowDrawCount = 0;
            let renderer3DShadowTriangleCount = 0;
            let renderer3DShadowPaletteUploadCount = 0;
            let renderer3DPostDrawCount = 0;
            let renderer3DResolveCount = 0;
            let renderer3DVfxDrawCount = 0;
            let renderer3DVfxTriangleCount = 0;
            let renderer3DVfxUploadCount = 0;
            let renderer3DVfxRejectedOperationCount = 0;
            let renderer3DVfxParticleDrawCount = 0;
            let renderer3DVfxRibbonDrawCount = 0;
            let renderer3DVfxParticleTriangleCount = 0;
            let renderer3DVfxRibbonTriangleCount = 0;
            let renderer3DVfxParticleSubmissionCount = 0;
            let renderer3DVfxRibbonSubmissionCount = 0;
            let renderer3DStagedParticleCapacity = 0;
            let renderer3DStagedRibbonCapacity = 0;
            let renderer3DSubmissionCount = 0;
            let renderer3DPaletteSnapshotCount = 0;
            let renderer3DPhysicalSubmissionCount = 0;
            let renderer3DSubmissionGroupActive = false;
            let renderer3DSubmissionGroupStart = 0;
            let renderer3DSubmissionGroupPaletteStart = 0;
            let renderer3DSubmissionGroupReserved = 0;
            let renderer3DSubmissionGroupPhysical = 0;
            let renderer3DSubmissionGroupLogical = 0;
            let renderer3DSubmissionGroupToken = 0;
            let renderer3DSubmissionGroupSerial = 0;
            let renderer3DTargetBytes = 0;
            let renderer3DShadowBytes = 0;
            let renderer3DSceneBytes = 0;
            let renderer3DBloomBytes = 0;
            let renderer3DSoftDepthRequested = false;
            let renderer3DSoftDepthEffective = 0;
            let renderer3DSoftDepthFallbackReason = 1;
            let renderer3DSoftDepthWidth = 0;
            let renderer3DSoftDepthHeight = 0;
            let renderer3DSoftDepthBytes = 0;
            let renderer3DSoftDepthCopyDrawCount = 0;
            let renderer3DSoftDepthCopyFailureCount = 0;
            let renderer3DSoftParticleDrawCount = 0;
            let renderer3DSoftDepthResourceGeneration = 1;
            let renderer3DDistortionRequested = false;
            let renderer3DDistortionQuality = 3;
            let renderer3DDistortionEffective = 0;
            let renderer3DDistortionFallbackReason = 1;
            let renderer3DDistortionWidth = 0;
            let renderer3DDistortionHeight = 0;
            let renderer3DDistortionBytes = 0;
            let renderer3DDistortionVectorDrawCount = 0;
            let renderer3DDistortionCompositeDrawCount = 0;
            let renderer3DDistortionEmitterCount = 0;
            let renderer3DDistortionMaximumStrength = 0;
            let renderer3DDistortionResourceGeneration = 1;
            let renderer3DRenderingDistortionVectors = false;
            let renderer3DGpuParticleTotalCapacity = 0;
            let renderer3DGpuParticleSpawnsAccepted = 0;
            let renderer3DGpuParticleSpawnsRejected = 0;
            let renderer3DGpuParticleSimulationSteps = 0;
            let renderer3DGpuParticleDroppedTime = 0;
            let renderer3DGpuParticleCpuUploadBytes = 0;
            let renderer3DGpuParticleQueueEntries = 0;
            let renderer3DGpuParticleFrameCount = 0;
            let renderer3DGpuParticleDispatchCount = 0;
            let renderer3DGpuParticleRenderDrawCount = 0;
            let renderer3DGpuParticleGpuStateBytes = 0;
            let renderer3DGpuParticleRestartCount = 0;
            let renderer3DGpuParticleReadbackCount = 0;
            let renderer3DGpuParticlePipeline = null;
            let renderer3DGpuParticlePipelineAttempted = false;
            let renderer3DGpuParticleBackendAvailable = false;
            const renderer3DMeshes = new Map();
            const renderer3DObjects = new Map();
            const renderer3DTextures = new Map();
            const renderer3DMaterials = new Map();
            const renderer3DModels = new Map();
            const renderer3DSkeletons = new Map();
            const renderer3DClips = new Map();
            const renderer3DAnimators = new Map();
            const renderer3DParticleBatches = new Map();
            const renderer3DRibbonBatches = new Map();
            const renderer3DGpuParticleSystems = new Map();
            const renderer3DGpuParticleFrameHandles = new Float64Array(32);
            const renderer3DStaticBones = new Float32Array(32 * 16);
            const renderer3DModelScratch = new Float32Array(16);
            const renderer3DViewScratch = new Float32Array(16);
            const renderer3DProjectionScratch = new Float32Array(16);
            const renderer3DMvpScratch = new Float32Array(16);
            const renderer3DMatrixScratchA = new Float32Array(16);
            const renderer3DMatrixScratchB = new Float32Array(16);
            const renderer3DMatrixScratchC = new Float32Array(16);
            const renderer3DMatrixScratchD = new Float32Array(16);
            const renderer3DTintScratch = new Float32Array(4);
            const renderer3DMaterialScratch = new Float32Array(4);
            const renderer3DNormalScratch = new Float32Array(9);
            const renderer3DSubmissions = new Float64Array(512);
            const renderer3DSubmissionObjects = [];
            const renderer3DPaletteSnapshots = [];
            const renderer3DSubmissionObject = 1;
            const renderer3DSubmissionParticleBatch = 2;
            const renderer3DSubmissionRibbonBatch = 3;
            const renderer3DShadowCenter = new Float32Array([0, 100, 0]);
            const renderer3DShadowArea = new Float32Array([1200, 900, 1, 2400]);
            const renderer3DShadowSettings = new Float32Array([.0015, .006, 0, 0]);
            const renderer3DShadowMatrixScratch = new Float32Array(16);
            const renderer3DShadowViewScratch = new Float32Array(16);
            const renderer3DShadowProjectionScratch = new Float32Array(16);
            const renderer3DPostFirstScratch = new Float32Array(4);
            const renderer3DPostSecondScratch = new Float32Array(4);
            const renderer3DClearScratch = new Float32Array(4);
            const renderer3DAmbient = new Float32Array([1, 1, 1, .25]);
            const renderer3DDirectionalDirection = new Float32Array([-.35, .8, -.45, 1]);
            const renderer3DDirectionalColor = new Float32Array([1, 1, 1, 1]);
            const renderer3DLocalPositionType = new Float32Array(16);
            const renderer3DLocalDirectionRange = new Float32Array(16);
            const renderer3DLocalColorIntensity = new Float32Array(16);
            const renderer3DLocalCone = new Float32Array(16);
            for (let bone = 0; bone < 32; bone += 1)
                renderer3DStaticBones[bone * 16] = renderer3DStaticBones[bone * 16 + 5] =
                    renderer3DStaticBones[bone * 16 + 10] = renderer3DStaticBones[bone * 16 + 15] = 1;
            for (let submission = 0; submission < 512; submission += 1) {
                renderer3DSubmissionObjects.push({
                    kind:renderer3DSubmissionObject,snapshot:true,source:0,mesh:0,material:0,resourceRevision:0,
                    animator:0,paletteIndex:-1,ignoreNodeOffsets:false,cullMode:0,
                    pivotPosition:new Float32Array(3),pivotRotation:new Float32Array(3),
                    position:new Float32Array(3),rotation:new Float32Array(3),scale:new Float32Array(3),
                    color:new Float32Array(4),visible:false,castsShadow:false,receivesShadow:false,
                    hasMaterial:false,snapshotMaterial:{kind:0,texture:0,textures:new Float64Array(4),alphaMode:0,
                        color:new Float32Array(4),unlit:false,emissive:0,cutoff:.5,doubleSided:false,
                        softDepthMode:0,softDepthDistance:0,vfxShadingMode:0,
                        distortionStrength:0,distortionNoiseScale:0,distortionNoiseSpeed:0,
                        distortionFlowX:0,distortionFlowY:0,
                        baseColor:new Float32Array(4),surface:new Float32Array(4),
                        emissiveAlpha:new Float32Array(4),textureFlags:new Float32Array(4)}
                });
                renderer3DPaletteSnapshots.push({animatorHandle:0,revision:0,production:false,
                    palette:new Float32Array(128*16)});
            }
            const renderer3DCamera = {
                position: [0, 300, -800], target: [0, 0, 0], up: [0, 1, 0], fov: 55, near: 1, far: 10000
            };
            const renderer3DPendingCamera = {
                position: [0, 0, 0], target: [0, 0, 0], up: [0, 0, 0], fov: 0, near: 0, far: 0,
                hasProjection: false, hasUp: false
            };
            const renderer3DCameraWorldBound = 1000000;
            const renderer3DCameraErrorInvalidPositionTarget = 58;
            const renderer3DCameraErrorZeroViewDirection = 59;
            const renderer3DCameraErrorInvalidProjection = 60;
            const renderer3DCameraErrorInvalidUp = 61;
            const renderer3DCameraErrorParallelUp = 62;
            const renderer3DCameraErrorPendingIncomplete = 63;
            const renderer3DCameraErrorFrameActive = 64;

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
                keys.push({ key, held: new Set(heldKeyCounts.keys()) });
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
                keyEventHeldKeys.clear();
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
                canvas.focus({ preventScroll: true });
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
            function windowWidth() { return logicalWidth; }
            function windowHeight() { return logicalHeight; }
            function windowTitle(value) {
                document.title = String(value);
                return true;
            }
            function windowActivate() { return false; }
            function pointerY() { return pointerYValue; }
            function pointerDeltaX() { return pointerDeltaXValue; }
            function pointerDeltaY() { return pointerDeltaYValue; }
            function pointerWheelDelta() { return pointerWheelDeltaValue; }
            function pointerWheelRemainder() { return 0; }
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
                if (!document.activeElement || document.activeElement === document.body ||
                    document.activeElement === consoleOutput)
                    canvas.focus({ preventScroll: true });
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
                if (responsiveWindowEnabled && gameWindowCreated) {
                    logicalWidth = Math.max(1, Math.floor(window.innerWidth));
                    logicalHeight = Math.max(1, Math.floor(window.innerHeight));
                    canvas.style.aspectRatio = `${logicalWidth} / ${logicalHeight}`;
                }
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
                // Context loss can precede its queued DOM event. Never compile against
                // that invalid driver object or treat it as a permanent shader defect.
                if (renderer3DContextLost) return null;
                if (renderer3DGl) return renderer3DGl.isContextLost() ? null : renderer3DGl;
                let context = null;
                try {
                    context = renderer3DCanvas.getContext("webgl2", {
                        alpha: false, antialias: true, depth: true, preserveDrawingBuffer: true
                    });
                } catch (_) { }
                if (!context || typeof context.createShader !== "function" || context.isContextLost()) return null;
                renderer3DGl = context;
                if (!renderer3DContextEvents) {
                    renderer3DContextEvents = true;
                    renderer3DCanvas.addEventListener("webglcontextlost", event => {
                        event.preventDefault();
                        renderer3DContextLost = true;
                        renderer3DFrameActive = false;
                        renderer3DGl = null;
                        renderer3DProgram = null;
                        renderer3DPbrProgram = null;
                        renderer3DPbrAttempted = false;
                        renderer3DPbrState = 0;
                        renderer3DPbrFailure = 0;
                        renderer3DPbrAttemptCount = 0;
                        renderer3DModelPaletteTexture = null;
                        renderer3DModelPaletteCachedAnimator = 0;
                        renderer3DModelPaletteCachedRevision = 0;
                        renderer3DShadowProgram = null;
                        renderer3DPostProgram = null;
                        renderer3DVfxProgram = null;
                        renderer3DDepthProgram = null;
                        renderer3DParticleQuadBuffer = null;
                        renderer3DParticleQuadIndexBuffer = null;
                        renderer3DShadowTexture = null;
                        renderer3DShadowFramebuffer = null;
                        renderer3DSceneTexture = null;
                        renderer3DSceneFramebuffer = null;
                        renderer3DMsaaTarget = null;
                        renderer3DEffectiveSamples = 1;
                        renderer3DSceneDepth = null;
                        renderer3DLinearDepthTexture = null;
                        renderer3DLinearDepthFramebuffer = null;
                        renderer3DDistortionTexture = null;
                        renderer3DDistortionFramebuffer = null;
                        renderer3DDistortionScratchTexture = null;
                        renderer3DDistortionScratchFramebuffer = null;
                        renderer3DBloomTextureA = null;
                        renderer3DBloomFramebufferA = null;
                        renderer3DBloomTextureB = null;
                        renderer3DBloomFramebufferB = null;
                        renderer3DShadowEffective = false;
                        renderer3DHdrEffective = false;
                        renderer3DBloomEffective = false;
                        renderer3DPostEffective = false;
                        renderer3DSoftDepthEffective = 0;
                        renderer3DSoftDepthWidth = renderer3DSoftDepthHeight = renderer3DSoftDepthBytes = 0;
                        renderer3DDistortionEffective = 0;
                        renderer3DDistortionWidth = renderer3DDistortionHeight = renderer3DDistortionBytes = 0;
                        renderer3DGpuParticleHandleContextLoss();
                        renderer3DMultipassActive = false;
                        renderer3DM5AppliedRevision = 0;
                        renderer3DReleaseSubmissions(0, renderer3DSubmissionCount);
                        renderer3DReleaseGpuParticleFrameSystems();
                        renderer3DSubmissionCount = renderer3DPaletteSnapshotCount = 0;
                        renderer3DSubmissionGroupActive = false;
                        renderer3DSubmissionGroupToken = 0;
                        renderer3DAnisotropyAttempted = false;
                        for (const mesh of renderer3DMeshes.values()) {
                            mesh.vertexBuffer = null;
                            mesh.indexBuffer = null;
                        }
                        for (const texture of renderer3DTextures.values()) texture.gpu = null;
                        for (const batch of renderer3DParticleBatches.values()) {
                            batch.gpu = null;
                            batch.uploadedRevision = 0;
                        }
                        for (const batch of renderer3DRibbonBatches.values()) {
                            batch.gpu = null;
                            batch.uploadedRevision = 0;
                        }
                    });
                    renderer3DCanvas.addEventListener("webglcontextrestored", () => {
                        renderer3DContextLost = false;
                        renderer3DGl = null;
                        renderer3DProgram = null;
                        renderer3DPbrProgram = null;
                        renderer3DPbrAttempted = false;
                        renderer3DPbrState = 0;
                        renderer3DPbrFailure = 0;
                        renderer3DPbrAttemptCount = 0;
                        renderer3DModelPaletteTexture = null;
                        renderer3DModelPaletteCachedAnimator = 0;
                        renderer3DModelPaletteCachedRevision = 0;
                        renderer3DShadowProgram = null;
                        renderer3DPostProgram = null;
                        renderer3DVfxProgram = null;
                        renderer3DDepthProgram = null;
                        renderer3DParticleQuadBuffer = null;
                        renderer3DParticleQuadIndexBuffer = null;
                        renderer3DShadowTexture = null;
                        renderer3DShadowFramebuffer = null;
                        renderer3DSceneTexture = null;
                        renderer3DSceneFramebuffer = null;
                        renderer3DMsaaTarget = null;
                        renderer3DEffectiveSamples = 1;
                        renderer3DSceneDepth = null;
                        renderer3DLinearDepthTexture = null;
                        renderer3DLinearDepthFramebuffer = null;
                        renderer3DDistortionTexture = null;
                        renderer3DDistortionFramebuffer = null;
                        renderer3DDistortionScratchTexture = null;
                        renderer3DDistortionScratchFramebuffer = null;
                        renderer3DBloomTextureA = null;
                        renderer3DBloomFramebufferA = null;
                        renderer3DBloomTextureB = null;
                        renderer3DBloomFramebufferB = null;
                        renderer3DShadowEffective = false;
                        renderer3DHdrEffective = false;
                        renderer3DBloomEffective = false;
                        renderer3DPostEffective = false;
                        renderer3DSoftDepthEffective = 0;
                        renderer3DSoftDepthWidth = renderer3DSoftDepthHeight = renderer3DSoftDepthBytes = 0;
                        renderer3DDistortionEffective = 0;
                        renderer3DDistortionWidth = renderer3DDistortionHeight = renderer3DDistortionBytes = 0;
                        renderer3DGpuParticlePipeline = null;
                        renderer3DGpuParticlePipelineAttempted = false;
                        renderer3DGpuParticleBackendAvailable = false;
                        renderer3DMultipassActive = false;
                        renderer3DM5AppliedRevision = 0;
                        renderer3DReleaseSubmissions(0, renderer3DSubmissionCount);
                        renderer3DReleaseGpuParticleFrameSystems();
                        renderer3DSubmissionCount = renderer3DPaletteSnapshotCount = 0;
                        renderer3DSubmissionGroupActive = false;
                        renderer3DSubmissionGroupToken = 0;
                        renderer3DAnisotropyAttempted = false;
                        for (const batch of renderer3DParticleBatches.values()) {
                            batch.gpu = null;
                            batch.uploadedRevision = 0;
                        }
                        for (const batch of renderer3DRibbonBatches.values()) {
                            batch.gpu = null;
                            batch.uploadedRevision = 0;
                        }
                    });
                }
                return context;
            }

            function renderer3DRecordFailure(stage, detail, path = "") {
                const failure = { stage, detail, path };
                window.__smileWeb.rendererFailure = failure;
                const failures = window.__smileWeb.rendererFailures || (window.__smileWeb.rendererFailures = []);
                if (failures.length < 8) failures.push(failure);
            }

            function renderer3DCompile(gl, type, source) {
                const shader = gl.createShader(type);
                gl.shaderSource(shader, source);
                gl.compileShader(shader);
                if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
                    const detail = gl.getShaderInfoLog(shader) || "unknown shader error";
                    renderer3DRecordFailure("shader", detail);
                    gl.deleteShader(shader);
                    throw new Error(`Renderer3D WebGL2 shader compilation failed: ${detail}`);
                }
                return shader;
            }

            function renderer3DInitialize() {
                const gl = renderer3DContext();
                if (!gl) { renderer3DLastError = 20; return false; }
                if (!renderer3DProgram) {
                    const vertex = renderer3DCompile(gl, gl.VERTEX_SHADER, `#version 300 es
                        precision highp float;
                        layout(location=0) in vec3 position;
                        layout(location=1) in vec3 normal;
                        layout(location=2) in vec2 textureUv;
                        layout(location=3) in vec4 joints;
                        layout(location=4) in vec4 weights;
                        uniform mat4 model;
                        uniform mat4 mvp;
                        uniform mat4 shadowMvp;
                        uniform mat4 bones[32];
                        uniform float skinning;
                        uniform highp sampler2D modelPalette;
                        uniform float modelSkinning;
                        out vec3 surfaceNormal;
                        out vec3 worldPosition;
                        out vec2 surfaceUv;
                        out vec4 shadowPosition;
                        mat4 modelBone(int index){return mat4(texelFetch(modelPalette,ivec2(0,index),0),texelFetch(modelPalette,ivec2(1,index),0),texelFetch(modelPalette,ivec2(2,index),0),texelFetch(modelPalette,ivec2(3,index),0));}
                        void main(){vec4 localPosition=vec4(position,1.0);vec3 localNormal=normal;if(skinning>.5){mat4 skin=modelSkinning>.5?modelBone(int(joints.x))*weights.x+modelBone(int(joints.y))*weights.y+modelBone(int(joints.z))*weights.z+modelBone(int(joints.w))*weights.w:bones[int(joints.x)]*weights.x+bones[int(joints.y)]*weights.y+bones[int(joints.z)]*weights.z+bones[int(joints.w)]*weights.w;localPosition=skin*localPosition;localNormal=mat3(skin)*localNormal;}vec4 world=model*localPosition;gl_Position=mvp*localPosition;worldPosition=world.xyz;shadowPosition=shadowMvp*localPosition;surfaceNormal=normalize(mat3(model)*localNormal);surfaceUv=textureUv;}`);
                    const fragment = renderer3DCompile(gl, gl.FRAGMENT_SHADER, `#version 300 es
                        precision highp float;
                        in vec3 surfaceNormal;
                        in vec3 worldPosition;
                        in vec2 surfaceUv;
                        in vec4 shadowPosition;
                        uniform vec4 tint;
                        uniform vec4 material;
                        uniform sampler2D baseTexture;
                        uniform highp sampler2DShadow shadowMap;
                        uniform vec4 shadowSettings;
                        uniform vec4 shadowLight;
                        uniform float hdrOutput;
                        out vec4 outputColor;
                        vec3 toLinear(vec3 color){return mix(color/12.92,pow((color+.055)/1.055,vec3(2.4)),step(vec3(.04045),color));}
                        float shadowValue(vec3 n,vec3 l){if(shadowSettings.x<.5||shadowPosition.w<=0.0)return 1.0;vec3 projected=shadowPosition.xyz/shadowPosition.w;vec2 uv=projected.xy*.5+.5;float depth=projected.z*.5+.5;if(any(lessThan(uv,vec2(0.0)))||any(greaterThan(uv,vec2(1.0)))||depth<0.0||depth>1.0)return 1.0;float bias=shadowSettings.y+shadowSettings.z*(1.0-max(dot(n,l),0.0));float sum=0.0;for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)sum+=texture(shadowMap,vec3(uv+vec2(x,y)*shadowSettings.w,depth-bias));return sum/9.0;}
                        void main(){vec4 base=tint;if(material.x>.5)base*=texture(baseTexture,surfaceUv);if(material.w>=0.0&&base.a<material.w)discard;vec3 n=normalize(surfaceNormal),l=normalize(vec3(-.35,.8,-.45));vec3 shadowL=shadowLight.w>1.5?normalize(shadowLight.xyz-worldPosition):normalize(shadowLight.xyz);float lit=.28+.72*max(0.0,dot(n,l))*shadowValue(n,shadowL);float light=material.y>.5?1.0:lit+material.z;vec3 color=base.rgb*light;outputColor=vec4(hdrOutput>.5?max(toLinear(color),vec3(0.0)):color,base.a);}`);
                    const program = gl.createProgram();
                    gl.attachShader(program, vertex); gl.attachShader(program, fragment); gl.linkProgram(program);
                    gl.deleteShader(vertex); gl.deleteShader(fragment);
                    if (!gl.getProgramParameter(program, gl.LINK_STATUS))
                        throw new Error(`Renderer3D WebGL2 program link failed: ${gl.getProgramInfoLog(program) || "unknown link error"}`);
                    renderer3DProgram = {
                        handle: program,
                        model: gl.getUniformLocation(program, "model"),
                        mvp: gl.getUniformLocation(program, "mvp"),
                        shadowMvp: gl.getUniformLocation(program, "shadowMvp"),
                        tint: gl.getUniformLocation(program, "tint"),
                        material: gl.getUniformLocation(program, "material"),
                        baseTexture: gl.getUniformLocation(program, "baseTexture"),
                        shadowMap: gl.getUniformLocation(program, "shadowMap"),
                        shadowSettings: gl.getUniformLocation(program, "shadowSettings"),
                        shadowLight: gl.getUniformLocation(program, "shadowLight"),
                        hdrOutput: gl.getUniformLocation(program, "hdrOutput"),
                        bones: gl.getUniformLocation(program, "bones[0]"),
                        skinning: gl.getUniformLocation(program, "skinning"),
                        modelPalette: gl.getUniformLocation(program, "modelPalette"),
                        modelSkinning: gl.getUniformLocation(program, "modelSkinning")
                    };
                }
                if (!renderer3DDepthProgram) {
                    let vertex = null, fragment = null, handle = null;
                    try {
                        vertex = renderer3DCompile(gl, gl.VERTEX_SHADER, `#version 300 es
                            precision highp float;
                            out vec2 surfaceUv;
                            void main(){vec2 p=gl_VertexID==0?vec2(-1.0,-1.0):(gl_VertexID==1?vec2(3.0,-1.0):vec2(-1.0,3.0));gl_Position=vec4(p,0.0,1.0);surfaceUv=p*.5+.5;}`);
                        fragment = renderer3DCompile(gl, gl.FRAGMENT_SHADER, `#version 300 es
                            precision highp float;
                            in vec2 surfaceUv;
                            uniform sampler2D sourceDepth;
                            uniform vec2 nearFar;
                            uniform float packedMode;
                            out vec4 outputDepth;
                            vec4 packDepth(float value){vec4 encoded=fract(value*vec4(1.0,255.0,65025.0,16581375.0));encoded-=encoded.yzww*vec4(1.0/255.0,1.0/255.0,1.0/255.0,0.0);return encoded;}
                            void main(){float z=texture(sourceDepth,surfaceUv).r*2.0-1.0;float linear=(2.0*nearFar.x*nearFar.y)/max(nearFar.y+nearFar.x-z*(nearFar.y-nearFar.x),.000001);outputDepth=packedMode>.5?packDepth(clamp(linear/nearFar.y,0.0,1.0)):vec4(linear,0.0,0.0,1.0);}`);
                        handle = gl.createProgram();gl.attachShader(handle,vertex);gl.attachShader(handle,fragment);gl.linkProgram(handle);
                        if(!gl.getProgramParameter(handle,gl.LINK_STATUS))throw new Error(gl.getProgramInfoLog(handle)||"soft-depth link error");
                        renderer3DDepthProgram={handle,sourceDepth:gl.getUniformLocation(handle,"sourceDepth"),nearFar:gl.getUniformLocation(handle,"nearFar"),packedMode:gl.getUniformLocation(handle,"packedMode")};
                    } catch (_) {
                        if(handle)gl.deleteProgram(handle);renderer3DDepthProgram=null;renderer3DSoftDepthFallbackReason=2;
                    } finally {
                        if(vertex)gl.deleteShader(vertex);if(fragment)gl.deleteShader(fragment);
                    }
                }
                if (!renderer3DVfxProgram) {
                    let particleVertex = null, ribbonVertex = null, fragment = null;
                    let particleHandle = null, ribbonHandle = null;
                    try {
                        particleVertex = renderer3DCompile(gl, gl.VERTEX_SHADER, `#version 300 es
                            precision highp float;
                            layout(location=0) in vec2 corner;
                            layout(location=1) in vec2 textureUv;
                            layout(location=2) in vec4 positionSize;
                            layout(location=3) in vec4 instanceColor;
                            layout(location=4) in vec4 rotationUv;
                            uniform mat4 viewProjection;
                            uniform vec3 cameraRight;
                            uniform vec3 cameraUp;
                            uniform vec2 atlasScale;
                            out vec2 surfaceUv;
                            out vec4 surfaceColor;
                            void main(){float c=cos(rotationUv.x),s=sin(rotationUv.x);vec2 q=vec2(corner.x*c-corner.y*s,corner.x*s+corner.y*c)*positionSize.w;vec3 world=positionSize.xyz+cameraRight*q.x+cameraUp*q.y;gl_Position=viewProjection*vec4(world,1.0);surfaceUv=rotationUv.yz+textureUv*atlasScale;surfaceColor=instanceColor;}`);
                        ribbonVertex = renderer3DCompile(gl, gl.VERTEX_SHADER, `#version 300 es
                            precision highp float;
                            layout(location=0) in vec3 position;
                            layout(location=1) in vec2 textureUv;
                            layout(location=2) in vec4 vertexColor;
                            uniform mat4 viewProjection;
                            out vec2 surfaceUv;
                            out vec4 surfaceColor;
                            void main(){gl_Position=viewProjection*vec4(position,1.0);surfaceUv=textureUv;surfaceColor=vertexColor;}`);
                        fragment = renderer3DCompile(gl, gl.FRAGMENT_SHADER, `#version 300 es
                            precision highp float;
                            in vec2 surfaceUv;
                            in vec4 surfaceColor;
                            uniform sampler2D effectTexture;
                            uniform vec4 materialColor;
                            uniform float textureEnabled;
                            uniform float emissive;
                            uniform float hdrOutput;
                            uniform sampler2D sceneDepthTexture;
                            uniform vec4 softDepthSettings;
                            uniform vec2 targetSize;
                            uniform float softDepthFormat;
                            uniform vec4 distortionSettings;
                            uniform vec2 distortionFlow;
                            uniform float distortionFormat;
                            out vec4 outputColor;
                            vec3 toLinear(vec3 color){return mix(color/12.92,pow((color+.055)/1.055,vec3(2.4)),step(vec3(.04045),color));}
                            float unpackDepth(vec4 value){return dot(value,vec4(1.0,1.0/255.0,1.0/65025.0,1.0/16581375.0));}
                            float linearDepth(float depth){float z=depth*2.0-1.0;return(2.0*softDepthSettings.z*softDepthSettings.w)/max(softDepthSettings.w+softDepthSettings.z-z*(softDepthSettings.w-softDepthSettings.z),.000001);}
                            void main(){vec4 sampled=textureEnabled>.5?texture(effectTexture,surfaceUv):vec4(1.0);vec4 base=surfaceColor*materialColor*sampled;if(softDepthSettings.x>.5){vec4 stored=texture(sceneDepthTexture,gl_FragCoord.xy/targetSize);float scene=softDepthFormat<1.5?unpackDepth(stored)*softDepthSettings.w:stored.r;float distance=max(scene-linearDepth(gl_FragCoord.z),0.0);base.a*=clamp(distance/max(softDepthSettings.y,.0001),0.0,1.0);}if(distortionSettings.x>.5){float wave=.65+.35*sin((surfaceUv.x+surfaceUv.y)*max(distortionSettings.z,.01)*6.283185+distortionSettings.w);vec2 flow=length(distortionFlow)>.0001?normalize(distortionFlow):vec2(0.0,1.0);vec2 delta=flow*distortionSettings.y*base.a*wave;outputColor=distortionFormat<1.5?vec4(delta/.06+.5,0.0,base.a):vec4(delta,0.0,base.a);return;}vec3 color=hdrOutput>.5?toLinear(clamp(base.rgb,0.0,1.0))*max(emissive,1.0):clamp(base.rgb*max(emissive,1.0),0.0,1.0);outputColor=vec4(color,base.a);}`);
                        particleHandle = gl.createProgram();
                        gl.attachShader(particleHandle, particleVertex);gl.attachShader(particleHandle, fragment);gl.linkProgram(particleHandle);
                        if (!gl.getProgramParameter(particleHandle, gl.LINK_STATUS))
                            throw new Error(gl.getProgramInfoLog(particleHandle) || "particle VFX link error");
                        ribbonHandle = gl.createProgram();
                        gl.attachShader(ribbonHandle, ribbonVertex);gl.attachShader(ribbonHandle, fragment);gl.linkProgram(ribbonHandle);
                        if (!gl.getProgramParameter(ribbonHandle, gl.LINK_STATUS))
                            throw new Error(gl.getProgramInfoLog(ribbonHandle) || "ribbon VFX link error");
                        const describe = handle => {
                            const result = {handle};
                            for (const name of ["viewProjection","cameraRight","cameraUp","atlasScale","effectTexture","materialColor","textureEnabled","emissive","hdrOutput","sceneDepthTexture","softDepthSettings","targetSize","softDepthFormat","distortionSettings","distortionFlow","distortionFormat"])
                                result[name] = gl.getUniformLocation(handle, name);
                            return result;
                        };
                        renderer3DVfxProgram = {particle:describe(particleHandle),ribbon:describe(ribbonHandle)};
                        renderer3DParticleQuadBuffer = gl.createBuffer();
                        gl.bindBuffer(gl.ARRAY_BUFFER, renderer3DParticleQuadBuffer);
                        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([
                            -.5,-.5,0,1, -.5,.5,0,0, .5,.5,1,0, .5,-.5,1,1
                        ]), gl.STATIC_DRAW);
                        renderer3DParticleQuadIndexBuffer = gl.createBuffer();
                        gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, renderer3DParticleQuadIndexBuffer);
                        gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint16Array([0,1,2,0,2,3]), gl.STATIC_DRAW);
                    } catch (_) {
                        if (particleHandle) gl.deleteProgram(particleHandle);
                        if (ribbonHandle) gl.deleteProgram(ribbonHandle);
                        renderer3DVfxProgram = null;
                        renderer3DLastError = 57;
                    } finally {
                        if (particleVertex) gl.deleteShader(particleVertex);
                        if (ribbonVertex) gl.deleteShader(ribbonVertex);
                        if (fragment) gl.deleteShader(fragment);
                    }
                }
                if (!renderer3DAnisotropyAttempted) {
                    renderer3DAnisotropyAttempted = true;
                    renderer3DAnisotropy = gl.getExtension("EXT_texture_filter_anisotropic") ||
                        gl.getExtension("WEBKIT_EXT_texture_filter_anisotropic") || null;
                    renderer3DMaximumAnisotropy = renderer3DAnisotropy
                        ? Math.max(1, Math.min(16, gl.getParameter(renderer3DAnisotropy.MAX_TEXTURE_MAX_ANISOTROPY_EXT) || 1))
                        : 1;
                }
                if (!renderer3DPbrAttempted) {
                    renderer3DPbrAttempted = true;
                    renderer3DPbrAttemptCount += 1;
                    let pbrVertex = null;
                    let pbrFragment = null;
                    let pbrHandle = null;
                    try {
                        if (globalThis.SMILE_TEST_RENDERER3D_FORCE_PBR_FAILURE)
                            throw new Error("forced Renderer3D PBR test failure");
                        pbrVertex = renderer3DCompile(gl, gl.VERTEX_SHADER, `#version 300 es
                            precision highp float;
                            layout(location=0) in vec3 position;
                            layout(location=1) in vec3 normal;
                            layout(location=2) in vec2 textureUv;
                            layout(location=3) in vec4 joints;
                            layout(location=4) in vec4 weights;
                            layout(location=5) in vec4 tangent;
                            uniform mat4 model;
                            uniform mat4 mvp;
                            uniform mat3 normalMatrix;
                            uniform mat4 shadowMvp;
                            uniform mat4 bones[32];
                            uniform float skinning;
                            uniform highp sampler2D modelPalette;
                            uniform float modelSkinning;
                            out vec3 worldPosition;
                            out vec3 surfaceNormal;
                            out vec4 surfaceTangent;
                            out vec2 surfaceUv;
                            out vec4 shadowPosition;
                            mat4 modelBone(int index){return mat4(texelFetch(modelPalette,ivec2(0,index),0),texelFetch(modelPalette,ivec2(1,index),0),texelFetch(modelPalette,ivec2(2,index),0),texelFetch(modelPalette,ivec2(3,index),0));}
                            void main(){vec4 localPosition=vec4(position,1.0);vec3 localNormal=normal;vec4 localTangent=tangent;if(skinning>.5){mat4 skin=modelSkinning>.5?modelBone(int(joints.x))*weights.x+modelBone(int(joints.y))*weights.y+modelBone(int(joints.z))*weights.z+modelBone(int(joints.w))*weights.w:bones[int(joints.x)]*weights.x+bones[int(joints.y)]*weights.y+bones[int(joints.z)]*weights.z+bones[int(joints.w)]*weights.w;localPosition=skin*localPosition;localNormal=mat3(skin)*localNormal;localTangent.xyz=mat3(skin)*localTangent.xyz;}vec4 world=model*localPosition;vec3 n=normalize(normalMatrix*localNormal);vec3 t=mat3(model)*localTangent.xyz;t=normalize(t-n*dot(n,t));gl_Position=mvp*localPosition;worldPosition=world.xyz;surfaceNormal=n;surfaceTangent=vec4(t,localTangent.w);surfaceUv=textureUv;shadowPosition=shadowMvp*localPosition;}`);
                        pbrFragment = renderer3DCompile(gl, gl.FRAGMENT_SHADER, `#version 300 es
                            precision highp float;
                            in vec3 worldPosition;
                            in vec3 surfaceNormal;
                            in vec4 surfaceTangent;
                            in vec2 surfaceUv;
                            in vec4 shadowPosition;
                            uniform vec4 objectColor;
                            uniform vec4 baseFactor;
                            uniform vec4 surfaceFactors;
                            uniform vec4 emissiveAlpha;
                            uniform vec4 textureFlags;
                            uniform vec3 cameraPosition;
                            uniform vec4 ambientLight;
                            uniform vec4 directionalDirection;
                            uniform vec4 directionalColor;
                            uniform vec4 localPositionType[4];
                            uniform vec4 localDirectionRange[4];
                            uniform vec4 localColorIntensity[4];
                            uniform vec4 localCone[4];
                            uniform sampler2D baseTexture;
                            uniform sampler2D normalTexture;
                            uniform sampler2D ormTexture;
                            uniform sampler2D emissiveTexture;
                            uniform highp sampler2DShadow shadowMap;
                            uniform vec4 shadowSettings;
                            uniform vec2 shadowSelection;
                            uniform float hdrOutput;
                            uniform float materialInspection;
                            out vec4 outputColor;
                            const float PI=3.14159265359;
                            vec3 fresnelSchlick(vec3 f0,float value){return f0+(vec3(1.0)-f0)*pow(1.0-value,5.0);}
                            float distribution(float nh,float rough){float a=rough*rough;float a2=a*a;float q=nh*nh*(a2-1.0)+1.0;return a2/max(PI*q*q,.0001);}
                            float geometryOne(float nv,float rough){float k=(rough+1.0)*(rough+1.0)/8.0;return nv/max(nv*(1.0-k)+k,.0001);}
                            vec3 shade(vec3 n,vec3 v,vec3 l,vec3 radiance,vec3 base,float metal,float rough){vec3 halfDirection=normalize(v+l);float nl=max(dot(n,l),0.0);float nv=max(dot(n,v),0.0);float vh=max(dot(v,halfDirection),0.0);float nh=max(dot(n,halfDirection),0.0);vec3 f0=mix(vec3(.04),base,metal);vec3 fresnel=fresnelSchlick(f0,vh);float geometry=geometryOne(nv,rough)*geometryOne(nl,rough);vec3 specular=distribution(nh,rough)*geometry*fresnel/max(4.0*nv*nl,.0001);vec3 diffuse=(vec3(1.0)-fresnel)*(1.0-metal);return (diffuse*base/PI+specular)*radiance*nl;}
                            float shadowValue(vec3 n,vec3 l){if(shadowSettings.x<.5||shadowPosition.w<=0.0)return 1.0;vec3 projected=shadowPosition.xyz/shadowPosition.w;vec2 uv=projected.xy*.5+.5;float depth=projected.z*.5+.5;if(any(lessThan(uv,vec2(0.0)))||any(greaterThan(uv,vec2(1.0)))||depth<0.0||depth>1.0)return 1.0;float bias=shadowSettings.y+shadowSettings.z*(1.0-max(dot(n,l),0.0));float sum=0.0;for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)sum+=texture(shadowMap,vec3(uv+vec2(x,y)*shadowSettings.w,depth-bias));return sum/9.0;}
                            vec3 applyLdrOutputTransfer(vec3 color){vec3 low=color*12.92;vec3 high=1.055*pow(max(color,vec3(0.0)),vec3(1.0/2.4))-.055;return mix(low,high,step(vec3(.0031308),color));}
                            void main(){vec4 sampled=textureFlags.x>.5?texture(baseTexture,surfaceUv):vec4(1.0);vec4 base=baseFactor*objectColor*sampled;if(emissiveAlpha.w>=0.0&&base.a<emissiveAlpha.w)discard;vec3 n=normalize(surfaceNormal);if(!gl_FrontFacing)n=-n;vec3 t=normalize(surfaceTangent.xyz-n*dot(n,surfaceTangent.xyz));vec3 bitangent=normalize(cross(n,t)*surfaceTangent.w);if(textureFlags.y>.5){vec3 mapped=texture(normalTexture,surfaceUv).xyz*2.0-1.0;mapped.xy*=surfaceFactors.z;n=normalize(t*mapped.x+bitangent*mapped.y+n*mapped.z);}vec3 orm=textureFlags.z>.5?texture(ormTexture,surfaceUv).rgb:vec3(1.0);float occlusion=mix(1.0,orm.r,surfaceFactors.w);float rough=clamp(surfaceFactors.y*orm.g,.045,1.0);float metal=clamp(surfaceFactors.x*orm.b,0.0,1.0);vec3 viewDirection=normalize(cameraPosition-worldPosition);vec3 color=ambientLight.rgb*ambientLight.a*base.rgb*occlusion;if(directionalDirection.w>.5){vec3 lightDirection=normalize(directionalDirection.xyz);float factor=shadowSelection.x<1.5?shadowValue(n,lightDirection):1.0;color+=shade(n,viewDirection,lightDirection,directionalColor.rgb*directionalColor.a*factor,base.rgb,metal,rough);}for(int light=0;light<4;light++){float type=localPositionType[light].w;if(type>.5){vec3 delta=localPositionType[light].xyz-worldPosition;float distanceToLight=length(delta);float range=max(localDirectionRange[light].w,.0001);if(distanceToLight<range){vec3 lightDirection=delta/max(distanceToLight,.0001);float ratio=distanceToLight/range;float attenuation=pow(clamp(1.0-ratio*ratio,0.0,1.0),2.0)/(1.0+2.0*ratio*ratio);if(type>1.5){float spot=dot(-lightDirection,normalize(localDirectionRange[light].xyz));attenuation*=smoothstep(localCone[light].y,localCone[light].x,spot);}float factor=shadowSelection.x>1.5&&abs(shadowSelection.y-float(light))<.5?shadowValue(n,lightDirection):1.0;color+=shade(n,viewDirection,lightDirection,localColorIntensity[light].rgb*localColorIntensity[light].a*attenuation*factor,base.rgb,metal,rough);}}}vec3 emissive=emissiveAlpha.rgb*(textureFlags.w>.5?texture(emissiveTexture,surfaceUv).rgb:vec3(1.0));vec3 finalColor=max(color+emissive,vec3(0.0));if(materialInspection>.5){if(materialInspection<1.5)finalColor=base.rgb;else if(materialInspection<2.5)finalColor=n*.5+.5;else if(materialInspection<3.5)finalColor=vec3(rough);else if(materialInspection<4.5)finalColor=vec3(metal);else if(materialInspection<5.5)finalColor=vec3(occlusion);else finalColor=emissive;}outputColor=vec4(hdrOutput>.5?finalColor:clamp(applyLdrOutputTransfer(finalColor),0.0,1.0),base.a);}`);
                        pbrHandle = gl.createProgram();
                        gl.attachShader(pbrHandle, pbrVertex);
                        gl.attachShader(pbrHandle, pbrFragment);
                        gl.linkProgram(pbrHandle);
                        if (!gl.getProgramParameter(pbrHandle, gl.LINK_STATUS))
                            throw new Error(gl.getProgramInfoLog(pbrHandle) || "unknown PBR program link error");
                        renderer3DPbrProgram = { handle: pbrHandle };
                        for (const name of ["model","mvp","normalMatrix","bones[0]","skinning","modelPalette","modelSkinning","objectColor",
                            "baseFactor","surfaceFactors","emissiveAlpha","textureFlags","cameraPosition",
                            "ambientLight","directionalDirection","directionalColor","localPositionType[0]",
                            "localDirectionRange[0]","localColorIntensity[0]","localCone[0]","baseTexture",
                            "normalTexture","ormTexture","emissiveTexture","shadowMvp","shadowMap",
                            "shadowSettings","shadowSelection","hdrOutput","materialInspection"])
                            renderer3DPbrProgram[name] = gl.getUniformLocation(pbrHandle, name);
                        renderer3DPbrState = 1;
                        renderer3DPbrFailure = 0;
                    } catch (_) {
                        if (pbrHandle) gl.deleteProgram(pbrHandle);
                        renderer3DPbrProgram = null;
                        renderer3DPbrState = 2;
                        renderer3DPbrFailure = 44;
                        renderer3DLastError = 44;
                    } finally {
                        if (pbrVertex) gl.deleteShader(pbrVertex);
                        if (pbrFragment) gl.deleteShader(pbrFragment);
                    }
                }
                if (renderer3DShadowRequested && !renderer3DShadowProgram) {
                    let vertex = null, fragment = null, handle = null;
                    try {
                        vertex = renderer3DCompile(gl, gl.VERTEX_SHADER, `#version 300 es
                            precision highp float;
                            layout(location=0) in vec3 position;
                            layout(location=2) in vec2 textureUv;
                            layout(location=3) in vec4 joints;
                            layout(location=4) in vec4 weights;
                            uniform mat4 mvp;
                            uniform mat4 bones[32];
                            uniform float skinning;
                            uniform highp sampler2D modelPalette;
                            uniform float modelSkinning;
                            out vec2 surfaceUv;
                            mat4 modelBone(int index){return mat4(texelFetch(modelPalette,ivec2(0,index),0),texelFetch(modelPalette,ivec2(1,index),0),texelFetch(modelPalette,ivec2(2,index),0),texelFetch(modelPalette,ivec2(3,index),0));}
                            void main(){vec4 localPosition=vec4(position,1.0);if(skinning>.5){mat4 skin=modelSkinning>.5?modelBone(int(joints.x))*weights.x+modelBone(int(joints.y))*weights.y+modelBone(int(joints.z))*weights.z+modelBone(int(joints.w))*weights.w:bones[int(joints.x)]*weights.x+bones[int(joints.y)]*weights.y+bones[int(joints.z)]*weights.z+bones[int(joints.w)]*weights.w;localPosition=skin*localPosition;}gl_Position=mvp*localPosition;surfaceUv=textureUv;}`);
                        fragment = renderer3DCompile(gl, gl.FRAGMENT_SHADER, `#version 300 es
                            precision highp float;
                            in vec2 surfaceUv;
                            uniform sampler2D baseTexture;
                            uniform vec3 alphaSettings;
                            out vec4 outputColor;
                            void main(){float alpha=alphaSettings.z;if(alphaSettings.x>.5)alpha*=texture(baseTexture,surfaceUv).a;if(alphaSettings.y>=0.0&&alpha<alphaSettings.y)discard;outputColor=vec4(0.0);}`);
                        handle = gl.createProgram();gl.attachShader(handle,vertex);gl.attachShader(handle,fragment);gl.linkProgram(handle);
                        if (!gl.getProgramParameter(handle,gl.LINK_STATUS)) throw new Error(gl.getProgramInfoLog(handle)||"shadow link error");
                        renderer3DShadowProgram={handle};
                        for(const name of ["mvp","bones[0]","skinning","modelPalette","modelSkinning","baseTexture","alphaSettings"])
                            renderer3DShadowProgram[name]=gl.getUniformLocation(handle,name);
                    } catch (_) {
                        if(handle)gl.deleteProgram(handle);renderer3DShadowProgram=null;renderer3DFallbackFlags|=2;
                    } finally {if(vertex)gl.deleteShader(vertex);if(fragment)gl.deleteShader(fragment);}
                }
                if ((renderer3DPostRequested || renderer3DRequestedSamples > 1 || renderer3DSoftDepthRequested || renderer3DDistortionRequested ||
                    renderer3DBackdropTexture !== 0) && !renderer3DPostProgram) {
                    let vertex = null, fragment = null, handle = null;
                    try {
                        vertex = renderer3DCompile(gl, gl.VERTEX_SHADER, `#version 300 es
                            precision highp float;out vec2 surfaceUv;void main(){vec2 p=gl_VertexID==0?vec2(-1.0,-1.0):(gl_VertexID==1?vec2(3.0,-1.0):vec2(-1.0,3.0));gl_Position=vec4(p,0.0,1.0);surfaceUv=p*.5+.5;}`);
                        fragment = renderer3DCompile(gl, gl.FRAGMENT_SHADER, `#version 300 es
                            precision highp float;in vec2 surfaceUv;uniform sampler2D sceneTexture;uniform sampler2D bloomTexture;uniform vec4 first;uniform vec4 second;out vec4 outputColor;
                            vec3 sampleBlur(vec2 axis){vec3 c=texture(sceneTexture,surfaceUv).rgb*.4;c+=(texture(sceneTexture,surfaceUv+axis).rgb+texture(sceneTexture,surfaceUv-axis).rgb)*.24;c+=(texture(sceneTexture,surfaceUv+axis*2.0).rgb+texture(sceneTexture,surfaceUv-axis*2.0).rgb)*.06;return c;}
                            vec3 tone(vec3 x){return clamp((x*(2.51*x+.03))/(x*(2.43*x+.59)+.14),0.0,1.0);}vec3 encode(vec3 c){return mix(c*12.92,1.055*pow(max(c,vec3(0.0)),vec3(1.0/2.4))-.055,step(vec3(.0031308),c));}vec3 decode(vec3 c){return mix(c/12.92,pow((c+.055)/1.055,vec3(2.4)),step(vec3(.04045),c));}
                            void main(){if(first.x<.5){vec3 c=texture(sceneTexture,surfaceUv).rgb;float bright=max(c.r,max(c.g,c.b));outputColor=vec4(bright>=first.w?c:vec3(0.0),1.0);return;}if(first.x<1.5){outputColor=vec4(sampleBlur(vec2(first.y,0.0)),1.0);return;}if(first.x<2.5){outputColor=vec4(sampleBlur(vec2(0.0,first.z)),1.0);return;}if(first.x>5.5){vec4 c=texture(sceneTexture,vec2(surfaceUv.x,1.0-surfaceUv.y));outputColor=vec4(first.x<6.5?decode(c.rgb):c.rgb,c.a);return;}if(first.x>4.5){vec2 stored=texture(bloomTexture,surfaceUv).rg;vec2 delta=second.x<1.5?(stored-.5)*.06:stored;outputColor=texture(sceneTexture,clamp(surfaceUv+clamp(delta,vec2(-.03),vec2(.03)),vec2(0.0),vec2(1.0)));return;}if(first.x>3.5){outputColor=texture(sceneTexture,surfaceUv);return;}vec3 scene=texture(sceneTexture,surfaceUv).rgb;vec3 bloom=texture(bloomTexture,surfaceUv).rgb*second.x;outputColor=vec4(encode(tone(max((scene+bloom)*second.y,vec3(0.0)))),1.0);}`);
                        handle=gl.createProgram();gl.attachShader(handle,vertex);gl.attachShader(handle,fragment);gl.linkProgram(handle);
                        if(!gl.getProgramParameter(handle,gl.LINK_STATUS))throw new Error(gl.getProgramInfoLog(handle)||"post link error");
                        renderer3DPostProgram={handle};for(const name of ["sceneTexture","bloomTexture","first","second"])
                            renderer3DPostProgram[name]=gl.getUniformLocation(handle,name);
                    } catch (error) {
                        console.warn("Renderer3D post-processing shader unavailable:", error);
                        if(handle)gl.deleteProgram(handle);renderer3DPostProgram=null;renderer3DFallbackFlags|=4|32|64|128;
                    } finally {if(vertex)gl.deleteShader(vertex);if(fragment)gl.deleteShader(fragment);}
                }
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
                const vertices = new Float32Array(vertexCount * 20);
                for (let index = 0; index < vertexCount; index += 1) {
                    vertices[index * 20 + 12] = 1;
                    vertices[index * 20 + 16] = 1;
                    vertices[index * 20 + 19] = 1;
                }
                renderer3DMeshes.set(handle, {
                    vertexCount, indexCount, vertices,
                    indices: new Uint32Array(indexCount), committed: false, explicitNormals: false,
                    maxJoint: 0, vertexBuffer: null, indexBuffer: null, inFlight: 0
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
                let count = renderer3DMeshes.get(handle).inFlight;
                for (const object of renderer3DObjects.values()) if (object.mesh === handle) count += 1;
                return count;
            }

            function renderer3DTextureReferenceCount(handle) {
                handle = safe(handle);
                if (!renderer3DTextures.has(handle)) return 0;
                let count = renderer3DTextures.get(handle).inFlight;
                if (renderer3DBackdropTexture === handle) count += 1;
                for (const material of renderer3DMaterials.values()) {
                    if (material.kind === 0 && material.texture === handle) count += 1;
                    else if (material.kind === 1 && material.textures.includes(handle)) count += 1;
                }
                return count;
            }

            function renderer3DMaterialReferenceCount(handle) {
                handle = safe(handle);
                if (!renderer3DMaterials.has(handle)) return 0;
                let count = 0;
                for (const object of renderer3DObjects.values()) if (object.material === handle) count += 1;
                for (const batch of renderer3DParticleBatches.values()) if (batch.material === handle) count += 1;
                for (const batch of renderer3DRibbonBatches.values()) if (batch.material === handle) count += 1;
                for (const system of renderer3DGpuParticleSystems.values()) if (system.material === handle) count += 1;
                return count;
            }

            function renderer3DSetVertex(mesh, index, x, y, z) {
                index = safe(index);
                if (index < 0 || index >= mesh.vertexCount) { renderer3DLastError = 5; return false; }
                if (mesh.inFlight) { renderer3DLastError = 53; return false; }
                const offset = index * 20;
                mesh.vertices[offset] = safe(x); mesh.vertices[offset + 1] = safe(y); mesh.vertices[offset + 2] = safe(z);
                mesh.committed = false;
                return true;
            }

            function renderer3DSetUv(mesh, index, u, v) {
                index = safe(index);
                if (index < 0 || index >= mesh.vertexCount) { renderer3DLastError = 5; return false; }
                if (mesh.inFlight) { renderer3DLastError = 53; return false; }
                const offset = index * 20;
                mesh.vertices[offset + 6] = safe(u) / 1000;
                mesh.vertices[offset + 7] = safe(v) / 1000;
                mesh.committed = false;
                return true;
            }

            function renderer3DSetNormal(mesh, index, x, y, z) {
                index = safe(index);
                if (index < 0 || index >= mesh.vertexCount) { renderer3DLastError = 5; return false; }
                if (mesh.inFlight) { renderer3DLastError = 53; return false; }
                const offset = index * 20;
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
                if(mesh.inFlight){renderer3DLastError=53;return false;}
                if(index<0||index>=mesh.vertexCount||weights.reduce((a,b)=>a+b,0)!==1000||
                    joints.some(value=>value<0||value>=32)||weights.some(value=>value<0||value>1000)){
                    renderer3DLastError=34;return false;
                }
                const offset=index*20;
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
                renderer3DTextures.set(handle, {
                    image, filter, effectiveFilter: filter, wrap, pbr: false, usage: 0, requestedAnisotropy: 1,
                    effectiveAnisotropy: 1, mipLevels: 1, gpu: null, inFlight: 0
                });
                return handle;
            }

            function renderer3DCreatePbrTexture(image, usage, filter, wrap, anisotropy) {
                [usage,filter,wrap,anisotropy]=[usage,filter,wrap,anisotropy].map(safe);
                if (!imageLoadedRaw(image) || renderer3DTextures.size >= 128 ||
                    image.entry.width > 8192 || image.entry.height > 8192 ||
                    usage < 1 || usage > 2 || filter < 0 || filter > 3 || wrap < 0 || wrap > 1 ||
                    anisotropy < 1 || anisotropy > 16) {
                    imageRelease(image); renderer3DLastError = 38; return 0;
                }
                renderer3DInitialize();
                const handle=renderer3DHandle();
                const mipLevels=filter>=2?Math.floor(Math.log2(Math.max(image.entry.width,image.entry.height)))+1:1;
                renderer3DTextures.set(handle,{image,filter,effectiveFilter:filter===3&&renderer3DMaximumAnisotropy===1?2:filter,
                    wrap,pbr:true,usage,requestedAnisotropy:anisotropy,
                    effectiveAnisotropy:filter===3?Math.min(anisotropy,renderer3DMaximumAnisotropy):1,
                    mipLevels,gpu:null,inFlight:0});
                return handle;
            }

            function renderer3DSetMaterial(material, alphaMode, red, green, blue, opacity, unlit, emissive, cutoff) {
                [alphaMode,red,green,blue,opacity,unlit,emissive,cutoff]=
                    [alphaMode,red,green,blue,opacity,unlit,emissive,cutoff].map(safe);
                if (!material || material.kind !== 0 || alphaMode < 0 || alphaMode > 3 || opacity < 0 || opacity > 100 ||
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
                const material={kind:0,ownerModel:0,texture,alphaMode:0,color:[1,1,1,1],unlit:false,emissive:0,cutoff:.5,softDepthMode:0,softDepthDistance:0,vfxShadingMode:0,distortionStrength:0,distortionNoiseScale:0,distortionNoiseSpeed:0,distortionFlowX:0,distortionFlowY:0};
                if (!renderer3DSetMaterial(material,alphaMode,red,green,blue,opacity,unlit,emissive,cutoff))return 0;
                const handle=renderer3DHandle();renderer3DMaterials.set(handle,material);return handle;
            }

            function renderer3DPbrTextureSetValid(handles) {
                const expected=[1,2,2,1];
                for(let index=0;index<4;index+=1){if(handles[index]===0)continue;const texture=renderer3DTextures.get(handles[index]);
                    if(!texture||!texture.pbr||texture.usage!==expected[index])return false;}
                return true;
            }

            function renderer3DSetPbrTextures(material,baseTexture,normalTexture,ormTexture,emissiveTexture,alphaMode,doubleSided) {
                [baseTexture,normalTexture,ormTexture,emissiveTexture,alphaMode,doubleSided]=
                    [baseTexture,normalTexture,ormTexture,emissiveTexture,alphaMode,doubleSided].map(safe);
                const handles=[baseTexture,normalTexture,ormTexture,emissiveTexture];
                if(!material||material.kind!==1||alphaMode<0||alphaMode>2||(doubleSided!==0&&doubleSided!==1)||
                    !renderer3DPbrTextureSetValid(handles)){renderer3DLastError=39;return false;}
                material.textures=handles;material.alphaMode=alphaMode;material.doubleSided=doubleSided!==0;
                for(let channel=0;channel<4;channel+=1)material.textureFlags[channel]=handles[channel]===0?0:1;
                material.emissiveAlpha[3]=alphaMode===1?material.cutoff:-1;return true;
            }

            function renderer3DCreatePbrMaterial(baseTexture,normalTexture,ormTexture,emissiveTexture,alphaMode,doubleSided,ownerModel=0) {
                if(!renderer3DPbrProgram||renderer3DMaterials.size>=128){renderer3DLastError=renderer3DPbrProgram?20:44;return 0;}
                const material={kind:1,ownerModel,textures:[0,0,0,0],alphaMode:0,doubleSided:false,
                    baseColor:new Float32Array([1,1,1,1]),surface:new Float32Array([0,1,1,1]),cutoff:.5,
                    emissiveAlpha:new Float32Array([0,0,0,-1]),textureFlags:new Float32Array(4)};
                if(!renderer3DSetPbrTextures(material,baseTexture,normalTexture,ormTexture,emissiveTexture,alphaMode,doubleSided))return 0;
                const handle=renderer3DHandle();renderer3DMaterials.set(handle,material);return handle;
            }

            function renderer3DSetPbrFactors(material,red,green,blue,alpha,metallic,roughness,normalStrength,occlusionStrength,cutoff) {
                [red,green,blue,alpha,metallic,roughness,normalStrength,occlusionStrength,cutoff]=
                    [red,green,blue,alpha,metallic,roughness,normalStrength,occlusionStrength,cutoff].map(safe);
                if(!material||material.kind!==1||[red,green,blue,alpha,metallic,roughness,occlusionStrength,cutoff].some(value=>value<0||value>1000)||
                    normalStrength<0||normalStrength>4000){renderer3DLastError=39;return false;}
                material.baseColor[0]=red/1000;material.baseColor[1]=green/1000;material.baseColor[2]=blue/1000;material.baseColor[3]=alpha/1000;
                material.surface[0]=metallic/1000;material.surface[1]=roughness/1000;material.surface[2]=normalStrength/1000;material.surface[3]=occlusionStrength/1000;
                material.cutoff=cutoff/1000;material.emissiveAlpha[3]=material.alphaMode===1?material.cutoff:-1;return true;
            }

            function renderer3DSetPbrEmissive(material,red,green,blue) {
                [red,green,blue]=[red,green,blue].map(safe);
                if(!material||material.kind!==1||[red,green,blue].some(value=>value<0||value>4000)){
                    renderer3DLastError=39;return false;}
                material.emissiveAlpha[0]=red/1000;material.emissiveAlpha[1]=green/1000;material.emissiveAlpha[2]=blue/1000;return true;
            }

            function renderer3DResetLights(){renderer3DAmbient.set([1,1,1,.25]);renderer3DDirectionalDirection.set([-.35,.8,-.45,1]);
                renderer3DDirectionalColor.set([1,1,1,1]);renderer3DLocalPositionType.fill(0);renderer3DLocalDirectionRange.fill(0);
                renderer3DLocalColorIntensity.fill(0);renderer3DLocalCone.fill(0);return true;}
            function renderer3DSetAmbient(red,green,blue,intensity){[red,green,blue,intensity]=[red,green,blue,intensity].map(safe);
                if([red,green,blue].some(value=>value<0||value>255)||intensity<0||intensity>1000){renderer3DLastError=43;return false;}
                renderer3DAmbient[0]=red/255;renderer3DAmbient[1]=green/255;renderer3DAmbient[2]=blue/255;renderer3DAmbient[3]=intensity/1000;return true;}
            function renderer3DSetDirectional(x,y,z,red,green,blue,intensity){[x,y,z,red,green,blue,intensity]=[x,y,z,red,green,blue,intensity].map(safe);
                const length=Math.hypot(x,y,z);if(length<.0001||[x,y,z].some(value=>value< -1000||value>1000)||
                    [red,green,blue].some(value=>value<0||value>255)||intensity<0||intensity>16000){renderer3DLastError=43;return false;}
                renderer3DDirectionalDirection[0]=x/length;renderer3DDirectionalDirection[1]=y/length;renderer3DDirectionalDirection[2]=z/length;
                renderer3DDirectionalDirection[3]=intensity===0?0:1;renderer3DDirectionalColor[0]=red/255;renderer3DDirectionalColor[1]=green/255;
                renderer3DDirectionalColor[2]=blue/255;renderer3DDirectionalColor[3]=intensity/1000;return true;}
            function renderer3DSetLocalLight(slot,type,x,y,z,red,green,blue,intensity,range){[slot,type,x,y,z,red,green,blue,intensity,range]=
                    [slot,type,x,y,z,red,green,blue,intensity,range].map(safe);if(slot<0||slot>=4||type<0||type>2){renderer3DLastError=43;return false;}
                const offset=slot*4;if(type===0){renderer3DLocalPositionType.fill(0,offset,offset+4);renderer3DLocalDirectionRange.fill(0,offset,offset+4);
                    renderer3DLocalColorIntensity.fill(0,offset,offset+4);renderer3DLocalCone.fill(0,offset,offset+4);return true;}
                if([x,y,z].some(value=>value< -1000000||value>1000000)||[red,green,blue].some(value=>value<0||value>255)||
                    intensity<0||intensity>16000||range<1||range>1000000){renderer3DLastError=43;return false;}
                renderer3DLocalPositionType[offset]=x;renderer3DLocalPositionType[offset+1]=y;renderer3DLocalPositionType[offset+2]=z;renderer3DLocalPositionType[offset+3]=type;
                renderer3DLocalColorIntensity[offset]=red/255;renderer3DLocalColorIntensity[offset+1]=green/255;renderer3DLocalColorIntensity[offset+2]=blue/255;
                renderer3DLocalColorIntensity[offset+3]=intensity/1000;renderer3DLocalDirectionRange[offset+3]=range;
                if(renderer3DLocalCone[offset]===0&&renderer3DLocalCone[offset+1]===0){renderer3DLocalDirectionRange[offset+1]=-1;
                    renderer3DLocalCone[offset]=Math.cos(20*Math.PI/180);renderer3DLocalCone[offset+1]=Math.cos(30*Math.PI/180);}return true;}
            function renderer3DSetSpotCone(slot,x,y,z,innerDegrees,outerDegrees){[slot,x,y,z,innerDegrees,outerDegrees]=
                    [slot,x,y,z,innerDegrees,outerDegrees].map(safe);const length=Math.hypot(x,y,z),offset=slot*4;
                if(slot<0||slot>=4||renderer3DLocalPositionType[offset+3]!==2||length<.0001||[x,y,z].some(value=>value< -1000||value>1000)||
                    innerDegrees<1||innerDegrees>89||outerDegrees<innerDegrees||outerDegrees>89){renderer3DLastError=43;return false;}
                renderer3DLocalDirectionRange[offset]=x/length;renderer3DLocalDirectionRange[offset+1]=y/length;renderer3DLocalDirectionRange[offset+2]=z/length;
                renderer3DLocalCone[offset]=Math.cos(innerDegrees*Math.PI/180);renderer3DLocalCone[offset+1]=Math.cos(outerDegrees*Math.PI/180);return true;}

            function renderer3DPbrTextureValue(texture,property){if(!texture||!texture.pbr){renderer3DLastError=5;return 0;}
                if(property===1)return texture.usage;if(property===2)return texture.filter;if(property===3)return texture.wrap;
                if(property===4)return texture.requestedAnisotropy;if(property===5)return texture.effectiveAnisotropy;
                if(property===6)return texture.mipLevels;renderer3DLastError=5;return 0;}
            function renderer3DPbrMaterialValue(material,property){if(!material){renderer3DLastError=5;return 0;}
                if(property===1)return material.kind;if(property>=2&&property<=5)return material.kind===1?material.textures[property-2]:0;
                if(property===6)return material.alphaMode;if(property===7)return material.kind===1&&material.doubleSided?1:0;
                if(property>=8&&property<=11)return material.kind===1?Math.round(material.surface[property-8]*1000):0;
                if(property>=12&&property<=14)return material.kind===1?Math.round(material.emissiveAlpha[property-12]*1000):0;
                if(property===15)return Math.round(material.cutoff*1000);
                if(property===16)return material.ownerModel?1:0;renderer3DLastError=5;return 0;}
            function renderer3DLightValue(query,index,property){[query,index,property]=[query,index,property].map(safe);
                if(query===1){let count=renderer3DAmbient[3]>0?1:0;if(renderer3DDirectionalDirection[3]!==0)count+=1;
                    for(let slot=0;slot<4;slot+=1)if(renderer3DLocalPositionType[slot*4+3]!==0)count+=1;return count;}
                if(query===2){if(property>=1&&property<=3)return Math.round(renderer3DAmbient[property-1]*255);
                    if(property===4)return Math.round(renderer3DAmbient[3]*1000);}
                if(query===3){if(property===1)return renderer3DDirectionalDirection[3]!==0?1:0;
                    if(property>=2&&property<=4)return Math.round(renderer3DDirectionalDirection[property-2]*1000);
                    if(property>=5&&property<=7)return Math.round(renderer3DDirectionalColor[property-5]*255);
                    if(property===8)return Math.round(renderer3DDirectionalColor[3]*1000);}
                if(query===4&&index>=0&&index<4){const offset=index*4;if(property===1)return renderer3DLocalPositionType[offset+3];
                    if(property>=2&&property<=4)return Math.round(renderer3DLocalPositionType[offset+property-2]);
                    if(property>=5&&property<=7)return Math.round(renderer3DLocalDirectionRange[offset+property-5]*1000);
                    if(property>=8&&property<=10)return Math.round(renderer3DLocalColorIntensity[offset+property-8]*255);
                    if(property===11)return Math.round(renderer3DLocalColorIntensity[offset+3]*1000);
                    if(property===12)return Math.round(renderer3DLocalDirectionRange[offset+3]);}
                renderer3DLastError=5;return 0;}
            function renderer3DModelPbrValue(model,property,index){if(!model){renderer3DLastError=5;return 0;}
                if(property===1)return model.pbrReady?1:0;if(property===2)return model.pbrMaterials.length;
                if(property===3)return model.pbrTextures.length;if(property===4&&index>=0&&index<model.parts.length){
                    const slot=model.materials[index];return model.pbrReady&&slot>=0&&slot<model.pbrMaterials.length?1:0;}
                if(property===5)return 1;if(property===6)return model.pbrFailure;
                if(property===7)return model.textureMetadata.length;
                renderer3DLastError=5;return 0;}

            function renderer3DSetTriangle(mesh, triangle, a, b, c) {
                triangle = safe(triangle); a = safe(a); b = safe(b); c = safe(c);
                const offset = triangle * 3;
                if (triangle < 0 || offset + 2 >= mesh.indexCount || a < 0 || b < 0 || c < 0) {
                    renderer3DLastError = 5; return false;
                }
                if (mesh.inFlight) { renderer3DLastError = 53; return false; }
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
                if (mesh.inFlight) { renderer3DLastError = 53; return false; }
                if (!mesh.explicitNormals)
                    mesh.vertices.forEach((_, index) => { if (index % 20 >= 3 && index % 20 < 6) mesh.vertices[index] = 0; });
                for (let offset = 0; offset < mesh.indexCount; offset += 3) {
                    const ia = mesh.indices[offset], ib = mesh.indices[offset + 1], ic = mesh.indices[offset + 2];
                    if (ia >= mesh.vertexCount || ib >= mesh.vertexCount || ic >= mesh.vertexCount) {
                        renderer3DLastError = 6; return false;
                    }
                    const a = ia * 20, b = ib * 20, c = ic * 20;
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
                    const offset = index * 20 + 3;
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

            function renderer3DTextHash(value) {
                let result=2166136261;
                for(const byte of utf8(value))result=Math.imul((result^byte)>>>0,16777619)>>>0;
                return result;
            }

            function renderer3DDecodeUtf8(bytes,start,end) {
                let result="";
                for(let index=start;index<end;){
                    const first=bytes[index++];let code=0,remaining=0,minimum=0;
                    if(first<=0x7f){code=first;}
                    else if(first>=0xc2&&first<=0xdf){code=first&0x1f;remaining=1;minimum=0x80;}
                    else if(first>=0xe0&&first<=0xef){code=first&0x0f;remaining=2;minimum=0x800;}
                    else if(first>=0xf0&&first<=0xf4){code=first&7;remaining=3;minimum=0x10000;}
                    else return null;
                    if(index+remaining>end)return null;
                    for(let count=0;count<remaining;count+=1){const next=bytes[index++];if((next&0xc0)!==0x80)return null;code=(code<<6)|(next&0x3f);}
                    if(code<minimum||code>0x10ffff||(code>=0xd800&&code<=0xdfff))return null;
                    result+=String.fromCodePoint(code);
                }
                return result;
            }

            function renderer3DTexturePath(path,byteLength) {
                if(!path||byteLength<1||byteLength>1024||path.startsWith("/")||path.startsWith("//")||
                    path.includes("\\")||path.includes(":")||/[\x00-\x1f\x7f*?\[\]{}!;"<>|]/u.test(path))return false;
                return path.split("/").every(segment=>segment&&segment!=="."&&segment!=="..");
            }

            function renderer3DParseModelV1(buffer) {
                const bytes=new Uint8Array(buffer),view=new DataView(buffer),u16=offset=>view.getUint16(offset,true),u32=offset=>view.getUint32(offset,true);
                const partCount=u32(8),vertexCount=u32(12),indexCount=u32(16),materialCount=u32(20);
                const partBytes=partCount*24,vertexBytes=vertexCount*32,expected=32+partBytes+vertexBytes+indexCount*4;
                if(bytes[0]!==83||bytes[1]!==77||bytes[2]!==51||bytes[3]!==68||u16(4)!==1||u16(6)!==32||
                    partCount<1||partCount>16||vertexCount<1||vertexCount>16*65535||
                    indexCount<1||indexCount>16*196608||materialCount<1||materialCount>64||
                    u32(24)!==buffer.byteLength||expected!==buffer.byteLength||u32(28)!==renderer3DChecksum(bytes,32))return null;
                const parts=[];
                for(let partIndex=0;partIndex<partCount;partIndex+=1){
                    const offset=32+partIndex*24,firstVertex=u32(offset),partVertices=u32(offset+4),
                        firstIndex=u32(offset+8),partIndices=u32(offset+12),material=u32(offset+16);
                    if(partVertices<1||partVertices>65535||partIndices<1||partIndices>196608||partIndices%3!==0||
                        firstVertex>vertexCount||partVertices>vertexCount-firstVertex||firstIndex>indexCount||
                        partIndices>indexCount-firstIndex||material>=materialCount||u32(offset+20)!==0)return null;
                    for(let index=0;index<partVertices*8;index+=1)
                        if(!Number.isFinite(view.getFloat32(32+partBytes+(firstVertex*8+index)*4,true)))return null;
                    for(let index=0;index<partIndices;index+=1)
                        if(u32(32+partBytes+vertexBytes+(firstIndex+index)*4)>=partVertices)return null;
                    parts.push({firstVertex,vertexCount:partVertices,firstIndex,indexCount:partIndices,material});
                }
                return {version:1,parts,vertexCount,indexCount,materialCount,textureMetadata:[],materialMetadata:[],
                    name:"",partNames:[],bounds:null,partBounds:[],tangentPositive:0,tangentNegative:0,
                    vertexOffset:32+partBytes,indexOffset:32+partBytes+vertexBytes};
            }

            function renderer3DParseModelV2(buffer) {
                const bytes=new Uint8Array(buffer),view=new DataView(buffer),u16=offset=>view.getUint16(offset,true),u32=offset=>view.getUint32(offset,true),f32=offset=>view.getFloat32(offset,true);
                if(buffer.byteLength<64||bytes[0]!==83||bytes[1]!==77||bytes[2]!==51||bytes[3]!==68||u16(4)!==2||u16(6)!==64||
                    u32(8)!==0||u32(12)!==buffer.byteLength||u32(16)!==renderer3DChecksum(bytes,64)||u32(24)!==64||u32(28)!==32||
                    u32(56)!==0||u32(60)!==0)return null;
                const chunkCount=u32(20),directoryEnd=64+chunkCount*32;
                if(chunkCount<1||chunkCount>32||directoryEnd>buffer.byteLength)return null;
                const chunks=new Map(),required=new Set(["STR0","PART","VERT","INDX","MATL","TEXR","BOND"]),
                    animationIds=["NODE","SKIN","SKEL","CLIP","TRAK","AFRM","EVNT","SOCK","ROOT"],
                    known=new Set([...required,...animationIds]),ranges=[];
                for(let index=0;index<chunkCount;index+=1){
                    const entry=64+index*32,id=String.fromCharCode(bytes[entry],bytes[entry+1],bytes[entry+2],bytes[entry+3]),
                        flags=u32(entry+4),offset=u32(entry+8),length=u32(entry+12),count=u32(entry+16),stride=u32(entry+20);
                    if(bytes[entry]<32||bytes[entry]>126||bytes[entry+1]<32||bytes[entry+1]>126||
                        bytes[entry+2]<32||bytes[entry+2]>126||bytes[entry+3]<32||bytes[entry+3]>126||
                        chunks.has(id)||(flags&~1)!==0||u32(entry+24)!==0||u32(entry+28)!==0||offset<((directoryEnd+3)&~3)||
                        offset%4!==0||offset>buffer.byteLength||length>buffer.byteLength-offset||(!known.has(id)&&(flags&1)===0))return null;
                    const chunk={id,flags,offset,length,count,stride};chunks.set(id,chunk);if(length)ranges.push(chunk);
                }
                ranges.sort((left,right)=>left.offset-right.offset);
                for(let index=1;index<ranges.length;index+=1)if(ranges[index-1].offset+ranges[index-1].length>ranges[index].offset)return null;
                for(const id of required)if(!chunks.has(id)||chunks.get(id).flags!==0)return null;
                const animationPresent=animationIds.filter(id=>chunks.has(id)).length;
                if(animationPresent!==0&&animationPresent!==animationIds.length)return null;
                if(animationPresent&&animationIds.some(id=>chunks.get(id).flags!==1))return null;
                const strings=chunks.get("STR0"),partChunk=chunks.get("PART"),vertexChunk=chunks.get("VERT"),indexChunk=chunks.get("INDX"),
                    materialChunk=chunks.get("MATL"),textureChunk=chunks.get("TEXR"),boundsChunk=chunks.get("BOND");
                const partCount=u32(36),vertexCount=u32(40),indexCount=u32(44),materialCount=u32(48),textureCount=u32(52);
                if(partCount<1||partCount>16||vertexCount<1||vertexCount>131072||indexCount<3||indexCount>393216||indexCount%3!==0||
                    materialCount<1||materialCount>64||textureCount>128||strings.count<1||strings.stride!==0||strings.length<1||
                    partChunk.count!==partCount||partChunk.stride!==32||partChunk.length!==partCount*32||
                    vertexChunk.count!==vertexCount||vertexChunk.stride!==48||vertexChunk.length!==vertexCount*48||
                    indexChunk.count!==indexCount||indexChunk.stride!==4||indexChunk.length!==indexCount*4||
                    materialChunk.count!==materialCount||materialChunk.stride!==80||materialChunk.length!==materialCount*80||
                    textureChunk.count!==textureCount||textureChunk.stride!==16||textureChunk.length!==textureCount*16||
                    boundsChunk.count!==partCount+1||boundsChunk.stride!==32||boundsChunk.length!==(partCount+1)*32)return null;
                const stringValues=new Map();let stringOffset=0,stringCount=0;
                while(stringOffset<strings.length){let end=stringOffset;while(end<strings.length&&bytes[strings.offset+end]!==0)end+=1;if(end>=strings.length)return null;
                    const value=renderer3DDecodeUtf8(bytes,strings.offset+stringOffset,strings.offset+end);if(value===null)return null;
                    stringValues.set(stringOffset,value);stringCount+=1;stringOffset=end+1;}
                if(stringOffset!==strings.length||stringCount!==strings.count||stringValues.get(0)!=="")return null;
                const stringAt=offset=>stringValues.has(offset)?stringValues.get(offset):null,name=stringAt(u32(32));if(name===null)return null;
                const textureMetadata=[],textureKeys=new Set();
                for(let index=0;index<textureCount;index+=1){const offset=textureChunk.offset+index*16,pathOffset=u32(offset),path=stringAt(pathOffset),semantic=u32(offset+4);
                    if(path===null||semantic<1||semantic>4||u32(offset+8)!==0||u32(offset+12)!==0||
                        !renderer3DTexturePath(path,utf8(path).length)||textureKeys.has(`${semantic}\0${path}`))return null;
                    textureKeys.add(`${semantic}\0${path}`);textureMetadata.push({path,semantic});}
                const reference=(offset,semantic)=>{const value=u32(offset);if(value===0xffffffff)return -1;if(value>=textureCount||textureMetadata[value].semantic!==semantic)return -2;return value;};
                const finite=(offset,minimum,maximum)=>{const value=f32(offset);return Number.isFinite(value)&&value>=minimum&&value<=maximum?value:null;};
                const materialMetadata=[];
                for(let index=0;index<materialCount;index+=1){const offset=materialChunk.offset+index*80,materialName=stringAt(u32(offset)),
                    refs=[reference(offset+4,1),reference(offset+8,2),reference(offset+12,3),reference(offset+16,4)],alphaMode=u32(offset+20),flags=u32(offset+24);
                    if(materialName===null||refs.some(value=>value===-2)||alphaMode>2||flags>1||u32(offset+28)!==0)return null;
                    const baseColor=[0,1,2,3].map(component=>finite(offset+32+component*4,0,1)),metallic=finite(offset+48,0,1),roughness=finite(offset+52,0,1),
                        normalStrength=finite(offset+56,0,8),occlusionStrength=finite(offset+60,0,1),
                        emissive=[0,1,2].map(component=>finite(offset+64+component*4,0,64)),alphaCutoff=finite(offset+76,0,1);
                    if(baseColor.some(value=>value===null)||emissive.some(value=>value===null)||[metallic,roughness,normalStrength,occlusionStrength,alphaCutoff].some(value=>value===null))return null;
                    materialMetadata.push({name:materialName,refs,alphaMode,doubleSided:flags,baseColor,metallic,roughness,normalStrength,occlusionStrength,emissive,alphaCutoff});}
                const readBounds=offset=>{const values=[0,1,2,3,4,5].map(component=>f32(offset+component*4));return values.every(Number.isFinite)&&u32(offset+24)===0&&u32(offset+28)===0&&
                    values[0]<=values[3]&&values[1]<=values[4]&&values[2]<=values[5]?values:null;};
                const bounds=readBounds(boundsChunk.offset);if(!bounds)return null;
                const parts=[],partNames=[],partBounds=[];let expectedVertex=0,expectedIndex=0,tangentPositive=0,tangentNegative=0;
                const modelComputed=[Infinity,Infinity,Infinity,-Infinity,-Infinity,-Infinity];
                for(let partIndex=0;partIndex<partCount;partIndex+=1){const offset=partChunk.offset+partIndex*32,partName=stringAt(u32(offset)),firstVertex=u32(offset+4),
                    partVertices=u32(offset+8),firstIndex=u32(offset+12),partIndices=u32(offset+16),material=u32(offset+20),declared=readBounds(boundsChunk.offset+(partIndex+1)*32);
                    if(partName===null||firstVertex!==expectedVertex||firstIndex!==expectedIndex||partVertices<1||partVertices>65535||partIndices<3||partIndices>196608||partIndices%3!==0||
                        partVertices>vertexCount-firstVertex||partIndices>indexCount-firstIndex||material>=materialCount||u32(offset+24)!==partIndex+1||u32(offset+28)!==0||!declared)return null;
                    const computed=[Infinity,Infinity,Infinity,-Infinity,-Infinity,-Infinity];
                    for(let vertex=0;vertex<partVertices;vertex+=1){const source=vertexChunk.offset+(firstVertex+vertex)*48,values=[...Array(12)].map((_,field)=>f32(source+field*4));
                        const normalLength=values[3]*values[3]+values[4]*values[4]+values[5]*values[5],tangentLength=values[6]*values[6]+values[7]*values[7]+values[8]*values[8],basisDot=values[3]*values[6]+values[4]*values[7]+values[5]*values[8];
                        if(!values.every(Number.isFinite)||Math.abs(normalLength-1)>.0001||Math.abs(tangentLength-1)>.0001||Math.abs(basisDot)>.0001||Math.abs(Math.abs(values[9])-1)>.0001)return null;
                        if(values[9]<0)tangentNegative+=1;else tangentPositive+=1;for(let axis=0;axis<3;axis+=1){computed[axis]=Math.min(computed[axis],values[axis]);computed[axis+3]=Math.max(computed[axis+3],values[axis]);}}
                    for(let index=0;index<partIndices;index+=1)if(u32(indexChunk.offset+(firstIndex+index)*4)>=partVertices)return null;
                    for(let triangle=0;triangle<partIndices;triangle+=3){const ids=[0,1,2].map(value=>u32(indexChunk.offset+(firstIndex+triangle+value)*4)),positions=ids.map(id=>{const source=vertexChunk.offset+(firstVertex+id)*48;return[f32(source),f32(source+4),f32(source+8)];}),
                        u=positions[1].map((value,axis)=>value-positions[0][axis]),v=positions[2].map((value,axis)=>value-positions[0][axis]),cross=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]];
                        if(cross.reduce((sum,value)=>sum+value*value,0)<=1e-12)return null;}
                    if(computed.some((value,index)=>value!==declared[index]))return null;for(let axis=0;axis<3;axis+=1){modelComputed[axis]=Math.min(modelComputed[axis],computed[axis]);modelComputed[axis+3]=Math.max(modelComputed[axis+3],computed[axis+3]);}
                    parts.push({firstVertex,vertexCount:partVertices,firstIndex,indexCount:partIndices,material});partNames.push(partName);partBounds.push(declared);expectedVertex+=partVertices;expectedIndex+=partIndices;}
                if(expectedVertex!==vertexCount||expectedIndex!==indexCount||modelComputed.some((value,index)=>value!==bounds[index]))return null;
                const animation=animationPresent?renderer3DParseAnimationV2(buffer,chunks,stringAt,vertexCount):null;
                if(animationPresent&&!animation)return null;
                return {version:2,parts,vertexCount,indexCount,materialCount,textureMetadata,materialMetadata,name,partNames,bounds,partBounds,tangentPositive,tangentNegative,
                    vertexOffset:vertexChunk.offset,indexOffset:indexChunk.offset,animation};
            }

            function renderer3DParseAnimationV2(buffer,chunks,stringAt,vertexCount){
                const view=new DataView(buffer),u16=offset=>view.getUint16(offset,true),u32=offset=>view.getUint32(offset,true),
                    i32=offset=>view.getInt32(offset,true),f32=offset=>view.getFloat32(offset,true),
                    ids=["NODE","SKIN","SKEL","CLIP","TRAK","AFRM","EVNT","SOCK","ROOT"],
                    strides=[64,16,80,40,48,4,20,64,24],limits=[256,131072,128,64,16384,4194304,4096,64,64],values=ids.map(id=>chunks.get(id));
                if(values.some((chunk,index)=>!chunk||chunk.flags!==1||chunk.stride!==strides[index]||chunk.count>limits[index]||chunk.length!==chunk.count*chunk.stride)||
                    values[0].count<1||values[1].count!==vertexCount||values[2].count<1||values[3].count<1||values[5].count<1)return null;
                const finiteArray=(offset,count)=>{const result=new Float32Array(count);for(let index=0;index<count;index+=1){const value=f32(offset+index*4);if(!Number.isFinite(value))return null;result[index]=value;}return result;};
                const nodes=[],nodeNames=new Set();
                for(let index=0;index<values[0].count;index+=1){const offset=values[0].offset+index*64,name=stringAt(u32(offset)),parent=i32(offset+4),flags=u32(offset+8),
                    translation=finiteArray(offset+16,3),rotation=finiteArray(offset+28,4),scale=finiteArray(offset+44,3);
                    if(name===null||nodeNames.has(name)||parent< -1||parent>=index||(flags&~3)!==0||u32(offset+12)!==0||u32(offset+56)!==0||u32(offset+60)!==0||
                        !translation||!rotation||!scale||Math.abs(rotation.reduce((sum,value)=>sum+value*value,0)-1)>.0001||scale.some(value=>value<=0)||
                        Math.abs(scale[0]-scale[1])>.0001||Math.abs(scale[0]-scale[2])>.0001)return null;
                    nodeNames.add(name);nodes.push({name,parent,flags,translation,rotation,scale});}
                const bones=[];let rootBones=0;
                for(let index=0;index<values[2].count;index+=1){const offset=values[2].offset+index*80,node=u32(offset),parent=i32(offset+4),inverse=finiteArray(offset+16,16);
                    if(node>=nodes.length||parent< -1||parent>=index||u32(offset+8)!==0||u32(offset+12)!==0||!inverse)return null;
                    if(parent<0)rootBones+=1;bones.push({node,parent,inverse});}
                if(rootBones!==1)return null;
                const clips=[],clipNames=new Set();
                for(let index=0;index<values[3].count;index+=1){const offset=values[3].offset+index*40,name=stringAt(u32(offset)),duration=u32(offset+4),rate=u32(offset+8),samples=u32(offset+12),
                    firstTrack=u32(offset+16),trackCount=u32(offset+20),firstEvent=u32(offset+24),eventCount=u32(offset+28),flags=u32(offset+32),root=u32(offset+36),
                    minimumSamples=Math.floor(duration/1000*rate)+1,maximumSamples=Math.ceil(duration/1000*rate)+1;
                    if(name===null||clipNames.has(name)||duration<1||duration>120000||rate<15||rate>60||samples<minimumSamples||samples>maximumSamples||
                        firstTrack>values[4].count||trackCount>values[4].count-firstTrack||firstEvent>values[6].count||eventCount>64||eventCount>values[6].count-firstEvent||
                        flags>1||(root!==0xffffffff&&root>=values[8].count))return null;
                    clipNames.add(name);clips.push({name,duration,rate,samples,firstTrack,trackCount,firstEvent,eventCount,loop:flags!==0,root:root===0xffffffff?-1:root});}
                const frames=finiteArray(values[5].offset,values[5].count);if(!frames)return null;
                const tracks=[];
                for(let index=0;index<values[4].count;index+=1){const offset=values[4].offset+index*48,clip=u32(offset),node=u32(offset+4),flags=u32(offset+8);
                    if(clip>=clips.length||node>=nodes.length||(flags&~63)!==0||u32(offset+12)!==0||u32(offset+40)!==0||u32(offset+44)!==0||
                        index<clips[clip].firstTrack||index>=clips[clip].firstTrack+clips[clip].trackCount)return null;
                    const channel=(field,components,present,sampled)=>{const first=u32(offset+field),count=u32(offset+field+4),exists=(flags&present)!==0,dense=(flags&sampled)!==0;
                        if(!exists)return first===0xffffffff&&count===0?null:false;if(first===0xffffffff||count!==(dense?clips[clip].samples:1)||first>frames.length||count*components>frames.length-first)return false;
                        return {first,count,components};};
                    const translation=channel(16,3,1,2),rotation=channel(24,4,4,8),scale=channel(32,3,16,32);
                    if(translation===false||rotation===false||scale===false)return null;
                    if(rotation)for(let sample=0;sample<rotation.count;sample+=1){let length=0;for(let component=0;component<4;component+=1)length+=frames[rotation.first+sample*4+component]**2;if(Math.abs(length-1)>.0001)return null;}
                    if(scale)for(let sample=0;sample<scale.count;sample+=1){const first=scale.first+sample*3;if(frames[first]<=0||Math.abs(frames[first]-frames[first+1])>.0001||Math.abs(frames[first]-frames[first+2])>.0001)return null;}
                    tracks.push({clip,node,flags,translation,rotation,scale});}
                const events=[];let priorClip=-1,priorTime=-1,priorOrder=-1;
                for(let index=0;index<values[6].count;index+=1){const offset=values[6].offset+index*20,clip=u32(offset),time=u32(offset+4),name=stringAt(u32(offset+8)),value=i32(offset+12),order=u32(offset+16);
                    if(clip>=clips.length||name===null||time>clips[clip].duration||index<clips[clip].firstEvent||index>=clips[clip].firstEvent+clips[clip].eventCount||clip<priorClip||
                        (clip===priorClip&&(time<priorTime||(time===priorTime&&order<=priorOrder))))return null;
                    events.push({clip,time,name,value,order});priorClip=clip;priorTime=time;priorOrder=order;}
                const sockets=[],socketNames=new Set();
                for(let index=0;index<values[7].count;index+=1){const offset=values[7].offset+index*64,name=stringAt(u32(offset)),node=u32(offset+4),translation=finiteArray(offset+16,3),rotation=finiteArray(offset+28,4),scale=finiteArray(offset+44,3);
                    if(name===null||socketNames.has(name)||node>=nodes.length||u32(offset+8)!==0||u32(offset+12)!==0||u32(offset+56)!==0||u32(offset+60)!==0||!translation||!rotation||!scale)return null;
                    socketNames.add(name);sockets.push({name,node,translation,rotation,scale});}
                const roots=[];
                for(let index=0;index<values[8].count;index+=1){const offset=values[8].offset+index*24,clip=u32(offset),node=u32(offset+4),axes=u32(offset+8),yaw=u32(offset+12),remove=u32(offset+16);
                    if(clip>=clips.length||node>=nodes.length||axes<1||axes>7||yaw>1||remove>1||u32(offset+20)!==0||clips[clip].root!==index)return null;
                    roots.push({clip,node,axes,yaw:yaw!==0,remove:remove!==0});}
                const joints=new Uint16Array(vertexCount*4),weights=new Uint16Array(vertexCount*4);
                for(let vertex=0;vertex<vertexCount;vertex+=1){const offset=values[1].offset+vertex*16;let total=0;
                    for(let influence=0;influence<4;influence+=1){const joint=u16(offset+influence*2),weight=u16(offset+8+influence*2);if(joint>=bones.length||(weight===0&&joint!==0))return null;
                        joints[vertex*4+influence]=joint;weights[vertex*4+influence]=weight;total+=weight;}if(total!==65535)return null;}
                const logicalBytes=values.reduce((sum,chunk)=>sum+chunk.length,0),residentBytes=values.reduce((sum,chunk)=>((sum+3)&~3)+chunk.length,0);
                return {nodes,bones,clips,tracks,frames,events,sockets,roots,joints,weights,bytes:logicalBytes,fileBytes:buffer.byteLength,residentBytes,animatorBytes:16988+nodes.length*220};
            }

            async function renderer3DLoadModel(path, preparePbr = true) {
                if (renderer3DModels.size >= 64) { renderer3DLastError = 25; return 0; }
                let buffer;
                try {
                    buffer = await fetchAssetBytes(path, { cache: "no-store" }, true);
                } catch (_) { renderer3DLastError = 26; return 0; }
                if (!(buffer instanceof ArrayBuffer) || buffer.byteLength < 32 || buffer.byteLength > 16*1024*1024) {
                    forgetAssetDownload(logicalPath(path));
                    renderer3DLastError = 24; return 0;
                }
                const view = new DataView(buffer), version=buffer.byteLength>=6?view.getUint16(4,true):0;
                const descriptor=version===1?renderer3DParseModelV1(buffer):version===2?renderer3DParseModelV2(buffer):null;
                if(!descriptor){forgetAssetDownload(logicalPath(path));renderer3DLastError=24;return 0;}
                if(renderer3DMeshes.size+descriptor.parts.length>128){renderer3DLastError=3;return 0;}
                const modelHandle=renderer3DHandle(),meshHandles=[];
                const rollback=()=>{
                    for(const value of meshHandles){const old=renderer3DMeshes.get(value);if(old)renderer3DDeleteGpu(old);renderer3DMeshes.delete(value);}
                };
                try{
                    for(const part of descriptor.parts){
                        const handle=renderer3DCreateMesh(part.vertexCount,part.indexCount),mesh=renderer3DMeshes.get(handle);
                        if(!mesh){rollback();return 0;}
                        for(let vertex=0;vertex<part.vertexCount;vertex+=1){
                            const source=descriptor.vertexOffset+(part.firstVertex+vertex)*(descriptor.version===1?32:48),target=vertex*20;
                            if(descriptor.version===1)for(let field=0;field<8;field+=1)mesh.vertices[target+field]=view.getFloat32(source+field*4,true);
                            else{for(let field=0;field<6;field+=1)mesh.vertices[target+field]=view.getFloat32(source+field*4,true);for(let field=0;field<4;field+=1)mesh.vertices[target+16+field]=view.getFloat32(source+(6+field)*4,true);mesh.vertices[target+6]=view.getFloat32(source+40,true);mesh.vertices[target+7]=view.getFloat32(source+44,true);
                                if(descriptor.animation)for(let influence=0;influence<4;influence+=1){const skin=(part.firstVertex+vertex)*4+influence,joint=descriptor.animation.joints[skin];mesh.vertices[target+8+influence]=joint;mesh.vertices[target+12+influence]=descriptor.animation.weights[skin]/65535;mesh.maxJoint=Math.max(mesh.maxJoint,joint);}}
                        }
                        mesh.explicitNormals=true;
                        for(let index=0;index<part.indexCount;index+=1)
                            mesh.indices[index]=view.getUint32(descriptor.indexOffset+(part.firstIndex+index)*4,true);
                        if(!renderer3DCommit(mesh)){renderer3DDeleteGpu(mesh);renderer3DMeshes.delete(handle);rollback();return 0;}
                        meshHandles.push(handle);
                    }
                }catch(error){rollback();renderer3DLastError=42;renderer3DRecordFailure("model",String(error.stack||error),path);return 0;}
                if(renderer3DModels.size>=64){rollback();renderer3DLastError=25;return 0;}
                renderer3DModels.set(modelHandle,{parts:meshHandles,materials:descriptor.parts.map(part=>part.material),materialCount:descriptor.materialCount,
                    version:descriptor.version,vertexCount:descriptor.vertexCount,indexCount:descriptor.indexCount,textureMetadata:descriptor.textureMetadata,
                    materialMetadata:descriptor.materialMetadata,name:descriptor.name,partNames:descriptor.partNames,bounds:descriptor.bounds,partBounds:descriptor.partBounds,
                    tangentPositive:descriptor.tangentPositive,tangentNegative:descriptor.tangentNegative,
                    animation:descriptor.animation||null,pbrReady:false,pbrFailure:0,pbrTextureByReference:[],pbrTextures:[],pbrMaterials:[]});
                if(preparePbr&&descriptor.version===2&&!await renderer3DPrepareModelPbr(modelHandle,3,1,8)){
                    const failure=renderer3DLastError;renderer3DDeleteModel(modelHandle);renderer3DLastError=failure;return 0;
                }
                return modelHandle;
            }

            async function renderer3DPrepareModelPbr(modelHandle,filter,wrap,anisotropy){
                [modelHandle,filter,wrap,anisotropy]=[modelHandle,filter,wrap,anisotropy].map(safe);
                const model=renderer3DModels.get(modelHandle);
                const fail=code=>{renderer3DLastError=code;if(model)model.pbrFailure=code;return 0;};
                if(!model||model.version!==2||filter<0||filter>3||wrap<0||wrap>1||anisotropy<1||anisotropy>16)
                    return fail(40);
                if(model.pbrReady){model.pbrFailure=0;return 1;}
                if(!renderer3DInitialize()||!renderer3DPbrProgram)return fail(44);
                const unique=[],referenceToUnique=new Array(model.textureMetadata.length);
                const effectiveAnisotropy=filter===3?Math.min(anisotropy,renderer3DMaximumAnisotropy):1;
                for(let reference=0;reference<model.textureMetadata.length;reference+=1){
                    const metadata=model.textureMetadata[reference],usage=metadata.semantic===1||metadata.semantic===4?1:2;
                    let uniqueIndex=unique.findIndex(identity=>identity.path===metadata.path&&identity.usage===usage&&
                        identity.filter===filter&&identity.wrap===wrap&&identity.anisotropy===anisotropy&&
                        identity.effectiveAnisotropy===effectiveAnisotropy&&identity.mips===(filter>=2));
                    if(uniqueIndex<0){uniqueIndex=unique.length;unique.push({path:metadata.path,usage,filter,wrap,anisotropy,
                        effectiveAnisotropy,mips:filter>=2});}
                    referenceToUnique[reference]=uniqueIndex;
                }
                if(renderer3DTextures.size+unique.length>128||renderer3DMaterials.size+model.materialMetadata.length>128)
                    return fail(41);
                const ownedTextures=[],textureByReference=new Array(model.textureMetadata.length).fill(0),materials=[];
                let pendingImage=null;
                const rollback=()=>{
                    if(pendingImage){imageRelease(pendingImage);pendingImage=null;}
                    for(const handle of materials)renderer3DMaterials.delete(handle);
                    for(const handle of ownedTextures){const texture=renderer3DTextures.get(handle);if(texture){renderer3DDeleteTextureGpu(texture);imageRelease(texture.image);}renderer3DTextures.delete(handle);}
                };
                try{
                    for(const identity of unique){
                        pendingImage=await loadImage(identity.path);
                        const handle=renderer3DCreatePbrTexture(pendingImage,identity.usage,filter,wrap,anisotropy);
                        pendingImage=null;
                        if(!handle){const failure=renderer3DLastError;rollback();return fail(failure);}
                        ownedTextures.push(handle);
                    }
                    for(let reference=0;reference<referenceToUnique.length;reference+=1)
                        textureByReference[reference]=ownedTextures[referenceToUnique[reference]];
                    for(const metadata of model.materialMetadata){
                        const selected=metadata.refs.map(reference=>reference<0?0:textureByReference[reference]);
                        const materialHandle=renderer3DCreatePbrMaterial(selected[0],selected[1],selected[2],selected[3],
                            metadata.alphaMode,metadata.doubleSided,modelHandle);
                        if(!materialHandle){const failure=renderer3DLastError;rollback();return fail(failure);}
                        const material=renderer3DMaterials.get(materialHandle);material.baseColor.set(metadata.baseColor);
                        material.surface[0]=metadata.metallic;material.surface[1]=metadata.roughness;
                        material.surface[2]=metadata.normalStrength;material.surface[3]=metadata.occlusionStrength;
                        material.emissiveAlpha[0]=metadata.emissive[0];material.emissiveAlpha[1]=metadata.emissive[1];
                        material.emissiveAlpha[2]=metadata.emissive[2];material.cutoff=metadata.alphaCutoff;
                        material.emissiveAlpha[3]=metadata.alphaMode===1?metadata.alphaCutoff:-1;
                        for(let channel=0;channel<4;channel+=1)material.textureFlags[channel]=selected[channel]===0?0:1;
                        materials.push(materialHandle);
                    }
                }catch(_){rollback();return fail(42);}
                model.pbrTextureByReference=textureByReference;model.pbrTextures=ownedTextures;
                model.pbrMaterials=materials;model.pbrReady=true;model.pbrFailure=0;return 1;
            }

            function renderer3DModelStaticValue(model,query,index,property) {
                if(!model){renderer3DLastError=5;return 0;}
                const rounded=value=>value<0?-Math.floor(-value*1000+.5):Math.floor(value*1000+.5);
                if(query===1)return model.version;
                if(query===2)return model.vertexCount;
                if(query===3)return model.indexCount;
                if(query===4)return model.textureMetadata.length;
                if(query===5)return model.version===2?model.tangentPositive:0;
                if(query===6)return model.version===2?model.tangentNegative:0;
                if(query===7){
                    const material=model.version===2&&index>=0&&index<model.materialMetadata.length?model.materialMetadata[index]:null;
                    if(!material){renderer3DLastError=5;return 0;}
                    if(property>=1&&property<=4)return rounded(material.baseColor[property-1]);
                    if(property===5)return rounded(material.metallic);if(property===6)return rounded(material.roughness);
                    if(property===7)return rounded(material.normalStrength);if(property===8)return rounded(material.occlusionStrength);
                    if(property>=9&&property<=11)return rounded(material.emissive[property-9]);
                    if(property===12)return material.alphaMode;if(property===13)return rounded(material.alphaCutoff);
                    if(property===14)return material.doubleSided;if(property>=15&&property<=18)return material.refs[property-15]+1;
                    if(property===19)return renderer3DTextHash(material.name);
                }else if(query===8){
                    const texture=model.version===2&&index>=0&&index<model.textureMetadata.length?model.textureMetadata[index]:null;
                    if(!texture){renderer3DLastError=5;return 0;}if(property===1)return texture.semantic;if(property===2)return renderer3DTextHash(texture.path);
                }else if(query===9){
                    const bounds=model.version===2&&(index===-1||index>=0&&index<model.partBounds.length)?(index===-1?model.bounds:model.partBounds[index]):null;
                    if(!bounds||property<0||property>=6){renderer3DLastError=5;return 0;}return rounded(bounds[property]);
                }else if(query===10){
                    if(model.version!==2||index<0||index>=model.partNames.length){renderer3DLastError=5;return 0;}return renderer3DTextHash(model.partNames[index]);
                }else if(query===11){if(model.version!==2){renderer3DLastError=5;return 0;}return renderer3DTextHash(model.name);}
                renderer3DLastError=5;return 0;
            }

            function renderer3DDeleteModel(handle) {
                const model=renderer3DModels.get(handle);if(!model)return false;
                if(renderer3DModelAnimatorReferences(handle)!==0)return false;
                for(const mesh of model.parts)if(renderer3DMeshReferenceCount(mesh)!==0)return false;
                for(const material of model.pbrMaterials||[])if(renderer3DMaterialReferenceCount(material)!==0)return false;
                for(const textureHandle of model.pbrTextures||[]){const texture=renderer3DTextures.get(textureHandle);if(texture&&texture.inFlight)return false;}
                for(const material of model.pbrMaterials||[])renderer3DMaterials.delete(material);
                for(const textureHandle of model.pbrTextures||[]){const texture=renderer3DTextures.get(textureHandle);
                    if(texture){renderer3DDeleteTextureGpu(texture);imageRelease(texture.image);}renderer3DTextures.delete(textureHandle);}
                for(const handle of model.parts){const mesh=renderer3DMeshes.get(handle);if(mesh)renderer3DDeleteGpu(mesh);renderer3DMeshes.delete(handle);}
                renderer3DModels.delete(handle);return true;
            }

            function renderer3DCreateSkeleton(boneCount){boneCount=safe(boneCount);if(boneCount<1||boneCount>32||renderer3DSkeletons.size>=64){renderer3DLastError=28;return 0;}const handle=renderer3DHandle();renderer3DSkeletons.set(handle,{boneCount,parents:new Array(boneCount).fill(-2),bind:Array.from({length:boneCount},()=>[0,0,0]),inverse:Array.from({length:boneCount},()=>[0,0,0]),committed:false});return handle;}
            function renderer3DCommitSkeleton(skeleton){for(let bone=0;bone<skeleton.boneCount;bone+=1){const parent=skeleton.parents[bone];if(parent< -1||parent>=bone){renderer3DLastError=30;return false;}const global=[...skeleton.bind[bone]];if(parent>=0)for(let axis=0;axis<3;axis+=1)global[axis]-=skeleton.inverse[parent][axis];skeleton.inverse[bone]=global.map(value=>-value);}skeleton.committed=true;return true;}
            function renderer3DCreateClip(skeletonHandle,duration){duration=safe(duration);const skeleton=renderer3DSkeletons.get(skeletonHandle);if(!skeleton||!skeleton.committed||duration<1||duration>600000||renderer3DClips.size>=128){renderer3DLastError=31;return 0;}const handle=renderer3DHandle();renderer3DClips.set(handle,{skeleton:skeletonHandle,duration,translation:new Array(skeleton.boneCount).fill(null),rotation:new Array(skeleton.boneCount).fill(null),scale:new Array(skeleton.boneCount).fill(null),events:[],pbrScaleSafe:true});return handle;}
            function renderer3DUpdateClipScaleSafety(clip){clip.pbrScaleSafe=true;for(const track of clip.scale){if(track&&(Math.abs(track[0][0]-track[0][1])>.0001||Math.abs(track[0][0]-track[0][2])>.0001||Math.abs(track[1][0]-track[1][1])>.0001||Math.abs(track[1][0]-track[1][2])>.0001)){clip.pbrScaleSafe=false;break;}}}
            function renderer3DCreateAnimator(skeletonHandle){const skeleton=renderer3DSkeletons.get(skeletonHandle);if(!skeleton||!skeleton.committed||renderer3DAnimators.size>=128){renderer3DLastError=33;return 0;}const handle=renderer3DHandle();renderer3DAnimators.set(handle,{skeleton:skeletonHandle,clip:0,loop:false,complete:false,time:0,previous:0,speed:100,pending:0,production:false,revision:0,bones:Array.from({length:32},()=>renderer3DIdentity()),palette:new Float32Array(32*16)});renderer3DUpdatePose(renderer3DAnimators.get(handle));return handle;}
            function renderer3DPose(tx,ty,tz,qx,qy,qz,qw,sx,sy,sz){let length=Math.hypot(qx,qy,qz,qw);if(length<.000001){qx=qy=qz=0;qw=1;}else{qx/=length;qy/=length;qz/=length;qw/=length;}const result=renderer3DIdentity();result[0]=(1-2*qy*qy-2*qz*qz)*sx;result[1]=(2*qx*qy+2*qw*qz)*sx;result[2]=(2*qx*qz-2*qw*qy)*sx;result[4]=(2*qx*qy-2*qw*qz)*sy;result[5]=(1-2*qx*qx-2*qz*qz)*sy;result[6]=(2*qy*qz+2*qw*qx)*sy;result[8]=(2*qx*qz+2*qw*qy)*sz;result[9]=(2*qy*qz-2*qw*qx)*sz;result[10]=(1-2*qx*qx-2*qy*qy)*sz;result[12]=tx;result[13]=ty;result[14]=tz;return result;}
            function renderer3DUpdatePose(animator){const skeleton=renderer3DSkeletons.get(animator.skeleton),clip=renderer3DClips.get(animator.clip),amount=clip?animator.time/clip.duration:0,global=[];if(!skeleton)return;const lerp=(a,b)=>a+(b-a)*amount;for(let bone=0;bone<skeleton.boneCount;bone+=1){let [tx,ty,tz]=skeleton.bind[bone],qx=0,qy=0,qz=0,qw=1,sx=1,sy=1,sz=1;const translation=clip&&clip.translation[bone],rotation=clip&&clip.rotation[bone],scale=clip&&clip.scale[bone];if(translation){tx=lerp(translation[0][0],translation[1][0]);ty=lerp(translation[0][1],translation[1][1]);tz=lerp(translation[0][2],translation[1][2]);}if(rotation){const dot=rotation[0].reduce((sum,value,index)=>sum+value*rotation[1][index],0),direction=dot<0?-1:1;qx=lerp(rotation[0][0],rotation[1][0]*direction);qy=lerp(rotation[0][1],rotation[1][1]*direction);qz=lerp(rotation[0][2],rotation[1][2]*direction);qw=lerp(rotation[0][3],rotation[1][3]*direction);}if(scale){sx=lerp(scale[0][0],scale[1][0]);sy=lerp(scale[0][1],scale[1][1]);sz=lerp(scale[0][2],scale[1][2]);}const local=renderer3DPose(tx,ty,tz,qx,qy,qz,qw,sx,sy,sz),parent=skeleton.parents[bone];global[bone]=parent<0?local:renderer3DMultiply(global[parent],local);const inverse=renderer3DIdentity();inverse[12]=skeleton.inverse[bone][0];inverse[13]=skeleton.inverse[bone][1];inverse[14]=skeleton.inverse[bone][2];animator.bones[bone]=renderer3DMultiply(global[bone],inverse);animator.palette.set(animator.bones[bone],bone*16);}for(let bone=skeleton.boneCount;bone<32;bone+=1){animator.bones[bone]=renderer3DIdentity();animator.palette.set(animator.bones[bone],bone*16);}animator.revision=(animator.revision+1)>>>0;if(animator.revision===0)animator.revision=1;}
            function renderer3DUpdateAnimator(animator,delta){delta=safe(delta);if(!animator||delta<0||delta>600000){renderer3DLastError=35;return false;}const clip=renderer3DClips.get(animator.clip);if(!clip){renderer3DUpdatePose(animator);return true;}animator.previous=animator.time;const advance=Math.trunc(delta*animator.speed/100),total=animator.time+advance,wrapped=animator.loop&&total>=clip.duration;if(animator.loop){animator.time=total%clip.duration;animator.complete=false;}else{animator.time=Math.min(total,clip.duration);animator.complete=total>=clip.duration;}for(const event of clip.events)if((!wrapped&&event.time>animator.previous&&event.time<=animator.time)||(wrapped&&(event.time>animator.previous||event.time<=animator.time))||(animator.loop&&advance>=clip.duration))animator.pending=event.id;renderer3DUpdatePose(animator);return true;}
            function renderer3DSkeletonReferences(handle){let count=0;for(const clip of renderer3DClips.values())if(clip.skeleton===handle)count+=1;for(const animator of renderer3DAnimators.values())if(animator.skeleton===handle)count+=1;return count;}
            function renderer3DClipReferences(handle){let count=0;for(const animator of renderer3DAnimators.values())if(animator.clip===handle)count+=1;return count;}
            function renderer3DAnimatorReferences(handle){let count=0;for(const object of renderer3DObjects.values())if(object.animator===handle)count+=1;return count;}

            function renderer3DModelAnimatorReferences(handle){let count=0;for(const animator of renderer3DAnimators.values())if(animator.production&&animator.model===handle)count+=1;return count;}
            function renderer3DModelOwnsMesh(modelHandle,meshHandle){const model=renderer3DModels.get(modelHandle);return !!model&&model.parts.includes(meshHandle);}
            function renderer3DPoseIntoAt(output,offset,tx,ty,tz,qx,qy,qz,qw,sx,sy,sz){let length=Math.hypot(qx,qy,qz,qw);if(length<.000001){qx=qy=qz=0;qw=1;}else{qx/=length;qy/=length;qz/=length;qw/=length;}
                for(let index=0;index<16;index+=1)output[offset+index]=0;output[offset]=(1-2*qy*qy-2*qz*qz)*sx;output[offset+1]=(2*qx*qy+2*qw*qz)*sx;output[offset+2]=(2*qx*qz-2*qw*qy)*sx;
                output[offset+4]=(2*qx*qy-2*qw*qz)*sy;output[offset+5]=(1-2*qx*qx-2*qz*qz)*sy;output[offset+6]=(2*qy*qz+2*qw*qx)*sy;
                output[offset+8]=(2*qx*qz+2*qw*qy)*sz;output[offset+9]=(2*qy*qz-2*qw*qx)*sz;output[offset+10]=(1-2*qx*qx-2*qy*qy)*sz;
                output[offset+12]=tx;output[offset+13]=ty;output[offset+14]=tz;output[offset+15]=1;}
            function renderer3DMultiplyAt(output,outputOffset,left,leftOffset,right,rightOffset){for(let column=0;column<4;column+=1)for(let row=0;row<4;row+=1){let value=0;
                for(let index=0;index<4;index+=1)value+=left[leftOffset+index*4+row]*right[rightOffset+column*4+index];output[outputOffset+column*4+row]=value;}}
            function renderer3DModelChannelInto(animation,track,channel,time,clip,output,offset){const value=track[channel];if(!value)return;const components=value.components;
                if(value.count===1){for(let component=0;component<components;component+=1)output[offset+component]=animation.frames[value.first+component];return;}
                const scaled=time*clip.rate,finalFirst=clip.samples-2,finalStart=finalFirst*1000,finalEnd=clip.duration*clip.rate;let firstSample,amount;
                if(time>=clip.duration){firstSample=clip.samples-1;amount=0;}else if(scaled>=finalStart){firstSample=finalFirst;amount=finalEnd<=finalStart?0:(scaled-finalStart)/(finalEnd-finalStart);}else{firstSample=Math.trunc(scaled/1000);amount=(scaled%1000)/1000;}
                const secondSample=Math.min(clip.samples-1,firstSample+1);
                if(channel==="rotation"){let dot=0;for(let component=0;component<4;component+=1)dot+=animation.frames[value.first+firstSample*4+component]*animation.frames[value.first+secondSample*4+component];const direction=dot<0?-1:1;let length=0;
                    for(let component=0;component<4;component+=1){const first=animation.frames[value.first+firstSample*4+component],second=animation.frames[value.first+secondSample*4+component]*direction,mixed=first+(second-first)*amount;output[offset+component]=mixed;length+=mixed*mixed;}length=Math.sqrt(length)||1;for(let component=0;component<4;component+=1)output[offset+component]/=length;}
                else for(let component=0;component<components;component+=1){const first=animation.frames[value.first+firstSample*components+component],second=animation.frames[value.first+secondSample*components+component];output[offset+component]=first+(second-first)*amount;}}
            function renderer3DModelLocalsInto(model,clipIndex,time,output){const animation=model.animation;for(let node=0;node<animation.nodes.length;node+=1){const source=animation.nodes[node],offset=node*10;
                    output.set(source.translation,offset);output.set(source.rotation,offset+3);output.set(source.scale,offset+7);}if(clipIndex<0)return;const clip=animation.clips[clipIndex];
                for(let ordinal=0;ordinal<clip.trackCount;ordinal+=1){const track=animation.tracks[clip.firstTrack+ordinal],offset=track.node*10;renderer3DModelChannelInto(animation,track,"translation",time,clip,output,offset);
                    renderer3DModelChannelInto(animation,track,"rotation",time,clip,output,offset+3);renderer3DModelChannelInto(animation,track,"scale",time,clip,output,offset+7);}}
            function renderer3DRemoveModelRoot(model,clipIndex,output){if(clipIndex<0)return;const animation=model.animation,clip=animation.clips[clipIndex];if(clip.root<0)return;const root=animation.roots[clip.root];if(!root.remove)return;const offset=root.node*10,node=animation.nodes[root.node],bind=node.translation;
                for(let axis=0;axis<3;axis+=1)if(root.axes&(1<<axis))output[offset+axis]=bind[axis];if(root.yaw){const qx=output[offset+3],qy=output[offset+4],qz=output[offset+5],qw=output[offset+6],twistLength=Math.hypot(qy,qw)||1,ty=qy/twistLength,tw=qw/twistLength,
                    sx=qx*tw+qz*ty,sy=-qw*ty+qy*tw,sz=-qx*ty+qz*tw,sw=qw*tw+qy*ty,bindLength=Math.hypot(node.rotation[1],node.rotation[3])||1,by=node.rotation[1]/bindLength,bw=node.rotation[3]/bindLength;
                    output[offset+3]=sx*bw-sz*by;output[offset+4]=sw*by+sy*bw;output[offset+5]=sx*by+sz*bw;output[offset+6]=sw*bw-sy*by;}}
            function renderer3DUpdateModelPose(animator){const model=renderer3DModels.get(animator.model);if(!model||!model.animation)return;const animation=model.animation;
                renderer3DModelLocalsInto(model,animator.clipIndex,animator.time,animator.locals);if(animator.rootMode)renderer3DRemoveModelRoot(model,animator.clipIndex,animator.locals);if(animator.destinationClip>=0){renderer3DModelLocalsInto(model,animator.destinationClip,animator.destinationTime,animator.destinationLocals);if(animator.rootMode)renderer3DRemoveModelRoot(model,animator.destinationClip,animator.destinationLocals);
                    const amount=animator.fadeDuration===0?1:Math.min(1,animator.fadeElapsed/animator.fadeDuration);for(let node=0;node<animation.nodes.length;node+=1){const offset=node*10;
                        for(let component=0;component<3;component+=1){animator.locals[offset+component]+=(animator.destinationLocals[offset+component]-animator.locals[offset+component])*amount;animator.locals[offset+7+component]+=(animator.destinationLocals[offset+7+component]-animator.locals[offset+7+component])*amount;}
                        let dot=0;for(let component=0;component<4;component+=1)dot+=animator.locals[offset+3+component]*animator.destinationLocals[offset+3+component];const direction=dot<0?-1:1;let length=0;
                        for(let component=0;component<4;component+=1){const value=animator.locals[offset+3+component]+(animator.destinationLocals[offset+3+component]*direction-animator.locals[offset+3+component])*amount;animator.locals[offset+3+component]=value;length+=value*value;}length=Math.sqrt(length)||1;for(let component=0;component<4;component+=1)animator.locals[offset+3+component]/=length;}}
                for(let node=0;node<animation.nodes.length;node+=1){const offset=node*10,matrix=node*16;renderer3DPoseIntoAt(animator.globals,matrix,animator.locals[offset],animator.locals[offset+1],animator.locals[offset+2],animator.locals[offset+3],animator.locals[offset+4],animator.locals[offset+5],animator.locals[offset+6],animator.locals[offset+7],animator.locals[offset+8],animator.locals[offset+9]);
                    const parent=animation.nodes[node].parent;
                    for(let field=0;field<16;field+=1)animator.scratch[field]=animator.globals[matrix+field];
                    if(parent>=0)renderer3DMultiplyAt(animator.baseGlobals,matrix,animator.baseGlobals,parent*16,animator.scratch,0);
                    else for(let field=0;field<16;field+=1)animator.baseGlobals[matrix+field]=animator.scratch[field];
                    const correction=node*3;
                    renderer3DEulerInto(animator.rotationScratch,animator.nodeRotationOffsets[correction],animator.nodeRotationOffsets[correction+1],animator.nodeRotationOffsets[correction+2]);
                    renderer3DMultiplyAt(animator.globals,matrix,animator.scratch,0,animator.rotationScratch,0);
                    if(parent>=0){for(let field=0;field<16;field+=1)animator.scratch[field]=animator.globals[matrix+field];renderer3DMultiplyAt(animator.globals,matrix,animator.globals,parent*16,animator.scratch,0);}}
                for(let bone=0;bone<animation.bones.length;bone+=1){const value=animation.bones[bone];renderer3DMultiplyAt(animator.palette,bone*16,animator.globals,value.node*16,value.inverse,0);renderer3DMultiplyAt(animator.basePalette,bone*16,animator.baseGlobals,value.node*16,value.inverse,0);}
                for(let bone=animation.bones.length;bone<128;bone+=1){const offset=bone*16;for(let field=0;field<16;field+=1)animator.basePalette[offset+field]=animator.palette[offset+field]=field%5===0?1:0;}animator.revision=(animator.revision+1)>>>0||1;}
            function renderer3DCreateModelAnimator(modelHandle){const model=renderer3DModels.get(modelHandle);if(!model||!model.animation||renderer3DAnimators.size>=128||!renderer3DInitialize()){renderer3DLastError=48;return 0;}
                const nodes=model.animation.nodes.length,handle=renderer3DHandle(),animator={production:true,model:modelHandle,clipIndex:-1,destinationClip:-1,mode:0,destinationMode:0,time:0,previous:0,destinationTime:0,timeRemainder:0,destinationTimeRemainder:0,speed:100,complete:false,destinationComplete:false,
                    fadeElapsed:0,fadeDuration:0,rootMode:0,rootDelta:new Float32Array(4),eventQueue:new Int32Array(32),eventHead:0,eventCount:0,eventOverflowed:false,droppedEventCount:0,locals:new Float32Array(nodes*10),destinationLocals:new Float32Array(nodes*10),
                    nodeRotationOffsets:new Float32Array(nodes*3),baseGlobals:new Float32Array(nodes*16),basePalette:new Float32Array(128*16),rotationScratch:new Float32Array(16),
                    globals:new Float32Array(nodes*16),palette:new Float32Array(128*16),scratch:new Float32Array(16),socketScratch:new Float32Array(48),timeResult:new Uint32Array(3),revision:0,mutableBytes:model.animation.animatorBytes};
                animator.rootPrevious=animator.socketScratch.subarray(0,4);animator.rootCurrent=animator.socketScratch.subarray(4,8);animator.rootStart=animator.socketScratch.subarray(8,12);animator.rootEnd=animator.socketScratch.subarray(12,16);animator.rootQuaternion=animator.socketScratch.subarray(16,20);animator.sourceDelta=animator.socketScratch.subarray(20,24);animator.destinationDelta=animator.socketScratch.subarray(24,28);renderer3DAnimators.set(handle,animator);renderer3DUpdateModelPose(animator);return handle;}
            function renderer3DClearModelEvents(animator){animator.eventHead=animator.eventCount=0;animator.eventOverflowed=false;animator.droppedEventCount=0;}
            function renderer3DDropModelEvents(animator,count){if(count<=0)return;animator.eventOverflowed=true;animator.droppedEventCount=Math.min(0xffffffff,animator.droppedEventCount+count);renderer3DLastError=49;}
            function renderer3DQueueModelTimeZero(animator,clip){renderer3DQueueModelEventRange(animator,clip,0,0,true);}
            function renderer3DPlayModelAnimator(animator,clipIndex,mode,speed){const model=animator&&animator.production?renderer3DModels.get(animator.model):null;if(!model||clipIndex<0||clipIndex>=model.animation.clips.length||mode<1||mode>3||speed<1||speed>1000){renderer3DLastError=48;return false;}
                animator.clipIndex=clipIndex;animator.destinationClip=-1;animator.mode=mode;animator.destinationMode=0;animator.speed=speed;animator.time=animator.previous=animator.destinationTime=animator.timeRemainder=animator.destinationTimeRemainder=0;animator.fadeElapsed=animator.fadeDuration=0;animator.complete=animator.destinationComplete=false;renderer3DClearModelEvents(animator);animator.rootDelta.fill(0);renderer3DQueueModelTimeZero(animator,model.animation.clips[clipIndex]);renderer3DUpdateModelPose(animator);return true;}
            function renderer3DCrossFadeModelAnimator(animator,clipIndex,fade,mode){const model=animator&&animator.production?renderer3DModels.get(animator.model):null;if(!model||clipIndex<0||clipIndex>=model.animation.clips.length||fade<0||fade>600000||mode<1||mode>3){renderer3DLastError=48;return false;}
                if(animator.clipIndex<0||fade===0)return renderer3DPlayModelAnimator(animator,clipIndex,mode,animator.speed);if(animator.destinationClip>=0){animator.clipIndex=animator.destinationClip;animator.mode=animator.destinationMode;animator.time=animator.previous=animator.destinationTime;animator.timeRemainder=animator.destinationTimeRemainder;animator.complete=animator.destinationComplete;}
                animator.destinationClip=clipIndex;animator.destinationMode=mode;animator.destinationTime=animator.destinationTimeRemainder=0;animator.destinationComplete=false;animator.fadeElapsed=0;animator.fadeDuration=fade;animator.complete=false;renderer3DQueueModelTimeZero(animator,model.animation.clips[clipIndex]);renderer3DUpdateModelPose(animator);return true;}
            function renderer3DQueueModelEventRange(animator,clip,minimum,maximum,includeZero){const model=renderer3DModels.get(animator.model);for(let ordinal=0;ordinal<clip.eventCount;ordinal+=1){const index=clip.firstEvent+ordinal,event=model.animation.events[index];if(!((includeZero&&event.time===0)||(event.time>minimum&&event.time<=maximum)))continue;if(animator.eventCount>=32){renderer3DDropModelEvents(animator,1);continue;}animator.eventQueue[(animator.eventHead+animator.eventCount)%32]=index;animator.eventCount+=1;}}
            function renderer3DCountModelEventRange(animator,clip,minimum,maximum,includeZero){const model=renderer3DModels.get(animator.model);let result=0;for(let ordinal=0;ordinal<clip.eventCount;ordinal+=1){const event=model.animation.events[clip.firstEvent+ordinal];if((includeZero&&event.time===0)||(event.time>minimum&&event.time<=maximum))result+=1;}return result;}
            function renderer3DQueueModelEvents(animator,clip,previous,current,advance,mode){if(clip.eventCount===0)return;const wraps=mode===1?Math.trunc((previous+advance)/clip.duration):0;if(wraps===0){renderer3DQueueModelEventRange(animator,clip,previous,current,false);return;}renderer3DQueueModelEventRange(animator,clip,previous,clip.duration,false);let intermediate=wraps-1;while(intermediate&&animator.eventCount<32){renderer3DQueueModelEventRange(animator,clip,0,clip.duration,true);intermediate-=1;}if(intermediate)renderer3DDropModelEvents(animator,intermediate*renderer3DCountModelEventRange(animator,clip,0,clip.duration,true));renderer3DQueueModelEventRange(animator,clip,0,current,true);}
            function renderer3DAdvanceModelTime(clip,time,advance,mode,output){const total=time+advance;if(mode===1){output[0]=total%clip.duration;output[1]=Math.trunc(total/clip.duration);output[2]=0;return;}output[0]=Math.min(total,clip.duration);output[1]=0;output[2]=total>=clip.duration?1:0;}
            function renderer3DModelRootSample(model,clipIndex,time,output,quaternion){const animation=model.animation,clip=animation.clips[clipIndex];if(clip.root<0)return 0;const root=animation.roots[clip.root],node=animation.nodes[root.node];for(let axis=0;axis<3;axis+=1)output[axis]=node.translation[axis];quaternion.set(node.rotation);
                for(let ordinal=0;ordinal<clip.trackCount;ordinal+=1){const track=animation.tracks[clip.firstTrack+ordinal];if(track.node!==root.node)continue;if(track.translation)renderer3DModelChannelInto(animation,track,"translation",time,clip,output,0);if(track.rotation)renderer3DModelChannelInto(animation,track,"rotation",time,clip,quaternion,0);}
                output[3]=root.yaw?Math.atan2(2*(quaternion[3]*quaternion[1]+quaternion[0]*quaternion[2]),1-2*(quaternion[1]*quaternion[1]+quaternion[2]*quaternion[2]))*180/Math.PI:0;return root.axes|(root.yaw?8:0);}
            function renderer3DRootDelta(current,previous,angle){let value=current-previous;if(angle){while(value>180)value-=360;while(value< -180)value+=360;}return value;}
            function renderer3DScaledModelAdvance(animator,delta,destination){const field=destination?"destinationTimeRemainder":"timeRemainder",scaled=delta*animator.speed+animator[field];animator[field]=scaled%100;return Math.trunc(scaled/100);}
            function renderer3DModelRootTransition(animator,model,clipIndex,previous,current,wraps,output){output.fill(0);const rootPrevious=animator.rootPrevious,rootCurrent=animator.rootCurrent,rootStart=animator.rootStart,rootEnd=animator.rootEnd,axes=renderer3DModelRootSample(model,clipIndex,previous,rootPrevious,animator.rootQuaternion);if(!axes)return;renderer3DModelRootSample(model,clipIndex,current,rootCurrent,animator.rootQuaternion);if(wraps){const clip=model.animation.clips[clipIndex];renderer3DModelRootSample(model,clipIndex,0,rootStart,animator.rootQuaternion);renderer3DModelRootSample(model,clipIndex,clip.duration,rootEnd,animator.rootQuaternion);}for(let axis=0;axis<4;axis+=1)if(axes&(1<<axis))output[axis]=wraps?renderer3DRootDelta(rootEnd[axis],rootPrevious[axis],axis===3)+(wraps-1)*renderer3DRootDelta(rootEnd[axis],rootStart[axis],axis===3)+renderer3DRootDelta(rootCurrent[axis],rootStart[axis],axis===3):renderer3DRootDelta(rootCurrent[axis],rootPrevious[axis],axis===3);}
            function renderer3DPromoteModelDestination(animator){animator.clipIndex=animator.destinationClip;animator.mode=animator.destinationMode;animator.time=animator.previous=animator.destinationTime;animator.timeRemainder=animator.destinationTimeRemainder;animator.complete=animator.destinationComplete;animator.destinationClip=-1;animator.destinationTime=animator.destinationTimeRemainder=0;animator.destinationComplete=false;animator.fadeElapsed=animator.fadeDuration=0;}
            function renderer3DAdvanceModelCurrent(animator,model,delta){const previous=animator.time,advance=renderer3DScaledModelAdvance(animator,delta,false),clip=model.animation.clips[animator.clipIndex];animator.previous=previous;renderer3DAdvanceModelTime(clip,previous,advance,animator.mode,animator.timeResult);animator.time=animator.timeResult[0];animator.complete=animator.timeResult[2]!==0;renderer3DQueueModelEvents(animator,clip,previous,animator.time,advance,animator.mode);if(animator.rootMode){renderer3DModelRootTransition(animator,model,animator.clipIndex,previous,animator.time,animator.timeResult[1],animator.sourceDelta);for(let axis=0;axis<4;axis+=1)animator.rootDelta[axis]+=animator.sourceDelta[axis];}}
            function renderer3DUpdateModelAnimator(animator,delta){const model=animator&&animator.production?renderer3DModels.get(animator.model):null;if(!model||delta<0||delta>600000){renderer3DLastError=48;return false;}if(animator.clipIndex<0){renderer3DUpdateModelPose(animator);return true;}let remaining=delta;
                if(animator.destinationClip>=0){const fadeRemaining=animator.fadeDuration-animator.fadeElapsed,fadeDelta=Math.min(remaining,fadeRemaining),sourcePrevious=animator.time,destinationPrevious=animator.destinationTime,sourceAdvance=renderer3DScaledModelAdvance(animator,fadeDelta,false),destinationAdvance=renderer3DScaledModelAdvance(animator,fadeDelta,true),sourceClip=model.animation.clips[animator.clipIndex],destinationClip=model.animation.clips[animator.destinationClip];animator.previous=sourcePrevious;renderer3DAdvanceModelTime(sourceClip,sourcePrevious,sourceAdvance,animator.mode,animator.timeResult);animator.time=animator.timeResult[0];const sourceWraps=animator.timeResult[1];renderer3DAdvanceModelTime(destinationClip,destinationPrevious,destinationAdvance,animator.destinationMode,animator.timeResult);animator.destinationTime=animator.timeResult[0];animator.destinationComplete=animator.timeResult[2]!==0;const destinationWraps=animator.timeResult[1];renderer3DQueueModelEvents(animator,destinationClip,destinationPrevious,animator.destinationTime,destinationAdvance,animator.destinationMode);
                    if(animator.rootMode){renderer3DModelRootTransition(animator,model,animator.clipIndex,sourcePrevious,animator.time,sourceWraps,animator.sourceDelta);renderer3DModelRootTransition(animator,model,animator.destinationClip,destinationPrevious,animator.destinationTime,destinationWraps,animator.destinationDelta);const weight=((animator.fadeElapsed/animator.fadeDuration)+((animator.fadeElapsed+fadeDelta)/animator.fadeDuration))*.5;for(let axis=0;axis<4;axis+=1)animator.rootDelta[axis]+=animator.sourceDelta[axis]+(animator.destinationDelta[axis]-animator.sourceDelta[axis])*weight;}
                    animator.fadeElapsed+=fadeDelta;animator.complete=animator.destinationComplete;remaining-=fadeDelta;if(animator.fadeElapsed>=animator.fadeDuration)renderer3DPromoteModelDestination(animator);}
                if(animator.destinationClip<0&&remaining)renderer3DAdvanceModelCurrent(animator,model,remaining);renderer3DUpdateModelPose(animator);return true;}
            function renderer3DTakeModelEvent(animator,name=null){if(!animator||!animator.production)return 0;const model=renderer3DModels.get(animator.model);for(let ordinal=0;ordinal<animator.eventCount;ordinal+=1){const queue=(animator.eventHead+ordinal)%32,index=animator.eventQueue[queue];if(name!==null&&model.animation.events[index].name!==name)continue;
                    for(let move=ordinal;move+1<animator.eventCount;move+=1)animator.eventQueue[(animator.eventHead+move)%32]=animator.eventQueue[(animator.eventHead+move+1)%32];animator.eventCount-=1;return index+1;}return 0;}
            function renderer3DModelAnimationValue(model,property,index){if(!model){renderer3DLastError=5;return 0;}const animation=model.animation;if(property===1)return animation?1:0;if(property===2)return animation?animation.bones.length:0;if(property===3)return animation?animation.clips.length:0;if(property===4)return animation?animation.sockets.length:0;if(property===5)return animation?animation.bytes:0;if(!animation)return 0;
                if(property===6||property===7){if(index<0||index>=animation.clips.length){renderer3DLastError=48;return 0;}return property===6?animation.clips[index].duration:animation.clips[index].rate;}if(property===8)return animation.events.length;if(property===9)return animation.nodes.length;if(property===10){if(index<=0||index>animation.events.length){renderer3DLastError=48;return 0;}return animation.events[index-1].value;}if(property===11)return animation.fileBytes;if(property===12)return animation.residentBytes;if(property===13)return animation.animatorBytes;
                if(property>=14&&property<=16){if(index<0||index>=animation.clips.length){renderer3DLastError=48;return 0;}const clip=animation.clips[index];if(property===14)return clip.samples;if(property===15)return clip.loop?1:0;return clip.eventCount;}if(property===17||property===18){if(index<=0||index>animation.events.length){renderer3DLastError=48;return 0;}const event=animation.events[index-1];return property===17?event.clip:event.time;}if(property===19){if(index<0||index>=animation.sockets.length){renderer3DLastError=48;return 0;}return animation.sockets[index].node;}renderer3DLastError=48;return 0;}
            function renderer3DSetModelAnimatorTime(animator,time){const model=animator&&animator.production?renderer3DModels.get(animator.model):null,clip=model&&animator.clipIndex>=0?model.animation.clips[animator.clipIndex]:null;if(!model||!clip||time<0||time>clip.duration){renderer3DLastError=48;return false;}animator.destinationClip=-1;animator.destinationMode=0;animator.destinationTime=animator.destinationTimeRemainder=0;animator.destinationComplete=false;animator.fadeElapsed=animator.fadeDuration=0;animator.time=animator.previous=time;animator.timeRemainder=0;animator.complete=animator.mode!==1&&time===clip.duration;renderer3DClearModelEvents(animator);animator.rootDelta.fill(0);renderer3DUpdateModelPose(animator);return true;}
            function renderer3DAnimatorProductionValue(animator,property){if(!animator||!animator.production){renderer3DLastError=48;return 0;}if(property===1)return animator.destinationClip;if(property===2)return animator.timeRemainder;if(property===3)return animator.destinationTimeRemainder;if(property===4)return animator.destinationTime;if(property===5)return animator.eventOverflowed?1:0;if(property===6)return animator.droppedEventCount;if(property===7)return animator.mode;if(property===8)return animator.destinationMode;if(property===9)return animator.revision;if(property===10)return animator.mutableBytes;renderer3DLastError=48;return 0;}
            function renderer3DModelSocketValue(animator,index,property,objectHandle=0,ignoreOffsets=0){const model=animator&&animator.production?renderer3DModels.get(animator.model):null;if(!model||index<0||index>=model.animation.sockets.length||ignoreOffsets<0||ignoreOffsets>1){renderer3DLastError=48;return 0;}const socket=model.animation.sockets[index],first=animator.socketScratch,second=animator.socketScratch;
                renderer3DPoseIntoAt(first,0,socket.translation[0],socket.translation[1],socket.translation[2],socket.rotation[0],socket.rotation[1],socket.rotation[2],socket.rotation[3],socket.scale[0],socket.scale[1],socket.scale[2]);renderer3DMultiplyAt(second,16,ignoreOffsets?animator.baseGlobals:animator.globals,socket.node*16,first,0);let offset=16;
                if(objectHandle){const object=renderer3DObjects.get(objectHandle);if(!object||renderer3DAnimators.get(object.animator)!==animator){renderer3DLastError=48;return 0;}if(object.ignoreNodeOffsets)renderer3DMultiplyAt(second,16,animator.baseGlobals,socket.node*16,first,0);renderer3DModelInto(animator.scratch,object);renderer3DMultiplyAt(first,32,animator.scratch,0,second,16);offset=32;}
                if(property>=1&&property<=3)return Math.round(first[offset+11+property]*1000);const fields=[0,1,2,4,5,6,8,9,10];if(property>=4&&property<=12)return Math.round(first[offset+fields[property-4]]*1000);renderer3DLastError=48;return 0;}

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

            function renderer3DClearPendingCamera(){renderer3DPendingCamera.hasProjection=false;renderer3DPendingCamera.hasUp=false;}
            function renderer3DCameraWorldValue(value){return Number.isSafeInteger(value)&&value>=-renderer3DCameraWorldBound&&value<=renderer3DCameraWorldBound;}
            function renderer3DValidatePendingCamera(){const pending=renderer3DPendingCamera;if(!pending.hasProjection||!pending.hasUp){renderer3DLastError=renderer3DCameraErrorPendingIncomplete;return false;}
                const fx=pending.target[0]-pending.position[0],fy=pending.target[1]-pending.position[1],fz=pending.target[2]-pending.position[2],forwardLengthSquared=fx*fx+fy*fy+fz*fz;
                if(!(forwardLengthSquared>0)){renderer3DLastError=renderer3DCameraErrorZeroViewDirection;return false;}
                const ux=pending.up[0],uy=pending.up[1],uz=pending.up[2],upLengthSquared=ux*ux+uy*uy+uz*uz;
                if(!(upLengthSquared>0)){renderer3DLastError=renderer3DCameraErrorInvalidUp;return false;}
                const rx=uy*fz-uz*fy,ry=uz*fx-ux*fz,rz=ux*fy-uy*fx,rightLengthSquared=rx*rx+ry*ry+rz*rz;
                if(rightLengthSquared<=forwardLengthSquared*upLengthSquared*.00000001){renderer3DLastError=renderer3DCameraErrorParallelUp;return false;}return true;}
            function renderer3DPromotePendingCamera(){const pending=renderer3DPendingCamera;renderer3DCamera.position=pending.position.slice();renderer3DCamera.target=pending.target.slice();renderer3DCamera.up=pending.up.slice();renderer3DCamera.fov=pending.fov;renderer3DCamera.near=pending.near;renderer3DCamera.far=pending.far;renderer3DClearPendingCamera();}
            function renderer3DIdentity() { return [1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1]; }
            function renderer3DMultiply(a,b){const r=new Array(16).fill(0);for(let col=0;col<4;col+=1)for(let row=0;row<4;row+=1)for(let k=0;k<4;k+=1)r[col*4+row]+=a[k*4+row]*b[col*4+k];return r;}
            function renderer3DIdentityInto(output){output.fill(0);output[0]=output[5]=output[10]=output[15]=1;return output;}
            function renderer3DMultiplyInto(output,left,right){for(let column=0;column<4;column+=1)for(let row=0;row<4;row+=1){let value=0;for(let index=0;index<4;index+=1)value+=left[index*4+row]*right[column*4+index];output[column*4+row]=value;}return output;}
            function renderer3DEulerInto(output,xDegrees,yDegrees,zDegrees) {
                const x=xDegrees*Math.PI/180,y=yDegrees*Math.PI/180,z=zDegrees*Math.PI/180;
                const cx=Math.cos(x),sx=Math.sin(x),cy=Math.cos(y),sy=Math.sin(y),cz=Math.cos(z),sz=Math.sin(z);
                renderer3DIdentityInto(output);
                // Native uses row-vector Rx * Ry * Rz. Web's column-vector matrix
                // is its transpose, not an angle negation: positive yaw turns +Z toward +X.
                output[0]=cz*cy;output[1]=sz*cy;output[2]=-sy;
                output[4]=cz*sy*sx-sz*cx;output[5]=sz*sy*sx+cz*cx;output[6]=cy*sx;
                output[8]=cz*sy*cx+sz*sx;output[9]=sz*sy*cx-cz*sx;output[10]=cy*cx;
                return output;
            }

            function renderer3DApplyPivot(output,object) {
                const rotation=object.pivotRotation,pivot=object.pivotPosition;
                if(!rotation||(!rotation[0]&&!rotation[1]&&!rotation[2]))return output;
                const matrix=renderer3DEulerInto(renderer3DPivotScratch,rotation[0],rotation[1],rotation[2]);
                for(let column=0;column<4;column+=1){
                    const offset=column*4,x=output[offset]-(column===3?pivot[0]:0),y=output[offset+1]-(column===3?pivot[1]:0),z=output[offset+2]-(column===3?pivot[2]:0);
                    output[offset]=matrix[0]*x+matrix[4]*y+matrix[8]*z+(column===3?pivot[0]:0);
                    output[offset+1]=matrix[1]*x+matrix[5]*y+matrix[9]*z+(column===3?pivot[1]:0);
                    output[offset+2]=matrix[2]*x+matrix[6]*y+matrix[10]*z+(column===3?pivot[2]:0);
                }
                return output;
            }

            function renderer3DApplyCull(object,doubleSided) {
                const gl=renderer3DGl,mode=object.cullMode||0;
                if(mode===1||(mode===0&&doubleSided))gl.disable(gl.CULL_FACE);
                else {gl.enable(gl.CULL_FACE);gl.cullFace(mode===3?gl.FRONT:gl.BACK);}
            }

            function renderer3DModelInto(output,object){renderer3DEulerInto(output,object.rotation[0],object.rotation[1],object.rotation[2]);for(let column=0;column<3;column+=1)for(let row=0;row<3;row+=1)output[column*4+row]*=object.scale[column];output[12]=object.position[0];output[13]=object.position[1];output[14]=object.position[2];return renderer3DApplyPivot(output,object);}
            function renderer3DViewInto(output){const eye=renderer3DCamera.position,target=renderer3DCamera.target,up=renderer3DCamera.up;let zx=target[0]-eye[0],zy=target[1]-eye[1],zz=target[2]-eye[2],length=Math.hypot(zx,zy,zz);zx/=length;zy/=length;zz/=length;let xx=up[1]*zz-up[2]*zy,xy=up[2]*zx-up[0]*zz,xz=up[0]*zy-up[1]*zx;length=Math.hypot(xx,xy,xz);xx/=length;xy/=length;xz/=length;const yx=zy*xz-zz*xy,yy=zz*xx-zx*xz,yz=zx*xy-zy*xx;output[0]=xx;output[1]=yx;output[2]=zx;output[3]=0;output[4]=xy;output[5]=yy;output[6]=zy;output[7]=0;output[8]=xz;output[9]=yz;output[10]=zz;output[11]=0;output[12]=-(xx*eye[0]+xy*eye[1]+xz*eye[2]);output[13]=-(yx*eye[0]+yy*eye[1]+yz*eye[2]);output[14]=-(zx*eye[0]+zy*eye[1]+zz*eye[2]);output[15]=1;return output;}
            function renderer3DProjectionInto(output,aspect){const f=1/Math.tan(renderer3DCamera.fov*Math.PI/360),near=renderer3DCamera.near,far=renderer3DCamera.far;output.fill(0);output[0]=f/aspect;output[5]=f;output[10]=(far+near)/(far-near);output[11]=1;output[14]=-2*far*near/(far-near);return output;}
            function renderer3DLookAtInto(output,eyeX,eyeY,eyeZ,targetX,targetY,targetZ){let zx=targetX-eyeX,zy=targetY-eyeY,zz=targetZ-eyeZ,length=Math.hypot(zx,zy,zz)||1;zx/=length;zy/=length;zz/=length;
                let xx=zz,xz=-zx;length=Math.hypot(xx,xz);if(length<.000001){xx=1;xz=0;}else{xx/=length;xz/=length;}const yx=zy*xz,yz=-zy*xx,yy=zz*xx-zx*xz;
                output[0]=xx;output[1]=yx;output[2]=zx;output[3]=0;output[4]=0;output[5]=yy;output[6]=zy;output[7]=0;output[8]=xz;output[9]=yz;output[10]=zz;output[11]=0;
                output[12]=-(xx*eyeX+xz*eyeZ);output[13]=-(yx*eyeX+yy*eyeY+yz*eyeZ);output[14]=-(zx*eyeX+zy*eyeY+zz*eyeZ);output[15]=1;return output;}
            function renderer3DUpdateShadowMatrix(){let eyeX,eyeY,eyeZ,targetX,targetY,targetZ,near,far;if(renderer3DShadowCaster===1){if(renderer3DDirectionalDirection[3]<.5)return false;
                    targetX=renderer3DShadowCenter[0];targetY=renderer3DShadowCenter[1];targetZ=renderer3DShadowCenter[2];far=renderer3DShadowArea[3];near=renderer3DShadowArea[2];
                    eyeX=targetX+renderer3DDirectionalDirection[0]*far*.5;eyeY=targetY+renderer3DDirectionalDirection[1]*far*.5;eyeZ=targetZ+renderer3DDirectionalDirection[2]*far*.5;
                    renderer3DLookAtInto(renderer3DShadowViewScratch,eyeX,eyeY,eyeZ,targetX,targetY,targetZ);if(renderer3DShadowResolution>0){const lightX=targetX*renderer3DShadowViewScratch[0]+targetY*renderer3DShadowViewScratch[4]+targetZ*renderer3DShadowViewScratch[8]+renderer3DShadowViewScratch[12],lightY=targetX*renderer3DShadowViewScratch[1]+targetY*renderer3DShadowViewScratch[5]+targetZ*renderer3DShadowViewScratch[9]+renderer3DShadowViewScratch[13],texelX=renderer3DShadowArea[0]/renderer3DShadowResolution,texelY=renderer3DShadowArea[1]/renderer3DShadowResolution;renderer3DShadowViewScratch[12]+=Math.round(lightX/texelX)*texelX-lightX;renderer3DShadowViewScratch[13]+=Math.round(lightY/texelY)*texelY-lightY;}renderer3DShadowProjectionScratch.fill(0);
                    renderer3DShadowProjectionScratch[0]=2/renderer3DShadowArea[0];renderer3DShadowProjectionScratch[5]=2/renderer3DShadowArea[1];renderer3DShadowProjectionScratch[10]=2/(far-near);
                    renderer3DShadowProjectionScratch[14]=-(far+near)/(far-near);renderer3DShadowProjectionScratch[15]=1;
                }else if(renderer3DShadowCaster===2){const offset=renderer3DShadowSlot*4;if(renderer3DLocalPositionType[offset+3]!==2||renderer3DLocalDirectionRange[offset+3]<=0)return false;
                    eyeX=renderer3DLocalPositionType[offset];eyeY=renderer3DLocalPositionType[offset+1];eyeZ=renderer3DLocalPositionType[offset+2];targetX=eyeX+renderer3DLocalDirectionRange[offset];targetY=eyeY+renderer3DLocalDirectionRange[offset+1];targetZ=eyeZ+renderer3DLocalDirectionRange[offset+2];
                    renderer3DLookAtInto(renderer3DShadowViewScratch,eyeX,eyeY,eyeZ,targetX,targetY,targetZ);const f=1/Math.tan(Math.acos(Math.max(-1,Math.min(1,renderer3DLocalCone[offset+1])))),range=renderer3DLocalDirectionRange[offset+3];
                    renderer3DShadowProjectionScratch.fill(0);renderer3DShadowProjectionScratch[0]=f;renderer3DShadowProjectionScratch[5]=f;renderer3DShadowProjectionScratch[10]=(range+1)/(range-1);renderer3DShadowProjectionScratch[11]=1;renderer3DShadowProjectionScratch[14]=-2*range/(range-1);
                }else return false;renderer3DMultiplyInto(renderer3DShadowMatrixScratch,renderer3DShadowProjectionScratch,renderer3DShadowViewScratch);return true;}
            function renderer3DCaptureM5Bundle(){return{msaa:renderer3DMsaaTarget,shadowFramebuffer:renderer3DShadowFramebuffer,shadowTexture:renderer3DShadowTexture,sceneFramebuffer:renderer3DSceneFramebuffer,sceneTexture:renderer3DSceneTexture,sceneDepth:renderer3DSceneDepth,linearDepthFramebuffer:renderer3DLinearDepthFramebuffer,linearDepthTexture:renderer3DLinearDepthTexture,distortionFramebuffer:renderer3DDistortionFramebuffer,distortionTexture:renderer3DDistortionTexture,distortionScratchFramebuffer:renderer3DDistortionScratchFramebuffer,distortionScratchTexture:renderer3DDistortionScratchTexture,bloomFramebufferA:renderer3DBloomFramebufferA,bloomTextureA:renderer3DBloomTextureA,bloomFramebufferB:renderer3DBloomFramebufferB,bloomTextureB:renderer3DBloomTextureB,shadowEffective:renderer3DShadowEffective,hdrEffective:renderer3DHdrEffective,bloomEffective:renderer3DBloomEffective,postEffective:renderer3DPostEffective,toneEffective:renderer3DToneMappingEffective,softDepthEffective:renderer3DSoftDepthEffective,softDepthFallbackReason:renderer3DSoftDepthFallbackReason,distortionEffective:renderer3DDistortionEffective,distortionFallbackReason:renderer3DDistortionFallbackReason,multipass:renderer3DMultipassActive,shadowResolution:renderer3DShadowResolution,bloomWidth:renderer3DBloomWidth,bloomHeight:renderer3DBloomHeight,softDepthWidth:renderer3DSoftDepthWidth,softDepthHeight:renderer3DSoftDepthHeight,distortionWidth:renderer3DDistortionWidth,distortionHeight:renderer3DDistortionHeight,width:renderer3DM5Width,height:renderer3DM5Height,targetBytes:renderer3DTargetBytes,shadowBytes:renderer3DShadowBytes,sceneBytes:renderer3DSceneBytes,bloomBytes:renderer3DBloomBytes,softDepthBytes:renderer3DSoftDepthBytes,distortionBytes:renderer3DDistortionBytes,samples:renderer3DEffectiveSamples};}
            function renderer3DClearM5Bundle(){
                renderer3DMsaaTarget=renderer3DShadowFramebuffer=renderer3DShadowTexture=renderer3DSceneFramebuffer=renderer3DSceneTexture=renderer3DSceneDepth=null;renderer3DLinearDepthFramebuffer=renderer3DLinearDepthTexture=null;renderer3DDistortionFramebuffer=renderer3DDistortionTexture=renderer3DDistortionScratchFramebuffer=renderer3DDistortionScratchTexture=null;
                renderer3DBloomFramebufferA=renderer3DBloomTextureA=renderer3DBloomFramebufferB=renderer3DBloomTextureB=null;
                renderer3DShadowEffective=renderer3DHdrEffective=renderer3DBloomEffective=renderer3DPostEffective=renderer3DToneMappingEffective=false;renderer3DSoftDepthEffective=renderer3DDistortionEffective=0;renderer3DSoftDepthFallbackReason=renderer3DDistortionFallbackReason=0;renderer3DMultipassActive=false;
                renderer3DShadowResolution=renderer3DBloomWidth=renderer3DBloomHeight=renderer3DSoftDepthWidth=renderer3DSoftDepthHeight=renderer3DDistortionWidth=renderer3DDistortionHeight=renderer3DM5Width=renderer3DM5Height=0;renderer3DTargetBytes=renderer3DShadowBytes=renderer3DSceneBytes=renderer3DBloomBytes=renderer3DSoftDepthBytes=renderer3DDistortionBytes=0;renderer3DEffectiveSamples=1;}
            function renderer3DApplyM5Bundle(bundle){renderer3DMsaaTarget=bundle.msaa;renderer3DShadowFramebuffer=bundle.shadowFramebuffer;renderer3DShadowTexture=bundle.shadowTexture;renderer3DSceneFramebuffer=bundle.sceneFramebuffer;renderer3DSceneTexture=bundle.sceneTexture;renderer3DSceneDepth=bundle.sceneDepth;renderer3DLinearDepthFramebuffer=bundle.linearDepthFramebuffer;renderer3DLinearDepthTexture=bundle.linearDepthTexture;renderer3DDistortionFramebuffer=bundle.distortionFramebuffer;renderer3DDistortionTexture=bundle.distortionTexture;renderer3DDistortionScratchFramebuffer=bundle.distortionScratchFramebuffer;renderer3DDistortionScratchTexture=bundle.distortionScratchTexture;renderer3DBloomFramebufferA=bundle.bloomFramebufferA;renderer3DBloomTextureA=bundle.bloomTextureA;renderer3DBloomFramebufferB=bundle.bloomFramebufferB;renderer3DBloomTextureB=bundle.bloomTextureB;renderer3DShadowEffective=bundle.shadowEffective;renderer3DHdrEffective=bundle.hdrEffective;renderer3DBloomEffective=bundle.bloomEffective;renderer3DPostEffective=bundle.postEffective;renderer3DToneMappingEffective=bundle.toneEffective;renderer3DSoftDepthEffective=bundle.softDepthEffective;renderer3DSoftDepthFallbackReason=bundle.softDepthFallbackReason;renderer3DDistortionEffective=bundle.distortionEffective;renderer3DDistortionFallbackReason=bundle.distortionFallbackReason;renderer3DMultipassActive=bundle.multipass;renderer3DShadowResolution=bundle.shadowResolution;renderer3DBloomWidth=bundle.bloomWidth;renderer3DBloomHeight=bundle.bloomHeight;renderer3DSoftDepthWidth=bundle.softDepthWidth;renderer3DSoftDepthHeight=bundle.softDepthHeight;renderer3DDistortionWidth=bundle.distortionWidth;renderer3DDistortionHeight=bundle.distortionHeight;renderer3DM5Width=bundle.width;renderer3DM5Height=bundle.height;renderer3DTargetBytes=bundle.targetBytes;renderer3DShadowBytes=bundle.shadowBytes;renderer3DSceneBytes=bundle.sceneBytes;renderer3DBloomBytes=bundle.bloomBytes;renderer3DSoftDepthBytes=bundle.softDepthBytes;renderer3DDistortionBytes=bundle.distortionBytes;renderer3DEffectiveSamples=bundle.samples;}
            function renderer3DDeleteM5Bundle(bundle){const gl=renderer3DGl;if(!gl||!bundle)return;renderer3DDeleteMsaaTarget(bundle.msaa);if(bundle.shadowFramebuffer)gl.deleteFramebuffer(bundle.shadowFramebuffer);if(bundle.shadowTexture)gl.deleteTexture(bundle.shadowTexture);if(bundle.sceneFramebuffer)gl.deleteFramebuffer(bundle.sceneFramebuffer);if(bundle.sceneTexture)gl.deleteTexture(bundle.sceneTexture);if(bundle.sceneDepth)gl.deleteTexture(bundle.sceneDepth);if(bundle.linearDepthFramebuffer)gl.deleteFramebuffer(bundle.linearDepthFramebuffer);if(bundle.linearDepthTexture)gl.deleteTexture(bundle.linearDepthTexture);if(bundle.distortionFramebuffer)gl.deleteFramebuffer(bundle.distortionFramebuffer);if(bundle.distortionTexture)gl.deleteTexture(bundle.distortionTexture);if(bundle.distortionScratchFramebuffer)gl.deleteFramebuffer(bundle.distortionScratchFramebuffer);if(bundle.distortionScratchTexture)gl.deleteTexture(bundle.distortionScratchTexture);if(bundle.bloomFramebufferA)gl.deleteFramebuffer(bundle.bloomFramebufferA);if(bundle.bloomTextureA)gl.deleteTexture(bundle.bloomTextureA);if(bundle.bloomFramebufferB)gl.deleteFramebuffer(bundle.bloomFramebufferB);if(bundle.bloomTextureB)gl.deleteTexture(bundle.bloomTextureB);}
            function renderer3DDeleteM5Targets(){const bundle=renderer3DCaptureM5Bundle();renderer3DClearM5Bundle();renderer3DDeleteM5Bundle(bundle);}
            function renderer3DDeleteMsaaTarget(target){if(!target)return;const gl=renderer3DGl;if(target.framebuffer)gl.deleteFramebuffer(target.framebuffer);if(target.color)gl.deleteRenderbuffer(target.color);if(target.depth)gl.deleteRenderbuffer(target.depth);}
            function renderer3DCreateMsaaTarget(width,height,hdr){
                const gl=renderer3DGl,colorFormat=hdr?gl.RGBA16F:gl.RGBA8;
                if(renderer3DRequestedSamples<=1||globalThis.SMILE_TEST_RENDERER3D_FORCE_MSAA_FAILURE)return null;
                const colorSamples=gl.getInternalformatParameter(gl.RENDERBUFFER,colorFormat,gl.SAMPLES),depthSamples=gl.getInternalformatParameter(gl.RENDERBUFFER,gl.DEPTH_COMPONENT24,gl.SAMPLES),maximum=gl.getParameter(gl.MAX_SAMPLES)||0;
                if(!colorSamples||!depthSamples)return null;
                for(const samples of [4,2]){
                    if(samples>renderer3DRequestedSamples||samples>maximum||!colorSamples.includes(samples)||!depthSamples.includes(samples))continue;
                    const target={framebuffer:gl.createFramebuffer(),color:gl.createRenderbuffer(),depth:gl.createRenderbuffer(),samples,bytes:width*height*samples*(hdr?12:8)};
                    if(!target.framebuffer||!target.color||!target.depth){renderer3DDeleteMsaaTarget(target);continue;}
                    gl.bindFramebuffer(gl.FRAMEBUFFER,target.framebuffer);
                    gl.bindRenderbuffer(gl.RENDERBUFFER,target.color);gl.renderbufferStorageMultisample(gl.RENDERBUFFER,samples,colorFormat,width,height);gl.framebufferRenderbuffer(gl.FRAMEBUFFER,gl.COLOR_ATTACHMENT0,gl.RENDERBUFFER,target.color);
                    const actualColor=gl.getRenderbufferParameter(gl.RENDERBUFFER,gl.RENDERBUFFER_SAMPLES);
                    gl.bindRenderbuffer(gl.RENDERBUFFER,target.depth);gl.renderbufferStorageMultisample(gl.RENDERBUFFER,samples,gl.DEPTH_COMPONENT24,width,height);gl.framebufferRenderbuffer(gl.FRAMEBUFFER,gl.DEPTH_ATTACHMENT,gl.RENDERBUFFER,target.depth);
                    const actualDepth=gl.getRenderbufferParameter(gl.RENDERBUFFER,gl.RENDERBUFFER_SAMPLES);
                    gl.bindRenderbuffer(gl.RENDERBUFFER,null);
                    if(actualColor===samples&&actualDepth===samples&&gl.checkFramebufferStatus(gl.FRAMEBUFFER)===gl.FRAMEBUFFER_COMPLETE)return target;
                    renderer3DDeleteMsaaTarget(target);
                }
                return null;
            }
            function renderer3DSceneDrawTarget(){return renderer3DMsaaTarget?renderer3DMsaaTarget.framebuffer:renderer3DSceneFramebuffer;}
            function renderer3DResolveScene(includeDepth){
                if(!renderer3DMsaaTarget)return;
                const gl=renderer3DGl;
                gl.bindFramebuffer(gl.READ_FRAMEBUFFER,renderer3DMsaaTarget.framebuffer);gl.bindFramebuffer(gl.DRAW_FRAMEBUFFER,renderer3DSceneFramebuffer);
                gl.blitFramebuffer(0,0,backingWidth,backingHeight,0,0,backingWidth,backingHeight,gl.COLOR_BUFFER_BIT|(includeDepth?gl.DEPTH_BUFFER_BIT:0),gl.NEAREST);
                renderer3DResolveCount+=1;
                gl.bindFramebuffer(gl.FRAMEBUFFER,renderer3DSceneFramebuffer);
            }
            function renderer3DCreateColorTarget(width,height){const gl=renderer3DGl,texture=gl.createTexture(),framebuffer=gl.createFramebuffer();if(!texture||!framebuffer)return null;
                gl.bindTexture(gl.TEXTURE_2D,texture);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.NEAREST);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.NEAREST);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.CLAMP_TO_EDGE);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.CLAMP_TO_EDGE);
                gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA16F,width,height,0,gl.RGBA,gl.HALF_FLOAT,null);gl.bindFramebuffer(gl.FRAMEBUFFER,framebuffer);gl.framebufferTexture2D(gl.FRAMEBUFFER,gl.COLOR_ATTACHMENT0,gl.TEXTURE_2D,texture,0);
                if(gl.checkFramebufferStatus(gl.FRAMEBUFFER)!==gl.FRAMEBUFFER_COMPLETE){gl.deleteFramebuffer(framebuffer);gl.deleteTexture(texture);return null;}return {texture,framebuffer};}
            function renderer3DCreateSceneTarget(width,height,hdr){const gl=renderer3DGl,texture=gl.createTexture(),depth=gl.createTexture(),framebuffer=gl.createFramebuffer();if(!texture||!depth||!framebuffer){if(texture)gl.deleteTexture(texture);if(depth)gl.deleteTexture(depth);if(framebuffer)gl.deleteFramebuffer(framebuffer);return null;}
                gl.bindTexture(gl.TEXTURE_2D,texture);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.NEAREST);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.NEAREST);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.CLAMP_TO_EDGE);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.CLAMP_TO_EDGE);gl.texImage2D(gl.TEXTURE_2D,0,hdr?gl.RGBA16F:gl.RGBA8,width,height,0,gl.RGBA,hdr?gl.HALF_FLOAT:gl.UNSIGNED_BYTE,null);
                gl.bindTexture(gl.TEXTURE_2D,depth);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.NEAREST);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.NEAREST);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.CLAMP_TO_EDGE);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.CLAMP_TO_EDGE);gl.texImage2D(gl.TEXTURE_2D,0,gl.DEPTH_COMPONENT24,width,height,0,gl.DEPTH_COMPONENT,gl.UNSIGNED_INT,null);
                gl.bindFramebuffer(gl.FRAMEBUFFER,framebuffer);gl.framebufferTexture2D(gl.FRAMEBUFFER,gl.COLOR_ATTACHMENT0,gl.TEXTURE_2D,texture,0);gl.framebufferTexture2D(gl.FRAMEBUFFER,gl.DEPTH_ATTACHMENT,gl.TEXTURE_2D,depth,0);if(gl.checkFramebufferStatus(gl.FRAMEBUFFER)!==gl.FRAMEBUFFER_COMPLETE){gl.deleteFramebuffer(framebuffer);gl.deleteTexture(texture);gl.deleteTexture(depth);return null;}return{texture,depth,framebuffer};}
            function renderer3DCreateLinearDepthTarget(width,height){const gl=renderer3DGl;if(!renderer3DDepthProgram||globalThis.SMILE_TEST_RENDERER3D_FORCE_SOFT_DEPTH_FAILURE)return null;const floating=!!gl.getExtension("EXT_color_buffer_float");for(const format of (floating?[3,1]:[1])){const texture=gl.createTexture(),framebuffer=gl.createFramebuffer();if(!texture||!framebuffer){if(texture)gl.deleteTexture(texture);if(framebuffer)gl.deleteFramebuffer(framebuffer);continue;}gl.bindTexture(gl.TEXTURE_2D,texture);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.NEAREST);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.NEAREST);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.CLAMP_TO_EDGE);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.CLAMP_TO_EDGE);if(format===3)gl.texImage2D(gl.TEXTURE_2D,0,gl.R32F,width,height,0,gl.RED,gl.FLOAT,null);else gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA8,width,height,0,gl.RGBA,gl.UNSIGNED_BYTE,null);gl.bindFramebuffer(gl.FRAMEBUFFER,framebuffer);gl.framebufferTexture2D(gl.FRAMEBUFFER,gl.COLOR_ATTACHMENT0,gl.TEXTURE_2D,texture,0);if(gl.checkFramebufferStatus(gl.FRAMEBUFFER)===gl.FRAMEBUFFER_COMPLETE)return{texture,framebuffer,format,bytes:width*height*4};gl.deleteFramebuffer(framebuffer);gl.deleteTexture(texture);}return null;}
            function renderer3DCreateDistortionTarget(width,height){const gl=renderer3DGl;if(globalThis.SMILE_TEST_RENDERER3D_FORCE_DISTORTION_FAILURE)return null;const floating=!!gl.getExtension("EXT_color_buffer_float");for(const format of (floating?[2,1]:[1])){const texture=gl.createTexture(),framebuffer=gl.createFramebuffer();if(!texture||!framebuffer){if(texture)gl.deleteTexture(texture);if(framebuffer)gl.deleteFramebuffer(framebuffer);continue;}gl.bindTexture(gl.TEXTURE_2D,texture);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,format===1?gl.LINEAR:gl.NEAREST);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,format===1?gl.LINEAR:gl.NEAREST);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.CLAMP_TO_EDGE);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.CLAMP_TO_EDGE);if(format===2)gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA16F,width,height,0,gl.RGBA,gl.HALF_FLOAT,null);else gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA8,width,height,0,gl.RGBA,gl.UNSIGNED_BYTE,null);gl.bindFramebuffer(gl.FRAMEBUFFER,framebuffer);gl.framebufferTexture2D(gl.FRAMEBUFFER,gl.COLOR_ATTACHMENT0,gl.TEXTURE_2D,texture,0);if(gl.checkFramebufferStatus(gl.FRAMEBUFFER)===gl.FRAMEBUFFER_COMPLETE)return{texture,framebuffer,format,bytes:width*height*(format===2?8:4)};gl.deleteFramebuffer(framebuffer);gl.deleteTexture(texture);}return null;}
            function renderer3DCreateDistortionScratch(width,height,hdr,depth){const gl=renderer3DGl,texture=gl.createTexture(),framebuffer=gl.createFramebuffer();if(!texture||!framebuffer){if(texture)gl.deleteTexture(texture);if(framebuffer)gl.deleteFramebuffer(framebuffer);return null;}gl.bindTexture(gl.TEXTURE_2D,texture);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.NEAREST);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.NEAREST);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.CLAMP_TO_EDGE);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.CLAMP_TO_EDGE);gl.texImage2D(gl.TEXTURE_2D,0,hdr?gl.RGBA16F:gl.RGBA8,width,height,0,gl.RGBA,hdr?gl.HALF_FLOAT:gl.UNSIGNED_BYTE,null);gl.bindFramebuffer(gl.FRAMEBUFFER,framebuffer);gl.framebufferTexture2D(gl.FRAMEBUFFER,gl.COLOR_ATTACHMENT0,gl.TEXTURE_2D,texture,0);if(depth)gl.framebufferTexture2D(gl.FRAMEBUFFER,gl.DEPTH_ATTACHMENT,gl.TEXTURE_2D,depth,0);if(gl.checkFramebufferStatus(gl.FRAMEBUFFER)!==gl.FRAMEBUFFER_COMPLETE){gl.deleteFramebuffer(framebuffer);gl.deleteTexture(texture);return null;}return{texture,framebuffer,bytes:width*height*(hdr?8:4)};}
            function renderer3DPrepareM5Resources(){const gl=renderer3DGl;if(renderer3DM5AppliedRevision===renderer3DM5ConfigurationRevision&&renderer3DM5Width===backingWidth&&renderer3DM5Height===backingHeight){
                    renderer3DShadowEffective=renderer3DShadowRequested&&!!renderer3DShadowTexture&&renderer3DUpdateShadowMatrix();if(renderer3DShadowRequested&&!renderer3DShadowEffective)renderer3DFallbackFlags|=2;renderer3DMultipassActive=renderer3DShadowEffective||renderer3DHdrEffective||renderer3DSoftDepthEffective!==0||renderer3DDistortionRequested;return true;}
                const previous=renderer3DCaptureM5Bundle();renderer3DClearM5Bundle();renderer3DFallbackFlags=0;
                if(renderer3DShadowRequested&&renderer3DShadowProgram&&!globalThis.SMILE_TEST_RENDERER3D_FORCE_SHADOW_FAILURE){const maximum=gl.getParameter(gl.MAX_TEXTURE_SIZE)||0,choices=renderer3DShadowRequestedResolution===2048?[2048,1024]:[1024];
                    for(const resolution of choices){if(resolution>maximum)continue;const texture=gl.createTexture(),framebuffer=gl.createFramebuffer();if(!texture||!framebuffer)continue;
                        gl.bindTexture(gl.TEXTURE_2D,texture);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.NEAREST);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.NEAREST);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.CLAMP_TO_EDGE);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.CLAMP_TO_EDGE);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_COMPARE_MODE,gl.COMPARE_REF_TO_TEXTURE);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_COMPARE_FUNC,gl.LEQUAL);
                        gl.texImage2D(gl.TEXTURE_2D,0,gl.DEPTH_COMPONENT24,resolution,resolution,0,gl.DEPTH_COMPONENT,gl.UNSIGNED_INT,null);gl.bindFramebuffer(gl.FRAMEBUFFER,framebuffer);gl.framebufferTexture2D(gl.FRAMEBUFFER,gl.DEPTH_ATTACHMENT,gl.TEXTURE_2D,texture,0);gl.drawBuffers([gl.NONE]);gl.readBuffer(gl.NONE);
                        if(gl.checkFramebufferStatus(gl.FRAMEBUFFER)===gl.FRAMEBUFFER_COMPLETE){renderer3DShadowTexture=texture;renderer3DShadowFramebuffer=framebuffer;renderer3DShadowResolution=resolution;renderer3DShadowBytes=resolution*resolution*4;renderer3DShadowEffective=renderer3DUpdateShadowMatrix();if(resolution<renderer3DShadowRequestedResolution)renderer3DFallbackFlags|=1;break;}
                        gl.deleteFramebuffer(framebuffer);gl.deleteTexture(texture);}}
                if(renderer3DShadowRequested&&!renderer3DShadowEffective)renderer3DFallbackFlags|=2;
                let scene=null;if(renderer3DPostRequested&&renderer3DHdrRequested&&renderer3DPostProgram&&!globalThis.SMILE_TEST_RENDERER3D_FORCE_HDR_FAILURE&&gl.getExtension("EXT_color_buffer_float")){scene=renderer3DCreateSceneTarget(backingWidth,backingHeight,true);if(scene){renderer3DHdrEffective=renderer3DPostEffective=renderer3DToneMappingEffective=true;}}
                if(!scene&&(renderer3DRequestedSamples>1||renderer3DSoftDepthRequested||(renderer3DDistortionRequested&&renderer3DDistortionQuality>1))&&renderer3DPostProgram)scene=renderer3DCreateSceneTarget(backingWidth,backingHeight,false);
                if(scene){renderer3DSceneTexture=scene.texture;renderer3DSceneFramebuffer=scene.framebuffer;renderer3DSceneDepth=scene.depth;renderer3DMsaaTarget=renderer3DCreateMsaaTarget(backingWidth,backingHeight,renderer3DHdrEffective);if(renderer3DMsaaTarget)renderer3DEffectiveSamples=renderer3DMsaaTarget.samples;}
                if(renderer3DRequestedSamples>renderer3DEffectiveSamples)renderer3DFallbackFlags|=8;
                if(renderer3DSoftDepthRequested&&scene&&renderer3DDepthProgram){const linear=renderer3DCreateLinearDepthTarget(backingWidth,backingHeight);if(linear){renderer3DLinearDepthTexture=linear.texture;renderer3DLinearDepthFramebuffer=linear.framebuffer;renderer3DSoftDepthEffective=linear.format;renderer3DSoftDepthWidth=backingWidth;renderer3DSoftDepthHeight=backingHeight;renderer3DSoftDepthBytes=linear.bytes;renderer3DSoftDepthFallbackReason=0;}else{renderer3DSoftDepthFallbackReason=3;renderer3DSoftDepthCopyFailureCount+=1;}}
                else if(renderer3DSoftDepthRequested){renderer3DSoftDepthFallbackReason=renderer3DDepthProgram?3:2;renderer3DSoftDepthCopyFailureCount+=1;}
                if(renderer3DDistortionRequested&&renderer3DDistortionQuality===1)renderer3DDistortionFallbackReason=4;
                else if(renderer3DDistortionRequested&&scene&&renderer3DPostProgram){const divisor=renderer3DDistortionQuality===2?4:2,width=Math.max(1,Math.floor(backingWidth/divisor)),height=Math.max(1,Math.floor(backingHeight/divisor)),vectors=renderer3DCreateDistortionTarget(width,height),scratch=vectors?renderer3DCreateDistortionScratch(backingWidth,backingHeight,renderer3DHdrEffective,renderer3DSceneDepth):null;if(vectors&&scratch){renderer3DDistortionTexture=vectors.texture;renderer3DDistortionFramebuffer=vectors.framebuffer;renderer3DDistortionScratchTexture=scratch.texture;renderer3DDistortionScratchFramebuffer=scratch.framebuffer;renderer3DDistortionEffective=vectors.format;renderer3DDistortionFallbackReason=0;renderer3DDistortionWidth=width;renderer3DDistortionHeight=height;renderer3DDistortionBytes=vectors.bytes+scratch.bytes;}else{if(vectors){gl.deleteFramebuffer(vectors.framebuffer);gl.deleteTexture(vectors.texture);}if(scratch){gl.deleteFramebuffer(scratch.framebuffer);gl.deleteTexture(scratch.texture);}renderer3DDistortionFallbackReason=renderer3DPostProgram?3:2;}}
                else if(renderer3DDistortionRequested)renderer3DDistortionFallbackReason=renderer3DPostProgram?3:2;
                if(renderer3DHdrEffective&&renderer3DBloomRequested){renderer3DBloomWidth=Math.max(1,Math.floor(backingWidth/renderer3DBloomDownsample));renderer3DBloomHeight=Math.max(1,Math.floor(backingHeight/renderer3DBloomDownsample));const first=renderer3DCreateColorTarget(renderer3DBloomWidth,renderer3DBloomHeight),second=renderer3DCreateColorTarget(renderer3DBloomWidth,renderer3DBloomHeight);
                    if(first&&second){renderer3DBloomTextureA=first.texture;renderer3DBloomFramebufferA=first.framebuffer;renderer3DBloomTextureB=second.texture;renderer3DBloomFramebufferB=second.framebuffer;renderer3DBloomEffective=true;}else{if(first){gl.deleteFramebuffer(first.framebuffer);gl.deleteTexture(first.texture);}if(second){gl.deleteFramebuffer(second.framebuffer);gl.deleteTexture(second.texture);}renderer3DBloomWidth=renderer3DBloomHeight=0;renderer3DFallbackFlags|=32;}}
                if(!renderer3DHdrEffective){if(renderer3DPostRequested)renderer3DFallbackFlags|=4|64|128;if(renderer3DBloomRequested)renderer3DFallbackFlags|=32;}
                renderer3DM5Width=backingWidth;renderer3DM5Height=backingHeight;renderer3DSceneBytes=scene?backingWidth*backingHeight*(renderer3DHdrEffective?12:8)+(renderer3DMsaaTarget?renderer3DMsaaTarget.bytes:0):0;renderer3DBloomBytes=renderer3DBloomEffective?renderer3DBloomWidth*renderer3DBloomHeight*16:0;renderer3DTargetBytes=renderer3DShadowBytes+renderer3DSceneBytes+renderer3DBloomBytes+renderer3DSoftDepthBytes+renderer3DDistortionBytes;
                renderer3DMultipassActive=renderer3DShadowEffective||renderer3DHdrEffective||renderer3DSoftDepthEffective!==0||renderer3DDistortionRequested;let installed=true;const requiredFailed=(renderer3DShadowRequested&&previous.shadowEffective&&!renderer3DShadowEffective)||(renderer3DHdrRequested&&renderer3DPostRequested&&previous.hdrEffective&&!renderer3DHdrEffective)||(renderer3DSoftDepthRequested&&previous.softDepthEffective&&!renderer3DSoftDepthEffective)||(renderer3DDistortionRequested&&renderer3DDistortionQuality>1&&previous.distortionEffective&&!renderer3DDistortionEffective);if(previous.width===backingWidth&&previous.height===backingHeight&&requiredFailed){const failed=renderer3DCaptureM5Bundle();renderer3DClearM5Bundle();renderer3DDeleteM5Bundle(failed);renderer3DApplyM5Bundle(previous);renderer3DFallbackFlags=0;if(renderer3DShadowRequested&&!renderer3DShadowEffective)renderer3DFallbackFlags|=2;if(renderer3DPostRequested&&renderer3DHdrRequested&&!renderer3DHdrEffective)renderer3DFallbackFlags|=4|64|128;if(renderer3DBloomRequested&&!renderer3DBloomEffective)renderer3DFallbackFlags|=32;if(renderer3DRequestedSamples>renderer3DEffectiveSamples)renderer3DFallbackFlags|=8;installed=false;}else renderer3DDeleteM5Bundle(previous);renderer3DM5AppliedRevision=renderer3DM5ConfigurationRevision;if(installed){renderer3DM5ResourceGeneration+=1;if(renderer3DM5ResourceGeneration>2147483647)renderer3DM5ResourceGeneration=1;if(renderer3DSoftDepthEffective){renderer3DSoftDepthResourceGeneration+=1;if(renderer3DSoftDepthResourceGeneration>2147483647)renderer3DSoftDepthResourceGeneration=1;}if(renderer3DDistortionEffective){renderer3DDistortionResourceGeneration+=1;if(renderer3DDistortionResourceGeneration>2147483647)renderer3DDistortionResourceGeneration=1;}}if(typeof gl.bindFramebuffer==="function")gl.bindFramebuffer(gl.FRAMEBUFFER,null);return true;}
            function renderer3DNormalInto(output,matrix){const a=matrix[0],b=matrix[4],c=matrix[8],d=matrix[1],e=matrix[5],f=matrix[9],g=matrix[2],h=matrix[6],i=matrix[10];
                const determinant=a*(e*i-f*h)-b*(d*i-f*g)+c*(d*h-e*g);if(determinant<=1e-8)return null;const inverse=1/determinant;
                output[0]=(e*i-f*h)*inverse;output[1]=(c*h-b*i)*inverse;output[2]=(b*f-c*e)*inverse;
                output[3]=(f*g-d*i)*inverse;output[4]=(a*i-c*g)*inverse;output[5]=(c*d-a*f)*inverse;
                output[6]=(d*h-e*g)*inverse;output[7]=(b*g-a*h)*inverse;output[8]=(a*e-b*d)*inverse;return output;}
            function renderer3DNormalize(v){const l=Math.hypot(v[0],v[1],v[2]);return l>.000001?[v[0]/l,v[1]/l,v[2]/l]:[0,1,0];}
            function renderer3DCross(a,b){return[a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]];}
            function renderer3DDot(a,b){return a[0]*b[0]+a[1]*b[1]+a[2]*b[2];}
            function renderer3DModel(object){return renderer3DModelInto(new Float32Array(16),object);}
            function renderer3DView(){const eye=renderer3DCamera.position,target=renderer3DCamera.target,z=renderer3DNormalize([target[0]-eye[0],target[1]-eye[1],target[2]-eye[2]]),x=renderer3DNormalize(renderer3DCross(renderer3DCamera.up,z)),y=renderer3DCross(z,x);return[x[0],y[0],z[0],0,x[1],y[1],z[1],0,x[2],y[2],z[2],0,-renderer3DDot(x,eye),-renderer3DDot(y,eye),-renderer3DDot(z,eye),1];}
            function renderer3DProjection(aspect){const f=1/Math.tan(renderer3DCamera.fov*Math.PI/360),near=renderer3DCamera.near,far=renderer3DCamera.far;return[f/aspect,0,0,0,0,f,0,0,0,0,(far+near)/(far-near),1,0,0,-2*far*near/(far-near),0];}

            function renderer3DUpload(mesh) {
                const gl=renderer3DGl;if(mesh.vertexBuffer&&mesh.indexBuffer)return true;if(!gl||!mesh.committed)return false;
                mesh.vertexBuffer=gl.createBuffer();gl.bindBuffer(gl.ARRAY_BUFFER,mesh.vertexBuffer);gl.bufferData(gl.ARRAY_BUFFER,mesh.vertices,gl.STATIC_DRAW);
                mesh.indexBuffer=gl.createBuffer();gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,mesh.indexBuffer);gl.bufferData(gl.ELEMENT_ARRAY_BUFFER,mesh.indices,gl.STATIC_DRAW);return true;
            }

            function renderer3DUploadTexture(texture) {
                const gl=renderer3DGl;if(texture.gpu)return true;if(!gl||!imageLoadedRaw(texture.image))return false;
                texture.gpu=gl.createTexture();gl.activeTexture(gl.TEXTURE0);gl.bindTexture(gl.TEXTURE_2D,texture.gpu);
                gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL,false);
                gl.pixelStorei(gl.UNPACK_PREMULTIPLY_ALPHA_WEBGL,false);
                if(gl.UNPACK_COLORSPACE_CONVERSION_WEBGL!==undefined)gl.pixelStorei(gl.UNPACK_COLORSPACE_CONVERSION_WEBGL,gl.NONE);
                const internal=texture.pbr?(texture.usage===1?gl.SRGB8_ALPHA8:gl.RGBA8):gl.RGBA;
                gl.texImage2D(gl.TEXTURE_2D,0,internal,gl.RGBA,gl.UNSIGNED_BYTE,texture.image.entry.resource);
                const address=texture.wrap===0?gl.CLAMP_TO_EDGE:gl.REPEAT;
                gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,texture.filter===0?gl.NEAREST:gl.LINEAR);
                gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,texture.filter===0?gl.NEAREST:
                    texture.filter===1?gl.LINEAR:gl.LINEAR_MIPMAP_LINEAR);
                gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,address);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,address);
                if(texture.pbr&&texture.filter>=2)gl.generateMipmap(gl.TEXTURE_2D);
                if(texture.pbr&&texture.filter===3&&renderer3DAnisotropy){gl.texParameterf(gl.TEXTURE_2D,
                    renderer3DAnisotropy.TEXTURE_MAX_ANISOTROPY_EXT,texture.effectiveAnisotropy);}
                return true;
            }

            function renderer3DBindMesh(mesh,pbr){const gl=renderer3DGl;gl.bindBuffer(gl.ARRAY_BUFFER,mesh.vertexBuffer);
                gl.enableVertexAttribArray(0);gl.vertexAttribPointer(0,3,gl.FLOAT,false,80,0);
                gl.enableVertexAttribArray(1);gl.vertexAttribPointer(1,3,gl.FLOAT,false,80,12);
                gl.enableVertexAttribArray(2);gl.vertexAttribPointer(2,2,gl.FLOAT,false,80,24);
                gl.enableVertexAttribArray(3);gl.vertexAttribPointer(3,4,gl.FLOAT,false,80,32);
                gl.enableVertexAttribArray(4);gl.vertexAttribPointer(4,4,gl.FLOAT,false,80,48);
                if(pbr){gl.enableVertexAttribArray(5);gl.vertexAttribPointer(5,4,gl.FLOAT,false,80,64);}else gl.disableVertexAttribArray(5);
                gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,mesh.indexBuffer);}
            function renderer3DBindModelPalette(handle,animator,program,shadowPass,ignoreOffsets=false){ignoreOffsets=!!(animator&&animator.production&&(ignoreOffsets||animator.ignoreNodeOffsets));const gl=renderer3DGl;if(!animator||!animator.production){gl.uniform1f(program.modelSkinning,0);return true;}
                if(!renderer3DModelPaletteTexture){renderer3DModelPaletteTexture=gl.createTexture();if(!renderer3DModelPaletteTexture){renderer3DLastError=48;return false;}gl.activeTexture(gl.TEXTURE4);gl.bindTexture(gl.TEXTURE_2D,renderer3DModelPaletteTexture);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.NEAREST);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.NEAREST);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.CLAMP_TO_EDGE);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.CLAMP_TO_EDGE);gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA32F,4,128,0,gl.RGBA,gl.FLOAT,null);}
                gl.activeTexture(gl.TEXTURE4);gl.bindTexture(gl.TEXTURE_2D,renderer3DModelPaletteTexture);if(renderer3DModelPaletteCachedAnimator!==handle||renderer3DModelPaletteCachedRevision!==animator.revision||renderer3DModelPaletteCachedIgnoreOffsets!==ignoreOffsets){gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL,false);gl.texSubImage2D(gl.TEXTURE_2D,0,0,0,4,128,gl.RGBA,gl.FLOAT,ignoreOffsets&&animator.basePalette?animator.basePalette:animator.palette);renderer3DModelPaletteCachedAnimator=handle;renderer3DModelPaletteCachedRevision=animator.revision;renderer3DModelPaletteCachedIgnoreOffsets=ignoreOffsets;if(shadowPass)renderer3DShadowPaletteUploadCount+=1;else renderer3DModelPaletteUploadCount+=1;}
                gl.uniform1i(program.modelPalette,4);gl.uniform1f(program.modelSkinning,1);return true;}

            function renderer3DPaletteSnapshot(handle,animator,ignoreOffsets=false){if(!animator)return-1;ignoreOffsets=!!(animator.production&&ignoreOffsets);for(let index=0;index<renderer3DPaletteSnapshotCount;index+=1){const snapshot=renderer3DPaletteSnapshots[index];if(snapshot.animatorHandle===handle&&snapshot.revision===animator.revision&&snapshot.production===!!animator.production&&snapshot.ignoreNodeOffsets===ignoreOffsets)return index;}if(renderer3DPaletteSnapshotCount>=renderer3DPaletteSnapshots.length){renderer3DLastError=51;return-2;}const snapshot=renderer3DPaletteSnapshots[renderer3DPaletteSnapshotCount];snapshot.animatorHandle=handle;snapshot.revision=animator.revision;snapshot.production=!!animator.production;snapshot.ignoreNodeOffsets=ignoreOffsets;snapshot.palette.set(ignoreOffsets?animator.basePalette:animator.palette);return renderer3DPaletteSnapshotCount++;}
            function renderer3DSubmissionHasTexture(object,handle,before){for(let channel=0;channel<before;channel+=1)if(object.snapshotMaterial.textures[channel]===handle)return true;return false;}
            function renderer3DReleaseSubmission(index){const object=renderer3DSubmissionObjects[index];if(object.kind===renderer3DSubmissionObject){const mesh=renderer3DMeshes.get(object.mesh);if(mesh&&mesh.inFlight>0)mesh.inFlight-=1;}else if(object.kind===renderer3DSubmissionParticleBatch){const batch=renderer3DParticleBatches.get(object.source);if(batch&&batch.inFlight>0)batch.inFlight-=1;}else if(object.kind===renderer3DSubmissionRibbonBatch){const batch=renderer3DRibbonBatches.get(object.source);if(batch&&batch.inFlight>0)batch.inFlight-=1;}for(let channel=0;channel<4;channel+=1){const handle=object.snapshotMaterial.textures[channel];if(!handle||renderer3DSubmissionHasTexture(object,handle,channel))continue;const texture=renderer3DTextures.get(handle);if(texture&&texture.inFlight>0)texture.inFlight-=1;}object.kind=renderer3DSubmissionObject;object.mesh=object.source=object.animator=0;object.resourceRevision=0;object.hasMaterial=false;object.paletteIndex=-1;renderer3DSubmissions[index]=0;}
            function renderer3DReleaseSubmissions(first,last){while(last>first)renderer3DReleaseSubmission(--last);}
            function renderer3DCaptureSubmission(handle,index){const source=renderer3DRequireObject(handle);if(!renderer3DFrameActive||!source){renderer3DLastError=14;return 0;}if(!source.visible)return 2;const mesh=renderer3DRequireMesh(source.mesh);if(!mesh||!renderer3DUpload(mesh))return 0;const material=source.material?renderer3DRequireMaterial(source.material):null,animator=source.animator?renderer3DAnimators.get(source.animator):null,skeleton=animator&&!animator.production?renderer3DSkeletons.get(animator.skeleton):null;if(source.animator&&(!animator||(animator.production?!renderer3DModelOwnsMesh(animator.model,source.mesh):(!skeleton||mesh.maxJoint>=skeleton.boneCount)))){renderer3DLastError=36;return 0;}if(material&&material.kind===1&&animator&&!animator.production){const clip=animator.clip?renderer3DClips.get(animator.clip):null;if(clip&&!clip.pbrScaleSafe){renderer3DLastError=45;return 0;}}const paletteStart=renderer3DPaletteSnapshotCount,object=renderer3DSubmissionObjects[index],snapshot=object.snapshotMaterial;object.source=handle;object.mesh=source.mesh;object.animator=source.animator;object.position.set(source.position);object.rotation.set(source.rotation);object.scale.set(source.scale);object.color.set(source.color);object.pivotPosition.fill(0);object.pivotRotation.fill(0);if(source.pivotPosition)object.pivotPosition.set(source.pivotPosition);if(source.pivotRotation)object.pivotRotation.set(source.pivotRotation);object.cullMode=source.cullMode||0;object.ignoreNodeOffsets=!!source.ignoreNodeOffsets;object.visible=true;object.castsShadow=source.castsShadow;object.receivesShadow=source.receivesShadow;object.hasMaterial=!!material;object.paletteIndex=renderer3DPaletteSnapshot(source.animator,animator,source.ignoreNodeOffsets);if(object.paletteIndex===-2)return 0;snapshot.textures.fill(0);snapshot.texture=0;snapshot.kind=material?material.kind:0;snapshot.alphaMode=material?material.alphaMode:(source.color[3]<.999?2:0);snapshot.doubleSided=!!(material&&material.doubleSided);snapshot.softDepthMode=material&&material.kind===0?material.softDepthMode:0;snapshot.softDepthDistance=material&&material.kind===0?material.softDepthDistance:0;snapshot.vfxShadingMode=material&&material.kind===0?material.vfxShadingMode:0;snapshot.distortionStrength=material&&material.kind===0?material.distortionStrength:0;snapshot.distortionNoiseScale=material&&material.kind===0?material.distortionNoiseScale:0;snapshot.distortionNoiseSpeed=material&&material.kind===0?material.distortionNoiseSpeed:0;snapshot.distortionFlowX=material&&material.kind===0?material.distortionFlowX:0;snapshot.distortionFlowY=material&&material.kind===0?material.distortionFlowY:0;if(material){if(material.kind===1){snapshot.textures.set(material.textures);snapshot.baseColor.set(material.baseColor);snapshot.surface.set(material.surface);snapshot.emissiveAlpha.set(material.emissiveAlpha);snapshot.textureFlags.set(material.textureFlags);snapshot.cutoff=material.cutoff;}else{snapshot.texture=material.texture;snapshot.textures[0]=material.texture;snapshot.color.set(material.color);snapshot.unlit=material.unlit;snapshot.emissive=material.emissive;snapshot.cutoff=material.cutoff;}for(let channel=0;channel<4;channel+=1){const textureHandle=snapshot.textures[channel];if(!textureHandle)continue;const texture=renderer3DRequireTexture(textureHandle);if(!texture||!renderer3DUploadTexture(texture)){renderer3DPaletteSnapshotCount=paletteStart;return 0;}}}mesh.inFlight+=1;for(let channel=0;channel<4;channel+=1){const textureHandle=snapshot.textures[channel];if(!textureHandle||renderer3DSubmissionHasTexture(object,textureHandle,channel))continue;renderer3DTextures.get(textureHandle).inFlight+=1;}renderer3DSubmissions[index]=handle;return 1;}

            function renderer3DCreateParticleBatch(capacity,materialHandle,billboard,columns,rows){const material=renderer3DMaterials.get(materialHandle);if(capacity<1||capacity>4096||capacity>8192-renderer3DStagedParticleCapacity||renderer3DParticleBatches.size>=32||!material||material.kind!==0||(material.alphaMode!==2&&material.alphaMode!==3)||billboard<1||billboard>2||columns<1||columns>16||rows<1||rows>16||!renderer3DInitialize()||!renderer3DVfxProgram){renderer3DLastError=54;renderer3DVfxRejectedOperationCount+=1;return 0;}const gl=renderer3DGl,handle=renderer3DHandle(),instances=new Float32Array(capacity*12),committedInstances=new Float32Array(capacity*12),gpu=gl.createBuffer();if(!gpu){renderer3DLastError=55;renderer3DVfxRejectedOperationCount+=1;return 0;}gl.bindBuffer(gl.ARRAY_BUFFER,gpu);gl.bufferData(gl.ARRAY_BUFFER,instances.byteLength,gl.DYNAMIC_DRAW);renderer3DParticleBatches.set(handle,{capacity,material:materialHandle,billboard,columns,rows,count:0,stagingRevision:0,revision:0,uploadedRevision:0,inFlight:0,instances,committedInstances,gpu});renderer3DStagedParticleCapacity+=capacity;return handle;}
            function renderer3DCreateRibbonBatch(capacity,materialHandle){const material=renderer3DMaterials.get(materialHandle);if(capacity<2||capacity>8192||capacity>32768-renderer3DStagedRibbonCapacity||renderer3DRibbonBatches.size>=16||!material||material.kind!==0||(material.alphaMode!==2&&material.alphaMode!==3)||!renderer3DInitialize()||!renderer3DVfxProgram){renderer3DLastError=54;renderer3DVfxRejectedOperationCount+=1;return 0;}const gl=renderer3DGl,handle=renderer3DHandle(),points=new Float32Array(capacity*11),stagingVertices=new Float32Array(capacity*18),vertices=new Float32Array(capacity*18),gpu=gl.createBuffer();if(!gpu){renderer3DLastError=55;renderer3DVfxRejectedOperationCount+=1;return 0;}gl.bindBuffer(gl.ARRAY_BUFFER,gpu);gl.bufferData(gl.ARRAY_BUFFER,vertices.byteLength,gl.DYNAMIC_DRAW);renderer3DRibbonBatches.set(handle,{capacity,material:materialHandle,count:0,stagingRevision:0,revision:0,uploadedRevision:0,inFlight:0,points,stagingVertices,vertices,gpu});renderer3DStagedRibbonCapacity+=capacity;return handle;}
            function renderer3DEnsureParticleBatchGpu(batch){const gl=renderer3DGl;if(!gl){renderer3DLastError=57;return 0;}if(!batch.gpu){batch.gpu=gl.createBuffer();if(!batch.gpu){renderer3DLastError=57;return 0;}gl.bindBuffer(gl.ARRAY_BUFFER,batch.gpu);gl.bufferData(gl.ARRAY_BUFFER,batch.committedInstances.byteLength,gl.DYNAMIC_DRAW);batch.uploadedRevision=0;}if(batch.uploadedRevision!==batch.revision){gl.bindBuffer(gl.ARRAY_BUFFER,batch.gpu);gl.bufferSubData(gl.ARRAY_BUFFER,0,batch.committedInstances,0,batch.count*12);batch.uploadedRevision=batch.revision;renderer3DVfxUploadCount+=1;}return 1;}
            function renderer3DEnsureRibbonBatchGpu(batch){const gl=renderer3DGl;if(!gl){renderer3DLastError=57;return 0;}if(!batch.gpu){batch.gpu=gl.createBuffer();if(!batch.gpu){renderer3DLastError=57;return 0;}gl.bindBuffer(gl.ARRAY_BUFFER,batch.gpu);gl.bufferData(gl.ARRAY_BUFFER,batch.vertices.byteLength,gl.DYNAMIC_DRAW);batch.uploadedRevision=0;}if(batch.uploadedRevision!==batch.revision){gl.bindBuffer(gl.ARRAY_BUFFER,batch.gpu);gl.bufferSubData(gl.ARRAY_BUFFER,0,batch.vertices,0,batch.count*18);batch.uploadedRevision=batch.revision;renderer3DVfxUploadCount+=1;}return 1;}
            function renderer3DDrawPbr(object,mesh,material,animator){if(!renderer3DPbrProgram){renderer3DLastError=44;return 0;}
                const clip=animator&&animator.clip?renderer3DClips.get(animator.clip):null;
                if(clip&&!clip.pbrScaleSafe){renderer3DLastError=45;return 0;}
                const gl=renderer3DGl,model=renderer3DModelInto(renderer3DModelScratch,object),view=renderer3DViewInto(renderer3DViewScratch),
                    projection=renderer3DProjectionInto(renderer3DProjectionScratch,backingWidth/backingHeight);
                if(!renderer3DNormalInto(renderer3DNormalScratch,model)){renderer3DLastError=46;return 0;}
                renderer3DMultiplyInto(renderer3DMatrixScratchA,view,model);renderer3DMultiplyInto(renderer3DMvpScratch,projection,renderer3DMatrixScratchA);
                renderer3DMultiplyInto(renderer3DMatrixScratchB,renderer3DShadowMatrixScratch,model);
                for(let channel=0;channel<4;channel+=1){const texture=material.textures[channel]?renderer3DRequireTexture(material.textures[channel]):null;
                    if(texture&&!renderer3DUploadTexture(texture))return 0;gl.activeTexture(gl.TEXTURE0+channel);gl.bindTexture(gl.TEXTURE_2D,texture?texture.gpu:null);}
                if(material.alphaMode===2){gl.enable(gl.BLEND);gl.blendFunc(gl.SRC_ALPHA,gl.ONE_MINUS_SRC_ALPHA);gl.depthMask(false);}
                else{gl.disable(gl.BLEND);gl.depthMask(true);}renderer3DApplyCull(object,material.doubleSided);
                gl.useProgram(renderer3DPbrProgram.handle);renderer3DBindMesh(mesh,true);
                gl.uniformMatrix4fv(renderer3DPbrProgram.model,false,model);gl.uniformMatrix4fv(renderer3DPbrProgram.mvp,false,renderer3DMvpScratch);
                gl.uniformMatrix4fv(renderer3DPbrProgram.shadowMvp,false,renderer3DMatrixScratchB);
                gl.uniformMatrix3fv(renderer3DPbrProgram.normalMatrix,false,renderer3DNormalScratch);
                gl.uniformMatrix4fv(renderer3DPbrProgram["bones[0]"],false,animator&&!animator.production?animator.palette:renderer3DStaticBones);
                gl.uniform1f(renderer3DPbrProgram.skinning,animator?1:0);if(!renderer3DBindModelPalette(object.animator,animator,renderer3DPbrProgram,false,object.ignoreNodeOffsets))return 0;gl.uniform4fv(renderer3DPbrProgram.objectColor,object.color);
                gl.uniform4fv(renderer3DPbrProgram.baseFactor,material.baseColor);gl.uniform4fv(renderer3DPbrProgram.surfaceFactors,material.surface);
                gl.uniform4fv(renderer3DPbrProgram.emissiveAlpha,material.emissiveAlpha);gl.uniform4fv(renderer3DPbrProgram.textureFlags,material.textureFlags);
                gl.uniform3fv(renderer3DPbrProgram.cameraPosition,renderer3DCamera.position);gl.uniform4fv(renderer3DPbrProgram.ambientLight,renderer3DAmbient);
                gl.uniform4fv(renderer3DPbrProgram.directionalDirection,renderer3DDirectionalDirection);
                gl.uniform4fv(renderer3DPbrProgram.directionalColor,renderer3DDirectionalColor);
                gl.uniform4fv(renderer3DPbrProgram["localPositionType[0]"],renderer3DLocalPositionType);
                gl.uniform4fv(renderer3DPbrProgram["localDirectionRange[0]"],renderer3DLocalDirectionRange);
                gl.uniform4fv(renderer3DPbrProgram["localColorIntensity[0]"],renderer3DLocalColorIntensity);
                gl.uniform4fv(renderer3DPbrProgram["localCone[0]"],renderer3DLocalCone);
                gl.uniform1i(renderer3DPbrProgram.baseTexture,0);gl.uniform1i(renderer3DPbrProgram.normalTexture,1);
                gl.uniform1i(renderer3DPbrProgram.ormTexture,2);gl.uniform1i(renderer3DPbrProgram.emissiveTexture,3);
                if(renderer3DShadowEffective){gl.activeTexture(gl.TEXTURE5);gl.bindTexture(gl.TEXTURE_2D,renderer3DShadowTexture);}gl.uniform1i(renderer3DPbrProgram.shadowMap,5);
                if(typeof gl.uniform4f==="function")gl.uniform4f(renderer3DPbrProgram.shadowSettings,renderer3DShadowEffective&&object.receivesShadow?1:0,renderer3DShadowSettings[0],renderer3DShadowSettings[1],renderer3DShadowResolution>0?1/renderer3DShadowResolution:0);
                if(typeof gl.uniform2f==="function")gl.uniform2f(renderer3DPbrProgram.shadowSelection,renderer3DShadowCaster,renderer3DShadowSlot);gl.uniform1f(renderer3DPbrProgram.hdrOutput,renderer3DHdrEffective?1:0);gl.uniform1f(renderer3DPbrProgram.materialInspection,renderer3DMaterialInspection);
                gl.drawElements(gl.TRIANGLES,mesh.indexCount,gl.UNSIGNED_INT,0);renderer3DDrawCallCount+=1;
                renderer3DSubmittedTriangleCount+=mesh.indexCount/3;renderer3DPbrDrawCount+=1;renderer3DPbrTriangleCount+=mesh.indexCount/3;return 1;}
            function renderer3DSrgbToLinear(value){return value<=.04045?value/12.92:Math.pow((value+.055)/1.055,2.4);}
            // Cold GPU constructors stay outside the allocation-free Begin/End frame section.
            function renderer3DCreateGpuParticlePipeline(){if(renderer3DGpuParticlePipeline)return true;if(renderer3DGpuParticlePipelineAttempted)return false;renderer3DGpuParticlePipelineAttempted=true;const gl=renderer3DGl;if(!gl||typeof gl.createVertexArray!=="function"||typeof gl.createTransformFeedback!=="function"||typeof gl.transformFeedbackVaryings!=="function"||typeof gl.bindBufferBase!=="function"||typeof gl.beginTransformFeedback!=="function"||typeof gl.endTransformFeedback!=="function"||gl.getParameter(gl.MAX_VERTEX_ATTRIBS)<10||gl.getParameter(gl.MAX_TRANSFORM_FEEDBACK_INTERLEAVED_COMPONENTS)<20||globalThis.SMILE_TEST_RENDERER3D_FORCE_GPU_PARTICLE_ATTRIBUTE_FAILURE)return false;let simulationVertex=null,simulationFragment=null,simulationHandle=null,renderVertex=null,renderFragment=null,renderHandle=null;try{simulationVertex=renderer3DCompile(gl,gl.VERTEX_SHADER,`#version 300 es
                        precision highp float;
                        layout(location=0) in vec4 statePositionAge;
                        layout(location=1) in vec4 stateVelocityLifetime;
                        layout(location=2) in vec4 stateSizeRotationAngular;
                        layout(location=3) in vec4 stateThermalDensityNoise;
                        layout(location=4) in vec4 stateSeedFlagsGradientFrame;
                        layout(location=5) in vec4 spawnPositionAge;
                        layout(location=6) in vec4 spawnVelocityLifetime;
                        layout(location=7) in vec4 spawnSizeRotationAngular;
                        layout(location=8) in vec4 spawnThermalDensityNoise;
                        layout(location=9) in vec4 spawnSeedFlagsGradientFrame;
                        uniform float stepSeconds;
                        uniform float stepMilliseconds;
                        uniform vec4 gravityBuoyancy;
                        uniform vec4 windDrag;
                        uniform vec4 turbulence;
                        uniform vec4 evolution;
                        uniform vec4 boundsMin;
                        uniform vec4 boundsMax;
                        uniform vec4 fireRender;
                        uniform vec4 fireTime;
                        out vec4 nextPositionAge;
                        out vec4 nextVelocityLifetime;
                        out vec4 nextSizeRotationAngular;
                        out vec4 nextThermalDensityNoise;
                        out vec4 nextSeedFlagsGradientFrame;
                        uint fireHash(uint value){value^=value>>16;value*=0x7feb352du;value^=value>>15;value*=0x846ca68bu;return value^(value>>16);}
                        float fireNoise(vec3 point,uint seed){vec3 lattice=floor(point),weight=fract(point);weight=weight*weight*(3.0-2.0*weight);float result=0.0;for(int corner=0;corner<8;corner+=1){uvec3 cell=uvec3(ivec3(lattice))+uvec3(uint(corner&1),uint((corner>>1)&1),uint((corner>>2)&1));float value=float(fireHash(cell.x*73856093u^cell.y*19349663u^cell.z*83492791u^seed)&65535u)/32767.5-1.0;result+=value*((corner&1)!=0?weight.x:1.0-weight.x)*((corner&2)!=0?weight.y:1.0-weight.y)*((corner&4)!=0?weight.z:1.0-weight.z);}return result;}
                        bool fireStep(inout vec4 positionAge,inout vec4 velocityLifetime,inout vec4 sizeRotationAngular,inout vec4 thermalDensityNoise,uint seed){if(any(isnan(positionAge))||any(isinf(positionAge))||any(isnan(velocityLifetime))||any(isinf(velocityLifetime))||any(isnan(sizeRotationAngular))||any(isinf(sizeRotationAngular))||any(isnan(thermalDensityNoise))||any(isinf(thermalDensityNoise)))return false;thermalDensityNoise.xy=max(vec2(0.0),thermalDensityNoise.xy-evolution.xy*stepSeconds);vec3 point=positionAge.xyz*turbulence.y+fireTime.x*turbulence.z+float(seed&255u)*.03125;vec3 flow=vec3(0.0);if(turbulence.x>0.0){for(int octave=0;octave<2;octave+=1){if(float(octave)>=turbulence.w)break;float frequency=octave==0?1.0:2.0;float gain=octave==0?1.0:.5;flow+=gain*vec3(fireNoise(point*frequency,seed),fireNoise(point*frequency,seed+1013u),fireNoise(point*frequency,seed+2026u));}}vec3 acceleration=gravityBuoyancy.xyz+windDrag.xyz+vec3(0.0,gravityBuoyancy.w*thermalDensityNoise.x,0.0)+flow*turbulence.x;velocityLifetime.xyz=(velocityLifetime.xyz+acceleration*stepSeconds)*exp(-windDrag.w*stepSeconds);float speed=length(velocityLifetime.xyz);if(speed>evolution.w)velocityLifetime.xyz*=evolution.w/max(speed,.00001);positionAge.xyz+=velocityLifetime.xyz*stepSeconds;sizeRotationAngular.xy+=evolution.z*stepSeconds;return !any(isnan(positionAge.xyz))&&!any(isinf(positionAge.xyz))&&all(greaterThanEqual(positionAge.xyz,boundsMin.xyz))&&all(lessThanEqual(positionAge.xyz,boundsMax.xyz))&&thermalDensityNoise.y>0.0&&all(greaterThan(sizeRotationAngular.xy,vec2(0.0)))&&all(lessThanEqual(sizeRotationAngular.xy,vec2(1000000.0)));}
                        void main(){vec4 positionAge=statePositionAge;vec4 velocityLifetime=stateVelocityLifetime;vec4 sizeRotationAngular=stateSizeRotationAngular;vec4 thermalDensityNoise=stateThermalDensityNoise;vec4 seedFlagsGradientFrame=stateSeedFlagsGradientFrame;if(spawnSeedFlagsGradientFrame.y>.5&&spawnSeedFlagsGradientFrame.z>seedFlagsGradientFrame.z){positionAge=spawnPositionAge;velocityLifetime=spawnVelocityLifetime;sizeRotationAngular=spawnSizeRotationAngular;thermalDensityNoise=spawnThermalDensityNoise;seedFlagsGradientFrame=spawnSeedFlagsGradientFrame;}if(seedFlagsGradientFrame.y>.5){if(fireRender.x>.5){if(!fireStep(positionAge,velocityLifetime,sizeRotationAngular,thermalDensityNoise,uint(max(seedFlagsGradientFrame.x,0.0))))seedFlagsGradientFrame.y=0.0;}else positionAge.xyz+=velocityLifetime.xyz*stepSeconds;positionAge.w+=stepMilliseconds;sizeRotationAngular.z+=sizeRotationAngular.w*stepSeconds;if(positionAge.w>=velocityLifetime.w)seedFlagsGradientFrame.y=0.0;}nextPositionAge=positionAge;nextVelocityLifetime=velocityLifetime;nextSizeRotationAngular=sizeRotationAngular;nextThermalDensityNoise=thermalDensityNoise;nextSeedFlagsGradientFrame=seedFlagsGradientFrame;}`);simulationFragment=renderer3DCompile(gl,gl.FRAGMENT_SHADER,`#version 300 es
                        precision highp float;
                        void main(){}`);simulationHandle=gl.createProgram();if(!simulationHandle)throw new Error("simulation program allocation failed");gl.attachShader(simulationHandle,simulationVertex);gl.attachShader(simulationHandle,simulationFragment);gl.transformFeedbackVaryings(simulationHandle,["nextPositionAge","nextVelocityLifetime","nextSizeRotationAngular","nextThermalDensityNoise","nextSeedFlagsGradientFrame"],gl.INTERLEAVED_ATTRIBS);gl.linkProgram(simulationHandle);if(globalThis.SMILE_TEST_RENDERER3D_FORCE_GPU_PARTICLE_SHADER_FAILURE||!gl.getProgramParameter(simulationHandle,gl.LINK_STATUS))throw new Error(gl.getProgramInfoLog(simulationHandle)||"simulation program link failed");renderVertex=renderer3DCompile(gl,gl.VERTEX_SHADER,`#version 300 es
                        precision highp float;
                        layout(location=0) in vec2 corner;
                        layout(location=1) in vec2 textureUv;
                        layout(location=2) in vec4 statePositionAge;
                        layout(location=3) in vec4 stateVelocityLifetime;
                        layout(location=4) in vec4 stateSizeRotationAngular;
                        layout(location=5) in vec4 stateThermalDensityNoise;
                        layout(location=6) in vec4 stateSeedFlagsGradientFrame;
                        uniform mat4 viewProjection;
                        uniform vec3 cameraRight;
                        uniform vec3 cameraUp;
                        uniform vec4 fireRender;
                        out vec2 effectUv;
                        out vec4 instanceColor;
                        vec3 thermalColor(float temperature){if(temperature<.25)return mix(vec3(.16,.005,0.0),vec3(.95,.1,.005),temperature*4.0);if(temperature<.55)return mix(vec3(.95,.1,.005),vec3(1.0,.55,.03),(temperature-.25)/.3);if(temperature<.8)return mix(vec3(1.0,.55,.03),vec3(1.0,.92,.3),(temperature-.55)/.25);return mix(vec3(1.0,.92,.3),vec3(1.0,1.0,.96),(temperature-.8)*5.0);}
                        void main(){float particleActive=step(.5,stateSeedFlagsGradientFrame.y);float life=max(stateVelocityLifetime.w,1.0);float progress=clamp(statePositionAge.w/life,0.0,1.0);float size=mix(stateSizeRotationAngular.x,stateSizeRotationAngular.y,progress);float angle=stateSizeRotationAngular.z*.01745329252;vec2 quad=mat2(cos(angle),-sin(angle),sin(angle),cos(angle))*corner*size;vec3 right=cameraRight,up=cameraUp;if(fireRender.y>.5){vec3 forward=cross(cameraRight,cameraUp);up=fireRender.y<1.5?vec3(0.0,1.0,0.0):stateVelocityLifetime.xyz;up-=forward*dot(up,forward);float upLength=length(up);if(upLength>.0001){up/=upLength;right=cross(up,forward);quad=corner*size;quad.y*=fireRender.y<1.5?1.7:clamp(length(stateVelocityLifetime.xyz)/max(size,1.0)*.04,1.0,4.0);}else up=cameraUp;}vec3 world=statePositionAge.xyz+right*quad.x+up*quad.y;gl_Position=particleActive>.5?viewProjection*vec4(world,1.0):vec4(2.0,2.0,2.0,1.0);uint columns=uint(max(fireRender.z,1.0)),rows=uint(max(fireRender.w,1.0)),frame=uint(max(stateSeedFlagsGradientFrame.x,0.0))%(columns*rows);effectUv=(vec2(float(frame%columns),float(frame/columns))+textureUv)/vec2(float(columns),float(rows));float temperature=clamp(stateThermalDensityNoise.x,0.0,1.0),density=clamp(stateThermalDensityNoise.y,0.0,1.0);vec3 color=fireRender.x>.5?thermalColor(temperature):vec3(1.0,mix(.25,.85,temperature),.08);if(fireRender.x>1.5&&fireRender.x<2.5)color=vec3(.24,.22,.2);float fade=1.0-progress;if(fireRender.x>.5)fade*=clamp(statePositionAge.w/60.0,0.0,1.0);instanceColor=vec4(color,density*fade*particleActive);}`);renderFragment=renderer3DCompile(gl,gl.FRAGMENT_SHADER,`#version 300 es
                        precision highp float;
                        in vec2 effectUv;
                        in vec4 instanceColor;
                        uniform vec4 materialColor;
                        uniform sampler2D effectTexture;
                        uniform float textureEnabled;
                        uniform float emissive;
                        uniform float hdrOutput;
                        uniform highp sampler2D sceneDepthTexture;
                        uniform vec4 softDepthSettings;
                        uniform vec2 targetSize;
                        uniform float softDepthFormat;
                        uniform vec4 distortionSettings;
                        uniform vec2 distortionFlow;
                        uniform float distortionFormat;
                        out vec4 outputColor;
                        vec3 toLinear(vec3 color){return mix(color/12.92,pow((color+.055)/1.055,vec3(2.4)),step(vec3(.04045),color));}
                        float unpackDepth(vec4 value){return dot(value,vec4(1.0,1.0/255.0,1.0/65025.0,1.0/16581375.0));}
                        float linearDepth(float depth){float z=depth*2.0-1.0;return(2.0*softDepthSettings.z*softDepthSettings.w)/max(softDepthSettings.w+softDepthSettings.z-z*(softDepthSettings.w-softDepthSettings.z),.000001);}
                        void main(){vec4 sampled=textureEnabled>.5?texture(effectTexture,effectUv):vec4(1.0);vec4 color=sampled*materialColor*instanceColor;if(color.a<=.001)discard;
                            if(softDepthSettings.x>.5){vec4 stored=texture(sceneDepthTexture,gl_FragCoord.xy/targetSize);float scene=softDepthFormat<1.5?unpackDepth(stored)*softDepthSettings.w:stored.r;float distance=max(scene-linearDepth(gl_FragCoord.z),0.0);color.a*=clamp(distance/max(softDepthSettings.y,.0001),0.0,1.0);}
                            if(distortionSettings.x>.5){float wave=.65+.35*sin((effectUv.x+effectUv.y)*max(distortionSettings.z,.01)*6.283185+distortionSettings.w);vec2 flow=length(distortionFlow)>.0001?normalize(distortionFlow):vec2(0.0,1.0);vec2 delta=flow*distortionSettings.y*color.a*wave;outputColor=distortionFormat<1.5?vec4(delta/.06+.5,0.0,color.a):vec4(delta,0.0,color.a);return;}
                            color.rgb=hdrOutput>.5?toLinear(clamp(color.rgb,0.0,1.0))*max(emissive,1.0):clamp(color.rgb*max(emissive,1.0),0.0,1.0);outputColor=color;}`);renderHandle=gl.createProgram();if(!renderHandle)throw new Error("render program allocation failed");gl.attachShader(renderHandle,renderVertex);gl.attachShader(renderHandle,renderFragment);gl.linkProgram(renderHandle);if(!gl.getProgramParameter(renderHandle,gl.LINK_STATUS))throw new Error(gl.getProgramInfoLog(renderHandle)||"render program link failed");renderer3DGpuParticlePipeline={simulation:{handle:simulationHandle,stepSeconds:gl.getUniformLocation(simulationHandle,"stepSeconds"),stepMilliseconds:gl.getUniformLocation(simulationHandle,"stepMilliseconds"),gravityBuoyancy:gl.getUniformLocation(simulationHandle,"gravityBuoyancy"),windDrag:gl.getUniformLocation(simulationHandle,"windDrag"),turbulence:gl.getUniformLocation(simulationHandle,"turbulence"),evolution:gl.getUniformLocation(simulationHandle,"evolution"),boundsMin:gl.getUniformLocation(simulationHandle,"boundsMin"),boundsMax:gl.getUniformLocation(simulationHandle,"boundsMax"),fireRender:gl.getUniformLocation(simulationHandle,"fireRender"),fireTime:gl.getUniformLocation(simulationHandle,"fireTime")},render:{handle:renderHandle,viewProjection:gl.getUniformLocation(renderHandle,"viewProjection"),cameraRight:gl.getUniformLocation(renderHandle,"cameraRight"),cameraUp:gl.getUniformLocation(renderHandle,"cameraUp"),fireRender:gl.getUniformLocation(renderHandle,"fireRender"),materialColor:gl.getUniformLocation(renderHandle,"materialColor"),effectTexture:gl.getUniformLocation(renderHandle,"effectTexture"),textureEnabled:gl.getUniformLocation(renderHandle,"textureEnabled"),emissive:gl.getUniformLocation(renderHandle,"emissive"),hdrOutput:gl.getUniformLocation(renderHandle,"hdrOutput"),sceneDepthTexture:gl.getUniformLocation(renderHandle,"sceneDepthTexture"),softDepthSettings:gl.getUniformLocation(renderHandle,"softDepthSettings"),targetSize:gl.getUniformLocation(renderHandle,"targetSize"),softDepthFormat:gl.getUniformLocation(renderHandle,"softDepthFormat"),distortionSettings:gl.getUniformLocation(renderHandle,"distortionSettings"),distortionFlow:gl.getUniformLocation(renderHandle,"distortionFlow"),distortionFormat:gl.getUniformLocation(renderHandle,"distortionFormat")}};renderer3DGpuParticleBackendAvailable=true;return true;}catch(error){renderer3DRecordFailure("gpu-particle-pipeline",String(error.stack||error));if(simulationHandle)gl.deleteProgram(simulationHandle);if(renderHandle)gl.deleteProgram(renderHandle);renderer3DGpuParticlePipeline=null;renderer3DGpuParticleBackendAvailable=false;return false;}finally{if(simulationVertex)gl.deleteShader(simulationVertex);if(simulationFragment)gl.deleteShader(simulationFragment);if(renderVertex)gl.deleteShader(renderVertex);if(renderFragment)gl.deleteShader(renderFragment);}}
            function renderer3DGpuParticleCreate(capacity,materialHandle,requested,fixedStep){const material=renderer3DMaterials.get(materialHandle);const maximumCapacity=requested===1?8192:16384;if(capacity<1||capacity>maximumCapacity||capacity>32768-renderer3DGpuParticleTotalCapacity||renderer3DGpuParticleSystems.size>=32||requested<1||requested>3||fixedStep<5||fixedStep>50||!material||material.kind!==0||(material.alphaMode!==2&&material.alphaMode!==3)){renderer3DLastError=67;return 0;}const buffers=[new ArrayBuffer(capacity*80),new ArrayBuffer(capacity*80)],stagedBuffer=new ArrayBuffer(capacity*80),commandBuffer=new ArrayBuffer(512*80),handle=renderer3DHandle(),system={capacity,material:materialHandle,requested,effective:1,fixedStep,activeCount:0,pendingCount:0,accumulator:0,readIndex:0,readGeneration:1,writeGeneration:2,inFlight:0,simulationSteps:0,uploadBytes:0,queueEntries:0,buffers,stateF:[new Float32Array(buffers[0]),new Float32Array(buffers[1])],stateU:[new Uint32Array(buffers[0]),new Uint32Array(buffers[1])],stagedBuffer,stagedF:new Float32Array(stagedBuffer),stagedU:new Uint32Array(stagedBuffer),commandBuffer,commandF:new Float32Array(commandBuffer),commandU:new Uint32Array(commandBuffer),commandSlots:new Uint32Array(512),commandSerials:new Uint32Array(512),serials:new Uint32Array(capacity),ages:new Uint32Array(capacity),lifetimes:new Uint32Array(capacity),active:new Uint8Array(capacity),zeroF:new Float32Array(20),stateGpu:[null,null],simulationVaos:[null,null],renderVaos:[null,null],transformFeedbacks:[null,null],spawnGpu:null,gpuBytes:0,firstDispatchComplete:false,restartPending:false,fire:{gravityBuoyancy:new Float32Array(4),windDrag:new Float32Array(4),turbulence:new Float32Array(4),evolution:new Float32Array(4),boundsMin:new Float32Array(4),boundsMax:new Float32Array(4),render:new Float32Array(4),time:new Float32Array(4)}};renderer3DGpuParticleSystems.set(handle,system);renderer3DGpuParticleTotalCapacity+=capacity;if(requested!==1)renderer3DGpuParticleCreateGpu(system);return handle;}
            function renderer3DSetBackdropTexture(handle){if(renderer3DFrameActive||(handle!==0&&!renderer3DTextures.has(handle))){renderer3DLastError=5;return 0;}renderer3DBackdropTexture=handle;return 1;}
            function renderer3DDrawBackdrop(){if(renderer3DBackdropTexture===0)return 1;const texture=renderer3DTextures.get(renderer3DBackdropTexture),gl=renderer3DGl;if(!texture||!renderer3DPostProgram||!renderer3DUploadTexture(texture)){renderer3DLastError=5;return 0;}renderer3DPostPass(renderer3DSceneDrawTarget()||null,backingWidth,backingHeight,texture.gpu,null,renderer3DHdrEffective?6:7,0,0,0,0,0);gl.bindFramebuffer(gl.FRAMEBUFFER,renderer3DSceneDrawTarget()||null);gl.viewport(0,0,backingWidth,backingHeight);gl.enable(gl.DEPTH_TEST);gl.depthFunc(gl.LESS);gl.depthMask(true);gl.disable(gl.BLEND);gl.disable(gl.CULL_FACE);return 1;}
            function renderer3DBegin(red,green,blue){if(renderer3DFrameActive){renderer3DClearPendingCamera();renderer3DLastError=renderer3DCameraErrorFrameActive;return 0;}const usePendingCamera=renderer3DPendingCamera.hasProjection||renderer3DPendingCamera.hasUp;if(usePendingCamera&&!renderer3DValidatePendingCamera()){renderer3DClearPendingCamera();return 0;}if(renderer3DCanvas.width!==backingWidth||renderer3DCanvas.height!==backingHeight){renderer3DCanvas.width=backingWidth;renderer3DCanvas.height=backingHeight;}
                if(!renderer3DInitialize()||!renderer3DPrepareM5Resources()){renderer3DClearPendingCamera();return 0;}if(usePendingCamera)renderer3DPromotePendingCamera();const gl=renderer3DGl;
                renderer3DClearScratch[0]=(safe(red)&255)/255;renderer3DClearScratch[1]=(safe(green)&255)/255;renderer3DClearScratch[2]=(safe(blue)&255)/255;renderer3DClearScratch[3]=1;
                gl.bindFramebuffer(gl.FRAMEBUFFER,renderer3DSceneDrawTarget()||null);gl.viewport(0,0,backingWidth,backingHeight);gl.enable(gl.DEPTH_TEST);gl.depthFunc(gl.LESS);gl.depthMask(true);gl.disable(gl.BLEND);gl.disable(gl.CULL_FACE);
                gl.clearColor(renderer3DHdrEffective?renderer3DSrgbToLinear(renderer3DClearScratch[0]):renderer3DClearScratch[0],renderer3DHdrEffective?renderer3DSrgbToLinear(renderer3DClearScratch[1]):renderer3DClearScratch[1],renderer3DHdrEffective?renderer3DSrgbToLinear(renderer3DClearScratch[2]):renderer3DClearScratch[2],1);gl.clearDepth(1);gl.clear(gl.COLOR_BUFFER_BIT|gl.DEPTH_BUFFER_BIT);if(!renderer3DDrawBackdrop()){renderer3DClearPendingCamera();return 0;}
                gl.useProgram(renderer3DProgram.handle);renderer3DDrawCallCount=0;renderer3DSubmittedTriangleCount=0;renderer3DPbrDrawCount=0;renderer3DSimpleDrawCount=0;renderer3DPbrTriangleCount=0;
                renderer3DReleaseSubmissions(0,renderer3DSubmissionCount);renderer3DReleaseGpuParticleFrameSystems();renderer3DLogicalSubmissionCount=renderer3DPhysicalSubmissionCount=0;renderer3DRejectedSubmissionCount=0;renderer3DSubmissionCount=renderer3DPaletteSnapshotCount=0;renderer3DSubmissionGroupActive=false;renderer3DSubmissionGroupToken=0;renderer3DSubmissionGroupReserved=renderer3DSubmissionGroupPhysical=renderer3DSubmissionGroupLogical=0;renderer3DShadowDrawCount=0;renderer3DShadowTriangleCount=0;renderer3DShadowPaletteUploadCount=0;renderer3DPostDrawCount=0;renderer3DResolveCount=0;renderer3DSoftDepthCopyDrawCount=0;renderer3DSoftParticleDrawCount=0;renderer3DDistortionVectorDrawCount=renderer3DDistortionCompositeDrawCount=renderer3DDistortionEmitterCount=renderer3DDistortionMaximumStrength=0;renderer3DRenderingDistortionVectors=false;renderer3DVfxDrawCount=renderer3DVfxTriangleCount=renderer3DVfxParticleDrawCount=renderer3DVfxRibbonDrawCount=renderer3DVfxParticleTriangleCount=renderer3DVfxRibbonTriangleCount=renderer3DVfxParticleSubmissionCount=renderer3DVfxRibbonSubmissionCount=0;renderer3DFrameActive=true;return 1;}
            function renderer3DSetParticle(batch,index,x,y,z,size,rotation,frame){if(!batch||index<0||index>=batch.capacity||x< -1000000||x>1000000||y< -1000000||y>1000000||z< -1000000||z>1000000||size<=0||size>1000000||rotation< -1000000||rotation>1000000||frame<0||frame>=batch.columns*batch.rows){renderer3DLastError=54;renderer3DVfxRejectedOperationCount+=1;return 0;}const offset=index*12;batch.instances[offset]=x;batch.instances[offset+1]=y;batch.instances[offset+2]=z;batch.instances[offset+3]=size;batch.instances[offset+8]=rotation*Math.PI/180;batch.instances[offset+9]=(frame%batch.columns)/batch.columns;batch.instances[offset+10]=Math.floor(frame/batch.columns)/batch.rows;batch.stagingRevision=batch.stagingRevision>=2147483647?1:batch.stagingRevision+1;return 1;}
            function renderer3DSetParticleColor(batch,index,red,green,blue,opacity){if(!batch||index<0||index>=batch.capacity||red<0||red>255||green<0||green>255||blue<0||blue>255||opacity<0||opacity>100){renderer3DLastError=54;renderer3DVfxRejectedOperationCount+=1;return 0;}const offset=index*12+4;batch.instances[offset]=red/255;batch.instances[offset+1]=green/255;batch.instances[offset+2]=blue/255;batch.instances[offset+3]=opacity/100;batch.stagingRevision=batch.stagingRevision>=2147483647?1:batch.stagingRevision+1;return 1;}
            function renderer3DCommitParticleBatch(batch,count){if(!batch||batch.inFlight||count<0||count>batch.capacity){renderer3DLastError=batch&&batch.inFlight?56:54;renderer3DVfxRejectedOperationCount+=1;return 0;}if(!renderer3DEnsureParticleBatchGpu(batch))return 0;const gl=renderer3DGl,revision=batch.revision>=2147483647?1:batch.revision+1;gl.bindBuffer(gl.ARRAY_BUFFER,batch.gpu);gl.bufferSubData(gl.ARRAY_BUFFER,0,batch.instances,0,count*12);batch.committedInstances.set(batch.instances);batch.count=count;batch.revision=revision;batch.uploadedRevision=revision;renderer3DVfxUploadCount+=1;return 1;}
            function renderer3DSetRibbonPoint(batch,index,lx,ly,lz,rx,ry,rz,u){if(!batch||index<0||index>=batch.capacity||lx< -1000000||lx>1000000||ly< -1000000||ly>1000000||lz< -1000000||lz>1000000||rx< -1000000||rx>1000000||ry< -1000000||ry>1000000||rz< -1000000||rz>1000000||u<0||u>1000){renderer3DLastError=54;renderer3DVfxRejectedOperationCount+=1;return 0;}const offset=index*11;batch.points[offset]=lx;batch.points[offset+1]=ly;batch.points[offset+2]=lz;batch.points[offset+3]=rx;batch.points[offset+4]=ry;batch.points[offset+5]=rz;batch.points[offset+10]=u/1000;batch.stagingRevision=batch.stagingRevision>=2147483647?1:batch.stagingRevision+1;return 1;}
            function renderer3DSetRibbonColor(batch,index,red,green,blue,opacity){if(!batch||index<0||index>=batch.capacity||red<0||red>255||green<0||green>255||blue<0||blue>255||opacity<0||opacity>100){renderer3DLastError=54;renderer3DVfxRejectedOperationCount+=1;return 0;}const offset=index*11+6;batch.points[offset]=red/255;batch.points[offset+1]=green/255;batch.points[offset+2]=blue/255;batch.points[offset+3]=opacity/100;batch.stagingRevision=batch.stagingRevision>=2147483647?1:batch.stagingRevision+1;return 1;}
            function renderer3DCommitRibbonBatch(batch,count){if(!batch||batch.inFlight||count<0||count>batch.capacity){renderer3DLastError=batch&&batch.inFlight?56:54;renderer3DVfxRejectedOperationCount+=1;return 0;}for(let point=0;point<count;point+=1){const source=point*11;for(let side=0;side<2;side+=1){const target=(point*2+side)*9,position=source+(side?3:0);batch.stagingVertices[target]=batch.points[position];batch.stagingVertices[target+1]=batch.points[position+1];batch.stagingVertices[target+2]=batch.points[position+2];batch.stagingVertices[target+3]=batch.points[source+10];batch.stagingVertices[target+4]=side;batch.stagingVertices[target+5]=batch.points[source+6];batch.stagingVertices[target+6]=batch.points[source+7];batch.stagingVertices[target+7]=batch.points[source+8];batch.stagingVertices[target+8]=batch.points[source+9];}}if(!renderer3DEnsureRibbonBatchGpu(batch))return 0;const gl=renderer3DGl,revision=batch.revision>=2147483647?1:batch.revision+1;gl.bindBuffer(gl.ARRAY_BUFFER,batch.gpu);gl.bufferSubData(gl.ARRAY_BUFFER,0,batch.stagingVertices,0,count*18);const swap=batch.vertices;batch.vertices=batch.stagingVertices;batch.stagingVertices=swap;batch.count=count;batch.revision=revision;batch.uploadedRevision=revision;renderer3DVfxUploadCount+=1;return 1;}
            function renderer3DDeleteParticleBatch(handle){const batch=renderer3DParticleBatches.get(handle);if(!batch)return 0;if(batch.inFlight){renderer3DLastError=56;renderer3DVfxRejectedOperationCount+=1;return 0;}if(renderer3DGl&&batch.gpu)renderer3DGl.deleteBuffer(batch.gpu);renderer3DStagedParticleCapacity-=batch.capacity;renderer3DParticleBatches.delete(handle);return 1;}
            function renderer3DDeleteRibbonBatch(handle){const batch=renderer3DRibbonBatches.get(handle);if(!batch)return 0;if(batch.inFlight){renderer3DLastError=56;renderer3DVfxRejectedOperationCount+=1;return 0;}if(renderer3DGl&&batch.gpu)renderer3DGl.deleteBuffer(batch.gpu);renderer3DStagedRibbonCapacity-=batch.capacity;renderer3DRibbonBatches.delete(handle);return 1;}
            function renderer3DCaptureVfxSubmission(kind,handle,index){if(!renderer3DFrameActive){renderer3DLastError=14;return 0;}const particle=kind===renderer3DSubmissionParticleBatch,batch=particle?renderer3DParticleBatches.get(handle):renderer3DRibbonBatches.get(handle);if(!batch){renderer3DLastError=54;return 0;}if(batch.count===0)return 2;if(!(particle?renderer3DEnsureParticleBatchGpu(batch):renderer3DEnsureRibbonBatchGpu(batch))){renderer3DLastError=57;return 0;}const material=renderer3DMaterials.get(batch.material);if(!material||material.kind!==0||(material.alphaMode!==2&&material.alphaMode!==3)){renderer3DLastError=54;return 0;}const texture=material.texture?renderer3DRequireTexture(material.texture):null;if(texture&&!renderer3DUploadTexture(texture))return 0;const object=renderer3DSubmissionObjects[index],snapshot=object.snapshotMaterial;object.kind=kind;object.source=handle;object.mesh=object.animator=0;object.resourceRevision=batch.revision;object.visible=true;object.castsShadow=object.receivesShadow=false;object.hasMaterial=true;object.paletteIndex=-1;snapshot.textures.fill(0);snapshot.texture=material.texture;snapshot.textures[0]=material.texture;snapshot.kind=0;snapshot.alphaMode=material.alphaMode;snapshot.color.set(material.color);snapshot.unlit=material.unlit;snapshot.emissive=material.emissive;snapshot.cutoff=material.cutoff;snapshot.doubleSided=false;snapshot.softDepthMode=material.softDepthMode;snapshot.softDepthDistance=material.softDepthDistance;snapshot.vfxShadingMode=material.vfxShadingMode;snapshot.distortionStrength=material.distortionStrength;snapshot.distortionNoiseScale=material.distortionNoiseScale;snapshot.distortionNoiseSpeed=material.distortionNoiseSpeed;snapshot.distortionFlowX=material.distortionFlowX;snapshot.distortionFlowY=material.distortionFlowY;batch.inFlight+=1;if(texture)texture.inFlight+=1;renderer3DSubmissions[index]=handle;return 1;}
            function renderer3DDrawVfxImmediate(object){const gl=renderer3DGl,particle=object.kind===renderer3DSubmissionParticleBatch,batch=particle?renderer3DParticleBatches.get(object.source):renderer3DRibbonBatches.get(object.source),program=particle?renderer3DVfxProgram.particle:renderer3DVfxProgram.ribbon,material=object.snapshotMaterial;if(!batch||batch.revision!==object.resourceRevision||!batch.gpu||!program){renderer3DLastError=56;return 0;}const texture=material.texture?renderer3DRequireTexture(material.texture):null;if(texture&&!renderer3DUploadTexture(texture))return 0;renderer3DViewInto(renderer3DViewScratch);renderer3DProjectionInto(renderer3DProjectionScratch,backingWidth/backingHeight);renderer3DMultiplyInto(renderer3DMvpScratch,renderer3DProjectionScratch,renderer3DViewScratch);gl.enable(gl.DEPTH_TEST);gl.depthFunc(gl.LESS);gl.depthMask(false);gl.disable(gl.CULL_FACE);if(renderer3DRenderingDistortionVectors&&renderer3DDistortionEffective===1)gl.disable(gl.BLEND);else{gl.enable(gl.BLEND);gl.blendFunc(gl.SRC_ALPHA,renderer3DRenderingDistortionVectors||material.alphaMode===3?gl.ONE:gl.ONE_MINUS_SRC_ALPHA);}gl.useProgram(program.handle);gl.uniformMatrix4fv(program.viewProjection,false,renderer3DMvpScratch);gl.uniform4fv(program.materialColor,material.color);gl.uniform1f(program.textureEnabled,texture?1:0);gl.uniform1f(program.emissive,material.emissive);gl.uniform1f(program.hdrOutput,renderer3DHdrEffective?1:0);gl.activeTexture(gl.TEXTURE0);gl.bindTexture(gl.TEXTURE_2D,texture?texture.gpu:null);gl.uniform1i(program.effectTexture,0);const softDepthEnabled=material.softDepthMode!==0&&renderer3DSoftDepthEffective!==0&&!!renderer3DLinearDepthTexture,softDepthDistance=material.softDepthMode===2?material.softDepthDistance:24,targetWidth=renderer3DRenderingDistortionVectors?renderer3DDistortionWidth:backingWidth,targetHeight=renderer3DRenderingDistortionVectors?renderer3DDistortionHeight:backingHeight;gl.activeTexture(gl.TEXTURE6);gl.bindTexture(gl.TEXTURE_2D,softDepthEnabled?renderer3DLinearDepthTexture:null);gl.uniform1i(program.sceneDepthTexture,6);gl.uniform4f(program.softDepthSettings,softDepthEnabled?1:0,softDepthDistance,renderer3DCamera.near,renderer3DCamera.far);gl.uniform2f(program.targetSize,targetWidth,targetHeight);gl.uniform1f(program.softDepthFormat,renderer3DSoftDepthEffective);gl.uniform4f(program.distortionSettings,renderer3DRenderingDistortionVectors?1:0,material.distortionStrength/10000,material.distortionNoiseScale/100,material.distortionNoiseSpeed/1000);gl.uniform2f(program.distortionFlow,material.distortionFlowX/100,material.distortionFlowY/100);gl.uniform1f(program.distortionFormat,renderer3DDistortionEffective);if(softDepthEnabled)renderer3DSoftParticleDrawCount+=1;if(particle){let rightX=renderer3DViewScratch[0],rightY=renderer3DViewScratch[4],rightZ=renderer3DViewScratch[8],upX=renderer3DViewScratch[1],upY=renderer3DViewScratch[5],upZ=renderer3DViewScratch[9];if(batch.billboard===2){const length=Math.hypot(rightX,rightZ)||1;rightX/=length;rightY=0;rightZ/=length;upX=0;upY=1;upZ=0;}gl.uniform3f(program.cameraRight,rightX,rightY,rightZ);gl.uniform3f(program.cameraUp,upX,upY,upZ);gl.uniform2f(program.atlasScale,1/batch.columns,1/batch.rows);gl.bindBuffer(gl.ARRAY_BUFFER,renderer3DParticleQuadBuffer);gl.enableVertexAttribArray(0);gl.vertexAttribPointer(0,2,gl.FLOAT,false,16,0);gl.vertexAttribDivisor(0,0);gl.enableVertexAttribArray(1);gl.vertexAttribPointer(1,2,gl.FLOAT,false,16,8);gl.vertexAttribDivisor(1,0);gl.bindBuffer(gl.ARRAY_BUFFER,batch.gpu);for(let attribute=2;attribute<=4;attribute+=1){gl.enableVertexAttribArray(attribute);gl.vertexAttribPointer(attribute,4,gl.FLOAT,false,48,(attribute-2)*16);gl.vertexAttribDivisor(attribute,1);}gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,renderer3DParticleQuadIndexBuffer);gl.drawElementsInstanced(gl.TRIANGLES,6,gl.UNSIGNED_SHORT,0,batch.count);for(let attribute=2;attribute<=4;attribute+=1)gl.vertexAttribDivisor(attribute,0);renderer3DVfxParticleDrawCount+=1;renderer3DVfxParticleTriangleCount+=batch.count*2;renderer3DVfxTriangleCount+=batch.count*2;renderer3DSubmittedTriangleCount+=batch.count*2;}else{gl.bindBuffer(gl.ARRAY_BUFFER,batch.gpu);gl.enableVertexAttribArray(0);gl.vertexAttribPointer(0,3,gl.FLOAT,false,36,0);gl.vertexAttribDivisor(0,0);gl.enableVertexAttribArray(1);gl.vertexAttribPointer(1,2,gl.FLOAT,false,36,12);gl.vertexAttribDivisor(1,0);gl.enableVertexAttribArray(2);gl.vertexAttribPointer(2,4,gl.FLOAT,false,36,20);gl.vertexAttribDivisor(2,0);gl.disableVertexAttribArray(3);gl.disableVertexAttribArray(4);gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,null);gl.drawArrays(gl.TRIANGLE_STRIP,0,batch.count*2);const triangles=batch.count<2?0:batch.count*2-2;renderer3DVfxRibbonDrawCount+=1;renderer3DVfxRibbonTriangleCount+=triangles;renderer3DVfxTriangleCount+=triangles;renderer3DSubmittedTriangleCount+=triangles;}renderer3DDrawCallCount+=1;renderer3DVfxDrawCount+=1;return 1;}
            function renderer3DDrawVfxBatch(kind,handle){if(!renderer3DFrameActive){renderer3DLastError=14;return 0;}if(renderer3DMultipassActive||renderer3DSubmissionGroupActive){if(renderer3DSubmissionCount>=renderer3DSubmissions.length||(renderer3DSubmissionGroupActive&&renderer3DSubmissionGroupPhysical>=renderer3DSubmissionGroupReserved)){renderer3DRejectedSubmissionCount+=1;renderer3DVfxRejectedOperationCount+=1;renderer3DLastError=51;return 0;}const captured=renderer3DCaptureVfxSubmission(kind,handle,renderer3DSubmissionCount);if(!captured)return 0;if(captured===1){renderer3DSubmissionCount+=1;if(renderer3DSubmissionGroupActive)renderer3DSubmissionGroupPhysical+=1;}if(renderer3DSubmissionGroupActive)renderer3DSubmissionGroupLogical+=1;else{renderer3DLogicalSubmissionCount+=1;if(captured===1)renderer3DPhysicalSubmissionCount+=1;}if(kind===renderer3DSubmissionParticleBatch)renderer3DVfxParticleSubmissionCount+=1;else renderer3DVfxRibbonSubmissionCount+=1;return 1;}const captured=renderer3DCaptureVfxSubmission(kind,handle,0);if(!captured)return 0;const result=captured===2?1:renderer3DDrawVfxImmediate(renderer3DSubmissionObjects[0]);if(captured===1)renderer3DReleaseSubmission(0);if(result){renderer3DLogicalSubmissionCount+=1;if(captured===1)renderer3DPhysicalSubmissionCount+=1;if(kind===renderer3DSubmissionParticleBatch)renderer3DVfxParticleSubmissionCount+=1;else renderer3DVfxRibbonSubmissionCount+=1;}return result;}
            function renderer3DParticleBatchCommand(operation,b,c,d,e,f,g,h,i){if(operation===1)return renderer3DCreateParticleBatch(b,c,d,e,f);const batch=renderer3DParticleBatches.get(b);if(operation===7)return batch?1:0;if(!batch){renderer3DLastError=54;renderer3DVfxRejectedOperationCount+=1;return 0;}if(operation===2)return renderer3DSetParticle(batch,c,d,e,f,g,h,i);if(operation===3)return renderer3DSetParticleColor(batch,c,d,e,f,g);if(operation===4)return renderer3DCommitParticleBatch(batch,c);if(operation===5)return renderer3DDrawVfxBatch(renderer3DSubmissionParticleBatch,b);if(operation===6)return renderer3DDeleteParticleBatch(b);renderer3DLastError=54;renderer3DVfxRejectedOperationCount+=1;return 0;}
            function renderer3DRibbonBatchCommand(operation,b,c,d,e,f,g,h,i,j){if(operation===1)return renderer3DCreateRibbonBatch(b,c);const batch=renderer3DRibbonBatches.get(b);if(operation===7)return batch?1:0;if(!batch){renderer3DLastError=54;renderer3DVfxRejectedOperationCount+=1;return 0;}if(operation===2)return renderer3DSetRibbonPoint(batch,c,d,e,f,g,h,i,j);if(operation===3)return renderer3DSetRibbonColor(batch,c,d,e,f,g);if(operation===4)return renderer3DCommitRibbonBatch(batch,c);if(operation===5)return renderer3DDrawVfxBatch(renderer3DSubmissionRibbonBatch,b);if(operation===6)return renderer3DDeleteRibbonBatch(b);renderer3DLastError=54;renderer3DVfxRejectedOperationCount+=1;return 0;}
            function renderer3DM6Value(query,handle){if(query===1)return renderer3DParticleBatches.size;if(query===2)return 32;if(query===3)return renderer3DRibbonBatches.size;if(query===4)return 16;if(query===5)return renderer3DStagedParticleCapacity;if(query===6)return 8192;if(query===7)return renderer3DStagedRibbonCapacity;if(query===8)return 32768;if(query===9){let count=0;for(const batch of renderer3DParticleBatches.values())count+=batch.count;return count;}if(query===10){let count=0;for(const batch of renderer3DRibbonBatches.values())count+=batch.count;return count;}if(query===11)return renderer3DVfxDrawCount;if(query===12)return renderer3DVfxTriangleCount;if(query===13)return renderer3DVfxUploadCount;if(query===14)return renderer3DStagedParticleCapacity*96+renderer3DStagedRibbonCapacity*(44+144);if(query===15)return renderer3DStagedParticleCapacity*48+renderer3DStagedRibbonCapacity*72+76;if(query===16)return renderer3DVfxRejectedOperationCount;if(query===17)return renderer3DVfxParticleDrawCount;if(query===18)return renderer3DVfxRibbonDrawCount;if(query===19){let count=0;for(const batch of renderer3DParticleBatches.values())if(batch.inFlight)count+=1;for(const batch of renderer3DRibbonBatches.values())if(batch.inFlight)count+=1;return count;}if(query===20)return renderer3DVfxParticleSubmissionCount;if(query===21)return renderer3DVfxRibbonSubmissionCount;if(query===22)return renderer3DVfxParticleTriangleCount;if(query===23)return renderer3DVfxRibbonTriangleCount;const particle=renderer3DParticleBatches.get(handle),ribbon=renderer3DRibbonBatches.get(handle),batch=particle||ribbon;if(query>=30&&query<=41&&batch){if(query===30)return batch.capacity;if(query===31)return batch.count;if(query===32)return batch.revision;if(query===33)return particle?batch.capacity*96:batch.capacity*188;if(query===34)return particle?batch.capacity*48:batch.capacity*72;if(query===35)return batch.inFlight;if(query===36)return batch.material;if(query===37)return batch.stagingRevision;if(query===38)return batch.uploadedRevision;if(query===39)return batch.inFlight?7:(batch.revision?3:1);if(query===40)return 0;if(query===41)return particle?batch.count*48:batch.count*72;}renderer3DLastError=54;return 0;}
            function renderer3DGpuParticleReleaseGpu(system,deleteResources=true){const gl=renderer3DGl;if(deleteResources&&gl){for(let index=0;index<2;index+=1){if(system.simulationVaos[index])gl.deleteVertexArray(system.simulationVaos[index]);if(system.renderVaos[index])gl.deleteVertexArray(system.renderVaos[index]);if(system.transformFeedbacks[index])gl.deleteTransformFeedback(system.transformFeedbacks[index]);if(system.stateGpu[index])gl.deleteBuffer(system.stateGpu[index]);}if(system.spawnGpu)gl.deleteBuffer(system.spawnGpu);}if(system.gpuBytes){renderer3DGpuParticleGpuStateBytes-=system.gpuBytes;system.gpuBytes=0;}system.stateGpu[0]=system.stateGpu[1]=null;system.simulationVaos[0]=system.simulationVaos[1]=null;system.renderVaos[0]=system.renderVaos[1]=null;system.transformFeedbacks[0]=system.transformFeedbacks[1]=null;system.spawnGpu=null;system.firstDispatchComplete=false;}
            function renderer3DGpuParticleCreateGpu(system){if(system.requested===1)return false;if(!renderer3DInitialize()||!renderer3DCreateGpuParticlePipeline())return false;const gl=renderer3DGl;try{const usage=gl.DYNAMIC_COPY||gl.DYNAMIC_DRAW;system.spawnGpu=gl.createBuffer();if(!system.spawnGpu)throw new Error("spawn buffer allocation failed");gl.bindBuffer(gl.ARRAY_BUFFER,system.spawnGpu);gl.bufferData(gl.ARRAY_BUFFER,system.capacity*80,usage);for(let index=0;index<2;index+=1){system.stateGpu[index]=gl.createBuffer();if(!system.stateGpu[index])throw new Error("state buffer allocation failed");gl.bindBuffer(gl.ARRAY_BUFFER,system.stateGpu[index]);gl.bufferData(gl.ARRAY_BUFFER,system.capacity*80,usage);}for(let index=0;index<2;index+=1){system.simulationVaos[index]=gl.createVertexArray();system.renderVaos[index]=gl.createVertexArray();system.transformFeedbacks[index]=gl.createTransformFeedback();if(!system.stateGpu[index]||!system.simulationVaos[index]||!system.renderVaos[index]||!system.transformFeedbacks[index])throw new Error("particle resource allocation failed");gl.bindVertexArray(system.simulationVaos[index]);gl.bindBuffer(gl.ARRAY_BUFFER,system.stateGpu[index]);for(let attribute=0;attribute<5;attribute+=1){gl.enableVertexAttribArray(attribute);gl.vertexAttribPointer(attribute,4,gl.FLOAT,false,80,attribute*16);}gl.bindBuffer(gl.ARRAY_BUFFER,system.spawnGpu);for(let attribute=5;attribute<10;attribute+=1){gl.enableVertexAttribArray(attribute);gl.vertexAttribPointer(attribute,4,gl.FLOAT,false,80,(attribute-5)*16);}gl.bindVertexArray(system.renderVaos[index]);gl.bindBuffer(gl.ARRAY_BUFFER,renderer3DParticleQuadBuffer);gl.enableVertexAttribArray(0);gl.vertexAttribPointer(0,2,gl.FLOAT,false,16,0);gl.vertexAttribDivisor(0,0);gl.enableVertexAttribArray(1);gl.vertexAttribPointer(1,2,gl.FLOAT,false,16,8);gl.vertexAttribDivisor(1,0);gl.bindBuffer(gl.ARRAY_BUFFER,system.stateGpu[index]);for(let attribute=2;attribute<7;attribute+=1){gl.enableVertexAttribArray(attribute);gl.vertexAttribPointer(attribute,4,gl.FLOAT,false,80,(attribute-2)*16);gl.vertexAttribDivisor(attribute,1);}gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,renderer3DParticleQuadIndexBuffer);gl.bindTransformFeedback(gl.TRANSFORM_FEEDBACK,system.transformFeedbacks[index]);gl.bindBufferBase(gl.TRANSFORM_FEEDBACK_BUFFER,0,system.stateGpu[1-index]);}gl.bindTransformFeedback(gl.TRANSFORM_FEEDBACK,null);gl.bindVertexArray(null);gl.bindBuffer(gl.ARRAY_BUFFER,null);if(globalThis.SMILE_TEST_RENDERER3D_FORCE_GPU_PARTICLE_BUFFER_FAILURE||gl.getError()!==gl.NO_ERROR)throw new Error("particle resource validation failed");system.gpuBytes=system.capacity*240;renderer3DGpuParticleGpuStateBytes+=system.gpuBytes;system.effective=2;system.restartPending=false;return true;}catch(error){renderer3DGpuParticleReleaseGpu(system,true);system.effective=1;return false;}}
            function renderer3DGpuParticleStageKinematics(system,slot,x,y,z,vx,vy,vz){if(slot<0||slot>=system.capacity||x< -1000000||x>1000000||y< -1000000||y>1000000||z< -1000000||z>1000000||vx< -1000000||vx>1000000||vy< -1000000||vy>1000000||vz< -1000000||vz>1000000){renderer3DLastError=67;return 0;}if(system.inFlight){renderer3DLastError=69;return 0;}const offset=slot*20;system.stagedF[offset]=x;system.stagedF[offset+1]=y;system.stagedF[offset+2]=z;system.stagedF[offset+4]=vx;system.stagedF[offset+5]=vy;system.stagedF[offset+6]=vz;return 1;}
            function renderer3DGpuParticleStageVisual(system,slot,lifetime,startSize,endSize,rotation,angularVelocity,temperature,density){if(slot<0||slot>=system.capacity||lifetime<1||lifetime>600000||startSize<1||startSize>1000000||endSize<1||endSize>1000000||rotation< -1000000||rotation>1000000||angularVelocity< -1000000||angularVelocity>1000000||temperature<0||temperature>1000||density<0||density>1000){renderer3DLastError=67;return 0;}if(system.inFlight){renderer3DLastError=69;return 0;}const offset=slot*20;system.stagedF[offset+7]=lifetime;system.stagedF[offset+8]=startSize;system.stagedF[offset+9]=endSize;system.stagedF[offset+10]=rotation;system.stagedF[offset+11]=angularVelocity;system.stagedF[offset+12]=temperature/1000;system.stagedF[offset+13]=density/1000;return 1;}
            function renderer3DGpuParticleCommit(system,slot,serial,seed){if(slot<0||slot>=system.capacity||serial<=0||serial>2147483647||seed<0||seed>2147483647||system.pendingCount>=512||system.active[slot]||serial<=system.serials[slot]){renderer3DLastError=68;renderer3DGpuParticleSpawnsRejected+=1;return 0;}const staged=slot*20;if(system.inFlight||system.stagedF[staged+7]<=0||system.stagedF[staged+8]<=0){renderer3DLastError=system.inFlight?69:67;renderer3DGpuParticleSpawnsRejected+=1;return 0;}const command=system.pendingCount,commandOffset=command*20;system.commandSlots[command]=slot;system.commandSerials[command]=serial;for(let word=0;word<20;word+=1)system.commandF[commandOffset+word]=system.stagedF[staged+word];system.commandF[commandOffset+3]=0;system.commandF[commandOffset+16]=seed;system.commandF[commandOffset+17]=1;system.commandF[commandOffset+18]=serial;system.commandF[commandOffset+19]=0;system.pendingCount+=1;system.serials[slot]=serial;system.ages[slot]=0;system.lifetimes[slot]=Math.round(system.stagedF[staged+7]);system.active[slot]=1;system.activeCount+=1;renderer3DGpuParticleSpawnsAccepted+=1;return 1;}
            function renderer3DGpuParticleApplySpawns(system){const sourceF=system.stateF[system.readIndex],gl=renderer3DGl;if(system.effective===2){gl.bindBuffer(gl.ARRAY_BUFFER,system.spawnGpu);}for(let command=0;command<system.pendingCount;command+=1){const slot=system.commandSlots[command],serial=system.commandSerials[command];if(slot>=system.capacity||!system.active[slot]||system.serials[slot]!==serial)continue;const target=slot*20,source=command*20;for(let word=0;word<20;word+=1)sourceF[target+word]=system.commandF[source+word];if(system.effective===2)gl.bufferSubData(gl.ARRAY_BUFFER,slot*80,system.commandF,source,20);}if(system.pendingCount){const bytes=system.pendingCount*88;system.uploadBytes+=bytes;renderer3DGpuParticleCpuUploadBytes+=bytes;}system.pendingCount=0;}
            function renderer3DGpuParticleFinishStep(system){system.readIndex=1-system.readIndex;system.readGeneration=system.writeGeneration;system.writeGeneration+=1;if(system.writeGeneration>4294967295)system.writeGeneration=1;system.simulationSteps+=1;renderer3DGpuParticleSimulationSteps+=1;}
            function renderer3DGpuParticleUpdateSchedule(system){for(let slot=0;slot<system.capacity;slot+=1){if(!system.active[slot])continue;system.ages[slot]+=system.fixedStep;if(system.ages[slot]>=system.lifetimes[slot]){system.active[slot]=0;system.ages[slot]=system.lifetimes[slot];if(system.activeCount)system.activeCount-=1;}}}
            function renderer3DFireHash(value){value=(value^(value>>>16))>>>0;value=Math.imul(value,0x7feb352d)>>>0;value=(value^(value>>>15))>>>0;value=Math.imul(value,0x846ca68b)>>>0;return (value^(value>>>16))>>>0;}
            function renderer3DFireNoise(x,y,z,seed){const floorX=Math.floor(x),floorY=Math.floor(y),floorZ=Math.floor(z);let u=x-floorX,v=y-floorY,w=z-floorZ,result=0;u=u*u*(3-2*u);v=v*v*(3-2*v);w=w*w*(3-2*w);for(let corner=0;corner<8;corner+=1){const cellX=(floorX+(corner&1))|0,cellY=(floorY+((corner>>1)&1))|0,cellZ=(floorZ+((corner>>2)&1))|0,key=(Math.imul(cellX,73856093)^Math.imul(cellY,19349663)^Math.imul(cellZ,83492791)^(seed>>>0))>>>0,value=(renderer3DFireHash(key)&65535)/32767.5-1;result+=value*((corner&1)?u:1-u)*((corner&2)?v:1-v)*((corner&4)?w:1-w);}return result;}
            function renderer3DFireStep(system,state,offset,seconds){const dynamics=system.fire;for(let axis=0;axis<3;axis+=1)if(!Number.isFinite(state[offset+axis])||!Number.isFinite(state[offset+4+axis]))return false;if(!Number.isFinite(state[offset+8])||!Number.isFinite(state[offset+9])||!Number.isFinite(state[offset+12])||!Number.isFinite(state[offset+13]))return false;state[offset+12]=Math.max(0,state[offset+12]-dynamics.evolution[0]*seconds);state[offset+13]=Math.max(0,state[offset+13]-dynamics.evolution[1]*seconds);const seed=state[offset+16]>>>0,seedOffset=(seed&255)*.03125,pointX=state[offset]*dynamics.turbulence[1]+dynamics.time[0]*dynamics.turbulence[2]+seedOffset,pointY=state[offset+1]*dynamics.turbulence[1]+dynamics.time[0]*dynamics.turbulence[2]+seedOffset,pointZ=state[offset+2]*dynamics.turbulence[1]+dynamics.time[0]*dynamics.turbulence[2]+seedOffset;let flowX=0,flowY=0,flowZ=0;if(dynamics.turbulence[0]>0){const octaves=Math.min(2,Math.trunc(dynamics.turbulence[3]));for(let octave=0;octave<octaves;octave+=1){const frequency=octave===0?1:2,gain=octave===0?1:.5;flowX+=gain*renderer3DFireNoise(pointX*frequency,pointY*frequency,pointZ*frequency,seed);flowY+=gain*renderer3DFireNoise(pointX*frequency,pointY*frequency,pointZ*frequency,(seed+1013)>>>0);flowZ+=gain*renderer3DFireNoise(pointX*frequency,pointY*frequency,pointZ*frequency,(seed+2026)>>>0);}}const drag=Math.exp(-dynamics.windDrag[3]*seconds),temperature=state[offset+12];state[offset+4]=(state[offset+4]+(dynamics.gravityBuoyancy[0]+dynamics.windDrag[0]+flowX*dynamics.turbulence[0])*seconds)*drag;state[offset+5]=(state[offset+5]+(dynamics.gravityBuoyancy[1]+dynamics.windDrag[1]+dynamics.gravityBuoyancy[3]*temperature+flowY*dynamics.turbulence[0])*seconds)*drag;state[offset+6]=(state[offset+6]+(dynamics.gravityBuoyancy[2]+dynamics.windDrag[2]+flowZ*dynamics.turbulence[0])*seconds)*drag;const speedSquared=state[offset+4]*state[offset+4]+state[offset+5]*state[offset+5]+state[offset+6]*state[offset+6],speedLimit=dynamics.evolution[3],velocityScale=speedSquared>speedLimit*speedLimit?speedLimit/Math.sqrt(speedSquared):1;for(let axis=0;axis<3;axis+=1){state[offset+4+axis]*=velocityScale;state[offset+axis]+=state[offset+4+axis]*seconds;if(!Number.isFinite(state[offset+axis])||state[offset+axis]<dynamics.boundsMin[axis]||state[offset+axis]>dynamics.boundsMax[axis])return false;}state[offset+8]+=dynamics.evolution[2]*seconds;state[offset+9]+=dynamics.evolution[2]*seconds;return state[offset+13]>0&&state[offset+8]>0&&state[offset+9]>0&&state[offset+8]<=1000000&&state[offset+9]<=1000000;}
            function renderer3DGpuParticleCpuStep(system,applySpawns=true){if(applySpawns)renderer3DGpuParticleApplySpawns(system);const sourceF=system.stateF[system.readIndex],destinationF=system.stateF[1-system.readIndex],seconds=system.fixedStep/1000;for(let slot=0;slot<system.capacity;slot+=1){const offset=slot*20;for(let word=0;word<20;word+=1)destinationF[offset+word]=sourceF[offset+word];if(!system.active[slot]){destinationF[offset+17]=0;continue;}if(system.fire.render[0]>.5){if(destinationF[offset+17]>.5&&!renderer3DFireStep(system,destinationF,offset,seconds))destinationF[offset+17]=0;}else{destinationF[offset]+=sourceF[offset+4]*seconds;destinationF[offset+1]+=sourceF[offset+5]*seconds;destinationF[offset+2]+=sourceF[offset+6]*seconds;}destinationF[offset+3]+=system.fixedStep;destinationF[offset+10]+=sourceF[offset+11]*seconds;if(destinationF[offset+3]>=destinationF[offset+7])destinationF[offset+17]=0;}renderer3DGpuParticleUpdateSchedule(system);renderer3DGpuParticleFinishStep(system);}
            function renderer3DGpuParticleGpuStep(system){renderer3DGpuParticleApplySpawns(system);const gl=renderer3DGl,pipeline=renderer3DGpuParticlePipeline,simulation=pipeline.simulation;try{gl.useProgram(simulation.handle);gl.uniform1f(simulation.stepSeconds,system.fixedStep/1000);gl.uniform1f(simulation.stepMilliseconds,system.fixedStep);gl.uniform4fv(simulation.gravityBuoyancy,system.fire.gravityBuoyancy);gl.uniform4fv(simulation.windDrag,system.fire.windDrag);gl.uniform4fv(simulation.turbulence,system.fire.turbulence);gl.uniform4fv(simulation.evolution,system.fire.evolution);gl.uniform4fv(simulation.boundsMin,system.fire.boundsMin);gl.uniform4fv(simulation.boundsMax,system.fire.boundsMax);gl.uniform4fv(simulation.fireRender,system.fire.render);gl.uniform4fv(simulation.fireTime,system.fire.time);gl.enable(gl.RASTERIZER_DISCARD);gl.bindVertexArray(system.simulationVaos[system.readIndex]);gl.bindTransformFeedback(gl.TRANSFORM_FEEDBACK,system.transformFeedbacks[system.readIndex]);gl.beginTransformFeedback(gl.POINTS);gl.drawArrays(gl.POINTS,0,system.capacity);gl.endTransformFeedback();gl.bindTransformFeedback(gl.TRANSFORM_FEEDBACK,null);gl.bindVertexArray(null);gl.disable(gl.RASTERIZER_DISCARD);if(!system.firstDispatchComplete){if(gl.getError()!==gl.NO_ERROR)throw 0;system.firstDispatchComplete=true;}renderer3DGpuParticleDispatchCount+=1;renderer3DGpuParticleUpdateSchedule(system);renderer3DGpuParticleFinishStep(system);return;}catch(error){gl.disable(gl.RASTERIZER_DISCARD);renderer3DGpuParticleReleaseGpu(system,true);system.effective=1;renderer3DGpuParticleCpuStep(system,false);}}
            function renderer3DGpuParticleStep(system){if(system.restartPending&&system.requested!==1)renderer3DGpuParticleCreateGpu(system);if(system.effective===2)renderer3DGpuParticleGpuStep(system);else renderer3DGpuParticleCpuStep(system);}
            function renderer3DGpuParticleAdvance(system,elapsed){if(elapsed<0||elapsed>1000){renderer3DLastError=67;return 0;}if(system.inFlight){renderer3DLastError=69;return 0;}const accepted=Math.min(elapsed,250);renderer3DGpuParticleDroppedTime+=elapsed-accepted;system.accumulator+=accepted;while(system.accumulator>=system.fixedStep){renderer3DGpuParticleStep(system);system.fire.time[0]+=system.fixedStep/1000;if(system.fire.time[0]>=4096)system.fire.time[0]-=4096;system.accumulator-=system.fixedStep;}return 1;}
            function renderer3DGpuParticleKill(system,slot){if(slot<0||slot>=system.capacity){renderer3DLastError=67;return 0;}if(system.inFlight){renderer3DLastError=69;return 0;}if(system.active[slot]){system.active[slot]=0;if(system.activeCount)system.activeCount-=1;}system.stateF[0][slot*20+17]=0;system.stateF[1][slot*20+17]=0;system.ages[slot]=system.lifetimes[slot];if(system.effective===2){const gl=renderer3DGl;for(let index=0;index<2;index+=1){gl.bindBuffer(gl.ARRAY_BUFFER,system.stateGpu[index]);gl.bufferSubData(gl.ARRAY_BUFFER,slot*80,system.zeroF);}gl.bindBuffer(gl.ARRAY_BUFFER,system.spawnGpu);gl.bufferSubData(gl.ARRAY_BUFFER,slot*80,system.zeroF);system.uploadBytes+=240;renderer3DGpuParticleCpuUploadBytes+=240;}return 1;}
            function renderer3DReleaseGpuParticleFrameSystems(){while(renderer3DGpuParticleFrameCount){const handle=renderer3DGpuParticleFrameHandles[--renderer3DGpuParticleFrameCount],system=renderer3DGpuParticleSystems.get(handle);if(system&&system.inFlight)system.inFlight-=1;renderer3DGpuParticleFrameHandles[renderer3DGpuParticleFrameCount]=0;}}
            function renderer3DGpuParticleQueue(handle,system){if(!renderer3DFrameActive){renderer3DLastError=14;return 0;}if(!system.activeCount)return 2;if(renderer3DSubmissionGroupActive||renderer3DGpuParticleFrameCount>=32){renderer3DLastError=68;return 0;}for(let index=0;index<renderer3DGpuParticleFrameCount;index+=1)if(renderer3DGpuParticleFrameHandles[index]===handle)return 2;renderer3DGpuParticleFrameHandles[renderer3DGpuParticleFrameCount++]=handle;system.inFlight+=1;system.queueEntries+=1;renderer3DGpuParticleQueueEntries+=1;renderer3DLogicalSubmissionCount+=1;return 1;}
            function renderer3DGpuParticleIsDistortion(system){const material=renderer3DMaterials.get(system.material);return !!material&&material.vfxShadingMode===1;}
            function renderer3DDrawGpuParticleSystem(system){if(system.effective!==2||!system.stateGpu[system.readIndex]||!renderer3DGpuParticlePipeline)return 1;const gl=renderer3DGl,program=renderer3DGpuParticlePipeline.render,material=renderer3DMaterials.get(system.material);if(!material){renderer3DLastError=67;return 0;}const texture=material.texture?renderer3DRequireTexture(material.texture):null;if(texture&&!renderer3DUploadTexture(texture))return 0;renderer3DViewInto(renderer3DViewScratch);renderer3DProjectionInto(renderer3DProjectionScratch,backingWidth/backingHeight);renderer3DMultiplyInto(renderer3DMvpScratch,renderer3DProjectionScratch,renderer3DViewScratch);gl.enable(gl.DEPTH_TEST);gl.depthFunc(gl.LESS);gl.depthMask(false);gl.disable(gl.CULL_FACE);if(renderer3DRenderingDistortionVectors&&renderer3DDistortionEffective===1)gl.disable(gl.BLEND);else{gl.enable(gl.BLEND);gl.blendFunc(gl.SRC_ALPHA,renderer3DRenderingDistortionVectors||material.alphaMode===3?gl.ONE:gl.ONE_MINUS_SRC_ALPHA);}gl.useProgram(program.handle);gl.uniformMatrix4fv(program.viewProjection,false,renderer3DMvpScratch);let rightX=renderer3DViewScratch[0],rightY=renderer3DViewScratch[4],rightZ=renderer3DViewScratch[8],upX=renderer3DViewScratch[1],upY=renderer3DViewScratch[5],upZ=renderer3DViewScratch[9];gl.uniform3f(program.cameraRight,rightX,rightY,rightZ);gl.uniform3f(program.cameraUp,upX,upY,upZ);gl.uniform4f(program.fireRender,system.fire.render[1],system.fire.render[2],system.fire.render[3],system.fire.time[1]);gl.uniform4fv(program.materialColor,material.color);gl.uniform1f(program.textureEnabled,texture?1:0);gl.uniform1f(program.emissive,material.emissive);gl.uniform1f(program.hdrOutput,renderer3DHdrEffective?1:0);gl.activeTexture(gl.TEXTURE0);gl.bindTexture(gl.TEXTURE_2D,texture?texture.gpu:null);gl.uniform1i(program.effectTexture,0);const softDepthEnabled=material.softDepthMode!==0&&renderer3DSoftDepthEffective!==0&&!!renderer3DLinearDepthTexture,softDepthDistance=material.softDepthMode===2?material.softDepthDistance:24,targetWidth=renderer3DRenderingDistortionVectors?renderer3DDistortionWidth:backingWidth,targetHeight=renderer3DRenderingDistortionVectors?renderer3DDistortionHeight:backingHeight;gl.activeTexture(gl.TEXTURE6);gl.bindTexture(gl.TEXTURE_2D,softDepthEnabled?renderer3DLinearDepthTexture:null);gl.uniform1i(program.sceneDepthTexture,6);gl.uniform4f(program.softDepthSettings,softDepthEnabled?1:0,softDepthDistance,renderer3DCamera.near,renderer3DCamera.far);gl.uniform2f(program.targetSize,targetWidth,targetHeight);gl.uniform1f(program.softDepthFormat,renderer3DSoftDepthEffective);gl.uniform4f(program.distortionSettings,renderer3DRenderingDistortionVectors?1:0,material.distortionStrength/10000,material.distortionNoiseScale/100,material.distortionNoiseSpeed/1000);gl.uniform2f(program.distortionFlow,material.distortionFlowX/100,material.distortionFlowY/100);gl.uniform1f(program.distortionFormat,renderer3DDistortionEffective);if(softDepthEnabled)renderer3DSoftParticleDrawCount+=1;gl.bindVertexArray(system.renderVaos[system.readIndex]);gl.drawElementsInstanced(gl.TRIANGLES,6,gl.UNSIGNED_SHORT,0,system.capacity);gl.bindVertexArray(null);renderer3DGpuParticleRenderDrawCount+=1;renderer3DVfxDrawCount+=1;renderer3DVfxParticleDrawCount+=1;renderer3DVfxParticleTriangleCount+=system.capacity*2;renderer3DVfxTriangleCount+=system.capacity*2;renderer3DSubmittedTriangleCount+=system.capacity*2;renderer3DDrawCallCount+=1;return 1;}
            function renderer3DDrawQueuedGpuParticles(includeDistortion){for(let index=0;index<renderer3DGpuParticleFrameCount;index+=1){const system=renderer3DGpuParticleSystems.get(renderer3DGpuParticleFrameHandles[index]);if(!system||(!includeDistortion&&renderer3DGpuParticleIsDistortion(system)))continue;if(!renderer3DDrawGpuParticleSystem(system))return 0;}return 1;}
            function renderer3DGpuParticleDelete(handle,system){if(system.inFlight){renderer3DLastError=69;return 0;}renderer3DGpuParticleReleaseGpu(system,true);renderer3DGpuParticleTotalCapacity-=system.capacity;renderer3DGpuParticleSystems.delete(handle);return 1;}
            function renderer3DGpuParticleHandleContextLoss(){renderer3DGpuParticlePipeline=null;renderer3DGpuParticlePipelineAttempted=false;renderer3DGpuParticleBackendAvailable=false;for(const system of renderer3DGpuParticleSystems.values()){if(system.effective===2){renderer3DGpuParticleRestartCount+=1;system.restartPending=true;}renderer3DGpuParticleReleaseGpu(system,false);system.effective=1;system.active.fill(0);system.ages.fill(0);system.activeCount=0;system.pendingCount=0;system.accumulator=0;}}
            function renderer3DGpuParticleValue(query,handle,slot){if(query===1)return renderer3DGpuParticleSystems.size;if(query===2)return 32;if(query===3)return 8192;if(query===60)return 32768;if(query===4)return 1;if(query===5)return 80;if(query===6)return renderer3DGpuParticleBackendAvailable?2:0;if(query===7)return renderer3DGpuParticleBackendAvailable?0:1;if(query===8)return renderer3DGpuParticleTotalCapacity;if(query===9){let count=0;for(const system of renderer3DGpuParticleSystems.values())count+=system.activeCount;return count;}if(query===10)return renderer3DGpuParticleSpawnsAccepted;if(query===11)return renderer3DGpuParticleSpawnsRejected;if(query===12)return renderer3DGpuParticleSimulationSteps;if(query===13)return renderer3DGpuParticleDroppedTime;if(query===14)return renderer3DGpuParticleDispatchCount;if(query===15)return renderer3DGpuParticleRenderDrawCount;if(query===16)return renderer3DGpuParticleCpuUploadBytes;if(query===17)return renderer3DGpuParticleGpuStateBytes;if(query===18)return renderer3DGpuParticleRestartCount;if(query===19)return renderer3DGpuParticleReadbackCount;if(query===20)return 512;if(query===21)return 16384;if(query===22)return renderer3DInitialize()&&renderer3DCreateGpuParticlePipeline()?1:0;const system=renderer3DGpuParticleSystems.get(handle);if(query>=51&&query<=59){if(!system){renderer3DLastError=67;return 0;}if(query===51)return system.fire.render[0]>.5?1:0;if(system.effective!==1)return -1;if(slot<0||slot>=system.capacity){renderer3DLastError=67;return 0;}const offset=slot*20,state=system.stateF[system.readIndex];if(query===52)return Math.round(state[offset+1]*1000);if(query===53)return Math.round(state[offset+4]*1000);if(query===54)return Math.round(state[offset+5]*1000);if(query===55)return Math.round(state[offset+12]*1000);if(query===56)return Math.round(state[offset+13]*1000);if(query===57)return Math.round(state[offset+8]*1000);if(query===58)return state[offset+17]>.5?1:0;if(query===59)return Math.round(state[offset]*1000);}if(query>=30&&query<=50){if(!system){renderer3DLastError=67;return 0;}if(query===30)return system.capacity;if(query===31)return system.activeCount;if(query===32)return system.pendingCount;if(query===33)return system.fixedStep;if(query===34)return system.accumulator;if(query===35)return system.readGeneration;if(query===36)return system.writeGeneration;if(query===37)return system.requested;if(query===38)return system.effective;if(query===39)return system.inFlight;if(query===40)return system.material;if(query===46)return system.capacity*253+45056;if(query===47)return system.gpuBytes;if(query===48)return system.simulationSteps;if(query===49)return system.uploadBytes;if(query===50)return system.queueEntries;if(slot<0||slot>=system.capacity){renderer3DLastError=67;return 0;}const offset=slot*20,state=system.stateF[system.readIndex];if(query===41)return Math.round(state[offset]);if(query===42)return system.ages[slot];if(query===43)return system.lifetimes[slot];if(query===44)return system.serials[slot];if(query===45)return system.active[slot]?1:0;}renderer3DLastError=67;return 0;}
            function renderer3DGpuParticleCommand(operation,b,c,d,e,f,g,h,i,j){if(operation===1)return renderer3DGpuParticleCreate(b,c,d,e);if(operation===10)return renderer3DGpuParticleValue(b,c,d);const system=renderer3DGpuParticleSystems.get(b);if(operation===8)return system?1:0;if(!system){renderer3DLastError=67;return 0;}if(operation===2)return renderer3DGpuParticleStageKinematics(system,c,d,e,f,g,h,i);if(operation===3)return renderer3DGpuParticleStageVisual(system,c,d,e,f,g,h,i,j);if(operation===4)return renderer3DGpuParticleCommit(system,c,d,e);if(operation===5)return renderer3DGpuParticleAdvance(system,c);if(operation===6)return renderer3DGpuParticleKill(system,c);if(operation===7)return renderer3DGpuParticleQueue(b,system);if(operation>=11&&operation<=15){if(system.inFlight){renderer3DLastError=69;return 0;}let valid=true;if(operation===11){valid=c>=-1000000&&c<=1000000&&d>=-1000000&&d<=1000000&&e>=-1000000&&e<=1000000&&f>=-1000000&&f<=1000000&&g>=-1000000&&g<=1000000&&h>=-1000000&&h<=1000000&&i>=-1000000&&i<=1000000&&j>=0&&j<=100000;if(valid){system.fire.gravityBuoyancy[0]=c;system.fire.gravityBuoyancy[1]=d;system.fire.gravityBuoyancy[2]=e;system.fire.gravityBuoyancy[3]=i;system.fire.windDrag[0]=f;system.fire.windDrag[1]=g;system.fire.windDrag[2]=h;system.fire.windDrag[3]=j/1000;}}else if(operation===12){valid=c>=0&&c<=1000000&&d>=0&&d<=1000000&&e>=0&&e<=10000&&f>=0&&f<=2;if(valid){system.fire.turbulence[0]=c;system.fire.turbulence[1]=d/1000000;system.fire.turbulence[2]=e/1000;system.fire.turbulence[3]=f;}}else if(operation===13){valid=c>=0&&c<=100000&&d>=0&&d<=100000&&e>=-100000&&e<=100000&&f>=1&&f<=1000000;if(valid){system.fire.evolution[0]=c/1000;system.fire.evolution[1]=d/1000;system.fire.evolution[2]=e;system.fire.evolution[3]=f;}}else if(operation===14){valid=c>=-1000000&&d>=-1000000&&e>=-1000000&&f<=1000000&&g<=1000000&&h<=1000000&&c<f&&d<g&&e<h;if(valid){system.fire.boundsMin[0]=c;system.fire.boundsMin[1]=d;system.fire.boundsMin[2]=e;system.fire.boundsMax[0]=f;system.fire.boundsMax[1]=g;system.fire.boundsMax[2]=h;}}else{valid=c>=0&&c<=3&&d>=0&&d<=2&&e>=1&&e<=8&&f>=1&&f<=8;if(valid){system.fire.render[1]=c;system.fire.render[2]=d;system.fire.render[3]=e;system.fire.time[1]=f;}}if(!valid){renderer3DLastError=67;return 0;}system.fire.render[0]=1;return 1;}if(operation===9)return renderer3DGpuParticleDelete(b,system);renderer3DLastError=67;return 0;}
            function renderer3DSoftDepthCommand(operation,b,c,d){if(operation===1){if(renderer3DFrameActive||(b!==0&&b!==1)){renderer3DLastError=65;return 0;}if(renderer3DSoftDepthRequested!==(b!==0)){renderer3DSoftDepthRequested=b!==0;renderer3DM5ConfigurationRevision+=1;if(renderer3DM5ConfigurationRevision>2147483647)renderer3DM5ConfigurationRevision=1;}return 1;}if(operation===2){const material=renderer3DMaterials.get(b);if(!material||material.kind!==0||(material.alphaMode!==2&&material.alphaMode!==3)||c<0||c>2||(c===2&&(d<=0||d>1000000))||(c!==2&&d!==0)){renderer3DLastError=65;return 0;}material.softDepthMode=c;material.softDepthDistance=c===2?d:0;return 1;}if(operation===3){if(b===1)return renderer3DSoftDepthRequested?1:0;if(b===2||b===5)return renderer3DSoftDepthEffective;if(b===3)return renderer3DSoftDepthWidth;if(b===4)return renderer3DSoftDepthHeight;if(b===6)return renderer3DSoftDepthBytes;if(b===7)return renderer3DSoftDepthCopyDrawCount;if(b===8)return renderer3DSoftDepthCopyFailureCount;if(b===9)return renderer3DSoftParticleDrawCount;if(b===10){let count=0;for(const material of renderer3DMaterials.values())if(material.softDepthMode)count+=1;return count;}if(b===11)return renderer3DSoftDepthFallbackReason;if(b===12)return renderer3DSoftDepthResourceGeneration;if(b===13||b===14){const material=renderer3DMaterials.get(c);if(!material){renderer3DLastError=65;return 0;}return b===13?material.softDepthMode:Math.round(material.softDepthDistance);}}renderer3DLastError=65;return 0;}
            function renderer3DDistortionCommand(operation,b,c,d,e,f,g){if(operation===1){if(renderer3DFrameActive||(b!==0&&b!==1)||c<0||c>3){renderer3DLastError=66;return 0;}const quality=c===0?3:c;if(renderer3DDistortionRequested!==(b!==0)||renderer3DDistortionQuality!==quality){renderer3DDistortionRequested=b!==0;renderer3DDistortionQuality=quality;renderer3DM5ConfigurationRevision+=1;if(renderer3DM5ConfigurationRevision>2147483647)renderer3DM5ConfigurationRevision=1;}return 1;}if(operation===2){const material=renderer3DMaterials.get(b);if(!material||material.kind!==0||(material.alphaMode!==2&&material.alphaMode!==3)||c<0||c>100||d<1||d>1000||e< -1000||e>1000||f< -100||f>100||g< -100||g>100||(c>0&&f===0&&g===0)){renderer3DLastError=66;return 0;}material.vfxShadingMode=c===0?0:1;material.distortionStrength=c;material.distortionNoiseScale=d;material.distortionNoiseSpeed=e;material.distortionFlowX=f;material.distortionFlowY=g;return 1;}if(operation===3){if(b===1)return renderer3DDistortionRequested?1:0;if(b===2||b===5)return renderer3DDistortionEffective;if(b===3)return renderer3DDistortionWidth;if(b===4)return renderer3DDistortionHeight;if(b===6)return renderer3DDistortionBytes;if(b===7)return renderer3DDistortionEmitterCount;if(b===8)return renderer3DDistortionVectorDrawCount;if(b===9)return renderer3DDistortionCompositeDrawCount;if(b===10)return renderer3DDistortionMaximumStrength;if(b===11)return renderer3DDistortionFallbackReason;if(b===12)return renderer3DDistortionResourceGeneration;if(b===15)return renderer3DDistortionQuality;if(b===13||b===14){const material=renderer3DMaterials.get(c);if(!material){renderer3DLastError=66;return 0;}return b===13?material.vfxShadingMode:Math.round(material.distortionStrength);}}renderer3DLastError=66;return 0;}
            function renderer3DDrawImmediate(handle,snapshot=null){const object=snapshot||renderer3DRequireObject(handle);if(!renderer3DFrameActive||!object){renderer3DLastError=14;return 0;}if(object.snapshot&&object.kind!==renderer3DSubmissionObject)return renderer3DDrawVfxImmediate(object);
                if(!object.visible)return 1;const mesh=renderer3DRequireMesh(object.mesh);if(!mesh||!renderer3DUpload(mesh))return 0;
                const material=object.snapshot?(object.hasMaterial?object.snapshotMaterial:null):(object.material?renderer3DRequireMaterial(object.material):null),animator=object.snapshot?(object.paletteIndex>=0?renderer3DPaletteSnapshots[object.paletteIndex]:null):(object.animator?renderer3DAnimators.get(object.animator):null),
                    skeleton=animator&&!animator.production&&!object.snapshot?renderer3DSkeletons.get(animator.skeleton):null;if(!object.snapshot&&object.animator&&(!animator||(animator.production?!renderer3DModelOwnsMesh(animator.model,object.mesh):(!skeleton||mesh.maxJoint>=skeleton.boneCount)))){
                    renderer3DLastError=36;return 0;}if(material&&material.kind===1)return renderer3DDrawPbr(object,mesh,material,animator);
                const texture=material&&material.texture?renderer3DRequireTexture(material.texture):null;if(texture&&!renderer3DUploadTexture(texture))return 0;
                const gl=renderer3DGl,model=renderer3DModelInto(renderer3DModelScratch,object),view=renderer3DViewInto(renderer3DViewScratch),
                    projection=renderer3DProjectionInto(renderer3DProjectionScratch,backingWidth/backingHeight),tint=renderer3DTintScratch,materialValues=renderer3DMaterialScratch;
                renderer3DMultiplyInto(renderer3DMatrixScratchA,view,model);renderer3DMultiplyInto(renderer3DMvpScratch,projection,renderer3DMatrixScratchA);
                renderer3DMultiplyInto(renderer3DMatrixScratchB,renderer3DShadowMatrixScratch,model);
                for(let index=0;index<4;index+=1)tint[index]=object.color[index]*(material?material.color[index]:1);
                materialValues[0]=texture?1:0;materialValues[1]=material&&material.unlit?1:0;materialValues[2]=material?material.emissive:0;
                materialValues[3]=material&&material.alphaMode===1?material.cutoff:-1;const alphaMode=material?material.alphaMode:(tint[3]<.999?2:0);
                renderer3DApplyCull(object,true);if(alphaMode===2||alphaMode===3){gl.enable(gl.BLEND);gl.blendFunc(gl.SRC_ALPHA,alphaMode===3?gl.ONE:gl.ONE_MINUS_SRC_ALPHA);gl.depthMask(false);}
                else{gl.disable(gl.BLEND);gl.depthMask(true);}gl.useProgram(renderer3DProgram.handle);renderer3DBindMesh(mesh,false);gl.activeTexture(gl.TEXTURE0);
                gl.bindTexture(gl.TEXTURE_2D,texture?texture.gpu:null);gl.uniform1i(renderer3DProgram.baseTexture,0);
                gl.uniformMatrix4fv(renderer3DProgram.model,false,model);gl.uniformMatrix4fv(renderer3DProgram.mvp,false,renderer3DMvpScratch);
                gl.uniformMatrix4fv(renderer3DProgram.shadowMvp,false,renderer3DMatrixScratchB);
                gl.uniformMatrix4fv(renderer3DProgram.bones,false,animator&&!animator.production?animator.palette:renderer3DStaticBones);gl.uniform1f(renderer3DProgram.skinning,animator?1:0);if(!renderer3DBindModelPalette(object.animator,animator,renderer3DProgram,false,object.ignoreNodeOffsets))return 0;
                gl.uniform4fv(renderer3DProgram.tint,tint);gl.uniform4fv(renderer3DProgram.material,materialValues);
                if(renderer3DShadowEffective){gl.activeTexture(gl.TEXTURE5);gl.bindTexture(gl.TEXTURE_2D,renderer3DShadowTexture);}gl.uniform1i(renderer3DProgram.shadowMap,5);
                if(typeof gl.uniform4f==="function")gl.uniform4f(renderer3DProgram.shadowSettings,renderer3DShadowEffective&&object.receivesShadow?1:0,renderer3DShadowSettings[0],renderer3DShadowSettings[1],renderer3DShadowResolution>0?1/renderer3DShadowResolution:0);gl.uniform1f(renderer3DProgram.hdrOutput,renderer3DHdrEffective?1:0);
                if(typeof gl.uniform4f==="function"){const offset=renderer3DShadowSlot*4;if(renderer3DShadowCaster===2)gl.uniform4f(renderer3DProgram.shadowLight,renderer3DLocalPositionType[offset],renderer3DLocalPositionType[offset+1],renderer3DLocalPositionType[offset+2],2);else gl.uniform4f(renderer3DProgram.shadowLight,renderer3DDirectionalDirection[0],renderer3DDirectionalDirection[1],renderer3DDirectionalDirection[2],1);}
                gl.drawElements(gl.TRIANGLES,mesh.indexCount,gl.UNSIGNED_INT,0);renderer3DDrawCallCount+=1;renderer3DSubmittedTriangleCount+=mesh.indexCount/3;
                renderer3DSimpleDrawCount+=1;return 1;}
            function renderer3DDraw(handle){const object=renderer3DRequireObject(handle);if(!renderer3DFrameActive||!object){renderer3DLastError=14;return 0;}if(renderer3DMultipassActive||renderer3DSubmissionGroupActive){if(!object.visible){if(renderer3DSubmissionGroupActive)renderer3DSubmissionGroupLogical+=1;else renderer3DLogicalSubmissionCount+=1;return 1;}if(renderer3DSubmissionCount>=renderer3DSubmissions.length||(renderer3DSubmissionGroupActive&&renderer3DSubmissionGroupPhysical>=renderer3DSubmissionGroupReserved)){renderer3DRejectedSubmissionCount+=1;renderer3DLastError=51;return 0;}const captured=renderer3DCaptureSubmission(handle,renderer3DSubmissionCount);if(!captured)return 0;if(captured===1){renderer3DSubmissionCount+=1;if(renderer3DSubmissionGroupActive)renderer3DSubmissionGroupPhysical+=1;}if(renderer3DSubmissionGroupActive)renderer3DSubmissionGroupLogical+=1;else{renderer3DLogicalSubmissionCount+=1;if(captured===1)renderer3DPhysicalSubmissionCount+=1;}return 1;}const paletteStart=renderer3DPaletteSnapshotCount,captured=renderer3DCaptureSubmission(handle,0);if(!captured){renderer3DPaletteSnapshotCount=paletteStart;return 0;}const result=captured===2?1:renderer3DDrawImmediate(0,renderer3DSubmissionObjects[0]);if(captured===1)renderer3DReleaseSubmission(0);renderer3DPaletteSnapshotCount=paletteStart;if(result){renderer3DLogicalSubmissionCount+=1;if(captured===1)renderer3DPhysicalSubmissionCount+=1;}return result;}
            function renderer3DRenderShadowPass(){if(!renderer3DShadowEffective)return 1;const gl=renderer3DGl;gl.activeTexture(gl.TEXTURE5);gl.bindTexture(gl.TEXTURE_2D,null);gl.bindFramebuffer(gl.FRAMEBUFFER,renderer3DShadowFramebuffer);gl.viewport(0,0,renderer3DShadowResolution,renderer3DShadowResolution);gl.colorMask(false,false,false,false);gl.clearDepth(1);gl.clear(gl.DEPTH_BUFFER_BIT);
                gl.enable(gl.DEPTH_TEST);gl.depthFunc(gl.LESS);gl.depthMask(true);gl.disable(gl.BLEND);gl.enable(gl.POLYGON_OFFSET_FILL);gl.polygonOffset(1,Math.max(1,renderer3DShadowSettings[0]*100000));gl.useProgram(renderer3DShadowProgram.handle);
                renderer3DModelPaletteCachedAnimator=0;renderer3DModelPaletteCachedRevision=0;
                for(let submission=0;submission<renderer3DSubmissionCount;submission+=1){const object=renderer3DSubmissionObjects[submission];if(object.kind!==renderer3DSubmissionObject||!object.visible||!object.castsShadow)continue;
                    const mesh=renderer3DRequireMesh(object.mesh);if(!mesh||!renderer3DUpload(mesh))return 0;const material=object.hasMaterial?object.snapshotMaterial:null;
                    const alphaMode=material?material.alphaMode:(object.color[3]<.999?2:0);if(alphaMode===2||alphaMode===3)continue;const textureHandle=material?(material.kind===1?material.textures[0]:material.texture):0,texture=textureHandle?renderer3DRequireTexture(textureHandle):null;if(texture&&!renderer3DUploadTexture(texture))return 0;
                    const animator=object.paletteIndex>=0?renderer3DPaletteSnapshots[object.paletteIndex]:null;
                    const model=renderer3DModelInto(renderer3DModelScratch,object);if(!renderer3DNormalInto(renderer3DNormalScratch,model)){renderer3DLastError=46;return 0;}renderer3DMultiplyInto(renderer3DMvpScratch,renderer3DShadowMatrixScratch,model);
                    gl.uniformMatrix4fv(renderer3DShadowProgram.mvp,false,renderer3DMvpScratch);gl.uniformMatrix4fv(renderer3DShadowProgram["bones[0]"],false,animator&&!animator.production?animator.palette:renderer3DStaticBones);gl.uniform1f(renderer3DShadowProgram.skinning,animator?1:0);if(!renderer3DBindModelPalette(object.animator,animator,renderer3DShadowProgram,true,object.ignoreNodeOffsets))return 0;
                    gl.activeTexture(gl.TEXTURE0);gl.bindTexture(gl.TEXTURE_2D,texture?texture.gpu:null);gl.uniform1i(renderer3DShadowProgram.baseTexture,0);const baseAlpha=object.color[3]*(material?(material.kind===1?material.baseColor[3]:material.color[3]):1);gl.uniform3f(renderer3DShadowProgram.alphaSettings,texture?1:0,alphaMode===1&&material?material.cutoff:-1,baseAlpha);
                    renderer3DApplyCull(object,!!(material&&material.doubleSided));renderer3DBindMesh(mesh,false);gl.drawElements(gl.TRIANGLES,mesh.indexCount,gl.UNSIGNED_INT,0);renderer3DShadowDrawCount+=1;renderer3DShadowTriangleCount+=mesh.indexCount/3;}
                renderer3DModelPaletteCachedAnimator=0;renderer3DModelPaletteCachedRevision=0;gl.disable(gl.POLYGON_OFFSET_FILL);gl.colorMask(true,true,true,true);gl.bindFramebuffer(gl.FRAMEBUFFER,null);return 1;}
            function renderer3DPostPass(target,width,height,sceneTexture,bloomTexture,mode,x,y,w,secondX,secondY){const gl=renderer3DGl;gl.bindFramebuffer(gl.FRAMEBUFFER,target);gl.viewport(0,0,width,height);gl.disable(gl.DEPTH_TEST);gl.depthMask(false);gl.disable(gl.BLEND);gl.disable(gl.CULL_FACE);gl.useProgram(renderer3DPostProgram.handle);
                gl.activeTexture(gl.TEXTURE0);gl.bindTexture(gl.TEXTURE_2D,sceneTexture);gl.uniform1i(renderer3DPostProgram.sceneTexture,0);gl.activeTexture(gl.TEXTURE1);gl.bindTexture(gl.TEXTURE_2D,bloomTexture||sceneTexture);gl.uniform1i(renderer3DPostProgram.bloomTexture,1);gl.uniform4f(renderer3DPostProgram.first,mode,x,y,w);gl.uniform4f(renderer3DPostProgram.second,secondX,secondY,0,0);gl.drawArrays(gl.TRIANGLES,0,3);renderer3DPostDrawCount+=1;}
            function renderer3DSubmissionIsOpaque(object){if(object.kind!==renderer3DSubmissionObject)return false;const material=object.hasMaterial?object.snapshotMaterial:null,alphaMode=material?material.alphaMode:(object.color[3]<.999?2:0);return alphaMode!==2&&alphaMode!==3;}
            function renderer3DSubmissionIsDistortion(object){return object.kind!==renderer3DSubmissionObject&&object.hasMaterial&&object.snapshotMaterial.vfxShadingMode===1;}
            function renderer3DSnapshotLinearDepth(){if(!renderer3DSoftDepthEffective)return 1;const gl=renderer3DGl;if(!renderer3DDepthProgram||!renderer3DSceneDepth||!renderer3DLinearDepthFramebuffer){renderer3DSoftDepthCopyFailureCount+=1;renderer3DSoftDepthFallbackReason=3;renderer3DSoftDepthEffective=0;return 1;}gl.bindFramebuffer(gl.FRAMEBUFFER,renderer3DLinearDepthFramebuffer);gl.viewport(0,0,backingWidth,backingHeight);gl.disable(gl.DEPTH_TEST);gl.depthMask(false);gl.disable(gl.BLEND);gl.disable(gl.CULL_FACE);gl.useProgram(renderer3DDepthProgram.handle);gl.activeTexture(gl.TEXTURE0);gl.bindTexture(gl.TEXTURE_2D,renderer3DSceneDepth);gl.uniform1i(renderer3DDepthProgram.sourceDepth,0);gl.uniform2f(renderer3DDepthProgram.nearFar,renderer3DCamera.near,renderer3DCamera.far);gl.uniform1f(renderer3DDepthProgram.packedMode,renderer3DSoftDepthEffective===1?1:0);gl.drawArrays(gl.TRIANGLES,0,3);renderer3DSoftDepthCopyDrawCount+=1;return 1;}
            function renderer3DRenderDistortionPass(){if(!renderer3DDistortionEffective)return 1;const gl=renderer3DGl;if(!renderer3DDistortionFramebuffer||!renderer3DDistortionTexture||!renderer3DDistortionScratchFramebuffer||!renderer3DSceneTexture){renderer3DDistortionEffective=0;renderer3DDistortionFallbackReason=3;return 1;}gl.bindFramebuffer(gl.FRAMEBUFFER,renderer3DDistortionFramebuffer);gl.viewport(0,0,renderer3DDistortionWidth,renderer3DDistortionHeight);gl.disable(gl.DEPTH_TEST);gl.depthMask(false);gl.disable(gl.CULL_FACE);gl.clearColor(renderer3DDistortionEffective===1?.5:0,renderer3DDistortionEffective===1?.5:0,0,0);gl.clear(gl.COLOR_BUFFER_BIT);renderer3DRenderingDistortionVectors=true;for(let submission=0;submission<renderer3DSubmissionCount;submission+=1){const object=renderer3DSubmissionObjects[submission];if(!renderer3DSubmissionIsDistortion(object))continue;if(!renderer3DDrawImmediate(0,object)){renderer3DRenderingDistortionVectors=false;return 0;}renderer3DDistortionEmitterCount+=1;renderer3DDistortionVectorDrawCount+=1;renderer3DDistortionMaximumStrength=Math.max(renderer3DDistortionMaximumStrength,Math.round(object.snapshotMaterial.distortionStrength));}for(let index=0;index<renderer3DGpuParticleFrameCount;index+=1){const system=renderer3DGpuParticleSystems.get(renderer3DGpuParticleFrameHandles[index]);if(!system||!renderer3DGpuParticleIsDistortion(system))continue;if(!renderer3DDrawGpuParticleSystem(system)){renderer3DRenderingDistortionVectors=false;return 0;}const material=renderer3DMaterials.get(system.material);renderer3DDistortionEmitterCount+=1;renderer3DDistortionVectorDrawCount+=1;renderer3DDistortionMaximumStrength=Math.max(renderer3DDistortionMaximumStrength,Math.round(material.distortionStrength));}renderer3DRenderingDistortionVectors=false;if(!renderer3DDistortionEmitterCount)return 1;renderer3DPostPass(renderer3DDistortionScratchFramebuffer,backingWidth,backingHeight,renderer3DSceneTexture,renderer3DDistortionTexture,5,0,0,0,renderer3DDistortionEffective,0);renderer3DDistortionCompositeDrawCount+=1;[renderer3DSceneTexture,renderer3DDistortionScratchTexture]=[renderer3DDistortionScratchTexture,renderer3DSceneTexture];[renderer3DSceneFramebuffer,renderer3DDistortionScratchFramebuffer]=[renderer3DDistortionScratchFramebuffer,renderer3DSceneFramebuffer];return 1;}
            function renderer3DRunPostProcessing(){if(!renderer3DHdrEffective)return 1;const gl=renderer3DGl;if(renderer3DBloomEffective){renderer3DPostPass(renderer3DBloomFramebufferA,renderer3DBloomWidth,renderer3DBloomHeight,renderer3DSceneTexture,null,0,0,0,renderer3DBloomThreshold/1000,0,0);
                    for(let cycle=0;cycle<renderer3DBloomCycles;cycle+=1){renderer3DPostPass(renderer3DBloomFramebufferB,renderer3DBloomWidth,renderer3DBloomHeight,renderer3DBloomTextureA,null,1,1/renderer3DBloomWidth,1/renderer3DBloomHeight,0,0,0);renderer3DPostPass(renderer3DBloomFramebufferA,renderer3DBloomWidth,renderer3DBloomHeight,renderer3DBloomTextureB,null,2,1/renderer3DBloomWidth,1/renderer3DBloomHeight,0,0,0);}}
                renderer3DPostPass(null,backingWidth,backingHeight,renderer3DSceneTexture,renderer3DBloomEffective?renderer3DBloomTextureA:null,3,0,0,0,renderer3DBloomEffective?renderer3DBloomIntensity/100:0,renderer3DExposure/100);gl.bindFramebuffer(gl.FRAMEBUFFER,null);return 1;}
            function renderer3DSubmissionGroup(operation,value){if(!renderer3DFrameActive){renderer3DLastError=52;return 0;}if(operation===1){if(renderer3DSubmissionGroupActive||value<0||value>renderer3DSubmissions.length-renderer3DSubmissionCount||value>renderer3DPaletteSnapshots.length-renderer3DPaletteSnapshotCount){renderer3DLastError=52;return 0;}renderer3DSubmissionGroupActive=true;renderer3DSubmissionGroupStart=renderer3DSubmissionCount;renderer3DSubmissionGroupPaletteStart=renderer3DPaletteSnapshotCount;renderer3DSubmissionGroupReserved=value;renderer3DSubmissionGroupPhysical=renderer3DSubmissionGroupLogical=0;renderer3DSubmissionGroupSerial+=1;if(renderer3DSubmissionGroupSerial>2147483647)renderer3DSubmissionGroupSerial=1;renderer3DSubmissionGroupToken=renderer3DSubmissionGroupSerial;return renderer3DSubmissionGroupToken;}if(!renderer3DSubmissionGroupActive||value!==renderer3DSubmissionGroupToken){renderer3DLastError=52;return 0;}if(operation===3){renderer3DReleaseSubmissions(renderer3DSubmissionGroupStart,renderer3DSubmissionCount);renderer3DSubmissionCount=renderer3DSubmissionGroupStart;renderer3DPaletteSnapshotCount=renderer3DSubmissionGroupPaletteStart;renderer3DSubmissionGroupActive=false;renderer3DSubmissionGroupToken=0;renderer3DSubmissionGroupReserved=renderer3DSubmissionGroupPhysical=renderer3DSubmissionGroupLogical=0;return 1;}if(operation===2){let success=1;if(!renderer3DMultipassActive)for(let index=renderer3DSubmissionGroupStart;index<renderer3DSubmissionCount;index+=1)if(!renderer3DDrawImmediate(0,renderer3DSubmissionObjects[index])){success=0;break;}if(success){renderer3DLogicalSubmissionCount+=renderer3DSubmissionGroupLogical;renderer3DPhysicalSubmissionCount+=renderer3DSubmissionGroupPhysical;}if(!renderer3DMultipassActive||!success){renderer3DReleaseSubmissions(renderer3DSubmissionGroupStart,renderer3DSubmissionCount);renderer3DSubmissionCount=renderer3DSubmissionGroupStart;renderer3DPaletteSnapshotCount=renderer3DSubmissionGroupPaletteStart;}renderer3DSubmissionGroupActive=false;renderer3DSubmissionGroupToken=0;renderer3DSubmissionGroupReserved=renderer3DSubmissionGroupPhysical=renderer3DSubmissionGroupLogical=0;return success;}renderer3DLastError=52;return 0;}
            function renderer3DEnd(){if(!renderer3DFrameActive)return 1;const gl=renderer3DGl;let success=1;if(renderer3DSubmissionGroupActive){renderer3DSubmissionGroup(3,renderer3DSubmissionGroupToken);renderer3DLastError=52;success=0;}if(success&&renderer3DMultipassActive){success=renderer3DRenderShadowPass();gl.bindFramebuffer(gl.FRAMEBUFFER,renderer3DSceneDrawTarget()||null);gl.viewport(0,0,backingWidth,backingHeight);gl.enable(gl.DEPTH_TEST);gl.depthFunc(gl.LESS);gl.depthMask(true);
                    if(success&&(renderer3DSoftDepthEffective||renderer3DDistortionRequested)){for(let submission=0;submission<renderer3DSubmissionCount;submission+=1){const object=renderer3DSubmissionObjects[submission];if(renderer3DSubmissionIsOpaque(object)&&!renderer3DDrawImmediate(0,object)){success=0;break;}}if(success)renderer3DResolveScene(true);if(success)success=renderer3DSnapshotLinearDepth();if(success)success=renderer3DRenderDistortionPass();if(success&&renderer3DMsaaTarget&&renderer3DDistortionCompositeDrawCount)renderer3DPostPass(renderer3DMsaaTarget.framebuffer,backingWidth,backingHeight,renderer3DSceneTexture,null,4,0,0,0,0,0);gl.bindFramebuffer(gl.FRAMEBUFFER,renderer3DSceneDrawTarget()||null);gl.viewport(0,0,backingWidth,backingHeight);gl.enable(gl.DEPTH_TEST);gl.depthFunc(gl.LESS);gl.depthMask(true);if(success)for(let submission=0;submission<renderer3DSubmissionCount;submission+=1){const object=renderer3DSubmissionObjects[submission];if(!renderer3DSubmissionIsOpaque(object)&&!renderer3DSubmissionIsDistortion(object)&&!renderer3DDrawImmediate(0,object)){success=0;break;}}if(success)success=renderer3DDrawQueuedGpuParticles(false);}
                    else if(success){for(let submission=0;submission<renderer3DSubmissionCount;submission+=1)if(!renderer3DDrawImmediate(0,renderer3DSubmissionObjects[submission])){success=0;break;}if(success)success=renderer3DDrawQueuedGpuParticles(true);}}
                else if(success)success=renderer3DDrawQueuedGpuParticles(true);
                if(success)renderer3DResolveScene(false);if(success&&renderer3DHdrEffective)success=renderer3DRunPostProcessing();else if(success&&renderer3DSceneFramebuffer)renderer3DPostPass(null,backingWidth,backingHeight,renderer3DSceneTexture,null,4,0,0,0,0,0);if(typeof gl.bindFramebuffer==="function")gl.bindFramebuffer(gl.FRAMEBUFFER,null);gl.depthMask(true);for(let unit=0;unit<7;unit+=1){gl.activeTexture(gl.TEXTURE0+unit);gl.bindTexture(gl.TEXTURE_2D,null);}gl.useProgram(null);gl.bindBuffer(gl.ARRAY_BUFFER,null);gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,null);for(let attribute=0;attribute<6;attribute+=1)gl.disableVertexAttribArray(attribute);gl.disable(gl.BLEND);gl.disable(gl.CULL_FACE);gl.disable(gl.POLYGON_OFFSET_FILL);renderer3DFrameActive=false;renderer3DReleaseSubmissions(0,renderer3DSubmissionCount);renderer3DSubmissionCount=renderer3DPaletteSnapshotCount=0;renderer3DSubmissionGroupActive=false;renderer3DSubmissionGroupToken=0;renderer3DSubmissionGroupReserved=renderer3DSubmissionGroupPhysical=renderer3DSubmissionGroupLogical=0;
                renderer3DReleaseGpuParticleFrameSystems();back.drawImage(renderer3DCanvas,0,0,logicalWidth,logicalHeight);return success?1:0;}
            function renderer3DReset(){renderer3DReleaseSubmissions(0,renderer3DSubmissionCount);renderer3DReleaseGpuParticleFrameSystems();renderer3DSubmissionCount=renderer3DPaletteSnapshotCount=0;renderer3DSubmissionGroupActive=false;renderer3DSubmissionGroupToken=0;renderer3DFrameActive=false;renderer3DBackdropTexture=0;renderer3DObjects.clear();renderer3DAnimators.clear();for(const model of [...renderer3DModels.keys()])renderer3DDeleteModel(model);
                renderer3DClips.clear();renderer3DSkeletons.clear();for(const handle of [...renderer3DParticleBatches.keys()])renderer3DDeleteParticleBatch(handle);for(const handle of [...renderer3DRibbonBatches.keys()])renderer3DDeleteRibbonBatch(handle);for(const [handle,system] of [...renderer3DGpuParticleSystems.entries()])renderer3DGpuParticleDelete(handle,system);for(const mesh of renderer3DMeshes.values())renderer3DDeleteGpu(mesh);
                for(const texture of renderer3DTextures.values()){renderer3DDeleteTextureGpu(texture);imageRelease(texture.image);}renderer3DMeshes.clear();
                renderer3DModels.clear();renderer3DMaterials.clear();renderer3DTextures.clear();renderer3DCamera.position=[0,300,-800];renderer3DCamera.target=[0,0,0];renderer3DCamera.up=[0,1,0];renderer3DCamera.fov=55;renderer3DCamera.near=1;renderer3DCamera.far=10000;renderer3DClearPendingCamera();renderer3DResetLights();renderer3DMaterialInspection=0;renderer3DLastError=0;
                renderer3DDrawCallCount=0;renderer3DSubmittedTriangleCount=0;renderer3DPbrDrawCount=0;renderer3DSimpleDrawCount=0;
                renderer3DPbrTriangleCount=0;if(renderer3DGl&&renderer3DPbrProgram)renderer3DGl.deleteProgram(renderer3DPbrProgram.handle);if(renderer3DGl&&renderer3DVfxProgram){renderer3DGl.deleteProgram(renderer3DVfxProgram.particle.handle);renderer3DGl.deleteProgram(renderer3DVfxProgram.ribbon.handle);}if(renderer3DGl&&renderer3DGpuParticlePipeline){renderer3DGl.deleteProgram(renderer3DGpuParticlePipeline.simulation.handle);renderer3DGl.deleteProgram(renderer3DGpuParticlePipeline.render.handle);}renderer3DGpuParticlePipeline=null;renderer3DGpuParticlePipelineAttempted=false;renderer3DGpuParticleBackendAvailable=false;if(renderer3DGl&&renderer3DParticleQuadBuffer)renderer3DGl.deleteBuffer(renderer3DParticleQuadBuffer);if(renderer3DGl&&renderer3DParticleQuadIndexBuffer)renderer3DGl.deleteBuffer(renderer3DParticleQuadIndexBuffer);renderer3DVfxProgram=null;renderer3DParticleQuadBuffer=renderer3DParticleQuadIndexBuffer=null;
                renderer3DPbrProgram=null;renderer3DPbrAttempted=false;renderer3DPbrState=0;renderer3DPbrFailure=0;
                renderer3DPbrAttemptCount=0;if(renderer3DGl&&renderer3DModelPaletteTexture)renderer3DGl.deleteTexture(renderer3DModelPaletteTexture);
                renderer3DModelPaletteTexture=null;renderer3DModelPaletteCachedAnimator=0;renderer3DModelPaletteCachedRevision=0;renderer3DModelPaletteUploadCount=0;renderer3DDeleteM5Targets();
                if(renderer3DGl&&renderer3DShadowProgram)renderer3DGl.deleteProgram(renderer3DShadowProgram.handle);if(renderer3DGl&&renderer3DPostProgram)renderer3DGl.deleteProgram(renderer3DPostProgram.handle);if(renderer3DGl&&renderer3DDepthProgram)renderer3DGl.deleteProgram(renderer3DDepthProgram.handle);renderer3DShadowProgram=renderer3DPostProgram=renderer3DDepthProgram=null;
                renderer3DPostRequested=renderer3DHdrRequested=renderer3DBloomRequested=renderer3DShadowRequested=false;renderer3DSoftDepthRequested=renderer3DDistortionRequested=false;renderer3DSoftDepthEffective=renderer3DDistortionEffective=0;renderer3DSoftDepthFallbackReason=renderer3DDistortionFallbackReason=1;renderer3DDistortionQuality=3;renderer3DFallbackFlags=0;renderer3DSubmissionCount=renderer3DLogicalSubmissionCount=renderer3DPhysicalSubmissionCount=renderer3DRejectedSubmissionCount=0;renderer3DSubmissionGroupReserved=renderer3DSubmissionGroupPhysical=renderer3DSubmissionGroupLogical=0;
                renderer3DShadowDrawCount=renderer3DShadowTriangleCount=renderer3DShadowPaletteUploadCount=renderer3DPostDrawCount=renderer3DResolveCount=renderer3DSoftDepthCopyDrawCount=renderer3DSoftDepthCopyFailureCount=renderer3DSoftParticleDrawCount=0;renderer3DDistortionVectorDrawCount=renderer3DDistortionCompositeDrawCount=renderer3DDistortionEmitterCount=renderer3DDistortionMaximumStrength=0;renderer3DRenderingDistortionVectors=false;renderer3DVfxDrawCount=renderer3DVfxTriangleCount=renderer3DVfxUploadCount=renderer3DVfxRejectedOperationCount=renderer3DVfxParticleDrawCount=renderer3DVfxRibbonDrawCount=renderer3DVfxParticleTriangleCount=renderer3DVfxRibbonTriangleCount=renderer3DVfxParticleSubmissionCount=renderer3DVfxRibbonSubmissionCount=0;renderer3DStagedParticleCapacity=renderer3DStagedRibbonCapacity=0;renderer3DGpuParticleTotalCapacity=renderer3DGpuParticleSpawnsAccepted=renderer3DGpuParticleSpawnsRejected=renderer3DGpuParticleSimulationSteps=renderer3DGpuParticleDroppedTime=renderer3DGpuParticleCpuUploadBytes=renderer3DGpuParticleQueueEntries=renderer3DGpuParticleFrameCount=renderer3DGpuParticleDispatchCount=renderer3DGpuParticleRenderDrawCount=renderer3DGpuParticleGpuStateBytes=renderer3DGpuParticleRestartCount=renderer3DGpuParticleReadbackCount=0;renderer3DM5AppliedRevision=0;renderer3DM5ConfigurationRevision+=1;if(renderer3DM5ConfigurationRevision>2147483647)renderer3DM5ConfigurationRevision=1;
                renderer3DResourceEpoch+=1;if(renderer3DResourceEpoch>2147483647)renderer3DResourceEpoch=1;return 1;}

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
                    case 8:if(!renderer3DRequireMesh(a)||renderer3DObjects.size>=1024){renderer3DLastError=9;return 0;}const handle=renderer3DHandle();renderer3DObjects.set(handle,{mesh:a,material:0,defaultMaterial:0,animator:0,position:[0,0,0],rotation:[0,0,0],scale:[1,1,1],color:[1,1,1,1],visible:true,castsShadow:true,receivesShadow:true});return handle;
                    case 9:if(renderer3DObjects.delete(a))return 1;if(renderer3DModels.has(a)){if(!renderer3DDeleteModel(a)){renderer3DLastError=27;return 0;}return 1;}if(renderer3DAnimators.has(a)){if(renderer3DAnimatorReferences(a)!==0){renderer3DLastError=37;return 0;}renderer3DAnimators.delete(a);return 1;}if(renderer3DClips.has(a)){if(renderer3DClipReferences(a)!==0){renderer3DLastError=37;return 0;}renderer3DClips.delete(a);return 1;}if(renderer3DSkeletons.has(a)){if(renderer3DSkeletonReferences(a)!==0){renderer3DLastError=37;return 0;}renderer3DSkeletons.delete(a);return 1;}mesh=renderer3DMeshes.get(a);if(mesh){if(renderer3DMeshReferenceCount(a)!==0){renderer3DLastError=16;return 0;}renderer3DDeleteGpu(mesh);renderer3DMeshes.delete(a);return 1;}material=renderer3DMaterials.get(a);if(material){if(material.ownerModel||renderer3DMaterialReferenceCount(a)!==0){renderer3DLastError=22;return 0;}renderer3DMaterials.delete(a);return 1;}texture=renderer3DTextures.get(a);if(texture){if(renderer3DTextureReferenceCount(a)!==0){renderer3DLastError=23;return 0;}renderer3DDeleteTextureGpu(texture);imageRelease(texture.image);renderer3DTextures.delete(a);return 1;}renderer3DLastError=5;return 0;
                    case 10:if(renderer3DFrameActive){renderer3DClearPendingCamera();renderer3DLastError=renderer3DCameraErrorFrameActive;return 0;}if(![a,b,c,d,e,f].every(renderer3DCameraWorldValue)){renderer3DClearPendingCamera();renderer3DLastError=renderer3DCameraErrorInvalidPositionTarget;return 0;}if(a===d&&b===e&&c===f){renderer3DClearPendingCamera();renderer3DLastError=renderer3DCameraErrorZeroViewDirection;return 0;}if(g<10||g>160||h<=0||i<=h||i>2000000){renderer3DClearPendingCamera();renderer3DLastError=renderer3DCameraErrorInvalidProjection;return 0;}renderer3DPendingCamera.position=[a,b,c];renderer3DPendingCamera.target=[d,e,f];renderer3DPendingCamera.fov=g;renderer3DPendingCamera.near=h;renderer3DPendingCamera.far=i;renderer3DPendingCamera.hasProjection=true;return 1;
                    case 123:if(renderer3DFrameActive){renderer3DClearPendingCamera();renderer3DLastError=renderer3DCameraErrorFrameActive;return 0;}if(![a,b,c].every(renderer3DCameraWorldValue)||(a===0&&b===0&&c===0)){renderer3DClearPendingCamera();renderer3DLastError=renderer3DCameraErrorInvalidUp;return 0;}renderer3DPendingCamera.up=[a,b,c];renderer3DPendingCamera.hasUp=true;return 1;
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
                    case 25:return 1024;
                    case 26:return renderer3DMeshes.has(a)?1:0;
                    case 27:return renderer3DObjects.has(a)?1:0;
                    case 28:return renderer3DMeshReferenceCount(a);
                    case 29:return renderer3DCreateMaterial(a,b,c,d,e,f,g,h,i);
                    case 30:object=renderer3DRequireObject(a);if(!object||(b!==0&&!renderer3DMaterials.has(b))){renderer3DLastError=5;return 0;}object.material=b===0?object.defaultMaterial:b;return 1;
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
                    case 49:model=renderer3DModels.get(a);if(!model||b<0||b>=model.parts.length||renderer3DObjects.size>=1024){renderer3DLastError=9;return 0;}const partHandle=renderer3DHandle(),materialSlot=model.materials[b],defaultMaterial=model.pbrReady?model.pbrMaterials[materialSlot]:0;renderer3DObjects.set(partHandle,{mesh:model.parts[b],material:defaultMaterial,defaultMaterial,animator:0,position:[0,0,0],rotation:[0,0,0],scale:[1,1,1],color:[1,1,1,1],visible:true,castsShadow:true,receivesShadow:true});return partHandle;
                    case 50:model=renderer3DModels.get(a);if(!model||b<0||b>=model.parts.length){renderer3DLastError=5;return -1;}return model.materials[b];
                    case 51:mesh=renderer3DRequireMesh(a);return mesh&&renderer3DSetSkin(mesh,b,c,d,e,f,g,h,i,j)?1:0;
                    case 52:return renderer3DCreateSkeleton(a);
                    case 53:skeleton=renderer3DSkeletons.get(a);if(!skeleton||b<0||b>=skeleton.boneCount||c< -1||c>=b){renderer3DLastError=30;return 0;}skeleton.parents[b]=c;skeleton.bind[b]=[d,e,f];skeleton.committed=false;return 1;
                    case 54:skeleton=renderer3DSkeletons.get(a);return skeleton&&renderer3DCommitSkeleton(skeleton)?1:0;
                    case 55:return renderer3DCreateClip(a,b);
                    case 56:clip=renderer3DClips.get(a);skeleton=clip&&renderer3DSkeletons.get(clip.skeleton);if(!clip||!skeleton||b<0||b>=skeleton.boneCount){renderer3DLastError=31;return 0;}clip.translation[b]=[[c,d,e],[f,g,h]];return 1;
                    case 57:clip=renderer3DClips.get(a);skeleton=clip&&renderer3DSkeletons.get(clip.skeleton);if(!clip||!skeleton||b<0||b>=skeleton.boneCount){renderer3DLastError=31;return 0;}clip.rotation[b]=[[c/1000,d/1000,e/1000,f/1000],[g/1000,h/1000,i/1000,j/1000]];return 1;
                    case 58:clip=renderer3DClips.get(a);skeleton=clip&&renderer3DSkeletons.get(clip.skeleton);if(!clip||!skeleton||b<0||b>=skeleton.boneCount||[c,d,e,f,g,h].some(value=>value<=0)){renderer3DLastError=31;return 0;}clip.scale[b]=[[c/100,d/100,e/100],[f/100,g/100,h/100]];renderer3DUpdateClipScaleSafety(clip);return 1;
                    case 59:clip=renderer3DClips.get(a);if(!clip||b<=0||b>clip.duration||c<=0||clip.events.length>=16||(clip.events.length&&b<clip.events[clip.events.length-1].time)){renderer3DLastError=31;return 0;}clip.events.push({time:b,id:c});return 1;
                    case 60:return renderer3DCreateAnimator(a);
                    case 61:animator=renderer3DAnimators.get(a);clip=renderer3DClips.get(b);if(!animator||!clip||clip.skeleton!==animator.skeleton||d<=0||d>1000){renderer3DLastError=35;return 0;}animator.clip=b;animator.loop=c!==0;animator.complete=false;animator.time=0;animator.previous=0;animator.speed=d;animator.pending=0;renderer3DUpdatePose(animator);return 1;
                    case 62:animator=renderer3DAnimators.get(a);return (animator&&animator.production?renderer3DUpdateModelAnimator(animator,b):renderer3DUpdateAnimator(animator,b))?1:0;
                    case 63:animator=renderer3DAnimators.get(a);return animator&&animator.complete?1:0;
                    case 64:animator=renderer3DAnimators.get(a);return animator?animator.time:0;
                    case 65:animator=renderer3DAnimators.get(a);if(!animator)return 0;if(animator.production)return renderer3DTakeModelEvent(animator);const eventValue=animator.pending;animator.pending=0;return eventValue;
                    case 66:object=renderer3DObjects.get(a);animator=b===0?null:renderer3DAnimators.get(b);mesh=object&&renderer3DMeshes.get(object.mesh);skeleton=animator&&!animator.production&&renderer3DSkeletons.get(animator.skeleton);if(!object||!mesh||(b!==0&&(!animator||(animator.production?!renderer3DModelOwnsMesh(animator.model,object.mesh):(!skeleton||mesh.maxJoint>=skeleton.boneCount))))){renderer3DLastError=36;return 0;}object.animator=b;return 1;
                    case 67:return renderer3DSkeletons.size;
                    case 68:return renderer3DClips.size;
                    case 69:return renderer3DAnimators.size;
                    case 70:return 32;
                    case 71:return renderer3DSkeletons.has(a)?1:0;
                    case 72:return renderer3DClips.has(a)?1:0;
                    case 73:return renderer3DAnimators.has(a)?1:0;
                    case 74:animator=renderer3DAnimators.get(a);if(!animator)return 0;if(animator.production){animator.clipIndex=animator.destinationClip=-1;animator.mode=animator.destinationMode=0;animator.time=animator.previous=animator.destinationTime=animator.timeRemainder=animator.destinationTimeRemainder=0;animator.fadeElapsed=animator.fadeDuration=0;animator.complete=animator.destinationComplete=false;renderer3DClearModelEvents(animator);animator.rootDelta.fill(0);renderer3DUpdateModelPose(animator);return 1;}animator.clip=0;animator.time=0;animator.previous=0;animator.complete=false;animator.pending=0;renderer3DUpdatePose(animator);return 1;
                    case 75:return 64;
                    case 76:return 128;
                    case 77:return 128;
                    case 78:return renderer3DDrawCallCount;
                    case 79:return renderer3DSubmittedTriangleCount;
                    case 80:return renderer3DModelStaticValue(renderer3DModels.get(a),b,c,d);
                    case 81:if(!renderer3DInitialize()||!renderer3DPbrProgram){renderer3DLastError=44;return 0;}return renderer3DCreatePbrMaterial(a,b,c,d,e,f);
                    case 82:material=renderer3DMaterials.get(a);return renderer3DSetPbrFactors(material,b,c,d,e,f,g,h,i,j)?1:0;
                    case 83:material=renderer3DMaterials.get(a);return renderer3DSetPbrEmissive(material,b,c,d)?1:0;
                    case 84:material=renderer3DMaterials.get(a);return renderer3DSetPbrTextures(material,b,c,d,e,f,g)?1:0;
                    case 85:return renderer3DResetLights()?1:0;
                    case 86:return renderer3DSetAmbient(a,b,c,d)?1:0;
                    case 87:return renderer3DSetDirectional(a,b,c,d,e,f,g)?1:0;
                    case 88:return renderer3DSetLocalLight(a,b,c,d,e,f,g,h,i,j)?1:0;
                    case 89:return renderer3DSetSpotCone(a,b,c,d,e,f)?1:0;
                    case 90:return renderer3DPbrTextureValue(renderer3DTextures.get(a),b);
                    case 91:return renderer3DPbrMaterialValue(renderer3DMaterials.get(a),b);
                    case 92:return renderer3DLightValue(a,b,c);
                    case 93:return renderer3DPbrDrawCount;
                    case 94:return renderer3DSimpleDrawCount;
                    case 95:return renderer3DPbrTriangleCount;
                    case 96:if(a===0){renderer3DInitialize();return renderer3DPbrProgram?1:0;}if(a===1)return renderer3DPbrState;
                        if(a===2)return renderer3DPbrFailure;if(a===3)return renderer3DPbrAttemptCount;renderer3DLastError=5;return 0;
                    case 97:return renderer3DModelPbrValue(renderer3DModels.get(a),b,c);
                    case 98:return renderer3DModelAnimationValue(renderer3DModels.get(a),b,c);
                    case 99:return renderer3DCreateModelAnimator(a);
                    case 100:animator=renderer3DAnimators.get(a);return renderer3DPlayModelAnimator(animator,b,c,d)?1:0;
                    case 101:animator=renderer3DAnimators.get(a);return renderer3DCrossFadeModelAnimator(animator,b,c,d)?1:0;
                    case 102:animator=renderer3DAnimators.get(a);return animator&&animator.production?animator.clipIndex:-1;
                    case 103:animator=renderer3DAnimators.get(a);return animator&&animator.production&&animator.destinationClip>=0?Math.trunc(animator.fadeElapsed*100/animator.fadeDuration):0;
                    case 104:animator=renderer3DAnimators.get(a);return animator&&animator.production?animator.eventCount:0;
                    case 105:animator=renderer3DAnimators.get(a);if(!animator||!animator.production||b<0||b>1){renderer3DLastError=48;return 0;}animator.rootMode=b;animator.rootDelta.fill(0);renderer3DUpdateModelPose(animator);return 1;
                    case 106:animator=renderer3DAnimators.get(a);if(!animator||!animator.production||b<1||b>4){renderer3DLastError=48;return 0;}const rootValue=Math.round(animator.rootDelta[b-1]*1000);if(b===4)animator.rootDelta.fill(0);return rootValue;
                    case 107:return renderer3DModelSocketValue(renderer3DAnimators.get(a),b,c,d,e);
                    case 108:return renderer3DInitialize()?1:0;
                    case 109:return renderer3DModelPaletteUploadCount;
                    case 110:return renderer3DAnimatorProductionValue(renderer3DAnimators.get(a),b);
                    case 111:animator=renderer3DAnimators.get(a);if(!animator||!animator.production){renderer3DLastError=48;return 0;}renderer3DClearModelEvents(animator);return 1;
                    case 112:if(a===1)return renderer3DResourceEpoch;if(a===2)return renderer3DFrameActive?1:0;renderer3DLastError=5;return 0;
                    case 113:if(renderer3DFrameActive||a<0||a>1||b<0||b>1||c<0||c>1||d<25||d>400||e<500||e>8000||f<0||f>400||(g!==2&&g!==4)||h<0||h>2||(i!==1&&i!==2&&i!==4)){renderer3DLastError=50;return 0;}
                        if(renderer3DPostRequested===(a!==0)&&renderer3DHdrRequested===(b!==0)&&renderer3DBloomRequested===(c!==0)&&renderer3DExposure===d&&renderer3DBloomThreshold===e&&renderer3DBloomIntensity===f&&renderer3DBloomDownsample===g&&renderer3DBloomCycles===h&&renderer3DRequestedSamples===i)return 1;
                        renderer3DPostRequested=a!==0;renderer3DHdrRequested=b!==0;renderer3DBloomRequested=c!==0;renderer3DExposure=d;renderer3DBloomThreshold=e;renderer3DBloomIntensity=f;renderer3DBloomDownsample=g;renderer3DBloomCycles=h;renderer3DRequestedSamples=i;renderer3DM5ConfigurationRevision+=1;if(renderer3DM5ConfigurationRevision>2147483647)renderer3DM5ConfigurationRevision=1;return 1;
                    case 114:if(renderer3DFrameActive||a<0||a>1||b<0||b>2||c<0||c>=4||(d!==1024&&d!==2048)||e<0||e>1000||f<0||f>1000||(a!==0&&b===0)){renderer3DLastError=50;return 0;}
                        if(renderer3DShadowRequested===(a!==0)&&renderer3DShadowCaster===b&&renderer3DShadowSlot===c&&renderer3DShadowRequestedResolution===d&&Math.round(renderer3DShadowSettings[0]*1000000)===e&&Math.round(renderer3DShadowSettings[1]*100000)===f)return 1;
                        renderer3DShadowRequested=a!==0;renderer3DShadowCaster=b;renderer3DShadowSlot=c;renderer3DShadowRequestedResolution=d;renderer3DShadowSettings[0]=e/1000000;renderer3DShadowSettings[1]=f/100000;renderer3DM5ConfigurationRevision+=1;if(renderer3DM5ConfigurationRevision>2147483647)renderer3DM5ConfigurationRevision=1;return 1;
                    case 115:if(renderer3DFrameActive||a< -1000000||a>1000000||b< -1000000||b>1000000||c< -1000000||c>1000000||d<=0||d>2000000||e<=0||e>2000000||f<=0||g<=f||g>2000000){renderer3DLastError=50;return 0;}
                        if(renderer3DShadowCenter[0]===a&&renderer3DShadowCenter[1]===b&&renderer3DShadowCenter[2]===c&&renderer3DShadowArea[0]===d&&renderer3DShadowArea[1]===e&&renderer3DShadowArea[2]===f&&renderer3DShadowArea[3]===g)return 1;
                        renderer3DShadowCenter.set([a,b,c]);renderer3DShadowArea.set([d,e,f,g]);renderer3DM5ConfigurationRevision+=1;if(renderer3DM5ConfigurationRevision>2147483647)renderer3DM5ConfigurationRevision=1;return 1;
                    case 116:object=renderer3DObjects.get(a);if(!object||b<0||b>1||c<0||c>1){renderer3DLastError=50;return 0;}object.castsShadow=b!==0;object.receivesShadow=c!==0;return 1;
                    case 117:if(a===1)return renderer3DLogicalSubmissionCount;if(a===2)return renderer3DSubmissions.length;if(a===3)return renderer3DMultipassActive?1:0;if(a===4)return renderer3DShadowRequested?1:0;
                        if(a===5)return renderer3DShadowEffective?1:0;if(a===6)return renderer3DShadowResolution;if(a===7)return renderer3DShadowDrawCount;if(a===8)return renderer3DShadowTriangleCount;if(a===9)return renderer3DShadowPaletteUploadCount;
                        if(a===10)return renderer3DHdrRequested?1:0;if(a===11)return renderer3DHdrEffective?1:0;if(a===12)return renderer3DHdrEffective?1:0;if(a===13)return renderer3DEffectiveSamples;if(a===14)return renderer3DM5Width;if(a===15)return renderer3DM5Height;if(a===16)return renderer3DResolveCount;
                        if(a===17)return renderer3DBloomRequested?1:0;if(a===18)return renderer3DBloomEffective?1:0;if(a===19)return renderer3DBloomWidth;if(a===20)return renderer3DBloomHeight;if(a===21)return renderer3DBloomEffective?renderer3DBloomCycles:0;if(a===22)return renderer3DPostDrawCount;
                        if(a===23)return renderer3DToneMappingEffective?1:0;if(a===24)return renderer3DExposure;if(a===25)return renderer3DFallbackFlags;if(a===26)return renderer3DM5ResourceGeneration;if(a===27)return renderer3DTargetBytes;if(a===28)return renderer3DShadowCaster;if(a===29)return renderer3DShadowSlot;
                        if(a===30)return Math.round(renderer3DShadowSettings[0]*1000000);if(a===31)return Math.round(renderer3DShadowSettings[1]*100000);if(a===32)return renderer3DPostRequested?1:0;if(a===33)return renderer3DPostEffective?1:0;if(a===34)return renderer3DRejectedSubmissionCount;if(a===35)return renderer3DShadowBytes;if(a===36)return renderer3DSceneBytes;if(a===37)return renderer3DBloomBytes;
                        if(a===42)return renderer3DPhysicalSubmissionCount;if(a===43)return renderer3DSubmissionGroupPhysical;if(a===44)return renderer3DSubmissionGroupReserved;if(a===45)return renderer3DPaletteSnapshotCount;if(a===46){let count=0;for(const mesh of renderer3DMeshes.values())count+=mesh.inFlight;return count;}if(a===47){let count=0;for(const texture of renderer3DTextures.values())count+=texture.inFlight;return count;}if(a===48)return renderer3DSubmissionCount*512+renderer3DPaletteSnapshotCount*8208;if(a===49)return renderer3DPaletteSnapshots.length;if(a===50)return renderer3DSubmissionGroupActive?1:0;if(a===51)return renderer3DSubmissionGroupLogical;
                        if(a>=60&&a<=69){if(b<0||b>=renderer3DSubmissionCount){renderer3DLastError=50;return 0;}const submission=renderer3DSubmissionObjects[b];if(a===60)return submission.source;if(a===61)return submission.mesh;if(a===62)return Math.round(submission.position[0]*1000);if(a===63)return Math.round(submission.color[0]*1000);if(a===64)return Math.round(submission.color[3]*1000);if(a===65)return submission.hasMaterial?submission.snapshotMaterial.kind:-1;if(a===66)return submission.snapshotMaterial.doubleSided?1:0;if(a===67)return submission.paletteIndex+1;if(a===68)return submission.paletteIndex<0?0:renderer3DPaletteSnapshots[submission.paletteIndex].revision;if(a===69)return submission.castsShadow?1:0;}
                        if(a===40||a===41){object=renderer3DObjects.get(b);if(!object){renderer3DLastError=50;return 0;}return a===40?(object.castsShadow?1:0):(object.receivesShadow?1:0);}renderer3DLastError=50;return 0;
                    case 118:return renderer3DSubmissionGroup(a,b);
                    case 119:return renderer3DParticleBatchCommand(a,b,c,d,e,f,g,h,i);
                    case 120:return renderer3DRibbonBatchCommand(a,b,c,d,e,f,g,h,i,j);
                    case 121:return renderer3DM6Value(a,b);
                    case 122:if(a===-1)return renderer3DMaterialInspection;if(renderer3DFrameActive||a<0||a>6){renderer3DLastError=5;return 0;}renderer3DMaterialInspection=a;return 1;
                    case 124:animator=renderer3DAnimators.get(a);return renderer3DSetModelAnimatorTime(animator,b)?1:0;
                    case 125:return renderer3DSoftDepthCommand(a,b,c,d);
                    case 126:return renderer3DDistortionCommand(a,b,c,d,e,f,g);
                    // Shared thermal fire uses WebGL2 transform feedback when available
                    // and the same complete deterministic CPU path when it is not.
                    case 127:return renderer3DGpuParticleCommand(a,b,c,d,e,f,g,h,i,j);
                    case 128:
                        animator=renderer3DAnimators.get(a);model=animator&&animator.production?renderer3DModels.get(animator.model):null;
                        if(!model||b<0||b>=model.animation.nodes.length||c< -360||c>360||d< -360||d>360||e< -360||e>360){renderer3DLastError=48;return 0;}
                        animator.nodeRotationOffsets[b*3]=c;animator.nodeRotationOffsets[b*3+1]=d;animator.nodeRotationOffsets[b*3+2]=e;renderer3DUpdateModelPose(animator);return 1;
                    case 129:
                        object=renderer3DObjects.get(a);const pivotBound=h===1?1000000000:1000000;
                        if(!object||h<0||h>1||Math.abs(b)>pivotBound||Math.abs(c)>pivotBound||Math.abs(d)>pivotBound||Math.abs(e)>360||Math.abs(f)>360||Math.abs(g)>360){renderer3DLastError=5;return 0;}
                        if(!object.pivotPosition){object.pivotPosition=new Float32Array(3);object.pivotRotation=new Float32Array(3);}
                        object.pivotPosition[0]=h===1?b/1000:b;object.pivotPosition[1]=h===1?c/1000:c;object.pivotPosition[2]=h===1?d/1000:d;
                        object.pivotRotation[0]=e;object.pivotRotation[1]=f;object.pivotRotation[2]=g;return 1;
                    case 130:object=renderer3DObjects.get(a);if(!object||b<0||b>3){renderer3DLastError=5;return 0;}object.cullMode=b;return 1;
                    case 131:return renderer3DSetBackdropTexture(a);
                    case 132:object=renderer3DObjects.get(a);if(!object||b<0||b>1){renderer3DLastError=5;return 0;}object.ignoreNodeOffsets=b===0;return 1;
                    default:renderer3DLastError=1;return 0;
                }
            }

            function renderer3DImage(command,image,a,b,c,d,e,f,g,h) {
                [command,a,b,c,d,e,f,g,h]=[command,a,b,c,d,e,f,g,h].map(safe);
                if(command===1)return renderer3DCreateTexture(image,a,b);
                if(command===2)return renderer3DCreatePbrTexture(image,a,b,c,d);
                imageRelease(image);renderer3DLastError=1;return 0;
            }

            async function renderer3DText(command,value,a,b,c,d,e,f,g,h) {
                [command,a,b,c,d,e,f,g,h]=[command,a,b,c,d,e,f,g,h].map(safe);
                if(command===1)return renderer3DLoadModel(String(value),true);
                if(command===2)return renderer3DLoadModel(String(value),false);
                if(command===3)return renderer3DPrepareModelPbr(a,b,c,d);
                const name=String(value);
                if(command===4){const model=renderer3DModels.get(a),index=model&&model.animation?model.animation.clips.findIndex(clip=>clip.name===name):-1;if(index<0)renderer3DLastError=48;return index;}
                if(command===5){const model=renderer3DModels.get(a),index=model&&model.animation?model.animation.sockets.findIndex(socket=>socket.name===name):-1;if(index<0)renderer3DLastError=48;return index;}
                if(command===6){const model=renderer3DModels.get(a);return model&&model.animation&&b>0&&b<=model.animation.events.length&&model.animation.events[b-1].name===name?1:0;}
                if(command===7){const animator=renderer3DAnimators.get(a),model=animator&&animator.production?renderer3DModels.get(animator.model):null,index=model&&model.animation?model.animation.clips.findIndex(clip=>clip.name===name):-1;return renderer3DPlayModelAnimator(animator,index,b,c)?1:0;}
                if(command===8){const animator=renderer3DAnimators.get(a),model=animator&&animator.production?renderer3DModels.get(animator.model):null,index=model&&model.animation?model.animation.clips.findIndex(clip=>clip.name===name):-1;return renderer3DCrossFadeModelAnimator(animator,index,b,c)?1:0;}
                if(command===9)return renderer3DTakeModelEvent(renderer3DAnimators.get(a),name);
                renderer3DLastError=1;return 0;
            }

            function renderer3DTextValue(command,a,b,c,d,e,f,g,h,i) {
                [command,a,b,c,d,e,f,g,h,i]=[command,a,b,c,d,e,f,g,h,i].map(safe);
                const model=renderer3DModels.get(a),animation=model&&model.animation;
                if(!animation){renderer3DLastError=48;return "";}
                if(command===10&&b>=0&&b<animation.clips.length)return animation.clips[b].name;
                if(command===11&&b>=0&&b<animation.sockets.length)return animation.sockets[b].name;
                if(command===12&&b>0&&b<=animation.events.length)return animation.events[b-1].name;
                renderer3DLastError=command>=10&&command<=12?48:1;return "";
            }

            function clear(fillColor) {
                back.fillStyle = color(fillColor);
                back.fillRect(0, 0, logicalWidth, logicalHeight);
            }

            function fillRectangle(x, y, width, height, fillColor) {
                back.fillStyle = color(fillColor);
                back.fillRect(safe(x), safe(y), safe(width), safe(height));
            }

            function fillRectangleOpacity(x, y, width, height, fillColor, opacity) {
                back.save();
                back.globalAlpha = Math.max(0, Math.min(100, safe(opacity))) / 100;
                back.fillStyle = color(fillColor);
                back.fillRect(safe(x), safe(y), safe(width), safe(height));
                back.restore();
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
                    startupAsset(logical, "loading");
                    imageDecodeCount += 1;
                    entry.promise = (async () => {
                        const bytes = await fetchAssetBytes(logical, { cache: "no-store" }, true);
                        startupAsset(logical, "loading");
                        const url = URL.createObjectURL(new Blob([bytes]));
                        try { return await new Promise((resolve, reject) => {
                        const resource = new Image();
                        resource.onload = () => {
                            entry.resource = resource;
                            entry.width = safe(resource.naturalWidth || resource.width);
                            entry.height = safe(resource.naturalHeight || resource.height);
                            if (entry.width <= 0 || entry.height <= 0) reject(new Error(`Load Image decoded invalid dimensions: ${logical}`));
                            else { startupAsset(logical, "ready"); resolve(entry); }
                        };
                        resource.onerror = () => reject(new Error(`Load Image failed: ${logical}`));
                        resource.src = url;
                        }); } finally { URL.revokeObjectURL(url); }
                    })().catch(error => {
                        forgetAssetDownload(logical);
                        startupAsset(logical, "failed");
                        if (entry.refs === 0) imageCache.delete(logical);
                        throw error;
                    });
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
                const switchToConsole = consoleOutput.hidden;
                canvas.hidden = true;
                consoleOutput.hidden = false;
                consoleText += items.map(item => String(item)).join("");
                if (!suppressNewLine) consoleText += "\n";
                consoleOutput.textContent = consoleText;
                consoleOutput.scrollTop = consoleOutput.scrollHeight;
                finishStartupLoading();
                if (switchToConsole && (!document.activeElement ||
                    document.activeElement === document.body || document.activeElement === canvas))
                    consoleOutput.focus({ preventScroll: true });
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
                finishStartupLoading();
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
                    case "KeyO": return 27;
                    case "KeyF": return 28;
                    case "KeyG": return 29;
                    case "KeyR": return 30;
                    case "KeyP": return 31;
                    case "KeyB": return 32;
                    case "KeyX": return 35;
                    case "KeyY": return 36;
                    case "KeyZ": return 37;
                    case "KeyE": return 38;
                    case "ControlLeft": return 33;
                    case "ControlRight": return 33;
                    case "Backquote": return 34;
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
                    event.code === "Escape" || event.code === "Tab" || event.code.startsWith("Control") ||
                    event.code === "Backquote" || /^Key[WASDOFGRPBXYZE]$/.test(event.code);
            }

            async function toggleFullScreen() {
                try {
                    if (document.fullscreenElement) await document.exitFullscreen();
                    else await shell.requestFullscreen();
                } catch (_) { }
            }

            function updateFullScreenControl() {
                if (!fullScreenButton) return;
                fullScreenButton.hidden = typeof shell.requestFullscreen !== "function" ||
                    document.fullscreenEnabled === false;
                const isFullScreen = document.fullscreenElement === shell;
                fullScreenButton.textContent = isFullScreen ? "Exit Full Screen" : "Full Screen";
                fullScreenButton.setAttribute("aria-pressed", String(isFullScreen));
            }

            if (fullScreenButton) fullScreenButton.addEventListener("click", () => { void toggleFullScreen(); });
            updateFullScreenControl();

            window.addEventListener("keydown", event => {
                // Only the focused program surface owns keys. Do not record browser
                // accelerators or input in another control as held game actions.
                const surface = canvas.hidden ? consoleOutput : canvas;
                if (closed || surface.hidden || !active || document.hidden ||
                    document.activeElement !== surface ||
                    (event.target && event.target !== surface && event.target !== document.body) ||
                    event.isComposing || event.metaKey) return;
                if (event.altKey) {
                    if (event.code === "Enter" && !event.ctrlKey && !event.repeat) {
                        event.preventDefault();
                        void toggleFullScreen();
                    }
                    return;
                }
                const controlKey = event.code === "ControlLeft" || event.code === "ControlRight";
                const frameNavigation = event.ctrlKey &&
                    (event.code === "ArrowLeft" || event.code === "ArrowRight");
                if (event.ctrlKey && !controlKey && !frameNavigation) return;
                if (/^(Shift|Meta)(Left|Right)$/.test(event.code) || /^F\d{1,2}$/.test(event.code)) return;
                // Shift+Tab remains the keyboard route out of the canvas.
                if (event.shiftKey && event.code === "Tab") return;
                userInteracted = true;
                const key = mapKey(event);
                const newlyPressed = pressInput(`keyboard:${event.code}`, key, false);
                syncMusic();
                if (!controlKey && controlledKey(event)) event.preventDefault();
                if (newlyPressed && !event.repeat && !controlKey) enqueueKey(key);
            });

            window.addEventListener("keyup", event => { releaseInput(`keyboard:${event.code}`); });

            canvas.addEventListener("click", () => { userInteracted = true; canvas.focus(); syncMusic(); });
            canvas.addEventListener("blur", () => { keys.length = 0; keyEventHeldKeys.clear(); releaseInputsByPrefix("keyboard:"); });
            consoleOutput.addEventListener("click", () => { consoleOutput.focus(); });
            consoleOutput.addEventListener("blur", () => { keys.length = 0; keyEventHeldKeys.clear(); releaseInputsByPrefix("keyboard:"); });
            canvas.addEventListener("contextmenu", event => event.preventDefault());
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
            document.addEventListener("fullscreenchange", () => { resizeCanvas(); updateFullScreenControl(); });
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

            function getKey() {
                const event = keys.shift();
                keyEventHeldKeys = event ? event.held : new Set();
                return event ? event.key : 0;
            }
            function keyHeld(key) { return (heldKeyCounts.get(safe(key)) || 0) > 0 ? 1 : 0; }
            function keyEventHeld(key) { return key !== 19 && keyEventHeldKeys.has(safe(key)) ? 1 : 0; }

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
                    const bytes = await fetchAssetBytes(logical);
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

            const FILE_TRANSFER_MAX_BYTES = 8 * 1024 * 1024;
            const fileTransferUrls = new Set();
            let cancelFileImport = null;

            function fileExport(fileName, contents) {
                fileName = String(fileName);
                contents = String(contents);
                let link = null, url = null;
                try {
                    const bytes = utf8(contents);
                    if (!fileName || utf8(fileName).length > 200 || /[\\/:*?"<>|\x00-\x1f]/.test(fileName) ||
                        /[. ]$/.test(fileName) || bytes.length > FILE_TRANSFER_MAX_BYTES ||
                        (navigator.userActivation && !navigator.userActivation.isActive)) return false;
                    url = URL.createObjectURL(new Blob([bytes], { type: "text/plain;charset=utf-8" }));
                    fileTransferUrls.add(url);
                    link = document.createElement("a");
                    link.href = url;
                    link.download = fileName;
                    link.hidden = true;
                    document.body.appendChild(link);
                    link.click();
                    setTimeout(() => { URL.revokeObjectURL(url); fileTransferUrls.delete(url); }, 1000);
                    return true; // Download initiated, not proof the user saved it to disk.
                } catch (_) {
                    if (url) { URL.revokeObjectURL(url); fileTransferUrls.delete(url); }
                    return false;
                } finally { if (link) link.remove(); }
            }

            async function fileImport() {
                if (cancelFileImport || (navigator.userActivation && !navigator.userActivation.isActive)) return "";
                return new Promise(resolve => {
                    const input = document.createElement("input");
                    input.type = "file";
                    input.accept = "application/json,text/*,.smile";
                    input.hidden = true;
                    let finished = false;
                    const finish = value => {
                        if (finished) return;
                        finished = true;
                        input.removeEventListener("change", change);
                        input.removeEventListener("cancel", cancel);
                        input.remove();
                        cancelFileImport = null;
                        resolve(value);
                    };
                    const cancel = () => finish("");
                    const change = async () => {
                        try {
                            const file = input.files && input.files[0];
                            if (!file || file.size > FILE_TRANSFER_MAX_BYTES) return finish("");
                            const bytes = await file.arrayBuffer();
                            if (bytes.byteLength > FILE_TRANSFER_MAX_BYTES) return finish("");
                            finish(new TextDecoder("utf-8", { fatal: true }).decode(bytes));
                        } catch (_) { finish(""); }
                    };
                    cancelFileImport = cancel;
                    input.addEventListener("change", change);
                    input.addEventListener("cancel", cancel);
                    try { document.body.appendChild(input); input.click(); }
                    catch (_) { finish(""); }
                });
            }

            async function loadTextFile(path, target) {
                if (!target || !Array.isArray(target.data)) throw new Error("Load Text File requires a one-dimensional array.");
                target.data.fill(0);
                try {
                    const bytes = new Uint8Array(await fetchAssetBytes(path, { cache: "no-store" }));
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

            // Values match the shared DATA_STATUS_* constants. Checked calls never abort the scene.
            const DATA_OK = 0, DATA_MISSING = 1, DATA_RECOVERED = 2, DATA_INVALID = 3,
                DATA_UNAVAILABLE = 4, DATA_CORRUPT = 5, DATA_TOO_LARGE = 6;

            function dataBufferValid(target) {
                return target && Array.isArray(target.data) && target.dimensions?.length === 1 &&
                    target.data.length <= 1024 * 1024;
            }

            function dataDecode(text) {
                // Bound untrusted base64 before allocating/decoding it.
                if (typeof text !== "string" || text.length > Math.ceil((1024 * 1024 + 44) / 3) * 4)
                    throw new Error("Persistent-data envelope is oversized.");
                return dataPayload(new Uint8Array(decodeBytes(text)));
            }

            function saveDataCore(target, count, key, recover) {
                if (!dataBufferValid(target) || !Number.isSafeInteger(count) ||
                    count < 0 || count > target.data.length || count > 1024 * 1024) return DATA_INVALID;
                const bytes = new Uint8Array(count);
                for (let index = 0; index < count; index += 1) {
                    const value = target.data[index];
                    if (!Number.isSafeInteger(value) || value < 0 || value > 255) return DATA_INVALID;
                    bytes[index] = value;
                }
                try {
                    const fullKey = dataStorageKey(key);
                    const text = encodeBytes(dataEnvelope(bytes));
                    const previous = localStorage.getItem(fullKey);
                    if (previous !== null) {
                        let valid = true;
                        try { dataDecode(previous); } catch (_) { valid = false; }
                        if (valid) {
                            // A failed quota/write leaves the primary (and memory) unchanged.
                            localStorage.setItem(fullKey + ".bak", previous);
                        } else {
                            if (!recover) return DATA_CORRUPT;
                            const backup = localStorage.getItem(fullKey + ".bak");
                            try { dataDecode(backup); } catch (_) { return DATA_CORRUPT; }
                            // Do not rotate a corrupt primary over the verified last-good backup.
                        }
                    }
                    localStorage.setItem(fullKey, text);
                    memoryStorage.set(fullKey, text);
                    return DATA_OK;
                } catch (_) { return DATA_UNAVAILABLE; }
            }

            function saveDataChecked(target, count, key) { return saveDataCore(target, count, key, true); }

            function saveData(target, count, key) {
                if (saveDataCore(target, count, key, false) !== DATA_OK)
                    throw new Error("Save Data received invalid bytes/count or could not atomically store the block.");
            }

            function loadDataCore(key, target, recover) {
                if (!dataBufferValid(target)) return { status: DATA_INVALID, count: 0 };
                try {
                    const fullKey = dataStorageKey(key);
                    // Persistent storage is authoritative for checked calls, including a missing entry.
                    const text = localStorage.getItem(fullKey) ?? (!recover ? memoryStorage.get(fullKey) ?? null : null);
                    let bytes, status = text === null ? DATA_MISSING : DATA_OK;
                    if (text !== null) {
                        try { bytes = dataDecode(text); } catch (_) { status = DATA_CORRUPT; }
                    }
                    if (recover && (status === DATA_MISSING || status === DATA_CORRUPT)) {
                        const backup = localStorage.getItem(fullKey + ".bak");
                        if (backup !== null) {
                            try { bytes = dataDecode(backup); status = DATA_RECOVERED; }
                            catch (_) { status = DATA_CORRUPT; }
                        }
                    }
                    if (status !== DATA_OK && status !== DATA_RECOVERED) return { status, count: 0 };
                    if (bytes.length > target.data.length) return { status: DATA_TOO_LARGE, count: 0 };
                    for (let index = 0; index < bytes.length; index += 1) target.data[index] = bytes[index];
                    return { status, count: bytes.length };
                } catch (_) { return { status: DATA_UNAVAILABLE, count: 0 }; }
            }

            function loadDataChecked(key, target) { return loadDataCore(key, target, true); }

            function loadData(key, target) {
                if (dataBufferValid(target)) target.data.fill(0);
                const result = loadDataCore(key, target, false);
                if (result.status !== DATA_OK && result.status !== DATA_MISSING)
                    throw new Error("Load Data encountered an invalid destination, corrupt block, oversized block, or unavailable storage.");
                return result.count;
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
                    assetDownloadCount, assetDownloadCacheHits, assetDownloadCacheBytes,
                    assetDownloadCacheCount: assetDownloadCache.size,
                    maximumAssetDownloadCacheBytes: MAX_ASSET_DOWNLOAD_CACHE_BYTES,
                    maximumAssetDownloadCacheEntries: MAX_ASSET_DOWNLOAD_CACHE_ENTRIES,
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
                if (cancelFileImport) cancelFileImport();
                for (const url of fileTransferUrls) URL.revokeObjectURL(url);
                fileTransferUrls.clear();
                shutdownImageCacheEntries = imageCache.size;
                shutdownImageReferences = 0;
                for (const entry of imageCache.values()) {
                    shutdownImageReferences += entry.refs;
                    if (entry.resource && typeof entry.resource.close === "function") entry.resource.close();
                    entry.refs = 0;
                    entry.disposed = true;
                }
                imageCache.clear();
                assetDownloadCache.clear();
                assetDownloadCacheBytes = 0;
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
                finishStartupLoading();
                closed = true;
                keys.length = 0;
                releaseAllInputs();
                setVirtualControlsVisible(false);
                mediaShutdown();
                window.__smileWeb.status = "stopped";
            }

            function fail(error) {
                if (error === STOP) { finish(); return; }
                finishStartupLoading();
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
                classMoveAssign, classOwnedRef, classLiveCount, configure, gameWindow, clear, fillRectangle, fillRectangleOpacity, drawRectangle,
                fillRoundedRectangle, drawRoundedRectangle, fillCircle, drawCircle, drawArc,
                fillQuadrilateral, drawQuadrilateral, drawLine, drawText, drawNumber, loadImage, imageRetain,
                imageRelease, imageAssign, imageMoveAssign, imageLoaded, imageWidth, imageHeight, drawImage,
                pushClip, popClip, textWidth, textHeight, textLength, textCodeAt, textSlice, showScreen,
                print, clearScreen, wait, getKey, keyHeld, keyEventHeld, windowWidth, windowHeight, windowTitle, windowActivate, pointerX, pointerY, pointerDeltaX, pointerDeltaY,
                pointerWheelDelta, pointerWheelRemainder, pointerInside, pointerHeld, pointerPressed, pointerReleased,
                playSound, stopSound,
                playMusic, pauseMusic, resumeMusic, stopMusic, setMusicVolume, loadTextFile, fileExport, fileImport,
                loadInt, saveInt, loadData, saveData, loadDataChecked, saveDataChecked, renderer3D, renderer3DImage, renderer3DText, renderer3DTextValue,
                gameClosed, endProgram, mediaShutdown, mediaDiagnostics, run
            };
        })();
        """;
}
