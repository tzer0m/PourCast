// Expands a source's row to show each metric's error by day.
document.querySelectorAll(".scores-toggle").forEach(button => {
  button.addEventListener("click", () => {
    const open = button.getAttribute("aria-expanded") !== "true";
    button.setAttribute("aria-expanded", String(open));
    button.closest("tbody").querySelectorAll(".scores-detail").forEach(row => row.hidden = !open);
  });
});
