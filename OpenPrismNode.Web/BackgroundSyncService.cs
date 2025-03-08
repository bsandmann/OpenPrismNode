namespace OpenPrismNode.Web;

using Common;
using Core.Commands.CreateBlock;
using Core.Commands.CreateEpoch;
using Core.Commands.CreateLedger;
using Core.Commands.GetMostRecentBlock;
using Core.Models;
using FluentResults;
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
    private Task _executingTask;
    public bool isRunning { get; private set; } = false;
    public bool isLocked { get; private set; } = false;
    private readonly SemaphoreSlim _restartSemaphore = new SemaphoreSlim(1, 1);

    /// <inheritdoc />
    public BackgroundSyncService(IOptions<AppSettings> appSettings, ILogger<BackgroundSyncService> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _appSettings = appSettings.Value;
        _serviceScopeFactory = serviceScopeFactory;
        _cts = new CancellationTokenSource();
    }

    /// <summary>
    /// Override of the StartAsync method that captures the executing task
    /// </summary>
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Background Sync Service starting");
        
        // Always create a new token source when starting
        _cts = new CancellationTokenSource();
        
        // Store the task we're executing
        _executingTask = ExecuteAsync(cancellationToken);
        
        // If the task is completed then return it, otherwise it's running
        return _executingTask.IsCompleted ? _executingTask : Task.CompletedTask;
    }
    
    /// <summary>
    /// Override of the StopAsync method to properly handle graceful shutdown
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Background Sync Service stopping");
        
        // Signal cancellation to the executing method
        if (_cts != null)
        {
            _cts.Cancel();
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }
        
        _logger.LogInformation("Background Sync Service stopped");
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var version = OpnVersion.GetVersion();
        Console.WriteLine(
            $"""
               ____   _____  _   _ 
              / __ \ |  __ \| \ | |
             | |  | || |__) |  \| |
             | |  | ||  ___/| . ` |
             | |__| || |    | |\  |
              \____/ |_|    |_| \_|
             Open PRISM Node (v{version})
             """);

        // Acquire semaphore to ensure only one execution at a time
        await _restartSemaphore.WaitAsync(stoppingToken);
        
        try
        {
            if (_appSettings.PrismLedger.Name.Equals("inMemory", StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogInformation("In-memory ledger detected. Skipping automatic sync service");
                using (IServiceScope scope = _serviceScopeFactory.CreateScope())
                {
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    var mostRecentInMemoryBlock = await mediator.Send(new GetMostRecentBlockRequest(LedgerType.InMemory), CancellationToken.None);
                    if (mostRecentInMemoryBlock.IsFailed)
                    {
                        _logger.LogInformation("In-memory ledger detected. Generating genesis block.");
                        // If we don't have any blocks in the in-memory ledger, we need to create the genesis block, and maybe even the ledger and  epoch
                        var ledgerResult = await mediator.Send(new CreateLedgerRequest(LedgerType.InMemory), CancellationToken.None);
                        if (ledgerResult.IsFailed)
                        {
                            _logger.LogCritical("Failed to create the in-memory ledger in the database");
                        }

                        var createStartingEpochResult = await mediator.Send(new CreateEpochRequest(LedgerType.InMemory, 1), CancellationToken.None);
                        if (createStartingEpochResult.IsFailed)
                        {
                            _logger.LogCritical("Failed to create the starting epoch for the in-memory ledger");
                            return;
                        }

                        var genesisBlock = await mediator.Send(new CreateBlockRequest(LedgerType.InMemory, Hash.CreateRandom(), null, 1, null, 1, DateTime.UtcNow, 0, false), CancellationToken.None);
                        if (genesisBlock.IsFailed)
                        {
                            _logger.LogCritical("Failed to create the genesis block for the in-memory ledger");
                            return;
                        }
                    }
                }

                return;
            }

            if (isLocked)
            {
                _logger.LogWarning("The automatic sync service is locked due to an ongoing operations. Wait or restart the node");
                return;
            }

            // Set running flag at the start of actual execution
            isRunning = true;
            _logger.LogInformation("Sync service is now running");
            
            try
            {
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

                    // Create a linked token source that will be cancelled if either the service is stopping
                    // or our manual cancellation is requested
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _cts.Token);

                    while (!linkedCts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            await Task.Delay(_appSettings.DelayBetweenSyncsInMs, linkedCts.Token);

                            _logger.LogInformation($"Sync running for {_appSettings.PrismLedger.Name}");

                            var syncResult = await SyncService.RunSync(mediator, _appSettings, _logger, _appSettings.PrismLedger.Name, linkedCts.Token, _appSettings.PrismLedger.StartAtEpochNumber, isInitialStartup);
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
            finally
            {
                // Ensure running flag is reset when execution ends
                isRunning = false;
                _logger.LogInformation("Sync service is no longer running");
            }
        }
        finally
        {
            // Always release the semaphore when we're done
            _restartSemaphore.Release();
        }
    }

    /// <summary>
    /// Stop the automatic sync service
    /// </summary>
    public async Task StopService()
    {
        _logger.LogInformation("Manual sync service stop requested");
        
        if (!isRunning)
        {
            _logger.LogInformation("Sync service is already stopped");
            return;
        }
        
        // Use the standard StopAsync with a short timeout
        await StopAsync(new CancellationToken());
        
        // Set flags immediately so UI shows correct state
        isLocked = false;
        
        _logger.LogInformation("Sync service has been manually stopped");
    }

    /// <summary>
    /// Restart the automatic sync service
    /// </summary>
    public async Task RestartServiceAsync()
    {
        _logger.LogInformation("Manual sync service restart requested");
        
        // Make sure the service is stopped first
        if (isRunning)
        {
            await StopService();
            
            // Give it a moment to clean up
            await Task.Delay(500);
        }
        
        // Start the service properly
        _logger.LogInformation("Starting sync service after manual restart request");
        await StartAsync(CancellationToken.None);
        
        _logger.LogInformation("Sync service has been manually restarted");
    }

    public void Lock()
    {
        isLocked = true;
    }

    public void Unlock()
    {
        isLocked = false;
    }
}