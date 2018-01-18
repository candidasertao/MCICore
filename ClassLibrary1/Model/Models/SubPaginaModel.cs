using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
	public class SubPaginaModel
	{
		[JsonProperty("subpagina", NullValueHandling = NullValueHandling.Ignore)]
		public string SubPagina { get; set; }
		[JsonProperty("subpaginaid", NullValueHandling = NullValueHandling.Ignore)]
		public int? SubPaginaID { get; set; }
	}
}
