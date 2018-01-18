using FluentValidation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
	public class UsuarioModelValidator : AbstractValidator<UsuarioModel>
	{
		public UsuarioModelValidator()
		{
			RuleFor(a => a.Nome) //nome
				.NotEmpty().WithMessage("O campo padrão é obrigatório")
				.MinimumLength(3).MaximumLength(160);

			RuleFor(a => a.Email)//email
			.NotEmpty().WithMessage("O campo e-mail não pode ser vazio")
			.MaximumLength(155).WithMessage("O Campo e-mail deve ter no máximo 150 caracteres");


			RuleFor(a => a.LoginUser)//ti0pocampanha
				.NotEmpty().WithMessage("O campo login do usuário não pode ser vazio")
				.MaximumLength(25).WithMessage("O campo login deve ter no máximo 25 caracteres");


			RuleFor(a => a.GrupoUsuario.GrupoUsuarioID) //leiaute
				.NotNull().WithMessage("O campo perfil é obrigatório")
				.GreaterThan(0).WithMessage("Valor de perfil inválido");
		}

	}

	public  class UsuarioModel:BaseEntity
    {

		[JsonProperty("usuarioid", NullValueHandling = NullValueHandling.Ignore)]
		public int UsuarioID { get; set; }

		[JsonProperty("quantidadecarteiras", NullValueHandling = NullValueHandling.Ignore)]
		public int QuantidadeCarteiras { get; set; }

		[JsonProperty("nome", NullValueHandling = NullValueHandling.Ignore)]
		public string Nome { get; set; }

		[JsonProperty("loginuser", NullValueHandling = NullValueHandling.Ignore)]
		public string LoginUser { get; set; }

		[JsonProperty("senha")]
		public string Senha { get; set; }

		[JsonProperty("cota", NullValueHandling = NullValueHandling.Ignore)]
		public int? Cota { get; set; }

		[JsonProperty("cliente", NullValueHandling = NullValueHandling.Ignore)]
		public ClienteModel Cliente { get; set; }

		[JsonProperty("saldo", NullValueHandling = NullValueHandling.Ignore)]
		public int Saldo { get; set; }

		[JsonProperty("saldocotailimitado", NullValueHandling = NullValueHandling.Ignore)]
		public bool SaldoCotaIlimitado { get; set; }

		[JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
        public string Email { get; set; }

		[JsonProperty("telefone", NullValueHandling = NullValueHandling.Ignore)]
		public decimal Telefone { get; set; }

		[JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime Data { get; set; }

		[JsonProperty("ativo", NullValueHandling = NullValueHandling.Ignore)]
		public bool Ativo { get; set; }

		[JsonProperty("admperfil", NullValueHandling = NullValueHandling.Ignore)]
		public bool AdmPerfil { get; set; }

		[JsonProperty("grupousuario", NullValueHandling = NullValueHandling.Ignore)]
		public GrupoUsuariosModel GrupoUsuario { get; set; }

		[JsonProperty("carteiras", NullValueHandling = NullValueHandling.Ignore)]
		public List<CarteiraModel> Carteiras { get; set; }

		public UsuarioModel()
		{
			Carteiras = new List<CarteiraModel>() { };
		}
	}
}
