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
            const errorPanel = document.getElementById("smile-error");
            const keys = [];
            const memoryStorage = new Map();
            let logicalWidth = 960;
            let logicalHeight = 540;
            let closed = false;
            let active = true;
            let userInteracted = false;
            let currentSound = null;
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

            function isTrue(value) { return safe(value) !== 0; }
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

            function array(dimensions) {
                let total = 1;
                for (const dimension of dimensions) {
                    if (!Number.isSafeInteger(dimension) || dimension <= 0)
                        throw new Error("SMILE Web array dimensions must be positive safe integers.");
                    total = safe(total * dimension);
                }
                return { dimensions: dimensions.slice(), data: new Array(total).fill(0) };
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
            function set(target, indices, value) { target.data[arrayOffset(target, indices)] = safe(value); }

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

            canvas.addEventListener("click", () => { userInteracted = true; canvas.focus(); });
            window.addEventListener("resize", resizeCanvas);
            document.addEventListener("fullscreenchange", resizeCanvas);
            window.addEventListener("focus", () => { active = !document.hidden; });
            window.addEventListener("blur", () => { active = false; keys.length = 0; stopSound(); });
            document.addEventListener("visibilitychange", () => {
                active = !document.hidden && document.hasFocus();
                if (!active) { keys.length = 0; stopSound(); }
            });
            window.addEventListener("pagehide", () => { closed = true; stopSound(); });

            function getKey() { return keys.length === 0 ? 0 : keys.shift(); }

            function stopSound() {
                if (!currentSound) return;
                currentSound.pause();
                currentSound.currentTime = 0;
                currentSound = null;
            }

            function playSound(path) {
                stopSound();
                if (!active || !userInteracted) return;
                try {
                    const sound = new Audio(String(path).replaceAll("\\", "/"));
                    currentSound = sound;
                    sound.addEventListener("ended", () => { if (currentSound === sound) currentSound = null; });
                    const playback = sound.play();
                    if (playback) playback.catch(() => { if (currentSound === sound) currentSound = null; });
                } catch (_) { currentSound = null; }
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

            function gameClosed() { return closed ? 1 : 0; }
            function endProgram() { closed = true; stopSound(); throw STOP; }

            function finish() {
                closed = true;
                stopSound();
                window.__smileWeb.status = "stopped";
            }

            function fail(error) {
                if (error === STOP) { finish(); return; }
                closed = true;
                stopSound();
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
                safe, add, sub, mul, div, mod, neg, isTrue, abs, min, max, timer, rgb, random,
                array, get, set, gameWindow, clear, fillRectangle, drawRectangle,
                fillRoundedRectangle, drawRoundedRectangle, drawText, drawNumber, showScreen,
                getKey, playSound, stopSound, loadInt, saveInt, gameClosed, endProgram, run
            };
        })();
        """;
}
