using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
	
	public class ContatoModel
    {
        [JsonProperty("codigo", NullValueHandling = NullValueHandling.Ignore)]
        public int Codigo { get; set; }
        [JsonProperty("descricao", NullValueHandling = NullValueHandling.Ignore)]
        public string Descricao { get; set; }
        [JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
        public string Email { get; set; }
        [JsonProperty("telefone", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? Celular { get; set; }
	}
}
