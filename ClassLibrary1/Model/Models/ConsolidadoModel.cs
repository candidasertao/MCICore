using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
	public class ConsolidadoModel : BaseEntity
	{


		[JsonProperty("fornecedores", NullValueHandling = NullValueHandling.Ignore)]
		public List<int> Fornecedores { get; set; }

		public int NaoEnviadas { get; set; }
		public int Enviadas { get; set; }
		public int? ClienteID { get; set; }
		public int? Hora { get; set; }
		public int Recebidas { get; set; }

		[JsonProperty("expiracao", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime Expiracao { get; set; }

		[JsonProperty("expirado", NullValueHandling = NullValueHandling.Ignore)]
		public bool Expirado { get; set; }

		public string Carteira { get; set; }


		[JsonProperty("codigo", NullValueHandling = NullValueHandling.Ignore)]
		public int Codigo { get; set; }

		[JsonProperty("arquivo", NullValueHandling = NullValueHandling.Ignore)]
		public string Arquivo { get; set; }

		public DateTime Validade { get; set; }

		public string TipoCampanha { get; set; }

		[JsonProperty("enviados", NullValueHandling = NullValueHandling.Ignore)]
		public int Enviados { get; set; }


		[JsonProperty("entregues", NullValueHandling = NullValueHandling.Ignore)]
		public int Entregues { get; set; }
		[JsonProperty("erros", NullValueHandling = NullValueHandling.Ignore)]
		public int Erros { get; set; }
		[JsonProperty("expiradas", NullValueHandling = NullValueHandling.Ignore)]
		public int Expiradas { get; set; }
		[JsonProperty("exluidas", NullValueHandling = NullValueHandling.Ignore)]
		public int Excluidas { get; set; }
		[JsonProperty("celularinvalido", NullValueHandling = NullValueHandling.Ignore)]
		public int CelularInvalido { get; set; }
		[JsonProperty("acima160caracteres", NullValueHandling = NullValueHandling.Ignore)]
		public int Acima160Caracteres { get; set; }
		[JsonProperty("usuarioid", NullValueHandling = NullValueHandling.Ignore)]
		public int? UsuarioID { get; set; }
		[JsonProperty("blacklist", NullValueHandling = NullValueHandling.Ignore)]
		public int BlackList { get; set; }
		[JsonProperty("higienizado", NullValueHandling = NullValueHandling.Ignore)]
		public int Higienizado { get; set; }
		[JsonProperty("datadia", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime DataDia { get; set; }
		[JsonProperty("suspensos", NullValueHandling = NullValueHandling.Ignore)]
		public int Suspensos { get; set; }

		[JsonProperty("fornecedorid", NullValueHandling = NullValueHandling.Ignore)]
		public int FornecedorID { get; set; }


		[JsonProperty("usuarionome", NullValueHandling = NullValueHandling.Ignore)]
		public string UsuarioNome { get; set; }

		[JsonProperty("canceladas", NullValueHandling = NullValueHandling.Ignore)]
		public int Canceladas { get; set; }


		[JsonProperty("positivo", NullValueHandling = NullValueHandling.Ignore)]
		public int? Positivo { get; set; }
		[JsonProperty("negativo", NullValueHandling = NullValueHandling.Ignore)]
		public int? Negativo { get; set; }
		[JsonProperty("neutro", NullValueHandling = NullValueHandling.Ignore)]
		public int? Neutro { get; set; }
		[JsonProperty("spcapital", NullValueHandling = NullValueHandling.Ignore)]
		public int? SpCapital { get; set; }
		[JsonProperty("spgrande", NullValueHandling = NullValueHandling.Ignore)]
		public int? SpGrande { get; set; }
		[JsonProperty("demaisddd", NullValueHandling = NullValueHandling.Ignore)]
		public int? DemaisDDD { get; set; }
		[JsonProperty("invalidos", NullValueHandling = NullValueHandling.Ignore)]
		public int? Invalidos { get; set; }
        
        [JsonProperty("classificacaoid", NullValueHandling = NullValueHandling.Ignore)]
        public int? ClassificacaoID { get; set; }

        [JsonProperty("classificacao", NullValueHandling = NullValueHandling.Ignore)]
        public string Classificacao { get; set; }



        public int Total
		{
			get { return Suspensos + Canceladas + Excluidas + Erros + Entregues + Expiradas + Enviados + Invalidos ?? 0; }
		}
	}
}
