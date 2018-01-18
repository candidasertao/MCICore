using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using Models;
using DAL;

namespace MoneoCI.Repository
{
	public class TipoCampanhaRepository : IRepository<TipoCampanhaModel>
	{

		DALTipoCampanha dal;

		public Task Add(IEnumerable<TipoCampanhaModel> r, int c, int? u)
		{
			dal = new DALTipoCampanha();
			return dal.AdicionarItensAsync(r, c, u);
		}

		public Task<TipoCampanhaModel> FindById(TipoCampanhaModel t, int? u)
		{
			dal = new DALTipoCampanha();
			return dal.BuscarItemByIDAsync(t, u);
		}

		public Task<IEnumerable<TipoCampanhaModel>> GetAll(TipoCampanhaModel t, int? u)
		{
			dal = new DALTipoCampanha();
			return dal.ObterTodosAsync(t, u);
		}

		public Task Remove(IEnumerable<TipoCampanhaModel> r, int c, int? u)
		{
			dal = new DALTipoCampanha();
			return dal.ExcluirItensAsync(r, c, u);
		}

		public Task<IEnumerable<TipoCampanhaModel>> Search(TipoCampanhaModel g, string s, int? u)
		{
			dal = new DALTipoCampanha();
			return dal.BuscarItensAsync(g, s, u);
		}

		public Task Update(IEnumerable<TipoCampanhaModel> r, int c, int? u)
		{
			dal = new DALTipoCampanha();
			return dal.AtualizaItensAsync(r, c, u);
		}

		public Task<IEnumerable<TipoCampanhaModel>> ObterTodosParaEnvioSMS(TipoCampanhaModel g)
		{
			dal = new DALTipoCampanha();
			return dal.ObterTodosParaEnvioSMS(g);
		}

		public Task<IEnumerable<TipoCampanhaModel>> GetAllPaginado(TipoCampanhaModel t, int? u)
		{
			dal = new DALTipoCampanha();
			return dal.ObterTodosPaginadoAsync(t, u);
		}
	}
}
