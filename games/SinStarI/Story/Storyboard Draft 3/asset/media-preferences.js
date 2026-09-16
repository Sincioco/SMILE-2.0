/* Shared clip audio and remembered variations, including file:// view links. */
(() => {
  const key = 'sin-star-media';
  const listeners = new Set();
  let storageAvailable = true;
  function read(value) {
    try {
      const parsed = JSON.parse(value || '{}');
      const choices = Object.fromEntries(Object.entries(parsed.choices || {}).filter(([id, clip]) =>
        /^[a-z0-9-]+$/.test(id) && typeof clip === 'string' && /^[a-z0-9-]+$/.test(clip)));
      return {audio: parsed.audio === true, choices};
    } catch (_) { return {audio:false, choices:{}}; }
  }
  let state = read(null);
  try { state = read(localStorage.getItem(key)); } catch (_) { storageAvailable = false; }
  const query = new URLSearchParams(location.search);
  if (['on', 'off'].includes(query.get('audio'))) state.audio = query.get('audio') === 'on';
  if (query.has('clips')) state.choices = read(JSON.stringify({choices:readChoices(query.get('clips'))})).choices;
  function readChoices(value) { try { return JSON.parse(value); } catch (_) { return {}; } }

  function addPreferences(url) {
    url.searchParams.set('audio', state.audio ? 'on' : 'off');
    url.searchParams.set('clips', JSON.stringify(state.choices));
  }
  function publish(kind, id, save = true) {
    if (save) {
      try { localStorage.setItem(key, JSON.stringify(state)); } catch (_) { storageAvailable = false; }
    }
    document.querySelectorAll('a[href]').forEach(link => {
      const raw = link.getAttribute('href');
      if (!raw || raw.startsWith('#')) return;
      const url = new URL(raw, location.href);
      if (url.origin !== location.origin || !/\/(index|movies|Sin-Star-I-Game-Script-v0\.2)\.html$/.test(url.pathname)) return;
      addPreferences(url);
      link.href = url.href;
    });
    try {
      const url = new URL(location.href);
      addPreferences(url);
      history.replaceState(null, '', url);
    } catch (_) { /* View links also carry preferences when file history is restricted. */ }
    button.textContent = state.audio ? 'Audio On' : 'Audio Off';
    button.setAttribute('aria-pressed', String(state.audio));
    button.setAttribute('aria-label', state.audio ? 'Turn clip audio off' : 'Turn clip audio on');
    listeners.forEach(listener => listener({kind, id}));
  }
  const button = document.getElementById('site-audio-toggle');
  button.hidden = false;
  window.SinStarMedia = {
    get audioEnabled() { return state.audio; },
    get storageAvailable() { return storageAvailable; },
    remembered: id => state.choices[id],
    remember(id, clip) {
      if (clip) state.choices[id] = clip;
      else delete state.choices[id];
      publish('choices', id);
    },
    subscribe: listener => listeners.add(listener)
  };
  button.addEventListener('click', () => { state.audio = !state.audio; publish('audio'); });
  window.addEventListener('storage', event => {
    if (event.key !== key) return;
    state = read(event.newValue);
    publish('external', null, false);
  });
  publish('initial');
})();
