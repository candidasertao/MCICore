using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Models
{

	public class BaseEntity
	{
		public int Indice { get; set; }

		[JsonProperty("atraso", NullValueHandling = NullValueHandling.Ignore)]
		public TimeSpan Atraso { get; set; }

		[JsonProperty("hora", NullValueHandling = NullValueHandling.Ignore)]
		public int Hora { get; set; }
		[JsonProperty("codigo", NullValueHandling = NullValueHandling.Ignore)]
		public int Codigo { get; set; }

		public string FileName { get; set; }

		public OrigemChamadaEnums OrigemChamada { get; set; }

		

		[JsonProperty("carteiranome", NullValueHandling = NullValueHandling.Ignore)]
		public string CarteiraNome { get; set; }
		[JsonProperty("fornecedornome", NullValueHandling = NullValueHandling.Ignore)]
		public string FornecedorNome { get; set; }
		[JsonProperty("token", NullValueHandling = NullValueHandling.Ignore)]
		public string Token { get; set; }
		[JsonProperty("datainicial", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime? DataInicial { get; set; }
		[JsonProperty("datafinal", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime? DataFinal { get; set; }
		[JsonProperty("carteiraid", NullValueHandling = NullValueHandling.Ignore)]
		public int? CarteiraID { get; set; }
		[JsonProperty("arquivoid", NullValueHandling = NullValueHandling.Ignore)]
		public int? ArquivoID { get; set; }
		[JsonProperty("tamanhopagina", NullValueHandling = NullValueHandling.Ignore)]
		public int? TamanhoPagina { get; set; }
		[JsonProperty("registros", NullValueHandling = NullValueHandling.Ignore)]
		public int Registros { get; set; }
		[JsonProperty("paginaatual", NullValueHandling = NullValueHandling.Ignore)]
		public int? PaginaAtual { get; set; }
		[JsonProperty("paginas", NullValueHandling = NullValueHandling.Ignore)]
		public int Paginas { get; set; }
		[JsonProperty("search", NullValueHandling = NullValueHandling.Ignore)]
		public string Search { get; set; }
		[JsonProperty("quantidade", NullValueHandling = NullValueHandling.Ignore)]
		public int Quantidade { get; set; }

		IEnumerable<CarteiraModel> _CarteiraList;

		[JsonProperty("carteiralist", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<CarteiraModel> CarteiraList { get { return _CarteiraList ?? new CarteiraModel[] { }; } set { _CarteiraList = value; } }

		[JsonProperty("dataenviar", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime DataEnviar { get; set; }
		[JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
		public string Url { get; set; }
		[JsonProperty("quantidadetotal", NullValueHandling = NullValueHandling.Ignore)]
		public int? QuantidadeTotal { get; set; }
	}
}
