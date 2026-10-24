// Ranges the forecast can be viewed for: single 6am-to-6am days, 7 days in 6 hour columns, or 30 days in daily columns.
// The 30 day range uses the separate month forecast (source "Month"); the rest use the week forecast.
const hourMs = 60 * 60 * 1000;

function sixAm(daysFromToday) {
  const date = new Date();
  date.setDate(date.getDate() + daysFromToday);
  date.setHours(6, 0, 0, 0);
  return date;
}

function dateKey(date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
}

// The ranges that have data, as { key, label, start, end, stepHours, source }.
export function availableRanges(hourly) {
  if (hourly.length === 0) return [];
  const last = new Date(hourly[hourly.length - 1].time);
  const ranges = [];
  for (let day = 0; day < 7; day++) {
    const start = sixAm(day);
    if (start > last) break;
    const label = day === 0 ? "Today" : day === 1 ? "Tomorrow" : start.toLocaleDateString("en-GB", { weekday: "short", day: "numeric" });
    ranges.push({ key: day === 0 ? "today" : dateKey(start), label, start, end: sixAm(day + 1), stepHours: 1, source: "Week" });
  }
  ranges.push({ key: "week", label: "7 Days", start: sixAm(0), end: sixAm(7), stepHours: 6, source: "Week" });
  ranges.push({ key: "month", label: "30 Days", start: sixAm(0), end: sixAm(30), stepHours: 24, source: "Month" });
  return ranges;
}

// The hourly summaries within a range.
export function hoursInRange(hourly, range) {
  return hourly.filter(h => {
    const time = new Date(h.time);
    return time >= range.start && time < range.end && h.temperatureAverage !== null;
  });
}

// Column boundaries on the local clock (6am, 12pm, 6pm, 12am), so they stay put across a clock change.
function boundaries(range) {
  const result = [];
  for (let date = new Date(range.start); date < range.end; date = new Date(date)) {
    result.push(new Date(date));
    date.setHours(date.getHours() + range.stepHours);
  }
  result.push(new Date(range.end));
  return result;
}

// Groups hours into columns of the range's step, combining each metric sensibly. Every column gets an end time.
export function columnsForRange(hours, range) {
  if (range.stepHours === 1) return hours.map(h => ({ ...h, end: new Date(new Date(h.time).getTime() + hourMs).toISOString() }));
  const edges = boundaries(range);
  const columns = [];
  for (let i = 0; i < edges.length - 1; i++) {
    const group = hours.filter(h => new Date(h.time) >= edges[i] && new Date(h.time) < edges[i + 1]);
    if (group.length > 0) columns.push({ ...combine(group, edges[i]), end: edges[i + 1].toISOString() });
  }
  return columns;
}

function values(group, key) {
  return group.map(h => h[key]).filter(v => v !== null && v !== undefined);
}

function mean(group, key) {
  const list = values(group, key);
  return list.length ? list.reduce((sum, v) => sum + v, 0) / list.length : null;
}

function min(group, key) {
  const list = values(group, key);
  return list.length ? Math.min(...list) : null;
}

function max(group, key) {
  const list = values(group, key);
  return list.length ? Math.max(...list) : null;
}

function sum(group, key) {
  const list = values(group, key);
  return list.length ? list.reduce((total, v) => total + v, 0) : null;
}

function direction(group) {
  const list = values(group, "windDirectionAverage");
  if (list.length === 0) return null;
  const sin = list.reduce((total, d) => total + Math.sin(d * Math.PI / 180), 0);
  const cos = list.reduce((total, d) => total + Math.cos(d * Math.PI / 180), 0);
  const degrees = Math.atan2(sin, cos) * 180 / Math.PI;
  return degrees < 0 ? degrees + 360 : degrees;
}

function condition(group) {
  const counts = new Map();
  group.filter(h => h.condition && h.condition !== "Unknown").forEach(h => counts.set(h.condition, (counts.get(h.condition) ?? 0) + 1));
  return [...counts.entries()].sort((a, b) => b[1] - a[1])[0]?.[0] ?? "Unknown";
}

// Temperatures, humidity and wind are averaged; rain is totalled; UV, chance and gusts take the peak.
function combine(group, time) {
  return {
    time: time.toISOString(),
    condition: condition(group),
    temperatureAverage: mean(group, "temperatureAverage"),
    temperatureMin: min(group, "temperatureMin"),
    temperatureMax: max(group, "temperatureMax"),
    feelsLikeAverage: mean(group, "feelsLikeAverage"),
    feelsLikeMin: min(group, "feelsLikeMin"),
    feelsLikeMax: max(group, "feelsLikeMax"),
    humidityAverage: mean(group, "humidityAverage"),
    humidityMin: min(group, "humidityMin"),
    humidityMax: max(group, "humidityMax"),
    uvIndexAverage: max(group, "uvIndexAverage"),
    uvIndexMin: max(group, "uvIndexMin"),
    uvIndexMax: max(group, "uvIndexMax"),
    uvIndexClearSkyAverage: max(group, "uvIndexClearSkyAverage"),
    uvIndexClearSkyMin: max(group, "uvIndexClearSkyMin"),
    uvIndexClearSkyMax: max(group, "uvIndexClearSkyMax"),
    precipitationAverage: sum(group, "precipitationAverage"),
    precipitationMin: sum(group, "precipitationMin"),
    precipitationMax: sum(group, "precipitationMax"),
    precipitationProbabilityAverage: max(group, "precipitationProbabilityAverage"),
    precipitationProbabilityMin: max(group, "precipitationProbabilityMin"),
    precipitationProbabilityMax: max(group, "precipitationProbabilityMax"),
    windSpeedAverage: mean(group, "windSpeedAverage"),
    windSpeedMin: min(group, "windSpeedMin"),
    windSpeedMax: max(group, "windSpeedMax"),
    windGustAverage: max(group, "windGustAverage"),
    windGustMin: max(group, "windGustMin"),
    windGustMax: max(group, "windGustMax"),
    windDirectionAverage: direction(group)
  };
}
