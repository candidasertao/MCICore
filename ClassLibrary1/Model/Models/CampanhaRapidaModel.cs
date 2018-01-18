using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class CampanhaRapidaModel
    {
		[JsonProperty("idcliente", NullValueHandling = NullValueHandling.Ignore)]
		public string IDCliente { get; set; }
		[JsonProperty("carteiras", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<CarteiraModel> Carteiras { get; set; }
		[JsonProperty("texto", NullValueHandling = NullValueHandling.Ignore)]
		public string Texto { get; set; }
		[JsonProperty("celulares", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<decimal> Celulares { get; set; }
	}
}
