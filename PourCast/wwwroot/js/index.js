const statusElement = document.getElementById("status");
const axisElement = document.getElementById("axis");
const stripElement = document.getElementById("strip");
const svgNamespace = "http://www.w3.org/2000/svg";

// Chart geometry in pixels, before stretching to fit.
const chartLayout = {
  minimumColumnWidth: 56,
  axisWidth: 28,
  minimumUvScale: 1,
  height: 690,
  timeY: 22,
  iconY: 30,
  iconSize: 36,
  sunY: 88,
  temperatureTop: 106,
  temperatureBottom: 236,
  rainTop: 270,
  rainBottom: 344,
  uvTop: 378,
  uvBottom: 446,
  humidityTop: 480,
  humidityBottom: 548,
  windTop: 582,
  windBottom: 650,
  windArrowY: 670,
  minimumWindScale: 5,
  maximumHumidityFloor: 70,
  minimumRainScale: 0.5
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

function compass(degrees) {
  const points = ["N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"];
  return points[Math.round(degrees / 22.5) % 16];
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

// A tidy step (1, 2, 2.5 or 5 times a power of ten) giving roughly the target number of ticks.
function niceStep(span, target) {
  const raw = span / target;
  const magnitude = 10 ** Math.floor(Math.log10(raw));
  const fraction = raw / magnitude;
  return (fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 2.5 ? 2.5 : fraction <= 5 ? 5 : 10) * magnitude;
}

// Tick values at multiples of the step that fall within the range.
function ticks(low, high, step) {
  const values = [];
  for (let value = Math.ceil(low / step - 1e-9) * step; value <= high + 1e-9; value += step) {
    values.push(+value.toFixed(6));
  }
  return values;
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

// Stretches the chart sections so the chart card matches the summary column's height on wide screens.
function fitLayout() {
  const base = chartLayout;
  if (window.matchMedia("(max-width: 900px)").matches) return base;
  stripElement.setAttribute("height", 0);
  axisElement.setAttribute("height", 0);
  const chrome = document.getElementById("hourly").offsetHeight;
  const extra = document.getElementById("summary").offsetHeight - chrome - base.height;
  if (extra <= 0) return base;
  const sections = [["temperatureTop", "temperatureBottom"], ["rainTop", "rainBottom"], ["uvTop", "uvBottom"], ["humidityTop", "humidityBottom"], ["windTop", "windBottom"]];
  const total = sections.reduce((sum, [top, bottom]) => sum + base[bottom] - base[top], 0);
  const scaled = { ...base };
  let offset = 0;
  sections.forEach(([top, bottom]) => {
    scaled[top] = base[top] + offset;
    offset += extra * (base[bottom] - base[top]) / total;
    scaled[bottom] = base[bottom] + offset;
  });
  scaled.windArrowY = base.windArrowY + offset;
  scaled.height = base.height + offset;
  return scaled;
}

function renderStrip(hourly, daylight) {
  const hours = windowHours(hourly);
  if (hours.length === 0) {
    statusElement.textContent = "No hourly data available for today.";
    statusElement.hidden = false;
    return;
  }
  document.getElementById("dashboard").hidden = false;
  const layout = fitLayout();

  // Temperature scale snapped to tidy steps.
  const temperatures = hours.flatMap(h => [h.temperatureMin, h.temperatureMax, h.feelsLikeMin, h.feelsLikeMax]).filter(t => t !== null);
  const rawLow = Math.min(...temperatures);
  const rawHigh = Math.max(...temperatures);
  const low = rawLow - 0.5;
  const high = rawHigh + 0.5;
  const step = Math.max(1, niceStep(high - low, 4));
  const temperatureY = t => layout.temperatureTop + (high - t) / (high - low) * (layout.temperatureBottom - layout.temperatureTop);

  // Rain scale.
  const rainScale = Math.max(layout.minimumRainScale, ...hours.map(h => h.precipitationMax ?? h.precipitationAverage ?? 0));
  const rainY = mm => layout.rainBottom - Math.min(mm, rainScale) / rainScale * (layout.rainBottom - layout.rainTop);

  // Stretch columns to fill the available width, scrolling only when they would get too narrow.
  const scroll = document.getElementById("strip-scroll");
  document.getElementById("dashboard").hidden = false;
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
    if (x2 > x1) stripElement.appendChild(svg("rect", { class: "night", x: x1, y: 0, width: x2 - x1, height: layout.windBottom }));
  });
  daylight.flatMap(d => [["sunrise", d.sunrise], ["sunset", d.sunset]]).filter(([, value]) => value).forEach(([kind, value]) => {
    const time = new Date(value);
    const x = timeX(time);
    if (x < 0 || x > width) return;
    stripElement.appendChild(svg("line", { class: "sun-line", x1: x, x2: x, y1: layout.sunY + 6, y2: layout.windBottom }));
    const label = svg("text", { class: "sun-label", x: x + 4, y: layout.sunY }, `${kind === "sunrise" ? "↑" : "↓"} ${clockTime(time)}`);
    label.appendChild(svg("title", {}, `${kind === "sunrise" ? "Sunrise" : "Sunset"} ${clockTime(time)}`));
    stripElement.appendChild(label);
  });

  // Current hour highlight.
  const nowIndex = hours.findIndex(h => new Date(h.time).getTime() === now.getTime());
  if (nowIndex >= 0) {
    stripElement.appendChild(svg("rect", { class: "now-column", x: nowIndex * columnWidth + 3, y: 4, width: columnWidth - 6, height: layout.height - 8, rx: 12 }));
  }

  // Dividers between chart sections, across both the chart and the axis.
  [layout.uvTop, layout.humidityTop, layout.rainTop, layout.windTop].forEach(top => {
    const y = top - 21;
    stripElement.appendChild(svg("line", { class: "section-divider", x1: 0, x2: width, y1: y, y2: y }));
    axisElement.appendChild(svg("line", { class: "section-divider", x1: 0, x2: layout.axisWidth, y1: y, y2: y }));
  });

  // Gridlines and y axis labels.
  for (const t of ticks(low, high, step)) {
    stripElement.appendChild(svg("line", { class: "grid", x1: 0, x2: width, y1: temperatureY(t), y2: temperatureY(t) }));
    axisElement.appendChild(svg("text", { class: "axis-label axis-temperature", x: layout.axisWidth - 4, y: temperatureY(t) + 4 }, `${t}°`));
  }
  ticks(0, rainScale, niceStep(rainScale, 3)).forEach(mm => {
    stripElement.appendChild(svg("line", { class: mm === 0 ? "grid grid-base" : "grid", x1: 0, x2: width, y1: rainY(mm), y2: rainY(mm) }));
    axisElement.appendChild(svg("text", { class: "axis-label axis-rain", x: layout.axisWidth - 4, y: rainY(mm) + 4 }, mm === 0 ? "0" : `${+mm.toFixed(1)}`));
  });
  axisElement.appendChild(svg("text", { class: "axis-unit", x: layout.axisWidth - 4, y: layout.temperatureTop - 9 }, "°C"));
  axisElement.appendChild(svg("text", { class: "axis-unit", x: layout.axisWidth - 4, y: layout.rainTop - 9 }, "mm"));

  // UV index section between rain and humidity.
  const uvValues = hours.flatMap(h => [h.uvIndexMax, h.uvIndexClearSkyMax]).filter(u => u !== null);
  const uvScale = Math.max(layout.minimumUvScale, ...uvValues);
  const uvY = uv => layout.uvBottom - uv / uvScale * (layout.uvBottom - layout.uvTop);
  ticks(0, uvScale, Math.max(1, niceStep(uvScale, 3))).forEach(uv => {
    stripElement.appendChild(svg("line", { class: uv === 0 ? "grid grid-base" : "grid", x1: 0, x2: width, y1: uvY(uv), y2: uvY(uv) }));
    axisElement.appendChild(svg("text", { class: "axis-label", x: layout.axisWidth - 4, y: uvY(uv) + 4 }, `${uv}`));
  });
  axisElement.appendChild(svg("text", { class: "axis-unit", x: layout.axisWidth - 4, y: layout.uvTop - 9 }, "UV"));

  // Wind section below rain.
  const windValues = hours.flatMap(h => [h.windSpeedMax, h.windGustMax]).filter(w => w !== null);
  const windScale = Math.max(layout.minimumWindScale, ...windValues);
  const windY = mph => layout.windBottom - mph / windScale * (layout.windBottom - layout.windTop);
  ticks(0, windScale, Math.max(1, niceStep(windScale, 3))).forEach(mph => {
    stripElement.appendChild(svg("line", { class: mph === 0 ? "grid grid-base" : "grid", x1: 0, x2: width, y1: windY(mph), y2: windY(mph) }));
    axisElement.appendChild(svg("text", { class: "axis-label", x: layout.axisWidth - 4, y: windY(mph) + 4 }, `${mph}`));
  });
  axisElement.appendChild(svg("text", { class: "axis-unit", x: layout.axisWidth - 4, y: layout.windTop - 9 }, "mph"));

  // Humidity section between UV and wind.
  // Axis starts just below the day's lowest humidity (rounded down to 10%) and runs to 100%.
  const humidityValues = hours.map(h => h.humidityMin ?? h.humidityAverage).filter(rh => rh !== null);
  const humidityFloor = Math.min(layout.maximumHumidityFloor, Math.floor((Math.min(100, ...humidityValues) - 5) / 10) * 10);
  const humidityY = rh => layout.humidityBottom - (Math.max(rh, humidityFloor) - humidityFloor) / (100 - humidityFloor) * (layout.humidityBottom - layout.humidityTop);
  ticks(humidityFloor, 100, niceStep(100 - humidityFloor, 3)).forEach(rh => {
    stripElement.appendChild(svg("line", { class: rh === humidityFloor ? "grid grid-base" : "grid", x1: 0, x2: width, y1: humidityY(rh), y2: humidityY(rh) }));
    axisElement.appendChild(svg("text", { class: "axis-label", x: layout.axisWidth - 4, y: humidityY(rh) + 4 }, `${rh}`));
  });
  axisElement.appendChild(svg("text", { class: "axis-unit", x: layout.axisWidth - 4, y: layout.humidityTop - 9 }, "%"));

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
    if (h.windDirectionAverage !== null) {
      // Arrow points the way the wind is blowing, i.e. opposite to where it comes from.
      const arrow = svg("path", { class: "wind-arrow", d: "M 0 -7 L 5 4 L 0 2 L -5 4 Z", transform: `translate(${centre(i)} ${layout.windArrowY}) rotate(${h.windDirectionAverage + 180})` });
      arrow.appendChild(svg("title", {}, `From ${compass(h.windDirectionAverage)}`));
      stripElement.appendChild(arrow);
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
  band("range-wind", "windSpeedMin", "windSpeedMax", windY);
  band("range-wind range-wind-gust", "windGustMin", "windGustMax", windY);
  const windPoints = hours.map((h, i) => [centre(i), h.windSpeedAverage]).filter(([, w]) => w !== null).map(([x, w]) => [x, windY(w)]);
  const gustPoints = hours.map((h, i) => [centre(i), h.windGustAverage]).filter(([, w]) => w !== null).map(([x, w]) => [x, windY(w)]);
  if (gustPoints.length > 1) {
    stripElement.appendChild(svg("path", { class: "wind-line wind-gust", d: linearPath(gustPoints) }));
  }
  if (windPoints.length > 1) {
    stripElement.appendChild(svg("path", { class: "wind-line", d: linearPath(windPoints) }));
  }
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
    if (h.windGustAverage !== null) {
      valueLabel("value-wind value-wind-gust", x, windY(h.windGustAverage), true, `${Math.round(h.windGustAverage)}`);
    }
    if (h.windSpeedAverage !== null) {
      valueLabel("value-wind", x, windY(h.windSpeedAverage), h.windGustAverage === null, `${Math.round(h.windSpeedAverage)}`);
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

// Daily summary tiles: headline is now, bar shows the 24 hour range with daylight shaded.
function element(tag, className, text) {
  const node = document.createElement(tag);
  if (className) node.className = className;
  if (text !== undefined) node.textContent = text;
  return node;
}

function periodStats(period, key) {
  const entries = period.map(h => [h, h[`${key}Average`]]).filter(([, v]) => v !== null && v !== undefined);
  if (entries.length === 0) return null;
  const values = entries.map(([, v]) => v);
  const maxEntry = entries.reduce((best, entry) => entry[1] > best[1] ? entry : best);
  return {
    min: Math.min(...values),
    max: Math.max(...values),
    avg: values.reduce((sum, v) => sum + v, 0) / values.length,
    total: values.reduce((sum, v) => sum + v, 0),
    peakTime: new Date(maxEntry[0].time)
  };
}

// A small table with a Daylight row and a 24 Hours row.
function statsTable(columns, rows) {
  const table = element("table", "tile-table");
  const head = element("tr");
  head.appendChild(element("th"));
  columns.forEach(c => head.appendChild(element("th", "", c)));
  table.appendChild(element("thead")).appendChild(head);
  const body = table.appendChild(element("tbody"));
  rows.forEach(([label, values]) => {
    const row = element("tr");
    row.appendChild(element("th", "", label));
    values.forEach(v => row.appendChild(element("td", "", v)));
    body.appendChild(row);
  });
  return table;
}

function summaryTile(title, colour, headline, secondary, table, highlight) {
  const tile = element("article", "tile");
  const heading = element("h3", "tile-title", title);
  heading.style.setProperty("--key", colour);
  tile.appendChild(heading);
  const main = element("div", "tile-main");
  main.appendChild(element("span", "tile-headline", headline));
  main.appendChild(element("span", "tile-secondary", secondary ? `Now · ${secondary}` : "Now"));
  tile.appendChild(main);
  tile.appendChild(table);
  if (highlight) tile.appendChild(element("p", "tile-detail", highlight));
  return tile;
}

function renderSummary(hourly, daylight) {
  const hours = windowHours(hourly);
  const container = document.getElementById("summary");
  container.replaceChildren();
  if (hours.length === 0) return;
  const now = new Date();
  now.setMinutes(0, 0, 0);
  const current = hours.find(h => new Date(h.time).getTime() === now.getTime()) ?? {};
  const nights = nightPeriods(daylight);
  const dayHours = hours.filter(h => {
    const middle = new Date(new Date(h.time).getTime() + 30 * 60 * 1000);
    return !nights.some(([from, to]) => middle >= from && middle < to);
  });
  const value = (key, digits, unit) => {
    const v = current[`${key}Average`];
    return v === null || v === undefined ? "–" : `${v.toFixed(digits)}${unit}`;
  };
  const formatter = (digits, unit) => v => `${v.toFixed(digits)}${unit}`;
  const at = date => hourLabel(date);

  // Low / Avg / High table for a metric over daylight and 24 hours.
  const rangeTable = (key, format) => statsTable(["Low", "Avg", "High"], [["Daylight", dayHours], ["24 Hours", hours]].map(([label, period]) => {
    const stats = periodStats(period, key);
    return [label, stats ? [format(stats.min), format(stats.avg), format(stats.max)] : ["–", "–", "–"]];
  }));

  // Temperature.
  const temperature = periodStats(hours, "temperature");
  if (temperature) {
    const degrees = formatter(0, "°");
    container.appendChild(summaryTile("Temperature", "var(--temperature)", value("temperature", 0, "°"), "", rangeTable("temperature", degrees), `Warmest ${degrees(temperature.max)} at ${at(temperature.peakTime)}`));
  }

  // Feels like.
  const feelsLike = periodStats(hours, "feelsLike");
  if (feelsLike) {
    const degrees = formatter(0, "°");
    const coldest = hours.reduce((best, h) => h.feelsLikeAverage !== null && (best === null || h.feelsLikeAverage < best.feelsLikeAverage) ? h : best, null);
    container.appendChild(summaryTile("Feels Like", "var(--feels-like)", value("feelsLike", 0, "°"), "", rangeTable("feelsLike", degrees), coldest ? `Coldest ${degrees(coldest.feelsLikeAverage)} at ${at(new Date(coldest.time))}` : ""));
  }

  // Rain: total, wettest hour and highest chance.
  const rain = periodStats(hours, "precipitation");
  if (rain) {
    const mm = formatter(1, " mm");
    const table = statsTable(["Total", "Wettest", "Chance"], [["Daylight", dayHours], ["24 Hours", hours]].map(([label, period]) => {
      const amount = periodStats(period, "precipitation");
      const chance = periodStats(period, "precipitationProbability");
      return [label, [amount ? mm(amount.total) : "–", amount ? mm(amount.max) : "–", chance ? `${Math.round(chance.max)}%` : "–"]];
    }));
    container.appendChild(summaryTile("Rain", "rgb(var(--rain))", value("precipitation", 1, " mm"), `${value("precipitationProbability", 0, "%")} Chance`, table, rain.max > 0 ? `Wettest ${mm(rain.max)} at ${at(rain.peakTime)}` : "No Rain Expected"));
  }

  // UV.
  const uv = periodStats(hours, "uvIndex");
  const uvClearSky = periodStats(hours, "uvIndexClearSky");
  if (uv) {
    const index = formatter(1, "");
    container.appendChild(summaryTile("UV", "var(--uv)", value("uvIndex", 1, ""), `Clear Sky ${value("uvIndexClearSky", 1, "")}`, rangeTable("uvIndex", index), `Peak ${index(uv.max)} at ${at(uv.peakTime)}${uvClearSky ? ` (Clear Sky ${index(uvClearSky.max)})` : ""}`));
  }

  // Humidity.
  const humidity = periodStats(hours, "humidity");
  if (humidity) {
    container.appendChild(summaryTile("Humidity", "var(--humidity)", value("humidity", 0, "%"), "", rangeTable("humidity", formatter(0, "%"))));
  }

  // Wind.
  const wind = periodStats(hours, "windSpeed");
  const gust = periodStats(hours, "windGust");
  if (wind) {
    const mph = formatter(0, " mph");
    const direction = current.windDirectionAverage === null || current.windDirectionAverage === undefined ? "" : ` from ${compass(current.windDirectionAverage)}`;
    container.appendChild(summaryTile("Wind", "var(--wind)", value("windSpeed", 0, " mph"), `Gusts ${value("windGust", 0, " mph")}${direction}`, rangeTable("windSpeed", mph), gust ? `Strongest Gust ${mph(gust.max)} at ${at(gust.peakTime)}` : ""));
  }
}

async function load(latitude, longitude) {
  statusElement.textContent = "Loading Forecast…";
  try {
    const response = await fetch(`?handler=forecast&latitude=${latitude}&longitude=${longitude}`);
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    const report = await response.json();
    statusElement.hidden = true;
    lastReport = report;
    document.getElementById("dashboard").hidden = false;
    renderSummary(report.hourly, report.daylight);
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
