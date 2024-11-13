using Microsoft.EntityFrameworkCore;
using NetworkLocator.MVVM.Model;

namespace ServerSignalR.Models;

public class ApplicationContextDb : DbContext
{
    public DbSet<WiFiModel> WifiModels => Set<WiFiModel>();
    
    public ApplicationContextDb(DbContextOptions<ApplicationContextDb> options) : base(options) { }
}