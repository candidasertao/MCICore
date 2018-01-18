using FluentValidation;
using FluentValidation.Validators;
using Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DTO
{
	public class CustomValidator<T> : PropertyValidator
	{
		int maxValue { get; set; }

		public CustomValidator(int max) : base("teste") { maxValue = max; }

		protected override bool IsValid(PropertyValidatorContext context)
		{
			throw new NotImplementedException();
		}
	}

	public class CampanhaModelDTOValidator : AbstractValidator<CampanhaModelDTO>
	{
		public CampanhaModelDTOValidator()
		{
			RuleFor(customer => customer.Texto) //mensagem
				.NotNull()
				.NotEmpty()
				.MinimumLength(3).MaximumLength(160);

			RuleFor(customer => customer.Carteira) //carteira
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

	public class CampanhaModelDTO
	{

		[JsonProperty("celular", Required = Required.Always)]
		public decimal Celular { get; set; }

		[JsonProperty("texto", Required = Required.Always)]
		public string Texto { get; set; }

		[JsonProperty("dataenviar", Required = Required.Always)]
		public DateTime DataEnviar { get; set; }

		[JsonProperty("carteira", Required = Required.Always)]
		public string Carteira { get; set; }

		[JsonProperty("campanhaid", Required = Required.Always)]
		public long CampanhaID { get; set; }



		[JsonIgnore]
		public OperadorasEnums Operadora { get; set; }
		[JsonIgnore]
		public TiposInvalidosEnums TipoInvalido { get; set; }
		[JsonIgnore]
		public string Fornecedor { get; set; }
		[JsonIgnore]
		public string IDCliente { get; set; }
		[JsonIgnore]
		public int ClienteID { get; set; }
		[JsonIgnore]
		public int? UsuarioID { get; set; }

		public int CarteiraID { get; set; }

		[JsonIgnore]
		public string TipoCampanha { get; set; }

		public override bool Equals(object obj)
		{
			CampanhaModelDTO p = obj as CampanhaModelDTO;

			return p != null && p.Celular == this.Celular && p.Texto == this.Texto;
		}
		public override int GetHashCode()
		{
			return (this.Celular.GetHashCode() ^ this.Texto.GetHashCode());
		}
	}
}
