using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
	public class FornecedorMinModel
	{
		[JsonProperty("estimativamedia", NullValueHandling = NullValueHandling.Ignore)]
		public int? EstimativaMedia { get; set; }
		[JsonProperty("entregues", NullValueHandling = NullValueHandling.Ignore)]
		public int? Entregues { get; set; }
		[JsonProperty("agendados", NullValueHandling = NullValueHandling.Ignore)]
		public int? Agendados { get; set; }
		[JsonProperty("nome", NullValueHandling = NullValueHandling.Ignore)]
		public string Nome { get; set; }
		[JsonProperty("fornecedorid", NullValueHandling = NullValueHandling.Ignore)]
		public int FornecedorID { get; set; }
		[JsonProperty("capacidade", NullValueHandling = NullValueHandling.Ignore)]
		public decimal? Capacidade { get; set; }
		[JsonProperty("distribuicao", NullValueHandling = NullValueHandling.Ignore)]
		public decimal? Distribuicao { get; set; }
		[JsonProperty("entregatime", NullValueHandling = NullValueHandling.Ignore)]
		public TimeSpan EntregaTime { get; set; }
		[JsonProperty("statusoperacional", NullValueHandling = NullValueHandling.Ignore)]
		public StatusOperacionalFornecedorEnum StatusOperacional { get; set; }



		//entregues no dia de hj e estimativa de tempo
	}
	public class FornecedorModel : BaseEntity
    {


        public FornecedorModel()
        {
            this.Faixa = new List<FaixaModel>() { };
            this.Capacidade = new List<CapacidadeModel>() { };
        }

		[JsonProperty("datavinculo", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime DataVinculo { get; set; }

		[JsonProperty("statusoperacionalfornecedor", NullValueHandling = NullValueHandling.Ignore)]
		public StatusOperacionalFornecedorEnum StatusOperacionalFornecedor { get; set; }
		[JsonProperty("estimativaentrega", NullValueHandling = NullValueHandling.Ignore)]
		public TimeSpan EstimativaEntrega { get; set; }
		[JsonProperty("capacidade5min", NullValueHandling = NullValueHandling.Ignore)]
		public int Capacidade5M { get; set; }
		[JsonProperty("fornecedorid", NullValueHandling = NullValueHandling.Ignore)]
        public int FornecedorID { get; set; }
        [JsonProperty("nome", NullValueHandling = NullValueHandling.Ignore)]
        public string Nome { get; set; }
        [JsonProperty("cpfcnpj", NullValueHandling = NullValueHandling.Ignore)]
        public string CPFCNPJ { get; set; }
        [JsonProperty("endereco", NullValueHandling = NullValueHandling.Ignore)]
        public string Endereco { get; set; }
        [JsonProperty("numero", NullValueHandling = NullValueHandling.Ignore)]
        public string Numero { get; set; }
        [JsonProperty("complemento", NullValueHandling = NullValueHandling.Ignore)]
        public string Complemento { get; set; }
        [JsonProperty("bairro", NullValueHandling = NullValueHandling.Ignore)]
        public string Bairro { get; set; }
        [JsonProperty("cidade", NullValueHandling = NullValueHandling.Ignore)]
        public string Cidade { get; set; }
        [JsonProperty("uf", NullValueHandling = NullValueHandling.Ignore)]
        public string UF { get; set; }
        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime Data { get; set; }
        [JsonProperty("eficiencia", NullValueHandling = NullValueHandling.Ignore)]
        public decimal Eficiencia { get; set; }
        [JsonProperty("entrega", NullValueHandling = NullValueHandling.Ignore)]
        public int Entrega { get; set; }
        [JsonProperty("entregatime", NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan EntregaTime { get; set; }
        [JsonProperty("distribuicao", NullValueHandling = NullValueHandling.Ignore)]
        public decimal Distribuicao { get; set; }
        [JsonProperty("faixa", NullValueHandling = NullValueHandling.Ignore)]
        public List<FaixaModel> Faixa { get; set; }
        [JsonProperty("capacidade", NullValueHandling = NullValueHandling.Ignore)]
        public List<CapacidadeModel> Capacidade { get; set; }
        [JsonProperty("visivel", NullValueHandling = NullValueHandling.Ignore)]
        public bool Visivel { get; set; }
        [JsonProperty("ativo", NullValueHandling = NullValueHandling.Ignore)]
        public bool Ativo { get; set; }
        [JsonProperty("capacidadetotal", NullValueHandling = NullValueHandling.Ignore)]
        public decimal CapacidadeTotal { get; set; }
        [JsonProperty("envioshoje", NullValueHandling = NullValueHandling.Ignore)]
        public decimal EnviosHoje { get; set; }
		[JsonProperty("capacidadeglobal", NullValueHandling = NullValueHandling.Ignore)]
		public int CapacidadeGlobal { get; set; }
		[JsonProperty("agendados", NullValueHandling = NullValueHandling.Ignore)]
		public int Agendados { get; set; }
		[JsonProperty("distribuicaoautomatica", NullValueHandling = NullValueHandling.Ignore)]
		public decimal? DistribuicaoAutomatica { get; set; }
		[JsonProperty("apikey", NullValueHandling = NullValueHandling.Ignore)]
		public string ApiKey { get; set; }
		[JsonProperty("statusfornecedor", NullValueHandling = NullValueHandling.Ignore)]
		public StatusFornecedorEnums StatusFornecedor { get; set; }
		[JsonProperty("contatos", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<ContatoModel> Contatos { get; set; }
        [JsonProperty("login", NullValueHandling = NullValueHandling.Ignore)]
        public LoginViewModel Login { get; set; }
        [DataType(DataType.PostalCode)]
        [JsonProperty("cep", NullValueHandling = NullValueHandling.Ignore)]
        public string CEP { get; set; }
    }
}
