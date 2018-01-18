using Newtonsoft.Json;

namespace Models
{

    public class PaginaModel
    {
		[JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
		public string Url { get; set; }
		[JsonProperty("pagina", NullValueHandling = NullValueHandling.Ignore)]
		public string Pagina { get; set; }
		[JsonProperty("paginaid", NullValueHandling = NullValueHandling.Ignore)]
		public int PaginaID { get; set; }
		[JsonProperty("grupopagina", NullValueHandling = NullValueHandling.Ignore)]
		public GrupoPaginasModel GrupoPagina { get; set; }
		[JsonProperty("subpagina", NullValueHandling = NullValueHandling.Ignore)]
		public SubPaginaModel SubPagina { get; set; }



	}
}
