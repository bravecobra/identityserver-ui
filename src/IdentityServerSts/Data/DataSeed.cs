using System.Collections.Generic;
using IdentityServer4.Models;

namespace IdentityServerSts.Data
{
    public class DataSeed
    {
        public IEnumerable<IdentityResource> IdResources { get; set; }
        public IEnumerable<ApiResource> ApiResources { get; set; }
        public IEnumerable<Client> ClientResources { get; set; }
    }
}