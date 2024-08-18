namespace OpenPrismNode.Web;

using MediatR;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Sync.Commands.GetPostgresBlockTip;
using OpenPrismNode.Sync.Services;

/// <summary>
/// Service than automatically syncs the entire network.
/// The service starts automatically when the application starts, but can be stopped and restarted manually
/// </summary>
public class BackgroundSyncService : BackgroundService
{
    private readonly ILogger<BackgroundSyncService> _logger;
    private readonly AppSettings _appSettings;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private CancellationTokenSource _cts;
    private readonly IMediator _mediator;

    /// <inheritdoc />
    public BackgroundSyncService(IOptions<AppSettings> appSettings, ILogger<BackgroundSyncService> logger, IServiceScopeFactory serviceScopeFactory, IMediator mediator)
    {
        _logger = logger;
        _appSettings = appSettings.Value;
        _serviceScopeFactory = serviceScopeFactory;
        _cts = new CancellationTokenSource();
        _mediator = mediator;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var isInitialStartup = true;

        _logger.LogInformation($"Starting background sync for {_appSettings.PrismNetwork.Name}");
        var postgresSqlTip = await mediator.Send(new GetPostgresBlockTipRequest(), stoppingToken);
        if (postgresSqlTip.IsFailed)
        {
            _logger.LogCritical($"Cannot get the postgres tip (dbSync) for syncing {_appSettings.PrismNetwork.Name}: {postgresSqlTip.Errors.First().Message}");
            return;
        }

        _logger.LogInformation($"Postgres (dbSync) tip for {_appSettings.PrismNetwork.Name}: {postgresSqlTip.Value.block_no} in epoch {postgresSqlTip.Value.epoch_no}");

        //TODO renable loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_appSettings.DelayBetweenSyncsInMs, stoppingToken);

                if (_cts.Token.IsCancellationRequested)
                {
                    continue;
                }

                _logger.LogInformation($"Sync running for {_appSettings.PrismNetwork.Name}");

                var syncResult = await SyncService.RunSync(mediator, _logger, _appSettings.PrismNetwork.Name, _appSettings.PrismNetwork.StartAtEpochNumber, isInitialStartup);
                if (syncResult.IsFailed)
                {
                    _logger.LogCritical($"Sync failed for {_appSettings.PrismNetwork.Name}: {syncResult.Errors.SingleOrDefault()}");
                }
                else
                {
                    isInitialStartup = false;
                    _logger.LogInformation($"Sync succussfully completed for {_appSettings.PrismNetwork.Name}");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation($"Sync operation was cancelled for {_appSettings.PrismNetwork.Name}");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"An error occurred during the sync process for {_appSettings.PrismNetwork.Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Stop the automatic sync service
    /// </summary>
    public void StopService()
    {
        _logger.LogInformation($"The automatic sync service is stopped");
        _cts.Cancel();
    }

    /// <summary>
    /// Restart the automatic sync service
    /// </summary>
    public async Task RestartServiceAsync()
    {
        _cts = new CancellationTokenSource();
        _logger.LogInformation($"The automatic sync service has been restarted");
    }
}