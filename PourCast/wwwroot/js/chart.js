import { conditionIcons, conditionLabels } from "/js/conditions.js";
import { svg, compass, hourLabel, clockTime, niceStep, ticks, linearPath, smoothPath, nightPeriods } from "/js/helpers.js";

const axisElement = document.getElementById("axis");
const stripElement = document.getElementById("strip");

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

// Draws the chart for a set of columns, each stepHours long. Returns false if there are no columns.
export function renderChart(hours, daylight, stepHours) {
  if (hours.length === 0) {
    return false;
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

  stripElement.replaceChildren();
  axisElement.replaceChildren();
  stripElement.setAttribute("width", width);
  stripElement.setAttribute("height", layout.height);
  stripElement.setAttribute("viewBox", `0 0 ${width} ${layout.height}`);

  // Night shading and sunrise/sunset markers.
  const start = new Date(hours[0].time).getTime();
  const stepMs = stepHours * 60 * 60 * 1000;
  const timeX = date => (date.getTime() - start) / stepMs * columnWidth;
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
  const nowIndex = hours.findIndex(h => new Date(h.time) <= now && now < new Date(new Date(h.time).getTime() + stepMs));
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
    // Hourly views divide days at midnight; the 7 day view divides them at each 6am start.
    const isDayStart = stepHours === 1 ? time.getHours() === 0 : time.getHours() === 6;
    if (isDayStart && i > 0) {
      stripElement.appendChild(svg("line", { class: "day-divider", x1: i * columnWidth, x2: i * columnWidth, y1: 8, y2: layout.height - 8 }));
    }
    const label = isNow ? "Now" : isDayStart && (stepHours > 1 || i > 0) ? time.toLocaleDateString("en-GB", { weekday: "short" }) : hourLabel(time);
    stripElement.appendChild(svg("text", { class: isNow ? "time-label time-now" : "time-label", x: centre(i), y: layout.timeY }, label));
    const icon = conditionIcons[h.condition] ?? conditionIcons.Unknown;
    const night = isNight(new Date(time.getTime() + stepMs / 2));
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

  scroll.scrollLeft = nowIndex >= 0 ? nowIndex * columnWidth : 0;
  return true;
}
