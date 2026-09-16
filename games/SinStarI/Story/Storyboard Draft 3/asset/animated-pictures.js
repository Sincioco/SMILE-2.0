/* One active preview. Images and navigation work without JavaScript. */
(() => {
  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
  const preferences = window.SinStarMedia;
  const items = [];
  let active = null;

  function buttonLabel(item, text) {
    item.button.textContent = text;
    item.button.setAttribute('aria-label', text + ': ' + item.button.dataset.caption);
  }

  function stop(item, message = '') {
    if (!item) return;
    item.generation += 1;
    clearTimeout(item.loadingTimer);
    clearTimeout(item.failureTimer);
    item.host.classList.remove('is-playing', 'is-loading');
    buttonLabel(item, 'Play Preview');
    item.button.setAttribute('aria-pressed', 'false');
    item.status.textContent = message;
    item.mode = null;
    item.audioBlocked = false;
    if (active === item) active = null;
    item.video.pause();
    item.video.removeAttribute('src');
    item.video.load();
  }

  async function start(item, explicit = false) {
    if (!explicit && reducedMotion.matches) return;
    if (active === item) return;
    stop(active);
    active = item;
    item.mode = explicit ? 'manual' : 'auto';
    const generation = ++item.generation;
    item.status.textContent = '';
    buttonLabel(item, 'Stop Preview');
    item.button.setAttribute('aria-pressed', 'true');
    item.loadingTimer = setTimeout(() => {
      if (active === item) {
        item.host.classList.add('is-loading');
        item.status.textContent = 'Loading preview…';
      }
    }, 350);
    item.failureTimer = setTimeout(() => {
      if (active === item && !item.host.classList.contains('is-playing')) {
        stop(item, 'Preview unavailable. Try Play Preview again.');
      }
    }, 12000);
    item.audioBlocked = false;
    item.video.muted = !preferences.audioEnabled;
    item.video.src = item.video.dataset.src;
    try {
      try {
        await item.video.play();
      } catch (error) {
        if (error.name !== 'NotAllowedError' || generation !== item.generation || active !== item) throw error;
        item.video.muted = true;
        item.audioBlocked = preferences.audioEnabled;
        await item.video.play();
      }
      if (generation !== item.generation || active !== item || document.hidden) {
        if (active !== item) item.video.pause();
        return;
      }
      clearTimeout(item.loadingTimer);
      clearTimeout(item.failureTimer);
      item.host.classList.remove('is-loading');
      item.host.classList.add('is-playing');
      buttonLabel(item, item.audioBlocked ? 'Play With Sound' : 'Stop Preview');
      item.button.setAttribute('aria-pressed', String(!item.audioBlocked));
      item.status.textContent = item.audioBlocked ? 'Click Play With Sound to enable audio.' : '';
    } catch (error) {
      if (generation === item.generation && active === item) {
        stop(item, 'Select Play Preview to try again.');
      }
    }
  }

  const observer = 'IntersectionObserver' in window
    ? new IntersectionObserver(entries => {
      for (const entry of entries) {
        if (!entry.isIntersecting && active?.host === entry.target) stop(active);
      }
    }, {threshold: 0}) : null;

  document.querySelectorAll('.animated-picture').forEach(host => {
    const item = {
      host, video: host.querySelector('video'),
      button: host.querySelector('.animation-toggle'),
      status: host.querySelector('.animation-status'), generation: 0, mode: null,
      audioBlocked:false, id:host.dataset.mediaId,
      choices:[...host.querySelectorAll('[data-clip]')], remember:host.querySelector('.remember-clip')
    };
    items.push(item);
    item.select = (id, play = false) => {
      const choice = item.choices.find(button => button.dataset.clip === id);
      if (!choice) return;
      const wasActive = active === item;
      const explicit = play || item.mode === 'manual';
      if (wasActive) stop(item);
      item.selected = id;
      item.video.dataset.src = choice.dataset.src;
      item.choices.forEach(button => button.setAttribute('aria-pressed', String(button === choice)));
      if (play || wasActive) start(item, explicit);
    };
    if (item.choices.length) {
      const saved = preferences.remembered(item.id);
      const valid = item.choices.some(button => button.dataset.clip === saved);
      item.select(valid ? saved : host.dataset.defaultClip);
      item.remember.checked = valid;
      host.querySelector('.clip-choices').hidden = false;
      const remember = () => {
        preferences.remember(item.id, item.remember.checked ? item.selected : null);
        host.querySelector('.clip-save-status').textContent = item.remember.checked && !preferences.storageAvailable
          ? 'Selection follows view links; browser storage is unavailable.' : '';
      };
      item.choices.forEach(button => button.addEventListener('click', () => {
        item.select(button.dataset.clip, true);
        if (item.remember.checked) remember();
      }));
      item.remember.addEventListener('change', remember);
    }
    item.button.hidden = false;
    host.addEventListener('pointerenter', event => {
      if (event.pointerType !== 'touch') start(item);
    });
    host.addEventListener('pointerleave', () => {
      if (active === item && item.mode === 'auto' && !host.contains(document.activeElement)) stop(item);
    });
    host.addEventListener('focusin', event => {
      // Buttons start only on activation, so a keyboard user can reliably toggle them.
      if (event.target.closest('a')) start(item);
    });
    host.addEventListener('focusout', event => {
      if (active === item && !host.contains(event.relatedTarget) && !host.matches(':hover')) stop(item);
    });
    item.button.addEventListener('click', () => {
      if (active === item && !item.audioBlocked) stop(item);
      else { stop(active); start(item, true); }
    });
    item.video.addEventListener('error', () => {
      if (active === item) stop(item, 'Preview unavailable. The illustration remains available.');
    });
    observer?.observe(host);
  });
  preferences.subscribe(event => {
    if (event.kind === 'choices' || event.kind === 'external') {
      items.filter(item => item.remember && (!event.id || item.id === event.id)).forEach(item => {
        const saved = preferences.remembered(item.id);
        const valid = item.choices.some(button => button.dataset.clip === saved);
        item.remember.checked = valid;
        const selected = valid ? saved : event.kind === 'external' ? item.host.dataset.defaultClip : item.selected;
        if (selected !== item.selected) item.select(selected);
      });
    }
    if (active && (event.kind === 'audio' || event.kind === 'external')) {
      if (preferences.audioEnabled && (active.video.muted || active.audioBlocked)) {
        const item = active;
        const explicit = item.mode === 'manual';
        stop(item);
        start(item, explicit);
      } else if (!preferences.audioEnabled) {
        active.video.muted = true;
        active.audioBlocked = false;
        buttonLabel(active, 'Stop Preview');
        active.button.setAttribute('aria-pressed', 'true');
        active.status.textContent = '';
      }
    }
  });
  document.addEventListener('keydown', event => {
    if (event.key === 'Escape') stop(active);
  });
  document.addEventListener('visibilitychange', () => {
    if (document.hidden) stop(active);
  });
  window.addEventListener('pagehide', () => stop(active));
  reducedMotion.addEventListener('change', () => {
    if (reducedMotion.matches && active?.mode === 'auto') stop(active);
  });
})();
