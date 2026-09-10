// Runs in the loader document before the program/runtime scripts are loaded.
// Each presentation owns its promises and callbacks; an aborted repeat never opens a new scene.
window.smileStartup = (() => {
    "use strict";
    const screen = document.getElementById("smile-loading");
    const logo = document.getElementById("smile-loading-logo");
    const status = document.getElementById("smile-loading-status");
    const detail = document.getElementById("smile-loading-detail");
    const overall = document.getElementById("smile-loading-progress");
    const transfer = document.getElementById("smile-loading-transfer");
    const transferText = document.getElementById("smile-loading-transfer-text");
    const cancelButton = document.getElementById("smile-loading-cancel");
    let current, knownFiles = 0;

    function stopCallbacks(cycle) {
        clearTimeout(cycle.timeout);
        for (const id of cycle.frames) cancelAnimationFrame(id);
        cycle.frames.clear();
    }
    function settle(cycle, success) {
        if (cycle.terminal) return;
        cycle.terminal = true;
        stopCallbacks(cycle);
        cycle.resolvePainted(success);
        cycle.resolveFinished(success);
        if (cycle !== current) return;
        cancelButton.hidden = true;
        if (success || cycle.repeat) screen.hidden = true;
    }
    function fail(cycle, reason) {
        if (cycle !== current || cycle.terminal) return;
        status.textContent = "Unable to load the SMILE logo";
        detail.textContent = reason;
        settle(cycle, false);
    }
    function frame(cycle, callback) {
        const id = requestAnimationFrame(now => {
            cycle.frames.delete(id);
            if (cycle === current && !cycle.terminal) callback(now);
        });
        cycle.frames.add(id);
    }
    function watchDecode(cycle) {
        clearTimeout(cycle.timeout);
        const now = performance.now();
        if (cycle.decodeSince !== null) cycle.decodeRemaining -= Math.max(0, now - cycle.decodeSince);
        cycle.decodeSince = null;
        if (cycle.decoded || cycle.terminal || document.hidden) return;
        cycle.decodeSince = now;
        cycle.timeout = setTimeout(() => fail(cycle, "Logo loading timed out. Retry the tab or reload the page."),
            Math.max(0, cycle.decodeRemaining));
    }
    function start(repeat) {
        const cycle = { repeat, terminal: false, ready: false, presented: false,
            decoded: false, decodeRemaining: 30000, decodeSince: null, visibleFrames: 0,
            visibleMs: 0, visibleSince: null, frames: new Set() };
        cycle.painted = new Promise(resolve => { cycle.resolvePainted = resolve; });
        cycle.finished = new Promise(resolve => { cycle.resolveFinished = resolve; });
        current = cycle;
        knownFiles = 0;
        screen.hidden = false;
        cancelButton.hidden = !repeat;
        // Charge only eligible decode time. Successful decode ends this deadline;
        // a healthy image may then wait for visible presentation without failing.
        watchDecode(cycle);
        Promise.resolve().then(() => logo.decode()).then(() => {
            if (cycle !== current || cycle.terminal) return;
            cycle.decoded = true;
            cycle.decodeSince = null;
            clearTimeout(cycle.timeout);
            function present() {
                cycle.visibleFrames = document.hidden ? 0 : cycle.visibleFrames + 1;
                if (cycle.visibleFrames < 2) { frame(cycle, present); return; }
                cycle.presented = true;
                cycle.visibleSince = performance.now();
                clearTimeout(cycle.timeout);
                cancelButton.hidden = true;
                cycle.resolvePainted(true);
            }
            frame(cycle, present);
        }).catch(() => fail(cycle, "Logo decoding failed. Retry the tab or reload the page."));
        return cycle.painted;
    }
    document.addEventListener("visibilitychange", () => {
        const cycle = current;
        if (cycle.terminal) return;
        watchDecode(cycle);
        if (!cycle.presented) cycle.visibleFrames = 0;
        if (!cycle.presented || cycle.terminal) return;
        if (cycle.visibleSince !== null) cycle.visibleMs += Math.max(0, performance.now() - cycle.visibleSince);
        cycle.visibleSince = document.hidden ? null : performance.now();
    });
    function cancel() {
        // Once loading was admitted, the caller may be replacing assets: preserve the minimum.
        if (!current.repeat || current.presented || current.terminal) return false;
        settle(current, false);
        return true;
    }
    cancelButton.addEventListener("click", cancel);
    window.addEventListener("pagehide", () => settle(current, false));
    start(false);

    function begin() {
        if (!current.terminal) return current.painted;
        status.textContent = "Preparing the next scene…";
        detail.textContent = "Loading duration is not yet known.";
        overall.removeAttribute("value");
        transfer.hidden = true;
        transferText.textContent = "Identifying files to load…";
        return start(true);
    }

    function finish() {
        const cycle = current;
        if (cycle.ready || cycle.terminal) return cycle.finished;
        cycle.ready = true;
        status.textContent = "Ready";
        detail.textContent = "Opening the program…";
        if (!knownFiles) { overall.max = 1; overall.value = 1; }
        transfer.hidden = true;
        transferText.textContent = "Startup preparation complete.";
        void cycle.painted.then(shown => {
            if (!shown || cycle !== current || cycle.terminal) return;
            function tick(now) {
                const elapsed = cycle.visibleMs + (cycle.visibleSince === null ? 0 : Math.max(0, now - cycle.visibleSince));
                if (!document.hidden && elapsed >= 1000) settle(cycle, true);
                else frame(cycle, tick);
            }
            frame(cycle, tick);
        });
        return cycle.finished;
    }

    function update(assets) {
        if (current.ready || current.terminal) return;
        const entries = Array.from(assets.entries());
        const complete = entries.filter(([, item]) => item.state === "ready").length;
        const failed = entries.filter(([, item]) => item.state === "failed").length;
        const pending = entries.filter(([, item]) => item.state === "loading" || item.state === "decoding");
        knownFiles = entries.length;
        if (knownFiles) {
            overall.max = knownFiles;
            overall.value = complete;
            status.textContent = `Files: ${complete} / ${knownFiles} ready${failed ? `, ${failed} failed` : ""}`;
        } else {
            overall.removeAttribute("value");
            status.textContent = "Identifying files to load…";
        }
        detail.textContent = pending.length
            ? `${pending.length} asset${pending.length === 1 ? "" : "s"} downloading or decoding. File total grows as dependencies are identified.`
            : "Preparing the scene. File total includes known load dependencies.";
        const download = pending[pending.length - 1];
        transfer.hidden = !download;
        if (!download) {
            transferText.textContent = failed ? "Some assets failed; the program may report recovery details." : "No active asset download.";
            return;
        }
        const [path, item] = download;
        if (item.state === "decoding") {
            transfer.removeAttribute("value");
            transferText.textContent = `Current asset: ${path} — download complete; decoding`;
            return;
        }
        const bytes = Number(item.received) || 0;
        const total = Number(item.total) || 0;
        if (total > 0 && bytes <= total) {
            transfer.max = total;
            transfer.value = bytes;
            transferText.textContent = `Current asset: ${path} — ${bytes.toLocaleString()} / ${total.toLocaleString()} bytes (${Math.floor(bytes / total * 100)}%)`;
        } else {
            transfer.removeAttribute("value");
            transferText.textContent = `Current asset: ${path} — ${bytes.toLocaleString()} bytes received; total size unknown or decoding`;
        }
    }
    return { get painted() { return current.painted; }, begin, finish, update, cancel };
})();
