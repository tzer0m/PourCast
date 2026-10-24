using Microsoft.Extensions.Options;
using PourCast.Providers;
using PourCast.Providers.MetOffice;
using PourCast.Providers.OpenMeteo;
using PourCast.Services;
using PourCast.Services.Geocoding;
using System.Text.Json.Serialization;

// Create web application builder.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// Register weather providers.
builder.Services.AddMemoryCache();
builder.Services.Configure<MetOfficeOptions>(builder.Configuration.GetSection("Providers:MetOffice"));
builder.Services.AddHttpClient<IWeatherProvider, MetOfficeProvider>("MetOffice", (IServiceProvider services, HttpClient client) => client.BaseAddress = new Uri(services.GetRequiredService<IOptions<MetOfficeOptions>>().Value.BaseUrl));
builder.Services.Configure<OpenMeteoOptions>(builder.Configuration.GetSection("Providers:OpenMeteo"));
builder.Services.AddHttpClient<IWeatherProvider, OpenMeteoProvider>("OpenMeteo", (IServiceProvider services, HttpClient client) => client.BaseAddress = new Uri(services.GetRequiredService<IOptions<OpenMeteoOptions>>().Value.BaseUrl));
builder.Services.AddTransient<WeatherService>();

// Register place search.
builder.Services.Configure<GeocodingOptions>(builder.Configuration.GetSection("Geocoding"));
builder.Services.AddHttpClient<GeocodingService>((IServiceProvider services, HttpClient client) => client.BaseAddress = new Uri(services.GetRequiredService<IOptions<GeocodingOptions>>().Value.BaseUrl));

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