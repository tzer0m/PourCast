import { element, compass, hourLabel, windowHours, nightPeriods } from "/js/helpers.js";

// Daily summary tiles: current value, highlights, and daylight vs 24 hour sliders.
function periodStats(period, key) {
  const entries = period.map(h => [h, h[`${key}Average`]]).filter(([, v]) => v !== null && v !== undefined);
  if (entries.length === 0) return null;
  const values = entries.map(([, v]) => v);
  const maxEntry = entries.reduce((best, entry) => entry[1] > best[1] ? entry : best);
  const minEntry = entries.reduce((best, entry) => entry[1] < best[1] ? entry : best);
  return {
    min: Math.min(...values),
    max: Math.max(...values),
    avg: values.reduce((sum, v) => sum + v, 0) / values.length,
    total: values.reduce((sum, v) => sum + v, 0),
    peakTime: new Date(maxEntry[0].time),
    lowTime: new Date(minEntry[0].time)
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
  tile.style.setProperty("--key", colour);
  // Title and current value on the left, highlights top right alongside both.
  const main = element("div", "tile-main");
  const left = element("div");
  left.appendChild(element("h3", "tile-title", title));
  const current = element("div", "tile-current");
  current.appendChild(element("span", "tile-headline", headline));
  if (secondary) current.appendChild(element("span", "tile-secondary", secondary));
  left.appendChild(current);
  main.appendChild(left);
  // Highlights are [value, time, tooltip]: a big value with the time next to it, stacked top right.
  const highlights = (highlight ?? []).filter(Boolean);
  if (highlights.length > 0) {
    const stack = element("div", "tile-highlights");
    highlights.forEach(([highlightValue, highlightTime, tooltip]) => {
      const peak = element("span", "tile-highlight");
      peak.title = tooltip;
      peak.appendChild(element("span", "tile-highlight-value", highlightValue));
      if (highlightTime) peak.appendChild(element("span", "tile-highlight-time", highlightTime));
      stack.appendChild(peak);
    });
    main.appendChild(stack);
  }
  tile.appendChild(main);
  tile.appendChild(table);
  return tile;
}

export function renderSummary(hourly, daylight) {
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

  // Two sliders, daylight and 24 hours, on the same scale so they can be compared.
  const rangeTable = (key, format) => {
    const all = periodStats(hours, key);
    const span = all.max - all.min || 1;
    const position = v => (v - all.min) / span * 100;
    const nowValue = current[`${key}Average`];
    const container = element("div", "sliders");
    [["Daylight", dayHours], ["24 Hours", hours]].forEach(([label, period]) => {
      const stats = periodStats(period, key);
      container.appendChild(element("span", "slider-label", label));
      container.appendChild(element("span", "slider-end", stats ? format(stats.min) : "–"));
      const track = element("div", "slider-track");
      if (stats) {
        const fill = element("div", "slider-fill");
        fill.style.left = `${position(stats.min)}%`;
        fill.style.width = `${position(stats.max) - position(stats.min)}%`;
        track.appendChild(fill);
        const average = element("div", "slider-average");
        average.style.left = `${position(stats.avg)}%`;
        average.title = `Avg ${format(stats.avg)}`;
        track.appendChild(average);
      }
      if (nowValue !== null && nowValue !== undefined) {
        const dot = element("div", "slider-now");
        dot.style.left = `${Math.min(100, Math.max(0, position(nowValue)))}%`;
        dot.title = `Now ${format(nowValue)}`;
        track.appendChild(dot);
      }
      container.appendChild(track);
      container.appendChild(element("span", "slider-end", stats ? format(stats.max) : "–"));
    });
    return container;
  };

  // Temperature.
  const temperature = periodStats(hours, "temperature");
  if (temperature) {
    const degrees = formatter(0, "°");
    container.appendChild(summaryTile("Temperature", "var(--temperature)", value("temperature", 0, "°"), "", rangeTable("temperature", degrees), [[`↑${degrees(temperature.max)}`, at(temperature.peakTime), "Warmest"], [`↓${degrees(temperature.min)}`, at(temperature.lowTime), "Coldest"]]));
  }

  // Feels like.
  const feelsLike = periodStats(hours, "feelsLike");
  if (feelsLike) {
    const degrees = formatter(0, "°");
    container.appendChild(summaryTile("Feels Like", "var(--feels-like)", value("feelsLike", 0, "°"), "", rangeTable("feelsLike", degrees), [[`↑${degrees(feelsLike.max)}`, at(feelsLike.peakTime), "Warmest"], [`↓${degrees(feelsLike.min)}`, at(feelsLike.lowTime), "Coldest"]]));
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
    container.appendChild(summaryTile("Rain", "rgb(var(--rain))", value("precipitation", 1, " mm"), `${value("precipitationProbability", 0, "%")} Chance`, table, [rain.max > 0 ? [`↑${mm(rain.max)}`, at(rain.peakTime), "Wettest Hour"] : ["Dry", "", "No Rain Expected"]]));
  }

  // UV.
  const uv = periodStats(hours, "uvIndex");
  const uvClearSky = periodStats(hours, "uvIndexClearSky");
  if (uv) {
    const index = formatter(1, "");
    container.appendChild(summaryTile("UV", "var(--uv)", value("uvIndex", 1, ""), `Clear Sky ${value("uvIndexClearSky", 1, "")}`, rangeTable("uvIndex", index), [[`↑${index(uv.max)}`, at(uv.peakTime), uvClearSky ? `Peak (Clear Sky ${index(uvClearSky.max)})` : "Peak"]]));
  }

  // Humidity.
  const humidity = periodStats(hours, "humidity");
  if (humidity) {
    container.appendChild(summaryTile("Humidity", "var(--humidity)", value("humidity", 0, "%"), "", rangeTable("humidity", formatter(0, "%")), [[`↑${Math.round(humidity.max)}%`, at(humidity.peakTime), "Most Humid"]]));
  }

  // Wind.
  const wind = periodStats(hours, "windSpeed");
  const gust = periodStats(hours, "windGust");
  if (wind) {
    const mph = formatter(0, " mph");
    const direction = current.windDirectionAverage === null || current.windDirectionAverage === undefined ? "" : ` from ${compass(current.windDirectionAverage)}`;
    container.appendChild(summaryTile("Wind", "var(--wind)", value("windSpeed", 0, " mph"), `Gusts ${value("windGust", 0, " mph")}${direction}`, rangeTable("windSpeed", mph), [[`↑${mph(wind.max)}`, at(wind.peakTime), "Strongest Wind"], gust ? [`↑G ${mph(gust.max)}`, at(gust.peakTime), "Strongest Gust"] : null]));
  }
}
