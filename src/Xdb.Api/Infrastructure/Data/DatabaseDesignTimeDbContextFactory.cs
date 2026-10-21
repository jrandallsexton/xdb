using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Xdb.Api.Infrastructure.Data;

/// <summary>Lets `dotnet ef migrations add` build the model without a connection string or the app host.</summary>
public class DatabaseDesignTimeDbContextFactory : IDesignTimeDbContextFactory<XdbDataContext>
{
    public XdbDataContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<XdbDataContext>()
            .UseSqlServer(sql => sql.UseCompatibilityLevel(DatabaseRegistration.CompatibilityLevel))
            .Options;

        return new XdbDataContext(options);
    }
}
