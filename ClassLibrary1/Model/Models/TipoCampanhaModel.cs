using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
	
    public class TipoCampanhaModel:BaseEntity
    {
		[JsonProperty("tipocampanhaid", NullValueHandling = NullValueHandling.Ignore)]
		public int TipoCampanhaID { get; set; }
		[JsonProperty("tipocampanha", NullValueHandling = NullValueHandling.Ignore)]
		public string TipoCampanha { get; set; }
		[JsonProperty("cliente", NullValueHandling = NullValueHandling.Ignore)]
		public ClienteModel Cliente { get; set; }
		[JsonProperty("visivel", NullValueHandling = NullValueHandling.Ignore)]
		public bool Visivel { get; set; }

		public override string ToString()
		{
			return TipoCampanha;
		}
	}
}
