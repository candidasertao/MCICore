using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class CarteiraTelefonesModel
    {
		[JsonProperty("numero", NullValueHandling = NullValueHandling.Ignore)]
		public decimal Numero { get; set; }
		[JsonProperty("descricao", NullValueHandling = NullValueHandling.Ignore)]
		public string Descricao { get; set; }
		[JsonProperty("codigo", NullValueHandling = NullValueHandling.Ignore)]
		public int Codigo { get; set; }
	}
}
