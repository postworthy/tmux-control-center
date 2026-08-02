using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TmuxMobile.Core;

namespace TmuxMobile.Infrastructure;

public sealed class InventoryStore(
    ITmuxService tmux,
    TimeProvider timeProvider,
    ILogger<InventoryStore> logger) : IInventoryStore
{
    private InventorySnapshot _current = new(0, DateTimeOffset.MinValue, []);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    public InventorySnapshot Current => Volatile.Read(ref _current);
    public event Action<InventorySnapshot>? Changed;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var sessions = await tmux.GetSessionsAsync(cancellationToken);
            var prior = Current;
            if (InventoryComparer.MeaningfullyEqual(prior.Sessions, sessions)) return;
            var next = new InventorySnapshot(prior.Version + 1, timeProvider.GetUtcNow(), sessions);
            Volatile.Write(ref _current, next);
            Changed?.Invoke(next);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Inventory refresh failed");
        }
        finally { _refreshLock.Release(); }
    }
}

public sealed class InventoryPollingService(
    IInventoryStore store,
    IOptions<TmuxOptions> options,
    ILogger<InventoryPollingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Inventory polling started");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.PollingIntervalSeconds));
        do { await store.RefreshAsync(stoppingToken); }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Inventory polling stopped");
        return base.StopAsync(cancellationToken);
    }
}

public sealed class JsonLineAuditLogger(
    IOptions<AuditOptions> options,
    TimeProvider timeProvider,
    IHostEnvironment environment) : IAuditLogger
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task WriteAsync(string action, string subject, string target, bool succeeded,
        CancellationToken cancellationToken)
    {
        var path = Path.IsPathFullyQualified(options.Value.Destination)
            ? options.Value.Destination
            : Path.Combine(environment.ContentRootPath, options.Value.Destination);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var line = JsonSerializer.Serialize(new
        {
            timestamp = timeProvider.GetUtcNow(),
            action,
            subject,
            target,
            succeeded
        }) + Environment.NewLine;
        await _gate.WaitAsync(cancellationToken);
        try { await File.AppendAllTextAsync(path, line, cancellationToken); }
        finally { _gate.Release(); }
    }
}
