(() => {
  "use strict";

  const STORAGE_KEY = "smile-snake-tutorial-completed-v1";
  const NAV_SCROLL_KEY = "smile-snake-sidebar-scroll-v1";
  const WINDOW_STATE_ID = "smile-snake-tutorial-window-state-v1";

  function getCompleted() {
    try {
      const value = JSON.parse(localStorage.getItem(STORAGE_KEY) || "[]");
      return Array.isArray(value) ? value : [];
    } catch {
      return [];
    }
  }

  function setCompleted(items) {
    try { localStorage.setItem(STORAGE_KEY, JSON.stringify(items)); } catch { /* local file privacy mode */ }
  }

  function updateProgress() {
    const completed = new Set(getCompleted());
    const navLinks = [...document.querySelectorAll(".nav-link[data-topic-id]")];
    navLinks.forEach(link => {
      link.classList.toggle("completed", completed.has(link.dataset.topicId));
    });

    const total = Number(document.body.dataset.topicTotal || 0);
    const count = [...completed].filter(id => id !== "home").length;
    const fill = document.querySelector(".progress-fill");
    const label = document.querySelector(".progress-label");
    if (fill && total > 0) fill.style.width = `${Math.min(100, Math.round((count / total) * 100))}%`;
    if (label) label.textContent = `${count} of ${total} topics completed`;

    const currentId = document.body.dataset.topicId;
    const button = document.querySelector(".complete-topic");
    if (button && currentId && currentId !== "home") {
      const done = completed.has(currentId);
      button.classList.toggle("is-complete", done);
      button.textContent = done ? "✓ Topic completed" : "Mark this topic complete";
      button.setAttribute("aria-pressed", String(done));
    }
  }

  function toggleCurrentComplete() {
    const id = document.body.dataset.topicId;
    if (!id || id === "home") return;
    const completed = new Set(getCompleted());
    if (completed.has(id)) completed.delete(id); else completed.add(id);
    setCompleted([...completed]);
    updateProgress();
  }

  function readWindowState() {
    try {
      const parsed = JSON.parse(window.name || "{}");
      if (parsed && parsed.id === WINDOW_STATE_ID) return parsed;
    } catch { /* another page may have used window.name */ }
    return { id: WINDOW_STATE_ID };
  }

  function writeWindowState(patch) {
    try {
      window.name = JSON.stringify({ ...readWindowState(), ...patch, id: WINDOW_STATE_ID });
    } catch { /* ignore */ }
  }

  function getSavedNavScroll() {
    try {
      const stored = sessionStorage.getItem(NAV_SCROLL_KEY);
      if (stored !== null) return Number(stored);
    } catch { /* file URLs may restrict storage */ }
    const state = readWindowState();
    return Number.isFinite(Number(state.navScroll)) ? Number(state.navScroll) : null;
  }

  function saveNavScroll(sidebar) {
    if (!sidebar) return;
    const value = Math.max(0, Math.round(sidebar.scrollTop));
    try { sessionStorage.setItem(NAV_SCROLL_KEY, String(value)); } catch { /* use window.name fallback */ }
    writeWindowState({ navScroll: value });
  }

  function setupSidebarScroll() {
    const sidebar = document.querySelector(".sidebar");
    if (!sidebar) return;

    const saved = getSavedNavScroll();
    window.requestAnimationFrame(() => {
      if (saved !== null) {
        sidebar.scrollTop = saved;
      } else {
        document.querySelector(".nav-link.active")?.scrollIntoView({ block: "nearest" });
      }
    });

    let pending = 0;
    sidebar.addEventListener("scroll", () => {
      if (pending) window.cancelAnimationFrame(pending);
      pending = window.requestAnimationFrame(() => saveNavScroll(sidebar));
    }, { passive: true });

    document.querySelectorAll(".sidebar a").forEach(link => {
      link.addEventListener("pointerdown", () => saveNavScroll(sidebar));
      link.addEventListener("click", () => saveNavScroll(sidebar));
    });
    window.addEventListener("pagehide", () => saveNavScroll(sidebar));
    window.addEventListener("beforeunload", () => saveNavScroll(sidebar));
  }

  async function copyText(text) {
    if (navigator.clipboard && window.isSecureContext) {
      await navigator.clipboard.writeText(text);
      return;
    }
    const area = document.createElement("textarea");
    area.value = text;
    area.setAttribute("readonly", "");
    area.style.position = "fixed";
    area.style.opacity = "0";
    document.body.appendChild(area);
    area.select();
    const ok = document.execCommand("copy");
    area.remove();
    if (!ok) throw new Error("Copy command was not accepted.");
  }

  function setupCopyButtons() {
    document.querySelectorAll(".copy-code").forEach(button => {
      button.addEventListener("click", async () => {
        const panel = button.closest(".code-panel");
        const code = panel?.querySelector("code");
        if (!code) return;
        const original = button.textContent;
        const cleanCode = (code.dataset.raw || code.textContent).replace(/\n$/, "");
        try {
          await copyText(cleanCode);
          button.textContent = "Copied!";
          button.classList.add("copied");
        } catch {
          button.textContent = "Select and copy";
        }
        window.setTimeout(() => {
          button.textContent = original;
          button.classList.remove("copied");
        }, 1600);
      });
    });
  }

  const keywordSet = new Set([
    "AND", "CALL", "CASE", "CENTERED", "CLEAR", "COLOR", "CONST", "DEFAULT", "DIM",
    "DO", "DOWN", "DRAW", "ELSE", "END", "EXIT", "FALSE", "FILL", "FOR", "FROM",
    "FUNCTION", "GAME", "GET", "IF", "INTO", "KEY", "LOAD", "LOOP", "MAX", "MIN",
    "MOD", "MUSIC", "NOT", "NUMBER", "OR", "PAUSE", "PLAY", "RANDOM", "RECTANGLE",
    "RESUME", "RETURN", "RGB", "ROUNDED", "SAVE", "SCREEN", "SHOW", "SIZE", "SOUND",
    "STOP", "SUB", "TEXT", "THEN", "TO", "TRUE", "UNTIL", "VOLUME", "WAIT", "WINDOW"
  ]);

  const constantPattern = /^(KEY_[A-Z0-9_]+|BLACK|WHITE|RED|GREEN|BLUE|CYAN|MAGENTA|YELLOW|ORANGE|GRAY|LIGHT_[A-Z_]+|DARK_[A-Z_]+)$/i;
  const functionPattern = /^(TIMER|ABS|MIN|MAX|RGB|GAME_CLOSED|KEY_HELD)$/i;

  function escapeHtml(value) {
    return value.replace(/[&<>]/g, ch => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;" }[ch]));
  }

  function highlightLine(line) {
    const trimmed = line.trimStart();
    if (trimmed.startsWith("'")) return `<span class="tok-comment">${escapeHtml(line)}</span>`;

    let result = "";
    let i = 0;
    while (i < line.length) {
      const ch = line[i];
      if (ch === '"') {
        let j = i + 1;
        while (j < line.length) {
          if (line[j] === '"') { j += 1; break; }
          j += 1;
        }
        result += `<span class="tok-string">${escapeHtml(line.slice(i, j))}</span>`;
        i = j;
        continue;
      }
      if (/[A-Za-z_]/.test(ch)) {
        let j = i + 1;
        while (j < line.length && /[A-Za-z0-9_]/.test(line[j])) j += 1;
        const word = line.slice(i, j);
        const upper = word.toUpperCase();
        if (keywordSet.has(upper)) result += `<span class="tok-keyword">${word}</span>`;
        else if (functionPattern.test(word)) result += `<span class="tok-function">${word}</span>`;
        else if (constantPattern.test(word)) result += `<span class="tok-constant">${word}</span>`;
        else result += escapeHtml(word);
        i = j;
        continue;
      }
      if (/\d/.test(ch)) {
        let j = i + 1;
        while (j < line.length && /\d/.test(line[j])) j += 1;
        result += `<span class="tok-number">${line.slice(i, j)}</span>`;
        i = j;
        continue;
      }
      result += escapeHtml(ch);
      i += 1;
    }
    return result;
  }

  function addSourceRangeBadge(code, start, lineCount) {
    const panel = code.closest(".code-panel");
    const toolbar = panel?.querySelector(".code-toolbar");
    if (!toolbar || toolbar.querySelector(".source-range-badge")) return;

    const end = start + lineCount - 1;
    const text = start === end ? `Full source line ${start}` : `Full source lines ${start}–${end}`;
    const badge = document.createElement(code.dataset.sourceAnchor === "true" ? "span" : "a");
    badge.className = "source-range-badge";
    badge.textContent = text;
    if (badge instanceof HTMLAnchorElement) {
      badge.href = `19-complete-source.html#source-line-${start}`;
      badge.title = "Open this fragment in the complete source";
    }

    const label = toolbar.querySelector(".code-label");
    if (label) label.insertAdjacentElement("afterend", badge); else toolbar.prepend(badge);
  }

  function highlightCode() {
    document.querySelectorAll("code.language-smile").forEach(code => {
      const raw = code.textContent.replace(/\r\n/g, "\n").replace(/\r/g, "\n");
      code.dataset.raw = raw;
      const lines = raw.split("\n");
      const start = Number(code.dataset.sourceStart || 0);

      if (start > 0) {
        code.classList.add("numbered-code");
        code.innerHTML = lines.map((line, index) => {
          const number = start + index;
          const anchor = code.dataset.sourceAnchor === "true" ? ` id="source-line-${number}"` : "";
          const highlighted = highlightLine(line) || "&nbsp;";
          return `<span class="code-line"${anchor} data-source-line="${number}"><span class="line-number" aria-hidden="true">${number}</span><span class="line-code">${highlighted}</span></span>`;
        }).join("");
        addSourceRangeBadge(code, start, lines.length);
      } else {
        code.innerHTML = lines.map(highlightLine).join("\n");
      }
    });
  }

  function clearSourceHighlights() {
    document.querySelectorAll(".code-line.range-highlight").forEach(line => line.classList.remove("range-highlight"));
  }

  function highlightSourceRange(start, end) {
    clearSourceHighlights();
    for (let line = start; line <= end; line += 1) {
      document.getElementById(`source-line-${line}`)?.classList.add("range-highlight");
    }
  }

  function setupSourceMapLinks() {
    document.querySelectorAll(".source-range-link").forEach(link => {
      link.addEventListener("click", () => {
        highlightSourceRange(Number(link.dataset.lineStart), Number(link.dataset.lineEnd));
      });
    });

    const hashMatch = window.location.hash.match(/^#source-line-(\d+)$/);
    if (!hashMatch) return;
    const line = Number(hashMatch[1]);
    const rangeLink = [...document.querySelectorAll(".source-range-link")].find(link => {
      const start = Number(link.dataset.lineStart);
      const end = Number(link.dataset.lineEnd);
      return line >= start && line <= end;
    });
    if (rangeLink) highlightSourceRange(Number(rangeLink.dataset.lineStart), Number(rangeLink.dataset.lineEnd));

    window.requestAnimationFrame(() => {
      document.getElementById(`source-line-${line}`)?.scrollIntoView({ block: "center" });
    });
  }

  function setupNavigation() {
    const openButton = document.querySelector("[data-open-nav]");
    const scrim = document.querySelector(".nav-scrim");
    const close = () => document.body.classList.remove("nav-open");
    openButton?.addEventListener("click", () => document.body.classList.toggle("nav-open"));
    scrim?.addEventListener("click", close);
    document.querySelectorAll(".sidebar a").forEach(link => link.addEventListener("click", close));
    document.addEventListener("keydown", event => { if (event.key === "Escape") close(); });
  }

  function setupSyntaxSearch() {
    const input = document.querySelector("[data-syntax-search]");
    if (!input) return;
    input.addEventListener("input", () => {
      const query = input.value.trim().toLowerCase();
      document.querySelectorAll(".syntax-card").forEach(card => {
        const haystack = (card.dataset.search || card.textContent).toLowerCase();
        card.classList.toggle("hidden", query.length > 0 && !haystack.includes(query));
      });
    });
  }

  function setupSpeedLab() {
    const root = document.querySelector("[data-speed-lab]");
    if (!root) return;
    const slider = root.querySelector("input[type=range]");
    const scoreOut = root.querySelector("[data-score]");
    const bandOut = root.querySelector("[data-band]");
    const delayOut = root.querySelector("[data-delay]");
    const update = () => {
      const score = Number(slider.value);
      const band = Math.trunc(score / 50);
      const delay = Math.max(45, 100 - band * 4);
      scoreOut.textContent = String(score);
      bandOut.textContent = String(band);
      delayOut.textContent = `${delay} milliseconds`;
    };
    slider.addEventListener("input", update);
    update();
  }

  function setupCoordinateLab() {
    const root = document.querySelector("[data-coordinate-lab]");
    if (!root) return;
    const x = root.querySelector("[data-x-input]");
    const y = root.querySelector("[data-y-input]");
    const xOut = root.querySelector("[data-x-value]");
    const yOut = root.querySelector("[data-y-value]");
    const pxOut = root.querySelector("[data-pixel-x]");
    const pyOut = root.querySelector("[data-pixel-y]");
    const update = () => {
      const xv = Number(x.value);
      const yv = Number(y.value);
      xOut.textContent = String(xv);
      yOut.textContent = String(yv);
      pxOut.textContent = String(20 + xv * 20 + 2);
      pyOut.textContent = String(30 + yv * 20 + 2);
    };
    x.addEventListener("input", update);
    y.addEventListener("input", update);
    update();
  }

  document.addEventListener("DOMContentLoaded", () => {
    setupSidebarScroll();
    highlightCode();
    setupCopyButtons();
    setupSourceMapLinks();
    setupNavigation();
    setupSyntaxSearch();
    setupSpeedLab();
    setupCoordinateLab();
    updateProgress();
    document.querySelector(".complete-topic")?.addEventListener("click", toggleCurrentComplete);
  });
})();
