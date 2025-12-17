using Microsoft.Extensions.Hosting;
using Serilog;
using SpaceLoctor;
using SpaceLoctor.Services;

var builder = Host.CreateApplicationBuilder(args);
// 1. Read log directory from config
string logDir = builder.Configuration["ScanSettings:LogFilePath"]
                ?? "Logs"; // fallback

// Ensure directory exists
Directory.CreateDirectory(logDir);
// Configure Serilog BEFORE builder.Build()
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(path: Path.Combine(logDir, "log-.txt"), rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Logging.ClearProviders();     // remove default logging
builder.Logging.AddSerilog(Log.Logger); // plug Serilog into .NET logging

builder.Services.AddWindowsService();
builder.Services.AddSingleton<FolderScanner>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();