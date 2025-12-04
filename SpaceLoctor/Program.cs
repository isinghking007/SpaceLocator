using SpaceLoctor;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService();
builder.Services.AddHostedService<Worker>();
var host = builder.Build();
await host.RunAsync();