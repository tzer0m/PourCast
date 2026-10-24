const statusElement = document.getElementById("status");
const axisElement = document.getElementById("axis");
const stripElement = document.getElementById("strip");
const svgNamespace = "http://www.w3.org/2000/svg";

// Chart geometry in pixels.
const layout = {
  minimumColumnWidth: 56,
  axisWidth: 44,
  height: 360,
  timeY: 24,
  temperatureLabelY: 58,
  sunY: 80,
  temperatureTop: 98,
  temperatureBottom: 228,
  rainTop: 262,
  rainBottom: 336,
  minimumRainScale: 2
};

function svg(tag, attributes = {}, text) {
  const node = document.createElementNS(svgNamespace, tag);
  Object.entries(attributes).forEach(([key, value]) => node.setAttribute(key, value));
  if (text !== undefined) node.textContent = text;
  return node;
}

function hourLabel(date) {
  const hour = date.getHours();
  return `${hour % 12 || 12}${hour < 12 ? "am" : "pm"}`;
}

function windowHours(hourly) {
  const start = new Date();
  start.setHours(6, 0, 0, 0);
  const end = new Date(start.getTime() + 24 * 60 * 60 * 1000);
  return hourly.filter(h => {
    const time = new Date(h.time);
    return time >= start && time < end && h.temperatureAverage !== null;
  });
}

function temperatureStep(range) {
  if (range <= 8) return 2;
  if (range <= 20) return 4;
  return 5;
}

// Smooth path through points using a Catmull-Rom spline.
function smoothPath(points, move = true) {
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

function clockTime(date) {
  return date.toLocaleTimeString("en-GB", { hour: "2-digit", minute: "2-digit" });
}

// Night periods between each sunset and the following sunrise.
function nightPeriods(daylight) {
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

function renderStrip(hourly, daylight) {
  const hours = windowHours(hourly);
  if (hours.length === 0) {
    statusElement.textContent = "No hourly data available for today.";
    statusElement.hidden = false;
    return;
  }

  // Temperature scale snapped to tidy steps.
  const temperatures = hours.flatMap(h => [h.temperatureMin, h.temperatureMax]);
  const rawLow = Math.min(...temperatures);
  const rawHigh = Math.max(...temperatures);
  const step = temperatureStep(rawHigh - rawLow);
  const low = Math.floor((rawLow - 1) / step) * step;
  const high = Math.ceil((rawHigh + 1) / step) * step;
  const temperatureY = t => layout.temperatureTop + (high - t) / (high - low) * (layout.temperatureBottom - layout.temperatureTop);

  // Rain scale.
  const rainScale = Math.max(layout.minimumRainScale, Math.ceil(Math.max(...hours.map(h => h.precipitationAverage ?? 0))));
  const rainY = mm => layout.rainBottom - Math.min(mm, rainScale) / rainScale * (layout.rainBottom - layout.rainTop);

  // Stretch columns to fill the available width, scrolling only when they would get too narrow.
  const scroll = document.getElementById("strip-scroll");
  document.getElementById("hourly").hidden = false;
  stripElement.setAttribute("width", 0);
  axisElement.setAttribute("width", layout.axisWidth);
  axisElement.setAttribute("height", layout.height);
  const columnWidth = Math.max(layout.minimumColumnWidth, scroll.clientWidth / hours.length);
  const width = hours.length * columnWidth;
  const centre = i => i * columnWidth + columnWidth / 2;
  const now = new Date();
  now.setMinutes(0, 0, 0);

  stripElement.replaceChildren();
  axisElement.replaceChildren();
  stripElement.setAttribute("width", width);
  stripElement.setAttribute("height", layout.height);
  stripElement.setAttribute("viewBox", `0 0 ${width} ${layout.height}`);

  // Night shading and sunrise/sunset markers.
  const start = new Date(hours[0].time).getTime();
  const timeX = date => (date.getTime() - start) / (60 * 60 * 1000) * columnWidth;
  nightPeriods(daylight).forEach(([from, to]) => {
    const x1 = Math.max(0, timeX(from));
    const x2 = Math.min(width, timeX(to));
    if (x2 > x1) stripElement.appendChild(svg("rect", { class: "night", x: x1, y: 0, width: x2 - x1, height: layout.height }));
  });
  daylight.flatMap(d => [["sunrise", d.sunrise], ["sunset", d.sunset]]).filter(([, value]) => value).forEach(([kind, value]) => {
    const time = new Date(value);
    const x = timeX(time);
    if (x < 0 || x > width) return;
    stripElement.appendChild(svg("line", { class: "sun-line", x1: x, x2: x, y1: layout.sunY + 6, y2: layout.rainBottom }));
    const label = svg("text", { class: "sun-label", x: x + 4, y: layout.sunY }, `${kind === "sunrise" ? "↑" : "↓"} ${clockTime(time)}`);
    label.appendChild(svg("title", {}, `${kind === "sunrise" ? "Sunrise" : "Sunset"} ${clockTime(time)}`));
    stripElement.appendChild(label);
  });

  // Current hour highlight.
  const nowIndex = hours.findIndex(h => new Date(h.time).getTime() === now.getTime());
  if (nowIndex >= 0) {
    stripElement.appendChild(svg("rect", { class: "now-column", x: nowIndex * columnWidth + 3, y: 4, width: columnWidth - 6, height: layout.height - 8, rx: 12 }));
  }

  // Gridlines and y axis labels.
  for (let t = low; t <= high; t += step) {
    stripElement.appendChild(svg("line", { class: "grid", x1: 0, x2: width, y1: temperatureY(t), y2: temperatureY(t) }));
    axisElement.appendChild(svg("text", { class: "axis-label axis-temperature", x: layout.axisWidth - 8, y: temperatureY(t) + 4 }, `${t}°`));
  }
  [0, rainScale / 2, rainScale].forEach(mm => {
    stripElement.appendChild(svg("line", { class: mm === 0 ? "grid grid-base" : "grid", x1: 0, x2: width, y1: rainY(mm), y2: rainY(mm) }));
    axisElement.appendChild(svg("text", { class: "axis-label axis-rain", x: layout.axisWidth - 8, y: rainY(mm) + 4 }, mm === 0 ? "0" : `${+mm.toFixed(1)}`));
  });
  axisElement.appendChild(svg("text", { class: "axis-unit", x: layout.axisWidth - 8, y: layout.temperatureTop - 14 }, "°C"));
  axisElement.appendChild(svg("text", { class: "axis-unit", x: layout.axisWidth - 8, y: layout.rainTop - 14 }, "mm"));

  // Column labels, midnight dividers and rain bars.
  hours.forEach((h, i) => {
    const time = new Date(h.time);
    const isNow = i === nowIndex;
    const isMidnight = time.getHours() === 0;
    if (isMidnight) {
      stripElement.appendChild(svg("line", { class: "day-divider", x1: i * columnWidth, x2: i * columnWidth, y1: 8, y2: layout.height - 8 }));
    }
    const label = isNow ? "Now" : isMidnight ? time.toLocaleDateString("en-GB", { weekday: "short" }) : hourLabel(time);
    stripElement.appendChild(svg("text", { class: isNow ? "time-label time-now" : "time-label", x: centre(i), y: layout.timeY }, label));
    stripElement.appendChild(svg("text", { class: "temperature-label", x: centre(i), y: layout.temperatureLabelY }, `${Math.round(h.temperatureAverage)}°`));

    const rain = h.precipitationAverage ?? 0;
    if (rain > 0) {
      const chance = (h.precipitationProbabilityAverage ?? 100) / 100;
      const y = Math.min(rainY(rain), layout.rainBottom - 2);
      const bar = svg("rect", { class: "rain-bar", x: i * columnWidth + 12, y, width: columnWidth - 24, height: layout.rainBottom - y, rx: 4 });
      bar.style.setProperty("--chance", 0.2 + 0.8 * chance);
      bar.appendChild(svg("title", {}, `${rain.toFixed(1)} mm · ${Math.round(chance * 100)}% Chance`));
      stripElement.appendChild(bar);
      if (rain >= 0.1) {
        stripElement.appendChild(svg("text", { class: "rain-label", x: centre(i), y: y - 5 }, h.precipitationProbabilityAverage === null ? rain.toFixed(1) : `${rain.toFixed(1)} - ${Math.round(h.precipitationProbabilityAverage)}%`));
      }
    }
  });

  // Temperature range band and average line.
  const maxPoints = hours.map((h, i) => [centre(i), temperatureY(h.temperatureMax)]);
  const minPoints = hours.map((h, i) => [centre(i), temperatureY(h.temperatureMin)]).reverse();
  const averagePoints = hours.map((h, i) => [centre(i), temperatureY(h.temperatureAverage)]);
  stripElement.appendChild(svg("path", { class: "temperature-range", d: `${smoothPath(maxPoints)} ${smoothPath(minPoints, false)} Z` }));
  stripElement.appendChild(svg("path", { class: "temperature-average", d: smoothPath(averagePoints) }));
  averagePoints.forEach(([x, y], i) => stripElement.appendChild(svg("circle", { class: i === nowIndex ? "temperature-point temperature-point-now" : "temperature-point", cx: x, cy: y, r: i === nowIndex ? 5 : 3 })));

  if (nowIndex >= 0) {
    scroll.scrollLeft = Math.max(0, centre(nowIndex) - scroll.clientWidth / 3);
  }
}

let lastReport = null;
let resizeTimer = null;
window.addEventListener("resize", () => {
  clearTimeout(resizeTimer);
  resizeTimer = setTimeout(() => { if (lastReport) renderStrip(lastReport.hourly, lastReport.daylight); }, 150);
});

async function load(latitude, longitude) {
  statusElement.textContent = "Loading Forecast…";
  try {
    const response = await fetch(`?handler=forecast&latitude=${latitude}&longitude=${longitude}`);
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    const report = await response.json();
    statusElement.hidden = true;
    lastReport = report;
    renderStrip(report.hourly, report.daylight);
  } catch (error) {
    statusElement.hidden = false;
    statusElement.textContent = `Couldn't load the forecast (${error.message}).`;
  }
}

if ("geolocation" in navigator) {
  navigator.geolocation.getCurrentPosition(p => load(p.coords.latitude, p.coords.longitude), e => { statusElement.textContent = `Couldn't get your location (${e.message}).`; }, { timeout: 10000, maximumAge: 600000 });
} else {
  statusElement.textContent = "Your browser doesn't support location.";
}
