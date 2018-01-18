using DAL;
using DTO;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Repository
{
	public class CarteiraRepository : IRepository<CarteiraModel>
	{
		DALCarteira dal;

		public Task Add(IEnumerable<CarteiraModel> r, int c, int? u)
		{
			dal = new DALCarteira();
			return dal.AdicionarItensAsync(r, c, u);
		}

		public Task<CarteiraModel> FindById(CarteiraModel t, int? u)
		{
			dal = new DALCarteira();
			return dal.BuscarItemByIDAsync(t, u);
		}

		public Task<IEnumerable<CarteiraModel>> GetAll(CarteiraModel t, int? u)
		{
			dal = new DALCarteira();
			return dal.ObterTodosAsync(t, u);
		}

		public Task<IEnumerable<CarteiraModel>> GetAllPaginado(CarteiraModel t, int? u)
		{
			dal = new DALCarteira();
			return dal.ObterTodosPaginadoAsync(t, u);
		}

		public Task Remove(IEnumerable<CarteiraModel> r, int c, int? u)
		{
			dal = new DALCarteira();
			return dal.ExcluirItensAsync(r, c, u);
		}
		public Task LimiteCarteira(int? careiraid = null)
		{
			dal = new DALCarteira();
			return dal.LimiteCarteira(careiraid);
		}

		public Task<IEnumerable<CarteiraModel>> Search(CarteiraModel g, string s, int? u)
		{
			dal = new DALCarteira();
			return dal.BuscarItensAsync(g, s, u);
		}

		public Task<(IEnumerable<CampanhaModel>, IEnumerable<CampanhaModel>)> CarteirasToApi(IEnumerable<CampanhaModel> v, IEnumerable<CampanhaModel> i, int s)
		{
			dal = new DALCarteira();
			return dal.CarteirasToApi(v, i, s);
		}

		public Task Update(IEnumerable<CarteiraModel> r, int c, int? u)
		{
			dal = new DALCarteira();
			return dal.AtualizaItensAsync(r, c, u);
		}
	}
}
