using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class CampanhaRequisicaoRelatorioModel:BaseEntity
    {
		[JsonProperty("requisicaoid", NullValueHandling = NullValueHandling.Ignore)]
		public int RequisicaoID { get; set; }
		[JsonProperty("cliente", NullValueHandling = NullValueHandling.Ignore)]
		public ClienteModel Cliente { get; set; }
		[JsonProperty("usuario", NullValueHandling = NullValueHandling.Ignore)]
		public UsuarioModel Usuario { get; set; }
		[JsonProperty("emails", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<string> Emails { get; set; }
		[JsonProperty("carteiras", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<CarteiraModel> Carteiras { get; set; }
		[JsonProperty("tiporelatorio", NullValueHandling = NullValueHandling.Ignore)]
		public TipoRelatorioEnum TipoRelatorio { get; set; }
		[JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime Data { get; set; }
		[JsonProperty("arquivo", NullValueHandling = NullValueHandling.Ignore)]
		public string Arquivo { get; set; }
		[JsonProperty("statusrelatorio", NullValueHandling = NullValueHandling.Ignore)]
		public StatusRelatorioEnum StatusRelatorio { get; set; }

		[JsonProperty("tamanho", NullValueHandling = NullValueHandling.Ignore)]
		public decimal? Tamanho { get; set; }




	}
}
