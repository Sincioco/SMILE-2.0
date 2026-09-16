/* Shared theme, scene navigation and reading-position links for both views. */
(() => {
  const themeButton = document.getElementById('site-theme-toggle');
  const printButton = document.getElementById('site-print');
  const tabs = [...document.querySelectorAll('[data-view][href]')];
  const sceneLinks = [...document.querySelectorAll('[data-scene-link]')];
  const scenes = sceneLinks.map(link => document.getElementById(link.hash.slice(1))).filter(Boolean);
  let currentScene = null;
  let scheduled = false;
  let headerHeight = 0;
  const header = document.querySelector('.site-header');

  function carryTheme() {
    const theme = document.documentElement.dataset.theme;
    document.querySelectorAll('a[href]').forEach(link => {
      const raw = link.getAttribute('href');
      if (!raw || raw.startsWith('#')) return;
      const target = new URL(raw, location.href);
      if (target.origin !== location.origin) return;
      if (!/\/(index|movies|Sin-Star-I-Game-Script-v0\.2)\.html$/.test(target.pathname)) return;
      target.searchParams.set('theme', theme);
      link.href = target.href;
    });
  }

  function applyTheme(theme) {
    document.documentElement.dataset.theme = theme;
    const dark = theme === 'dark';
    themeButton.textContent = dark ? 'Light Theme' : 'Dark Theme';
    themeButton.setAttribute('aria-pressed', String(dark));
    themeButton.setAttribute('aria-label', dark ? 'Switch to light theme' : 'Switch to dark theme');
    try { localStorage.setItem('sin-star-theme', theme); } catch (_) { /* Links also carry the preference. */ }
    const color = document.querySelector('meta[name="theme-color"]');
    if (color) color.content = dark ? '#0c1422' : '#eef2f7';
    carryTheme();
  }

  themeButton.hidden = false;
  printButton.hidden = false;
  applyTheme(document.documentElement.dataset.theme);
  themeButton.addEventListener('click', () => {
    const theme = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark';
    applyTheme(theme);
    try {
      const url = new URL(location.href);
      url.searchParams.set('theme', theme);
      history.replaceState(null, '', url);
    } catch (_) { /* Some file browsers restrict history changes. */ }
  });
  printButton.addEventListener('click', () => window.print());
  window.addEventListener('storage', event => {
    if (event.key === 'sin-star-theme' && ['dark', 'light'].includes(event.newValue)) applyTheme(event.newValue);
  });

  function updatePosition() {
    scheduled = false;
    const bounds = header.getBoundingClientRect();
    if (bounds.height !== headerHeight) {
      headerHeight = bounds.height;
      document.documentElement.style.setProperty('--site-header-height', headerHeight + 'px');
    }
    const edge = bounds.bottom + 80;
    let selected = null;
    for (const scene of scenes) {
      if (scene.getBoundingClientRect().top > edge) break;
      selected = scene.id;
    }
    if (selected === currentScene) return;
    currentScene = selected;
    sceneLinks.forEach(link => {
      if (link.hash === '#' + selected) link.setAttribute('aria-current', 'location');
      else link.removeAttribute('aria-current');
    });
    tabs.forEach(tab => {
      if (tab.getAttribute('aria-current') === 'page') return;
      const target = new URL(tab.href, location.href);
      target.hash = selected || '';
      tab.href = target.href;
    });
  }
  function schedulePosition() {
    if (!scheduled) {
      scheduled = true;
      requestAnimationFrame(updatePosition);
    }
  }
  window.addEventListener('scroll', schedulePosition, {passive:true});
  window.addEventListener('resize', schedulePosition);
  window.addEventListener('load', schedulePosition);
  if ('ResizeObserver' in window) new ResizeObserver(schedulePosition).observe(header);
  updatePosition();

  const contents = document.getElementById('scene-index');
  if (contents) {
    const filter = document.getElementById('scene-filter');
    const status = document.getElementById('scene-filter-status');
    const groups = [...document.querySelectorAll('.scene-nav-group')];
    const narrow = matchMedia('(max-width: 860px)');
    const sizeContents = () => { contents.open = !narrow.matches; };
    sizeContents();
    narrow.addEventListener('change', sizeContents);
    document.querySelector('.scene-filter-tools').hidden = false;
    filter.addEventListener('input', () => {
      const query = filter.value.trim().toLocaleLowerCase();
      let count = 0;
      groups.forEach(group => {
        const chapterMatches = group.querySelector('summary').textContent.toLocaleLowerCase().includes(query);
        let matching = 0;
        group.querySelectorAll('li').forEach(row => {
          row.hidden = !!query && !chapterMatches && !row.textContent.toLocaleLowerCase().includes(query);
          if (!row.hidden) matching += 1;
        });
        group.hidden = matching === 0;
        if (query && matching) group.open = true;
        count += matching;
      });
      status.textContent = query ? `${count} matching scene${count === 1 ? '' : 's'}.` : '';
    });
    contents.addEventListener('click', event => {
      const link = event.target.closest('a[href^="#"]');
      if (!link) return;
      const target = document.getElementById(link.hash.slice(1));
      if (!target) return;
      if (narrow.matches) contents.open = false;
      target.setAttribute('tabindex', '-1');
      requestAnimationFrame(() => target.focus({preventScroll:true}));
    });
  }
  function revealNotes() {
    if (location.hash === '#canon') {
      const notes = document.getElementById('canon');
      if (notes) notes.open = true;
    }
  }
  window.addEventListener('hashchange', () => { revealNotes(); schedulePosition(); });
  revealNotes();
})();
