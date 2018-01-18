using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{

	public class EmailViewModel : BaseEntity
	{


		[Required]
		[JsonProperty("nome")]
		public string Nome { get; set; }

		[Required]
		[JsonProperty("email")]
		[EmailAddress]
		public string Email { get; set; }

		[Required]
		[JsonProperty("telefone")]
		public string Telefone { get; set; }

		public EmailViewModel(string email, string nome)
		{
			Nome = nome;
			Email = email;
		}

		public EmailViewModel() { }

		public EmailViewModel(string email)
		{
			Email = email;
		}
	}
}
