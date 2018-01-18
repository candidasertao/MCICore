
using FluentValidation;
using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{

	public class CampanhaModelValidatorApiRatinho : AbstractValidator<CampanhaModel>
	{
		public CampanhaModelValidatorApiRatinho()
		{
			RuleFor(a => a.Data).NotNull().NotEmpty();
			RuleFor(a => a.Celular).NotNull().NotEmpty();
			RuleFor(a => a.Texto).NotNull().NotEmpty();
		}



	}

	public class CampanhaModelValidatorApi : AbstractValidator<CampanhaModel>
	{
		public CampanhaModelValidatorApi()
		{
			RuleFor(a => a.DataFinal).NotNull().NotEmpty();
			RuleFor(a => a.DataInicial).NotNull().NotEmpty();
			RuleFor(a => a.Token).NotNull().NotEmpty();
		}
	}
	public class CampanhaModelValidatorCallBackApi : AbstractValidator<CampanhaModel>
	{
		public CampanhaModelValidatorCallBackApi()
		{
			RuleFor(a => a.CampanhaID).NotNull().NotEmpty().GreaterThan(0);
			RuleFor(a => a.Token).NotNull().NotEmpty();
		}
	}

	public class CampanhaValidatorDLR : AbstractValidator<CampanhaModel>
	{
		public CampanhaValidatorDLR()
		{
			RuleFor(a => a.CampanhaID).NotNull().NotEmpty().GreaterThan(0);
			RuleFor(a => a.Token).NotNull().NotEmpty();
			RuleFor(a => a.DataReport).NotNull().NotEmpty();
			RuleFor(a => a.Report).NotNull().NotEmpty();
		}
	}
	/// <summary>
	/// Validação pra envio de sms via API
	/// </summary>
	public class CampanhaModelValidatorSendSMS : AbstractValidator<CampanhaModel>
	{
		public CampanhaModelValidatorSendSMS()
		{
			RuleFor(customer => customer.Texto) //mensagem
				.NotNull()
				.NotEmpty()
				.MinimumLength(3).MaximumLength(160);

			RuleFor(customer => customer.CarteiraNome) //carteira
			.NotNull()
			.NotEmpty()
			.MinimumLength(3).MaximumLength(150);

			RuleFor(a => a.DataEnviar)//dataenviar
				.NotNull()
				.NotEmpty()
				.GreaterThanOrEqualTo(DateTime.Now.Date);

			RuleFor(camp => camp.Celular) //celular
				.NotNull()
				.NotEmpty()
				.LessThanOrEqualTo(99999999999)
				.GreaterThanOrEqualTo(1170000000);

		}

	}

	public class CampanhaModel : BaseEntity
	{

		public string[] ForadoPadrao { get; set; }

		IEnumerable<VariavelModel> _Variaveis;

		public IEnumerable<VariavelModel> Variaveis { get => _Variaveis == null || !_Variaveis.Any() ? new VariavelModel[] { } : _Variaveis; set => _Variaveis = value; }

		[JsonProperty("celulares", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<decimal> Celulares { get; set; }

		[JsonProperty("campanhaid", NullValueHandling = NullValueHandling.Ignore)]
		public long CampanhaID { get; set; }
		[JsonProperty("cliente", NullValueHandling = NullValueHandling.Ignore)]
		public ClienteModel Cliente { get; set; }
		[JsonProperty("usuario", NullValueHandling = NullValueHandling.Ignore)]
		public UsuarioModel Usuario { get; set; }
		[JsonProperty("arquivo", NullValueHandling = NullValueHandling.Ignore)]
		public ArquivoCampanhaModel Arquivo { get; set; }
		[JsonProperty("carteira", NullValueHandling = NullValueHandling.Ignore)]
		public CarteiraModel Carteira { get; set; }
		[JsonProperty("dataenviaroriginal", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime DataEnviarOriginal { get; set; }
		[JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime Data { get; set; }
		[JsonProperty("operadora", NullValueHandling = NullValueHandling.Ignore)]
		public OperadorasEnums Operadora { get; set; }
		[JsonProperty("fornecedor", NullValueHandling = NullValueHandling.Ignore)]
		public FornecedorModel Fornecedor { get; set; }
		[JsonProperty("idcliente", NullValueHandling = NullValueHandling.Ignore)]
		public string IDCliente { get; set; }
		[JsonProperty("statusenvio", NullValueHandling = NullValueHandling.Ignore)]
		public int StatusEnvio { get; set; }
		[JsonProperty("statusenviooriginal", NullValueHandling = NullValueHandling.Ignore)]
		public int StatusEnvioOriginal { get; set; }
		[JsonProperty("tipocampanha", NullValueHandling = NullValueHandling.Ignore)]
		public TipoCampanhaModel TipoCampanha { get; set; }
		[JsonProperty("tiposms", NullValueHandling = NullValueHandling.Ignore)]
		public Tipo TipoSMS { get; set; }
		[JsonProperty("celular", NullValueHandling = NullValueHandling.Ignore)]
		public decimal Celular { get; set; }
		[JsonProperty("report", NullValueHandling = NullValueHandling.Ignore)]
		public string Report { get; set; }
		[JsonProperty("timecampanha", NullValueHandling = NullValueHandling.Ignore)]
		public TimeSpan TimeCampanha { get; set; }


		[JsonProperty("statusreport", NullValueHandling = NullValueHandling.Ignore)]
		public ReportDeliveryEnums StatusReport { get; set; }
		[JsonProperty("datareport", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime? DataReport { get; set; }

		[JsonProperty("texto", NullValueHandling = NullValueHandling.Ignore)]
		public string Texto { get; set; }

		[JsonProperty("messageid", NullValueHandling = NullValueHandling.Ignore)]
		public string MessageID { get; set; }
		[JsonProperty("tipoinvalido", NullValueHandling = NullValueHandling.Ignore)]
		public TiposInvalidosEnums TipoInvalido { get; set; }
		[JsonProperty("datadia", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime DataDia { get; set; }
		[JsonProperty("prefixo", NullValueHandling = NullValueHandling.Ignore)]
		public PrefixoModel Prefixo { get; set; }
		[JsonProperty("ddd", NullValueHandling = NullValueHandling.Ignore)]
		public byte DDD { get; set; }
		[JsonProperty("uf", NullValueHandling = NullValueHandling.Ignore)]
		public string UF { get; set; }
		[JsonProperty("regiao", NullValueHandling = NullValueHandling.Ignore)]
		public string Regiao { get; set; }
		[JsonProperty("retorno", NullValueHandling = NullValueHandling.Ignore)]
		public string Retorno { get; set; }

		public bool Atualizado { get; set; }
		public string ArquivoZip { get; set; }

		public TipoRetornoErroApiEnum TipoErroApi { get; set; }

		public string CelularInvalido { get; set; }

		public int FornecedorID { get; set; }


		public override bool Equals(object obj)
		{
			CampanhaModel p = obj as CampanhaModel;

			if (p != null && !string.IsNullOrEmpty(p.Texto))
				return p != null && p.Celular == this.Celular && p.Texto == this.Texto;
			else
				return p != null && p.Celular == this.Celular;
		}
		public override int GetHashCode()
		{
			if (!string.IsNullOrEmpty(this.Texto))
				return (this.Celular.GetHashCode() ^ this.Texto.GetHashCode());
			else
				return this.Celular.GetHashCode();
		}

	}
}
