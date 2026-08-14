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
            const sfxGenerations = new Array(16).fill(0);
            const clipStack = [];
            const assetPaths = new Set();
            const MAX_BACKING_DIMENSION = 8192;
            const MAX_BACKING_PIXELS = 33554432;
            let logicalWidth = 960;
            let logicalHeight = 540;
            let backingWidth = 960;
            let backingHeight = 540;
            let imageDecodeCount = 0;
            let imageCacheHitCount = 0;
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

            function configure(appIdentity, manifest) {
                storageNamespace = `smile2:${sha256Hex(utf8(String(appIdentity)))}`;
                assetPaths.clear();
                for (const path of manifest || []) assetPaths.add(canonicalAssetPath(path));
            }

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
                canvas.style.aspectRatio = `${logicalWidth} / ${logicalHeight}`;
                document.title = title;
                canvas.setAttribute("aria-label", title);
                canvas.hidden = false;
                consoleOutput.hidden = true;
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
                    restoreVisibleState();
                    restoreBackState();
                }
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
                if (!logical) throw new Error("LOAD IMAGE path must not be empty.");
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
                            if (entry.width <= 0 || entry.height <= 0) reject(new Error(`LOAD IMAGE decoded invalid dimensions: ${logical}`));
                            else resolve(entry);
                        };
                        resource.onerror = () => reject(new Error(`LOAD IMAGE failed: ${logical}`));
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
                    if (!imageLoadedRaw(handle)) throw new Error("DRAW IMAGE requires a loaded IMAGE.");
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
                if (width <= 0 || height <= 0) throw new Error("CLIP RECTANGLE width and height must be positive.");
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
            if (window.visualViewport) window.visualViewport.addEventListener("resize", resizeCanvas);
            if (window.screen && window.screen.orientation) window.screen.orientation.addEventListener("change", resizeCanvas);
            document.addEventListener("fullscreenchange", resizeCanvas);
            window.addEventListener("focus", () => { active = !document.hidden; syncMusic(); });
            window.addEventListener("blur", () => { active = false; keys.length = 0; heldKeys.clear(); stopSound(); syncMusic(); });
            document.addEventListener("visibilitychange", () => {
                active = !document.hidden && document.hasFocus();
                if (!active) { keys.length = 0; heldKeys.clear(); stopSound(); }
                syncMusic();
            });
            window.addEventListener("pagehide", () => { closed = true; mediaShutdown(); });

            function getKey() { return keys.length === 0 ? 0 : keys.shift(); }
            function keyHeld(key) { return heldKeys.has(safe(key)) ? 1 : 0; }

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
                if (!target || !Array.isArray(target.data)) throw new Error("LOAD TEXT FILE requires a one-dimensional array.");
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
                    throw new Error("LOAD DATA encountered an invalid persistent-data envelope.");
                const view = new DataView(envelope.buffer, envelope.byteOffset, envelope.byteLength);
                const version = view.getUint32(4, true);
                const length = view.getUint32(8, true);
                if (version !== 1 || length > 1024 * 1024 || envelope.length !== 44 + length)
                    throw new Error("LOAD DATA encountered an unsupported or malformed persistent-data envelope.");
                const payload = envelope.slice(44);
                const digest = sha256(payload);
                for (let index = 0; index < digest.length; index += 1)
                    if (digest[index] !== envelope[12 + index])
                        throw new Error("LOAD DATA persistent-data checksum mismatch.");
                return payload;
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
                const fullKey = dataStorageKey(key);
                const text = encodeBytes(dataEnvelope(new Uint8Array(bytes)));
                memoryStorage.set(fullKey, text);
                localStorage.setItem(fullKey, text);
            }

            function loadData(key, target) {
                if (!target || !Array.isArray(target.data) || target.dimensions.length !== 1)
                    throw new Error("LOAD DATA destination must be a one-dimensional NUMBER array.");
                target.data.fill(0);
                const fullKey = dataStorageKey(key);
                let text = memoryStorage.has(fullKey) ? memoryStorage.get(fullKey) : null;
                text = localStorage.getItem(fullKey) ?? text;
                if (text === null) return 0;
                let bytes;
                try { bytes = dataPayload(new Uint8Array(decodeBytes(text))); }
                catch (error) { target.data.fill(0); throw error; }
                if (bytes.length > 1024 * 1024 || bytes.length > target.data.length)
                    throw new Error("LOAD DATA block exceeds the destination capacity.");
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
                    imageDecodeCount, imageCacheHitCount,
                    shutdownImageCacheEntries, shutdownImageReferences,
                    sfxActiveCount: sfxChannels.filter(Boolean).length,
                    sfxCacheCount: sfxCache.size,
                    sfxCompletionCount,
                    mediaStopped,
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
                mediaShutdown();
                window.__smileWeb.status = "stopped";
            }

            function fail(error) {
                if (error === STOP) { finish(); return; }
                closed = true;
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
                array, get, set, ref, refArray, invalidRef, configure, gameWindow, clear, fillRectangle, drawRectangle,
                fillRoundedRectangle, drawRoundedRectangle, fillCircle, drawCircle, drawArc,
                fillQuadrilateral, drawQuadrilateral, drawLine, drawText, drawNumber, loadImage, imageRetain,
                imageRelease, imageAssign, imageMoveAssign, imageLoaded, imageWidth, imageHeight, drawImage,
                pushClip, popClip, textWidth, textHeight, textLength, textCodeAt, textSlice, showScreen,
                print, clearScreen, wait, getKey, keyHeld, playSound, stopSound,
                playMusic, pauseMusic, resumeMusic, stopMusic, setMusicVolume, loadTextFile,
                loadInt, saveInt, loadData, saveData, gameClosed, endProgram, mediaShutdown, mediaDiagnostics, run
            };
        })();
        """;
}
