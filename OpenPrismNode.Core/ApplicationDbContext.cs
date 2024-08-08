namespace OpenPrismNode.Core;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public class ApplicationDbContext : DbContext 
{
    protected readonly IConfiguration Configuration;

    // Constructor that accepts DbContextOptions
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        :base(options)
    {
    }

    // DbSet property
    public DbSet<NetworkEntity> PrismNetworkEntities { get; set; }
  
}