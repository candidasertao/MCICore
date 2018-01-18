using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Repository
{
    public class BlacklistRepository: IRepository<BlackListModel>
	{
		DALBlacklist dal;


		public Task Add(IEnumerable<BlackListModel> r, int c, int? u)
		{
			dal = new DALBlacklist();
			return dal.AdicionarItensAsync(r, c, u);
		}

		public Task<BlackListModel> FindById(BlackListModel t, int? u)
		{
			dal = new DALBlacklist();
			return dal.BuscarItemByIDAsync(t, u);
		}

		public Task<IEnumerable<BlackListModel>> GetAll(BlackListModel t, int? u)
		{
			dal = new DALBlacklist();
			return dal.ObterTodosAsync(t, u);
		}

		public Task<IEnumerable<BlackListModel>> GetAllPaginado(BlackListModel t, int? u)
		{
			dal = new DALBlacklist();
			return dal.ObterTodosPaginadoAsync(t, u);
		}
		
		public Task Remove(IEnumerable<BlackListModel> r, int c, int? u)
		{
			dal = new DALBlacklist();
			return dal.ExcluirItensAsync(r, c, u);
		}

		public Task<IEnumerable<BlackListModel>> Search(BlackListModel g, string s, int? u)
		{
			dal = new DALBlacklist();
			return dal.BuscarItensAsync(g, s, u);
		}

		public Task Update(IEnumerable<BlackListModel> r, int c, int? u)
		{
			dal = new DALBlacklist();
			return dal.AtualizaItensAsync(r, c, u);
		}
	}
}
