using Dapper;
using Helpers;
using Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
	public class DALMonitoria : IDal<MonitoriaModel>
	{
		public Task AdicionarItensAsync(IEnumerable<MonitoriaModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task AtualizaItensAsync(IEnumerable<MonitoriaModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<MonitoriaModel> BuscarItemByIDAsync(MonitoriaModel t, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<MonitoriaModel>> BuscarItensAsync(MonitoriaModel t, string s, int? u)
		{
			throw new NotImplementedException();
		}

		public Task ExcluirItensAsync(IEnumerable<MonitoriaModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task ExcluirItensUpdateAsync(IEnumerable<MonitoriaModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<MonitoriaModel>> ObterTodosAsync(MonitoriaModel t, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<MonitoriaModel>> ObterTodosPaginadoAsync(MonitoriaModel t, int? u)
		{
			throw new NotImplementedException();
		}
	}
}
