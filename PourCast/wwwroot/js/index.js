const statusElement = document.getElementById("status");
const axisElement = document.getElementById("axis");
const stripElement = document.getElementById("strip");
const svgNamespace = "http://www.w3.org/2000/svg";

// Chart geometry in pixels.
const layout = {
  minimumColumnWidth: 56,
  axisWidth: 24,
  minimumUvScale: 4,
  height: 636,
  timeY: 22,
  iconY: 30,
  iconSize: 36,
  temperatureLabelY: 90,
  feelsLikeLabelY: 110,
  windLabelY: 128,
  sunY: 150,
  temperatureTop: 168,
  temperatureBottom: 298,
  uvTop: 332,
  uvBottom: 400,
  humidityTop: 434,
  humidityBottom: 502,
  rainTop: 536,
  rainBottom: 610,
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

// Straight path through points, used where smoothing could dip below zero.
function linearPath(points, move = true) {
  return points.map(([x, y], i) => `${i === 0 && move ? "M" : "L"} ${x} ${y}`).join(" ");
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
  const temperatures = hours.flatMap(h => [h.temperatureMin, h.temperatureMax, h.feelsLikeMin, h.feelsLikeMax]).filter(t => t !== null);
  const rawLow = Math.min(...temperatures);
  const rawHigh = Math.max(...temperatures);
  const step = temperatureStep(rawHigh - rawLow);
  const low = Math.floor((rawLow - 1) / step) * step;
  const high = Math.ceil((rawHigh + 1) / step) * step;
  const temperatureY = t => layout.temperatureTop + (high - t) / (high - low) * (layout.temperatureBottom - layout.temperatureTop);

  // Rain scale.
  const rainScale = Math.max(layout.minimumRainScale, Math.ceil(Math.max(...hours.map(h => h.precipitationMax ?? h.precipitationAverage ?? 0))));
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
  const uvValues = hours.flatMap(h => [h.uvIndexMax, h.uvIndexClearSkyMax]).filter(u => u !== null);
  const uvScale = Math.ceil(Math.max(layout.minimumUvScale, ...uvValues) / 2) * 2;
  const uvY = uv => layout.uvBottom - uv / uvScale * (layout.uvBottom - layout.uvTop);
  [0, uvScale / 2, uvScale].forEach(uv => {
    stripElement.appendChild(svg("line", { class: uv === 0 ? "grid grid-base" : "grid", x1: 0, x2: width, y1: uvY(uv), y2: uvY(uv) }));
    axisElement.appendChild(svg("text", { class: "axis-label", x: layout.axisWidth - 4, y: uvY(uv) + 4 }, `${uv}`));
  });
  axisElement.appendChild(svg("text", { class: "axis-unit", x: layout.axisWidth - 4, y: layout.uvTop - 14 }, "UV"));

  // Humidity section between UV and rain.
  const humidityY = rh => layout.humidityBottom - rh / 100 * (layout.humidityBottom - layout.humidityTop);
  [0, 50, 100].forEach(rh => {
    stripElement.appendChild(svg("line", { class: rh === 0 ? "grid grid-base" : "grid", x1: 0, x2: width, y1: humidityY(rh), y2: humidityY(rh) }));
    axisElement.appendChild(svg("text", { class: "axis-label", x: layout.axisWidth - 4, y: humidityY(rh) + 4 }, `${rh}`));
  });
  axisElement.appendChild(svg("text", { class: "axis-unit", x: layout.axisWidth - 4, y: layout.humidityTop - 14 }, "%"));

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
    if (h.windSpeedAverage !== null) {
      stripElement.appendChild(svg("text", { class: "wind-value", x: centre(i), y: layout.windLabelY }, `${Math.round(h.windSpeedAverage)} mph`));
    }
  });

  // Temperature range band and average line.
  const maxPoints = hours.map((h, i) => [centre(i), temperatureY(h.temperatureMax)]);
  const minPoints = hours.map((h, i) => [centre(i), temperatureY(h.temperatureMin)]).reverse();
  const averagePoints = hours.map((h, i) => [centre(i), temperatureY(h.temperatureAverage)]);
  stripElement.appendChild(svg("path", { class: "temperature-range", d: `${smoothPath(maxPoints)} ${smoothPath(minPoints, false)} Z` }));

  // Min/max bands for the other lines.
  const band = (className, minKey, maxKey, toY) => {
    const valid = hours.map((h, i) => [i, h[minKey], h[maxKey]]).filter(([, min, max]) => min !== null && max !== null);
    if (valid.length < 2) return;
    const upper = valid.map(([i, , max]) => [centre(i), toY(max)]);
    const lower = valid.map(([i, min]) => [centre(i), toY(min)]).reverse();
    stripElement.appendChild(svg("path", { class: `range-band ${className}`, d: `${smoothPath(upper)} ${smoothPath(lower, false)} Z` }));
  };
  band("range-feels-like", "feelsLikeMin", "feelsLikeMax", temperatureY);
  band("range-uv", "uvIndexMin", "uvIndexMax", uvY);
  band("range-uv range-uv-clear-sky", "uvIndexClearSkyMin", "uvIndexClearSkyMax", uvY);
  band("range-humidity", "humidityMin", "humidityMax", humidityY);

  // Rain as a line with a shaded area underneath and a min/max band.
  const rainHours = hours.map((h, i) => [i, h]).filter(([, h]) => h.precipitationAverage !== null);
  if (rainHours.length > 1) {
    const rainRange = rainHours.filter(([, h]) => h.precipitationMin !== null && h.precipitationMax !== null);
    if (rainRange.length > 1) {
      const upper = rainRange.map(([i, h]) => [centre(i), rainY(h.precipitationMax)]);
      const lower = rainRange.map(([i, h]) => [centre(i), rainY(h.precipitationMin)]).reverse();
      stripElement.appendChild(svg("path", { class: "range-band range-rain", d: `${linearPath(upper)} ${linearPath(lower, false)} Z` }));
    }
    const rainPoints = rainHours.map(([i, h]) => [centre(i), rainY(h.precipitationAverage)]);
    const firstX = rainPoints[0][0];
    const lastX = rainPoints[rainPoints.length - 1][0];
    stripElement.appendChild(svg("path", { class: "rain-area", d: `${linearPath(rainPoints)} L ${lastX} ${layout.rainBottom} L ${firstX} ${layout.rainBottom} Z` }));
    stripElement.appendChild(svg("path", { class: "rain-line", d: linearPath(rainPoints) }));
  }

  // Chance of rain as a dashed line on a 0-100% scale across the rain section.
  const chanceY = pct => layout.rainBottom - pct / 100 * (layout.rainBottom - layout.rainTop);
  band("range-rain", "precipitationProbabilityMin", "precipitationProbabilityMax", chanceY);
  const chancePoints = hours.map((h, i) => [centre(i), h.precipitationProbabilityAverage]).filter(([, pct]) => pct !== null).map(([x, pct]) => [x, chanceY(pct)]);
  if (chancePoints.length > 1) {
    stripElement.appendChild(svg("path", { class: "chance-line", d: linearPath(chancePoints) }));
  }
  const feelsLikePoints = hours.map((h, i) => [centre(i), h.feelsLikeAverage]).filter(([, t]) => t !== null).map(([x, t]) => [x, temperatureY(t)]);
  const uvPoints = hours.map((h, i) => [centre(i), h.uvIndexAverage]).filter(([, u]) => u !== null).map(([x, u]) => [x, uvY(u)]);
  const uvClearSkyPoints = hours.map((h, i) => [centre(i), h.uvIndexClearSkyAverage]).filter(([, u]) => u !== null).map(([x, u]) => [x, uvY(u)]);
  if (uvClearSkyPoints.length > 1) {
    stripElement.appendChild(svg("path", { class: "uv-line uv-clear-sky", d: smoothPath(uvClearSkyPoints) }));
  }
  const humidityPoints = hours.map((h, i) => [centre(i), h.humidityAverage]).filter(([, rh]) => rh !== null).map(([x, rh]) => [x, humidityY(rh)]);
  if (humidityPoints.length > 1) {
    stripElement.appendChild(svg("path", { class: "humidity-line", d: smoothPath(humidityPoints) }));
  }
  if (uvPoints.length > 1) {
    stripElement.appendChild(svg("path", { class: "uv-line", d: smoothPath(uvPoints) }));
  }
  if (feelsLikePoints.length > 1) {
    stripElement.appendChild(svg("path", { class: "feels-like", d: smoothPath(feelsLikePoints) }));
  }
  stripElement.appendChild(svg("path", { class: "temperature-average", d: smoothPath(averagePoints) }));
  averagePoints.forEach(([x, y], i) => stripElement.appendChild(svg("circle", { class: i === nowIndex ? "temperature-point temperature-point-now" : "temperature-point", cx: x, cy: y, r: i === nowIndex ? 5 : 3 })));

  // Value labels for each line, placing pairs that share a section on opposite sides.
  const valueLabel = (className, x, y, above, text) => stripElement.appendChild(svg("text", { class: `value-label ${className}`, x, y: above ? y - 8 : y + 15 }, text));
  hours.forEach((h, i) => {
    const x = centre(i);
    const temperatureAbove = h.feelsLikeAverage === null || h.feelsLikeAverage <= h.temperatureAverage;
    valueLabel("value-temperature", x, temperatureY(h.temperatureAverage), temperatureAbove, `${Math.round(h.temperatureAverage)}°`);
    if (h.feelsLikeAverage !== null) {
      valueLabel("value-feels-like", x, temperatureY(h.feelsLikeAverage), !temperatureAbove, `${Math.round(h.feelsLikeAverage)}°`);
    }
    const clearSkyAbove = h.uvIndexAverage === null || h.uvIndexClearSkyAverage === null || h.uvIndexClearSkyAverage >= h.uvIndexAverage;
    if (h.uvIndexClearSkyAverage !== null) {
      valueLabel("value-uv value-uv-clear-sky", x, uvY(h.uvIndexClearSkyAverage), clearSkyAbove, h.uvIndexClearSkyAverage.toFixed(1));
    }
    if (h.uvIndexAverage !== null) {
      valueLabel("value-uv", x, uvY(h.uvIndexAverage), !clearSkyAbove || h.uvIndexClearSkyAverage === null, h.uvIndexAverage.toFixed(1));
    }
    const rain = h.precipitationAverage ?? 0;
    const chance = h.precipitationProbabilityAverage;
    const chanceAbove = chance === null || rain === 0 || chanceY(chance) <= rainY(rain);
    if (rain > 0) {
      valueLabel("value-rain", x, rainY(rain), !chanceAbove, rain < 0.1 ? "<0.1" : rain.toFixed(1));
    }
    if (chance !== null && chance > 0) {
      valueLabel("value-rain value-chance", x, chanceY(chance), chanceAbove, `${Math.round(chance)}%`);
    }
    if (h.humidityAverage !== null) {
      valueLabel("value-humidity", x, humidityY(h.humidityAverage), h.humidityAverage < 75, `${Math.round(h.humidityAverage)}%`);
    }
  });

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
