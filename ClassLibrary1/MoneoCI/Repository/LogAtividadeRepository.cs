using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Repository
{
	public class LogAtividadeRepository : IRepository<LogAtividadeModel>
	{
		DALLogAtividade dal;

		public Task Add(IEnumerable<LogAtividadeModel> r, int c, int? u)
		{
			dal = new DALLogAtividade();
			return dal.AdicionarItensAsync(r, c, u);
		}

		public Task<LogAtividadeModel> FindById(LogAtividadeModel t, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<LogAtividadeModel>> GetAll(LogAtividadeModel t, int? u)
		{
			throw new NotImplementedException();
		}

		public Task Remove(IEnumerable<LogAtividadeModel> r, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<LogAtividadeModel>> Search(LogAtividadeModel g, string s, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<LogAtividadeModel>> DashBoard(int c, int? u)
		{
			dal = new DALLogAtividade();
			return dal.DashBoard(c, u);
		}

		public Task Update(IEnumerable<LogAtividadeModel> r, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<LogAtividadeModel>> GetAllPaginado(LogAtividadeModel t, int? u)
		{
			throw new NotImplementedException();
		}
	}
}
