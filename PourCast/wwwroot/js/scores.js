// Expands a source's row to show each metric's error by day.
document.querySelectorAll(".scores-toggle").forEach(button => {
  button.addEventListener("click", () => {
    const open = button.getAttribute("aria-expanded") !== "true";
    button.setAttribute("aria-expanded", String(open));
    button.closest("tbody").querySelectorAll(".scores-detail").forEach(row => row.hidden = !open);
  });
});

// Sorts the sources by a column. Scores start best first, names A to Z, and sources without a score always go last.
const table = document.querySelector(".scores");
if (table) {
  const headers = [...table.querySelectorAll("thead th")];
  const sourceName = group => group.querySelector(".scores-toggle").textContent.trim();
  headers.forEach((header, column) => {
    const button = header.querySelector(".scores-sort");
    button.addEventListener("click", () => {
      const text = button.dataset.type === "text";
      const current = header.getAttribute("aria-sort");
      const ascending = current === "none" ? text : current === "descending";
      headers.forEach(x => x.setAttribute("aria-sort", "none"));
      header.setAttribute("aria-sort", ascending ? "ascending" : "descending");
      const direction = ascending ? 1 : -1;
      const groups = [...table.querySelectorAll("tbody.scores-group")];
      groups.sort((a, b) => {
        if (text) {
          return sourceName(a).localeCompare(sourceName(b)) * direction;
        }
        const first = Number.parseFloat(a.querySelector(".scores-row").children[column].dataset.value);
        const second = Number.parseFloat(b.querySelector(".scores-row").children[column].dataset.value);
        if (Number.isNaN(first) !== Number.isNaN(second)) {
          return Number.isNaN(first) ? 1 : -1;
        }
        return (Number.isNaN(first) ? 0 : (first - second) * direction) || sourceName(a).localeCompare(sourceName(b));
      });
      table.append(...groups);
    });
  });
}
