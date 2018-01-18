using ConecttaManagerData.DAL;
using IdentityServer4.Models;
using MoneoCI.Helpers;
using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Repository
{
    public class ClientIdentityServerRepository : IRepository<Client>
    {
        DALIdentityServer dal;

        public Task Add(IEnumerable<Client> r, int c, int? u)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(string scopeNames)
        {
            dal = new DALIdentityServer();
            return dal.FindApiResourcesByScopeAsync(scopeNames);
        }

        public Task<(string clienteid, string senha, int id)> AdicionaApiResource(Client c, ApiResource api)
        {
            dal = new DALIdentityServer();
            return dal.AdicionaApiResource(c, api);
        }

        public Task AtualizaApiResoureTokenAsync(string token, int id)
        {
            dal = new DALIdentityServer();
            return dal.AtualizaApiResoureTokenAsync(token, id);
        }
        public Task AtualizaTokenReseted(string token)
        {
            dal = new DALIdentityServer();
            return dal.RemoveToken(token);
        }

        public Task<ApiResource> GetApiResourceByToken(string token)
        {
            dal = new DALIdentityServer();
            return dal.GetApiResourceByToken(token);
        }
        public Task<IEnumerable<ApiResource>> GetAllResources()
        {
            dal = new DALIdentityServer();
            return dal.GetAllResources();
        }
        public Task<Client> FindById(Client t, int? u)
        {
            dal = new DALIdentityServer();
            return dal.BuscarItemByIDAsync(t, u);
        }

        public Task<IEnumerable<Client>> GetAll(Client t, int? u)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Client>> GetAllPaginado(Client t, int? u)
        {
            throw new NotImplementedException();
        }

        public Task Remove(IEnumerable<Client> r, int c, int? u)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Client>> Search(Client g, string s, int? u)
        {
            throw new NotImplementedException();
        }

        public Task Update(IEnumerable<Client> r, int c, int? u)
        {
            throw new NotImplementedException();
        }
    }
}
