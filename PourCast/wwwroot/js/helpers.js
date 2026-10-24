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

export function hourLabel(date) {
  const hour = date.getHours();
  return `${hour % 12 || 12}${hour < 12 ? "am" : "pm"}`;
}

export function clockTime(date) {
  return date.toLocaleTimeString("en-GB", { hour: "2-digit", minute: "2-digit" });
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
