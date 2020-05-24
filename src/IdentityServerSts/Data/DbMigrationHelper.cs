using System.Threading.Tasks;
using IdentityServer4.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IdentityServerSts.Data
{
    public static class DbMigrationHelper
    {
        public static async Task Migrate(IHost host)
        {
            using var serviceScope = host.Services.GetService<IServiceScopeFactory>().CreateScope();
            await serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
            await serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.MigrateAsync();
            await serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>().Database.MigrateAsync();
        }
    }
}
