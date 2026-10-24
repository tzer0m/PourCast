using Microsoft.Extensions.Options;
using PourCast.Providers;
using PourCast.Providers.MetOffice;
using PourCast.Providers.OpenMeteo;
using PourCast.Providers.OpenMeteoEnsemble;
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
builder.Services.Configure<OpenMeteoOptions>(builder.Configuration.GetSection("Providers:OpenMeteo"));
builder.Services.AddHttpClient<IWeatherProvider, OpenMeteoProvider>("OpenMeteo", (IServiceProvider services, HttpClient client) => client.BaseAddress = new Uri(services.GetRequiredService<IOptions<OpenMeteoOptions>>().Value.BaseUrl)).AddHttpMessageHandler((IServiceProvider services) => UsageHandlerFor(services, services.GetRequiredService<IOptions<OpenMeteoOptions>>().Value));
builder.Services.Configure<OpenMeteoEnsembleOptions>(builder.Configuration.GetSection("Providers:OpenMeteoEnsemble"));
builder.Services.AddHttpClient<IWeatherProvider, OpenMeteoEnsembleProvider>("OpenMeteoEnsemble", (IServiceProvider services, HttpClient client) => client.BaseAddress = new Uri(services.GetRequiredService<IOptions<OpenMeteoEnsembleOptions>>().Value.BaseUrl)).AddHttpMessageHandler((IServiceProvider services) => UsageHandlerFor(services, services.GetRequiredService<IOptions<OpenMeteoEnsembleOptions>>().Value));
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

// Creates the usage handler for a provider's HTTP client from its options.
static UsageHandler UsageHandlerFor(IServiceProvider services, ProviderOptions options)
{
    return new UsageHandler(services.GetRequiredService<UsageTracker>(), options.UsageKey, options.DailyLimit);
}