using SpaceLoctor.Services;

namespace SpaceLoctor
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly FolderScanner _folderScanner;
        public Worker(ILogger<Worker> logger, IConfiguration configuration, FolderScanner folderScanner)
        {
            _logger = logger;
            _configuration = configuration;
            _folderScanner = folderScanner;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string emergencyPath = "D:\\logs";
            string rootPath = _configuration["ScanSettings:RootPath"] ?? emergencyPath;
            int interval = int.Parse(_configuration["ScanSettings:ScanIntervalMinutes"] ?? "60");
            int maxThreads = int.Parse(_configuration["ScanSettings:MaxParallelThreads"] ?? "3");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Scan started at {Time}", DateTimeOffset.Now);
                var scanStartTime= DateTimeOffset.Now;
                _logger.LogInformation(
                    "Scanning root path: {Path} with max {Threads} threads.",
                    rootPath,
                    maxThreads
                );

                Dictionary<string, long> result;

                try
                {
                    result = await _folderScanner.ScanTopFolderAsync(
                        rootPath,
                        maxThreads,
                        stoppingToken
                    );
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Scan was cancelled.");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during folder scanning.");
                    await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
                    continue;
                }

                //  PRINT ONLY IMMEDIATE CHILD FOLDERS
                var immediateChildren = result
                    .Where(kvp =>
                    {
                        try
                        {
                            var parent = Directory.GetParent(kvp.Key);
                            return parent != null &&
                                   string.Equals(
                                       parent.FullName.TrimEnd('\\'),
                                       rootPath.TrimEnd('\\'),
                                       StringComparison.OrdinalIgnoreCase);
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .OrderBy(kvp => kvp.Key);

                foreach (var kvp in immediateChildren)
                {
                    _logger.LogInformation(
                        "Folder: {Folder} -> {SizeBytes} bytes ({SizeMB:F2} MB)",
                        kvp.Key,
                        kvp.Value,
                        kvp.Value / 1024.0 / 1024.0
                    );
                }

                _logger.LogInformation(
                    "Scan completed at {Time}. Next scan in {Interval} minutes.",
                    DateTimeOffset.Now,
                    interval
                );
                var scanEndTime = DateTimeOffset.Now;
                var scanDuration = scanEndTime - scanStartTime;
                _logger.LogInformation(
                    "Total scan duration: {Duration} seconds.",
                    scanDuration.TotalSeconds
                );  


                await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
            }
        }
    }
}
