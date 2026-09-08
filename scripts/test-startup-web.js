"use strict";
const fs = require("fs"), path = require("path"), vm = require("vm"), assert = require("assert/strict");
const directory = path.resolve(process.argv[2]);
const html = fs.readFileSync(path.join(directory, "index.html"), "utf8");
const script = html.match(/<script>\s*([\s\S]*?)<\/script>/)[1];

function fixture() {
    let now = 0, frames = [], decoded, rejectDecode;
    const events = new Map(), elements = new Map();
    const decode = new Promise((resolve, reject) => { decoded = resolve; rejectDecode = reject; });
    const document = {
        hidden: false,
        addEventListener: (name, listener) => events.set(name, listener),
        getElementById(id) {
            if (!elements.has(id)) elements.set(id, { hidden: false, textContent: "", value: undefined,
                decode: () => decode, removeAttribute(name) { delete this[name]; } });
            return elements.get(id);
        }
    };
    const host = { document, performance: { now: () => now },
        requestAnimationFrame: callback => frames.push(callback), addEventListener() {} };
    host.window = host;
    vm.runInNewContext(script, host);
    async function frame(ms) {
        now += ms;
        const callbacks = frames; frames = [];
        for (const callback of callbacks) callback(now);
        for (let count = 0; count < 8; count++) await Promise.resolve();
    }
    return { host, frame, decoded, rejectDecode,
        element: id => document.getElementById("smile-loading" + id),
        visibility(hidden) { document.hidden = hidden; events.get("visibilitychange")(); }
    };
}

(async () => {
    const fast = fixture();
    fast.host.smileStartup.finish();
    await fast.frame(5000);
    assert.equal(fast.element("").hidden, false, "download time is not visible logo time");
    fast.decoded(); await fast.frame(0); await fast.frame(16); await fast.frame(16);
    await fast.host.smileStartup.painted;
    await fast.frame(999);
    assert.equal(fast.element("").hidden, false, "fast startup keeps the visible one-second minimum");
    await fast.frame(1);
    assert.equal(fast.element("").hidden, true);

    const hidden = fixture();
    hidden.decoded(); await hidden.frame(0); await hidden.frame(16); await hidden.frame(16);
    hidden.host.smileStartup.finish();
    await hidden.frame(300); hidden.visibility(true); await hidden.frame(10000);
    assert.equal(hidden.element("").hidden, false, "background time does not dismiss the loader");
    hidden.visibility(false); await hidden.frame(699);
    assert.equal(hidden.element("").hidden, false);
    await hidden.frame(1); assert.equal(hidden.element("").hidden, true);

    const slow = fixture();
    slow.decoded(); await slow.frame(0); await slow.frame(16); await slow.frame(16);
    await slow.frame(3000); slow.host.smileStartup.finish(); await slow.frame(0); await slow.frame(16);
    assert.equal(slow.element("").hidden, true, "long loading adds no unconditional extra second");

    const progress = fixture();
    progress.host.smileStartup.update(new Map());
    assert.equal(progress.element("-progress").value, undefined, "initial discovery keeps the left-right animation");
    const files = new Map([
        ["Assets/Model.sm3d", { state: "ready" }],
        ["Assets/Texture.png", { state: "loading", received: 512, total: 2048 }],
        ["Assets/Normal.png", { state: "queued" }]
    ]);
    progress.host.smileStartup.update(files);
    assert.equal(progress.element("-progress").value, 1, "only ready files advance the top bar");
    assert.equal(progress.element("-progress").max, 3, "known queued dependencies are included before downloading");
    assert.match(progress.element("-status").textContent, /1 \/ 3 ready/);
    assert.equal(progress.element("-transfer").value, 512);
    assert.equal(progress.element("-transfer").max, 2048);
    assert.match(progress.element("-transfer-text").textContent, /25%/);
    files.set("Assets/Texture.png", { state: "decoding" });
    progress.host.smileStartup.update(files);
    assert.equal(progress.element("-progress").value, 1, "decoding is not ready");
    assert.equal(progress.element("-transfer").value, undefined);
    files.set("Assets/Texture.png", { state: "ready" });
    files.set("Assets/Normal.png", { state: "failed" });
    files.set("Assets/Later.png", { state: "queued" });
    progress.host.smileStartup.update(files);
    assert.equal(progress.element("-progress").value, 2);
    assert.equal(progress.element("-progress").max, 4, "newly discovered files extend the known total");
    assert.match(progress.element("-status").textContent, /2 \/ 4 ready, 1 failed/);
    progress.host.smileStartup.finish();
    assert.equal(progress.element("-progress").value, 2, "opening after recovery does not mark failed or unstarted files ready");
    assert.equal(progress.element("-progress").max, 4);
    const unknown = fixture();
    unknown.host.smileStartup.update(new Map([["Assets/Compressed.sm3d", { state: "loading", received: 5000, total: 0 }]]));
    assert.equal(unknown.element("-transfer").value, undefined);
    assert.match(unknown.element("-transfer-text").textContent, /total size unknown/);
    const missing = fixture(); missing.rejectDecode(new Error("missing")); await missing.frame(0);
    assert.equal(missing.element("").hidden, false);
    assert.match(missing.element("-status").textContent, /Unable to load/);
    console.log("PASS: decoded/presented minimum, background exclusion, slow-load overlap, known file counts, byte/unknown progress, missing logo.");
})().catch(error => { console.error(error); process.exitCode = 1; });
