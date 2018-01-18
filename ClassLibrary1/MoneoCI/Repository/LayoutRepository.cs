using ConecttaManagerData.DAL;
using Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoneoCI.Repository
{
	public class LeiauteRepository : IRepository<LeiauteModel>
	{
		DALLeiaute dal;


		public Task Add(IEnumerable<LeiauteModel> r, int c, int? u)
		{
			dal = new DALLeiaute();
			return dal.AdicionarItensAsync(r, c, u);
		}

		public Task<LeiauteModel> FindById(LeiauteModel t, int? u)
		{
			dal = new DALLeiaute();
			return dal.BuscarItemByIDAsync(t, u);
		}
	

		public Task<IEnumerable<LeiauteModel>> GetAll(LeiauteModel t, int? u)
		{
			dal = new DALLeiaute();
			return dal.ObterTodosAsync(t, u);
		}

		public Task<IEnumerable<LeiauteModel>> GetAllPaginado(LeiauteModel t, int? u)
		{
			dal = new DALLeiaute();
			return dal.ObterTodosPaginadoAsync(t, u);
		}

		public Task Remove(IEnumerable<LeiauteModel> r, int c, int? u)
		{
			dal = new DALLeiaute();
			return dal.ExcluirItensAsync(r, c, u);
		}

		public Task<IEnumerable<LeiauteModel>> Search(LeiauteModel g, string s, int? u)
		{
			dal = new DALLeiaute();
			return dal.BuscarItensAsync(g, s, u);
		}

		public Task Update(IEnumerable<LeiauteModel> r, int c, int? u)
		{
			dal = new DALLeiaute();
			return dal.AtualizaItensAsync(r, c, u);
		}

		public Task DefiniPadraoAsync(LeiauteModel l, int c, int? u)
		{
			dal = new DALLeiaute();
			return dal.DefiniPadraoAsync(l, c, u);
		}
		public Task<IEnumerable<LeiauteModel>> ListaLayouts(int c,  int? u)
		{
			dal = new DALLeiaute();
			return dal.ListaLayouts(c, u);
		}

	}
}
