import { renderChart } from "/js/chart.js";
import { renderSummary } from "/js/summary.js";
import { initSearch } from "/js/search.js";
import { availableRanges, hoursInRange, columnsForRange } from "/js/range.js";

const statusElement = document.getElementById("status");

// The week forecast (days and 7 Days) loads straight away; the month forecast only when 30 Days is first picked.
let lastReport = null;
let monthReport = null;
let monthLoading = null;
let lastLocation = null;

// Redraw the chart when the window is resized.
let resizeTimer = null;
window.addEventListener("resize", () => {
  clearTimeout(resizeTimer);
  resizeTimer = setTimeout(() => { if (lastReport) render(); }, 150);
});

// Range chips: Today, Tomorrow, the next few days and 7 Days. The selected range is kept in the URL.
const rangesElement = document.getElementById("ranges");
let selectedRange = new URL(window.location.href).searchParams.get("range") ?? "today";

// Close the range dropdown when tapping anywhere outside it (pointer events rather than focus, as Safari doesn't focus tapped buttons).
let closeRangeMenu = () => {};
document.addEventListener("pointerdown", e => closeRangeMenu(e.target));

function selectRange(key) {
  selectedRange = key;
  const url = new URL(window.location.href);
  if (key === "today") url.searchParams.delete("range"); else url.searchParams.set("range", key);
  history.replaceState(null, "", url);
  render();
}

// Source list at the bottom: a tick if the source fed this view, a cross if it failed or isn't used for it.
const sourcesElement = document.getElementById("sources");
const rangeNames = { Week: "Days and 7 Days", Month: "30 Days" };

function renderSources(sources) {
  sourcesElement.replaceChildren();
  sourcesElement.appendChild(Object.assign(document.createElement("span"), { className: "sources-title", textContent: "Sources" }));
  sources.forEach(source => {
    const item = document.createElement("span");
    item.className = `source source-${source.status === "Used" ? "used" : source.status === "Failed" ? "failed" : "not-used"}`;
    item.appendChild(Object.assign(document.createElement("span"), { className: "source-icon", textContent: source.status === "Used" ? "✓" : "✗" }));
    item.appendChild(document.createTextNode(source.source));
    const ranges = source.ranges.map(r => rangeNames[r] ?? r).join(", ");
    item.title = source.status === "Used" ? `Used for this view (${ranges})` : source.status === "Failed" ? `Failed: ${source.error}` : `Not used for this view (only ${ranges})`;
    sourcesElement.appendChild(item);
  });
  sourcesElement.hidden = false;
}

// Fetches the 30 day forecast for the current place, once.
function loadMonth() {
  if (!monthLoading) {
    const location = lastLocation;
    const { latitude, longitude } = location;
    monthLoading = fetch(`?handler=forecast&latitude=${latitude}&longitude=${longitude}&range=month`).then(response => {
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
      return response.json();
    }).then(report => {
      // Ignore a late response if the place has changed since.
      if (location !== lastLocation) return;
      monthReport = report;
      render();
    }).catch(error => {
      monthLoading = null;
      statusElement.hidden = false;
      statusElement.textContent = `Couldn't load the 30 day forecast (${error.message}).`;
    });
  }
}

function render() {
  const ranges = availableRanges(lastReport.hourly);
  const range = ranges.find(r => r.key === selectedRange) ?? ranges[0];
  selectedRange = range.key;
  // Chips for wide screens and a custom dropdown for phones; CSS shows whichever fits.
  rangesElement.replaceChildren();
  const menu = document.createElement("div");
  menu.className = "range-menu";
  const button = document.createElement("button");
  button.type = "button";
  button.className = "range-menu-button";
  button.setAttribute("aria-haspopup", "listbox");
  button.setAttribute("aria-expanded", "false");
  button.appendChild(Object.assign(document.createElement("span"), { textContent: range.label }));
  button.insertAdjacentHTML("beforeend", '<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M6 9l6 6 6-6" /></svg>');
  const list = document.createElement("ul");
  list.className = "range-menu-list";
  list.setAttribute("role", "listbox");
  list.hidden = true;
  const setOpen = open => {
    list.hidden = !open;
    button.setAttribute("aria-expanded", String(open));
    menu.classList.toggle("range-menu-open", open);
  };
  button.addEventListener("click", () => setOpen(list.hidden));
  closeRangeMenu = target => { if (!menu.contains(target)) setOpen(false); };
  menu.addEventListener("keydown", e => { if (e.key === "Escape") { setOpen(false); button.focus(); } });
  ranges.forEach(r => {
    const chip = document.createElement("button");
    chip.type = "button";
    chip.className = r.key === range.key ? "range-chip range-chip-selected" : "range-chip";
    chip.textContent = r.label;
    chip.addEventListener("click", () => selectRange(r.key));
    rangesElement.appendChild(chip);
    const item = document.createElement("li");
    const option = document.createElement("button");
    option.type = "button";
    option.className = r.key === range.key ? "range-menu-option range-menu-option-selected" : "range-menu-option";
    option.setAttribute("role", "option");
    option.setAttribute("aria-selected", String(r.key === range.key));
    option.textContent = r.label;
    option.addEventListener("click", () => selectRange(r.key));
    item.appendChild(option);
    list.appendChild(item);
  });
  menu.appendChild(button);
  menu.appendChild(list);
  rangesElement.appendChild(menu);
  const report = range.source === "Month" ? monthReport : lastReport;
  if (!report) {
    statusElement.hidden = false;
    statusElement.textContent = "Loading 30 Day Forecast…";
    loadMonth();
    return;
  }
  renderSources(report.sources);
  const hours = hoursInRange(report.hourly, range);
  renderSummary(hours, report.daylight, range);
  if (!renderChart(columnsForRange(hours, range), report.daylight, range.stepHours)) {
    statusElement.hidden = false;
    statusElement.textContent = "No forecast data available for this range.";
  } else {
    statusElement.hidden = true;
  }
}

// Current place, kept in the URL so it can be bookmarked.
const placeNameElement = document.getElementById("place-name");
const placeDetailElement = document.getElementById("place-detail");

function showPlace(name, detail) {
  placeNameElement.textContent = name;
  placeDetailElement.textContent = detail ?? "";
  document.title = `${name} - PourCast`;
}

function savePlace(place) {
  const url = new URL(window.location.href);
  url.searchParams.set("lat", place.latitude.toFixed(4));
  url.searchParams.set("lon", place.longitude.toFixed(4));
  url.searchParams.set("name", place.name);
  if (place.detail) url.searchParams.set("detail", place.detail); else url.searchParams.delete("detail");
  history.replaceState(null, "", url);
}

async function load(latitude, longitude, name, detail) {
  statusElement.hidden = false;
  statusElement.textContent = "Loading Forecast…";
  if (name) showPlace(name, detail);
  try {
    const response = await fetch(`?handler=forecast&latitude=${latitude}&longitude=${longitude}`);
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    const report = await response.json();
    statusElement.hidden = true;
    lastReport = report;
    lastLocation = { latitude, longitude };
    monthReport = null;
    monthLoading = null;
    if (!name) {
      showPlace(report.locationName ?? `${latitude.toFixed(2)}, ${longitude.toFixed(2)}`, "Current Location");
    }
    savePlace({ latitude, longitude, name: placeNameElement.textContent, detail: placeDetailElement.textContent });
    document.getElementById("dashboard").hidden = false;
    render();
  } catch (error) {
    statusElement.hidden = false;
    statusElement.textContent = `Couldn't load the forecast (${error.message}).`;
  }
}

function locate() {
  if (!("geolocation" in navigator)) {
    statusElement.textContent = "Your browser doesn't support location. Search for a place instead.";
    return;
  }
  statusElement.hidden = false;
  statusElement.textContent = "Finding Your Location…";
  navigator.geolocation.getCurrentPosition(p => load(p.coords.latitude, p.coords.longitude), e => { statusElement.textContent = `Couldn't get your location (${e.message}). Search for a place instead.`; }, { timeout: 10000, maximumAge: 600000 });
}

initSearch(place => load(place.latitude, place.longitude, place.name, [place.region, place.country].filter(Boolean).join(", ")));
document.getElementById("locate").addEventListener("click", locate);

// Start from the place in the URL if there is one, otherwise the browser location.
const startUrl = new URL(window.location.href);
const startLatitude = parseFloat(startUrl.searchParams.get("lat"));
const startLongitude = parseFloat(startUrl.searchParams.get("lon"));
if (!Number.isNaN(startLatitude) && !Number.isNaN(startLongitude)) {
  load(startLatitude, startLongitude, startUrl.searchParams.get("name") ?? undefined, startUrl.searchParams.get("detail") ?? undefined);
} else {
  locate();
}
