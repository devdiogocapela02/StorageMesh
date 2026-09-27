namespace StorageMesh.Server.Services;

public class NodeSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public NodeSyncBackgroundService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var sync = scope.ServiceProvider.GetRequiredService<NodeSyncService>();

                await sync.Sync();
            }
            catch
            {
                // Sync failed; try again on the next cycle.
            }

            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}
