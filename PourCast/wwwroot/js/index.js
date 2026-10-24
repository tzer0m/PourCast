const statusElement = document.getElementById("status");
const axisElement = document.getElementById("axis");
const stripElement = document.getElementById("strip");
const svgNamespace = "http://www.w3.org/2000/svg";

// Chart geometry in pixels.
const layout = {
  minimumColumnWidth: 56,
  axisWidth: 24,
  minimumUvScale: 4,
  height: 514,
  timeY: 22,
  iconY: 30,
  iconSize: 36,
  temperatureLabelY: 90,
  feelsLikeLabelY: 110,
  sunY: 132,
  temperatureTop: 150,
  temperatureBottom: 280,
  uvTop: 314,
  uvBottom: 382,
  rainTop: 416,
  rainBottom: 490,
  minimumRainScale: 2
};

// Meteocons icon for each condition, as [day, night].
const conditionIcons = {
  Unknown: ["not-available", "not-available"],
  Clear: ["clear-day", "clear-night"],
  PartlyCloudy: ["partly-cloudy-day", "partly-cloudy-night"],
  Cloudy: ["overcast", "overcast"],
  Fog: ["fog-day", "fog-night"],
  Drizzle: ["drizzle", "drizzle"],
  Rain: ["rain", "rain"],
  HeavyRain: ["rain", "rain"],
  FreezingRain: ["sleet", "sleet"],
  Showers: ["partly-cloudy-day-rain", "partly-cloudy-night-rain"],
  Sleet: ["sleet", "sleet"],
  Hail: ["hail", "hail"],
  Snow: ["snow", "snow"],
  SnowShowers: ["partly-cloudy-day-snow", "partly-cloudy-night-snow"],
  Thunderstorm: ["thunderstorms-day-rain", "thunderstorms-night-rain"]
};

const conditionLabels = {
  Unknown: "Unknown",
  Clear: "Clear",
  PartlyCloudy: "Partly Cloudy",
  Cloudy: "Cloudy",
  Fog: "Fog",
  Drizzle: "Drizzle",
  Rain: "Rain",
  HeavyRain: "Heavy Rain",
  FreezingRain: "Freezing Rain",
  Showers: "Showers",
  Sleet: "Sleet",
  Hail: "Hail",
  Snow: "Snow",
  SnowShowers: "Snow Showers",
  Thunderstorm: "Thunderstorm"
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
  const temperatures = hours.flatMap(h => [h.temperatureMin, h.temperatureMax, h.feelsLikeAverage]).filter(t => t !== null);
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
  const nights = nightPeriods(daylight);
  const isNight = date => nights.some(([from, to]) => date >= from && date < to);
  nights.forEach(([from, to]) => {
    const x1 = Math.max(0, timeX(from));
    const x2 = Math.min(width, timeX(to));
    if (x2 > x1) stripElement.appendChild(svg("rect", { class: "night", x: x1, y: 0, width: x2 - x1, height: layout.rainBottom }));
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
    axisElement.appendChild(svg("text", { class: "axis-label axis-temperature", x: layout.axisWidth - 4, y: temperatureY(t) + 4 }, `${t}°`));
  }
  [0, rainScale / 2, rainScale].forEach(mm => {
    stripElement.appendChild(svg("line", { class: mm === 0 ? "grid grid-base" : "grid", x1: 0, x2: width, y1: rainY(mm), y2: rainY(mm) }));
    axisElement.appendChild(svg("text", { class: "axis-label axis-rain", x: layout.axisWidth - 4, y: rainY(mm) + 4 }, mm === 0 ? "0" : `${+mm.toFixed(1)}`));
  });
  axisElement.appendChild(svg("text", { class: "axis-unit", x: layout.axisWidth - 4, y: layout.temperatureTop - 14 }, "°C"));
  axisElement.appendChild(svg("text", { class: "axis-unit", x: layout.axisWidth - 4, y: layout.rainTop - 14 }, "mm"));

  // UV index section between temperature and rain.
  const uvValues = hours.flatMap(h => [h.uvIndexAverage, h.uvIndexClearSkyAverage]).filter(u => u !== null);
  const uvScale = Math.ceil(Math.max(layout.minimumUvScale, ...uvValues) / 2) * 2;
  const uvY = uv => layout.uvBottom - uv / uvScale * (layout.uvBottom - layout.uvTop);
  [0, uvScale / 2, uvScale].forEach(uv => {
    stripElement.appendChild(svg("line", { class: uv === 0 ? "grid grid-base" : "grid", x1: 0, x2: width, y1: uvY(uv), y2: uvY(uv) }));
    axisElement.appendChild(svg("text", { class: "axis-label", x: layout.axisWidth - 4, y: uvY(uv) + 4 }, `${uv}`));
  });
  axisElement.appendChild(svg("text", { class: "axis-unit", x: layout.axisWidth - 4, y: layout.uvTop - 14 }, "UV"));

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
    const icon = conditionIcons[h.condition] ?? conditionIcons.Unknown;
    const night = isNight(new Date(time.getTime() + 30 * 60 * 1000));
    const image = svg("image", { class: "condition-icon", href: `/icons/${night ? icon[1] : icon[0]}.svg`, x: centre(i) - layout.iconSize / 2, y: layout.iconY, width: layout.iconSize, height: layout.iconSize });
    image.appendChild(svg("title", {}, conditionLabels[h.condition] ?? h.condition));
    stripElement.appendChild(image);
    stripElement.appendChild(svg("text", { class: "temperature-label", x: centre(i), y: layout.temperatureLabelY }, `${Math.round(h.temperatureAverage)}°`));
    if (h.feelsLikeAverage !== null) {
      stripElement.appendChild(svg("text", { class: "feels-like-value", x: centre(i), y: layout.feelsLikeLabelY }, `${Math.round(h.feelsLikeAverage)}°`));
    }

    const rain = h.precipitationAverage ?? 0;
    if (rain > 0) {
      const chance = (h.precipitationProbabilityAverage ?? 100) / 100;
      const y = Math.min(rainY(rain), layout.rainBottom - 2);
      const bar = svg("rect", { class: "rain-bar", x: i * columnWidth + 12, y, width: columnWidth - 24, height: layout.rainBottom - y, rx: 4 });
      bar.style.setProperty("--chance", 0.2 + 0.8 * chance);
      bar.appendChild(svg("title", {}, `${rain.toFixed(2)} mm · ${Math.round(chance * 100)}% Chance`));
      stripElement.appendChild(bar);
      const amount = rain < 0.1 ? "<0.1" : rain.toFixed(1);
      stripElement.appendChild(svg("text", { class: "rain-label", x: centre(i), y: y - 5 }, h.precipitationProbabilityAverage === null ? amount : `${amount} - ${Math.round(h.precipitationProbabilityAverage)}%`));
    }
  });

  // Temperature range band and average line.
  const maxPoints = hours.map((h, i) => [centre(i), temperatureY(h.temperatureMax)]);
  const minPoints = hours.map((h, i) => [centre(i), temperatureY(h.temperatureMin)]).reverse();
  const averagePoints = hours.map((h, i) => [centre(i), temperatureY(h.temperatureAverage)]);
  stripElement.appendChild(svg("path", { class: "temperature-range", d: `${smoothPath(maxPoints)} ${smoothPath(minPoints, false)} Z` }));
  const feelsLikePoints = hours.map((h, i) => [centre(i), h.feelsLikeAverage]).filter(([, t]) => t !== null).map(([x, t]) => [x, temperatureY(t)]);
  const uvPoints = hours.map((h, i) => [centre(i), h.uvIndexAverage]).filter(([, u]) => u !== null).map(([x, u]) => [x, uvY(u)]);
  const uvClearSkyPoints = hours.map((h, i) => [centre(i), h.uvIndexClearSkyAverage]).filter(([, u]) => u !== null).map(([x, u]) => [x, uvY(u)]);
  if (uvClearSkyPoints.length > 1) {
    stripElement.appendChild(svg("path", { class: "uv-line uv-clear-sky", d: smoothPath(uvClearSkyPoints) }));
  }
  if (uvPoints.length > 1) {
    stripElement.appendChild(svg("path", { class: "uv-line", d: smoothPath(uvPoints) }));
  }
  if (feelsLikePoints.length > 1) {
    stripElement.appendChild(svg("path", { class: "feels-like", d: smoothPath(feelsLikePoints) }));
  }
  stripElement.appendChild(svg("path", { class: "temperature-average", d: smoothPath(averagePoints) }));
  averagePoints.forEach(([x, y], i) => stripElement.appendChild(svg("circle", { class: i === nowIndex ? "temperature-point temperature-point-now" : "temperature-point", cx: x, cy: y, r: i === nowIndex ? 5 : 3 })));

  if (nowIndex >= 0) {
    scroll.scrollLeft = nowIndex * columnWidth;
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
