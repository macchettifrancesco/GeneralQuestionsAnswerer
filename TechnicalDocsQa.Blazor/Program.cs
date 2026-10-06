using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TechnicalDocsQa.Blazor;
using TechnicalDocsQa.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure API base address
var apiBaseAddress = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:8000";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseAddress) });

// Register API clients
builder.Services.AddScoped<QuestionApiClient>();
builder.Services.AddScoped<DocumentApiClient>();

await builder.Build().RunAsync();
