using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.Interfaces;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace IdentityServerSts.Data
{
    public static class DbSeederHelper
    {
        public static async Task Seed(IHost host)
        {
            var configuration = host.Services.GetRequiredService<IConfiguration>();
            var seedfile = configuration.GetValue<string>("Seed:File");

            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IConfigurationDbContext>();

            if (!File.Exists(seedfile)) return;
            var json = await File.ReadAllTextAsync(seedfile);
            var seeddata = JsonConvert.DeserializeObject<DataSeed>(json);


            if (!context.Clients.Any())
            {
                foreach (var client in seeddata.ClientResources)
                {
                    await context.Clients.AddAsync(client.ToEntity());
                }
                await context.SaveChangesAsync();
            }

            if (!context.IdentityResources.Any())
            {
                foreach (var resource in seeddata.IdResources)
                {
                    await context.IdentityResources.AddAsync(resource.ToEntity());
                }
                await context.SaveChangesAsync();
            }

            if (!context.ApiResources.Any())
            {
                foreach (var resource in seeddata.ApiResources)
                {
                    await context.ApiResources.AddAsync(resource.ToEntity());
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
