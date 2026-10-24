import { hourLabel } from "/js/helpers.js";

// Each metric with its field on the mapped hours, display format, and the difference from the median that counts as "far".
const metrics = [
  { key: "temperature", label: "Temperature", unit: "°", digits: 1, far: 3 },
  { key: "feelsLike", label: "Feels Like", unit: "°", digits: 1, far: 4 },
  { key: "precipitation", label: "Rain", unit: " mm", digits: 2, far: 1 },
  { key: "precipitationProbability", label: "Rain Chance", unit: "%", digits: 0, far: 30 },
  { key: "humidity", label: "Humidity", unit: "%", digits: 0, far: 15 },
  { key: "windSpeed", label: "Wind", unit: "", digits: 1, far: 8 },
  { key: "windGust", label: "Gusts", unit: "", digits: 1, far: 12 },
  { key: "windDirection", label: "Wind Direction", unit: "°", digits: 0, far: 60, circular: true },
  { key: "cloudCover", label: "Cloud Cover", unit: "%", digits: 0, far: 30 },
  { key: "uvIndex", label: "UV", unit: "", digits: 1, far: 1.5 },
  { key: "uvIndexClearSky", label: "Clear Sky UV", unit: "", digits: 1, far: 1.5 },
  { key: "condition", label: "Condition", text: true }
];

const params = new URL(window.location.href).searchParams;
const latitude = parseFloat(params.get("lat"));
const longitude = parseFloat(params.get("lon"));
const statusElement = document.getElementById("compare-status");
const table = document.getElementById("compare-table");
const metricSelect = document.getElementById("compare-metric");
const startSelect = document.getElementById("compare-start");
const hoursShown = 24;
let sources = [];

document.getElementById("compare-place").textContent = params.get("name") ?? "";
document.getElementById("compare-back").href = `/${window.location.search}`;

function cell(tag, text, className) {
  const node = document.createElement(tag);
  node.textContent = text;
  if (className) node.className = className;
  return node;
}

function median(values) {
  const sorted = [...values].sort((a, b) => a - b);
  const middle = Math.floor(sorted.length / 2);
  return sorted.length % 2 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
}

// Difference between two compass directions, going the short way round.
function angleDifference(a, b) {
  const difference = Math.abs(a - b) % 360;
  return difference > 180 ? 360 - difference : difference;
}

function render() {
  const metric = metrics.find(m => m.key === metricSelect.value);
  const start = new Date(startSelect.value);
  const times = Array.from({ length: hoursShown }, (_, i) => new Date(start.getTime() + i * 3600000));
  const lookup = sources.map(source => new Map(source.hourly.map(h => [new Date(h.time).getTime(), h])));

  table.replaceChildren();
  const head = document.createElement("tr");
  head.appendChild(cell("th", "Source", "compare-source"));
  times.forEach(t => head.appendChild(cell("th", t.getHours() === 0 ? t.toLocaleDateString("en-GB", { weekday: "short" }) : hourLabel(t))));
  table.appendChild(head);

  // Median per hour, used for shading and for the last row.
  const medians = times.map(t => {
    const values = lookup.map(map => map.get(t.getTime())?.[metric.key]).filter(v => v !== null && v !== undefined);
    return metric.text || values.length === 0 ? null : median(values);
  });

  sources.forEach((source, index) => {
    const row = document.createElement("tr");
    const name = cell("th", source.source, "compare-source");
    if (source.status !== "Used") {
      name.classList.add("compare-failed");
      name.title = source.error ?? source.status;
    }
    row.appendChild(name);
    times.forEach((t, i) => {
      const value = lookup[index].get(t.getTime())?.[metric.key];
      if (value === null || value === undefined) {
        row.appendChild(cell("td", "–", "compare-empty"));
        return;
      }
      if (metric.text) {
        row.appendChild(cell("td", value));
        return;
      }
      const td = cell("td", `${value.toFixed(metric.digits)}${metric.unit}`);
      if (medians[i] !== null) {
        const difference = metric.circular ? angleDifference(value, medians[i]) : Math.abs(value - medians[i]);
        td.style.setProperty("--far", Math.min(1, difference / metric.far));
        td.classList.add("compare-value");
        td.title = `${(value - medians[i] >= 0 ? "+" : "")}${(value - medians[i]).toFixed(metric.digits)} from median`;
      }
      row.appendChild(td);
    });
    table.appendChild(row);
  });

  if (!metric.text) {
    const row = document.createElement("tr");
    row.className = "compare-median";
    row.appendChild(cell("th", "Median", "compare-source"));
    medians.forEach(m => row.appendChild(cell("td", m === null ? "–" : `${m.toFixed(metric.digits)}${metric.unit}`)));
    table.appendChild(row);
  }
}

async function load() {
  if (Number.isNaN(latitude) || Number.isNaN(longitude)) {
    statusElement.textContent = "Open this page from the forecast so it knows the location.";
    return;
  }
  metrics.forEach(m => metricSelect.appendChild(Object.assign(document.createElement("option"), { value: m.key, textContent: m.label })));

  // Start options: 6am today, then every 24 hours for 3 days, plus "now".
  const now = new Date();
  now.setMinutes(0, 0, 0);
  const options = [["Now", now]];
  for (let day = 0; day < 3; day++) {
    const date = new Date();
    date.setDate(date.getDate() + day);
    date.setHours(6, 0, 0, 0);
    options.push([`${day === 0 ? "Today" : day === 1 ? "Tomorrow" : date.toLocaleDateString("en-GB", { weekday: "short", day: "numeric" })} 6am`, date]);
  }
  options.forEach(([label, date]) => startSelect.appendChild(Object.assign(document.createElement("option"), { value: date.toISOString(), textContent: label })));

  try {
    const response = await fetch(`?handler=data&latitude=${latitude}&longitude=${longitude}`);
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    sources = await response.json();
    statusElement.hidden = true;
    document.getElementById("compare-export").disabled = false;
    render();
  } catch (error) {
    statusElement.textContent = `Couldn't load sources (${error.message}).`;
  }
}

// Exports every source's hours (all metrics, all days) as one CSV row per source per hour.
function exportCsv() {
  const columns = ["time", "condition", "temperature", "feelsLike", "precipitation", "precipitationProbability", "humidity", "cloudCover", "windSpeed", "windGust", "windDirection", "uvIndex", "uvIndexClearSky"];
  const escape = value => {
    const text = value === null || value === undefined ? "" : String(value);
    return /[",\n]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text;
  };
  const lines = [["source", "status", ...columns].join(",")];
  sources.forEach(source => {
    if (source.hourly.length === 0) {
      lines.push([escape(source.source), escape(source.status)].join(","));
    }
    source.hourly.forEach(h => lines.push([escape(source.source), escape(source.status), ...columns.map(c => escape(h[c]))].join(",")));
  });
  const place = (params.get("name") ?? "location").toLowerCase().replace(/[^a-z0-9]+/g, "-");
  const stamp = new Date().toISOString().slice(0, 16).replace(":", "");
  const header = `# PourCast source export, ${params.get("name") ?? ""} (${latitude}, ${longitude}), exported ${new Date().toISOString()}`;
  const blob = new Blob([[header, ...lines].join("\n")], { type: "text/csv" });
  const link = Object.assign(document.createElement("a"), { href: URL.createObjectURL(blob), download: `pourcast-sources-${place}-${stamp}.csv` });
  link.click();
  URL.revokeObjectURL(link.href);
}

document.getElementById("compare-export").addEventListener("click", exportCsv);
metricSelect.addEventListener("change", render);
startSelect.addEventListener("change", render);
load();
