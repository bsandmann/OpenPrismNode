namespace OpenPrismNode.Core.Common;

#pragma warning disable CS8618
/// <summary>
/// AppSettings-Configuration for the app
/// </summary>
public class AppSettings
{
    // /// <summary>
    // /// Time between the Postgres-DB is checked for new entries
    // /// </summary>
    public int DelayBetweenSyncsInMs { get; set; }
    
    /// <summary>
    /// Configuration of all supported PRISM-networks for which data is in the postgres-db
    /// </summary>
    public PrismNetwork PrismNetwork { get; set; }
}
