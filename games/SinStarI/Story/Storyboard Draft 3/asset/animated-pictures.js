/* One active preview. Images and navigation work without JavaScript. */
(() => {
  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
  let active = null;

  function stop(item, message = '') {
    if (!item) return;
    item.generation += 1;
    clearTimeout(item.loadingTimer);
    clearTimeout(item.failureTimer);
    item.host.classList.remove('is-playing', 'is-loading');
    item.button.textContent = 'Play Preview';
    item.button.setAttribute('aria-pressed', 'false');
    item.status.textContent = message;
    item.mode = null;
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
    item.button.textContent = 'Stop Preview';
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
    item.video.muted = true;
    item.video.src = item.video.dataset.src;
    try {
      await item.video.play();
      if (generation !== item.generation || active !== item || document.hidden) {
        if (active !== item) item.video.pause();
        return;
      }
      clearTimeout(item.loadingTimer);
      clearTimeout(item.failureTimer);
      item.host.classList.remove('is-loading');
      item.host.classList.add('is-playing');
      item.status.textContent = '';
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
      status: host.querySelector('.animation-status'), generation: 0, mode: null
    };
    item.button.hidden = false;
    host.addEventListener('pointerenter', event => {
      if (event.pointerType !== 'touch') start(item);
    });
    host.addEventListener('pointerleave', () => {
      if (active === item && item.mode === 'auto' && !host.contains(document.activeElement)) stop(item);
    });
    host.addEventListener('focusin', event => {
      // Buttons start only on activation, so a keyboard user can reliably toggle them.
      if (event.target !== item.button) start(item);
    });
    host.addEventListener('focusout', event => {
      if (active === item && !host.contains(event.relatedTarget) && !host.matches(':hover')) stop(item);
    });
    item.button.addEventListener('click', () => {
      if (active === item) stop(item);
      else start(item, true);
    });
    item.video.addEventListener('error', () => {
      if (active === item) stop(item, 'Preview unavailable. The illustration remains available.');
    });
    observer?.observe(host);
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
