// Runs in the loader document before the program/runtime scripts are loaded.
// A decoded logo plus two animation frames establishes a presentation boundary.
window.smileStartup = (() => {
    "use strict";
    const screen = document.getElementById("smile-loading");
    const logo = document.getElementById("smile-loading-logo");
    const status = document.getElementById("smile-loading-status");
    const detail = document.getElementById("smile-loading-detail");
    const overall = document.getElementById("smile-loading-progress");
    const transfer = document.getElementById("smile-loading-transfer");
    const transferText = document.getElementById("smile-loading-transfer-text");
    let visibleMs = 0, visibleSince = null, ready = false, presented = false;
    let knownFiles = 0;
    let resolvePainted, resolveFinished;
    const painted = new Promise(resolve => { resolvePainted = resolve; });
    const finished = new Promise(resolve => { resolveFinished = resolve; });
    const nextFrame = () => new Promise(resolve => requestAnimationFrame(resolve));

    document.addEventListener("visibilitychange", () => {
        if (!presented) return;
        if (visibleSince !== null) visibleMs += Math.max(0, performance.now() - visibleSince);
        visibleSince = document.hidden ? null : performance.now();
    });

    async function present() {
        try {
            await logo.decode();
        } catch (_) {
            status.textContent = "Unable to load the SMILE logo";
            detail.textContent = "Check your connection and reload this page.";
            return; // Do not pretend branding was shown or run a broken publication.
        }
        // Do not count background-tab time or script/download time as presentation.
        while (document.hidden) await nextFrame();
        await nextFrame();
        await nextFrame();
        if (document.hidden) { void present(); return; }
        presented = true;
        visibleSince = performance.now();
        resolvePainted();
    }
    void present();

    function finish() {
        if (ready) return finished;
        ready = true;
        status.textContent = "Ready";
        detail.textContent = "Opening the program…";
        if (!knownFiles) {
            overall.max = 1;
            overall.value = 1;
        }
        transfer.hidden = true;
        transferText.textContent = "Startup preparation complete.";
        void painted.then(() => {
            function tick(now) {
                const elapsed = visibleMs + (visibleSince === null ? 0 : Math.max(0, now - visibleSince));
                if (!document.hidden && elapsed >= 1000) {
                    screen.hidden = true;
                    resolveFinished();
                } else requestAnimationFrame(tick);
            }
            requestAnimationFrame(tick);
        });
        return finished;
    }

    function update(assets) {
        if (ready) return;
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
        const current = pending[pending.length - 1];
        transfer.hidden = !current;
        if (!current) {
            transferText.textContent = failed ? "Some assets failed; the program may report recovery details." : "No active asset download.";
            return;
        }
        const [path, item] = current;
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
    return { painted, finish, update };
})();
