using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Repository
{
	public class ClienteRepository : IRepository<ClienteModel>
	{

		DALClientes dal;

		public Task<int> AddCliente(IEnumerable<ClienteModel> r)
		{
			dal = new DALClientes();
			return dal.AdicionarItens(r);
		}

		public Task Add(IEnumerable<ClienteModel> r, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<ClienteModel> FindById(ClienteModel t, int? u)
		{
			dal = new DALClientes();
			return dal.BuscarItemByID(t, u);
		}

		public Task<IEnumerable<ClienteModel>> GetAll(ClienteModel t, int? u)
		{
			dal = new DALClientes();
			return dal.ObterTodos(t, u);
		}

		public Task Remove(IEnumerable<ClienteModel> r, int c, int? u)
		{
			dal = new DALClientes();
			return dal.ExcluirItens(r, c, u);
		}

		public Task<IEnumerable<ClienteModel>> Search(ClienteModel g, string s, int? u)
		{
			dal = new DALClientes();
			return dal.BuscarItens(g, s, u);
		}

		public Task Update(IEnumerable<ClienteModel> r, int c, int? u)
		{
			dal = new DALClientes();
			return dal.AtualizaItens(r, c, u);
		}

		public Task<ClienteModel> ClienteLogin(ClienteModel t)
		{
			dal = new DALClientes();
			return dal.ClienteLogin(t);
		}
		public Task<IEnumerable<FornecedorModel>> FornecedoresCliente(ClienteModel t, int? u)
		{
			dal = new DALClientes();
			return dal.FornecedoresCliente(t, u);

		}


		public Task<IEnumerable<ClienteModel>> GetAllPaginado(ClienteModel t, int? u)
		{
			throw new NotImplementedException();
		}

        public Task<dynamic> GetInfoEnvio(int c, int u)
        {
            dal = new DALClientes();
            return dal.GetInfoEnvio(c, u);
        }
    }
}
