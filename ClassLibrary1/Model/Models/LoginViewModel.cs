using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class LoginViewModel
    {
        [JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
        public string Email { get; set; }

        [JsonProperty("nome", NullValueHandling = NullValueHandling.Ignore)]
		public string Nome { get; set; }

		[JsonProperty("username", NullValueHandling = NullValueHandling.Ignore)]
		[Required]
		public string Username { get; set; }

		[JsonProperty("password", NullValueHandling = NullValueHandling.Ignore)]
		[Required]
		public string Password { get; set; }

		[JsonProperty("newpassword", NullValueHandling = NullValueHandling.Ignore)]
		public string NewPassword { get; set; }

		[JsonProperty("token", NullValueHandling = NullValueHandling.Ignore)]
		public string Token { get; set; }

		[JsonProperty("role", NullValueHandling = NullValueHandling.Ignore)]
		public string Role{ get; set; }

		[JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
		public string ID { get; set; }

		[JsonProperty("guid", NullValueHandling = NullValueHandling.Ignore)]
		public string Guid { get; set; }

		[JsonProperty("admperfil", NullValueHandling = NullValueHandling.Ignore)]
		public bool AdmPerfil { get; set; }

		[JsonProperty("grupousuariopages", NullValueHandling = NullValueHandling.Ignore)]
		public dynamic GrupoUsuarioPages { get; set; }

        [JsonProperty("organizacao", NullValueHandling = NullValueHandling.Ignore)]
        [Required]
        public string Organizacao { get; set; }
    }
}
