using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class GrupoUsuarioPaginas
    {
        [JsonProperty("grupousuariopaginaid", NullValueHandling = NullValueHandling.Ignore)]
        public int GrupoUsuarioPaginaID { get; set; }
        [JsonProperty("tipoacesso", NullValueHandling = NullValueHandling.Ignore)]
        public TipoAcessoSistemaEnums TipoAcesso { get; set; }
        [JsonProperty("pagina", NullValueHandling = NullValueHandling.Ignore)]
        public PaginaModel Pagina { get; set; }
	}
}
