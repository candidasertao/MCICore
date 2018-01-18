using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Repository
{
	public class SegmentacaoRepository : IRepository<SegmentacaoModel>
	{
		DALSegmentacao dal;


		public Task Add(IEnumerable<SegmentacaoModel> r, int c, int? u)
		{
			dal = new DALSegmentacao();
			return dal.AdicionarItensAsync(r, c, u);
		}

		public Task<SegmentacaoModel> FindById(SegmentacaoModel t, int? u)
		{
			dal = new DALSegmentacao();
			return dal.BuscarItemByIDAsync(t, u);
		}

		public Task<IEnumerable<SegmentacaoModel>> GetAll(SegmentacaoModel t, int? u)
		{
			dal = new DALSegmentacao();
			return dal.ObterTodosAsync(t, u);
		}


		public Task<IEnumerable<SegmentacaoModel>> GetAllPaginado(SegmentacaoModel t, int? u)
		{
			throw new NotImplementedException();
		}

		public Task Remove(IEnumerable<SegmentacaoModel> r, int c, int? u)
		{
			dal = new DALSegmentacao();
			return dal.ExcluirItensAsync(r, c, u);
		}

		public Task<IEnumerable<SegmentacaoModel>> Search(SegmentacaoModel g, string s, int? u)
		{
			dal = new DALSegmentacao();
			return dal.BuscarItensAsync(g, s, u);
		}

		public Task Update(IEnumerable<SegmentacaoModel> r, int c, int? u)
		{
			dal = new DALSegmentacao();
			return dal.AtualizaItensAsync(r, c, u);
		}
	}
}
