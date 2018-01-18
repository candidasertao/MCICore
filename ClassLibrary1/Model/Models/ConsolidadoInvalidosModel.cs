using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
   public class ConsolidadoInvalidosModel:BaseEntity
    {
		[JsonProperty("duplicado", NullValueHandling = NullValueHandling.Ignore)]
		public int Duplicado { get; set; }
		[JsonProperty("leiauteinvalido", NullValueHandling = NullValueHandling.Ignore)]
		public int LayoutInvalido { get; set; }
		[JsonProperty("acima160caracteres", NullValueHandling = NullValueHandling.Ignore)]
        public int Acima160Caracteres { get; set; }
        [JsonProperty("higienizado", NullValueHandling = NullValueHandling.Ignore)]
        public int Higienizado { get; set; }
        [JsonProperty("celularinvalido", NullValueHandling = NullValueHandling.Ignore)]
        public int CelularInvalido { get; set; }
        [JsonProperty("blacklist", NullValueHandling = NullValueHandling.Ignore)]
        public int BlackList { get; set; }
        [JsonProperty("filtrado", NullValueHandling = NullValueHandling.Ignore)]
        public int Filtrado { get; set; }
        [JsonProperty("arquivo", NullValueHandling = NullValueHandling.Ignore)]
        public string Arquivo { get; set; }
        [JsonProperty("carteira", NullValueHandling = NullValueHandling.Ignore)]
        public string Carteira { get; set; }
        [JsonProperty("datadia", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime DataDia { get; set; }


    }
}
