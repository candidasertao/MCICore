using FluentValidation;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Models
{
	public class PadraoPostagensModelValidator : AbstractValidator<PadraoPostagensModel>
	{
		public PadraoPostagensModelValidator()
		{
			RuleFor(a => a.Padrao) //nome
				.NotEmpty().WithMessage("O campo padrão é obrigatório")
				.MinimumLength(3).MaximumLength(160);

			RuleFor(a => a.Carteira.CarteiraID) //carteira
			.NotEmpty().WithMessage("O campo carteira não pode ser vazio");
			

			RuleFor(a => a.TipoCampanha.TipoCampanhaID)//ti0pocampanha
				.NotEmpty().WithMessage("O campo tipocampanha não pode ser vazio");
				

			RuleFor(a => a.Leiaute.LeiauteID) //leiaute
				.NotEmpty().WithMessage("O campo leiaute não pode ser vazio.")
				.NotNull().WithMessage("O campo leiaute não pode ser vazio.");
		}

	}

	public class PadraoPostagensModel:BaseEntity
    {

		[JsonProperty("codigo", NullValueHandling = NullValueHandling.Ignore)]
		public int Codigo { get; set; }
		[JsonProperty("padrao", NullValueHandling = NullValueHandling.Ignore)]
		public string Padrao { get; set; }

		public byte[] Linhas { get; set; }

		public bool ForaPadrao { get; set; }
		[JsonProperty("cliente", NullValueHandling = NullValueHandling.Ignore)]
		public ClienteModel Cliente { get; set; }
		[JsonProperty("carteira", NullValueHandling = NullValueHandling.Ignore)]
		public CarteiraModel Carteira { get; set; }
		[JsonProperty("tipocampanha", NullValueHandling = NullValueHandling.Ignore)]
		public TipoCampanhaModel TipoCampanha { get; set; }
		[JsonProperty("usuario", NullValueHandling = NullValueHandling.Ignore)]
		public UsuarioModel Usuario { get; set; }
		[JsonProperty("leiaute", NullValueHandling = NullValueHandling.Ignore)]
		public LeiauteModel Leiaute { get; set; }
	}
}
