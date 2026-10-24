import { zonedSixAm, zonedParts, dayLabel } from "/js/helpers.js";

// Ranges the forecast can be viewed for: single 6am-to-6am days, 7 days in 6 hour columns, or 30 days in daily columns.
// Days run from 6am in the place's time zone. The 30 day range uses the separate month forecast (source "Month"); the rest use the week forecast.
function dateKey(date, zone) {
  const p = zonedParts(date, zone);
  return `${p.year}-${String(p.month).padStart(2, "0")}-${String(p.day).padStart(2, "0")}`;
}

// The ranges that have data, as { key, label, start, end, stepHours, source }.
export function availableRanges(hourly, zone) {
  if (hourly.length === 0) return [];
  const last = new Date(hourly[hourly.length - 1].time);
  const ranges = [];
  for (let day = 0; day < 7; day++) {
    const start = zonedSixAm(day, zone);
    if (start > last) break;
    const label = day === 0 ? "Today" : day === 1 ? "Tomorrow" : dayLabel(start, zone);
    ranges.push({ key: day === 0 ? "today" : dateKey(start, zone), label, start, end: zonedSixAm(day + 1, zone), stepHours: 1, source: "Week" });
  }
  ranges.push({ key: "week", label: "7 Days", start: zonedSixAm(0, zone), end: zonedSixAm(7, zone), stepHours: 6, source: "Week" });
  ranges.push({ key: "month", label: "30 Days", start: zonedSixAm(0, zone), end: zonedSixAm(30, zone), stepHours: 24, source: "Month" });
  return ranges;
}

// The hourly summaries within a range.
export function hoursInRange(hourly, range) {
  return hourly.filter(h => {
    const time = new Date(h.time);
    return time >= range.start && time < range.end && h.temperatureAverage !== null;
  });
}

// Chart columns for a range: the hours themselves for single days, otherwise the server's combined columns.
export function columnsForRange(report, range) {
  if (range.stepHours === 1) return hoursInRange(report.hourly, range);
  return report.columns.filter(c => new Date(c.time) >= range.start && new Date(c.time) < range.end && c.temperatureAverage !== null);
}
