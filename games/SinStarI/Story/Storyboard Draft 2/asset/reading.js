const contents = document.querySelector('.contents-shell');
const narrow = window.matchMedia('(max-width: 900px)');
function sizeContents() { contents.open = !narrow.matches; }
sizeContents();
narrow.addEventListener('change', sizeContents);
contents.addEventListener('click', (event) => {
  if (narrow.matches && event.target.closest('a')) contents.open = false;
});
document.addEventListener('keydown', (event) => {
  if (event.key === 'Escape' && contents.open) {
    contents.open = false;
    contents.querySelector('summary').focus();
  }
});
function revealDraftNotes() {
  if (location.hash === '#canon') document.getElementById('canon').open = true;
}
window.addEventListener('hashchange', revealDraftNotes);
revealDraftNotes();
