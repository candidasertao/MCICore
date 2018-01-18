using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class FornecedorCapacidadeExtraModel : BaseEntity
    {
        [JsonProperty("clienteid", NullValueHandling = NullValueHandling.Ignore)]
        public int ClienteID { get; set; }

        [JsonProperty("capacidade", NullValueHandling = NullValueHandling.Ignore)]
        public int Capacidade { get; set; }

        [JsonProperty("ativo", NullValueHandling = NullValueHandling.Ignore)]
        public bool Ativo { get; set; }

    }
}
