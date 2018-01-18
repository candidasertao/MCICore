using FluentValidation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{

	public class CarteiraModellValidator : AbstractValidator<CarteiraModel>
	{
		public CarteiraModellValidator()
		{
			RuleFor(a => a.Carteira) //nome
				.NotEmpty().WithMessage($"O campo CARTEIRA é obrigatório")
				.MinimumLength(3).MaximumLength(160);

			RuleFor(a => a.DiaInicio) //carteira
		.NotEmpty().WithMessage(a => $"O campo DIA INÍCIO não pode ser vazio");

			RuleFor(a => a.Limite)//limite 
				.NotEmpty().WithMessage($"O campo LIMITE não pode ser vazio");
			
		}

	}
	public class CarteiraModel : BaseEntity
	{
		[JsonProperty("enviadosperiodo", NullValueHandling = NullValueHandling.Ignore)]
		public int EnviadosPeriodo { get; set; }

		[JsonProperty("statusenviocarteira", NullValueHandling = NullValueHandling.Ignore)]
		public byte StatusEnvioCarteira { get; set; }
		 
		[JsonProperty("agendados", NullValueHandling = NullValueHandling.Ignore)]
		public int Agendados { get; set; }

		[JsonProperty("carteira", NullValueHandling = NullValueHandling.Ignore)]
		public string Carteira { get; set; }

		[JsonProperty("idcarteira", NullValueHandling = NullValueHandling.Ignore)]
		public string IDCarteira { get; set; }

		[JsonProperty("usuario", NullValueHandling = NullValueHandling.Ignore)]
		public UsuarioModel Usuario { get; set; }

		[JsonProperty("cliente", NullValueHandling = NullValueHandling.Ignore)]
		public ClienteModel Cliente { get; set; }

		[JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime Data { get; set; }

		[JsonProperty("limite", NullValueHandling = NullValueHandling.Ignore)]
		public int? Limite { get; set; }

		[JsonProperty("periodicidade", NullValueHandling = NullValueHandling.Ignore)]
		public short? Periodicidade { get; set; }

		[JsonProperty("diainicio", NullValueHandling = NullValueHandling.Ignore)]
		public int? DiaInicio { get; set; }

		[JsonProperty("porcentagemaviso", NullValueHandling = NullValueHandling.Ignore)]
		public int? PorcentagemAviso { get; set; }

		[JsonProperty("visivel", NullValueHandling = NullValueHandling.Ignore)]
		public bool Visivel { get; set; }

		[JsonProperty("higieniza", NullValueHandling = NullValueHandling.Ignore)]
		public bool Higieniza { get; set; }

		[JsonProperty("diashigienizacao", NullValueHandling = NullValueHandling.Ignore)]
		public int? DiasHigienizacao { get; set; }

		[JsonProperty("enviadoshoje", NullValueHandling = NullValueHandling.Ignore)]
		public int? EnviadosHoje { get; set; }

		[JsonProperty("utlimos7dias", NullValueHandling = NullValueHandling.Ignore)]
		public int? Ultimos7Dias { get; set; }

		[JsonProperty("utlimos15dias", NullValueHandling = NullValueHandling.Ignore)]
		public int? Ultimos15Dias { get; set; }

		[JsonProperty("horalimite", NullValueHandling = NullValueHandling.Ignore)]
		public TimeSpan HoraLimite { get; set; }

		[JsonProperty("utlimos30dias", NullValueHandling = NullValueHandling.Ignore)]
		public int? Ultimos30Dias { get; set; }

		[JsonProperty("quantidadedisponivel", NullValueHandling = NullValueHandling.Ignore)]
		public int QuantidadeDisponivel { get; set; }

		[JsonProperty("segmentacao", NullValueHandling = NullValueHandling.Ignore)]
		public SegmentacaoModel Segmentacao { get; set; }

		[JsonProperty("carteiratelefone", NullValueHandling = NullValueHandling.Ignore)]
		public List<CarteiraTelefonesModel> CarteiraTelefone { get; set; }

		[JsonProperty("bloqueioenvio", NullValueHandling = NullValueHandling.Ignore)]
		public bool BloqueioEnvio { get; set; }

		[JsonProperty("consumoperiodo", NullValueHandling = NullValueHandling.Ignore)]
		public int ConsumoPeriodo { get; set; }

		[JsonProperty("nameinfile", NullValueHandling = NullValueHandling.Ignore)]
		public string NameInFile { get; set; }





	}
}
