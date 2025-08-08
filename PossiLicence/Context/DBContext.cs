using Microsoft.EntityFrameworkCore;
using PossiLicence.Entityies;

namespace PossiLicence.Context;

public class DBContext(DbContextOptions<DBContext> options) : DbContext(options)
{
    public DbSet<CompanyDbEntity> Companies { get; set; }
    public DbSet<CompanyPurchaseDbEntity> CompanyPurchases { get; set; }
    public DbSet<PackageTypeDbEntity> PackageTypes { get; set; }
    public DbSet<AdminDbEntity> Admins { get; set; }
}
