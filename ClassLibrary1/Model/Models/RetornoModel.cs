using FluentValidation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
	public class RetornoModelValidatorApi: AbstractValidator<RetornoModel>
	{

		public RetornoModelValidatorApi()
		{
			RuleFor(a => a.DataFinal).NotNull().NotEmpty();
			RuleFor(a => a.DataInicial).NotNull().NotEmpty();
			RuleFor(a => a.Token).NotNull().NotEmpty();

		}
	}

	public class RetornoModel:BaseEntity
    {


		#region IOPeople
		[JsonProperty("score", NullValueHandling = NullValueHandling.Ignore)]
		public decimal? Score { get; set; }

		[JsonProperty("class", NullValueHandling = NullValueHandling.Ignore)]
		public string ClassificacaoIOPeople { get; set; }
        
		[JsonProperty("comentarioadicional", NullValueHandling = NullValueHandling.Ignore)]
		public string ComentarioAdicional { get; set; }
		[JsonProperty("idclassificacaop", NullValueHandling = NullValueHandling.Ignore)]
		public int? IDClassificaoP { get; set; }
        #endregion



        public int FornecedorID { get; set; }
        public long CampanhaID { get; set; }

        [JsonProperty("retornocliente", NullValueHandling = NullValueHandling.Ignore)]
		public string RetornoCliente { get; set; }
		[JsonProperty("idcliente", NullValueHandling = NullValueHandling.Ignore)]
		public string IDCliente { get; set; }
		[JsonProperty("texto", NullValueHandling = NullValueHandling.Ignore)]
		public string Texto { get; set; }
		[JsonProperty("arquivo", NullValueHandling = NullValueHandling.Ignore)]
		public string Arquivo { get; set; }
		[JsonProperty("celular", NullValueHandling = NullValueHandling.Ignore)]
		public decimal Celular { get; set; }
	
		[JsonProperty("clienteid", NullValueHandling = NullValueHandling.Ignore)]
		public int? ClienteID { get; set; }
		[JsonProperty("carteira", NullValueHandling = NullValueHandling.Ignore)]
		public string Carteira { get; set; }
		[JsonProperty("dataretorno", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime DataRetorno { get; set; }
		[JsonProperty("classificacao", NullValueHandling = NullValueHandling.Ignore)]
		public ClassificaoRetornoEnums Classificacao { get; set; }
		[JsonProperty("totalregistros", NullValueHandling = NullValueHandling.Ignore)]
		public int? TotalRegistros { get; set; }
		[JsonProperty("usuarioid", NullValueHandling = NullValueHandling.Ignore)]
		public int? UsuarioID { get; set; }
		[JsonProperty("statusretornos", NullValueHandling = NullValueHandling.Ignore)]
		public byte? StatusRetorno { get; set; }
		
	}
}
		