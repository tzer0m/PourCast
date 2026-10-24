// Ranges the forecast can be viewed for: single 6am-to-6am days, 7 days in 6 hour columns, or 30 days in daily columns.
// The 30 day range uses the separate month forecast (source "Month"); the rest use the week forecast.
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

// Chart columns for a range: the hours themselves for single days, otherwise the server's combined columns.
export function columnsForRange(report, range) {
  if (range.stepHours === 1) return hoursInRange(report.hourly, range);
  return report.columns.filter(c => new Date(c.time) >= range.start && new Date(c.time) < range.end && c.temperatureAverage !== null);
}
