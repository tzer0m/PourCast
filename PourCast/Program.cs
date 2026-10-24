using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PourCast.Providers;
using PourCast.Providers.MetNorway;
using PourCast.Providers.MetOffice;
using PourCast.Providers.OpenMeteo;
using PourCast.Providers.OpenMeteoEnsemble;
using PourCast.Providers.PirateWeather;
using PourCast.Providers.TomorrowIo;
using PourCast.Providers.VisualCrossing;
using PourCast.Providers.WeatherApi;
using PourCast.Services;
using PourCast.Services.Geocoding;
using PourCast.Services.Usage;
using System.Text.Json.Serialization;

// Create web application builder.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// Register weather providers. Each HTTP client counts its calls against the provider's daily limit.
builder.Services.AddMemoryCache();
builder.Services.Configure<UsageOptions>(builder.Configuration.GetSection("Usage"));
builder.Services.AddSingleton<UsageTracker>();
builder.Services.Configure<MetOfficeOptions>(builder.Configuration.GetSection("Providers:MetOffice"));
builder.Services.AddHttpClient<IWeatherProvider, MetOfficeProvider>("MetOffice", (IServiceProvider services, HttpClient client) => client.BaseAddress = new Uri(services.GetRequiredService<IOptions<MetOfficeOptions>>().Value.BaseUrl)).AddHttpMessageHandler((IServiceProvider services) => UsageHandlerFor(services, services.GetRequiredService<IOptions<MetOfficeOptions>>().Value));
AddOpenMeteo("OpenMeteoUkmo");
AddOpenMeteo("OpenMeteoEcmwf");
AddOpenMeteo("OpenMeteoIcon");
AddOpenMeteo("OpenMeteoGfs");
AddOpenMeteo("OpenMeteoMeteoFrance");
builder.Services.Configure<MetNorwayOptions>(builder.Configuration.GetSection("Providers:MetNorway"));
builder.Services.AddHttpClient<IWeatherProvider, MetNorwayProvider>("MetNorway", (IServiceProvider services, HttpClient client) => client.BaseAddress = new Uri(services.GetRequiredService<IOptions<MetNorwayOptions>>().Value.BaseUrl)).AddHttpMessageHandler((IServiceProvider services) => UsageHandlerFor(services, services.GetRequiredService<IOptions<MetNorwayOptions>>().Value));
builder.Services.Configure<PirateWeatherOptions>(builder.Configuration.GetSection("Providers:PirateWeather"));
builder.Services.AddHttpClient<IWeatherProvider, PirateWeatherProvider>("PirateWeather", (IServiceProvider services, HttpClient client) => client.BaseAddress = new Uri(services.GetRequiredService<IOptions<PirateWeatherOptions>>().Value.BaseUrl)).AddHttpMessageHandler((IServiceProvider services) => UsageHandlerFor(services, services.GetRequiredService<IOptions<PirateWeatherOptions>>().Value));
builder.Services.Configure<TomorrowIoOptions>(builder.Configuration.GetSection("Providers:TomorrowIo"));
builder.Services.AddHttpClient<IWeatherProvider, TomorrowIoProvider>("TomorrowIo", (IServiceProvider services, HttpClient client) => client.BaseAddress = new Uri(services.GetRequiredService<IOptions<TomorrowIoOptions>>().Value.BaseUrl)).AddHttpMessageHandler((IServiceProvider services) => UsageHandlerFor(services, services.GetRequiredService<IOptions<TomorrowIoOptions>>().Value));
builder.Services.Configure<WeatherApiOptions>(builder.Configuration.GetSection("Providers:WeatherApi"));
builder.Services.AddHttpClient<IWeatherProvider, WeatherApiProvider>("WeatherApi", (IServiceProvider services, HttpClient client) => client.BaseAddress = new Uri(services.GetRequiredService<IOptions<WeatherApiOptions>>().Value.BaseUrl)).AddHttpMessageHandler((IServiceProvider services) => UsageHandlerFor(services, services.GetRequiredService<IOptions<WeatherApiOptions>>().Value));
builder.Services.Configure<VisualCrossingOptions>(builder.Configuration.GetSection("Providers:VisualCrossing"));
builder.Services.AddHttpClient<IWeatherProvider, VisualCrossingProvider>("VisualCrossing", (IServiceProvider services, HttpClient client) => client.BaseAddress = new Uri(services.GetRequiredService<IOptions<VisualCrossingOptions>>().Value.BaseUrl)).AddHttpMessageHandler((IServiceProvider services) => UsageHandlerFor(services, services.GetRequiredService<IOptions<VisualCrossingOptions>>().Value));
builder.Services.Configure<OpenMeteoEnsembleOptions>(builder.Configuration.GetSection("Providers:OpenMeteoEnsemble"));
builder.Services.AddHttpClient<IWeatherProvider, OpenMeteoEnsembleProvider>("OpenMeteoEnsemble", (IServiceProvider services, HttpClient client) => client.BaseAddress = new Uri(services.GetRequiredService<IOptions<OpenMeteoEnsembleOptions>>().Value.BaseUrl)).AddHttpMessageHandler((IServiceProvider services) => UsageHandlerFor(services, services.GetRequiredService<IOptions<OpenMeteoEnsembleOptions>>().Value));
builder.Services.Configure<CombiningOptions>(builder.Configuration.GetSection("Combining"));
builder.Services.AddTransient<WeatherService>();

// Register place search.
builder.Services.Configure<GeocodingOptions>(builder.Configuration.GetSection("Geocoding"));
builder.Services.AddHttpClient<GeocodingService>((IServiceProvider services, HttpClient client) => client.BaseAddress = new Uri(services.GetRequiredService<IOptions<GeocodingOptions>>().Value.BaseUrl)).AddHttpMessageHandler((IServiceProvider services) => new UsageHandler(services.GetRequiredService<UsageTracker>(), services.GetRequiredService<IOptions<GeocodingOptions>>().Value.UsageKey, services.GetRequiredService<IOptions<GeocodingOptions>>().Value.DailyLimit));

// Create the web application and configure.
WebApplication app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.Run();

// Registers an Open-Meteo provider from its own appsettings section, so the same provider can be added once per weather model.
void AddOpenMeteo(string section)
{
    OpenMeteoOptions options = builder.Configuration.GetSection($"Providers:{section}").Get<OpenMeteoOptions>() ?? new OpenMeteoOptions();
    builder.Services.AddHttpClient(section, (HttpClient client) => client.BaseAddress = new Uri(options.BaseUrl)).AddHttpMessageHandler((IServiceProvider services) => UsageHandlerFor(services, options)).AddTypedClient<IWeatherProvider>((HttpClient client, IServiceProvider services) => new OpenMeteoProvider(client, Options.Create(options), services.GetRequiredService<IMemoryCache>()));
}

// Creates the usage handler for a provider's HTTP client from its options.
static UsageHandler UsageHandlerFor(IServiceProvider services, ProviderOptions options)
{
    return new UsageHandler(services.GetRequiredService<UsageTracker>(), options.UsageKey, options.DailyLimit);
}