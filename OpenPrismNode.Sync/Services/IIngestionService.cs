namespace OpenPrismNode.Sync.Services;

using Core.Models;

public interface IIngestionService 
{
    Task Ingest(string didIdentifier, LedgerType requestLedger); 
}