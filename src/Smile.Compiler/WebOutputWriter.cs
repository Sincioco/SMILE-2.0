using System.Net;
using System.Text;

namespace Smile.Compiler;

internal static class WebOutputWriter
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);

    public static void Write(string outputDirectory, WebEmitter emitter)
    {
        var game = emitter.Emit();
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(outputDirectory, "index.html"), Index(emitter.Title), Utf8WithoutBom);
        File.WriteAllText(Path.Combine(outputDirectory, "smile-runtime.js"), Runtime, Utf8WithoutBom);
        File.WriteAllText(Path.Combine(outputDirectory, "game.js"), game, Utf8WithoutBom);
        File.WriteAllText(Path.Combine(outputDirectory, "smile.css"), Style, Utf8WithoutBom);
    }

    private static string Index(string title) => $$"""
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>{{WebUtility.HtmlEncode(title)}}</title>
          <link rel="stylesheet" href="smile.css">
        </head>
        <body>
          <main id="smile-shell">
            <canvas id="smile-canvas" width="960" height="540" tabindex="0" aria-label="{{WebUtility.HtmlEncode(title)}}"></canvas>
            <pre id="smile-console" hidden aria-live="polite"></pre>
            <pre id="smile-error" hidden></pre>
          </main>
          <script src="smile-runtime.js"></script>
          <script src="game.js"></script>
        </body>
        </html>
        """;

    private const string Style = """
        :root { color-scheme: dark; font-family: "Segoe UI", Arial, sans-serif; }
        * { box-sizing: border-box; }
        html, body { width: 100%; height: 100%; margin: 0; overflow: hidden; background: #05070c; }
        body { display: grid; place-items: center; }
        #smile-shell { position: relative; width: 100vw; height: 100vh; display: grid; place-items: center; background: #05070c; }
        #smile-canvas { display: block; max-width: 100vw; max-height: 100vh; width: auto; height: auto; aspect-ratio: 16 / 9; background: #000; outline: none; }
        #smile-canvas:focus-visible { box-shadow: inset 0 0 0 2px #46e6ff; }
        #smile-console { width: min(72rem, 100vw); height: 100vh; margin: 0; padding: 1rem; overflow: auto; color: #f2f4f8; background: #05070c; font: 16px/1.4 Consolas, monospace; white-space: pre-wrap; }
        #smile-error { position: absolute; left: 1rem; right: 1rem; bottom: 1rem; max-height: 35vh; overflow: auto; margin: 0; padding: 1rem; color: #fff; background: #761b25; border: 1px solid #ff8794; white-space: pre-wrap; }
        """;

    private const string Runtime = """
        "use strict";

        window.__smileWeb = { status: "starting", frameCount: 0 };

        window.smile = (() => {
            const MAX_SAFE = Number.MAX_SAFE_INTEGER;
            const STOP = Object.freeze({ smileStop: true });
            const canvas = document.getElementById("smile-canvas");
            const visible = canvas.getContext("2d", { alpha: false });
            const backCanvas = document.createElement("canvas");
            const back = backCanvas.getContext("2d", { alpha: false });
            const consoleOutput = document.getElementById("smile-console");
            const errorPanel = document.getElementById("smile-error");
            const keys = [];
            const heldKeys = new Set();
            const memoryStorage = new Map();
            const imageCache = new Map();
            const sfxCache = new Map();
            const sfxChannels = new Array(16).fill(null);
            let logicalWidth = 960;
            let logicalHeight = 540;
            let closed = false;
            let active = true;
            let userInteracted = false;
            let audioContext = null;
            let currentMusic = null;
            let musicVolume = 100;
            let musicRequested = false;
            let musicPaused = false;
            let consoleText = "";
            let storageNamespace = "smile2:web";

            function safe(value) {
                if (!Number.isSafeInteger(value))
                    throw new Error(`SMILE Web NUMBER is outside the safe integer range: ${value}`);
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
                if (right === 0) throw new Error("SMILE Web MOD by zero.");
                return safe(left % right);
            }

            function isTrue(value) { return typeof value === "boolean" ? value : safe(value) !== 0; }
            function booleanText(value) { return isTrue(value) ? "TRUE" : "FALSE"; }
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
                if (maximum < minimum) throw new Error("SMILE Web RANDOM maximum is below its minimum.");
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
            function ref(getter, setter) { return { get: getter, set: setter }; }
            function refArray(target, indices) {
                const offset = arrayOffset(target, indices);
                return { get: () => target.data[offset], set: value => { target.data[offset] = value; } };
            }
            function invalidRef() { throw new Error("Invalid SMILE BYREF argument."); }

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
                if (logicalWidth <= 0 || logicalHeight <= 0) throw new Error("GAME WINDOW dimensions must be positive.");
                canvas.width = backCanvas.width = logicalWidth;
                canvas.height = backCanvas.height = logicalHeight;
                canvas.style.aspectRatio = `${logicalWidth} / ${logicalHeight}`;
                document.title = title;
                canvas.setAttribute("aria-label", title);
                canvas.hidden = false;
                consoleOutput.hidden = true;
                storageNamespace = `smile2:${title}`;
                visible.imageSmoothingEnabled = false;
                back.imageSmoothingEnabled = false;
                resizeCanvas();
            }

            function resizeCanvas() {
                const scale = Math.min(window.innerWidth / logicalWidth, window.innerHeight / logicalHeight);
                canvas.style.width = `${Math.max(1, Math.floor(logicalWidth * scale))}px`;
                canvas.style.height = `${Math.max(1, Math.floor(logicalHeight * scale))}px`;
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

            function logicalPath(value) { return String(value).replaceAll("\\", "/").replace(/^\.\//, ""); }

            async function loadImage(path) {
                const logical = logicalPath(path);
                if (!logical) throw new Error("LOAD IMAGE path must not be empty.");
                let entry = imageCache.get(logical);
                if (!entry) {
                    entry = { logical, refs: 0, resource: null, width: 0, height: 0, promise: null };
                    entry.promise = new Promise((resolve, reject) => {
                        const resource = new Image();
                        resource.onload = () => {
                            entry.resource = resource;
                            entry.width = safe(resource.naturalWidth || resource.width);
                            entry.height = safe(resource.naturalHeight || resource.height);
                            if (entry.width <= 0 || entry.height <= 0) reject(new Error(`LOAD IMAGE decoded invalid dimensions: ${logical}`));
                            else resolve(entry);
                        };
                        resource.onerror = () => reject(new Error(`LOAD IMAGE failed: ${logical}`));
                        resource.src = logical;
                    }).catch(error => { if (entry.refs === 0) imageCache.delete(logical); throw error; });
                    imageCache.set(logical, entry);
                }
                await entry.promise;
                entry.refs += 1;
                return { entry };
            }

            function imageRetain(handle) {
                if (handle && handle.entry) handle.entry.refs += 1;
                return handle;
            }

            function imageRelease(handle) {
                if (!handle || !handle.entry) return;
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

            function imageLoaded(handle) { return Boolean(handle && handle.entry && handle.entry.resource); }
            function imageWidth(handle) { return imageLoaded(handle) ? safe(handle.entry.width) : 0; }
            function imageHeight(handle) { return imageLoaded(handle) ? safe(handle.entry.height) : 0; }

            function drawImage(handle, sourceX, sourceY, sourceWidth, sourceHeight, destinationX, destinationY,
                destinationWidth, destinationHeight, opacity, filter, flip, anchorX, anchorY) {
                if (!imageLoaded(handle)) throw new Error("DRAW IMAGE requires a loaded IMAGE.");
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
                    throw new Error("DRAW IMAGE source rectangle is outside the image.");
                if (destinationWidth <= 0 || destinationHeight <= 0 || opacity < 0 || opacity > 100 ||
                    (filter !== 0 && filter !== 1) || (flip & ~3) !== 0)
                    throw new Error("DRAW IMAGE destination, opacity, filter, or flip is invalid.");
                const left = destinationX - anchorX;
                const top = destinationY - anchorY;
                const flipX = (flip & 1) !== 0;
                const flipY = (flip & 2) !== 0;
                back.save();
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
                back.restore();
            }

            function pushClip(x, y, width, height) {
                x = safe(x); y = safe(y); width = safe(width); height = safe(height);
                if (width <= 0 || height <= 0) throw new Error("CLIP RECTANGLE width and height must be positive.");
                back.save();
                back.beginPath();
                back.rect(x, y, width, height);
                back.clip();
            }

            function popClip() { back.restore(); }

            function textWidth(text, size) {
                size = safe(size);
                if (size <= 0) return 0;
                textStyle(size, 0, false);
                return safe(Math.ceil(back.measureText(String(text)).width));
            }

            function textHeight(text, size) {
                size = safe(size);
                if (size <= 0) return 0;
                textStyle(size, 0, false);
                const metrics = back.measureText(String(text));
                const height = (metrics.actualBoundingBoxAscent || 0) + (metrics.actualBoundingBoxDescent || 0);
                return safe(Math.ceil(height > 0 ? height : safe(size)));
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
                visible.drawImage(backCanvas, 0, 0);
                window.__smileWeb.frameCount += 1;
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
                    default: return 19;
                }
            }

            function controlledKey(event) {
                return event.code.startsWith("Arrow") || event.code === "Space" || event.code === "Enter" ||
                    event.code === "Escape" || /^Key[WASD]$/.test(event.code);
            }

            async function toggleFullScreen() {
                try {
                    if (document.fullscreenElement) await document.exitFullscreen();
                    else await document.getElementById("smile-shell").requestFullscreen();
                } catch (_) { }
            }

            window.addEventListener("keydown", event => {
                userInteracted = true;
                heldKeys.add(mapKey(event));
                syncMusic();
                if (event.altKey && event.code === "Enter") {
                    event.preventDefault();
                    void toggleFullScreen();
                    return;
                }
                if (event.repeat || event.ctrlKey || event.altKey || event.metaKey) return;
                if (controlledKey(event)) event.preventDefault();
                keys.push(mapKey(event));
                if (keys.length > 256) keys.shift();
            });

            window.addEventListener("keyup", event => { heldKeys.delete(mapKey(event)); });

            canvas.addEventListener("click", () => { userInteracted = true; canvas.focus(); syncMusic(); });
            window.addEventListener("resize", resizeCanvas);
            document.addEventListener("fullscreenchange", resizeCanvas);
            window.addEventListener("focus", () => { active = !document.hidden; syncMusic(); });
            window.addEventListener("blur", () => { active = false; keys.length = 0; heldKeys.clear(); stopSound(); syncMusic(); });
            document.addEventListener("visibilitychange", () => {
                active = !document.hidden && document.hasFocus();
                if (!active) { keys.length = 0; heldKeys.clear(); stopSound(); }
                syncMusic();
            });
            window.addEventListener("pagehide", () => { closed = true; stopSound(); stopMusic(); });

            function getKey() { return keys.length === 0 ? 0 : keys.shift(); }
            function keyHeld(key) { return heldKeys.has(safe(key)) ? 1 : 0; }

            function checkedChannel(channel) {
                channel = safe(channel);
                if (channel < 0 || channel >= 16) throw new Error("Sound channel must be from 0 through 15.");
                return channel;
            }

            function stopSound(channel = null) {
                const first = channel === null ? 0 : checkedChannel(channel);
                const last = channel === null ? 15 : first;
                for (let index = first; index <= last; index += 1) {
                    const voice = sfxChannels[index];
                    if (!voice) continue;
                    try {
                        if (voice.stop) voice.stop();
                        else { voice.pause(); voice.currentTime = 0; }
                    } catch (_) { }
                    sfxChannels[index] = null;
                }
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
                    if (!response.ok) throw new Error(`PLAY SOUND failed: ${logical}`);
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
                stopSound(channel);
                if (!active || !userInteracted) return;
                const cached = await loadSfx(path);
                if (cached.buffer && audioContext) {
                    if (audioContext.state === "suspended") await audioContext.resume();
                    const source = audioContext.createBufferSource();
                    source.buffer = cached.buffer;
                    source.connect(audioContext.destination);
                    source.onended = () => { if (sfxChannels[channel] === source) sfxChannels[channel] = null; };
                    sfxChannels[channel] = source;
                    source.start();
                    return;
                }
                const sound = new Audio(cached.logical);
                sfxChannels[channel] = sound;
                sound.addEventListener("ended", () => { if (sfxChannels[channel] === sound) sfxChannels[channel] = null; });
                const playback = sound.play();
                if (playback) playback.catch(() => { if (sfxChannels[channel] === sound) sfxChannels[channel] = null; });
            }

            function syncMusic() {
                if (!currentMusic) return;
                currentMusic.volume = active ? Math.max(0, Math.min(100, musicVolume)) / 100 : 0;
                if (!musicRequested || musicPaused) {
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
                    currentMusic = new Audio(String(path).replaceAll("\\", "/"));
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
                if (!target || !Array.isArray(target.data)) throw new Error("LOAD TEXT FILE requires a one-dimensional array.");
                target.data.fill(0);
                try {
                    const response = await fetch(String(path).replaceAll("\\", "/"), { cache: "no-store" });
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

            function saveData(target, count, key) {
                if (!target || !Array.isArray(target.data) || target.dimensions.length !== 1)
                    throw new Error("SAVE DATA source must be a one-dimensional NUMBER array.");
                count = safe(count);
                if (count < 0 || count > target.data.length || count > 1024 * 1024)
                    throw new Error("SAVE DATA COUNT is outside the buffer or DATA_BLOCK_MAX_BYTES.");
                const bytes = target.data.slice(0, count).map(value => {
                    value = safe(value);
                    if (value < 0 || value > 255) throw new Error("SAVE DATA values must be bytes from 0 through 255.");
                    return value;
                });
                const fullKey = storageKey(`data:${String(key)}`);
                const text = encodeBytes(bytes);
                memoryStorage.set(fullKey, text);
                localStorage.setItem(fullKey, text);
            }

            function loadData(key, target) {
                if (!target || !Array.isArray(target.data) || target.dimensions.length !== 1)
                    throw new Error("LOAD DATA destination must be a one-dimensional NUMBER array.");
                target.data.fill(0);
                const fullKey = storageKey(`data:${String(key)}`);
                let text = memoryStorage.has(fullKey) ? memoryStorage.get(fullKey) : null;
                text = localStorage.getItem(fullKey) ?? text;
                if (text === null) return 0;
                let bytes;
                try { bytes = decodeBytes(text); }
                catch (_) { throw new Error("LOAD DATA encountered corrupt persistent data."); }
                if (bytes.length > 1024 * 1024 || bytes.length > target.data.length)
                    throw new Error("LOAD DATA block exceeds the destination capacity.");
                for (let index = 0; index < bytes.length; index += 1) target.data[index] = bytes[index];
                return safe(bytes.length);
            }

            function gameClosed() { return closed ? 1 : 0; }
            function endProgram() { closed = true; stopSound(); stopMusic(); throw STOP; }

            function finish() {
                closed = true;
                stopSound();
                stopMusic();
                window.__smileWeb.status = "stopped";
            }

            function fail(error) {
                if (error === STOP) { finish(); return; }
                closed = true;
                stopSound();
                stopMusic();
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
                array, get, set, ref, refArray, invalidRef, gameWindow, clear, fillRectangle, drawRectangle,
                fillRoundedRectangle, drawRoundedRectangle, fillCircle, drawCircle, drawArc,
                fillQuadrilateral, drawQuadrilateral, drawLine, drawText, drawNumber, loadImage, imageRetain,
                imageRelease, imageAssign, imageMoveAssign, imageLoaded, imageWidth, imageHeight, drawImage,
                pushClip, popClip, textWidth, textHeight, showScreen,
                print, clearScreen, wait, getKey, keyHeld, playSound, stopSound,
                playMusic, pauseMusic, resumeMusic, stopMusic, setMusicVolume, loadTextFile,
                loadInt, saveInt, loadData, saveData, gameClosed, endProgram, run
            };
        })();
        """;
}
