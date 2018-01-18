using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Repository
{
	public class SessionDataRepository : IRepository<SessionDataModel>
	{

		DALSessionData dal;


		public Task Add(IEnumerable<SessionDataModel> r, int c, int? u)
		{
			dal = new DALSessionData();
			return dal.AdicionarItensAsync(r, c, u);
		}

		public Task<SessionDataModel> FindById(SessionDataModel t, int? u)
		{
			dal = new DALSessionData();
			return dal.BuscarItemByIDAsync(t, u);
		}

		public Task<IEnumerable<SessionDataModel>> GetAll(SessionDataModel t, int? u)
		{
			dal = new DALSessionData();

			return dal.ObterTodosAsync(t, u);
		}
		public Task<IEnumerable<SessionDataModel>> ObterTodosAsyncByNumericKey(SessionDataModel t)
		{
			dal = new DALSessionData();
			return dal.ObterTodosAsyncByNumericKey(t);
		}
		public Task<IEnumerable<SessionDataModel>> GetAllPaginado(SessionDataModel t, int? u)
		{
			throw new NotImplementedException();
		}
		public Task RemoveByKey(IEnumerable<SessionDataModel> r, int c, int? u)
		{
			dal = new DALSessionData();

			return dal.ExcluirItemByKeysAsync(r, c, u);
		}

		public Task Remove(IEnumerable<SessionDataModel> r, int c, int? u)
		{
			dal = new DALSessionData();
			return dal.ExcluirItensAsync(r, c, u);
		}

		public Task<IEnumerable<SessionDataModel>> Search(SessionDataModel g, string s, int? u)
		{
			dal = new DALSessionData();
			return dal.BuscarItensAsync(g, s, u);
		}

		public Task Update(IEnumerable<SessionDataModel> r, int c, int? u)
		{
			dal = new DALSessionData();
			return dal.AtualizaItensAsync(r, c, u);
		}
	}
}
