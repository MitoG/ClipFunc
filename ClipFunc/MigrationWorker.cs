using ClipFunc.DataContext;
using Microsoft.EntityFrameworkCore;

namespace ClipFunc;

public class MigrationWorker : BackgroundService
{
    public bool MigrationDone { get; private set; }
    
    private readonly IDbContextFactory<ClipFuncContext> _contextFactory;
    private readonly ILogger<MigrationWorker> _logger;

    public MigrationWorker(IDbContextFactory<ClipFuncContext> contextFactory, ILogger<MigrationWorker> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        MigrationDone = false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting MigrationWorker");
        await using var context = await _contextFactory.CreateDbContextAsync(stoppingToken);

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync(stoppingToken);
        var pendingMigrationCount = pendingMigrations.Count();
        if (pendingMigrationCount <= 0)
            return;

        _logger.LogInformation("Starting {pendingMigrationCount} migrations", pendingMigrationCount);
        await context.Database.MigrateAsync(stoppingToken);

        await context.Database.EnsureCreatedAsync(stoppingToken);

        MigrationDone = true;
    }
}