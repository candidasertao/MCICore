using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class FornecedorClienteModel : BaseEntity
    {
        [JsonProperty("fornecedor", NullValueHandling = NullValueHandling.Ignore)]
        public FornecedorModel Fornecedor { get; set; }

        [JsonProperty("cliente", NullValueHandling = NullValueHandling.Ignore)]
        public ClienteModel Cliente { get; set; }

        [JsonProperty("distribuicao", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? Distribuicao { get; set; }

        [JsonProperty("capacidade", NullValueHandling = NullValueHandling.Ignore)]
        public int? Capacidade { get; set; }

        [JsonProperty("envio5min", NullValueHandling = NullValueHandling.Ignore)]
        public int? Envio5min { get; set; }

        [JsonProperty("tipo", NullValueHandling = NullValueHandling.Ignore)]
        public Tipo? Tipo { get; set; }

        [JsonProperty("statusfornecedor", NullValueHandling = NullValueHandling.Ignore)]
        public int StatusFornecedor { get; set; }

        [JsonProperty("usuario", NullValueHandling = NullValueHandling.Ignore)]
        public string Usuario { get; set; }

        [JsonProperty("senha", NullValueHandling = NullValueHandling.Ignore)]
        public string Senha { get; set; }

        [JsonProperty("statusoperacional", NullValueHandling = NullValueHandling.Ignore)]
        public int StatusOperacional { get; set; }

        [JsonProperty("isintegrado", NullValueHandling = NullValueHandling.Ignore)]
        public bool isIntegrado { get; set; }

        [JsonProperty("capacidades", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<FornecedorCapacidadeExtraModel> Capacidades { get; set; }

        [JsonProperty("tipocodigo", NullValueHandling = NullValueHandling.Ignore)]
        public int TipoCodigo { get; set; }

        [JsonProperty("capacidadeextra", NullValueHandling = NullValueHandling.Ignore)]
        public int CapacidadeExtra { get; set; }
    }
}
