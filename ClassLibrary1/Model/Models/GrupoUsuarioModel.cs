using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class GrupoUsuariosModel
    {
		[JsonProperty("grupousuarioid", NullValueHandling = NullValueHandling.Ignore)]
		public int GrupoUsuarioID { get; set; }
		[JsonProperty("nome", NullValueHandling = NullValueHandling.Ignore)]
		public string Nome { get; set; }
		[JsonProperty("saldocompartilhado", NullValueHandling = NullValueHandling.Ignore)]
		public bool SaldoCompartilhado { get; set; }
		[JsonProperty("saldo", NullValueHandling = NullValueHandling.Ignore)]
		public int Saldo { get; set; }
		[JsonProperty("cota", NullValueHandling = NullValueHandling.Ignore)]
		public int Cota { get; set; }
		[JsonProperty("cliente", NullValueHandling = NullValueHandling.Ignore)]
		public ClienteModel Cliente { get; set; }
		[JsonProperty("quantidadeusuarios", NullValueHandling = NullValueHandling.Ignore)]
		public int QuantidadeUsuarios { get; set; }
		[JsonProperty("grupopaginas", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<GrupoUsuarioPaginas> GrupoUserPaginas { get; set; }

		
	}
}
