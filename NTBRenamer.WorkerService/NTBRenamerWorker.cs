using NTBRenamer.WorkerService.Services;

namespace NTBRenamer.WorkerService;

public class NTBRenamerWorker(ILogger<NTBRenamerWorker> logger, IRenamerService renamerService) : BackgroundService
{
    private Timer _timer = null;
    private CancellationToken _cancellationToken = default;
    private int _minuteCount = 0;
    private int runCount = 0;
    private bool isRunning = false;

    public override Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cancellationToken = cancellationToken;
        _timer = new(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        => _cancellationToken = cancellationToken;

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _timer?.Change(Timeout.Infinite, 0);

        return base.StopAsync(cancellationToken);
    }

    private async void DoWork(object? state)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (isRunning)
            return;
        isRunning = true;

        _minuteCount++;

        logger.Log(LogLevel.Information, "Started Processing...");

        await renamerService.ProcessFiles();

        logger.Log(LogLevel.Information, "Stoped Processing...");

        isRunning = false;

        _minuteCount = 0;
        runCount++;
    }
}
