using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
	public class StatusValor
	{
		public string Status { get; set; }
		public int Quantidade { get; set; }
		public StatusValor(string _s, int _v)
		{
			Status = _s;
			Quantidade = _v;
		}
	}
	public class CampanhaGridLotesModel
	{
		[JsonProperty("lote", NullValueHandling = NullValueHandling.Ignore)]
		public int Lote { get; set; }
		[JsonProperty("quantidade", NullValueHandling = NullValueHandling.Ignore)]
		public int Quantidade { get; set; }
		[JsonProperty("lotes", NullValueHandling = NullValueHandling.Ignore)]
		public int Lotes { get; set; }
		[JsonProperty("dataenviar", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime DataEnviar { get; set; }
		[JsonProperty("datadia", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime DataDia { get; set; }
		[JsonProperty("dataenviarold", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime DataEnviarOld { get; set; }
		[JsonProperty("intervalos", NullValueHandling = NullValueHandling.Ignore)]
		public int Intervalos { get; set; }
		[JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
		public string Data { get; set; }
		[JsonProperty("hora", NullValueHandling = NullValueHandling.Ignore)]
		public string Hora { get; set; }
		[JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
		public string Status { get; set; }
		[JsonProperty("statuslista", NullValueHandling = NullValueHandling.Ignore)]
		public List<StatusValor> StatusLista { get; set; }
		[JsonProperty("fornecedores", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<FornecedorModel> Fornecedores { get; set; }

		public CampanhaGridLotesModel()
		{
			StatusLista = new List<StatusValor>() { };

		}
	}
}
