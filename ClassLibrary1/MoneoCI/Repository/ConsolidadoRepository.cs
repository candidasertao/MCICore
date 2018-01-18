using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Repository
{
	public class ConsolidadoRepository : IRepository<ConsolidadoModel>
	{
			DALConsolidado dal;


		public Task<(IEnumerable<ConsolidadoModel>, IEnumerable<ConsolidadoModel>)> Consolidado(ConsolidadoModel ct, int c, int? u)
		{
			dal = new DALConsolidado();
			return dal.Consolidados(ct, c, u);
		}
		public Task<IEnumerable<CampanhaModel>> ConsolidadoByStatus(ConsolidadoModel co, byte statusenvio, int c, int? u)
		{
			dal = new DALConsolidado();
			return dal.ConsolidadoByStatus(co, statusenvio, c, u);
		}

		public Task<IEnumerable<CampanhaModel>> DownCancelados(ConsolidadoModel co, int c, int? u, int? carteiraid = null)
		{
			dal = new DALConsolidado();
			return dal.DownCancelados(co, c, u, carteiraid);
		}

		public Task<IEnumerable<ConsolidadoModel>> DownEspecializado(ConsolidadoModel co, int c, int? u)
		{
			dal = new DALConsolidado();
			return dal.DownEspecializado(co, c, u);
		}
		public Task<IEnumerable<ConsolidadoModel>> Especializado(ConsolidadoModel co, int c, int? u)
		{
			dal = new DALConsolidado();
			return dal.Especializado(co, c, u);
		}

		public Task<IEnumerable<ConsolidadoModel>> ComparativoFornecedor(ConsolidadoModel co, int c, int? u)
		{
			dal = new DALConsolidado();
			return dal.ComparativoFornecedor(co, c, u);
		}


		public Task<IEnumerable<ConsolidadoModel>> Carteiras(ConsolidadoModel co, int c, int? u)
		{
			dal = new DALConsolidado();
			return dal.Carteiras(co, c, u);
		}

        public Task<IEnumerable<ConsolidadoModel>> RelatorioArquivosAsync(ConsolidadoModel co, int c, int? u)
		{
			dal = new DALConsolidado();
			return dal.RelatorioArquivosAsync(co, c, u);
		}

        public Task<IEnumerable<ConsolidadoInvalidosModel>> RelatorioInvalidosAsync(ConsolidadoModel co, int c, int? u)
		{
			dal = new DALConsolidado();
			return dal.RelatorioInvalidosAsync(co, c, u);
		}

		public Task Add(IEnumerable<ConsolidadoModel> r, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<ConsolidadoModel> FindById(ConsolidadoModel t, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<ConsolidadoModel>> GetAll(ConsolidadoModel t, int? u)
		{
			throw new NotImplementedException();
		}

		public Task Remove(IEnumerable<ConsolidadoModel> r, int c, int? u)
		{
			throw new NotImplementedException();
		}


		public Task<IEnumerable<ConsolidadoModel>> DownloadConsolidado(ConsolidadoModel g, int c, int? u)
		{
			dal = new DALConsolidado();
			return dal.DownloadConsolidado(g, c, u);
		}

		public Task<IEnumerable<ConsolidadoModel>> Search(ConsolidadoModel g, string s, int? u)
		{
			throw new NotImplementedException();
		}

		public Task Update(IEnumerable<ConsolidadoModel> r, int c, int? u)
		{
			dal = new DALConsolidado();
			return dal.AtualizaItensAsync(r, c, u);
		}

		public Task<IEnumerable<ConsolidadoModel>> GetAllPaginado(ConsolidadoModel t, int? u)
		{
			throw new NotImplementedException();
		}
	}
}
