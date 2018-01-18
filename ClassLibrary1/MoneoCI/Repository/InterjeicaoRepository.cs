using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Repository
{
    public class InterjeicaoRepository: IRepository<InterjeicaoModel>
	{
		DALInterjeicao dal;


		public Task Add(IEnumerable<InterjeicaoModel> r, int c, int? u)
		{
			dal = new DALInterjeicao();
			return dal.AdicionarItensAsync(r, c, u);
		}

		public Task<InterjeicaoModel> FindById(InterjeicaoModel t, int? u)
		{
			dal = new DALInterjeicao();
			return dal.BuscarItemByIDAsync(t, u);
		}

		public Task<IEnumerable<InterjeicaoModel>> GetAll(InterjeicaoModel t, int? u)
		{
			dal = new DALInterjeicao();
			return dal.ObterTodosAsync(t, u);
		}

		public Task<IEnumerable<InterjeicaoModel>> GetAllPaginado(InterjeicaoModel t, int? u)
		{
			throw new NotImplementedException();
		}

		public Task Remove(IEnumerable<InterjeicaoModel> r, int c, int? u)
		{
			dal = new DALInterjeicao();
			return dal.ExcluirItensAsync(r, c, u);
		}

		public Task<IEnumerable<InterjeicaoModel>> Search(InterjeicaoModel g, string s, int? u)
		{
			dal = new DALInterjeicao();
			return dal.BuscarItensAsync(g, s, u);
		}

		public Task Update(IEnumerable<InterjeicaoModel> r, int c, int? u)
		{
			dal = new DALInterjeicao();
			return dal.AtualizaItensAsync(r, c,1);
		}
	}
}
