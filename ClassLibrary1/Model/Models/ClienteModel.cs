using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
	
	public class ClienteModel:BaseEntity
    {

		[JsonProperty("telefone", NullValueHandling = NullValueHandling.Ignore)]
		public decimal Telefone { get; set; }
		[JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
		public string Email { get; set; }

		[JsonProperty("clienteid", NullValueHandling = NullValueHandling.Ignore)]
		public int ClienteID { get; set; }
	
		[JsonProperty("nome", NullValueHandling = NullValueHandling.Ignore)]
		public string Nome { get; set; }

		
		[JsonProperty("cnpj", NullValueHandling = NullValueHandling.Ignore)]
		public string CNPJ { get; set; }

		
		[JsonProperty("endereco", NullValueHandling = NullValueHandling.Ignore)]
		public string Endereco { get; set; }

		[JsonProperty("numero", NullValueHandling = NullValueHandling.Ignore)]
		public string Numero { get; set; }

		[JsonProperty("complemento", NullValueHandling = NullValueHandling.Ignore)]
		public string Complemento { get; set; }
		[JsonProperty("bairro", NullValueHandling = NullValueHandling.Ignore)]
		public string Bairro { get; set; }
		[DataType(DataType.PostalCode)]
		[JsonProperty("cep", NullValueHandling = NullValueHandling.Ignore)]
		public string CEP { get; set; }
		[JsonProperty("cidade", NullValueHandling = NullValueHandling.Ignore)]
		public string Cidade { get; set; }
		[JsonProperty("uf", NullValueHandling = NullValueHandling.Ignore)]
		public string UF { get; set; }
		[JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime Data { get; set; }

		[JsonProperty("contatos", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<ContatoModel> Contatos { get; set; }

		[JsonProperty("pospago", NullValueHandling = NullValueHandling.Ignore)]
		public bool PosPago { get; set; }

	}
}
