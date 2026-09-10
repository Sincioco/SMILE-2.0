"use strict";
const fs = require("fs"), path = require("path"), vm = require("vm"), assert = require("assert/strict");
const directory = path.resolve(process.argv[2]);
const html = fs.readFileSync(path.join(directory, "index.html"), "utf8");
const script = html.match(/<script>\s*([\s\S]*?)<\/script>/)[1];

function fixture(initiallyHidden = false) {
    let now = 0, nextId = 1, frames = new Map(), timers = new Map(), decoded, rejectDecode;
    const events = new Map(), elements = new Map();
    const decode = new Promise((resolve, reject) => { decoded = resolve; rejectDecode = reject; });
    const document = {
        hidden: initiallyHidden,
        addEventListener: (name, listener) => events.set(name, listener),
        getElementById(id) {
            if (!elements.has(id)) elements.set(id, { hidden: false, textContent: "", value: undefined,
                decode: () => decode, removeAttribute(name) { delete this[name]; },
                addEventListener(name, listener) { this[name] = listener; } });
            return elements.get(id);
        }
    };
    const host = { document, performance: { now: () => now },
        requestAnimationFrame(callback) { const id = nextId++; frames.set(id, callback); return id; },
        cancelAnimationFrame: id => frames.delete(id),
        setTimeout(callback, delay) { const id = nextId++; timers.set(id, { callback, at: now + delay }); return id; },
        clearTimeout: id => timers.delete(id),
        addEventListener: (name, listener) => events.set(name, listener) };
    host.window = host;
    vm.runInNewContext(script, host);
    async function frame(ms) {
        now += ms;
        if (!document.hidden) {
            const callbacks = frames; frames = new Map();
            for (const callback of callbacks.values()) callback(now);
        }
        for (const [id, timer] of timers) if (timer.at <= now) { timers.delete(id); timer.callback(); }
        for (let count = 0; count < 8; count++) await Promise.resolve();
    }
    return { host, frame, decoded, rejectDecode,
        pending: () => frames.size + timers.size,
        event: name => events.get(name)(),
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

    for (let cycle = 0; cycle < 2; cycle++) {
        const repaint = fast.host.smileStartup.begin();
        assert.equal(fast.element("").hidden, false, "tab loading reopens the same presentation");
        assert.equal(fast.element("-progress").value, undefined, "each loading cycle starts indeterminate");
        assert.equal(fast.host.smileStartup.begin(), repaint, "nested show calls do not restart the interval");
        await fast.frame(0); await fast.frame(16); await fast.frame(16); await repaint;
        fast.host.smileStartup.update(new Map([["Cached.sm3d", { state: "ready" }]]));
        assert.equal(fast.element("-progress").value, 1);
        fast.host.smileStartup.finish();
        await fast.frame(999);
        assert.equal(fast.element("").hidden, false, "cached switches get their own visible minimum");
        await fast.frame(1);
        assert.equal(fast.element("").hidden, true);
    }

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

    for (const repeat of [false, true]) {
        const waiting = fixture(!repeat);
        waiting.decoded(); await waiting.frame(0);
        if (repeat) {
            await waiting.frame(16); await waiting.frame(16);
            waiting.host.smileStartup.finish(); await waiting.frame(0); await waiting.frame(1000);
            waiting.visibility(true);
        }
        const admission = repeat ? waiting.host.smileStartup.begin() : waiting.host.smileStartup.painted;
        let shown = "pending";
        admission.then(value => { shown = value; });
        await waiting.frame(0); await waiting.frame(31000);
        assert.equal(shown, "pending", "healthy decoded initial/repeat waits beyond the deadline while hidden");
        assert.equal(waiting.element("").hidden, false);
        waiting.visibility(false); await waiting.frame(16);
        waiting.visibility(true); await waiting.frame(31000);
        waiting.visibility(false); await waiting.frame(16);
        assert.equal(shown, "pending", "hiding between the two presentation frames resets that boundary");
        await waiting.frame(16);
        assert.equal(await admission, true);
        waiting.host.smileStartup.finish(); await waiting.frame(999);
        assert.equal(waiting.element("").hidden, false, "pre-admission hidden time never satisfies branding");
        await waiting.frame(1);
        assert.equal(waiting.element("").hidden, true);
        assert.equal(waiting.pending(), 0);
    }

    const suspendedDecode = fixture(true);
    let suspendedResult = "pending";
    suspendedDecode.host.smileStartup.painted.then(value => { suspendedResult = value; });
    await suspendedDecode.frame(31000);
    assert.equal(suspendedResult, "pending", "hidden decode suspension does not consume eligible time");
    suspendedDecode.visibility(false); await suspendedDecode.frame(10000);
    suspendedDecode.visibility(true); await suspendedDecode.frame(31000);
    suspendedDecode.visibility(false); await suspendedDecode.frame(19999);
    assert.equal(suspendedResult, "pending");
    await suspendedDecode.frame(1);
    assert.equal(suspendedResult, false, "a stalled decoder fails after 30 seconds of eligible time");

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
    assert.equal(await missing.host.smileStartup.painted, false, "initial failure cannot admit main");
    assert.equal(await missing.host.smileStartup.finish(), false);

    const loader = fast.host.smileStartup;
    fast.element("-logo").decode = () => Promise.reject(new Error("repeat decode fault"));
    const rejected = loader.begin();
    let completions = 0;
    rejected.then(() => completions++);
    await fast.frame(0);
    assert.equal(await rejected, false, "repeat decode failure settles instead of hanging");
    assert.equal(await loader.finish(), false);
    assert.equal(fast.element("").hidden, true, "old scene becomes usable again");
    assert.equal(fast.pending(), 0);
    assert.equal(completions, 1);

    let staleDecode;
    fast.element("-logo").decode = () => new Promise(resolve => { staleDecode = resolve; });
    const cancelled = loader.begin();
    await fast.frame(0);
    fast.element("-cancel").click();
    assert.equal(await cancelled, false);
    assert.equal(loader.cancel(), false, "cancellation is idempotent");
    assert.equal(fast.pending(), 0);
    fast.element("-logo").decode = () => Promise.resolve();
    const retry = loader.begin();
    staleDecode();
    await fast.frame(0); await fast.frame(16); await fast.frame(16);
    assert.equal(await retry, true, "retry admits a fresh presentation");
    assert.equal(loader.cancel(), false, "an admitted scene cannot bypass branding");
    const completion = loader.finish();
    assert.equal(loader.finish(), completion);
    await fast.frame(999);
    assert.equal(fast.element("").hidden, false, "stale completion cannot hide a retry");
    await fast.frame(1);
    assert.equal(await completion, true);
    assert.equal(fast.pending(), 0);

    fast.element("-logo").decode = () => new Promise(() => {});
    const timeout = loader.begin(); await fast.frame(30000);
    assert.equal(await timeout, false, "a decoder that never settles has a bounded failure");
    assert.equal(fast.pending(), 0);
    const closing = loader.begin(); fast.event("pagehide");
    assert.equal(await closing, false);
    assert.equal(fast.pending(), 0);
    console.log("PASS (Node logic): visible minimum, hidden initial/repeat admission, interrupted presentation, eligible decode deadline, progress, failure, cancellation, stale callback and retry.");
})().catch(error => { console.error(error); process.exitCode = 1; });
