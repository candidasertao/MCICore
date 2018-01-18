using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class ValidacaoDeSessao
    {
		[JsonProperty("token")]
		public string Token { get; set; }
	}
}
