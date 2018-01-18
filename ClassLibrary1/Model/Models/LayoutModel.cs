using FluentValidation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
	

	public class LeiauteModel:BaseEntity
    {
		[JsonProperty("leiauteid", NullValueHandling = NullValueHandling.Ignore)]
		public int LeiauteID { get; set; }
		[JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime Data { get; set; }
		[JsonProperty("nome", NullValueHandling = NullValueHandling.Ignore)]
		public string Nome { get; set; }
		[JsonProperty("cliente", NullValueHandling = NullValueHandling.Ignore)]
		public ClienteModel Cliente { get; set; }
		[JsonProperty("usuario", NullValueHandling = NullValueHandling.Ignore)]
		public UsuarioModel Usuario { get; set; }
		[JsonProperty("visivel", NullValueHandling = NullValueHandling.Ignore)]
		public bool Visivel { get; set; }
		[JsonProperty("padrao", NullValueHandling = NullValueHandling.Ignore)]
		public bool Padrao { get; set; }
		[JsonProperty("leiautevariaveis", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<LeiauteViariaveisModel> LeiauteVariaveis { get; set; }
        [JsonProperty("especial", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsEspecial { get; set; }
    }
}
