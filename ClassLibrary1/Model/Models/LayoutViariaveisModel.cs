using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
   public class LeiauteViariaveisModel
    {
		[JsonProperty("variavel", NullValueHandling = NullValueHandling.Ignore)]
		public string Variavel { get; set; }
		[JsonProperty("idcoluna", NullValueHandling = NullValueHandling.Ignore)]
		public int IDColuna { get; set; }
		[JsonProperty("inicioleitura", NullValueHandling = NullValueHandling.Ignore)]
		public int? InicioLeitura { get; set; }
		[JsonProperty("quantidadecaracteres", NullValueHandling = NullValueHandling.Ignore)]
		public int? QuantidadeCaracteres { get; set; }
	}
}
