# PourCast

[![Deploy](https://github.com/tzer0m/PourCast/actions/workflows/deploy.yml/badge.svg)](https://github.com/tzer0m/PourCast/actions/workflows/deploy.yml) [![Dependabot](https://github.com/tzer0m/PourCast/actions/workflows/dependabot/dependabot-updates/badge.svg)](https://github.com/tzer0m/PourCast/actions/workflows/dependabot/dependabot-updates)

A personal weather site that pulls forecasts from a dozen sources and combines them, showing the most likely forecast alongside how much the sources agree. Covers any single day hourly, 7 days and 30 days, for your current location or any searched place.

Built with ASP.NET Core Razor Pages (.NET 10).

## Deployment

Runs on Tyrion as a systemd service (`PourCast.service`, port 7140) behind Cloudflare Tunnel at pc.tzer0m.co.uk. Pushes to `master` are published and deployed by GitHub Actions on the self-hosted runner.

## Attribution

Weather data:

- Contains Met Office data © Crown copyright, from [Met Office DataHub](https://datahub.metoffice.gov.uk/).
- Weather data by [Open-Meteo.com](https://open-meteo.com/), licensed under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).
- Data from [MET Norway](https://api.met.no/), licensed under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).
- Powered by [Pirate Weather](https://pirateweather.net/).
- Powered by [Tomorrow.io](https://www.tomorrow.io/).
- Powered by [WeatherAPI.com](https://www.weatherapi.com/).
- Weather data provided by [Visual Crossing](https://www.visualcrossing.com/).
- Weather data provided by [OpenWeather](https://openweathermap.org/).
- Place search data from [GeoNames](https://www.geonames.org/), via Open-Meteo, licensed under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).

Icons:

- Weather condition icons: [Meteocons](https://github.com/basmilius/weather-icons) by Bas Milius, [MIT licence](https://github.com/basmilius/weather-icons/blob/dev/LICENSE).
- App icon: <a href="https://www.flaticon.com/free-icons/meteorology" title="meteorology icons">Meteorology icons created by Magnific - Flaticon</a>.
