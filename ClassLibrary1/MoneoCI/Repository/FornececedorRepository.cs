using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Repository
{
	public class FornecedorRepository : IRepository<FornecedorModel>
	{
		DALFornecedor dal;



		public Task<int> AddItem(FornecedorModel r, int c, int? u)
		{
			dal = new DALFornecedor();
			return dal.AdicionarItemAsync(r, c, u);
		}

		public Task Add(IEnumerable<FornecedorModel> r, int c, int? u)
		{
			dal = new DALFornecedor();
			return dal.AdicionarItensAsync(r, c, u);
		}

		public Task<FornecedorModel> FindById(FornecedorModel t, int? u)
		{
			dal = new DALFornecedor();
			return dal.BuscarItemByIDAsync(t, u);
		}

		public Task<FornecedorModel> FornecedorByLogin(FornecedorModel t)
		{
			dal = new DALFornecedor();
			return dal.FornecedorByLogin(t);
		}

		public Task<IEnumerable<FornecedorModel>> GetAll(FornecedorModel t, int? u)
		{
			dal = new DALFornecedor();
			return dal.ObterTodosAsync(t, u);
		}

		public Task Remove(IEnumerable<FornecedorModel> r, int c, int? u)
		{
			dal = new DALFornecedor();
			return dal.ExcluirItensAsync(r, c, u);
		}

		public Task<IEnumerable<FornecedorModel>> Search(FornecedorModel g, string s, int? u)
		{
			dal = new DALFornecedor();
			return dal.BuscarItensAsync(g, s, u);
		}

		public Task Update(IEnumerable<FornecedorModel> r, int c, int? u)
		{
			dal = new DALFornecedor();
			return dal.AtualizaItensAsync(r, c, u);
		}

		public Task AtualizaStatusFornecedorCliente(IEnumerable<FornecedorModel> r, int c)
		{
			dal = new DALFornecedor();
			return dal.AtualizaStatusFornecedorCliente(r, c);
		}
		public Task<IEnumerable<FornecedorModel>> Redistribuicao(int c)
		{
			dal = new DALFornecedor();
			return dal.Redistribuicao(c);
		}

		public Task<int> RedistribuiLotes(IEnumerable<FornecedorMinModel> f, int arquivoid, int carteiraid, int c, int? u)
		{
			dal = new DALFornecedor();
			return dal.RedistribuiLotes(f, arquivoid, carteiraid, c, u);
		}


		public Task<int> AtualizaDistribuicao(IEnumerable<FornecedorModel> r, int c)
		{
			dal = new DALFornecedor();
			return dal.AtualizaDistribuicao(r, c);
		}
		public Task<IEnumerable<FornecedorModel>> DashBoard(int c, int? u)
		{
			dal = new DALFornecedor();
			return dal.DashBoard(c, u);
		}

		public Task<(IEnumerable<FornecedorCampanhaModel>, IEnumerable<CampanhaModel>)> Monitoria(int c)
		{
			dal = new DALFornecedor();
			return dal.Monitoria(c);
		}

		public Task<IEnumerable<FornecedorModel>> FornecedorTelaEnvio(int c)
		{
			dal = new DALFornecedor();
			return dal.FornecedoresTelaEnvio(c);
		}

		public Task<IEnumerable<FornecedorModel>> FornececedoresCadastro(int c)
		{
			dal = new DALFornecedor();
			return dal.FornececedoresCadastro(c);
		}
		public Task<int> AtualizaAPIKey(FornecedorModel c)
		{
			dal = new DALFornecedor();
			return dal.AtualizaAPIKey(c);
		}

		public Task<int> FornecedoresCliente(int c)
		{
			dal = new DALFornecedor();
			return dal.FornecedoresCliente(c);
		}

		public Task<(IEnumerable<ConsolidadoModel> item1, IEnumerable<ConsolidadoModel> item2, IEnumerable<ConsolidadoModel> item3, FornecedorModel f)> Relatorio(ConsolidadoModel cm, int c, int? u)
		{
			dal = new DALFornecedor();
			return dal.Relatorio(cm, c, u);
		}

		public Task<bool> IsApiKeyFornecedor(FornecedorModel c)
		{
			dal = new DALFornecedor();
			return dal.IsApiKeyFornecedor(c);
		}

		public Task<IEnumerable<dynamic>> ListaFornecedoresCadastro()
		{
			dal = new DALFornecedor();
			return dal.ListaFornecedoresCadastro();
		}

		public Task<IEnumerable<FornecedorModel>> GetAllPaginado(FornecedorModel t, int? u)
		{
			throw new NotImplementedException();
		}

        public Task<IEnumerable<FornecedorClienteModel>> GetAllPaginadoFornecedorClienteAsync(FornecedorClienteModel t, int f)
        {
            dal = new DALFornecedor();
            return dal.ObterTodosPaginadoFornecedorClienteAsync(t, f);
        }

        public Task AdicionarFornecedorCapacidadeExtraAsync(IEnumerable<FornecedorCapacidadeExtraModel> t, int f)
        {
            dal = new DALFornecedor();
            return dal.AdicionarFornecedorCapacidadeExtraAsync(t, f);
        }

        public Task AtualizarFornecedorClienteAsync(FornecedorClienteModel t, int f)
        {
            dal = new DALFornecedor();
            return dal.AtualizarFornecedorClienteAsync(t, f);
        }

        public Task RemoverFornecedorCapacidadeExtraAsync(IEnumerable<FornecedorCapacidadeExtraModel> t, int f)
        {
            dal = new DALFornecedor();
            return dal.RemoverFornecedorCapacidadeExtraAsync(t, f);
        }

        public Task<FornecedorMonitoria> MonitoriaFornecedor(int f)
        {
            dal = new DALFornecedor();
            return dal.MonitoriaFornecedor(f);
        }

        public Task<dynamic> Previsto(int f)
        {
            dal = new DALFornecedor();
            return dal.Previsto(f);
        }

        public Task AdicionarFornecedorServico(int f, IEnumerable<FornecedorServicoModel> t)
        {
            dal = new DALFornecedor();
            return dal.AdicionarFornecedorServico(f, t);
        }

        public Task<bool> isPodeAgendarInterrupcaoServico(int f, int? id)
        {
            dal = new DALFornecedor();
            return dal.isPodeAgendarInterrupcaoServico(f, id);
        }
        
        public Task AtualizarFornecedorServico(int f, IEnumerable<FornecedorServicoModel> t)
        {
            dal = new DALFornecedor();
            return dal.AtualizarFornecedorServico(f, t);
        }

        public Task FinalizarFornecedorServico(int f, IEnumerable<FornecedorServicoModel> t)
        {
            dal = new DALFornecedor();
            return dal.FinalizarFornecedorServico(f, t);
        }

        public Task ExcluirFornecedorServico(int f, IEnumerable<FornecedorServicoModel> t)
        {
            dal = new DALFornecedor();
            return dal.ExcluirFornecedorServico(f, t);
        }        
    }
}
