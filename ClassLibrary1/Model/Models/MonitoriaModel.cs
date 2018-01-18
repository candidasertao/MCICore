using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
	public class MonitoriaModel:BaseEntity
	{
		public class UsuarioMonitoria
		{
			public string Usuario { get; set; }
			public DateTime DataEnviar { get; set; }
		}
		public class StatusQuantidade
		{
			[JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
			public string Status { get; set; }
			[JsonProperty("quantidade", NullValueHandling = NullValueHandling.Ignore)]
			public int Quantidade { get; set; }
		}
		public class CarteiraArquivos
		{
			[JsonProperty("carteira", NullValueHandling = NullValueHandling.Ignore)]
			public string Carteira { get; set; }
			[JsonProperty("carteiraid", NullValueHandling = NullValueHandling.Ignore)]
			public int CarteiraID { get; set; }
			[JsonProperty("arquivos", NullValueHandling = NullValueHandling.Ignore)]
			public List<Arquivos> Arquivos { get; set; }
			[JsonProperty("horalimite", NullValueHandling = NullValueHandling.Ignore)]
			public TimeSpan HoraLimite { get; set; }
			[JsonProperty("datadia", NullValueHandling = NullValueHandling.Ignore)]
			public DateTime DataDia { get; set; }
		}
		public class Arquivos
		{
			[JsonProperty("fornecedoresmin", NullValueHandling = NullValueHandling.Ignore)]
			public IEnumerable<FornecedorMinModel> FornecedoresMin { get; set; }
			[JsonProperty("arquivo", NullValueHandling = NullValueHandling.Ignore)]
			public string Arquivo { get; set; }
			[JsonProperty("lotes", NullValueHandling = NullValueHandling.Ignore)]
			public IEnumerable<CampanhaGridLotesModel> Lotes { get; set; }
			[JsonProperty("usuario", NullValueHandling = NullValueHandling.Ignore)]
			public string Usuario { get; set; }
			[JsonProperty("datacadastro", NullValueHandling = NullValueHandling.Ignore)]
			public DateTime DataCadastro { get; set; }
			[JsonProperty("arquivoid", NullValueHandling = NullValueHandling.Ignore)]
			public int ArquivoID { get; set; }
			[JsonProperty("quantidade", NullValueHandling = NullValueHandling.Ignore)]
			public int Quantidade { get; set; }

		}

		public class ArquivosDia
		{
			[JsonProperty("datadia", NullValueHandling = NullValueHandling.Ignore)]
			public DateTime DataDia { get; set; }
			[JsonProperty("cartarquivos", NullValueHandling = NullValueHandling.Ignore)]
			public IEnumerable<CarteiraArquivos> CartAqruivos { get; set; }

		}
		[JsonProperty("quantidadebystatus", NullValueHandling = NullValueHandling.Ignore)]
		public List<StatusQuantidade> QuantidadeByStatus { get; set; }
		[JsonProperty("carteiraarquivos", NullValueHandling = NullValueHandling.Ignore)]
		public List<CarteiraArquivos> CartArquivos { get; set; }
		[JsonProperty("cliente", NullValueHandling = NullValueHandling.Ignore)]
		public ClienteModel Cliente { get; set; }
		[JsonProperty("usuario", NullValueHandling = NullValueHandling.Ignore)]
		public UsuarioModel Usuario { get; set; }
		[JsonProperty("totalregistros", NullValueHandling = NullValueHandling.Ignore)]
		public int TotalRegistros { get; set; }
		[JsonProperty("totalarquivos", NullValueHandling = NullValueHandling.Ignore)]
		public int TotalArquivos { get; set; }

		[JsonProperty("cartarquivosdia", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<ArquivosDia> CartArquivosDia { get; set; }




		public MonitoriaModel()
		{
			QuantidadeByStatus = new List<StatusQuantidade>() { };
			
		}

	}
}
