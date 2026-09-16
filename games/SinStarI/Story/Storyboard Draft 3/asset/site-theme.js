/* Apply the shared preference before the page is painted. */
(() => {
  let saved = null;
  try { saved = localStorage.getItem('sin-star-theme'); } catch (_) { /* Local files can restrict storage. */ }
  const linked = new URLSearchParams(location.search).get('theme');
  const preference = ['light', 'dark'].includes(linked) ? linked : saved;
  document.documentElement.dataset.theme = ['light', 'dark'].includes(preference)
    ? preference : (matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark');
})();
