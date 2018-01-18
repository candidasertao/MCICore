using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class FornecedorServicoModel
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public int Id { get; set; }

        [JsonProperty("datainicio", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime DataInicio { get; set; }

        [JsonProperty("datafim", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? DataFim { get; set; }
        
        [JsonProperty("ativo", NullValueHandling = NullValueHandling.Ignore)]
        public bool isAtivo { get; set; }

        [JsonProperty("imediato", NullValueHandling = NullValueHandling.Ignore)]
        public bool isImediato { get; set; }
    }
}
