import { renderChart } from "/js/chart.js";
import { renderSummary } from "/js/summary.js";
import { initSearch } from "/js/search.js";
import { availableRanges, hoursInRange, columnsForRange } from "/js/range.js";

const statusElement = document.getElementById("status");

// Redraw the chart when the window is resized.
let lastReport = null;
let resizeTimer = null;
window.addEventListener("resize", () => {
  clearTimeout(resizeTimer);
  resizeTimer = setTimeout(() => { if (lastReport) render(); }, 150);
});

// Range chips: Today, Tomorrow, the next few days and 7 Days. The selected range is kept in the URL.
const rangesElement = document.getElementById("ranges");
let selectedRange = new URL(window.location.href).searchParams.get("range") ?? "today";

function render() {
  const ranges = availableRanges(lastReport.hourly);
  const range = ranges.find(r => r.key === selectedRange) ?? ranges[0];
  selectedRange = range.key;
  rangesElement.replaceChildren();
  ranges.forEach(r => {
    const chip = document.createElement("button");
    chip.type = "button";
    chip.className = r.key === range.key ? "range-chip range-chip-selected" : "range-chip";
    chip.textContent = r.label;
    chip.addEventListener("click", () => {
      selectedRange = r.key;
      const url = new URL(window.location.href);
      if (r.key === "today") url.searchParams.delete("range"); else url.searchParams.set("range", r.key);
      history.replaceState(null, "", url);
      render();
    });
    rangesElement.appendChild(chip);
  });
  const hours = hoursInRange(lastReport.hourly, range);
  renderSummary(hours, lastReport.daylight, range);
  if (!renderChart(columnsForRange(hours, range), lastReport.daylight, range.stepHours)) {
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
