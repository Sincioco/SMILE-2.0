// Keep the contents list out of the way after a reader chooses a section.
const contents = document.querySelector('.chapters');
contents.addEventListener('click', (event) => {
  if (event.target.closest('a')) contents.open = false;
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
