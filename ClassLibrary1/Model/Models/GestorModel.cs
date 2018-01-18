using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
	
    public class GestorModel:BaseEntity
    {
		[JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
		public string Email { get; set; }
		[JsonProperty("celular", NullValueHandling = NullValueHandling.Ignore)]
		public decimal Celular { get; set; }

		[JsonProperty("arquivopadrao", NullValueHandling = NullValueHandling.Ignore)]
		public string ArquivoPadrao { get; set; }
		[JsonProperty("gestorid", NullValueHandling = NullValueHandling.Ignore)]
		public int GestorID { get; set; }
		[JsonProperty("nome", NullValueHandling = NullValueHandling.Ignore)]
		[Required]
		public string Nome { get; set; }
		[JsonProperty("cliente", NullValueHandling = NullValueHandling.Ignore)]
		public ClienteModel Cliente { get; set; }
		[JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime Data { get; set; }
		[JsonProperty("emails", NullValueHandling = NullValueHandling.Ignore)]
		public List<string> Emails { get; set; }
		[JsonProperty("telefones", NullValueHandling = NullValueHandling.Ignore)]
		public List<decimal> Telefones { get; set; }
		[JsonProperty("carteiras", NullValueHandling = NullValueHandling.Ignore)]
		public List<CarteiraModel> Carteiras { get; set; }
		[JsonProperty("tipocampanha", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<TipoCampanhaModel> TipoCampanha { get; set; }
		public GestorModel()
		{
			Emails = new List<string>() { };
			Telefones = new List<decimal>() { };
			Carteiras = new List<CarteiraModel>() { };
		}
	}
}
