const svgNamespace = "http://www.w3.org/2000/svg";

export function svg(tag, attributes = {}, text) {
  const node = document.createElementNS(svgNamespace, tag);
  Object.entries(attributes).forEach(([key, value]) => node.setAttribute(key, value));
  if (text !== undefined) node.textContent = text;
  return node;
}

export function element(tag, className, text) {
  const node = document.createElement(tag);
  if (className) node.className = className;
  if (text !== undefined) node.textContent = text;
  return node;
}

export function compass(degrees) {
  const points = ["N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"];
  return points[Math.round(degrees / 22.5) % 16];
}

// The browser's time zone. Places can be in a different one.
export const browserZone = Intl.DateTimeFormat().resolvedOptions().timeZone;

// Date and clock parts of a time in a time zone, as { year, month, day, hour, minute, second, weekday } with weekday 0 for Sunday.
const partFormats = new Map();
const weekdays = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
export function zonedParts(date, zone = browserZone) {
  if (!partFormats.has(zone)) partFormats.set(zone, new Intl.DateTimeFormat("en-GB", { timeZone: zone, hourCycle: "h23", year: "numeric", month: "numeric", day: "numeric", hour: "numeric", minute: "numeric", second: "numeric", weekday: "short" }));
  const parts = Object.fromEntries(partFormats.get(zone).formatToParts(date).map(p => [p.type, p.value]));
  return { year: +parts.year, month: +parts.month, day: +parts.day, hour: +parts.hour, minute: +parts.minute, second: +parts.second, weekday: weekdays.indexOf(parts.weekday) };
}

// How far ahead of UTC a time zone is at a given time, in milliseconds.
export function zoneOffset(date, zone = browserZone) {
  const p = zonedParts(date, zone);
  return Date.UTC(p.year, p.month - 1, p.day, p.hour, p.minute, p.second) - Math.floor(date.getTime() / 1000) * 1000;
}

// 6am in a time zone, a number of days from today there.
export function zonedSixAm(daysFromToday, zone = browserZone) {
  const today = zonedParts(new Date(), zone);
  const wall = Date.UTC(today.year, today.month - 1, today.day + daysFromToday, 6);
  // Checked twice, in case the offset changes between the guess and the answer (clock changes).
  const guess = wall - zoneOffset(new Date(wall), zone);
  return new Date(wall - zoneOffset(new Date(guess), zone));
}

// Short name for a time zone at a given time, e.g. "BST" or "GMT-4".
export function zoneName(date, zone = browserZone) {
  return new Intl.DateTimeFormat("en-GB", { timeZone: zone, timeZoneName: "short" }).formatToParts(date).find(p => p.type === "timeZoneName")?.value ?? zone;
}

export function hourLabel(date, zone = browserZone) {
  const hour = zonedParts(date, zone).hour;
  return `${hour % 12 || 12}${hour < 12 ? "am" : "pm"}`;
}

export function clockTime(date, zone = browserZone) {
  return date.toLocaleTimeString("en-GB", { hour: "2-digit", minute: "2-digit", timeZone: zone });
}

export function dayLabel(date, zone = browserZone) {
  return date.toLocaleDateString("en-GB", { weekday: "short", day: "numeric", timeZone: zone });
}

// A tidy step (1, 2, 2.5 or 5 times a power of ten) giving roughly the target number of ticks.
export function niceStep(span, target) {
  const raw = span / target;
  const magnitude = 10 ** Math.floor(Math.log10(raw));
  const fraction = raw / magnitude;
  return (fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 2.5 ? 2.5 : fraction <= 5 ? 5 : 10) * magnitude;
}

// Tick values at multiples of the step that fall within the range.
export function ticks(low, high, step) {
  const values = [];
  for (let value = Math.ceil(low / step - 1e-9) * step; value <= high + 1e-9; value += step) {
    values.push(+value.toFixed(6));
  }
  return values;
}

// Straight path through points, used where smoothing could dip below zero.
export function linearPath(points, move = true) {
  return points.map(([x, y], i) => `${i === 0 && move ? "M" : "L"} ${x} ${y}`).join(" ");
}

// Smooth path through points using a Catmull-Rom spline.
export function smoothPath(points, move = true) {
  if (points.length === 0) return "";
  let path = move ? `M ${points[0][0]} ${points[0][1]}` : `L ${points[0][0]} ${points[0][1]}`;
  for (let i = 0; i < points.length - 1; i++) {
    const p0 = points[i - 1] ?? points[i];
    const p1 = points[i];
    const p2 = points[i + 1];
    const p3 = points[i + 2] ?? p2;
    const c1 = [p1[0] + (p2[0] - p0[0]) / 6, p1[1] + (p2[1] - p0[1]) / 6];
    const c2 = [p2[0] - (p3[0] - p1[0]) / 6, p2[1] - (p3[1] - p1[1]) / 6];
    path += ` C ${c1[0]} ${c1[1]}, ${c2[0]} ${c2[1]}, ${p2[0]} ${p2[1]}`;
  }
  return path;
}

// Night periods between each sunset and the following sunrise.
export function nightPeriods(daylight) {
  const periods = [];
  daylight.forEach((d, i) => {
    if (d.sunrise === null && !d.alwaysUp) periods.push([new Date(`${d.date}T00:00:00Z`), new Date(`${d.date}T23:59:59Z`)]);
    const next = daylight[i + 1];
    if (d.sunset && next?.sunrise) periods.push([new Date(d.sunset), new Date(next.sunrise)]);
  });
  const first = daylight[0];
  if (first?.sunrise) periods.unshift([new Date(new Date(first.sunrise).getTime() - 24 * 60 * 60 * 1000), new Date(first.sunrise)]);
  return periods;
}
