import { element } from "/js/helpers.js";

const searchInput = document.getElementById("search-input");
const searchResults = document.getElementById("search-results");

// Place search with a short debounce.
let searchTimer = null;
let searchController = null;
let results = [];

function closeResults() {
  searchResults.hidden = true;
  searchResults.replaceChildren();
}

let onChoose = () => {};

function choosePlace(place) {
  closeResults();
  searchInput.value = "";
  searchInput.blur();
  onChoose(place);
}

async function search(query) {
  searchController?.abort();
  searchController = new AbortController();
  try {
    const response = await fetch(`?handler=search&query=${encodeURIComponent(query)}`, { signal: searchController.signal });
    if (!response.ok) return;
    results = await response.json();
    searchResults.replaceChildren();
    if (results.length === 0) {
      searchResults.appendChild(element("li", "search-empty", "No Places Found"));
    }
    results.forEach(place => {
      const item = element("li", "search-result");
      item.appendChild(element("span", "search-result-name", place.name));
      item.appendChild(element("span", "search-result-detail", [place.region, place.country].filter(Boolean).join(", ")));
      item.addEventListener("mousedown", e => { e.preventDefault(); choosePlace(place); });
      searchResults.appendChild(item);
    });
    searchResults.hidden = false;
  } catch (error) {
    if (error.name !== "AbortError") closeResults();
  }
}

// Wires up the search box. The callback receives the chosen place.
export function initSearch(callback) {
  onChoose = callback;
  searchInput.addEventListener("input", () => {
    clearTimeout(searchTimer);
    const query = searchInput.value.trim();
    if (query.length < 2) {
      results = [];
      closeResults();
      return;
    }
    searchTimer = setTimeout(() => search(query), 250);
  });
  searchInput.addEventListener("keydown", e => {
    if (e.key === "Enter" && results.length > 0) choosePlace(results[0]);
    if (e.key === "Escape") closeResults();
  });
  searchInput.addEventListener("blur", () => setTimeout(closeResults, 100));
}
