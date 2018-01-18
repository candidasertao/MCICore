using Gestores;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Repository
{
	public class GestorRepository : IRepository<GestorModel>
	{
		DALGestores dal;

		public Task<IEnumerable<GestorModel>> Search(GestorModel g, string s, int? usuarioid)
		{
			dal = new DALGestores();
			return dal.BuscarItensAsync(g, s, usuarioid);
		}

		public Task Add(IEnumerable<GestorModel> r, int c, int? usuarioid)
		{
			dal = new DALGestores();
			return dal.AdicionarItensAsync(r, c, usuarioid);
		}

		public Task<GestorModel> FindById(GestorModel g, int? usuarioid)
		{
			dal = new DALGestores();
			return dal.BuscarItemByIDAsync(g, usuarioid);
		}

		public Task<IEnumerable<GestorModel>> GetAll(GestorModel g, int? usuarioid)
		{
			dal = new DALGestores();
			return dal.ObterTodosAsync(g, usuarioid);
		}

		public Task Remove(IEnumerable<GestorModel> r, int c, int? usuarioid)
		{
			dal = new DALGestores();
			return dal.ExcluirItensAsync(r, c, usuarioid);
		}

		public Task Update(IEnumerable<GestorModel> r, int c, int? usuarioid)
		{
			dal = new DALGestores();
			return dal.AtualizaItensAsync(r, c, usuarioid);
		}

		public Task<IEnumerable<GestorModel>> GestoresEmailEnvio(int c, int? carteiraid = null)
		{
			dal = new DALGestores();
			return dal.GestoresEmailEnvio(c, carteiraid);
		}
		public Task<(IEnumerable<GestorModel>, Dictionary<string, string>)> GestoresPadraoEnvio(int c, int? u, Dictionary<string, string> padrao)
		{
			dal = new DALGestores();
			return dal.GestoresPadraoEnvio(c, u, padrao);

		}
		public Task<IEnumerable<GestorModel>> GestorByCarteira(int carteiraid, int clienteid)
		{
			dal = new DALGestores();
			return dal.GestorByCarteira(carteiraid, clienteid);
		}
		public Task<IEnumerable<GestorModel>> GestorByCarteirasEnvio(IEnumerable<int> carteiras, int clienteid)
		{
			dal = new DALGestores();
			return dal.GestorByCarteiras(carteiras, clienteid);
		}
		
		public Task<IEnumerable<GestorModel>> GetAllPaginado(GestorModel t, int? u)
		{
			dal = new DALGestores();
			return dal.ObterTodosPaginadoAsync(t, u);
		}
	}
}
