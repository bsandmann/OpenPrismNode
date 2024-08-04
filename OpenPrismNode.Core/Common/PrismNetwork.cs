namespace OpenPrismNode.Core.Common;

public class PrismNetwork
{
    /// <summary>
    /// Name of the PRISM network for syncing
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Connection-String of the database which holds the PRISM data
    /// </summary>
    public string SqlConnectionString { get; set; }
    
    /// <summary>
    /// Connectionstring to the Postgres-DB, on which the dbSync is putting the PRISM data
    /// </summary>
    public string PostgresConnectionString { get; set; }

}