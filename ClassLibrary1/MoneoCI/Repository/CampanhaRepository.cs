using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Models;
using DAL;
using static Models.MonitoriaModel;

namespace MoneoCI.Repository
{
	public class CampanhaRepository : IRepository<CampanhaModel>
	{
		DALCampanha dal;



		public Task Add(IEnumerable<CampanhaModel> r, int c, int? u)
		{
			dal = new DALCampanha();
			return dal.AdicionarItensAsync(r, c, u);
		}

		public Task AddCampanha(IEnumerable<CampanhaModel> r, IEnumerable<CampanhaModel> invalidos, int c, int? u)
		{
			dal = new DALCampanha();
			return dal.AdicionarItensAsync(r, c, u);
		}

		public Task<CampanhaModel> FindById(CampanhaModel t, int? u)
		{
			dal = new DALCampanha();
			return dal.BuscarItemByIDAsync(t, u);
		}

		public Task<IEnumerable<CampanhaModel>> GetAll(CampanhaModel t, int? u)
		{
			dal = new DALCampanha();
			return dal.ObterTodosAsync(t, u);
		}
		public Task<CampanhaModel> DadosIOPeople(CampanhaModel t, int f)
		{
			dal = new DALCampanha();
			return dal.DadosIOPeople(t, f);
		}

		public Task Remove(IEnumerable<CampanhaModel> r, int c, int? u)
		{
			dal = new DALCampanha();
			return dal.ExcluirItensAsync(r, c, u);
		}

		public Task<IEnumerable<CampanhaModel>> DownloadCelularesInvalidos(ConsolidadoModel g, int c, int? u, byte tipoinvalido)
		{
			dal = new DALCampanha();
			return dal.DownloadCelularesInvalidos(g, c, u, tipoinvalido);
		}
		public Task<IEnumerable<CampanhaModel>> Search(CampanhaModel g, string s, int? u)
		{
			dal = new DALCampanha();
			return dal.BuscarItensAsync(g, s, u);
		}

		public Task Update(IEnumerable<CampanhaModel> r, int c, int? u)
		{
			dal = new DALCampanha();
			return dal.AtualizaItensAsync(r, c, u);
		}
		/// <summary>
		/// Atualiza uma campanha como enviada com erro ou sucesso
		/// </summary>
		/// <param name="r">Lista de campanhas</param>
		/// <returns></returns>
		public Task UpdateCampanhaEnviada(IEnumerable<CampanhaModel> r)
		{
			dal = new DALCampanha();
			return dal.AtualizaItensCampanhaEnviada(r);
		}

		/// <summary>
		/// Atualiza o status report e data report da campanha enviada
		/// </summary>
		/// <param name="r"></param>
		/// <returns></returns>
		public Task UpdateStatusReport(IEnumerable<CampanhaModel> r)
		{
			dal = new DALCampanha();
			return dal.AtualizaItensStatusReport(r);
		}

		public Task AdicionaCampanhaAsync(List<CampanhaModel> validos, List<CampanhaModel> invalidos, int c, int? u)
		{
			dal = new DALCampanha();
			return dal.AdicionaCampanhaAsync(validos, invalidos, c, u);
		}

		public Task<MonitoriaModel> MonitoriaHoje(int c, int? u)
		{
			dal = new DALCampanha();
			return dal.MonitoriaHoje(c, u);

		}

		public Task<MonitoriaModel> MonitoriaActions(MonitoriaModel m, byte statusenvio, int c, int? u, int? carteiraid = null)
		{
			dal = new DALCampanha();
			return dal.MonitoriaActions(m, statusenvio, c, u, carteiraid);

		}
		public Task<IEnumerable<StatusQuantidade>> QuantidadeByStatus(int c, int? u)
		{
			dal = new DALCampanha();
			return dal.QuantidadeByStatus(c, u);
		}

		public Task<int> ActionsLoteCampanha(byte statusenvio, byte statusenvioold, int c, int? u, ActionCamp action)
		{
			dal = new DALCampanha();
			return dal.ActionsLoteCampanha(statusenvio, statusenvioold, c, u, action);

		}

		public Task<(IEnumerable<CampanhaModel>, IEnumerable<CampanhaModel>)> DashBoard(int c, int? u)
		{
			dal = new DALCampanha();
			return dal.DashBoard(c, u);

		}

		public Task<int> ActionCampanhas(IEnumerable<CampanhaGridLotesModel> campanhas, int arquivoid, int carteiraid, byte statusenvio, int c, int? u, ActionCamp action)
		{
			dal = new DALCampanha();
			return dal.ActionsCampanha(campanhas, arquivoid, carteiraid, statusenvio, c, u, action);
		}

		public Task CadastraRequsicao(CampanhaRequisicaoRelatorioModel ca, int c, int? u)
		{
			dal = new DALCampanha();
			return dal.CadastraRequsicao(ca, c, u);
		}

		public Task<IEnumerable<CampanhaRequisicaoRelatorioModel>> ListarRequisicaoRelatorio(int c, int? u)
		{
			dal = new DALCampanha();
			return dal.ListarRequisicaoRelatorio(c, u);
		}

		public Task<IEnumerable<CampanhaGridLotesModel>> RetornaLotes(MonitoriaModel m, int carteiraid, int arquivoid, int c, int? u)
		{
			dal = new DALCampanha();
			return dal.RetornaLotes(m, carteiraid, arquivoid, c, u);
		}

		public Task<IEnumerable<string>> ArquivoExistente(IEnumerable<string> a, int c)
		{
			dal = new DALCampanha();
			return dal.ArquivoExistente(a, c);

		}

		public Task<IEnumerable<CampanhaModel>> DownByStatusOnly(ConsolidadoModel co, byte statusenvio, int c, int? u)
		{
			dal = new DALCampanha();
			return dal.DownByStatusOnly(co, statusenvio, c, u);
		}
		public Task<IEnumerable<CampanhaModel>> DownByStatus(ConsolidadoModel co, byte statusenvio, int c, int? u)
		{
			dal = new DALCampanha();
			return dal.DownByStatus(co, statusenvio, c, u);
		}
		public Task<IEnumerable<CampanhaModel>> HigienizaCarteira(IEnumerable<CampanhaModel> c, int carteiraid, int diashigieniza, int cliente, int? u)
		{
			dal = new DALCampanha();
			return dal.HigienizaCarteira(c, carteiraid, diashigieniza, cliente, u);

		}

		public Task<IEnumerable<CampanhaModel>> Filtragem(IEnumerable<CampanhaModel> campanhas)
		{
			dal = new DALCampanha();
			return dal.Filtragem(campanhas);

		}

		public Task<IEnumerable<CampanhaModel>> GetAllPaginado(CampanhaModel t, int? u)
		{
			throw new NotImplementedException();
		}


		public Task ExcludeFileCards(string guid, int? codigo = null)
		{
			dal = new DALCampanha();
			return dal.ExcludeFileCards(guid, codigo);
		}

		public Task<int> InsertFileCards(string arquivo, string guid)
		{
			dal = new DALCampanha();
			return dal.InsertFileCards(arquivo, guid);
		}

		public Task EnviarSMSApi(IEnumerable<CampanhaModel> validos, IEnumerable<CampanhaModel> invalidos, int clienteID, int? usuarioID)
		{
			dal = new DALCampanha();
			return dal.EnviarSMSApi(validos, invalidos, clienteID, usuarioID);
		}
		public Task<int> AtualizaItensCampanhaEnviada(IEnumerable<CampanhaModel> t)
		{
			dal = new DALCampanha();
			return dal.AtualizaItensCampanhaEnviada(t);
		}

		public Task<int> AtualizaItensStatusReport(IEnumerable<CampanhaModel> t)
		{
			dal = new DALCampanha();
			return dal.AtualizaItensStatusReport(t);
		}

		public Task<IEnumerable<CampanhaModel>> DetalhadoCampanhas(CampanhaModel t, int clienteid, int? usuarioid)
		{
			dal = new DALCampanha();
			return dal.DetalhadoCampanhas(t, clienteid, usuarioid);
		}

		public Task<IEnumerable<CampanhaModel>> PesquisaByCelularAsync(CampanhaModel t, int? u)
		{
			dal = new DALCampanha();
			return dal.PesquisaByCelularAsync(t, u);
		}

		public Task<IEnumerable<CampanhaModel>> DetalhadoGenerico(CampanhaModel t)
		{
			dal = new DALCampanha();
			return dal.DetalhadoGenerico(t);
		}
		public Task<IEnumerable<decimal>> RetornaRejeitados()
		{
			dal = new DALCampanha();
			return dal.RetornaRejeitados();
		}
		public Task<int> GetClienteIDByArquivo(string arquivo)
		{
			dal = new DALCampanha();
			return dal.GetClienteIDByArquivo(arquivo);

		}
		public Task<int> GravaRetornoRatinhos(CampanhaModel c)
		{
			dal = new DALCampanha();
			return dal.GravaRetornoRatinhos(c);
		}

		public Task<int> AdicionaRatinho(CampanhaModel c)
		{
			dal = new DALCampanha();
			return dal.AdicionaRatinho(c);
		}

	}
}
