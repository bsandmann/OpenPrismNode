namespace OpenPrismNode.Web;

using MediatR;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Sync.Commands.GetPostgresBlockTip;
using OpenPrismNode.Sync.Services;

/// <summary>
/// Service than automatically syncs the entire ledger.
/// The service starts automatically when the application starts, but can be stopped and restarted manually
/// </summary>
public class BackgroundSyncService : BackgroundService
{
    private readonly ILogger<BackgroundSyncService> _logger;
    private readonly AppSettings _appSettings;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private CancellationTokenSource _cts;
    private readonly IMediator _mediator;
    private bool _isRunning = false;
    private bool _isLocked = false;

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
        if (_isRunning)
        {
            return;
        }

        if (_isLocked)
        {
            _logger.LogWarning("The automatic sync service is locked due to an ongoing operations. Wait or restart the node.");
        }
        
        _isRunning = true;
        using (IServiceScope scope = _serviceScopeFactory.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var isInitialStartup = true;

            _logger.LogInformation($"Starting background sync for {_appSettings.PrismLedger.Name}");
            var postgresSqlTip = await mediator.Send(new GetPostgresBlockTipRequest(), stoppingToken);
            if (postgresSqlTip.IsFailed)
            {
                _logger.LogCritical($"Cannot get the postgres tip (dbSync) for syncing {_appSettings.PrismLedger.Name}: {postgresSqlTip.Errors.First().Message}");
                return;
            }

            _logger.LogInformation($"Postgres (dbSync) tip for {_appSettings.PrismLedger.Name}: {postgresSqlTip.Value.block_no} in epoch {postgresSqlTip.Value.epoch_no}");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_appSettings.DelayBetweenSyncsInMs, stoppingToken);

                    if (_cts.Token.IsCancellationRequested)
                    {
                        continue;
                    }

                    _logger.LogInformation($"Sync running for {_appSettings.PrismLedger.Name}");

                    var syncResult = await SyncService.RunSync(mediator, _logger, _appSettings.PrismLedger.Name, _cts.Token, _appSettings.PrismLedger.StartAtEpochNumber, isInitialStartup);
                    if (syncResult.IsFailed)
                    {
                        _logger.LogCritical($"Sync failed for {_appSettings.PrismLedger.Name}: {syncResult.Errors.SingleOrDefault()}");
                    }
                    else
                    {
                        isInitialStartup = false;
                        _logger.LogInformation($"Sync succussfully completed for {_appSettings.PrismLedger.Name}");
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation($"Sync operation was cancelled for {_appSettings.PrismLedger.Name}");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogCritical($"An error occurred during the sync process for {_appSettings.PrismLedger.Name}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Stop the automatic sync service
    /// </summary>
    public async Task StopService()
    {
        await _cts.CancelAsync();
        await this.StopAsync(_cts.Token);
        _isRunning = false;
        _isLocked = false;
    }

    /// <summary>
    /// Restart the automatic sync service
    /// </summary>
    public async Task RestartServiceAsync()
    {
        _cts = new CancellationTokenSource();
        await this.StartAsync(_cts.Token);
    }
    
    public void Lock()
    {
        _isLocked = true;
    }
    
    public void Unlock()
    {
        _isLocked = false;
    }
}