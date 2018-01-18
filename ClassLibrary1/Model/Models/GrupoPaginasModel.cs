using Newtonsoft.Json;
using System.Collections.Generic;

namespace Models
{
    public class GrupoPaginasModel
    {
        [JsonProperty("grupoid", NullValueHandling = NullValueHandling.Ignore)]
        public int GrupoID { get; set; }
        [JsonProperty("grupo", NullValueHandling = NullValueHandling.Ignore)]
        public string Grupo { get; set; }
       
    }
}
