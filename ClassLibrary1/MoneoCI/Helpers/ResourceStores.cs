using IdentityServer4.Stores;
using System;
using System.Collections.Generic;
using System.Text;
using IdentityServer4.Models;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;
using MoneoCI.Repository;
using IdentityModel;
using Helpers;

namespace MoneoCI.Helpers
{
    public class ResourceStores : IResourceStore
    {
        public List<ApiResource> ApiResources { get; set; }
        public ResourceStores()
        {
            ApiResources = new List<ApiResource>()
            {
            new ApiResource("moneoci","Custom API") { UserClaims = new List<string> {ClaimTypes.Role,JwtClaimTypes.Email,"clienteid","usuarioid",ClaimTypes.GroupSid}}
            };
        }
        public async Task<ApiResource> FindApiResourceAsync(string name)
        {
            var dados = await new ClientIdentityServerRepository()
                .FindApiResourcesByScopeAsync(name);
            return dados.ElementAt(0);
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var retorno = await new ClientIdentityServerRepository().FindApiResourcesByScopeAsync(scopeNames.ElementAt(0));

            if (!ApiResources.Where(a => a.Name == scopeNames.ElementAt(0)).Any())
                ApiResources.Add(retorno.ElementAt(0));

            return ApiResources.Where(a => a.Name == scopeNames.ElementAt(0));
        }

        public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            await Task.Delay(0);

            return new List<IdentityResource> {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResource {Name = "role",UserClaims = new List<string> { ClaimTypes.Role, ClaimTypes.Email }}};
        }

        public async Task<Resources> GetAllResources()
        {
            var dados = await new ClientIdentityServerRepository().GetAllResources();

            foreach (var item in dados)
                if (item.Name != "moneoci")
                    ApiResources.Add(item);



            return new Resources()
            {
                ApiResources = ApiResources,
                IdentityResources = new List<IdentityResource> { new IdentityResource { Name = "role", UserClaims = new List<string> { ClaimTypes.Role, ClaimTypes.Email } } }
            };
        }

		public Task<Resources> GetAllResourcesAsync()
		{
			throw new NotImplementedException();
		}
	}
}
