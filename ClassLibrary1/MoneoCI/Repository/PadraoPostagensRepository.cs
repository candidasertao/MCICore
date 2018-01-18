using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Repository
{
	public class PadraoPostagensRepository : IRepository<PadraoPostagensModel>
	{

		DALPadraoPostagens dal;

		public Task Add(IEnumerable<PadraoPostagensModel> r, int c, int? u)
		{
			dal = new DALPadraoPostagens();
			return dal.AdicionarItensAsync(r, c, u);
		}

		public Task<PadraoPostagensModel> Adicionaitem(PadraoPostagensModel r, int c, int? u)
		{
			dal = new DALPadraoPostagens();
			return dal.Adicionaitem(r, c, u);
		}
		public Task<PadraoPostagensModel> FindById(PadraoPostagensModel t, int? u)
		{
			dal = new DALPadraoPostagens();
			return dal.BuscarItemByIDAsync(t, u);
		}

		public Task<IEnumerable<PadraoPostagensModel>> GetAll(PadraoPostagensModel t, int? u)
		{
			dal = new DALPadraoPostagens();
			return dal.ObterTodos(t, u);
		}

		public Task<IEnumerable<PadraoPostagensModel>> GetAllPaginado(PadraoPostagensModel t, int? u)
		{
			dal = new DALPadraoPostagens();
			return dal.ObterTodosPaginadoAsync(t, u);
		}

		public Task Remove(IEnumerable<PadraoPostagensModel> r, int c, int? u)
		{
			dal = new DALPadraoPostagens();
			return dal.ExcluirItensAsync(r, c, u);
		}

		public Task<IEnumerable<PadraoPostagensModel>> Search(PadraoPostagensModel g, string s, int? u)
		{
			dal = new DALPadraoPostagens();
			return dal.BuscarItensAsync(g, s, u);
		}

		public Task<(IEnumerable<PadraoPostagensModel> Padrao, Dictionary<string, string> PadraoOriginal)> PadroesToEnvio(Dictionary<string, string> g, int s, int? u)
		{
			dal = new DALPadraoPostagens();
			return dal.PadroesToEnvio(g, s, u);
		}
		public Task Update(IEnumerable<PadraoPostagensModel> r, int c, int? u)
		{
			dal = new DALPadraoPostagens();
			return dal.AtualizaItensAsync(r, c, u);
		}
	}

}
