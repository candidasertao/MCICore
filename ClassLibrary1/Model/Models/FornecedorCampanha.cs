using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class FornecedorCampanhaModel:BaseEntity
    {
		[JsonProperty("fornecedorid", NullValueHandling = NullValueHandling.Ignore)]
		public int FornecedorID { get; set; }
		[JsonProperty("atraso", NullValueHandling = NullValueHandling.Ignore)]
		public int? Atraso { get; set; }
		[JsonProperty("atrasotime", NullValueHandling = NullValueHandling.Ignore)]
		public TimeSpan AtrasoTime { get; set; }
		[JsonProperty("distribuicao", NullValueHandling = NullValueHandling.Ignore)]
		public decimal Distribuicao { get; set; }
		[JsonProperty("nome", NullValueHandling = NullValueHandling.Ignore)]
		public string Nome  { get; set; }
		[JsonProperty("capacidade", NullValueHandling = NullValueHandling.Ignore)]
		public int Capacidade { get; set; }
		[JsonProperty("statusenvio", NullValueHandling = NullValueHandling.Ignore)]
		public byte StatusEnvio { get; set; }

	
	}
}
