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
  Sleet: "Sleet",
  Hail: "Hail",
  Showers: "Showers",
  Snow: "Snow",
  SnowShowers: "Snow Showers",
  Thunderstorm: "Thunderstorm"
};

const statusElement = document.getElementById("status");

function format(value, unit, digits = 0) {
  return value === null || value === undefined ? "–" : `${value.toFixed(digits)}${unit}`;
}

function compass(degrees) {
  if (degrees === null || degrees === undefined) return "";
  const points = ["N", "NE", "E", "SE", "S", "SW", "W", "NW"];
  return points[Math.round(degrees / 45) % 8];
}

function time(value) {
  return value ? new Date(value).toLocaleTimeString("en-GB", { hour: "2-digit", minute: "2-digit" }) : "–";
}

function cell(row, text) {
  const td = document.createElement("td");
  td.textContent = text;
  row.appendChild(td);
}

function renderCurrent(forecast) {
  const current = forecast.current;
  if (!current) return;
  document.getElementById("current-temp").textContent = format(current.temperature, "°C");
  document.getElementById("current-condition").textContent = conditionLabels[current.condition];
  document.getElementById("current-feels").textContent = format(current.feelsLike, "°C");
  document.getElementById("current-wind").textContent = `${format(current.windSpeed, " mph")} ${compass(current.windDirection)} (Gusts ${format(current.windGust, " mph")})`;
  document.getElementById("current-humidity").textContent = format(current.humidity, "%");
  document.getElementById("current-pressure").textContent = format(current.pressure, " hPa");
  document.getElementById("current-source").textContent = forecast.locationName ? `${forecast.source} (${forecast.locationName})` : forecast.source;
  document.getElementById("current").hidden = false;
}

function renderHourly(forecast) {
  const body = document.getElementById("hourly-body");
  body.replaceChildren();
  const now = Date.now() - 60 * 60 * 1000;
  forecast.hourly.filter(h => new Date(h.time).getTime() >= now).slice(0, 24).forEach(h => {
    const row = document.createElement("tr");
    cell(row, time(h.time));
    cell(row, conditionLabels[h.condition]);
    cell(row, format(h.temperature, "°C"));
    cell(row, format(h.feelsLike, "°C"));
    cell(row, format(h.precipitationProbability, "%"));
    cell(row, format(h.precipitation, " mm", 1));
    cell(row, `${format(h.windSpeed, " mph")} ${compass(h.windDirection)}`);
    cell(row, format(h.windGust, " mph"));
    body.appendChild(row);
  });
  document.getElementById("hourly").hidden = false;
}

function renderDaily(forecast) {
  const body = document.getElementById("daily-body");
  body.replaceChildren();
  forecast.daily.forEach(d => {
    const row = document.createElement("tr");
    cell(row, new Date(d.date).toLocaleDateString("en-GB", { weekday: "short", day: "numeric", month: "short" }));
    cell(row, conditionLabels[d.condition]);
    cell(row, format(d.temperatureMax, "°C"));
    cell(row, format(d.temperatureMin, "°C"));
    cell(row, format(d.precipitationProbability, "%"));
    cell(row, format(d.precipitationTotal, " mm", 1));
    cell(row, format(d.windSpeedMax, " mph"));
    cell(row, format(d.windGustMax, " mph"));
    cell(row, time(d.sunrise));
    cell(row, time(d.sunset));
    body.appendChild(row);
  });
  document.getElementById("daily").hidden = false;
}

async function load(latitude, longitude) {
  statusElement.textContent = "Loading Forecast…";
  try {
    const response = await fetch(`?handler=forecast&latitude=${latitude}&longitude=${longitude}`);
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    const forecast = await response.json();
    renderCurrent(forecast);
    renderHourly(forecast);
    renderDaily(forecast);
    statusElement.hidden = true;
  } catch (error) {
    statusElement.textContent = `Couldn't load the forecast (${error.message}).`;
  }
}

if ("geolocation" in navigator) {
  navigator.geolocation.getCurrentPosition(p => load(p.coords.latitude, p.coords.longitude), e => { statusElement.textContent = `Couldn't get your location (${e.message}).`; }, { timeout: 10000, maximumAge: 600000 });
} else {
  statusElement.textContent = "Your browser doesn't support location.";
}
