using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TechTicker.DataAccess;

public class TechTickerDbContextFactory : IDesignTimeDbContextFactory<TechTickerDbContext>
{
    public TechTickerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TechTickerDbContext>();
        
        // Set your local or development connection string
        optionsBuilder.UseNpgsql("Server=localhost;Database=TechTickerDb;Trusted_Connection=True;");

        return new TechTickerDbContext(optionsBuilder.Options);
    }
}
