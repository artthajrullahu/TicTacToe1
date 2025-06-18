using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TicTacToe.Client;
using TicTacToe.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HTTP client
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add logging
builder.Services.AddLogging();

// Register services
builder.Services.AddScoped<IGameHubService, GameHubService>();
builder.Services.AddScoped<IApiService, ApiService>();

// Configure API base URL from appsettings.json
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7001";
builder.Services.Configure<ApiServiceOptions>(options =>
{
    options.BaseUrl = apiBaseUrl;
});

// Build and run the application
await builder.Build().RunAsync();
