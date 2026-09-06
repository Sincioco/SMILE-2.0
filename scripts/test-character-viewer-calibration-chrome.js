// Visible installed-Chrome calibration transfer check on a random disposable origin.
"use strict";

const assert = require("node:assert/strict");
const fs = require("node:fs");
const http = require("node:http");
const path = require("node:path");
const { chromium } = require("playwright");

const args = process.argv.slice(2);
const option = (name, fallback) => args.includes(name) ? args[args.indexOf(name) + 1] : fallback;
const webRoot = path.resolve(option("--web-root", "tools/Character3DViewer/bin/Release/Web"));
const canonicalPath = path.resolve(option("--canonical",
    "games/SinStarI/SourceAssets/Characters/Paladin/ArinV57/Calibration/arin-v5.7-pose-calibration.json"));
const output = path.resolve(option("--output", "artifacts/tests/character-viewer-refactor-r4-chrome"));
const chromePath = option("--chrome", "C:/Program Files/Google/Chrome/Application/chrome.exe");

fs.mkdirSync(output, { recursive: true });

const server = http.createServer((request, response) => {
    let file;

    try {
        file = path.resolve(webRoot, "." + decodeURIComponent(
            new URL(request.url, "http://localhost").pathname));
    } catch {
        response.writeHead(400).end();
        return;
    }

    if (file !== webRoot && !file.startsWith(webRoot + path.sep)) {
        response.writeHead(403).end();
        return;
    }

    if (request.url === "/favicon.ico") {
        response.writeHead(204).end();
        return;
    }

    if (fs.existsSync(file) && fs.statSync(file).isDirectory()) {
        file = path.join(file, "index.html");
    }

    if (!fs.existsSync(file) || !fs.statSync(file).isFile()) {
        response.writeHead(404).end();
        return;
    }

    const mime = {
        ".html": "text/html",
        ".js": "text/javascript",
        ".png": "image/png",
        ".wav": "audio/wav",
    };
    response.writeHead(200, {
        "Content-Type": mime[path.extname(file)] || "application/octet-stream",
        "Cache-Control": "no-store",
    });
    fs.createReadStream(file).pipe(response);
});

async function installTextObserver(page) {
    await page.addInitScript(() => {
        window.__calibrationChrome = { text: [], lastText: [] };
        let api;
        Object.defineProperty(window, "smile", {
            get: () => api,
            set: value => {
                api = value;
                const drawText = api.drawText;
                const drawNumber = api.drawNumber;
                const showScreen = api.showScreen;
                api.showScreen = (...callArgs) => {
                    window.__calibrationChrome.lastText = window.__calibrationChrome.text;
                    window.__calibrationChrome.text = [];
                    return showScreen(...callArgs);
                };
                api.drawText = (...callArgs) => {
                    window.__calibrationChrome.text.push(callArgs);
                    return drawText(...callArgs);
                };
                api.drawNumber = (...callArgs) => {
                    window.__calibrationChrome.text.push(callArgs);
                    return drawNumber(...callArgs);
                };
            },
        });
    });
}

async function waitForFrames(page, count = 8) {
    const start = await page.evaluate(() => window.__smileWeb.frameCount);
    await page.waitForFunction(target => window.__smileWeb.status !== "running" ||
        window.__smileWeb.frameCount >= target, start + count, { timeout: 45000 });
}

async function canvasPoint(page, logicalX, logicalY) {
    return page.evaluate(({ logicalX, logicalY }) => {
        const canvas = document.querySelector("#smile-canvas");
        const box = canvas.getBoundingClientRect();
        const media = smile.mediaDiagnostics();
        return {
            x: box.x + logicalX * box.width / media.logicalWidth,
            y: box.y + logicalY * box.height / media.logicalHeight,
        };
    }, { logicalX, logicalY });
}

async function clickLogical(page, logicalX, logicalY) {
    const point = await canvasPoint(page, logicalX, logicalY);
    await page.mouse.click(point.x, point.y);
}

async function clickText(page, label, maximumY = Infinity, waitForNextFrames = true) {
    const logical = await page.evaluate(({ label, maximumY }) => {
        const entry = window.__calibrationChrome.lastText.find(
            item => item[0] === label && item[2] < maximumY);
        if (!entry) {
            throw new Error(`Visible canvas label not found: ${label}`);
        }
        return { x: entry[1] + 8, y: entry[2] + 6 };
    }, { label, maximumY });
    await clickLogical(page, logical.x, logical.y);

    if (waitForNextFrames) {
        await waitForFrames(page);
    }
}

async function waitForText(page, predicateText) {
    await page.waitForFunction(text => window.__calibrationChrome.lastText.some(
        item => String(item[0]).includes(text)), predicateText, { timeout: 30000 });
}

async function currentInspectorFrame(page) {
    return page.evaluate(() => {
        const entry = window.__calibrationChrome.lastText.find(item =>
            item[1] >= 190 && item[1] <= 230 && item[2] >= 340 && item[2] <= 365 &&
            /^-?\d+$/.test(String(item[0])));
        if (!entry) {
            throw new Error("Current inspector frame value was not rendered");
        }
        return Number(entry[0]);
    });
}

function findKey(snapshot, clipName, frame) {
    const clip = snapshot.clips.find(item => item.name === clipName);
    assert(clip, `Missing clip ${clipName}`);
    const key = clip.keyframes.find(item => item.frame === frame);
    assert(key, `Missing ${clipName} frame ${frame}`);
    return key;
}

async function run() {
    await new Promise(resolve => server.listen(0, "127.0.0.1", resolve));
    const origin = `http://127.0.0.1:${server.address().port}`;
    const canonical = JSON.parse(fs.readFileSync(canonicalPath, "utf8"));
    const changed = JSON.parse(JSON.stringify(canonical));
    const targetClip = changed.clips.find(item => item.keyframes.length > 0);
    const targetKey = targetClip.keyframes[0];
    const originalValue = targetKey.sword.position[0];
    const changedValue = originalValue < 100 ? originalValue + 1 : originalValue - 1;
    targetKey.sword.position[0] = changedValue;
    const fixturePath = path.join(output, "arin-disposable-import.json");
    const downloadPath = path.join(output, "arin-reloaded-download.json");
    fs.writeFileSync(fixturePath, JSON.stringify(changed, null, 2));

    const browser = await chromium.launch({
        executablePath: chromePath,
        headless: false,
        chromiumSandbox: true,
        args: ["--start-maximized"],
    });
    const report = {
        browser: "Google Chrome",
        browserVersion: browser.version(),
        visible: true,
        origin,
        originLifetime: "random-port disposable test server",
        target: { clip: targetClip.name, frame: targetKey.frame, originalValue, changedValue },
        checks: [],
        messages: [],
    };

    try {
        const page = await browser.newPage({ viewport: null });
        const chromeSession = await page.context().newCDPSession(page);
        const { windowId } = await chromeSession.send("Browser.getWindowForTarget");
        await chromeSession.send("Browser.setWindowBounds", {
            windowId,
            bounds: { windowState: "maximized" },
        });
        page.on("console", message => {
            if (["error", "warning"].includes(message.type())) {
                report.messages.push({ type: message.type(), text: message.text() });
            }
        });
        page.on("pageerror", error => report.messages.push({
            type: "pageerror", text: String(error.stack || error),
        }));
        await installTextObserver(page);
        await page.goto(origin + "/");
        await page.bringToFront();
        await page.waitForFunction(() => window.__smileWeb?.frameCount >= 50 ||
            window.__smileWeb?.status === "error", {}, { timeout: 60000 });
        assert.equal(await page.evaluate(() => window.__smileWeb.status), "running");
        await page.evaluate(() => window.scrollTo(0, 0));
        await clickText(page, "Arin", 110);
        report.checks.push("Arin loaded from the untouched published default on a fresh origin");

        await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight));
        await waitForFrames(page, 2);
        await page.screenshot({ path: path.join(output, "before-import.png") });
        const chooserPromise = page.waitForEvent("filechooser", { timeout: 30000 });
        await clickText(page, "Import Key Frames", Infinity, false);
        const chooser = await chooserPromise;
        await chooser.setFiles(fixturePath);
        await waitForText(page, "Validated; Replace All Keys?");
        report.checks.push("Identity-valid JSON was staged without replacing keys");
        await clickText(page, "Replace Keys?");
        await waitForText(page, "Imported Saved Keys");
        assert.equal(await page.evaluate(() => window.__smileWeb.status), "running");
        await page.screenshot({ path: path.join(output, "after-import.png") });
        report.checks.push("Confirmed import committed without recovery UI");

        await page.reload();
        await page.waitForFunction(() => window.__smileWeb?.frameCount >= 50 ||
            window.__smileWeb?.status === "error", {}, { timeout: 60000 });
        assert.equal(await page.evaluate(() => window.__smileWeb.status), "running");
        await page.evaluate(() => window.scrollTo(0, 0));
        await clickText(page, "Arin", 110);
        await clickText(page, "Pose");
        await waitForText(page, "Pose Calibration");
        const frameBeforeQueuedKey = await currentInspectorFrame(page);
        await page.keyboard.down("Control");
        await page.keyboard.press("ArrowRight");
        await page.keyboard.up("Control");
        await waitForFrames(page);
        const frameAfterQueuedKey = await currentInspectorFrame(page);
        assert(frameAfterQueuedKey > frameBeforeQueuedKey);
        await page.keyboard.press("ArrowLeft");
        await waitForFrames(page);
        assert.equal(await currentInspectorFrame(page), frameAfterQueuedKey);
        report.queuedFrameStep = { before: frameBeforeQueuedKey, after: frameAfterQueuedKey };
        report.checks.push("Queued Ctrl+Right advanced after modifier release; plain Left remained a camera command");

        await clickText(page, "Show Gizmo");
        await waitForText(page, "Hide Gizmo");
        await clickText(page, "Hide Gizmo");
        await waitForText(page, "Show Gizmo");
        report.checks.push("The opt-in gizmo showed and hid without closing the numeric Pose inspector");
        await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight));
        await waitForFrames(page, 2);
        await page.screenshot({ path: path.join(output, "after-reload-inspection.png") });
        report.checks.push("Reload retained the imported working save and Pose inspector rendered");

        const downloadPromise = page.waitForEvent("download", { timeout: 30000 });
        await clickText(page, "Download Key Frames");
        const download = await downloadPromise;
        await download.saveAs(downloadPath);
        const downloaded = JSON.parse(fs.readFileSync(downloadPath, "utf8"));
        const downloadedKey = findKey(downloaded, targetClip.name, targetKey.frame);
        assert.equal(downloadedKey.sword.position[0], changedValue);
        assert.equal(downloaded.totalKeyframes, canonical.totalKeyframes);
        assert.equal(downloaded.assetId, canonical.assetId);
        assert.equal(downloaded.profile.sm3dSha256, canonical.profile.sm3dSha256);
        report.download = { suggestedFilename: download.suggestedFilename(), path: downloadPath };
        report.checks.push("Downloaded saved JSON after reload retained all identity and the imported channel");
        assert.equal(report.messages.length, 0, JSON.stringify(report.messages));
    } finally {
        await browser.close();
        fs.writeFileSync(path.join(output, "report.json"), JSON.stringify(report, null, 2));
        server.close();
    }

    console.log(`Chrome ${report.browserVersion}: PASS`);
    console.log(`Evidence: ${output}`);
}

run().catch(error => {
    console.error(error);
    process.exitCode = 1;
    server.close();
});
