using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class InterjeicaoModel
    {
		[JsonProperty("codigo", NullValueHandling = NullValueHandling.Ignore)]
		public int Codigo { get; set; }
		[JsonProperty("interjeicao", NullValueHandling = NullValueHandling.Ignore)]
		public string Interjeicao { get; set; }
		[JsonProperty("cliente", NullValueHandling = NullValueHandling.Ignore)]
		public ClienteModel Cliente { get; set; }
		[JsonProperty("usuario", NullValueHandling = NullValueHandling.Ignore)]
		public UsuarioModel Usuario { get; set; }
		[JsonProperty("classificacao", NullValueHandling = NullValueHandling.Ignore)]
		public ClassificaoRetornoEnums Classificacao { get; set; }

	}
}
