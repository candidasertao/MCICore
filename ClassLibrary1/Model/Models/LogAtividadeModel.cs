using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class LogAtividadeModel
    {
		[JsonProperty("cliente", NullValueHandling = NullValueHandling.Ignore)]
		public ClienteModel Cliente { get; set; }
		[JsonProperty("usuario", NullValueHandling = NullValueHandling.Ignore)]
		public UsuarioModel Usuario { get; set; }
		[JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime Data { get; set; }
		[JsonProperty("descricao", NullValueHandling = NullValueHandling.Ignore)]
		public string Descricao { get; set; }
		[JsonProperty("tipo", NullValueHandling = NullValueHandling.Ignore)]
		public TiposLogAtividadeEnums Tipo { get; set; }
        [JsonProperty("modulo", NullValueHandling = NullValueHandling.Ignore)]
        public ModuloAtividadeEnumns Modulo { get; set; }
        [JsonProperty("carteira", NullValueHandling = NullValueHandling.Ignore)]
        public CarteiraModel Carteira { get; set; }

    }
}
	