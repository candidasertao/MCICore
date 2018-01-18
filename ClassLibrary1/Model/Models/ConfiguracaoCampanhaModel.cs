using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class ConfiguracaoCampanhaModel
    {
		[JsonProperty("codigo"), Key]
		public int Codigo { get; set; }
		[JsonProperty("dataenviar", Required= Required.Always), Required]
		public DateTime DataEnviar { get; set; }
		[JsonProperty("lote", Required = Required.Always), Required]
		public int Lote { get; set; }
		[JsonProperty("arquivo", Required = Required.Always), Required]
		public string Arquivo { get; set; }
		[JsonProperty("intervalo", Required = Required.Always), Required]
		public int Intervalo { get; set; }
		[JsonProperty("carteiraid", Required = Required.Always), Required]
		public int CarteiraID { get; set; }
	}
}
