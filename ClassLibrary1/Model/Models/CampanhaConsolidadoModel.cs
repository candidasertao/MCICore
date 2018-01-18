using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class CampanhaConsolidadoModel
    {
		[JsonProperty("enviada")]
		public int Enviada { get; set; }
		[JsonProperty("expirada")]
		public int Expirada { get; set; }
		[JsonProperty("erro")]
		public int Erro { get; set; }
		[JsonProperty("suspensa")]
		public int Suspensa { get; set; }
		[JsonProperty("entregue")]
		public int Entregue { get; set; }
		[JsonProperty("excluida")]
		public int Excluida { get; set; }

		[JsonProperty("datadia")]
		public DateTime DataDia { get; set; }

		[JsonProperty("carteira")]
		public CarteiraModel Carteira { get; set; }

		[JsonProperty("cliente")]
		public ClienteModel Cliente { get; set; }
		[JsonProperty("usuario")]
		public UsuarioModel Usuario { get; set; }
		[JsonProperty("dataenviar")]
		public DateTime DataEnviar { get; set; }
		[JsonProperty("arquivo")]
		public ArquivoCampanhaModel Arquivo { get; set; }

		[JsonProperty("acima160caracteres")]
		public int Acima160Caracteres { get; set; }
		[JsonProperty("higienizado")]
		public int Higienizado { get; set; }
		[JsonProperty("blacklist")]
		public int Blacklist { get; set; }
		[JsonProperty("celularinvalido")]
		public int CelularInvalido { get; set; }
		[JsonProperty("codigo")]
		public int Codigo { get; set; }

	}
}
