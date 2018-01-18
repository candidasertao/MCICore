using IdentityServer4.Models;
using IdentityServer4.Stores;
using MoneoCI.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Helpers
{
	public class ClientStores : IClientStore
	{
		public async Task<Client> FindClientByIdAsync(string clientId)
		{
			return await new ClientIdentityServerRepository().FindById(new Client() { ClientId = clientId }, null);
		}
	}
}
