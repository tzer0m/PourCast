import { renderChart } from "/js/chart.js";
import { renderSummary } from "/js/summary.js";
import { initSearch } from "/js/search.js";
import { availableRanges, hoursInRange, columnsForRange } from "/js/range.js";

const statusElement = document.getElementById("status");
const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;

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

// Keeps the selected range in the URL.
function saveRange(key) {
  const url = new URL(window.location.href);
  if (key === "today") url.searchParams.delete("range"); else url.searchParams.set("range", key);
  history.replaceState(null, "", url);
}

function selectRange(key) {
  const day = dayView?.ranges.find(r => r.key === key);
  // Days share one continuous chart, so picking another day scrolls to it rather than redrawing.
  if (day) {
    showDay(day);
    scrollChart(dayView.starts.get(key) * dayView.columnWidth, "smooth");
    return;
  }
  selectedRange = key;
  saveRange(key);
  render();
}

// The single day views are drawn as one continuous chart that scrolls sideways from day to day.
// Holds the day ranges, the column each starts at and the column width, or null for the 7 and 30 day views.
const scrollElement = document.getElementById("strip-scroll");
let dayView = null;
let chipElements = new Map();
let menuLabel = null;
let setMenuOpen = () => {};

// Marks a day as selected and shows its summary, without redrawing the chart.
function showDay(day) {
  selectedRange = day.key;
  saveRange(day.key);
  setMenuOpen(false);
  menuLabel.textContent = day.label;
  chipElements.forEach(({ chip, option }, key) => {
    chip.classList.toggle("range-chip-selected", key === day.key);
    option.classList.toggle("range-menu-option-selected", key === day.key);
    option.setAttribute("aria-selected", String(key === day.key));
  });
  renderSummary(hoursInRange(lastReport.hourly, day), lastReport.daylight, day);
}

// Scrolls the chart without the scroll changing the selected day. The day is followed again once scrolling stops.
let followingScroll = true;
let scrollIdleTimer = null;
function scrollChart(left, behavior = "auto") {
  followingScroll = false;
  clearTimeout(scrollIdleTimer);
  scrollIdleTimer = setTimeout(() => followingScroll = true, 150);
  scrollElement.scrollTo({ left, behavior });
}

// Selects whichever day is in the middle of the chart as it's scrolled.
let scrollFrame = null;
scrollElement.addEventListener("scroll", () => {
  if (!followingScroll) {
    clearTimeout(scrollIdleTimer);
    scrollIdleTimer = setTimeout(() => followingScroll = true, 150);
    return;
  }
  if (!dayView || scrollFrame) return;
  scrollFrame = requestAnimationFrame(() => {
    scrollFrame = null;
    const column = Math.floor((scrollElement.scrollLeft + scrollElement.clientWidth / 2) / dayView.columnWidth);
    const day = dayView.ranges.filter(r => dayView.starts.get(r.key) <= column).pop() ?? dayView.ranges[0];
    if (day.key !== selectedRange) showDay(day);
  });
});

// Mouse drag to scroll the chart, like swiping on a touch screen.
let drag = null;
scrollElement.addEventListener("pointerdown", e => {
  if (e.pointerType !== "mouse" || e.button !== 0 || scrollElement.scrollWidth <= scrollElement.clientWidth) return;
  drag = { x: e.clientX, left: scrollElement.scrollLeft };
  scrollElement.setPointerCapture(e.pointerId);
  scrollElement.classList.add("strip-scroll-dragging");
});
scrollElement.addEventListener("pointermove", e => {
  if (drag) scrollElement.scrollLeft = drag.left - (e.clientX - drag.x);
});
const endDrag = () => {
  drag = null;
  scrollElement.classList.remove("strip-scroll-dragging");
};
scrollElement.addEventListener("pointerup", endDrag);
scrollElement.addEventListener("pointercancel", endDrag);
// Stops the browser dragging the condition icons as images instead.
scrollElement.addEventListener("dragstart", e => e.preventDefault());

// On wide screens the page doesn't scroll, so a mouse wheel over the day chart scrolls it sideways instead.
scrollElement.addEventListener("wheel", e => {
  if (!dayView || window.matchMedia("(max-width: 900px)").matches || Math.abs(e.deltaX) >= Math.abs(e.deltaY)) return;
  scrollElement.scrollLeft += e.deltaMode === 1 ? e.deltaY * 16 : e.deltaY;
  e.preventDefault();
}, { passive: false });

// Source list at the bottom: a tick if the source fed this view, a cross if it failed or isn't used for it.
const sourcesElement = document.getElementById("sources");
const rangeNames = { Week: "Days and 7 Days", Month: "30 Days" };

function renderSources(sources) {
  sourcesElement.replaceChildren();
  sourcesElement.appendChild(Object.assign(document.createElement("span"), { className: "sources-title", textContent: "Sources" }));
  sources.forEach(source => {
    const item = document.createElement("span");
    item.className = `source source-${source.status === "Used" ? "used" : source.status === "Failed" || source.status === "LimitReached" || source.status === "RateLimited" ? "failed" : "not-used"}`;
    item.appendChild(Object.assign(document.createElement("span"), { className: "source-icon", textContent: source.status === "Used" ? "✓" : "✗" }));
    item.appendChild(document.createTextNode(source.source));
    // Today's usage against the daily limit, e.g. "12%".
    const usage = source.dailyLimit ? `${source.callsToday} of ${source.dailyLimit} calls today` : `${source.callsToday} calls today, no limit`;
    if (source.dailyLimit) {
      const percent = Math.round(source.callsToday / source.dailyLimit * 100);
      item.appendChild(Object.assign(document.createElement("span"), { className: percent >= 90 ? "source-usage source-usage-high" : "source-usage", textContent: `${percent}%` }));
    }
    const ranges = source.ranges.map(r => rangeNames[r] ?? r).join(", ");
    const state = source.status === "Used" ? `Used for this view (${ranges})` : source.status === "Failed" ? `Failed: ${source.error}` : source.status === "LimitReached" ? "Daily limit reached" : source.status === "RateLimited" ? source.error : `Not used for this view (only ${ranges})`;
    // Values dropped as outliers, e.g. "Excluded 14 values: Humidity 10, Rain 4".
    const metricNames = { Temperature: "Temp", FeelsLike: "Feels Like", Humidity: "Humidity", WindSpeed: "Wind", WindGust: "Gusts", Precipitation: "Rain", PrecipitationProbability: "Rain Chance", UvIndex: "UV", UvIndexClearSky: "Clear Sky UV" };
    const excluded = Object.entries(source.exclusions ?? {}).sort((a, b) => b[1] - a[1]);
    const total = excluded.reduce((sum, [, count]) => sum + count, 0);
    const exclusionText = total > 0 ? `\nExcluded ${total} values: ${excluded.map(([metric, count]) => `${metricNames[metric] ?? metric} ${count}`).join(", ")}` : "";
    item.title = `${state}\n${usage}${exclusionText}`;
    sourcesElement.appendChild(item);
  });
  sourcesElement.hidden = false;
}

// Fetches the 30 day forecast for the current place, once.
function loadMonth() {
  if (!monthLoading) {
    const location = lastLocation;
    const { latitude, longitude } = location;
    monthLoading = fetch(`?handler=forecast&latitude=${latitude}&longitude=${longitude}&range=month&timeZone=${encodeURIComponent(timeZone)}`).then(response => {
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
  chipElements = new Map();
  const menu = document.createElement("div");
  menu.className = "range-menu";
  const button = document.createElement("button");
  button.type = "button";
  button.className = "range-menu-button";
  button.setAttribute("aria-haspopup", "listbox");
  button.setAttribute("aria-expanded", "false");
  menuLabel = Object.assign(document.createElement("span"), { textContent: range.label });
  button.appendChild(menuLabel);
  button.insertAdjacentHTML("beforeend", '<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M6 9l6 6 6-6" /></svg>');
  const list = document.createElement("ul");
  list.className = "range-menu-list";
  list.setAttribute("role", "listbox");
  list.hidden = true;
  setMenuOpen = open => {
    list.hidden = !open;
    button.setAttribute("aria-expanded", String(open));
    menu.classList.toggle("range-menu-open", open);
  };
  button.addEventListener("click", () => setMenuOpen(list.hidden));
  closeRangeMenu = target => { if (!menu.contains(target)) setMenuOpen(false); };
  menu.addEventListener("keydown", e => { if (e.key === "Escape") { setMenuOpen(false); button.focus(); } });
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
    chipElements.set(r.key, { chip, option });
  });
  menu.appendChild(button);
  menu.appendChild(list);
  rangesElement.appendChild(menu);
  dayView = null;
  const report = range.source === "Month" ? monthReport : lastReport;
  if (!report) {
    statusElement.hidden = false;
    statusElement.textContent = "Loading 30 Day Forecast…";
    loadMonth();
    return;
  }
  // Hide any loading message before drawing, as the chart sizes itself to the space left on the page.
  statusElement.hidden = true;
  renderSources(report.sources);
  renderSummary(hoursInRange(report.hourly, range), report.daylight, range);
  if (range.stepHours === 1) {
    renderDays(report, ranges.filter(r => r.stepHours === 1), range);
    return;
  }
  const chart = renderChart(columnsForRange(report, range), report.daylight, range.stepHours);
  if (!chart) {
    statusElement.hidden = false;
    statusElement.textContent = "No forecast data available for this range.";
    return;
  }
  scrollChart(chart.nowIndex >= 0 ? chart.nowIndex * chart.columnWidth : 0);
}

// Draws every day as one chart, a day wide on screen, and scrolls to the selected day.
function renderDays(report, days, range) {
  const columns = hoursInRange(report.hourly, { start: days[0].start, end: days[days.length - 1].end });
  const chart = renderChart(columns, report.daylight, 1, 24);
  if (!chart) {
    statusElement.hidden = false;
    statusElement.textContent = "No forecast data available for this range.";
    return;
  }
  const starts = new Map(days.map(d => [d.key, Math.max(0, columns.findIndex(c => new Date(c.time) >= d.start))]));
  dayView = { ranges: days, starts, columnWidth: chart.columnWidth };
  // On phones a day is wider than the screen, so today opens at the current hour rather than at 6am.
  const dayFits = chart.columnWidth * 24 <= scrollElement.clientWidth + 1;
  const column = range.key === "today" && !dayFits && chart.nowIndex >= 0 ? chart.nowIndex : starts.get(range.key);
  scrollChart(column * chart.columnWidth);
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
    const response = await fetch(`?handler=forecast&latitude=${latitude}&longitude=${longitude}&timeZone=${encodeURIComponent(timeZone)}`);
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
